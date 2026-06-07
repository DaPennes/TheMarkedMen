using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class ScenPart_MarkedVillageStart : ScenPart
    {
        private const int VillageFootprintRadius = 24;
        private const int SiteSearchRadius = 72;
        private const int SiteSearchStep = 3;
        private const float FullVillageScoreThreshold = 780f;

        private bool villageGenerated;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref villageGenerated, "villageGenerated", false);
        }

        public override void Notify_PawnGenerated(Pawn pawn, PawnGenerationContext context, bool redressed)
        {
            base.Notify_PawnGenerated(pawn, context, redressed);
            if (context == PawnGenerationContext.PlayerStarter)
            {
                CrossedUtility.GrantMarkedVillageFounderState(pawn);
            }
        }

        public override void GenerateIntoMap(Map map)
        {
            base.GenerateIntoMap(map);
            TryGenerateVillage(map);
        }

        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            TryGenerateVillage(map);
        }

        public override void PostGameStart()
        {
            base.PostGameStart();
            ApplyFounderStateToPlayerStarters();
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            yield return "One immune marked founder starts in a center-biased adaptive tribal village with industrial technology.";
        }

        private static void ApplyFounderStateToPlayerStarters()
        {
            if (Find.Maps == null)
            {
                return;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                IReadOnlyList<Pawn> colonists = map?.mapPawns?.FreeColonistsSpawned;
                if (colonists == null)
                {
                    continue;
                }

                for (int j = 0; j < colonists.Count; j++)
                {
                    CrossedUtility.GrantMarkedVillageFounderState(colonists[j]);
                }
            }
        }

        private void TryGenerateVillage(Map map)
        {
            if (villageGenerated || map == null || map.Size.x <= 0 || map.Size.z <= 0)
            {
                return;
            }

            VillageSite site = FindVillageSite(map);
            if (!site.origin.IsValid || !site.origin.InBounds(map))
            {
                return;
            }

            bool fullVillage = site.score >= FullVillageScoreThreshold;
            ClearVillageArea(map, site.origin);
            BuildCoreVillage(map, site.origin, fullVillage);
            BuildAdaptivePerimeter(map, site.origin, fullVillage);
            PrepareVillageGround(map, site.origin, fullVillage);
            BuildFields(map, site.origin);
            PlaceStartingCaches(map, site.origin);
            villageGenerated = true;
        }

        private static VillageSite FindVillageSite(Map map)
        {
            IntVec3 center = ClampToBuildSearchBounds(map, new IntVec3(map.Size.x / 2, 0, map.Size.z / 2));
            VillageSite centerSite = ScoreVillageSite(map, center);
            if (centerSite.score >= FullVillageScoreThreshold && centerSite.mediumCells >= 460)
            {
                return centerSite;
            }

            VillageSite best = centerSite;
            for (int radius = SiteSearchStep; radius <= SiteSearchRadius; radius += SiteSearchStep)
            {
                for (int x = -radius; x <= radius; x += SiteSearchStep)
                {
                    ConsiderSite(map, center, x, -radius, ref best);
                    ConsiderSite(map, center, x, radius, ref best);
                }

                for (int z = -radius + SiteSearchStep; z <= radius - SiteSearchStep; z += SiteSearchStep)
                {
                    ConsiderSite(map, center, -radius, z, ref best);
                    ConsiderSite(map, center, radius, z, ref best);
                }

                if (best.score >= FullVillageScoreThreshold && best.mediumCells >= 520)
                {
                    return best;
                }
            }

            return best;
        }

        private static void ConsiderSite(Map map, IntVec3 center, int offsetX, int offsetZ, ref VillageSite best)
        {
            IntVec3 candidate = ClampToBuildSearchBounds(map, new IntVec3(center.x + offsetX, 0, center.z + offsetZ));
            VillageSite site = ScoreVillageSite(map, candidate);
            float currentDistance = Mathf.Abs(best.origin.x - center.x) + Mathf.Abs(best.origin.z - center.z);
            float candidateDistance = Mathf.Abs(candidate.x - center.x) + Mathf.Abs(candidate.z - center.z);
            if (site.score > best.score + 12f || site.score >= best.score - 12f && candidateDistance < currentDistance)
            {
                best = site;
            }
        }

        private static IntVec3 ClampToBuildSearchBounds(Map map, IntVec3 cell)
        {
            int minX = Mathf.Min(VillageFootprintRadius + 3, Mathf.Max(0, map.Size.x / 2));
            int minZ = Mathf.Min(VillageFootprintRadius + 3, Mathf.Max(0, map.Size.z / 2));
            int maxX = Mathf.Max(minX, map.Size.x - VillageFootprintRadius - 4);
            int maxZ = Mathf.Max(minZ, map.Size.z - VillageFootprintRadius - 4);
            return new IntVec3(Mathf.Clamp(cell.x, minX, maxX), 0, Mathf.Clamp(cell.z, minZ, maxZ));
        }

        private static VillageSite ScoreVillageSite(Map map, IntVec3 origin)
        {
            VillageSite site = new VillageSite(origin);
            for (int x = -VillageFootprintRadius; x <= VillageFootprintRadius; x++)
            {
                for (int z = -VillageFootprintRadius; z <= VillageFootprintRadius; z++)
                {
                    IntVec3 cell = Cell(origin, x, z);
                    float weight = Mathf.Abs(x) <= 13 && Mathf.Abs(z) <= 13 ? 1.65f : 1f;
                    if (!cell.InBounds(map))
                    {
                        site.score -= 12f * weight;
                        site.blockedCells++;
                        continue;
                    }

                    TerrainDef terrain = cell.GetTerrain(map);
                    if (HasUnclearableEdifice(map, cell))
                    {
                        site.score -= 9f * weight;
                        site.blockedCells++;
                        continue;
                    }

                    if (TerrainSupports(terrain, "Heavy"))
                    {
                        site.score += 3.2f * weight;
                        site.heavyCells++;
                        site.mediumCells++;
                        site.lightCells++;
                    }
                    else if (TerrainSupports(terrain, "Medium"))
                    {
                        site.score += 2.5f * weight;
                        site.mediumCells++;
                        site.lightCells++;
                    }
                    else if (TerrainSupports(terrain, "Light"))
                    {
                        site.score += 1.3f * weight;
                        site.lightCells++;
                    }
                    else if (CanBridgeCell(map, cell))
                    {
                        site.score += 0.45f * weight;
                        site.bridgeableCells++;
                    }
                    else
                    {
                        site.score -= 4.5f * weight;
                        site.blockedCells++;
                    }
                }
            }

            return site;
        }

        private static void ClearVillageArea(Map map, IntVec3 origin)
        {
            ForRect(origin, -VillageFootprintRadius, -VillageFootprintRadius, VillageFootprintRadius, VillageFootprintRadius, cell =>
            {
                if (!cell.InBounds(map))
                {
                    return;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing thing = things[i];
                    if (IsClearableVillageBlocker(thing))
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }
            });
        }

        private static void PrepareVillageGround(Map map, IntVec3 origin, bool fullVillage)
        {
            TerrainDef packedDirt = Terrain("PackedDirt");
            TerrainDef straw = Terrain("StrawMatting");
            TerrainDef plazaTerrain = straw ?? packedDirt;
            int gateX = fullVillage ? 21 : 16;
            int gateZ = fullVillage ? 18 : 13;

            LayVillagePlaza(map, origin, plazaTerrain);
            LayVillagePath(map, origin, Cell(origin, -9, -9), 1, packedDirt);
            LayVillagePath(map, origin, Cell(origin, 9, -9), 1, packedDirt);
            LayVillagePath(map, origin, Cell(origin, 0, 12), 1, packedDirt);
            LayVillagePath(map, origin, Cell(origin, -7, 7), 1, packedDirt);
            LayVillagePath(map, origin, Cell(origin, 7, 7), 1, packedDirt);
            LayVillagePath(map, origin, Cell(origin, 0, -gateZ), 1, packedDirt);
            LayVillagePath(map, origin, Cell(origin, 0, gateZ), 1, packedDirt);
            LayVillagePath(map, origin, Cell(origin, -gateX, 0), 1, packedDirt);
            LayVillagePath(map, origin, Cell(origin, gateX, 0), 1, packedDirt);

            if (fullVillage)
            {
                LayVillagePath(map, origin, Cell(origin, -11, 0), 1, packedDirt);
                LayVillagePath(map, origin, Cell(origin, 11, 0), 1, packedDirt);
                LayVillagePath(map, origin, Cell(origin, -15, 8), 1, packedDirt);
                LayVillagePath(map, origin, Cell(origin, 15, 8), 1, packedDirt);
                LayVillagePath(map, origin, Cell(origin, 0, -15), 1, packedDirt);
            }
        }

        private static void BuildCoreVillage(Map map, IntVec3 origin, bool fullVillage)
        {
            BuildCourtyard(map, origin);

            TryBuildSleepingHut(map, origin, -9, -5, "western sleeping hut");
            TryBuildSleepingHut(map, origin, 9, -5, "eastern sleeping hut");
            TryBuildWorkshopHut(map, origin, 0, 8);
            TryBuildStorageHut(map, origin, -11, 7);
            TryBuildKitchenHut(map, origin, 11, 7);

            if (fullVillage)
            {
                TryBuildInfirmaryHut(map, origin, -15, 0);
                TryBuildPowerShed(map, origin, 15, 0);
                TryBuildArmoryHut(map, origin, -15, 10);
                TryBuildCommsHut(map, origin, 15, 10);
                TryBuildWatchPost(map, origin, 0, -15);
            }

            BuildFallbackCampIfSparse(map, origin);
        }

        private static void TryBuildSleepingHut(Map map, IntVec3 origin, int centerX, int centerZ, string label)
        {
            if (TryBuildRoom(map, origin, centerX, centerZ, 7, 7, DoorSide.South, "StrawMatting", roomOrigin =>
            {
                ThingDef leather = Thing("Leather_Plain");
                ThingDef wood = Thing("WoodLog");
                TrySpawnBuildingNear(Thing("Bedroll"), map, Cell(roomOrigin, -1, -1), Rot4.South, 3, leather);
                TrySpawnBuildingNear(Thing("Bedroll"), map, Cell(roomOrigin, 2, -1), Rot4.South, 3, leather);
                TrySpawnBuildingNear(Thing("Stool"), map, Cell(roomOrigin, -2, 1), Rot4.North, 3, wood);
                TrySpawnBuildingNear(Thing("TorchLamp"), map, Cell(roomOrigin, 2, 1), Rot4.North, 3, wood);
                TrySpawnBuildingNear(Thing("PassiveCooler"), map, Cell(roomOrigin, -2, -2), Rot4.North, 3, wood);
            }))
            {
                return;
            }

            BuildOpenCampCluster(map, Cell(origin, centerX, centerZ), label);
        }

        private static void TryBuildWorkshopHut(Map map, IntVec3 origin, int centerX, int centerZ)
        {
            if (TryBuildRoom(map, origin, centerX, centerZ, 9, 7, DoorSide.North, "WoodPlankFloor", roomOrigin =>
            {
                ThingDef wood = Thing("WoodLog");
                ThingDef steel = Thing("Steel");
                TrySpawnBuildingNear(Thing("SimpleResearchBench"), map, Cell(roomOrigin, -2, 0), Rot4.East, 3, wood);
                TrySpawnBuildingNear(Thing("FueledSmithy"), map, Cell(roomOrigin, 2, 0), Rot4.West, 3, steel);
                TrySpawnBuildingNear(Thing("HandTailoringBench"), map, Cell(roomOrigin, 0, 2), Rot4.South, 3, wood);
                TrySpawnBuildingNear(Thing("Shelf"), map, Cell(roomOrigin, -3, 2), Rot4.North, 3, wood);
                TrySpawnBuildingNear(Thing("TorchLamp"), map, Cell(roomOrigin, 3, 2), Rot4.North, 3, wood);
            }))
            {
                return;
            }

            BuildOpenWorkshop(map, Cell(origin, centerX, centerZ));
        }

        private static void TryBuildStorageHut(Map map, IntVec3 origin, int centerX, int centerZ)
        {
            if (TryBuildRoom(map, origin, centerX, centerZ, 7, 7, DoorSide.East, "StrawMatting", roomOrigin =>
            {
                ThingDef wood = Thing("WoodLog");
                TrySpawnBuildingNear(Thing("Shelf"), map, Cell(roomOrigin, -2, -1), Rot4.North, 3, wood);
                TrySpawnBuildingNear(Thing("Shelf"), map, Cell(roomOrigin, -2, 1), Rot4.North, 3, wood);
                TrySpawnBuildingNear(Thing("Shelf"), map, Cell(roomOrigin, 1, 1), Rot4.East, 3, wood);
                TrySpawnBuildingNear(Thing("TorchLamp"), map, Cell(roomOrigin, 2, -2), Rot4.North, 3, wood);
            }))
            {
                return;
            }

            TrySpawnBuildingNear(Thing("Shelf"), map, Cell(origin, centerX, centerZ), Rot4.North, 6, Thing("WoodLog"), true);
        }

        private static void TryBuildKitchenHut(Map map, IntVec3 origin, int centerX, int centerZ)
        {
            if (TryBuildRoom(map, origin, centerX, centerZ, 7, 7, DoorSide.West, "StrawMatting", roomOrigin =>
            {
                ThingDef wood = Thing("WoodLog");
                TrySpawnBuildingNear(Thing("TableButcher"), map, Cell(roomOrigin, -1, 0), Rot4.East, 3, wood);
                TrySpawnBuildingNear(Thing("FueledStove"), map, Cell(roomOrigin, 2, 0), Rot4.West, 3, wood);
                TrySpawnBuildingNear(Thing("Shelf"), map, Cell(roomOrigin, 0, 2), Rot4.North, 3, wood);
                TrySpawnBuildingNear(Thing("TorchLamp"), map, Cell(roomOrigin, -2, -2), Rot4.North, 3, wood);
            }))
            {
                return;
            }

            TrySpawnBuildingNear(Thing("Campfire"), map, Cell(origin, centerX, centerZ), Rot4.North, 6, null, true);
        }

        private static void TryBuildInfirmaryHut(Map map, IntVec3 origin, int centerX, int centerZ)
        {
            TryBuildRoom(map, origin, centerX, centerZ, 7, 6, DoorSide.East, "StrawMatting", roomOrigin =>
            {
                ThingDef leather = Thing("Leather_Plain");
                ThingDef wood = Thing("WoodLog");
                TrySpawnBuildingNear(Thing("Bedroll"), map, Cell(roomOrigin, -1, 0), Rot4.South, 3, leather);
                TrySpawnBuildingNear(Thing("Bedroll"), map, Cell(roomOrigin, 2, 0), Rot4.South, 3, leather);
                TrySpawnBuildingNear(Thing("Shelf"), map, Cell(roomOrigin, -2, 2), Rot4.North, 3, wood);
                TrySpawnBuildingNear(Thing("TorchLamp"), map, Cell(roomOrigin, 2, 2), Rot4.North, 3, wood);
            });
        }

        private static void TryBuildPowerShed(Map map, IntVec3 origin, int centerX, int centerZ)
        {
            TryBuildRoom(map, origin, centerX, centerZ, 8, 6, DoorSide.West, "WoodPlankFloor", roomOrigin =>
            {
                ThingDef steel = Thing("Steel");
                TrySpawnBuildingNear(Thing("WoodFiredGenerator"), map, Cell(roomOrigin, -2, 0), Rot4.East, 3, steel);
                TrySpawnBuildingNear(Thing("Battery"), map, Cell(roomOrigin, 2, 0), Rot4.North, 3, steel);
                TrySpawnBuildingNear(Thing("StandingLamp"), map, Cell(roomOrigin, 1, 2), Rot4.North, 3, steel);
            });
        }

        private static void TryBuildArmoryHut(Map map, IntVec3 origin, int centerX, int centerZ)
        {
            TryBuildRoom(map, origin, centerX, centerZ, 6, 6, DoorSide.South, "StrawMatting", roomOrigin =>
            {
                ThingDef wood = Thing("WoodLog");
                TrySpawnBuildingNear(Thing("Shelf"), map, Cell(roomOrigin, -1, 0), Rot4.North, 3, wood);
                TrySpawnBuildingNear(Thing("Shelf"), map, Cell(roomOrigin, 1, 0), Rot4.North, 3, wood);
                TrySpawnBuildingNear(Thing("TorchLamp"), map, Cell(roomOrigin, 0, 2), Rot4.North, 3, wood);
            });
        }

        private static void TryBuildCommsHut(Map map, IntVec3 origin, int centerX, int centerZ)
        {
            TryBuildRoom(map, origin, centerX, centerZ, 6, 6, DoorSide.South, "WoodPlankFloor", roomOrigin =>
            {
                ThingDef steel = Thing("Steel");
                ThingDef wood = Thing("WoodLog");
                TrySpawnBuildingNear(Thing("CommsConsole"), map, Cell(roomOrigin, -1, 0), Rot4.South, 3, steel);
                TrySpawnBuildingNear(Thing("Shelf"), map, Cell(roomOrigin, 2, 1), Rot4.North, 3, wood);
                TrySpawnBuildingNear(Thing("StandingLamp"), map, Cell(roomOrigin, 0, 2), Rot4.North, 3, steel);
            });
        }

        private static void TryBuildWatchPost(Map map, IntVec3 origin, int centerX, int centerZ)
        {
            IntVec3 preferred = Cell(origin, centerX, centerZ);
            IntVec3 post;
            if (!TryFindNearbyCellFor(Thing("Barricade"), map, preferred, Rot4.North, Thing("WoodLog"), 8, false, out post))
            {
                return;
            }

            for (int x = -3; x <= 3; x++)
            {
                TrySpawnBuildingAt(Thing("Barricade"), map, Cell(post, x, 0), Rot4.North, Thing("WoodLog"), false);
            }

            TrySpawnBuildingNear(Thing("TorchLamp"), map, Cell(post, 0, 1), Rot4.North, 4, Thing("WoodLog"), false);
        }

        private static bool TryBuildRoom(Map map, IntVec3 origin, int preferredX, int preferredZ, int width, int height, DoorSide doorSide, string floorDefName, Action<IntVec3> populate)
        {
            IntVec3 roomOrigin;
            if (!TryFindNearbyRoomOrigin(map, Cell(origin, preferredX, preferredZ), width, height, doorSide, 8, out roomOrigin))
            {
                return false;
            }

            ThingDef wall = Thing("Wall");
            ThingDef door = Thing("Door");
            ThingDef wood = Thing("WoodLog");
            TerrainDef floor = Terrain(floorDefName) ?? Terrain("StrawMatting");
            int minX = -width / 2;
            int maxX = minX + width - 1;
            int minZ = -height / 2;
            int maxZ = minZ + height - 1;
            IntVec3 doorOffset = DoorOffset(width, height, doorSide);
            Rot4 doorRot = DoorRotation(doorSide);

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    IntVec3 cell = Cell(roomOrigin, x, z);
                    bool border = x == minX || x == maxX || z == minZ || z == maxZ;
                    if (!border)
                    {
                        if (CanPlaceTerrain(map, cell, floor))
                        {
                            SetTerrain(map, cell, floor);
                        }
                        SetRoof(map, cell);
                        continue;
                    }

                    bool isDoor = x == doorOffset.x && z == doorOffset.z;
                    TrySpawnBuildingAt(isDoor ? door : wall, map, cell, isDoor ? doorRot : Rot4.North, wood, false);
                }
            }

            populate?.Invoke(roomOrigin);
            return true;
        }

        private static bool TryFindNearbyRoomOrigin(Map map, IntVec3 preferred, int width, int height, DoorSide doorSide, int radius, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            float bestScore = float.MinValue;
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    IntVec3 candidate = Cell(preferred, x, z);
                    if (!candidate.InBounds(map) || !CanSupportRoom(map, candidate, width, height, doorSide))
                    {
                        continue;
                    }

                    float distance = Mathf.Abs(x) + Mathf.Abs(z);
                    float score = 100f - distance;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        result = candidate;
                    }
                }
            }

            return result.IsValid;
        }

        private static bool CanSupportRoom(Map map, IntVec3 roomOrigin, int width, int height, DoorSide doorSide)
        {
            ThingDef wall = Thing("Wall");
            ThingDef door = Thing("Door");
            ThingDef wood = Thing("WoodLog");
            int minX = -width / 2;
            int maxX = minX + width - 1;
            int minZ = -height / 2;
            int maxZ = minZ + height - 1;
            IntVec3 doorOffset = DoorOffset(width, height, doorSide);

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    IntVec3 cell = Cell(roomOrigin, x, z);
                    bool border = x == minX || x == maxX || z == minZ || z == maxZ;
                    if (border)
                    {
                        bool isDoor = x == doorOffset.x && z == doorOffset.z;
                        if (!CanPlaceBuildableAt(isDoor ? door : wall, map, cell, isDoor ? DoorRotation(doorSide) : Rot4.North, wood, false, false))
                        {
                            return false;
                        }
                    }
                    else if (!cell.InBounds(map) || HasAnyEdifice(map, cell) || !CanPrepareCellForWalking(map, cell, false, false))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void BuildOpenCampCluster(Map map, IntVec3 preferred, string label)
        {
            IntVec3 camp;
            if (!TryFindNearbyLightCell(map, preferred, 8, out camp))
            {
                return;
            }

            ThingDef leather = Thing("Leather_Plain");
            ThingDef wood = Thing("WoodLog");
            TrySpawnBuildingNear(Thing("Bedroll"), map, Cell(camp, -1, 0), Rot4.South, 3, leather, true);
            TrySpawnBuildingNear(Thing("Bedroll"), map, Cell(camp, 1, 0), Rot4.South, 3, leather, true);
            TrySpawnBuildingNear(Thing("TorchLamp"), map, Cell(camp, 0, 2), Rot4.North, 3, wood, true);
            TrySpawnBuildingNear(Thing("Stool"), map, Cell(camp, -2, 1), Rot4.North, 3, wood, true);
        }

        private static void BuildOpenWorkshop(Map map, IntVec3 preferred)
        {
            IntVec3 center;
            if (!TryFindNearbyLightCell(map, preferred, 8, out center))
            {
                return;
            }

            ThingDef wood = Thing("WoodLog");
            ThingDef steel = Thing("Steel");
            TrySpawnBuildingNear(Thing("SimpleResearchBench"), map, Cell(center, -1, 0), Rot4.East, 4, wood, false);
            TrySpawnBuildingNear(Thing("FueledSmithy"), map, Cell(center, 2, 0), Rot4.West, 4, steel, false);
            TrySpawnBuildingNear(Thing("TorchLamp"), map, Cell(center, 0, 2), Rot4.North, 4, wood, true);
        }

        private static void BuildFallbackCampIfSparse(Map map, IntVec3 origin)
        {
            TrySpawnBuildingNear(Thing("Campfire"), map, origin, Rot4.North, 7, null, true);
            TrySpawnBuildingNear(Thing("Table1x2c"), map, Cell(origin, -2, 1), Rot4.East, 7, Thing("WoodLog"), true);
            TrySpawnBuildingNear(Thing("Stool"), map, Cell(origin, -4, 1), Rot4.North, 7, Thing("WoodLog"), true);
            TrySpawnBuildingNear(Thing("Stool"), map, Cell(origin, 1, 1), Rot4.North, 7, Thing("WoodLog"), true);
        }

        private static void BuildCourtyard(Map map, IntVec3 origin)
        {
            ThingDef wood = Thing("WoodLog");
            TrySpawnBuildingNear(Thing("Campfire"), map, origin, Rot4.North, 5, null, true);
            TrySpawnBuildingNear(Thing("Table1x2c"), map, Cell(origin, -2, 1), Rot4.East, 5, wood, true);
            TrySpawnBuildingNear(Thing("Stool"), map, Cell(origin, -4, 1), Rot4.North, 5, wood, true);
            TrySpawnBuildingNear(Thing("Stool"), map, Cell(origin, 1, 1), Rot4.North, 5, wood, true);
            TrySpawnBuildingNear(Thing("HoopstoneRing"), map, Cell(origin, 6, 3), Rot4.North, 7, wood, true);
            TrySpawnBuildingNear(Thing("HorseshoesPin"), map, Cell(origin, -6, 4), Rot4.North, 7, wood, true);
            TrySpawnBuildingNear(Thing("PlantPot"), map, Cell(origin, 4, -2), Rot4.North, 5, wood, true);
            TrySpawnBuildingNear(Thing("TorchLamp"), map, Cell(origin, -5, -7), Rot4.North, 8, wood, true);
            TrySpawnBuildingNear(Thing("TorchLamp"), map, Cell(origin, 5, -7), Rot4.North, 8, wood, true);
        }

        private static void BuildAdaptivePerimeter(Map map, IntVec3 origin, bool fullVillage)
        {
            int west = fullVillage ? -20 : -15;
            int east = fullVillage ? 20 : 15;
            int north = fullVillage ? 17 : 12;
            int south = fullVillage ? -17 : -12;
            ThingDef wall = Thing("Wall");
            ThingDef door = Thing("Door");
            ThingDef barricade = Thing("Barricade");
            ThingDef sandbags = Thing("Sandbags");
            ThingDef wood = Thing("WoodLog");
            ThingDef cloth = Thing("Cloth");

            for (int x = west; x <= east; x++)
            {
                bool gate = Mathf.Abs(x) <= 1;
                TrySpawnPerimeterPiece(map, Cell(origin, x, south), gate ? door : wall, gate ? Rot4.South : Rot4.North, wood, barricade, sandbags, cloth);
                TrySpawnPerimeterPiece(map, Cell(origin, x, north), gate ? door : wall, gate ? Rot4.North : Rot4.North, wood, barricade, sandbags, cloth);
            }

            for (int z = south + 1; z <= north - 1; z++)
            {
                bool gate = Mathf.Abs(z) <= 1;
                TrySpawnPerimeterPiece(map, Cell(origin, west, z), gate ? door : wall, gate ? Rot4.West : Rot4.North, wood, barricade, sandbags, cloth);
                TrySpawnPerimeterPiece(map, Cell(origin, east, z), gate ? door : wall, gate ? Rot4.East : Rot4.North, wood, barricade, sandbags, cloth);
            }
        }

        private static void TrySpawnPerimeterPiece(Map map, IntVec3 cell, ThingDef preferredDef, Rot4 rot, ThingDef wallStuff, ThingDef barricade, ThingDef sandbags, ThingDef cloth)
        {
            if (TrySpawnBuildingAt(preferredDef, map, cell, rot, wallStuff, false))
            {
                return;
            }

            if (TrySpawnBuildingAt(barricade, map, cell, rot, wallStuff, true))
            {
                return;
            }

            TrySpawnBuildingAt(sandbags, map, cell, rot, cloth, true);
        }

        private static void BuildFields(Map map, IntVec3 origin)
        {
            TerrainDef soil = Terrain("SoilRich") ?? Terrain("Soil");
            if (soil == null)
            {
                return;
            }

            PlantPatch(map, origin, -18, 18, -10, 22, "Plant_Potato", soil);
            PlantPatch(map, origin, 10, 18, 18, 22, "Plant_Rice", soil);
            PlantPatch(map, origin, -9, -22, -4, -18, "Plant_Corn", soil);
            PlantPatch(map, origin, 4, -22, 9, -18, "Plant_Corn", soil);
        }

        private static void PlantPatch(Map map, IntVec3 origin, int minX, int minZ, int maxX, int maxZ, string plantDefName, TerrainDef soil)
        {
            ThingDef plant = Thing(plantDefName);
            ForRect(origin, minX, minZ, maxX, maxZ, cell =>
            {
                if (!cell.InBounds(map) || HasAnyEdifice(map, cell))
                {
                    return;
                }

                TerrainDef current = cell.GetTerrain(map);
                if (!TerrainSupports(current, "GrowSoil") && !TerrainSupports(current, "Medium"))
                {
                    return;
                }

                SetTerrain(map, cell, soil);
                SpawnPlant(plant, map, cell);
            });
        }

        private static void PlaceStartingCaches(Map map, IntVec3 origin)
        {
            PlaceStackNear(Thing("Pemmican"), map, Cell(origin, -1, 9), 360);
            PlaceStackNear(Thing("MedicineIndustrial"), map, Cell(origin, 1, 9), 18);
            PlaceStackNear(Thing("ComponentIndustrial"), map, Cell(origin, 3, 9), 16);
            PlaceStackNear(Thing("Steel"), map, Cell(origin, 5, 8), 220);
            PlaceStackNear(Thing("WoodLog"), map, Cell(origin, -5, 8), 320);
            PlaceStackNear(Thing("Cloth"), map, Cell(origin, -7, 8), 120);
            PlaceStackNear(Thing("Leather_Plain"), map, Cell(origin, -8, 7), 90);
            PlaceStackNear(Thing("Gun_BoltActionRifle"), map, Cell(origin, 0, 5), 1);
            PlaceStackNear(Thing("Gun_Revolver"), map, Cell(origin, 2, 5), 1);
            PlaceStackNear(Thing("Bow_Short"), map, Cell(origin, -2, 5), 1);
        }

        private static bool TrySpawnBuildingNear(ThingDef def, Map map, IntVec3 preferred, Rot4 rot, int radius, ThingDef stuff = null, bool allowBridge = false)
        {
            IntVec3 cell;
            return TryFindNearbyCellFor(def, map, preferred, rot, stuff, radius, allowBridge, out cell)
                && TrySpawnBuildingAt(def, map, cell, rot, stuff, allowBridge);
        }

        private static bool TryFindNearbyCellFor(ThingDef def, Map map, IntVec3 preferred, Rot4 rot, ThingDef stuff, int radius, bool allowBridge, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            float bestScore = float.MinValue;
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    IntVec3 candidate = Cell(preferred, x, z);
                    if (!CanPlaceBuildableAt(def, map, candidate, rot, stuff, allowBridge, false))
                    {
                        continue;
                    }

                    TerrainDef terrain = candidate.GetTerrain(map);
                    float score = 100f - Mathf.Abs(x) - Mathf.Abs(z);
                    if (TerrainSupports(terrain, "Medium"))
                    {
                        score += 10f;
                    }
                    else if (CanBridgeCell(map, candidate))
                    {
                        score -= 5f;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        result = candidate;
                    }
                }
            }

            return result.IsValid;
        }

        private static bool TryFindNearbyLightCell(Map map, IntVec3 preferred, int radius, out IntVec3 result)
        {
            return TryFindNearbyCellFor(Thing("TorchLamp"), map, preferred, Rot4.North, Thing("WoodLog"), radius, true, out result);
        }

        private static bool TrySpawnBuildingAt(ThingDef def, Map map, IntVec3 cell, Rot4 rot, ThingDef stuff = null, bool allowBridge = false)
        {
            if (!CanPlaceBuildableAt(def, map, cell, rot, stuff, allowBridge, true))
            {
                return false;
            }

            ClearFootprint(map, cell, rot, def);
            ThingDef resolvedStuff = def.MadeFromStuff ? stuff ?? Thing("WoodLog") : null;
            Thing thing = ThingMaker.MakeThing(def, resolvedStuff);
            if (thing.def.CanHaveFaction)
            {
                thing.SetFactionDirect(Faction.OfPlayer);
            }

            Thing spawned = GenSpawn.Spawn(thing, cell, map, rot, WipeMode.Vanish, false, false);
            ClaimAndUnforbid(spawned);
            return true;
        }

        private static bool CanPlaceBuildableAt(ThingDef def, Map map, IntVec3 center, Rot4 rot, ThingDef stuff, bool allowBridge, bool prepareTerrain)
        {
            if (def == null || map == null || !center.InBounds(map))
            {
                return false;
            }

            foreach (IntVec3 cell in FootprintCells(center, rot, def))
            {
                if (!cell.InBounds(map) || HasAnyEdifice(map, cell))
                {
                    return false;
                }

                if (!CanPrepareCellForBuildable(map, cell, def, stuff, allowBridge, prepareTerrain))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CanPrepareCellForBuildable(Map map, IntVec3 cell, ThingDef def, ThingDef stuff, bool allowBridge, bool prepareTerrain)
        {
            TerrainAffordanceDef required = RequiredAffordance(def, stuff);
            TerrainDef terrain = cell.GetTerrain(map);
            if (TerrainSupports(terrain, required))
            {
                return true;
            }

            if (!allowBridge || !CanUseBridgeFor(required) || !CanBridgeCell(map, cell))
            {
                return false;
            }

            TerrainDef bridge = Terrain("Bridge");
            if (bridge == null || !TerrainSupports(bridge, required))
            {
                return false;
            }

            if (prepareTerrain)
            {
                SetTerrain(map, cell, bridge);
            }

            return true;
        }

        private static bool CanPrepareCellForWalking(Map map, IntVec3 cell, bool allowBridge, bool prepareTerrain)
        {
            if (!cell.InBounds(map) || HasAnyEdifice(map, cell))
            {
                return false;
            }

            TerrainDef terrain = cell.GetTerrain(map);
            if (TerrainSupports(terrain, "Walkable") || TerrainSupports(terrain, "Light"))
            {
                return true;
            }

            if (!allowBridge || !CanBridgeCell(map, cell))
            {
                return false;
            }

            TerrainDef bridge = Terrain("Bridge");
            if (bridge == null)
            {
                return false;
            }

            if (prepareTerrain)
            {
                SetTerrain(map, cell, bridge);
            }

            return true;
        }

        private static bool CanPlaceTerrain(Map map, IntVec3 cell, TerrainDef terrain)
        {
            if (map == null || terrain == null || !cell.InBounds(map) || HasAnyEdifice(map, cell))
            {
                return false;
            }

            TerrainAffordanceDef required = terrain.terrainAffordanceNeeded;
            TerrainDef current = cell.GetTerrain(map);
            return TerrainSupports(current, required);
        }

        private static void SpawnPlant(ThingDef plantDef, Map map, IntVec3 cell)
        {
            if (plantDef == null || map == null || !cell.InBounds(map) || HasAnyEdifice(map, cell))
            {
                return;
            }

            ClearFootprint(map, cell, Rot4.North, plantDef);
            Thing thing = ThingMaker.MakeThing(plantDef);
            Plant plant = thing as Plant;
            if (plant != null)
            {
                plant.Growth = Rand.Range(0.35f, 0.75f);
            }

            GenSpawn.Spawn(thing, cell, map, WipeMode.Vanish);
        }

        private static void PlaceStackNear(ThingDef def, Map map, IntVec3 preferred, int count)
        {
            if (def == null || map == null || count <= 0)
            {
                return;
            }

            IntVec3 cell;
            if (!TryFindNearbyLightCell(map, preferred, 10, out cell))
            {
                cell = preferred.InBounds(map) ? preferred : new IntVec3(map.Size.x / 2, 0, map.Size.z / 2);
            }

            int remaining = count;
            while (remaining > 0)
            {
                Thing thing = ThingMaker.MakeThing(def);
                int stackCount = Mathf.Min(remaining, thing.def.stackLimit);
                thing.stackCount = stackCount;
                if (GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near))
                {
                    ClaimAndUnforbid(thing);
                }
                remaining -= stackCount;
            }
        }

        private static void LayVillagePlaza(Map map, IntVec3 origin, TerrainDef terrain)
        {
            ForRect(origin, -4, -3, 4, 3, cell => TrySetVillageWalkSurface(map, cell, terrain));
            ThingDef torch = Thing("TorchLamp");
            ThingDef wood = Thing("WoodLog");
            TrySpawnBuildingNear(torch, map, Cell(origin, -4, -3), Rot4.North, 2, wood, true);
            TrySpawnBuildingNear(torch, map, Cell(origin, 4, -3), Rot4.North, 2, wood, true);
            TrySpawnBuildingNear(torch, map, Cell(origin, -4, 3), Rot4.North, 2, wood, true);
            TrySpawnBuildingNear(torch, map, Cell(origin, 4, 3), Rot4.North, 2, wood, true);
        }

        private static void LayVillagePath(Map map, IntVec3 start, IntVec3 end, int halfWidth, TerrainDef terrain)
        {
            IntVec3 corner = new IntVec3(end.x, 0, start.z);
            LayStraightVillagePath(map, start, corner, halfWidth, terrain);
            LayStraightVillagePath(map, corner, end, halfWidth, terrain);
        }

        private static void LayStraightVillagePath(Map map, IntVec3 start, IntVec3 end, int halfWidth, TerrainDef terrain)
        {
            if (map == null || !start.IsValid || !end.IsValid)
            {
                return;
            }

            int minX = Mathf.Min(start.x, end.x);
            int maxX = Mathf.Max(start.x, end.x);
            int minZ = Mathf.Min(start.z, end.z);
            int maxZ = Mathf.Max(start.z, end.z);
            if (start.x == end.x)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    for (int x = start.x - halfWidth; x <= start.x + halfWidth; x++)
                    {
                        TrySetVillageWalkSurface(map, new IntVec3(x, 0, z), terrain);
                    }
                }
                return;
            }

            if (start.z == end.z)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = start.z - halfWidth; z <= start.z + halfWidth; z++)
                    {
                        TrySetVillageWalkSurface(map, new IntVec3(x, 0, z), terrain);
                    }
                }
                return;
            }

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    TrySetVillageWalkSurface(map, new IntVec3(x, 0, z), terrain);
                }
            }
        }

        private static void TrySetVillageWalkSurface(Map map, IntVec3 cell, TerrainDef terrain)
        {
            if (map == null || !cell.InBounds(map) || HasAnyEdifice(map, cell))
            {
                return;
            }

            TerrainDef bridge = Terrain("Bridge");
            if (CanBridgeCell(map, cell) && bridge != null)
            {
                SetTerrain(map, cell, bridge);
                return;
            }

            if (CanPlaceTerrain(map, cell, terrain))
            {
                SetTerrain(map, cell, terrain);
                return;
            }

            TerrainDef packedDirt = Terrain("PackedDirt");
            if (CanPlaceTerrain(map, cell, packedDirt))
            {
                SetTerrain(map, cell, packedDirt);
            }
        }

        private static void ClaimAndUnforbid(Thing thing)
        {
            if (thing == null || thing.Destroyed)
            {
                return;
            }

            if (thing.def.CanHaveFaction && thing.Faction == null)
            {
                thing.SetFactionDirect(Faction.OfPlayer);
            }

            ForbidUtility.SetForbidden(thing, false, false);
        }

        private static void ClearFootprint(Map map, IntVec3 center, Rot4 rot, ThingDef def)
        {
            foreach (IntVec3 cell in FootprintCells(center, rot, def))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing thing = things[i];
                    if (IsClearableVillageBlocker(thing))
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        private static IEnumerable<IntVec3> FootprintCells(IntVec3 center, Rot4 rot, ThingDef def)
        {
            IntVec2 size = def == null ? IntVec2.One : def.size;
            int width = rot == Rot4.East || rot == Rot4.West ? size.z : size.x;
            int height = rot == Rot4.East || rot == Rot4.West ? size.x : size.z;
            int minX = -width / 2;
            int minZ = -height / 2;
            for (int x = minX; x < minX + width; x++)
            {
                for (int z = minZ; z < minZ + height; z++)
                {
                    yield return Cell(center, x, z);
                }
            }
        }

        private static bool IsClearableVillageBlocker(Thing thing)
        {
            if (thing == null || thing.Destroyed || thing is Pawn)
            {
                return false;
            }

            if (thing.def.category == ThingCategory.Plant || thing.def.category == ThingCategory.Filth || thing.def.category == ThingCategory.Item)
            {
                return true;
            }

            return thing.def.category == ThingCategory.Building && thing.def.building != null && !thing.def.building.isNaturalRock;
        }

        private static bool HasUnclearableEdifice(Map map, IntVec3 cell)
        {
            Thing edifice = cell.GetEdifice(map);
            return edifice != null && !IsClearableVillageBlocker(edifice);
        }

        private static bool HasAnyEdifice(Map map, IntVec3 cell)
        {
            return map != null && cell.InBounds(map) && cell.GetEdifice(map) != null;
        }

        private static bool CanBridgeCell(Map map, IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map) || HasAnyEdifice(map, cell))
            {
                return false;
            }

            return TerrainSupports(cell.GetTerrain(map), "Bridgeable");
        }

        private static bool CanUseBridgeFor(TerrainAffordanceDef required)
        {
            return required == null || AffordanceRank(required) <= AffordanceRank("Light");
        }

        private static TerrainAffordanceDef RequiredAffordance(ThingDef def, ThingDef stuff)
        {
            if (def == null)
            {
                return null;
            }

            TerrainAffordanceDef required = def.terrainAffordanceNeeded;
            if (stuff != null && def.useStuffTerrainAffordance)
            {
                required = MaxAffordance(required, stuff.terrainAffordanceNeeded);
            }

            return required;
        }

        private static TerrainAffordanceDef MaxAffordance(TerrainAffordanceDef a, TerrainAffordanceDef b)
        {
            if (a == null)
            {
                return b;
            }

            if (b == null)
            {
                return a;
            }

            return AffordanceRank(b) > AffordanceRank(a) ? b : a;
        }

        private static bool TerrainSupports(TerrainDef terrain, string requiredDefName)
        {
            TerrainAffordanceDef required = DefDatabase<TerrainAffordanceDef>.GetNamedSilentFail(requiredDefName);
            return TerrainSupports(terrain, required);
        }

        private static bool TerrainSupports(TerrainDef terrain, TerrainAffordanceDef required)
        {
            if (terrain == null)
            {
                return false;
            }

            if (required == null)
            {
                return true;
            }

            if (terrain.affordances == null)
            {
                return false;
            }

            int requiredRank = AffordanceRank(required);
            for (int i = 0; i < terrain.affordances.Count; i++)
            {
                TerrainAffordanceDef affordance = terrain.affordances[i];
                if (affordance == required || affordance.defName == required.defName)
                {
                    return true;
                }

                if (requiredRank > 0 && AffordanceRank(affordance) >= requiredRank)
                {
                    return true;
                }
            }

            return false;
        }

        private static int AffordanceRank(TerrainAffordanceDef affordance)
        {
            return affordance == null ? 0 : AffordanceRank(affordance.defName);
        }

        private static int AffordanceRank(string defName)
        {
            switch (defName)
            {
                case "Light":
                    return 1;
                case "Medium":
                    return 2;
                case "Heavy":
                    return 3;
                default:
                    return -1;
            }
        }

        private static IntVec3 DoorOffset(int width, int height, DoorSide side)
        {
            switch (side)
            {
                case DoorSide.North:
                    return new IntVec3(0, 0, height / 2);
                case DoorSide.South:
                    return new IntVec3(0, 0, -height / 2);
                case DoorSide.East:
                    return new IntVec3(width / 2, 0, 0);
                case DoorSide.West:
                    return new IntVec3(-width / 2, 0, 0);
                default:
                    return IntVec3.Zero;
            }
        }

        private static Rot4 DoorRotation(DoorSide side)
        {
            switch (side)
            {
                case DoorSide.North:
                    return Rot4.North;
                case DoorSide.South:
                    return Rot4.South;
                case DoorSide.East:
                    return Rot4.East;
                case DoorSide.West:
                    return Rot4.West;
                default:
                    return Rot4.North;
            }
        }

        private static void SetTerrain(Map map, IntVec3 cell, TerrainDef terrain)
        {
            if (map != null && terrain != null && cell.InBounds(map))
            {
                map.terrainGrid.SetTerrain(cell, terrain);
            }
        }

        private static void SetRoof(Map map, IntVec3 cell)
        {
            if (map != null && cell.InBounds(map))
            {
                map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
            }
        }

        private static ThingDef Thing(string defName)
        {
            return DefDatabase<ThingDef>.GetNamedSilentFail(defName);
        }

        private static TerrainDef Terrain(string defName)
        {
            return DefDatabase<TerrainDef>.GetNamedSilentFail(defName);
        }

        private static IntVec3 Cell(IntVec3 origin, int offsetX, int offsetZ)
        {
            return new IntVec3(origin.x + offsetX, 0, origin.z + offsetZ);
        }

        private static void ForRect(IntVec3 origin, int minX, int minZ, int maxX, int maxZ, Action<IntVec3> action)
        {
            if (action == null)
            {
                return;
            }

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    action(Cell(origin, x, z));
                }
            }
        }

        private enum DoorSide
        {
            North,
            South,
            East,
            West
        }

        private struct VillageSite
        {
            public readonly IntVec3 origin;
            public float score;
            public int lightCells;
            public int mediumCells;
            public int heavyCells;
            public int bridgeableCells;
            public int blockedCells;

            public VillageSite(IntVec3 origin)
            {
                this.origin = origin;
                score = 0f;
                lightCells = 0;
                mediumCells = 0;
                heavyCells = 0;
                bridgeableCells = 0;
                blockedCells = 0;
            }
        }
    }
}
