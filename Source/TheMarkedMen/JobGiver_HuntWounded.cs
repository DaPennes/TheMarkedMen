using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// JobGiver for infected to prioritize wounded and downed non-infected targets.
    /// Scans for downed colonists and injured pawns; pursues them preferentially.
    /// </summary>
    public class JobGiver_HuntWounded : ThinkNode_JobGiver
    {
        private const int WoundedSearchIntervalTicks = 200;
        private const float WoundedSearchRadius = 50f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return null;

            if (!pawn.IsHashIntervalTick(WoundedSearchIntervalTicks))
                return null;

            // Search for downed or heavily wounded pawns
            Pawn woundedTarget = FindWoundedTarget(pawn);
            if (woundedTarget != null)
            {
                return new Job(JobDefOf.AttackMelee, woundedTarget) { checkOverrideOnExpire = true };
            }

            return null;
        }

        private Pawn FindWoundedTarget(Pawn pawn)
        {
            if (pawn.Map == null)
                return null;

            Pawn bestTarget = null;
            float bestScore = 0f;

            // Check all pawns on map
            IReadOnlyList<Pawn> allPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            foreach (Pawn candidate in allPawns)
            {
                if (candidate == null || candidate.Dead || candidate == pawn)
                    continue;

                if (CrossedUtility.IsInfectedPawn(candidate))
                    continue;

                if (!pawn.CanReach(candidate, PathEndMode.Touch, Danger.Deadly))
                    continue;

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance > WoundedSearchRadius)
                    continue;

                float score = 0f;

                // Downed targets are highest priority
                if (candidate.Downed)
                {
                    score = 1000f;
                }
                else if (candidate.health?.hediffSet != null)
                {
                    // Count injury hediffs; more injuries = higher priority
                    List<Hediff_Injury> injuries = new List<Hediff_Injury>();
                    candidate.health.hediffSet.GetHediffs(ref injuries, null);
                    if (injuries.Count > 0)
                    {
                        score = 500f + (injuries.Count * 100f);

                        // Add health ratio (lower health = higher priority)
                        float healthPercent = candidate.health.summaryHealth.SummaryHealthPercent;
                        if (healthPercent < 0.5f)
                            score += (1f - healthPercent) * 200f;
                    }
                }

                // Reduce score by distance (closer = better)
                score -= distance * 2f;

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
