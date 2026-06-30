using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static class CrossedSocialUtility
    {
        private const int SocialPulseInterval = 1800;
        private const float SocialPulseBaseChance = 0.42f;
        private const float SocialPulseLeaderChance = 0.78f;
        private const float MaxSocialTargetDistanceSquared = 400f;
        private const float PackPanicRadius = 12f;

        public static void TryHostileSocialPulse(Pawn initiator)
        {
            if (initiator == null || !initiator.Spawned || initiator.Dead || initiator.Downed || initiator.Map == null || !CrossedUtility.IsCrossedPawn(initiator))
            {
                return;
            }

            float socialStrength = TheMarkedMenSettings.SocialTerrorStrength;
            if (socialStrength <= 0f || !initiator.IsHashIntervalTick(SocialPulseInterval))
            {
                return;
            }

            float chance = Mathf.Clamp01((initiator.kindDef == CADefOf.CrossedAlpha || initiator.kindDef == CADefOf.CrossedWarlord || initiator.kindDef == CADefOf.CrossedPyromaniac ? SocialPulseLeaderChance : SocialPulseBaseChance) * socialStrength);
            if (!Rand.Chance(chance))
            {
                return;
            }

            Pawn recipient = FindBestRecipient(initiator);
            if (recipient == null)
            {
                return;
            }

            InteractionDef interactionDef = PickInteraction(initiator, recipient);
            if (interactionDef != null)
            {
                TriggerInteraction(initiator, recipient, interactionDef);
            }
        }

        public static bool CanCrossedSocialInteract(Pawn initiator, Pawn recipient)
        {
            if (initiator == null || recipient == null || initiator == recipient || initiator.Map == null || recipient.Map != initiator.Map)
            {
                return false;
            }

            if (!CrossedUtility.IsCrossedPawn(initiator) || CrossedUtility.IsCrossedPawn(recipient))
            {
                return false;
            }

            return IsHumanlikeActivePawn(recipient) && IsPlayerAligned(recipient);
        }

        public static void ApplyCrossedSocialEffect(Pawn initiator, Pawn recipient, InteractionDef interactionDef)
        {
            if (!CanCrossedSocialInteract(initiator, recipient))
            {
                return;
            }

            if (TheMarkedMenSettings.SocialTerrorStrength <= 0f)
            {
                return;
            }

            ThoughtDef terror = CADefOf.CrossedSocialTerror;
            if (terror != null)
            {
                recipient.needs?.mood?.thoughts?.memories?.TryGainMemory(terror, initiator);
            }

            HediffDef panic = CADefOf.Panic;
            if (panic != null && recipient.health?.hediffSet != null && !recipient.health.hediffSet.HasHediff(panic))
            {
                recipient.health.AddHediff(panic);
            }

            if (interactionDef == CADefOf.CrossedPackLaughter || initiator.kindDef == CADefOf.CrossedPyromaniac || initiator.kindDef == CADefOf.CrossedAlpha || initiator.kindDef == CADefOf.CrossedWarlord)
            {
                CrossedUtility.ApplyMarkedPanic(recipient.Map, recipient.Position, PackPanicRadius);
            }
        }

        public static void TriggerInteraction(Pawn initiator, Pawn recipient, InteractionDef interactionDef)
        {
            if (interactionDef == null || !CanCrossedSocialInteract(initiator, recipient))
            {
                return;
            }

            ApplyCrossedSocialEffect(initiator, recipient, interactionDef);
            if (Find.PlayLog != null)
            {
                Find.PlayLog.Add(new PlayLogEntry_Interaction(interactionDef, initiator, recipient, new List<RulePackDef>()));
            }
        }

        private static Pawn FindBestRecipient(Pawn initiator)
        {
            Map map = initiator.Map;
            if (map?.mapPawns == null)
            {
                return null;
            }

            Pawn best = null;
            float bestScore = 0f;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!CanCrossedSocialInteract(initiator, candidate))
                {
                    continue;
                }

                float distanceSquared = initiator.Position.DistanceToSquared(candidate.Position);
                if (distanceSquared > MaxSocialTargetDistanceSquared)
                {
                    continue;
                }

                float score = 120f - Mathf.Sqrt(distanceSquared) * 4f;
                if (candidate.Downed)
                {
                    score += 70f;
                }

                if (candidate.ageTracker != null && candidate.ageTracker.AgeBiologicalYears < 18)
                {
                    score += 35f;
                }

                SkillRecord medicine = candidate.skills?.GetSkill(SkillDefOf.Medicine);
                if (medicine != null && medicine.Level >= 8)
                {
                    score += 35f;
                }

                if (!GenSight.LineOfSight(initiator.Position, candidate.Position, map))
                {
                    score *= 0.75f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static InteractionDef PickInteraction(Pawn initiator, Pawn recipient)
        {
            if (recipient.Downed && CADefOf.CrossedInfectionGloat != null)
            {
                return CADefOf.CrossedInfectionGloat;
            }

            if ((initiator.kindDef == CADefOf.CrossedPyromaniac || initiator.kindDef == CADefOf.CrossedAlpha || initiator.kindDef == CADefOf.CrossedWarlord) && CADefOf.CrossedPackLaughter != null && Rand.Chance(0.72f))
            {
                return CADefOf.CrossedPackLaughter;
            }

            float value = Rand.Value;
            if (value < 0.28f && CADefOf.CrossedFalseMercy != null)
            {
                return CADefOf.CrossedFalseMercy;
            }

            if (value < 0.74f && CADefOf.CrossedPredatoryTaunt != null)
            {
                return CADefOf.CrossedPredatoryTaunt;
            }

            if (CADefOf.CrossedInfectionGloat != null)
            {
                return CADefOf.CrossedInfectionGloat;
            }

            return CADefOf.CrossedPredatoryTaunt ?? CADefOf.CrossedFalseMercy ?? CADefOf.CrossedPackLaughter;
        }

        private static bool IsHumanlikeActivePawn(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && pawn.RaceProps != null && pawn.RaceProps.Humanlike;
        }

        private static bool IsPlayerAligned(Pawn pawn)
        {
            return pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer || pawn.IsColonistPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony;
        }
    }

    public class InteractionWorker_CrossedSocial : InteractionWorker
    {
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            if (!CrossedSocialUtility.CanCrossedSocialInteract(initiator, recipient))
            {
                return 0f;
            }

            if (interaction == CADefOf.CrossedPackLaughter && (initiator.kindDef == CADefOf.CrossedPyromaniac || initiator.kindDef == CADefOf.CrossedAlpha || initiator.kindDef == CADefOf.CrossedWarlord))
            {
                return 1.6f;
            }

            if (interaction == CADefOf.CrossedInfectionGloat && recipient.Downed)
            {
                return 1.8f;
            }

            return 0.7f;
        }

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            base.Interacted(initiator, recipient, extraSentencePacks, out letterText, out letterLabel, out letterDef, out lookTargets);
            CrossedSocialUtility.ApplyCrossedSocialEffect(initiator, recipient, interaction);
        }
    }
}
