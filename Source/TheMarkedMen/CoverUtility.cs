using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace TheMarkedMen
{
    /// <summary>
    /// Utility for finding cover positions and determining LOS blocking.
    /// Cached results reused every 500 ticks for performance.
    /// </summary>
    public static class CoverUtility
    {
        private const int CoverCacheIntervalTicks = 500;
        private static readonly Dictionary<int, (IntVec3 coverPos, int cachedTick)> coverCache
            = new Dictionary<int, (IntVec3, int)>();

        /// <summary>
        /// Finds nearest cover position relative to pawn within search radius.
        /// Prefers cover that allows counter-fire (has LOS back to threats).
        /// </summary>
        public static IntVec3 FindNearestCover(Pawn pawn, float? searchRadiusOverride = null)
        {
            if (pawn == null || pawn.Map == null || !pawn.Spawned)
                return IntVec3.Invalid;

            int pawnId = pawn.thingIDNumber;
            int currentTick = Find.TickManager.TicksGame;

            // Check cache
            if (coverCache.TryGetValue(pawnId, out var cached))
            {
                if (currentTick - cached.cachedTick < CoverCacheIntervalTicks && cached.coverPos.IsValid)
                    return cached.coverPos;
            }

            float searchRadius = searchRadiusOverride ?? 30f;
            IntVec3 bestCover = IntVec3.Invalid;
            float bestScore = -999f;

            // Search nearby cells for cover
            Region region = pawn.GetRegion();
            if (region == null)
                return IntVec3.Invalid;

            int searchCells = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, searchRadius, true))
            {
                searchCells++;
                if (searchCells > 200) // Limit search to avoid performance issues
                    break;

                if (!cell.InBounds(pawn.Map))
                    continue;

                if (!pawn.CanReach(cell, PathEndMode.Touch, Danger.Deadly))
                    continue;

                // Check if cell provides cover (adjacent to buildings/walls)
                if (!IsCoverCell(cell, pawn.Map))
                    continue;

                // Score: proximity + cover quality
                float distance = pawn.Position.DistanceTo(cell);
                float score = 100f - distance;

                // Bonus: if cover allows line-of-sight back to colony center (tactical position)
                if (CanSeeThreatFromCover(cell, pawn.Map))
                    score += 50f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCover = cell;
                }
            }

            // Cache result
            if (bestCover.IsValid)
                coverCache[pawnId] = (bestCover, currentTick);

            return bestCover;
        }

        /// <summary>
        /// Check if a cell is adjacent to buildings/walls that block LOS.
        /// </summary>
        private static bool IsCoverCell(IntVec3 cell, Map map)
        {
            // Check all 4 adjacent cells for blocking terrain/buildings
            IntVec3[] adjacentCells = new[]
            {
                cell + IntVec3.North,
                cell + IntVec3.South,
                cell + IntVec3.East,
                cell + IntVec3.West
            };

            foreach (IntVec3 adjacent in adjacentCells)
            {
                if (!adjacent.InBounds(map))
                    continue;

                Building building = adjacent.GetEdifice(map);
                if (building != null && building.def.Fillage == FillCategory.Full)
                    return true; // Full cover adjacent

                TerrainDef terrain = map.terrainGrid.TerrainAt(adjacent);
                if (terrain != null && terrain.defName.Contains("Wall"))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if cover position allows LOS back to map center or colony structures.
        /// Tactical position = can engage threats while protected.
        /// </summary>
        private static bool CanSeeThreatFromCover(IntVec3 coverCell, Map map)
        {
            // Simple heuristic: if cover can see map center, it's tactical
            IntVec3 mapCenter = new IntVec3(map.Size.x / 2, 0, map.Size.z / 2);
            return GenSight.LineOfSight(coverCell, mapCenter, map, true);
        }

        /// <summary>
        /// Check if pawn at posA can see pawn at posB (with cover blocks).
        /// Returns true if LOS is NOT blocked by full cover.
        /// </summary>
        public static bool HasLineOfSight(IntVec3 posA, IntVec3 posB, Map map)
        {
            if (!posA.InBounds(map) || !posB.InBounds(map))
                return false;

            return GenSight.LineOfSight(posA, posB, map, true);
        }

        /// <summary>
        /// Clear all cached cover positions (call on map load/cleanup).
        /// </summary>
        public static void ClearCache()
        {
            coverCache.Clear();
        }

        /// <summary>
        /// Clear cache for a specific pawn.
        /// </summary>
        public static void ClearCacheForPawn(Pawn pawn)
        {
            if (pawn != null)
                coverCache.Remove(pawn.thingIDNumber);
        }
    }
}
