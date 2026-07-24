using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlackMountains.AICharacterAuthoring
{
    public enum NpcBehaviorDomain
    {
        Core,
        Navigation,
        Movement,
        Senses,
        Combat,
        Interaction,
        Dialogue,
        Commerce,
        Companion,
        Animation,
        Equipment,
        SearchAndFallback
    }

    public enum NpcParameterValueType
    {
        String,
        StableId,
        Integer,
        Decimal,
        Boolean
    }

    public enum NpcBindingCardinality
    {
        Required,
        Optional,
        FallbackResolvable
    }

    public enum NpcResourceKind
    {
        AnimationRole,
        AnimationEventMarker,
        EquipmentSlot,
        EquipmentItem,
        Audio,
        Vfx,
        LocalizedText,
        Sensor,
        Navigation
    }

    [Serializable]
    public sealed class NpcResourceBindingRequirement
    {
        public string BindingId { get; set; }
        public NpcResourceKind Kind { get; set; }
        public NpcBindingCardinality Cardinality { get; set; }
        public string ResourceLogicalId { get; set; }
        public List<string> FallbackTags { get; set; } = new List<string>();
        public int MinimumCount { get; set; } = 1;
        public int MaximumCount { get; set; } = 1;
    }

    [Serializable]
    public sealed class NpcResourceCatalogEntry
    {
        public string StableId { get; set; }
        public NpcResourceKind Kind { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    [Serializable]
    public sealed class NpcResolvedResourceBinding
    {
        public string BindingId { get; set; }
        public NpcResourceKind Kind { get; set; }
        public NpcBindingCardinality Cardinality { get; set; }
        public List<string> ResolvedResourceIds { get; set; } =
            new List<string>();
        public bool UsedFallback { get; set; }
    }

    [Serializable]
    public sealed class NpcParameterDefinition
    {
        public string Key { get; set; }
        public NpcParameterValueType ValueType { get; set; }
        public bool Required { get; set; }
        public string DefaultValue { get; set; }
        public decimal? Minimum { get; set; }
        public decimal? Maximum { get; set; }
        public List<string> AllowedValues { get; set; } = new List<string>();
    }

    [Serializable]
    public sealed class NpcBehaviorAtomDefinition
    {
        public string StableId { get; set; }
        public string Version { get; set; } = "1";
        public string ProviderId { get; set; }
        public NpcBehaviorDomain Domain { get; set; }
        public List<NpcParameterDefinition> Parameters { get; set; } =
            new List<NpcParameterDefinition>();
        public List<string> RequiredCapabilityIds { get; set; } =
            new List<string>();
        public List<string> RequiredIntegrationIds { get; set; } =
            new List<string>();
        public List<NpcResourceBindingRequirement> ResourceBindings { get; set; } =
            new List<NpcResourceBindingRequirement>();
    }

    [Serializable]
    public sealed class NpcAtomInvocation
    {
        public string AtomStableId { get; set; }
        public string AtomVersion { get; set; } = "1";
        public string StepId { get; set; }
        public Dictionary<string, string> Parameters { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class NpcStrategyDefinition
    {
        public string StableId { get; set; }
        public string SemanticBehaviorId { get; set; }
        public string FamilyId { get; set; }
        public string VariantId { get; set; }
        public string Version { get; set; } = "1";
        public string ProviderId { get; set; }
        public NpcBehaviorDomain Domain { get; set; }
        public List<NpcParameterDefinition> Parameters { get; set; } =
            new List<NpcParameterDefinition>();
        public List<NpcAtomInvocation> Atoms { get; set; } =
            new List<NpcAtomInvocation>();
        public List<NpcResourceBindingRequirement> ResourceBindings { get; set; } =
            new List<NpcResourceBindingRequirement>();
        public string FallbackStrategyStableId { get; set; }
        public string FallbackStrategyVersion { get; set; }
    }

    [Serializable]
    public sealed class NpcStrategySelection
    {
        public string SemanticBehaviorId { get; set; }
        public string FamilyId { get; set; }
        public string StrategyStableId { get; set; }
        public string StrategyVersion { get; set; } = "1";
        public Dictionary<string, string> ParameterOverrides { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class NpcPriorityStateRecord
    {
        public string StateId { get; set; }
        public int Priority { get; set; }
        public string EntryConditionId { get; set; }
        public List<string> StrategyStableIds { get; set; } =
            new List<string>();
        public string FallbackStateId { get; set; }
    }

    [Serializable]
    public sealed class NpcBehaviorLegoCatalog
    {
        public const string CurrentSchemaVersion = "1";

        public string SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string StableId { get; set; }
        public string Version { get; set; } = "1";
        public List<NpcBehaviorAtomDefinition> Atoms { get; set; } =
            new List<NpcBehaviorAtomDefinition>();
        public List<NpcStrategyDefinition> Strategies { get; set; } =
            new List<NpcStrategyDefinition>();
    }

    public sealed class NpcResolvedStrategy
    {
        public string SemanticBehaviorId { get; set; }
        public string FamilyId { get; set; }
        public string StrategyStableId { get; set; }
        public string StrategyVersion { get; set; }
        public string ProviderId { get; set; }
        public Dictionary<string, string> Parameters { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
        public List<NpcAtomInvocation> Atoms { get; set; } =
            new List<NpcAtomInvocation>();
        public List<NpcResolvedResourceBinding> ResourceManifest { get; set; } =
            new List<NpcResolvedResourceBinding>();
    }

    public sealed class NpcStrategyResolutionResult
    {
        public List<NpcResolvedStrategy> Strategies { get; } =
            new List<NpcResolvedStrategy>();
        public DiagnosticList Diagnostics { get; } = new DiagnosticList();
        public bool Success => !Diagnostics.HasErrors;
    }

    public static class NpcStrategyResolver
    {
        public static NpcStrategyResolutionResult Resolve(
            NpcBehaviorLegoCatalog catalog,
            IList<NpcStrategySelection> selections,
            IList<NpcResourceCatalogEntry> availableResources = null)
        {
            var result = new NpcStrategyResolutionResult();
            NpcBehaviorLegoCatalogValidator.Validate(catalog, result.Diagnostics);
            if (result.Diagnostics.HasErrors)
                return result;

            var strategies = new Dictionary<string, NpcStrategyDefinition>(
                StringComparer.Ordinal);
            foreach (var strategy in catalog.Strategies)
                strategies[Key(strategy.StableId, strategy.Version)] = strategy;
            var atoms = new Dictionary<string, NpcBehaviorAtomDefinition>(
                StringComparer.Ordinal);
            foreach (var atom in catalog.Atoms)
                atoms[Key(atom.StableId, atom.Version)] = atom;

            var semantics = new HashSet<string>(StringComparer.Ordinal);
            foreach (var selection in selections ??
                new List<NpcStrategySelection>())
            {
                if (selection == null ||
                    !strategies.TryGetValue(
                        Key(
                            selection?.StrategyStableId,
                            selection?.StrategyVersion),
                        out var strategy))
                {
                    result.Diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-STRATEGY-EXACT-VARIANT-MISSING",
                        "The exact strategy stable ID and version are unavailable.",
                        selection?.StrategyStableId));
                    continue;
                }
                if (!string.Equals(
                        strategy.SemanticBehaviorId,
                        selection.SemanticBehaviorId,
                        StringComparison.Ordinal))
                {
                    result.Diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-STRATEGY-SEMANTIC-MISMATCH",
                        "Selected strategy does not implement the requested semantic behavior.",
                        selection.StrategyStableId));
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(selection.FamilyId) &&
                    !string.Equals(
                        strategy.FamilyId,
                        selection.FamilyId,
                        StringComparison.Ordinal))
                {
                    result.Diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-STRATEGY-FAMILY-MISMATCH",
                        "Selected strategy does not belong to the requested family.",
                        selection.StrategyStableId));
                    continue;
                }
                if (!semantics.Add(selection.SemanticBehaviorId))
                {
                    result.Diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-STRATEGY-SEMANTIC-DUPLICATE",
                        "A role recipe may select only one exact variant per semantic behavior.",
                        selection.SemanticBehaviorId));
                    continue;
                }

                var parameters = ResolveParameters(
                    strategy.Parameters,
                    selection.ParameterOverrides,
                    strategy.StableId,
                    result.Diagnostics);
                if (parameters == null)
                    continue;
                var resolved = new NpcResolvedStrategy
                {
                    SemanticBehaviorId = strategy.SemanticBehaviorId,
                    FamilyId = strategy.FamilyId,
                    StrategyStableId = strategy.StableId,
                    StrategyVersion = strategy.Version,
                    ProviderId = strategy.ProviderId,
                    Parameters = parameters,
                    Atoms = new List<NpcAtomInvocation>(strategy.Atoms)
                };
                var bindings = new List<NpcResourceBindingRequirement>(
                    strategy.ResourceBindings);
                foreach (var invocation in strategy.Atoms)
                {
                    if (atoms.TryGetValue(
                        Key(
                            invocation.AtomStableId,
                            invocation.AtomVersion),
                        out var atom))
                    {
                        bindings.AddRange(atom.ResourceBindings);
                    }
                }
                ResolveBindings(
                    bindings,
                    parameters,
                    availableResources,
                    resolved.ResourceManifest,
                    result.Diagnostics);
                result.Strategies.Add(resolved);
            }
            result.Strategies.Sort((left, right) =>
                string.CompareOrdinal(
                    left.SemanticBehaviorId,
                    right.SemanticBehaviorId));
            return result;
        }

        static void ResolveBindings(
            IList<NpcResourceBindingRequirement> requirements,
            IDictionary<string, string> parameters,
            IList<NpcResourceCatalogEntry> availableResources,
            ICollection<NpcResolvedResourceBinding> manifest,
            DiagnosticList diagnostics)
        {
            availableResources ??= new List<NpcResourceCatalogEntry>();
            foreach (var requirement in requirements ??
                new List<NpcResourceBindingRequirement>())
            {
                string logicalId = ResolveReference(
                    requirement.ResourceLogicalId,
                    parameters);
                var matches = new List<NpcResourceCatalogEntry>();
                foreach (var resource in availableResources)
                {
                    if (resource == null || resource.Kind != requirement.Kind)
                        continue;
                    if (!string.IsNullOrWhiteSpace(logicalId) &&
                        string.Equals(
                            resource.StableId,
                            logicalId,
                            StringComparison.Ordinal))
                    {
                        matches.Add(resource);
                    }
                }
                bool usedFallback = false;
                if (matches.Count < requirement.MinimumCount &&
                    requirement.Cardinality ==
                        NpcBindingCardinality.FallbackResolvable)
                {
                    matches.Clear();
                    foreach (var resource in availableResources)
                    {
                        if (resource != null &&
                            resource.Kind == requirement.Kind &&
                            ContainsAll(
                                resource.Tags,
                                requirement.FallbackTags))
                        {
                            matches.Add(resource);
                        }
                    }
                    usedFallback = matches.Count > 0;
                }
                matches.Sort((left, right) =>
                    string.CompareOrdinal(left.StableId, right.StableId));
                if (matches.Count > requirement.MaximumCount)
                    matches.RemoveRange(
                        requirement.MaximumCount,
                        matches.Count - requirement.MaximumCount);

                if (matches.Count < requirement.MinimumCount)
                {
                    if (requirement.Cardinality ==
                        NpcBindingCardinality.Optional)
                    {
                        diagnostics.Add(AuthoringDiagnostic.Warning(
                            "ACA-NPC-RESOURCE-OPTIONAL-MISSING",
                            "Optional strategy resource binding is unresolved.",
                            requirement.BindingId));
                    }
                    else
                    {
                        diagnostics.Add(AuthoringDiagnostic.Error(
                            "ACA-NPC-RESOURCE-REQUIRED-MISSING",
                            "Required or fallback-resolvable strategy resource binding is unresolved.",
                            requirement.BindingId));
                    }
                }
                manifest.Add(new NpcResolvedResourceBinding
                {
                    BindingId = requirement.BindingId,
                    Kind = requirement.Kind,
                    Cardinality = requirement.Cardinality,
                    UsedFallback = usedFallback,
                    ResolvedResourceIds = ToIds(matches)
                });
            }
        }

        static string ResolveReference(
            string value,
            IDictionary<string, string> parameters)
        {
            if (value != null &&
                value.StartsWith("$", StringComparison.Ordinal) &&
                parameters.TryGetValue(value.Substring(1), out var resolved))
            {
                return resolved;
            }
            return value;
        }

        static bool ContainsAll(
            IList<string> available,
            IList<string> required)
        {
            var values = new HashSet<string>(
                available ?? new List<string>(),
                StringComparer.Ordinal);
            foreach (var value in required ?? new List<string>())
                if (!values.Contains(value)) return false;
            return true;
        }

        static List<string> ToIds(
            IList<NpcResourceCatalogEntry> entries)
        {
            var ids = new List<string>();
            foreach (var entry in entries)
                ids.Add(entry.StableId);
            return ids;
        }

        static Dictionary<string, string> ResolveParameters(
            IList<NpcParameterDefinition> definitions,
            IDictionary<string, string> overrides,
            string target,
            DiagnosticList diagnostics)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var byKey = new Dictionary<string, NpcParameterDefinition>(
                StringComparer.Ordinal);
            foreach (var definition in definitions ??
                new List<NpcParameterDefinition>())
            {
                byKey[definition.Key] = definition;
                if (definition.DefaultValue != null)
                    result[definition.Key] = definition.DefaultValue;
            }
            foreach (var pair in overrides ??
                new Dictionary<string, string>())
            {
                if (!byKey.ContainsKey(pair.Key))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-STRATEGY-PARAMETER-UNKNOWN",
                        "Strategy override targets an unknown parameter.",
                        target + "." + pair.Key));
                    continue;
                }
                result[pair.Key] = pair.Value;
            }

            bool valid = true;
            foreach (var definition in byKey.Values)
            {
                if (!result.TryGetValue(definition.Key, out var value) ||
                    string.IsNullOrWhiteSpace(value))
                {
                    if (definition.Required)
                    {
                        diagnostics.Add(AuthoringDiagnostic.Error(
                            "ACA-NPC-STRATEGY-PARAMETER-REQUIRED",
                            "Required strategy parameter is missing.",
                            target + "." + definition.Key));
                        valid = false;
                    }
                    continue;
                }
                if (!ValidateValue(definition, value))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-STRATEGY-PARAMETER-INVALID",
                        "Strategy parameter value violates its type, range, or allowed values.",
                        target + "." + definition.Key));
                    valid = false;
                }
            }
            return valid ? result : null;
        }

        internal static bool ValidateValue(
            NpcParameterDefinition definition,
            string value)
        {
            if (definition.AllowedValues != null &&
                definition.AllowedValues.Count > 0 &&
                !definition.AllowedValues.Contains(value))
            {
                return false;
            }
            decimal numeric;
            switch (definition.ValueType)
            {
                case NpcParameterValueType.StableId:
                    return AuthoringIdentifier.IsValid(value);
                case NpcParameterValueType.Integer:
                    if (!int.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out var integer))
                        return false;
                    numeric = integer;
                    break;
                case NpcParameterValueType.Decimal:
                    if (!decimal.TryParse(
                            value,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out numeric))
                        return false;
                    break;
                case NpcParameterValueType.Boolean:
                    return bool.TryParse(value, out _);
                default:
                    return true;
            }
            return (!definition.Minimum.HasValue ||
                    numeric >= definition.Minimum.Value) &&
                (!definition.Maximum.HasValue ||
                 numeric <= definition.Maximum.Value);
        }

        static string Key(string stableId, string version)
        {
            return (stableId ?? string.Empty) + "@" +
                (version ?? string.Empty);
        }
    }

    public static class NpcBehaviorLegoCatalogValidator
    {
        public static void Validate(
            NpcBehaviorLegoCatalog catalog,
            DiagnosticList diagnostics)
        {
            if (catalog == null)
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-LEGO-CATALOG-NULL",
                    "Behavior Lego catalog is required."));
                return;
            }
            var atoms = new Dictionary<string, NpcBehaviorAtomDefinition>(
                StringComparer.Ordinal);
            foreach (var atom in catalog.Atoms ??
                new List<NpcBehaviorAtomDefinition>())
            {
                string key = Key(atom?.StableId, atom?.Version);
                if (atom == null ||
                    !AuthoringIdentifier.IsValid(atom.StableId) ||
                    !AuthoringIdentifier.IsValid(atom.ProviderId) ||
                    string.IsNullOrWhiteSpace(atom.Version) ||
                    atoms.ContainsKey(key))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-ATOM-DEFINITION",
                        "Atomic block stable ID/version/provider must be valid and unique.",
                        atom?.StableId));
                    continue;
                }
                atoms.Add(key, atom);
                ValidateParameterDefinitions(
                    atom.Parameters,
                    atom.StableId,
                    diagnostics);
                ValidateResourceBindings(
                    atom.ResourceBindings,
                    atom.StableId,
                    diagnostics);
            }

            var strategies = new HashSet<string>(StringComparer.Ordinal);
            foreach (var strategy in catalog.Strategies ??
                new List<NpcStrategyDefinition>())
            {
                string key = Key(strategy?.StableId, strategy?.Version);
                if (strategy == null ||
                    !AuthoringIdentifier.IsValid(strategy.StableId) ||
                    !AuthoringIdentifier.IsValid(
                        strategy.SemanticBehaviorId) ||
                    !AuthoringIdentifier.IsValid(strategy.FamilyId) ||
                    !AuthoringIdentifier.IsValid(strategy.VariantId) ||
                    !AuthoringIdentifier.IsValid(strategy.ProviderId) ||
                    string.IsNullOrWhiteSpace(strategy.Version) ||
                    !strategies.Add(key))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-STRATEGY-DEFINITION",
                        "Strategy stable ID/semantic/variant/version/provider must be valid and unique.",
                        strategy?.StableId));
                    continue;
                }
                ValidateParameterDefinitions(
                    strategy.Parameters,
                    strategy.StableId,
                    diagnostics);
                ValidateResourceBindings(
                    strategy.ResourceBindings,
                    strategy.StableId,
                    diagnostics);
                var stepIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var invocation in strategy.Atoms ??
                    new List<NpcAtomInvocation>())
                {
                    if (invocation == null ||
                        !atoms.TryGetValue(
                            Key(
                                invocation.AtomStableId,
                                invocation.AtomVersion),
                            out var atom) ||
                        !AuthoringIdentifier.IsValid(invocation.StepId) ||
                        !stepIds.Add(invocation.StepId))
                    {
                        diagnostics.Add(AuthoringDiagnostic.Error(
                            "ACA-NPC-STRATEGY-ATOM",
                            "Strategy atom reference/version/step ID is invalid.",
                            strategy.StableId));
                        continue;
                    }
                    ValidateBindings(
                        invocation,
                        atom,
                        strategy,
                        diagnostics);
                }
            }
            foreach (var strategy in catalog.Strategies ??
                new List<NpcStrategyDefinition>())
            {
                if (strategy != null &&
                    !string.IsNullOrWhiteSpace(
                        strategy.FallbackStrategyStableId) &&
                    !strategies.Contains(Key(
                        strategy.FallbackStrategyStableId,
                        strategy.FallbackStrategyVersion)))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-STRATEGY-FALLBACK",
                        "Strategy fallback exact ID/version is unresolved.",
                        strategy.StableId));
                }
            }
        }

        static void ValidateBindings(
            NpcAtomInvocation invocation,
            NpcBehaviorAtomDefinition atom,
            NpcStrategyDefinition strategy,
            DiagnosticList diagnostics)
        {
            var strategyParameters = new HashSet<string>(
                StringComparer.Ordinal);
            foreach (var parameter in strategy.Parameters)
                strategyParameters.Add(parameter.Key);
            var atomParameters = new Dictionary<string, NpcParameterDefinition>(
                StringComparer.Ordinal);
            foreach (var parameter in atom.Parameters)
                atomParameters[parameter.Key] = parameter;

            foreach (var pair in invocation.Parameters)
            {
                if (!atomParameters.TryGetValue(pair.Key, out var definition))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-ATOM-BINDING-UNKNOWN",
                        "Atom invocation binds an unknown atom parameter.",
                        strategy.StableId + "." + invocation.StepId));
                }
                else if (pair.Value != null &&
                    pair.Value.StartsWith("$", StringComparison.Ordinal))
                {
                    if (!strategyParameters.Contains(pair.Value.Substring(1)))
                    {
                        diagnostics.Add(AuthoringDiagnostic.Error(
                            "ACA-NPC-ATOM-BINDING-PARAMETER",
                            "Atom invocation references an unknown strategy parameter.",
                            strategy.StableId + "." + pair.Value));
                    }
                }
                else if (!NpcStrategyResolver.ValidateValue(
                    definition,
                    pair.Value))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-ATOM-BINDING-VALUE",
                        "Atom invocation literal is invalid.",
                        strategy.StableId + "." + invocation.StepId));
                }
            }
            foreach (var definition in atom.Parameters)
            {
                if (definition.Required &&
                    definition.DefaultValue == null &&
                    !invocation.Parameters.ContainsKey(definition.Key))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-ATOM-BINDING-REQUIRED",
                        "Atom invocation does not bind a required atom parameter.",
                        strategy.StableId + "." + invocation.StepId +
                            "." + definition.Key));
                }
            }
        }

        static void ValidateParameterDefinitions(
            IList<NpcParameterDefinition> parameters,
            string target,
            DiagnosticList diagnostics)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parameter in parameters ??
                new List<NpcParameterDefinition>())
            {
                if (parameter == null ||
                    !AuthoringIdentifier.IsValid(parameter.Key) ||
                    !keys.Add(parameter.Key) ||
                    (parameter.DefaultValue != null &&
                     !NpcStrategyResolver.ValidateValue(
                         parameter,
                         parameter.DefaultValue)))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-PARAMETER-DEFINITION",
                        "Parameter key/default must be valid and unique.",
                        target));
                }
            }
        }

        static void ValidateResourceBindings(
            IList<NpcResourceBindingRequirement> bindings,
            string target,
            DiagnosticList diagnostics)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in bindings ??
                new List<NpcResourceBindingRequirement>())
            {
                bool logicalOrFallback =
                    AuthoringIdentifier.IsValid(
                        binding?.ResourceLogicalId) ||
                    (binding?.ResourceLogicalId != null &&
                     binding.ResourceLogicalId.StartsWith(
                         "$",
                         StringComparison.Ordinal)) ||
                    (binding?.FallbackTags != null &&
                     binding.FallbackTags.Count > 0);
                if (binding == null ||
                    !AuthoringIdentifier.IsValid(binding.BindingId) ||
                    !ids.Add(binding.BindingId) ||
                    binding.MinimumCount < 0 ||
                    binding.MaximumCount < binding.MinimumCount ||
                    !logicalOrFallback)
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-RESOURCE-BINDING-DEFINITION",
                        "Resource binding ID/cardinality/count/resource query is invalid.",
                        target));
                }
            }
        }

        static string Key(string stableId, string version)
        {
            return (stableId ?? string.Empty) + "@" +
                (version ?? string.Empty);
        }
    }

    [Serializable]
    public sealed class NpcCustomCapabilityDiscoveryEvidence
    {
        public string RequestedCapabilityId { get; set; }
        public List<string> SearchedProviderIds { get; set; } =
            new List<string>();
        public List<string> EquivalentCapabilityIds { get; set; } =
            new List<string>();
        public string CustomProviderId { get; set; }
        public string CustomCapabilityVersion { get; set; }
        public string MigrationId { get; set; }
        public string FallbackCapabilityId { get; set; }
        public bool HasContractTests { get; set; }
    }

    public static class NpcCustomCapabilityPolicy
    {
        public static readonly IReadOnlyList<string> RequiredDiscoveryProviders =
            new[]
            {
                NpcIntegrationIds.BehaviorDesigner,
                NpcIntegrationIds.BehaviorDesignerMovementPack,
                NpcIntegrationIds.BehaviorDesignerSensesPack,
                NpcIntegrationIds.StpInteraction,
                NpcIntegrationIds.StpWeaponIntegration,
                NpcIntegrationIds.SemanticAnimationLibrary,
                "provider.nwb.project-adapters"
            };

        public static DiagnosticList Validate(
            NpcCustomCapabilityDiscoveryEvidence evidence)
        {
            var diagnostics = new DiagnosticList();
            if (evidence == null)
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-CUSTOM-DISCOVERY-REQUIRED",
                    "Custom capability generation requires discovery evidence."));
                return diagnostics;
            }
            var searched = new HashSet<string>(
                evidence.SearchedProviderIds ??
                new List<string>(),
                StringComparer.Ordinal);
            foreach (var provider in RequiredDiscoveryProviders)
            {
                if (!searched.Contains(provider))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-CUSTOM-DISCOVERY-INCOMPLETE",
                        "Equivalent-capability discovery did not inspect every required provider.",
                        provider));
                }
            }
            if (evidence.EquivalentCapabilityIds != null &&
                evidence.EquivalentCapabilityIds.Count > 0)
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-CUSTOM-EQUIVALENT-EXISTS",
                    "Custom code is prohibited because an equivalent capability was discovered.",
                    evidence.RequestedCapabilityId));
            }
            if (!AuthoringIdentifier.IsValid(evidence.CustomProviderId) ||
                string.IsNullOrWhiteSpace(
                    evidence.CustomCapabilityVersion) ||
                !AuthoringIdentifier.IsValid(evidence.MigrationId) ||
                !AuthoringIdentifier.IsValid(
                    evidence.FallbackCapabilityId) ||
                !evidence.HasContractTests)
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-CUSTOM-CONTRACT-INCOMPLETE",
                    "Custom capability requires provider/version, migration, fallback, and contract tests.",
                    evidence.RequestedCapabilityId));
            }
            return diagnostics;
        }
    }

    public static class NpcBuiltInBehaviorLegoCatalog
    {
        const string Provider = "provider.blackmountains.npc-lego";

        public static NpcBehaviorLegoCatalog Create()
        {
            var catalog = new NpcBehaviorLegoCatalog
            {
                StableId = "catalog.blackmountains.npc-behavior-legos"
            };
            AddAtoms(catalog);
            AddMovementStrategies(catalog);
            AddDomainStrategies(catalog);
            return catalog;
        }

        static void AddAtoms(NpcBehaviorLegoCatalog catalog)
        {
            foreach (var row in new[]
            {
                AtomRow("target.select", NpcBehaviorDomain.Core),
                AtomRow("target.validate", NpcBehaviorDomain.Core),
                AtomRow("faction.validate", NpcBehaviorDomain.Core, Id("faction-policy-id", true, null)),
                AtomRow("range.check", NpcBehaviorDomain.Core, Decimal("distance", true, "2", 0, 1000)),
                AtomRow("los.check", NpcBehaviorDomain.Senses),
                AtomRow("path.calculate", NpcBehaviorDomain.Navigation, Decimal("repath-seconds", false, "0.5", 0.05m, 30)),
                AtomRow("move", NpcBehaviorDomain.Movement, Decimal("speed", false, "3.5", 0, 20)),
                AtomRow("stop", NpcBehaviorDomain.Movement),
                AtomRow("wait", NpcBehaviorDomain.Movement, Decimal("seconds", true, "1", 0, 120)),
                AtomRow("face", NpcBehaviorDomain.Movement, Decimal("degrees-per-second", false, "360", 0, 2000)),
                AtomRow("sense", NpcBehaviorDomain.Senses, Id("sense-capability", true, null)),
                AtomRow("timeout", NpcBehaviorDomain.SearchAndFallback, Decimal("seconds", true, "5", 0, 600)),
                AtomRow("fallback", NpcBehaviorDomain.SearchAndFallback, Id("fallback-id", true, null)),
                AtomRow("blackboard.set", NpcBehaviorDomain.Core, Id("key", true, null), Text("value", true, null)),
                AtomRow("animation.play-role", NpcBehaviorDomain.Animation, Id("role-id", true, null)),
                AtomRow("weapon.draw", NpcBehaviorDomain.Equipment),
                AtomRow("weapon.stow", NpcBehaviorDomain.Equipment),
                AtomRow("weapon.equip", NpcBehaviorDomain.Equipment),
                AtomRow("interaction.gate", NpcBehaviorDomain.Interaction, Decimal("range", false, "3", 0, 20)),
                AtomRow("damage.window", NpcBehaviorDomain.Combat, Id("event-id", true, null)),
                AtomRow("cooldown", NpcBehaviorDomain.Combat, Decimal("seconds", true, "1", 0, 60)),
                AtomRow("interrupt.check", NpcBehaviorDomain.Combat, Id("interrupt-policy-id", true, null)),
                AtomRow("recovery", NpcBehaviorDomain.Combat, Decimal("seconds", true, "0.25", 0, 30)),
                AtomRow("aim", NpcBehaviorDomain.Combat, Decimal("seconds", false, "0.2", 0, 30)),
                AtomRow("weapon.fire", NpcBehaviorDomain.Combat, Integer("count", false, "1", 1, 100)),
                AtomRow("weapon.reload", NpcBehaviorDomain.Combat),
                AtomRow("weapon.block", NpcBehaviorDomain.Combat, Decimal("seconds", false, "0.5", 0, 30)),
                AtomRow("dialogue.present", NpcBehaviorDomain.Dialogue, Id("line-policy-id", true, null)),
                AtomRow("commerce.transact", NpcBehaviorDomain.Commerce, Id("transaction-policy-id", true, null)),
                AtomRow("companion.resolve-leader", NpcBehaviorDomain.Companion, Id("leader-policy-id", true, null)),
                AtomRow("equipment.select", NpcBehaviorDomain.Equipment, Id("equipment-query-id", true, null))
            })
            {
                catalog.Atoms.Add(row);
            }
            foreach (var atom in catalog.Atoms)
            {
                if (atom.StableId == "npc.atom.path.calculate")
                {
                    atom.ResourceBindings.Add(Resource(
                        "npc.binding.navigation.agent",
                        NpcResourceKind.Navigation,
                        NpcBindingCardinality.Required,
                        "nwb.nav-agent.humanoid"));
                }
            }
        }

        static void AddMovementStrategies(NpcBehaviorLegoCatalog catalog)
        {
            AddVariantFamily(
                catalog,
                "wander",
                NpcBehaviorDomain.Movement,
                new[]
                {
                    "uniform-radius",
                    "biased",
                    "avoid-recent",
                    "safe-zone"
                },
                new[]
                {
                    Decimal("radius", true, "5", 0.1m, 100),
                    Decimal("dwell-min", false, "1", 0, 120),
                    Decimal("dwell-max", false, "3", 0, 120),
                    Integer("seed", false, "1", 0, int.MaxValue),
                    Integer("area-mask", false, "-1", -1, int.MaxValue),
                    Id("failure-behavior", false, "npc.failure.stop")
                },
                Inv("target.select", "select-point"),
                Inv("path.calculate", "calculate-path"),
                Inv("move", "move"),
                Inv("wait", "dwell", "seconds", "$dwell-min"),
                Inv("fallback", "fallback", "fallback-id", "$failure-behavior"));
            AddVariantFamily(
                catalog,
                "patrol",
                NpcBehaviorDomain.Movement,
                new[]
                {
                    "sequence",
                    "random",
                    "ping-pong",
                    "area",
                    "alert-route",
                    "guard-orbit"
                },
                new[]
                {
                    Id("route-id", true, "npc.route.default"),
                    Decimal("dwell-seconds", false, "1", 0, 120),
                    Integer("seed", false, "1", 0, int.MaxValue),
                    Integer("area-mask", false, "-1", -1, int.MaxValue),
                    Id("failure-behavior", false, "npc.failure.stop")
                },
                Inv("target.select", "select-waypoint"),
                Inv("path.calculate", "calculate-path"),
                Inv("move", "move"),
                Inv("face", "face"),
                Inv("wait", "dwell", "seconds", "$dwell-seconds"),
                Inv("fallback", "fallback", "fallback-id", "$failure-behavior"));
            Strategy(
                catalog,
                "pursue",
                "direct",
                NpcBehaviorDomain.Movement,
                new[] { Decimal("stopping-distance", false, "1.5", 0, 20) },
                Inv("target.validate", "validate-target"),
                Inv("path.calculate", "calculate-path"),
                Inv("move", "approach"),
                Inv("face", "face"));
            Strategy(
                catalog,
                "flee",
                "safe-zone",
                NpcBehaviorDomain.Movement,
                new[] { Decimal("safe-distance", false, "12", 0.1m, 100) },
                Inv("target.validate", "validate-threat"),
                Inv("target.select", "select-safe-point"),
                Inv("path.calculate", "calculate-path"),
                Inv("move", "escape"),
                Inv("fallback", "fallback", "fallback-id", "npc.failure.stop"));
        }

        static void AddDomainStrategies(NpcBehaviorLegoCatalog catalog)
        {
            Strategy(catalog, "sense-visibility", "cone-memory", NpcBehaviorDomain.Senses,
                new[] { Decimal("distance", false, "24", 0, 200) },
                Inv("sense", "sense", "sense-capability", NpcCapabilityIds.SenseVisibility),
                Inv("timeout", "memory", "seconds", "5"),
                Inv("blackboard.set", "store-target", "key", "npc.blackboard.target", "value", "detected"));
            Strategy(catalog, "sense-sound", "radius", NpcBehaviorDomain.Senses,
                new[] { Decimal("distance", false, "18", 0, 200) },
                Inv("sense", "sense", "sense-capability", NpcCapabilityIds.SenseSound));
            AddCombatStrategies(catalog);
            Strategy(catalog, "interaction", "range-gate", NpcBehaviorDomain.Interaction,
                new[] { Decimal("range", false, "3", 0.1m, 20) },
                Inv("interaction.gate", "gate", "range", "$range"),
                Inv("stop", "stop"),
                Inv("face", "face"));
            Strategy(catalog, "dialogue", "localized-line", NpcBehaviorDomain.Dialogue,
                new[]
                {
                    Id("line-policy-id", true, "npc.dialogue.policy.localized-fallback"),
                    Id("localized-text-id", true, "npc.localization.dialogue.default")
                },
                Inv("dialogue.present", "present", "line-policy-id", "$line-policy-id"));
            Strategy(catalog, "commerce", "atomic-offer", NpcBehaviorDomain.Commerce,
                new[] { Id("transaction-policy-id", true, "npc.commerce.policy.atomic-rollback") },
                Inv("commerce.transact", "transact", "transaction-policy-id", "$transaction-policy-id"));
            Strategy(catalog, "companion-follow", "distance-band", NpcBehaviorDomain.Companion,
                new[] { Decimal("distance", false, "3", 0, 30) },
                Inv("companion.resolve-leader", "resolve-leader", "leader-policy-id", "project.local-player"),
                Inv("path.calculate", "calculate-path"),
                Inv("move", "follow"),
                Inv("stop", "hold-distance"));
            Strategy(catalog, "equipment-threat", "draw-stow", NpcBehaviorDomain.Equipment,
                Array.Empty<NpcParameterDefinition>(),
                Inv("equipment.select", "select", "equipment-query-id", "npc.equipment.query.primary-weapon"),
                Inv("weapon.equip", "equip"),
                Inv("weapon.draw", "draw"),
                Inv("weapon.stow", "stow"));
            Strategy(catalog, "search", "last-known-position", NpcBehaviorDomain.SearchAndFallback,
                new[] { Decimal("timeout-seconds", false, "8", 0, 120) },
                Inv("blackboard.set", "read-last-known", "key", "npc.blackboard.last-known", "value", "read"),
                Inv("path.calculate", "calculate-path"),
                Inv("move", "search"),
                Inv("timeout", "timeout", "seconds", "$timeout-seconds"),
                Inv("fallback", "fallback", "fallback-id", "npc.failure.return-to-route"));
            Strategy(catalog, "animation-role", "semantic-catalog", NpcBehaviorDomain.Animation,
                new[] { Id("role-id", true, "npc.animation.shared.idle") },
                Inv("animation.play-role", "play", "role-id", "$role-id"));

            AddStrategyBinding(
                catalog,
                "npc.strategy.dialogue.localized-line",
                Resource(
                    "npc.binding.dialogue.localized-text",
                    NpcResourceKind.LocalizedText,
                    NpcBindingCardinality.Required,
                    "$localized-text-id"));
            AddStrategyBinding(
                catalog,
                "npc.strategy.sense-visibility.cone-memory",
                Resource(
                    "npc.binding.sensor.visibility",
                    NpcResourceKind.Sensor,
                    NpcBindingCardinality.Required,
                    NpcCapabilityIds.SenseVisibility));
            AddStrategyBinding(
                catalog,
                "npc.strategy.sense-sound.radius",
                Resource(
                    "npc.binding.sensor.sound",
                    NpcResourceKind.Sensor,
                    NpcBindingCardinality.Required,
                    NpcCapabilityIds.SenseSound));
            AddStrategyBinding(
                catalog,
                "npc.strategy.animation-role.semantic-catalog",
                Resource(
                    "npc.binding.animation.role",
                    NpcResourceKind.AnimationRole,
                    NpcBindingCardinality.Required,
                    "$role-id"));
            AddStrategyBinding(
                catalog,
                "npc.strategy.equipment-threat.draw-stow",
                Resource(
                    "npc.binding.equipment.primary",
                    NpcResourceKind.EquipmentItem,
                    NpcBindingCardinality.FallbackResolvable,
                    "npc.equipment.primary-weapon",
                    "equipment.weapon"));
            AddStrategyBinding(
                catalog,
                "npc.strategy.commerce.atomic-offer",
                Resource(
                    "npc.binding.commerce.label",
                    NpcResourceKind.LocalizedText,
                    NpcBindingCardinality.Optional,
                    "npc.localization.commerce.default"));
        }

        static void AddCombatStrategies(NpcBehaviorLegoCatalog catalog)
        {
            var meleeParameters = new[]
            {
                Decimal("range", false, "1.5", 0.1m, 20),
                Decimal("cooldown-seconds", false, "1.2", 0, 60),
                Decimal("recovery-seconds", false, "0.25", 0, 30),
                Id("faction-policy-id", true, "npc.faction.policy.hostile-only"),
                Id("interrupt-policy-id", true, "npc.interrupt.policy.damage-or-death"),
                Id("animation-role-id", true, "npc.animation.shared.attack"),
                Id("damage-event-id", true, "npc.event.damage-window")
            };
            foreach (var variant in new[]
            {
                "unarmed",
                "one-hand",
                "two-hand",
                "combo",
                "charge",
                "block-counter"
            })
            {
                Strategy(
                    catalog,
                    "attack-melee",
                    variant,
                    NpcBehaviorDomain.Combat,
                    meleeParameters,
                    Inv("target.validate", "validate-target"),
                    Inv("faction.validate", "validate-faction", "faction-policy-id", "$faction-policy-id"),
                    Inv("range.check", "check-range", "distance", "$range"),
                    Inv("los.check", "check-los"),
                    Inv("interrupt.check", "check-interrupt", "interrupt-policy-id", "$interrupt-policy-id"),
                    Inv("weapon.equip", "equip"),
                    Inv("path.calculate", "calculate-approach"),
                    Inv("move", "approach"),
                    Inv("face", "face"),
                    Inv("animation.play-role", "play-attack", "role-id", "$animation-role-id"),
                    Inv("damage.window", "damage-window", "event-id", "$damage-event-id"),
                    Inv("cooldown", "cooldown", "seconds", "$cooldown-seconds"),
                    Inv("recovery", "recovery", "seconds", "$recovery-seconds"),
                    Inv("fallback", "fallback", "fallback-id", "npc.failure.stop"));
                AddCombatResources(
                    catalog,
                    "npc.strategy.attack-melee." + variant,
                    variant != "unarmed");
            }

            var rangedParameters = new[]
            {
                Decimal("minimum-range", false, "4", 0, 200),
                Decimal("maximum-range", false, "35", 0.1m, 500),
                Decimal("aim-seconds", false, "0.2", 0, 30),
                Integer("shot-count", false, "1", 1, 100),
                Decimal("cooldown-seconds", false, "0.6", 0, 60),
                Decimal("recovery-seconds", false, "0.15", 0, 30),
                Id("faction-policy-id", true, "npc.faction.policy.hostile-only"),
                Id("interrupt-policy-id", true, "npc.interrupt.policy.damage-or-death"),
                Id("animation-role-id", true, "npc.animation.shared.attack"),
                Id("damage-event-id", true, "npc.event.damage-window")
            };
            foreach (var variant in new[]
            {
                "hitscan",
                "projectile",
                "single",
                "burst",
                "aim-reload",
                "cover-peek"
            })
            {
                Strategy(
                    catalog,
                    "attack-ranged",
                    variant,
                    NpcBehaviorDomain.Combat,
                    rangedParameters,
                    Inv("target.validate", "validate-target"),
                    Inv("faction.validate", "validate-faction", "faction-policy-id", "$faction-policy-id"),
                    Inv("los.check", "check-los"),
                    Inv("range.check", "check-maximum-range", "distance", "$maximum-range"),
                    Inv("interrupt.check", "check-interrupt", "interrupt-policy-id", "$interrupt-policy-id"),
                    Inv("weapon.equip", "equip"),
                    Inv("face", "face"),
                    Inv("aim", "aim", "seconds", "$aim-seconds"),
                    Inv("animation.play-role", "play-attack", "role-id", "$animation-role-id"),
                    Inv("weapon.fire", "fire", "count", "$shot-count"),
                    Inv("damage.window", "damage-window", "event-id", "$damage-event-id"),
                    Inv("weapon.reload", "reload"),
                    Inv("cooldown", "cooldown", "seconds", "$cooldown-seconds"),
                    Inv("recovery", "recovery", "seconds", "$recovery-seconds"),
                    Inv("fallback", "fallback", "fallback-id", "npc.failure.seek-cover"));
                AddCombatResources(
                    catalog,
                    "npc.strategy.attack-ranged." + variant,
                    true);
            }

            foreach (var variant in new[] { "range-band-switch", "fallback" })
            {
                Strategy(
                    catalog,
                    "attack-hybrid",
                    variant,
                    NpcBehaviorDomain.Combat,
                    new[]
                    {
                        Decimal("melee-threshold", false, "2.5", 0, 50),
                        Decimal("ranged-threshold", false, "18", 0, 500),
                        Id("melee-strategy-id", true, "npc.strategy.attack-melee.one-hand"),
                        Id("ranged-strategy-id", true, "npc.strategy.attack-ranged.single"),
                        Id("fallback-strategy-id", true, "npc.strategy.attack-melee.unarmed")
                    },
                    Inv("target.validate", "validate-target"),
                    Inv("los.check", "check-los"),
                    Inv("range.check", "check-melee-band", "distance", "$melee-threshold"),
                    Inv("blackboard.set", "select-melee", "key", "npc.blackboard.attack-strategy", "value", "$melee-strategy-id"),
                    Inv("range.check", "check-ranged-band", "distance", "$ranged-threshold"),
                    Inv("blackboard.set", "select-ranged", "key", "npc.blackboard.attack-strategy", "value", "$ranged-strategy-id"),
                    Inv("fallback", "fallback", "fallback-id", "$fallback-strategy-id"));
            }
        }

        static void AddCombatResources(
            NpcBehaviorLegoCatalog catalog,
            string strategyStableId,
            bool requiresEquipment)
        {
            var bindings = new List<NpcResourceBindingRequirement>
            {
                Resource(
                    "npc.binding.combat.animation",
                    NpcResourceKind.AnimationRole,
                    NpcBindingCardinality.Required,
                    "$animation-role-id"),
                Resource(
                    "npc.binding.combat.damage-event",
                    NpcResourceKind.AnimationEventMarker,
                    NpcBindingCardinality.Required,
                    "$damage-event-id"),
                Resource(
                    "npc.binding.combat.audio",
                    NpcResourceKind.Audio,
                    NpcBindingCardinality.Optional,
                    "npc.audio.combat.attack"),
                Resource(
                    "npc.binding.combat.vfx",
                    NpcResourceKind.Vfx,
                    NpcBindingCardinality.Optional,
                    "npc.vfx.combat.attack")
            };
            if (requiresEquipment)
            {
                bindings.Add(Resource(
                    "npc.binding.combat.weapon",
                    NpcResourceKind.EquipmentItem,
                    NpcBindingCardinality.FallbackResolvable,
                    "npc.equipment.primary-weapon",
                    "equipment.weapon"));
            }
            AddStrategyBinding(
                catalog,
                strategyStableId,
                bindings.ToArray());
        }

        static void AddStrategyBinding(
            NpcBehaviorLegoCatalog catalog,
            string strategyStableId,
            params NpcResourceBindingRequirement[] bindings)
        {
            foreach (var strategy in catalog.Strategies)
            {
                if (string.Equals(
                    strategy.StableId,
                    strategyStableId,
                    StringComparison.Ordinal))
                {
                    strategy.ResourceBindings.AddRange(bindings);
                    return;
                }
            }
        }

        static NpcResourceBindingRequirement Resource(
            string bindingId,
            NpcResourceKind kind,
            NpcBindingCardinality cardinality,
            string logicalId,
            params string[] fallbackTags)
        {
            return new NpcResourceBindingRequirement
            {
                BindingId = bindingId,
                Kind = kind,
                Cardinality = cardinality,
                ResourceLogicalId = logicalId,
                FallbackTags = new List<string>(
                    fallbackTags ?? Array.Empty<string>())
            };
        }

        static void AddVariantFamily(
            NpcBehaviorLegoCatalog catalog,
            string semantic,
            NpcBehaviorDomain domain,
            IEnumerable<string> variants,
            NpcParameterDefinition[] parameters,
            params NpcAtomInvocation[] atoms)
        {
            foreach (var variant in variants)
                Strategy(catalog, semantic, variant, domain, parameters, atoms);
        }

        static void Strategy(
            NpcBehaviorLegoCatalog catalog,
            string semantic,
            string variant,
            NpcBehaviorDomain domain,
            NpcParameterDefinition[] parameters,
            params NpcAtomInvocation[] atoms)
        {
            catalog.Strategies.Add(new NpcStrategyDefinition
            {
                StableId = "npc.strategy." + semantic + "." + variant,
                SemanticBehaviorId = "npc.semantic." + semantic,
                FamilyId = "npc.family." + semantic,
                VariantId = "npc.variant." + variant,
                ProviderId = Provider,
                Domain = domain,
                Parameters = new List<NpcParameterDefinition>(parameters),
                Atoms = new List<NpcAtomInvocation>(atoms)
            });
        }

        static NpcBehaviorAtomDefinition AtomRow(
            string id,
            NpcBehaviorDomain domain,
            params NpcParameterDefinition[] parameters)
        {
            return new NpcBehaviorAtomDefinition
            {
                StableId = "npc.atom." + id,
                ProviderId = Provider,
                Domain = domain,
                Parameters = new List<NpcParameterDefinition>(parameters)
            };
        }

        static NpcAtomInvocation Inv(
            string atom,
            string step,
            params string[] parameterPairs)
        {
            var invocation = new NpcAtomInvocation
            {
                AtomStableId = "npc.atom." + atom,
                StepId = "npc.step." + step
            };
            for (int i = 0; i + 1 < parameterPairs.Length; i += 2)
                invocation.Parameters[parameterPairs[i]] =
                    parameterPairs[i + 1];
            return invocation;
        }

        static NpcParameterDefinition Decimal(
            string key,
            bool required,
            string value,
            decimal min,
            decimal max)
        {
            return Parameter(
                key,
                NpcParameterValueType.Decimal,
                required,
                value,
                min,
                max);
        }

        static NpcParameterDefinition Integer(
            string key,
            bool required,
            string value,
            decimal min,
            decimal max)
        {
            return Parameter(
                key,
                NpcParameterValueType.Integer,
                required,
                value,
                min,
                max);
        }

        static NpcParameterDefinition Id(
            string key,
            bool required,
            string value)
        {
            return Parameter(
                key,
                NpcParameterValueType.StableId,
                required,
                value,
                null,
                null);
        }

        static NpcParameterDefinition Text(
            string key,
            bool required,
            string value)
        {
            return Parameter(
                key,
                NpcParameterValueType.String,
                required,
                value,
                null,
                null);
        }

        static NpcParameterDefinition Parameter(
            string key,
            NpcParameterValueType type,
            bool required,
            string value,
            decimal? min,
            decimal? max)
        {
            return new NpcParameterDefinition
            {
                Key = key,
                ValueType = type,
                Required = required,
                DefaultValue = value,
                Minimum = min,
                Maximum = max
            };
        }
    }
}
