using System;
using System.Collections.Generic;
using System.IO;
using BlackMountains.AICharacterAuthoring.Editor;
using NUnit.Framework;
using BlackMountains.AuthoringKernel;
using BlackMountains.AuthoringKernel.Editor;

namespace BlackMountains.AICharacterAuthoring.Editor.Tests
{
    public sealed class FoundationEditorTests
    {
        static readonly FieldIdentity Health = new FieldIdentity("provider.test", "health.max", "1");

        [TestCase(100, 100, 100, MergeAction.NoOp, DriftClassification.NoDrift, false)]
        [TestCase(100, 100, 150, MergeAction.ApplyDesired, DriftClassification.NoDrift, false)]
        [TestCase(100, 130, 100, MergeAction.PreserveCurrent, DriftClassification.PreservedDrift, true)]
        [TestCase(100, 130, 130, MergeAction.NoOp, DriftClassification.ConvergedExternalChange, false)]
        [TestCase(100, 130, 150, MergeAction.Conflict, DriftClassification.Conflict, true)]
        public void PreserveOnDriftTruthTableIsNormative(int b, int c, int n, MergeAction action, DriftClassification drift, bool visible)
        {
            var engine = new ThreeWayMergeEngine();
            var decision = engine.MergeField(Health, OwnershipPolicy.PreserveOnDrift, Known(b), Known(c), Known(n));

            Assert.That(decision.Action, Is.EqualTo(action));
            Assert.That(decision.Drift, Is.EqualTo(drift));
            Assert.That(decision.DriftMustBeVisibleInPreview, Is.EqualTo(visible));
        }

        [Test]
        public void EnforcedDriftIsVisibleBeforeOverwrite()
        {
            var engine = new ThreeWayMergeEngine();
            var decision = engine.MergeField(Health, OwnershipPolicy.Enforced, Known(100), Known(130), Known(150));

            Assert.That(decision.Action, Is.EqualTo(MergeAction.ApplyDesired));
            Assert.That(decision.Drift, Is.EqualTo(DriftClassification.EnforcedDrift));
            Assert.That(decision.DriftMustBeVisibleInPreview, Is.True);
        }

        [Test]
        public void UnspecifiedDesiredPreservesCurrentWithoutClaimingDrift()
        {
            var engine = new ThreeWayMergeEngine();
            var decision = engine.MergeField(Health, OwnershipPolicy.Enforced, Known(100), Known(130), AuthoringFieldValue.Unspecified());

            Assert.That(decision.Action, Is.EqualTo(MergeAction.PreserveCurrent));
            Assert.That(decision.Drift, Is.EqualTo(DriftClassification.NoDrift));
            Assert.That(decision.DriftMustBeVisibleInPreview, Is.False);
        }

        [Test]
        public void UnknownObservedValueFailsClosed()
        {
            var engine = new ThreeWayMergeEngine();
            var decision = engine.MergeField(Health, OwnershipPolicy.Enforced, Known(100), AuthoringFieldValue.Unknown(), Known(150));

            Assert.That(decision.Action, Is.EqualTo(MergeAction.Conflict));
            Assert.That(decision.DriftMustBeVisibleInPreview, Is.True);
        }

        [Test]
        public void FieldIdentityExcludesSchemaVersion()
        {
            var v1 = new FieldIdentity("provider.test", "field.key", "1");
            var v2 = new FieldIdentity("provider.test", "field.key", "2");

            Assert.That(v1, Is.EqualTo(v2));
            Assert.That(v1.GetHashCode(), Is.EqualTo(v2.GetHashCode()));
        }

