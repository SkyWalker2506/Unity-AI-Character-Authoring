using System.Collections.Generic;

namespace BlackMountains.AICharacterAuthoring
{
    public static class NpcSampleRecipeCatalog
    {
        const string MovementTasks = "Opsive.BehaviorDesigner.AddOns.MovementPack.Runtime.Tasks.";
        const string MovementAssembly = ", Opsive.BehaviorDesigner.AddOns.MovementPack.Runtime";
        const string SensesTasks = "Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Tasks.";
        const string SensesAssembly = ", Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime";

        public static NpcDefinition CreateVendor()
        {
            var definition = Base(
                "npc.sample.vendor",
                NpcRole.Vendor,
                "npc.name.vendor",
                "Mara",
                "faction.neutral",
                "npc.behavior.vendor");
            AddCapability(definition, NpcCapabilityIds.Wander, NpcCapabilityIds.Navigation);
            AddCapability(definition, NpcCapabilityIds.Flee, NpcCapabilityIds.Navigation);
            AddCapability(definition, NpcCapabilityIds.Interaction, NpcCapabilityIds.Identity);
            AddCapability(definition, NpcCapabilityIds.Dialogue, NpcCapabilityIds.Interaction);
            AddCapability(definition, NpcCapabilityIds.Voice, NpcCapabilityIds.Dialogue);
            AddCapability(definition, NpcCapabilityIds.Commerce, NpcCapabilityIds.Interaction);
            AddCapability(definition, NpcCapabilityIds.BehaviorDesigner, NpcCapabilityIds.Wander, NpcCapabilityIds.Flee);
            AddCapability(definition, NpcCapabilityIds.SemanticAnimation, NpcCapabilityIds.Animator);
            AddRequiredIntegration(definition, NpcIntegrationIds.BehaviorDesigner);
            AddRequiredIntegration(definition, NpcIntegrationIds.BehaviorDesignerMovementPack);
            AddRequiredIntegration(definition, NpcIntegrationIds.StpInteraction);
            AddRequiredIntegration(definition, NpcIntegrationIds.StpInventoryCommerce);
            AddRequiredIntegration(definition, NpcIntegrationIds.UnityLocalization);
            AddRequiredIntegration(definition, NpcIntegrationIds.UnityInputSystem);
            AddRequiredIntegration(definition, NpcIntegrationIds.SemanticAnimationLibrary);
            definition.InteractionActions.Add(Action(NpcActionIds.Talk, "npc.prompt.talk", "Talk"));
            definition.InteractionActions.Add(Action(NpcActionIds.Trade, "npc.prompt.trade", "Trade"));
            definition.DialogueLines.Add(Line(
                "npc.vendor.greeting",
                "npc.dialogue.vendor.greeting",
                "Safe roads are worth more than full pockets.",
                "npc.voice.vendor.greeting"));
            definition.CommerceOffers.Add(Offer("npc.offer.vendor.water", "stp.item.water-bottle", 2, 5));
            definition.CommerceOffers.Add(Offer("npc.offer.vendor.food", "stp.item.energy-bar", 3, 5));
            definition.CommerceOffers.Add(Offer("npc.offer.vendor.medicine", "stp.item.antibiotics", 8, 2));
            definition.AnimationRequirements.Add(Animation("npc.animation.vendor.idle", "com.blackmountains.nowayback:motion/npc.idle", false));
            definition.AnimationRequirements.Add(Animation("npc.animation.vendor.talk", null, false, "ual.core:tag/gesture"));
            return definition;
        }

