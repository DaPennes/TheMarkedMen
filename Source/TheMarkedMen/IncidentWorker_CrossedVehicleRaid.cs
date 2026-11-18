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
                && base.CanFireNowSub(parms);
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
                int dynamicInfantryCount = GetDynamicInfantryCount(
                    estimatedGroundVehicleCount,
                    aircraftCount,
                    SpawnAirSupport);
                maximumInfantryCount = Mathf.Min(infantryLimit, dynamicInfantryCount);
                if (maximumInfantryCount == dynamicInfantryCount
                    && remainingCapacity >= totalVehicleCount + maximumInfantryCount)
                {
                    break;
                }

                totalVehicleCount--;
            }

            if (totalVehicleCount < minimumVehicleCount || maximumInfantryCount < 3)
            {
                return false;
            }

            // Vehicle assaults use only the crew and escort force required by their
            // actual vehicle mix. Raid points still determine requested vehicles, but
            // cannot expand the infantry force beyond this dynamic target.
            float infantryPointBudget = Mathf.Min(raidPoints, maximumInfantryCount * 90f);
            parms.points = Mathf.Max(120f, infantryPointBudget);
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
            if (SpawnAirSupport)
            {
                List<Pawn> deployedAircraft;
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
                if (!groundVehicleDeploymentSucceeded
                    || groundVehicleCount < minimumGroundVehicles
                    || !airVehicleDeploymentSucceeded
                    || deployedAircraft.Count == 0)
                {
                    Log.Warning("[The Marked Men] Combined-arms assault was cancelled because it requires at least one "
                        + "ground vehicle and one aircraft (ground vehicles: " + groundVehicleCount
                        + ", aircraft: " + deployedAircraft.Count + ", aircraft failure: " + deploymentFailure + ").");
                    for (int i = 0; i < deployedAircraft.Count; i++)
                    {
                        TheMarkedMenVehicleFrameworkCompatibility.AbortAirborneRaidVehicle(deployedAircraft[i]);
                    }
                    CleanupRaidPawns(spawnedPawns);
                    return false;
                }
            }
            else if (!groundVehicleDeploymentSucceeded || groundVehicleCount < minimumGroundVehicles)
            {
                Log.Warning("[The Marked Men] Land assault was cancelled because it requires at least one ground vehicle "
                    + "(deployment failure: " + vehicleDeploymentFailure + ").");
                CleanupRaidPawns(spawnedPawns);
                return false;
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

        internal static int GetDynamicInfantryCount(
            int groundVehicleCount,
            int aircraftCount,
            bool combinedArms)
        {
            if (combinedArms)
            {
                // A combined assault uses the ground and aircraft crews plus its
                // six-person combined escort: 11 pawns for one vehicle and one
                // aircraft, then two/three more for each additional vehicle/aircraft.
                return groundVehicleCount * 2 + aircraftCount * 3 + 6;
            }

            if (aircraftCount > 0)
            {
                // Six pawns for one aircraft, then its three-person crew for each
                // additional aircraft.
                return aircraftCount * 3 + 3;
            }

            // Eight pawns for one ground vehicle, then two additional crew-capable
            // infected for each additional ground vehicle.
            return groundVehicleCount * 2 + 6;
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
