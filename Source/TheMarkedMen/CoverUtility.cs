using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace TheMarkedMen
{
    
    
    
    
    public static class CoverUtility
    {
        private const int CoverCacheIntervalTicks = 500;
        private static readonly Dictionary<int, (IntVec3 coverPos, int cachedTick)> coverCache
            = new Dictionary<int, (IntVec3, int)>();

        
        
        
        
        public static IntVec3 FindNearestCover(Pawn pawn, float? searchRadiusOverride = null)
        {
            if (pawn == null || pawn.Map == null || !pawn.Spawned)
                return IntVec3.Invalid;

            int pawnId = pawn.thingIDNumber;
            int currentTick = Find.TickManager.TicksGame;

            
            if (coverCache.TryGetValue(pawnId, out var cached))
            {
                if (currentTick - cached.cachedTick < CoverCacheIntervalTicks && cached.coverPos.IsValid)
                    return cached.coverPos;
            }

            float searchRadius = searchRadiusOverride ?? 30f;
            IntVec3 bestCover = IntVec3.Invalid;
            float bestScore = -999f;

            
            Region region = pawn.GetRegion();
            if (region == null)
                return IntVec3.Invalid;

            int searchCells = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, searchRadius, true))
            {
                searchCells++;
                if (searchCells > 200) 
                    break;

                if (!cell.InBounds(pawn.Map))
                    continue;

                if (!pawn.CanReach(cell, PathEndMode.Touch, Danger.Deadly))
                    continue;

                
                if (!IsCoverCell(cell, pawn.Map))
                    continue;

                
                float distance = pawn.Position.DistanceTo(cell);
                float score = 100f - distance;

                
                if (CanSeeThreatFromCover(cell, pawn.Map))
                    score += 50f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCover = cell;
                }
            }

            
            if (bestCover.IsValid)
                coverCache[pawnId] = (bestCover, currentTick);

            return bestCover;
        }

        
        
        
        private static bool IsCoverCell(IntVec3 cell, Map map)
        {
            
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
                    return true; 

                TerrainDef terrain = map.terrainGrid.TerrainAt(adjacent);
                if (terrain != null && terrain.defName.Contains("Wall"))
                    return true;
            }

            return false;
        }

        
        
        
        
        private static bool CanSeeThreatFromCover(IntVec3 coverCell, Map map)
        {
            
            IntVec3 mapCenter = new IntVec3(map.Size.x / 2, 0, map.Size.z / 2);
            return GenSight.LineOfSight(coverCell, mapCenter, map, true);
        }

        
        
        
        
        public static bool HasLineOfSight(IntVec3 posA, IntVec3 posB, Map map)
        {
            if (!posA.InBounds(map) || !posB.InBounds(map))
                return false;

            return GenSight.LineOfSight(posA, posB, map, true);
        }

        
        
        
        public static void ClearCache()
        {
            coverCache.Clear();
        }

        
        
        
        public static void ClearCacheForPawn(Pawn pawn)
        {
            if (pawn != null)
                coverCache.Remove(pawn.thingIDNumber);
        }
    }
}
