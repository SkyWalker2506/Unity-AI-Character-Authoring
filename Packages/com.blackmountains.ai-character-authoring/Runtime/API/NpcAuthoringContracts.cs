using System;
using System.Collections.Generic;

namespace BlackMountains.AICharacterAuthoring
{
    public enum NpcRole
    {
        Vendor,
        Bandit,
        Companion
    }

    public enum NpcIntegrationMode
    {
        Disabled,
        Optional,
        Required
    }

    public enum NpcWeaponIntegrationChoice
    {
        Disabled,
        Stp
    }

    public enum NpcWeaponHolsterPolicy
    {
        Manual,
        AlwaysDrawn,
        DrawOnThreatStowWhenSafe
    }

    public enum NpcEquipmentSlot
    {
        Head,
        Hair,
        Hat,
        Torso,
        Hands,
        Legs,
        Feet,
        PrimaryWeapon,
        SecondaryWeapon,
        Holster
    }

    public enum NpcWeaponGrip
    {
        Unknown,
        OneHanded,
        TwoHanded
    }

    public static class NpcIntegrationIds
    {
        public const string BehaviorDesigner = "integration.behavior-designer";
        public const string BehaviorDesignerMovementPack = "integration.behavior-designer.movement-pack";
        public const string BehaviorDesignerSensesPack = "integration.behavior-designer.senses-pack";
        public const string StpInteraction = "integration.stp.interaction";
        public const string StpInventoryCommerce = "integration.stp.inventory-commerce";
        public const string StpWeaponIntegration = "integration.stp.weapon";
        public const string UnityLocalization = "integration.unity.localization";
        public const string UnityInputSystem = "integration.unity.input-system";
        public const string SemanticAnimationLibrary = "integration.semantic-animation-library";
        public const string RagdollAnimator2 = "integration.ragdoll-animator-2";
    }

    public static class NpcCapabilityIds
    {
        public const string Identity = "npc.core.identity";
        public const string Visual = "npc.core.visual";
        public const string Animator = "npc.core.animator";
        public const string Navigation = "npc.navigation.navmesh";
        public const string Idle = "npc.movement.idle";
        public const string Wander = "npc.movement.wander";
        public const string Patrol = "npc.movement.patrol";
        public const string Follow = "npc.movement.follow";
        public const string Pursue = "npc.movement.pursue";
        public const string Flee = "npc.movement.flee";
        public const string SenseVisibility = "npc.sense.visibility";
        public const string SenseSound = "npc.sense.sound";
        public const string SenseDistance = "npc.sense.distance";
        public const string Faction = "npc.faction";
        public const string Interaction = "npc.interaction";
        public const string Dialogue = "npc.dialogue";
        public const string Voice = "npc.voice";
        public const string Commerce = "npc.commerce";
        public const string CombatMelee = "npc.combat.melee";
        public const string CompanionFollow = "npc.companion.follow";
        public const string CompanionDefend = "npc.companion.defend";
        public const string BehaviorDesigner = "npc.behavior-designer";
        public const string SemanticAnimation = "npc.animation.semantic";
        public const string WeaponLoadout = "npc.weapon.loadout";
        public const string WeaponEquip = "npc.weapon.equip";
        public const string WeaponStow = "npc.weapon.stow";
        public const string WeaponDraw = "npc.weapon.draw";
        public const string WeaponAttack = "npc.weapon.attack";
    }

    public static class NpcCharacterSourceProviderIds
    {
        public const string StpStaticMale = "provider.nwb.character-source.stp-static-male";
        public const string AdventureCharactersPack2 = "provider.nwb.character-source.adventure-characters-pack-2";
    }

    public static class NpcEquipmentSourceProviderIds
    {
        public const string ProjectDefaults = "provider.nwb.equipment.project-defaults";
        public const string HqFpsWeapons2 = "provider.nwb.equipment.hq-fps-weapons-2";
        public const string PbrMeleeWeaponsPack = "provider.nwb.equipment.pbr-melee-weapons-pack";
    }

