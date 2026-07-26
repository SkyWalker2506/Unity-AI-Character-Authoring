using System;
using System.Collections.Generic;
using BlackMountains.AuthoringKernel;

namespace BlackMountains.AICharacterAuthoring
{
    [Serializable]
    public sealed class NpcModularEquipmentCatalog
    {
        public const string CurrentSchemaVersion = "1";

        public string SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string StableId { get; set; }
        public string Version { get; set; } = "1";
        public List<NpcModularEquipmentItemRecord> Items { get; set; } =
            new List<NpcModularEquipmentItemRecord>();
    }

    [Serializable]
    public sealed class NpcModularEquipmentQuery
    {
        public NpcEquipmentSlot Slot { get; set; }
        public string RigId { get; set; }
        public string BodyId { get; set; }
        public List<string> RequiredTags { get; set; } = new List<string>();
        public List<string> RequiredStyleTags { get; set; } = new List<string>();
        public List<string> PreferredVariantIds { get; set; } = new List<string>();
        public List<string> RequiredAnimationCompatibilityTags { get; set; } =
            new List<string>();
        public bool RequireStpItemCompatibility { get; set; }
    }

    public sealed class NpcModularEquipmentSelectionResult
    {
        public NpcModularEquipmentItemRecord Selected { get; set; }
        public List<NpcModularEquipmentItemRecord> Candidates { get; } =
            new List<NpcModularEquipmentItemRecord>();
        public DiagnosticList Diagnostics { get; } = new DiagnosticList();
        public bool Success => Selected != null && !Diagnostics.HasErrors;
    }

    public static class NpcModularEquipmentSelector
    {
        public static NpcModularEquipmentSelectionResult Select(
            NpcModularEquipmentCatalog catalog,
            NpcModularEquipmentQuery query)
        {
            var result = new NpcModularEquipmentSelectionResult();
            if (catalog == null || query == null)
            {
                result.Diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-EQUIPMENT-QUERY-NULL",
                    "Equipment catalog and query are required."));
                return result;
            }

            ValidateCatalog(catalog, result.Diagnostics);
            if (result.Diagnostics.HasErrors)
                return result;

            foreach (var item in catalog.Items)
            {
                if (item == null || item.Slot != query.Slot)
                    continue;
                if (!MatchesCompatibility(item.CompatibleRigIds, query.RigId) ||
                    !MatchesCompatibility(item.CompatibleBodyIds, query.BodyId) ||
                    !ContainsAll(item.Tags, query.RequiredTags) ||
                    !ContainsAll(item.StyleTags, query.RequiredStyleTags) ||
                    !ContainsAll(
                        item.AnimationCompatibilityTags,
                        query.RequiredAnimationCompatibilityTags) ||
                    (query.RequireStpItemCompatibility && !item.SupportsStpItem))
                {
                    continue;
                }

                result.Candidates.Add(item);
            }

            result.Candidates.Sort((left, right) =>
            {
                int preferred = IsPreferred(query.PreferredVariantIds, right.VariantId)
                    .CompareTo(IsPreferred(query.PreferredVariantIds, left.VariantId));
                return preferred != 0
                    ? preferred
                    : string.CompareOrdinal(left.StableId, right.StableId);
            });

            if (result.Candidates.Count == 0)
            {
                result.Diagnostics.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-EQUIPMENT-NO-MATCH",
                    "No modular equipment item matches the deterministic query.",
                    query.Slot.ToString()));
                return result;
            }

            result.Selected = result.Candidates[0];
            return result;
        }

        public static void ValidateCatalog(
            NpcModularEquipmentCatalog catalog,
            DiagnosticList diagnostics)
        {
            if (catalog == null)
            {
                diagnostics?.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-EQUIPMENT-CATALOG-NULL",
                    "Equipment catalog is required."));
                return;
            }

            if (catalog.SchemaVersion != NpcModularEquipmentCatalog.CurrentSchemaVersion)
            {
                diagnostics?.Add(AuthoringDiagnostic.Error(
                    "ACA-NPC-EQUIPMENT-CATALOG-SCHEMA",
                    "Unsupported modular equipment catalog schema.",
                    catalog.SchemaVersion));
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in catalog.Items ?? new List<NpcModularEquipmentItemRecord>())
            {
                if (item == null || !AuthoringIdentifier.IsValid(item.StableId))
                {
                    diagnostics?.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-EQUIPMENT-ID",
                        "Equipment stable ID is invalid.",
                        catalog.StableId));
                    continue;
                }

                if (!ids.Add(item.StableId))
                {
                    diagnostics?.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-EQUIPMENT-DUPLICATE",
                        "Equipment stable ID is duplicated.",
                        item.StableId));
                }
                if (!AuthoringIdentifier.IsValid(item.SourceProviderId))
                {
                    diagnostics?.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-EQUIPMENT-PROVIDER",
                        "Equipment source provider ID is invalid.",
                        item.StableId));
                }
                if (item.SupportsStpItem && !AuthoringIdentifier.IsValid(item.StpItemLogicalId))
                {
                    diagnostics?.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-EQUIPMENT-STP-ID",
                        "STP-compatible equipment requires a stable STP item logical ID.",
                        item.StableId));
                }
                if ((item.Slot == NpcEquipmentSlot.PrimaryWeapon ||
                     item.Slot == NpcEquipmentSlot.SecondaryWeapon) &&
                    (!AuthoringIdentifier.IsValid(item.ContactEventSemantic) ||
                     !AuthoringIdentifier.IsValid(item.HolsterSlotLogicalId) ||
                     item.WeaponGrip == NpcWeaponGrip.Unknown))
                {
                    diagnostics?.Add(AuthoringDiagnostic.Error(
                        "ACA-NPC-EQUIPMENT-WEAPON-METADATA",
                        "Weapon equipment requires grip, contact-event, and holster metadata.",
                        item.StableId));
                }
            }

            foreach (var item in catalog.Items ?? new List<NpcModularEquipmentItemRecord>())
            {
                if (item == null)
                    continue;
                foreach (var dependency in item.Dependencies ?? new List<string>())
                {
                    if (!ids.Contains(dependency))
                    {
                        diagnostics?.Add(AuthoringDiagnostic.Error(
                            "ACA-NPC-EQUIPMENT-DEPENDENCY",
                            "Equipment dependency is unresolved.",
                            item.StableId + " -> " + dependency));
                    }
                }
            }
        }

        static bool MatchesCompatibility(IList<string> compatibleIds, string requestedId)
        {
            if (compatibleIds == null || compatibleIds.Count == 0)
                return true;
            if (string.IsNullOrWhiteSpace(requestedId))
                return false;
            foreach (var value in compatibleIds)
                if (string.Equals(value, requestedId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        static bool ContainsAll(IList<string> available, IList<string> required)
        {
            if (required == null || required.Count == 0)
                return true;
            var values = new HashSet<string>(available ?? new List<string>(), StringComparer.Ordinal);
            foreach (var requiredValue in required)
                if (!values.Contains(requiredValue))
                    return false;
            return true;
        }

        static bool IsPreferred(IList<string> preferredIds, string variantId)
        {
            if (preferredIds == null)
                return false;
            foreach (var preferred in preferredIds)
                if (string.Equals(preferred, variantId, StringComparison.Ordinal))
                    return true;
            return false;
        }
    }
}
