using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using BlackMountains.AuthoringKernel;
using BlackMountains.AuthoringKernel.Editor;

namespace BlackMountains.AICharacterAuthoring.Editor.Transport
{
    /// <summary>
    /// File-backed facade intended for Synaptic run_csharp callers. It delegates to the same CLI command handler.
    /// </summary>
    public static class AuthoringTransportFacade
    {
        static readonly Dictionary<string, string> ResultPaths = new Dictionary<string, string>(StringComparer.Ordinal);

        public static string SubmitRequest(string absoluteRequestPath)
        {
            if (string.IsNullOrWhiteSpace(absoluteRequestPath) || !Path.IsPathRooted(absoluteRequestPath))
                throw new ArgumentException("Request path must be absolute.", nameof(absoluteRequestPath));

            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName
                ?? Directory.GetCurrentDirectory();
            var pathPolicy = new AuthoringExternalPathPolicy(AuthoringProjectIdentity.Create(projectRoot));
            var validation = pathPolicy.ValidateExternalFilePath(absoluteRequestPath);
            if (!validation.Success)
                throw new ArgumentException(validation.Diagnostic.Message, nameof(absoluteRequestPath));

            string json = File.ReadAllText(validation.CanonicalPath);
            string command = "plan";
            var token = JToken.Parse(json);
            if (token.Type == JTokenType.Object && token["command"] != null)
                command = token["command"].Value<string>();

            string requestId = "request." + Guid.NewGuid().ToString("N");
            string resultPath = Path.Combine(Path.GetDirectoryName(validation.CanonicalPath), requestId + ".result.json");
            AuthoringCli.ExecuteCommand(command, validation.CanonicalPath, resultPath);
            ResultPaths[requestId] = resultPath;
            return requestId;
        }

        public static string GetStatus(string requestId)
        {
            if (requestId == null || !ResultPaths.TryGetValue(requestId, out var resultPath))
                return "Unknown";
            return File.Exists(resultPath) ? "Completed" : "Pending";
        }

        public static string GetResult(string requestId)
        {
            if (requestId == null || !ResultPaths.TryGetValue(requestId, out var resultPath))
                return string.Empty;
            return File.Exists(resultPath) ? File.ReadAllText(resultPath) : string.Empty;
        }

        public static bool Cancel(string requestId)
        {
            return requestId != null && ResultPaths.Remove(requestId);
        }
    }
}
