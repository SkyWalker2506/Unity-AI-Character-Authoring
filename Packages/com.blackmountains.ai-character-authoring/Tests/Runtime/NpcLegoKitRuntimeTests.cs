using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BlackMountains.AICharacterAuthoring.Tests
{
    public sealed class NpcLegoKitRuntimeTests
    {
        [TestCase(NpcRole.Vendor)]
        [TestCase(NpcRole.Bandit)]
        [TestCase(NpcRole.Companion)]
        public void SampleRecipe_CompilesDeterministically(NpcRole role)
        {
            var definition = Definition(role);
            var behavior = NpcSampleRecipeCatalog.CreateBehaviorRecipe(role);
            var context = CompleteContext(definition, behavior);

            var first = NpcRecipePlanner.Compile(definition, behavior, context);
            var second = NpcRecipePlanner.Compile(definition, behavior, context);

            Assert.That(first.Success, Is.True, Diagnostics(first.Diagnostics));
            Assert.That(second.Success, Is.True, Diagnostics(second.Diagnostics));
            Assert.That(second.Plan.DeterministicHash, Is.EqualTo(first.Plan.DeterministicHash));
            Assert.That(second.Plan.OrderedCapabilityIds, Is.EqualTo(first.Plan.OrderedCapabilityIds));
            Assert.That(first.Plan.OrderedCapabilityIds.IndexOf(NpcCapabilityIds.Navigation),
                Is.LessThan(first.Plan.OrderedCapabilityIds.IndexOf(
                    role == NpcRole.Companion ? NpcCapabilityIds.Follow :
                    role == NpcRole.Bandit ? NpcCapabilityIds.Patrol :
                    NpcCapabilityIds.Wander)));
        }

        [Test]
        public void RequiredIntegration_Missing_FailsBeforePlan()
        {
            var definition = NpcSampleRecipeCatalog.CreateVendor();
            var behavior = NpcSampleRecipeCatalog.CreateBehaviorRecipe(NpcRole.Vendor);
            var context = CompleteContext(definition, behavior);
            context.Integrations[NpcIntegrationIds.StpInventoryCommerce] = false;

            var result = NpcRecipePlanner.Compile(definition, behavior, context);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(HasCode(result.Diagnostics, "ACA-NPC-INTEGRATION-MISSING"), Is.True);
        }

        [Test]
        public void CapabilityConflict_IsRejected()
        {
            var definition = NpcSampleRecipeCatalog.CreateCompanion();
            definition.Capabilities[0].Conflicts.Add(NpcCapabilityIds.Dialogue);
            var behavior = NpcSampleRecipeCatalog.CreateBehaviorRecipe(NpcRole.Companion);

            var result = NpcRecipePlanner.Compile(
                definition,
                behavior,
                CompleteContext(definition, behavior));

            Assert.That(result.Success, Is.False);
            Assert.That(HasCode(result.Diagnostics, "ACA-NPC-CAPABILITY-CONFLICT"), Is.True);
        }

        [Test]
        public void InputBindingConflict_IsScopedByContext()
        {
            var definition = NpcSampleRecipeCatalog.CreateVendor();
            var behavior = NpcSampleRecipeCatalog.CreateBehaviorRecipe(NpcRole.Vendor);
            var context = CompleteContext(definition, behavior);

            Assert.That(NpcDefinitionValidator.Validate(definition, behavior, context).Success, Is.True);

            definition.InputActions[1].KeyboardDefaultPath =
                definition.InputActions[0].KeyboardDefaultPath;
            definition.InputActions[1].ContextId =
                definition.InputActions[0].ContextId;

            var invalid = NpcDefinitionValidator.Validate(definition, behavior, context);
            Assert.That(invalid.Success, Is.False);
            Assert.That(HasCode(invalid.Diagnostics, "ACA-NPC-INPUT-CONFLICT"), Is.True);
        }

        [Test]
        public void DialogueFallback_PrefersLocalizedTextThenEnglishSource()
        {
            Assert.That(NpcDialogueFallback.Resolve("Merhaba", "Hello"), Is.EqualTo("Merhaba"));
            Assert.That(NpcDialogueFallback.Resolve(string.Empty, "Hello"), Is.EqualTo("Hello"));
            Assert.That(NpcDialogueFallback.Resolve(null, null), Is.EqualTo(string.Empty));
        }

        [Test]
        public void StpWeaponFlag_IsExplicitAndChangesDeterministicPlan()
        {
            var disabled = NpcSampleRecipeCatalog.CreateBandit(false);
            var enabled = NpcSampleRecipeCatalog.CreateBandit(true);
            var disabledBehavior =
                NpcSampleRecipeCatalog.CreateBehaviorRecipe(
                    NpcRole.Bandit,
                    false);
            var enabledBehavior =
                NpcSampleRecipeCatalog.CreateBehaviorRecipe(
                    NpcRole.Bandit,
                    true);

            var disabledPlan = NpcRecipePlanner.Compile(
                disabled,
                disabledBehavior,
                CompleteContext(disabled, disabledBehavior));
            var enabledPlan = NpcRecipePlanner.Compile(
                enabled,
                enabledBehavior,
                CompleteContext(enabled, enabledBehavior));

            Assert.That(disabledPlan.Success, Is.True, Diagnostics(disabledPlan.Diagnostics));
            Assert.That(enabledPlan.Success, Is.True, Diagnostics(enabledPlan.Diagnostics));
            Assert.That(enabledPlan.Plan.WeaponIntegrationChoice,
                Is.EqualTo(NpcWeaponIntegrationChoice.Stp));
            Assert.That(enabledPlan.Plan.OrderedCapabilityIds,
                Does.Contain(NpcCapabilityIds.WeaponAttack));
            Assert.That(
                enabledPlan.Plan.ResolvedStrategies.Exists(
                    item => item.StrategyStableId ==
                        "npc.strategy.attack-melee.one-hand"),
                Is.True);
            Assert.That(
                disabledPlan.Plan.ResolvedStrategies.Exists(
                    item => item.StrategyStableId ==
                        "npc.strategy.attack-melee.unarmed"),
                Is.True);
            Assert.That(enabledPlan.Plan.DeterministicHash,
                Is.Not.EqualTo(disabledPlan.Plan.DeterministicHash));
        }

        [Test]
        public void BehaviorLegoCatalog_ContainsRequestedVariantFamilies()
        {
            var catalog = NpcBuiltInBehaviorLegoCatalog.Create();
            var diagnostics = new DiagnosticList();
            NpcBehaviorLegoCatalogValidator.Validate(catalog, diagnostics);

            Assert.That(diagnostics.HasErrors, Is.False, Diagnostics(diagnostics));
            Assert.That(StrategyCount(catalog, "npc.family.wander"), Is.EqualTo(4));
            Assert.That(StrategyCount(catalog, "npc.family.patrol"), Is.EqualTo(6));
            Assert.That(StrategyCount(catalog, "npc.family.attack-melee"), Is.EqualTo(6));
            Assert.That(StrategyCount(catalog, "npc.family.attack-ranged"), Is.EqualTo(6));
            Assert.That(StrategyCount(catalog, "npc.family.attack-hybrid"), Is.EqualTo(2));
        }

        [Test]
        public void ExactStrategySelection_UsesOverridesAndRejectsVersionDrift()
        {
            var definition = NpcSampleRecipeCatalog.CreateVendor();
            var behavior = NpcSampleRecipeCatalog.CreateBehaviorRecipe(
                NpcRole.Vendor);
            var context = CompleteContext(definition, behavior);

            var valid = NpcRecipePlanner.Compile(
                definition,
                behavior,
                context);
            Assert.That(valid.Success, Is.True, Diagnostics(valid.Diagnostics));
            var wander = valid.Plan.ResolvedStrategies.Find(
                item => item.StrategyStableId ==
                    "npc.strategy.wander.uniform-radius");
            Assert.That(wander.Parameters["radius"], Is.EqualTo("4"));
            Assert.That(wander.Parameters["seed"], Is.EqualTo("1101"));

            behavior.Strategies[0].StrategyVersion = "999";
            var invalid = NpcRecipePlanner.Compile(
                definition,
                behavior,
                context);
            Assert.That(invalid.Success, Is.False);
            Assert.That(HasCode(
                invalid.Diagnostics,
                "ACA-NPC-STRATEGY-EXACT-VARIANT-MISSING"), Is.True);
        }

        [Test]
        public void ResourceManifest_RequiredOptionalAndFallbackRulesAreExplicit()
        {
            var definition = NpcSampleRecipeCatalog.CreateBandit(true);
            var behavior = NpcSampleRecipeCatalog.CreateBehaviorRecipe(
                NpcRole.Bandit,
                true);
            var context = CompleteContext(definition, behavior);
            var valid = NpcRecipePlanner.Compile(
                definition,
                behavior,
                context);

            Assert.That(valid.Success, Is.True, Diagnostics(valid.Diagnostics));
            Assert.That(HasCode(
                valid.Diagnostics,
                "ACA-NPC-RESOURCE-OPTIONAL-MISSING"), Is.True);
            Assert.That(
                valid.Plan.ResourceManifest.Exists(
                    item => item.BindingId ==
                        "npc.binding.combat.weapon" &&
                        item.UsedFallback),
                Is.True);

            context.AvailableResources.RemoveAll(
                item => item.Kind ==
                    NpcResourceKind.AnimationEventMarker);
            var missingRequired = NpcRecipePlanner.Compile(
                definition,
                behavior,
                context);
            Assert.That(missingRequired.Success, Is.False);
            Assert.That(HasCode(
                missingRequired.Diagnostics,
                "ACA-NPC-RESOURCE-REQUIRED-MISSING"), Is.True);
        }

        [Test]
        public void CustomCapabilityPolicy_RequiresDiscoveryAndContractEvidence()
        {
            var missing = NpcCustomCapabilityPolicy.Validate(null);
            Assert.That(HasCode(
                missing,
                "ACA-NPC-CUSTOM-DISCOVERY-REQUIRED"), Is.True);

            var evidence = new NpcCustomCapabilityDiscoveryEvidence
            {
                RequestedCapabilityId = "npc.custom.test",
                SearchedProviderIds = new List<string>(
                    NpcCustomCapabilityPolicy.RequiredDiscoveryProviders),
                CustomProviderId = "provider.test.custom",
                CustomCapabilityVersion = "1",
                MigrationId = "migration.test.custom-v1",
                FallbackCapabilityId = "npc.idle",
                HasContractTests = true
            };
            Assert.That(
                NpcCustomCapabilityPolicy.Validate(evidence).HasErrors,
                Is.False);
            evidence.EquivalentCapabilityIds.Add("npc.move.wander");
            Assert.That(HasCode(
                NpcCustomCapabilityPolicy.Validate(evidence),
                "ACA-NPC-CUSTOM-EQUIVALENT-EXISTS"), Is.True);
        }

        [Test]
        public void QuadrupedArchetype_ValidatesRigNavigationAndBundledRoles()
        {
            var definition = NpcSampleRecipeCatalog.CreateBandit();
            definition.ArchetypeProfile =
                NpcEcosystemSampleCatalog.Deer();
            definition.CharacterSource.RequireHumanoidAvatar = false;
            definition.CharacterSource.ProviderId =
                definition.ArchetypeProfile.SourceProviderId;
            definition.Navigation.AgentLogicalId =
                "nwb.nav-agent.quadruped";
            definition.AnimationRequirements.Clear();
            foreach (var role in
                definition.ArchetypeProfile.RequiredAnimationRoleIds)
            {
                definition.AnimationRequirements.Add(
                    new NpcAnimationRoleRequirement
                    {
                        RoleId = role,
                        MotionId = "animal-pack-deluxe:" + role,
                        Required = true
                    });
            }
            var diagnostics = new DiagnosticList();

            NpcArchetypeEcosystemValidator.Validate(
                definition,
                diagnostics);
            Assert.That(
                diagnostics.HasErrors,
                Is.False,
                Diagnostics(diagnostics));

            definition.Navigation.AgentLogicalId =
                "nwb.nav-agent.humanoid";
            diagnostics = new DiagnosticList();
            NpcArchetypeEcosystemValidator.Validate(
                definition,
                diagnostics);
            Assert.That(HasCode(
                diagnostics,
                "ACA-NPC-ARCHETYPE-NAVIGATION"), Is.True);
        }

        [Test]
        public void WildlifeFactory_LocksSpeciesToBundledProviderAnimations()
        {
            var source = new NpcSpeciesSourceDescriptor
            {
                ProviderId =
                    "provider.nwb.species.animal-pack-deluxe",
                SourceLogicalId =
                    "nwb.species-source.wolf",
                SpeciesId = "species.wolf",
                Archetype =
                    NpcArchetypeClass.Quadruped,
                DisplayName = "Wolf",
                ModelLogicalId = "nwb.visual.wolf",
                RigLogicalId = "rig.quadruped.wolf",
                AvatarLogicalId = "avatar.wolf",
                BundledAnimationCompatible = true,
                BundledAnimationRoleIds =
                    new List<string>
                    {
                        "npc.animation.wolf.idle",
                        "npc.animation.wolf.walk",
                        "npc.animation.wolf.attack"
                    }
            };

            var definition =
                NpcSampleRecipeCatalog.CreateWildlife(
                    source,
                    NpcEcologicalDisposition.Predator);
            var diagnostics = new DiagnosticList();
            NpcArchetypeEcosystemValidator.Validate(
                definition,
                diagnostics);

            Assert.That(
                diagnostics.HasErrors,
                Is.False,
                Diagnostics(diagnostics));
            Assert.That(
                definition.Role,
                Is.EqualTo(NpcRole.Wildlife));
            Assert.That(
                definition.ArchetypeProfile
                    .PreferBundledAnimations,
                Is.True);
            Assert.That(
                definition.ArchetypeProfile
                    .AllowCrossPackAnimationFallback,
                Is.False);
            Assert.That(
                definition.CharacterSource.ProviderId,
                Is.EqualTo(source.ProviderId));
            Assert.That(
                definition.AnimationRequirements,
                Has.All.Property("RoleId")
                    .StartsWith("npc.animation.wolf."));

            definition.ArchetypeProfile
                .AllowCrossPackAnimationFallback = true;
            diagnostics = new DiagnosticList();
            NpcArchetypeEcosystemValidator.Validate(
                definition,
                diagnostics);
            Assert.That(
                HasCode(
                    diagnostics,
                    "ACA-NPC-ARCHETYPE-CROSS-PACK-FORBIDDEN"),
                Is.True);
        }

        [Test]
        public void EcosystemEncounter_IsBudgetedAndDeterministic()
        {
            var recipe = NpcEcosystemSampleCatalog.ForestEdge();
            var firstDiagnostics = new DiagnosticList();
            var secondDiagnostics = new DiagnosticList();
            var first = NpcEncounterPlanner.Compile(
                recipe,
                firstDiagnostics);
            var second = NpcEncounterPlanner.Compile(
                recipe,
                secondDiagnostics);

            Assert.That(firstDiagnostics.HasErrors, Is.False);
            Assert.That(secondDiagnostics.HasErrors, Is.False);
            Assert.That(
                second.DeterministicHash,
                Is.EqualTo(first.DeterministicHash));
            Assert.That(
                first.OrderedParticipants[0].SpeciesId,
                Is.EqualTo("species.deer"));

            recipe.TotalBudget = 4;
            var invalidDiagnostics = new DiagnosticList();
            Assert.That(
                NpcEncounterPlanner.Compile(
                    recipe,
                    invalidDiagnostics),
                Is.Null);
            Assert.That(HasCode(
                invalidDiagnostics,
                "ACA-NPC-ENCOUNTER-BUDGET"), Is.True);
        }

        [Test]
        public void VariationSelection_PrefersBundledSamePackAndHonorsSuppression()
        {
            var request = new NpcVariationRequestRecord
            {
                RequestVariations = true,
                Status = NpcVariationDiscoveryStatus.Completed,
                Candidates = new List<NpcVariationCandidate>
                {
                    Candidate(
                        "npc.variation.external-high",
                        "provider.external",
                        0.95m,
                        false),
                    Candidate(
                        "npc.variation.same-pack",
                        "provider.animals",
                        0.8m,
                        true)
                }
            };

            Assert.That(
                NpcVariationSelector.Select(
                    request,
                    "provider.animals",
                    "rig.quadruped.deer").CandidateId,
                Is.EqualTo("npc.variation.same-pack"));

            request.SuppressedCandidateIds.Add(
                "npc.variation.same-pack");
            Assert.That(
                NpcVariationSelector.Select(
                    request,
                    "provider.animals",
                    "rig.quadruped.deer").CandidateId,
                Is.EqualTo("npc.variation.external-high"));
        }

        [Test]
        public void VariationRequest_NoCompatibleResultRequiresStatusNote()
        {
            var definition = NpcSampleRecipeCatalog.CreateVendor();
            definition.VariationRequest =
                new NpcVariationRequestRecord
                {
                    RequestVariations = true,
                    Status =
                        NpcVariationDiscoveryStatus
                            .NoCompatibleVariant
                };
            var diagnostics = new DiagnosticList();

            NpcArchetypeEcosystemValidator.Validate(
                definition,
                diagnostics);

            Assert.That(HasCode(
                diagnostics,
                "ACA-NPC-VARIATION-NO-MATCH-NOTE"), Is.True);
        }

        [Test]
        public void StpWeaponChoice_RequiresIntegrationAndCompatibleSource()
        {
            var definition = NpcSampleRecipeCatalog.CreateCompanion(true);
            var behavior = NpcSampleRecipeCatalog.CreateBehaviorRecipe(
                NpcRole.Companion);
            var context = CompleteContext(definition, behavior);
            context.Integrations[NpcIntegrationIds.StpWeaponIntegration] = false;
            definition.CharacterSource.RequireEquipmentCompatibility = false;

            var result = NpcRecipePlanner.Compile(definition, behavior, context);

            Assert.That(result.Success, Is.False);
            Assert.That(HasCode(result.Diagnostics, "ACA-NPC-INTEGRATION-MISSING"), Is.True);
            Assert.That(HasCode(result.Diagnostics, "ACA-NPC-WEAPON-SOURCE-COMPATIBILITY"), Is.True);
        }

        [Test]
        public void ModularEquipmentSelector_IsStableAndMetadataDriven()
        {
            var catalog = new NpcModularEquipmentCatalog
            {
                StableId = "catalog.test",
                Items = new List<NpcModularEquipmentItemRecord>
                {
                    Weapon("equipment.test.zulu", "variant.zulu"),
                    Weapon("equipment.test.alpha", "variant.alpha")
                }
            };
            var query = new NpcModularEquipmentQuery
            {
                Slot = NpcEquipmentSlot.SecondaryWeapon,
                RigId = "rig.humanoid",
                RequiredTags = new List<string> { "weapon.melee" },
                RequiredStyleTags = new List<string> { "style.realistic" },
                RequiredAnimationCompatibilityTags =
                    new List<string> { "weapon.animation.one-handed" }
            };

            var first = NpcModularEquipmentSelector.Select(catalog, query);
            var second = NpcModularEquipmentSelector.Select(catalog, query);

            Assert.That(first.Success, Is.True);
            Assert.That(first.Selected.StableId, Is.EqualTo("equipment.test.alpha"));
            Assert.That(second.Selected.StableId, Is.EqualTo(first.Selected.StableId));
        }

        [Test]
        public void ModularEquipmentCatalog_UnresolvedDependencyFailsClosed()
        {
            var item = Weapon("equipment.test.knife", "variant.knife");
            item.Dependencies.Add("equipment.test.missing-sheath");
            var catalog = new NpcModularEquipmentCatalog
            {
                StableId = "catalog.test",
                Items = new List<NpcModularEquipmentItemRecord> { item }
            };

            var result = NpcModularEquipmentSelector.Select(
                catalog,
                new NpcModularEquipmentQuery
                {
                    Slot = NpcEquipmentSlot.SecondaryWeapon,
                    RigId = "rig.humanoid"
                });

            Assert.That(result.Success, Is.False);
            Assert.That(HasCode(result.Diagnostics, "ACA-NPC-EQUIPMENT-DEPENDENCY"), Is.True);
        }

        [Test]
        public void CommerceTransaction_SuccessMutatesBothSidesExactly()
        {
            var inventory = new FakeInventory();
            inventory.Set("currency", 10);
            var offer = Offer();

            var result = NpcCommerceTransaction.Buy(offer, inventory, availableStock: 3);

            Assert.That(result.Code, Is.EqualTo(NpcCommerceResultCode.Success));
            Assert.That(inventory.Count("currency"), Is.EqualTo(6));
            Assert.That(inventory.Count("item"), Is.EqualTo(2));
        }

        [Test]
        public void CommerceTransaction_PartialDestinationAddRollsBackEverything()
        {
            var inventory = new FakeInventory { PartialAddItemId = "item", PartialAddCount = 1 };
            inventory.Set("currency", 10);
            var offer = Offer();

            var result = NpcCommerceTransaction.Buy(offer, inventory, availableStock: 3);

            Assert.That(result.Code, Is.EqualTo(NpcCommerceResultCode.DestinationRejected));
            Assert.That(inventory.Count("currency"), Is.EqualTo(10));
            Assert.That(inventory.Count("item"), Is.Zero);
        }

        [Test]
        public void CommerceTransaction_RollbackFailureIsExplicit()
        {
            var inventory = new FakeInventory
            {
                PartialAddItemId = "item",
                PartialAddCount = 1,
                RejectRemovalItemId = "item"
            };
            inventory.Set("currency", 10);

            var result = NpcCommerceTransaction.Buy(Offer(), inventory, availableStock: 3);

            Assert.That(result.Code, Is.EqualTo(NpcCommerceResultCode.RollbackFailed));
            Assert.That(result.Success, Is.False);
        }

        static NpcCommerceOfferRecord Offer()
        {
            return new NpcCommerceOfferRecord
            {
                OfferId = "offer.test",
                ItemLogicalId = "item",
                QuantityPerPurchase = 2,
                InitialStock = 3,
                CurrencyItemLogicalId = "currency",
                Price = 4
            };
        }

        static NpcModularEquipmentItemRecord Weapon(
            string stableId,
            string variant)
        {
            return new NpcModularEquipmentItemRecord
            {
                StableId = stableId,
                SourceProviderId = "provider.test.equipment",
                Slot = NpcEquipmentSlot.SecondaryWeapon,
                VariantId = variant,
                Tags = new List<string> { "weapon.melee" },
                StyleTags = new List<string> { "style.realistic" },
                CompatibleRigIds = new List<string> { "rig.humanoid" },
                AnimationCompatibilityTags =
                    new List<string> { "weapon.animation.one-handed" },
                WeaponGrip = NpcWeaponGrip.OneHanded,
                ContactEventSemantic = "weapon.contact.test",
                HolsterSlotLogicalId = "weapon.holster.test"
            };
        }

        static NpcVariationCandidate Candidate(
            string id,
            string provider,
            decimal confidence,
            bool bundled)
        {
            return new NpcVariationCandidate
            {
                CandidateId = id,
                ProviderId = provider,
                SourceLogicalId = id + ".source",
                Provenance = "test",
                Confidence = confidence,
                RigLogicalId = "rig.quadruped.deer",
                AvatarLogicalId = "avatar.deer",
                UsesBundledAnimations = bundled,
                RigCompatible = true,
                AnimationCompatible = true
            };
        }

        static NpcDefinition Definition(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Vendor:
                    return NpcSampleRecipeCatalog.CreateVendor();
                case NpcRole.Bandit:
                    return NpcSampleRecipeCatalog.CreateBandit();
                default:
                    return NpcSampleRecipeCatalog.CreateCompanion();
            }
        }

        static NpcValidationContext CompleteContext(
            NpcDefinition definition,
            NpcBehaviorRecipe behavior)
        {
            var context = new NpcValidationContext();
            context.NavigationAgents[definition.Navigation.AgentLogicalId] =
                definition.Navigation.ResolvedProjectAgentTypeId.Value;
            foreach (var integration in definition.Integrations)
                context.Integrations[integration.IntegrationId] = true;
            foreach (var task in behavior.Tasks)
                context.AvailableBehaviorTaskTypes.Add(task.AssemblyQualifiedTaskType);
            context.AvailableResources.AddRange(
                NpcDefinitionResourceCatalog.Build(definition));
            return context;
        }

        static int StrategyCount(
            NpcBehaviorLegoCatalog catalog,
            string familyId)
        {
            int count = 0;
            foreach (var strategy in catalog.Strategies)
                if (strategy.FamilyId == familyId) count++;
            return count;
        }

        static bool HasCode(DiagnosticList diagnostics, string code)
        {
            foreach (var diagnostic in diagnostics.Items)
                if (string.Equals(diagnostic.Code, code, StringComparison.Ordinal))
                    return true;
            return false;
        }

        static string Diagnostics(DiagnosticList diagnostics)
        {
            var messages = new List<string>();
            foreach (var diagnostic in diagnostics.Items)
                messages.Add(diagnostic.Code + ": " + diagnostic.Message);
            return string.Join(Environment.NewLine, messages);
        }

        sealed class FakeInventory : INpcCommerceInventory
        {
            readonly Dictionary<string, int> _counts =
                new Dictionary<string, int>(StringComparer.Ordinal);

            public string PartialAddItemId { get; set; }
            public int PartialAddCount { get; set; }
            public string RejectRemovalItemId { get; set; }

            public int Count(string itemLogicalId)
            {
                return _counts.TryGetValue(itemLogicalId, out var count) ? count : 0;
            }

            public void Set(string itemLogicalId, int count)
            {
                _counts[itemLogicalId] = count;
            }

            public NpcInventoryMutation Add(string itemLogicalId, int amount)
            {
                int applied = string.Equals(itemLogicalId, PartialAddItemId, StringComparison.Ordinal)
                    ? Math.Min(amount, PartialAddCount)
                    : amount;
                Set(itemLogicalId, Count(itemLogicalId) + applied);
                return new NpcInventoryMutation
                {
                    AppliedCount = applied,
                    RejectionReason = applied == amount ? string.Empty : "fake-capacity"
                };
            }

            public NpcInventoryMutation Remove(string itemLogicalId, int amount)
            {
                if (string.Equals(itemLogicalId, RejectRemovalItemId, StringComparison.Ordinal))
                    return new NpcInventoryMutation { AppliedCount = 0, RejectionReason = "fake-remove-rejected" };
                int applied = Math.Min(amount, Count(itemLogicalId));
                Set(itemLogicalId, Count(itemLogicalId) - applied);
                return new NpcInventoryMutation { AppliedCount = applied };
            }
        }
    }
}
