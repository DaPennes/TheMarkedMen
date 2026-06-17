using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TheMarkedMen
{
    public static class TheMarkedMenAncientUrbanRuinsIntegration
    {
        private const string AurNamespace = "AncientMarket_Libraray";

        public const string AurDefNamePrefix = "AM_";

        private static readonly Dictionary<Map, bool> aurMapCache = new Dictionary<Map, bool>();

        public static bool Active => TheMarkedMenMod.Settings?.urbanOutbreaksEnabled == true;

        public static void Apply(Harmony harmony)
        {
            if (harmony == null)
            {
                return;
            }

            try
            {
                MethodInfo initTarget = AccessTools.Method(typeof(Map), "FinalizeInit");
                MethodInfo initPostfix = AccessTools.Method(typeof(TheMarkedMenAncientUrbanRuinsIntegration), nameof(Postfix_MapFinalizeInit));
                if (initTarget != null && initPostfix != null)
                {
                    harmony.Patch(initTarget, postfix: new HarmonyMethod(initPostfix));
                }

                MethodInfo loadTarget = AccessTools.Method(typeof(Map), "FinalizeLoading");
                MethodInfo loadPostfix = AccessTools.Method(typeof(TheMarkedMenAncientUrbanRuinsIntegration), nameof(Postfix_MapFinalizeLoading));
                if (loadTarget != null && loadPostfix != null)
                {
                    harmony.Patch(loadTarget, postfix: new HarmonyMethod(loadPostfix));
                }

                LogVerbose("[The Marked Men] Ancient Urban Ruins integration active.");
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Ancient Urban Ruins integration skipped: " + ex.Message);
            }
        }

        public static void Postfix_MapFinalizeInit(Map __instance)
        {
            if (!Active || __instance == null)
            {
                return;
            }

            Log.Message("[The Marked Men] Map FinalizeInit: checking for AUR ruins.");
            TryInitializeUrbanOutbreakMap(__instance);
        }

        public static void Postfix_MapFinalizeLoading(Map __instance)
        {
            if (!Active || __instance == null)
            {
                return;
            }

            Log.Message("[The Marked Men] Map FinalizeLoading: checking for AUR ruins.");
            TryInitializeUrbanOutbreakMap(__instance);
        }

        public static bool IsAncientUrbanRuinsMap(Map map)
        {
            if (map == null)
            {
                return false;
            }

            if (aurMapCache.TryGetValue(map, out bool cached))
            {
                return cached;
            }

            if (map.Parent != null)
            {
                string parentTypeName = map.Parent.GetType().FullName;
                Log.Message("[The Marked Men] Map parent type: " + parentTypeName);
                if (parentTypeName.StartsWith("AncientMarket_Libraray.", StringComparison.Ordinal))
                {
                    Log.Message("[The Marked Men] Map identified as AUR ruins via parent type.");
                    aurMapCache[map] = true;
                    return true;
                }
            }

            bool result = HasAurBuildings(map);
            Log.Message("[The Marked Men] AUR building scan result: " + result);
            aurMapCache[map] = result;
            return result;
        }

        private static bool HasAurBuildings(Map map)
        {
            if (map?.listerThings?.AllThings == null)
            {
                return false;
            }

            List<Thing> allThings = map.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                Thing thing = allThings[i];
                if (thing is Building && thing.def != null &&
                    thing.def.defName.StartsWith(AurDefNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<Thing> GetAllAurBuildings(Map map)
        {
            List<Thing> results = new List<Thing>();
            if (map?.listerThings?.AllThings == null)
            {
                return results;
            }

            List<Thing> allThings = map.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                Thing thing = allThings[i];
                if (thing is Building && thing.Spawned && thing.def != null &&
                    thing.def.defName.StartsWith(AurDefNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(thing);
                }
            }

            return results;
        }

        public static void TryInitializeUrbanOutbreakMap(Map map)
        {
            if (!Active)
            {
                Log.Message("[The Marked Men] Urban outbreaks disabled in settings.");
                return;
            }

            if (!IsAncientUrbanRuinsMap(map))
            {
                Log.Message("[The Marked Men] Not an AUR map, skipping urban outbreak.");
                return;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.urbanOutbreaksEnabled)
            {
                Log.Message("[The Marked Men] Urban outbreaks disabled in settings (re-check).");
                return;
            }

            if (map.GetComponent<MapComponent_UrbanOutbreak>() != null)
            {
                Log.Message("[The Marked Men] Urban outbreak already initialized on this map.");
                return;
            }

            Log.Message("[The Marked Men] Initializing urban outbreak on AUR map.");
            MapComponent_UrbanOutbreak comp = new MapComponent_UrbanOutbreak(map);
            map.components.Add(comp);
            comp.InitializeFromBuildingLayout();
        }

        public static bool DebugInitializeCurrentMap()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Log.Message("[The Marked Men] Debug: no current map.");
                return false;
            }

            if (map.GetComponent<MapComponent_UrbanOutbreak>() != null)
            {
                Log.Message("[The Marked Men] Debug: outbreak already initialized.");
                return true;
            }

            if (!IsAncientUrbanRuinsMap(map))
            {
                Log.Message("[The Marked Men] Debug: not an AUR map.");
                return false;
            }

            Log.Message("[The Marked Men] Debug: forcing urban outbreak init.");
            MapComponent_UrbanOutbreak comp = new MapComponent_UrbanOutbreak(map);
            map.components.Add(comp);
            comp.InitializeFromBuildingLayout();
            return true;
        }

        public static bool DebugFireIncident(string defName)
        {
            IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail(defName);
            if (incidentDef == null)
            {
                return false;
            }

            Map map = Find.CurrentMap;
            if (map == null)
            {
                return false;
            }

            IncidentParms parms = new IncidentParms
            {
                target = map,
                faction = CrossedUtility.Component?.EnsureCrossedFaction(),
                points = StorytellerUtility.DefaultThreatPointsNow(map),
                forced = true
            };

            return incidentDef.Worker.TryExecute(parms);
        }

        public static float GetBuildingInfectionWeight(string defName)
        {
            if (defName.NullOrEmpty())
            {
                return 0f;
            }

            if (ContainsAny(defName, "Hospital", "Clinic", "Medical", "Pharmacy", "Lab"))
            {
                return 0.9f;
            }
            if (ContainsAny(defName, "Military", "Barracks", "Armory", "Bunker", "Police", "Station"))
            {
                return 1f;
            }
            if (ContainsAny(defName, "Mall", "Market", "Shop", "Store", "Supermarket"))
            {
                return 0.8f;
            }
            if (ContainsAny(defName, "Apartment", "Hotel", "Dorm", "Residential", "House", "Home"))
            {
                return 0.5f;
            }
            if (ContainsAny(defName, "Office", "Bank", "Library", "School", "Church", "Temple"))
            {
                return 0.6f;
            }
            if (ContainsAny(defName, "Restaurant", "Bar", "Cafe", "Kitchen", "Cafeteria"))
            {
                return 0.55f;
            }
            if (ContainsAny(defName, "Warehouse", "Storage", "Garage", "Parking"))
            {
                return 0.4f;
            }
            if (ContainsAny(defName, "Factory", "Industrial", "Workshop", "Plant"))
            {
                return 0.7f;
            }

            return 0.2f;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
            {
                if (value.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static void LogVerbose(string message)
        {
            if (TheMarkedMenMod.Settings?.verboseCompatibilityLogging == true)
            {
                Log.Message(message);
            }
        }
    }

    public class MapComponent_UrbanOutbreak : MapComponent
    {
        private const int UrbanTickInterval = 6000;
        private const int SpawnSearchRadius = 8;
        private const int MaxUrbanPawnsPerMap = 60;
        private const float DormantBuildingActivationRange = 12f;

        private int nextUrbanTick;
        private int nextAmbushTick;
        private int totalInfectedSpawned;
        private bool epicenter;
        private bool initialized;
        private int initRetryCount;
        private const int MaxInitRetries = 60;

        private List<IntVec3> dormantBuildingPositions = new List<IntVec3>();
        private List<ThingDef> dormantBuildingDefs = new List<ThingDef>();
        private List<bool> dormantBuildingActivated = new List<bool>();

        private List<IntVec3> buildingPositions = new List<IntVec3>();
        private List<float> buildingInfectionWeights = new List<float>();
        private List<PawnKindDef> buildingPawnKinds = new List<PawnKindDef>();

        public MapComponent_UrbanOutbreak(Map map) : base(map)
        {
        }

        public int TotalInfectedSpawned => totalInfectedSpawned;
        public bool IsEpicenter => epicenter;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextUrbanTick, "nextUrbanTick", 0);
            Scribe_Values.Look(ref nextAmbushTick, "nextAmbushTick", 0);
            Scribe_Values.Look(ref totalInfectedSpawned, "totalInfectedSpawned", 0);
            Scribe_Values.Look(ref epicenter, "epicenter", false);
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref initRetryCount, "initRetryCount", 0);
            Scribe_Collections.Look(ref dormantBuildingPositions, "dormantBuildingPositions", LookMode.Value);
            Scribe_Collections.Look(ref dormantBuildingDefs, "dormantBuildingDefs", LookMode.Def);
            Scribe_Collections.Look(ref dormantBuildingActivated, "dormantBuildingActivated", LookMode.Value);
            Scribe_Collections.Look(ref buildingPositions, "buildingPositions", LookMode.Value);
            Scribe_Collections.Look(ref buildingInfectionWeights, "buildingInfectionWeights", LookMode.Value);
            Scribe_Collections.Look(ref buildingPawnKinds, "buildingPawnKinds", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                dormantBuildingPositions ??= new List<IntVec3>();
                dormantBuildingDefs ??= new List<ThingDef>();
                dormantBuildingActivated ??= new List<bool>();
                buildingPositions ??= new List<IntVec3>();
                buildingInfectionWeights ??= new List<float>();
                buildingPawnKinds ??= new List<PawnKindDef>();
                initRetryCount = 0;
            }
        }

        public void InitializeFromBuildingLayout()
        {
            if (initialized || map == null)
            {
                return;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null)
            {
                return;
            }

            float infectionDensity = settings.urbanInfectionDensity;
            if (infectionDensity <= 0f)
            {
                initialized = true;
                return;
            }

            TheMarkedMenGameComponent component = CrossedUtility.Component;
            Faction faction = component?.EnsureCrossedFaction();
            if (faction == null)
            {
                initialized = true;
                return;
            }

            List<Thing> allBuildings = TheMarkedMenAncientUrbanRuinsIntegration.GetAllAurBuildings(map);
            if (allBuildings.Count == 0)
            {
                Log.Message("[The Marked Men] No AM_-prefixed buildings found yet, will retry next tick.");
                return;
            }

            Log.Message("[The Marked Men] Found " + allBuildings.Count + " AUR buildings on map.");

            for (int i = 0; i < allBuildings.Count; i++)
            {
                Thing building = allBuildings[i];
                if (building == null || !building.Spawned || building.def == null)
                {
                    continue;
                }

                string defName = building.def.defName;

                float weight = TheMarkedMenAncientUrbanRuinsIntegration.GetBuildingInfectionWeight(defName);
                if (weight <= 0f)
                {
                    continue;
                }

                weight = Mathf.Clamp01(weight * infectionDensity);
                if (!Rand.Chance(weight))
                {
                    continue;
                }

                buildingPositions.Add(building.Position);
                buildingInfectionWeights.Add(weight);
                PawnKindDef kind = PickUrbanPawnKind(weight);
                buildingPawnKinds.Add(kind);

                bool isDormant = settings.dormantInfestationsEnabled && Rand.Chance(settings.dormantInfestationFrequency * 0.3f);
                if (isDormant)
                {
                    dormantBuildingPositions.Add(building.Position);
                    dormantBuildingDefs.Add(building.def);
                    dormantBuildingActivated.Add(false);
                }
            }

            int epicenterRoll = Mathf.RoundToInt(settings.epicenterSpawnChance * 100f);
            epicenter = settings.cityEpicentersEnabled && Rand.RangeInclusive(1, 100) <= epicenterRoll;

            SpawnInitialUrbanPopulation();
            initialized = true;
        }

        private void SpawnInitialUrbanPopulation()
        {
            if (buildingPositions.Count == 0)
            {
                return;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            TheMarkedMenGameComponent component = CrossedUtility.Component;
            Faction faction = component?.EnsureCrossedFaction();
            if (settings == null || faction == null)
            {
                return;
            }

            float targetCount = Mathf.Min(settings.urbanInfectionDensity * buildingPositions.Count * 0.8f, MaxUrbanPawnsPerMap);
            if (epicenter)
            {
                targetCount *= 1.5f;
            }

            int count = Mathf.Clamp(Mathf.RoundToInt(targetCount), 10, MaxUrbanPawnsPerMap);

            for (int i = 0; i < buildingPositions.Count && totalInfectedSpawned < count; i++)
            {
                if (!Rand.Chance(buildingInfectionWeights[i]))
                {
                    continue;
                }

                IntVec3 spawnPos = CellFinder.RandomClosewalkCellNear(buildingPositions[i], map, SpawnSearchRadius, null);
                if (!spawnPos.IsValid || !spawnPos.Standable(map) || spawnPos.Fogged(map))
                {
                    continue;
                }

                PawnKindDef kind = buildingPawnKinds[i];
                if (kind == null)
                {
                    continue;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, spawnPos, map, Rot4.Random);
                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                EnsureUrbanLord(pawn, faction);

                totalInfectedSpawned++;
            }

            if (totalInfectedSpawned > 0)
            {
                component?.AddIncident("Urban outbreak: " + totalInfectedSpawned + " Marked Men detected in the ruins.");
            }
        }

        public void SpawnDormantOutbreak(IntVec3 position)
        {
            if (!Active)
            {
                return;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            TheMarkedMenGameComponent component = CrossedUtility.Component;
            Faction faction = component?.EnsureCrossedFaction();
            if (settings == null || faction == null)
            {
                return;
            }

            float weight = TheMarkedMenAncientUrbanRuinsIntegration.GetBuildingInfectionWeight("Dormant");
            int count = Rand.RangeInclusive(1, Mathf.RoundToInt(1f + weight * 3f));

            for (int i = 0; i < count; i++)
            {
                IntVec3 spawnPos = CellFinder.RandomClosewalkCellNear(position, map, SpawnSearchRadius, null);
                if (!spawnPos.IsValid || !spawnPos.Standable(map) || spawnPos.Fogged(map))
                {
                    continue;
                }

                PawnKindDef kind = PickUrbanPawnKind(weight);
                if (kind == null)
                {
                    continue;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, spawnPos, map, Rot4.Random);
                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                EnsureUrbanLord(pawn, faction);
                totalInfectedSpawned++;
            }

            if (count > 0)
            {
                component?.AddIncident("Dormant infestation awakened in the ruins.");
            }
        }

        public void TryActivateNearbyDormantBuildings(IntVec3 position)
        {
            if (!Active || dormantBuildingPositions.Count == 0)
            {
                return;
            }

            for (int i = 0; i < dormantBuildingPositions.Count; i++)
            {
                if (dormantBuildingActivated[i])
                {
                    continue;
                }

                if (dormantBuildingPositions[i].DistanceToSquared(position) <= DormantBuildingActivationRange * DormantBuildingActivationRange)
                {
                    dormantBuildingActivated[i] = true;
                    SpawnDormantOutbreak(dormantBuildingPositions[i]);
                }
            }
        }

        public override void MapComponentTick()
        {
            if (!Active || map == null)
            {
                return;
            }

            if (!initialized)
            {
                initRetryCount++;
                if (initRetryCount > MaxInitRetries)
                {
                    initialized = true;
                    Log.Message("[The Marked Men] Gave up retrying AUR building scan after " + MaxInitRetries + " attempts.");
                    return;
                }
                InitializeFromBuildingLayout();
            }

            if (buildingPositions.Count == 0)
            {
                return;
            }

            int tick = Find.TickManager?.TicksGame ?? 0;

            if (tick >= nextUrbanTick)
            {
                nextUrbanTick = tick + UrbanTickInterval;
                TryAmbushTick();
                TryReplenishUrbanPopulation();
            }

            if (tick >= nextAmbushTick)
            {
                nextAmbushTick = tick + Mathf.Max(600, Mathf.RoundToInt(6000f / Mathf.Max(0.1f, TheMarkedMenMod.Settings?.urbanAmbushFrequency ?? 1f)));
                TryFireMapAmbush();
            }
        }

        private void TryReplenishUrbanPopulation()
        {
            if (!Active)
            {
                return;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null)
            {
                return;
            }

            int currentCount = CountUrbanInfected();
            if (currentCount >= MaxUrbanPawnsPerMap)
            {
                return;
            }

            TheMarkedMenGameComponent component = CrossedUtility.Component;
            Faction faction = component?.EnsureCrossedFaction();
            if (faction == null)
            {
                return;
            }

            float replenishChance = 0.5f * settings.urbanInfectionDensity;
            if (!Rand.Chance(replenishChance))
            {
                return;
            }

            if (buildingPositions.Count == 0)
            {
                return;
            }

            int count = Rand.RangeInclusive(2, Mathf.Min(4, MaxUrbanPawnsPerMap - currentCount));
            for (int i = 0; i < count; i++)
            {
                int index = Rand.Range(0, buildingPositions.Count);
                IntVec3 spawnPos = CellFinder.RandomClosewalkCellNear(buildingPositions[index], map, SpawnSearchRadius, null);
                if (!spawnPos.IsValid || !spawnPos.Standable(map) || spawnPos.Fogged(map))
                {
                    continue;
                }

                PawnKindDef kind = PickUrbanPawnKind(buildingInfectionWeights[index]);
                if (kind == null)
                {
                    continue;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, spawnPos, map, Rot4.Random);
                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                EnsureUrbanLord(pawn, faction);
                totalInfectedSpawned++;
            }
        }

        private void TryAmbushTick()
        {
            if (!Active || map.mapPawns == null)
            {
                return;
            }

            IReadOnlyList<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            if (colonists == null || colonists.Count == 0)
            {
                return;
            }

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn colonist = colonists[i];
                if (colonist == null || !colonist.Spawned)
                {
                    continue;
                }

                TryActivateNearbyDormantBuildings(colonist.Position);
            }
        }

        private void TryFireMapAmbush()
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.urbanAmbushesEnabled || map.mapPawns == null)
            {
                return;
            }

            IReadOnlyList<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            if (colonists == null || colonists.Count == 0)
            {
                return;
            }

            float ambushChance = 0.5f * settings.urbanAmbushFrequency;
            if (!Rand.Chance(ambushChance))
            {
                return;
            }

            if (buildingPositions.Count == 0)
            {
                return;
            }

            int index = Rand.Range(0, buildingPositions.Count);
            IntVec3 ambushPos = buildingPositions[index];
            if (ambushPos.Fogged(map))
            {
                return;
            }

            TheMarkedMenGameComponent component = CrossedUtility.Component;
            Faction faction = component?.EnsureCrossedFaction();
            if (faction == null)
            {
                return;
            }

            int ambushSize = Mathf.Clamp(Mathf.RoundToInt(Rand.Range(3f, 3f + settings.urbanInfectionDensity * 3f)), 3, 10);
            List<Pawn> ambushPawns = new List<Pawn>();

            for (int i = 0; i < ambushSize; i++)
            {
                PawnKindDef kind = PickUrbanPawnKind(settings.urbanInfectionDensity);
                if (kind == null)
                {
                    continue;
                }

                IntVec3 spawnPos = CellFinder.RandomClosewalkCellNear(ambushPos, map, 10, null);
                if (!spawnPos.IsValid || !spawnPos.Standable(map) || spawnPos.Fogged(map))
                {
                    continue;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, spawnPos, map, Rot4.Random);
                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                EnsureUrbanLord(pawn, faction);
                ambushPawns.Add(pawn);
            }

            if (ambushPawns.Count > 0)
            {
                string label = "Urban ambush";
                string text = "Marked Men spring from cover in the ruins!";
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatSmall, new LookTargets(ambushPawns[0]));
            }
        }

        private int CountUrbanInfected()
        {
            if (map?.mapPawns == null)
            {
                return 0;
            }

            IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            int count = 0;
            for (int i = 0; i < allPawns.Count; i++)
            {
                if (CrossedUtility.IsInfectedPawn(allPawns[i]))
                {
                    count++;
                }
            }
            return count;
        }

        private bool Active => TheMarkedMenAncientUrbanRuinsIntegration.Active;

        public static void EnsureUrbanLord(Pawn pawn, Faction faction)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                return;
            }

            if (LordUtility.TryGetLord(pawn, out Lord existingLord) && existingLord.faction == faction)
            {
                return;
            }

            LordJob_AssaultColony lordJob = new LordJob_AssaultColony(faction, false, false, false, false, false, false, true);
            LordMaker.MakeNewLord(faction, lordJob, pawn.Map, new List<Pawn> { pawn });
        }

        public static PawnKindDef PickUrbanPawnKind(float weight)
        {
            PawnKindDef selected = null;
            float totalWeight = 0f;

            AddKind(ref selected, ref totalWeight, CADefOf.CrossedCivilian, 14f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedScout, Mathf.Lerp(4f, 8f, weight));
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedHunter, Mathf.Lerp(2f, 6f, weight));
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedShooter, weight >= 0.3f ? Mathf.Lerp(2f, 6f, weight) : 1f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedRaider, weight >= 0.5f ? Mathf.Lerp(1f, 4f, weight) : 0f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedSoldier, weight >= 0.6f ? Mathf.Lerp(0.5f, 3f, weight) : 0f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedBrute, weight >= 0.7f ? Mathf.Lerp(0.5f, 3f, weight) : 0f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, weight >= 0.4f ? 2f : 0.5f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedAlpha, weight >= 0.85f ? 0.3f : 0f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedWarlord, weight >= 0.9f ? 0.1f : 0f);
            AddKind(ref selected, ref totalWeight, CADefOf.MarkedMan, weight >= 0.95f ? 0.05f : 0f);

            return selected ?? CADefOf.CrossedCivilian ?? CADefOf.CrossedScout ?? CADefOf.CrossedHunter;
        }

        private static void AddKind(ref PawnKindDef selected, ref float totalWeight, PawnKindDef kind, float weight)
        {
            weight = TheMarkedMenSettings.AdjustKindWeight(kind, weight);
            if (kind == null || weight <= 0f)
            {
                return;
            }

            totalWeight += weight;
            if (Rand.Value * totalWeight <= weight)
            {
                selected = kind;
            }
        }
    }

    public sealed class IncidentWorker_UrbanAmbush : IncidentWorker
    {
        private const int MinAmbushCount = 3;
        private const int MaxAmbushCount = 10;

        public override float ChanceFactorNow(IIncidentTarget target)
        {
            return base.ChanceFactorNow(target) * (TheMarkedMenMod.Settings?.urbanAmbushFrequency ?? 1f);
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }

            Map map = parms.target as Map;
            if (map == null || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
            {
                return false;
            }

            if (!TheMarkedMenAncientUrbanRuinsIntegration.IsAncientUrbanRuinsMap(map))
            {
                return false;
            }

            if (TheMarkedMenMod.Settings == null || !TheMarkedMenMod.Settings.urbanOutbreaksEnabled || !TheMarkedMenMod.Settings.urbanAmbushesEnabled)
            {
                return false;
            }

            return CrossedUtility.Component?.EnsureCrossedFaction() != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (map == null || crossed == null)
            {
                return false;
            }

            MapComponent_UrbanOutbreak comp = map.GetComponent<MapComponent_UrbanOutbreak>();
            if (comp == null)
            {
                return false;
            }

            IReadOnlyList<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            if (colonists == null || colonists.Count == 0)
            {
                return false;
            }

            Pawn target = colonists[Rand.Range(0, colonists.Count)];
            IntVec3 ambushCenter = target.Position;

            float density = TheMarkedMenMod.Settings?.urbanInfectionDensity ?? 1f;
            int count = Mathf.Clamp(Mathf.RoundToInt(Rand.Range(MinAmbushCount, MaxAmbushCount * density)), MinAmbushCount, MaxAmbushCount);

            List<Pawn> pawns = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = PickUrbanAmbushKind(density);
                if (kind == null)
                {
                    continue;
                }

                IntVec3 spawnPos = CellFinder.RandomClosewalkCellNear(ambushCenter, map, 12, null);
                if (!spawnPos.IsValid || !spawnPos.Standable(map) || spawnPos.Fogged(map))
                {
                    continue;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kind, crossed);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, spawnPos, map, Rot4.Random);
                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                pawns.Add(pawn);
            }

            if (pawns.Count == 0)
            {
                return false;
            }

            LordJob_AssaultColony lordJob = new LordJob_AssaultColony(crossed, false, false, false, false, false, false, true);
            LordMaker.MakeNewLord(crossed, lordJob, map, pawns);

            string label = def.letterLabel ?? "Urban ambush";
            string text = def.letterText ?? "Marked Men ambush from the ruins!";
            Find.LetterStack.ReceiveLetter(label, text, def.letterDef ?? LetterDefOf.ThreatSmall, new LookTargets(pawns[0]));
            return true;
        }

        private static PawnKindDef PickUrbanAmbushKind(float density)
        {
            PawnKindDef selected = null;
            float totalWeight = 0f;

            AddKind(ref selected, ref totalWeight, CADefOf.CrossedScout, 10f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedHunter, 8f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedCivilian, 12f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedShooter, density >= 0.3f ? 6f : 0f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedRaider, density >= 0.5f ? 4f : 0f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, density >= 0.5f ? 3f : 0f);
            AddKind(ref selected, ref totalWeight, CADefOf.CrossedBrute, density >= 0.7f ? 1.5f : 0f);

            return selected ?? CADefOf.CrossedScout ?? CADefOf.CrossedHunter ?? CADefOf.CrossedCivilian;
        }

        private static void AddKind(ref PawnKindDef selected, ref float totalWeight, PawnKindDef kind, float weight)
        {
            weight = TheMarkedMenSettings.AdjustKindWeight(kind, weight);
            if (kind == null || weight <= 0f)
            {
                return;
            }

            totalWeight += weight;
            if (Rand.Value * totalWeight <= weight)
            {
                selected = kind;
            }
        }
    }

    public sealed class IncidentWorker_UrbanSurvivor : IncidentWorker
    {
        public override float ChanceFactorNow(IIncidentTarget target)
        {
            return base.ChanceFactorNow(target) * (TheMarkedMenMod.Settings?.survivorEncounterChance ?? 0.5f);
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }

            Map map = parms.target as Map;
            if (map == null || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
            {
                return false;
            }

            if (!TheMarkedMenAncientUrbanRuinsIntegration.IsAncientUrbanRuinsMap(map))
            {
                return false;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.urbanOutbreaksEnabled || !settings.survivorEncountersEnabled)
            {
                return false;
            }

            return CrossedUtility.Component?.EnsureCrossedFaction() != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (map == null || crossed == null)
            {
                return false;
            }

            bool isGenuineSurvivor = !Rand.Chance(0.4f);
            bool isHiddenInfected = !isGenuineSurvivor && Rand.Chance(0.6f);
            bool delayedTransformation = !isGenuineSurvivor && !isHiddenInfected && Rand.Chance(0.7f);
            bool leadsToInfestation = !isGenuineSurvivor && !isHiddenInfected && !delayedTransformation;

            PawnKindDef survivorKind = PawnKindDefOf.SpaceRefugee;
            Pawn survivor = PawnGenerator.GeneratePawn(survivorKind, Faction.OfPlayer);
            if (survivor == null)
            {
                return false;
            }

            IntVec3 dropSpot = CellFinderLoose.RandomCellWith(
                (IntVec3 c) => c.Standable(map) && !c.Fogged(map) && c.DistanceToEdge(map) > 10, map, 100);
            if (dropSpot == IntVec3.Invalid)
            {
                return false;
            }

            GenSpawn.Spawn(survivor, dropSpot, map, Rot4.Random);

            string letterText;
            string letterLabel;

            if (isGenuineSurvivor)
            {
                if (survivor.health != null && survivor.health.hediffSet != null)
                {
                    List<Hediff> hediffs = survivor.health.hediffSet.hediffs;
                    for (int i = hediffs.Count - 1; i >= 0; i--)
                    {
                        if (hediffs[i] is Hediff_Injury injury && !injury.IsPermanent())
                        {
                            survivor.health.RemoveHediff(hediffs[i]);
                        }
                    }
                }
                letterLabel = "Survivor found in ruins";
                letterText = "A genuine survivor was found hiding in the ruins. They seem healthy and uninfected.";
            }
            else if (isHiddenInfected)
            {
                CrossedUtility.TryExpose(survivor, 1f, "hidden infected in ruins");
                Hediff virus = CADefOf.CrossVirus == null ? null : survivor.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CrossVirus);
                if (virus != null)
                {
                    virus.Severity = 0.5f;
                }
                letterLabel = "Infected survivor found";
                letterText = "The survivor appears normal at first, but they are carrying the Marked Virus. They may turn at any moment.";
            }
            else if (delayedTransformation)
            {
                letterLabel = "Strange survivor found";
                letterText = "A survivor was found in the ruins. They seem fine for now, but something feels wrong.";
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    int delay = Rand.RangeInclusive(3000, 18000);
                    map?.GetComponent<MapComponent_UrbanOutbreak>()?.SpawnDormantOutbreak(survivor.Position);
                });
            }
            else
            {
                letterLabel = "Trapped survivor";
                letterText = "A survivor calling for help from inside a building. It may be a trap.";
            }

            Find.LetterStack.ReceiveLetter(letterLabel, letterText, def.letterDef ?? LetterDefOf.NeutralEvent, new LookTargets(survivor));
            return true;
        }
    }

}