        [Test]
        public void SnapshotHashUsesStableFieldOrder()
        {
            var first = new NormalizedSnapshot();
            first.AddField(new NormalizedField(new FieldIdentity("provider.b", "value"), OwnershipPolicy.Enforced, Known(2)));
            first.AddField(new NormalizedField(new FieldIdentity("provider.a", "value"), OwnershipPolicy.Enforced, Known(1)));

            var second = new NormalizedSnapshot();
            second.AddField(new NormalizedField(new FieldIdentity("provider.a", "value"), OwnershipPolicy.Enforced, Known(1)));
            second.AddField(new NormalizedField(new FieldIdentity("provider.b", "value"), OwnershipPolicy.Enforced, Known(2)));

            Assert.That(NormalizedSnapshotHasher.Hash(first), Is.EqualTo(NormalizedSnapshotHasher.Hash(second)));
        }

        [Test]
        public void CompilerOrdersOperationsDeterministically()
        {
            var registry = CreateRegistry();
            var compiler = new GenerationPlanCompiler(registry);
            var spec = CreateSpec("capability.test.melee", "capability.test.death");

            var result = compiler.Compile(spec);

            Assert.That(result.Success, Is.True, Describe(result.Diagnostics));
            Assert.That(result.Plan.Operations, Has.Count.EqualTo(4));
            Assert.That(result.Plan.Operations[0].StableOperationKey, Is.EqualTo("capability.test.locomotion"));
            Assert.That(result.Plan.Operations[1].StableOperationKey, Is.EqualTo("capability.test.perception"));
            Assert.That(result.Plan.Operations[2].StableOperationKey, Is.EqualTo("capability.test.death"));
            Assert.That(result.Plan.Operations[3].StableOperationKey, Is.EqualTo("capability.test.melee"));
        }

        [Test]
        public void CompileFailureHasZeroMutationSignal()
        {
            var registry = CreateRegistry();
            var compiler = new GenerationPlanCompiler(registry);
            var spec = CreateSpec("capability.test.missing");

            var result = compiler.Compile(spec);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ManagedMutationsAttempted, Is.Zero);
            Assert.That(result.Plan, Is.Null);
        }

        [Test]
        public void SharedOwnerSetDeletionRequiresEmptyOwnersAndDeletableProvenance()
        {
            var resource = new SharedResourceOwnership("component.health.primary", ResourceProvenance.CreatedByFramework);
            resource.AddOwner("capability.test.melee");
            resource.AddOwner("capability.test.death");

            resource.RemoveOwner("capability.test.melee");
            Assert.That(resource.CanDeleteWhenUnused(), Is.False);

            resource.RemoveOwner("capability.test.death");
            Assert.That(resource.CanDeleteWhenUnused(), Is.True);

            var external = new SharedResourceOwnership("component.external", ResourceProvenance.External);
            Assert.That(external.CanDeleteWhenUnused(), Is.False);
        }

        [Test]
        public void AIRequiredFailsClosedWithoutProvider()
        {
            var result = IntelligencePolicyResolver.Resolve(AuthoringQualityPolicy.AIRequired, aiProviderAvailable: false, deterministicFallbackAvailable: true);

            Assert.That(result.Success, Is.False);
            Assert.That(result.UsedDeterministicFallback, Is.False);
            Assert.That(result.Diagnostic.Code, Is.EqualTo("ACA-AI-REQUIRED-UNAVAILABLE"));
        }

        [Test]
        public void CapabilityAIRequirementCannotBeWeakenedBySpecPolicy()
        {
            var registry = CreateRegistry();
            registry.RegisterCapability(new CapabilityDescriptor
            {
                CapabilityId = "capability.test.ai-authored",
                ProviderId = "provider.test.ai-authored",
                QualityPolicy = AuthoringQualityPolicy.AIRequired
            });
            var compiler = new GenerationPlanCompiler(registry);
            var spec = CreateSpec("capability.test.ai-authored");
            spec.QualityPolicy = AuthoringQualityPolicy.DeterministicOnly;

            var unavailable = compiler.Compile(spec, aiProviderAvailable: false);
            var available = compiler.Compile(spec, aiProviderAvailable: true);

            Assert.That(unavailable.Success, Is.False);
            Assert.That(unavailable.Diagnostics.Items[0].Code, Is.EqualTo("ACA-AI-REQUIRED-UNAVAILABLE"));
            Assert.That(available.Success, Is.True, Describe(available.Diagnostics));
            Assert.That(available.Plan.Metadata["effectiveAiPolicy"], Is.EqualTo(AuthoringQualityPolicy.AIRequired.ToString()));
            Assert.That(available.Plan.Metadata["aiUsed"], Is.EqualTo("true"));
        }

