using System.Collections.Generic;
using BlackMountains.AuthoringKernel;

namespace BlackMountains.AICharacterAuthoring
{
    public sealed class ProviderDescriptor
    {
        public string ProviderId { get; set; }
        public string SchemaVersion { get; set; } = "1";
        public string DisplayName { get; set; }
        public string PackageOrAssembly { get; set; }
        public List<string> Capabilities { get; set; } = new List<string>();
    }

    public sealed class CapabilityDescriptor
    {
        public string CapabilityId { get; set; }
        public string ProviderId { get; set; }
        public string ProviderSchemaVersion { get; set; } = "1";
        public List<string> Provides { get; set; } = new List<string>();
        public List<string> Requires { get; set; } = new List<string>();
        public List<string> ConflictsWith { get; set; } = new List<string>();
        public ExecutionClassification Execution { get; set; } = new ExecutionClassification();
        public AuthoringQualityPolicy QualityPolicy { get; set; } = AuthoringQualityPolicy.DeterministicOnly;
    }

    public sealed class ExecutionClassification
    {
        public string TaskModel { get; set; } = "ManagedGameObject";
        public bool MainThreadOnly { get; set; } = true;
        public bool PhysicsDependent { get; set; }
        public bool NavigationDependent { get; set; }
        public bool AnimatorDependent { get; set; }
        public bool BurstCompatible { get; set; }
        public string MeasuredCostClass { get; set; } = "Unknown";
    }

    public interface ICharacterAuthoringProvider
    {
        ProviderDescriptor Descriptor { get; }
        void Contribute(ProviderPlanningContext context);
    }

    public interface ICapabilityProvider
    {
        CapabilityDescriptor Descriptor { get; }
        void Expand(CapabilityPlanningContext context);
    }

    public sealed class ProviderPlanningContext
    {
        readonly List<CapabilityDescriptor> _capabilities = new List<CapabilityDescriptor>();

        public IReadOnlyList<CapabilityDescriptor> Capabilities => _capabilities;
        public void AddCapability(CapabilityDescriptor descriptor)
        {
            if (descriptor != null) _capabilities.Add(descriptor);
        }
    }

    public sealed class CapabilityPlanningContext
    {
        readonly List<GenerationOperation> _operations = new List<GenerationOperation>();

        public CharacterSpec Spec { get; }
        public CapabilityDescriptor Capability { get; }
        public IReadOnlyList<GenerationOperation> Operations => _operations;

        public CapabilityPlanningContext(CharacterSpec spec, CapabilityDescriptor capability)
        {
            Spec = spec;
            Capability = capability;
        }

        public void AddOperation(GenerationOperation operation)
        {
            if (operation != null) _operations.Add(operation);
        }
    }
}
