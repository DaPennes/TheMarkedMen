using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    /// <summary>
    /// Compatibility layer for Vehicle Framework mod.
    /// Allows infected to interact with vehicles and vehicle-based caravans.
    /// Uses reflection-based detection to avoid hard dependency on Vehicle Framework.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class TheMarkedMenVehicleFrameworkCompatibility
    {
        private const string VehicleFrameworkAssemblyName = "Vehicles";
        private const string VehicleTypeFullName = "Vehicles.VehiclePawn";
        private const string CaravanTypeFullName = "RimWorld.Caravan";
        private const string VehicleDefTypeFullName = "Vehicles.VehicleDef";
        private const string VehiclePawnTypeFullName = "Vehicles.VehiclePawn";
        private const string VehicleRaidHelperTypeFullName = "Vehicles.RaidInjectionHelper";
        private const string VehicleRaidExtensionTypeFullName = "Vehicles.VehicleRaiderDefModExtension";
        private const string MosquitoVehicleDefName = "VVE_Mosquito";
        private const string LouieVehicleDefName = "VVE_Louie";
        private const int MinimumGroundRaidPawns = 3;
        public const int MinimumVehicleCrewReserve = MinimumGroundRaidPawns;

        private static bool isVehicleFrameworkActive;
        private static Type vehicleType;
        private static Assembly vehicleFrameworkAssembly;
        private static bool raidLookupFailureLogged;
        private static MethodInfo getPawnCapacityMethod;
        private static MethodInfo getPassengersMethod;
        private static bool airborneVehicleDefsCached;
        private static List<object> airborneVehicleDefs;
        private static FieldInfo airborneVehicleCombatPowerField;
        private static FieldInfo airborneVehicleTechLevelField;
        private static FieldInfo airborneVehicleNpcPropertiesField;

        static TheMarkedMenVehicleFrameworkCompatibility()
        {
            TryInitializeVehicleFrameworkSupport();
        }

        private static void TryInitializeVehicleFrameworkSupport()
        {
            try
            {
                // Try to load Vehicle Framework types
                Assembly vehicleAssembly = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == VehicleFrameworkAssemblyName)
                    {
                        vehicleAssembly = assembly;
                        break;
                    }
                }

                if (vehicleAssembly == null)
                {
                    return; // Vehicle Framework not loaded
                }

                vehicleType = vehicleAssembly.GetType(VehicleTypeFullName);
                if (vehicleType == null)
                {
                    return; // Vehicle type not found
                }

                // Get useful methods for vehicle interaction
                getPawnCapacityMethod = AccessTools.Method(vehicleType, "GetPawnCapacity");
                getPassengersMethod = AccessTools.PropertyGetter(vehicleType, "AllPawnsAboard");

                isVehicleFrameworkActive = true;
                Log.Message("[The Marked Men] Vehicle Framework compatibility initialized.");
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Vehicle Framework compatibility initialization failed: " + ex.Message);
                isVehicleFrameworkActive = false;
            }
        }

        public static bool IsVehicleFrameworkActive => isVehicleFrameworkActive;

        public static bool IsVehicleFrameworkLoaded => GetVehicleFrameworkAssembly() != null;

        public static Type VehicleType => vehicleType;

        public static bool HasEligibleRaidVehicle(Faction faction, float points, PawnsArrivalModeDef arrivalMode)
        {
            Assembly assembly = GetVehicleFrameworkAssembly();
            if (assembly == null || faction?.def?.modExtensions == null || points < 3000f)
            {
                return false;
            }

            return GetEligibleRaidVehicleDefs(faction, points, arrivalMode, assembly).Count > 0;
        }

        public static bool IsVehiclePawn(Pawn pawn)
        {
            return pawn != null && pawn.GetType().FullName == VehiclePawnTypeFullName;
        }

        public static void KeepMarkedOneOnFoot(List<Pawn> raidPawns, Map map)
        {
            if (raidPawns == null || map == null)
            {
                return;
            }

            List<Pawn> spawnedVehicles = new List<Pawn>(raidPawns);
            foreach (Pawn vehicle in spawnedVehicles)
            {
                if (!IsVehiclePawn(vehicle) || !vehicle.Spawned || vehicle.Map != map)
                {
                    continue;
                }

                List<Pawn> passengers = new List<Pawn>(GetVehiclePassengers(vehicle));
                MethodInfo removePawn = AccessTools.Method(vehicle.GetType(), "RemovePawn", new[] { typeof(Pawn) });
                for (int i = 0; i < passengers.Count; i++)
                {
                    Pawn passenger = passengers[i];
                    if (passenger?.kindDef != CADefOf.MarkedMan)
                    {
                        continue;
                    }

                    if (!CellFinder.TryFindRandomCellNear(vehicle.Position, map, 3, cell =>
                        cell.InBounds(map)
                        && cell.Walkable(map)
                        && cell != vehicle.Position
                        && cell.GetFirstPawn(map) == null, out IntVec3 spawnCell))
                    {
                        Log.Warning("[The Marked Men] Could not find a safe place to disembark the Marked One from a raid vehicle.");
                        continue;
                    }

                    if (removePawn?.Invoke(vehicle, new object[] { passenger }) is bool removed && removed)
                    {
                        GenSpawn.Spawn(passenger, spawnCell, map);
                        raidPawns.Add(passenger);
                    }
                }
            }
        }

        public static bool TrySpawnGroundRaidVehicles(
            Faction faction,
            float points,
            List<Pawn> availableRaidPawns,
            IncidentParms parms,
            out List<Pawn> spawnedVehicles,
            out string failureReason,
            int requestedVehicleCountOverride = -1,
            int minimumCrewPawnsRemaining = MinimumGroundRaidPawns)
        {
            spawnedVehicles = new List<Pawn>();
            failureReason = null;
            Map map = parms?.target as Map;
            if (faction == null || map == null || availableRaidPawns == null)
            {
                failureReason = "The faction, target map, or raid pawn list was missing.";
                return false;
            }

            List<object> eligibleDefs = GetEligibleRaidVehicleDefs(
                faction,
                points,
                parms.raidArrivalMode,
                GetVehicleFrameworkAssembly());
            int requestedVehicleCount = requestedVehicleCountOverride >= 0
                ? Mathf.Clamp(requestedVehicleCountOverride, 0, 10)
                : GetGroundRaidVehicleCount(points);
            minimumCrewPawnsRemaining = Mathf.Max(MinimumGroundRaidPawns, minimumCrewPawnsRemaining);
            int existingVehicleCount = 0;
            for (int i = 0; i < availableRaidPawns.Count; i++)
            {
                if (IsVehiclePawn(availableRaidPawns[i]))
                {
                    existingVehicleCount++;
                }
            }

            if (existingVehicleCount >= requestedVehicleCount && requestedVehicleCount > 0)
            {
                return true;
            }

            if (eligibleDefs.Count == 0 || requestedVehicleCount == 0)
            {
                failureReason = "No ground vehicles matched the faction's raid settings, points budget, or arrival restrictions.";
                return false;
            }

            if (CountAvailableVehicleCrewPawns(availableRaidPawns) <= minimumCrewPawnsRemaining)
            {
                failureReason = "There were not enough infantry pawns to crew a ground vehicle while preserving "
                    + minimumCrewPawnsRemaining + " infantry pawns.";
                return false;
            }

            int initialCount = eligibleDefs.Count;
            string lastFailure = "all eligible vehicles were rejected";
            while (spawnedVehicles.Count + existingVehicleCount < requestedVehicleCount
                && eligibleDefs.Count > 0
                && CountAvailableVehicleCrewPawns(availableRaidPawns) > minimumCrewPawnsRemaining)
            {
                int candidateIndex = Rand.Range(0, eligibleDefs.Count);
                object vehicleDef = eligibleDefs[candidateIndex];
                Pawn vehicle = null;
                Dictionary<Pawn, IntVec3> originalSpawnPositions = new Dictionary<Pawn, IntVec3>();
                try
                {
                    Type vehicleDefType = vehicleDef.GetType();
                    Type spawnerType = GetVehicleFrameworkAssembly()?.GetType("Vehicles.VehicleSpawner");
                    MethodInfo generateVehicle = spawnerType == null
                        ? null
                        : AccessTools.Method(spawnerType, "GenerateVehicle", new[] { vehicleDefType, typeof(Faction) });
                    if (generateVehicle == null)
                    {
                        lastFailure = "Vehicle Framework's vehicle generator could not be found";
                        eligibleDefs.RemoveAt(candidateIndex);
                        continue;
                    }

                    vehicle = generateVehicle.Invoke(null, new[] { vehicleDef, faction }) as Pawn;
                    if (vehicle == null)
                    {
                        lastFailure = "Vehicle Framework did not generate " + vehicleDef;
                        eligibleDefs.RemoveAt(candidateIndex);
                        continue;
                    }
                    EnsureRaidVehicleFaction(vehicle, faction);

                    MethodInfo tryAddPawn = AccessTools.Method(vehicle.GetType(), "TryAddPawn", new[] { typeof(Pawn) });
                    PropertyInfo seatsAvailable = AccessTools.Property(vehicle.GetType(), "SeatsAvailable");
                    if (tryAddPawn == null || seatsAvailable == null)
                    {
                        lastFailure = "Vehicle Framework crew boarding APIs could not be found";
                        vehicle.Destroy(DestroyMode.Vanish);
                        eligibleDefs.RemoveAt(candidateIndex);
                        continue;
                    }

                    int pawnIndex = 0;
                    PropertyInfo hasEnoughOperators = AccessTools.Property(vehicle.GetType(), "HasEnoughOperators");
                    while (CountAvailableVehicleCrewPawns(availableRaidPawns) > minimumCrewPawnsRemaining
                        && Convert.ToInt32(seatsAvailable.GetValue(vehicle, null)) > 0
                        && pawnIndex < availableRaidPawns.Count)
                    {
                        Pawn pawn = availableRaidPawns[pawnIndex];
                        if (!CanCrewRaidVehicle(pawn))
                        {
                            pawnIndex++;
                            continue;
                        }

                        if (pawn.Spawned)
                        {
                            originalSpawnPositions[pawn] = pawn.Position;
                        }

                        if (tryAddPawn.Invoke(vehicle, new object[] { pawn }) is bool added && added)
                        {
                            availableRaidPawns.RemoveAt(pawnIndex);
                            if (hasEnoughOperators?.GetValue(vehicle, null) is bool operatorsReady
                                && operatorsReady)
                            {
                                break;
                            }
                        }
                        else
                        {
                            originalSpawnPositions.Remove(pawn);
                            pawnIndex++;
                        }
                    }

                    bool hasCrew = false;
                    foreach (Pawn _ in GetVehiclePassengers(vehicle))
                    {
                        hasCrew = true;
                        break;
                    }

                    if (!hasCrew
                        || (hasEnoughOperators?.GetValue(vehicle, null) is bool operatorsSatisfied && !operatorsSatisfied))
                    {
                        lastFailure = "the vehicle could not be crewed while preserving "
                            + minimumCrewPawnsRemaining + " infantry pawns";
                        RestoreGroundRaidCrew(vehicle, map, availableRaidPawns, originalSpawnPositions);
                        vehicle.Destroy(DestroyMode.Vanish);
                        eligibleDefs.RemoveAt(candidateIndex);
                        continue;
                    }

                    if (!TrySpawnGroundRaidVehicle(vehicle, map, out string spawnFailure))
                    {
                        lastFailure = spawnFailure;
                        RestoreGroundRaidCrew(vehicle, map, availableRaidPawns, originalSpawnPositions);
                        vehicle.Destroy(DestroyMode.Vanish);
                        eligibleDefs.RemoveAt(candidateIndex);
                        continue;
                    }

                    spawnedVehicles.Add(vehicle);
                    availableRaidPawns.Add(vehicle);
                }
                catch (Exception ex)
                {
                    lastFailure = "deployment of " + vehicleDef + " threw "
                        + ex.GetType().Name + ": " + ex.Message;
                    if (vehicle != null && !vehicle.Destroyed)
                    {
                        RestoreGroundRaidCrew(vehicle, map, availableRaidPawns, originalSpawnPositions);
                        vehicle.Destroy(DestroyMode.Vanish);
                    }
                    eligibleDefs.RemoveAt(candidateIndex);
                }
            }

            if (spawnedVehicles.Count > 0)
            {
                return true;
            }

            failureReason = "No eligible ground vehicle could be deployed after trying "
                + initialCount + " candidate(s): " + lastFailure + ".";
            return false;
        }

        public static void TryAddGroundVehiclesToRaid(Faction faction, List<Pawn> raidPawns, IncidentParms parms)
        {
            Map map = parms?.target as Map;
            if (faction == null
                || map == null
                || raidPawns == null
                || !IsVehicleFrameworkLoaded)
            {
                return;
            }

            float vehiclePoints = Mathf.Max(parms.points, 3000f);
            int requestedVehicleCount = GetRaidAssaultVehicleCount(vehiclePoints);
            if (!HasEligibleRaidVehicle(faction, vehiclePoints, parms.raidArrivalMode))
            {
                Log.Warning("[The Marked Men] Could not add vehicles to the scheduled warband/horde: no eligible ground vehicle is available at the minimum vehicle budget.");
                return;
            }

            KeepMarkedOneOnFoot(raidPawns, map);
            EnsureRaidVehicleCrewPool(faction, raidPawns, map, requestedVehicleCount * 2 + MinimumGroundRaidPawns);
            if (!TrySpawnGroundRaidVehicles(
                faction,
                vehiclePoints,
                raidPawns,
                parms,
                out List<Pawn> vehicles,
                out string failureReason,
                requestedVehicleCount,
                MinimumGroundRaidPawns))
            {
                Log.Warning("[The Marked Men] Could not add ground vehicles to this raid (" + failureReason + ").");
                return;
            }

            Log.Message("[The Marked Men] Added " + vehicles.Count
                + " ground vehicle(s) to the scheduled raid.");
        }

        private static void EnsureRaidVehicleCrewPool(
            Faction faction,
            List<Pawn> raidPawns,
            Map map,
            int minimumCrewCount)
        {
            int missingCrew = minimumCrewCount - CountAvailableVehicleCrewPawns(raidPawns);
            if (missingCrew <= 0)
            {
                return;
            }

            PawnKindDef crewKind = CADefOf.CrossedSoldier
                ?? CADefOf.CrossedRaider
                ?? CADefOf.CrossedCivilian;
            if (crewKind == null)
            {
                Log.Warning("[The Marked Men] Could not generate additional infected vehicle crew: no suitable pawn kind is defined.");
                return;
            }

            IntVec3 anchor = raidPawns.FirstOrFallback(pawn => pawn != null && pawn.Spawned && !IsVehiclePawn(pawn))?.Position
                ?? map.Center;
            for (int i = 0; i < missingCrew; i++)
            {
                Pawn crew = PawnGenerator.GeneratePawn(crewKind, faction, map.Tile);
                if (crew == null)
                {
                    Log.Warning("[The Marked Men] Vehicle raid crew generation returned no pawn for " + crewKind.defName + ".");
                    continue;
                }

                CrossedUtility.ApplyClassHediffs(crew);
                CrossedUtility.ApplyInfectedTattoo(crew);
                if (!CellFinder.TryFindRandomCellNear(anchor, map, 8, cell =>
                    cell.InBounds(map)
                    && cell.Walkable(map)
                    && cell.GetFirstPawn(map) == null, out IntVec3 spawnCell))
                {
                    crew.Destroy(DestroyMode.Vanish);
                    Log.Warning("[The Marked Men] Could not find a safe spawn cell for additional vehicle raid crew.");
                    continue;
                }

                GenSpawn.Spawn(crew, spawnCell, map);
                if (crew.Spawned && crew.Map == map)
                {
                    raidPawns.Add(crew);
                }
                else
                {
                    crew.Destroy(DestroyMode.Vanish);
                    Log.Warning("[The Marked Men] Additional vehicle raid crew did not spawn on the target map.");
                }
            }
        }

        public static int GetRaidAssaultVehicleCount(float points)
        {
            float normalizedPoints = Mathf.Clamp(points, 3000f, 30000f);
            int configuredMaximum = Mathf.Clamp(TheMarkedMenMod.Settings?.maxRaidVehicles ?? 10, 3, 10);
            return Mathf.Clamp(
                3 + Mathf.FloorToInt((normalizedPoints - 3000f) * 7f / 27000f),
                3,
                configuredMaximum);
        }

        public static void ApplyVehicleCombatBehavior(Pawn vehicle)
        {
            if (vehicle == null || !vehicle.Spawned || vehicle.Dead
                || vehicle.Map == null || !IsVehiclePawn(vehicle)
                || vehicle.mindState == null)
            {
                return;
            }

            Pawn enemy = null;
            float enemyDistanceSquared = float.MaxValue;
            IReadOnlyList<Pawn> hostilePawns = vehicle.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < hostilePawns.Count; i++)
            {
                Pawn pawn = hostilePawns[i];
                if (pawn == null || pawn.Dead || pawn.Downed || !pawn.HostileTo(vehicle))
                {
                    continue;
                }

                float distanceSquared = pawn.Position.DistanceToSquared(vehicle.Position);
                if (distanceSquared < enemyDistanceSquared)
                {
                    enemyDistanceSquared = distanceSquared;
                    enemy = pawn;
                }
            }

            if (enemy == null)
            {
                vehicle.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                return;
            }

            float preferredRange = GetVehiclePreferredCombatRange(vehicle);
            float enemyDistance = Mathf.Sqrt(enemyDistanceSquared);
            if (enemyDistance < preferredRange * 0.72f)
            {
                IntVec3 retreatCell = GetVehicleStandoffCell(vehicle, enemy, preferredRange);
                vehicle.mindState.duty = retreatCell.IsValid
                    ? new PawnDuty(DutyDefOf.AssaultColony, retreatCell, 5f)
                    : new PawnDuty(DutyDefOf.AssaultColony);
            }
            else
            {
                vehicle.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
            }
        }

        private static float GetVehiclePreferredCombatRange(Pawn vehicle)
        {
            string identity = ((vehicle.def?.defName ?? string.Empty) + " "
                + (vehicle.def?.label ?? string.Empty)).ToLowerInvariant();

            if (identity.Contains("rocket") || identity.Contains("missile")
                || identity.Contains("artillery") || identity.Contains("mortar"))
            {
                return 36f;
            }
            if (identity.Contains("helicopter") || identity.Contains("mosquito")
                || identity.Contains("louie") || identity.Contains("hover"))
            {
                return 28f;
            }
            if (identity.Contains("tank") || identity.Contains("apc")
                || identity.Contains("cannon") || identity.Contains("gun"))
            {
                return 22f;
            }
            if (identity.Contains("car") || identity.Contains("truck"))
            {
                return 16f;
            }

            return 24f;
        }

        private static IntVec3 GetVehicleStandoffCell(Pawn vehicle, Pawn enemy, float preferredRange)
        {
            Vector3 away = (vehicle.DrawPos - enemy.DrawPos).normalized;
            if (away.sqrMagnitude < 0.01f)
            {
                away = new Vector3(Rand.Range(-1f, 1f), 0f, Rand.Range(-1f, 1f)).normalized;
            }

            IntVec3 proposed = vehicle.Position + new IntVec3(
                Mathf.RoundToInt(away.x * preferredRange * 0.55f),
                0,
                Mathf.RoundToInt(away.z * preferredRange * 0.55f));
            if (proposed.InBounds(vehicle.Map) && proposed.Walkable(vehicle.Map))
            {
                return proposed;
            }

            return IntVec3.Invalid;
        }

        private static bool TrySpawnGroundRaidVehicle(Pawn vehicle, Map map, out string failureReason)
        {
            failureReason = "Vehicle Framework could not find a clear map-edge cell for the ground vehicle.";
            Assembly assembly = GetVehicleFrameworkAssembly();
            Type mapHelperType = assembly?.GetType("Vehicles.MapHelper");
            MethodInfo blocked = mapHelperType == null
                ? null
                : AccessTools.Method(mapHelperType, "ImpassableOrVehicleBlocked");
            MethodInfo nonStandable = mapHelperType == null
                ? null
                : AccessTools.Method(mapHelperType, "NonStandableOrVehicleBlocked");
            if (vehicle == null || map == null || blocked == null || nonStandable == null)
            {
                failureReason = "Vehicle Framework's ground-vehicle placement APIs are unavailable.";
                return false;
            }

            Rot4 rotation = Rot4.Random;
            List<IntVec3> candidates = new List<IntVec3>();
            int maxDepth = Mathf.Min(8, Mathf.Min(map.Size.x, map.Size.z) / 4);
            for (int depth = 1; depth <= maxDepth; depth++)
            {
                int minX = depth;
                int maxX = map.Size.x - 1 - depth;
                int minZ = depth;
                int maxZ = map.Size.z - 1 - depth;
                for (int x = minX; x <= maxX; x++)
                {
                    AddGroundVehicleSpawnCandidate(vehicle, map, rotation, blocked, nonStandable, new IntVec3(x, 0, minZ), candidates);
                    AddGroundVehicleSpawnCandidate(vehicle, map, rotation, blocked, nonStandable, new IntVec3(x, 0, maxZ), candidates);
                }

                for (int z = minZ + 1; z < maxZ; z++)
                {
                    AddGroundVehicleSpawnCandidate(vehicle, map, rotation, blocked, nonStandable, new IntVec3(minX, 0, z), candidates);
                    AddGroundVehicleSpawnCandidate(vehicle, map, rotation, blocked, nonStandable, new IntVec3(maxX, 0, z), candidates);
                }

                if (candidates.Count > 0)
                {
                    break;
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            IntVec3 spawnCell = candidates[Rand.Range(0, candidates.Count)];
            GenSpawn.Spawn(vehicle, spawnCell, map, rotation);
            if (!vehicle.Spawned || vehicle.Map != map)
            {
                failureReason = "Vehicle Framework did not spawn the ground vehicle on the target map.";
                return false;
            }

            failureReason = null;
            return true;
        }

        private static void AddGroundVehicleSpawnCandidate(
            Pawn vehicle,
            Map map,
            Rot4 rotation,
            MethodInfo blocked,
            MethodInfo nonStandable,
            IntVec3 cell,
            List<IntVec3> candidates)
        {
            if (!cell.InBounds(map) || cell.GetRoof(map) != null || cell.GetFirstPawn(map) != null)
            {
                return;
            }

            if (blocked.Invoke(null, new object[] { vehicle, map, cell, rotation }) is not bool isBlocked
                || isBlocked
                || nonStandable.Invoke(null, new object[] { vehicle, map, cell, rotation }) is not bool isNotStandable
                || isNotStandable)
            {
                return;
            }

            candidates.Add(cell);
        }

            public static bool HasEligibleAirborneRaidVehicle(
                Faction faction,
                float points,
                float budgetMultiplier = 1f,
                PawnsArrivalModeDef arrivalMode = null)
            {
                return GetEligibleAirborneRaidVehicleDefs(faction, points, budgetMultiplier, arrivalMode).Count > 0;
            }

            public static bool TrySpawnAirborneRaidVehicles(
                Faction faction,
                float points,
                Map map,
                List<Pawn> availableRaidPawns,
                float budgetMultiplier,
                int requestedVehicleCount,
                int minimumVehicleCount,
                out List<Pawn> spawnedVehicles,
                out string failureReason,
                PawnsArrivalModeDef arrivalMode = null)
            {
                spawnedVehicles = new List<Pawn>();
                failureReason = null;
                requestedVehicleCount = Mathf.Clamp(requestedVehicleCount, 1, 10);
                minimumVehicleCount = Mathf.Clamp(minimumVehicleCount, 1, requestedVehicleCount);
                HashSet<IntVec3> reservedLandingCells = new HashSet<IntVec3>();

                for (int i = 0; i < requestedVehicleCount; i++)
                {
                    if (!TrySpawnAirborneRaidVehicle(
                        faction,
                        points,
                        map,
                        availableRaidPawns,
                        budgetMultiplier,
                        out Pawn vehicle,
                        out string deploymentFailure,
                        arrivalMode,
                        reservedLandingCells))
                    {
                        failureReason = deploymentFailure;
                        break;
                    }

                    spawnedVehicles.Add(vehicle);
                }

                if (spawnedVehicles.Count >= minimumVehicleCount)
                {
                    if (spawnedVehicles.Count < requestedVehicleCount)
                    {
                        Log.Warning("[The Marked Men] Airborne assault deployed "
                            + spawnedVehicles.Count + " of " + requestedVehicleCount
                            + " requested aircraft; continuing with the deployed aircraft.");
                    }

                    failureReason = null;
                    return true;
                }

                for (int i = 0; i < spawnedVehicles.Count; i++)
                {
                    AbortAirborneRaidVehicle(spawnedVehicles[i]);
                }

                failureReason = "Only " + spawnedVehicles.Count + " of " + requestedVehicleCount
                    + " requested aircraft could be deployed; at least " + minimumVehicleCount
                    + " are required. " + failureReason;
                spawnedVehicles.Clear();
                return false;
            }

            public static bool TrySpawnAirborneRaidVehicle(
                Faction faction,
                float points,
                Map map,
                List<Pawn> availableRaidPawns,
                float budgetMultiplier,
                out Pawn spawnedVehicle,
                out string failureReason,
                PawnsArrivalModeDef arrivalMode = null,
                HashSet<IntVec3> reservedLandingCells = null)
            {
                spawnedVehicle = null;
                failureReason = "Airborne vehicle deployment failed for an unknown reason.";
                if (faction == null || map == null || availableRaidPawns == null)
                {
                    failureReason = "The faction, target map, or raid pawn list was missing.";
                    return false;
                }

                List<object> eligibleDefs = GetEligibleAirborneRaidVehicleDefs(
                    faction,
                    points,
                    budgetMultiplier,
                    arrivalMode);
                if (eligibleDefs.Count == 0)
                {
                    failureReason = "No aircraft matched the faction's raid settings, points budget, crew-launch support, or arrival restrictions.";
                    return false;
                }

                List<object> candidates = new List<object>();
                List<object> fallbackCandidates = new List<object>();
                FieldInfo componentsField = AccessTools.Field(eligibleDefs[0].GetType(), "comps");
                for (int i = 0; i < eligibleDefs.Count; i++)
                {
                    object candidateDef = eligibleDefs[i];
                    object components = componentsField?.GetValue(candidateDef);
                    if (HasVehicleHoverComponent(components) && HasVehicleTurrets(components))
                    {
                        candidates.Add(candidateDef);
                    }
                    else
                    {
                        fallbackCandidates.Add(candidateDef);
                    }
                }

                string lastFailure = "all eligible aircraft were rejected";
                while (candidates.Count > 0 || fallbackCandidates.Count > 0)
                {
                    List<object> candidatePool = candidates.Count > 0 ? candidates : fallbackCandidates;
                    int candidateIndex = Rand.Range(0, candidatePool.Count);
                    object vehicleDef = candidatePool[candidateIndex];
                    candidatePool.RemoveAt(candidateIndex);
                    Pawn vehicle = null;
                    Dictionary<Pawn, IntVec3> originalSpawnPositions = new Dictionary<Pawn, IntVec3>();
                    try
                    {
                        Type vehicleDefType = vehicleDef.GetType();
                        Type spawnerType = GetVehicleFrameworkAssembly()?.GetType("Vehicles.VehicleSpawner");
                        MethodInfo generateVehicle = spawnerType == null
                            ? null
                            : AccessTools.Method(spawnerType, "GenerateVehicle", new[] { vehicleDefType, typeof(Faction) });
                        if (generateVehicle == null)
                        {
                            lastFailure = "Vehicle Framework's vehicle generator could not be found";
                            continue;
                        }

                        vehicle = generateVehicle.Invoke(null, new[] { vehicleDef, faction }) as Pawn;
                        if (vehicle == null)
                        {
                            lastFailure = "Vehicle Framework did not generate " + vehicleDef;
                            continue;
                        }
                        EnsureRaidVehicleFaction(vehicle, faction);

                        MethodInfo tryAddPawn = AccessTools.Method(vehicle.GetType(), "TryAddPawn", new[] { typeof(Pawn) });
                        PropertyInfo seatsAvailable = AccessTools.Property(vehicle.GetType(), "SeatsAvailable");
                        if (tryAddPawn == null || seatsAvailable == null)
                        {
                            lastFailure = "Vehicle Framework crew boarding APIs could not be found";
                            vehicle.Destroy(DestroyMode.Vanish);
                            continue;
                        }

                        int pawnIndex = 0;
                        PropertyInfo hasEnoughOperators = AccessTools.Property(vehicle.GetType(), "HasEnoughOperators");
                        while (CountAvailableVehicleCrewPawns(availableRaidPawns) > MinimumGroundRaidPawns
                            && Convert.ToInt32(seatsAvailable.GetValue(vehicle, null)) > 0
                            && pawnIndex < availableRaidPawns.Count)
                        {
                            Pawn pawn = availableRaidPawns[pawnIndex];
                            if (!CanCrewRaidVehicle(pawn))
                            {
                                pawnIndex++;
                                continue;
                            }

                            if (pawn.Spawned)
                            {
                                originalSpawnPositions[pawn] = pawn.Position;
                            }

                            if (tryAddPawn.Invoke(vehicle, new object[] { pawn }) is bool added && added)
                            {
                                availableRaidPawns.RemoveAt(pawnIndex);
                            }
                            else
                            {
                                originalSpawnPositions.Remove(pawn);
                                pawnIndex++;
                            }
                        }

                        bool hasCrew = false;
                        foreach (Pawn _ in GetVehiclePassengers(vehicle))
                        {
                            hasCrew = true;
                            break;
                        }

                        if (!hasCrew
                            || hasEnoughOperators?.GetValue(vehicle, null) is not bool hasOperator || !hasOperator)
                        {
                            lastFailure = "the aircraft could not be crewed while preserving "
                                + MinimumGroundRaidPawns + " ground raiders";
                            RestoreAirborneCrew(vehicle, map, availableRaidPawns, originalSpawnPositions);
                            vehicle.Destroy(DestroyMode.Vanish);
                            continue;
                        }

                        if (!TryBeginAirborneLanding(vehicle, map, reservedLandingCells, out string landingFailure))
                        {
                            lastFailure = landingFailure;
                            RestoreAirborneCrew(vehicle, map, availableRaidPawns, originalSpawnPositions);
                            vehicle.Destroy(DestroyMode.Vanish);
                            continue;
                        }

                        spawnedVehicle = vehicle;
                        failureReason = null;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        lastFailure = "deployment of " + vehicleDef + " threw "
                            + ex.GetType().Name + ": " + ex.Message;
                        if (vehicle != null && !vehicle.Destroyed)
                        {
                            RestoreAirborneCrew(vehicle, map, availableRaidPawns, originalSpawnPositions);
                            vehicle.Destroy(DestroyMode.Vanish);
                        }
                    }
                }

                failureReason = "No eligible aircraft could be deployed after trying "
                    + eligibleDefs.Count + " candidate(s): " + lastFailure + ".";
                Log.Warning("[The Marked Men] " + failureReason);
                return false;
            }

            private static List<object> GetEligibleAirborneRaidVehicleDefs(
                Faction faction,
                float points,
                float budgetMultiplier,
                PawnsArrivalModeDef arrivalMode)
            {
                List<object> eligible = new List<object>();
                Assembly assembly = GetVehicleFrameworkAssembly();
                if (assembly == null || faction?.def?.modExtensions == null || points < 3000f)
                {
                    return eligible;
                }

                try
                {
                    Type extensionType = assembly.GetType(VehicleRaidExtensionTypeFullName);
                    Type vehicleDefType = assembly.GetType(VehicleDefTypeFullName);
                    if (extensionType == null || vehicleDefType == null)
                    {
                        LogRaidLookupFailure("Vehicle Framework aircraft raid types could not be found.");
                        return eligible;
                    }

                    object extension = faction.def.modExtensions.Find(candidate => candidate.GetType() == extensionType);
                    if (extension == null)
                    {
                        LogRaidLookupFailure("The Marked Men faction is missing its optional vehicle-raider extension.");
                        return eligible;
                    }

                    FieldInfo pointMultiplierField = extensionType.GetField("pointMultiplier");
                    float pointMultiplier = pointMultiplierField == null
                        ? 1f
                        : Convert.ToSingle(pointMultiplierField.GetValue(extension));
                    float vehicleBudget = pointMultiplier * (points - 250f) / 2f * Mathf.Max(0f, budgetMultiplier);
                    if (vehicleBudget <= 0f)
                    {
                        return eligible;
                    }

                    if (!CacheAirborneVehicleDefs(vehicleDefType))
                    {
                        LogRaidLookupFailure("Vehicle Framework aircraft eligibility data could not be accessed.");
                        return eligible;
                    }

                    PawnsArrivalModeDef resolvedArrivalMode = arrivalMode ?? PawnsArrivalModeDefOf.EdgeWalkInGroups;
                    int factionTechLevel = Convert.ToInt32(faction.def.techLevel);
                    Type npcPropertiesType = null;
                    Type allowsRaidType = null;
                    FieldInfo raidParamsField = null;
                    MethodInfo allowsRaid = null;
                    for (int i = 0; i < airborneVehicleDefs.Count; i++)
                    {
                        object vehicleDef = airborneVehicleDefs[i];
                        if (Convert.ToSingle(airborneVehicleCombatPowerField.GetValue(vehicleDef)) > vehicleBudget)
                        {
                            continue;
                        }

                        if (airborneVehicleTechLevelField != null
                            && factionTechLevel < Convert.ToInt32(airborneVehicleTechLevelField.GetValue(vehicleDef)))
                        {
                            continue;
                        }

                        object npcProperties = airborneVehicleNpcPropertiesField.GetValue(vehicleDef);
                        Type currentRaidParamsType = npcProperties?.GetType();
                        if (currentRaidParamsType != null && currentRaidParamsType != npcPropertiesType)
                        {
                            npcPropertiesType = currentRaidParamsType;
                            raidParamsField = AccessTools.Field(npcPropertiesType, "raidParams");
                        }

                        object raidParams = raidParamsField?.GetValue(npcProperties);
                        if (raidParams != null)
                        {
                            if (raidParams.GetType() != allowsRaidType)
                            {
                                allowsRaidType = raidParams.GetType();
                                allowsRaid = AccessTools.Method(
                                    allowsRaidType,
                                    "Allows",
                                    new[] { typeof(Faction), typeof(PawnsArrivalModeDef) });
                            }

                            if (allowsRaid == null)
                            {
                                LogRaidLookupFailure("Vehicle Framework raid arrival restrictions could not be checked.");
                                continue;
                            }

                            object allowed = allowsRaid.Invoke(raidParams, new object[] { faction, resolvedArrivalMode });
                            if (allowed is not bool allowedForRaid || !allowedForRaid)
                            {
                                continue;
                            }
                        }

                        eligible.Add(vehicleDef);
                    }
                }
                catch (Exception ex)
                {
                    LogRaidLookupFailure("Could not check Vehicle Framework aircraft: " + ex);
                }

                return eligible;
            }

            private static bool CacheAirborneVehicleDefs(Type vehicleDefType)
            {
                if (airborneVehicleDefsCached)
                {
                    return airborneVehicleDefs != null;
                }

                Type databaseType = typeof(DefDatabase<>).MakeGenericType(vehicleDefType);
                IEnumerable definitions = databaseType.GetProperty("AllDefsListForReading")
                    ?.GetValue(null, null) as IEnumerable;
                FieldInfo typeField = AccessTools.Field(vehicleDefType, "type");
                FieldInfo categoryField = AccessTools.Field(vehicleDefType, "vehicleCategory");
                FieldInfo enabledField = AccessTools.Field(vehicleDefType, "enabled");
                FieldInfo combatPowerField = AccessTools.Field(vehicleDefType, "combatPower");
                FieldInfo techLevelField = AccessTools.Field(vehicleDefType, "techLevel");
                FieldInfo npcPropertiesField = AccessTools.Field(vehicleDefType, "npcProperties");
                FieldInfo componentsField = AccessTools.Field(vehicleDefType, "comps");
                if (definitions == null || typeField == null || categoryField == null
                    || enabledField == null || combatPowerField == null
                    || npcPropertiesField == null || componentsField == null)
                {
                    return false;
                }

                List<object> eligibleDefs = new List<object>();
                foreach (object vehicleDef in definitions)
                {
                    if (!string.Equals(Convert.ToString(typeField.GetValue(vehicleDef)), "Air", StringComparison.Ordinal)
                        || !IsSupportedAirborneRaidVehicle(vehicleDef)
                        || (Convert.ToInt32(categoryField.GetValue(vehicleDef)) & 5) == 0
                        || (Convert.ToInt32(enabledField.GetValue(vehicleDef)) & 2) == 0
                        || npcPropertiesField.GetValue(vehicleDef) == null
                        || !HasVehicleLauncher(componentsField.GetValue(vehicleDef)))
                    {
                        continue;
                    }

                    eligibleDefs.Add(vehicleDef);
                }

                airborneVehicleDefs = eligibleDefs;
                airborneVehicleCombatPowerField = combatPowerField;
                airborneVehicleTechLevelField = techLevelField;
                airborneVehicleNpcPropertiesField = npcPropertiesField;
                airborneVehicleDefsCached = true;
                return true;
            }

            private static bool IsSupportedAirborneRaidVehicle(object vehicleDef)
            {
                string defName = Convert.ToString(AccessTools.Field(vehicleDef.GetType(), "defName")?.GetValue(vehicleDef));
                return string.Equals(defName, MosquitoVehicleDefName, StringComparison.Ordinal)
                    || string.Equals(defName, LouieVehicleDefName, StringComparison.Ordinal);
            }

            private static bool HasVehicleHoverComponent(object components)
            {
                if (components is not IEnumerable componentList)
                {
                    return false;
                }

                foreach (object component in componentList)
                {
                    if (component?.GetType().FullName != "VehicleRaid.CompProperties_VehicleHover")
                    {
                        continue;
                    }

                    FieldInfo flightTypeField = AccessTools.Field(component.GetType(), "flightType");
                    return flightTypeField != null
                        && string.Equals(
                            Convert.ToString(flightTypeField.GetValue(component)),
                            "Hover",
                            StringComparison.Ordinal);
                }

                return false;
            }

            private static bool HasVehicleTurrets(object components)
            {
                if (components is not IEnumerable componentList)
                {
                    return false;
                }

                foreach (object component in componentList)
                {
                    if (component?.GetType().FullName == "Vehicles.CompProperties_VehicleTurrets")
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool HasVehicleLauncher(object components)
            {
                if (components is not IEnumerable componentList)
                {
                    return false;
                }

                foreach (object component in componentList)
                {
                    if (component?.GetType().FullName == "Vehicles.CompProperties_VehicleLauncher")
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool TryBeginAirborneLanding(
                Pawn vehicle,
                Map map,
                HashSet<IntVec3> reservedLandingCells,
                out string failureReason)
            {
                failureReason = "Could not start the aircraft landing.";
                Assembly assembly = GetVehicleFrameworkAssembly();
                Type launcherType = assembly?.GetType("Vehicles.CompVehicleLauncher");
                Type skyfallerMakerType = assembly?.GetType("Vehicles.VehicleSkyfallerMaker");
                Type vehicleSkyfallerType = assembly?.GetType("Vehicles.VehicleSkyfaller");
                Type arrivingSkyfallerType = assembly?.GetType("Vehicles.VehicleSkyfaller_Arriving");
                Type mapHelperType = assembly?.GetType("Vehicles.MapHelper");
                Type vehiclePawnType = assembly?.GetType(VehiclePawnTypeFullName);
                PropertyInfo launcherProperty = vehicle == null
                    ? null
                    : AccessTools.Property(vehicle.GetType(), "CompVehicleLauncher");
                object launcher = launcherProperty?.GetValue(vehicle, null);
                FieldInfo protocolField = launcher == null
                    ? null
                    : AccessTools.Field(launcher.GetType(), "launchProtocol");
                object protocol = protocolField?.GetValue(launcher);
                object launcherProps = launcher == null
                    ? null
                    : AccessTools.Property(launcher.GetType(), "Props")?.GetValue(launcher, null);
                ThingDef incomingSkyfaller = launcherProps == null
                    ? null
                    : AccessTools.Field(launcherProps.GetType(), "skyfallerIncoming")?.GetValue(launcherProps) as ThingDef;
                ThingDef markedMenIncomingSkyfaller = DefDatabase<ThingDef>.GetNamedSilentFail("CA_VF_AircraftIncoming");
                if (arrivingSkyfallerType != null
                    && (incomingSkyfaller?.thingClass == null
                        || !arrivingSkyfallerType.IsAssignableFrom(incomingSkyfaller.thingClass)))
                {
                    incomingSkyfaller = markedMenIncomingSkyfaller;
                }
                MethodInfo makeSkyfaller = skyfallerMakerType == null || vehiclePawnType == null
                    ? null
                    : AccessTools.Method(skyfallerMakerType, "MakeSkyfaller", new[] { typeof(ThingDef), vehiclePawnType });
                MethodInfo blocked = mapHelperType == null
                    ? null
                    : AccessTools.Method(mapHelperType, "ImpassableOrVehicleBlocked");
                MethodInfo nonStandable = mapHelperType == null
                    ? null
                    : AccessTools.Method(mapHelperType, "NonStandableOrVehicleBlocked");
                if (launcherType == null || launcher == null || protocol == null || vehicleSkyfallerType == null
                    || arrivingSkyfallerType == null
                    || incomingSkyfaller == null || makeSkyfaller == null
                    || blocked == null || nonStandable == null)
                {
                    failureReason = "Vehicle Framework aircraft landing APIs or the aircraft's incoming skyfaller are unavailable.";
                    LogRaidLookupFailure(failureReason);
                    return false;
                }

                Rot4 rotation = Rot4.Random;
                List<IntVec3> landingCells = FindOpenAirLandingCells(vehicle, map, rotation, blocked, nonStandable);
                if (reservedLandingCells != null)
                {
                    landingCells.RemoveAll(reservedLandingCells.Contains);
                }

                if (landingCells.Count == 0)
                {
                    failureReason = "No safe open-air landing cell was found for the aircraft's size and map.";
                    Log.Warning("[The Marked Men] " + failureReason);
                    return false;
                }

                Thing skyfaller = makeSkyfaller.Invoke(null, new object[] { incomingSkyfaller, vehicle }) as Thing;
                if (skyfaller == null)
                {
                    failureReason = "Vehicle Framework could not create the aircraft landing skyfaller.";
                    Log.Warning("[The Marked Men] " + failureReason);
                    return false;
                }

                IntVec3 landingCell = landingCells[Rand.Range(0, landingCells.Count)];
                FieldInfo inFlightField = AccessTools.Field(launcherType, "inFlight");
                object previousInFlight = inFlightField?.GetValue(launcher);
                inFlightField?.SetValue(launcher, true);
                try
                {
                    // VehicleSkyfaller_Arriving.SpawnSetup prepares and starts the landing protocol.
                    GenSpawn.Spawn(skyfaller, landingCell, map, rotation);
                }
                catch
                {
                    inFlightField?.SetValue(launcher, previousInFlight ?? false);
                    throw;
                }

                if (skyfaller.Spawned
                    && arrivingSkyfallerType.IsInstanceOfType(skyfaller)
                    && !vehicle.Spawned)
                {
                    reservedLandingCells?.Add(landingCell);
                    Log.Message("[The Marked Men] Started Vehicle Framework aircraft landing for "
                        + vehicle.def.defName + " at " + landingCell + " using " + incomingSkyfaller.defName + ".");
                    failureReason = null;
                    return true;
                }

                bool skyfallerSpawned = skyfaller.Spawned;
                bool vehicleStillSpawned = vehicle.Spawned;
                skyfaller.Destroy(DestroyMode.Vanish);
                inFlightField?.SetValue(launcher, previousInFlight ?? false);
                failureReason = "Vehicle Framework aircraft arrival did not complete setup "
                    + "(skyfaller spawned: " + skyfallerSpawned
                    + ", vehicle still spawned: " + vehicleStillSpawned
                    + ", skyfaller type: " + skyfaller.GetType().FullName + ").";
                return false;
            }

            private static List<IntVec3> FindOpenAirLandingCells(
                Pawn vehicle,
                Map map,
                Rot4 rotation,
                MethodInfo blocked,
                MethodInfo nonStandable)
            {
                List<IntVec3> candidates = new List<IntVec3>();
                List<IntVec3> assaultApproachCandidates = new List<IntVec3>();
                if (map == null || !map.Center.IsValid)
                {
                    return candidates;
                }

                float radius = Mathf.Max(1f, Mathf.Min(50f, Mathf.Min(map.Size.x, map.Size.z) * 0.45f));
                List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(map.Center, radius, true))
                {
                    if (!cell.InBounds(map) || cell.GetRoof(map) != null)
                    {
                        continue;
                    }

                    if (blocked.Invoke(null, new object[] { vehicle, map, cell, rotation }) is not bool isBlocked
                        || isBlocked
                        || nonStandable.Invoke(null, new object[] { vehicle, map, cell, rotation }) is not bool isNotStandable
                        || isNotStandable)
                    {
                        continue;
                    }

                    bool tooCloseToColonist = false;
                    for (int i = 0; i < colonists.Count; i++)
                    {
                        if (colonists[i].Position.DistanceToSquared(cell) < 225f)
                        {
                            tooCloseToColonist = true;
                            break;
                        }
                    }

                    if (!tooCloseToColonist)
                    {
                        candidates.Add(cell);
                        if (colonists.Count > 0)
                        {
                            float nearestColonistDistanceSquared = float.MaxValue;
                            for (int i = 0; i < colonists.Count; i++)
                            {
                                nearestColonistDistanceSquared = Mathf.Min(
                                    nearestColonistDistanceSquared,
                                    colonists[i].Position.DistanceToSquared(cell));
                            }

                            if (nearestColonistDistanceSquared >= 225f
                                && nearestColonistDistanceSquared <= 1225f)
                            {
                                assaultApproachCandidates.Add(cell);
                            }
                        }
                    }
                }

                return assaultApproachCandidates.Count > 0 ? assaultApproachCandidates : candidates;
            }

            private static void RestoreAirborneCrew(
                Pawn vehicle,
                Map map,
                List<Pawn> availableRaidPawns,
                Dictionary<Pawn, IntVec3> originalSpawnPositions)
            {
                MethodInfo removePawn = AccessTools.Method(vehicle.GetType(), "RemovePawn", new[] { typeof(Pawn) });
                List<Pawn> passengers = new List<Pawn>(GetVehiclePassengers(vehicle));
                for (int i = 0; i < passengers.Count; i++)
                {
                    Pawn passenger = passengers[i];
                    removePawn?.Invoke(vehicle, new object[] { passenger });
                    if (!availableRaidPawns.Contains(passenger))
                    {
                        availableRaidPawns.Add(passenger);
                    }

                    if (originalSpawnPositions.TryGetValue(passenger, out IntVec3 originalPosition)
                        && !passenger.Spawned
                        && originalPosition.InBounds(map))
                    {
                        GenSpawn.Spawn(passenger, originalPosition, map);
                    }
                }
            }

            public static void AbortAirborneRaidVehicle(Pawn vehicle)
            {
                if (vehicle == null || vehicle.Destroyed)
                {
                    return;
                }

                Thing skyfaller = vehicle.ParentHolder as Thing;
                MethodInfo removePawn = AccessTools.Method(vehicle.GetType(), "RemovePawn", new[] { typeof(Pawn) });
                List<Pawn> passengers = new List<Pawn>(GetVehiclePassengers(vehicle));
                for (int i = 0; i < passengers.Count; i++)
                {
                    Pawn passenger = passengers[i];
                    removePawn?.Invoke(vehicle, new object[] { passenger });
                    if (!passenger.Destroyed)
                    {
                        passenger.Destroy(DestroyMode.Vanish);
                    }
                }

                skyfaller?.Destroy(DestroyMode.Vanish);
                if (!vehicle.Destroyed)
                {
                    vehicle.Destroy(DestroyMode.Vanish);
                }
            }

            public static void AttachLandedAirborneVehicle(object skyfaller)
            {
                FieldInfo vehicleField = skyfaller == null
                    ? null
                    : AccessTools.Field(skyfaller.GetType(), "vehicle");
                Pawn vehicle = vehicleField?.GetValue(skyfaller) as Pawn;
                if (vehicle == null
                    || !vehicle.Spawned
                    || vehicle.Faction?.def != CADefOf.CrossedFaction)
                {
                    return;
                }

                Faction crossed = vehicle.Faction;
                Lord currentLord = vehicle.GetLord();
                if (currentLord != null)
                {
                    return;
                }

                ActivateHoverForRaid(vehicle);

                if (TryAssignMarkedMenVehicleLord(vehicle, crossed))
                {
                    return;
                }

                foreach (Lord lord in vehicle.Map.lordManager.lords)
                {
                    if (lord.faction == crossed && lord.LordJob is LordJob_CrossedAssault)
                    {
                        lord.AddPawn(vehicle);
                        return;
                    }
                }

                LordMaker.MakeNewLord(
                    crossed,
                    CrossedRaidTactics.CreateAssaultLordJob(crossed, 3000f, new[] { vehicle }),
                    vehicle.Map,
                    new[] { vehicle });
            }

            private static void ActivateHoverForRaid(Pawn vehicle)
            {
                if (vehicle?.AllComps == null)
                {
                    return;
                }

                foreach (ThingComp comp in vehicle.AllComps)
                {
                    if (comp?.GetType().FullName != "VehicleRaid.CompVehicleHover")
                    {
                        continue;
                    }

                    FieldInfo stateField = AccessTools.Field(comp.GetType(), "State");
                    MethodInfo activateHover = AccessTools.Method(comp.GetType(), "ActivateHoverNPC", Type.EmptyTypes);
                    if (stateField == null || activateHover == null)
                    {
                        Log.Warning("[The Marked Men] Could not activate the landed helicopter's VehicleRaid hover component.");
                        return;
                    }

                    if (string.Equals(Convert.ToString(stateField.GetValue(comp)), "Grounded", StringComparison.Ordinal))
                    {
                        activateHover.Invoke(comp, null);
                    }

                    if (!string.Equals(Convert.ToString(stateField.GetValue(comp)), "Hovering", StringComparison.Ordinal))
                    {
                        Log.Warning("[The Marked Men] The landed helicopter did not enter VehicleRaid's Hovering state.");
                    }

                    return;
                }

                Log.Warning("[The Marked Men] The landed aircraft has no VehicleRaid hover component.");
            }

            public static bool TryAssignMarkedMenVehicleLord(Pawn vehicle, Faction faction)
            {
                if (vehicle == null || !vehicle.Spawned || faction == null || vehicle.Map == null)
                {
                    return false;
                }
                
                LordMaker.MakeNewLord(
                    faction,
                    new LordJob_CrossedVehicleAssault(faction),
                    vehicle.Map,
                    new[] { vehicle });
                Log.Message("[The Marked Men] Assigned vehicle " + vehicle.def.defName
                    + " to The Marked Men vehicle assault AI.");
                return true;
            }

        private static List<object> GetEligibleRaidVehicleDefs(
            Faction faction,
            float points,
            PawnsArrivalModeDef arrivalMode,
            Assembly assembly)
        {
            List<object> eligible = new List<object>();
            if (assembly == null || faction?.def?.modExtensions == null)
            {
                return eligible;
            }

            try
            {
                Type extensionType = assembly.GetType(VehicleRaidExtensionTypeFullName);
                Type vehicleDefType = assembly.GetType(VehicleDefTypeFullName);
                Type categoryType = assembly.GetType("Vehicles.VehicleCategory");
                Type helperType = assembly.GetType(VehicleRaidHelperTypeFullName);
                if (extensionType == null || vehicleDefType == null || categoryType == null || helperType == null)
                {
                    LogRaidLookupFailure("Vehicle Framework raid API types could not be found.");
                    return eligible;
                }

                object extension = faction.def.modExtensions.Find(candidate => candidate.GetType() == extensionType);
                if (extension == null)
                {
                    LogRaidLookupFailure("The Marked Men faction is missing its optional vehicle-raider extension.");
                    return eligible;
                }

                FieldInfo pointMultiplierField = extensionType.GetField("pointMultiplier");
                float pointMultiplier = pointMultiplierField == null
                    ? 1f
                    : Convert.ToSingle(pointMultiplierField.GetValue(extension));
                float vehicleBudget = pointMultiplier * (points - 250f) / 2f;
                if (vehicleBudget <= 0f)
                {
                    return eligible;
                }

                Type databaseType = typeof(DefDatabase<>).MakeGenericType(vehicleDefType);
                PropertyInfo definitionsProperty = databaseType.GetProperty("AllDefsListForReading");
                IEnumerable definitions = definitionsProperty?.GetValue(null, null) as IEnumerable;
                MethodInfo validVehicle = AccessTools.Method(helperType, "ValidRaiderVehicle");
                if (definitions == null || validVehicle == null)
                {
                    LogRaidLookupFailure("Vehicle Framework raid eligibility data could not be accessed.");
                    return eligible;
                }

                object combatCategory = Enum.Parse(categoryType, "Combat");
                PawnsArrivalModeDef resolvedArrivalMode = arrivalMode ?? PawnsArrivalModeDefOf.EdgeWalkInGroups;
                foreach (object vehicleDef in definitions)
                {
                    if (validVehicle.Invoke(null, new object[]
                        {
                            vehicleDef,
                            combatCategory,
                            resolvedArrivalMode,
                            faction,
                            vehicleBudget
                        }) is bool valid && valid)
                    {
                        eligible.Add(vehicleDef);
                    }
                }
            }
            catch (Exception ex)
            {
                LogRaidLookupFailure("Could not check Vehicle Framework raid vehicles: " + ex);
            }

            return eligible;
        }

        private static int GetGroundRaidVehicleCount(float points)
        {
            float count;
            if (points <= 1000f)
            {
                count = 0f;
            }
            else if (points <= 3000f)
            {
                count = (points - 1000f) / 2000f;
            }
            else if (points <= 5000f)
            {
                count = 1f + (points - 3000f) / 2000f;
            }
            else
            {
                count = 2f + (points - 5000f) * 3f / 15000f;
            }

            int baseCount = Mathf.Max(0, Mathf.FloorToInt(count));
            return baseCount > 0 ? Mathf.Min(10, baseCount + 1) : 0;
        }

        private static bool CanCrewRaidVehicle(Pawn pawn)
        {
            return pawn != null
                && !pawn.Destroyed
                && pawn.RaceProps.Humanlike
                && pawn.kindDef != CADefOf.MarkedMan;
        }

        private static int CountAvailableVehicleCrewPawns(List<Pawn> pawns)
        {
            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (CanCrewRaidVehicle(pawns[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static void RestoreGroundRaidCrew(
            Pawn vehicle,
            Map map,
            List<Pawn> availableRaidPawns,
            Dictionary<Pawn, IntVec3> originalSpawnPositions)
        {
            MethodInfo removePawn = AccessTools.Method(vehicle.GetType(), "RemovePawn", new[] { typeof(Pawn) });
            List<Pawn> passengers = new List<Pawn>(GetVehiclePassengers(vehicle));
            for (int i = 0; i < passengers.Count; i++)
            {
                Pawn passenger = passengers[i];
                removePawn?.Invoke(vehicle, new object[] { passenger });
                if (!availableRaidPawns.Contains(passenger))
                {
                    availableRaidPawns.Add(passenger);
                }

                if (originalSpawnPositions.TryGetValue(passenger, out IntVec3 originalPosition)
                    && !passenger.Destroyed
                    && !passenger.Spawned
                    && originalPosition.InBounds(map))
                {
                    GenSpawn.Spawn(passenger, originalPosition, map);
                }
            }
        }

        public static IEnumerable<Pawn> GetVehiclePassengers(Pawn pawn)
        {
            if (pawn == null || !IsVehicleFrameworkLoaded
                || pawn.GetType().FullName != VehiclePawnTypeFullName)
            {
                yield break;
            }

            PropertyInfo passengersProperty = AccessTools.Property(pawn.GetType(), "AllPawnsAboard");
            if (passengersProperty?.GetValue(pawn, null) is IEnumerable<Pawn> passengers)
            {
                foreach (Pawn passenger in passengers)
                {
                    yield return passenger;
                }
            }
        }

        public static void TryDisembarkRaidPassengers(Pawn vehicle, float landRange, float airborneRange)
            {
                if (!IsVehiclePawn(vehicle) || !vehicle.Spawned || vehicle.Map == null || vehicle.GetLord() == null)
                {
                    return;
                }

                bool airborne = vehicle.AllComps != null && vehicle.AllComps.Any(comp =>
                    comp?.GetType().Name.IndexOf("Flying", StringComparison.OrdinalIgnoreCase) >= 0
                    || comp?.GetType().Name.IndexOf("Air", StringComparison.OrdinalIgnoreCase) >= 0);
                float range = airborne ? airborneRange : landRange;
                Pawn nearestEnemy = vehicle.Map.mapPawns.AllPawnsSpawned
                    .Where(pawn => pawn != null
                        && !pawn.Dead
                        && pawn.HostileTo(vehicle)
                        && pawn.Position.DistanceToSquared(vehicle.Position) <= range * range)
                    .OrderBy(pawn => pawn.Position.DistanceToSquared(vehicle.Position))
                    .FirstOrFallback();
                bool emergencyEvacuation = IsVehicleEmergencyEvacuation(vehicle);
                if (nearestEnemy == null && !emergencyEvacuation)
                {
                    return;
                }

                if (nearestEnemy == null && emergencyEvacuation)
                {
                    nearestEnemy = vehicle.Map.mapPawns.AllPawnsSpawned
                        .Where(pawn => pawn != null && !pawn.Dead && pawn.HostileTo(vehicle))
                        .OrderBy(pawn => pawn.Position.DistanceToSquared(vehicle.Position))
                        .FirstOrFallback();
                }

                if (nearestEnemy == null)
                {
                    return;
                }

                List<Pawn> passengers = new List<Pawn>(GetVehiclePassengers(vehicle));
                if (passengers.Count <= 1)
                {
                    return;
                }

                MethodInfo removePawn = AccessTools.Method(vehicle.GetType(), "RemovePawn", new[] { typeof(Pawn) });
                PropertyInfo hasEnoughOperators = AccessTools.Property(vehicle.GetType(), "HasEnoughOperators");
                if (removePawn == null)
                {
                    return;
                }

                for (int i = passengers.Count - 1; i >= 0; i--)
                {
                    Pawn passenger = passengers[i];
                    if (IsRequiredVehicleCrew(vehicle, passenger))
                    {
                        continue;
                    }

                    if (!TryFindDisembarkCell(vehicle, out IntVec3 cell))
                    {
                        return;
                    }

                    object removed = removePawn.Invoke(vehicle, new object[] { passenger });
                    bool removedSuccessfully = removed is bool removedResult && removedResult;
                    bool enoughOperators = hasEnoughOperators?.GetValue(vehicle, null) is not bool hasOperators || hasOperators;
                    if (!removedSuccessfully || !enoughOperators)
                    {
                        if (removedSuccessfully)
                        {
                            MethodInfo addPawn = AccessTools.Method(vehicle.GetType(), "TryAddPawn", new[] { typeof(Pawn) });
                            addPawn?.Invoke(vehicle, new object[] { passenger });
                        }
                        continue;
                    }

                    GenSpawn.Spawn(passenger, cell, vehicle.Map);
                    passenger.jobs?.EndCurrentJob(JobCondition.InterruptForced);
                    passenger.mindState?.duty = new PawnDuty(DutyDefOf.AssaultColony);
                    EnsureRaidPassengerAssaultLord(passenger, vehicle);
                    passenger.jobs?.StartJob(
                        JobMaker.MakeJob(JobDefOf.Goto, nearestEnemy.Position),
                        JobCondition.InterruptForced);
                }
            }

            private static bool IsVehicleEmergencyEvacuation(Pawn vehicle)
            {
                float damageThreshold = TheMarkedMenMod.Settings?.vehicleEmergencyDamageThreshold ?? 35f;
                if (vehicle.MaxHitPoints > 0
                    && 100f * vehicle.HitPoints / vehicle.MaxHitPoints <= Mathf.Clamp(damageThreshold, 5f, 90f))
                {
                    return true;
                }

                float? fuelPercent = TryGetVehicleFuelPercent(vehicle);
                return fuelPercent.HasValue && fuelPercent.Value <= 0.01f;
            }

            private static float? TryGetVehicleFuelPercent(Pawn vehicle)
            {
                if (vehicle?.AllComps == null)
                {
                    return null;
                }

                foreach (ThingComp comp in vehicle.AllComps)
                {
                    if (comp == null)
                    {
                        continue;
                    }

                    string fullName = comp.GetType().FullName ?? string.Empty;
                    if (!fullName.Contains("Refuelable")
                        && !fullName.Contains("VehicleFuel")
                        && !fullName.Contains("Fuel"))
                    {
                        continue;
                    }

                    object percent = ReadMember(comp, "FuelPercentOfMax", "FuelPercent", "PercentFull");
                    if (TryConvertFloat(percent, out float fuelPercent))
                    {
                        return Mathf.Clamp01(fuelPercent);
                    }

                    object fuel = ReadMember(comp, "Fuel", "fuel");
                    object fuelCapacity = ReadMember(comp, "FuelCapacity", "Props", "capacity");
                    if (TryConvertFloat(fuel, out float fuelAmount)
                        && TryConvertFloat(fuelCapacity, out float capacity)
                        && capacity > 0f)
                    {
                        return Mathf.Clamp01(fuelAmount / capacity);
                    }
                }

                return null;
            }

            private static bool IsRequiredVehicleCrew(Pawn vehicle, Pawn pawn)
            {
                if (vehicle == null || pawn == null)
                {
                    return false;
                }

                object handlers = ReadMember(vehicle, "handlers", "Handlers", "vehicleHandlers", "VehicleHandlers");
                IDictionary handlerDictionary = handlers as IDictionary;
                IEnumerable handlerValues = handlerDictionary != null
                    ? handlerDictionary.Values
                    : handlers as IEnumerable;
                if (handlerValues == null)
                {
                    return false;
                }

                foreach (object handler in handlerValues)
                {
                    if (handler == null || !HandlerOccupiesPawn(handler, pawn))
                    {
                        continue;
                    }

                    string role = DescribeRequiredCrewRole(handler, vehicle);
                    if (role.ToLowerInvariant().IndexOf("passenger", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    }
                    string requiredRole = role.ToLowerInvariant();
                    if (requiredRole.Contains("driver")
                        || requiredRole.Contains("pilot")
                        || requiredRole.Contains("operator")
                        || requiredRole.Contains("gunner")
                        || requiredRole.Contains("turret"))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool HandlerOccupiesPawn(object handler, Pawn pawn)
            {
                object occupants = ReadMember(handler, "occupants", "Occupants", "occupiedPawns", "OccupiedPawns", "pawn", "Pawn", "occupiedBy", "OccupiedBy");
                if (occupants == pawn)
                {
                    return true;
                }

                IEnumerable occupantsEnumerable = occupants as IEnumerable;
                if (occupantsEnumerable != null)
                {
                    foreach (object occupant in occupantsEnumerable)
                    {
                        if (occupant == pawn)
                        {
                            return true;
                        }
                    }
                }

                if (ContainsPawnReference(handler, pawn, new HashSet<object>(), 0))
                {
                    return true;
                }

                return false;
            }

            private static string DescribeRequiredCrewRole(object handler, Pawn vehicle)
            {
                string role = Convert.ToString(
                    ReadMember(handler, "handlingType", "HandlingType", "role", "Role", "roleType", "RoleType"))
                    ?? string.Empty;
                role += " " + handler.GetType().Name + " " + GetHandlerRoleText(handler);

                object handlers = ReadMember(vehicle, "handlers", "Handlers", "vehicleHandlers", "VehicleHandlers");
                if (handlers != null)
                {
                    role += " " + handlers.GetType().Name;
                }

                return role;
            }

            private static string GetHandlerRoleText(object handler)
            {
                string result = string.Empty;
                Type type = handler.GetType();
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    result += " " + field.Name;
                    if (field.FieldType.IsEnum || field.FieldType == typeof(string))
                    {
                        try
                        {
                            result += " " + Convert.ToString(field.GetValue(handler));
                        }
                        catch
                        {
                        }
                    }
                }
                return result;
            }

            private static bool ContainsPawnReference(object value, Pawn pawn, HashSet<object> visited, int depth)
            {
                if (value == null || depth > 5)
                {
                    return false;
                }

                if (value == pawn)
                {
                    return true;
                }

                if (!value.GetType().IsValueType)
                {
                    if (!visited.Add(value))
                    {
                        return false;
                    }
                }

                if (value is IEnumerable enumerable && !(value is string))
                {
                    foreach (object item in enumerable)
                    {
                        if (ContainsPawnReference(item, pawn, visited, depth + 1))
                        {
                            return true;
                        }
                    }
                }

                foreach (FieldInfo field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.FieldType.IsPrimitive || field.FieldType == typeof(string))
                    {
                        continue;
                    }

                    try
                    {
                        if (ContainsPawnReference(field.GetValue(value), pawn, visited, depth + 1))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }

                return false;
            }

            private static object ReadMember(object instance, params string[] names)
            {
                if (instance == null || names == null)
                {
                    return null;
                }

                Type type = instance.GetType();
                foreach (string name in names)
                {
                    FieldInfo field = AccessTools.Field(type, name);
                    if (field != null)
                    {
                        return field.GetValue(instance);
                    }

                    PropertyInfo property = AccessTools.Property(type, name);
                    if (property?.GetIndexParameters().Length == 0)
                    {
                        try
                        {
                            return property.GetValue(instance, null);
                        }
                        catch
                        {
                        }
                    }
                }

                return null;
            }

            private static bool TryConvertFloat(object value, out float result)
            {
                try
                {
                    if (value != null)
                    {
                        result = Convert.ToSingle(value);
                        return true;
                    }
                }
                catch
                {
                }

                result = 0f;
                return false;
            }

            private static void EnsureRaidPassengerAssaultLord(Pawn passenger, Pawn vehicle)
            {
                if (passenger == null || vehicle?.Map == null || vehicle.Faction == null)
                {
                    return;
                }

                Lord assaultLord = passenger.GetLord();
                if (assaultLord?.LordJob is LordJob_CrossedAssault)
                {
                    assaultLord.AddPawn(passenger);
                }
                else
                {
                    assaultLord = null;
                    foreach (Lord lord in vehicle.Map.lordManager.lords)
                    {
                        if (lord?.faction == vehicle.Faction
                            && lord.LordJob is LordJob_CrossedAssault
                            && !lord.ownedPawns.Contains(passenger))
                        {
                            assaultLord = lord;
                            break;
                        }
                    }

                    if (assaultLord != null)
                    {
                        assaultLord.AddPawn(passenger);
                    }
                    else
                    {
                        LordMaker.MakeNewLord(
                            vehicle.Faction,
                            CrossedRaidTactics.CreateAssaultLordJob(vehicle.Faction, 0f),
                            vehicle.Map,
                            new[] { passenger });
                    }
                }

                passenger.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
            }

            private static bool TryFindDisembarkCell(Pawn vehicle, out IntVec3 cell)
            {
                cell = IntVec3.Invalid;
                return CellFinder.TryFindRandomCellNear(vehicle.Position, vehicle.Map, 4, candidate =>
                    candidate.InBounds(vehicle.Map)
                    && candidate.Walkable(vehicle.Map)
                    && candidate.GetFirstPawn(vehicle.Map) == null, out cell);
            }

        private static void EnsureRaidVehicleFaction(Pawn vehicle, Faction faction)
        {
            if (vehicle == null || faction == null || faction == Faction.OfPlayer)
            {
                return;
            }

            if (vehicle.Faction != faction)
            {
                vehicle.SetFaction(faction);
            }
        }

        private static Assembly GetVehicleFrameworkAssembly()
        {
            if (vehicleFrameworkAssembly != null)
            {
                return vehicleFrameworkAssembly;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (assemblies[i].GetType(VehicleDefTypeFullName) != null)
                {
                    vehicleFrameworkAssembly = assemblies[i];
                    return vehicleFrameworkAssembly;
                }
            }

            return null;
        }

        private static void LogRaidLookupFailure(string message)
        {
            if (raidLookupFailureLogged)
            {
                return;
            }

            Log.Warning("[The Marked Men] " + message);
            raidLookupFailureLogged = true;
        }

        /// <summary>
        /// Check if a pawn is inside a vehicle (as a passenger).
        /// </summary>
        public static bool IsInVehicle(Pawn pawn)
        {
            if (!isVehicleFrameworkActive || pawn == null)
            {
                return false;
            }

            try
            {
                // Check if pawn's parent is a vehicle
                if (pawn.ParentHolder is IThingHolder holder)
                {
                    Thing parent = holder as Thing;
                    if (parent != null && vehicleType.IsInstanceOfType(parent))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get the vehicle that a pawn is riding in, if any.
        /// </summary>
        public static dynamic GetVehicleForPawn(Pawn pawn)
        {
            if (!isVehicleFrameworkActive || pawn == null)
            {
                return null;
            }

            try
            {
                if (pawn.ParentHolder is IThingHolder holder)
                {
                    Thing parent = holder as Thing;
                    if (parent != null && vehicleType.IsInstanceOfType(parent))
                    {
                        return parent;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get all passengers in a vehicle.
        /// </summary>
        public static List<Pawn> GetVehiclePassengers(dynamic vehicle)
        {
            if (!isVehicleFrameworkActive || vehicle == null || getPassengersMethod == null)
            {
                return new List<Pawn>();
            }

            try
            {
                var result = getPassengersMethod.Invoke(vehicle, null) as List<Pawn>;
                return result ?? new List<Pawn>();
            }
            catch
            {
                return new List<Pawn>();
            }
        }

        /// <summary>
        /// Check if infected can ride in a specific vehicle.
        /// Some vehicles may be restricted (e.g., player-controlled transports).
        /// </summary>
        public static bool CanInfectedRideVehicle(dynamic vehicle)
        {
            if (!isVehicleFrameworkActive || vehicle == null)
            {
                return false;
            }

            try
            {
                // Vehicle Framework vehicles are generally rideable
                // Check if vehicle is owned by player faction (skip if so)
                Thing vehicleThing = vehicle as Thing;
                if (vehicleThing == null)
                {
                    return false;
                }

                // Hostile to player-owned vehicles
                Faction playerFaction = Faction.OfPlayer;
                if (vehicleThing.Faction == playerFaction)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Apply Vehicle Framework compatibility patches to a Harmony instance.
        /// </summary>
        public static void ApplyPatches(Harmony harmony)
        {
            if (!isVehicleFrameworkActive || harmony == null || vehicleType == null)
            {
                return;
            }

            try
            {
                // Patch vehicle GetGizmos to show ambush timer for vehicle-based caravans
                MethodInfo getGizmosMethod = AccessTools.Method(vehicleType, "GetGizmos");
                if (getGizmosMethod != null)
                {
                    MethodInfo patchMethod = AccessTools.Method(typeof(TheMarkedMenVehicleFrameworkCompatibility), nameof(Patch_VehicleGetGizmos));
                    harmony.Patch(getGizmosMethod, postfix: new HarmonyMethod(patchMethod));
                    Log.Message("[The Marked Men] Patched Vehicle.GetGizmos for ambush timer integration.");
                }

                // Patch vehicle TakeDamage to track infected occupants
                MethodInfo takeDamageMethod = AccessTools.Method(
                    vehicleType,
                    "TakeDamage",
                    new[] { typeof(DamageInfo), typeof(IntVec2) });
                if (takeDamageMethod != null)
                {
                    MethodInfo postfixMethod = AccessTools.Method(typeof(TheMarkedMenVehicleFrameworkCompatibility), nameof(Patch_VehicleTakeDamage));
                    harmony.Patch(takeDamageMethod, postfix: new HarmonyMethod(postfixMethod));
                    Log.Message("[The Marked Men] Patched Vehicle.TakeDamage for infected occupant tracking.");
                }

                Type arrivingSkyfallerType = GetVehicleFrameworkAssembly()
                    ?.GetType("Vehicles.VehicleSkyfaller_Arriving");
                MethodInfo finalizeLanding = arrivingSkyfallerType == null
                    ? null
                    : AccessTools.Method(arrivingSkyfallerType, "FinalizeLanding");
                MethodInfo landingPostfix = AccessTools.Method(
                    typeof(TheMarkedMenVehicleFrameworkCompatibility),
                    nameof(Patch_VehicleSkyfallerFinalizeLanding));
                if (finalizeLanding != null && landingPostfix != null)
                {
                    harmony.Patch(finalizeLanding, postfix: new HarmonyMethod(landingPostfix));
                    Log.Message("[The Marked Men] Patched Vehicle Framework aircraft landing for assault integration.");
                }
                else
                {
                    LogRaidLookupFailure("Vehicle Framework's aircraft landing completion method could not be found.");
                }

                MethodInfo disembarkPawn = AccessTools.Method(vehicleType, "DisembarkPawn", new[] { typeof(Pawn) });
                MethodInfo disembarkPostfix = AccessTools.Method(
                    typeof(TheMarkedMenVehicleFrameworkCompatibility),
                    nameof(Patch_VehicleDisembarkPawn));
                if (disembarkPawn != null && disembarkPostfix != null)
                {
                    harmony.Patch(disembarkPawn, postfix: new HarmonyMethod(disembarkPostfix));
                }
                else
                {
                    LogRaidLookupFailure("Vehicle Framework's passenger disembark method could not be found.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Vehicle Framework patch application failed: " + ex.Message);
            }
        }

        public static void Patch_VehicleSkyfallerFinalizeLanding(object __instance)
        {
            try
            {
                AttachLandedAirborneVehicle(__instance);
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Could not assign a landed assault aircraft to its raid group: " + ex);
            }
        }

        public static void Patch_VehicleDisembarkPawn(object __instance, Pawn pawn)
        {
            Pawn vehicle = __instance as Pawn;
            if (vehicle == null
                || pawn == null
                || !vehicle.Spawned
                || !pawn.Spawned
                || pawn.Map != vehicle.Map
                || pawn.Faction?.def != CADefOf.CrossedFaction)
            {
                return;
            }

            Lord assaultLord = vehicle.GetLord();
            if (assaultLord?.faction?.def != CADefOf.CrossedFaction
                || !(assaultLord.LordJob is LordJob_CrossedAssault))
            {
                assaultLord = null;
                foreach (Lord lord in vehicle.Map.lordManager.lords)
                {
                    if (lord.faction?.def == CADefOf.CrossedFaction
                        && lord.LordJob is LordJob_CrossedAssault)
                    {
                        assaultLord = lord;
                        break;
                    }
                }
            }

            if (assaultLord != null && pawn.GetLord() == null)
            {
                assaultLord.AddPawn(pawn);
            }
            pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
            pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
        }

        // Harmony patch methods
        public static void Patch_VehicleGetGizmos(dynamic __instance, ref IEnumerable<Gizmo> __result)
        {
            try
            {
                Thing vehicleThing = __instance as Thing;
                if (vehicleThing == null)
                {
                    return;
                }

                if (IsVehiclePawn(__instance as Pawn)
                    && vehicleThing.Faction != null
                    && vehicleThing.Faction != Faction.OfPlayer)
                {
                    __result = FilterPlayerVehicleCommands(__result);
                }

                // Add ambush timer if vehicle is associated with caravan
                var passengers = GetVehiclePassengers(__instance);
                bool hasPassengers = passengers != null && passengers.Count > 0;

                if (hasPassengers)
                {
                    // Check if any passenger is marked for ambush tracking
                    // This would integrate with caravan ambush system
                    TheMarkedMenGameComponent component = CrossedUtility.Component;
                    if (component != null)
                    {
                        // Vehicle-based caravan tracking would go here
                    }
                }
            }
            catch
            {
                // Silently fail - don't break vehicle functionality
            }
        }

        private static IEnumerable<Gizmo> FilterPlayerVehicleCommands(IEnumerable<Gizmo> gizmos)
        {
            if (gizmos == null)
            {
                yield break;
            }

            foreach (Gizmo gizmo in gizmos)
            {
                if (gizmo is Command)
                {
                    continue;
                }

                yield return gizmo;
            }
        }

        public static void Patch_VehicleTakeDamage(dynamic __instance, DamageInfo dinfo)
        {
            try
            {
                // When vehicle takes damage, notify any infected passengers
                var passengers = GetVehiclePassengers(__instance);
                if (passengers == null || passengers.Count == 0)
                {
                    return;
                }

                foreach (Pawn passenger in passengers)
                {
                    if (passenger != null && CrossedUtility.IsInfectedPawn(passenger))
                    {
                        // Infected passenger experiences damage notification
                        // Could trigger combat reactions or escape behavior
                    }
                }
            }
            catch
            {
                // Silently fail
            }
        }
    }
}