        [Test]
        public void PlanHashCoversOperationInputsAndApprovalSemantics()
        {
            var plan = new GenerationPlan { PlanId = "plan.hash", SubjectSpecId = "char.hash" };
            var operation = new GenerationOperation
            {
                OperationId = "op.hash",
                OperationKind = "ensureCapability",
                LogicalTarget = "target.hash",
                StableOperationKey = "target.hash",
                SecurityLevel = OperationSecurityLevel.MutateFrameworkOwnedAsset,
                Reversibility = OperationReversibility.Reversible
            };
            operation.Inputs["value"] = Known(1);
            operation.Preconditions.Add("source-version=1");
            plan.Operations.Add(operation);
            string original = GenerationPlanHasher.Hash(plan);

            operation.Inputs["value"] = Known(2);
            string inputChanged = GenerationPlanHasher.Hash(plan);
            operation.Inputs["value"] = Known(1);
            operation.SecurityLevel = OperationSecurityLevel.MutateUserOwnedAsset;
            string securityChanged = GenerationPlanHasher.Hash(plan);
            operation.SecurityLevel = OperationSecurityLevel.MutateFrameworkOwnedAsset;
            operation.Reversibility = OperationReversibility.NonReversible;
            string reversibilityChanged = GenerationPlanHasher.Hash(plan);

            Assert.That(inputChanged, Is.Not.EqualTo(original));
            Assert.That(securityChanged, Is.Not.EqualTo(original));
            Assert.That(reversibilityChanged, Is.Not.EqualTo(original));
        }

