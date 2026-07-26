using System;

namespace BlackMountains.AuthoringKernel.Editor
{
    public interface IPlanApplicationService
    {
        PlanPreview Preview(GenerationPlan plan);
        ApplyResult Apply(GenerationPlan plan, ApprovalToken approval);
        RecoveryStatus InspectRecovery();
    }

    public sealed class PlanApplicationService : IPlanApplicationService
    {
        readonly AuthoringExternalPathPolicy _pathPolicy;
        readonly AuthoringJournalStore _journalStore;
        readonly RecoveryService _recoveryService;
        readonly OperationHandlerRegistry _handlers;

        public PlanApplicationService(AuthoringExternalPathPolicy pathPolicy, OperationHandlerRegistry handlers = null)
        {
            _pathPolicy = pathPolicy ?? throw new ArgumentNullException(nameof(pathPolicy));
            _journalStore = new AuthoringJournalStore(pathPolicy);
            _recoveryService = new RecoveryService(_journalStore);
            _handlers = handlers ?? new OperationHandlerRegistry();
        }

        public PlanPreview Preview(GenerationPlan plan)
        {
            var preview = new PlanPreview();
            if (plan == null)
            {
                preview.Diagnostics.Add(AuthoringDiagnostic.Error("ACA-PREVIEW-NO-PLAN", "GenerationPlan is required."));
                return preview;
            }

            plan.Operations.Sort(GenerationOperationComparer.Instance);
            preview.PlanHash = GenerationPlanHasher.Hash(plan);
            for (int i = 0; i < plan.Operations.Count; i++)
            {
                var op = plan.Operations[i];
                if (op.SecurityLevel >= OperationSecurityLevel.MutateFrameworkOwnedAsset)
                {
                    preview.Diagnostics.Add(AuthoringDiagnostic.Info("ACA-PREVIEW-MUTATION", "Operation requires managed-asset mutation through the executor.", op.OperationId));
                }
            }
            return preview;
        }

        public ApplyResult Apply(GenerationPlan plan, ApprovalToken approval)
        {
            var result = new ApplyResult { ManagedMutationsAttempted = 0 };
            if (plan == null)
            {
                result.Diagnostics.Add(AuthoringDiagnostic.Error("ACA-APPLY-NO-PLAN", "GenerationPlan is required."));
                return result;
            }

            plan.Operations.Sort(GenerationOperationComparer.Instance);
            string planHash = GenerationPlanHasher.Hash(plan);
            string projectId = _pathPolicy.ProjectIdentity.StableProjectId;

            for (int i = 0; i < plan.Operations.Count; i++)
            {
                var op = plan.Operations[i];
                if (approval == null || !approval.Allows(projectId, planHash, op.SecurityLevel, op.Reversibility))
                {
                    string requirement = op.Reversibility == OperationReversibility.NonReversible
                        ? " including its non-reversible classification"
                        : string.Empty;
                    result.Diagnostics.Add(AuthoringDiagnostic.Error("ACA-APPROVAL-DENIED", "Approval token does not permit operation " + op.OperationKind + requirement + ".", op.OperationId));
                    return result;
                }
                if (!_handlers.TryGet(op.OperationKind, out _))
                {
                    result.Diagnostics.Add(AuthoringDiagnostic.Error("ACA-OPERATION-NOT-IMPLEMENTED", "No operation handler is registered for " + op.OperationKind + ". Apply did not start.", op.OperationId));
                    return result;
                }
            }

            result.ExecutionId = "exec." + Guid.NewGuid().ToString("N");
            try
            {
                using (AuthoringApplyLock.Acquire(_pathPolicy, planHash))
                {
                    var recovery = InspectRecovery();
                    if (recovery.RecoveryRequired)
                    {
                        result.RecoveryRequired = true;
                        result.Diagnostics.Add(AuthoringDiagnostic.Error("ACA-RECOVERY-BLOCKED", recovery.Message));
                        return result;
                    }

                    for (int i = 0; i < plan.Operations.Count; i++)
                    {
                        var operation = plan.Operations[i];
                        var validation = GetHandler(operation.OperationKind).Validate(operation);
                        result.Diagnostics.AddRange(validation.Diagnostics.Items);
                        if (!validation.Success)
                            return result;
                    }

                    var journal = new ExecutionJournal
                    {
                        ExecutionId = result.ExecutionId,
                        ManifestId = plan.SubjectSpecId ?? "unknown-subject",
                        WriterId = Environment.MachineName + ":" + System.Diagnostics.Process.GetCurrentProcess().Id,
                        ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
                        ProcessStartTimeUtc = GetProcessStartTimeUtc(),
                        PlanHash = planHash
                    };
                    foreach (var op in plan.Operations)
                        journal.PendingOperations.Add(op.OperationId);
                    result.JournalPath = _journalStore.Begin(journal);

                    var context = new ApplyContext { ExecutionId = result.ExecutionId, ProjectId = projectId };
                    for (int i = 0; i < plan.Operations.Count; i++)
                    {
                        var op = plan.Operations[i];
                        var handler = GetHandler(op.OperationKind);
                        using (MutationScope.Enter())
                        {
                            result.ManagedMutationsAttempted++;
                            var apply = handler.Apply(op, context);
                            result.Diagnostics.AddRange(apply.Diagnostics.Items);
                            if (!apply.Success)
                                return MarkRecovery(result);

                            journal.CompletedOperations.Add(op.OperationId);
                            journal.PendingOperations.Remove(op.OperationId);
                            journal.LastHeartbeatUtc = DateTimeOffset.UtcNow;
                            journal.LeaseExpiresAtUtc = journal.LastHeartbeatUtc.AddMinutes(2);
                            journal.InversePayloads[op.OperationId] = apply.InversePayload ?? string.Empty;
                            _journalStore.Update(result.JournalPath, journal);
                        }
                    }

                    journal.Completed = true;
                    journal.LastHeartbeatUtc = DateTimeOffset.UtcNow;
                    journal.LeaseExpiresAtUtc = journal.LastHeartbeatUtc;
                    _journalStore.Update(result.JournalPath, journal);
                }
            }
            catch (Exception exception)
            {
                result.Diagnostics.Add(AuthoringDiagnostic.Error(
                    result.JournalPath == null ? "ACA-APPLY-START-FAILED" : "ACA-APPLY-EXCEPTION",
                    exception.GetType().Name + ": " + exception.Message));
                if (result.JournalPath != null)
                    return MarkRecovery(result);
            }

            return result;
        }

        public RecoveryStatus InspectRecovery() => _recoveryService.Inspect();

        IOperationHandler GetHandler(string operationKind)
        {
            _handlers.TryGet(operationKind, out var handler);
            return handler;
        }

        static ApplyResult MarkRecovery(ApplyResult result)
        {
            result.RecoveryRequired = true;
            result.Diagnostics.Add(AuthoringDiagnostic.Error("ACA-APPLY-PARTIAL", "Apply failed after journal creation. Recovery is required before new mutating plans."));
            return result;
        }

        static DateTimeOffset GetProcessStartTimeUtc()
        {
            try
            {
                return new DateTimeOffset(System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime());
            }
            catch
            {
                return DateTimeOffset.UtcNow;
            }
        }
    }
}