        public static NpcDefinition CreateBandit(bool stpWeaponIntegration = false)
        {
            var definition = Base(
                "npc.sample.bandit",
                NpcRole.Bandit,
                "npc.name.bandit",
                "Rook",
                "faction.bandit",
                "npc.behavior.bandit");
            AddCapability(definition, NpcCapabilityIds.Patrol, NpcCapabilityIds.Navigation);
            AddCapability(definition, NpcCapabilityIds.Pursue, NpcCapabilityIds.Navigation);
            AddCapability(definition, NpcCapabilityIds.SenseVisibility, NpcCapabilityIds.Identity);
            AddCapability(definition, NpcCapabilityIds.SenseSound, NpcCapabilityIds.Identity);
            AddCapability(definition, NpcCapabilityIds.SenseDistance, NpcCapabilityIds.Identity);
            AddCapability(definition, NpcCapabilityIds.CombatMelee, NpcCapabilityIds.Pursue, NpcCapabilityIds.SenseVisibility);
            AddCapability(definition, NpcCapabilityIds.BehaviorDesigner, NpcCapabilityIds.Patrol, NpcCapabilityIds.CombatMelee);
            AddCapability(definition, NpcCapabilityIds.SemanticAnimation, NpcCapabilityIds.Animator);
            ConfigureWeapon(definition, "bandit", stpWeaponIntegration);
            AddRequiredIntegration(definition, NpcIntegrationIds.BehaviorDesigner);
            AddRequiredIntegration(definition, NpcIntegrationIds.BehaviorDesignerMovementPack);
            AddRequiredIntegration(definition, NpcIntegrationIds.BehaviorDesignerSensesPack);
            AddRequiredIntegration(definition, NpcIntegrationIds.SemanticAnimationLibrary);
            definition.AnimationRequirements.Add(Animation("npc.animation.bandit.idle", "com.blackmountains.nowayback:motion/npc.idle", false));
            definition.AnimationRequirements.Add(Animation("npc.animation.bandit.walk", null, true, "ual.core:tag/locomotion.gait"));
            definition.AnimationRequirements.Add(Animation("npc.animation.bandit.run", null, true, "ual.core:tag/locomotion.gait"));
            definition.AnimationRequirements.Add(Animation("npc.animation.bandit.attack", null, true, "ual.core:tag/combat"));
            definition.AnimationRequirements.Add(Animation("npc.animation.bandit.hit", null, false, "ual.core:tag/reaction"));
            definition.AnimationRequirements.Add(Animation("npc.animation.bandit.death", null, false, "ual.core:tag/death"));
            return definition;
        }

