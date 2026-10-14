using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// JobGiver for infected to coordinate pack attacks and flank targets.
    /// Moves to flanking positions when allies are engaging the same target.
    /// </summary>
    public class JobGiver_FlankTarget : ThinkNode_JobGiver
    {
        private const int FlankCheckIntervalTicks = 200;
        private const float FlankSearchRadius = 15f;
        private const float MinFlankAngle = 60f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return null;

            // Only check periodically
            if (!pawn.IsHashIntervalTick(FlankCheckIntervalTicks))
                return null;

            // Find current target
            Pawn currentTarget = GetCurrentTarget(pawn);
            if (currentTarget == null)
                return null;

            // Find nearby allied infected
            List<Pawn> allies = GetNearbyAlliedInfected(pawn, currentTarget);
            if (allies.Count < 2) // Need at least 2 allies for flanking to be useful
                return null;

            // Find flanking position
            IntVec3 flankPos = FindFlankPosition(pawn, currentTarget, allies);
            if (!flankPos.IsValid)
                return null;

            // Move to flank position
            return new Job(JobDefOf.Goto, flankPos) { checkOverrideOnExpire = true };
        }

        private Pawn GetCurrentTarget(Pawn pawn)
        {
            // Check current job target
            if (pawn.jobs?.curJob != null && pawn.jobs.curJob.def == JobDefOf.AttackMelee)
            {
                return pawn.jobs.curJob.targetA.Thing as Pawn;
            }

            // Find nearest valid target
            foreach (Pawn candidate in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (candidate == null || candidate.Dead || candidate == pawn)
                    continue;

                if (CrossedUtility.IsInfectedPawn(candidate))
                    continue;

                if (pawn.CanReach(candidate, PathEndMode.Touch, Danger.Deadly))
                {
                    return candidate;
                }
            }

            return null;
        }

        private List<Pawn> GetNearbyAlliedInfected(Pawn pawn, Pawn target)
        {
            List<Pawn> allies = new List<Pawn>();

            foreach (Pawn candidate in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (candidate == pawn || candidate.Dead)
                    continue;

                if (!CrossedUtility.IsInfectedPawn(candidate))
                    continue;

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance > FlankSearchRadius)
                    continue;

                // Check if this ally is targeting the same enemy
                if (IsTargetingSameEnemy(candidate, target))
                {
                    allies.Add(candidate);
                }
            }

            return allies;
        }

        private bool IsTargetingSameEnemy(Pawn ally, Pawn enemy)
        {
            if (ally.jobs?.curJob != null && ally.jobs.curJob.def == JobDefOf.AttackMelee)
            {
                return ally.jobs.curJob.targetA.Thing as Pawn == enemy;
            }
            return false;
        }

        private IntVec3 FindFlankPosition(Pawn pawn, Pawn target, List<Pawn> allies)
        {
            // Calculate angles from target to all allies
            List<float> occupiedAngles = new List<float>();
            foreach (Pawn ally in allies)
            {
                float angle = (ally.Position - target.Position).AngleFlat;
                occupiedAngles.Add(angle);
            }

            // Add our own current angle
            float ourAngle = (pawn.Position - target.Position).AngleFlat;
            occupiedAngles.Add(ourAngle);

            // Sort and find the largest gap
            occupiedAngles.Sort();
            float bestGapStart = 0f;
            float bestGapSize = 0f;

            for (int i = 0; i < occupiedAngles.Count; i++)
            {
                float currentAngle = occupiedAngles[i];
                float nextAngle = occupiedAngles[(i + 1) % occupiedAngles.Count];
                if (i == occupiedAngles.Count - 1)
                {
                    nextAngle += 360f;
                }

                float gapSize = nextAngle - currentAngle;
                if (gapSize > bestGapSize)
                {
                    bestGapSize = gapSize;
                    bestGapStart = currentAngle;
                }
            }

            // If we have a good gap, position ourselves in the middle of it
            if (bestGapSize > MinFlankAngle)
            {
                float flankAngle = bestGapStart + (bestGapSize / 2f);
                if (flankAngle >= 360f)
                {
                    flankAngle -= 360f;
                }

                // Calculate position at flank angle from target
                Vector3 targetVec = target.Position.ToVector3Shifted();
                Vector3 offset = new Vector3(
                    Mathf.Sin(flankAngle * Mathf.Deg2Rad),
                    0,
                    Mathf.Cos(flankAngle * Mathf.Deg2Rad)
                ) * 5f; // Flank at ~5 cells from target

                IntVec3 flankPos = (targetVec + offset).ToIntVec3;

                if (pawn.Map.reachability.CanReach(pawn.Position, flankPos, PathEndMode.OnCell, TraverseMode.ByPawn))
                {
                    return flankPos;
                }
            }

            return IntVec3.Invalid;
        }
    }
}