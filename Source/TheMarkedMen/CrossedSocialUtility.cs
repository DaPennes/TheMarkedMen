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
            if (initiator == null || !initiator.Spawned || initiator.Dead || initiator.Downed || initiator.Map == null)
            {
                return;
            }

            
            if (CrossedUtility.IsCrossedPawn(initiator))
            {
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
            else
            {
                
                TryGeneralSocialPulse(initiator);
            }
        }

        
        public static void TryGeneralSocialPulse(Pawn initiator)
        {
            
            float chance = Mathf.Clamp01(TheMarkedMenSettings.GeneralSocialPulseChance);
            if (!initiator.IsHashIntervalTick(SocialPulseInterval) || !Rand.Chance(chance))
            {
                return;
            }

            Pawn recipient = FindBestGeneralRecipient(initiator);
            if (recipient == null)
            {
                return;
            }

            InteractionDef interactionDef = PickGeneralInteraction(initiator, recipient);
            if (interactionDef != null)
            {
                TriggerInteraction(initiator, recipient, interactionDef);
            }
        }

        private static Pawn FindBestGeneralRecipient(Pawn initiator)
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
                if (!CanGeneralSocialInteract(initiator, candidate))
                {
                    continue;
                }

                float distanceSquared = initiator.Position.DistanceToSquared(candidate.Position);
                if (distanceSquared > MaxSocialTargetDistanceSquared)
                {
                    continue;
                }

                float score = 100f - Mathf.Sqrt(distanceSquared) * 3f;
                if (initiator.Faction.HostileTo(candidate.Faction))
                {
                    score += 50f;
                }
                if (initiator.relations.OpinionOf(candidate) < 0)
                {
                    score += 30f;
                }
                if (initiator.IsPrisonerOfColony || candidate.IsPrisonerOfColony)
                {
                    score += 20f;
                }
                if (initiator.Downed || candidate.Downed)
                {
                    score += 40f;
                }
                if (initiator.ageTracker.Adult || candidate.ageTracker.Adult)
                {
                    
                    score += 25f;
                }
                if (!GenSight.LineOfSight(initiator.Position, candidate.Position, map))
                {
                    score *= 0.6f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        public static bool CanGeneralSocialInteract(Pawn initiator, Pawn recipient)
        {
            if (initiator == null || recipient == null || initiator == recipient || initiator.Map == null || recipient.Map != initiator.Map)
            {
                return false;
            }

            
            if (CrossedUtility.IsCrossedPawn(initiator) || CrossedUtility.IsCrossedPawn(recipient))
            {
                return false;
            }

            
            return IsHumanlikeActivePawn(initiator) && IsHumanlikeActivePawn(recipient);
        }

        private static InteractionDef PickGeneralInteraction(Pawn initiator, Pawn recipient)
        {
            float r = Rand.Value;
            bool hostile = initiator.Faction.HostileTo(recipient.Faction);
            bool initiatorPrisoner = initiator.IsPrisonerOfColony;
            bool recipientPrisoner = recipient.IsPrisonerOfColony;
            float opinion = initiator.relations.OpinionOf(recipient);

            
            if (hostile)
            {
                if (r < 0.3f && CADefOf.CA_GeneralHostileStandoff != null)
                {
                    return CADefOf.CA_GeneralHostileStandoff;
                }
                if (r < 0.6f && CADefOf.CA_GeneralThreateningTaunt != null)
                {
                    return CADefOf.CA_GeneralThreateningTaunt;
                }
                return CADefOf.CA_GeneralDefiantInsult ?? CADefOf.CA_GeneralHostileStandoff;
            }

            
            else if (initiatorPrisoner && recipientPrisoner)
            {
                if (r < 0.4f && CADefOf.CA_GeneralPrisonerSolidarity != null)
                {
                    return CADefOf.CA_GeneralPrisonerSolidarity;
                }
                return CADefOf.CA_GeneralPrisonerSmallTalk ?? CADefOf.CA_GeneralPrisonerSolidarity;
            }
            else if (initiatorPrisoner)
            {
                if (opinion < -10 && CADefOf.CA_GeneralPrisonerDefiance != null)
                {
                    return CADefOf.CA_GeneralPrisonerDefiance;
                }
                return CADefOf.CA_GeneralPrisonerResignation ?? CADefOf.CA_GeneralPrisonerSmallTalk;
            }
            else if (recipientPrisoner)
            {
                if (opinion > 10 && CADefOf.CA_GeneralGuardEncouragement != null)
                {
                    return CADefOf.CA_GeneralGuardEncouragement;
                }
                return CADefOf.CA_GeneralGuardIntimidation ?? CADefOf.CA_GeneralThreateningTaunt;
            }

            
            else
            {
                if (opinion > 20)
                {
                    if (r < 0.5f && CADefOf.CA_GeneralFriendlyGreeting != null)
                    {
                        return CADefOf.CA_GeneralFriendlyGreeting;
                    }
                    return CADefOf.CA_GeneralCasualChat ?? CADefOf.CA_GeneralFriendlyGreeting;
                }
                else if (opinion < -10)
                {
                    if (r < 0.4f && CADefOf.CA_GeneralTenseStandoff != null)
                    {
                        return CADefOf.CA_GeneralTenseStandoff;
                    }
                    return CADefOf.CA_GeneralColdShoulder ?? CADefOf.CA_GeneralTenseStandoff;
                }
                else
                {
                    if (r < 0.4f && CADefOf.CA_GeneralNervousAvoidance != null)
                    {
                        return CADefOf.CA_GeneralNervousAvoidance;
                    }
                    return CADefOf.CA_GeneralAwkwardSilence ?? CADefOf.CA_GeneralNervousAvoidance;
                }
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

            
            if (interactionDef == CADefOf.CrossedPredatoryTaunt)
            {
                if (CADefOf.CA_CrossedSpeechPredatory != null)
                    result.Add(CADefOf.CA_CrossedSpeechPredatory);
            }
            
            else if (interactionDef == CADefOf.CrossedFalseMercy)
            {
                if (CADefOf.CA_CrossedSpeechFalseMercy != null)
                    result.Add(CADefOf.CA_CrossedSpeechFalseMercy);
            }
            
            else if (interactionDef == CADefOf.CrossedPackLaughter)
            {
                if (CADefOf.CA_CrossedSpeechPackLaughter != null)
                    result.Add(CADefOf.CA_CrossedSpeechPackLaughter);
            }
            
            else if (interactionDef == CADefOf.CrossedInfectionGloat)
            {
                if (CADefOf.CA_CrossedSpeechInfection != null)
                    result.Add(CADefOf.CA_CrossedSpeechInfection);
            }
            
            else if (interactionDef == CADefOf.CrossedPrisonerHarassment)
            {
                if (CADefOf.CA_CrossedSpeechPrisonerHarassment != null)
                    result.Add(CADefOf.CA_CrossedSpeechPrisonerHarassment);
                if (CADefOf.CA_CrossedSpeechWoundedMockery != null)
                    result.Add(CADefOf.CA_CrossedSpeechWoundedMockery);
            }
            
            else if (interactionDef == CADefOf.CrossedWoundedMockery)
            {
                if (CADefOf.CA_CrossedSpeechWoundedMockery != null)
                    result.Add(CADefOf.CA_CrossedSpeechWoundedMockery);
            }
            
            else if (interactionDef == CADefOf.CrossedEscapeTaunt)
            {
                if (CADefOf.CA_CrossedSpeechEscapeTaunt != null)
                    result.Add(CADefOf.CA_CrossedSpeechEscapeTaunt);
            }
            
            else if (interactionDef == CADefOf.CrossedGroupCoordination)
            {
                if (CADefOf.CA_CrossedSpeechGroupCoordination != null)
                    result.Add(CADefOf.CA_CrossedSpeechGroupCoordination);
            }
            
            else if (interactionDef == CADefOf.CrossedCompanionMockery)
            {
                if (CADefOf.CA_CrossedSpeechCompanionMockery != null)
                    result.Add(CADefOf.CA_CrossedSpeechCompanionMockery);
            }
            
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
            
            if (isInfectedToInfected)
            {
                float rValue = Rand.Value;
                if (rValue < 0.4f && CADefOf.CrossedPackLaughter != null)
                {
                    return CADefOf.CrossedPackLaughter;
                }
                return CADefOf.CrossedInfectionGloat ?? CADefOf.CrossedPackLaughter;
            }

            
            if (recipient.Downed && CADefOf.CrossedInfectionGloat != null)
            {
                return CADefOf.CrossedInfectionGloat;
            }

            
            if ((initiator.kindDef == CADefOf.CrossedCivilian) && CADefOf.CrossedPackLaughter != null)
            {
                return CADefOf.CrossedPackLaughter;
            }

            float r = Rand.Value;

            
            if (r < 0.15f && CADefOf.CrossedFalseMercy != null)
            {
                return CADefOf.CrossedFalseMercy;
            }

            
            if (r < 0.45f && CADefOf.CrossedPredatoryTaunt != null)
            {
                return CADefOf.CrossedPredatoryTaunt;
            }

            
            if (recipient.IsPrisonerOfColony)
            {
                return CADefOf.CrossedPackLaughter;
            }

            
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

            
            if (interaction == CADefOf.CrossedPackLaughter && (initiator.kindDef == CADefOf.CrossedCivilian))
            {
                return 1.2f;
            }

            
            if (interaction == CADefOf.CrossedInfectionGloat && recipient.Downed)
            {
                return 1.8f;
            }

            
            if (interaction == CADefOf.CrossedPackLaughter && recipient.IsPrisonerOfColony)
            {
                return 1.3f;
            }

            
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