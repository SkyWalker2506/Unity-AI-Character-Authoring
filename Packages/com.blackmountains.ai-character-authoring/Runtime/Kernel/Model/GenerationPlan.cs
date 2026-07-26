using System.Collections.Generic;

namespace BlackMountains.AuthoringKernel
{
    public enum OperationSecurityLevel
    {
        ReadOnly = 0,
        CreateFrameworkOwnedAsset = 3,
        MutateFrameworkOwnedAsset = 4,
        MutateUserOwnedAsset = 5,
        DisposableValidationScene = 6,
        PackageInstallOrUpdate = 7,
        PlayBuildOrTest = 8,
        ArbitraryEditorEval = 9
    }

    public enum OperationReversibility
    {
        Reversible,
        ConditionallyReversible,
        NonReversible
    }

    public enum OperationLifecyclePhase
    {
        SourceResolve = 0,
        AssetCreate = 10,
        RequiredComponents = 20,
        SharedResources = 30,
        ProviderSetup = 40,
        FieldAssignment = 50,
        References = 60,
        Behavior = 70,
        Animator = 80,
        Physics = 90,
        ValidationPrep = 100
    }

    public sealed partial class GenerationPlan
    {
        public const string CurrentSchemaVersion = "1";

        public string SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string PlanId { get; set; }
        public string SubjectSpecId { get; set; }
        public PlanSource Source { get; set; } = new PlanSource();
        public List<GenerationOperation> Operations { get; set; } = new List<GenerationOperation>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(System.StringComparer.Ordinal);
    }

    public sealed class PlanSource
    {
        public string PrefabGuid { get; set; }
        public string SourceVersion { get; set; }
        public string SourceNormalizedHash { get; set; }
    }

    public sealed class GenerationOperation
    {
        public string OperationId { get; set; }
        public string OperationKind { get; set; }
        public OperationLifecyclePhase Phase { get; set; }
        public string LogicalTarget { get; set; }
        public string TargetAssetGuid { get; set; }
        public string TargetObjectId { get; set; }
        public string ProviderId { get; set; }
        public string StableOperationKey { get; set; }
        public Dictionary<string, AuthoringFieldValue> Inputs { get; set; } = new Dictionary<string, AuthoringFieldValue>(System.StringComparer.Ordinal);
        public List<string> Preconditions { get; set; } = new List<string>();
        public List<string> Postconditions { get; set; } = new List<string>();
        public OperationSecurityLevel SecurityLevel { get; set; } = OperationSecurityLevel.MutateFrameworkOwnedAsset;
        public OperationReversibility Reversibility { get; set; } = OperationReversibility.ConditionallyReversible;
    }
}
