using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace BlackMountains.AuthoringKernel
{
    public enum AuthoringValueState
    {
        Unspecified,
        Absent,
        Null,
        Known,
        Unknown,
        Computed
    }

    public enum CanonicalValueKind
    {
        Bool,
        Integer,
        Decimal,
        String,
        Enum,
        Vector,
        Quaternion,
        Color,
        AssetReference,
        ObjectReference,
        Array,
        Map
    }

    public sealed class CanonicalValue : IEquatable<CanonicalValue>
    {
        CanonicalValue(CanonicalValueKind kind, string scalar, IReadOnlyList<CanonicalValue> items, IReadOnlyDictionary<string, CanonicalValue> properties)
        {
            Kind = kind;
            Scalar = scalar ?? string.Empty;
            Items = items ?? System.Array.Empty<CanonicalValue>();
            Properties = properties ?? new ReadOnlyDictionary<string, CanonicalValue>(new Dictionary<string, CanonicalValue>());
        }

        public CanonicalValueKind Kind { get; }
        public string Scalar { get; }
        public IReadOnlyList<CanonicalValue> Items { get; }
        public IReadOnlyDictionary<string, CanonicalValue> Properties { get; }

        public static CanonicalValue Bool(bool value) => new CanonicalValue(CanonicalValueKind.Bool, value ? "true" : "false", null, null);
        public static CanonicalValue Integer(long value) => new CanonicalValue(CanonicalValueKind.Integer, value.ToString(CultureInfo.InvariantCulture), null, null);
        public static CanonicalValue Decimal(decimal value) => new CanonicalValue(CanonicalValueKind.Decimal, value.ToString("0.#############################", CultureInfo.InvariantCulture), null, null);
        public static CanonicalValue String(string value) => new CanonicalValue(CanonicalValueKind.String, value ?? string.Empty, null, null);
        public static CanonicalValue Enum(string value) => new CanonicalValue(CanonicalValueKind.Enum, AuthoringIdentifier.Require(value, nameof(value)), null, null);
        public static CanonicalValue AssetReference(string guid, long localId = 0) => Map(new Dictionary<string, CanonicalValue>
        {
            ["guid"] = String(new AssetId(guid, localId).Guid),
            ["localId"] = Integer(localId)
        }, CanonicalValueKind.AssetReference);
        public static CanonicalValue ObjectReference(string stableObjectId) => new CanonicalValue(CanonicalValueKind.ObjectReference, AuthoringIdentifier.Require(stableObjectId, nameof(stableObjectId)), null, null);
        public static CanonicalValue Vector(params decimal[] components) => Array(CanonicalValueKind.Vector, components.Select(Decimal));
        public static CanonicalValue Quaternion(params decimal[] components) => Array(CanonicalValueKind.Quaternion, components.Select(Decimal));
        public static CanonicalValue Color(params decimal[] components) => Array(CanonicalValueKind.Color, components.Select(Decimal));
        public static CanonicalValue Array(IEnumerable<CanonicalValue> items) => Array(CanonicalValueKind.Array, items);
        public static CanonicalValue Map(IDictionary<string, CanonicalValue> properties) => Map(properties, CanonicalValueKind.Map);

        static CanonicalValue Array(CanonicalValueKind kind, IEnumerable<CanonicalValue> items)
        {
            var copy = items == null ? new List<CanonicalValue>() : new List<CanonicalValue>(items.Select(v => v ?? throw new ArgumentNullException(nameof(items))));
            return new CanonicalValue(kind, string.Empty, copy.AsReadOnly(), null);
        }

        static CanonicalValue Map(IDictionary<string, CanonicalValue> properties, CanonicalValueKind kind)
        {
            var sorted = new SortedDictionary<string, CanonicalValue>(StringComparer.Ordinal);
            if (properties != null)
            {
                foreach (var pair in properties)
                {
                    if (string.IsNullOrEmpty(pair.Key)) throw new ArgumentException("Canonical map keys cannot be empty.", nameof(properties));
                    sorted[pair.Key] = pair.Value ?? throw new ArgumentNullException(nameof(properties));
                }
            }
            return new CanonicalValue(kind, string.Empty, null, new ReadOnlyDictionary<string, CanonicalValue>(sorted));
        }

        public bool Equals(CanonicalValue other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null || Kind != other.Kind || !string.Equals(Scalar, other.Scalar, StringComparison.Ordinal)) return false;
            if (Items.Count != other.Items.Count || Properties.Count != other.Properties.Count) return false;
            for (int i = 0; i < Items.Count; i++)
                if (!Items[i].Equals(other.Items[i])) return false;
            foreach (var pair in Properties)
            {
                if (!other.Properties.TryGetValue(pair.Key, out var value) || !pair.Value.Equals(value))
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj) => obj is CanonicalValue other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(CanonicalSerialization.Serialize(this));
        public override string ToString() => CanonicalSerialization.Serialize(this);
    }

    public readonly struct AuthoringFieldValue : IEquatable<AuthoringFieldValue>
    {
        AuthoringFieldValue(AuthoringValueState state, CanonicalValue value)
        {
            State = state;
            Value = value;
        }

        public AuthoringValueState State { get; }
        public CanonicalValue Value { get; }

        public static AuthoringFieldValue Unspecified() => new AuthoringFieldValue(AuthoringValueState.Unspecified, null);
        public static AuthoringFieldValue Absent() => new AuthoringFieldValue(AuthoringValueState.Absent, null);
        public static AuthoringFieldValue Null() => new AuthoringFieldValue(AuthoringValueState.Null, null);
        public static AuthoringFieldValue Unknown() => new AuthoringFieldValue(AuthoringValueState.Unknown, null);
        public static AuthoringFieldValue Computed() => new AuthoringFieldValue(AuthoringValueState.Computed, null);
        public static AuthoringFieldValue Known(CanonicalValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return new AuthoringFieldValue(AuthoringValueState.Known, value);
        }

        public bool Equals(AuthoringFieldValue other)
        {
            if (State != other.State) return false;
            return State != AuthoringValueState.Known || Value.Equals(other.Value);
        }

        public override bool Equals(object obj) => obj is AuthoringFieldValue other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(CanonicalSerialization.Serialize(this));
        public override string ToString() => CanonicalSerialization.Serialize(this);
        public static bool operator ==(AuthoringFieldValue left, AuthoringFieldValue right) => left.Equals(right);
        public static bool operator !=(AuthoringFieldValue left, AuthoringFieldValue right) => !left.Equals(right);
    }
}