    public static class NpcActionIds
    {
        public const string PrimaryInteract = "npc.action.primary-interact";
        public const string Talk = "npc.action.talk";
        public const string Trade = "npc.action.trade";
        public const string CompanionCommand = "npc.action.companion-command";
        public const string DialogueNext = "npc.action.dialogue-next";
        public const string DialogueCancel = "npc.action.dialogue-cancel";
    }

    [Serializable]
    public sealed class NpcDefinition
    {
        public const string CurrentSchemaVersion = "1";

        public string SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string StableId { get; set; }
        public NpcRole Role { get; set; }
        public string DisplayNameLocalizationKey { get; set; }
        public string EnglishDisplayNameFallback { get; set; }
        public string Archetype { get; set; }
        public NpcArchetypeProfileRecord ArchetypeProfile { get; set; }
        public NpcEcosystemProfileRecord Ecosystem { get; set; }
        public NpcVariationRequestRecord VariationRequest { get; set; } =
            new NpcVariationRequestRecord();
        public string FactionId { get; set; }
        public NpcCharacterSourceSelection CharacterSource { get; set; } =
            new NpcCharacterSourceSelection();
        public NpcAppearanceLoadoutRecord Appearance { get; set; } =
            new NpcAppearanceLoadoutRecord();
        public NpcVisualReference Visual { get; set; } = new NpcVisualReference();
        public NpcNavigationSelection Navigation { get; set; } = new NpcNavigationSelection();
        public List<NpcCapabilityRecipe> Capabilities { get; set; } = new List<NpcCapabilityRecipe>();
        public List<NpcIntegrationRequest> Integrations { get; set; } = new List<NpcIntegrationRequest>();
        public List<NpcInteractionActionRecord> InteractionActions { get; set; } = new List<NpcInteractionActionRecord>();
        public List<NpcInputActionRecord> InputActions { get; set; } = new List<NpcInputActionRecord>();
        public List<NpcDialogueLineRecord> DialogueLines { get; set; } = new List<NpcDialogueLineRecord>();
        public List<NpcCommerceOfferRecord> CommerceOffers { get; set; } = new List<NpcCommerceOfferRecord>();
        public List<NpcAnimationRoleRequirement> AnimationRequirements { get; set; } = new List<NpcAnimationRoleRequirement>();
        public NpcCompanionRecord Companion { get; set; }
        public NpcWeaponLoadoutRecord WeaponLoadout { get; set; }
        public string BehaviorRecipeId { get; set; }
        public string Version { get; set; } = "1";
    }

    [Serializable]
    public sealed class NpcCharacterSourceSelection
    {
        public string ProviderId { get; set; }
        public string SourceLogicalId { get; set; }
        public string ModelLogicalId { get; set; }
        public string AvatarLogicalId { get; set; }
        public List<string> RequiredAnimationTags { get; set; } = new List<string>();
        public bool RequireHumanoidAvatar { get; set; } = true;
        public bool RequireEquipmentCompatibility { get; set; }
    }

    [Serializable]
    public sealed class NpcCharacterSourceDescriptor
    {
        public string ProviderId { get; set; }
        public string SourceLogicalId { get; set; }
        public string DisplayName { get; set; }
        public string ModelLogicalId { get; set; }
        public string AvatarLogicalId { get; set; }
        public bool HumanoidAvatarCompatible { get; set; }
        public bool EquipmentCompatible { get; set; }
        public List<string> AnimationTags { get; set; } = new List<string>();
        public List<string> Diagnostics { get; set; } = new List<string>();
    }

    [Serializable]
    public sealed class NpcCharacterSourceProviderStatus
    {
        public string ProviderId { get; set; }
        public bool Installed { get; set; }
        public string Message { get; set; }
        public List<NpcCharacterSourceDescriptor> Sources { get; set; } =
            new List<NpcCharacterSourceDescriptor>();
    }

    public interface INpcCharacterSourceProvider
    {
        string ProviderId { get; }
        NpcCharacterSourceProviderStatus Discover();
    }