        public static NpcDefinition CreateCompanion(bool stpWeaponIntegration = false)
        {
            var definition = Base(
                "npc.sample.companion",
                NpcRole.Companion,
                "npc.name.companion",
                "Eli",
                "faction.player-allies",
                "npc.behavior.companion");
            AddCapability(definition, NpcCapabilityIds.Follow, NpcCapabilityIds.Navigation);
            AddCapability(definition, NpcCapabilityIds.Pursue, NpcCapabilityIds.Navigation);
            AddCapability(definition, NpcCapabilityIds.SenseVisibility, NpcCapabilityIds.Identity);
            AddCapability(definition, NpcCapabilityIds.SenseDistance, NpcCapabilityIds.Identity);
            AddCapability(definition, NpcCapabilityIds.Interaction, NpcCapabilityIds.Identity);
            AddCapability(definition, NpcCapabilityIds.Dialogue, NpcCapabilityIds.Interaction);
            AddCapability(definition, NpcCapabilityIds.Voice, NpcCapabilityIds.Dialogue);
            AddCapability(definition, NpcCapabilityIds.CombatMelee, NpcCapabilityIds.Pursue, NpcCapabilityIds.SenseVisibility);
            AddCapability(definition, NpcCapabilityIds.CompanionFollow, NpcCapabilityIds.Follow);
            AddCapability(definition, NpcCapabilityIds.CompanionDefend, NpcCapabilityIds.CombatMelee);
            AddCapability(definition, NpcCapabilityIds.BehaviorDesigner, NpcCapabilityIds.CompanionFollow, NpcCapabilityIds.CompanionDefend);
            AddCapability(definition, NpcCapabilityIds.SemanticAnimation, NpcCapabilityIds.Animator);
            ConfigureWeapon(definition, "companion", stpWeaponIntegration);
            AddRequiredIntegration(definition, NpcIntegrationIds.BehaviorDesigner);
            AddRequiredIntegration(definition, NpcIntegrationIds.BehaviorDesignerMovementPack);
            AddRequiredIntegration(definition, NpcIntegrationIds.BehaviorDesignerSensesPack);
            AddRequiredIntegration(definition, NpcIntegrationIds.StpInteraction);
            AddRequiredIntegration(definition, NpcIntegrationIds.UnityLocalization);
            AddRequiredIntegration(definition, NpcIntegrationIds.UnityInputSystem);
            AddRequiredIntegration(definition, NpcIntegrationIds.SemanticAnimationLibrary);
            definition.InteractionActions.Add(Action(NpcActionIds.Talk, "npc.prompt.talk", "Talk"));
            definition.InteractionActions.Add(Action(NpcActionIds.CompanionCommand, "npc.prompt.command", "Command"));
            definition.DialogueLines.Add(Line(
                "npc.companion.ready",
                "npc.dialogue.companion.ready",
                "I'm with you. Say the word.",
                "npc.voice.companion.ready"));
            definition.Companion = new NpcCompanionRecord
            {
                LeaderPolicy = "project.local-player",
                FollowDistance = 3m,
                HoldDistance = 1m,
                DefendPlayer = true,
                DefendChild = true,
                FriendlyFactions = new List<string> { "faction.player", "faction.child", "faction.player-allies" },
                ThreatFactions = new List<string> { "faction.bandit", "faction.hostile" }
            };
            definition.AnimationRequirements.Add(Animation("npc.animation.companion.idle", "com.blackmountains.nowayback:motion/npc.idle", false));
            definition.AnimationRequirements.Add(Animation("npc.animation.companion.follow", null, true, "ual.core:tag/locomotion.gait"));
            definition.AnimationRequirements.Add(Animation("npc.animation.companion.defend", null, true, "ual.core:tag/combat"));
            definition.AnimationRequirements.Add(Animation("npc.animation.companion.talk", null, false, "ual.core:tag/gesture"));
            return definition;
        }