        [Test]
        public void ExternalPathPolicyRejectsProjectLocalPaths()
        {
            string project = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "aca-project"));
            string external = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "aca-external"));
            var policy = new AuthoringExternalPathPolicy(AuthoringProjectIdentity.Create(project), external);

            var rejected = policy.ValidateExternalFilePath(Path.Combine(project, "Assets", "request.json"));
            var accepted = policy.ValidateExternalFilePath(Path.Combine(external, "request.json"));

            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostic.Code, Is.EqualTo("ACA-PATH-PROJECT-ROOT"));
            Assert.That(accepted.Success, Is.True);
        }

        [Test]
        public void ApplyWithoutHandlerReportsNotImplementedAndDoesNotStartMutation()
        {
            string project = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "aca-apply-project-" + Guid.NewGuid().ToString("N")));
            string external = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "aca-apply-external-" + Guid.NewGuid().ToString("N")));
            var policy = new AuthoringExternalPathPolicy(AuthoringProjectIdentity.Create(project), external);
            var plan = new GenerationPlan { PlanId = "plan.test", SubjectSpecId = "char.test" };
            plan.Operations.Add(new GenerationOperation
            {
                OperationId = "op.test",
                OperationKind = "missing.handler",
                Phase = OperationLifecyclePhase.ProviderSetup,
                LogicalTarget = "target",
                StableOperationKey = "target",
                SecurityLevel = OperationSecurityLevel.MutateFrameworkOwnedAsset
            });
            string hash = GenerationPlanHasher.Hash(plan);
            var approval = new ApprovalToken { ProjectId = policy.ProjectIdentity.StableProjectId, PlanHash = hash };
            approval.Allow(OperationSecurityLevel.MutateFrameworkOwnedAsset);

            var result = new PlanApplicationService(policy).Apply(plan, approval);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ManagedMutationsAttempted, Is.Zero);
            Assert.That(result.JournalPath, Is.Null);
            Assert.That(result.Diagnostics.Items[0].Code, Is.EqualTo("ACA-OPERATION-NOT-IMPLEMENTED"));
        }

        [Test]
        public void NonReversibleOperationRequiresExplicitElevatedApproval()
        {
            string project = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "aca-approval-project-" + Guid.NewGuid().ToString("N")));
            string external = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "aca-approval-external-" + Guid.NewGuid().ToString("N")));
            var policy = new AuthoringExternalPathPolicy(AuthoringProjectIdentity.Create(project), external);
            var plan = new GenerationPlan { PlanId = "plan.nonreversible", SubjectSpecId = "char.nonreversible" };
            plan.Operations.Add(new GenerationOperation
            {
                OperationId = "op.nonreversible",
                OperationKind = "missing.handler",
                LogicalTarget = "target",
                StableOperationKey = "target",
                SecurityLevel = OperationSecurityLevel.MutateFrameworkOwnedAsset,
                Reversibility = OperationReversibility.NonReversible
            });
            var approval = new ApprovalToken
            {
                ProjectId = policy.ProjectIdentity.StableProjectId,
                PlanHash = GenerationPlanHasher.Hash(plan)
            };
            approval.Allow(OperationSecurityLevel.MutateFrameworkOwnedAsset);

            var denied = new PlanApplicationService(policy).Apply(plan, approval);
            approval.AllowNonReversible();
            var elevated = new PlanApplicationService(policy).Apply(plan, approval);

            Assert.That(denied.Diagnostics.Items[0].Code, Is.EqualTo("ACA-APPROVAL-DENIED"));
            Assert.That(denied.ManagedMutationsAttempted, Is.Zero);
            Assert.That(elevated.Diagnostics.Items[0].Code, Is.EqualTo("ACA-OPERATION-NOT-IMPLEMENTED"));
            Assert.That(elevated.ManagedMutationsAttempted, Is.Zero);
        }

        [Test]
        public void MutationScopeFailsClosedOutsideExecutor()
        {
            Assert.That(MutationScope.IsActive, Is.False);
            Assert.Throws<InvalidOperationException>(MutationScope.RequireActive);

            using (MutationScope.Enter())
            {
                Assert.That(MutationScope.IsActive, Is.True);
                Assert.DoesNotThrow(MutationScope.RequireActive);
            }

            Assert.That(MutationScope.IsActive, Is.False);
        }

        static AuthoringProviderRegistry CreateRegistry()
        {
            var registry = new AuthoringProviderRegistry();
            registry.RegisterCapability(new CapabilityDescriptor
            {
                CapabilityId = "capability.test.locomotion",
                ProviderId = "provider.test.locomotion"
            });
            registry.RegisterCapability(new CapabilityDescriptor
            {
                CapabilityId = "capability.test.perception",
                ProviderId = "provider.test.perception"
            });
            registry.RegisterCapability(new CapabilityDescriptor
            {
                CapabilityId = "capability.test.melee",
                ProviderId = "provider.test.melee",
                Requires = new List<string> { "capability.test.locomotion", "capability.test.perception" }
            });
            registry.RegisterCapability(new CapabilityDescriptor
            {
                CapabilityId = "capability.test.death",
                ProviderId = "provider.test.death",
                Requires = new List<string> { "capability.test.locomotion" }
            });
            return registry;
        }

        static CharacterSpec CreateSpec(params string[] capabilities)
        {
            return new CharacterSpec
            {
                SchemaVersion = CharacterSpec.CurrentSchemaVersion,
                SpecId = "char.test.hostile",
                Capabilities = new List<string>(capabilities)
            };
        }

        static AuthoringFieldValue Known(int value) => AuthoringFieldValue.Known(CanonicalValue.Integer(value));

        static string Describe(DiagnosticList diagnostics)
        {
            var lines = new List<string>();
            foreach (var diagnostic in diagnostics.Items)
                lines.Add(diagnostic.Code + ": " + diagnostic.Message);
            return string.Join("\n", lines);
        }
    }
}
