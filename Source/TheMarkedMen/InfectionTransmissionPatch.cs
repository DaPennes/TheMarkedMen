using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    
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

            
            
            DamageDef damageDef = __instance.verbProps?.meleeDamageDef ?? DamageDefOf.Scratch;
            float armorPen = __instance.verbProps?.meleeDamageBaseAmount ?? 1f;
            var dinfo = new DamageInfo(damageDef, 1f, armorPen, instigator: attacker);

            InfectionTransmissionHelper.TryTransmitInfection(attacker, victim, dinfo);
        }
    }

    
    
    
    

    
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