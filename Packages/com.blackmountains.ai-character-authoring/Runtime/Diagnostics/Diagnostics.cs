using System;
using System.Collections.Generic;

namespace BlackMountains.AICharacterAuthoring
{
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class AuthoringDiagnostic
    {
        public AuthoringDiagnostic(string code, DiagnosticSeverity severity, string message, string target = null)
        {
            Code = string.IsNullOrWhiteSpace(code) ? "ACA-UNKNOWN" : code;
            Severity = severity;
            Message = message ?? string.Empty;
            Target = target ?? string.Empty;
        }

        public string Code { get; }
        public DiagnosticSeverity Severity { get; }
        public string Message { get; }
        public string Target { get; }
        public Dictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public static AuthoringDiagnostic Info(string code, string message, string target = null) => new AuthoringDiagnostic(code, DiagnosticSeverity.Info, message, target);
        public static AuthoringDiagnostic Warning(string code, string message, string target = null) => new AuthoringDiagnostic(code, DiagnosticSeverity.Warning, message, target);
        public static AuthoringDiagnostic Error(string code, string message, string target = null) => new AuthoringDiagnostic(code, DiagnosticSeverity.Error, message, target);
    }

    public sealed class DiagnosticList
    {
        readonly List<AuthoringDiagnostic> _items = new List<AuthoringDiagnostic>();

        public IReadOnlyList<AuthoringDiagnostic> Items => _items;
        public bool HasErrors
        {
            get
            {
                for (int i = 0; i < _items.Count; i++)
                    if (_items[i].Severity == DiagnosticSeverity.Error) return true;
                return false;
            }
        }

        public void Add(AuthoringDiagnostic diagnostic)
        {
            if (diagnostic != null) _items.Add(diagnostic);
        }

        public void AddRange(IEnumerable<AuthoringDiagnostic> diagnostics)
        {
            if (diagnostics == null) return;
            foreach (var diagnostic in diagnostics) Add(diagnostic);
        }
    }
}
