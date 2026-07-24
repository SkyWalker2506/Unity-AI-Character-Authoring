using System;
using System.Collections.Generic;

namespace BlackMountains.AICharacterAuthoring.Editor
{
    public enum MergeAction
    {
        NoOp,
        ApplyDesired,
        PreserveCurrent,
        Conflict,
        RequiresReplacement,
        Recompute
    }

    public enum DriftClassification
    {
        NoDrift,
        EnforcedDrift,
        PreservedDrift,
        ConvergedExternalChange,
        Conflict,
        ReplacementRequired,
        Computed
    }

    public sealed class MergeDecision
    {
        public FieldIdentity Field { get; set; }
        public OwnershipPolicy Policy { get; set; }
        public AuthoringFieldValue BaseValue { get; set; }
        public AuthoringFieldValue CurrentValue { get; set; }
        public AuthoringFieldValue DesiredValue { get; set; }
        public MergeAction Action { get; set; }
        public DriftClassification Drift { get; set; }
        public bool DriftMustBeVisibleInPreview { get; set; }
        public string Reason { get; set; }
    }

    public sealed class MergeResult
    {
        readonly List<MergeDecision> _decisions = new List<MergeDecision>();

        public IReadOnlyList<MergeDecision> Decisions => _decisions;
        public bool HasConflicts
        {
            get
            {
                for (int i = 0; i < _decisions.Count; i++)
                    if (_decisions[i].Action == MergeAction.Conflict) return true;
                return false;
            }
        }

        public bool HasVisibleDrift
        {
            get
            {
                for (int i = 0; i < _decisions.Count; i++)
                    if (_decisions[i].DriftMustBeVisibleInPreview) return true;
                return false;
            }
        }

        public void Add(MergeDecision decision)
        {
            if (decision != null) _decisions.Add(decision);
        }
    }

    public interface IThreeWayMergeEngine
    {
        MergeDecision MergeField(FieldIdentity field, OwnershipPolicy policy, AuthoringFieldValue @base, AuthoringFieldValue current, AuthoringFieldValue desired);
        MergeResult Merge(NormalizedSnapshot @base, NormalizedSnapshot current, NormalizedSnapshot desired, IReadOnlyList<FieldSchema> schemas);
    }

    public sealed class ThreeWayMergeEngine : IThreeWayMergeEngine
    {
        public MergeDecision MergeField(FieldIdentity field, OwnershipPolicy policy, AuthoringFieldValue @base, AuthoringFieldValue current, AuthoringFieldValue desired)
        {
            var decision = new MergeDecision
            {
                Field = field,
                Policy = policy,
                BaseValue = @base,
                CurrentValue = current,
                DesiredValue = desired
            };

            if (desired.State == AuthoringValueState.Computed || policy == OwnershipPolicy.Computed)
                return Complete(decision, MergeAction.Recompute, DriftClassification.Computed, false, "Computed fields are resolved by the provider/executor.");

            if (desired.State == AuthoringValueState.Unspecified)
                return Complete(decision, MergeAction.PreserveCurrent, DriftClassification.NoDrift, false, "Desired state has no opinion about this field.");

            if (@base.State == AuthoringValueState.Unknown
                || current.State == AuthoringValueState.Unknown
                || desired.State == AuthoringValueState.Unknown
                || current.State == AuthoringValueState.Unspecified)
            {
                return Complete(decision, MergeAction.Conflict, DriftClassification.Conflict, true, "The field cannot be merged safely because one of the required observed values is indeterminate.");
            }

            if (current == desired)
            {
                var drift = current == @base ? DriftClassification.NoDrift : DriftClassification.ConvergedExternalChange;
                return Complete(decision, MergeAction.NoOp, drift, false, current == @base ? "Already converged." : "Current already equals desired.");
            }

            bool currentEqualsBase = current == @base;
            bool desiredEqualsBase = desired == @base;

            if (currentEqualsBase && !desiredEqualsBase)
            {
                if (policy == OwnershipPolicy.Immutable)
                    return Complete(decision, MergeAction.RequiresReplacement, DriftClassification.ReplacementRequired, true, "Immutable field desired value changed.");
                return Complete(decision, MergeAction.ApplyDesired, DriftClassification.NoDrift, false, "Desired changed while current still matches base.");
            }

            if (!currentEqualsBase && desiredEqualsBase)
            {
                switch (policy)
                {
                    case OwnershipPolicy.Enforced:
                        return Complete(decision, MergeAction.ApplyDesired, DriftClassification.EnforcedDrift, true, "Enforced drift will be restored to desired/base.");
                    case OwnershipPolicy.PreserveOnDrift:
                    case OwnershipPolicy.UserOwned:
                        return Complete(decision, MergeAction.PreserveCurrent, DriftClassification.PreservedDrift, true, "Current drift is preserved by ownership policy.");
                    case OwnershipPolicy.Immutable:
                        return Complete(decision, MergeAction.Conflict, DriftClassification.Conflict, true, "Immutable field drift requires explicit recovery or replacement.");
                }
            }

            if (!currentEqualsBase && !desiredEqualsBase)
            {
                switch (policy)
                {
                    case OwnershipPolicy.Enforced:
                        return Complete(decision, MergeAction.ApplyDesired, DriftClassification.EnforcedDrift, true, "Enforced desired value wins after visible drift preview.");
                    case OwnershipPolicy.PreserveOnDrift:
                        return Complete(decision, MergeAction.Conflict, DriftClassification.Conflict, true, "PreserveOnDrift cannot choose between current drift and new desired.");
                    case OwnershipPolicy.UserOwned:
                        return Complete(decision, MergeAction.PreserveCurrent, DriftClassification.PreservedDrift, true, "UserOwned field keeps current value.");
                    case OwnershipPolicy.Immutable:
                        return Complete(decision, MergeAction.RequiresReplacement, DriftClassification.ReplacementRequired, true, "Immutable field changed and requires replacement.");
                }
            }

            return Complete(decision, MergeAction.Conflict, DriftClassification.Conflict, true, "Unhandled merge state.");
        }

        public MergeResult Merge(NormalizedSnapshot @base, NormalizedSnapshot current, NormalizedSnapshot desired, IReadOnlyList<FieldSchema> schemas)
        {
            if (@base == null) throw new ArgumentNullException(nameof(@base));
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (desired == null) throw new ArgumentNullException(nameof(desired));
            if (schemas == null) throw new ArgumentNullException(nameof(schemas));

            var result = new MergeResult();
            var ordered = new List<FieldSchema>(schemas);
            ordered.Sort((a, b) => a.Identity.CompareTo(b.Identity));

            for (int i = 0; i < ordered.Count; i++)
            {
                var schema = ordered[i];
                @base.TryGetValue(schema.Identity, out var baseValue);
                current.TryGetValue(schema.Identity, out var currentValue);
                desired.TryGetValue(schema.Identity, out var desiredValue);
                result.Add(MergeField(schema.Identity, schema.Policy, baseValue, currentValue, desiredValue));
            }

            return result;
        }

        static MergeDecision Complete(MergeDecision decision, MergeAction action, DriftClassification drift, bool visible, string reason)
        {
            decision.Action = action;
            decision.Drift = drift;
            decision.DriftMustBeVisibleInPreview = visible;
            decision.Reason = reason;
            return decision;
        }
    }
}
