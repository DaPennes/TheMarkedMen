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

        [Flags]
        private enum ApparelCategory
        {
            None = 0,
            Civilian = 1 << 0,
            LightArmor = 1 << 1,
            HeavyArmor = 1 << 2,
            Shield = 1 << 3,
        }

        private static List<ThingDef>[] shirtsByTier;
        private static List<ThingDef>[] pantsByTier;
        private static List<ThingDef>[] midBodyByTier;
        private static List<ThingDef>[] outerByTier;
        private static List<ThingDef>[] footwearByTier;
        private static List<ThingDef>[] handwearByTier;
        private static List<ThingDef>[] headgearByTier;
        private static List<ThingDef>[] shieldsByTier;
        private static List<ThingDef>[] accessoriesByTier;
        private static List<ThingDef>[] eyewearByTier;
        private static List<ThingDef>[] weaponsByTier;
        private static Dictionary<ThingDef, ApparelCategory> apparelRoles;
        private static bool cacheBuilt;
        private static bool initAttempted;

        private static readonly BodyPartGroupDef LegsGroup;
        private static readonly BodyPartGroupDef TorsoGroup;
        private static readonly BodyPartGroupDef HandsGroup;
        private static readonly BodyPartGroupDef FeetGroup;
        private static readonly BodyPartGroupDef FullHeadGroup;
        private static readonly BodyPartGroupDef UpperHeadGroup;
        private static readonly ApparelLayerDef OnSkinLayer;
        private static readonly ApparelLayerDef MiddleLayer;
        private static readonly ApparelLayerDef ShellLayer;
        private static readonly ApparelLayerDef BeltLayer;
        private static readonly ApparelLayerDef OverheadLayer;
        private static readonly ApparelLayerDef EyeCoverLayer;

        static CrossedEquipmentGenerator()
        {
            LegsGroup = BodyPartGroupDefOf.Legs;
            TorsoGroup = BodyPartGroupDefOf.Torso;
            HandsGroup = TryGetBodyPartGroup("Hands");
            FeetGroup = TryGetBodyPartGroup("Feet");
            FullHeadGroup = BodyPartGroupDefOf.FullHead;
            UpperHeadGroup = BodyPartGroupDefOf.UpperHead;
            OnSkinLayer = ApparelLayerDefOf.OnSkin;
            MiddleLayer = ApparelLayerDefOf.Middle;
            ShellLayer = ApparelLayerDefOf.Shell;
            BeltLayer = ApparelLayerDefOf.Belt;
            OverheadLayer = ApparelLayerDefOf.Overhead;
            EyeCoverLayer = TryGetApparelLayer("EyeCover");
        }

        private static BodyPartGroupDef TryGetBodyPartGroup(string name)
        {
            return DefDatabase<BodyPartGroupDef>.GetNamedSilentFail(name);
        }

        private static ApparelLayerDef TryGetApparelLayer(string name)
        {
            return DefDatabase<ApparelLayerDef>.GetNamedSilentFail(name);
        }

        private static readonly Dictionary<PawnKindDef, ApparelCategory> KindApparelMask = new()
        {
            { CADefOf.CrossedCivilian,   ApparelCategory.Civilian | ApparelCategory.Shield },
            { CADefOf.CrossedScout,      ApparelCategory.Civilian | ApparelCategory.LightArmor | ApparelCategory.Shield },
            { CADefOf.CrossedHunter,     ApparelCategory.Civilian | ApparelCategory.LightArmor },
            { CADefOf.CrossedShooter,    ApparelCategory.LightArmor | ApparelCategory.HeavyArmor },
            { CADefOf.CrossedRaider,     ApparelCategory.LightArmor | ApparelCategory.HeavyArmor | ApparelCategory.Shield },
            { CADefOf.CrossedSoldier,    ApparelCategory.LightArmor | ApparelCategory.HeavyArmor | ApparelCategory.Shield },
            { CADefOf.CrossedBrute,      ApparelCategory.HeavyArmor | ApparelCategory.LightArmor },
            { CADefOf.CrossedPyromaniac, ApparelCategory.Civilian },
            { CADefOf.CrossedAlpha,      ApparelCategory.HeavyArmor | ApparelCategory.LightArmor | ApparelCategory.Shield },
            { CADefOf.CrossedWarlord,    ApparelCategory.HeavyArmor | ApparelCategory.LightArmor | ApparelCategory.Shield },
            { CADefOf.MarkedMan,         ApparelCategory.HeavyArmor | ApparelCategory.LightArmor | ApparelCategory.Shield },
        };

        private static readonly Dictionary<PawnKindDef, float[]> KindTierWeights = new Dictionary<PawnKindDef, float[]>
        {
            { CADefOf.CrossedCivilian,  new[] { 0.70f, 0.22f, 0.08f, 0f,    0f,    0f,    0f     } },
            { CADefOf.CrossedScout,     new[] { 0.15f, 0.45f, 0.30f, 0.10f, 0f,    0f,    0f     } },
            { CADefOf.CrossedHunter,    new[] { 0.10f, 0.40f, 0.35f, 0.15f, 0f,    0f,    0f     } },
            { CADefOf.CrossedShooter,   new[] { 0.05f, 0.15f, 0.45f, 0.25f, 0.08f, 0.02f, 0f     } },
            { CADefOf.CrossedRaider,    new[] { 0.03f, 0.10f, 0.30f, 0.35f, 0.18f, 0.04f, 0f     } },
            { CADefOf.CrossedSoldier,   new[] { 0f,    0.05f, 0.15f, 0.30f, 0.30f, 0.17f, 0.03f  } },
            { CADefOf.CrossedBrute,     new[] { 0f,    0.03f, 0.12f, 0.25f, 0.35f, 0.22f, 0.03f  } },
            { CADefOf.CrossedPyromaniac,new[] { 0.08f, 0.22f, 0.38f, 0.22f, 0.10f, 0f,    0f     } },
            { CADefOf.CrossedAlpha,     new[] { 0f,    0f,    0.05f, 0.15f, 0.35f, 0.32f, 0.13f  } },
            { CADefOf.CrossedWarlord,   new[] { 0f,    0f,    0f,    0.05f, 0.20f, 0.40f, 0.35f  } },
            { CADefOf.MarkedMan,        new[] { 0f,    0f,    0f,    0.02f, 0.08f, 0.30f, 0.60f  } },
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
                shirtsByTier = new List<ThingDef>[TierCount];
                pantsByTier = new List<ThingDef>[TierCount];
                midBodyByTier = new List<ThingDef>[TierCount];
                outerByTier = new List<ThingDef>[TierCount];
                footwearByTier = new List<ThingDef>[TierCount];
                handwearByTier = new List<ThingDef>[TierCount];
                headgearByTier = new List<ThingDef>[TierCount];
                shieldsByTier = new List<ThingDef>[TierCount];
                accessoriesByTier = new List<ThingDef>[TierCount];
                eyewearByTier = new List<ThingDef>[TierCount];
                weaponsByTier = new List<ThingDef>[TierCount];

                for (int i = 0; i < TierCount; i++)
                {
                    shirtsByTier[i] = new List<ThingDef>();
                    pantsByTier[i] = new List<ThingDef>();
                    midBodyByTier[i] = new List<ThingDef>();
                    outerByTier[i] = new List<ThingDef>();
                    footwearByTier[i] = new List<ThingDef>();
                    handwearByTier[i] = new List<ThingDef>();
                    headgearByTier[i] = new List<ThingDef>();
                    shieldsByTier[i] = new List<ThingDef>();
                    accessoriesByTier[i] = new List<ThingDef>();
                    eyewearByTier[i] = new List<ThingDef>();
                    weaponsByTier[i] = new List<ThingDef>();
                }

                apparelRoles = new Dictionary<ThingDef, ApparelCategory>();

                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def?.apparel == null)
                        continue;

                    int tier = ClassifyApparel(def);
                    ApparelCategory role = ClassifyApparelRole(def);
                    apparelRoles[def] = role;
                    ApparelLayerDef layer = def.apparel.LastLayer;

                    if (IsShield(def))
                    {
                        shieldsByTier[tier].Add(def);
                        continue;
                    }

                    if (layer == OverheadLayer)
                    {
                        headgearByTier[tier].Add(def);
                        continue;
                    }

                    if (layer == EyeCoverLayer)
                    {
                        eyewearByTier[tier].Add(def);
                        continue;
                    }

                    if (layer == BeltLayer)
                    {
                        accessoriesByTier[tier].Add(def);
                        continue;
                    }

                    if (layer == ShellLayer)
                    {
                        outerByTier[tier].Add(def);
                        continue;
                    }

                    if (layer == MiddleLayer)
                    {
                        if (CoversAnyGroup(def, HandsGroup))
                            handwearByTier[tier].Add(def);
                        else if (CoversAnyGroup(def, FeetGroup))
                            footwearByTier[tier].Add(def);
                        else
                            midBodyByTier[tier].Add(def);
                        continue;
                    }

                    if (layer == OnSkinLayer)
                    {
                        if (CoversAnyGroup(def, LegsGroup) || CoversAnyGroup(def, FeetGroup))
                            pantsByTier[tier].Add(def);
                        else
                            shirtsByTier[tier].Add(def);
                        continue;
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

        private static bool CoversAnyGroup(ThingDef def, BodyPartGroupDef group)
        {
            return def.apparel?.bodyPartGroups?.Any(g => g == group) == true;
        }

        public static void AssignEquipment(Pawn pawn)
        {
            if (pawn?.health == null || !IsCrossedKind(pawn.kindDef))
                return;

            if (!cacheBuilt)
                BuildCache();

            if (!cacheBuilt)
                return;

            if (HasEquipment(pawn))
                return;

            StripEquipment(pawn);

            int bodyTier = RollTier(KindTierWeights[pawn.kindDef]);
            int midTier = RollTier(KindTierWeights[pawn.kindDef]);
            int outerTier = RollTier(KindTierWeights[pawn.kindDef]);
            int headTier = RollTier(KindTierWeights[pawn.kindDef]);
            int beltTier = RollTier(KindTierWeights[pawn.kindDef]);
            int weaponTier = RollTier(KindTierWeights[pawn.kindDef]);

            EquipShirt(pawn, bodyTier);
            EquipPants(pawn, bodyTier);
            EquipGloves(pawn, midTier);
            EquipBoots(pawn, midTier);
            EquipMidBody(pawn, midTier);
            EquipOuter(pawn, outerTier);
            EquipHeadgear(pawn, headTier);
            EquipEyewear(pawn, headTier);
            EquipShield(pawn, beltTier);
            EquipAccessory(pawn, beltTier);

            if (pawn.kindDef == CADefOf.CrossedPyromaniac)
                EquipPyromaniacWeapon(pawn);
            else
                EquipWeapon(pawn, weaponTier);
        }

        private static void EquipShirt(Pawn pawn, int tier)
        {
            ThingDef shirt = PickFromTier(shirtsByTier, tier, d =>
                CanWear(pawn, d) && IsAllowedForKind(pawn.kindDef, d));
            if (shirt != null)
                EquipApparel(pawn, shirt, tier);
        }

        private static void EquipPants(Pawn pawn, int tier)
        {
            ThingDef pants = PickFromTier(pantsByTier, tier, d =>
                CanWear(pawn, d) && IsAllowedForKind(pawn.kindDef, d));
            if (pants != null)
                EquipApparel(pawn, pants, tier);
        }

        private static void EquipGloves(Pawn pawn, int tier)
        {
            if (!Rand.Chance(0.50f))
                return;

            ThingDef gloves = PickFromTier(handwearByTier, tier, d =>
                CanWear(pawn, d) && IsAllowedForKind(pawn.kindDef, d));
            if (gloves != null)
                EquipApparel(pawn, gloves, tier);
        }

        private static void EquipBoots(Pawn pawn, int tier)
        {
            if (!Rand.Chance(0.60f))
                return;

            ThingDef boots = PickFromTier(footwearByTier, tier, d =>
                CanWear(pawn, d) && IsAllowedForKind(pawn.kindDef, d));
            if (boots != null)
                EquipApparel(pawn, boots, tier);
        }

        private static void EquipMidBody(Pawn pawn, int tier)
        {
            if (HasLayerOnGroup(pawn, MiddleLayer, TorsoGroup))
                return;

            ThingDef vest = PickFromTier(midBodyByTier, tier, d =>
                CanWear(pawn, d) && IsAllowedForKind(pawn.kindDef, d));
            if (vest != null)
                EquipApparel(pawn, vest, tier);
        }

        private static void EquipOuter(Pawn pawn, int tier)
        {
            if (HasLayerOnGroup(pawn, ShellLayer, TorsoGroup))
                return;

            if (!Rand.Chance(0.80f))
                return;

            ThingDef outer = PickFromTier(outerByTier, tier, d =>
                CanWear(pawn, d) && IsAllowedForKind(pawn.kindDef, d));
            if (outer != null)
                EquipApparel(pawn, outer, tier);
        }

        private static void EquipHeadgear(Pawn pawn, int tier)
        {
            if (HasLayer(pawn, OverheadLayer))
                return;

            if (!Rand.Chance(HeadgearChanceForKind(pawn.kindDef, tier)))
                return;

            ThingDef headgear = PickFromTier(headgearByTier, tier, d =>
                CanWear(pawn, d) && IsAllowedForKind(pawn.kindDef, d));
            if (headgear != null)
                EquipApparel(pawn, headgear, tier);
        }

        private static void EquipEyewear(Pawn pawn, int tier)
        {
            if (EyeCoverLayer == null) return;
            if (HasLayer(pawn, EyeCoverLayer)) return;

            if (!Rand.Chance(0.10f))
                return;

            ThingDef eyewear = PickFromTier(eyewearByTier, tier, d => CanWear(pawn, d));
            if (eyewear != null)
                EquipApparel(pawn, eyewear, tier);
        }

        private static void EquipShield(Pawn pawn, int tier)
        {
            if (HasLayer(pawn, BeltLayer))
                return;

            if (!Rand.Chance(ShieldChanceForKind(pawn.kindDef)))
                return;

            List<ThingDef> pool = shieldsByTier[tier].Where(d => CanWear(pawn, d) && IsAllowedForKind(pawn.kindDef, d)).ToList();
            if (pool.Count == 0) return;

            EquipApparel(pawn, pool.RandomElement(), tier);
        }

        private static void EquipAccessory(Pawn pawn, int tier)
        {
            if (HasLayer(pawn, BeltLayer))
                return;

            if (!Rand.Chance(0.15f))
                return;

            List<ThingDef> pool = accessoriesByTier[tier].Where(d => CanWear(pawn, d) && IsAllowedForKind(pawn.kindDef, d)).ToList();
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

        private static void EquipPyromaniacWeapon(Pawn pawn)
        {
            if (pawn.equipment == null) return;

            pawn.equipment.DestroyAllEquipment();

            ThingDef molotov = DefDatabase<ThingDef>.AllDefs.FirstOrDefault(d =>
                d.IsWeapon && d.defName.IndexOf("Molotov", StringComparison.OrdinalIgnoreCase) >= 0);
            if (molotov == null) return;

            ThingWithComps weapon = (ThingWithComps)ThingMaker.MakeThing(molotov);
            pawn.equipment.AddEquipment(weapon);
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

        private static bool HasEquipment(Pawn pawn)
        {
            if (pawn.apparel != null && pawn.apparel.WornApparel.Count > 0)
                return true;
            if (pawn.equipment != null && pawn.equipment.Primary != null)
                return true;
            return false;
        }

        public static void StripEquipment(Pawn pawn)
        {
            if (pawn == null) return;
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

        private static float HeadgearChanceForKind(PawnKindDef kind, int tier)
        {
            // Per-kind headgear chance for visual distinction
            if (kind == CADefOf.CrossedCivilian) return Mathf.Lerp(0.20f, 0.50f, tier / 6f);
            if (kind == CADefOf.CrossedScout) return Mathf.Lerp(0.30f, 0.70f, tier / 6f);
            if (kind == CADefOf.CrossedHunter) return Mathf.Lerp(0.35f, 0.75f, tier / 6f);
            if (kind == CADefOf.CrossedPyromaniac) return Mathf.Lerp(0.15f, 0.40f, tier / 6f);
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
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] IsShield check failed for " + (def?.defName ?? "null") + ": " + ex.Message);
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
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] CanWear check failed for " + (def?.defName ?? "null") + ": " + ex.Message);
                return false;
            }
        }

        private static bool IsAllowedForKind(PawnKindDef kind, ThingDef def)
        {
            if (def?.apparel == null) return true;
            if (!apparelRoles.TryGetValue(def, out var role)) return true;
            return KindApparelMask.TryGetValue(kind, out var mask) && (mask & role) != 0;
        }

        private static ApparelCategory ClassifyApparelRole(ThingDef def)
        {
            if (IsShield(def)) return ApparelCategory.Shield;

            string name = def.defName;

            bool hasHeavyKeyword = name.IndexOf("Marine", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Cataphract", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Warcasket", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Phoenix", StringComparison.OrdinalIgnoreCase) >= 0;

            bool hasLightKeyword = name.IndexOf("Flak", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Vest", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Bullet", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("BombPack", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("TorchBelt", StringComparison.OrdinalIgnoreCase) >= 0;

            float sharp = def.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);

            if (sharp >= 0.40f || hasHeavyKeyword) return ApparelCategory.HeavyArmor;
            if (sharp >= 0.15f || hasLightKeyword) return ApparelCategory.LightArmor;
            return ApparelCategory.Civilian;
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
                float range = def.Verbs?.FirstOrDefault()?.range ?? 0f;
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

            if (sharp >= 0.55f) return 6;
            if (sharp >= 0.45f) return 5;
            if (sharp >= 0.35f) return 4;
            if (sharp >= 0.25f) return 3;
            if (sharp >= 0.15f) return 2;
            if (sharp >= 0.06f) return 1;
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
