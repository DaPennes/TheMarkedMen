using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    
    
    
    
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

            
            if (targetMemory.TryGetValue(pawnId, out var memory))
            {
                
                if (memory.target != null && !memory.target.Dead && GenSight.LineOfSight(pawn.Position, memory.target.Position, pawn.Map))
                {
                    
                    return new Job(JobDefOf.AttackMelee, memory.target) { checkOverrideOnExpire = true };
                }

                
                if (currentTick - memory.lastSeenTick < 500)
                {
                    return new Job(JobDefOf.Goto, memory.lastSeenPos) { checkOverrideOnExpire = true };
                }

                
                targetMemory.Remove(pawnId);
            }

            
            if (!pawn.IsHashIntervalTick(TargetCacheIntervalTicks))
                return null;

            Pawn bestTarget = FindBestTarget(pawn);
            if (bestTarget != null)
            {
                
                targetMemory[pawnId] = (bestTarget, bestTarget.Position, currentTick);
                return new Job(JobDefOf.AttackMelee, bestTarget) { checkOverrideOnExpire = true };
            }

            return null;
        }

        private Pawn FindBestTarget(Pawn pawn)
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

                if (!pawn.CanReach(candidate, PathEndMode.Touch, Danger.Deadly))
                    continue;

                
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
                
                targetMemory[pawn.thingIDNumber] = (null, lostTarget.Position, Find.TickManager.TicksGame);
            }
        }

        public static void ClearMemory(Pawn pawn)
        {
            targetMemory.Remove(pawn.thingIDNumber);
        }
    }
}