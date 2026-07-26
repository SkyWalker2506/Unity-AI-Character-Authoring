using System;
using System.Collections.Generic;
using BlackMountains.AuthoringKernel;

namespace BlackMountains.AICharacterAuthoring
{
    /// <summary>
    /// Retired character specification. Superseded by <see cref="NpcDefinition"/> in WP-07.
    /// </summary>
    /// <remarks>
    /// Kept — deliberately not deleted — because Million Dollars Project pins this package by
    /// commit and still compiles against this type. It carries no authority: the capability list
    /// here produces one synthetic <c>ensureCapability</c> operation per capability and never a
    /// real mutation target. Its <c>Parameters</c> channel additionally does not survive a
    /// Newtonsoft round-trip (<see cref="AuthoringFieldValue"/> has a private constructor and no
    /// converter), which is the measured reason it cannot be the authoritative input.
    /// </remarks>
    [Obsolete(ObsoleteAuthoringMessages.CharacterSpec, false)]
    public sealed class CharacterSpec
    {
        public const string CurrentSchemaVersion = "1";

        public string SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string SpecId { get; set; }
        public CharacterIdentity Identity { get; set; } = new CharacterIdentity();
        public CharacterSource Source { get; set; } = new CharacterSource();
        public CharacterRig Rig { get; set; } = new CharacterRig();
        public List<string> Capabilities { get; set; } = new List<string>();
        public List<string> Exclusions { get; set; } = new List<string>();
        public string BehaviorFamily { get; set; }
        public string PhysicsProfile { get; set; }
        public string PerformanceProfile { get; set; }
        public Dictionary<string, AuthoringFieldValue> Parameters { get; set; } = new Dictionary<string, AuthoringFieldValue>(System.StringComparer.Ordinal);
        public AuthoringQualityPolicy QualityPolicy { get; set; } = AuthoringQualityPolicy.DeterministicOnly;
    }

    public sealed class CharacterIdentity
    {
        public string DisplayName { get; set; }
        public string Archetype { get; set; }
    }

    public sealed class CharacterSource
    {
        public string SourcePrefabLogicalId { get; set; }
        public string SourcePrefabGuid { get; set; }
        public string SourceVersion { get; set; }
    }

    public sealed class CharacterRig
    {
        public string Type { get; set; } = "Humanoid";
        public bool UseRegularAnimator { get; set; } = true;
    }
}
