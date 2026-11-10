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

            parms.points = Mathf.Max(120f, raidPoints * 0.42f);
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
            CrossedLordCleanupUtility.DetachFromAnyLord(spawnedPawns);
            int totalVehicleCount = TheMarkedMenVehicleFrameworkCompatibility.GetRaidAssaultVehicleCount(raidPoints);
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
            int minimumGroundVehicles = SpawnAirSupport ? 2 : 3;
            if (!groundVehicleDeploymentSucceeded || groundVehicleCount < minimumGroundVehicles)
            {
                Log.Warning("[The Marked Men] Vehicle assault deployed only " + groundVehicleCount
                    + " ground vehicle(s); at least " + minimumGroundVehicles + " are required.");
                CleanupRaidPawns(spawnedPawns);
                return false;
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
                        + deploymentFailure + ").");
                    CleanupRaidPawns(spawnedPawns);
                    return false;
                }

                if (groundVehicleCount + deployedAircraft.Count < 3)
                {
                    Log.Warning("[The Marked Men] Combined-arms assault could deploy only "
                        + (groundVehicleCount + deployedAircraft.Count)
                        + " vehicles; at least three are required.");
                    for (int i = 0; i < deployedAircraft.Count; i++)
                    {
                        TheMarkedMenVehicleFrameworkCompatibility.AbortAirborneRaidVehicle(deployedAircraft[i]);
                    }
                    CleanupRaidPawns(spawnedPawns);
                    return false;
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
