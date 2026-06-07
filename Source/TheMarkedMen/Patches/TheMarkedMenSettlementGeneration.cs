using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TheMarkedMen
{
    [HarmonyPatch(typeof(GenStep_Settlement), "ScatterAt")]
    public static class Patch_MarkedMenSettlementGeneration
    {
        private const int MinBuildings = 7;
        private const int MaxBuildings = 15;
        private const int MinBuildingSize = 5;
        private const int MaxBuildingSize = 8;
        private const int BuildingSlotSize = 10;
        private const int StreetWidth = 3;
        private const int OuterMargin = 2;
        private const int MapEdgeMargin = 12;
        private const string SettlementRectVarName = "SettlementRect";

        private static readonly List<LayoutSlot> tmpSlots = new List<LayoutSlot>();
        private static readonly List<BuildingPlan> tmpBuildings = new List<BuildingPlan>();

        public static bool Prefix(GenStep_Settlement __instance, IntVec3 c, Map map, GenStepParams parms, int stackCount)
        {
            if (__instance == null || map == null || !TryGetMarkedMenFaction(__instance, map, out Faction faction))
            {
                return true;
            }

            bool baseGenStarted = false;
            try
            {
                int targetBuildings = Rand.RangeInclusive(MinBuildings, MaxBuildings);
                if (!TryCreateLayout(c, map, targetBuildings, faction, out CellRect settlementRect))
                {
                    Log.Warning("[The Marked Men] Could not fit custom Marked Men settlement layout; falling back to vanilla settlement generation.");
                    return true;
                }

                List<Building> previousBuildings = null;
                if (__instance.postProcessSettlementParams != null)
                {
                    __instance.postProcessSettlementParams.faction = faction;
                    previousBuildings = new List<Building>(map.listerThings.GetThingsOfType<Building>());
                }

                MapGenerator.SetVar(SettlementRectVarName, settlementRect);
                BaseGen.globalSettings.map = map;
                BaseGen.globalSettings.mainRect = settlementRect;
                BaseGen.globalSettings.minBuildings = tmpBuildings.Count;
                BaseGen.globalSettings.minBarracks = 1;
                BaseGen.globalSettings.requiredGravcoreRooms = __instance.requiredGravcoreRooms;

                ResolveParams rootParams = new ResolveParams
                {
                    sitePart = parms.sitePart,
                    rect = settlementRect,
                    faction = faction,
                    settlementDontGeneratePawns = !__instance.generatePawns,
                    thingSetMakerDef = __instance.lootThingSetMaker,
                    lootMarketValue = __instance.lootMarketValue,
                    floorOnlyIfTerrainSupports = true,
                    allowBridgeOnAnyImpassableTerrain = true
                };

                BaseGen.symbolStack.Push("lootScatter", WithDefaultLoot(rootParams));
                PushPawnGroupIfNeeded(__instance, map, faction, rootParams);
                PushOutdoorSettlementSupport(rootParams);
                PushStreets(rootParams, settlementRect);
                PushBuildings(rootParams);
                PushTerrainPreparation(rootParams);

                baseGenStarted = true;
                BaseGen.Generate();

                if (previousBuildings != null)
                {
                    List<Building> placedBuildings = map.listerThings.GetThingsOfType<Building>()
                        .Where(building => !previousBuildings.Contains(building))
                        .ToList();
                    previousBuildings.Clear();
                    MapGenUtility.PostProcessSettlement(map, placedBuildings, __instance.postProcessSettlementParams);
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Custom Marked Men settlement generation failed"
                    + (baseGenStarted ? " after BaseGen started" : string.Empty) + ": " + ex);
                TryResetBaseGen();
                return !baseGenStarted;
            }
            finally
            {
                tmpSlots.Clear();
                tmpBuildings.Clear();
            }
        }

        private static bool TryGetMarkedMenFaction(GenStep_Settlement genStep, Map map, out Faction faction)
        {
            faction = genStep.overrideFaction;
            if (faction == null && map.ParentFaction != null && map.ParentFaction != Faction.OfPlayer)
            {
                faction = map.ParentFaction;
            }

            FactionDef markedDef = CADefOf.CrossedFaction;
            return faction?.def != null && (faction.def == markedDef || faction.def.defName == "CA_CrossedFaction");
        }

        private static bool TryCreateLayout(IntVec3 center, Map map, int targetBuildings, Faction faction, out CellRect settlementRect)
        {
            settlementRect = CellRect.Empty;
            CellRect bounds = map.BoundsRect(MapEdgeMargin);
            targetBuildings = Mathf.Clamp(targetBuildings, MinBuildings, MaxBuildings);

            for (int count = targetBuildings; count >= MinBuildings; count--)
            {
                int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
                int rows = Mathf.CeilToInt((float)count / columns);
                int width = LayoutLength(columns);
                int height = LayoutLength(rows);
                if (width > bounds.Width || height > bounds.Height)
                {
                    continue;
                }

                settlementRect = MakeCenteredRect(center, width, height, bounds);
                CreateBuildingPlans(settlementRect, columns, rows, count, faction);
                return tmpBuildings.Count >= MinBuildings && tmpBuildings.Count <= MaxBuildings;
            }

            return false;
        }

        private static int LayoutLength(int slots)
        {
            return OuterMargin * 2 + slots * BuildingSlotSize + Math.Max(0, slots - 1) * StreetWidth;
        }

        private static CellRect MakeCenteredRect(IntVec3 center, int width, int height, CellRect bounds)
        {
            int minX = center.x - width / 2;
            int minZ = center.z - height / 2;
            minX = Mathf.Clamp(minX, bounds.minX, bounds.maxX - width + 1);
            minZ = Mathf.Clamp(minZ, bounds.minZ, bounds.maxZ - height + 1);
            return new CellRect(minX, minZ, width, height);
        }

        private static void CreateBuildingPlans(CellRect settlementRect, int columns, int rows, int count, Faction faction)
        {
            tmpSlots.Clear();
            tmpBuildings.Clear();
            for (int z = 0; z < rows; z++)
            {
                for (int x = 0; x < columns; x++)
                {
                    tmpSlots.Add(new LayoutSlot(x, z));
                }
            }

            tmpSlots.Shuffle();
            int batteryRooms = 0;
            int breweries = 0;
            for (int i = 0; i < count && i < tmpSlots.Count; i++)
            {
                LayoutSlot slot = tmpSlots[i];
                int width = Rand.RangeInclusive(MinBuildingSize, MaxBuildingSize);
                int height = Rand.RangeInclusive(MinBuildingSize, MaxBuildingSize);
                int localX = OuterMargin + slot.X * (BuildingSlotSize + StreetWidth) + Rand.RangeInclusive(0, BuildingSlotSize - width);
                int localZ = OuterMargin + slot.Z * (BuildingSlotSize + StreetWidth) + Rand.RangeInclusive(0, BuildingSlotSize - height);
                CellRect rect = new CellRect(settlementRect.minX + localX, settlementRect.minZ + localZ, width, height);
                string symbol = ChooseRoomSymbol(i, faction, ref batteryRooms, ref breweries);
                int bedCount = symbol == "barracks" ? Rand.RangeInclusive(2, 4) : 0;
                tmpBuildings.Add(new BuildingPlan(rect, symbol, bedCount));
            }
        }

        private static string ChooseRoomSymbol(int index, Faction faction, ref int batteryRooms, ref int breweries)
        {
            switch (index)
            {
                case 0:
                case 1:
                case 5:
                    return "barracks";
                case 2:
                    return "diningRoom";
                case 3:
                case 4:
                case 6:
                    return "storage";
            }

            TechLevel techLevel = faction?.def?.techLevel ?? TechLevel.Undefined;
            if ((int)techLevel >= 4 && batteryRooms < 2 && Rand.Chance(0.22f))
            {
                batteryRooms++;
                return "batteryRoom";
            }

            if ((int)techLevel >= 3 && breweries < 1 && Rand.Chance(0.16f))
            {
                breweries++;
                return "brewery";
            }

            return Rand.Element("barracks", "storage", "diningRoom");
        }

        private static ResolveParams WithDefaultLoot(ResolveParams rootParams)
        {
            ResolveParams lootParams = rootParams;
            if (lootParams.thingSetMakerDef == null)
            {
                lootParams.thingSetMakerDef = ThingSetMakerDefOf.MapGen_DefaultStockpile;
            }

            if (!lootParams.lootMarketValue.HasValue)
            {
                lootParams.lootMarketValue = SymbolResolver_Settlement.DefaultLootMarketValue;
            }

            return lootParams;
        }

        private static void PushPawnGroupIfNeeded(GenStep_Settlement genStep, Map map, Faction faction, ResolveParams rootParams)
        {
            if (!genStep.generatePawns)
            {
                return;
            }

            ResolveParams pawnParams = rootParams;
            pawnParams.faction = faction;
            pawnParams.singlePawnLord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, rootParams.rect.CenterCell, 25000), map);
            pawnParams.pawnGroupKindDef = PawnGroupKindDefOf.Settlement;
            pawnParams.singlePawnSpawnCellExtraPredicate = cell => map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.PassDoors));
            pawnParams.pawnGroupMakerParams = new PawnGroupMakerParms
            {
                tile = map.Tile,
                faction = faction,
                points = SymbolResolver_Settlement.DefaultPawnsPoints.RandomInRange,
                inhabitants = true
            };

            int bedCount = PawnGroupMakerUtility.GeneratePawnKindsExample(SymbolResolver_PawnGroup.GetGroupMakerParms(map, pawnParams)).Count();
            for (int i = 0; i < tmpBuildings.Count && bedCount > 0; i++)
            {
                if (tmpBuildings[i].Symbol != "barracks")
                {
                    continue;
                }

                int bedsForRoom = Mathf.Clamp(bedCount, 2, 4);
                tmpBuildings[i] = tmpBuildings[i].WithBedCount(bedsForRoom);
                bedCount -= bedsForRoom;
            }

            BaseGen.symbolStack.Push("pawnGroup", pawnParams);
        }

        private static void PushOutdoorSettlementSupport(ResolveParams rootParams)
        {
            BaseGen.symbolStack.Push("outdoorLighting", rootParams);
            BaseGen.symbolStack.Push("ensureCanReachMapEdge", rootParams);
        }

        private static void PushStreets(ResolveParams rootParams, CellRect settlementRect)
        {
            TerrainDef pathFloor = BaseGenUtility.RandomBasicFloorDef(rootParams.faction);
            int columns = CountSlots(settlementRect.Width);
            int rows = CountSlots(settlementRect.Height);
            int gridMinX = settlementRect.minX + OuterMargin;
            int gridMinZ = settlementRect.minZ + OuterMargin;
            int gridWidth = columns * BuildingSlotSize + Math.Max(0, columns - 1) * StreetWidth;
            int gridHeight = rows * BuildingSlotSize + Math.Max(0, rows - 1) * StreetWidth;

            for (int x = 1; x < columns; x++)
            {
                ResolveParams streetParams = rootParams;
                streetParams.rect = new CellRect(gridMinX + x * BuildingSlotSize + (x - 1) * StreetWidth, gridMinZ, StreetWidth, gridHeight);
                streetParams.floorDef = pathFloor;
                streetParams.streetHorizontal = false;
                BaseGen.symbolStack.Push("street", streetParams);
            }

            for (int z = 1; z < rows; z++)
            {
                ResolveParams streetParams = rootParams;
                streetParams.rect = new CellRect(gridMinX, gridMinZ + z * BuildingSlotSize + (z - 1) * StreetWidth, gridWidth, StreetWidth);
                streetParams.floorDef = pathFloor;
                streetParams.streetHorizontal = true;
                BaseGen.symbolStack.Push("street", streetParams);
            }
        }

        private static int CountSlots(int layoutLength)
        {
            return Mathf.Max(1, (layoutLength - OuterMargin * 2 + StreetWidth) / (BuildingSlotSize + StreetWidth));
        }

        private static void PushBuildings(ResolveParams rootParams)
        {
            for (int i = 0; i < tmpBuildings.Count; i++)
            {
                BuildingPlan building = tmpBuildings[i];
                ResolveParams roomParams = rootParams;
                roomParams.rect = building.Rect;
                roomParams.wallStuff = BaseGenUtility.RandomCheapWallStuff(rootParams.faction);
                roomParams.floorDef = BaseGenUtility.RandomBasicFloorDef(rootParams.faction, allowCarpet: true);
                roomParams.floorOnlyIfTerrainSupports = false;
                roomParams.allowBridgeOnAnyImpassableTerrain = true;
                roomParams.clearRoof = true;
                if (building.BedCount > 0)
                {
                    roomParams.bedCount = building.BedCount;
                }

                BaseGen.symbolStack.Push(building.Symbol, roomParams);
            }
        }

        private static void PushTerrainPreparation(ResolveParams rootParams)
        {
            ResolveParams bridgeParams = rootParams;
            bridgeParams.floorDef = TerrainDefOf.Bridge;
            bridgeParams.floorOnlyIfTerrainSupports = true;
            bridgeParams.allowBridgeOnAnyImpassableTerrain = true;
            BaseGen.symbolStack.Push("floor", bridgeParams);

            ResolveParams clearParams = rootParams;
            clearParams.clearRoof = true;
            BaseGen.symbolStack.Push("clear", clearParams);

            ResolveParams dangerousTerrainParams = rootParams;
            BaseGen.symbolStack.Push("removeDangerousTerrain", dangerousTerrainParams);
        }

        private static void TryResetBaseGen()
        {
            try
            {
                BaseGen.symbolStack.Clear();
                BaseGen.globalSettings.Clear();
                BaseGen.Reset();
            }
            catch
            {
            }
        }

        private readonly struct LayoutSlot
        {
            public readonly int X;
            public readonly int Z;

            public LayoutSlot(int x, int z)
            {
                X = x;
                Z = z;
            }
        }

        private readonly struct BuildingPlan
        {
            public readonly CellRect Rect;
            public readonly string Symbol;
            public readonly int BedCount;

            public BuildingPlan(CellRect rect, string symbol, int bedCount)
            {
                Rect = rect;
                Symbol = symbol;
                BedCount = bedCount;
            }

            public BuildingPlan WithBedCount(int bedCount)
            {
                return new BuildingPlan(Rect, Symbol, bedCount);
            }
        }
    }
}
