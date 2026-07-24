using System;
using System.Collections.Generic;

namespace BlackMountains.AICharacterAuthoring.Editor
{
    public sealed class ApprovalToken
    {
        readonly HashSet<OperationSecurityLevel> _allowedSecurityLevels = new HashSet<OperationSecurityLevel>();

        public string TokenId { get; set; }
        public string ProjectId { get; set; }
        public string PlanHash { get; set; }
        public DateTimeOffset ExpiresAtUtc { get; set; } = DateTimeOffset.UtcNow.AddMinutes(10);
        public bool AllowsNonReversibleOperations { get; private set; }

        public void Allow(OperationSecurityLevel level) => _allowedSecurityLevels.Add(level);
        public void AllowNonReversible() => AllowsNonReversibleOperations = true;

        public bool Allows(string projectId, string planHash, OperationSecurityLevel level, OperationReversibility reversibility)
        {
            return DateTimeOffset.UtcNow <= ExpiresAtUtc
                && string.Equals(ProjectId, projectId, StringComparison.Ordinal)
                && string.Equals(PlanHash, planHash, StringComparison.Ordinal)
                && _allowedSecurityLevels.Contains(level)
                && (reversibility != OperationReversibility.NonReversible || AllowsNonReversibleOperations);
        }
    }

    public sealed class PlanPreview
    {
        public bool Success => !Diagnostics.HasErrors;
        public DiagnosticList Diagnostics { get; } = new DiagnosticList();
        public string PlanHash { get; set; }
        public List<MergeDecision> MergeDecisions { get; } = new List<MergeDecision>();
    }

    public sealed class ApplyResult
    {
        public bool Success => !Diagnostics.HasErrors;
        public DiagnosticList Diagnostics { get; } = new DiagnosticList();
        public string ExecutionId { get; set; }
        public int ManagedMutationsAttempted { get; set; }
        public bool RecoveryRequired { get; set; }
        public string JournalPath { get; set; }
    }

    public sealed class OperationValidation
    {
        public bool Success => !Diagnostics.HasErrors;
        public DiagnosticList Diagnostics { get; } = new DiagnosticList();
    }

    public sealed class OperationApplyResult
    {
        public bool Success => !Diagnostics.HasErrors;
        public DiagnosticList Diagnostics { get; } = new DiagnosticList();
        public string InversePayload { get; set; }
    }

    public sealed class AppliedOperation
    {
        public GenerationOperation Operation { get; set; }
        public string InversePayload { get; set; }
    }

    public sealed class OperationRollbackResult
    {
        public bool Success => !Diagnostics.HasErrors;
        public DiagnosticList Diagnostics { get; } = new DiagnosticList();
    }

    public sealed class ApplyContext
    {
        public string ExecutionId { get; set; }
        public string ProjectId { get; set; }
    }

    public sealed class RollbackContext
    {
        public string ExecutionId { get; set; }
        public string ProjectId { get; set; }
    }

    public interface IOperationHandler
    {
        string OperationKind { get; }
        OperationValidation Validate(GenerationOperation operation);
        OperationApplyResult Apply(GenerationOperation operation, ApplyContext context);
        OperationRollbackResult Rollback(AppliedOperation operation, RollbackContext context);
    }

    public sealed class OperationHandlerRegistry
    {
        readonly Dictionary<string, IOperationHandler> _handlers = new Dictionary<string, IOperationHandler>(StringComparer.Ordinal);

        public void Register(IOperationHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (string.IsNullOrWhiteSpace(handler.OperationKind)) throw new ArgumentException("OperationKind is required.", nameof(handler));
            _handlers[handler.OperationKind] = handler;
        }

        public bool TryGet(string operationKind, out IOperationHandler handler)
        {
            if (operationKind == null)
            {
                handler = null;
                return false;
            }
            return _handlers.TryGetValue(operationKind, out handler);
        }
    }

    public sealed class MutationScope : IDisposable
    {
        [ThreadStatic] static int _depth;
        bool _disposed;

        MutationScope() => _depth++;

        public static bool IsActive => _depth > 0;
        public static MutationScope Enter() => new MutationScope();

        public static void RequireActive()
        {
            if (!IsActive)
                throw new InvalidOperationException("Managed asset mutation is only allowed inside the deterministic authoring executor scope.");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _depth = Math.Max(0, _depth - 1);
        }
    }
}