        public static NpcBehaviorRecipe CreateBehaviorRecipe(
            NpcRole role,
            bool stpWeaponIntegration = false)
        {
            switch (role)
            {
                case NpcRole.Vendor:
                    return new NpcBehaviorRecipe
                    {
                        RecipeId = "npc.behavior.vendor",
                        Role = role,
                        Tasks = new List<NpcBehaviorTaskRequirement>
                        {
                            Task("bd.move.wander", MovementTasks + "Wander" + MovementAssembly, NpcIntegrationIds.BehaviorDesignerMovementPack),
                            Task("bd.move.flee", MovementTasks + "Flee" + MovementAssembly, NpcIntegrationIds.BehaviorDesignerMovementPack, "Threat"),
                            Task("bd.move.rotate", MovementTasks + "RotateTowards" + MovementAssembly, NpcIntegrationIds.BehaviorDesignerMovementPack, "InteractionTarget")
                        },
                        Strategies = new List<NpcStrategySelection>
                        {
                            Selection("wander", "uniform-radius",
                                "radius", "4",
                                "dwell-min", "1",
                                "dwell-max", "2.5",
                                "seed", "1101"),
                            Selection("flee", "safe-zone",
                                "safe-distance", "14"),
                            Selection("interaction", "range-gate",
                                "range", "3"),
                            Selection("dialogue", "localized-line",
                                "localized-text-id",
                                "npc.dialogue.vendor.greeting"),
                            Selection("commerce", "atomic-offer"),
                            Selection("animation-role", "semantic-catalog",
                                "role-id", "npc.animation.vendor.idle")
                        },
                        PriorityStates = new List<NpcPriorityStateRecord>
                        {
                            State("npc.state.vendor.threatened", 300,
                                "npc.condition.threat-present",
                                "npc.state.vendor.ambient",
                                "npc.strategy.flee.safe-zone"),
                            State("npc.state.vendor.interaction", 200,
                                "npc.condition.interaction-active",
                                "npc.state.vendor.ambient",
                                "npc.strategy.interaction.range-gate",
                                "npc.strategy.dialogue.localized-line",
                                "npc.strategy.commerce.atomic-offer"),
                            State("npc.state.vendor.ambient", 100,
                                "npc.condition.always",
                                null,
                                "npc.strategy.wander.uniform-radius",
                                "npc.strategy.animation-role.semantic-catalog")
                        },
                        NormalizedFlow = new List<string>
                        {
                            "threatened:flee",
                            "interaction-active:rotate-and-pause",
                            "default:idle-or-short-wander"
                        }
                    };
                case NpcRole.Bandit:
                    return new NpcBehaviorRecipe
                    {
                        RecipeId = "npc.behavior.bandit",
                        Role = role,
                        Tasks = new List<NpcBehaviorTaskRequirement>
                        {
                            Task("bd.sense.detect", SensesTasks + "CanDetectObject" + SensesAssembly, NpcIntegrationIds.BehaviorDesignerSensesPack, "Target"),
                            Task("bd.sense.range", SensesTasks + "WithinRange" + SensesAssembly, NpcIntegrationIds.BehaviorDesignerSensesPack, "TargetDistance"),
                            Task("bd.move.patrol", MovementTasks + "Patrol" + MovementAssembly, NpcIntegrationIds.BehaviorDesignerMovementPack, "Waypoints"),
                            Task("bd.move.pursue", MovementTasks + "Pursue" + MovementAssembly, NpcIntegrationIds.BehaviorDesignerMovementPack, "Target"),
                            Task("bd.move.rotate", MovementTasks + "RotateTowards" + MovementAssembly, NpcIntegrationIds.BehaviorDesignerMovementPack, "Target")
                        },
                        Strategies = BanditStrategies(stpWeaponIntegration),
                        PriorityStates = BanditStates(stpWeaponIntegration),
                        NormalizedFlow = new List<string>
                        {
                            "threat:draw-equip-attack",
                            "safe:stow",
                            "detect-hostile:pursue-rotate-attack",
                            "lost-target:search-last-known-return-patrol",
                            "default:patrol"
                        }
                    };
                default:
                    return new NpcBehaviorRecipe
                    {
                        RecipeId = "npc.behavior.companion",
                        Role = role,
                        Tasks = new List<NpcBehaviorTaskRequirement>
                        {
                            Task("bd.sense.detect", SensesTasks + "CanDetectObject" + SensesAssembly, NpcIntegrationIds.BehaviorDesignerSensesPack, "Threat"),
                            Task("bd.sense.range", SensesTasks + "WithinRange" + SensesAssembly, NpcIntegrationIds.BehaviorDesignerSensesPack, "LeaderDistance"),
                            Task("bd.move.follow", MovementTasks + "Follow" + MovementAssembly, NpcIntegrationIds.BehaviorDesignerMovementPack, "Leader"),
                            Task("bd.move.pursue", MovementTasks + "Pursue" + MovementAssembly, NpcIntegrationIds.BehaviorDesignerMovementPack, "Threat"),
                            Task("bd.move.rotate", MovementTasks + "RotateTowards" + MovementAssembly, NpcIntegrationIds.BehaviorDesignerMovementPack, "Threat")
                        },
                        Strategies = CompanionStrategies(stpWeaponIntegration),
                        PriorityStates = CompanionStates(stpWeaponIntegration),
                        NormalizedFlow = new List<string>
                        {
                            "threat:draw-equip-attack",
                            "safe:stow",
                            "explicit-wait:hold",
                            "threat-to-player-or-child:pursue-defend",
                            "leader-too-far:follow",
                            "default:idle-near-leader"
                        }
                    };
            }
        }

