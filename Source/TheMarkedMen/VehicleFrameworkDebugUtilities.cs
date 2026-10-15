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
            Log.Message("[The Marked Men] Vehicle Framework compatibility patch not found - functionality disabled.");
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

            Log.Message("[The Marked Men] Vehicle Framework compatibility patch not found - vehicle status checking disabled.");
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

            Log.Message("[The Marked Men] Vehicle Framework compatibility patch not found - vehicle listing disabled.");
        }
    }
}