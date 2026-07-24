using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BlackMountains.AICharacterAuthoring.Editor
{
    public enum OwnershipPolicy
    {
        Enforced,
        PreserveOnDrift,
        UserOwned,
        Computed,
        Immutable
    }

    public enum CollectionMergeStrategy
    {
        Replace,
        MergeByKey,
        SetUnion,
        OrderedMerge,
        ProviderSpecific
    }

    public readonly struct FieldIdentity : IEquatable<FieldIdentity>, IComparable<FieldIdentity>
    {
        public FieldIdentity(string providerId, string stableFieldKey, string providerSchemaVersion = null)
        {
            ProviderId = AuthoringIdentifier.Require(providerId, nameof(providerId));
            StableFieldKey = AuthoringIdentifier.Require(stableFieldKey, nameof(stableFieldKey));
            ProviderSchemaVersion = string.IsNullOrWhiteSpace(providerSchemaVersion) ? "1" : providerSchemaVersion;
        }

        public string ProviderId { get; }
        public string StableFieldKey { get; }

        /// <summary>
        /// Migration context only. Equality and hashing intentionally exclude this value.
        /// </summary>
        public string ProviderSchemaVersion { get; }

        public bool Equals(FieldIdentity other)
        {
            return string.Equals(ProviderId, other.ProviderId, StringComparison.Ordinal)
                && string.Equals(StableFieldKey, other.StableFieldKey, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is FieldIdentity other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ProviderId + "::" + StableFieldKey);

        public int CompareTo(FieldIdentity other)
        {
            int provider = string.CompareOrdinal(ProviderId, other.ProviderId);
            return provider != 0 ? provider : string.CompareOrdinal(StableFieldKey, other.StableFieldKey);
        }

        public override string ToString() => ProviderId + "::" + StableFieldKey;
        public FieldIdentity WithSchemaVersion(string providerSchemaVersion) => new FieldIdentity(ProviderId, StableFieldKey, providerSchemaVersion);
    }

    public sealed class FieldSchema
    {
        public FieldIdentity Identity { get; set; }
        public string ProviderSchemaVersion { get; set; } = "1";
        public OwnershipPolicy Policy { get; set; } = OwnershipPolicy.Enforced;
        public CollectionMergeStrategy CollectionStrategy { get; set; } = CollectionMergeStrategy.Replace;
        public bool RequiresReplaceWhenChanged { get; set; }
        public string Description { get; set; }
    }

    public sealed class FieldSchemaSet
    {
        readonly List<FieldSchema> _fields = new List<FieldSchema>();

        public IReadOnlyList<FieldSchema> Fields => _fields;
        public void Add(FieldSchema schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            _fields.Add(schema);
        }
    }

    public sealed class NormalizedField
    {
        public NormalizedField(FieldIdentity identity, OwnershipPolicy policy, AuthoringFieldValue value)
        {
            Identity = identity;
            Policy = policy;
            Value = value;
        }

        public FieldIdentity Identity { get; }
        public OwnershipPolicy Policy { get; }
        public AuthoringFieldValue Value { get; }
    }

    public sealed class NormalizedSnapshot
    {
        readonly List<NormalizedField> _fields = new List<NormalizedField>();

        public string SchemaVersion { get; set; } = "1";
        public IReadOnlyList<NormalizedField> Fields => _fields;

        public void AddField(NormalizedField field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            _fields.Add(field);
        }

        public bool TryGetValue(FieldIdentity identity, out AuthoringFieldValue value)
        {
            for (int i = 0; i < _fields.Count; i++)
            {
                if (_fields[i].Identity.Equals(identity))
                {
                    value = _fields[i].Value;
                    return true;
                }
            }

            value = AuthoringFieldValue.Absent();
            return false;
        }

        public string ToCanonicalPayload()
        {
            var copy = new List<NormalizedField>(_fields);
            copy.Sort((a, b) => a.Identity.CompareTo(b.Identity));

            var builder = new StringBuilder(512);
            builder.Append("{\"schemaVersion\":");
            AppendJsonString(builder, SchemaVersion ?? "1");
            builder.Append(",\"fields\":[");
            for (int i = 0; i < copy.Count; i++)
            {
                if (i > 0) builder.Append(',');
                var field = copy[i];
                builder.Append("{\"identity\":");
                AppendJsonString(builder, field.Identity.ToString());
                builder.Append(",\"policy\":");
                AppendJsonString(builder, field.Policy.ToString());
                builder.Append(",\"value\":");
                builder.Append(CanonicalSerialization.Serialize(field.Value));
                builder.Append('}');
            }
            builder.Append("]}");
            return builder.ToString();
        }

        static void AppendJsonString(StringBuilder builder, string value)
        {
            builder.Append('"');
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    if (c == '\\') builder.Append("\\\\");
                    else if (c == '"') builder.Append("\\\"");
                    else if (c == '\n') builder.Append("\\n");
                    else if (c == '\r') builder.Append("\\r");
                    else if (c == '\t') builder.Append("\\t");
                    else if (c < 32) builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else builder.Append(c);
                }
            }
            builder.Append('"');
        }
    }

    public static class NormalizedSnapshotHasher
    {
        public static string Hash(NormalizedSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return CanonicalSerialization.Sha256(snapshot.ToCanonicalPayload());
        }
    }

    public enum ResourceProvenance
    {
        CreatedByFramework,
        AdoptedByFramework,
        SourceOwned,
        External
    }

    public sealed class SharedResourceOwnership
    {
        readonly SortedSet<string> _owners = new SortedSet<string>(StringComparer.Ordinal);

        public SharedResourceOwnership(string resourceId, ResourceProvenance provenance)
        {
            ResourceId = AuthoringIdentifier.Require(resourceId, nameof(resourceId));
            Provenance = provenance;
        }

        public string ResourceId { get; }
        public ResourceProvenance Provenance { get; }
        public IReadOnlyCollection<string> Owners => _owners;

        public void AddOwner(string ownerId) => _owners.Add(AuthoringIdentifier.Require(ownerId, nameof(ownerId)));
        public void RemoveOwner(string ownerId)
        {
            if (!string.IsNullOrWhiteSpace(ownerId)) _owners.Remove(ownerId);
        }

        public bool CanDeleteWhenUnused()
        {
            return _owners.Count == 0 && (Provenance == ResourceProvenance.CreatedByFramework || Provenance == ResourceProvenance.AdoptedByFramework);
        }
    }
}
