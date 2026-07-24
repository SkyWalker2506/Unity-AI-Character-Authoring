using System;
using System.Collections.Generic;

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
                CharacterSpecId = spec.SpecId,
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

    public sealed class GenerationOperationComparer : IComparer<GenerationOperation>
    {
        public static readonly GenerationOperationComparer Instance = new GenerationOperationComparer();

        public int Compare(GenerationOperation x, GenerationOperation y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            int phase = x.Phase.CompareTo(y.Phase);
            if (phase != 0) return phase;
            int asset = string.CompareOrdinal(x.TargetAssetGuid ?? string.Empty, y.TargetAssetGuid ?? string.Empty);
            if (asset != 0) return asset;
            int obj = string.CompareOrdinal(x.TargetObjectId ?? string.Empty, y.TargetObjectId ?? string.Empty);
            if (obj != 0) return obj;
            int provider = string.CompareOrdinal(x.ProviderId ?? string.Empty, y.ProviderId ?? string.Empty);
            if (provider != 0) return provider;
            int kind = string.CompareOrdinal(x.OperationKind ?? string.Empty, y.OperationKind ?? string.Empty);
            if (kind != 0) return kind;
            return string.CompareOrdinal(x.StableOperationKey ?? string.Empty, y.StableOperationKey ?? string.Empty);
        }
    }

    public static class GenerationPlanHasher
    {
        public static string Hash(GenerationPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var map = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["schemaVersion"] = plan.SchemaVersion ?? string.Empty,
                ["planId"] = plan.PlanId ?? string.Empty,
                ["specId"] = plan.CharacterSpecId ?? string.Empty,
                ["source.guid"] = plan.Source?.PrefabGuid ?? string.Empty,
                ["source.version"] = plan.Source?.SourceVersion ?? string.Empty,
                ["source.normalizedHash"] = plan.Source?.SourceNormalizedHash ?? string.Empty
            };

            AddStringMap(map, "metadata.", plan.Metadata);

            var operations = new List<GenerationOperation>(plan.Operations ?? new List<GenerationOperation>());
            operations.Sort(GenerationOperationComparer.Instance);
            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                string prefix = "op." + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".";
                map[prefix + "id"] = op.OperationId ?? string.Empty;
                map[prefix + "phase"] = ((int)op.Phase).ToString(System.Globalization.CultureInfo.InvariantCulture);
                map[prefix + "logicalTarget"] = op.LogicalTarget ?? string.Empty;
                map[prefix + "asset"] = op.TargetAssetGuid ?? string.Empty;
                map[prefix + "object"] = op.TargetObjectId ?? string.Empty;
                map[prefix + "provider"] = op.ProviderId ?? string.Empty;
                map[prefix + "kind"] = op.OperationKind ?? string.Empty;
                map[prefix + "key"] = op.StableOperationKey ?? string.Empty;
                map[prefix + "security"] = ((int)op.SecurityLevel).ToString(System.Globalization.CultureInfo.InvariantCulture);
                map[prefix + "reversibility"] = ((int)op.Reversibility).ToString(System.Globalization.CultureInfo.InvariantCulture);

                if (op.Inputs != null)
                {
                    foreach (var input in new SortedDictionary<string, AuthoringFieldValue>(op.Inputs, StringComparer.Ordinal))
                        map[prefix + "input." + input.Key] = CanonicalSerialization.Serialize(input.Value);
                }

                AddStringList(map, prefix + "precondition.", op.Preconditions);
                AddStringList(map, prefix + "postcondition.", op.Postconditions);
            }

            return CanonicalSerialization.Sha256(CanonicalSerialization.SerializeMap(map));
        }

        static void AddStringMap(
            IDictionary<string, string> destination,
            string prefix,
            IDictionary<string, string> values)
        {
            if (values == null) return;
            foreach (var pair in new SortedDictionary<string, string>(values, StringComparer.Ordinal))
                destination[prefix + pair.Key] = pair.Value ?? string.Empty;
        }

        static void AddStringList(
            IDictionary<string, string> destination,
            string prefix,
            IList<string> values)
        {
            if (values == null)
            {
                destination[prefix + "count"] = "0";
                return;
            }

            destination[prefix + "count"] = values.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            for (int i = 0; i < values.Count; i++)
                destination[prefix + i.ToString(System.Globalization.CultureInfo.InvariantCulture)] = values[i] ?? string.Empty;
        }
    }
}
