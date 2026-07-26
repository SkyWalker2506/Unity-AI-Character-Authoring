using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace BlackMountains.AuthoringKernel.Editor
{
    public sealed class ExecutionJournal
    {
        public string SchemaVersion { get; set; } = "1";
        public string ExecutionId { get; set; }
        public string ManifestId { get; set; }
        public string WriterId { get; set; }
        public int ProcessId { get; set; }
        public DateTimeOffset ProcessStartTimeUtc { get; set; }
        public string PlanHash { get; set; }
        public List<string> CompletedOperations { get; set; } = new List<string>();
        public List<string> PendingOperations { get; set; } = new List<string>();
        public Dictionary<string, string> InversePayloads { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public DateTimeOffset LastHeartbeatUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LeaseExpiresAtUtc { get; set; } = DateTimeOffset.UtcNow.AddMinutes(2);
        public bool Completed { get; set; }
    }

    public sealed class AuthoringJournalStore
    {
        readonly AuthoringExternalPathPolicy _pathPolicy;

        public AuthoringJournalStore(AuthoringExternalPathPolicy pathPolicy)
        {
            _pathPolicy = pathPolicy ?? throw new ArgumentNullException(nameof(pathPolicy));
        }

        public string Begin(ExecutionJournal journal)
        {
            if (journal == null) throw new ArgumentNullException(nameof(journal));
            string root = _pathPolicy.GetJournalRoot(journal.ManifestId);
            Directory.CreateDirectory(root);
            string executionId = AuthoringIdentifier.Require(journal.ExecutionId, nameof(journal.ExecutionId));
            string path = Path.Combine(root, executionId + ".json");
            WriteAtomically(path, journal);
            return path;
        }

        public void Update(string path, ExecutionJournal journal)
        {
            var validation = _pathPolicy.ValidateExternalFilePath(path);
            if (!validation.Success) throw new InvalidOperationException(validation.Diagnostic.Message);
            WriteAtomically(validation.CanonicalPath, journal);
        }

        public bool HasIncompleteJournal()
        {
            string root = _pathPolicy.GetJournalStorageRoot();
            if (!Directory.Exists(root)) return false;
            foreach (var file in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    var journal = JsonConvert.DeserializeObject<ExecutionJournal>(File.ReadAllText(file));
                    if (journal != null && !journal.Completed) return true;
                }
                catch (JsonException)
                {
                    return true;
                }
            }
            return false;
        }

        static void WriteAtomically(string path, ExecutionJournal journal)
        {
            if (journal == null) throw new ArgumentNullException(nameof(journal));
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            string temporaryPath = path + ".tmp." + Guid.NewGuid().ToString("N");
            try
            {
                var json = JsonConvert.SerializeObject(journal, Formatting.Indented);
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }

    public sealed class AuthoringApplyLock : IDisposable
    {
        FileStream _stream;
        bool _disposed;

        AuthoringApplyLock(FileStream stream, string path)
        {
            _stream = stream;
            Path = path;
        }

        public string Path { get; }

        public static AuthoringApplyLock Acquire(AuthoringExternalPathPolicy pathPolicy, string planHash)
        {
            if (pathPolicy == null) throw new ArgumentNullException(nameof(pathPolicy));
            string root = pathPolicy.GetProjectExternalRoot();
            Directory.CreateDirectory(root);
            string lockPath = System.IO.Path.Combine(root, "apply.lock");
            var stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var process = System.Diagnostics.Process.GetCurrentProcess();
            DateTime processStartTimeUtc;
            try
            {
                processStartTimeUtc = process.StartTime.ToUniversalTime();
            }
            catch
            {
                processStartTimeUtc = DateTime.UtcNow;
            }
            var leaseIssuedAtUtc = DateTimeOffset.UtcNow;
            using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 1024, leaveOpen: true))
            {
                stream.SetLength(0);
                writer.WriteLine("projectId=" + pathPolicy.ProjectIdentity.StableProjectId);
                writer.WriteLine("rootHash=" + pathPolicy.ProjectIdentity.CanonicalProjectRootHash);
                writer.WriteLine("host=" + Environment.MachineName);
                writer.WriteLine("pid=" + process.Id);
                writer.WriteLine("processStartTimeUtc=" + processStartTimeUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteLine("leaseIssuedAtUtc=" + leaseIssuedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteLine("leaseExpiresAtUtc=" + leaseIssuedAtUtc.AddMinutes(2).ToString("O", System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteLine("planHash=" + (planHash ?? string.Empty));
                writer.Flush();
                stream.Flush();
                stream.Position = 0;
            }
            return new AuthoringApplyLock(stream, lockPath);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _stream?.Dispose();
            _stream = null;
        }
    }

    public sealed class RecoveryStatus
    {
        public bool RecoveryRequired { get; set; }
        public string Message { get; set; }
    }

    public sealed class RecoveryService
    {
        readonly AuthoringJournalStore _journalStore;

        public RecoveryService(AuthoringJournalStore journalStore)
        {
            _journalStore = journalStore ?? throw new ArgumentNullException(nameof(journalStore));
        }

        public RecoveryStatus Inspect()
        {
            bool incomplete = _journalStore.HasIncompleteJournal();
            return new RecoveryStatus
            {
                RecoveryRequired = incomplete,
                Message = incomplete ? "Incomplete authoring journal detected. New mutating plans are blocked." : "No incomplete authoring journal detected."
            };
        }
    }
}
