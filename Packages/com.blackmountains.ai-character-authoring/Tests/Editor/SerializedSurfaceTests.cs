// Knowledge lock for `unity-binds-by-guid-not-assembly-name`.
//
// The measured claim: moving this package's assemblies — which WP-06 already did once and WP-52
// will do again — cannot break serialized Unity data, because the package emits no serialized
// Unity data at all. Unity's `m_Script` binds through the .cs.meta GUID, which survives an asmdef
// rename; `SerializeReference` binds through a literal assembly name written into the asset
// (`RefIds[].type.asm`), which does not.
//
// So the boundary of the safe claim is exactly "zero SerializeReference, zero UnityEngine.Object".
// A comment cannot hold that boundary across work packages. These tests can.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlackMountains.AICharacterAuthoring.Editor;
using BlackMountains.AuthoringKernel;
using BlackMountains.AuthoringKernel.Editor;
using NUnit.Framework;
using ReflectionAssembly = System.Reflection.Assembly;

namespace BlackMountains.AICharacterAuthoring.Editor.Tests
{
    /// <summary>
    /// Pins the package's Unity-serialization surface at zero across all four production
    /// assemblies. This is what makes assembly moves a folder operation rather than a data
    /// migration.
    /// </summary>
    public sealed class SerializedSurfaceTests
    {
        const string RuntimeKernel = "BlackMountains.AuthoringKernel";
        const string EditorKernel = "BlackMountains.AuthoringKernel.Editor";
        const string DomainRuntime = "BlackMountains.AICharacterAuthoring.Runtime";
        const string DomainEditor = "BlackMountains.AICharacterAuthoring.Editor";

        const BindingFlags AllFields =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        /// <summary>
        /// The one construct that would make an asmdef rename destructive.
        /// </summary>
        /// <remarks>
        /// A <c>[SerializeReference]</c> field causes Unity to write the declaring assembly's
        /// *name* into every asset that stores it. Rename or move the assembly and the stored
        /// reference no longer resolves — the data is gone, silently, and no compiler complains.
        /// Three of the four assemblies below set <c>noEngineReferences: true</c> and therefore
        /// cannot even name the attribute; <see cref="DomainEditor"/> can, and this test is the
        /// reason it still does not.
        /// </remarks>
        [TestCase(RuntimeKernel)]
        [TestCase(EditorKernel)]
        [TestCase(DomainRuntime)]
        [TestCase(DomainEditor)]
        public void ProductionAssembly_DeclaresNoSerializeReferenceField(string assemblyName)
        {
            var offenders = Resolve(assemblyName)
                .GetTypes()
                .SelectMany(type => type.GetFields(AllFields)
                    .Where(HasSerializeReference)
                    .Select(field => type.FullName + "." + field.Name))
                .ToArray();

            Assert.That(
                offenders,
                Is.Empty,
                assemblyName + " must declare no [SerializeReference] field: that attribute writes "
                    + "the assembly name into every asset, and this package's assemblies are still "
                    + "expected to move (WP-52). If a SerializeReference graph is genuinely needed, "
                    + "flip this test in the same commit and write the migration story into "
                    + "docs/AI/CONTRACTS.md first. Found: " + Join(offenders));
        }

        /// <summary>
        /// The broader claim behind the narrow one: nothing in this package is a Unity object, so
        /// nothing in this package ever becomes serialized instance data in a user's project.
        /// </summary>
        /// <remarks>
        /// Without this, <see cref="ProductionAssembly_DeclaresNoSerializeReferenceField"/> would
        /// go green while a <c>ScriptableObject</c> quietly appeared — and a ScriptableObject is
        /// how <c>SerializeReference</c> arrives in the first place. The pair is the boundary; one
        /// alone is not.
        /// </remarks>
        [TestCase(RuntimeKernel)]
        [TestCase(EditorKernel)]
        [TestCase(DomainRuntime)]
        [TestCase(DomainEditor)]
        public void ProductionAssembly_DefinesNoUnityObjectDerivedType(string assemblyName)
        {
            var offenders = Resolve(assemblyName)
                .GetTypes()
                .Where(type => typeof(UnityEngine.Object).IsAssignableFrom(type))
                .Select(type => type.FullName)
                .ToArray();

            Assert.That(
                offenders,
                Is.Empty,
                assemblyName + " must define no UnityEngine.Object-derived type. The package's "
                    + "authoring model is plain C# persisted as JSON; the moment it becomes Unity "
                    + "serialized data, assembly identity starts leaking into user assets. Found: "
                    + Join(offenders));
        }

        /// <summary>
        /// Guards the guard: if the anchor types stopped resolving to the assemblies they name,
        /// both tests above would scan the wrong thing and pass for the wrong reason.
        /// </summary>
        [TestCase(RuntimeKernel)]
        [TestCase(EditorKernel)]
        [TestCase(DomainRuntime)]
        [TestCase(DomainEditor)]
        public void ResolvedAssembly_IsTheOneItClaimsToBe(string assemblyName)
        {
            Assert.That(Resolve(assemblyName).GetName().Name, Is.EqualTo(assemblyName));
            Assert.That(
                Resolve(assemblyName).GetTypes(),
                Is.Not.Empty,
                assemblyName + " reported zero types; a scan over nothing proves nothing.");
        }

        static bool HasSerializeReference(FieldInfo field)
        {
            // Compared by name rather than by typeof: three of the four assemblies under test are
            // engine-free, and this test must keep working if that ever changes in either direction.
            return field.GetCustomAttributesData().Any(attribute =>
                attribute.AttributeType.FullName == "UnityEngine.SerializeReference");
        }

        static ReflectionAssembly Resolve(string assemblyName)
        {
            switch (assemblyName)
            {
                case RuntimeKernel: return typeof(CanonicalValue).Assembly;
                case EditorKernel: return typeof(ThreeWayMergeEngine).Assembly;
                case DomainRuntime: return typeof(NpcDefinition).Assembly;
                case DomainEditor: return typeof(NpcAuthoringPlannerFacade).Assembly;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(assemblyName), assemblyName, "Unknown production assembly.");
            }
        }

        static string Join(IEnumerable<string> values)
        {
            return string.Join(", ", values.ToArray());
        }
    }
}
