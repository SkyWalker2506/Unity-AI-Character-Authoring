using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace BlackMountains.AICharacterAuthoring.Editor.Transport
{
    public sealed class CliResultEnvelope
    {
        public string SchemaVersion { get; set; } = "1";
        public string Command { get; set; }
        public bool Success { get; set; }
        public string ExitCategory { get; set; }
        public List<AuthoringDiagnostic> Diagnostics { get; set; } = new List<AuthoringDiagnostic>();
        public string PlanId { get; set; }
        public string PlanHash { get; set; }
        public RecoveryStatus RecoveryStatus { get; set; }
        public Dictionary<string, object> Payload { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
    }

    public static class AuthoringCli
    {
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            string command = ReadArg(args, "--aca-command");
            string request = ReadArg(args, "--aca-request");
            string result = ReadArg(args, "--aca-result");
            var envelope = ExecuteCommand(command, request, result);
            if (ApplicationIsBatchMode())
                EditorApplication.Exit(envelope.Success ? 0 : 1);
        }

        public static CliResultEnvelope ExecuteCommand(string command, string requestPath, string resultPath)
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName
                ?? Directory.GetCurrentDirectory();
            var identity = AuthoringProjectIdentity.Create(projectRoot);
            var pathPolicy = new AuthoringExternalPathPolicy(identity);
            CliResultEnvelope envelope;
            try
            {
                envelope = ExecuteCommandInternal(command, requestPath, resultPath, pathPolicy);
            }
            catch (Exception exception)
            {
                envelope = new CliResultEnvelope
                {
                    Command = command ?? string.Empty,
                    Success = false,
                    ExitCategory = "UnhandledError"
                };
                envelope.Diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-CLI-EXCEPTION",
                    exception.GetType().Name + ": " + exception.Message));
            }
            if (!string.IsNullOrWhiteSpace(resultPath))
                WriteResult(pathPolicy, resultPath, envelope);
            return envelope;
        }

        static CliResultEnvelope ExecuteCommandInternal(string command, string requestPath, string resultPath, AuthoringExternalPathPolicy pathPolicy)
        {
            var envelope = new CliResultEnvelope
            {
                Command = command ?? string.Empty,
                ExitCategory = "Unknown"
            };

            if (string.IsNullOrWhiteSpace(command))
                return Fail(envelope, "ACA-CLI-NO-COMMAND", "Missing --aca-command.");

            if (!string.IsNullOrWhiteSpace(requestPath))
            {
                var requestValidation = pathPolicy.ValidateExternalFilePath(requestPath);
                if (!requestValidation.Success)
                    return Fail(envelope, requestValidation.Diagnostic);
            }

            if (!string.IsNullOrWhiteSpace(resultPath))
            {
                var resultValidation = pathPolicy.ValidateExternalFilePath(resultPath);
                if (!resultValidation.Success)
                    return Fail(envelope, resultValidation.Diagnostic);
            }

            if (AuthoringCliExtensionRegistry.TryExecute(command, requestPath, envelope, out var extensionResult))
                return extensionResult;

            switch (command)
            {
                case "doctor":
                    envelope.Success = true;
                    envelope.ExitCategory = "Success";
                    envelope.Diagnostics.Add(AuthoringDiagnostic.Info("ACA-DOCTOR-OK", "AI Character Authoring package loaded."));
                    envelope.Payload["projectId"] = pathPolicy.ProjectIdentity.StableProjectId;
                    envelope.Payload["externalRoot"] = pathPolicy.GetProjectExternalRoot();
                    return envelope;
                case "plan":
                    return RunPlan(envelope, requestPath);
                case "preview":
                    return RunPreview(envelope, requestPath, pathPolicy);
                case "recover-status":
                    envelope.Success = true;
                    envelope.ExitCategory = "Success";
                    envelope.RecoveryStatus = new RecoveryService(new AuthoringJournalStore(pathPolicy)).Inspect();
                    return envelope;
                case "export-state":
                    envelope.Success = true;
                    envelope.ExitCategory = "Success";
                    envelope.Payload["providers"] = new List<string>(GlobalAuthoringRegistry.Registry.Providers.Keys);
                    envelope.Payload["capabilities"] = new List<string>(GlobalAuthoringRegistry.Registry.Capabilities.Keys);
                    return envelope;
                case "apply":
                case "index":
                case "validate":
                case "recover-resume":
                case "recover-rollback":
                    return Fail(envelope, "ACA-CLI-NOT-IMPLEMENTED", command + " is intentionally gated in this foundation slice and did not run.");
                default:
                    return Fail(envelope, "ACA-CLI-UNKNOWN", "Unknown authoring command: " + command);
            }
        }

        static CliResultEnvelope RunPlan(CliResultEnvelope envelope, string requestPath)
        {
            var spec = ReadSpec(requestPath);
            var compiler = new GenerationPlanCompiler(GlobalAuthoringRegistry.Registry);
            var result = compiler.Compile(spec);
            envelope.Diagnostics.AddRange(result.Diagnostics.Items);
            envelope.Success = result.Success;
            envelope.ExitCategory = result.Success ? "Success" : "CompileFailed";
            if (result.Success)
            {
                envelope.PlanId = result.Plan.PlanId;
                envelope.PlanHash = result.PlanHash;
                envelope.Payload["plan"] = result.Plan;
            }
            return envelope;
        }

        static CliResultEnvelope RunPreview(CliResultEnvelope envelope, string requestPath, AuthoringExternalPathPolicy pathPolicy)
        {
            var spec = ReadSpec(requestPath);
            var compiler = new GenerationPlanCompiler(GlobalAuthoringRegistry.Registry);
            var compile = compiler.Compile(spec);
            envelope.Diagnostics.AddRange(compile.Diagnostics.Items);
            if (!compile.Success)
            {
                envelope.Success = false;
                envelope.ExitCategory = "CompileFailed";
                return envelope;
            }

            var preview = new PlanApplicationService(pathPolicy).Preview(compile.Plan);
            envelope.Diagnostics.AddRange(preview.Diagnostics.Items);
            envelope.Success = preview.Success;
            envelope.ExitCategory = preview.Success ? "Success" : "PreviewFailed";
            envelope.PlanId = compile.Plan.PlanId;
            envelope.PlanHash = preview.PlanHash;
            envelope.Payload["preview"] = preview;
            return envelope;
        }

        static CharacterSpec ReadSpec(string requestPath)
        {
            if (string.IsNullOrWhiteSpace(requestPath)) throw new ArgumentException("Request path is required for this command.", nameof(requestPath));
            var json = File.ReadAllText(requestPath);
            var token = JToken.Parse(json);
            if (token.Type == JTokenType.Object && token["characterSpec"] != null)
                return token["characterSpec"].ToObject<CharacterSpec>();
            return token.ToObject<CharacterSpec>();
        }

        static CliResultEnvelope Fail(CliResultEnvelope envelope, string code, string message)
        {
            return Fail(envelope, AuthoringDiagnostic.Error(code, message));
        }

        static CliResultEnvelope Fail(CliResultEnvelope envelope, AuthoringDiagnostic diagnostic)
        {
            envelope.Success = false;
            envelope.ExitCategory = "Failed";
            envelope.Diagnostics.Add(diagnostic);
            return envelope;
        }

        static void WriteResult(AuthoringExternalPathPolicy pathPolicy, string resultPath, CliResultEnvelope envelope)
        {
            var validation = pathPolicy.ValidateExternalFilePath(resultPath);
            if (!validation.Success) return;
            Directory.CreateDirectory(Path.GetDirectoryName(validation.CanonicalPath));
            string temporaryPath = validation.CanonicalPath + ".tmp." + Guid.NewGuid().ToString("N");
            try
            {
                string json = JsonConvert.SerializeObject(envelope, Formatting.Indented);
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(validation.CanonicalPath))
                    File.Replace(temporaryPath, validation.CanonicalPath, null);
                else
                    File.Move(temporaryPath, validation.CanonicalPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        static string ReadArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                    return args[i + 1];
            return null;
        }

        static bool ApplicationIsBatchMode()
        {
            return UnityEngine.Application.isBatchMode;
        }
    }
}
