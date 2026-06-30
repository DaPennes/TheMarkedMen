using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    public static class MarkedMenPrisonerUtility
    {
        public static bool IsMarkedPrisoner(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && pawn.IsPrisonerOfColony
                && CrossedUtility.IsCrossedPawn(pawn);
        }

        public static bool CanBeMarkedPrisonerTarget(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && !pawn.Downed
                && pawn.Spawned
                && pawn.RaceProps.Humanlike
                && !CrossedUtility.IsCrossedPawn(pawn);
        }

        public static Pawn SelectBestAttackTarget(Pawn prisoner, Map map, float rangeMultiplier = 1f)
        {
            if (prisoner == null || map == null)
            {
                return null;
            }

            Pawn best = null;
            float bestScore = 0f;
            IntVec3 pos = prisoner.Position;
            float maxDist = 15f * Mathf.Max(0.1f, rangeMultiplier);
            float maxDistSq = maxDist * maxDist;

            IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn candidate = allPawns[i];
                if (!CanBeMarkedPrisonerTarget(candidate))
                {
                    continue;
                }

                float distSq = pos.DistanceToSquared(candidate.Position);
                if (distSq > maxDistSq)
                {
                    continue;
                }

                float score = ScoreTarget(candidate, prisoner, distSq, maxDist);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static float ScoreTarget(Pawn candidate, Pawn prisoner, float distSq, float maxDist)
        {
            float score = 100f;

            if (!candidate.WorkTypeIsDisabled(WorkTypeDefOf.Warden))
            {
                score += 500f;
            }
            if (!candidate.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
            {
                score += 400f;
            }
            if (candidate.IsColonist)
            {
                score += 200f;
            }
            if (candidate.IsPrisonerOfColony && !CrossedUtility.IsCrossedPawn(candidate))
            {
                score += 50f;
            }
            if (candidate.IsSlaveOfColony)
            {
                score += 100f;
            }
            if (candidate.RaceProps.Animal)
            {
                score -= 300f;
            }

            float distFactor = 1f - Mathf.Sqrt(distSq) / maxDist;
            score *= Mathf.Max(0.1f, distFactor);

            return score;
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), "Interacted")]
    public static class Patch_MarkedPrisonerRecruitBlock
    {
        public static bool Prefix(Pawn initiator, Pawn recipient)
        {
            if (!MarkedMenPrisonerUtility.IsMarkedPrisoner(recipient))
            {
                return true;
            }

            if (recipient.guest != null)
            {
                string message = "CA_MarkedPrisoner_RecruitBlocked".Translate(recipient.Named("PAWN"));
                Messages.Message(message, recipient, MessageTypeDefOf.RejectInput, false);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_Suppress), "Interacted")]
    public static class Patch_MarkedPrisonerSuppressBlock
    {
        public static bool Prefix(Pawn initiator, Pawn recipient)
        {
            if (!MarkedMenPrisonerUtility.IsMarkedPrisoner(recipient))
            {
                return true;
            }

            if (recipient.guest != null)
            {
                string message = "CA_MarkedPrisoner_RecruitBlocked".Translate(recipient.Named("PAWN"));
                Messages.Message(message, recipient, MessageTypeDefOf.RejectInput, false);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_ConvertIdeoAttempt), "Interacted")]
    public static class Patch_MarkedPrisonerConvertBlock
    {
        public static bool Prefix(Pawn initiator, Pawn recipient)
        {
            if (!MarkedMenPrisonerUtility.IsMarkedPrisoner(recipient))
            {
                return true;
            }

            if (recipient.guest != null)
            {
                string message = "CA_MarkedPrisoner_RecruitBlocked".Translate(recipient.Named("PAWN"));
                Messages.Message(message, recipient, MessageTypeDefOf.RejectInput, false);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(PrisonBreakUtility), "StartPrisonBreak", new Type[] { typeof(Pawn) })]
    public static class Patch_MarkedPrisonerEscapeAggression
    {
        public static void Postfix(Pawn initiator)
        {
            if (initiator == null || initiator.Map == null)
            {
                return;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.prisonerInfectionEnabled)
            {
                return;
            }

            Map map = initiator.Map;
            List<Pawn> allPawns = map.mapPawns.AllPawnsSpawned.ToList();
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (!MarkedMenPrisonerUtility.IsMarkedPrisoner(pawn))
                {
                    continue;
                }

                if (pawn.Downed || pawn.Dead)
                {
                    continue;
                }

                ModifyEscapeBehavior(pawn, settings);
            }
        }

        private static void ModifyEscapeBehavior(Pawn pawn, TheMarkedMenSettings settings)
        {
            if (pawn?.mindState == null)
            {
                return;
            }

            pawn.mindState.canFleeIndividual = false;

            if (pawn.mindState.mentalStateHandler != null)
            {
                pawn.mindState.mentalStateHandler.neverFleeIndividual = true;
            }

            float rangeMult = Mathf.Max(0.1f, settings.prisonerEscapeAggressionMultiplier);
            Pawn target = MarkedMenPrisonerUtility.SelectBestAttackTarget(pawn, pawn.Map, rangeMult);
            if (target != null)
            {
                Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
                attackJob.canBashDoors = true;
                attackJob.locomotionUrgency = LocomotionUrgency.Sprint;
                attackJob.expiryInterval = 300;
                attackJob.checkOverrideOnExpire = true;
                pawn.jobs.TryTakeOrderedJob(attackJob, JobTag.Misc);
            }

            if (settings.prisonerDebugLogging)
            {
                Log.Message($"[TheMarkedMen] Marked prisoner {pawn.LabelShort} is escaping with aggression modifier");
            }
        }
    }

    [HarmonyPatch(typeof(PrisonBreakUtility), "CanParticipateInPrisonBreak")]
    public static class Patch_MarkedPrisonerAlwaysBreaks
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (__result || TheMarkedMenMod.Settings == null || !TheMarkedMenMod.Settings.prisonerInfectionEnabled)
            {
                return;
            }

            if (MarkedMenPrisonerUtility.IsMarkedPrisoner(pawn))
            {
                __result = true;
            }
        }
    }
}
