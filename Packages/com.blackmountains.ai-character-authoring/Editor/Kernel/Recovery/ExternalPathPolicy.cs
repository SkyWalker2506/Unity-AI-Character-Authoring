using System;
using System.IO;

namespace BlackMountains.AuthoringKernel.Editor
{
    public sealed class AuthoringProjectIdentity
    {
        public string ProjectRoot { get; private set; }
        public string StableProjectId { get; private set; }
        public string CanonicalProjectRootHash { get; private set; }

        public static AuthoringProjectIdentity Create(string projectRoot, string stableProjectId = null)
        {
            if (string.IsNullOrWhiteSpace(projectRoot)) throw new ArgumentException("Project root is required.", nameof(projectRoot));
            string canonicalRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string rootHash = CanonicalSerialization.Sha256(canonicalRoot);
            string resolvedProjectId = string.IsNullOrWhiteSpace(stableProjectId)
                ? "project." + rootHash.Substring(0, 24)
                : AuthoringIdentifier.Require(stableProjectId, nameof(stableProjectId));
            return new AuthoringProjectIdentity
            {
                ProjectRoot = canonicalRoot,
                CanonicalProjectRootHash = rootHash,
                StableProjectId = resolvedProjectId
            };
        }
    }

    public sealed class ExternalPathValidation
    {
        public bool Success { get; set; }
        public string CanonicalPath { get; set; }
        public AuthoringDiagnostic Diagnostic { get; set; }
    }

    public sealed class AuthoringExternalPathPolicy
    {
        public AuthoringExternalPathPolicy(AuthoringProjectIdentity projectIdentity, string externalRoot = null)
        {
            ProjectIdentity = projectIdentity ?? throw new ArgumentNullException(nameof(projectIdentity));
            ExternalRoot = string.IsNullOrWhiteSpace(externalRoot)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlackMountains", "AuthoringKernel")
                : Path.GetFullPath(externalRoot);
            if (IsInside(ExternalRoot, ProjectIdentity.ProjectRoot))
                throw new ArgumentException("External authoring root must not be inside the Unity project.", nameof(externalRoot));
        }

        public AuthoringProjectIdentity ProjectIdentity { get; }
        public string ExternalRoot { get; }

        public string GetProjectExternalRoot()
        {
            return Path.Combine(ExternalRoot, ProjectIdentity.StableProjectId + "-" + ProjectIdentity.CanonicalProjectRootHash.Substring(0, 12));
        }

        public string GetJournalRoot(string manifestId)
        {
            string safeManifest = string.IsNullOrWhiteSpace(manifestId)
                ? "unknown-manifest"
                : AuthoringIdentifier.Require(manifestId, nameof(manifestId));
            return Path.Combine(GetJournalStorageRoot(), safeManifest);
        }

        public string GetJournalStorageRoot() => Path.Combine(GetProjectExternalRoot(), "journals");

        public ExternalPathValidation ValidateExternalFilePath(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath) || !Path.IsPathRooted(absolutePath))
            {
                return Failure("ACA-PATH-NOT-ABSOLUTE", "Path must be absolute.", absolutePath);
            }

            string canonical = Path.GetFullPath(absolutePath);
            if (IsInside(canonical, ProjectIdentity.ProjectRoot))
                return Failure("ACA-PATH-PROJECT-ROOT", "External authoring path must not be inside the Unity project.", canonical);

            return new ExternalPathValidation { Success = true, CanonicalPath = canonical };
        }

        public ExternalPathValidation ValidateExternalDirectoryPath(string absolutePath)
        {
            return ValidateExternalFilePath(absolutePath);
        }

        static ExternalPathValidation Failure(string code, string message, string path)
        {
            return new ExternalPathValidation
            {
                Success = false,
                CanonicalPath = path ?? string.Empty,
                Diagnostic = AuthoringDiagnostic.Error(code, message, path)
            };
        }

        static bool IsInside(string path, string root)
        {
            string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(fullPath, fullRoot, StringComparison.Ordinal)
                || fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        }
    }
}
