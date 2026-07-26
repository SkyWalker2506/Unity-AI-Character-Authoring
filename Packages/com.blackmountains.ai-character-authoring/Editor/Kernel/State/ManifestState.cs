using System;
using System.Collections.Generic;

namespace BlackMountains.AuthoringKernel.Editor
{
    public enum ValidationLifecycleState
    {
        Planned,
        Applying,
        Applied,
        ValidationPending,
        Validated,
        ValidationFailed
    }

    public sealed class GeneratedAssetRecord
    {
        public string LogicalId { get; set; }
        public string Guid { get; set; }
        public long LocalFileId { get; set; }
    }

    public sealed class GenerationSourceRecord
    {
        public string SourcePrefabGuid { get; set; }
        public string SourcePrefabVersion { get; set; }
        public string SourceNormalizedHash { get; set; }
        public string SourceProviderSchemaVersion { get; set; }
    }

    public sealed partial class GenerationManifest
    {
        public const string CurrentSchemaVersion = "1";

        public string SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string ManifestId { get; set; }
        public string SubjectSpecId { get; set; }
        public string SubjectSpecHash { get; set; }
        public string GenerationPlanHash { get; set; }
        public string GeneratorVersion { get; set; } = "0.1.0";
        public List<GeneratedAssetRecord> GeneratedAssets { get; set; } = new List<GeneratedAssetRecord>();
        public GenerationSourceRecord Source { get; set; } = new GenerationSourceRecord();
        public Dictionary<string, string> ProviderVersions { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public List<SharedResourceRecord> SharedResources { get; set; } = new List<SharedResourceRecord>();
        public string LastSuccessfulSnapshotHash { get; set; }
        public NormalizedSnapshot LastSuccessfulSnapshot { get; set; }
        public string LastSuccessfulExecutionId { get; set; }
        public bool RecoveryRequired { get; set; }
        public ValidationLifecycleState LifecycleState { get; set; } = ValidationLifecycleState.Planned;
    }

    public sealed class SharedResourceRecord
    {
        public string ResourceId { get; set; }
        public ResourceProvenance Provenance { get; set; }
        public List<string> Owners { get; set; } = new List<string>();

        public bool CanDeleteWhenUnused()
        {
            return Owners.Count == 0 && (Provenance == ResourceProvenance.CreatedByFramework || Provenance == ResourceProvenance.AdoptedByFramework);
        }
    }

    public interface IManifestStore
    {
        GenerationManifest Load(string manifestId);
        void Save(GenerationManifest manifest);
    }

    public sealed class InMemoryManifestStore : IManifestStore
    {
        readonly Dictionary<string, GenerationManifest> _manifests = new Dictionary<string, GenerationManifest>(StringComparer.Ordinal);

        public int SaveCount { get; private set; }

        public GenerationManifest Load(string manifestId)
        {
            if (manifestId == null) return null;
            _manifests.TryGetValue(manifestId, out var manifest);
            return manifest;
        }

        public void Save(GenerationManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (string.IsNullOrWhiteSpace(manifest.ManifestId)) throw new ArgumentException("ManifestId is required.", nameof(manifest));
            _manifests[manifest.ManifestId] = manifest;
            SaveCount++;
        }
    }
}