        static List<NpcStrategySelection> BanditStrategies(
            bool stpWeaponIntegration)
        {
            var strategies = new List<NpcStrategySelection>
            {
                Selection("sense-visibility", "cone-memory",
                    "distance", "28"),
                Selection("sense-sound", "radius",
                    "distance", "20"),
                Selection("patrol", "sequence",
                    "route-id", "npc.route.bandit.demo",
                    "dwell-seconds", "1.5",
                    "seed", "2202"),
                Selection("pursue", "direct",
                    "stopping-distance", "1.4"),
                Selection(
                    "attack-melee",
                    stpWeaponIntegration ? "one-hand" : "unarmed",
                    "animation-role-id", "npc.animation.bandit.attack",
                    "damage-event-id", "npc.event.damage-window"),
                Selection("search", "last-known-position",
                    "timeout-seconds", "8"),
                Selection("animation-role", "semantic-catalog",
                    "role-id", "npc.animation.bandit.idle")
            };
            if (stpWeaponIntegration)
                strategies.Add(Selection("equipment-threat", "draw-stow"));
            return strategies;
        }

        static List<NpcPriorityStateRecord> BanditStates(
            bool stpWeaponIntegration)
        {
            var combat = new List<string>
            {
                "npc.strategy.sense-visibility.cone-memory",
                "npc.strategy.sense-sound.radius",
                "npc.strategy.pursue.direct",
                "npc.strategy.attack-melee." +
                    (stpWeaponIntegration ? "one-hand" : "unarmed")
            };
            if (stpWeaponIntegration)
                combat.Add("npc.strategy.equipment-threat.draw-stow");
            return new List<NpcPriorityStateRecord>
            {
                State("npc.state.bandit.combat", 300,
                    "npc.condition.hostile-detected",
                    "npc.state.bandit.search",
                    combat.ToArray()),
                State("npc.state.bandit.search", 200,
                    "npc.condition.target-lost",
                    "npc.state.bandit.patrol",
                    "npc.strategy.search.last-known-position"),
                State("npc.state.bandit.patrol", 100,
                    "npc.condition.always",
                    null,
                    "npc.strategy.patrol.sequence",
                    "npc.strategy.animation-role.semantic-catalog")
            };
        }

        static List<NpcStrategySelection> CompanionStrategies(
            bool stpWeaponIntegration)
        {
            var strategies = new List<NpcStrategySelection>
            {
                Selection("sense-visibility", "cone-memory",
                    "distance", "24"),
                Selection("pursue", "direct",
                    "stopping-distance", "1.5"),
                Selection(
                    "attack-melee",
                    stpWeaponIntegration ? "one-hand" : "unarmed",
                    "animation-role-id",
                    "npc.animation.companion.defend",
                    "damage-event-id",
                    "npc.event.damage-window"),
                Selection("companion-follow", "distance-band",
                    "distance", "3"),
                Selection("interaction", "range-gate",
                    "range", "3"),
                Selection("dialogue", "localized-line",
                    "localized-text-id",
                    "npc.dialogue.companion.ready"),
                Selection("animation-role", "semantic-catalog",
                    "role-id", "npc.animation.companion.idle")
            };
            if (stpWeaponIntegration)
                strategies.Add(Selection("equipment-threat", "draw-stow"));
            return strategies;
        }

        static List<NpcPriorityStateRecord> CompanionStates(
            bool stpWeaponIntegration)
        {
            var defend = new List<string>
            {
                "npc.strategy.sense-visibility.cone-memory",
                "npc.strategy.pursue.direct",
                "npc.strategy.attack-melee." +
                    (stpWeaponIntegration ? "one-hand" : "unarmed")
            };
            if (stpWeaponIntegration)
                defend.Add("npc.strategy.equipment-threat.draw-stow");
            return new List<NpcPriorityStateRecord>
            {
                State("npc.state.companion.defend", 300,
                    "npc.condition.ally-threatened",
                    "npc.state.companion.follow",
                    defend.ToArray()),
                State("npc.state.companion.interaction", 200,
                    "npc.condition.interaction-active",
                    "npc.state.companion.follow",
                    "npc.strategy.interaction.range-gate",
                    "npc.strategy.dialogue.localized-line"),
                State("npc.state.companion.follow", 100,
                    "npc.condition.always",
                    null,
                    "npc.strategy.companion-follow.distance-band",
                    "npc.strategy.animation-role.semantic-catalog")
            };
        }

