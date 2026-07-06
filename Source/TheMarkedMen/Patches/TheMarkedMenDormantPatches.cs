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

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_DormantMarkOnSpawn
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || __instance.health == null) return;

            if (!__instance.IsColonist || __instance.Dead || __instance.Destroyed) return;

            if (!TheMarkedMenMod.Settings?.lostSurvivorEnabled ?? true) return;

            Hediff existing = __instance.health.hediffSet.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
            if (existing != null) return;

            if (!IsLostSurvivorPawn(__instance)) return;

            Hediff dormantMark = HediffMaker.MakeHediff(CADefOf.CA_DormantMark, __instance);
            __instance.health.AddHediff(dormantMark);
        }

        private static bool IsLostSurvivorPawn(Pawn pawn)
        {
            if (pawn == null) return false;

            if (pawn.questTags != null && pawn.questTags.Contains("CA_LostSurvivor")) return true;

            return false;
        }
    }
}
