using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// Manages vehicle combat behavior, targeting, and weapon engagement.
    /// Caches target information to avoid excessive scanning.
    /// </summary>
    public static class VehicleCombatController
    {
        /// <summary>
        /// Cached combat state per vehicle (indexed by thing ID).
        /// </summary>
        private static readonly Dictionary<int, VehicleCombatState> combatStates = new Dictionary<int, VehicleCombatState>();

        /// <summary>
        /// Minimum ticks between duty reassignments to prevent thrashing.
        /// </summary>
        private const int DutyReassignCooldownTicks = 60;

        /// <summary>
        /// How often to scan for new targets (in ticks).
        /// </summary>
        private const int TargetScanIntervalTicks = 60;

        /// <summary>
        /// Maximum distance to consider engaging targets.
        /// </summary>
        private const float MaxCombatEngagementDistance = 150f;

        /// <summary>
        /// Combat state data for a single vehicle.
        /// </summary>
        private class VehicleCombatState
        {
            public Pawn vehicle;
            public Pawn lastTarget;
            public int lastTargetUpdateTick;
            public int lastDutyReassignTick;
            public bool isEngaging;

            public VehicleCombatState(Pawn vehicle)
            {
                this.vehicle = vehicle;
                this.lastTarget = null;
                this.lastTargetUpdateTick = 0;
                this.lastDutyReassignTick = 0;
                this.isEngaging = false;
            }

            public bool IsTargetValid()
            {
                return lastTarget != null
                    && !lastTarget.Destroyed
                    && !lastTarget.Dead
                    && lastTarget.Spawned
                    && lastTarget.Map == vehicle.Map
                    && vehicle.HostileTo(lastTarget);
            }

            public bool ShouldUpdateTarget(int currentTick)
            {
                return currentTick - lastTargetUpdateTick >= TargetScanIntervalTicks;
            }

            public bool CanReassignDuty(int currentTick)
            {
                return currentTick - lastDutyReassignTick >= DutyReassignCooldownTicks;
            }
        }

        /// <summary>
        /// Gets or creates combat state for a vehicle.
        /// </summary>
        private static VehicleCombatState GetCombatState(Pawn vehicle)
        {
            if (vehicle == null)
            {
                return null;
            }

            int vehicleId = vehicle.thingIDNumber;
            if (!combatStates.TryGetValue(vehicleId, out VehicleCombatState state))
            {
                state = new VehicleCombatState(vehicle);
                combatStates[vehicleId] = state;
            }

            return state;
        }

        /// <summary>
        /// Cleans up combat state when a vehicle is destroyed.
        /// </summary>
        public static void NotifyVehicleDestroyed(Pawn vehicle)
        {
            if (vehicle != null)
            {
                combatStates.Remove(vehicle.thingIDNumber);
            }
        }

        /// <summary>
        /// Main entry point: evaluates and applies combat behavior to a raid vehicle.
        /// </summary>
        public static void UpdateVehicleCombatBehavior(Pawn vehicle)
        {
            if (vehicle == null || !vehicle.Spawned || vehicle.Dead || vehicle.Map == null)
            {
                return;
            }

            VehicleCombatState state = GetCombatState(vehicle);
            if (state == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;

            // Update target if needed
            if (state.ShouldUpdateTarget(currentTick))
            {
                Pawn bestTarget = FindBestCombatTarget(vehicle);
                state.lastTarget = bestTarget;
                state.lastTargetUpdateTick = currentTick;
            }

            // Ensure target is still valid
            if (!state.IsTargetValid())
            {
                state.lastTarget = null;
            }

            // Update combat behavior based on target
            if (state.lastTarget != null)
            {
                ApplyCombatBehavior(vehicle, state.lastTarget, state, currentTick);
            }
            else
            {
                ApplyAssaultBehavior(vehicle, state, currentTick);
            }
        }

        /// <summary>
        /// Finds the highest-priority target for a vehicle to engage.
        /// Prioritizes: Defensive structures > Armed enemies > Unarmed enemies
        /// Then by distance and threat assessment.
        /// </summary>
        private static Pawn FindBestCombatTarget(Pawn vehicle)
        {
            if (vehicle?.Map?.mapPawns == null)
            {
                return null;
            }

            IReadOnlyList<Pawn> allPawns = vehicle.Map.mapPawns.AllPawnsSpawned;
            Pawn bestTarget = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn == null
                    || pawn.Dead
                    || pawn.Downed
                    || !pawn.HostileTo(vehicle)
                    || pawn.Position.DistanceToSquared(vehicle.Position) > MaxCombatEngagementDistance * MaxCombatEngagementDistance)
                {
                    continue;
                }

                float score = CalculateTargetScore(pawn, vehicle);
                if (score > bestScore)
                {
                    bestTarget = pawn;
                    bestScore = score;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Calculates a score for prioritizing a target.
        /// Higher scores = higher priority.
        /// </summary>
        private static float CalculateTargetScore(Pawn target, Pawn vehicle)
        {
            float score = 0f;

            // Prioritize dangerous structures (turrets, defensive positions)
            if (IsThreatBuilding(target))
            {
                score += 1000f;
            }

            // Prioritize armed enemies
            if (target.equipment?.Primary != null)
            {
                score += 500f;
            }

            // Closer targets are higher priority
            float distance = target.Position.DistanceTo(vehicle.Position);
            score += Mathf.Max(0f, 200f - distance);

            // Prioritize healthy enemies (less wasted firepower)
            if (target.health != null)
            {
                float healthPercent = target.health.summaryHealth.SummaryHealthPercent;
                score += healthPercent * 100f;
            }

            return score;
        }

        /// <summary>
        /// Checks if a pawn represents a defensive threat (heavily armed, in turret, etc).
        /// </summary>
        private static bool IsThreatBuilding(Pawn target)
        {
            // Non-pawn threats are handled elsewhere
            // This just marks heavily armed or positioned enemies as higher threat
            if (target.equipment?.Primary != null
                && target.equipment.Primary.def.Verbs != null
                && target.equipment.Primary.def.Verbs.Count > 0)
            {
                VerbProperties verbProps = target.equipment.Primary.def.Verbs[0];
                if (verbProps.range > 10f)  // Ranged weapon
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Applies combat behavior: maintain distance, prepare for engagement.
        /// </summary>
        private static void ApplyCombatBehavior(Pawn vehicle, Pawn target, VehicleCombatState state, int currentTick)
        {
            if (target == null || !state.CanReassignDuty(currentTick))
            {
                return;
            }

            // Check for duty conflicts before reassigning
            PawnDuty newDuty = null;
            float preferredRange = GetPreferredCombatRange(vehicle);
            float actualDistance = vehicle.Position.DistanceTo(target.Position);

            // If too close to target, move to maintain distance
            if (preferredRange > 0f && actualDistance < preferredRange * 0.7f)
            {
                IntVec3 standoffCell = FindStandoffPosition(vehicle, target, preferredRange);
                if (standoffCell.IsValid)
                {
                    newDuty = new PawnDuty(DutyDefOf.AssaultColony, standoffCell, preferredRange * 0.5f);
                }
            }

            // Otherwise, maintain assault stance on target
            if (newDuty == null)
            {
                newDuty = new PawnDuty(DutyDefOf.AssaultColony);
            }

            // Validate assignment before applying
            if (AIConflictPrevention.CanAssignDuty(vehicle, newDuty))
            {
                vehicle.mindState.duty = newDuty;
                state.lastDutyReassignTick = currentTick;
                state.isEngaging = true;
            }
        }

        /// <summary>
        /// Applies general assault behavior when no specific target is engaged.
        /// </summary>
        private static void ApplyAssaultBehavior(Pawn vehicle, VehicleCombatState state, int currentTick)
        {
            if (!state.CanReassignDuty(currentTick))
            {
                return;
            }

            PawnDuty newDuty = new PawnDuty(DutyDefOf.AssaultColony);

            // Validate assignment before applying
            if (AIConflictPrevention.CanAssignDuty(vehicle, newDuty))
            {
                vehicle.mindState.duty = newDuty;
                state.lastDutyReassignTick = currentTick;
                state.isEngaging = false;
            }
        }

        /// <summary>
        /// Gets the preferred combat engagement range for a vehicle based on its weapons.
        /// Returns 0 if unable to determine or vehicle has no ranged weapons.
        /// </summary>
        private static float GetPreferredCombatRange(Pawn vehicle)
        {
            // Default to a reasonable distance for general assault
            // Vehicle Framework's weapons would be checked here if available
            return 35f;
        }

        /// <summary>
        /// Finds a position where the vehicle can maintain combat distance from target.
        /// </summary>
        private static IntVec3 FindStandoffPosition(Pawn vehicle, Pawn target, float preferredRange)
        {
            if (vehicle?.Map == null || target == null)
            {
                return IntVec3.Invalid;
            }

            // Try to find a cell at the preferred distance
            IntVec3 vehiclePos = vehicle.Position;
            IntVec3 targetPos = target.Position;

            int attempts = 0;
            int maxAttempts = 8;

            while (attempts < maxAttempts)
            {
                // Calculate direction away from target
                Vector3 directionVec = (vehiclePos - targetPos).ToVector3().normalized;
                IntVec3 direction = new IntVec3(Mathf.RoundToInt(directionVec.x), 0, Mathf.RoundToInt(directionVec.z));
                IntVec3 candidateCell = targetPos + (direction * Mathf.RoundToInt(preferredRange * 0.9f));

                // Check if this cell is valid
                if (candidateCell.InBounds(vehicle.Map)
                    && candidateCell.Walkable(vehicle.Map)
                    && vehicle.CanReachImmediate(candidateCell, PathEndMode.OnCell))
                {
                    return candidateCell;
                }

                // Try a nearby cell if the ideal one doesn't work
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        IntVec3 nearby = candidateCell + new IntVec3(dx, 0, dz);
                        if (nearby.InBounds(vehicle.Map)
                            && nearby.Walkable(vehicle.Map)
                            && vehicle.CanReachImmediate(nearby, PathEndMode.OnCell))
                        {
                            return nearby;
                        }
                    }
                }

                attempts++;
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// Clears all cached combat states. Call this when cleaning up raids.
        /// </summary>
        public static void ClearAllCombatStates()
        {
            combatStates.Clear();
        }
    }
}