        static NpcStrategySelection Selection(
            string semantic,
            string variant,
            params string[] parameterPairs)
        {
            var selection = new NpcStrategySelection
            {
                SemanticBehaviorId = "npc.semantic." + semantic,
                FamilyId = "npc.family." + semantic,
                StrategyStableId =
                    "npc.strategy." + semantic + "." + variant
            };
            for (int index = 0;
                 index + 1 < parameterPairs.Length;
                 index += 2)
            {
                selection.ParameterOverrides[parameterPairs[index]] =
                    parameterPairs[index + 1];
            }
            return selection;
        }

        static NpcPriorityStateRecord State(
            string stateId,
            int priority,
            string entryConditionId,
            string fallbackStateId,
            params string[] strategyStableIds)
        {
            return new NpcPriorityStateRecord
            {
                StateId = stateId,
                Priority = priority,
                EntryConditionId = entryConditionId,
                FallbackStateId = fallbackStateId,
                StrategyStableIds = new List<string>(
                    strategyStableIds ?? new string[0])
            };
        }

        static NpcDefinition Base(
            string stableId,
            NpcRole role,
            string nameKey,
            string fallbackName,
            string factionId,
            string behaviorRecipeId)
        {
            var definition = new NpcDefinition
            {
                StableId = stableId,
                Role = role,
                DisplayNameLocalizationKey = nameKey,
                EnglishDisplayNameFallback = fallbackName,
                Archetype = role.ToString().ToLowerInvariant(),
                ArchetypeProfile = new NpcArchetypeProfileRecord
                {
                    ProfileId = "npc.archetype.humanoid." +
                        role.ToString().ToLowerInvariant(),
                    Archetype = NpcArchetypeClass.Humanoid,
                    SpeciesId = "species.human",
                    RigLogicalId = "rig.humanoid",
                    AvatarLogicalId =
                        "nwb.avatar.stp-static-male-survivor",
                    NavigationAgentLogicalId =
                        "nwb.nav-agent.humanoid",
                    LocomotionFamilyId =
                        "npc.locomotion.humanoid.ground",
                    SensorOriginLogicalId =
                        "npc.sensor-origin.head",
                    SensorHeight = 1.65m,
                    ColliderRadius = 0.35m,
                    ColliderHeight = 1.8m,
                    BoundsWidth = 0.7m,
                    BoundsHeight = 1.8m,
                    BoundsLength = 0.7m,
                    SourceProviderId =
                        NpcCharacterSourceProviderIds.StpStaticMale,
                    PreferBundledAnimations = false,
                    AllowCrossPackAnimationFallback = true
                },
                FactionId = factionId,
                BehaviorRecipeId = behaviorRecipeId,
                CharacterSource = new NpcCharacterSourceSelection
                {
                    ProviderId = NpcCharacterSourceProviderIds.StpStaticMale,
                    SourceLogicalId = "nwb.character-source.stp-static-male-survivor",
                    ModelLogicalId = "nwb.visual.stp-static-male-survivor",
                    AvatarLogicalId = "nwb.avatar.stp-static-male-survivor",
                    RequiredAnimationTags = new List<string> { "ual.core:tag/humanoid" },
                    RequireHumanoidAvatar = true,
                    RequireEquipmentCompatibility = false
                },
                Appearance = new NpcAppearanceLoadoutRecord
                {
                    DefaultCharacterSourceLogicalId =
                        "nwb.character-source.stp-static-male-survivor"
                },
                Visual = new NpcVisualReference
                {
                    ModelLogicalId = "nwb.visual.stp-static-male-survivor",
                    RigProfileId = "rig.humanoid",
                    AnimationCatalogLogicalId = "nwb.animation.default-npc"
                },
                Navigation = new NpcNavigationSelection
                {
                    AgentLogicalId = "nwb.nav-agent.humanoid",
                    ResolvedProjectAgentTypeId = 0
                }
            };
            AddCapability(definition, NpcCapabilityIds.Identity);
            AddCapability(definition, NpcCapabilityIds.Visual, NpcCapabilityIds.Identity);
            AddCapability(definition, NpcCapabilityIds.Animator, NpcCapabilityIds.Visual);
            AddCapability(definition, NpcCapabilityIds.Faction, NpcCapabilityIds.Identity);
            AddCapability(definition, NpcCapabilityIds.Navigation, NpcCapabilityIds.Identity);
            AddCapability(definition, NpcCapabilityIds.Idle, NpcCapabilityIds.Animator);
            definition.InputActions.AddRange(DefaultInputActions());
            return definition;
        }

