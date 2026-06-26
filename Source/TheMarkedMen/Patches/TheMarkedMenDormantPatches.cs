using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    public static class Patch_DormantMarkDamageTrigger
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null || __instance.Dead || totalDamageDealt <= 0f) return;

            Hediff dormant = __instance.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
            if (dormant == null) return;

            HediffComp_DormantMark comp = dormant.TryGetComp<HediffComp_DormantMark>();
            if (comp == null || comp.IsActivated) return;

            float maxHealth = __instance.HealthScale * 100f;
            float damageFraction = totalDamageDealt / Math.Max(maxHealth, 1f);
            comp.NotifyDamaged(damageFraction);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_DormantMarkOnDeath
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || __instance.health == null) return;
            Hediff dormant = __instance.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
            if (dormant == null) return;
            __instance.health.RemoveHediff(dormant);
        }
    }

    [HarmonyPatch]
    public static class Patch_DormantMarkHideFromHealthCard
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(HealthCardUtility), "DrawHediffListing");
        }

        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn)
        {
            if (pawn?.health == null) return true;
            Hediff dormant = pawn.health.hediffSet?.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
            if (dormant != null && !dormant.Visible)
            {
                return true;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_DormantMarkOnSpawn
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || __instance.health == null) return;
            Hediff dormant = __instance.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
            if (dormant == null) return;
        }
    }
}
