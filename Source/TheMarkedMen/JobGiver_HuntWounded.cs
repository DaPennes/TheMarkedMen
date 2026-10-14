using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// JobGiver for infected to prioritize hunting wounded/downed targets.
    /// Actively seeks out and pursues injured colonists.
    /// </summary>
    public class JobGiver_HuntWounded : ThinkNode_JobGiver
    {
        private const int WoundCheckIntervalTicks = 150;
        private const float MaxWoundSearchDistance = 25f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return null;

            // Only check periodically
            if (!pawn.IsHashIntervalTick(WoundCheckIntervalTicks))
                return null;

            Pawn woundedTarget = FindBestWoundedTarget(pawn);
            if (woundedTarget == null)
                return null;

            // Chase and attack the wounded target
            return new Job(JobDefOf.AttackMelee, woundedTarget) { checkOverrideOnExpire = true };
        }

        private Pawn FindBestWoundedTarget(Pawn pawn)
        {
            IReadOnlyList<Pawn> candidates = pawn.Map.mapPawns.AllPawnsSpawned;
            Pawn bestTarget = null;
            float bestScore = 0f;

            foreach (Pawn candidate in candidates)
            {
                if (candidate == null || candidate.Dead || candidate == pawn)
                    continue;

                if (CrossedUtility.IsInfectedPawn(candidate))
                    continue;

                // Must be within reasonable distance
                if (!pawn.CanReach(candidate, PathEndMode.Touch, Danger.Deadly))
                    continue;

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance > MaxWoundSearchDistance)
                    continue;

                // Score based on wound severity
                float score = 1f;

                // Prioritize downed targets
                if (candidate.Downed)
                    score += 200f;
                // Prioritize wounded targets
                else if (candidate.health?.hediffSet != null)
                {
                    List<Hediff_Injury> injuries = new List<Hediff_Injury>();
                    candidate.health.hediffSet.GetHediffs(ref injuries, null);
                    float woundPct = Mathf.InverseLerp(0f, 1f, injuries.Count / (float)candidate.health.hediffSet.hediffs.Count);
                    score += woundPct * 100f;
                }

                // Colonist bonus
                if (candidate.IsColonist)
                    score += 50f;

                // Closer targets score higher
                score += Mathf.Max(0f, 20f - distance);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }
    }
}