using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BlackMountains.AICharacterAuthoring
{
    public static class CanonicalSerialization
    {
        public static string Serialize(AuthoringFieldValue value)
        {
            var builder = new StringBuilder(128);
            builder.Append("{\"state\":");
            WriteString(builder, value.State.ToString());
            if (value.State == AuthoringValueState.Known)
            {
                builder.Append(",\"value\":");
                WriteValue(builder, value.Value);
            }
            builder.Append('}');
            return builder.ToString();
        }

        public static string Serialize(CanonicalValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var builder = new StringBuilder(128);
            WriteValue(builder, value);
            return builder.ToString();
        }

        public static string SerializeMap(IReadOnlyDictionary<string, string> values)
        {
            var builder = new StringBuilder(256);
            builder.Append('{');
            bool first = true;
            foreach (var key in SortedKeys(values))
            {
                if (!first) builder.Append(',');
                first = false;
                WriteString(builder, key);
                builder.Append(':');
                WriteString(builder, values[key] ?? string.Empty);
            }
            builder.Append('}');
            return builder.ToString();
        }

        public static string Sha256(string canonicalPayload)
        {
            if (canonicalPayload == null) throw new ArgumentNullException(nameof(canonicalPayload));
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonicalPayload));
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        public static string Hash(CanonicalValue value) => Sha256(Serialize(value));
        public static string Hash(AuthoringFieldValue value) => Sha256(Serialize(value));

        static void WriteValue(StringBuilder builder, CanonicalValue value)
        {
            builder.Append("{\"kind\":");
            WriteString(builder, value.Kind.ToString());
            if (value.Items.Count > 0 || value.Kind == CanonicalValueKind.Array || value.Kind == CanonicalValueKind.Vector || value.Kind == CanonicalValueKind.Quaternion || value.Kind == CanonicalValueKind.Color)
            {
                builder.Append(",\"items\":[");
                for (int i = 0; i < value.Items.Count; i++)
                {
                    if (i > 0) builder.Append(',');
                    WriteValue(builder, value.Items[i]);
                }
                builder.Append(']');
            }
            else if (value.Properties.Count > 0 || value.Kind == CanonicalValueKind.Map || value.Kind == CanonicalValueKind.AssetReference)
            {
                builder.Append(",\"properties\":{");
                bool first = true;
                foreach (var key in SortedKeys(value.Properties))
                {
                    if (!first) builder.Append(',');
                    first = false;
                    WriteString(builder, key);
                    builder.Append(':');
                    WriteValue(builder, value.Properties[key]);
                }
                builder.Append('}');
            }
            else
            {
                builder.Append(",\"value\":");
                WriteString(builder, value.Scalar);
            }
            builder.Append('}');
        }

        static IEnumerable<string> SortedKeys<T>(IReadOnlyDictionary<string, T> values)
        {
            if (values == null) yield break;
            var keys = new List<string>(values.Keys);
            keys.Sort(StringComparer.Ordinal);
            for (int i = 0; i < keys.Count; i++) yield return keys[i];
        }

        static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    switch (c)
                    {
                        case '\\': builder.Append("\\\\"); break;
                        case '"': builder.Append("\\\""); break;
                        case '\n': builder.Append("\\n"); break;
                        case '\r': builder.Append("\\r"); break;
                        case '\t': builder.Append("\\t"); break;
                        default:
                            if (c < 32)
                                builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            else
                                builder.Append(c);
                            break;
                    }
                }
            }
            builder.Append('"');
        }
    }
}
