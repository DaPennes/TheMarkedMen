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

        private static List<ThingDef>[] bodyApparelByTier;
        private static List<ThingDef>[] armorByTier;
        private static List<ThingDef>[] headgearByTier;
        private static List<ThingDef>[] shieldsByTier;
        private static List<ThingDef>[] weaponsByTier;
        private static bool cacheBuilt;
        private static bool initAttempted;

        private static readonly Dictionary<PawnKindDef, float[]> KindTierWeights = new Dictionary<PawnKindDef, float[]>
        {
            { CADefOf.CrossedCivilian,  new[] { 0.70f, 0.20f, 0.07f, 0.02f, 0.008f, 0.002f, 0f     } },
            { CADefOf.CrossedScout,     new[] { 0.20f, 0.50f, 0.22f, 0.06f, 0.02f,  0f,     0f     } },
            { CADefOf.CrossedHunter,    new[] { 0.15f, 0.45f, 0.30f, 0.08f, 0.02f,  0f,     0f     } },
            { CADefOf.CrossedShooter,   new[] { 0.05f, 0.15f, 0.50f, 0.22f, 0.07f,  0.01f,  0f     } },
            { CADefOf.CrossedRaider,    new[] { 0.03f, 0.10f, 0.35f, 0.35f, 0.14f,  0.03f,  0f     } },
            { CADefOf.CrossedSoldier,   new[] { 0f,    0.05f, 0.15f, 0.35f, 0.30f,  0.13f,  0.02f  } },
            { CADefOf.CrossedBrute,     new[] { 0f,    0.03f, 0.12f, 0.30f, 0.35f,  0.18f,  0.02f  } },
            { CADefOf.CrossedPyromaniac,new[] { 0.10f, 0.25f, 0.40f, 0.18f, 0.06f,  0.01f,  0f     } },
            { CADefOf.CrossedAlpha,     new[] { 0f,    0f,    0.05f, 0.15f, 0.35f,  0.32f,  0.13f  } },
            { CADefOf.CrossedWarlord,   new[] { 0f,    0f,    0f,    0.05f, 0.20f,  0.40f,  0.35f  } },
            { CADefOf.MarkedMan,        new[] { 0f,    0f,    0f,    0.02f, 0.08f,  0.30f,  0.60f  } },
        };

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
            new[] { "Cloth", "Synthread", "Devilstrand", "Steel" },
            new[] { "Synthread", "Devilstrand", "Hyperweave", "Steel", "Plasteel" },
            new[] { "Devilstrand", "Hyperweave", "Plasteel", "Uranium" },
            new[] { "Hyperweave", "Plasteel", "Uranium" },
            new[] { "Hyperweave", "Plasteel", "Uranium" },
        };

        public static void BuildCache()
        {
            if (cacheBuilt || initAttempted)
                return;

            initAttempted = true;

            try
            {
                for (int i = 0; i < TierCount; i++)
                {
                    bodyApparelByTier = new List<ThingDef>[TierCount];
                    armorByTier = new List<ThingDef>[TierCount];
                    headgearByTier = new List<ThingDef>[TierCount];
                    shieldsByTier = new List<ThingDef>[TierCount];
                    weaponsByTier = new List<ThingDef>[TierCount];
                }

                for (int i = 0; i < TierCount; i++)
                {
                    bodyApparelByTier[i] = new List<ThingDef>();
                    armorByTier[i] = new List<ThingDef>();
                    headgearByTier[i] = new List<ThingDef>();
                    shieldsByTier[i] = new List<ThingDef>();
                    weaponsByTier[i] = new List<ThingDef>();
                }

                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def?.apparel == null)
                        continue;

                    int tier = ClassifyApparel(def);
                    ApparelLayerDef layer = def.apparel.LastLayer;
                    bool isHeadgear = def.apparel.bodyPartGroups?.Any(g => g == BodyPartGroupDefOf.FullHead
                                                                         || g == BodyPartGroupDefOf.UpperHead) == true;

                    if (IsShield(def))
                    {
                        shieldsByTier[tier].Add(def);
                    }
                    else if (isHeadgear || layer == ApparelLayerDefOf.Overhead)
                    {
                        headgearByTier[tier].Add(def);
                    }
                    else if (layer == ApparelLayerDefOf.OnSkin)
                    {
                        bodyApparelByTier[tier].Add(def);
                    }
                    else if (layer == ApparelLayerDefOf.Middle || layer == ApparelLayerDefOf.Shell)
                    {
                        armorByTier[tier].Add(def);
                    }
                    else if (layer == ApparelLayerDefOf.Belt)
                    {
                        bodyApparelByTier[tier].Add(def);
                    }
                }

                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def?.IsWeapon != true || def.weaponTags == null || def.weaponTags.Count == 0)
                        continue;

                    int tier = ClassifyWeapon(def);
                    weaponsByTier[tier].Add(def);
                }

                cacheBuilt = true;
            }
            catch (Exception ex)
            {
                Log.Error("[The Marked Men] CrossedEquipmentGenerator.BuildCache failed: " + ex.Message);
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

            if (!IsCrossedKind(pawn.kindDef))
                return;

            StripEquipment(pawn);

            if (pawn.kindDef == CADefOf.CrossedPyromaniac)
            {
                EquipPyromaniac(pawn);
                return;
            }

            int headTier = RollTier(KindTierWeights[pawn.kindDef]);
            int bodyTier = RollTier(KindTierWeights[pawn.kindDef]);
            int armorTier = RollTier(KindTierWeights[pawn.kindDef]);
            int shieldTier = RollTier(KindTierWeights[pawn.kindDef]);
            int weaponTier = RollTier(KindTierWeights[pawn.kindDef]);

            EquipBodyClothing(pawn, bodyTier);
            EquipArmor(pawn, armorTier);
            EquipHeadgear(pawn, headTier);
            EquipShield(pawn, shieldTier);
            EquipWeapon(pawn, weaponTier);
        }

        private static void EquipBodyClothing(Pawn pawn, int tier)
        {
            bool hasShirt = false;
            bool hasPants = false;

            foreach (Apparel ap in pawn.apparel.WornApparel)
            {
                if (ap.def.apparel?.LastLayer == ApparelLayerDefOf.OnSkin)
                {
                    bool coversTorso = ap.def.apparel.bodyPartGroups.Any(g => g == BodyPartGroupDefOf.Torso);
                    bool coversLegs = ap.def.apparel.bodyPartGroups.Any(g => g == BodyPartGroupDefOf.Legs);
                    if (coversTorso) hasShirt = true;
                    if (coversLegs) hasPants = true;
                }
            }

            if (!hasShirt)
            {
                ThingDef shirt = PickFromTier(bodyApparelByTier, tier, d =>
                    d.apparel?.LastLayer == ApparelLayerDefOf.OnSkin &&
                    !d.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs) &&
                    d.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) &&
                    CanWear(pawn, d));
                if (shirt != null)
                    EquipApparel(pawn, shirt, tier);
            }

            if (!hasPants)
            {
                ThingDef pants = PickFromTier(bodyApparelByTier, tier, d =>
                    d.apparel?.LastLayer == ApparelLayerDefOf.OnSkin &&
                    d.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs) &&
                    CanWear(pawn, d));
                if (pants != null)
                    EquipApparel(pawn, pants, tier);
            }
        }

        private static void EquipArmor(Pawn pawn, int tier)
        {
            if (HasLayer(pawn, ApparelLayerDefOf.Middle) || HasLayer(pawn, ApparelLayerDefOf.Shell))
                return;

            ThingDef armor = PickFromTier(armorByTier, tier, d => CanWear(pawn, d));
            if (armor != null)
                EquipApparel(pawn, armor, tier);
        }

        private static void EquipHeadgear(Pawn pawn, int tier)
        {
            if (HasLayer(pawn, ApparelLayerDefOf.Overhead))
                return;

            if (!Rand.Chance(HeadgearChanceForTier(tier)))
                return;

            ThingDef headgear = PickFromTier(headgearByTier, tier, d => CanWear(pawn, d));
            if (headgear != null)
                EquipApparel(pawn, headgear, tier);
        }

        private static void EquipShield(Pawn pawn, int tier)
        {
            if (HasLayer(pawn, ApparelLayerDefOf.Belt))
                return;

            if (!Rand.Chance(ShieldChanceForKind(pawn.kindDef)))
                return;

            List<ThingDef> pool = shieldsByTier[tier].Where(d => CanWear(pawn, d)).ToList();
            if (pool.Count == 0) return;

            EquipApparel(pawn, pool.RandomElement(), tier);
        }

        private static void EquipWeapon(Pawn pawn, int tier)
        {
            if (pawn.equipment == null || pawn.equipment.Primary != null)
                return;

            ThingDef weapon = PickFromTier(weaponsByTier, tier, d => CanUseWeapon(pawn, d));
            if (weapon == null)
            {
                weapon = PickFromTier(weaponsByTier, 0, d => CanUseWeapon(pawn, d));
                if (weapon == null) return;
            }

            ThingDef stuff = null;
            if (weapon.MadeFromStuff)
            {
                for (int i = 0; i < StuffMaterials[tier].Length; i++)
                {
                    stuff = DefDatabase<ThingDef>.GetNamedSilentFail(StuffMaterials[tier][i]);
                    if (stuff != null) break;
                }
            }

            ThingWithComps thing = stuff != null
                ? (ThingWithComps)ThingMaker.MakeThing(weapon, stuff)
                : (ThingWithComps)ThingMaker.MakeThing(weapon);

            ApplyQualityAndHP(thing, tier);
            pawn.equipment.AddEquipment(thing);
        }

        private static void EquipPyromaniac(Pawn pawn)
        {
            ThingDef molotov = DefDatabase<ThingDef>.AllDefs.FirstOrDefault(d =>
                d.IsWeapon && d.defName.IndexOf("Molotov", StringComparison.OrdinalIgnoreCase) >= 0);
            if (molotov == null) return;

            pawn.equipment?.DestroyAllEquipment();

            ThingWithComps weapon = (ThingWithComps)ThingMaker.MakeThing(molotov);
            pawn.equipment?.AddEquipment(weapon);
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

        private static void StripEquipment(Pawn pawn)
        {
            if (pawn.apparel != null)
            {
                for (int i = pawn.apparel.WornApparel.Count - 1; i >= 0; i--)
                {
                    Apparel ap = pawn.apparel.WornApparel[i];
                    if (ap.Destroyed) continue;
                    ap.Destroy(DestroyMode.Vanish);
                    pawn.apparel.Remove(ap);
                }
            }

            pawn.equipment?.DestroyAllEquipment();

            if (pawn.inventory != null)
            {
                for (int i = pawn.inventory.innerContainer.Count - 1; i >= 0; i--)
                {
                    Thing t = pawn.inventory.innerContainer[i];
                    if (!t.Destroyed)
                        t.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void EquipApparel(Pawn pawn, ThingDef def, int tier)
        {
            ThingDef stuff = null;
            if (def.MadeFromStuff)
            {
                for (int i = 0; i < StuffMaterials[tier].Length; i++)
                {
                    stuff = DefDatabase<ThingDef>.GetNamedSilentFail(StuffMaterials[tier][i]);
                    if (stuff != null) break;
                }
            }

            Apparel apparel = (Apparel)(stuff != null
                ? ThingMaker.MakeThing(def, stuff)
                : ThingMaker.MakeThing(def));

            ApplyQualityAndHP(apparel, tier);
            pawn.apparel.Wear(apparel);
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

        private static ThingDef PickFromTier(List<ThingDef>[] pools, int baseTier, Func<ThingDef, bool> filter)
        {
            for (int t = baseTier; t >= 0; t--)
            {
                List<ThingDef> candidates = pools[t].Where(filter).ToList();
                if (candidates.Count > 0)
                    return candidates.RandomElement();
            }
            return null;
        }

        private static int RollTier(float[] weights)
        {
            float roll = Rand.Value;
            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                    return i;
            }
            return weights.Length - 1;
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

        private static float HeadgearChanceForTier(int tier)
        {
            return tier switch
            {
                0 => 0.30f,
                1 => 0.40f,
                2 => 0.55f,
                3 => 0.70f,
                4 => 0.85f,
                5 => 0.95f,
                _ => 1.0f,
            };
        }

        private static float ShieldChanceForKind(PawnKindDef kind)
        {
            if (kind == CADefOf.MarkedMan) return 1.0f;
            if (kind == CADefOf.CrossedWarlord) return 0.40f;
            if (kind == CADefOf.CrossedAlpha) return 0.25f;
            if (kind == CADefOf.CrossedSoldier) return 0.10f;
            return 0.02f;
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

        private static bool CanWear(Pawn pawn, ThingDef def)
        {
            if (def?.apparel == null) return false;
            if (!pawn.RaceProps.Humanlike) return false;

            try
            {
                return def.apparel.bodyPartGroups == null || def.apparel.bodyPartGroups.Count == 0
                    || def.apparel.bodyPartGroups.Any(g => pawn.RaceProps.body.AllParts.Any(p => p.groups.Contains(g)));
            }
            catch
            {
                return false;
            }
        }

        private static bool IsCrossedKind(PawnKindDef kind)
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

        private static bool CanUseWeapon(Pawn pawn, ThingDef def)
        {
            if (def?.IsWeapon != true || def.weaponTags == null || def.weaponTags.Count == 0)
                return false;

            if (def.IsRangedWeapon)
            {
                float range = def.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown) > 0f
                    ? def.Verbs?.FirstOrDefault()?.range ?? 0f
                    : 0f;
                if (range > 75f)
                    return false;
            }

            float mass = def.GetStatValueAbstract(StatDefOf.Mass);
            if (mass > 40f)
                return false;

            if (def.weaponTags.Any(t => t.IndexOf("Mounted", StringComparison.OrdinalIgnoreCase) >= 0
                                     || t.IndexOf("Siege", StringComparison.OrdinalIgnoreCase) >= 0
                                     || t.IndexOf("Turret", StringComparison.OrdinalIgnoreCase) >= 0))
                return false;

            return true;
        }

        private static int ClassifyApparel(ThingDef def)
        {
            if (def?.apparel == null) return 0;

            float sharp = def.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);
            float blunt = def.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt);
            float heat = def.GetStatValueAbstract(StatDefOf.ArmorRating_Heat);
            float maxArmor = Mathf.Max(sharp, blunt, heat);

            if (maxArmor >= 0.55f) return 6;
            if (maxArmor >= 0.45f) return 5;
            if (maxArmor >= 0.35f) return 4;
            if (maxArmor >= 0.25f) return 3;
            if (maxArmor >= 0.15f) return 2;
            if (maxArmor >= 0.06f) return 1;
            return 0;
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