        static void ConfigureWeapon(
            NpcDefinition definition,
            string roleName,
            bool stpWeaponIntegration)
        {
            if (!stpWeaponIntegration)
            {
                definition.WeaponLoadout = new NpcWeaponLoadoutRecord
                {
                    IntegrationChoice = NpcWeaponIntegrationChoice.Disabled,
                    HolsterPolicy = NpcWeaponHolsterPolicy.DrawOnThreatStowWhenSafe
                };
                return;
            }

            AddCapability(definition, NpcCapabilityIds.WeaponLoadout, NpcCapabilityIds.Animator);
            AddCapability(definition, NpcCapabilityIds.WeaponEquip, NpcCapabilityIds.WeaponLoadout);
            AddCapability(definition, NpcCapabilityIds.WeaponStow, NpcCapabilityIds.WeaponLoadout);
            AddCapability(definition, NpcCapabilityIds.WeaponDraw, NpcCapabilityIds.WeaponEquip);
            AddCapability(definition, NpcCapabilityIds.WeaponAttack, NpcCapabilityIds.WeaponDraw, NpcCapabilityIds.CombatMelee);
            AddRequiredIntegration(definition, NpcIntegrationIds.StpWeaponIntegration);
            definition.CharacterSource.RequireEquipmentCompatibility = true;
            const string equipmentItemId = "nwb.equipment.hqfps.combat-knife";
            definition.Appearance.Parts.Add(new NpcAppearancePartSelection
            {
                Slot = NpcEquipmentSlot.SecondaryWeapon,
                ItemStableId = equipmentItemId,
                ReplaceDefault = true
            });

            string rolePrefix = "npc.animation." + roleName + ".weapon.";
            definition.WeaponLoadout = new NpcWeaponLoadoutRecord
            {
                LoadoutId = "npc.weapon-loadout." + roleName,
                IntegrationChoice = NpcWeaponIntegrationChoice.Stp,
                WeaponItemLogicalId = "stp.item.hqfps-combat-knife",
                EquipmentItemStableId = equipmentItemId,
                HolsterSocketLogicalId = "stp.socket.secondary-holster",
                HolsterPolicy = NpcWeaponHolsterPolicy.DrawOnThreatStowWhenSafe,
                EquipAnimationRoleId = rolePrefix + "equip",
                DrawAnimationRoleId = rolePrefix + "draw",
                AttackAnimationRoleId = "npc.animation." + roleName +
                    (roleName == "bandit" ? ".attack" : ".defend"),
                StowAnimationRoleId = rolePrefix + "stow"
            };
            definition.AnimationRequirements.Add(Animation(rolePrefix + "equip", null, false, "ual.core:tag/equipment"));
            definition.AnimationRequirements.Add(Animation(rolePrefix + "draw", null, false, "ual.core:tag/equipment"));
            definition.AnimationRequirements.Add(Animation(rolePrefix + "stow", null, false, "ual.core:tag/equipment"));
        }

