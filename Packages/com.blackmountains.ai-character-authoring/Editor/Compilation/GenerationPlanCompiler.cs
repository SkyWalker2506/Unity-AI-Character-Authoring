using System;
using System.Collections.Generic;
using BlackMountains.AuthoringKernel;

namespace BlackMountains.AICharacterAuthoring.Editor
{
    public sealed class PlanCompilationResult
    {
        public bool Success => !Diagnostics.HasErrors;
        public DiagnosticList Diagnostics { get; } = new DiagnosticList();
        public GenerationPlan Plan { get; set; }
        public string PlanHash { get; set; }
        public int ManagedMutationsAttempted { get; set; }
    }

    public sealed class GenerationPlanCompiler
    {
        readonly AuthoringProviderRegistry _registry;

        public GenerationPlanCompiler(AuthoringProviderRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public PlanCompilationResult Compile(CharacterSpec spec, bool aiProviderAvailable = false)
        {
            var result = new PlanCompilationResult();
            result.ManagedMutationsAttempted = 0;

            ValidateSpec(spec, result.Diagnostics);
            if (result.Diagnostics.HasErrors) return result;

            var resolvedCapabilities = ResolveCapabilities(spec, result.Diagnostics);
            if (result.Diagnostics.HasErrors) return result;

            var effectiveQualityPolicy = ResolveEffectiveQualityPolicy(spec.QualityPolicy, resolvedCapabilities);
            var aiPolicy = IntelligencePolicyResolver.Resolve(effectiveQualityPolicy, aiProviderAvailable, deterministicFallbackAvailable: true);
            if (!aiPolicy.Success)
            {
                result.Diagnostics.Add(aiPolicy.Diagnostic);
                return result;
            }

            var plan = new GenerationPlan
            {
                SchemaVersion = GenerationPlan.CurrentSchemaVersion,
                SubjectSpecId = spec.SpecId,
                PlanId = BuildPlanId(spec, resolvedCapabilities)
            };
            plan.Source.PrefabGuid = spec.Source?.SourcePrefabGuid;
            plan.Source.SourceVersion = spec.Source?.SourceVersion;

            foreach (var capability in resolvedCapabilities)
                plan.Operations.Add(BuildCapabilityOperation(spec, capability));

            plan.Operations.Sort(GenerationOperationComparer.Instance);
            plan.Metadata["requestedAiPolicy"] = spec.QualityPolicy.ToString();
            plan.Metadata["effectiveAiPolicy"] = effectiveQualityPolicy.ToString();
            plan.Metadata["aiUsed"] = aiPolicy.UsedAI ? "true" : "false";
            plan.Metadata["deterministicFallbackUsed"] = aiPolicy.UsedDeterministicFallback ? "true" : "false";

            result.Plan = plan;
            result.PlanHash = GenerationPlanHasher.Hash(plan);
            return result;
        }

        static void ValidateSpec(CharacterSpec spec, DiagnosticList diagnostics)
        {
            if (spec == null)
            {
                diagnostics.Add(AuthoringDiagnostic.Error("ACA-SPEC-NULL", "CharacterSpec is required."));
                return;
            }

            if (spec.SchemaVersion != CharacterSpec.CurrentSchemaVersion)
                diagnostics.Add(AuthoringDiagnostic.Error("ACA-SPEC-SCHEMA", "Unsupported CharacterSpec schema version.", "schemaVersion"));

            if (!AuthoringIdentifier.IsValid(spec.SpecId))
                diagnostics.Add(AuthoringDiagnostic.Error("ACA-SPEC-ID", "CharacterSpec.SpecId must be a stable authoring identifier.", "specId"));

            if (spec.Capabilities == null || spec.Capabilities.Count == 0)
                diagnostics.Add(AuthoringDiagnostic.Error("ACA-SPEC-CAPABILITIES", "At least one capability is required.", "capabilities"));
        }

        List<CapabilityDescriptor> ResolveCapabilities(CharacterSpec spec, DiagnosticList diagnostics)
        {
            var requested = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var capability in spec.Capabilities)
            {
                if (!AuthoringIdentifier.IsValid(capability))
                    diagnostics.Add(AuthoringDiagnostic.Error("ACA-CAPABILITY-ID", "Invalid capability id.", capability));
                else
                    requested.Add(capability);
            }

            var resolved = new SortedDictionary<string, CapabilityDescriptor>(StringComparer.Ordinal);
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            foreach (var capability in requested)
                ResolveCapabilityRecursive(capability, resolved, visiting, diagnostics);

            foreach (var capability in resolved.Values)
            {
                foreach (var conflict in capability.ConflictsWith)
                {
                    if (resolved.ContainsKey(conflict))
                        diagnostics.Add(AuthoringDiagnostic.Error("ACA-CAPABILITY-CONFLICT", capability.CapabilityId + " conflicts with " + conflict, capability.CapabilityId));
                }
            }

            return new List<CapabilityDescriptor>(resolved.Values);
        }

