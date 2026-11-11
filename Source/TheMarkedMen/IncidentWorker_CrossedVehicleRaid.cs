using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TheMarkedMen
{
    public class IncidentWorker_CrossedVehicleRaid : IncidentWorker_RaidEnemy
    {
        protected virtual bool SpawnAirSupport => false;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Faction faction = CrossedUtility.Component?.EnsureCrossedFaction();
            return !ScenPart_MarkedMenPlayerState.IsActive
                && faction != null
                && base.CanFireNowSub(parms)
                && TheMarkedMenVehicleFrameworkCompatibility.HasEligibleRaidVehicle(
                    faction,
                    parms?.points ?? 0f,
                    parms?.raidArrivalMode,
                    SpawnAirSupport ? RaidVehicleSelectionGroup.CombinedLand : RaidVehicleSelectionGroup.Land)
                && (!SpawnAirSupport
                    || TheMarkedMenVehicleFrameworkCompatibility.HasEligibleAirborneRaidVehicle(
                        faction,
                        parms?.points ?? 0f,
                        0.5f,
                        parms?.raidArrivalMode,
                        RaidVehicleSelectionGroup.CombinedAir));
        }

        protected override string GetLetterLabel(IncidentParms parms)
        {
            return CrossedRaidAlertUtility.BuildRaidLetterLabel(def.letterLabel, null, parms?.points ?? 0f);
        }

        protected override string GetLetterText(IncidentParms parms, List<Pawn> pawns)
        {
            return CrossedRaidAlertUtility.BuildRaidLetterText(def.letterText, pawns, parms, false);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (ScenPart_MarkedMenPlayerState.IsActive)
            {
                return false;
            }

            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            Map map = parms?.target as Map;
            TheMarkedMenGameComponent component = CrossedUtility.Component;
            if (crossed == null || map == null)
            {
                return false;
            }

            HashSet<Pawn> existingPawns = new HashSet<Pawn>(map.mapPawns.AllPawnsSpawned);
            parms.faction = crossed;
            parms.points = component?.CalculateEscalatedRaidPoints(parms.points) ?? parms.points;
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = false;
            TheMarkedMenGameComponent.ApplyMarkedRaidArrivalPattern(parms);

            float raidPoints = parms.points;
            if (!TheMarkedMenVehicleFrameworkCompatibility.HasEligibleRaidVehicle(
                crossed,
                raidPoints,
                parms.raidArrivalMode,
                SpawnAirSupport ? RaidVehicleSelectionGroup.CombinedLand : RaidVehicleSelectionGroup.Land)
                || (SpawnAirSupport
                    && !TheMarkedMenVehicleFrameworkCompatibility.HasEligibleAirborneRaidVehicle(
                        crossed,
                        raidPoints,
                        0.5f,
                        parms.raidArrivalMode,
                        RaidVehicleSelectionGroup.CombinedAir))
                )
            {
                return false;
            }

            int remainingCapacity = CrossedRaidTactics.GetRemainingRaidPawnCapacity(map);
            int minimumVehicleCount = SpawnAirSupport ? 2 : 1;
            int configuredMaximumVehicleCount = TheMarkedMenVehicleFrameworkCompatibility.GetMaximumRaidVehicleCount();
            int requestedVehicleCount = Mathf.Min(
                configuredMaximumVehicleCount,
                Mathf.Min(
                    Mathf.Max(
                        minimumVehicleCount,
                        TheMarkedMenVehicleFrameworkCompatibility.GetRaidAssaultVehicleCount(raidPoints)),
                    Mathf.Max(0, remainingCapacity - 3)));
            int infantryLimit = Mathf.Clamp(TheMarkedMenMod.Settings?.maxRaidInfantry ?? 60, 12, 90);
            int totalVehicleCount = requestedVehicleCount;
            int maximumInfantryCount = 0;
            while (totalVehicleCount >= minimumVehicleCount)
            {
                int estimatedGroundVehicleCount = SpawnAirSupport
                    ? (totalVehicleCount + 1) / 2
                    : totalVehicleCount;
                int aircraftCount = SpawnAirSupport
                    ? totalVehicleCount - estimatedGroundVehicleCount
                    : 0;
                maximumInfantryCount = Mathf.Min(infantryLimit, remainingCapacity - totalVehicleCount);
                int minimumCrewCount = 3 + estimatedGroundVehicleCount * 2 + aircraftCount * 3;
                if (maximumInfantryCount >= minimumCrewCount)
                {
                    break;
                }

                totalVehicleCount--;
            }

            if (totalVehicleCount < minimumVehicleCount || maximumInfantryCount < 3)
            {
                return false;
            }

            // Allocate points primarily to infantry, independent of vehicle selection.
            // Vehicles are selected based on available raid capacity, not point budget.
            // This ensures vehicle presence doesn't artificially limit infantry strength.
            float infantryPointBudget = Mathf.Max(
                raidPoints * 0.5f,  // At least 50% of raid points for infantry
                maximumInfantryCount * 75f);  // Or enough for scaled infantry force
            parms.points = Mathf.Clamp(infantryPointBudget, 120f, raidPoints * 0.9f);
            bool baseRaidLaunched;
            try
            {
                baseRaidLaunched = base.TryExecuteWorker(parms);
            }
            finally
            {
                parms.points = raidPoints;
            }

            if (!baseRaidLaunched)
            {
                return false;
            }

            List<Pawn> spawnedPawns = new List<Pawn>();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn != null && pawn.Faction == crossed && !existingPawns.Contains(pawn))
                {
                    spawnedPawns.Add(pawn);
                    EnsureRaidPawnState(pawn);
                }
            }

            TheMarkedMenVehicleFrameworkCompatibility.KeepMarkedOneOnFoot(spawnedPawns, map);
            if (!TrimRaidPawnsToLimit(spawnedPawns, maximumInfantryCount))
            {
                Log.Warning("[The Marked Men] Vehicle assault exceeded the remaining active-pawn budget and was cancelled.");
                CleanupRaidPawns(spawnedPawns);
                return false;
            }

            CrossedLordCleanupUtility.DetachFromAnyLord(spawnedPawns);
            int requestedGroundVehicleCount = SpawnAirSupport
                ? (totalVehicleCount + 1) / 2
                : totalVehicleCount;
            int requestedAircraftCount = SpawnAirSupport
                ? totalVehicleCount - requestedGroundVehicleCount
                : 0;
            int reservedAirCrew = requestedAircraftCount * 3;
            bool groundVehicleDeploymentSucceeded = TheMarkedMenVehicleFrameworkCompatibility.TrySpawnGroundRaidVehicles(
                crossed,
                raidPoints,
                spawnedPawns,
                parms,
                out _,
                out string vehicleDeploymentFailure,
                requestedGroundVehicleCount,
                TheMarkedMenVehicleFrameworkCompatibility.MinimumVehicleCrewReserve + reservedAirCrew,
                SpawnAirSupport ? RaidVehicleSelectionGroup.CombinedLand : RaidVehicleSelectionGroup.Land);
            if (!groundVehicleDeploymentSucceeded)
            {
                Log.Warning("[The Marked Men] Land assault could not deploy its ground vehicles ("
                    + vehicleDeploymentFailure + ").");
            }

            int groundVehicleCount = CountVehiclePawns(spawnedPawns);
            const int minimumGroundVehicles = 1;
            if (!groundVehicleDeploymentSucceeded || groundVehicleCount < minimumGroundVehicles)
            {
                Log.Warning("[The Marked Men] Vehicle assault deployed only " + groundVehicleCount
                    + " ground vehicle(s); at least " + minimumGroundVehicles
                    + " are required. Continuing with the infantry assault instead of cancelling the raid.");
            }

            List<Pawn> deployedAircraft = new List<Pawn>();
            if (SpawnAirSupport)
            {
                bool airVehicleDeploymentSucceeded = TheMarkedMenVehicleFrameworkCompatibility.TrySpawnAirborneRaidVehicles(
                    crossed,
                    raidPoints,
                    map,
                    spawnedPawns,
                    0.5f,
                    requestedAircraftCount,
                    1,
                    out deployedAircraft,
                    out string deploymentFailure,
                    parms.raidArrivalMode,
                    RaidVehicleSelectionGroup.CombinedAir);
                if (!airVehicleDeploymentSucceeded)
                {
                    Log.Warning("[The Marked Men] Combined-arms assault could not deploy its aircraft ("
                        + deploymentFailure + "). Continuing with the deployed ground forces and infantry.");
                }

                if (groundVehicleCount + deployedAircraft.Count < 2)
                {
                    Log.Warning("[The Marked Men] Combined-arms assault could deploy only "
                        + (groundVehicleCount + deployedAircraft.Count)
                        + " vehicles instead of one ground vehicle and one aircraft. Continuing with available forces.");
                }
            }

            List<Pawn> lordPawns = CrossedLordCleanupUtility.CollectValidSpawnedLordPawns(spawnedPawns, map, crossed);
            if (lordPawns.Count == 0)
            {
                return true;
            }

            List<Pawn> infantryPawns = new List<Pawn>();
            List<Pawn> vehiclePawns = new List<Pawn>();
            for (int i = 0; i < lordPawns.Count; i++)
            {
                Pawn pawn = lordPawns[i];
                if (TheMarkedMenVehicleFrameworkCompatibility.IsVehiclePawn(pawn))
                {
                    vehiclePawns.Add(pawn);
                }
                else
                {
                    infantryPawns.Add(pawn);
                }
            }

            CrossedLordCleanupUtility.DetachFromAnyLord(lordPawns);
            if (infantryPawns.Count > 0)
            {
                CrossedUtility.ApplyGeneratedRaidKindTuning(infantryPawns);
                LordJob lordJob = CrossedRaidTactics.CreateAssaultLordJob(crossed, parms.points, infantryPawns);
                LordMaker.MakeNewLord(crossed, lordJob, map, infantryPawns);
                CrossedRaidTactics.AssignAssaultDuty(infantryPawns);
            }

            for (int i = 0; i < vehiclePawns.Count; i++)
            {
                Pawn vehicle = vehiclePawns[i];
                if (TheMarkedMenVehicleFrameworkCompatibility.TryAssignMarkedMenVehicleLord(vehicle, crossed))
                {
                    continue;
                }

                if (infantryPawns.Count > 0)
                {
                    Log.Warning("[The Marked Men] Could not assign vehicle " + vehicle.LabelShort
                        + " to the Marked Men vehicle assault AI; keeping it with the infantry assault group.");
                    infantryPawns[0].GetLord()?.AddPawn(vehicle);
                }
                else
                {
                    Log.Warning("[The Marked Men] Could not assign vehicle " + vehicle.LabelShort
                        + " to the Marked Men vehicle assault AI; the vehicle may not move to attack.");
                }
            }

            component?.NotifyRaidLaunched(raidPoints, lordPawns, map);
            return true;
        }

        private static bool TrimRaidPawnsToLimit(List<Pawn> pawns, int maximumCount)
        {
            if (pawns == null)
            {
                return false;
            }

            if (pawns.Count <= maximumCount)
            {
                return true;
            }

            for (int i = pawns.Count - 1; i >= 0 && pawns.Count > maximumCount; i--)
            {
                Pawn pawn = pawns[i];
                if (pawn?.kindDef == CADefOf.MarkedMan
                    || TheMarkedMenVehicleFrameworkCompatibility.IsVehiclePawn(pawn))
                {
                    continue;
                }

                pawns.RemoveAt(i);
                if (pawn != null && !pawn.Destroyed)
                {
                    CrossedLordCleanupUtility.DetachFromAnyLord(new[] { pawn });
                    pawn.Destroy(DestroyMode.Vanish);
                }
            }

            return pawns.Count <= maximumCount;
        }

        private static int CountVehiclePawns(List<Pawn> pawns)
        {
            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (TheMarkedMenVehicleFrameworkCompatibility.IsVehiclePawn(pawns[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static void CleanupRaidPawns(List<Pawn> pawns)
        {
            if (pawns == null)
            {
                return;
            }

            CrossedLordCleanupUtility.DetachFromAnyLord(pawns);
            List<Pawn> spawnedVehicles = new List<Pawn>();
            for (int i = 0; i < pawns.Count; i++)
            {
                if (TheMarkedMenVehicleFrameworkCompatibility.IsVehiclePawn(pawns[i]))
                {
                    spawnedVehicles.Add(pawns[i]);
                }
            }

            for (int i = 0; i < spawnedVehicles.Count; i++)
            {
                TheMarkedMenVehicleFrameworkCompatibility.AbortAirborneRaidVehicle(spawnedVehicles[i]);
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && !pawn.Destroyed)
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }
            }
        }

        internal static void EnsureRaidPawnState(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike)
            {
                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.EnsureStarterLineageResistance(pawn);
                CrossedUtility.EnsureInfectedState(pawn);
                CrossedUtility.RemoveMarkedVirusHediffFromFullyTurnedPawn(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                CureUtility.NormalizeRemorseThought(pawn);
            }

            foreach (Pawn passenger in TheMarkedMenVehicleFrameworkCompatibility.GetVehiclePassengers(pawn))
            {
                EnsureRaidPawnState(passenger);
            }
        }
    }
}