        static void AddCapability(NpcDefinition definition, string id, params string[] requires)
        {
            definition.Capabilities.Add(new NpcCapabilityRecipe
            {
                CapabilityId = id,
                Requires = new List<string>(requires ?? new string[0]),
                OwnedFields = new List<string> { "npc." + id },
                ValidationRules = new List<string> { "present-and-linked" }
            });
        }

        static void AddRequiredIntegration(NpcDefinition definition, string integrationId)
        {
            definition.Integrations.Add(new NpcIntegrationRequest
            {
                IntegrationId = integrationId,
                Mode = NpcIntegrationMode.Required
            });
        }

        static NpcInteractionActionRecord Action(string id, string key, string fallback)
        {
            return new NpcInteractionActionRecord
            {
                ActionId = id,
                PromptLocalizationKey = key,
                EnglishPromptFallback = fallback,
                Range = 3m
            };
        }

        static NpcDialogueLineRecord Line(string id, string key, string fallback, string voiceKey)
        {
            return new NpcDialogueLineRecord
            {
                LineId = id,
                SpeakerLocalizationKey = "npc.speaker." + id.Split('.')[1],
                SubtitleLocalizationKey = key,
                EnglishSourceFallback = fallback,
                VoiceLocalizationKey = voiceKey,
                CooldownSeconds = 2m,
                Weight = 1m
            };
        }

        static NpcCommerceOfferRecord Offer(string id, string item, int price, int stock)
        {
            return new NpcCommerceOfferRecord
            {
                OfferId = id,
                ItemLogicalId = item,
                QuantityPerPurchase = 1,
                InitialStock = stock,
                CurrencyItemLogicalId = "stp.item.metal-scrap",
                Price = price,
                LabelLocalizationKey = id + ".label",
                EnglishLabelFallback = id.Substring(id.LastIndexOf('.') + 1)
            };
        }

        static NpcAnimationRoleRequirement Animation(
            string role,
            string motion,
            bool required,
            params string[] tags)
        {
            return new NpcAnimationRoleRequirement
            {
                RoleId = role,
                MotionId = motion,
                Required = required,
                RequiredTags = new List<string>(tags ?? new string[0]),
                SafeFallbackRoleId = required ? "npc.animation.shared.idle" : null
            };
        }

        static NpcBehaviorTaskRequirement Task(
            string block,
            string type,
            string pack,
            params string[] variables)
        {
            return new NpcBehaviorTaskRequirement
            {
                BlockId = block,
                AssemblyQualifiedTaskType = type,
                PackIntegrationId = pack,
                RequiredSharedVariables = new List<string>(variables ?? new string[0])
            };
        }

        static IEnumerable<NpcInputActionRecord> DefaultInputActions()
        {
            return new[]
            {
                Input(NpcActionIds.PrimaryInteract, "world", "<Keyboard>/e", "<Gamepad>/buttonSouth"),
                Input(NpcActionIds.Talk, "world", "<Keyboard>/y", "<Gamepad>/buttonNorth"),
                Input(NpcActionIds.Trade, "world", "<Keyboard>/r", "<Gamepad>/buttonWest"),
                Input(NpcActionIds.CompanionCommand, "world", "<Keyboard>/c", "<Gamepad>/dpad/up"),
                Input(NpcActionIds.DialogueNext, "dialogue", "<Keyboard>/space", "<Gamepad>/buttonSouth"),
                Input(NpcActionIds.DialogueCancel, "dialogue", "<Keyboard>/escape", "<Gamepad>/buttonEast")
            };
        }

        static NpcInputActionRecord Input(
            string id,
            string context,
            string keyboard,
            string gamepad)
        {
            return new NpcInputActionRecord
            {
                ActionId = id,
                ContextId = context,
                KeyboardDefaultPath = keyboard,
                GamepadDefaultPath = gamepad
            };
        }
    }
}