    [Serializable]
    public sealed class NpcModularEquipmentItemRecord
    {
        public string StableId { get; set; }
        public string SourceProviderId { get; set; }
        public NpcEquipmentSlot Slot { get; set; }
        public string DisplayName { get; set; }
        public string VariantId { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> StyleTags { get; set; } = new List<string>();
        public List<string> CompatibleRigIds { get; set; } = new List<string>();
        public List<string> CompatibleBodyIds { get; set; } = new List<string>();
        public List<string> AnimationCompatibilityTags { get; set; } = new List<string>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public NpcWeaponGrip WeaponGrip { get; set; }
        public string ContactEventSemantic { get; set; }
        public string HolsterSlotLogicalId { get; set; }
        public bool SupportsStpItem { get; set; }
        public string StpItemLogicalId { get; set; }
    }

    [Serializable]
    public sealed class NpcAppearancePartSelection
    {
        public NpcEquipmentSlot Slot { get; set; }
        public string ItemStableId { get; set; }
        public bool ReplaceDefault { get; set; } = true;
    }

    [Serializable]
    public sealed class NpcAppearanceLoadoutRecord
    {
        public string DefaultCharacterSourceLogicalId { get; set; }
        public List<NpcAppearancePartSelection> Parts { get; set; } =
            new List<NpcAppearancePartSelection>();
    }

    [Serializable]
    public sealed class NpcEquipmentSourceProviderStatus
    {
        public string ProviderId { get; set; }
        public bool Installed { get; set; }
        public string Message { get; set; }
        public List<NpcModularEquipmentItemRecord> Items { get; set; } =
            new List<NpcModularEquipmentItemRecord>();
    }

    public interface INpcEquipmentSourceProvider
    {
        string ProviderId { get; }
        NpcEquipmentSourceProviderStatus Discover();
    }

    [Serializable]
    public sealed class NpcVisualReference
    {
        public string ModelLogicalId { get; set; }
        public string RigProfileId { get; set; } = "rig.humanoid";
        public string AnimationCatalogLogicalId { get; set; }
    }

    [Serializable]
    public sealed class NpcNavigationSelection
    {
        public string AgentLogicalId { get; set; }
        public int? ResolvedProjectAgentTypeId { get; set; }
    }

    [Serializable]
    public sealed class NpcCapabilityRecipe
    {
        public const string CurrentSchemaVersion = "1";

        public string CapabilityId { get; set; }
        public string ProviderId { get; set; } = "provider.blackmountains.npc-lego";
        public string ProviderSchemaVersion { get; set; } = CurrentSchemaVersion;
        public List<string> Requires { get; set; } = new List<string>();
        public List<string> Conflicts { get; set; } = new List<string>();
        public Dictionary<string, string> Parameters { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
        public List<string> OwnedFields { get; set; } = new List<string>();
        public List<string> ValidationRules { get; set; } = new List<string>();
        public AuthoringQualityPolicy QualityPolicy { get; set; } = AuthoringQualityPolicy.DeterministicOnly;
    }

    [Serializable]
    public sealed class NpcIntegrationRequest
    {
        public string IntegrationId { get; set; }
        public NpcIntegrationMode Mode { get; set; }
    }

    [Serializable]
    public sealed class NpcInteractionActionRecord
    {
        public string ActionId { get; set; }
        public string PromptLocalizationKey { get; set; }
        public string EnglishPromptFallback { get; set; }
        public decimal Range { get; set; } = 3m;
        public decimal HoldSeconds { get; set; }
    }

    [Serializable]
    public sealed class NpcInputActionRecord
    {
        public string ActionId { get; set; }
        public string ContextId { get; set; } = "world";
        public string KeyboardDefaultPath { get; set; }
        public string GamepadDefaultPath { get; set; }
    }

    [Serializable]
    public sealed class NpcDialogueLineRecord
    {
        public string LineId { get; set; }
        public string SpeakerLocalizationKey { get; set; }
        public string SubtitleLocalizationKey { get; set; }
        public string EnglishSourceFallback { get; set; }
        public string VoiceLocalizationKey { get; set; }
        public string OptionalAudioClipLogicalId { get; set; }
        public decimal CooldownSeconds { get; set; }
        public decimal Weight { get; set; } = 1m;
        public List<string> Tags { get; set; } = new List<string>();
    }

