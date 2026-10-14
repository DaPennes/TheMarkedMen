using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Test utilities for Vehicle Framework compatibility.
    /// Allows debugging and verification of vehicle detection without a full game run.
    /// </summary>
    public static class VehicleFrameworkDebugUtilities
    {
        /// <summary>
        /// Log current Vehicle Framework status to the debug log.
        /// </summary>
        public static void LogVehicleFrameworkStatus()
        {
            bool isActive = TheMarkedMenVehicleFrameworkCompatibility.IsVehicleFrameworkActive;
            Type vehicleType = TheMarkedMenVehicleFrameworkCompatibility.VehicleType;

            Log.Message($"[The Marked Men] Vehicle Framework Status:");
            Log.Message($"  - Active: {isActive}");
            Log.Message($"  - Vehicle Type: {vehicleType?.FullName ?? "Not loaded"}");

            if (isActive)
            {
                Log.Message("[The Marked Men] Vehicle Framework compatibility is fully operational.");
            }
            else
            {
                Log.Message("[The Marked Men] Vehicle Framework mod not detected or not compatible.");
            }
        }

        /// <summary>
        /// Check if a specific pawn is in a vehicle and log the result.
        /// </summary>
        public static void LogPawnVehicleStatus(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Warning("[The Marked Men] Cannot check vehicle status: pawn is null");
                return;
            }

            bool inVehicle = TheMarkedMenVehicleFrameworkCompatibility.IsInVehicle(pawn);
            Log.Message($"[The Marked Men] Pawn '{pawn.Name}' in vehicle: {inVehicle}");

            if (inVehicle)
            {
                dynamic vehicle = TheMarkedMenVehicleFrameworkCompatibility.GetVehicleForPawn(pawn);
                Thing vehicleThing = vehicle as Thing;
                Log.Message($"  - Vehicle: {vehicleThing?.def?.label ?? "Unknown"}");
                Log.Message($"  - Vehicle Position: {vehicleThing?.Position ?? IntVec3.Invalid}");
            }
        }

        /// <summary>
        /// List all vehicles on the current map.
        /// </summary>
        public static void ListMapVehicles(Map map)
        {
            if (map == null)
            {
                Log.Warning("[The Marked Men] Cannot list vehicles: map is null");
                return;
            }

            if (!TheMarkedMenVehicleFrameworkCompatibility.IsVehicleFrameworkActive)
            {
                Log.Message("[The Marked Men] Vehicle Framework not active - no vehicles to list");
                return;
            }

            Type vehicleType = TheMarkedMenVehicleFrameworkCompatibility.VehicleType;
            int vehicleCount = 0;

            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (vehicleType != null && vehicleType.IsInstanceOfType(thing))
                {
                    vehicleCount++;
                    dynamic vehicle = thing;
                    Log.Message($"[The Marked Men] Vehicle: {thing.def.label} at {thing.Position}");

                    // List passengers
                    List<Pawn> passengers = TheMarkedMenVehicleFrameworkCompatibility.GetVehiclePassengers(vehicle);
                    if (passengers.Count > 0)
                    {
                        Log.Message($"  - Passengers: {passengers.Count}");
                        foreach (Pawn passenger in passengers)
                        {
                            string infectionStatus = CrossedUtility.IsInfectedPawn(passenger) ? "(INFECTED)" : "";
                            Log.Message($"    • {passenger.Name} {infectionStatus}");
                        }
                    }
                }
            }

            Log.Message($"[The Marked Men] Total vehicles on map: {vehicleCount}");
        }
    }
}
