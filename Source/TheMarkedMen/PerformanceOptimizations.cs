using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    
    
    
    
    public static class PerformanceOptimizations
    {
        
        
        
        public static class ReflectionCache
        {
            private static Dictionary<string, MethodInfo> methodCache = new Dictionary<string, MethodInfo>();
            private static Dictionary<string, PropertyInfo> propertyCache = new Dictionary<string, PropertyInfo>();
            private static int lastCacheClearTick = 0;
            private const int CacheClearIntervalTicks = 10000;  

            
            
            
            public static MethodInfo GetMethod(Type type, string methodName, Type[] parameterTypes = null)
            {
                if (type == null || string.IsNullOrEmpty(methodName))
                {
                    return null;
                }

                string parameterSignature = parameterTypes == null
                    ? "*"
                    : "(" + string.Join(",", Array.ConvertAll(parameterTypes, parameterType => parameterType?.AssemblyQualifiedName ?? "<null>")) + ")";
                string cacheKey = $"{type.AssemblyQualifiedName}.{methodName}{parameterSignature}";
                if (!methodCache.TryGetValue(cacheKey, out MethodInfo method))
                {
                    method = parameterTypes != null
                        ? type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static, null, parameterTypes, null)
                        : type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);

                    if (method != null)
                    {
                        methodCache[cacheKey] = method;
                    }
                }

                
                int? currentTick = Find.TickManager?.TicksGame;
                if (currentTick.HasValue && currentTick.Value - lastCacheClearTick > CacheClearIntervalTicks)
                {
                    ClearOldCache();
                    lastCacheClearTick = currentTick.Value;
                }

                return method;
            }

            
            
            
            public static PropertyInfo GetProperty(Type type, string propertyName)
            {
                if (type == null || string.IsNullOrEmpty(propertyName))
                {
                    return null;
                }

                string cacheKey = $"{type.FullName}.{propertyName}";
                if (!propertyCache.TryGetValue(cacheKey, out PropertyInfo prop))
                {
                    prop = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                    if (prop != null)
                    {
                        propertyCache[cacheKey] = prop;
                    }
                }

                return prop;
            }

            
            
            
            private static void ClearOldCache()
            {
                methodCache.Clear();
                propertyCache.Clear();
            }

            
            
            
            public static void ClearCache()
            {
                ClearOldCache();
            }
        }

        
        
        
        public static class TickScheduler
        {
            
            
            
            public const int FastTickInterval = 12;          
            public const int NormalTickInterval = 120;        
            public const int SlowTickInterval = 500;         
            public const int RareTickInterval = 5000;        
            public const int CombatScanInterval = 60;        
            public const int DisembarkCheckInterval = 30;    

            
            
            
            
            public static bool ShouldUpdateThisTick(Pawn pawn, int intervalTicks)
            {
                if (pawn == null)
                {
                    return false;
                }

                return pawn.IsHashIntervalTick(intervalTicks);
            }

            
            
            
            
            public static bool ShouldUpdateGlobalThisTick(int intervalTicks)
            {
                int currentTick = Find.TickManager.TicksGame;
                return currentTick % intervalTicks == 0;
            }

            
            
            
            
            public static int GetNextUpdateTick(int currentTick, int intervalTicks)
            {
                return currentTick + intervalTicks;
            }
        }

        
        
        
        
        public static class RegionSearch
        {
            
            
            
            
            public static List<Pawn> GetPawnsInSearchRadius(Pawn origin, float searchRadiusSquared, Predicate<Pawn> validator = null)
            {
                List<Pawn> results = new List<Pawn>();

                if (origin?.Map == null || searchRadiusSquared <= 0f)
                {
                    return results;
                }

                Map map = origin.Map;
                IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
                if (allPawns == null)
                {
                    return results;
                }

                for (int i = 0; i < allPawns.Count; i++)
                {
                    Pawn pawn = allPawns[i];
                    if (pawn != origin
                        && pawn.Position.DistanceToSquared(origin.Position) <= searchRadiusSquared
                        && (validator == null || validator(pawn)))
                    {
                        results.Add(pawn);
                    }
                }

                return results;
            }

            
            
            
            public static List<Thing> GetThingsInSearchRadius(Pawn origin, float searchRadiusSquared, Predicate<Thing> validator = null)
            {
                List<Thing> results = new List<Thing>();

                if (origin?.Map == null || searchRadiusSquared <= 0f)
                {
                    return results;
                }

                Map map = origin.Map;
                List<Thing> allThings = map.listerThings.AllThings;
                if (allThings == null)
                {
                    return results;
                }

                for (int i = 0; i < allThings.Count; i++)
                {
                    Thing thing = allThings[i];
                    if (thing.Position.DistanceToSquared(origin.Position) <= searchRadiusSquared
                        && (validator == null || validator(thing)))
                    {
                        results.Add(thing);
                    }
                }

                return results;
            }
        }

        
        
        
        public static class CacheManager
        {
            private class CacheEntry
            {
                public object value;
                public int createdTick;
                public int ttlTicks;

                public bool IsExpired(int currentTick)
                {
                    return currentTick - createdTick >= ttlTicks;
                }
            }

            private static Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>();
            private static int lastCleanupTick = 0;
            private const int CleanupIntervalTicks = 5000;

            
            
            
            public static object GetCached(string key)
            {
                if (string.IsNullOrEmpty(key) || !cache.TryGetValue(key, out CacheEntry entry))
                {
                    return null;
                }

                int currentTick = Find.TickManager.TicksGame;
                if (entry.IsExpired(currentTick))
                {
                    cache.Remove(key);
                    return null;
                }

                return entry.value;
            }

            
            
            
            public static void Cache(string key, object value, int ttlTicks)
            {
                if (string.IsNullOrEmpty(key) || value == null)
                {
                    return;
                }

                cache[key] = new CacheEntry
                {
                    value = value,
                    createdTick = Find.TickManager.TicksGame,
                    ttlTicks = ttlTicks
                };

                
                int currentTick = Find.TickManager.TicksGame;
                if (currentTick - lastCleanupTick > CleanupIntervalTicks)
                {
                    CleanupExpired();
                    lastCleanupTick = currentTick;
                }
            }

            
            
            
            private static void CleanupExpired()
            {
                int currentTick = Find.TickManager.TicksGame;
                List<string> keysToRemove = new List<string>();

                foreach (var kvp in cache)
                {
                    if (kvp.Value.IsExpired(currentTick))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (string key in keysToRemove)
                {
                    cache.Remove(key);
                }
            }

            
            
            
            public static void ClearCache()
            {
                cache.Clear();
            }
        }

        
        
        
        
        public static class BatchProcessor
        {
            
            
            
            public static void ProcessBatch<T>(List<T> items, int batchSize, System.Action<T> processor)
            {
                if (items == null || items.Count == 0 || processor == null)
                {
                    return;
                }

                int currentTick = Find.TickManager.TicksGame;
                int batchIndex = (currentTick / TickScheduler.NormalTickInterval) % ((items.Count + batchSize - 1) / batchSize);
                int startIndex = batchIndex * batchSize;
                int endIndex = Mathf.Min(startIndex + batchSize, items.Count);

                for (int i = startIndex; i < endIndex; i++)
                {
                    processor(items[i]);
                }
            }

            
            
            
            public static int GetCurrentBatchSize(int totalItems, int itemsPerBatch)
            {
                if (totalItems <= itemsPerBatch)
                {
                    return totalItems;  
                }

                return itemsPerBatch;
            }
        }

        
        
        
        public static class Diagnostics
        {
            private static int lastProfileTick = 0;
            private const int ProfileIntervalTicks = 2500;  

            
            
            
            
            public static string GetPerformanceReport()
            {
                int currentTick = Find.TickManager.TicksGame;
                if (currentTick - lastProfileTick < ProfileIntervalTicks)
                {
                    return null;  
                }

                lastProfileTick = currentTick;

                float memoryMB = GC.GetTotalMemory(false) / (1024f * 1024f);
                return $"[Perf] Memory: {memoryMB:F1}MB";
            }

            
            
            
            public static void ResetProfile()
            {
                lastProfileTick = 0;
            }
        }
    }
}
