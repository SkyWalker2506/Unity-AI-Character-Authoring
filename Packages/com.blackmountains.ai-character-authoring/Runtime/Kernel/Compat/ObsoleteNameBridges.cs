using System;

// Deprecated names kept alive for consumers pinned to the pre-WP-06 surface.
// This file is the ONLY place in the runtime kernel that is allowed to mention a subject domain;
// the kernel boundary test asserts every type here is [Obsolete]. Removed with WP-52 (kernel extraction).
namespace BlackMountains.AuthoringKernel
{
    public sealed partial class GenerationPlan
    {
        /// <summary>
        /// Deprecated alias for <see cref="SubjectSpecId"/>. Mirrors the same storage; do not persist both.
        /// </summary>
        [Obsolete("Renamed to SubjectSpecId: the kernel is subject-neutral and is shared by non-character pipelines. Removed with WP-52.")]
        public string CharacterSpecId
        {
            get { return SubjectSpecId; }
            set { SubjectSpecId = value; }
        }
    }

    /// <summary>
    /// Deprecated alias for <see cref="SubjectId"/>. Implicitly converts in both directions so pinned
    /// call sites keep compiling after adding <c>using BlackMountains.AuthoringKernel;</c>.
    /// </summary>
    [Obsolete("Renamed to SubjectId: the kernel is subject-neutral and is shared by non-character pipelines. Removed with WP-52.")]
    public readonly struct CharacterId : IEquatable<CharacterId>, IComparable<CharacterId>
    {
        public CharacterId(string value) => Value = AuthoringIdentifier.Require(value, nameof(value));
        public string Value { get; }
        public bool Equals(CharacterId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public int CompareTo(CharacterId other) => string.CompareOrdinal(Value, other.Value);
        public override string ToString() => Value ?? string.Empty;
        public static implicit operator string(CharacterId id) => id.Value;
        public static implicit operator SubjectId(CharacterId id) => new SubjectId(id.Value);
        public static implicit operator CharacterId(SubjectId id) => new CharacterId(id.Value);
    }
}
