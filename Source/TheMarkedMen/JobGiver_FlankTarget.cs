using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// JobGiver for infected packs to coordinate attacks on single targets.
    /// Multiple infected attacking same target from different angles gain melee accuracy bonus.
    /// </summary>
    public class JobGiver_FlankTarget : ThinkNode_JobGiver
    {
        private const int PackCoordinationCheckIntervalTicks = 250;
        private const float PackDetectionRadius = 20f;
        private const int MinPackSizeForBonus = 2;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return null;

            if (!pawn.IsHashIntervalTick(PackCoordinationCheckIntervalTicks))
                return null;

            // Check if in a pack situation (2+ infected nearby)
            List<Pawn> nearbyInfected = FindNearbyInfected(pawn, PackDetectionRadius);
            if (nearbyInfected.Count < MinPackSizeForBonus)
                return null;

            // If pack members are attacking same target, continue current job
            Pawn currentTarget = pawn.CurJob?.targetA.Pawn;
            if (currentTarget != null && !currentTarget.Dead && IsPackTargeting(pawn, currentTarget, nearbyInfected))
                return null; // Keep current job

            // Try to find a target that other pack members are attacking
            Pawn packTarget = FindPackTarget(pawn, nearbyInfected);
            if (packTarget != null)
            {
                return new Job(JobDefOf.AttackMelee, packTarget) { checkOverrideOnExpire = true };
            }

            return null;
        }

        private List<Pawn> FindNearbyInfected(Pawn pawn, float radius)
        {
            List<Pawn> nearby = new List<Pawn>();

            if (pawn.Map == null)
                return nearby;

            foreach (Pawn candidate in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (candidate == null || candidate.Dead || candidate == pawn)
                    continue;

                if (!CrossedUtility.IsInfectedPawn(candidate))
                    continue;

                if (pawn.Position.DistanceTo(candidate.Position) > radius)
                    continue;

                nearby.Add(candidate);
            }

            return nearby;
        }

        private bool IsPackTargeting(Pawn pawn, Pawn target, List<Pawn> packMembers)
        {
            // Count how many pack members are attacking this target
            int attackCount = 0;
            foreach (Pawn member in packMembers)
            {
                if (member.CurJob != null && member.CurJob.targetA.Pawn == target)
                    attackCount++;
            }

            return attackCount >= MinPackSizeForBonus;
        }

        private Pawn FindPackTarget(Pawn pawn, List<Pawn> packMembers)
        {
            // Find the target most pack members are currently attacking
            Dictionary<Pawn, int> targetVotes = new Dictionary<Pawn, int>();

            foreach (Pawn member in packMembers)
            {
                Pawn memberTarget = member.CurJob?.targetA.Pawn;
                if (memberTarget != null && !memberTarget.Dead && !CrossedUtility.IsInfectedPawn(memberTarget))
                {
                    if (!targetVotes.ContainsKey(memberTarget))
                        targetVotes[memberTarget] = 0;
                    targetVotes[memberTarget]++;
                }
            }

            // Return target with most votes (if 2+ members voting for same target)
            var topTarget = targetVotes
                .Where(kvp => kvp.Value >= MinPackSizeForBonus)
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault();

            return topTarget.Key;
        }

        /// <summary>
        /// Calculate flank bonus for a pawn attacking a target from a pack.
        /// Bonus scales with number of pack members attacking from different angles.
        /// </summary>
        public static float GetFlankBonusForTarget(Pawn pawn, Pawn target)
        {
            if (pawn == null || target == null || pawn.Map == null)
                return 0f;

            // Count nearby infected also attacking this target
            List<Pawn> packMembers = new List<Pawn>();
            foreach (Pawn candidate in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (candidate == null || candidate.Dead || candidate == pawn)
                    continue;

                if (!CrossedUtility.IsInfectedPawn(candidate))
                    continue;

                if (pawn.Position.DistanceTo(candidate.Position) > PackDetectionRadius)
                    continue;

                Pawn candidateTarget = candidate.CurJob?.targetA.Pawn;
                if (candidateTarget == target)
                    packMembers.Add(candidate);
            }

            // Calculate flank angle bonus
            float angleBonus = 0f;
            for (int i = 0; i < packMembers.Count; i++)
            {
                for (int j = i + 1; j < packMembers.Count; j++)
                {
                    Pawn member1 = packMembers[i];
                    Pawn member2 = packMembers[j];

                    // Calculate angle between members relative to target
                    Vector3 vec1 = (member1.Position - target.Position).ToVector3();
                    Vector3 vec2 = (member2.Position - target.Position).ToVector3();

                    float angle = Vector3.Angle(vec1, vec2);

                    // Bonus for attacking from different angles (90+ degrees = +5% per flanker)
                    if (angle > 90f)
                        angleBonus += 5f;
                }
            }

            return Mathf.Min(angleBonus, 15f); // Cap at +15% melee accuracy
        }
    }
}
