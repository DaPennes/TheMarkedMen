using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    
    
    
    
    public class JobGiver_RetreatIfOutgunned : ThinkNode_JobGiver
    {
        private const int RetreatCheckIntervalTicks = 250;
        private const float OutgunnedHealthThreshold = 0.4f; 
        private const float OutgunnedEnemyRatio = 2.0f; 
        private const float RetreatSearchRadius = 30f;
        private const float MinRetreatDistance = 10f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return null;

            
            if (!pawn.IsHashIntervalTick(RetreatCheckIntervalTicks))
                return null;

            
            if (!ShouldRetreat(pawn))
                return null;

            
            IntVec3 retreatPos = FindRetreatPosition(pawn);
            if (!retreatPos.IsValid)
                return null;

            return new Job(JobDefOf.Goto, retreatPos) { checkOverrideOnExpire = true };
        }

        private bool ShouldRetreat(Pawn pawn)
        {
            
            float healthPct = pawn.health?.summaryHealth?.SummaryHealthPercent ?? 1f;
            if (healthPct > OutgunnedHealthThreshold)
                return false;

            
            int enemyCount = CountNearbyEnemies(pawn);
            int allyCount = CountNearbyAllies(pawn);

            if (allyCount == 0)
                return false; 

            float ratio = (float)enemyCount / Mathf.Max(1f, allyCount);
            return ratio >= OutgunnedEnemyRatio;
        }

        private int CountNearbyEnemies(Pawn pawn)
        {
            int count = 0;
            float searchRadius = 20f;

            foreach (Pawn candidate in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (candidate == pawn || candidate.Dead)
                    continue;

                if (CrossedUtility.IsInfectedPawn(candidate))
                    continue;

                if (!pawn.CanReach(candidate, PathEndMode.Touch, Danger.Deadly))
                    continue;

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance <= searchRadius)
                    count++;
            }

            return count;
        }

        private int CountNearbyAllies(Pawn pawn)
        {
            int count = 0;
            float searchRadius = 15f;

            foreach (Pawn candidate in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (candidate == pawn || candidate.Dead)
                    continue;

                if (!CrossedUtility.IsInfectedPawn(candidate))
                    continue;

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance <= searchRadius)
                    count++;
            }

            return count;
        }

        private IntVec3 FindRetreatPosition(Pawn pawn)
        {
            
            Vector3 threatVector = CalculateThreatVector(pawn);
            if (threatVector == Vector3.zero)
                return IntVec3.Invalid;

            
            Vector3 retreatDir = -threatVector.normalized;

            
            for (int i = 0; i < 8; i++)
            {
                float angle = Mathf.Atan2(retreatDir.z, retreatDir.x) * Mathf.Rad2Deg;
                angle += i * 45f; 
                if (angle >= 360f) angle -= 360f;

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    0,
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                ) * MinRetreatDistance;

                IntVec3 candidatePos = (pawn.Position.ToVector3Shifted() + offset).ToIntVec3();

                if (IsValidRetreatPosition(pawn, candidatePos))
                    return candidatePos;
            }

            
            return CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 10);
        }

        private Vector3 CalculateThreatVector(Pawn pawn)
        {
            Vector3 threat = Vector3.zero;
            float threatRadius = 25f;

            foreach (Pawn candidate in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (candidate == pawn || candidate.Dead)
                    continue;

                if (!CrossedUtility.IsInfectedPawn(candidate))
                {
                    
                    if (pawn.CanSee(candidate) || candidate.CanSee(pawn))
                    {
                        Vector3 toThreat = (candidate.Position.ToVector3Shifted() - pawn.Position.ToVector3Shifted());
                        float distance = toThreat.magnitude;
                        if (distance > 0f && distance <= threatRadius)
                        {
                            
                            float weight = 1f - (distance / threatRadius);
                            threat += toThreat.normalized * weight;
                        }
                    }
                }
            }

            return threat;
        }

        private bool IsValidRetreatPosition(Pawn pawn, IntVec3 candidate)
        {
            if (!candidate.InBounds(pawn.Map))
                return false;

            if (!pawn.Map.reachability.CanReach(pawn.Position, candidate, PathEndMode.OnCell, TraverseMode.ByPawn))
                return false;

            
            Thing cover = candidate.GetCover(pawn.Map);
            if (cover != null && cover.Position.InBounds(pawn.Map))
                return true;

            
            if (pawn.Map.roofGrid.Roofed(candidate))
                return true;

            
            float minEnemyDist = 999f;
            foreach (Pawn enemy in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (enemy == pawn || enemy.Dead)
                    continue;

                if (CrossedUtility.IsInfectedPawn(enemy))
                    continue;

                float dist = enemy.Position.DistanceTo(candidate);
                if (dist < minEnemyDist)
                    minEnemyDist = dist;
            }

            return minEnemyDist > 3f; 
        }
    }
}