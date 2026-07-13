using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MarkedWeaponPatch
{
    [StaticConstructorOnStartup]
    public static class WeaponValidationInitializer
    {
        static WeaponValidationInitializer()
        {
            Harmony harmony = new Harmony("edria.markedmen.weaponpatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[MarkedWeaponPatch] Weapon validation patches applied.");
        }
    }

    /// <summary>
    /// Postfix on CrossedEquipmentGenerator.CanUseWeapon to add stricter validation.
    /// This prevents infected from equipping turret weapons, race-exclusive weapons,
    /// mounted weapons, and other invalid equipment.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_CanUseWeapon
    {
        // Target the private static method CrossedEquipmentGenerator.CanUseWeapon
        static MethodBase TargetMethod()
        {
            return AccessTools.Method("TheMarkedMen.CrossedEquipmentGenerator:CanUseWeapon");
        }

        static bool Prefix(Pawn pawn, ThingDef def, ref bool __result)
        {
            if (def == null)
            {
                __result = false;
                return false;
            }

            // Basic checks
            if (!def.IsWeapon || def.weaponTags == null || def.weaponTags.Count == 0)
            {
                __result = false;
                return false;
            }

            // Exclude building-mounted weapons (turrets, mortars, etc.)
            if (def.building != null)
            {
                __result = false;
                return false;
            }

            // Exclude weapons with a race (animal/turret weapons that are creatures)
            if (def.race != null)
            {
                __result = false;
                return false;
            }

            // Exclude weapons that are plants
            if (def.plant != null)
            {
                __result = false;
                return false;
            }

            // Exclude if the weapon has a thingClass that is a Building
            if (def.thingClass != null && def.thingClass.IsSubclassOf(typeof(Building)))
            {
                __result = false;
                return false;
            }

            // Exclude equipment that has a ThingDef of category Building
            if (def.thingCategories != null)
            {
                foreach (ThingCategoryDef cat in def.thingCategories)
                {
                    if (cat == ThingCategoryDefOf.Buildings || 
                        (cat.defName != null && cat.defName.IndexOf("Building", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        __result = false;
                        return false;
                    }
                }
            }

            // Check weaponTags for exclusion patterns more comprehensively
            for (int i = 0; i < def.weaponTags.Count; i++)
            {
                string tag = def.weaponTags[i];
                if (tag == null) continue;

                // Exclude mounted/siege/turret weapons
                if (tag.IndexOf("Mounted", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf("Siege", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf("Turret", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    __result = false;
                    return false;
                }

                // Exclude race-specific weapons by checking for tags that indicate
                // the weapon is restricted to specific non-human races
                if (tag.IndexOf("Exclusive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf("Restricted", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    __result = false;
                    return false;
                }

                // Exclude weapons tagged as racial or faction-specific
                if (tag.StartsWith("Race_", StringComparison.OrdinalIgnoreCase) ||
                    tag.StartsWith("Faction_", StringComparison.OrdinalIgnoreCase))
                {
                    __result = false;
                    return false;
                }
            }

            // Check if this weapon has a required race tag for a non-human race
            // (weapons that can only be used by specific alien races)
            if (def.weaponTags.Any(tag => 
                tag != null && (
                    tag.IndexOf("Alien", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf("Xenotype", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf("RaceLock", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf("Genetic", StringComparison.OrdinalIgnoreCase) >= 0
                )))
            {
                // Only allow if the pawn's race/humanlikeness matches - for humanlikes,
                // exclude alien/xenotype-specific weapons
                if (pawn != null && pawn.RaceProps != null && pawn.RaceProps.Humanlike)
                {
                    __result = false;
                    return false;
                }
            }

            // Ranged weapon range check
            if (def.IsRangedWeapon)
            {
                List<VerbProperties> verbs = def.Verbs;
                float range = 0f;
                if (verbs != null && verbs.Count > 0)
                {
                    range = verbs[0].range;
                }
                // Exclude extreme-range weapons (snipers, artillery)
                if (range > 75f)
                {
                    __result = false;
                    return false;
                }
            }

            // Mass check - weapons over 40kg are likely mounted/vehicle weapons
            float mass = StatExtension.GetStatValueAbstract(def, StatDefOf.Mass, null);
            if (mass > 40f)
            {
                __result = false;
                return false;
            }

            // Run the original method's logic for the remaining checks
            // (weaponTags checks for Mounted/Siege/Turret were already done above)
            __result = true;
            return false; // Skip original, we've done all the checks
        }
    }

    /// <summary>
    /// Prefix on Pawn_EquipmentTracker.AddEquipment to block invalid weapon equipping
    /// at runtime (e.g., from ground pickup, jobs, or other mod interactions).
    /// </summary>
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "AddEquipment")]
    public static class Patch_AddEquipment
    {
        static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            if (eq == null || __instance == null)
            {
                return true;
            }

            ThingDef def = eq.def;
            if (def == null)
            {
                return true;
            }

            // Only validate for infected pawns
            Pawn pawn = __instance.pawn;
            if (pawn == null)
            {
                return true;
            }

            // We only block invalid equipment for infected pawns
            if (!IsInfectedPawn(pawn))
            {
                return true;
            }

            // Run the same validation as CanUseWeapon
            if (!IsValidWeaponForInfected(def, pawn))
            {
                if (Prefs.DevMode)
                {
                    Log.Warning($"[MarkedWeaponPatch] Blocked invalid weapon {def.defName} from being equipped by infected pawn {pawn.Name?.ToStringShort ?? pawn.LabelShort}");
                }
                return false; // Block the equip
            }

            return true;
        }

        private static bool IsInfectedPawn(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.Dead)
            {
                return false;
            }

            // Check for the CrossVirus hediff
            HediffDef virusDef = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossVirus");
            if (virusDef != null && pawn.health.hediffSet.HasHediff(virusDef, false))
            {
                return true;
            }

            // Check if they're in the Crossed faction
            FactionDef factionDef = DefDatabase<FactionDef>.GetNamedSilentFail("CA_CrossedFaction");
            if (factionDef != null && pawn.Faction?.def == factionDef)
            {
                return true;
            }

            return false;
        }

        private static bool IsValidWeaponForInfected(ThingDef def, Pawn pawn)
        {
            // Must be a weapon
            if (!def.IsWeapon || def.weaponTags == null || def.weaponTags.Count == 0)
            {
                return false;
            }

            // Exclude building-mounted weapons
            if (def.building != null)
            {
                return false;
            }

            // Exclude weapons with a race
            if (def.race != null)
            {
                return false;
            }

            // Exclude plants
            if (def.plant != null)
            {
                return false;
            }

            // Exclude building-type thing classes
            if (def.thingClass != null && def.thingClass.IsSubclassOf(typeof(Building)))
            {
                return false;
            }

            // Check weapon tags for exclusion patterns
            for (int i = 0; i < def.weaponTags.Count; i++)
            {
                string tag = def.weaponTags[i];
                if (tag == null) continue;

                if (tag.IndexOf("Mounted", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf("Siege", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf("Turret", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }

                // Exclude race/ faction-specific weapons
                if (tag.IndexOf("Exclusive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf("Restricted", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }

                if (tag.StartsWith("Race_", StringComparison.OrdinalIgnoreCase) ||
                    tag.StartsWith("Faction_", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Alien/race-specific weapons
            if (def.weaponTags.Any(tag =>
                tag != null && (
                    tag.IndexOf("Alien", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf("Xenotype", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf("RaceLock", StringComparison.OrdinalIgnoreCase) >= 0
                )))
            {
                if (pawn.RaceProps != null && pawn.RaceProps.Humanlike)
                {
                    return false;
                }
            }

            // Range check
            if (def.IsRangedWeapon)
            {
                float range = 0f;
                if (def.Verbs != null && def.Verbs.Count > 0)
                {
                    range = def.Verbs[0].range;
                }
                if (range > 75f)
                {
                    return false;
                }
            }

            // Mass check
            float mass = StatExtension.GetStatValueAbstract(def, StatDefOf.Mass, null);
            if (mass > 40f)
            {
                return false;
            }

            return true;
        }
    }
}
