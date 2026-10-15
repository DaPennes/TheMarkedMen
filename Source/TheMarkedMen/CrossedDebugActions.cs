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

        private static void Report(bool success, string successText, string failureText)
        {
            Messages.Message(success ? successText : failureText, success ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput, false);
        }
    }
}
