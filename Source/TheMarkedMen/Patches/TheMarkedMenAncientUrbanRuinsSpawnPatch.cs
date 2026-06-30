using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    [StaticConstructorOnStartup]
    public static class TheMarkedMenAncientUrbanRuinsSpawnPatch
    {
        private const string AurPackageId = "XMB.AncientUrbanRuins.MO";
        private const string AurNamespace = "AncientMarket_Libraray";
        private const int EdgeSpawnMaxAttempts = 100;
        private const int RandomSpawnMaxAttempts = 200;

        private static bool patchApplied;
        private static bool aurDetected;

        private static bool currentlyRedirecting;

        public static bool Active
        {
            get
            {
                TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
                return settings != null && settings.aurSpawnPatchEnabled && AurDetected;
            }
        }

        public static bool AurDetected
        {
            get
            {
                return aurDetected;
            }
        }

        public static void Apply(Harmony harmony)
        {
            if (harmony == null || patchApplied)
            {
                return;
            }

            try
            {
                aurDetected = IsAurModLoaded();
                if (!aurDetected)
                {
                    Log.Message("[The Marked Men] AUR spawn patch: Ancient Urban Ruins not detected, skipping.");
                    return;
                }

                MethodInfo target = AccessTools.Method(typeof(GenSpawn), "Spawn", new[]
                {
                    typeof(Thing),
                    typeof(IntVec3),
                    typeof(Map),
                    typeof(Rot4),
                    typeof(WipeMode),
                    typeof(bool),
                    typeof(bool)
                });

                MethodInfo prefix = AccessTools.Method(
                    typeof(TheMarkedMenAncientUrbanRuinsSpawnPatch),
                    nameof(Prefix_Spawn));

                if (target == null || prefix == null)
                {
                    Log.Warning("[The Marked Men] AUR spawn patch: could not resolve GenSpawn.Spawn target.");
                    return;
                }

                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                patchApplied = true;
                Log.Message("[The Marked Men] AUR spawn patch active. Ancient Urban Ruins detected.");
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] AUR spawn patch failed to apply: " + ex.Message);
            }
        }

        public static bool Prefix_Spawn(
            ref IntVec3 loc,
            Thing newThing,
            Map map,
            Rot4 rot,
            WipeMode wipeMode,
            bool respawningAfterLoad,
            bool forbidLeavings)
        {
            if (currentlyRedirecting)
            {
                return true;
            }

            if (!Active || map == null || newThing == null)
            {
                return true;
            }

            if (respawningAfterLoad)
            {
                return true;
            }

            Pawn pawn = newThing as Pawn;
            if (pawn == null)
            {
                return true;
            }

            if (!pawn.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            if (!MapClassificationService.IsUrbanRuinMap(map))
            {
                return true;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null)
            {
                return true;
            }

            float minDist = Mathf.Max(10f, settings.aurMinimumSpawnDistance);
            if (IsPositionSafe(map, loc, minDist))
            {
                return true;
            }

            LogVerbose(
                "[The Marked Men] AUR spawn patch: redirecting hostile pawn spawn at " +
                loc + " (too close to player pawns).");

            currentlyRedirecting = true;
            try
            {
                IntVec3 safePos;
                if (TryFindSafeSpawnPosition(map, minDist, settings.aurPreferEdgeSpawn, out safePos))
                {
                    loc = safePos;
                    LogVerbose("[The Marked Men] AUR spawn patch: redirected to " + safePos + ".");
                }
                else
                {
                    LogVerbose("[The Marked Men] AUR spawn patch: could not find safe spawn position, using original.");
                }
            }
            finally
            {
                currentlyRedirecting = false;
            }

            return true;
        }

        private static bool TryFindSafeSpawnPosition(Map map, float minDistance, bool preferEdge, out IntVec3 result)
        {
            if (preferEdge)
            {
                for (int i = 0; i < EdgeSpawnMaxAttempts; i++)
                {
                    IntVec3 cell = CellFinder.RandomEdgeCell(map);
                    if (IsValidSpawnCell(map, cell))
                    {
                        result = cell;
                        return true;
                    }
                }

                if (CellFinder.TryFindRandomEdgeCellWith(
                    (IntVec3 c) => IsValidSpawnCell(map, c),
                    map,
                    0f,
                    out IntVec3 edgeResult))
                {
                    result = edgeResult;
                    return true;
                }
            }

            float minDistSq = minDistance * minDistance;
            List<Pawn> playerPawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);

            if (playerPawns.Count == 0)
            {
                for (int i = 0; i < RandomSpawnMaxAttempts; i++)
                {
                    IntVec3 cell = CellFinder.RandomCell(map);
                    if (IsValidSpawnCell(map, cell))
                    {
                        result = cell;
                        return true;
                    }
                }

                result = IntVec3.Invalid;
                return false;
            }

            for (int i = 0; i < RandomSpawnMaxAttempts; i++)
            {
                IntVec3 cell = CellFinder.RandomCell(map);
                if (!IsValidSpawnCell(map, cell))
                {
                    continue;
                }

                bool farEnough = true;
                for (int j = 0; j < playerPawns.Count; j++)
                {
                    Pawn playerPawn = playerPawns[j];
                    if (playerPawn == null || !playerPawn.Spawned)
                    {
                        continue;
                    }

                    if (cell.DistanceToSquared(playerPawn.Position) < minDistSq)
                    {
                        farEnough = false;
                        break;
                    }
                }

                if (farEnough)
                {
                    result = cell;
                    return true;
                }
            }

            result = IntVec3.Invalid;
            return false;
        }

        private static bool IsPositionSafe(Map map, IntVec3 loc, float minDistance)
        {
            if (!loc.IsValid || !loc.InBounds(map))
            {
                return false;
            }

            if (!IsValidSpawnCell(map, loc))
            {
                return false;
            }

            float minDistSq = minDistance * minDistance;
            List<Pawn> playerPawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);

            for (int i = 0; i < playerPawns.Count; i++)
            {
                Pawn playerPawn = playerPawns[i];
                if (playerPawn == null || !playerPawn.Spawned)
                {
                    continue;
                }

                if (loc.DistanceToSquared(playerPawn.Position) < minDistSq)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidSpawnCell(Map map, IntVec3 cell)
        {
            if (!cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            if (cell.Fogged(map))
            {
                return false;
            }

            if (!cell.Standable(map))
            {
                return false;
            }

            if (!cell.Walkable(map))
            {
                return false;
            }

            if (cell.Impassable(map))
            {
                return false;
            }

            Building building = cell.GetEdifice(map);
            if (building != null)
            {
                if (building.Faction == Faction.OfPlayer)
                {
                    return false;
                }

                if (building is Building_Door)
                {
                    return false;
                }

                if (building is Building_Trap)
                {
                    return false;
                }
            }

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain != null && terrain.passability == Traversability.Impassable)
            {
                return false;
            }

            return true;
        }

        private static bool IsAurModLoaded()
        {
            try
            {
                foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
                {
                    if (string.Equals(
                            mod.PackageIdPlayerFacing,
                            AurPackageId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                if (CheckAurNamespace())
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool? aurNamespaceChecked;

        private static bool CheckAurNamespace()
        {
            if (aurNamespaceChecked.HasValue)
            {
                return aurNamespaceChecked.Value;
            }

            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        if (asm.GetTypes().Any(t =>
                            t.Namespace != null &&
                            t.Namespace.StartsWith(AurNamespace, StringComparison.Ordinal)))
                        {
                            aurNamespaceChecked = true;
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            aurNamespaceChecked = false;
            return false;
        }

        private static void LogVerbose(string message)
        {
            if (TheMarkedMenMod.Settings?.aurSpawnPatchDebugLogging == true)
            {
                Log.Message(message);
            }
        }
    }
}
