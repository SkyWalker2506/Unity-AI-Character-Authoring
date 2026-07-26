using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BlackMountains.AuthoringKernel
{
    /// <summary>
    /// Shared validation for stable framework identifiers. Identifiers are logical data, not asset paths.
    /// </summary>
    public static class AuthoringIdentifier
    {
        static readonly Regex ValidIdPattern = new Regex("^[a-z0-9][a-z0-9._:-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool IsValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && ValidIdPattern.IsMatch(value);
        }

        public static string Require(string value, string parameterName)
        {
            if (!IsValid(value))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid authoring identifier '{0}'.", value), parameterName);
            return value;
        }
    }

    public readonly struct SubjectId : IEquatable<SubjectId>, IComparable<SubjectId>
    {
        public SubjectId(string value) => Value = AuthoringIdentifier.Require(value, nameof(value));
        public string Value { get; }
        public bool Equals(SubjectId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SubjectId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public int CompareTo(SubjectId other) => string.CompareOrdinal(Value, other.Value);
        public override string ToString() => Value ?? string.Empty;
        public static implicit operator string(SubjectId id) => id.Value;
    }

    public readonly struct ProviderId : IEquatable<ProviderId>, IComparable<ProviderId>
    {
        public ProviderId(string value) => Value = AuthoringIdentifier.Require(value, nameof(value));
        public string Value { get; }
        public bool Equals(ProviderId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ProviderId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public int CompareTo(ProviderId other) => string.CompareOrdinal(Value, other.Value);
        public override string ToString() => Value ?? string.Empty;
        public static implicit operator string(ProviderId id) => id.Value;
    }

    public readonly struct CapabilityId : IEquatable<CapabilityId>, IComparable<CapabilityId>
    {
        public CapabilityId(string value) => Value = AuthoringIdentifier.Require(value, nameof(value));
        public string Value { get; }
        public bool Equals(CapabilityId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CapabilityId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public int CompareTo(CapabilityId other) => string.CompareOrdinal(Value, other.Value);
        public override string ToString() => Value ?? string.Empty;
        public static implicit operator string(CapabilityId id) => id.Value;
    }

    public readonly struct FieldKey : IEquatable<FieldKey>, IComparable<FieldKey>
    {
        public FieldKey(string value) => Value = AuthoringIdentifier.Require(value, nameof(value));
        public string Value { get; }
        public bool Equals(FieldKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is FieldKey other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public int CompareTo(FieldKey other) => string.CompareOrdinal(Value, other.Value);
        public override string ToString() => Value ?? string.Empty;
        public static implicit operator string(FieldKey id) => id.Value;
    }

    public readonly struct OperationId : IEquatable<OperationId>, IComparable<OperationId>
    {
        public OperationId(string value) => Value = AuthoringIdentifier.Require(value, nameof(value));
        public string Value { get; }
        public bool Equals(OperationId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is OperationId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public int CompareTo(OperationId other) => string.CompareOrdinal(Value, other.Value);
        public override string ToString() => Value ?? string.Empty;
        public static implicit operator string(OperationId id) => id.Value;
    }

    public readonly struct AssetId : IEquatable<AssetId>, IComparable<AssetId>
    {
        public AssetId(string guid, long localFileId = 0)
        {
            if (string.IsNullOrWhiteSpace(guid) || guid.Length != 32)
                throw new ArgumentException("Asset GUID must be 32 characters.", nameof(guid));
            Guid = guid.ToLowerInvariant();
            LocalFileId = localFileId;
        }

        public string Guid { get; }
        public long LocalFileId { get; }
        public bool Equals(AssetId other) => string.Equals(Guid, other.Guid, StringComparison.Ordinal) && LocalFileId == other.LocalFileId;
        public override bool Equals(object obj) => obj is AssetId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode((Guid ?? string.Empty) + ":" + LocalFileId.ToString(CultureInfo.InvariantCulture));
        public int CompareTo(AssetId other)
        {
            int guid = string.CompareOrdinal(Guid, other.Guid);
            return guid != 0 ? guid : LocalFileId.CompareTo(other.LocalFileId);
        }

        public override string ToString() => LocalFileId == 0 ? Guid : Guid + ":" + LocalFileId.ToString(CultureInfo.InvariantCulture);
    }
}
