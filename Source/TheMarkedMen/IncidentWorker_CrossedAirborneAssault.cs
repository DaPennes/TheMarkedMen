using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TheMarkedMen
{
    public sealed class IncidentWorker_CrossedAirborneAssault : IncidentWorker_RaidEnemy
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Faction faction = CrossedUtility.Component?.EnsureCrossedFaction();
            return !ScenPart_MarkedMenPlayerState.IsActive
                && faction != null
                && base.CanFireNowSub(parms)
                && TheMarkedMenVehicleFrameworkCompatibility.HasEligibleAirborneRaidVehicle(
                    faction,
                    parms?.points ?? 0f,
                    1f,
                    parms?.raidArrivalMode,
                    RaidVehicleSelectionGroup.Air);
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

            parms.faction = crossed;
            float originalPoints = parms.points;
            parms.points = component?.CalculateEscalatedRaidPoints(parms.points) ?? parms.points;
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = false;
            TheMarkedMenGameComponent.ApplyMarkedRaidArrivalPattern(parms);

            int requestedAircraftCount = Mathf.Min(
                Mathf.Min(
                    TheMarkedMenVehicleFrameworkCompatibility.GetRaidAssaultVehicleCount(parms.points),
                    TheMarkedMenVehicleFrameworkCompatibility.GetMaximumRaidVehicleCount()),
                Mathf.Max(0, CrossedRaidTactics.GetRemainingRaidPawnCapacity(map) - 6));
            int infantryCount = IncidentWorker_CrossedRaid.CalculateRaidCount(
                parms.points * 0.35f,
                map,
                requestedAircraftCount);
            if (requestedAircraftCount <= 0 || infantryCount <= 0)
            {
                return ExecuteFallbackRaid(parms, originalPoints, new List<Pawn>(), "the map's active-raider budget is full");
            }

            List<Pawn> pawns = IncidentWorker_CrossedRaid.GenerateRaidPawns(
                infantryCount,
                parms.points,
                crossed);
            if (pawns.Count == 0)
            {
                return ExecuteFallbackRaid(parms, originalPoints, pawns, "no infantry pawns could be generated");
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                IncidentWorker_CrossedVehicleRaid.EnsureRaidPawnState(pawns[i]);
            }

            if (!TheMarkedMenVehicleFrameworkCompatibility.TrySpawnAirborneRaidVehicles(
                crossed,
                parms.points,
                map,
                pawns,
                1f,
                requestedAircraftCount,
                1,
                out List<Pawn> aircraft,
                out string deploymentFailure,
                parms.raidArrivalMode,
                RaidVehicleSelectionGroup.Air))
            {
                return ExecuteFallbackRaid(parms, originalPoints, pawns, deploymentFailure);
            }

            parms.pawnCount = pawns.Count;
            if (!TheMarkedMenGameComponent.TryArriveRaidPawns(pawns, parms))
            {
                for (int i = 0; i < aircraft.Count; i++)
                {
                    TheMarkedMenVehicleFrameworkCompatibility.AbortAirborneRaidVehicle(aircraft[i]);
                }
                return ExecuteFallbackRaid(parms, originalPoints, pawns, "the infantry arrival failed");
            }

            List<Pawn> lordPawns = CrossedLordCleanupUtility.CollectValidSpawnedLordPawns(pawns, map, crossed);
            if (lordPawns.Count == 0)
            {
                Log.Warning("[The Marked Men] Airborne assault landed but no infantry could join its assault group.");
                for (int i = 0; i < aircraft.Count; i++)
                {
                    TheMarkedMenVehicleFrameworkCompatibility.AbortAirborneRaidVehicle(aircraft[i]);
                }
                return ExecuteFallbackRaid(parms, originalPoints, pawns, "no infantry could join the assault group");
            }

            CrossedUtility.ApplyGeneratedRaidKindTuning(lordPawns);
            CrossedLordCleanupUtility.DetachFromAnyLord(lordPawns);
            LordMaker.MakeNewLord(
                crossed,
                CrossedRaidTactics.CreateAssaultLordJob(crossed, parms.points, lordPawns),
                map,
                lordPawns);
            CrossedRaidTactics.AssignAssaultDuty(lordPawns);
            IncidentWorker_CrossedRaid.SendRaidLetter(def, lordPawns, parms);
            component?.NotifyRaidLaunched(parms.points, lordPawns, map);
            return true;
        }

        private bool ExecuteFallbackRaid(
            IncidentParms parms,
            float originalPoints,
            List<Pawn> generatedPawns,
            string reason)
        {
            DestroyPawns(generatedPawns);
            Log.Warning("[The Marked Men] Airborne assault failed (" + reason + "); falling back to a land assault.");
            IncidentDef fallback = CADefOf.CrossedLandAssault;
            if (fallback?.Worker == null)
            {
                fallback = CADefOf.CrossedRaid;
            }

            if (fallback?.Worker == null)
            {
                Log.Error("[The Marked Men] Could not find a raid worker for airborne assault fallback.");
                return false;
            }

            parms.points = originalPoints;
            return fallback.Worker.TryExecute(parms);
        }

        private static void DestroyPawns(List<Pawn> pawns)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && !pawn.Destroyed)
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
