using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    
    
    
    public static class SaveCompatibility
    {
        
        
        
        
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

        
        
        
        
        [Obsolete("Persistent migrations run in TheMarkedMenGameComponent. Use ValidateSaveIntegrity only for explicit diagnostics.")]
        public static void ValidateAndMigrateSaveData()
        {
            ValidateSaveIntegrity();
        }

        
        
        
        
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

        
        
        
        private static void ValidateMapIntegrity(Map map)
        {
            try
            {
                
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

                
                ValidateLordStructures(map);

                
                ValidateVehicleStructures(map);
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error validating map '{map.uniqueID}': {ex}");
            }
        }

        
        
        
        private static void ValidatePawnIntegrity(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed)
            {
                return;
            }

            
            if (pawn.Spawned && pawn.Map == null)
            {
                Log.Warning($"[The Marked Men] Pawn {pawn.LabelShort} has null Map while spawned. Correcting...");
                pawn.DeSpawn();
            }

            
            if (pawn.Spawned && !pawn.Position.InBounds(pawn.Map))
            {
                Log.Warning($"[The Marked Men] Pawn {pawn.LabelShort} at invalid position. Correcting...");
                pawn.DeSpawn();
            }

            
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

            
            if (pawn.GetLord() != null && pawn.GetLord().faction != pawn.Faction)
            {
                Log.Warning($"[The Marked Men] Pawn {pawn.LabelShort} has faction mismatch with lord. Detaching...");
                pawn.GetLord().RemovePawn(pawn);
            }
        }

        
        
        
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

                
                if (lord.ownedPawns.Count == 0)
                {
                    Log.Message($"[The Marked Men] Removing orphaned lord with job {lord.LordJob?.GetType().Name ?? "Unknown"}");
                    loadsToRemove.Add(lord);
                    continue;
                }

                
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

            
            foreach (Lord lord in loadsToRemove)
            {
                if (lord != null && map.lordManager.lords.Contains(lord))
                {
                    map.lordManager.RemoveLord(lord);
                }
            }
        }

        
        
        
        private static void ValidateVehicleStructures(Map map)
        {
            
            try
            {
                var vehicleFrameworkCompat = typeof(TheMarkedMenVehicleFrameworkCompatibility);
                if (vehicleFrameworkCompat == null)
                {
                    return;
                }

                
                
                
            }
            catch
            {
                
            }
        }

        
        
        
        public static void OnMapGenerated(Map map)
        {
            try
            {
                if (map == null)
                {
                    return;
                }

                Log.Message($"[The Marked Men] Initializing AI systems for map {map.uniqueID}");

                
                AIConflictPrevention.ClearAllState();

                
                PerformanceOptimizations.ReflectionCache.ClearCache();
                PerformanceOptimizations.CacheManager.ClearCache();

                Log.Message($"[The Marked Men] Map {map.uniqueID} AI systems initialized");
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error initializing map: {ex}");
            }
        }

        
        
        
        public static void OnPawnSpawned(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned)
            {
                return;
            }

            try
            {
                
                if (!pawn.Position.InBounds(pawn.Map))
                {
                    Log.Warning($"[The Marked Men] Pawn {pawn.LabelShort} spawned at invalid position, correcting...");
                    pawn.DeSpawn();
                    return;
                }

                
                if (pawn.Faction != null && pawn.Faction.def?.defName == "Crossed")
                {
                    
                    
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error validating spawned pawn: {ex}");
            }
        }

        
        
        
        public static void OnPawnDied(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            try
            {
                
                AIConflictPrevention.NotifyPawnRemoved(pawn);
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error cleaning up dead pawn: {ex}");
            }
        }

        
        
        
        public static void OnMapCleanup(Map map)
        {
            if (map == null)
            {
                return;
            }

            try
            {
                Log.Message($"[The Marked Men] Cleaning up AI systems for map {map.uniqueID}");

                
                AIConflictPrevention.ClearAllState();

                
                
            }
            catch (Exception ex)
            {
                Log.Error($"[The Marked Men] Error cleaning up map: {ex}");
            }
        }

        
        
        
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
