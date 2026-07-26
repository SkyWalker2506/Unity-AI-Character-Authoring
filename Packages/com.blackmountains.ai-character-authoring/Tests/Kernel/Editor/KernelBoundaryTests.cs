using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlackMountains.AuthoringKernel;
using BlackMountains.AuthoringKernel.Editor;
using NUnit.Framework;
using UnityEditor.Compilation;
using CompiledAssembly = UnityEditor.Compilation.Assembly;
using ReflectionAssembly = System.Reflection.Assembly;

namespace BlackMountains.AuthoringKernel.Tests
{
    /// <summary>
    /// WP-06 enforcement: the authoring kernel must stay domain-neutral and Unity-free.
    /// This assembly deliberately references only the two kernel assemblies — if the kernel ever
    /// needed a character type to compile, this test assembly would stop compiling first.
    /// </summary>
    public sealed class KernelBoundaryTests
    {
        const string RuntimeKernel = "BlackMountains.AuthoringKernel";
        const string EditorKernel = "BlackMountains.AuthoringKernel.Editor";

        static readonly string[] DomainAssemblyFragments = { "AICharacterAuthoring" };
        static readonly string[] EngineAssemblyNames = { "UnityEngine", "UnityEditor" };

        static ReflectionAssembly RuntimeKernelAssembly => typeof(CanonicalValue).Assembly;
        static ReflectionAssembly EditorKernelAssembly => typeof(ThreeWayMergeEngine).Assembly;

        [Test]
        public void KernelTypesLiveInTheTwoKernelAssemblies()
        {
            Assert.That(RuntimeKernelAssembly.GetName().Name, Is.EqualTo(RuntimeKernel));
            Assert.That(EditorKernelAssembly.GetName().Name, Is.EqualTo(EditorKernel));
        }

        [TestCase(RuntimeKernel)]
        [TestCase(EditorKernel)]
        public void KernelAssemblyDoesNotReferenceCharacterAuthoring(string assemblyName)
        {
            var referenced = ReferencedAssemblyNames(Resolve(assemblyName));
            var offenders = referenced.Where(ContainsDomainFragment).ToArray();

            Assert.That(offenders, Is.Empty,
                assemblyName + " must not reference the character-authoring assemblies. Found: " + Join(offenders));
        }

        [TestCase(RuntimeKernel)]
        [TestCase(EditorKernel)]
        public void KernelAssemblyDefinitionDeclaresNoCharacterAuthoringReference(string assemblyName)
        {
            var declared = DeclaredReferences(assemblyName);
            var offenders = declared.Where(ContainsDomainFragment).ToArray();

            Assert.That(offenders, Is.Empty,
                assemblyName + ".asmdef must not list a character-authoring assembly. Found: " + Join(offenders));
        }

        [TestCase(RuntimeKernel)]
        [TestCase(EditorKernel)]
        public void KernelAssemblyIsEngineFree(string assemblyName)
        {
            var referenced = ReferencedAssemblyNames(Resolve(assemblyName));
            var offenders = referenced
                .Where(name => EngineAssemblyNames.Any(engine => name.StartsWith(engine, StringComparison.Ordinal)))
                .ToArray();

            Assert.That(offenders, Is.Empty,
                assemblyName + " must compile without UnityEngine/UnityEditor. Found: " + Join(offenders));
        }

        [TestCase(RuntimeKernel)]
        [TestCase(EditorKernel)]
        public void KernelExposesNoSubjectDomainTypes(string assemblyName)
        {
            var offenders = Resolve(assemblyName).GetExportedTypes()
                .Where(type => type.Name.IndexOf("Npc", StringComparison.OrdinalIgnoreCase) >= 0
                    || type.Name.IndexOf("Character", StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(type => type.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Select(type => type.FullName)
                .ToArray();

            Assert.That(offenders, Is.Empty,
                assemblyName + " must expose no NPC/character type. Only [Obsolete] rename bridges may keep the old name. Found: " + Join(offenders));
        }

        [Test]
        public void CharacterSpecIdBridgeStillWritesTheNeutralProperty()
        {
            var plan = new GenerationPlan();
#pragma warning disable 618
            plan.CharacterSpecId = "subject.bridge";
            Assert.That(plan.SubjectSpecId, Is.EqualTo("subject.bridge"));

            plan.SubjectSpecId = "subject.neutral";
            Assert.That(plan.CharacterSpecId, Is.EqualTo("subject.neutral"));

            var manifest = new GenerationManifest { CharacterSpecId = "subject.bridge", CharacterSpecHash = "hash.bridge" };
#pragma warning restore 618
            Assert.That(manifest.SubjectSpecId, Is.EqualTo("subject.bridge"));
            Assert.That(manifest.SubjectSpecHash, Is.EqualTo("hash.bridge"));
        }

        [Test]
        public void RenamedIdentifiersKeepTheirDeprecatedAlias()
        {
#pragma warning disable 618
            var deprecated = new CharacterId("subject.alias");
            SubjectId neutral = deprecated;
#pragma warning restore 618
            Assert.That(neutral.Value, Is.EqualTo("subject.alias"));
        }

        /// <summary>
        /// Golden vector produced by an independent Python implementation of the same canonical map
        /// (C1: one rule, two languages, zero difference). It pins the digest key grammar: the
        /// CharacterSpecId -> SubjectSpecId rename must not move a single byte of the payload.
        /// </summary>
        [Test]
        public void PlanDigestGoldenVectorSurvivedTheRename()
        {
            Assert.That(GenerationPlanHasher.Hash(GoldenPlan()),
                Is.EqualTo("d566ed35a599a310ee2f61063416d8920c006759880bd03a22ba46ae597f498b"));
        }

        [Test]
        public void PlanDigestIsBlindToWhichAliasWroteTheSubjectId()
        {
            var viaBridge = GoldenPlan();
            viaBridge.SubjectSpecId = null;
#pragma warning disable 618
            viaBridge.CharacterSpecId = "subject.digest";
#pragma warning restore 618

            Assert.That(GenerationPlanHasher.Hash(viaBridge), Is.EqualTo(GenerationPlanHasher.Hash(GoldenPlan())));
        }

        static GenerationPlan GoldenPlan()
        {
            var plan = new GenerationPlan { PlanId = "plan.digest", SubjectSpecId = "subject.digest" };
            plan.Operations.Add(new GenerationOperation
            {
                OperationId = "op.digest",
                OperationKind = "kernel.noop",
                LogicalTarget = "target",
                StableOperationKey = "target"
            });
            return plan;
        }

        static ReflectionAssembly Resolve(string assemblyName)
        {
            if (assemblyName == RuntimeKernel) return RuntimeKernelAssembly;
            if (assemblyName == EditorKernel) return EditorKernelAssembly;
            throw new ArgumentOutOfRangeException(nameof(assemblyName), assemblyName, "Unknown kernel assembly.");
        }

        static IEnumerable<string> ReferencedAssemblyNames(ReflectionAssembly assembly)
        {
            return assembly.GetReferencedAssemblies().Select(reference => reference.Name);
        }

        static IEnumerable<string> DeclaredReferences(string assemblyName)
        {
            CompiledAssembly assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                .FirstOrDefault(candidate => candidate.name == assemblyName);

            Assert.That(assembly, Is.Not.Null, assemblyName + " was not found in the editor compilation graph.");
            return assembly.assemblyReferences.Select(reference => reference.name);
        }

        static bool ContainsDomainFragment(string name)
        {
            return DomainAssemblyFragments.Any(fragment => name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        static string Join(IEnumerable<string> values)
        {
            return string.Join(", ", values.ToArray());
        }
    }
}
