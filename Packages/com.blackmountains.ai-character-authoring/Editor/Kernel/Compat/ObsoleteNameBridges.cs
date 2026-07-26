using System;

// Deprecated names kept alive for consumers pinned to the pre-WP-06 surface.
// This file is the ONLY place in the editor kernel that is allowed to mention a subject domain.
// Removed with WP-52 (kernel extraction).
namespace BlackMountains.AuthoringKernel.Editor
{
    public sealed partial class GenerationManifest
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

        /// <summary>
        /// Deprecated alias for <see cref="SubjectSpecHash"/>. Mirrors the same storage; do not persist both.
        /// </summary>
        [Obsolete("Renamed to SubjectSpecHash: the kernel is subject-neutral and is shared by non-character pipelines. Removed with WP-52.")]
        public string CharacterSpecHash
        {
            get { return SubjectSpecHash; }
            set { SubjectSpecHash = value; }
        }
    }
}
