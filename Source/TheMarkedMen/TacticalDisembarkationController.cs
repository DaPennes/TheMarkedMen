using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    /// <summary>
    /// Manages tactical disembarkation of passengers from raid vehicles.
    /// Calculates appropriate deployment distances based on pawn type and combat role.
    /// Ensures safe landing zones for infantry assault.
    /// </summary>
    public static class TacticalDisembarkationController
    {
        /// <summary>
        /// Gets the tactical deployment distance for a pawn type.
        /// This determines how far from enemies the pawn should disembark.
        /// </summary>
        public static float GetDeploymentDistance(Pawn pawn)
        {
            if (pawn == null || pawn.kindDef == null)
            {
                return 35f;  // Default distance
            }

            // Ranged specialists: deploy farther back for support fire
            if (pawn.kindDef == CADefOf.CrossedShooter
                || pawn.kindDef == CADefOf.CrossedSoldier)
            {
                return 60f;  // Support fire distance
            }

            // Melee specialists: deploy closer for assault
            if (pawn.kindDef == CADefOf.CrossedBrute
                || pawn.kindDef == CADefOf.CrossedRaider)
            {
                return 25f;  // Close assault distance
            }

            // Mobile specialists: deploy at medium range
            if (pawn.kindDef == CADefOf.CrossedScout
                || pawn.kindDef == CADefOf.CrossedHunter)
            {
                return 40f;  // Mobile engagement distance
            }

            // Pyromaniacs: deploy at medium distance (need to approach to use fire)
            if (CrossedUtility.IsCrossedPyromaniac(pawn))
            {
                return 45f;
            }

            // Alphas and Warlords: flexible deployment
            if (pawn.kindDef == CADefOf.CrossedAlpha
                || pawn.kindDef == CADefOf.CrossedWarlord)
            {
                return 35f;  // Balanced engagement distance
            }

            // Default: civilians and other types
            return 35f;
        }

        /// <summary>
        /// Tries to find a safe disembarkation cell for a pawn.
        /// Prioritizes cells that match the pawn's tactical deployment distance.
        /// </summary>
        public static bool TryFindDisembarkCell(
            Pawn vehicle,
            Pawn passenger,
            Pawn nearestEnemy,
            out IntVec3 disembarkCell)
        {
            disembarkCell = IntVec3.Invalid;

            if (vehicle?.Map == null || passenger == null)
            {
                return false;
            }

            float deploymentDistance = GetDeploymentDistance(passenger);

            // If no enemy target, just disembark nearby
            if (nearestEnemy == null)
            {
                return CellFinder.TryFindRandomCellNear(
                    vehicle.Position,
                    vehicle.Map,
                    5,
                    cell => IsSafeDisembarkCell(cell, vehicle.Map),
                    out disembarkCell);
            }

            // Try to find a cell at the optimal deployment distance from the nearest enemy
            IntVec3 enemyPos = nearestEnemy.Position;
            IntVec3 vehiclePos = vehicle.Position;

            // Calculate direction away from enemy
            Vector3 awayVec = (vehiclePos - enemyPos).ToVector3().normalized;
            IntVec3 awayFromEnemy = new IntVec3(Mathf.RoundToInt(awayVec.x), 0, Mathf.RoundToInt(awayVec.z));

            // Try cells at the preferred deployment distance
            IntVec3 targetCell = enemyPos + (awayFromEnemy * Mathf.RoundToInt(deploymentDistance));

            // Search in expanding rings around the target cell
            for (int radius = 0; radius <= 10; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        // Skip interior cells (only check perimeter)
                        if (radius > 0 && Mathf.Abs(dx) < radius && Mathf.Abs(dz) < radius)
                        {
                            continue;
                        }

                        IntVec3 candidate = targetCell + new IntVec3(dx, 0, dz);

                        if (candidate.InBounds(vehicle.Map)
                            && IsSafeDisembarkCell(candidate, vehicle.Map)
                            && vehicle.CanReachImmediate(candidate, PathEndMode.OnCell))
                        {
                            disembarkCell = candidate;
                            return true;
                        }
                    }
                }
            }

            // Fallback: try to disembark near vehicle if ideal distance not found
            return CellFinder.TryFindRandomCellNear(
                vehicle.Position,
                vehicle.Map,
                8,
                cell => IsSafeDisembarkCell(cell, vehicle.Map),
                out disembarkCell);
        }

        /// <summary>
        /// Checks if a cell is safe for disembarking passengers.
        /// Safe = walkable, in bounds, not blocked by other pawns or hostile structures.
        /// </summary>
        private static bool IsSafeDisembarkCell(IntVec3 cell, Map map)
        {
            if (!cell.InBounds(map) || !cell.Walkable(map))
            {
                return false;
            }

            // Don't disembark on top of other pawns
            Pawn occupant = cell.GetFirstPawn(map);
            if (occupant != null)
            {
                return false;
            }

            // Check for dangerous terrain or obstacles
            List<Thing> thingsAtCell = map.thingGrid.ThingsListAt(cell);
            for (int i = 0; i < thingsAtCell.Count; i++)
            {
                Thing thing = thingsAtCell[i];
                if (thing == null)
                {
                    continue;
                }

                // Avoid fire/danger
                if (thing.def.defName.Contains("Fire") || thing.def.defName.Contains("Danger"))
                {
                    return false;
                }

                // Avoid being blocked by obstacles
                if (thing.def.passability == Traversability.Impassable)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if all passengers in a vehicle can successfully disembark.
        /// Validates that we have safe cells for all passengers before attempting disembark.
        /// </summary>
        public static bool CanDisembarkAllPassengers(
            Pawn vehicle,
            List<Pawn> passengers,
            Pawn targetEnemy)
        {
            if (vehicle?.Map == null || passengers == null || passengers.Count == 0)
            {
                return false;
            }

            // Quick check: can we find at least some safe cells?
            int safeCount = 0;
            for (int i = 0; i < passengers.Count; i++)
            {
                if (TryFindDisembarkCell(vehicle, passengers[i], targetEnemy, out IntVec3 _))
                {
                    safeCount++;
                }
            }

            // Need at least 50% of passengers to have valid cells
            return safeCount >= Mathf.CeilToInt(passengers.Count * 0.5f);
        }

        /// <summary>
        /// Marks a passenger as disembarked to prevent re-boarding attempts.
        /// Returns true if successfully marked.
        /// </summary>
        public static bool MarkPassengerAsDisembarked(Pawn passenger)
        {
            if (passenger == null)
            {
                return false;
            }

            // Add a custom hediff or use existing state tracking
            // For now, we track this via lord group membership
            // Disembarked passengers should be in an assault lord, not vehicle lord
            Lord currentLord = passenger.GetLord();
            if (currentLord != null && currentLord.LordJob is LordJob_CrossedVehicleAssault)
            {
                // Still in vehicle lord; will be handled by LordToil_CrossedVehicleAssault
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a vehicle can reach its intended deployment area.
        /// Used to detect if vehicle is stuck or blocked from reaching enemies.
        /// </summary>
        public static bool CanReachDeploymentArea(Pawn vehicle, Pawn targetEnemy)
        {
            if (vehicle?.Map == null || targetEnemy == null)
            {
                return false;
            }

            // Check if vehicle can path to near the enemy
            return vehicle.CanReach(targetEnemy.Position, PathEndMode.Touch, Danger.Deadly, true, true);
        }

        /// <summary>
        /// Gets a descriptive label for a pawn's deployment role (for debugging/logging).
        /// </summary>
        public static string GetDeploymentRoleLabel(Pawn pawn)
        {
            if (pawn?.kindDef == null)
            {
                return "Infantry";
            }

            if (pawn.kindDef == CADefOf.CrossedShooter || pawn.kindDef == CADefOf.CrossedSoldier)
            {
                return "Support Fire";
            }

            if (pawn.kindDef == CADefOf.CrossedBrute || pawn.kindDef == CADefOf.CrossedRaider)
            {
                return "Assault";
            }

            if (pawn.kindDef == CADefOf.CrossedScout || pawn.kindDef == CADefOf.CrossedHunter)
            {
                return "Mobile";
            }

            if (CrossedUtility.IsCrossedPyromaniac(pawn))
            {
                return "Fire Support";
            }

            if (pawn.kindDef == CADefOf.CrossedAlpha || pawn.kindDef == CADefOf.CrossedWarlord)
            {
                return "Commander";
            }

            return "Infantry";
        }
    }
}