    [Serializable]
    public sealed class NpcCommerceOfferRecord
    {
        public string OfferId { get; set; }
        public string ItemLogicalId { get; set; }
        public int QuantityPerPurchase { get; set; } = 1;
        public int InitialStock { get; set; } = 1;
        public string CurrencyItemLogicalId { get; set; }
        public int Price { get; set; } = 1;
        public string LabelLocalizationKey { get; set; }
        public string EnglishLabelFallback { get; set; }
    }

    [Serializable]
    public sealed class NpcAnimationRoleRequirement
    {
        public string RoleId { get; set; }
        public string MotionId { get; set; }
        public List<string> RequiredTags { get; set; } = new List<string>();
        public List<string> PreferredVariantIds { get; set; } = new List<string>();
        public bool Required { get; set; } = true;
        public string SafeFallbackRoleId { get; set; }
    }

    [Serializable]
    public sealed class NpcCompanionRecord
    {
        public string LeaderPolicy { get; set; } = "project.local-player";
        public decimal FollowDistance { get; set; } = 3m;
        public decimal HoldDistance { get; set; } = 1m;
        public bool DefendPlayer { get; set; } = true;
        public bool DefendChild { get; set; } = true;
        public List<string> FriendlyFactions { get; set; } = new List<string>();
        public List<string> ThreatFactions { get; set; } = new List<string>();
    }

    [Serializable]
    public sealed class NpcWeaponLoadoutRecord
    {
        public string LoadoutId { get; set; }
        public NpcWeaponIntegrationChoice IntegrationChoice { get; set; }
        public string WeaponItemLogicalId { get; set; }
        public string EquipmentItemStableId { get; set; }
        public string HolsterSocketLogicalId { get; set; }
        public NpcWeaponHolsterPolicy HolsterPolicy { get; set; } =
            NpcWeaponHolsterPolicy.DrawOnThreatStowWhenSafe;
        public string EquipAnimationRoleId { get; set; }
        public string DrawAnimationRoleId { get; set; }
        public string AttackAnimationRoleId { get; set; }
        public string StowAnimationRoleId { get; set; }
    }

    [Serializable]
    public sealed class NpcBehaviorTaskRequirement
    {
        public string BlockId { get; set; }
        public string AssemblyQualifiedTaskType { get; set; }
        public string PackIntegrationId { get; set; }
        public List<string> RequiredSharedVariables { get; set; } = new List<string>();
    }

    [Serializable]
    public sealed class NpcBehaviorRecipe
    {
        public string RecipeId { get; set; }
        public NpcRole Role { get; set; }
        public List<NpcBehaviorTaskRequirement> Tasks { get; set; } = new List<NpcBehaviorTaskRequirement>();
        public List<NpcStrategySelection> Strategies { get; set; } =
            new List<NpcStrategySelection>();
        public List<NpcPriorityStateRecord> PriorityStates { get; set; } =
            new List<NpcPriorityStateRecord>();
        public List<string> NormalizedFlow { get; set; } = new List<string>();
    }

    public interface INpcCommerceInventory
    {
        int Count(string itemLogicalId);
        NpcInventoryMutation Add(string itemLogicalId, int amount);
        NpcInventoryMutation Remove(string itemLogicalId, int amount);
    }

    [Serializable]
    public sealed class NpcInventoryMutation
    {
        public int AppliedCount { get; set; }
        public string RejectionReason { get; set; }
    }

    public enum NpcCommerceResultCode
    {
        Success,
        InvalidRequest,
        OfferUnavailable,
        OutOfStock,
        InsufficientCurrency,
        CurrencyRemovalFailed,
        DestinationRejected,
        RollbackFailed
    }

    [Serializable]
    public sealed class NpcCommerceResult
    {
        public NpcCommerceResultCode Code { get; set; }
        public string OfferId { get; set; }
        public int CurrencyRemoved { get; set; }
        public int ItemsAdded { get; set; }
        public string Message { get; set; }
        public bool Success => Code == NpcCommerceResultCode.Success;
    }
}
