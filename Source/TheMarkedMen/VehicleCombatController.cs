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
        /// Vehicle type categories for behavior differentiation.
        /// </summary>
        private enum VehicleTypeCategory
        {
            Tank,
            APC,
            Technical,
            Motorcycle,
            ArmoredCar,
            HalfTrack,
            Airplane,
            Helicopter,
            Unknown
        }

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

            // Immediately clear target if vehicle's current target is no longer valid
            // (destroyed, downed, map change, or no longer hostile). This prevents
            // vehicles from stubbornly continuing to engage invalid targets.
            if (state?.lastTarget != null
                && (!state.lastTarget.Spawned
                    || state.lastTarget.Dead
                    || state.lastTarget.Downed
                    || state.lastTarget.Map != vehicle.Map
                    || !vehicle.HostileTo(state.lastTarget)))
            {
                Log.Message("[The Marked Men] Clearing invalid combat target for vehicle " + vehicle.def.defName);
                state.lastTarget = null;
                state.lastTargetUpdateTick = 0;
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
            if (state.lastTarget != null && !state.IsTargetValid())
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
        /// Checks if a pawn represents a defensive threat (heavily armed, in turret, etc).
        /// </summary>
        private static bool IsThreatBuilding(Pawn target)
        {
            // Non-pawn threats are handled elsewhere
            // This marks enemies with equipment as higher threat
            if (target.equipment?.Primary != null)
            {
                // Has any primary weapon equipped
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates a score for prioritizing a target.
        /// Higher scores = higher priority.
        /// Score weights are modified by vehicle type for differentiated behavior.
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

            // Closer targets are higher priority (weight scaled by vehicle type)
            float distance = target.Position.DistanceTo(vehicle.Position);
            VehicleTypeCategory vtype = GetVehicleTypeCategory(vehicle);
            float distanceWeight = 200f;

            // Tanks/APCs value distance more (they have long-range turrets)
            // Motorcycles/Technicals value closeness more (flanking behavior)
            switch (vtype)
            {
                case VehicleTypeCategory.Tank:
                case VehicleTypeCategory.APC:
                    distanceWeight = 300f; // Value range more
                    break;

                case VehicleTypeCategory.Motorcycle:
                case VehicleTypeCategory.Technical:
                    distanceWeight = 150f; // Value closeness more
                    break;

                default:
                    distanceWeight = 200f;
                    break;
            }

            score += Mathf.Max(0f, distanceWeight - distance);

            // Prioritize healthy enemies (less wasted firepower)
            if (target.health != null)
            {
                float healthPercent = target.health.summaryHealth.SummaryHealthPercent;
                score += healthPercent * 100f;
            }

            // Vehicle-type-specific target priorities (using only verifiable API)
            score += ApplyVehicleTargetPriority(target, vehicle);

            return score;
        }

        /// <summary>
        /// Applies vehicle-type-specific target priority modifications.
        /// Uses only RimWorld API properties that are guaranteed to exist.
        /// </summary>
        private static float ApplyVehicleTargetPriority(Pawn target, Pawn vehicle)
        {
            float score = 0f;
            VehicleTypeCategory vtype = GetVehicleTypeCategory(vehicle);

            // Tanks prioritize colonists (they hunt aggressively)
            if (vtype == VehicleTypeCategory.Tank)
            {
                if (target.Faction == Faction.OfPlayer || target.IsColonist || target.IsPrisonerOfColony)
                {
                    score += 200f;
                }
            }

            // APCs prioritize colonists
            if (vtype == VehicleTypeCategory.APC)
            {
                if (target.IsColonist || target.IsPrisonerOfColony || target.IsFreeColonist)
                {
                    score += 150f;
                }
            }

            // Technicals prioritize unarmed targets
            if (vtype == VehicleTypeCategory.Technical)
            {
                if (target.equipment?.Primary == null)
                {
                    score += 100f; // Technicals gun down unarmed targets
                }
            }

            // Motorcycles prioritize colonists and close targets
            if (vtype == VehicleTypeCategory.Motorcycle)
            {
                if (target.IsColonist || target.IsPrisonerOfColony || target.IsFreeColonist)
                {
                    score += 125f;
                }
                // Close targets for rapid engagement
                float distance = target.Position.DistanceTo(vehicle.Position);
                if (distance < 30f)
                {
                    score += 75f;
                }
            }

            // Armored cars have mild colonist preference
            if (vtype == VehicleTypeCategory.ArmoredCar)
            {
                if (target.Faction == Faction.OfPlayer || target.IsColonist)
                {
                    score += 100f;
                }
            }

            // Half-tracks prioritize enemy infantry
            if (vtype == VehicleTypeCategory.HalfTrack)
            {
                if (target.RaceProps.Humanlike && !target.IsColonist)
                {
                    score += 100f;
                }
            }

            // Airplanes value healthy targets
            if (vtype == VehicleTypeCategory.Airplane)
            {
                if (target.health != null)
                {
                    float healthPercent = target.health.summaryHealth.SummaryHealthPercent;
                    score += healthPercent * 50f;
                }
            }

            // Helicopters have mild colonist preference
            if (vtype == VehicleTypeCategory.Helicopter)
            {
                if (target.IsColonist || target.IsPrisonerOfColony || target.IsFreeColonist)
                {
                    score += 80f;
                }
            }

            return score;
        }

        /// <summary>
        /// Applies combat behavior: maintain distance, prepare for engagement.
        /// </summary>
        private static void ApplyCombatBehavior(Pawn vehicle, Pawn target, VehicleCombatState state, int currentTick)
        {
            // Immediately invalidate target if it's no longer viable
            if (target != null
                && (!target.Spawned
                    || target.Dead
                    || target.Downed
                    || target.Map != vehicle.Map
                    || !vehicle.HostileTo(target)))
            {
                Log.Message("[The Marked Men] Invalid combat target cleared during behavior application for vehicle " + vehicle.def.defName);
                state.lastTarget = null;
                state.lastTargetUpdateTick = 0;
                return;
            }

            if (target == null || !state.CanReassignDuty(currentTick))
            {
                return;
            }

            // Check for duty conflicts before reassigning
            PawnDuty newDuty = null;
            float preferredRange = GetPreferredCombatRange(vehicle);
            float actualDistance = vehicle.Position.DistanceTo(target.Position);

            // If too close to target, move to maintain distance based on vehicle type
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
        /// Gets the preferred combat engagement range for a vehicle based on its type.
        /// Different vehicle types have different optimal engagement distances.
        /// </summary>
        private static float GetPreferredCombatRange(Pawn vehicle)
        {
            VehicleTypeCategory type = GetVehicleTypeCategory(vehicle);
            float baseRange = 35f; // Default general assault range

            switch (type)
            {
                case VehicleTypeCategory.Tank:
                case VehicleTypeCategory.APC:
                    // Tanks and APCs maintain standoff distance for turret engagement
                    return 60f;

                case VehicleTypeCategory.Technical:
                case VehicleTypeCategory.Motorcycle:
                    // Technicals and motorcycles engage at closer ranges, flank aggressively
                    return 20f;

                case VehicleTypeCategory.ArmoredCar:
                    // Armored cars maintain moderate distance, can ram
                    return 40f;

                case VehicleTypeCategory.HalfTrack:
                    // Half-tracks transport troops, support from moderate distance
                    return 35f;

                case VehicleTypeCategory.Airplane:
                    // Aircraft engage from distance, guns have long range
                    return 100f;

                case VehicleTypeCategory.Helicopter:
                    // Helicopters can engage at various ranges, default to 50f
                    return 50f;

                default:
                    // Unknown or vanilla vehicles use default
                    return baseRange;
            }
        }

        /// <summary>
        /// Determines the vehicle type category based on its definition.
        /// </summary>
        private static VehicleTypeCategory GetVehicleTypeCategory(Pawn vehicle)
        {
            if (vehicle == null || vehicle.def == null)
            {
                return VehicleTypeCategory.Unknown;
            }

            string defName = vehicle.def.defName;

            // Check for known vehicle type keywords in def name
            defName = defName.ToLower();

            // Tank-like vehicles
            if (defName.Contains("tank")
                || defName.Contains("heavy")
                || defName.Contains("main battle"))
            {
                return VehicleTypeCategory.Tank;
            }

            // APC vehicles
            if (defName.Contains("apc")
                || defName.Contains("personal carriage"))
            {
                return VehicleTypeCategory.APC;
            }

            // Technical vehicles (technicals, pickups with weapons)
            if (defName.Contains("technical")
                || defName.Contains("pickup")
                || defName.Contains("hilux"))
            {
                return VehicleTypeCategory.Technical;
            }

            // Motorcycle-like vehicles
            if (defName.Contains("motorcycle")
                || defName.Contains("bike")
                || defName.Contains("quad"))
            {
                return VehicleTypeCategory.Motorcycle;
            }

            // Armored car vehicles
            if (defName.Contains("armored")
                || defName.Contains("car")
                || defName.Contains("sedan"))
            {
                return VehicleTypeCategory.ArmoredCar;
            }

            // Half-track vehicles
            if (defName.Contains("half-track")
                || defName.Contains("halftrack")
                || defName.Contains("scout"))
            {
                return VehicleTypeCategory.HalfTrack;
            }

            // Aircraft
            if (defName.Contains("plane")
                || defName.Contains("aircraft")
                || defName.Contains("jet")
                || defName.Contains("skyfaller"))
            {
                return VehicleTypeCategory.Airplane;
            }

            // Helicopters
            if (defName.Contains("heli")
                || defName.Contains("helicopter")
                || defName.Contains("chopper"))
            {
                return VehicleTypeCategory.Helicopter;
            }

            return VehicleTypeCategory.Unknown;
        }

        /// <summary>
        /// Finds a position where the vehicle can maintain combat distance from target.
        /// Different vehicle types have different standoff preferences.
        /// </summary>
        private static IntVec3 FindStandoffPosition(Pawn vehicle, Pawn target, float preferredRange)
        {
            if (vehicle?.Map == null || target == null)
            {
                return IntVec3.Invalid;
            }

            VehicleTypeCategory type = GetVehicleTypeCategory(vehicle);
            float standoffMultiplier = 1f;

            // Adjust standoff distance based on vehicle type
            switch (type)
            {
                case VehicleTypeCategory.Tank:
                case VehicleTypeCategory.APC:
                    standoffMultiplier = 1.2f; // Tanks prefer slightly further back
                    break;

                case VehicleTypeCategory.Motorcycle:
                case VehicleTypeCategory.Technical:
                    standoffMultiplier = 0.8f; // Motorcycles/technicals prefer closer
                    break;

                case VehicleTypeCategory.Helicopter:
                    standoffMultiplier = 1.5f; // Helicopters can operate further out
                    break;

                default:
                    standoffMultiplier = 1f;
                    break;
            }

            // Try to find a cell at the preferred distance
            IntVec3 vehiclePos = vehicle.Position;
            IntVec3 targetPos = target.Position;

            int attempts = 0;
            int maxAttempts = 12; // More attempts for vehicle types

            while (attempts < maxAttempts)
            {
                // Calculate direction away from target
                Vector3 directionVec = (vehiclePos - targetPos).ToVector3().normalized;
                IntVec3 direction = new IntVec3(Mathf.RoundToInt(directionVec.x), 0, Mathf.RoundToInt(directionVec.z));
                IntVec3 candidateCell = targetPos + (direction * Mathf.RoundToInt(preferredRange * standoffMultiplier * 0.9f));

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