using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Manages intelligent crew assignment for raid vehicles.
    /// Prioritizes crew roles (driver > gunner > passenger) and caches assignments
    /// to avoid repeated calculation during vehicle lifetime.
    /// </summary>
    public static class VehicleCrewManager
    {
        /// <summary>
        /// Crew role priorities for assignment. Higher values = higher priority.
        /// </summary>
        public enum CrewRolePriority
        {
            Driver = 100,
            Gunner = 50,
            Passenger = 10
        }

        /// <summary>
        /// Gets the priority score for assigning a pawn to a crew role.
        /// Higher scores indicate better fit for the role.
        /// </summary>
        public static int GetCrewRolePriority(Pawn pawn, string handlingTypeName)
        {
            if (pawn == null || string.IsNullOrEmpty(handlingTypeName))
            {
                return 0;
            }

            int baseScore = 0;

            // Determine base priority by role type
            if (handlingTypeName.Equals("Movement", StringComparison.OrdinalIgnoreCase))
            {
                baseScore = (int)CrewRolePriority.Driver;
                // Prefer pawns with better shooting/melee for driver (can engage if needed)
                baseScore += pawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
                baseScore += pawn.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            }
            else if (handlingTypeName.Equals("Turret", StringComparison.OrdinalIgnoreCase))
            {
                baseScore = (int)CrewRolePriority.Gunner;
                // Strongly prefer high shooting skill for gunners
                baseScore += (pawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0) * 3;
            }
            else
            {
                baseScore = (int)CrewRolePriority.Passenger;
            }

            // Apply combat readiness modifier
            if (pawn.health != null && pawn.health.capacities != null)
            {
                float combatReadiness = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
                baseScore += Mathf.RoundToInt(combatReadiness * 10f);
            }

            // Prefer infected with combat-focused roles
            if (pawn.kindDef == CADefOf.CrossedShooter
                || pawn.kindDef == CADefOf.CrossedSoldier
                || pawn.kindDef == CADefOf.CrossedRaider
                || pawn.kindDef == CADefOf.CrossedAlpha)
            {
                baseScore += 5;
            }

            return baseScore;
        }

        /// <summary>
        /// Checks if a pawn is suitable for vehicle crew assignment.
        /// Excludes non-humanlike and special unit types.
        /// </summary>
        public static bool IsValidCrewCandidate(Pawn pawn)
        {
            return pawn != null
                && !pawn.Destroyed
                && pawn.RaceProps.Humanlike
                && pawn.kindDef != CADefOf.MarkedMan  // MarkedMen should not crew vehicles
                && !pawn.Dead
                && !pawn.Downed;
        }

        /// <summary>
        /// Counts the number of available pawns that can crew vehicles in the list.
        /// Excludes vehicle pawns themselves.
        /// </summary>
        public static int CountAvailableCrewCandidates(List<Pawn> pawns)
        {
            if (pawns == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (IsValidCrewCandidate(pawn)
                    && !TheMarkedMenVehicleFrameworkCompatibility.IsVehiclePawn(pawn))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Finds the best pawn to fill a specific crew role from available candidates.
        /// Scoring prioritizes role fit, skill level, and combat readiness.
        /// </summary>
        public static Pawn FindBestCrewForRole(
            List<Pawn> candidates,
            string roleName,
            Func<Pawn, bool> additionalFilter = null)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            Pawn bestPawn = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn pawn = candidates[i];
                if (!IsValidCrewCandidate(pawn))
                {
                    continue;
                }

                if (additionalFilter != null && !additionalFilter(pawn))
                {
                    continue;
                }

                int score = GetCrewRolePriority(pawn, roleName);
                if (score > bestScore)
                {
                    bestPawn = pawn;
                    bestScore = score;
                }
            }

            return bestPawn;
        }

        /// <summary>
        /// Validates that a pawn in a crew role is still suitable.
        /// Used to detect when reassignment is necessary (after death/downed).
        /// </summary>
        public static bool IsCrewAssignmentValid(Pawn pawn)
        {
            return pawn != null
                && !pawn.Destroyed
                && !pawn.Dead
                && !pawn.Downed
                && pawn.RaceProps.Humanlike;
        }

        /// <summary>
        /// Gets a descriptive label for a crew role (used in logging).
        /// </summary>
        public static string GetRoleLabel(string handlingTypeName)
        {
            if (handlingTypeName.Equals("Movement", StringComparison.OrdinalIgnoreCase))
            {
                return "Driver";
            }

            if (handlingTypeName.Equals("Turret", StringComparison.OrdinalIgnoreCase))
            {
                return "Gunner";
            }

            return "Crew";
        }
    }
}
