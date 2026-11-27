using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    
    
    
    
    public class JobGiver_TakeCover : ThinkNode_JobGiver
    {
        private const int CoverCheckIntervalTicks = 120;
        private const float CoverSearchRadius = 25f;
        private const float ThreatDetectionRange = 35f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return null;

            
            if (!pawn.IsHashIntervalTick(CoverCheckIntervalTicks))
                return null;

            
            if (!IsUnderRangedAttack(pawn))
                return null;

            
            IntVec3 coverPos = FindCoverPosition(pawn);
            if (!coverPos.IsValid)
                return null;

            return new Job(JobDefOf.Goto, coverPos) { checkOverrideOnExpire = true };
        }

        private bool IsUnderRangedAttack(Pawn pawn)
        {
            foreach (Pawn shooter in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (shooter == pawn || shooter.Dead || !shooter.Spawned)
                    continue;

                
                if (CrossedUtility.IsInfectedPawn(shooter))
                    continue;

                
                if (shooter.equipment?.Primary == null)
                    continue;

                float distance = shooter.Position.DistanceTo(pawn.Position);
                if (distance > ThreatDetectionRange)
                    continue;

                
                if (shooter.CanSee(pawn))
                {
                    return true;
                }
            }

            return false;
        }

        private IntVec3 FindCoverPosition(Pawn pawn)
        {
            
            IntVec3 bestCover = IntVec3.Invalid;
            float bestScore = -1f;

            
            for (int i = 0; i < 20; i++)
            {
                IntVec3 cell = pawn.Position + GenRadial.RadialPattern[i];
                if (!cell.InBounds(pawn.Map))
                    continue;

                if (!pawn.Map.reachability.CanReach(pawn.Position, cell, PathEndMode.OnCell, TraverseMode.ByPawn))
                    continue;

                
                Thing cover = cell.GetCover(pawn.Map);
                if (cover == null)
                    continue;

                
                float score = 0f;
                if (pawn.Map.roofGrid.Roofed(cell))
                    score += 10f;
                if (cover != null)
                    score += 5f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCover = cell;
                }
            }

            return bestCover;
        }
    }
}