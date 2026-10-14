using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// JobGiver for infected to find and pursue nearest non-infected target with memory-based tracking.
    /// Caches target for 250 ticks; tracks last-seen position if target LOS breaks.
    /// </summary>
    public class JobGiver_FindNearestTarget : ThinkNode_JobGiver
    {
        private const int TargetCacheIntervalTicks = 250;
        private static readonly Dictionary<int, (Pawn target, IntVec3 lastSeenPos, int lastSeenTick)> targetMemory
            = new Dictionary<int, (Pawn, IntVec3, int)>();

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return null;

            int pawnId = pawn.thingIDNumber;
            int currentTick = Find.TickManager.TicksGame;

            // Check memory for cached target
            if (targetMemory.TryGetValue(pawnId, out var memory))
            {
                // If target still alive and in LOS, continue pursuit
                if (memory.target != null && !memory.target.Dead && GenSight.LineOfSight(pawn.Position, memory.target.Position, pawn.Map))
                {
                    // Still valid - attack
                    return new Job(JobDefOf.AttackMelee, memory.target) { checkOverrideOnExpire = true };
                }

                // Target lost or dead. Hunt last-seen position if recent
                if (currentTick - memory.lastSeenTick < 500)
                {
                    return new Job(JobDefOf.Goto, memory.lastSeenPos) { checkOverrideOnExpire = true };
                }

                // Memory stale - clear it
                targetMemory.Remove(pawnId);
            }

            // Cache expired or no memory - search for new target
            if (!pawn.IsHashIntervalTick(TargetCacheIntervalTicks))
                return null;

            Pawn bestTarget = FindBestTarget(pawn);
            if (bestTarget != null)
            {
                // Store in memory
                targetMemory[pawnId] = (bestTarget, bestTarget.Position, currentTick);
                return new Job(JobDefOf.AttackMelee, bestTarget) { checkOverrideOnExpire = true };
            }

            return null;
        }

        private Pawn FindBestTarget(Pawn pawn)
        {
            // Priority: wounded/downed colonists first, then any non-infected
            IReadOnlyList<Pawn> candidates = pawn.Map.mapPawns.AllPawnsSpawned;
            Pawn bestTarget = null;
            float bestScore = 0f;

            foreach (Pawn candidate in candidates)
            {
                if (candidate == null || candidate.Dead || candidate == pawn)
                    continue;

                if (CrossedUtility.IsInfectedPawn(candidate))
                    continue;

                if (!pawn.CanReach(candidate, PathEndMode.Touch, Danger.Deadly))
                    continue;

                // Score: wounded/downed are prioritized
                float score = 1f;
                if (candidate.Downed)
                    score += 100f;
                else if (candidate.health?.hediffSet != null)
                {
                    List<Hediff_Injury> injuries = new List<Hediff_Injury>();
                    candidate.health.hediffSet.GetHediffs(ref injuries, null);
                    if (injuries.Count > 0)
                        score += 50f;
                }

                // Closer targets score higher
                float distance = pawn.Position.DistanceTo(candidate.Position);
                score += Mathf.Max(0f, 30f - distance);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        public static void NotifyTargetLost(Pawn pawn, Pawn lostTarget)
        {
            if (targetMemory.TryGetValue(pawn.thingIDNumber, out var memory) && memory.target == lostTarget)
            {
                // Keep the last known position for hunting
                targetMemory[pawn.thingIDNumber] = (null, lostTarget.Position, Find.TickManager.TicksGame);
            }
        }

        public static void ClearMemory(Pawn pawn)
        {
            targetMemory.Remove(pawn.thingIDNumber);
        }
    }
}