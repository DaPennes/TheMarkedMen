using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Performance optimization module for vehicle raid AI and infected behavior systems.
    /// Implements caching, tick intervals, and efficient data structures.
    /// </summary>
    public static class PerformanceOptimizations
    {
        /// <summary>
        /// Caches reflection MethodInfo/PropertyInfo to avoid repeated reflection overhead.
        /// </summary>
        public static class ReflectionCache
        {
            private static Dictionary<string, MethodInfo> methodCache = new Dictionary<string, MethodInfo>();
            private static Dictionary<string, PropertyInfo> propertyCache = new Dictionary<string, PropertyInfo>();
            private static int lastCacheClearTick = 0;
            private const int CacheClearIntervalTicks = 10000;  // Clear old cache every ~2.8 hours

            /// <summary>
            /// Gets cached MethodInfo for a type's method, caching the result.
            /// </summary>
            public static MethodInfo GetMethod(Type type, string methodName, Type[] parameterTypes = null)
            {
                if (type == null || string.IsNullOrEmpty(methodName))
                {
                    return null;
                }

                string cacheKey = $"{type.FullName}.{methodName}";
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

                // Periodically clear cache to prevent indefinite growth
                int currentTick = Find.TickManager.TicksGame;
                if (currentTick - lastCacheClearTick > CacheClearIntervalTicks)
                {
                    ClearOldCache();
                    lastCacheClearTick = currentTick;
                }

                return method;
            }

            /// <summary>
            /// Gets cached PropertyInfo for a type's property.
            /// </summary>
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

            /// <summary>
            /// Clears reflection cache to prevent unbounded memory growth.
            /// </summary>
            private static void ClearOldCache()
            {
                methodCache.Clear();
                propertyCache.Clear();
            }

            /// <summary>
            /// Manual cache clearing for map transitions or cleanup.
            /// </summary>
            public static void ClearCache()
            {
                ClearOldCache();
            }
        }

        /// <summary>
        /// Efficient tick-based update scheduling to prevent TPS degradation.
        /// </summary>
        public static class TickScheduler
        {
            /// <summary>
            /// Standard tick intervals (in ticks) for different update frequencies.
            /// </summary>
            public const int FastTickInterval = 6;          // ~0.1 seconds
            public const int NormalTickInterval = 60;        // ~1 second
            public const int SlowTickInterval = 250;         // ~4 seconds
            public const int RareTickInterval = 2500;        // ~42 seconds
            public const int CombatScanInterval = 60;        // Vehicle combat target scan
            public const int DisembarkCheckInterval = 30;    // Tactical disembarkation checks

            /// <summary>
            /// Checks if a pawn should update on a given tick interval using hash-based distribution.
            /// This spreads updates across the interval to prevent TPS spikes.
            /// </summary>
            public static bool ShouldUpdateThisTick(Pawn pawn, int intervalTicks)
            {
                if (pawn == null)
                {
                    return false;
                }

                return pawn.IsHashIntervalTick(intervalTicks);
            }

            /// <summary>
            /// Checks if something should update on a global tick interval.
            /// Useful for map-wide systems that don't tie to individual pawns.
            /// </summary>
            public static bool ShouldUpdateGlobalThisTick(int intervalTicks)
            {
                int currentTick = Find.TickManager.TicksGame;
                return currentTick % intervalTicks == 0;
            }

            /// <summary>
            /// Gets the next tick when an update should occur.
            /// Useful for external scheduling systems.
            /// </summary>
            public static int GetNextUpdateTick(int currentTick, int intervalTicks)
            {
                return currentTick + intervalTicks;
            }
        }

        /// <summary>
        /// Region-based search optimization for spatial queries.
        /// Reduces search space by searching only relevant map regions.
        /// </summary>
        public static class RegionSearch
        {
            /// <summary>
            /// Gets all pawns within a search radius using basic distance checks.
            /// Simpler than region traversal but still efficient for moderate searches.
            /// </summary>
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

            /// <summary>
            /// Gets all things of a specific type within a search radius.
            /// </summary>
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

        /// <summary>
        /// Generic caching system for expensive calculations with TTL (time-to-live).
        /// </summary>
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

            /// <summary>
            /// Gets a cached value, or returns null if not found or expired.
            /// </summary>
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

            /// <summary>
            /// Caches a value with a time-to-live in ticks.
            /// </summary>
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

                // Periodically clean up expired entries
                int currentTick = Find.TickManager.TicksGame;
                if (currentTick - lastCleanupTick > CleanupIntervalTicks)
                {
                    CleanupExpired();
                    lastCleanupTick = currentTick;
                }
            }

            /// <summary>
            /// Removes expired entries from cache.
            /// </summary>
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

            /// <summary>
            /// Clears entire cache.
            /// </summary>
            public static void ClearCache()
            {
                cache.Clear();
            }
        }

        /// <summary>
        /// Batch processing system for handling multiple operations efficiently.
        /// Spreads work across multiple ticks to prevent frame hitches.
        /// </summary>
        public static class BatchProcessor
        {
            /// <summary>
            /// Processes a collection in batches spread across ticks.
            /// </summary>
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

            /// <summary>
            /// Gets how many items to process this tick given total count and batch size.
            /// </summary>
            public static int GetCurrentBatchSize(int totalItems, int itemsPerBatch)
            {
                if (totalItems <= itemsPerBatch)
                {
                    return totalItems;  // Process all this tick if small enough
                }

                return itemsPerBatch;
            }
        }

        /// <summary>
        /// Gets diagnostic performance information for profiling.
        /// </summary>
        public static class Diagnostics
        {
            private static int lastProfileTick = 0;
            private const int ProfileIntervalTicks = 2500;  // Profile every ~42 seconds

            /// <summary>
            /// Returns performance diagnostics string (memory usage, cache sizes, etc).
            /// Only generates report periodically to avoid overhead.
            /// </summary>
            public static string GetPerformanceReport()
            {
                int currentTick = Find.TickManager.TicksGame;
                if (currentTick - lastProfileTick < ProfileIntervalTicks)
                {
                    return null;  // Don't profile every tick
                }

                lastProfileTick = currentTick;

                float memoryMB = GC.GetTotalMemory(false) / (1024f * 1024f);
                return $"[Perf] Memory: {memoryMB:F1}MB";
            }

            /// <summary>
            /// Resets profiling timers.
            /// </summary>
            public static void ResetProfile()
            {
                lastProfileTick = 0;
            }
        }
    }
}