        static AuthoringQualityPolicy ResolveEffectiveQualityPolicy(
            AuthoringQualityPolicy requestedPolicy,
            IReadOnlyList<CapabilityDescriptor> capabilities)
        {
            var effective = requestedPolicy;
            for (int i = 0; i < capabilities.Count; i++)
            {
                if (capabilities[i].QualityPolicy > effective)
                    effective = capabilities[i].QualityPolicy;
            }
            return effective;
        }

        void ResolveCapabilityRecursive(string capabilityId, SortedDictionary<string, CapabilityDescriptor> resolved, HashSet<string> visiting, DiagnosticList diagnostics)
        {
            if (resolved.ContainsKey(capabilityId)) return;
            if (!visiting.Add(capabilityId))
            {
                diagnostics.Add(AuthoringDiagnostic.Error("ACA-CAPABILITY-CYCLE", "Capability dependency cycle detected.", capabilityId));
                return;
            }

            if (!_registry.TryGetCapability(capabilityId, out var descriptor))
            {
                diagnostics.Add(AuthoringDiagnostic.Error("ACA-CAPABILITY-MISSING", "Capability is not registered.", capabilityId));
                visiting.Remove(capabilityId);
                return;
            }

            var requires = new List<string>(descriptor.Requires ?? new List<string>());
            requires.Sort(StringComparer.Ordinal);
            foreach (var required in requires)
                ResolveCapabilityRecursive(required, resolved, visiting, diagnostics);

            resolved[capabilityId] = descriptor;
            visiting.Remove(capabilityId);
        }

        static GenerationOperation BuildCapabilityOperation(CharacterSpec spec, CapabilityDescriptor capability)
        {
            string key = capability.CapabilityId;
            var operation = new GenerationOperation
            {
                OperationKind = "ensureCapability",
                Phase = capability.Requires == null || capability.Requires.Count == 0
                    ? OperationLifecyclePhase.RequiredComponents
                    : OperationLifecyclePhase.ProviderSetup,
                LogicalTarget = key,
                ProviderId = capability.ProviderId,
                StableOperationKey = key,
                SecurityLevel = OperationSecurityLevel.MutateFrameworkOwnedAsset,
                Reversibility = OperationReversibility.ConditionallyReversible
            };
            operation.OperationId = BuildOperationId(spec.SpecId, operation);
            operation.Inputs["capability.id"] = AuthoringFieldValue.Known(CanonicalValue.String(capability.CapabilityId));
            operation.Inputs["provider.id"] = AuthoringFieldValue.Known(CanonicalValue.String(capability.ProviderId));
            operation.Inputs["provider.schema"] = AuthoringFieldValue.Known(CanonicalValue.String(capability.ProviderSchemaVersion));
            return operation;
        }

        static string BuildPlanId(CharacterSpec spec, IReadOnlyList<CapabilityDescriptor> capabilities)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["schema"] = GenerationPlan.CurrentSchemaVersion,
                ["spec"] = spec.SpecId
            };
            for (int i = 0; i < capabilities.Count; i++)
                map["capability." + i.ToString(System.Globalization.CultureInfo.InvariantCulture)] = capabilities[i].CapabilityId;
            return "plan." + CanonicalSerialization.Sha256(CanonicalSerialization.SerializeMap(map)).Substring(0, 24);
        }

        static string BuildOperationId(string specId, GenerationOperation operation)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["spec"] = specId,
                ["target"] = operation.LogicalTarget ?? string.Empty,
                ["provider"] = operation.ProviderId ?? string.Empty,
                ["kind"] = operation.OperationKind ?? string.Empty,
                ["key"] = operation.StableOperationKey ?? string.Empty
            };
            return "op." + CanonicalSerialization.Sha256(CanonicalSerialization.SerializeMap(map)).Substring(0, 24);
        }
    }
}
