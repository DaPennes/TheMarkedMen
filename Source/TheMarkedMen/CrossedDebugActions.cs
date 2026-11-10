using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static class CrossedDebugActions
    {
        private const string DebugCategory = "The Marked Men";

        [DebugAction(DebugCategory, "Start scheduled raid now", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 1000)]
        public static void StartScheduledRaidNow()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Report(Component?.DebugFireRaidNow() ?? false, "DevMode: Started a Marked Men raid now.", "DevMode: Could not start Marked Men raid. Load a player home map with free colonists.");
        }

        [DebugAction(DebugCategory, "Move next raid to 1 hour", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 990)]
        public static void MoveNextRaidToOneHour()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Report(Component?.DebugScheduleRaidSoon() ?? false, "DevMode: Next Marked Men raid will start in one in-game hour.", "DevMode: Could not move raid timer. Load a player home map with free colonists.");
        }

        [DebugAction(DebugCategory, "Start scouting pack event now", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 980)]
        public static void StartScoutingPackNow()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Report(Component?.DebugFireProbeNow() ?? false, "DevMode: Started a Marked Men scouting pack event now.", "DevMode: Could not start scouting pack. Load a player home map with free colonists.");
        }

        [DebugAction(DebugCategory, "Start horde event now", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 970)]
        public static void StartHordeNow()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Report(Component?.DebugFireHordeNow() ?? false, "DevMode: Started a Marked Men horde event now.", "DevMode: Could not start horde. Load a player home map with free colonists.");
        }

        [DebugAction(DebugCategory, "Fire Marked Men land assault", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 965)]
        public static void FireLandVehicleAssault()
        {
            FireVehicleAssault("CA_CrossedLandAssault", "land assault", true, false);
        }

        [DebugAction(DebugCategory, "Fire Marked Men airborne assault", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 964)]
        public static void FireAirborneVehicleAssault()
        {
            FireVehicleAssault("CA_CrossedAirborneAssault", "airborne assault", false, true);
        }

        [DebugAction(DebugCategory, "Fire Marked Men combined-arms assault", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 963)]
        public static void FireCombinedArmsAssault()
        {
            FireVehicleAssault("CA_CrossedCombinedArmsAssault", "combined-arms assault", true, true);
        }

        [DebugAction(DebugCategory, "Init urban outbreak on this map", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 960)]
        public static void InitUrbanOutbreak()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Report(false, "DevMode: Ancient Urban Ruins integration not loaded.", "DevMode: This feature requires the Ancient Urban Ruins mod.");
        }

        [DebugAction(DebugCategory, "Fire urban ambush incident", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 950)]
        public static void FireUrbanAmbush()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Report(false, "DevMode: Ancient Urban Ruins integration not loaded.", "DevMode: This feature requires the Ancient Urban Ruins mod.");
        }

        [DebugAction(DebugCategory, "Fire survivor encounter incident", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 940)]
        public static void FireUrbanSurvivor()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Report(false, "DevMode: Ancient Urban Ruins integration not loaded.", "DevMode: This feature requires the Ancient Urban Ruins mod.");
        }

        [DebugAction(DebugCategory, "Spawn lost survivor with dormant mark", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 930)]
        public static void SpawnLostSurvivor()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Map map = Find.CurrentMap;
            if (map == null)
            {
                Report(false, null, "DevMode: No active map.");
                return;
            }

            if (CrossedUtility.Component?.EnsureCrossedFaction() == null)
            {
                Report(false, null, "DevMode: Crossed faction does not exist yet. Start a game first.");
                return;
            }

            IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_LostSurvivor");
            if (incidentDef == null)
            {
                Report(false, null, "DevMode: CA_LostSurvivor incident def not found. Check XML.");
                return;
            }

            IncidentParms parms = new IncidentParms
            {
                target = map,
                faction = CrossedUtility.Component.EnsureCrossedFaction(),
                forced = true
            };

            bool success = incidentDef.Worker.TryExecute(parms);
            Report(success, "DevMode: Lost Survivor incident fired.", "DevMode: Could not fire Lost Survivor incident.");
        }

        [DebugAction(DebugCategory, "Trigger dormant mark on targeted pawn", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.ToolMap, displayPriority = 920)]
        public static void TriggerDormantMark()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Map map = Find.CurrentMap;
            if (map == null)
            {
                Report(false, null, "DevMode: No active map.");
                return;
            }

            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                Hediff dormant = p.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
                if (dormant != null)
                {
                    HediffComp_DormantMark comp = dormant.TryGetComp<HediffComp_DormantMark>();
                    if (comp != null && !comp.IsActivated)
                    {
                        comp.AttemptTransformation("debug force trigger");
                        return;
                    }
                }
            }

            Report(false, null, "DevMode: No dormant mark found on any pawn.");
        }

        [DebugAction(DebugCategory, "List dormant carriers on map", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 910)]
        public static void ListDormantCarriers()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            Map map = Find.CurrentMap;
            if (map == null) return;

            int count = 0;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                Hediff dormant = pawn.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
                if (dormant == null) continue;
                HediffComp_DormantMark comp = dormant.TryGetComp<HediffComp_DormantMark>();
                if (comp == null || comp.IsActivated) continue;

                int ticksLeft = comp.TicksUntilActivation;
                float daysLeft = ticksLeft / (float)GenDate.TicksPerDay;
                count++;
                Log.Message($"[TheMarkedMen] Dormant carrier: {pawn.LabelShort}, days until activation: {daysLeft:F1}, tick: {Find.TickManager.TicksGame}");
            }

            Report(count > 0, $"DevMode: Found {count} dormant carrier(s) on map. Check debug log for details.", "DevMode: No dormant carriers found on this map.");
        }

        private static TheMarkedMenGameComponent Component => Current.Game?.GetComponent<TheMarkedMenGameComponent>();

        private static void FireVehicleAssault(string incidentDefName, string assaultName, bool requireLandVehicle, bool includeAircraft)
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            if (!TheMarkedMenVehicleFrameworkCompatibility.IsVehicleFrameworkLoaded)
            {
                Report(false, null, "DevMode: Vehicle Framework is not loaded; vehicle assaults cannot be tested.");
                return;
            }

            Map map = Find.CurrentMap;
            if (map == null || !map.IsPlayerHome || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
            {
                Report(false, null, "DevMode: Load a player home map with free colonists before testing a vehicle assault.");
                return;
            }

            IncidentDef incident = DefDatabase<IncidentDef>.GetNamedSilentFail(incidentDefName);
            Faction faction = CrossedUtility.Component?.EnsureCrossedFaction();
            if (incident?.Worker == null || faction == null)
            {
                Report(false, null, "DevMode: Vehicle assault incident or Marked Men faction is unavailable. Check the game log and incident XML.");
                return;
            }

            const float DebugRaidPoints = 1000000f;
            IncidentParms parms = new IncidentParms
            {
                target = map,
                faction = faction,
                points = Mathf.Max(incident.minThreatPoints, DebugRaidPoints),
                pawnGroupKind = PawnGroupKindDefOf.Combat,
                forced = true,
                canKidnap = false,
                canSteal = false,
                canTimeoutOrFlee = false
            };
            TheMarkedMenGameComponent.ApplyMarkedRaidArrivalPattern(parms);
            bool combinedArms = incident.defName == "CA_CrossedCombinedArmsAssault";
            RaidVehicleSelectionGroup groundSelection = combinedArms
                ? RaidVehicleSelectionGroup.CombinedLand
                : RaidVehicleSelectionGroup.Land;
            RaidVehicleSelectionGroup airSelection = combinedArms
                ? RaidVehicleSelectionGroup.CombinedAir
                : RaidVehicleSelectionGroup.Air;

            if (requireLandVehicle
                && !TheMarkedMenVehicleFrameworkCompatibility.HasEligibleRaidVehicle(
                    faction,
                    parms.points,
                    parms.raidArrivalMode,
                    groundSelection))
            {
                Report(false, null, "DevMode: No eligible land vehicle for this assault. Check the faction vehicle-raider extension, vehicle settings, tech level, and arrival-mode restrictions.");
                return;
            }

            if (includeAircraft
                && !TheMarkedMenVehicleFrameworkCompatibility.HasEligibleAirborneRaidVehicle(
                    faction,
                    parms.points,
                    incident.defName == "CA_CrossedCombinedArmsAssault" ? 0.5f : 1f,
                    parms.raidArrivalMode,
                    airSelection))
            {
                Report(false, null, "DevMode: No eligible aircraft for this assault. Check aircraft raider settings, aircraft launcher components, tech level, and arrival-mode restrictions.");
                return;
            }

            bool success = incident.Worker.TryExecute(parms);
            Report(
                success,
                "DevMode: Marked Men " + assaultName + " triggered. Check the map and log to confirm vehicle deployment.",
                "DevMode: Marked Men " + assaultName + " failed during execution. Check the game log for the Vehicle Framework or raid-worker error.");
        }

        private static void Report(bool success, string successText, string failureText)
        {
            Messages.Message(success ? successText : failureText, success ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput, false);
        }
    }
}
