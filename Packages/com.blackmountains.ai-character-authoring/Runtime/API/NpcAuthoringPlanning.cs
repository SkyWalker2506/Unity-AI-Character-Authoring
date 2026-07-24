using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlackMountains.AICharacterAuthoring
{
    public sealed class NpcValidationContext
    {
        public Dictionary<string, bool> Integrations { get; } =
            new Dictionary<string, bool>(StringComparer.Ordinal);
        public Dictionary<string, int> NavigationAgents { get; } =
            new Dictionary<string, int>(StringComparer.Ordinal);
        public HashSet<string> AvailableBehaviorTaskTypes { get; } =
            new HashSet<string>(StringComparer.Ordinal);
        public NpcBehaviorLegoCatalog LegoCatalog { get; set; } =
            NpcBuiltInBehaviorLegoCatalog.Create();
        public List<NpcResourceCatalogEntry> AvailableResources { get; } =
            new List<NpcResourceCatalogEntry>();
    }

    public static class NpcDefinitionResourceCatalog
    {
        public static List<NpcResourceCatalogEntry> Build(
            NpcDefinition definition)
        {
            var resources = new List<NpcResourceCatalogEntry>();
            if (definition == null)
                return resources;

            Add(
                resources,
                definition.Navigation?.AgentLogicalId,
                NpcResourceKind.Navigation,
                "navigation.agent");
            foreach (var capability in definition.Capabilities ??
                new List<NpcCapabilityRecipe>())
            {
                if (capability == null)
                    continue;
                if (capability.CapabilityId ==
                        NpcCapabilityIds.SenseVisibility ||
                    capability.CapabilityId ==
                        NpcCapabilityIds.SenseSound ||
                    capability.CapabilityId ==
                        NpcCapabilityIds.SenseDistance)
                {
                    Add(
                        resources,
                        capability.CapabilityId,
                        NpcResourceKind.Sensor,
                        "sensor");
                }
            }
            foreach (var animation in definition.AnimationRequirements ??
                new List<NpcAnimationRoleRequirement>())
            {
                Add(
                    resources,
                    animation?.RoleId,
                    NpcResourceKind.AnimationRole,
                    "animation.role");
            }
            foreach (var line in definition.DialogueLines ??
                new List<NpcDialogueLineRecord>())
            {
                Add(
                    resources,
                    line?.SubtitleLocalizationKey,
                    NpcResourceKind.LocalizedText,
                    "localized.dialogue");
                Add(
                    resources,
                    line?.OptionalAudioClipLogicalId,
                    NpcResourceKind.Audio,
                    "audio.dialogue");
            }
            bool hasCombat = false;
            foreach (var capability in definition.Capabilities ??
                new List<NpcCapabilityRecipe>())
            {
                if (capability != null &&
                    (capability.CapabilityId ==
                         NpcCapabilityIds.CombatMelee ||
                     capability.CapabilityId ==
                         "npc.combat.ranged"))
                {
                    hasCombat = true;
                    break;
                }
            }
            if (hasCombat)
            {
                Add(
                    resources,
                    "npc.event.damage-window",
                    NpcResourceKind.AnimationEventMarker,
                    "animation.event.damage");
            }
            if (definition.WeaponLoadout != null &&
                definition.WeaponLoadout.IntegrationChoice !=
                    NpcWeaponIntegrationChoice.Disabled)
            {
                Add(
                    resources,
                    definition.WeaponLoadout.EquipmentItemStableId,
                    NpcResourceKind.EquipmentItem,
                    "equipment.weapon",
                    "equipment.primary");
                Add(
                    resources,
                    definition.WeaponLoadout.HolsterSocketLogicalId,
                    NpcResourceKind.EquipmentSlot,
                    "equipment.slot.holster");
            }
            return resources;
        }

        static void Add(
            ICollection<NpcResourceCatalogEntry> resources,
            string stableId,
            NpcResourceKind kind,
            params string[] tags)
        {
            if (string.IsNullOrWhiteSpace(stableId))
                return;
            resources.Add(new NpcResourceCatalogEntry
            {
                StableId = stableId,
                Kind = kind,
                Tags = new List<string>(tags ?? new string[0])
            });
        }
    }

    public sealed class NpcDefinitionValidationResult
    {
        public DiagnosticList Diagnostics { get; } = new DiagnosticList();
        public bool Success => !Diagnostics.HasErrors;
    }

    public sealed class NpcAuthoringPlan
    {
        public const string CurrentSchemaVersion = "1";

        public string SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string NpcStableId { get; set; }
        public string DefinitionVersion { get; set; }
        public string BehaviorRecipeId { get; set; }
        public string CharacterSourceProviderId { get; set; }
        public string CharacterSourceLogicalId { get; set; }
        public List<string> AppearanceItemIds { get; set; } = new List<string>();
        public NpcWeaponIntegrationChoice WeaponIntegrationChoice { get; set; }
        public string WeaponLoadoutId { get; set; }
        public NpcWeaponHolsterPolicy WeaponHolsterPolicy { get; set; }
        public int ResolvedAgentTypeId { get; set; }
        public List<string> OrderedCapabilityIds { get; set; } = new List<string>();
        public List<string> RequiredIntegrations { get; set; } = new List<string>();
        public List<NpcBehaviorTaskRequirement> RequiredBehaviorTasks { get; set; } =
            new List<NpcBehaviorTaskRequirement>();
        public List<NpcAnimationRoleRequirement> AnimationRequirements { get; set; } =
            new List<NpcAnimationRoleRequirement>();
        public List<NpcResolvedStrategy> ResolvedStrategies { get; set; } =
            new List<NpcResolvedStrategy>();
        public List<NpcPriorityStateRecord> PriorityStates { get; set; } =
            new List<NpcPriorityStateRecord>();
        public List<NpcResolvedResourceBinding> ResourceManifest { get; set; } =
            new List<NpcResolvedResourceBinding>();
        public string DeterministicHash { get; set; }
    }

    public sealed class NpcPlanCompilationResult
    {
        public NpcAuthoringPlan Plan { get; set; }
        public DiagnosticList Diagnostics { get; } = new DiagnosticList();
        public bool Success => !Diagnostics.HasErrors && Plan != null;
    }

    public static class NpcDefinitionValidator
    {
        public static NpcDefinitionValidationResult Validate(
            NpcDefinition definition,
            NpcBehaviorRecipe behaviorRecipe,
            NpcValidationContext context)
        {
            var result = new NpcDefinitionValidationResult();
            context = context ?? new NpcValidationContext();

            if (definition == null)
            {
                result.Diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-DEFINITION-NULL",
                    "NpcDefinition is required."));
                return result;
            }

            if (definition.SchemaVersion != NpcDefinition.CurrentSchemaVersion)
                Error(result, "ACA-NPC-SCHEMA", "Unsupported NPC definition schema.", "schemaVersion");
            if (!AuthoringIdentifier.IsValid(definition.StableId))
                Error(result, "ACA-NPC-ID", "NPC stable ID is invalid.", "stableId");
            RequireText(result, definition.DisplayNameLocalizationKey, "displayNameLocalizationKey");
            RequireText(result, definition.EnglishDisplayNameFallback, "englishDisplayNameFallback");
            RequireId(result, definition.FactionId, "factionId");
            RequireId(result, definition.Navigation?.AgentLogicalId, "navigation.agentLogicalId");

            ValidateCharacterSource(definition, result);
            ValidateAppearance(definition, result);
            ValidateCapabilities(definition, result);
            ValidateIntegrations(definition, context, result);
            ValidateNavigation(definition, context, result);
            ValidateWeapon(definition, result);
            ValidateActions(definition, result);
            ValidateDialogue(definition, result);
            ValidateCommerce(definition, result);
            ValidateAnimations(definition, result);
            ValidateRoleRequirements(definition, result);
            NpcArchetypeEcosystemValidator.Validate(
                definition,
                result.Diagnostics);
            ValidateBehavior(definition, behaviorRecipe, context, result);

            return result;
        }

        static void ValidateAppearance(
            NpcDefinition definition,
            NpcDefinitionValidationResult result)
        {
            var appearance = definition.Appearance;
            if (appearance == null)
            {
                Error(result, "ACA-NPC-APPEARANCE", "Appearance loadout is required.", definition.StableId);
                return;
            }

            RequireId(
                result,
                appearance.DefaultCharacterSourceLogicalId,
                "appearance.defaultCharacterSourceLogicalId");
            if (definition.CharacterSource != null &&
                !string.Equals(
                    appearance.DefaultCharacterSourceLogicalId,
                    definition.CharacterSource.SourceLogicalId,
                    StringComparison.Ordinal))
            {
                Error(result, "ACA-NPC-APPEARANCE-SOURCE-DRIFT", "Appearance default source does not match the character source selection.", definition.StableId);
            }

            var slots = new HashSet<NpcEquipmentSlot>();
            foreach (var part in appearance.Parts ?? new List<NpcAppearancePartSelection>())
            {
                if (part == null || !AuthoringIdentifier.IsValid(part.ItemStableId))
                {
                    Error(result, "ACA-NPC-APPEARANCE-ITEM", "Appearance item stable ID is invalid.", definition.StableId);
                    continue;
                }
                if (!slots.Add(part.Slot))
                    Error(result, "ACA-NPC-APPEARANCE-SLOT", "Appearance loadout contains more than one item for a slot.", part.Slot.ToString());
            }
        }

        static void ValidateCharacterSource(
            NpcDefinition definition,
            NpcDefinitionValidationResult result)
        {
            var source = definition.CharacterSource;
            if (source == null)
            {
                Error(result, "ACA-NPC-CHARACTER-SOURCE", "Character source selection is required.", definition.StableId);
                return;
            }

            RequireId(result, source.ProviderId, "characterSource.providerId");
            RequireId(result, source.SourceLogicalId, "characterSource.sourceLogicalId");
            RequireId(result, source.ModelLogicalId, "characterSource.modelLogicalId");
            if (source.RequireHumanoidAvatar)
                RequireId(result, source.AvatarLogicalId, "characterSource.avatarLogicalId");
        }

        static void ValidateCapabilities(NpcDefinition definition, NpcDefinitionValidationResult result)
        {
            var capabilities = new Dictionary<string, NpcCapabilityRecipe>(StringComparer.Ordinal);
            foreach (var recipe in definition.Capabilities ?? new List<NpcCapabilityRecipe>())
            {
                if (recipe == null || !AuthoringIdentifier.IsValid(recipe.CapabilityId))
                {
                    Error(result, "ACA-NPC-CAPABILITY-ID", "Capability ID is invalid.", definition.StableId);
                    continue;
                }

                if (capabilities.ContainsKey(recipe.CapabilityId))
                    Error(result, "ACA-NPC-CAPABILITY-DUPLICATE", "Capability is duplicated.", recipe.CapabilityId);
                else
                    capabilities.Add(recipe.CapabilityId, recipe);
            }

            foreach (var pair in capabilities)
            {
                foreach (var required in pair.Value.Requires ?? new List<string>())
                {
                    if (!capabilities.ContainsKey(required))
                        Error(result, "ACA-NPC-CAPABILITY-REQUIRED", pair.Key + " requires " + required + ".", pair.Key);
                }

                foreach (var conflict in pair.Value.Conflicts ?? new List<string>())
                {
                    if (capabilities.ContainsKey(conflict))
                        Error(result, "ACA-NPC-CAPABILITY-CONFLICT", pair.Key + " conflicts with " + conflict + ".", pair.Key);
                }
            }
        }

        static void ValidateIntegrations(
            NpcDefinition definition,
            NpcValidationContext context,
            NpcDefinitionValidationResult result)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var integration in definition.Integrations ?? new List<NpcIntegrationRequest>())
            {
                if (integration == null || !AuthoringIdentifier.IsValid(integration.IntegrationId))
                {
                    Error(result, "ACA-NPC-INTEGRATION-ID", "Integration ID is invalid.", definition.StableId);
                    continue;
                }

                if (!seen.Add(integration.IntegrationId))
                    Error(result, "ACA-NPC-INTEGRATION-DUPLICATE", "Integration is duplicated.", integration.IntegrationId);

                bool available = context.Integrations.TryGetValue(integration.IntegrationId, out var value) && value;
                if (!available && integration.Mode == NpcIntegrationMode.Required)
                    Error(result, "ACA-NPC-INTEGRATION-MISSING", "Required integration is unavailable.", integration.IntegrationId);
                else if (!available && integration.Mode == NpcIntegrationMode.Optional)
                    result.Diagnostics.Add(AuthoringDiagnostic.Warning(
                        "ACA-NPC-INTEGRATION-OPTIONAL-MISSING",
                        "Optional integration is unavailable.",
                        integration.IntegrationId));
            }
        }

        static void ValidateNavigation(
            NpcDefinition definition,
            NpcValidationContext context,
            NpcDefinitionValidationResult result)
        {
            string logicalId = definition.Navigation?.AgentLogicalId;
            if (string.IsNullOrWhiteSpace(logicalId))
                return;

            if (!context.NavigationAgents.TryGetValue(logicalId, out var resolved))
            {
                Error(result, "ACA-NPC-NAVIGATION-UNRESOLVED", "Navigation agent logical ID is not resolved.", logicalId);
                return;
            }

            if (definition.Navigation.ResolvedProjectAgentTypeId.HasValue &&
                definition.Navigation.ResolvedProjectAgentTypeId.Value != resolved)
            {
                Error(result, "ACA-NPC-NAVIGATION-DRIFT", "Serialized project agent type does not match project resolution.", logicalId);
            }
        }

        static void ValidateWeapon(
            NpcDefinition definition,
            NpcDefinitionValidationResult result)
        {
            var loadout = definition.WeaponLoadout;
            if (loadout == null)
                return;

            if (!Enum.IsDefined(typeof(NpcWeaponIntegrationChoice), loadout.IntegrationChoice))
            {
                Error(result, "ACA-NPC-WEAPON-INTEGRATION-CHOICE", "Weapon integration choice is invalid.", definition.StableId);
                return;
            }

            if (loadout.IntegrationChoice == NpcWeaponIntegrationChoice.Disabled)
                return;

            RequireId(result, loadout.LoadoutId, "weaponLoadout.loadoutId");
            RequireId(result, loadout.WeaponItemLogicalId, "weaponLoadout.weaponItemLogicalId");
            RequireId(result, loadout.EquipmentItemStableId, "weaponLoadout.equipmentItemStableId");
            RequireId(result, loadout.HolsterSocketLogicalId, "weaponLoadout.holsterSocketLogicalId");
            RequireId(result, loadout.EquipAnimationRoleId, "weaponLoadout.equipAnimationRoleId");
            RequireId(result, loadout.DrawAnimationRoleId, "weaponLoadout.drawAnimationRoleId");
            RequireId(result, loadout.AttackAnimationRoleId, "weaponLoadout.attackAnimationRoleId");
            RequireId(result, loadout.StowAnimationRoleId, "weaponLoadout.stowAnimationRoleId");

            if (!Enum.IsDefined(typeof(NpcWeaponHolsterPolicy), loadout.HolsterPolicy))
                Error(result, "ACA-NPC-WEAPON-HOLSTER-POLICY", "Weapon holster policy is invalid.", loadout.LoadoutId);

            var capabilities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var capability in definition.Capabilities ?? new List<NpcCapabilityRecipe>())
                if (capability != null) capabilities.Add(capability.CapabilityId);
            RequireCapability(capabilities, NpcCapabilityIds.WeaponLoadout, definition, result);
            RequireCapability(capabilities, NpcCapabilityIds.WeaponEquip, definition, result);
            RequireCapability(capabilities, NpcCapabilityIds.WeaponStow, definition, result);
            RequireCapability(capabilities, NpcCapabilityIds.WeaponDraw, definition, result);
            RequireCapability(capabilities, NpcCapabilityIds.WeaponAttack, definition, result);

            bool requiredStp = false;
            foreach (var integration in definition.Integrations ?? new List<NpcIntegrationRequest>())
            {
                if (integration != null &&
                    integration.IntegrationId == NpcIntegrationIds.StpWeaponIntegration &&
                    integration.Mode == NpcIntegrationMode.Required)
                {
                    requiredStp = true;
                    break;
                }
            }

            if (!requiredStp)
                Error(result, "ACA-NPC-WEAPON-STP-REQUIRED", "STP weapon choice requires the STP weapon integration.", loadout.LoadoutId);
            if (definition.CharacterSource == null || !definition.CharacterSource.RequireEquipmentCompatibility)
                Error(result, "ACA-NPC-WEAPON-SOURCE-COMPATIBILITY", "Weapon integration requires an equipment-compatible character source.", loadout.LoadoutId);
        }

        static void ValidateActions(NpcDefinition definition, NpcDefinitionValidationResult result)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var action in definition.InteractionActions ?? new List<NpcInteractionActionRecord>())
            {
                if (action == null || !AuthoringIdentifier.IsValid(action.ActionId))
                    Error(result, "ACA-NPC-ACTION-ID", "Interaction action ID is invalid.", definition.StableId);
                else if (!ids.Add(action.ActionId))
                    Error(result, "ACA-NPC-ACTION-DUPLICATE", "Interaction action is duplicated.", action.ActionId);
                if (action != null && action.Range <= 0)
                    Error(result, "ACA-NPC-ACTION-RANGE", "Interaction range must be positive.", action.ActionId);
            }

            var bindings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var input in definition.InputActions ?? new List<NpcInputActionRecord>())
            {
                if (input == null || !AuthoringIdentifier.IsValid(input.ActionId))
                {
                    Error(result, "ACA-NPC-INPUT-ID", "Input action ID is invalid.", definition.StableId);
                    continue;
                }

                CheckBinding(input.ContextId, input.KeyboardDefaultPath, input.ActionId, bindings, result);
                CheckBinding(input.ContextId, input.GamepadDefaultPath, input.ActionId, bindings, result);
            }
        }

        static void CheckBinding(
            string contextId,
            string path,
            string actionId,
            ISet<string> bindings,
            NpcDefinitionValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            string key = (contextId ?? "world") + "|" + path;
            if (!bindings.Add(key))
                Error(result, "ACA-NPC-INPUT-CONFLICT", "Default binding conflicts in the same context.", actionId);
        }

        static void ValidateDialogue(NpcDefinition definition, NpcDefinitionValidationResult result)
        {
            var lineIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in definition.DialogueLines ?? new List<NpcDialogueLineRecord>())
            {
                if (line == null || !AuthoringIdentifier.IsValid(line.LineId))
                {
                    Error(result, "ACA-NPC-DIALOGUE-ID", "Dialogue line ID is invalid.", definition.StableId);
                    continue;
                }

                if (!lineIds.Add(line.LineId))
                    Error(result, "ACA-NPC-DIALOGUE-DUPLICATE", "Dialogue line is duplicated.", line.LineId);
                RequireId(result, line.SubtitleLocalizationKey, line.LineId + ".subtitleLocalizationKey");
                RequireText(result, line.EnglishSourceFallback, line.LineId + ".englishSourceFallback");
                if (!string.IsNullOrWhiteSpace(line.OptionalAudioClipLogicalId) &&
                    string.IsNullOrWhiteSpace(line.VoiceLocalizationKey))
                {
                    Error(result, "ACA-NPC-VOICE-KEY", "An optional audio clip requires a stable voice localization key.", line.LineId);
                }
            }
        }

        static void ValidateCommerce(NpcDefinition definition, NpcDefinitionValidationResult result)
        {
            var offers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var offer in definition.CommerceOffers ?? new List<NpcCommerceOfferRecord>())
            {
                if (offer == null || !AuthoringIdentifier.IsValid(offer.OfferId))
                {
                    Error(result, "ACA-NPC-OFFER-ID", "Commerce offer ID is invalid.", definition.StableId);
                    continue;
                }

                if (!offers.Add(offer.OfferId))
                    Error(result, "ACA-NPC-OFFER-DUPLICATE", "Commerce offer is duplicated.", offer.OfferId);
                RequireId(result, offer.ItemLogicalId, offer.OfferId + ".itemLogicalId");
                RequireId(result, offer.CurrencyItemLogicalId, offer.OfferId + ".currencyItemLogicalId");
                if (offer.QuantityPerPurchase <= 0 || offer.InitialStock < 0 || offer.Price <= 0)
                    Error(result, "ACA-NPC-OFFER-QUANTITY", "Offer quantities and price are invalid.", offer.OfferId);
                if (string.Equals(offer.ItemLogicalId, offer.CurrencyItemLogicalId, StringComparison.Ordinal))
                    Error(result, "ACA-NPC-OFFER-SAME-ITEM", "Offer item and currency item must differ.", offer.OfferId);
            }
        }

        static void ValidateAnimations(NpcDefinition definition, NpcDefinitionValidationResult result)
        {
            var roles = new HashSet<string>(StringComparer.Ordinal);
            foreach (var requirement in definition.AnimationRequirements ?? new List<NpcAnimationRoleRequirement>())
            {
                if (requirement == null || !AuthoringIdentifier.IsValid(requirement.RoleId))
                {
                    Error(result, "ACA-NPC-ANIMATION-ROLE", "Animation role ID is invalid.", definition.StableId);
                    continue;
                }

                if (!roles.Add(requirement.RoleId))
                    Error(result, "ACA-NPC-ANIMATION-DUPLICATE", "Animation role is duplicated.", requirement.RoleId);
                if (string.IsNullOrWhiteSpace(requirement.MotionId) &&
                    (requirement.RequiredTags == null || requirement.RequiredTags.Count == 0))
                {
                    Error(result, "ACA-NPC-ANIMATION-QUERY", "Animation role requires a motion ID or semantic tags.", requirement.RoleId);
                }
            }
        }

        static void ValidateRoleRequirements(NpcDefinition definition, NpcDefinitionValidationResult result)
        {
            var capabilities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var capability in definition.Capabilities ?? new List<NpcCapabilityRecipe>())
                if (capability != null) capabilities.Add(capability.CapabilityId);

            switch (definition.Role)
            {
                case NpcRole.Vendor:
                    RequireCapability(capabilities, NpcCapabilityIds.Interaction, definition, result);
                    RequireCapability(capabilities, NpcCapabilityIds.Dialogue, definition, result);
                    RequireCapability(capabilities, NpcCapabilityIds.Commerce, definition, result);
                    if (definition.CommerceOffers == null || definition.CommerceOffers.Count == 0)
                        Error(result, "ACA-NPC-VENDOR-OFFERS", "Vendor requires at least one commerce offer.", definition.StableId);
                    break;
                case NpcRole.Bandit:
                    RequireCapability(capabilities, NpcCapabilityIds.Patrol, definition, result);
                    RequireCapability(capabilities, NpcCapabilityIds.Pursue, definition, result);
                    RequireCapability(capabilities, NpcCapabilityIds.SenseVisibility, definition, result);
                    RequireCapability(capabilities, NpcCapabilityIds.CombatMelee, definition, result);
                    break;
                case NpcRole.Companion:
                    RequireCapability(capabilities, NpcCapabilityIds.CompanionFollow, definition, result);
                    RequireCapability(capabilities, NpcCapabilityIds.CompanionDefend, definition, result);
                    RequireCapability(capabilities, NpcCapabilityIds.Dialogue, definition, result);
                    if (definition.Companion == null)
                        Error(result, "ACA-NPC-COMPANION-PROFILE", "Companion record is required.", definition.StableId);
                    break;
            }
        }

        static void ValidateBehavior(
            NpcDefinition definition,
            NpcBehaviorRecipe recipe,
            NpcValidationContext context,
            NpcDefinitionValidationResult result)
        {
            if (recipe == null || !string.Equals(recipe.RecipeId, definition.BehaviorRecipeId, StringComparison.Ordinal))
            {
                Error(result, "ACA-NPC-BEHAVIOR-RECIPE", "Behavior recipe is missing or does not match the definition.", definition.BehaviorRecipeId);
                return;
            }

            foreach (var task in recipe.Tasks ?? new List<NpcBehaviorTaskRequirement>())
            {
                if (!context.AvailableBehaviorTaskTypes.Contains(task.AssemblyQualifiedTaskType))
                    Error(result, "ACA-NPC-BEHAVIOR-TASK-MISSING", "Required Behavior Designer task type is unavailable.", task.AssemblyQualifiedTaskType);
            }

            var strategyResolution = NpcStrategyResolver.Resolve(
                context.LegoCatalog ??
                    NpcBuiltInBehaviorLegoCatalog.Create(),
                recipe.Strategies,
                context.AvailableResources);
            result.Diagnostics.AddRange(
                strategyResolution.Diagnostics.Items);
            ValidatePriorityStates(
                recipe,
                strategyResolution,
                result);
        }

        static void ValidatePriorityStates(
            NpcBehaviorRecipe recipe,
            NpcStrategyResolutionResult strategyResolution,
            NpcDefinitionValidationResult result)
        {
            if (recipe.Strategies == null || recipe.Strategies.Count == 0)
            {
                Error(
                    result,
                    "ACA-NPC-STRATEGY-SELECTION-EMPTY",
                    "Behavior recipe must select at least one exact strategy variant.",
                    recipe.RecipeId);
            }
            if (recipe.PriorityStates == null ||
                recipe.PriorityStates.Count == 0)
            {
                Error(
                    result,
                    "ACA-NPC-PRIORITY-STATE-EMPTY",
                    "Behavior recipe must define at least one priority state.",
                    recipe.RecipeId);
                return;
            }

            var selected = new HashSet<string>(StringComparer.Ordinal);
            foreach (var strategy in strategyResolution.Strategies)
                selected.Add(strategy.StrategyStableId);
            var stateIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var state in recipe.PriorityStates)
            {
                if (state == null ||
                    !AuthoringIdentifier.IsValid(state.StateId) ||
                    !AuthoringIdentifier.IsValid(state.EntryConditionId) ||
                    !stateIds.Add(state.StateId))
                {
                    Error(
                        result,
                        "ACA-NPC-PRIORITY-STATE-DEFINITION",
                        "Priority state ID/entry condition must be valid and unique.",
                        state?.StateId);
                    continue;
                }
                foreach (var strategyId in state.StrategyStableIds ??
                    new List<string>())
                {
                    if (!selected.Contains(strategyId))
                    {
                        Error(
                            result,
                            "ACA-NPC-PRIORITY-STATE-STRATEGY",
                            "Priority state references a strategy that is not selected by the recipe.",
                            state.StateId + ":" + strategyId);
                    }
                }
            }
            foreach (var state in recipe.PriorityStates)
            {
                if (state != null &&
                    !string.IsNullOrWhiteSpace(state.FallbackStateId) &&
                    !stateIds.Contains(state.FallbackStateId))
                {
                    Error(
                        result,
                        "ACA-NPC-PRIORITY-STATE-FALLBACK",
                        "Priority state fallback is unresolved.",
                        state.StateId + ":" + state.FallbackStateId);
                }
            }
        }

        static void RequireCapability(
            ISet<string> capabilities,
            string capability,
            NpcDefinition definition,
            NpcDefinitionValidationResult result)
        {
            if (!capabilities.Contains(capability))
                Error(result, "ACA-NPC-ROLE-CAPABILITY", definition.Role + " requires " + capability + ".", definition.StableId);
        }

        static void RequireId(NpcDefinitionValidationResult result, string value, string target)
        {
            if (!AuthoringIdentifier.IsValid(value))
                Error(result, "ACA-NPC-REQUIRED-ID", "A stable logical ID is required.", target);
        }

        static void RequireText(NpcDefinitionValidationResult result, string value, string target)
        {
            if (string.IsNullOrWhiteSpace(value))
                Error(result, "ACA-NPC-REQUIRED-TEXT", "A non-empty value is required.", target);
        }

        static void Error(NpcDefinitionValidationResult result, string code, string message, string target)
        {
            result.Diagnostics.Add(AuthoringDiagnostic.Error(code, message, target));
        }
    }

    public static class NpcRecipePlanner
    {
        public static NpcPlanCompilationResult Compile(
            NpcDefinition definition,
            NpcBehaviorRecipe behaviorRecipe,
            NpcValidationContext context)
        {
            var result = new NpcPlanCompilationResult();
            var validation = NpcDefinitionValidator.Validate(definition, behaviorRecipe, context);
            result.Diagnostics.AddRange(validation.Diagnostics.Items);
            if (!validation.Success)
                return result;

            var orderedCapabilities = TopologicalOrder(definition.Capabilities, result.Diagnostics);
            if (result.Diagnostics.HasErrors)
                return result;
            var strategyResolution = NpcStrategyResolver.Resolve(
                context?.LegoCatalog ??
                    NpcBuiltInBehaviorLegoCatalog.Create(),
                behaviorRecipe.Strategies,
                context?.AvailableResources);
            if (!strategyResolution.Success)
            {
                result.Diagnostics.AddRange(
                    strategyResolution.Diagnostics.Items);
                return result;
            }

            var plan = new NpcAuthoringPlan
            {
                NpcStableId = definition.StableId,
                DefinitionVersion = definition.Version,
                BehaviorRecipeId = behaviorRecipe.RecipeId,
                CharacterSourceProviderId = definition.CharacterSource.ProviderId,
                CharacterSourceLogicalId = definition.CharacterSource.SourceLogicalId,
                AppearanceItemIds = CollectAppearanceItemIds(definition.Appearance),
                WeaponIntegrationChoice = definition.WeaponLoadout?.IntegrationChoice ??
                    NpcWeaponIntegrationChoice.Disabled,
                WeaponLoadoutId = definition.WeaponLoadout?.LoadoutId,
                WeaponHolsterPolicy = definition.WeaponLoadout?.HolsterPolicy ??
                    NpcWeaponHolsterPolicy.Manual,
                ResolvedAgentTypeId = context.NavigationAgents[definition.Navigation.AgentLogicalId],
                OrderedCapabilityIds = orderedCapabilities,
                AnimationRequirements = new List<NpcAnimationRoleRequirement>(definition.AnimationRequirements),
                ResolvedStrategies = new List<NpcResolvedStrategy>(
                    strategyResolution.Strategies),
                PriorityStates = new List<NpcPriorityStateRecord>(
                    behaviorRecipe.PriorityStates)
            };
            foreach (var strategy in plan.ResolvedStrategies)
                plan.ResourceManifest.AddRange(strategy.ResourceManifest);

            foreach (var integration in definition.Integrations)
                if (integration.Mode == NpcIntegrationMode.Required)
                    plan.RequiredIntegrations.Add(integration.IntegrationId);
            plan.RequiredIntegrations.Sort(StringComparer.Ordinal);
            plan.RequiredBehaviorTasks.AddRange(behaviorRecipe.Tasks);
            plan.RequiredBehaviorTasks.Sort((left, right) =>
                string.CompareOrdinal(left.AssemblyQualifiedTaskType, right.AssemblyQualifiedTaskType));
            plan.AnimationRequirements.Sort((left, right) =>
                string.CompareOrdinal(left.RoleId, right.RoleId));
            plan.PriorityStates.Sort((left, right) =>
            {
                int priority = right.Priority.CompareTo(left.Priority);
                return priority != 0
                    ? priority
                    : string.CompareOrdinal(left.StateId, right.StateId);
            });
            plan.ResourceManifest.Sort((left, right) =>
                string.CompareOrdinal(left.BindingId, right.BindingId));
            plan.DeterministicHash = HashPlan(plan);
            result.Plan = plan;
            return result;
        }

        static List<string> TopologicalOrder(
            IList<NpcCapabilityRecipe> recipes,
            DiagnosticList diagnostics)
        {
            var byId = new SortedDictionary<string, NpcCapabilityRecipe>(StringComparer.Ordinal);
            foreach (var recipe in recipes)
                byId[recipe.CapabilityId] = recipe;

            var ordered = new List<string>();
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in byId.Keys)
                Visit(id, byId, visiting, visited, ordered, diagnostics);
            return ordered;
        }

        static void Visit(
            string id,
            IDictionary<string, NpcCapabilityRecipe> recipes,
            ISet<string> visiting,
            ISet<string> visited,
            ICollection<string> ordered,
            DiagnosticList diagnostics)
        {
            if (visited.Contains(id))
                return;
            if (!visiting.Add(id))
            {
                diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-CAPABILITY-CYCLE",
                    "NPC capability dependency cycle detected.",
                    id));
                return;
            }

            var dependencies = new List<string>(recipes[id].Requires ?? new List<string>());
            dependencies.Sort(StringComparer.Ordinal);
            foreach (var dependency in dependencies)
                Visit(dependency, recipes, visiting, visited, ordered, diagnostics);
            visiting.Remove(id);
            if (visited.Add(id))
                ordered.Add(id);
        }

        static string HashPlan(NpcAuthoringPlan plan)
        {
            var map = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["schema"] = plan.SchemaVersion,
                ["npc"] = plan.NpcStableId,
                ["definitionVersion"] = plan.DefinitionVersion ?? string.Empty,
                ["behaviorRecipe"] = plan.BehaviorRecipeId ?? string.Empty,
                ["characterSourceProvider"] = plan.CharacterSourceProviderId ?? string.Empty,
                ["characterSource"] = plan.CharacterSourceLogicalId ?? string.Empty,
                ["weaponIntegration"] = plan.WeaponIntegrationChoice.ToString(),
                ["weaponLoadout"] = plan.WeaponLoadoutId ?? string.Empty,
                ["weaponHolsterPolicy"] = plan.WeaponHolsterPolicy.ToString(),
                ["agentType"] = plan.ResolvedAgentTypeId.ToString(CultureInfo.InvariantCulture)
            };
            AddStrings(map, "capability", plan.OrderedCapabilityIds);
            AddStrings(map, "appearance", plan.AppearanceItemIds);
            AddStrings(map, "integration", plan.RequiredIntegrations);
            for (int i = 0; i < plan.RequiredBehaviorTasks.Count; i++)
            {
                var task = plan.RequiredBehaviorTasks[i];
                string prefix = "task." + i.ToString(CultureInfo.InvariantCulture) + ".";
                map[prefix + "block"] = task.BlockId ?? string.Empty;
                map[prefix + "type"] = task.AssemblyQualifiedTaskType ?? string.Empty;
                map[prefix + "pack"] = task.PackIntegrationId ?? string.Empty;
                AddStrings(map, prefix + "variable", task.RequiredSharedVariables);
            }
            for (int i = 0; i < plan.AnimationRequirements.Count; i++)
            {
                var animation = plan.AnimationRequirements[i];
                string prefix = "animation." + i.ToString(CultureInfo.InvariantCulture) + ".";
                map[prefix + "role"] = animation.RoleId ?? string.Empty;
                map[prefix + "motion"] = animation.MotionId ?? string.Empty;
                map[prefix + "required"] = animation.Required ? "true" : "false";
                map[prefix + "fallback"] = animation.SafeFallbackRoleId ?? string.Empty;
                AddStrings(map, prefix + "tag", animation.RequiredTags);
                AddStrings(map, prefix + "variant", animation.PreferredVariantIds);
            }
            for (int i = 0; i < plan.ResolvedStrategies.Count; i++)
            {
                var strategy = plan.ResolvedStrategies[i];
                string prefix = "strategy." +
                    i.ToString(CultureInfo.InvariantCulture) + ".";
                map[prefix + "semantic"] =
                    strategy.SemanticBehaviorId ?? string.Empty;
                map[prefix + "family"] = strategy.FamilyId ?? string.Empty;
                map[prefix + "id"] =
                    strategy.StrategyStableId ?? string.Empty;
                map[prefix + "version"] =
                    strategy.StrategyVersion ?? string.Empty;
                map[prefix + "provider"] =
                    strategy.ProviderId ?? string.Empty;
                AddMap(map, prefix + "parameter", strategy.Parameters);
                for (int atomIndex = 0;
                     atomIndex < strategy.Atoms.Count;
                     atomIndex++)
                {
                    var atom = strategy.Atoms[atomIndex];
                    string atomPrefix = prefix + "atom." +
                        atomIndex.ToString(
                            CultureInfo.InvariantCulture) + ".";
                    map[atomPrefix + "id"] =
                        atom.AtomStableId ?? string.Empty;
                    map[atomPrefix + "version"] =
                        atom.AtomVersion ?? string.Empty;
                    map[atomPrefix + "step"] =
                        atom.StepId ?? string.Empty;
                    AddMap(
                        map,
                        atomPrefix + "parameter",
                        atom.Parameters);
                }
            }
            for (int i = 0; i < plan.PriorityStates.Count; i++)
            {
                var state = plan.PriorityStates[i];
                string prefix = "state." +
                    i.ToString(CultureInfo.InvariantCulture) + ".";
                map[prefix + "id"] = state.StateId ?? string.Empty;
                map[prefix + "priority"] = state.Priority.ToString(
                    CultureInfo.InvariantCulture);
                map[prefix + "condition"] =
                    state.EntryConditionId ?? string.Empty;
                map[prefix + "fallback"] =
                    state.FallbackStateId ?? string.Empty;
                AddStrings(
                    map,
                    prefix + "strategy",
                    state.StrategyStableIds);
            }
            for (int i = 0; i < plan.ResourceManifest.Count; i++)
            {
                var binding = plan.ResourceManifest[i];
                string prefix = "resource." +
                    i.ToString(CultureInfo.InvariantCulture) + ".";
                map[prefix + "binding"] =
                    binding.BindingId ?? string.Empty;
                map[prefix + "kind"] = binding.Kind.ToString();
                map[prefix + "cardinality"] =
                    binding.Cardinality.ToString();
                map[prefix + "fallback"] =
                    binding.UsedFallback ? "true" : "false";
                AddStrings(
                    map,
                    prefix + "resolved",
                    binding.ResolvedResourceIds);
            }
            return CanonicalSerialization.Sha256(CanonicalSerialization.SerializeMap(map));
        }

        static List<string> CollectAppearanceItemIds(NpcAppearanceLoadoutRecord appearance)
        {
            var ids = new List<string>();
            foreach (var part in appearance?.Parts ?? new List<NpcAppearancePartSelection>())
                if (part != null && !string.IsNullOrWhiteSpace(part.ItemStableId))
                    ids.Add(part.Slot + ":" + part.ItemStableId + ":" + (part.ReplaceDefault ? "replace" : "add"));
            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        static void AddStrings(
            IDictionary<string, string> map,
            string prefix,
            IList<string> values)
        {
            var sorted = new List<string>(values ?? new List<string>());
            sorted.Sort(StringComparer.Ordinal);
            map[prefix + ".count"] = sorted.Count.ToString(CultureInfo.InvariantCulture);
            for (int i = 0; i < sorted.Count; i++)
                map[prefix + "." + i.ToString(CultureInfo.InvariantCulture)] = sorted[i] ?? string.Empty;
        }

        static void AddMap(
            IDictionary<string, string> map,
            string prefix,
            IDictionary<string, string> values)
        {
            var sorted = new SortedDictionary<string, string>(
                values ?? new Dictionary<string, string>(),
                StringComparer.Ordinal);
            map[prefix + ".count"] = sorted.Count.ToString(
                CultureInfo.InvariantCulture);
            int index = 0;
            foreach (var pair in sorted)
            {
                string item = prefix + "." +
                    index.ToString(CultureInfo.InvariantCulture) + ".";
                map[item + "key"] = pair.Key ?? string.Empty;
                map[item + "value"] = pair.Value ?? string.Empty;
                index++;
            }
        }
    }

    public static class NpcCommerceTransaction
    {
        public static NpcCommerceResult Buy(
            NpcCommerceOfferRecord offer,
            INpcCommerceInventory inventory,
            int availableStock)
        {
            if (offer == null || inventory == null || availableStock < 0)
                return Result(NpcCommerceResultCode.InvalidRequest, offer, "Offer, inventory, and stock are required.");
            if (availableStock < offer.QuantityPerPurchase)
                return Result(NpcCommerceResultCode.OutOfStock, offer, "Offer stock is insufficient.");
            if (offer.Price <= 0 || offer.QuantityPerPurchase <= 0 ||
                string.IsNullOrWhiteSpace(offer.ItemLogicalId) ||
                string.IsNullOrWhiteSpace(offer.CurrencyItemLogicalId) ||
                string.Equals(offer.ItemLogicalId, offer.CurrencyItemLogicalId, StringComparison.Ordinal))
            {
                return Result(NpcCommerceResultCode.InvalidRequest, offer, "Offer configuration is invalid.");
            }
            if (inventory.Count(offer.CurrencyItemLogicalId) < offer.Price)
                return Result(NpcCommerceResultCode.InsufficientCurrency, offer, "Inventory does not contain enough currency.");

            var currencyRemoval = inventory.Remove(offer.CurrencyItemLogicalId, offer.Price);
            if (currencyRemoval == null || currencyRemoval.AppliedCount != offer.Price)
            {
                int removed = currencyRemoval?.AppliedCount ?? 0;
                if (removed > 0)
                {
                    var refund = inventory.Add(offer.CurrencyItemLogicalId, removed);
                    if (refund == null || refund.AppliedCount != removed)
                        return Result(NpcCommerceResultCode.RollbackFailed, offer, "Partial currency removal could not be refunded.", removed, 0);
                }
                return Result(NpcCommerceResultCode.CurrencyRemovalFailed, offer, "Currency removal failed.", removed, 0);
            }

            var itemAddition = inventory.Add(offer.ItemLogicalId, offer.QuantityPerPurchase);
            int added = itemAddition?.AppliedCount ?? 0;
            if (added != offer.QuantityPerPurchase)
            {
                bool rollbackOk = true;
                if (added > 0)
                {
                    var itemRollback = inventory.Remove(offer.ItemLogicalId, added);
                    rollbackOk = itemRollback != null && itemRollback.AppliedCount == added;
                }

                var currencyRefund = inventory.Add(offer.CurrencyItemLogicalId, offer.Price);
                rollbackOk &= currencyRefund != null && currencyRefund.AppliedCount == offer.Price;
                return Result(
                    rollbackOk ? NpcCommerceResultCode.DestinationRejected : NpcCommerceResultCode.RollbackFailed,
                    offer,
                    rollbackOk
                        ? (itemAddition?.RejectionReason ?? "Destination inventory rejected the item.")
                        : "The rejected purchase could not be rolled back completely.",
                    offer.Price,
                    added);
            }

            return Result(NpcCommerceResultCode.Success, offer, "Purchase completed.", offer.Price, added);
        }

        static NpcCommerceResult Result(
            NpcCommerceResultCode code,
            NpcCommerceOfferRecord offer,
            string message,
            int removed = 0,
            int added = 0)
        {
            return new NpcCommerceResult
            {
                Code = code,
                OfferId = offer?.OfferId,
                CurrencyRemoved = removed,
                ItemsAdded = added,
                Message = message
            };
        }
    }

    public static class NpcDialogueFallback
    {
        public static string Resolve(string localizedText, string englishSourceFallback)
        {
            return string.IsNullOrWhiteSpace(localizedText)
                ? (englishSourceFallback ?? string.Empty)
                : localizedText;
        }
    }
}
