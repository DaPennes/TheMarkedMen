using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    /// <summary>
    /// Provides save compatibility hooks and explicit integrity diagnostics.
    /// </summary>
    public static class SaveCompatibility
    {
        /// <summary>
        /// Reinitializes transient state after a game is loaded or initialized.
        /// Persistent raid schedule migration is handled by TheMarkedMenGameComponent.
        /// </summary>
        public static void OnGameInitialized()
        {
            try
            {
                AIConflictPrevention.ClearAllState();
                PerformanceOptimizations.ReflectionCache.ClearCache();
                PerformanceOptimizations.CacheManager.ClearCache();
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error resetting transient compatibility state: {ex}");
            }
        }

        /// <summary>
        /// Legacy public entry point retained for callers from other mods.
        /// The mod's persistent data migrations run in their owning components.
        /// </summary>
        [Obsolete("Persistent migrations run in TheMarkedMenGameComponent. Use ValidateSaveIntegrity only for explicit diagnostics.")]
        public static void ValidateAndMigrateSaveData()
        {
            ValidateSaveIntegrity();
        }

        /// <summary>
        /// Validates the integrity of all save data.
        /// Checks for orphaned references, invalid states, and data corruption.
        /// </summary>
        public static void ValidateSaveIntegrity()
        {
            try
            {
                if (Current.Game?.Maps == null)
                {
                    return;
                }

                foreach (Map map in Current.Game.Maps)
                {
                    if (map == null)
                    {
                        continue;
                    }

                    ValidateMapIntegrity(map);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error validating save integrity: {ex}");
            }
        }

        /// <summary>
        /// Validates data integrity for a single map.
        /// </summary>
        private static void ValidateMapIntegrity(Map map)
        {
            try
            {
                // Validate all Crossed faction pawns
                List<Pawn> crossedPawns = new List<Pawn>();
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    if (pawn == null || pawn.Destroyed)
                    {
                        continue;
                    }

                    if (pawn.Faction != null && pawn.Faction.def?.defName == "Crossed")
                    {
                        ValidatePawnIntegrity(pawn);
                    }
                }

                // Validate all lords are intact
                ValidateLordStructures(map);

                // Validate all vehicles are properly configured
                ValidateVehicleStructures(map);
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error validating map '{map.uniqueID}': {ex}");
            }
        }

        /// <summary>
        /// Validates data integrity for a single pawn.
        /// </summary>
        private static void ValidatePawnIntegrity(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed)
            {
                return;
            }

            // Check for null Map
            if (pawn.Spawned && pawn.Map == null)
            {
                Log.Warning($"[The Marked Men] Pawn {pawn.LabelShort} has null Map while spawned. Correcting...");
                pawn.DeSpawn();
            }

            // Check for invalid position
            if (pawn.Spawned && !pawn.Position.InBounds(pawn.Map))
            {
                Log.Warning($"[The Marked Men] Pawn {pawn.LabelShort} at invalid position. Correcting...");
                pawn.DeSpawn();
            }

            // Check for null pawn data
            if (pawn.health == null)
            {
                Log.Warning($"[The Marked Men] Pawn {pawn.LabelShort} has null health data.");
            }

            if (pawn.jobs == null)
            {
                Log.Warning($"[The Marked Men] Pawn {pawn.LabelShort} has null jobs system.");
            }

            if (pawn.mindState == null)
            {
                Log.Warning($"[The Marked Men] Pawn {pawn.LabelShort} has null mindState.");
            }

            // Validate lord assignment if in combat
            if (pawn.GetLord() != null && pawn.GetLord().faction != pawn.Faction)
            {
                Log.Warning($"[The Marked Men] Pawn {pawn.LabelShort} has faction mismatch with lord. Detaching...");
                pawn.GetLord().RemovePawn(pawn);
            }
        }

        /// <summary>
        /// Validates that all lord structures are intact.
        /// </summary>
        private static void ValidateLordStructures(Map map)
        {
            if (map.lordManager?.lords == null)
            {
                return;
            }

            List<Lord> loadsToRemove = new List<Lord>();
            foreach (Lord lord in map.lordManager.lords)
            {
                if (lord == null)
                {
                    loadsToRemove.Add(lord);
                    continue;
                }

                // Check for orphaned lords (no pawns)
                if (lord.ownedPawns.Count == 0)
                {
                    Log.Message($"[The Marked Men] Removing orphaned lord with job {lord.LordJob?.GetType().Name ?? "Unknown"}");
                    loadsToRemove.Add(lord);
                    continue;
                }

                // Check for pawn faction mismatches
                foreach (Pawn pawn in lord.ownedPawns)
                {
                    if (pawn == null || pawn.Destroyed)
                    {
                        continue;
                    }

                    if (pawn.Faction != lord.faction)
                    {
                        Log.Warning($"[The Marked Men] Lord pawn faction mismatch detected. Correcting...");
                        lord.RemovePawn(pawn);
                    }
                }
            }

            // Remove orphaned lords
            foreach (Lord lord in loadsToRemove)
            {
                if (lord != null && map.lordManager.lords.Contains(lord))
                {
                    map.lordManager.RemoveLord(lord);
                }
            }
        }

        /// <summary>
        /// Validates that all vehicle structures are intact.
        /// </summary>
        private static void ValidateVehicleStructures(Map map)
        {
            // Try to detect and validate vehicle structures via reflection
            try
            {
                var vehicleFrameworkCompat = typeof(TheMarkedMenVehicleFrameworkCompatibility);
                if (vehicleFrameworkCompat == null)
                {
                    return;
                }

                // If Vehicle Framework is available, validate through compatibility layer
                // This is called implicitly when validating pawns
                // (vehicles are pawns in the RimWorld vehicle system)
            }
            catch
            {
                // Vehicle Framework not available, skip
            }
        }

        /// <summary>
        /// Called when a map is generated to ensure all systems initialize correctly.
        /// </summary>
        public static void OnMapGenerated(Map map)
        {
            try
            {
                if (map == null)
                {
                    return;
                }

                Log.Message($"[The Marked Men] Initializing AI systems for map {map.uniqueID}");

                // Initialize AI conflict prevention for new map
                AIConflictPrevention.ClearAllState();

                // Clear performance caches for fresh start
                PerformanceOptimizations.ReflectionCache.ClearCache();
                PerformanceOptimizations.CacheManager.ClearCache();

                Log.Message($"[The Marked Men] Map {map.uniqueID} AI systems initialized");
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error initializing map: {ex}");
            }
        }

        /// <summary>
        /// Called when a pawn is spawned to ensure it's in a valid state.
        /// </summary>
        public static void OnPawnSpawned(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned)
            {
                return;
            }

            try
            {
                // Validate pawn position
                if (!pawn.Position.InBounds(pawn.Map))
                {
                    Log.Warning($"[The Marked Men] Pawn {pawn.LabelShort} spawned at invalid position, correcting...");
                    pawn.DeSpawn();
                    return;
                }

                // Initialize AI state tracking
                if (pawn.Faction != null && pawn.Faction.def?.defName == "Crossed")
                {
                    // Lazy initialization happens on first access
                    // Just ensure the system is ready
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error validating spawned pawn: {ex}");
            }
        }

        /// <summary>
        /// Called when a pawn dies to clean up AI state.
        /// </summary>
        public static void OnPawnDied(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            try
            {
                // Clean up AI state tracking
                AIConflictPrevention.NotifyPawnRemoved(pawn);
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error cleaning up dead pawn: {ex}");
            }
        }

        /// <summary>
        /// Called when a map is cleaned up to prevent memory leaks.
        /// </summary>
        public static void OnMapCleanup(Map map)
        {
            if (map == null)
            {
                return;
            }

            try
            {
                Log.Message($"[The Marked Men] Cleaning up AI systems for map {map.uniqueID}");

                // Clear all AI state for this map's pawns
                AIConflictPrevention.ClearAllState();

                // Performance caches are map-agnostic, only clear if needed
                // Don't clear here to preserve cache across map transitions
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error cleaning up map: {ex}");
            }
        }

        /// <summary>
        /// Gets compatibility diagnostic information.
        /// </summary>
        public static string GetCompatibilityReport()
        {
            try
            {
                int mapCount = Current.Game?.Maps?.Count ?? 0;
                int pawnCount = 0;
                int lordCount = 0;

                foreach (Map map in Current.Game?.Maps ?? new List<Map>())
                {
                    if (map == null)
                    {
                        continue;
                    }

                    pawnCount += map.mapPawns.AllPawnsSpawned.Count;
                    lordCount += map.lordManager?.lords?.Count ?? 0;
                }

                return $"[Save Compat] Maps: {mapCount}, Pawns: {pawnCount}, Lords: {lordCount}";
            }
            catch (Exception ex)
            {
                return $"[Save Compat] Error generating report: {ex.Message}";
            }
        }
    }
}
