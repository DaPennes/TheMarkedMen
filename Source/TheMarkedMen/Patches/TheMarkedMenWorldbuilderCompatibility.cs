using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace TheMarkedMen
{
    public static class TheMarkedMenWorldbuilderCompatibility
    {
        private const string WorldbuilderGuardTypeName = "Worldbuilder.WorldGridReachabilityGuard";

        private static bool delayedRetryScheduled;
        private static bool patchApplied;
        private static bool warningLogged;

        public static void Apply(Harmony harmony)
        {
            if (harmony == null || patchApplied)
            {
                return;
            }

            if (TryApply(harmony) || delayedRetryScheduled)
            {
                return;
            }

            delayedRetryScheduled = true;
            LongEventHandler.ExecuteWhenFinished(() => TryApply(harmony));
        }

        private static bool TryApply(Harmony harmony)
        {
            if (harmony == null || patchApplied)
            {
                return patchApplied;
            }

            try
            {
                Type guardType = AccessTools.TypeByName(WorldbuilderGuardTypeName);
                if (guardType == null)
                {
                    return false;
                }

                MethodInfo fixTileBackReferences = AccessTools.Method(guardType, "FixTileBackReferences");
                MethodInfo prefix = AccessTools.Method(typeof(TheMarkedMenWorldbuilderCompatibility), nameof(Prefix_FixTileBackReferences));
                if (fixTileBackReferences == null || prefix == null)
                {
                    LogWarningOnce("Worldbuilder compatibility skipped: reachability back-reference method was not found.");
                    return false;
                }

                harmony.Patch(fixTileBackReferences, prefix: new HarmonyMethod(prefix));

                MethodInfo ensureSafeForReachability = AccessTools.Method(guardType, "EnsureSafeForReachability", new[] { typeof(World) });
                MethodInfo finalizer = AccessTools.Method(typeof(TheMarkedMenWorldbuilderCompatibility), nameof(Finalizer_EnsureSafeForReachability));
                if (ensureSafeForReachability != null && finalizer != null)
                {
                    harmony.Patch(ensureSafeForReachability, finalizer: new HarmonyMethod(finalizer));
                }

                patchApplied = true;
                LogVerbose("[The Marked Men] Worldbuilder reachability guard compatibility active.");
                return true;
            }
            catch (Exception ex)
            {
                LogWarningOnce("Worldbuilder compatibility skipped: " + ex.Message);
                return false;
            }
        }

        public static bool Prefix_FixTileBackReferences(PlanetLayer layer, ref int __result)
        {
            try
            {
                __result = FixTileBackReferencesSafely(layer);
            }
            catch (Exception ex)
            {
                __result = 0;
                LogWarningOnce("Worldbuilder reachability guard repair failed safely: " + ex.Message);
            }

            return false;
        }

        public static Exception Finalizer_EnsureSafeForReachability(Exception __exception)
        {
            if (__exception == null)
            {
                return null;
            }

            if (__exception is ArgumentOutOfRangeException || __exception is IndexOutOfRangeException)
            {
                LogWarningOnce("Suppressed Worldbuilder reachability guard bounds error during world generation: " + __exception.Message);
                return null;
            }

            return __exception;
        }

        private static int FixTileBackReferencesSafely(PlanetLayer layer)
        {
            List<Tile> tiles = layer?.Tiles;
            if (tiles == null || tiles.Count == 0)
            {
                return 0;
            }

            int declaredTileCount = Math.Max(0, layer.TilesCount);
            int safeTileCount = Math.Min(declaredTileCount, tiles.Count);
            int fixedCount = 0;

            for (int i = 0; i < safeTileCount; i++)
            {
                Tile tile = tiles[i];
                if (tile == null)
                {
                    continue;
                }

                PlanetTile expected = layer.PlanetTileForID(i);
                if (tile.tile != expected)
                {
                    tile.tile = expected;
                    fixedCount++;
                }
            }

            if (declaredTileCount > tiles.Count)
            {
                LogVerbose("[The Marked Men] Worldbuilder reachability guard clamped tile back-reference repair for layer "
                    + layer.LayerID + ": declared tiles " + declaredTileCount + ", available tile objects " + tiles.Count + ".");
            }

            return fixedCount;
        }

        private static void LogVerbose(string message)
        {
            if (TheMarkedMenMod.Settings?.verboseCompatibilityLogging == true)
            {
                Log.Message(message);
            }
        }

        private static void LogWarningOnce(string message)
        {
            if (warningLogged)
            {
                return;
            }

            warningLogged = true;
            Log.Warning("[The Marked Men] " + message);
        }
    }
}
