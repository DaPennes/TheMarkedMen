using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static class CrossedEquipmentGenerator
    {
        private const int TierCount = 7;

        private static bool cacheBuilt;
        private static bool initAttempted;

        private static readonly HashSet<string> ExcludedDefNames = new()
        {
            "VAE_Headgear_TrooperHelmet",
            "VAE_Apparel_TrooperArmor",
        };

        private static readonly BodyPartGroupDef LegsGroup;
        private static readonly BodyPartGroupDef TorsoGroup;
        private static readonly ApparelLayerDef OnSkinLayer;
        private static readonly ApparelLayerDef MiddleLayer;
        private static readonly ApparelLayerDef ShellLayer;
        private static readonly ApparelLayerDef BeltLayer;
        private static readonly ApparelLayerDef OverheadLayer;

        private static List<ThingDef>[] apparelByTier;
        private static List<ThingDef>[] headgearByTier;
        private static List<ThingDef>[] shieldsByTier;

        static CrossedEquipmentGenerator()
        {
            LegsGroup = BodyPartGroupDefOf.Legs;
            TorsoGroup = BodyPartGroupDefOf.Torso;
            OnSkinLayer = ApparelLayerDefOf.OnSkin;
            MiddleLayer = ApparelLayerDefOf.Middle;
            ShellLayer = ApparelLayerDefOf.Shell;
            BeltLayer = ApparelLayerDefOf.Belt;
            OverheadLayer = ApparelLayerDefOf.Overhead;
        }

        private static readonly float[][] QualityWeights =
        {
            new[] { 0.40f, 0.35f, 0.20f, 0.05f, 0f,    0f,    0f     },
            new[] { 0.20f, 0.35f, 0.30f, 0.12f, 0.03f, 0f,    0f     },
            new[] { 0.08f, 0.22f, 0.35f, 0.25f, 0.08f, 0.02f, 0f     },
            new[] { 0.03f, 0.10f, 0.30f, 0.32f, 0.18f, 0.06f, 0.01f  },
            new[] { 0f,    0.05f, 0.15f, 0.30f, 0.30f, 0.16f, 0.04f  },
            new[] { 0f,    0f,    0.08f, 0.20f, 0.32f, 0.28f, 0.12f  },
            new[] { 0f,    0f,    0.02f, 0.10f, 0.25f, 0.33f, 0.30f  },
        };

        private static readonly float[][] DurabilityRanges =
        {
            new[] { 0.15f, 0.80f }, new[] { 0.25f, 0.85f }, new[] { 0.35f, 0.90f },
            new[] { 0.50f, 0.95f }, new[] { 0.65f, 1.00f }, new[] { 0.80f, 1.00f },
            new[] { 0.90f, 1.00f },
        };

        private static readonly string[][] StuffMaterials =
        {
            new[] { "Cloth", "Bluefur", "Bearfur" },
            new[] { "Cloth", "Synthread", "Bluefur", "Wolfskin" },
            new[] { "Cloth", "Synthread", "Devilstrand" },
            new[] { "Synthread", "Devilstrand", "Hyperweave", "Steel", "Plasteel" },
            new[] { "Devilstrand", "Hyperweave", "Plasteel", "Uranium" },
            new[] { "Hyperweave", "Plasteel", "Uranium" },
            new[] { "Hyperweave", "Plasteel", "Uranium" },
        };

        private static readonly List<string> IndustrialMilitaryTags = new()
        {
            "IndustrialMilitaryAdvanced",
            "IndustrialMilitaryBasic",
            "SpacerMilitary",
        };

        private static readonly List<string> CivilianTags = new()
        {
            "IndustrialBasic",
            "Neolithic",
        };

        private static readonly List<string> SpacerTags = new()
        {
            "SpacerMilitary",
        };

        public static void BuildCache()
        {
            if (cacheBuilt || initAttempted)
                return;

            initAttempted = true;

            try
            {
                apparelByTier = new List<ThingDef>[TierCount];
                headgearByTier = new List<ThingDef>[TierCount];
                shieldsByTier = new List<ThingDef>[TierCount];

                for (int i = 0; i < TierCount; i++)
                {
                    apparelByTier[i] = new List<ThingDef>();
                    headgearByTier[i] = new List<ThingDef>();
                    shieldsByTier[i] = new List<ThingDef>();
                }

                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def?.apparel == null)
                        continue;

                    if (ExcludedDefNames.Contains(def.defName))
                        continue;

                    if (def.apparel.LastLayer == OverheadLayer)
                    {
                        int tier = ClassifyApparel(def);
                        headgearByTier[tier].Add(def);
                        continue;
                    }

                    if (IsShield(def))
                    {
                        int tier = ClassifyApparel(def);
                        shieldsByTier[tier].Add(def);
                        continue;
                    }

                    int apparelTier = ClassifyApparel(def);
                    apparelByTier[apparelTier].Add(def);
                }

                BuildWeaponCache();

                cacheBuilt = true;
            }
            catch (Exception ex)
            {
                Log.Error("[The Marked Men] CrossedEquipmentGenerator.BuildCache failed: " + ex.Message);
            }
        }

        private static bool IsShield(ThingDef def)
        {
            if (def.defName.IndexOf("ShieldBelt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                def.defName.IndexOf("Shield_Belt", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            try
            {
                return def.apparel?.LastLayer == ApparelLayerDefOf.Belt
                    && def.GetStatValueAbstract(StatDefOf.EnergyShieldEnergyMax) > 0f;
            }
            catch
            {
                return false;
            }
        }

        public static void AssignEquipment(Pawn pawn)
        {
            if (pawn?.health == null || !IsCrossedKind(pawn.kindDef))
                return;

            if (!cacheBuilt)
                BuildCache();

            if (!cacheBuilt)
                return;

            int tier = GetKindBaseTier(pawn.kindDef);

            EnsureSignatureApparel(pawn, tier, pawn.kindDef);

            StripDisallowedApparel(pawn);

            EquipWeapon(pawn, tier);

            AdjustQuality(pawn, tier);
        }

        private static void EnsureSignatureApparel(Pawn pawn, int tier, PawnKindDef kind)
        {
            if (kind == CADefOf.CrossedSoldier)
            {
                EnsureTaggedOnSkin(pawn, tier, IndustrialMilitaryTags, "military shirt", minSharp: 0f);
                EnsureLayerByTags(pawn, tier, ShellLayer, TorsoGroup, IndustrialMilitaryTags, "flak vest", minSharp: 0.15f);
                EnsureLayerByTags(pawn, tier, OverheadLayer, null, IndustrialMilitaryTags, "military helmet", minSharp: 0.10f);
                EnsureLayerByTags(pawn, tier, MiddleLayer, LegsGroup, IndustrialMilitaryTags, "military pants", minSharp: 0f);
            }
            else if (kind == CADefOf.CrossedBrute)
            {
                EnsureLayerByTags(pawn, tier, ShellLayer, TorsoGroup, IndustrialMilitaryTags, "heavy armor", minSharp: 0.30f);
                EnsureLayerByTags(pawn, tier, OverheadLayer, null, IndustrialMilitaryTags, "heavy helmet", minSharp: 0.20f);
            }
            else if (kind == CADefOf.CrossedShooter)
            {
                EnsureLayerByTags(pawn, tier, ShellLayer, TorsoGroup, IndustrialMilitaryTags, "flak vest", minSharp: 0.15f);
            }
            else if (kind == CADefOf.CrossedRaider)
            {
                if (Rand.Chance(0.50f))
                    EnsureLayerByTags(pawn, tier, ShellLayer, TorsoGroup, IndustrialMilitaryTags, "armor", minSharp: 0.10f);
                if (Rand.Chance(0.40f))
                    EnsureLayerByTags(pawn, tier, OverheadLayer, null, IndustrialMilitaryTags, "helmet", minSharp: 0.05f);
            }
            else if (kind == CADefOf.CrossedScout)
            {
                if (Rand.Chance(0.60f))
                    EnsureLayerByTags(pawn, tier, ShellLayer, TorsoGroup, CivilianTags, "jacket or duster", minSharp: 0f);
            }
            else if (kind == CADefOf.CrossedHunter)
            {
                if (Rand.Chance(0.70f))
                    EnsureLayerByTags(pawn, tier, ShellLayer, TorsoGroup, CivilianTags, "outdoor wear", minSharp: 0f);
            }
            else if (kind == CADefOf.CrossedPyromaniac)
            {
                if (Rand.Chance(0.40f))
                    EnsureLayerByTags(pawn, tier, ShellLayer, TorsoGroup, CivilianTags, "outerwear", minSharp: 0f);
            }
            else if (kind == CADefOf.CrossedAlpha)
            {
                EnsureLayerByTags(pawn, tier, ShellLayer, TorsoGroup, SpacerTags, "elite armor", minSharp: 0.30f);
                EnsureLayerByTags(pawn, tier, OverheadLayer, null, SpacerTags, "elite helmet", minSharp: 0.20f);
            }
            else if (kind == CADefOf.CrossedWarlord)
            {
                EnsureLayerByTags(pawn, tier, ShellLayer, TorsoGroup, IndustrialMilitaryTags, "heavy armor", minSharp: 0.35f);
                EnsureLayerByTags(pawn, tier, OverheadLayer, null, IndustrialMilitaryTags, "elite helmet", minSharp: 0.25f);
            }
            else if (kind == CADefOf.MarkedMan)
            {
                EnsureLayerByTags(pawn, tier, ShellLayer, TorsoGroup, SpacerTags, "apex armor", minSharp: 0.40f);
                EnsureLayerByTags(pawn, tier, OverheadLayer, null, SpacerTags, "apex helmet", minSharp: 0.30f);
                EnsureShield(pawn, tier, 1.0f);
            }

            EnsureShield(pawn, tier, ShieldChanceForKind(kind));
        }

        private static void EnsureTaggedOnSkin(Pawn pawn, int tier, List<string> preferredTags, string label, float minSharp)
        {
            if (HasLayerOnGroup(pawn, OnSkinLayer, TorsoGroup))
            {
                Apparel existing = GetApparelOnLayer(pawn, OnSkinLayer, TorsoGroup);
                if (existing != null && HasAnyTag(existing.def, preferredTags))
                    return;
            }

            ThingDef shirt = FindByTagsAndLayer(preferredTags, OnSkinLayer, TorsoGroup, tier, minSharp);
            if (shirt == null)
                shirt = FindAnyOnLayer(OnSkinLayer, TorsoGroup, 0);

            if (shirt != null && !HasLayerOnGroup(pawn, OnSkinLayer, TorsoGroup))
                EquipApparel(pawn, shirt, tier);
        }

        private static void EnsureLayerByTags(Pawn pawn, int tier, ApparelLayerDef layer, BodyPartGroupDef bodyPart,
            List<string> preferredTags, string label, float minSharp)
        {
            if (bodyPart != null && HasLayerOnGroup(pawn, layer, bodyPart))
            {
                Apparel existing = GetApparelOnLayer(pawn, layer, bodyPart);
                if (existing != null)
                    return;
            }
            else if (bodyPart == null && HasLayer(pawn, layer))
            {
                return;
            }

            ThingDef found = FindByTagsAndLayer(preferredTags, layer, bodyPart, tier, minSharp);
            if (found == null)
                found = FindAnyOnLayer(layer, bodyPart, Mathf.Max(0, tier - 1));

            if (found != null)
            {
                if (bodyPart != null && !HasLayerOnGroup(pawn, layer, bodyPart))
                    EquipApparel(pawn, found, tier);
                else if (bodyPart == null && !HasLayer(pawn, layer))
                    EquipApparel(pawn, found, tier);
            }
        }

        private static ThingDef FindByTagsAndLayer(List<string> preferredTags, ApparelLayerDef layer,
            BodyPartGroupDef bodyPart, int tier, float minSharp)
        {
            for (int t = Mathf.Min(tier + 1, TierCount - 1); t >= 0; t--)
            {
                foreach (ThingDef def in apparelByTier[t])
                {
                    if (def.apparel?.LastLayer != layer)
                        continue;

                    if (bodyPart != null && !CoversAnyGroup(def, bodyPart))
                        continue;

                    float sharp = def.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);
                    if (sharp < minSharp)
                        continue;

                    if (HasAnyTag(def, preferredTags))
                        return def;
                }

                foreach (ThingDef def in headgearByTier[t])
                {
                    if (layer != OverheadLayer)
                        continue;

                    float sharp = def.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);
                    if (sharp < minSharp)
                        continue;

                    if (HasAnyTag(def, preferredTags))
                        return def;
                }
            }

            return null;
        }

        private static ThingDef FindAnyOnLayer(ApparelLayerDef layer, BodyPartGroupDef bodyPart, int minTier)
        {
            for (int t = TierCount - 1; t >= Mathf.Max(0, minTier); t--)
            {
                List<ThingDef> pool = layer == OverheadLayer ? headgearByTier[t] : apparelByTier[t];
                for (int i = 0; i < pool.Count; i++)
                {
                    ThingDef def = pool[i];
                    if (def.apparel?.LastLayer != layer)
                        continue;

                    if (bodyPart != null && !CoversAnyGroup(def, bodyPart))
                        continue;

                    return def;
                }
            }

            return null;
        }

        private static bool HasAnyTag(ThingDef def, List<string> tags)
        {
            if (def.apparel?.tags == null || def.apparel.tags.Count == 0)
                return false;

            for (int i = 0; i < def.apparel.tags.Count; i++)
            {
                string tag = def.apparel.tags[i];
                for (int j = 0; j < tags.Count; j++)
                {
                    if (string.Equals(tag, tags[j], StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private static Apparel GetApparelOnLayer(Pawn pawn, ApparelLayerDef layer, BodyPartGroupDef bodyPart)
        {
            foreach (Apparel ap in pawn.apparel.WornApparel)
            {
                if (ap.def.apparel?.LastLayer == layer && CoversAnyGroup(ap.def, bodyPart))
                    return ap;
            }

            return null;
        }

        private static void EnsureShield(Pawn pawn, int tier, float chance)
        {
            if (!Rand.Chance(chance))
                return;

            if (HasLayer(pawn, BeltLayer))
                return;

            for (int t = Mathf.Min(tier + 1, TierCount - 1); t >= 0; t--)
            {
                List<ThingDef> pool = shieldsByTier[t];
                if (pool.Count > 0)
                {
                    EquipApparel(pawn, pool[Rand.RangeInclusive(0, pool.Count - 1)], tier);
                    return;
                }
            }
        }

        private static void StripDisallowedApparel(Pawn pawn)
        {
            if (pawn?.apparel == null) return;

            PawnKindDef kind = pawn.kindDef;
            bool isArmedKind = kind == CADefOf.CrossedSoldier || kind == CADefOf.CrossedAlpha
                || kind == CADefOf.CrossedWarlord || kind == CADefOf.MarkedMan
                || kind == CADefOf.CrossedBrute || kind == CADefOf.CrossedShooter
                || kind == CADefOf.CrossedRaider;

            bool isCivilianKind = kind == CADefOf.CrossedCivilian || kind == CADefOf.CrossedPyromaniac
                || kind == CADefOf.CrossedScout || kind == CADefOf.CrossedHunter;

            for (int i = pawn.apparel.WornApparel.Count - 1; i >= 0; i--)
            {
                Apparel ap = pawn.apparel.WornApparel[i];
                if (ap == null || ap.Destroyed) continue;

                ThingDef def = ap.def;

                if (def.apparel == null) continue;

                if (CrossedUtility.IsInfectedPawn(pawn) && !CanWearApparel(pawn, def))
                {
                    pawn.apparel.Remove(ap);
                    ap.Destroy(DestroyMode.Vanish);
                    continue;
                }

                if (IsShield(def))
                    continue;

                if (def.apparel.LastLayer == OverheadLayer)
                {
                    if (isCivilianKind && def.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) >= 0.10f)
                    {
                        pawn.apparel.Remove(ap);
                        ap.Destroy(DestroyMode.Vanish);
                    }
                    continue;
                }

                if (isArmedKind)
                {
                    float sharp = def.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);
                    bool hasMilTag = HasAnyMilitaryTag(def);

                    if (sharp < 0.05f && !hasMilTag && def.apparel.LastLayer == ShellLayer)
                    {
                        pawn.apparel.Remove(ap);
                        ap.Destroy(DestroyMode.Vanish);
                    }
                }

                if (isCivilianKind)
                {
                    float sharp = def.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);
                    bool hasMilTag = HasAnyMilitaryTag(def);

                    if (sharp >= 0.20f || hasMilTag)
                    {
                        pawn.apparel.Remove(ap);
                        ap.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        private static bool HasAnyMilitaryTag(ThingDef def)
        {
            if (def.apparel?.tags == null) return false;

            for (int i = 0; i < def.apparel.tags.Count; i++)
            {
                string tag = def.apparel.tags[i];
                for (int j = 0; j < IndustrialMilitaryTags.Count; j++)
                {
                    if (string.Equals(tag, IndustrialMilitaryTags[j], StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private static void AdjustQuality(Pawn pawn, int tier)
        {
            tier = Mathf.Clamp(tier, 0, TierCount - 1);

            foreach (Apparel ap in pawn.apparel.WornApparel)
            {
                if (ap == null || ap.Destroyed) continue;
                ApplyQualityAndHP(ap, tier);
            }

            if (pawn.equipment != null)
            {
                ThingWithComps primary = pawn.equipment.Primary;
                if (primary != null && !primary.Destroyed)
                    ApplyQualityAndHP(primary, tier);
            }
        }

        private static void EquipWeapon(Pawn pawn, int tier)
        {
            if (pawn.equipment == null || weaponsByTier == null)
                return;

            if (pawn.kindDef == CADefOf.CrossedPyromaniac)
                return;

            if (pawn.equipment.Primary != null && !pawn.equipment.Primary.Destroyed)
                return;

            ThingDef weapon = null;

            for (int t = Mathf.Min(tier + 1, TierCount - 1); t >= 0; t--)
            {
                for (int w = 0; w < weaponsByTier[t].Count; w++)
                {
                    ThingDef candidate = weaponsByTier[t][w];
                    if (CanUseWeapon(pawn, candidate))
                    {
                        weapon = candidate;
                        break;
                    }
                }

                if (weapon != null) break;
            }

            if (weapon == null)
            {
                for (int t = TierCount - 1; t >= 0; t--)
                {
                    for (int w = 0; w < weaponsByTier[t].Count; w++)
                    {
                        ThingDef candidate = weaponsByTier[t][w];
                        if (candidate.IsRangedWeapon)
                        {
                            List<VerbProperties> verbs = candidate.Verbs;
                            float range = 0f;
                            if (verbs != null && verbs.Count > 0)
                                range = verbs[0].range;
                            if (range > 75f)
                                continue;
                        }
                        if (candidate.GetStatValueAbstract(StatDefOf.Mass) > 40f)
                            continue;
                        weapon = candidate;
                        break;
                    }
                    if (weapon != null) break;
                }
            }

            if (weapon == null) return;

            ThingDef stuff = null;
            if (weapon.MadeFromStuff)
            {
                string[] mats = StuffMaterials[tier];
                for (int i = 0; i < mats.Length; i++)
                {
                    stuff = DefDatabase<ThingDef>.GetNamedSilentFail(mats[i]);
                    if (stuff != null) break;
                }

                if (stuff == null)
                {
                    for (int fallbackT = tier - 1; fallbackT >= 0; fallbackT--)
                    {
                        string[] fallbackMats = StuffMaterials[fallbackT];
                        for (int i = 0; i < fallbackMats.Length; i++)
                        {
                            stuff = DefDatabase<ThingDef>.GetNamedSilentFail(fallbackMats[i]);
                            if (stuff != null) break;
                        }
                        if (stuff != null) break;
                    }
                }
            }

            ThingWithComps thing;
            try
            {
                thing = stuff != null
                    ? (ThingWithComps)ThingMaker.MakeThing(weapon, stuff)
                    : (ThingWithComps)ThingMaker.MakeThing(weapon);
            }
            catch
            {
                return;
            }

            pawn.equipment.AddEquipment(thing);
        }

        private static void EquipApparel(Pawn pawn, ThingDef def, int tier)
        {
            if (CrossedUtility.IsInfectedPawn(pawn) && !CanWearApparel(pawn, def))
                return;
            ThingDef stuff = null;
            if (def.MadeFromStuff)
            {
                string[] mats = StuffMaterials[tier];
                for (int i = 0; i < mats.Length; i++)
                {
                    stuff = DefDatabase<ThingDef>.GetNamedSilentFail(mats[i]);
                    if (stuff != null) break;
                }

                if (stuff == null)
                {
                    for (int t = tier - 1; t >= 0; t--)
                    {
                        string[] fallbackMats = StuffMaterials[t];
                        for (int i = 0; i < fallbackMats.Length; i++)
                        {
                            stuff = DefDatabase<ThingDef>.GetNamedSilentFail(fallbackMats[i]);
                            if (stuff != null) break;
                        }
                        if (stuff != null) break;
                    }
                }
            }

            Apparel apparel = null;
            try
            {
                apparel = (Apparel)(stuff != null
                    ? ThingMaker.MakeThing(def, stuff)
                    : ThingMaker.MakeThing(def));
                ApplyQualityAndHP(apparel, tier);
                pawn.apparel.Wear(apparel);
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] EquipApparel failed for " + (def?.defName ?? "null") + ": " + ex.Message);
                apparel?.Destroy(DestroyMode.Vanish);
            }
        }

        private static void ApplyQualityAndHP(Thing thing, int tier)
        {
            tier = Mathf.Clamp(tier, 0, TierCount - 1);

            QualityCategory quality = RollQuality(tier);
            thing.TryGetComp<CompQuality>()?.SetQuality(quality, ArtGenerationContext.Outsider);

            int maxHP = thing.MaxHitPoints;
            float hpPct = DurabilityRanges[tier][0] + Rand.Value * (DurabilityRanges[tier][1] - DurabilityRanges[tier][0]);
            thing.HitPoints = Mathf.Max(1, Mathf.RoundToInt(maxHP * hpPct));
        }

        private static QualityCategory RollQuality(int tier)
        {
            tier = Mathf.Clamp(tier, 0, TierCount - 1);
            float[] weights = QualityWeights[tier];
            float roll = Rand.Value;
            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                    return (QualityCategory)i;
            }
            return QualityCategory.Normal;
        }

        private static bool HasLayer(Pawn pawn, ApparelLayerDef layer)
        {
            foreach (Apparel ap in pawn.apparel.WornApparel)
            {
                if (ap.def.apparel?.LastLayer == layer)
                    return true;
            }
            return false;
        }

        private static bool HasLayerOnGroup(Pawn pawn, ApparelLayerDef layer, BodyPartGroupDef group)
        {
            foreach (Apparel ap in pawn.apparel.WornApparel)
            {
                if (ap.def.apparel?.LastLayer == layer && CoversAnyGroup(ap.def, group))
                    return true;
            }
            return false;
        }

        private static bool CoversAnyGroup(ThingDef def, BodyPartGroupDef group)
        {
            List<BodyPartGroupDef> groups = def.apparel?.bodyPartGroups;
            if (groups == null) return false;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] == group) return true;
            }
            return false;
        }

        private static float ShieldChanceForKind(PawnKindDef kind)
        {
            if (kind == CADefOf.MarkedMan) return 1.0f;
            if (kind == CADefOf.CrossedWarlord) return 0.40f;
            if (kind == CADefOf.CrossedAlpha) return 0.25f;
            if (kind == CADefOf.CrossedSoldier) return 0.10f;
            if (kind == CADefOf.CrossedBrute) return 0.05f;
            return 0.02f;
        }

        internal static bool IsCrossedKind(PawnKindDef kind)
        {
            return kind == CADefOf.CrossedCivilian
                || kind == CADefOf.CrossedScout
                || kind == CADefOf.CrossedHunter
                || kind == CADefOf.CrossedShooter
                || kind == CADefOf.CrossedRaider
                || kind == CADefOf.CrossedSoldier
                || kind == CADefOf.CrossedBrute
                || kind == CADefOf.CrossedPyromaniac
                || kind == CADefOf.CrossedAlpha
                || kind == CADefOf.CrossedWarlord
                || kind == CADefOf.MarkedMan;
        }

        internal static bool CanUseWeapon(Pawn pawn, ThingDef def)
        {
            if (def?.IsWeapon != true)
                return false;

            if (def.IsRangedWeapon)
            {
                List<VerbProperties> verbs = def.Verbs;
                float range = 0f;
                if (verbs != null && verbs.Count > 0)
                    range = verbs[0].range;
                if (range > 75f)
                    return false;
            }

            float mass = def.GetStatValueAbstract(StatDefOf.Mass);
            if (mass > 40f)
                return false;

            if (def.weaponTags != null)
            {
                for (int i = 0; i < def.weaponTags.Count; i++)
                {
                    string tag = def.weaponTags[i];
                    if (tag == null) continue;

                    if (tag.IndexOf("Mounted", StringComparison.OrdinalIgnoreCase) >= 0
                        || tag.IndexOf("Siege", StringComparison.OrdinalIgnoreCase) >= 0
                        || tag.IndexOf("Turret", StringComparison.OrdinalIgnoreCase) >= 0)
                        return false;

                    if (tag.IndexOf("Exclusive", StringComparison.OrdinalIgnoreCase) >= 0
                        || tag.IndexOf("Restricted", StringComparison.OrdinalIgnoreCase) >= 0)
                        return false;

                    if (tag.StartsWith("Race_", StringComparison.OrdinalIgnoreCase)
                        || tag.StartsWith("Faction_", StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            return true;
        }

        internal static bool CanWearApparel(Pawn pawn, ThingDef def)
        {
            if (def?.apparel == null)
                return false;

            if (IsShield(def))
                return false;

            return true;
        }

        private static int GetKindBaseTier(PawnKindDef kind)
        {
            if (kind == CADefOf.CrossedCivilian) return 0;
            if (kind == CADefOf.CrossedScout) return 1;
            if (kind == CADefOf.CrossedHunter) return 2;
            if (kind == CADefOf.CrossedShooter) return 2;
            if (kind == CADefOf.CrossedRaider) return 3;
            if (kind == CADefOf.CrossedPyromaniac) return 2;
            if (kind == CADefOf.CrossedSoldier) return 4;
            if (kind == CADefOf.CrossedBrute) return 4;
            if (kind == CADefOf.CrossedAlpha) return 5;
            if (kind == CADefOf.CrossedWarlord) return 5;
            if (kind == CADefOf.MarkedMan) return 6;
            return 0;
        }

        private static int ClassifyApparel(ThingDef def)
        {
            if (def?.apparel == null) return 0;

            float sharp = def.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);

            if (sharp >= 0.55f) return 6;
            if (sharp >= 0.45f) return 5;
            if (sharp >= 0.35f) return 4;
            if (sharp >= 0.25f) return 3;
            if (sharp >= 0.15f) return 2;
            if (sharp >= 0.06f) return 1;
            return 0;
        }

        private static List<ThingDef>[] weaponsByTier;
        public static void BuildWeaponCache()
        {
            if (weaponsByTier != null) return;
            weaponsByTier = new List<ThingDef>[TierCount];
            for (int i = 0; i < TierCount; i++)
                weaponsByTier[i] = new List<ThingDef>();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def?.IsWeapon != true)
                    continue;

                int tier = ClassifyWeapon(def);
                weaponsByTier[tier].Add(def);
            }
        }

        private static int ClassifyWeapon(ThingDef def)
        {
            if (def == null) return 0;

            float value = def.GetStatValueAbstract(StatDefOf.MarketValue);

            if (def.IsRangedWeapon)
            {
                if (value >= 3000f) return 6;
                if (value >= 1500f) return 5;
                if (value >= 800f)  return 4;
                if (value >= 500f)  return 3;
                if (value >= 200f)  return 2;
                if (value >= 80f)   return 1;
                return 0;
            }

            if (value >= 2000f) return 6;
            if (value >= 1000f) return 5;
            if (value >= 500f)  return 4;
            if (value >= 250f)  return 3;
            if (value >= 100f)  return 2;
            if (value >= 40f)   return 1;
            return 0;
        }
    }
}
