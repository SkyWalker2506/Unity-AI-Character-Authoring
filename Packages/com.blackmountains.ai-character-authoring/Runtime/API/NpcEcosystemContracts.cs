using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlackMountains.AICharacterAuthoring
{
    public enum NpcArchetypeClass
    {
        Humanoid,
        Quadruped,
        Flying,
        SmallCritter,
        Custom
    }

    public enum NpcEcologicalDisposition
    {
        Neutral,
        Predator,
        Prey,
        Avoid,
        Compete
    }

    public enum NpcVariationDiscoveryStatus
    {
        Disabled,
        Requested,
        Completed,
        NoCompatibleVariant,
        Failed
    }

    public enum NpcVariationCandidateDecision
    {
        Pending,
        Accepted,
        Rejected,
        Suppressed
    }

    [Serializable]
    public sealed class NpcAttackContactRecord
    {
        public string ContactId { get; set; }
        public string TransformLogicalId { get; set; }
        public decimal Radius { get; set; } = 0.15m;
        public string AnimationEventSemantic { get; set; }
    }

    [Serializable]
    public sealed class NpcArchetypeProfileRecord
    {
        public string ProfileId { get; set; }
        public NpcArchetypeClass Archetype { get; set; }
        public string SpeciesId { get; set; }
        public string RigLogicalId { get; set; }
        public string AvatarLogicalId { get; set; }
        public string NavigationAgentLogicalId { get; set; }
        public string LocomotionFamilyId { get; set; }
        public string SensorOriginLogicalId { get; set; }
        public decimal SensorHeight { get; set; } = 1.6m;
        public decimal ColliderRadius { get; set; } = 0.35m;
        public decimal ColliderHeight { get; set; } = 1.8m;
        public decimal BoundsWidth { get; set; } = 0.7m;
        public decimal BoundsHeight { get; set; } = 1.8m;
        public decimal BoundsLength { get; set; } = 0.7m;
        public string SourceProviderId { get; set; }
        public bool PreferBundledAnimations { get; set; } = true;
        public bool AllowCrossPackAnimationFallback { get; set; }
        public List<NpcAttackContactRecord> AttackContacts { get; set; } =
            new List<NpcAttackContactRecord>();
        public List<string> RequiredAnimationRoleIds { get; set; } =
            new List<string>();
    }

    [Serializable]
    public sealed class NpcSpeciesRelationshipRule
    {
        public string SubjectSpeciesId { get; set; }
        public string TargetSpeciesId { get; set; }
        public NpcEcologicalDisposition Disposition { get; set; }
        public decimal TriggerDistance { get; set; } = 18m;
        public string ResponseStrategyStableId { get; set; }
    }

    [Serializable]
    public sealed class NpcHabitatRecord
    {
        public string HabitatId { get; set; }
        public string NavigationAgentLogicalId { get; set; }
        public int AreaMask { get; set; } = -1;
        public List<string> AreaTags { get; set; } = new List<string>();
        public decimal TerritoryRadius { get; set; } = 12m;
    }

    [Serializable]
    public sealed class NpcScheduleEntry
    {
        public string EntryId { get; set; }
        public decimal StartHour { get; set; }
        public decimal EndHour { get; set; } = 24m;
        public string StrategyStableId { get; set; }
    }

    [Serializable]
    public sealed class NpcNeedProfile
    {
        public decimal InitialHunger { get; set; } = 0.25m;
        public decimal HungerPerHour { get; set; } = 0.05m;
        public decimal InitialFear { get; set; }
        public decimal FearDecayPerSecond { get; set; } = 0.02m;
        public decimal TerritoryCommitment { get; set; } = 0.5m;
    }

    [Serializable]
    public sealed class NpcPopulationBudget
    {
        public int MaximumActive { get; set; } = 4;
        public int MinimumGroupSize { get; set; } = 1;
        public int MaximumGroupSize { get; set; } = 3;
        public decimal RespawnCooldownSeconds { get; set; } = 90m;
        public int EncounterCost { get; set; } = 1;
    }

    [Serializable]
    public sealed class NpcEcosystemProfileRecord
    {
        public NpcHabitatRecord Habitat { get; set; } =
            new NpcHabitatRecord();
        public NpcNeedProfile Needs { get; set; } =
            new NpcNeedProfile();
        public NpcPopulationBudget Population { get; set; } =
            new NpcPopulationBudget();
        public List<NpcSpeciesRelationshipRule> Relationships { get; set; } =
            new List<NpcSpeciesRelationshipRule>();
        public List<NpcScheduleEntry> Schedule { get; set; } =
            new List<NpcScheduleEntry>();
    }

    [Serializable]
    public sealed class NpcEncounterParticipantRecord
    {
        public string SpeciesId { get; set; }
        public string ArchetypeProfileId { get; set; }
        public int MinimumCount { get; set; } = 1;
        public int MaximumCount { get; set; } = 1;
        public int CostPerInstance { get; set; } = 1;
        public string SpawnZoneId { get; set; }
    }

    [Serializable]
    public sealed class NpcDeterministicEncounterRecipe
    {
        public string RecipeId { get; set; }
        public int Seed { get; set; }
        public int TotalBudget { get; set; } = 8;
        public string HabitatId { get; set; }
        public List<NpcEncounterParticipantRecord> Participants { get; set; } =
            new List<NpcEncounterParticipantRecord>();
    }

    [Serializable]
    public sealed class NpcVariationCandidate
    {
        public string CandidateId { get; set; }
        public string ProviderId { get; set; }
        public string SourceLogicalId { get; set; }
        public string Provenance { get; set; }
        public decimal Confidence { get; set; }
        public string RigLogicalId { get; set; }
        public string AvatarLogicalId { get; set; }
        public bool UsesBundledAnimations { get; set; }
        public bool RigCompatible { get; set; }
        public bool AnimationCompatible { get; set; }
        public NpcVariationCandidateDecision Decision { get; set; }
        public bool DoNotSuggestAgain { get; set; }
        public string DecisionNote { get; set; }
        public List<string> CompatibilityTags { get; set; } =
            new List<string>();
    }

    [Serializable]
    public sealed class NpcVariationRequestRecord
    {
        public bool RequestVariations { get; set; }
        public NpcVariationDiscoveryStatus Status { get; set; } =
            NpcVariationDiscoveryStatus.Disabled;
        public string StatusNote { get; set; }
        public List<string> SuppressedCandidateIds { get; set; } =
            new List<string>();
        public List<NpcVariationCandidate> Candidates { get; set; } =
            new List<NpcVariationCandidate>();
    }

    [Serializable]
    public sealed class NpcSpeciesSourceDescriptor
    {
        public string ProviderId { get; set; }
        public string SourceLogicalId { get; set; }
        public string SpeciesId { get; set; }
        public NpcArchetypeClass Archetype { get; set; }
        public string DisplayName { get; set; }
        public string ModelLogicalId { get; set; }
        public string RigLogicalId { get; set; }
        public string AvatarLogicalId { get; set; }
        public string ProjectAddress { get; set; }
        public bool BundledAnimationCompatible { get; set; }
        public List<string> BundledAnimationRoleIds { get; set; } =
            new List<string>();
        public List<string> Diagnostics { get; set; } =
            new List<string>();
    }

    [Serializable]
    public sealed class NpcSpeciesSourceProviderStatus
    {
        public string ProviderId { get; set; }
        public bool Installed { get; set; }
        public string Message { get; set; }
        public List<NpcSpeciesSourceDescriptor> Sources { get; set; } =
            new List<NpcSpeciesSourceDescriptor>();
    }

    public interface INpcSpeciesSourceProvider
    {
        string ProviderId { get; }
        NpcSpeciesSourceProviderStatus Discover();
    }

    public sealed class NpcEncounterPlan
    {
        public List<NpcEncounterParticipantRecord> OrderedParticipants {
            get;
        } = new List<NpcEncounterParticipantRecord>();
        public string DeterministicHash { get; set; }
    }

    public static class NpcEncounterPlanner
    {
        public static NpcEncounterPlan Compile(
            NpcDeterministicEncounterRecipe recipe,
            DiagnosticList diagnostics)
        {
            var plan = new NpcEncounterPlan();
            if (recipe == null ||
                !AuthoringIdentifier.IsValid(recipe.RecipeId) ||
                !AuthoringIdentifier.IsValid(recipe.HabitatId) ||
                recipe.Seed < 0 ||
                recipe.TotalBudget <= 0)
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-ENCOUNTER-DEFINITION",
                    "Encounter ID/habitat/seed/budget is invalid."));
                return null;
            }
            int minimumCost = 0;
            var species = new HashSet<string>(StringComparer.Ordinal);
            foreach (var participant in recipe.Participants ??
                new List<NpcEncounterParticipantRecord>())
            {
                if (participant == null ||
                    !AuthoringIdentifier.IsValid(participant.SpeciesId) ||
                    !AuthoringIdentifier.IsValid(
                        participant.ArchetypeProfileId) ||
                    !AuthoringIdentifier.IsValid(
                        participant.SpawnZoneId) ||
                    !species.Add(participant.SpeciesId) ||
                    participant.MinimumCount < 0 ||
                    participant.MaximumCount <
                        participant.MinimumCount ||
                    participant.CostPerInstance <= 0)
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-ENCOUNTER-PARTICIPANT",
                        "Encounter participant is invalid or duplicated.",
                        participant?.SpeciesId));
                    continue;
                }
                minimumCost += participant.MinimumCount *
                    participant.CostPerInstance;
                plan.OrderedParticipants.Add(participant);
            }
            if (minimumCost > recipe.TotalBudget)
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-ENCOUNTER-BUDGET",
                    "Minimum encounter population exceeds the total budget.",
                    recipe.RecipeId));
            }
            if (diagnostics.HasErrors)
                return null;
            plan.OrderedParticipants.Sort((left, right) =>
                string.CompareOrdinal(
                    left.SpeciesId,
                    right.SpeciesId));
            var map = new SortedDictionary<string, string>(
                StringComparer.Ordinal)
            {
                ["recipe"] = recipe.RecipeId,
                ["habitat"] = recipe.HabitatId,
                ["seed"] = recipe.Seed.ToString(
                    CultureInfo.InvariantCulture),
                ["budget"] = recipe.TotalBudget.ToString(
                    CultureInfo.InvariantCulture)
            };
            for (int index = 0;
                 index < plan.OrderedParticipants.Count;
                 index++)
            {
                var item = plan.OrderedParticipants[index];
                string prefix = "participant." +
                    index.ToString(CultureInfo.InvariantCulture) + ".";
                map[prefix + "species"] = item.SpeciesId;
                map[prefix + "profile"] =
                    item.ArchetypeProfileId;
                map[prefix + "min"] = item.MinimumCount.ToString(
                    CultureInfo.InvariantCulture);
                map[prefix + "max"] = item.MaximumCount.ToString(
                    CultureInfo.InvariantCulture);
                map[prefix + "cost"] = item.CostPerInstance.ToString(
                    CultureInfo.InvariantCulture);
                map[prefix + "zone"] = item.SpawnZoneId;
            }
            plan.DeterministicHash = CanonicalSerialization.Sha256(
                CanonicalSerialization.SerializeMap(map));
            return plan;
        }
    }

    public static class NpcVariationSelector
    {
        public static NpcVariationCandidate Select(
            NpcVariationRequestRecord request,
            string currentProviderId,
            string requiredRigId)
        {
            if (request == null || !request.RequestVariations)
                return null;
            var suppressed = new HashSet<string>(
                request.SuppressedCandidateIds ??
                    new List<string>(),
                StringComparer.Ordinal);
            var compatible = new List<NpcVariationCandidate>();
            foreach (var candidate in request.Candidates ??
                new List<NpcVariationCandidate>())
            {
                if (candidate == null ||
                    suppressed.Contains(candidate.CandidateId) ||
                    candidate.DoNotSuggestAgain ||
                    candidate.Decision ==
                        NpcVariationCandidateDecision.Rejected ||
                    candidate.Decision ==
                        NpcVariationCandidateDecision.Suppressed ||
                    !candidate.RigCompatible ||
                    !candidate.AnimationCompatible ||
                    !string.Equals(
                        candidate.RigLogicalId,
                        requiredRigId,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                compatible.Add(candidate);
            }
            compatible.Sort((left, right) =>
            {
                bool leftBundled = left.UsesBundledAnimations &&
                    left.ProviderId == currentProviderId;
                bool rightBundled = right.UsesBundledAnimations &&
                    right.ProviderId == currentProviderId;
                int bundled = rightBundled.CompareTo(leftBundled);
                if (bundled != 0)
                    return bundled;
                int confidence =
                    right.Confidence.CompareTo(left.Confidence);
                if (confidence != 0)
                    return confidence;
                int provider = string.CompareOrdinal(
                    left.ProviderId,
                    right.ProviderId);
                return provider != 0
                    ? provider
                    : string.CompareOrdinal(
                        left.CandidateId,
                        right.CandidateId);
            });
            return compatible.Count == 0 ? null : compatible[0];
        }
    }

    public static class NpcArchetypeEcosystemValidator
    {
        public static void Validate(
            NpcDefinition definition,
            DiagnosticList diagnostics)
        {
            if (definition?.ArchetypeProfile != null)
                ValidateArchetype(
                    definition,
                    definition.ArchetypeProfile,
                    diagnostics);
            if (definition?.Ecosystem != null)
                ValidateEcosystem(
                    definition,
                    definition.Ecosystem,
                    diagnostics);
            ValidateVariations(
                definition?.VariationRequest,
                definition?.ArchetypeProfile,
                diagnostics);
        }

        static void ValidateArchetype(
            NpcDefinition definition,
            NpcArchetypeProfileRecord profile,
            DiagnosticList diagnostics)
        {
            bool identifiers =
                AuthoringIdentifier.IsValid(profile.ProfileId) &&
                AuthoringIdentifier.IsValid(profile.SpeciesId) &&
                AuthoringIdentifier.IsValid(profile.RigLogicalId) &&
                AuthoringIdentifier.IsValid(profile.AvatarLogicalId) &&
                AuthoringIdentifier.IsValid(
                    profile.NavigationAgentLogicalId) &&
                AuthoringIdentifier.IsValid(
                    profile.LocomotionFamilyId) &&
                AuthoringIdentifier.IsValid(
                    profile.SensorOriginLogicalId) &&
                AuthoringIdentifier.IsValid(
                    profile.SourceProviderId);
            if (!identifiers ||
                profile.SensorHeight <= 0 ||
                profile.ColliderRadius <= 0 ||
                profile.ColliderHeight <= 0 ||
                profile.BoundsWidth <= 0 ||
                profile.BoundsHeight <= 0 ||
                profile.BoundsLength <= 0)
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-ARCHETYPE-PROFILE",
                    "Archetype IDs, sensor, collider, and bounds must be valid.",
                    profile.ProfileId));
            }
            if (!string.Equals(
                    profile.NavigationAgentLogicalId,
                    definition.Navigation?.AgentLogicalId,
                    StringComparison.Ordinal))
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-ARCHETYPE-NAVIGATION",
                    "Archetype navigation agent must match the NPC navigation selection.",
                    profile.ProfileId));
            }
            bool humanoid =
                profile.Archetype == NpcArchetypeClass.Humanoid;
            if (definition.CharacterSource != null &&
                definition.CharacterSource.RequireHumanoidAvatar !=
                    humanoid)
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-ARCHETYPE-AVATAR",
                    "Humanoid avatar requirement does not match the archetype.",
                    profile.ProfileId));
            }
            var roles = new HashSet<string>(StringComparer.Ordinal);
            foreach (var role in definition.AnimationRequirements ??
                new List<NpcAnimationRoleRequirement>())
            {
                if (role != null)
                    roles.Add(role.RoleId);
            }
            foreach (var requiredRole in
                profile.RequiredAnimationRoleIds ??
                    new List<string>())
            {
                if (!roles.Contains(requiredRole))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-ARCHETYPE-ANIMATION-ROLE",
                        "Archetype-required animation role is missing.",
                        requiredRole));
                }
            }
            foreach (var contact in profile.AttackContacts ??
                new List<NpcAttackContactRecord>())
            {
                if (contact == null ||
                    !AuthoringIdentifier.IsValid(contact.ContactId) ||
                    !AuthoringIdentifier.IsValid(
                        contact.TransformLogicalId) ||
                    !AuthoringIdentifier.IsValid(
                        contact.AnimationEventSemantic) ||
                    contact.Radius <= 0)
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-ARCHETYPE-ATTACK-CONTACT",
                        "Attack contact transform/event/radius is invalid.",
                        contact?.ContactId));
                }
            }
            if (profile.AllowCrossPackAnimationFallback)
            {
                diagnostics.Add(AuthoringDiagnostic.Warning(
                    "ACA-NPC-ARCHETYPE-CROSS-PACK-ANIMATION",
                    "Cross-pack animation fallback requires explicit rig/avatar compatibility evidence.",
                    profile.ProfileId));
            }
        }

        static void ValidateEcosystem(
            NpcDefinition definition,
            NpcEcosystemProfileRecord ecosystem,
            DiagnosticList diagnostics)
        {
            var habitat = ecosystem.Habitat;
            if (habitat == null ||
                !AuthoringIdentifier.IsValid(habitat.HabitatId) ||
                !AuthoringIdentifier.IsValid(
                    habitat.NavigationAgentLogicalId) ||
                habitat.TerritoryRadius <= 0 ||
                !string.Equals(
                    habitat.NavigationAgentLogicalId,
                    definition.Navigation?.AgentLogicalId,
                    StringComparison.Ordinal))
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-HABITAT",
                    "Habitat ID/navigation/territory is invalid.",
                    habitat?.HabitatId));
            }
            var needs = ecosystem.Needs;
            if (needs == null ||
                !Unit(needs.InitialHunger) ||
                needs.HungerPerHour < 0 ||
                !Unit(needs.InitialFear) ||
                needs.FearDecayPerSecond < 0 ||
                !Unit(needs.TerritoryCommitment))
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-ECOSYSTEM-NEEDS",
                    "Hunger, fear, and territory values are invalid.",
                    definition.StableId));
            }
            var population = ecosystem.Population;
            if (population == null ||
                population.MaximumActive <= 0 ||
                population.MinimumGroupSize <= 0 ||
                population.MaximumGroupSize <
                    population.MinimumGroupSize ||
                population.MaximumGroupSize >
                    population.MaximumActive ||
                population.RespawnCooldownSeconds < 0 ||
                population.EncounterCost <= 0)
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-POPULATION-BUDGET",
                    "Population/spawn budget is invalid.",
                    definition.StableId));
            }
            foreach (var relationship in ecosystem.Relationships ??
                new List<NpcSpeciesRelationshipRule>())
            {
                if (relationship == null ||
                    !AuthoringIdentifier.IsValid(
                        relationship.SubjectSpeciesId) ||
                    !AuthoringIdentifier.IsValid(
                        relationship.TargetSpeciesId) ||
                    !AuthoringIdentifier.IsValid(
                        relationship.ResponseStrategyStableId) ||
                    relationship.TriggerDistance <= 0)
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-SPECIES-RELATIONSHIP",
                        "Species relationship rule is invalid.",
                        relationship?.SubjectSpeciesId));
                }
            }
            foreach (var entry in ecosystem.Schedule ??
                new List<NpcScheduleEntry>())
            {
                if (entry == null ||
                    !AuthoringIdentifier.IsValid(entry.EntryId) ||
                    !AuthoringIdentifier.IsValid(
                        entry.StrategyStableId) ||
                    entry.StartHour < 0 ||
                    entry.StartHour >= 24 ||
                    entry.EndHour <= 0 ||
                    entry.EndHour > 24 ||
                    entry.StartHour == entry.EndHour)
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-SCHEDULE",
                        "Schedule entry ID/time/strategy is invalid.",
                        entry?.EntryId));
                }
            }
        }

        static void ValidateVariations(
            NpcVariationRequestRecord request,
            NpcArchetypeProfileRecord archetype,
            DiagnosticList diagnostics)
        {
            if (request == null)
                return;
            if (request.RequestVariations &&
                request.Status ==
                    NpcVariationDiscoveryStatus.Disabled)
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-VARIATION-STATUS",
                    "Enabled variation discovery cannot remain Disabled."));
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var suppressed = new HashSet<string>(
                request.SuppressedCandidateIds ??
                    new List<string>(),
                StringComparer.Ordinal);
            foreach (var candidate in request.Candidates ??
                new List<NpcVariationCandidate>())
            {
                bool accepted = candidate != null &&
                    candidate.Decision ==
                        NpcVariationCandidateDecision.Accepted;
                if (candidate == null ||
                    !AuthoringIdentifier.IsValid(
                        candidate.CandidateId) ||
                    !AuthoringIdentifier.IsValid(
                        candidate.ProviderId) ||
                    !AuthoringIdentifier.IsValid(
                        candidate.SourceLogicalId) ||
                    !AuthoringIdentifier.IsValid(
                        candidate.RigLogicalId) ||
                    !ids.Add(candidate.CandidateId) ||
                    candidate.Confidence < 0 ||
                    candidate.Confidence > 1 ||
                    (accepted &&
                     (!candidate.RigCompatible ||
                      !candidate.AnimationCompatible)))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-VARIATION-CANDIDATE",
                        "Variation candidate provenance/confidence/compatibility is invalid.",
                        candidate?.CandidateId));
                }
                if (candidate != null &&
                    (candidate.DoNotSuggestAgain ||
                     candidate.Decision ==
                         NpcVariationCandidateDecision.Suppressed) &&
                    !suppressed.Contains(candidate.CandidateId))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-VARIATION-SUPPRESSION",
                        "Suppressed candidate must be persisted in the per-NPC suppression list.",
                        candidate.CandidateId));
                }
                if (accepted &&
                    archetype != null &&
                    !string.Equals(
                        candidate.RigLogicalId,
                        archetype.RigLogicalId,
                        StringComparison.Ordinal))
                {
                    diagnostics.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-VARIATION-RIG-MISMATCH",
                        "Accepted variation candidate does not match the archetype rig.",
                        candidate.CandidateId));
                }
            }
            if (request.RequestVariations &&
                request.Status ==
                    NpcVariationDiscoveryStatus.NoCompatibleVariant &&
                string.IsNullOrWhiteSpace(request.StatusNote))
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-VARIATION-NO-MATCH-NOTE",
                    "No-compatible-variant status requires a note."));
            }
        }

        static bool Unit(decimal value)
        {
            return value >= 0 && value <= 1;
        }
    }

    public static class NpcEcosystemSampleCatalog
    {
        public static NpcArchetypeProfileRecord Deer()
        {
            return Quadruped(
                "npc.archetype.deer",
                "species.deer",
                "provider.nwb.species.animal-pack-deluxe",
                1.3m,
                1.4m,
                1.8m,
                "npc.animation.deer.idle",
                "npc.animation.deer.walk",
                "npc.animation.deer.run",
                "npc.animation.deer.death");
        }

        public static NpcArchetypeProfileRecord Wolf()
        {
            var profile = Quadruped(
                "npc.archetype.wolf",
                "species.wolf",
                "provider.nwb.species.animal-pack-deluxe",
                1.1m,
                1.2m,
                1.9m,
                "npc.animation.wolf.idle",
                "npc.animation.wolf.walk",
                "npc.animation.wolf.run",
                "npc.animation.wolf.attack",
                "npc.animation.wolf.death");
            profile.AttackContacts.Add(new NpcAttackContactRecord
            {
                ContactId = "npc.contact.wolf.bite",
                TransformLogicalId = "npc.transform.wolf.jaw",
                Radius = 0.18m,
                AnimationEventSemantic =
                    "npc.event.damage-window"
            });
            return profile;
        }

        public static NpcDeterministicEncounterRecipe ForestEdge()
        {
            return new NpcDeterministicEncounterRecipe
            {
                RecipeId = "npc.encounter.forest-edge",
                HabitatId = "npc.habitat.forest-edge",
                Seed = 4721,
                TotalBudget = 8,
                Participants = new List<NpcEncounterParticipantRecord>
                {
                    new NpcEncounterParticipantRecord
                    {
                        SpeciesId = "species.deer",
                        ArchetypeProfileId =
                            "npc.archetype.deer",
                        MinimumCount = 2,
                        MaximumCount = 3,
                        CostPerInstance = 1,
                        SpawnZoneId = "npc.zone.prey-meadow"
                    },
                    new NpcEncounterParticipantRecord
                    {
                        SpeciesId = "species.wolf",
                        ArchetypeProfileId =
                            "npc.archetype.wolf",
                        MinimumCount = 1,
                        MaximumCount = 1,
                        CostPerInstance = 3,
                        SpawnZoneId =
                            "npc.zone.predator-territory"
                    }
                }
            };
        }

        static NpcArchetypeProfileRecord Quadruped(
            string profileId,
            string speciesId,
            string providerId,
            decimal sensorHeight,
            decimal boundsHeight,
            decimal boundsLength,
            params string[] roles)
        {
            return new NpcArchetypeProfileRecord
            {
                ProfileId = profileId,
                Archetype = NpcArchetypeClass.Quadruped,
                SpeciesId = speciesId,
                RigLogicalId = "rig.quadruped." +
                    speciesId.Substring(
                        speciesId.LastIndexOf('.') + 1),
                AvatarLogicalId = "avatar." + speciesId,
                NavigationAgentLogicalId =
                    "nwb.nav-agent.quadruped",
                LocomotionFamilyId =
                    "npc.locomotion.quadruped.ground",
                SensorOriginLogicalId =
                    "npc.sensor-origin.head",
                SensorHeight = sensorHeight,
                ColliderRadius = 0.45m,
                ColliderHeight = boundsHeight,
                BoundsWidth = 0.9m,
                BoundsHeight = boundsHeight,
                BoundsLength = boundsLength,
                SourceProviderId = providerId,
                PreferBundledAnimations = true,
                AllowCrossPackAnimationFallback = false,
                RequiredAnimationRoleIds =
                    new List<string>(roles)
            };
        }
    }
}
