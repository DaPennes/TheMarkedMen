using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// JobGiver for infected to seek cover when under fire.
    /// Moves to nearby cover cells when threatened by ranged attacks.
    /// </summary>
    public class JobGiver_TakeCover : ThinkNode_JobGiver
    {
        private const int CoverCheckIntervalTicks = 120;
        private const float CoverSearchRadius = 25f;
        private const float ThreatDetectionRange = 35f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return null;

            // Only check periodically
            if (!pawn.IsHashIntervalTick(CoverCheckIntervalTicks))
                return null;

            // Check if under fire from ranged attackers
            if (!IsUnderRangedAttack(pawn))
                return null;

            // Find nearest cover position
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

                // Skip infected
                if (CrossedUtility.IsInfectedPawn(shooter))
                    continue;

                // Check if this pawn has a ranged weapon and is targeting us
                if (shooter.equipment?.Primary == null || !shooter.equipment.Primary.def.ranged)
                    continue;

                float distance = shooter.Position.DistanceTo(pawn.Position);
                if (distance > ThreatDetectionRange)
                    continue;

                // Check if line of sight
                if (shooter.CanSee(pawn) && !GenAdjective.PositiveSkillOrTraitForTool(shooter))
                {
                    return true;
                }
            }

            return false;
        }

        private IntVec3 FindCoverPosition(Pawn pawn)
        {
            // Use RimWorld's cover utility
            IntVec3 bestCover = IntVec3.Invalid;
            float bestScore = -1f;

            // Search nearby cells for cover
            for (int i = 0; i < 20; i++)
            {
                IntVec3 cell = pawn.Position + GenRadial.RadialPattern[i];
                if (!cell.InBounds(pawn.Map))
                    continue;

                if (!pawn.Map.reachability.CanReach(pawn.Position, cell, PathEndMode.OnCell, TraverseMode.ByPawn))
                    continue;

                // Check if this cell has cover
                Thing cover = cell.GetCover(pawn.Map);
                if (cover == null)
                    continue;

                float score = CoverUtility.CalculateCoverScore(cover, cell, pawn.Map);
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