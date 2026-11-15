using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Applies the category-based apparel rules configured under
    /// Mod Options → Infected Apparel Compatibility.
    ///
    /// The category toggles are independent: disabling a category blocks every
    /// apparel item that matches it. <c>allowInfectedWearApparelWithTags</c>
    /// controls apparel whose tags are not recognized by any of the categories
    /// (for example tags added by other mods); it does not override the
    /// category toggles.
    /// </summary>
    public static class CrossedApparelCompatibility
    {
        private static readonly string[] MilitaryTags =
        {
            "IndustrialMilitaryBasic",
            "IndustrialMilitaryAdvanced",
            "SpacerMilitary",
            "Military",
            "MilitaryBasic",
            "MilitaryAdvanced"
        };

        private static readonly string[] PoweredArmorKeywords =
        {
            "PowerArmor",
            "ArmorRecon",
            "ArmorMarine",
            "ArmorCataphract",
            "ArmorLocust",
            "ArmorPhoenix",
            "MechlordSuit",
            "ArmorAbsolver",
            "ArmorDeserter",
            "power armor",
            "recon armor",
            "marine armor",
            "cataphract armor"
        };

        public static bool CanWearApparel(Pawn pawn, ThingDef def)
        {
            if (pawn == null || def?.apparel == null)
            {
                return false;
            }

            // Virus-transformed original-kind pawns keep their pre-infection
            // gear by design and are never restricted by these options.
            if (CrossedUtility.IsMarkedTurnedPawn(pawn))
            {
                return true;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null)
            {
                return true;
            }

            if (!settings.allowInfectedWearHelmets && def.apparel.LastLayer == ApparelLayerDefOf.Overhead)
            {
                return false;
            }

            if (!settings.allowInfectedWearShields && IsShield(def))
            {
                return false;
            }

            if (!settings.allowInfectedWearPoweredArmor && IsPoweredArmor(def))
            {
                return false;
            }

            bool tribal = HasTag(def, "Tribal");
            bool neolithic = HasTag(def, "Neolithic");
            bool military = HasMilitaryTag(def);
            bool scout = HasScoutTag(def);

            if (tribal && !settings.allowInfectedWearTribalApparel)
            {
                return false;
            }

            if (neolithic && !settings.allowInfectedWearNeolithicApparel)
            {
                return false;
            }

            if (military && !settings.allowInfectedWearMilitaryApparel)
            {
                return false;
            }

            if (scout && !settings.allowInfectedWearScoutApparel)
            {
                return false;
            }

            bool hasTags = def.apparel.tags != null && def.apparel.tags.Count > 0;
            bool recognizedCategory = tribal || neolithic || military || scout;
            if (hasTags && !recognizedCategory && !settings.allowInfectedWearApparelWithTags)
            {
                return false;
            }

            return true;
        }

        public static bool IsShield(ThingDef def)
        {
            if (def?.apparel == null)
            {
                return false;
            }

            if (ContainsOrdinalIgnoreCase(def.defName, "ShieldBelt")
                || ContainsOrdinalIgnoreCase(def.defName, "Shield_Belt"))
            {
                return true;
            }

            if (def.apparel.LastLayer != ApparelLayerDefOf.Belt)
            {
                return false;
            }

            try
            {
                return def.GetStatValueAbstract(StatDefOf.EnergyShieldEnergyMax) > 0f;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsPoweredArmor(ThingDef def)
        {
            if (def?.apparel == null)
            {
                return false;
            }

            for (int i = 0; i < PoweredArmorKeywords.Length; i++)
            {
                string keyword = PoweredArmorKeywords[i];
                if (ContainsOrdinalIgnoreCase(def.defName, keyword)
                    || ContainsOrdinalIgnoreCase(def.label, keyword))
                {
                    return true;
                }
            }

            List<string> tags = def.apparel.tags;
            if (tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                if (ContainsOrdinalIgnoreCase(tags[i], "PowerArmor")
                    || ContainsOrdinalIgnoreCase(tags[i], "PoweredArmor"))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasTribalTag(ThingDef def)
        {
            return HasTag(def, "Tribal");
        }

        public static bool HasNeolithicTag(ThingDef def)
        {
            return HasTag(def, "Neolithic");
        }

        public static bool HasMilitaryTag(ThingDef def)
        {
            List<string> tags = def?.apparel?.tags;
            if (tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                for (int j = 0; j < MilitaryTags.Length; j++)
                {
                    if (string.Equals(tag, MilitaryTags[j], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                if (ContainsOrdinalIgnoreCase(tag, "Military"))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasScoutTag(ThingDef def)
        {
            List<string> tags = def?.apparel?.tags;
            if (tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (ContainsOrdinalIgnoreCase(tag, "Scout")
                    || ContainsOrdinalIgnoreCase(tag, "Skirmisher"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasTag(ThingDef def, string tag)
        {
            List<string> tags = def?.apparel?.tags;
            if (tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                if (ContainsOrdinalIgnoreCase(tags[i], tag))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsOrdinalIgnoreCase(string text, string value)
        {
            return !string.IsNullOrEmpty(text)
                && !string.IsNullOrEmpty(value)
                && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
