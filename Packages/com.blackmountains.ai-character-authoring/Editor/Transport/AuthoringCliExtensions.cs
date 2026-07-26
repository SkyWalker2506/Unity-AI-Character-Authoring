using System;
using System.Collections.Generic;
using BlackMountains.AuthoringKernel;
using BlackMountains.AuthoringKernel.Editor;

namespace BlackMountains.AICharacterAuthoring.Editor.Transport
{
    /// <summary>
    /// Project packs register bounded CLI commands here. Extensions use the same result envelope
    /// and external result-path policy as the portable CLI; they do not replace the transport.
    /// </summary>
    public interface IAuthoringCliExtension
    {
        string Command { get; }
        CliResultEnvelope Execute(string requestPath, CliResultEnvelope envelope);
    }

    public static class AuthoringCliExtensionRegistry
    {
        static readonly Dictionary<string, IAuthoringCliExtension> Extensions =
            new Dictionary<string, IAuthoringCliExtension>(StringComparer.Ordinal);

        public static void Register(IAuthoringCliExtension extension)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));
            if (string.IsNullOrWhiteSpace(extension.Command))
                throw new ArgumentException("CLI extension command is required.", nameof(extension));
            Extensions[Normalize(extension.Command)] = extension;
        }

        public static bool TryExecute(
            string command,
            string requestPath,
            CliResultEnvelope envelope,
            out CliResultEnvelope result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(command) ||
                !Extensions.TryGetValue(Normalize(command), out var extension))
            {
                return false;
            }

            result = extension.Execute(requestPath, envelope);
            return true;
        }

        public static void Clear()
        {
            Extensions.Clear();
        }

        static string Normalize(string command)
        {
            return command.Trim()
                .Replace(" ", ".")
                .Replace("-", ".")
                .ToLowerInvariant();
        }
    }
}
