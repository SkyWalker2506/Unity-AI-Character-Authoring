// WP-07 — NpcDefinition is the single authoritative character specification.
//
// This file does three things, in order of importance:
//
//   1. MEASURES whether NpcDefinition survives a plain Newtonsoft JSON round-trip. The design
//      review (character.md, risk R2) asserted that the data model "can live neither in Unity nor
//      in JSON" but never measured it. An unmeasured risk cannot be closed, and WP-08 is about to
//      build a compiler on top of this model. If the round-trip loses fields, the exact lost set
//      is printed by the failure message — that list is the deliverable, not the pass/fail bit.
//   2. Proves NpcAuthoringPlan is reachable from an Editor assembly. Before WP-07 it was
//      referenced by zero Editor files.
//   3. Pins the deprecation of the retired stack, including the requirement that each [Obsolete]
//      message names the replacement and the work package that finishes the migration.
//
// 618 is suppressed because measuring the retired CharacterSpec channel is the point of the
// AuthoringFieldValue contrast test.
#pragma warning disable 618

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using BlackMountains.AICharacterAuthoring.Editor;
using BlackMountains.AuthoringKernel;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BlackMountains.AICharacterAuthoring.Editor.Tests
{
    public sealed class NpcDefinitionAuthorityTests
    {
        // ---------------------------------------------------------------------------------
        // 1. The measurement: NpcDefinition -> JSON -> NpcDefinition
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// The headline WP-07 measurement. Every writable member of <see cref="NpcDefinition"/> and
        /// of every type it transitively owns is populated with a distinctive value, serialized
        /// with default Newtonsoft settings, read back, and compared property-by-property.
        /// </summary>
        /// <remarks>
        /// Default settings on purpose: the question is whether the model round-trips *unaided*.
        /// A model that needs custom converters to survive its own persistence format is a model
        /// that will silently drop authoring intent the first time anyone forgets the converter.
        /// </remarks>
        [Test]
        public void NpcDefinition_SurvivesPlainNewtonsoftRoundTrip()
        {
            var original = MaximalDefinition();

            var json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<NpcDefinition>(json);

            var losses = new List<string>();
            DeepCompare("NpcDefinition", original, restored, losses);

            TestContext.WriteLine("round-trip json length: " + json.Length);
            TestContext.WriteLine("field losses: " + losses.Count);
            foreach (var loss in losses)
                TestContext.WriteLine("  LOST: " + loss);

            Assert.That(
                losses,
                Is.Empty,
                "NpcDefinition did not survive a plain Newtonsoft round-trip. Lost/altered:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, losses.ToArray()));
        }

        /// <summary>
        /// The same measurement against the shipped sample recipes, which are the definitions the
        /// package actually produces today.
        /// </summary>
        [TestCase(NpcRole.Vendor)]
        [TestCase(NpcRole.Bandit)]
        [TestCase(NpcRole.Companion)]
        public void NpcDefinition_CatalogSamples_SurvivePlainNewtonsoftRoundTrip(NpcRole role)
        {
            var original = Sample(role);

            var restored = JsonConvert.DeserializeObject<NpcDefinition>(
                JsonConvert.SerializeObject(original));

            var losses = new List<string>();
            DeepCompare("NpcDefinition(" + role + ")", original, restored, losses);

            foreach (var loss in losses)
                TestContext.WriteLine("  LOST: " + loss);

            Assert.That(
                losses,
                Is.Empty,
                "Sample " + role + " did not survive a plain Newtonsoft round-trip:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, losses.ToArray()));
        }

        /// <summary>
        /// The domain plan is the artifact WP-08 has to persist and hash, so its round-trip is
        /// measured here too rather than discovered later.
        /// </summary>
        [Test]
        public void NpcAuthoringPlan_SurvivesPlainNewtonsoftRoundTrip()
        {
            var definition = Sample(NpcRole.Vendor);
            var behavior = NpcSampleRecipeCatalog.CreateBehaviorRecipe(NpcRole.Vendor);
            var compiled = NpcAuthoringPlannerFacade.CompileDomainPlan(
                definition, behavior, CompleteContext(definition, behavior));
            Assert.That(compiled.Success, Is.True, Describe(compiled.Diagnostics));

            var restored = JsonConvert.DeserializeObject<NpcAuthoringPlan>(
                JsonConvert.SerializeObject(compiled.Plan));

            var losses = new List<string>();
            DeepCompare("NpcAuthoringPlan", compiled.Plan, restored, losses);

            foreach (var loss in losses)
                TestContext.WriteLine("  LOST: " + loss);

            Assert.That(
                losses,
                Is.Empty,
                "NpcAuthoringPlan did not survive a plain Newtonsoft round-trip:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, losses.ToArray()));
            Assert.That(
                restored.DeterministicHash,
                Is.EqualTo(compiled.Plan.DeterministicHash),
                "Plan identity must survive persistence or approval tokens cannot be trusted.");
        }

        /// <summary>
        /// The contrast case, and the concrete reason <c>CharacterSpec</c> cannot be the authority.
        /// </summary>
        /// <remarks>
        /// <see cref="AuthoringFieldValue"/> is a readonly struct with a private constructor and
        /// get-only properties; Newtonsoft has no way to reconstruct one without a converter. This
        /// test asserts the defect deliberately. <b>When WP-20 adds the converter, this test must
        /// be inverted, not deleted</b> — it is the record of what was broken and when it stopped
        /// being broken.
        /// </remarks>
        [Test]
        public void Measured_AuthoringFieldValueDoesNotSurvivePlainNewtonsoftRoundTrip()
        {
            var original = AuthoringFieldValue.Known(CanonicalValue.String("intent-bearing-value"));
            var json = JsonConvert.SerializeObject(original);
            TestContext.WriteLine("AuthoringFieldValue json: " + json);

            string outcome;
            bool preserved;
            try
            {
                var restored = JsonConvert.DeserializeObject<AuthoringFieldValue>(json);
                preserved = restored.State == AuthoringValueState.Known && restored.Value != null;
                outcome = "deserialized to State=" + restored.State
                    + ", Value=" + (restored.Value == null ? "<null>" : restored.Value.Kind.ToString());
            }
            catch (Exception ex)
            {
                preserved = false;
                outcome = "threw " + ex.GetType().Name;
            }

            TestContext.WriteLine("AuthoringFieldValue round-trip outcome: " + outcome);

            Assert.That(
                preserved,
                Is.False,
                "AuthoringFieldValue now round-trips (" + outcome + "). If WP-20 fixed this, invert "
                    + "this assertion and update the CharacterSpec/NpcDefinition XML docs — do not "
                    + "delete the measurement.");
        }

        /// <summary>
        /// The same defect seen through the retired spec's actual parameter channel.
        /// </summary>
        [Test]
        public void Measured_CharacterSpecParametersChannelIsLostOnRoundTrip()
        {
            var spec = new CharacterSpec { SpecId = "spec.measurement" };
            spec.Parameters["height"] = AuthoringFieldValue.Known(CanonicalValue.Decimal(1.8m));
            spec.Capabilities.Add("cap.identity");

            string outcome;
            bool preserved;
            try
            {
                var restored = JsonConvert.DeserializeObject<CharacterSpec>(
                    JsonConvert.SerializeObject(spec));
                var value = restored.Parameters["height"];
                preserved = value.State == AuthoringValueState.Known && value.Value != null;
                outcome = "Parameters[\"height\"].State=" + value.State;
            }
            catch (Exception ex)
            {
                preserved = false;
                outcome = "threw " + ex.GetType().Name;
            }

            TestContext.WriteLine("CharacterSpec.Parameters round-trip outcome: " + outcome);

            Assert.That(
                preserved,
                Is.False,
                "CharacterSpec.Parameters now survives persistence (" + outcome + "). Update the "
                    + "WP-07 rationale in docs/AI/STATE.md if this changed.");
        }

        // ---------------------------------------------------------------------------------
        // 2. Reachability: NpcAuthoringPlan must be nameable from an Editor assembly
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Before WP-07 no Editor file referenced <see cref="NpcAuthoringPlan"/>, so the rich
        /// planner had no execution path to attach to. This test fails if that seam is deleted.
        /// </summary>
        [Test]
        public void AuthoritativePlanner_IsReachableFromTheEditorAssembly()
        {
            var facade = typeof(NpcAuthoringPlannerFacade);
            Assert.That(
                facade.Assembly.GetName().Name,
                Is.EqualTo("BlackMountains.AICharacterAuthoring.Editor"),
                "The planner seam must live in the Editor assembly; that is the whole point of it.");

            var definition = Sample(NpcRole.Companion);
            var behavior = NpcSampleRecipeCatalog.CreateBehaviorRecipe(NpcRole.Companion);

            var validation = NpcAuthoringPlannerFacade.ValidateDefinition(
                definition, behavior, CompleteContext(definition, behavior));
            var compiled = NpcAuthoringPlannerFacade.CompileDomainPlan(
                definition, behavior, CompleteContext(definition, behavior));

            Assert.That(validation.Success, Is.True, Describe(validation.Diagnostics));
            Assert.That(compiled.Success, Is.True, Describe(compiled.Diagnostics));
            Assert.That(compiled.Plan, Is.TypeOf<NpcAuthoringPlan>());
            Assert.That(compiled.Plan.NpcStableId, Is.EqualTo(definition.StableId));
        }

        /// <summary>
        /// The facade forwards; it does not decide. If a future change makes the facade produce a
        /// different plan than the planner it wraps, split authority has been reintroduced.
        /// </summary>
        [Test]
        public void PlannerFacade_ForwardsWithoutAlteringThePlan()
        {
            var definition = Sample(NpcRole.Bandit);
            var behavior = NpcSampleRecipeCatalog.CreateBehaviorRecipe(NpcRole.Bandit);

            var viaFacade = NpcAuthoringPlannerFacade.CompileDomainPlan(
                definition, behavior, CompleteContext(definition, behavior));
            var viaPlanner = NpcRecipePlanner.Compile(
                definition, behavior, CompleteContext(definition, behavior));

            Assert.That(viaFacade.Success, Is.True, Describe(viaFacade.Diagnostics));
            Assert.That(viaPlanner.Success, Is.True, Describe(viaPlanner.Diagnostics));
            Assert.That(
                viaFacade.Plan.DeterministicHash,
                Is.EqualTo(viaPlanner.Plan.DeterministicHash));

            var differences = new List<string>();
            DeepCompare("plan", viaPlanner.Plan, viaFacade.Plan, differences);
            Assert.That(
                differences,
                Is.Empty,
                "The Editor facade must be a pure forward:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, differences.ToArray()));
        }

        /// <summary>
        /// Tripwire. WP-08 must flip this together with the facade property; until then the honest
        /// answer to "can an NpcDefinition be applied?" is no.
        /// </summary>
        [Test]
        public void ExecutionPlanCompiler_IsStillAbsent()
        {
            Assert.That(
                NpcAuthoringPlannerFacade.ExecutionPlanCompilerAvailable,
                Is.False,
                "If the execution-plan compiler now exists, flip this test and the facade property "
                    + "in the same commit.");
            Assert.That(
                NpcAuthoringPlannerFacade.ExecutionPlanCompilerWorkPackage,
                Is.EqualTo("WP-08"));
        }

        // ---------------------------------------------------------------------------------
        // 2b. Negative control for the measurement instrument itself
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Proves <see cref="DeepCompare"/> can actually fail. Without this, "0 field losses" is
        /// indistinguishable from "the comparer walked nothing", and the headline WP-07 result
        /// would be worthless.
        /// </summary>
        /// <remarks>
        /// A reflection walker silently degrades: one wrong <see cref="BindingFlags"/>, one early
        /// return, and every round-trip test in this file goes green forever while measuring
        /// nothing. Each case below induces a known loss at a different structural depth — root
        /// scalar, nested record, list element, dictionary value, dropped dictionary key, nulled
        /// sub-object, list length — and asserts the loss is both detected and correctly located.
        /// </remarks>
        [Test]
        public void DeepCompare_DetectsInducedLossAtEveryStructuralDepth()
        {
            var control = new List<string>();
            DeepCompare("ctl", MaximalDefinition(), MaximalDefinition(), control);
            Assert.That(
                control,
                Is.Empty,
                "Two identically-built fixtures must compare equal, or every other case here is "
                    + "measuring construction noise instead of round-trip loss:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, control.ToArray()));

            AssertDetected("root scalar", "ctl.StableId", d => d.StableId = "mutated");
            AssertDetected(
                "nested record decimal",
                "ctl.Ecosystem.Needs.InitialHunger",
                d => d.Ecosystem.Needs.InitialHunger = 0.999m);
            AssertDetected(
                "list element scalar",
                "ctl.DialogueLines[0].Weight",
                d => d.DialogueLines[0].Weight = 0.001m);
            AssertDetected(
                "dictionary value",
                "ordinal-key",
                d => d.Capabilities[0].Parameters["ordinal-key"] = "mutated");
            AssertDetected(
                "dropped dictionary key",
                "missing after round-trip",
                d => d.Capabilities[0].Parameters.Remove("ordinal-key"));
            AssertDetected(
                "nulled sub-object",
                "ctl.Companion",
                d => d.Companion = null);
            AssertDetected(
                "list length",
                "ctl.DialogueLines",
                d => d.DialogueLines.RemoveAt(d.DialogueLines.Count - 1));
        }

        /// <summary>
        /// Applies <paramref name="induce"/> to one of two identical fixtures and asserts the
        /// comparer reports a loss whose path contains <paramref name="expectedPathFragment"/>.
        /// </summary>
        static void AssertDetected(
            string label,
            string expectedPathFragment,
            Action<NpcDefinition> induce)
        {
            var pristine = MaximalDefinition();
            var mutated = MaximalDefinition();
            induce(mutated);

            var losses = new List<string>();
            DeepCompare("ctl", pristine, mutated, losses);

            Assert.That(
                losses,
                Is.Not.Empty,
                "DeepCompare missed an induced loss (" + label + "). The round-trip measurements in "
                    + "this file are only trustworthy while this fails on purpose.");
            Assert.That(
                losses,
                Has.Some.Contains(expectedPathFragment),
                "DeepCompare detected a loss for '" + label + "' but at the wrong path. Reported:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, losses.ToArray()));
        }

        // ---------------------------------------------------------------------------------
        // 3. Deprecation of the retired stack
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// A deprecation that does not say where to go is a dead end. Each retired type must name
        /// <c>NpcDefinition</c> and the work package that completes the migration, and must be a
        /// warning rather than an error so pinned consumers keep compiling.
        /// </summary>
        [TestCase(typeof(CharacterSpec))]
        [TestCase(typeof(GenerationPlanCompiler))]
        public void RetiredTypes_AreObsoleteWithActionableGuidance(Type retired)
        {
            var attribute = (ObsoleteAttribute)Attribute.GetCustomAttribute(
                retired, typeof(ObsoleteAttribute));

            Assert.That(attribute, Is.Not.Null, retired.Name + " must be marked [Obsolete].");
            Assert.That(
                attribute.IsError,
                Is.False,
                retired.Name + " must warn, not error: Million Dollars Project pins this package.");
            Assert.That(
                attribute.Message,
                Does.Contain("NpcDefinition"),
                retired.Name + " deprecation must name the replacement authority.");
            Assert.That(
                attribute.Message,
                Does.Contain("WP-08"),
                retired.Name + " deprecation must say where the missing execution path lands.");
        }

        /// <summary>
        /// The authority itself must never be deprecated by accident.
        /// </summary>
        [Test]
        public void AuthoritativeTypes_AreNotObsolete()
        {
            foreach (var type in new[]
            {
                typeof(NpcDefinition),
                typeof(NpcAuthoringPlan),
                typeof(NpcRecipePlanner),
                typeof(NpcDefinitionValidator),
                typeof(NpcAuthoringPlannerFacade)
            })
            {
                Assert.That(
                    Attribute.GetCustomAttribute(type, typeof(ObsoleteAttribute)),
                    Is.Null,
                    type.Name + " is part of the authoritative path and must not be [Obsolete].");
            }
        }

        // ---------------------------------------------------------------------------------
        // Fixtures
        // ---------------------------------------------------------------------------------

        static NpcDefinition Sample(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Vendor: return NpcSampleRecipeCatalog.CreateVendor();
                case NpcRole.Bandit: return NpcSampleRecipeCatalog.CreateBandit(true);
                case NpcRole.Companion: return NpcSampleRecipeCatalog.CreateCompanion(true);
                default: throw new ArgumentOutOfRangeException(nameof(role));
            }
        }

        /// <summary>
        /// A definition that touches every branch of the model the samples leave empty — archetype
        /// profile, ecosystem, variation candidates, companion, weapon loadout, nullable ints,
        /// decimals with fractional parts, and an ordinal-comparer dictionary.
        /// </summary>
        static NpcDefinition MaximalDefinition()
        {
            var definition = NpcSampleRecipeCatalog.CreateCompanion(true);

            definition.StableId = "npc.measurement.maximal";
            definition.Archetype = "archetype.measurement";
            definition.EnglishDisplayNameFallback = "Maximal Measurement Subject";
            definition.Version = "7";

            definition.Navigation.ResolvedProjectAgentTypeId = 42;

            definition.ArchetypeProfile = new NpcArchetypeProfileRecord
            {
                ProfileId = "npc.archetype.measurement",
                Archetype = NpcArchetypeClass.Humanoid,
                SpeciesId = "species.measurement",
                RigLogicalId = "rig.humanoid",
                AvatarLogicalId = "avatar.measurement",
                NavigationAgentLogicalId = definition.Navigation.AgentLogicalId,
                LocomotionFamilyId = "locomotion.biped",
                SensorOriginLogicalId = "socket.head",
                SensorHeight = 1.66m,
                ColliderRadius = 0.33m,
                ColliderHeight = 1.77m,
                BoundsWidth = 0.71m,
                BoundsHeight = 1.81m,
                BoundsLength = 0.72m,
                SourceProviderId = NpcCharacterSourceProviderIds.StpStaticMale,
                PreferBundledAnimations = false,
                AllowCrossPackAnimationFallback = true,
                AttackContacts =
                {
                    new NpcAttackContactRecord
                    {
                        ContactId = "contact.right-hand",
                        TransformLogicalId = "socket.hand.right",
                        Radius = 0.125m,
                        AnimationEventSemantic = "npc.event.damage-window"
                    }
                },
                RequiredAnimationRoleIds = { "role.idle", "role.walk" }
            };

            definition.Ecosystem = new NpcEcosystemProfileRecord
            {
                Habitat = new NpcHabitatRecord
                {
                    HabitatId = "habitat.measurement",
                    NavigationAgentLogicalId = definition.Navigation.AgentLogicalId,
                    AreaMask = 5,
                    AreaTags = { "area.forest", "area.clearing" },
                    TerritoryRadius = 13.5m
                },
                Needs = new NpcNeedProfile
                {
                    InitialHunger = 0.125m,
                    HungerPerHour = 0.075m,
                    InitialFear = 0.5m,
                    FearDecayPerSecond = 0.025m,
                    TerritoryCommitment = 0.625m
                },
                Population = new NpcPopulationBudget
                {
                    MaximumActive = 9,
                    MinimumGroupSize = 2,
                    MaximumGroupSize = 5,
                    RespawnCooldownSeconds = 91.5m,
                    EncounterCost = 3
                },
                Relationships =
                {
                    new NpcSpeciesRelationshipRule
                    {
                        SubjectSpeciesId = "species.measurement",
                        TargetSpeciesId = "species.other",
                        Disposition = NpcEcologicalDisposition.Predator,
                        TriggerDistance = 17.25m,
                        ResponseStrategyStableId = "npc.strategy.flee.sprint"
                    }
                },
                Schedule =
                {
                    new NpcScheduleEntry
                    {
                        EntryId = "schedule.dawn",
                        StartHour = 5.5m,
                        EndHour = 11.25m,
                        StrategyStableId = "npc.strategy.wander.calm"
                    }
                }
            };

            definition.VariationRequest = new NpcVariationRequestRecord
            {
                RequestVariations = true,
                Status = NpcVariationDiscoveryStatus.NoCompatibleVariant,
                StatusNote = "measurement fixture",
                SuppressedCandidateIds = { "candidate.suppressed" },
                Candidates =
                {
                    new NpcVariationCandidate
                    {
                        CandidateId = "candidate.measurement",
                        ProviderId = NpcCharacterSourceProviderIds.AdventureCharactersPack2,
                        SourceLogicalId = "source.measurement",
                        Provenance = "owned-asset",
                        Confidence = 0.875m,
                        RigLogicalId = "rig.humanoid"
                    }
                }
            };

            definition.Companion = new NpcCompanionRecord
            {
                LeaderPolicy = "project.local-player",
                FollowDistance = 3.25m,
                HoldDistance = 1.125m,
                DefendPlayer = true,
                DefendChild = false,
                FriendlyFactions = { "faction.village" },
                ThreatFactions = { "faction.bandit" }
            };

            definition.Appearance.Parts.Add(new NpcAppearancePartSelection
            {
                Slot = NpcEquipmentSlot.Hat,
                ItemStableId = "equipment.measurement.hat",
                ReplaceDefault = false
            });

            definition.CharacterSource.RequiredAnimationTags.Add("animation.tag.measurement");

            if (definition.Capabilities.Count > 0)
            {
                var capability = definition.Capabilities[0];
                capability.Parameters["ordinal-key"] = "ordinal-value";
                capability.Parameters["Ordinal-Key"] = "case-sensitive-sibling";
                capability.OwnedFields.Add("field.measurement");
                capability.ValidationRules.Add("rule.measurement");
                capability.QualityPolicy = AuthoringQualityPolicy.DeterministicOnly;
            }

            definition.InteractionActions.Add(new NpcInteractionActionRecord
            {
                ActionId = "npc.action.measurement",
                PromptLocalizationKey = "npc.prompt.measurement",
                EnglishPromptFallback = "Measure",
                Range = 2.75m,
                HoldSeconds = 0.35m
            });

            definition.DialogueLines.Add(new NpcDialogueLineRecord
            {
                LineId = "line.measurement",
                SpeakerLocalizationKey = "npc.speaker.measurement",
                SubtitleLocalizationKey = "npc.subtitle.measurement",
                EnglishSourceFallback = "Measured.",
                VoiceLocalizationKey = "npc.voice.measurement",
                OptionalAudioClipLogicalId = "audio.measurement",
                CooldownSeconds = 4.5m,
                Weight = 0.375m,
                Tags = { "tag.measurement" }
            });

            definition.CommerceOffers.Add(new NpcCommerceOfferRecord
            {
                OfferId = "offer.measurement",
                ItemLogicalId = "item.measurement",
                QuantityPerPurchase = 3,
                InitialStock = 11,
                CurrencyItemLogicalId = "item.coin",
                Price = 17,
                LabelLocalizationKey = "npc.label.measurement",
                EnglishLabelFallback = "Measured Goods"
            });

            return definition;
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

        static string Describe(DiagnosticList diagnostics)
        {
            var builder = new StringBuilder();
            foreach (var diagnostic in diagnostics.Items)
                builder.AppendLine(diagnostic.Code + ": " + diagnostic.Message);
            return builder.ToString();
        }

        // ---------------------------------------------------------------------------------
        // Reflection-based deep comparison
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Records every path at which <paramref name="restored"/> differs from
        /// <paramref name="original"/>. The point is the list, not the boolean: a caller needs to
        /// know *which* authoring intent a round-trip drops.
        /// </summary>
        static void DeepCompare(string path, object original, object restored, ICollection<string> losses)
        {
            if (original == null && restored == null)
                return;
            if (original == null || restored == null)
            {
                losses.Add(path + ": " + Render(original) + " -> " + Render(restored));
                return;
            }

            var type = original.GetType();
            if (type != restored.GetType())
            {
                losses.Add(path + ": type " + type.Name + " -> " + restored.GetType().Name);
                return;
            }

            if (IsScalar(type))
            {
                if (!Equals(original, restored))
                    losses.Add(path + ": " + Render(original) + " -> " + Render(restored));
                return;
            }

            if (original is IDictionary originalMap)
            {
                var restoredMap = (IDictionary)restored;
                foreach (DictionaryEntry entry in originalMap)
                {
                    if (!restoredMap.Contains(entry.Key))
                    {
                        losses.Add(path + "[" + entry.Key + "]: missing after round-trip");
                        continue;
                    }
                    DeepCompare(path + "[" + entry.Key + "]", entry.Value, restoredMap[entry.Key], losses);
                }
                foreach (DictionaryEntry entry in restoredMap)
                    if (!originalMap.Contains(entry.Key))
                        losses.Add(path + "[" + entry.Key + "]: appeared after round-trip");
                return;
            }

            if (original is IList originalList)
            {
                var restoredList = (IList)restored;
                if (originalList.Count != restoredList.Count)
                {
                    losses.Add(path + ": count " + originalList.Count + " -> " + restoredList.Count);
                    return;
                }
                for (int i = 0; i < originalList.Count; i++)
                    DeepCompare(path + "[" + i + "]", originalList[i], restoredList[i], losses);
                return;
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                    continue;
                object originalValue;
                object restoredValue;
                try
                {
                    originalValue = property.GetValue(original, null);
                    restoredValue = property.GetValue(restored, null);
                }
                catch (TargetInvocationException)
                {
                    continue;
                }
                DeepCompare(path + "." + property.Name, originalValue, restoredValue, losses);
            }
        }

        static bool IsScalar(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return underlying.IsPrimitive
                || underlying.IsEnum
                || underlying == typeof(string)
                || underlying == typeof(decimal)
                || underlying == typeof(Guid)
                || underlying == typeof(DateTime)
                || underlying == typeof(TimeSpan);
        }

        static string Render(object value)
        {
            if (value == null)
                return "<null>";
            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            return value.ToString();
        }
    }
}
