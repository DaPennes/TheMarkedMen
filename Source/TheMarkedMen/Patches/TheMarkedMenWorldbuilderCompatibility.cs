using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    [StaticConstructorOnStartup]
    public static class TheMarkedMenWorldbuilderCompatibility
    {
        private const string WorldbuilderCreateWorldParamsPatchTypeName = "Worldbuilder.Page_CreateWorldParams_DoWindowContents_Patch";
        private const string WorldbuilderGuardTypeName = "Worldbuilder.WorldGridReachabilityGuard";
        private const string WorldbuilderPreviewMethodName = "getWorldCameraPreview";

        private static readonly FieldInfo PlanetTileLayerIdField = AccessTools.Field(typeof(PlanetTile), "layerId");
        private static readonly FieldInfo PlanetLayerNeighborOffsetsField = AccessTools.Field(typeof(PlanetLayer), "tileIDToNeighbors_offsets");
        private static readonly FieldInfo PlanetLayerNeighborValuesField = AccessTools.Field(typeof(PlanetLayer), "tileIDToNeighbors_values");
        private static readonly FieldInfo PlanetLayerFillerField = AccessTools.Field(typeof(PlanetLayer), "filler");
        private static readonly FieldInfo WorldFloodFillerTraversalDistanceField = AccessTools.Field(typeof(WorldFloodFiller), "traversalDistance");
        private static readonly MethodInfo CalculateTileNeighborsMethod = AccessTools.Method(typeof(PlanetLayer), "CalculateTileNeighbors");

        private static FieldInfo worldbuilderPreviewDirtyField;
        private static FieldInfo worldbuilderPreviewTextureField;
        private static Texture2D fallbackWorldPreviewTexture;
        private static bool delayedRetryScheduled;
        private static bool patchApplied;
        private static bool previewWarningLogged;
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
                bool appliedAnyPatch = false;

                Type guardType = AccessTools.TypeByName(WorldbuilderGuardTypeName);
                if (guardType != null)
                {
                    MethodInfo fixTileBackReferences = AccessTools.Method(guardType, "FixTileBackReferences");
                    MethodInfo prefix = AccessTools.Method(typeof(TheMarkedMenWorldbuilderCompatibility), nameof(Prefix_FixTileBackReferences));
                    if (fixTileBackReferences == null || prefix == null)
                    {
                        LogWarningOnce("Worldbuilder compatibility skipped: reachability back-reference method was not found.");
                    }
                    else
                    {
                        harmony.Patch(fixTileBackReferences, prefix: new HarmonyMethod(prefix));
                        appliedAnyPatch = true;
                    }

                    MethodInfo ensureSafeForReachability = AccessTools.Method(guardType, "EnsureSafeForReachability", new[] { typeof(World) });
                    MethodInfo ensurePrefix = AccessTools.Method(typeof(TheMarkedMenWorldbuilderCompatibility), nameof(Prefix_EnsureSafeForReachability));
                    MethodInfo finalizer = AccessTools.Method(typeof(TheMarkedMenWorldbuilderCompatibility), nameof(Finalizer_EnsureSafeForReachability));
                    if (ensureSafeForReachability != null)
                    {
                        harmony.Patch(
                            ensureSafeForReachability,
                            prefix: ensurePrefix == null ? null : new HarmonyMethod(ensurePrefix),
                            finalizer: finalizer == null ? null : new HarmonyMethod(finalizer));
                        appliedAnyPatch = true;
                    }
                }

                if (TryPatchWorldPreviewCapture(harmony))
                {
                    appliedAnyPatch = true;
                }

                if (!appliedAnyPatch)
                {
                    return false;
                }

                patchApplied = true;
                LogVerbose("[The Marked Men] Worldbuilder compatibility active.");
                return true;
            }
            catch (Exception ex)
            {
                LogWarningOnce("Worldbuilder compatibility skipped: " + ex.Message);
                return false;
            }
        }

        private static bool TryPatchWorldPreviewCapture(Harmony harmony)
        {
            Type previewType = AccessTools.TypeByName(WorldbuilderCreateWorldParamsPatchTypeName);
            if (previewType == null)
            {
                return false;
            }

            MethodInfo getWorldCameraPreview = AccessTools.Method(previewType, WorldbuilderPreviewMethodName, new[] { typeof(int), typeof(int) });
            MethodInfo prefix = AccessTools.Method(typeof(TheMarkedMenWorldbuilderCompatibility), nameof(Prefix_GetWorldCameraPreview));
            if (getWorldCameraPreview == null || prefix == null)
            {
                LogWarningOnce("Worldbuilder preview compatibility skipped: preview capture method was not found.");
                return false;
            }

            worldbuilderPreviewDirtyField = AccessTools.Field(previewType, "dirty");
            worldbuilderPreviewTextureField = AccessTools.Field(previewType, "worldPreview");
            harmony.Patch(getWorldCameraPreview, prefix: new HarmonyMethod(prefix));
            return true;
        }

        public static bool Prefix_GetWorldCameraPreview(int width, int height, ref Texture2D __result)
        {
            __result = GetFallbackWorldPreviewTexture();
            TrySetWorldbuilderPreviewState(__result);
            TryResetWorldCameraPreviewState();
            LogPreviewWarningOnce("Worldbuilder world preview capture was disabled to avoid a Unity Texture2D.ReadPixels crash during new-world setup. World generation remains available; only the preview thumbnail is replaced.");
            return false;
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

        public static bool Prefix_EnsureSafeForReachability(World __0)
        {
            try
            {
                WorldGridRepairResult result = RepairWorldGridForReachability(__0?.grid);
                if (result.HasChanges)
                {
                    LogVerbose("[The Marked Men] Worldbuilder reachability guard repaired generated world-grid data. Tile refs: "
                        + result.FixedTileReferences + ", neighbor layers rebuilt: " + result.RebuiltNeighborLayers
                        + ", invalid neighbor links removed: " + result.RemovedNeighborLinks + ", flood fillers reset: "
                        + result.RebuiltFloodFillers + ".");
                }
            }
            catch (Exception ex)
            {
                LogWarningOnce("Worldbuilder reachability guard replacement failed safely: " + ex.Message);
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
                int repaired = RepairAllRegisteredLayersSafely();
                LogWarningOnce("Suppressed Worldbuilder reachability guard bounds error during world generation: " + __exception.Message
                    + (repaired > 0 ? " Repaired " + repaired + " world tile back-references before continuing." : string.Empty));
                return null;
            }

            return __exception;
        }

        private static WorldGridRepairResult RepairWorldGridForReachability(WorldGrid grid)
        {
            WorldGridRepairResult result = default;
            IReadOnlyDictionary<int, PlanetLayer> layers = grid?.PlanetLayers;
            if (layers == null || layers.Count == 0)
            {
                return result;
            }

            foreach (KeyValuePair<int, PlanetLayer> layerEntry in layers)
            {
                PlanetLayer layer = layerEntry.Value;
                if (!HasGeneratedTiles(layer))
                {
                    continue;
                }

                result.FixedTileReferences += FixTileBackReferencesSafely(layer);

                int removedNeighborLinks;
                if (TryRepairNeighborDataSafely(layer, out removedNeighborLinks))
                {
                    result.RebuiltNeighborLayers++;
                    result.RemovedNeighborLinks += removedNeighborLinks;
                }

                if (!HasInvalidNeighborData(layer) && EnsureFloodFillerMatchesLayer(layer))
                {
                    result.RebuiltFloodFillers++;
                }
            }

            return result;
        }

        private static int FixTileBackReferencesSafely(PlanetLayer layer)
        {
            List<Tile> tiles = layer?.Tiles;
            if (!HasGeneratedTiles(layer))
            {
                return 0;
            }

            int declaredTileCount = GetDeclaredTileCountSafely(layer, tiles.Count);
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
                if (NeedsBackReferenceRepair(tile.tile, i, layer.LayerID))
                {
                    fixedCount++;
                }

                tile.tile = expected;
            }

            if (declaredTileCount > tiles.Count)
            {
                LogVerbose("[The Marked Men] Worldbuilder reachability guard clamped tile back-reference repair for layer "
                    + layer.LayerID + ": declared tiles " + declaredTileCount + ", available tile objects " + tiles.Count + ".");
            }

            if (fixedCount > 0)
            {
                DirtyLayerTileCache(layer);
            }

            return fixedCount;
        }

        private static int RepairAllRegisteredLayersSafely()
        {
            try
            {
                return RepairWorldGridForReachability(Find.WorldGrid).FixedTileReferences;
            }
            catch (Exception ex)
            {
                LogWarningOnce("Worldbuilder reachability guard fallback repair failed safely: " + ex.Message);
                return 0;
            }
        }

        private static int GetDeclaredTileCountSafely(PlanetLayer layer, int fallback)
        {
            try
            {
                int count = layer.TilesCount;
                return count > 0 ? count : Math.Max(0, fallback);
            }
            catch
            {
                return Math.Max(0, fallback);
            }
        }

        private static bool NeedsBackReferenceRepair(PlanetTile current, int expectedTileId, int expectedLayerId)
        {
            if (!current.Valid || current.tileId != expectedTileId)
            {
                return true;
            }

            int currentLayerId;
            if (!TryGetPlanetTileLayerId(current, out currentLayerId))
            {
                return true;
            }

            return currentLayerId != expectedLayerId;
        }

        private static bool TryRepairNeighborDataSafely(PlanetLayer layer, out int removedLinks)
        {
            removedLinks = 0;
            if (!HasGeneratedTiles(layer))
            {
                return false;
            }

            if (!HasInvalidNeighborData(layer))
            {
                return false;
            }

            TryRecalculateTileNeighbors(layer);
            if (!HasInvalidNeighborData(layer))
            {
                DirtyLayerTileCache(layer);
                return true;
            }

            bool rebuilt = RebuildFilteredNeighborData(layer, out removedLinks);
            if (rebuilt)
            {
                DirtyLayerTileCache(layer);
            }

            return rebuilt;
        }

        private static bool HasInvalidNeighborData(PlanetLayer layer)
        {
            List<Tile> tiles = layer?.Tiles;
            if (!HasGeneratedTiles(layer))
            {
                return false;
            }

            int tileCount = tiles.Count;
            NativeArray<int> offsets = layer.UnsafeTileIDToNeighbors_offsets;
            NativeArray<PlanetTile> values = layer.UnsafeTileIDToNeighbors_values;
            if (!offsets.IsCreated || !values.IsCreated || offsets.Length != tileCount)
            {
                return true;
            }

            int previous = 0;
            for (int i = 0; i < tileCount; i++)
            {
                int offset = offsets[i];
                if (offset < previous || offset < 0 || offset > values.Length)
                {
                    return true;
                }

                int nextOffset = i + 1 < offsets.Length ? offsets[i + 1] : values.Length;
                if (nextOffset < offset || nextOffset > values.Length)
                {
                    return true;
                }

                for (int j = offset; j < nextOffset; j++)
                {
                    if (!IsValidSameLayerTile(values[j], layer.LayerID, tileCount))
                    {
                        return true;
                    }
                }

                previous = offset;
            }

            return false;
        }

        private static bool TryRecalculateTileNeighbors(PlanetLayer layer)
        {
            int tileCount = layer?.Tiles?.Count ?? 0;
            if (tileCount <= 0 || !HasGeneratedTileGeometry(layer, tileCount) || CalculateTileNeighborsMethod == null)
            {
                return false;
            }

            try
            {
                CalculateTileNeighborsMethod.Invoke(layer, null);
                return true;
            }
            catch (Exception ex)
            {
                LogVerbose("[The Marked Men] Worldbuilder compatibility could not recalculate neighbor data directly: " + ex.Message);
                return false;
            }
        }

        private static bool RebuildFilteredNeighborData(PlanetLayer layer, out int removedLinks)
        {
            removedLinks = 0;
            if (!HasGeneratedTiles(layer) || PlanetLayerNeighborOffsetsField == null || PlanetLayerNeighborValuesField == null)
            {
                return false;
            }

            int tileCount = layer.Tiles.Count;
            NativeArray<int> oldOffsets = layer.UnsafeTileIDToNeighbors_offsets;
            NativeArray<PlanetTile> oldValues = layer.UnsafeTileIDToNeighbors_values;
            bool canReadOldData = oldOffsets.IsCreated && oldValues.IsCreated && oldOffsets.Length > 0;
            if (!canReadOldData)
            {
                return false;
            }

            List<int> offsets = new List<int>(tileCount);
            List<PlanetTile> validNeighbors = new List<PlanetTile>();
            for (int i = 0; i < tileCount; i++)
            {
                offsets.Add(validNeighbors.Count);
                HashSet<int> seenTileIds = new HashSet<int>();
                if (!canReadOldData || i >= oldOffsets.Length)
                {
                    continue;
                }

                int start = Clamp(oldOffsets[i], 0, oldValues.Length);
                int end = i + 1 < oldOffsets.Length ? Clamp(oldOffsets[i + 1], start, oldValues.Length) : oldValues.Length;
                for (int j = start; j < end; j++)
                {
                    PlanetTile neighbor = oldValues[j];
                    int neighborId = neighbor.tileId;
                    if (neighborId != i && IsValidSameLayerTile(neighbor, layer.LayerID, tileCount) && seenTileIds.Add(neighborId))
                    {
                        validNeighbors.Add(neighbor);
                    }
                    else
                    {
                        removedLinks++;
                    }
                }
            }

            NativeArray<int> newOffsets = new NativeArray<int>(offsets.Count, Allocator.Persistent);
            NativeArray<PlanetTile> newValues = new NativeArray<PlanetTile>(validNeighbors.Count, Allocator.Persistent);
            for (int i = 0; i < offsets.Count; i++)
            {
                newOffsets[i] = offsets[i];
            }

            for (int i = 0; i < validNeighbors.Count; i++)
            {
                newValues[i] = validNeighbors[i];
            }

            DisposeNativeArraySafely(oldOffsets);
            DisposeNativeArraySafely(oldValues);
            PlanetLayerNeighborOffsetsField.SetValue(layer, newOffsets);
            PlanetLayerNeighborValuesField.SetValue(layer, newValues);
            return true;
        }

        private static bool EnsureFloodFillerMatchesLayer(PlanetLayer layer)
        {
            if (!HasGeneratedTiles(layer) || PlanetLayerFillerField == null)
            {
                return false;
            }

            try
            {
                WorldFloodFiller filler = layer.Filler;
                if (filler != null && WorldFloodFillerTraversalDistanceField != null)
                {
                    List<int> traversalDistance = WorldFloodFillerTraversalDistanceField.GetValue(filler) as List<int>;
                    if (traversalDistance != null && traversalDistance.Count == layer.Tiles.Count)
                    {
                        return false;
                    }
                }

                PlanetLayerFillerField.SetValue(layer, new WorldFloodFiller(layer));
                return true;
            }
            catch (Exception ex)
            {
                LogVerbose("[The Marked Men] Worldbuilder compatibility could not reset flood filler: " + ex.Message);
                return false;
            }
        }

        private static bool IsValidSameLayerTile(PlanetTile tile, int expectedLayerId, int tileCount)
        {
            if (!tile.Valid || tile.tileId < 0 || tile.tileId >= tileCount)
            {
                return false;
            }

            int layerId;
            return TryGetPlanetTileLayerId(tile, out layerId) && layerId == expectedLayerId;
        }

        private static bool HasGeneratedTiles(PlanetLayer layer)
        {
            return layer?.Tiles != null && layer.Tiles.Count > 0;
        }

        private static bool HasGeneratedTileGeometry(PlanetLayer layer, int tileCount)
        {
            if (layer == null || tileCount <= 0)
            {
                return false;
            }

            try
            {
                NativeArray<int> vertOffsets = layer.UnsafeTileIDToVerts_offsets;
                NativeArray<UnityEngine.Vector3> verts = layer.UnsafeVerts;
                return vertOffsets.IsCreated && vertOffsets.Length == tileCount && verts.IsCreated && verts.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetPlanetTileLayerId(PlanetTile tile, out int layerId)
        {
            layerId = -1;
            if (PlanetTileLayerIdField == null)
            {
                return false;
            }

            try
            {
                object value = PlanetTileLayerIdField.GetValue(tile);
                if (value is int id)
                {
                    layerId = id;
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static void DisposeNativeArraySafely<T>(NativeArray<T> array) where T : struct
        {
            try
            {
                if (array.IsCreated)
                {
                    array.Dispose();
                }
            }
            catch
            {
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static void DirtyLayerTileCache(PlanetLayer layer)
        {
            try
            {
                layer?.FastTileFinder?.DirtyCache();
            }
            catch
            {
            }
        }

        private static Texture2D GetFallbackWorldPreviewTexture()
        {
            if (fallbackWorldPreviewTexture != null)
            {
                return fallbackWorldPreviewTexture;
            }

            const int size = 2;
            Color color = new Color(0.12f, 0.12f, 0.12f, 1f);
            fallbackWorldPreviewTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "TheMarkedMen_WorldbuilderPreviewDisabled"
            };

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            fallbackWorldPreviewTexture.SetPixels(pixels);
            fallbackWorldPreviewTexture.Apply(false, false);
            return fallbackWorldPreviewTexture;
        }

        private static void TrySetWorldbuilderPreviewState(Texture2D previewTexture)
        {
            try
            {
                worldbuilderPreviewTextureField?.SetValue(null, previewTexture);
                worldbuilderPreviewDirtyField?.SetValue(null, false);
            }
            catch
            {
            }
        }

        private static void TryResetWorldCameraPreviewState()
        {
            try
            {
                if (Find.WorldCamera != null)
                {
                    Find.WorldCamera.targetTexture = null;
                }
            }
            catch
            {
            }

            try
            {
                RenderTexture.active = null;
            }
            catch
            {
            }

            try
            {
                if (Find.WorldCamera?.gameObject != null)
                {
                    Find.WorldCamera.gameObject.SetActive(false);
                }
            }
            catch
            {
            }

            try
            {
                if (Find.World?.renderer != null)
                {
                    Find.World.renderer.wantedMode = WorldRenderMode.None;
                }
            }
            catch
            {
            }
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

        private static void LogPreviewWarningOnce(string message)
        {
            if (previewWarningLogged)
            {
                return;
            }

            previewWarningLogged = true;
            Log.Warning("[The Marked Men] " + message);
        }

        private struct WorldGridRepairResult
        {
            public int FixedTileReferences;
            public int RebuiltNeighborLayers;
            public int RemovedNeighborLinks;
            public int RebuiltFloodFillers;

            public bool HasChanges => FixedTileReferences > 0 || RebuiltNeighborLayers > 0 || RemovedNeighborLinks > 0 || RebuiltFloodFillers > 0;
        }
    }
}
