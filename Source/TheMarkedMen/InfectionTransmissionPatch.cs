using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    // Patch to transmit Marked Virus infection on melee hits
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class Verb_MeleeAttackDamage_ApplyMeleeDamageToTarget_Patch
    {
        public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
        {
            Pawn attacker = __instance.Caster as Pawn;
            if (attacker == null || target.Thing is not Pawn victim)
            {
                return;
            }

            // Create a pseudo DamageInfo for checking since the original method
            // creates DamageInfo internally
            DamageDef damageDef = __instance.verbProps?.meleeDamageDef ?? DamageDefOf.Scratch;
            float armorPen = __instance.verbProps?.meleeDamageBaseAmount ?? 1f;
            var dinfo = new DamageInfo(damageDef, 1f, armorPen, instigator: attacker);

            InfectionTransmissionHelper.TryTransmitInfection(attacker, victim, dinfo);
        }
    }

    // Note: We cannot patch the abstract Verb.TryCastShot - it has no method body.
    // The primary infection transmission is handled by the
    // Verb_MeleeAttackDamage_ApplyMeleeDamageToTarget_Patch above, which covers
    // all melee verb attacks in RimWorld.

    // Helper class for infection transmission logic
    public static class InfectionTransmissionHelper
    {
        public static void TryTransmitInfection(Pawn attacker, Pawn victim, DamageInfo dinfo)
        {
            if (!CrossedDamageUtility.IsValidMeleeInfectionSource(dinfo))
            {
                return;
            }

            if (!CrossedUtility.IsInfectedPawn(attacker))
            {
                return;
            }

            float infectionChance = CrossedDamageUtility.GetInfectChanceForAttack(dinfo, victim);
            if (attacker.IsPrisonerOfColony)
            {
                infectionChance = Mathf.Min(infectionChance, CrossedContagionUtility.GetPrisonerExposureChance(attacker));
            }

            if (infectionChance <= 0f)
            {
                return;
            }

            CrossedUtility.TryExpose(victim, infectionChance, "infected melee attack", attacker);
        }

        public static void TryTransmitInfectionFromVerb(Pawn attacker, Pawn victim, Verb verb)
        {
            if (!CrossedUtility.IsInfectedPawn(attacker))
            {
                return;
            }

            // Create a pseudo DamageInfo for checking
            var dinfo = new DamageInfo(
                verb.verbProps?.meleeDamageDef ?? DamageDefOf.Scratch,
                1f,
                armorPenetration: verb.verbProps?.meleeDamageBaseAmount ?? 1f,
                instigator: attacker);

            if (!CrossedDamageUtility.IsValidMeleeInfectionSource(dinfo))
            {
                return;
            }

            float infectionChance = CrossedDamageUtility.GetInfectChanceForAttack(dinfo, victim);
            if (attacker.IsPrisonerOfColony)
            {
                infectionChance = Mathf.Min(infectionChance, CrossedContagionUtility.GetPrisonerExposureChance(attacker));
            }

            if (infectionChance <= 0f)
            {
                return;
            }

            CrossedUtility.TryExpose(victim, infectionChance, "infected melee attack", attacker);
        }
    }
}