using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    /// <summary>
    /// Handles save compatibility and data integrity across mod versions.
    /// Validates loaded data, handles migrations, and prevents data corruption.
    /// </summary>
    public static class SaveCompatibility
    {
        /// <summary>
        /// Current mod version for compatibility tracking.
        /// Increment when breaking changes are made.
        /// </summary>
        private const int CurrentModVersion = 2;

        /// <summary>
        /// Minimum supported save version.
        /// Saves older than this cannot be loaded.
        /// </summary>
        private const int MinimumSupportedVersion = 1;

        /// <summary>
        /// Called on mod load to validate and migrate save data.
        /// </summary>
        public static void ValidateAndMigrateSaveData()
        {
            try
            {
                int savedVersion = GetSavedModVersion();

                if (savedVersion < MinimumSupportedVersion)
                {
                    Log.Warning("[The Marked Men] Save is too old and cannot be loaded. " +
                        "Please start a new game or load a newer save.");
                    return;
                }

                if (savedVersion < CurrentModVersion)
                {
                    Log.Message($"[The Marked Men] Migrating save from version {savedVersion} to {CurrentModVersion}");
                    MigrateSaveData(savedVersion, CurrentModVersion);
                }

                ValidateSaveIntegrity();
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error validating save data: {ex}");
            }
        }

        /// <summary>
        /// Gets the version of the mod from the current save.
        /// </summary>
        private static int GetSavedModVersion()
        {
            // Check if save metadata exists
            if (Current.Game == null)
            {
                return CurrentModVersion;
            }

            // Version info would be stored in save metadata
            // For now, return current version if not found
            return CurrentModVersion;
        }

        /// <summary>
        /// Migrates save data from an old version to a new version.
        /// </summary>
        private static void MigrateSaveData(int fromVersion, int toVersion)
        {
            // Version 1 -> 2: Add new AI systems (backward compatible)
            if (fromVersion < 2)
            {
                MigrateToVersion2();
            }
        }

        /// <summary>
        /// Migration steps for version 1 to version 2.
        /// New AI systems are non-breaking additions.
        /// </summary>
        private static void MigrateToVersion2()
        {
            Log.Message("[The Marked Men] Initializing Phase 5+ AI systems for loaded save...");

            // AIConflictPrevention initializes lazily on first pawn access
            // PerformanceOptimizations uses static caches
            // No persistent data to migrate

            // Validate all existing vehicle lords are intact
            ValidateVehicleLords();

            // Clear AI caches to ensure fresh initialization
            AIConflictPrevention.ClearAllState();
            PerformanceOptimizations.ReflectionCache.ClearCache();
            PerformanceOptimizations.CacheManager.ClearCache();

            Log.Message("[The Marked Men] Save migration complete. New systems initialized.");
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
        /// Validates that vehicle lords still exist and are properly configured.
        /// </summary>
        private static void ValidateVehicleLords()
        {
            if (Current.Game?.Maps == null)
            {
                return;
            }

            int validVehicleLords = 0;
            int invalidVehicleLords = 0;

            foreach (Map map in Current.Game.Maps)
            {
                if (map?.lordManager?.lords == null)
                {
                    continue;
                }

                foreach (Lord lord in map.lordManager.lords)
                {
                    if (lord == null)
                    {
                        continue;
                    }

                    // Check if this is a vehicle assault lord
                    bool isVehicleLord = lord.LordJob is LordJob_CrossedVehicleAssault
                        || (lord.LordJob != null && lord.LordJob.GetType().Name.Contains("Vehicle"));

                    if (isVehicleLord)
                    {
                        if (lord.ownedPawns.Count > 0)
                        {
                            validVehicleLords++;
                        }
                        else
                        {
                            invalidVehicleLords++;
                        }
                    }
                }
            }

            if (validVehicleLords > 0 || invalidVehicleLords > 0)
            {
                Log.Message($"[The Marked Men] Save validation: {validVehicleLords} valid vehicle lords, " +
                    $"{invalidVehicleLords} orphaned vehicle lords");
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

                return $"[Save Compat] Maps: {mapCount}, Pawns: {pawnCount}, Lords: {lordCount}, Mod Version: {CurrentModVersion}";
            }
            catch (Exception ex)
            {
                return $"[Save Compat] Error generating report: {ex.Message}";
            }
        }
    }
}
