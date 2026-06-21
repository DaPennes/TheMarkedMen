using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static class CrossedDamageUtility
    {
        public static bool IsValidMeleeInfectionSource(DamageInfo dinfo)
        {
            if (!(dinfo.Instigator is Pawn))
            {
                return false;
            }

            if (dinfo.Def != null)
            {
                if (dinfo.Def.isRanged)
                {
                    return false;
                }

                if (dinfo.Def.isExplosive)
                {
                    return false;
                }
            }

            if (dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon)
            {
                return false;
            }

            return true;
        }

        public static float GetInfectChanceForAttack(DamageInfo dinfo, Pawn victim)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null)
            {
                return TheMarkedMenSettings.InfectionTransmissionChance;
            }

            if (!settings.meleeTransmissionEnabled)
            {
                return 0f;
            }

            if (dinfo.Instigator is Pawn instigator && CrossedUtility.IsInfectedPawn(instigator))
            {
                if (CrossedUtility.IsCrossedPawn(instigator) && settings.markedMenGuaranteedInfection)
                {
                    return 1f;
                }

                if (CrossedUtility.IsCrossedPawn(instigator))
                {
                    return Mathf.Clamp01(settings.markedMenInfectionChance);
                }
            }

            if (dinfo.Weapon == null)
            {
                string defName = dinfo.Def?.defName ?? string.Empty;

                if (defName.IndexOf("Bite", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return settings.biteTransmissionEnabled ? Mathf.Clamp01(settings.biteInfectionChance) : 0f;
                }

                if (defName.IndexOf("Claw", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return settings.clawTransmissionEnabled ? Mathf.Clamp01(settings.clawInfectionChance) : 0f;
                }

                if (defName.IndexOf("Scratch", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return settings.scratchTransmissionEnabled ? Mathf.Clamp01(settings.scratchInfectionChance) : 0f;
                }

                if (defName.IndexOf("Punch", System.StringComparison.OrdinalIgnoreCase) >= 0 || defName.IndexOf("Blunt", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return settings.punchTransmissionEnabled ? Mathf.Clamp01(settings.punchInfectionChance) : 0f;
                }

                if (defName.IndexOf("Cut", System.StringComparison.OrdinalIgnoreCase) >= 0 || defName.IndexOf("Stab", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return settings.clawTransmissionEnabled ? Mathf.Clamp01(settings.clawInfectionChance) : 0f;
                }
            }

            if (dinfo.Weapon != null)
            {
                return settings.meleeWeaponTransmissionEnabled ? Mathf.Clamp01(settings.meleeWeaponInfectionChance) : 0f;
            }

            return Mathf.Clamp01(TheMarkedMenMod.Settings?.infectedAssaultExposureChance ?? TheMarkedMenSettings.InfectionTransmissionChance);
        }
    }
}
