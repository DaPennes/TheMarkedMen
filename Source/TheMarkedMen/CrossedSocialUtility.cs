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

        private const int QuickSocialPulseInterval = 600;
        private const float QuickSocialPulseChance = 0.15f;

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

            float chance = Mathf.Clamp01(SocialPulseBaseChance * socialStrength);
            if (!Rand.Chance(chance))
            {
                return;
            }

            Pawn recipient = FindBestRecipient(initiator, true);
            if (recipient == null)
            {
                return;
            }

            InteractionDef interactionDef = PickInteraction(initiator, recipient, true);
            if (interactionDef != null)
            {
                TriggerInteraction(initiator, recipient, interactionDef);
            }
        }

        public static void TryQuickSocialPulse(Pawn initiator)
        {
            if (initiator == null || !initiator.Spawned || initiator.Dead || initiator.Map == null || !CrossedUtility.IsCrossedPawn(initiator))
            {
                return;
            }

            float socialStrength = TheMarkedMenSettings.SocialTerrorStrength;
            if (socialStrength <= 0f || !initiator.IsHashIntervalTick(QuickSocialPulseInterval))
            {
                return;
            }

            float chance = Mathf.Clamp01(QuickSocialPulseChance * socialStrength);
            if (!Rand.Chance(chance))
            {
                return;
            }

            Pawn recipient = FindQuickRecipient(initiator);
            if (recipient == null)
            {
                return;
            }

            InteractionDef interactionDef = PickQuickInteraction(initiator, recipient);
            if (interactionDef != null)
            {
                TriggerInteraction(initiator, recipient, interactionDef);
            }
        }

        private static Pawn FindQuickRecipient(Pawn initiator)
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
                if (!CanCrossedSocialInteract(initiator, candidate, true))
                {
                    continue;
                }

                float distanceSquared = initiator.Position.DistanceToSquared(candidate.Position);
                if (distanceSquared > MaxSocialTargetDistanceSquared)
                {
                    continue;
                }

                float score = 150f - Mathf.Sqrt(distanceSquared) * 3f;
                if (candidate.Downed)
                {
                    score += 70f;
                }

                if (candidate.ageTracker != null && candidate.ageTracker.AgeBiologicalYears < 18)
                {
                    score += 35f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        public static bool CanCrossedSocialInteract(Pawn initiator, Pawn recipient, bool allowInfectedToInfected = false)
        {
            if (initiator == null || recipient == null || initiator == recipient || initiator.Map == null || recipient.Map != initiator.Map)
            {
                return false;
            }

            if (!CrossedUtility.IsCrossedPawn(initiator))
            {
                return false;
            }

            if (CrossedUtility.IsCrossedPawn(recipient))
            {
                return allowInfectedToInfected && TheMarkedMenSettings.SocialTerrorStrength > 0f;
            }

            return IsHumanlikeActivePawn(recipient);
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

            CrossedUtility.ApplyMarkedPanicToPawn(recipient);

            if (interactionDef == CADefOf.CrossedPackLaughter)
            {
                CrossedUtility.ApplyMarkedPanic(recipient.Map, recipient.Position, TheMarkedMenSettings.MarkedPanicRadius);
            }
        }

        public static void TriggerInteraction(Pawn initiator, Pawn recipient, InteractionDef interactionDef)
        {
            if (interactionDef == null)
            {
                return;
            }

            if (!CanCrossedSocialInteract(initiator, recipient))
            {
                return;
            }

            List<RulePackDef> extraSentencePacks = GetSentencePacks(interactionDef);

            if (interactionDef.Worker != null)
            {
                interactionDef.Worker.Interacted(initiator, recipient, extraSentencePacks, out _, out _, out _, out _);
            }
        }

        private static List<RulePackDef> GetSentencePacks(InteractionDef interactionDef)
        {
            if (interactionDef == null)
                return new List<RulePackDef>();

            List<RulePackDef> result = new List<RulePackDef>();

            // Predatory taunt - sadistic attacks revealing weaknesses
            if (interactionDef == CADefOf.CrossedPredatoryTaunt)
            {
                if (CADefOf.CA_CrossedSpeechPredatory != null)
                    result.Add(CADefOf.CA_CrossedSpeechPredatory);
            }
            // False mercy - deceptive mockery
            else if (interactionDef == CADefOf.CrossedFalseMercy)
            {
                if (CADefOf.CA_CrossedSpeechFalseMercy != null)
                    result.Add(CADefOf.CA_CrossedSpeechFalseMercy);
            }
            // Pack laughter - coordinated sadism
            else if (interactionDef == CADefOf.CrossedPackLaughter)
            {
                if (CADefOf.CA_CrossedSpeechPackLaughter != null)
                    result.Add(CADefOf.CA_CrossedSpeechPackLaughter);
            }
            // Infection gloat - transformation horror
            else if (interactionDef == CADefOf.CrossedInfectionGloat)
            {
                if (CADefOf.CA_CrossedSpeechInfection != null)
                    result.Add(CADefOf.CA_CrossedSpeechInfection);
            }
            // Prisoner harassment - mockery of prisoners
            else if (interactionDef == CADefOf.CrossedPrisonerHarassment)
            {
                if (CADefOf.CA_CrossedSpeechPrisonerHarassment != null)
                    result.Add(CADefOf.CA_CrossedSpeechPrisonerHarassment);
                if (CADefOf.CA_CrossedSpeechWoundedMockery != null)
                    result.Add(CADefOf.CA_CrossedSpeechWoundedMockery);
            }
            // Wounded mockery - targeting injured victims
            else if (interactionDef == CADefOf.CrossedWoundedMockery)
            {
                if (CADefOf.CA_CrossedSpeechWoundedMockery != null)
                    result.Add(CADefOf.CA_CrossedSpeechWoundedMockery);
            }
            // Escape taunt - mocking escape attempts
            else if (interactionDef == CADefOf.CrossedEscapeTaunt)
            {
                if (CADefOf.CA_CrossedSpeechEscapeTaunt != null)
                    result.Add(CADefOf.CA_CrossedSpeechEscapeTaunt);
            }
            // Group coordination - pack-level communication
            else if (interactionDef == CADefOf.CrossedGroupCoordination)
            {
                if (CADefOf.CA_CrossedSpeechGroupCoordination != null)
                    result.Add(CADefOf.CA_CrossedSpeechGroupCoordination);
            }
            // Companion mockery - mocking other survivors
            else if (interactionDef == CADefOf.CrossedCompanionMockery)
            {
                if (CADefOf.CA_CrossedSpeechCompanionMockery != null)
                    result.Add(CADefOf.CA_CrossedSpeechCompanionMockery);
            }
            // Pack announcement - announcing victims to the pack
            else if (interactionDef == CADefOf.CrossedPackAnnouncement)
            {
                if (CADefOf.CA_CrossedSpeechPackAnnouncement != null)
                    result.Add(CADefOf.CA_CrossedSpeechPackAnnouncement);
            }

            return result;
        }

        private static Pawn FindBestRecipient(Pawn initiator, bool allowInfectedToInfected)
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
                if (!CanCrossedSocialInteract(initiator, candidate, allowInfectedToInfected))
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

        private static InteractionDef PickInteraction(Pawn initiator, Pawn recipient, bool isInfectedToInfected = false)
        {
            // For infected-to-infected interactions, use pack laughter or infection gloat
            if (isInfectedToInfected)
            {
                float rValue = Rand.Value;
                if (rValue < 0.4f && CADefOf.CrossedPackLaughter != null)
                {
                    return CADefOf.CrossedPackLaughter;
                }
                return CADefOf.CrossedInfectionGloat ?? CADefOf.CrossedPackLaughter;
            }

            // Wounded target - use infection gloat to emphasize suffering
            if (recipient.Downed && CADefOf.CrossedInfectionGloat != null)
            {
                return CADefOf.CrossedInfectionGloat;
            }

            // Civilian initiate - pack laughter is primary
            if ((initiator.kindDef == CADefOf.CrossedCivilian) && CADefOf.CrossedPackLaughter != null)
            {
                return CADefOf.CrossedPackLaughter;
            }

            float r = Rand.Value;

            // False mercy - deceptive approach
            if (r < 0.15f && CADefOf.CrossedFalseMercy != null)
            {
                return CADefOf.CrossedFalseMercy;
            }

            // Predatory taunt - direct sadistic attack
            if (r < 0.45f && CADefOf.CrossedPredatoryTaunt != null)
            {
                return CADefOf.CrossedPredatoryTaunt;
            }

            // Prisoner harassment - when targeting prisoners
            if (recipient.IsPrisonerOfColony)
            {
                return CADefOf.CrossedPackLaughter;
            }

            // Infection gloat - transformation horror
            if (CADefOf.CrossedInfectionGloat != null)
            {
                return CADefOf.CrossedInfectionGloat;
            }

            return CADefOf.CrossedPredatoryTaunt ?? CADefOf.CrossedFalseMercy ?? CADefOf.CrossedPackLaughter;
        }

        private static InteractionDef PickQuickInteraction(Pawn initiator, Pawn recipient)
        {
            if (recipient == null)
                return null;

            float r = Rand.Value;

            if (r < 0.25f && CADefOf.CrossedPredatoryTaunt != null)
            {
                return CADefOf.CrossedPredatoryTaunt;
            }

            if (r < 0.5f && CADefOf.CrossedPackLaughter != null)
            {
                return CADefOf.CrossedPackLaughter;
            }

            if (r < 0.75f && CADefOf.CrossedEscapeTaunt != null)
            {
                return CADefOf.CrossedEscapeTaunt;
            }

            if (recipient.Downed && CADefOf.CrossedInfectionGloat != null)
            {
                return CADefOf.CrossedInfectionGloat;
            }

            return CADefOf.CrossedPredatoryTaunt ?? CADefOf.CrossedPackLaughter ?? CADefOf.CrossedEscapeTaunt;
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
            bool isInfectedToInfected = CrossedUtility.IsCrossedPawn(recipient);
            if (!CrossedSocialUtility.CanCrossedSocialInteract(initiator, recipient, isInfectedToInfected))
            {
                return 0f;
            }

            // Civilian initiate - pack laughter gets extra weight
            if (interaction == CADefOf.CrossedPackLaughter && (initiator.kindDef == CADefOf.CrossedCivilian))
            {
                return 1.2f;
            }

            // Wounded target - infection gloat is extra effective
            if (interaction == CADefOf.CrossedInfectionGloat && recipient.Downed)
            {
                return 1.8f;
            }

            // Prisoner harassment - extra weight for prisoners
            if (interaction == CADefOf.CrossedPackLaughter && recipient.IsPrisonerOfColony)
            {
                return 1.3f;
            }

            // Group coordination - weight for Civilian initiates
            if (interaction == CADefOf.CrossedGroupCoordination && initiator.kindDef == CADefOf.CrossedCivilian)
            {
                return 0.9f;
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