using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
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

            CrossedUtility.ApplyMarkedPanicToPawn(recipient);

            if (interactionDef == CADefOf.CrossedPackLaughter)
            {
                CrossedUtility.ApplyMarkedPanic(recipient.Map, recipient.Position, TheMarkedMenSettings.MarkedPanicRadius);
            }
        }

        public static void TriggerInteraction(Pawn initiator, Pawn recipient, InteractionDef interactionDef)
        {
            if (interactionDef == null || !CanCrossedSocialInteract(initiator, recipient))
            {
                return;
            }

            ApplyCrossedSocialEffect(initiator, recipient, interactionDef);

            List<RulePackDef> extraSentencePacks = GetSentencePacks(interactionDef);

            string letterText = null;
            string letterLabel = null;
            LetterDef letterDef = null;
            LookTargets lookTargets = new LookTargets(initiator, recipient);

            if (interactionDef.Worker != null)
            {
                interactionDef.Worker.Interacted(initiator, recipient, extraSentencePacks, out letterText, out letterLabel, out letterDef, out lookTargets);
            }

            string speechText = ResolveSpeechText(interactionDef, initiator, recipient, extraSentencePacks);
            DisplaySpeech(initiator, recipient, speechText);

            if (Find.PlayLog != null)
            {
                Find.PlayLog.Add(new PlayLogEntry_Interaction(interactionDef, initiator, recipient, extraSentencePacks));
            }
        }

        private static string ResolveSpeechText(InteractionDef interactionDef, Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks)
        {
            if (interactionDef == null || initiator == null || recipient == null)
                return null;

            try
            {
                Type speechType = AccessTools.TypeByName("Speech");
                if (speechType == null)
                    return null;

                MethodInfo resolveMethod = AccessTools.Method(speechType, "ResolveText",
                    new[] { typeof(InteractionDef), typeof(Pawn), typeof(Pawn), typeof(List<RulePackDef>) });

                if (resolveMethod == null)
                    return null;

                return (string)resolveMethod.Invoke(null, new object[] { interactionDef, initiator, recipient, extraSentencePacks });
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Failed to resolve speech text via reflection: " + ex.Message);
                return null;
            }
        }

        private static void DisplaySpeech(Pawn initiator, Pawn recipient, string speechText)
        {
            if (initiator == null || recipient == null || string.IsNullOrEmpty(speechText))
            {
                return;
            }
            if (initiator.Map == null || recipient.Map != initiator.Map)
            {
                return;
            }

            Color textcolor = Color.white;
            MoteMaker.ThrowText(initiator.DrawPos, initiator.Map, speechText, textcolor, 3.5f);
            MoteMaker.ThrowText(recipient.DrawPos, recipient.Map, speechText, textcolor, 3.5f);
        }

        private static List<RulePackDef> GetSentencePacks(InteractionDef interactionDef)
        {
            List<RulePackDef> result = new List<RulePackDef>();

            if (interactionDef == CADefOf.CrossedPredatoryTaunt)
            {
                if (CADefOf.CA_CrossedSpeechPredatory != null)
                {
                    result.Add(CADefOf.CA_CrossedSpeechPredatory);
                }
            }
            else if (interactionDef == CADefOf.CrossedFalseMercy)
            {
                if (CADefOf.CA_CrossedSpeechFalseMercy != null)
                {
                    result.Add(CADefOf.CA_CrossedSpeechFalseMercy);
                }
            }
            else if (interactionDef == CADefOf.CrossedPackLaughter)
            {
                if (CADefOf.CA_CrossedSpeechPackLaughter != null)
                {
                    result.Add(CADefOf.CA_CrossedSpeechPackLaughter);
                }
            }
            else if (interactionDef == CADefOf.CrossedInfectionGloat)
            {
                if (CADefOf.CA_CrossedSpeechInfection != null)
                {
                    result.Add(CADefOf.CA_CrossedSpeechInfection);
                }
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
                float value = Rand.Value;
                if (value < 0.5f && CADefOf.CrossedPackLaughter != null)
                {
                    return CADefOf.CrossedPackLaughter;
                }
                return CADefOf.CrossedInfectionGloat ?? CADefOf.CrossedPackLaughter;
            }

            if (recipient.Downed && CADefOf.CrossedInfectionGloat != null)
            {
                return CADefOf.CrossedInfectionGloat;
            }

            if ((initiator.kindDef == CADefOf.CrossedCivilian) && CADefOf.CrossedPackLaughter != null && Rand.Chance(0.5f))
            {
                return CADefOf.CrossedPackLaughter;
            }

            float value2 = Rand.Value;
            if (value2 < 0.28f && CADefOf.CrossedFalseMercy != null)
            {
                return CADefOf.CrossedFalseMercy;
            }

            if (value2 < 0.74f && CADefOf.CrossedPredatoryTaunt != null)
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

            return 0.7f;
        }

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            base.Interacted(initiator, recipient, extraSentencePacks, out letterText, out letterLabel, out letterDef, out lookTargets);
            CrossedSocialUtility.ApplyCrossedSocialEffect(initiator, recipient, interaction);
        }
    }
}