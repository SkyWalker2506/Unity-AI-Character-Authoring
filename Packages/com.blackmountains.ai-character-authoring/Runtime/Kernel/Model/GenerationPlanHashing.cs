using System;
using System.Collections.Generic;

namespace BlackMountains.AuthoringKernel
{
    /// <summary>
    /// Total order over plan operations. Two runs that produce the same operation set must produce
    /// the same sequence, otherwise the plan digest is not comparable across runs.
    /// </summary>
    public sealed class GenerationOperationComparer : IComparer<GenerationOperation>
    {
        public static readonly GenerationOperationComparer Instance = new GenerationOperationComparer();

        public int Compare(GenerationOperation x, GenerationOperation y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            int phase = x.Phase.CompareTo(y.Phase);
            if (phase != 0) return phase;
            int asset = string.CompareOrdinal(x.TargetAssetGuid ?? string.Empty, y.TargetAssetGuid ?? string.Empty);
            if (asset != 0) return asset;
            int obj = string.CompareOrdinal(x.TargetObjectId ?? string.Empty, y.TargetObjectId ?? string.Empty);
            if (obj != 0) return obj;
            int provider = string.CompareOrdinal(x.ProviderId ?? string.Empty, y.ProviderId ?? string.Empty);
            if (provider != 0) return provider;
            int kind = string.CompareOrdinal(x.OperationKind ?? string.Empty, y.OperationKind ?? string.Empty);
            if (kind != 0) return kind;
            return string.CompareOrdinal(x.StableOperationKey ?? string.Empty, y.StableOperationKey ?? string.Empty);
        }
    }

    /// <summary>
    /// Canonical plan digest. The key grammar below is the wire contract: renaming a key changes every
    /// previously issued plan hash and every approval token bound to it. Property renames on
    /// <see cref="GenerationPlan"/> must not change these keys.
    /// </summary>
    public static class GenerationPlanHasher
    {
        public static string Hash(GenerationPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var map = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["schemaVersion"] = plan.SchemaVersion ?? string.Empty,
                ["planId"] = plan.PlanId ?? string.Empty,
                ["specId"] = plan.SubjectSpecId ?? string.Empty,
                ["source.guid"] = plan.Source?.PrefabGuid ?? string.Empty,
                ["source.version"] = plan.Source?.SourceVersion ?? string.Empty,
                ["source.normalizedHash"] = plan.Source?.SourceNormalizedHash ?? string.Empty
            };

            AddStringMap(map, "metadata.", plan.Metadata);

            var operations = new List<GenerationOperation>(plan.Operations ?? new List<GenerationOperation>());
            operations.Sort(GenerationOperationComparer.Instance);
            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                string prefix = "op." + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".";
                map[prefix + "id"] = op.OperationId ?? string.Empty;
                map[prefix + "phase"] = ((int)op.Phase).ToString(System.Globalization.CultureInfo.InvariantCulture);
                map[prefix + "logicalTarget"] = op.LogicalTarget ?? string.Empty;
                map[prefix + "asset"] = op.TargetAssetGuid ?? string.Empty;
                map[prefix + "object"] = op.TargetObjectId ?? string.Empty;
                map[prefix + "provider"] = op.ProviderId ?? string.Empty;
                map[prefix + "kind"] = op.OperationKind ?? string.Empty;
                map[prefix + "key"] = op.StableOperationKey ?? string.Empty;
                map[prefix + "security"] = ((int)op.SecurityLevel).ToString(System.Globalization.CultureInfo.InvariantCulture);
                map[prefix + "reversibility"] = ((int)op.Reversibility).ToString(System.Globalization.CultureInfo.InvariantCulture);

                if (op.Inputs != null)
                {
                    foreach (var input in new SortedDictionary<string, AuthoringFieldValue>(op.Inputs, StringComparer.Ordinal))
                        map[prefix + "input." + input.Key] = CanonicalSerialization.Serialize(input.Value);
                }

                AddStringList(map, prefix + "precondition.", op.Preconditions);
                AddStringList(map, prefix + "postcondition.", op.Postconditions);
            }

            return CanonicalSerialization.Sha256(CanonicalSerialization.SerializeMap(map));
        }

        static void AddStringMap(
            IDictionary<string, string> destination,
            string prefix,
            IDictionary<string, string> values)
        {
            if (values == null) return;
            foreach (var pair in new SortedDictionary<string, string>(values, StringComparer.Ordinal))
                destination[prefix + pair.Key] = pair.Value ?? string.Empty;
        }

        static void AddStringList(
            IDictionary<string, string> destination,
            string prefix,
            IList<string> values)
        {
            if (values == null)
            {
                destination[prefix + "count"] = "0";
                return;
            }

            destination[prefix + "count"] = values.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            for (int i = 0; i < values.Count; i++)
                destination[prefix + i.ToString(System.Globalization.CultureInfo.InvariantCulture)] = values[i] ?? string.Empty;
        }
    }
}
