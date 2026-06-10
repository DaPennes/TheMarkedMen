using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class TheMarkedMenGameComponent : GameComponent
    {
        private const int MaintenanceTickInterval = 2500;
        private const int RaidMonitorIntervalTicks = 250;
        private const int InitialThreatFirstTick = GenDate.TicksPerDay * 45;
        private const int RaidFirstTick = InitialThreatFirstTick;
        private const int RaidIntervalTicks = GenDate.TicksPerDay * 5;
        private const int RaidMinimumIntervalTicks = GenDate.TicksPerDay;
        private const int DebugEarlyRaidDelayTicks = 2500;
        private const int RaidScheduleVersion = 3;
        private const int HordeFirstTick = InitialThreatFirstTick + HordeBaseIntervalTicks;
        private const int HordeRetryTicks = GenDate.TicksPerDay;
        private const int HordeBaseIntervalTicks = GenDate.TicksPerDay * 3;
        private const int HordeMinIntervalTicks = GenDate.TicksPerDay * 2;
        private const int HordeMaxIntervalTicks = HordeBaseIntervalTicks;
        private const int RecentIncidentLimit = 12;
        private const int CorpseLingeringRequiredTicks = 2500;
        private const float RandomRaidIntervalMinFactor = 0.6f;
        private const float RandomRaidIntervalMaxFactor = 1.6f;


        private readonly Game game;
        private int nextMaintenanceTick;
        private int nextRaidMonitorTick;
        private int nextCorpseExposureTick;
        private int nextRaidTick;
        private int nextHordeTick;

        private bool raidScheduleActivated;
        private int raidScheduleVersion;
        private bool starterLineageInitialized;
        private int totalCrossedRaidsStarted;
        private int survivedRaidCount;
        private bool activeRaid;
        private int activeRaidStartedTick;
        private int activeRaidWaveCount;
        private int activeRaidPeakInfected;
        private float activeRaidPoints;
        private Map activeRaidMap;
        private bool crossedWorldSettlementInitialized;
        private bool factionCreationFailed;
        private List<string> recentIncidents = new List<string>();
        private List<Pawn> activeRaidPawns = new List<Pawn>();
        private List<Pawn> activeRaidColonistsAtStart = new List<Pawn>();
        private List<Pawn> corpseLingeringPawns = new List<Pawn>();
        private List<int> corpseLingeringTicks = new List<int>();
        private List<int> corpseLingeringLastSeenTicks = new List<int>();

        public TheMarkedMenGameComponent(Game game)
        {
            this.game = game;
            CrossedReanimationManager.IncidentRecorded += AddIncident;
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            EnsureCrossedFaction(false);
            EnsureCrossedWorldSettlement();
            MarkedIdeologyUtility.NormalizeMarkedOneIdeology();
            raidScheduleActivated = false;
            raidScheduleVersion = RaidScheduleVersion;
            starterLineageInitialized = false;
            ScheduleNextRaid(Find.TickManager?.TicksGame ?? 0);
            ScheduleNextHorde(Find.TickManager?.TicksGame ?? 0);

            InitializeStarterLineageResistance();
            AddIncident("Emergency broadcast: Marked Virus quarantine advisory initialized.");
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            EnsureCrossedFaction(false);
            EnsureCrossedWorldSettlement();
            MarkedIdeologyUtility.NormalizeMarkedOneIdeology();
            InitializeStarterLineageResistance();
            EnsureInfectedStateOnLoadedPawns();
            int ticks = Find.TickManager?.TicksGame ?? 0;
            int raidFirstTick = TheMarkedMenSettings.FirstMarkedRaidTick;
            int hordeFirstTick = CurrentHordeFirstTick;
            raidScheduleActivated = ticks >= raidFirstTick;
            if (TheMarkedMenSettings.WarbandsEnabled)
            {
                bool raidTimerInvalid = nextRaidTick <= 0
                    || !raidScheduleActivated && ticks < raidFirstTick && nextRaidTick != raidFirstTick
                    || raidScheduleActivated && nextRaidTick - ticks > CalculateMaxAdjustedRaidIntervalTicks();
                if (raidTimerInvalid)
                    ScheduleNextRaid(ticks);
                else if (raidScheduleVersion < RaidScheduleVersion)
                    MigrateRaidSchedule(ticks);
            }
            else
                nextRaidTick = 0;
            raidScheduleVersion = RaidScheduleVersion;
            if (TheMarkedMenSettings.HordesEnabled)
            {
                bool hordeTimerInvalid = nextHordeTick <= 0
                    || ticks < hordeFirstTick && nextHordeTick != hordeFirstTick
                    || ticks >= hordeFirstTick && nextHordeTick - ticks > CalculateMaxAdjustedHordeIntervalTicks();
                if (hordeTimerInvalid)
                    ScheduleNextHorde(ticks);
            }
            else
                nextHordeTick = 0;

        }

        public override void ExposeData()
        {
            CrossedReanimationManager.ExposeData();
            Scribe_Values.Look(ref nextMaintenanceTick, "nextMaintenanceTick", 0);
            Scribe_Values.Look(ref nextRaidMonitorTick, "nextRaidMonitorTick", 0);
            Scribe_Values.Look(ref nextCorpseExposureTick, "nextCorpseExposureTick", 0);
            Scribe_Values.Look(ref nextRaidTick, "nextRaidTick", 0);
            Scribe_Values.Look(ref nextHordeTick, "nextHordeTick", 0);

            Scribe_Values.Look(ref raidScheduleActivated, "raidScheduleActivated", false);
            Scribe_Values.Look(ref raidScheduleVersion, "raidScheduleVersion", 0);
            Scribe_Values.Look(ref starterLineageInitialized, "starterLineageInitialized", false);
            Scribe_Values.Look(ref totalCrossedRaidsStarted, "totalCrossedRaidsStarted", 0);
            Scribe_Values.Look(ref survivedRaidCount, "survivedRaidCount", 0);
            Scribe_Values.Look(ref activeRaid, "activeRaid", false);
            Scribe_Values.Look(ref activeRaidStartedTick, "activeRaidStartedTick", 0);
            Scribe_Values.Look(ref activeRaidWaveCount, "activeRaidWaveCount", 0);
            Scribe_Values.Look(ref activeRaidPeakInfected, "activeRaidPeakInfected", 0);
            Scribe_Values.Look(ref activeRaidPoints, "activeRaidPoints", 0f);
            Scribe_Values.Look(ref crossedWorldSettlementInitialized, "crossedWorldSettlementInitialized", false);
            Scribe_References.Look(ref activeRaidMap, "activeRaidMap");
            Scribe_Collections.Look(ref recentIncidents, "recentIncidents", LookMode.Value);
            Scribe_Collections.Look(ref activeRaidPawns, "activeRaidPawns", LookMode.Reference);
            Scribe_Collections.Look(ref activeRaidColonistsAtStart, "activeRaidColonistsAtStart", LookMode.Reference);
            Scribe_Collections.Look(ref corpseLingeringPawns, "corpseLingeringPawns", LookMode.Reference);
            Scribe_Collections.Look(ref corpseLingeringTicks, "corpseLingeringTicks", LookMode.Value);
            Scribe_Collections.Look(ref corpseLingeringLastSeenTicks, "corpseLingeringLastSeenTicks", LookMode.Value);
            if (recentIncidents == null) recentIncidents = new List<string>();
            if (activeRaidPawns == null) activeRaidPawns = new List<Pawn>();
            if (activeRaidColonistsAtStart == null) activeRaidColonistsAtStart = new List<Pawn>();
            EnsureCorpseLingeringTrackerLists();
        }

        private void EnsureCorpseLingeringTrackerLists()
        {
            if (corpseLingeringPawns == null) corpseLingeringPawns = new List<Pawn>();
            if (corpseLingeringTicks == null) corpseLingeringTicks = new List<int>();
            if (corpseLingeringLastSeenTicks == null) corpseLingeringLastSeenTicks = new List<int>();
            while (corpseLingeringTicks.Count < corpseLingeringPawns.Count)
                corpseLingeringTicks.Add(0);
            while (corpseLingeringLastSeenTicks.Count < corpseLingeringPawns.Count)
                corpseLingeringLastSeenTicks.Add(0);
            while (corpseLingeringTicks.Count > corpseLingeringPawns.Count)
                corpseLingeringTicks.RemoveAt(corpseLingeringTicks.Count - 1);
            while (corpseLingeringLastSeenTicks.Count > corpseLingeringPawns.Count)
                corpseLingeringLastSeenTicks.RemoveAt(corpseLingeringLastSeenTicks.Count - 1);
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null) return;
            int ticks = Find.TickManager.TicksGame;
            CrossedReanimationManager.Tick(ticks, TheMarkedMenSettings.ReanimationProcessIntervalTicks);
            TryFireScheduledRaid(ticks);

            MonitorActiveRaid(ticks);
            if (ticks >= nextCorpseExposureTick)
            {
                nextCorpseExposureTick = ticks + TheMarkedMenSettings.CorpseContaminationIntervalTicks;
                CrossedCorpseUtility.TryExposeNearbyPawnsToInfectedCorpses();

                PruneCorpseLingeringTrackers(ticks);
            }
            if (ticks < nextMaintenanceTick) return;
            nextMaintenanceTick = ticks + MaintenanceTickInterval;
            TryFireScheduledHorde(ticks);
        }

        public bool NoteCorpseLingering(Pawn pawn, int currentTick, int observedTicks)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || CrossedUtility.IsInfectedPawn(pawn) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(pawn))
                return false;
            EnsureCorpseLingeringTrackerLists();
            int index = corpseLingeringPawns.IndexOf(pawn);
            if (index < 0)
            {
                corpseLingeringPawns.Add(pawn);
                corpseLingeringTicks.Add(Mathf.Max(0, observedTicks));
                corpseLingeringLastSeenTicks.Add(currentTick);
                return corpseLingeringTicks[corpseLingeringTicks.Count - 1] >= CorpseLingeringRequiredTicks;
            }
            int gapTicks = currentTick - corpseLingeringLastSeenTicks[index];
            if (gapTicks <= Mathf.Max(observedTicks + 5, TheMarkedMenSettings.CorpseContaminationIntervalTicks + 5))
                corpseLingeringTicks[index] = Mathf.Min(CorpseLingeringRequiredTicks, corpseLingeringTicks[index] + Mathf.Max(0, observedTicks));
            else
                corpseLingeringTicks[index] = Mathf.Max(0, observedTicks);
            corpseLingeringLastSeenTicks[index] = currentTick;
            return corpseLingeringTicks[index] >= CorpseLingeringRequiredTicks;
        }

        public void ResetCorpseLingering(Pawn pawn)
        {
            if (pawn == null) return;
            EnsureCorpseLingeringTrackerLists();
            int index = corpseLingeringPawns.IndexOf(pawn);
            if (index < 0) return;
            corpseLingeringTicks[index] = 0;
            corpseLingeringLastSeenTicks[index] = Find.TickManager?.TicksGame ?? corpseLingeringLastSeenTicks[index];
        }

        private void PruneCorpseLingeringTrackers(int currentTick)
        {
            EnsureCorpseLingeringTrackerLists();
            int staleAfterTicks = Mathf.Max(CorpseLingeringRequiredTicks * 2, TheMarkedMenSettings.CorpseContaminationIntervalTicks * 4);
            for (int i = corpseLingeringPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = corpseLingeringPawns[i];
                bool invalid = pawn == null || pawn.Destroyed || pawn.Dead || CrossedUtility.IsInfectedPawn(pawn) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(pawn);
                bool stale = i >= corpseLingeringLastSeenTicks.Count || currentTick - corpseLingeringLastSeenTicks[i] > staleAfterTicks;
                if (invalid || stale || i >= corpseLingeringTicks.Count)
                {
                    corpseLingeringPawns.RemoveAt(i);
                    if (i < corpseLingeringTicks.Count) corpseLingeringTicks.RemoveAt(i);
                    if (i < corpseLingeringLastSeenTicks.Count) corpseLingeringLastSeenTicks.RemoveAt(i);
                }
            }
        }

        public bool TryGetRaidCountdownForAlert(out int nextTick, out int ticksUntilRaid, out Map targetMap)
        {
            nextTick = 0; ticksUntilRaid = 0; targetMap = null;
            if (Find.TickManager == null || activeRaid || CADefOf.CrossedRaid == null || !TheMarkedMenSettings.WarbandsEnabled)
                return false;
            targetMap = FindRaidTargetMap();
            if (targetMap == null) return false;
            int ticks = Find.TickManager.TicksGame;
            int raidFirstTick = TheMarkedMenSettings.FirstMarkedRaidTick;
            nextTick = !raidScheduleActivated && ticks < raidFirstTick ? raidFirstTick : nextRaidTick;
            if (nextTick <= 0)
                nextTick = ticks + CalculateAdjustedRaidIntervalTicks(false);
            if (nextTick < ticks) nextTick = ticks;
            ticksUntilRaid = Mathf.Max(0, nextTick - ticks);
            return true;
        }

        public float EstimateUpcomingRaidPoints(Map map)
        {
            IncidentDef raidDef = CADefOf.CrossedRaid;
            float scheduledPoints = CalculateStorytellerRaidPoints(map, raidDef, 0f);
            return CalculateEscalatedRaidPoints(scheduledPoints);
        }

        public bool DebugScheduleRaidSoon()
        {
            if (Find.TickManager == null || CADefOf.CrossedRaid == null || FindRaidTargetMap() == null || !TheMarkedMenSettings.WarbandsEnabled)
                return false;
            int ticks = Find.TickManager.TicksGame;
            raidScheduleActivated = true;
            nextRaidTick = ticks + DebugEarlyRaidDelayTicks;
            AddIncident("DevMode moved the next Marked Men raid to one in-game hour from now.");
            return true;
        }

        public bool DebugFireRaidNow()
        {
            if (Find.TickManager == null) return false;
            int ticks = Find.TickManager.TicksGame;
            raidScheduleActivated = true;
            nextRaidTick = ticks;
            bool fired = TryFireRaidIncident(true);
            if (fired) ScheduleNextRaid(ticks);
            return fired;
        }

        public bool DebugFireHordeNow()
        {
            if (Find.TickManager == null) return false;
            bool fired = TryFireHordeIncident(true);
            if (fired) ScheduleNextHorde(Find.TickManager.TicksGame);
            return fired;
        }

        public bool DebugFireProbeNow()
        {
            return TryFireProbeIncident(true);
        }

        public void AddIncident(string text)
        {
            if (!TheMarkedMenSettings.IncidentLogEnabled || text.NullOrEmpty()) return;
            string day = GenDate.DaysPassed.ToString();
            recentIncidents.Insert(0, "Day " + day + ": " + text);
            while (recentIncidents.Count > RecentIncidentLimit)
                recentIncidents.RemoveAt(recentIncidents.Count - 1);
        }

        public void NotifyExposure(Pawn pawn, string source) { }

        public void NotifyDiseaseActivated(Pawn pawn)
        {
            if (pawn == null || pawn.Faction != Faction.OfPlayer) return;
            AddIncident(pawn.LabelShortCap + "'s Marked Virus incubation ended with active symptoms.");
            if (pawn.Spawned)
                Messages.Message(pawn.LabelShortCap + " is showing active Marked Virus symptoms.", pawn, MessageTypeDefOf.ThreatSmall, false);
        }

        public void NotifyIncubationSurvived(Pawn pawn)
        {
            if (pawn == null || pawn.Faction != Faction.OfPlayer) return;
            AddIncident(pawn.LabelShortCap + " survived Marked Virus incubation and developed immunity.");
            if (pawn.Spawned)
                Messages.Message(pawn.LabelShortCap + " resisted the Marked Virus and developed immunity.", pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        public void NotifyTransformation(Pawn pawn)
        {
            AddIncident(pawn.LabelShortCap + " transformed into one of the Marked Men.");
        }

        public void NotifyVirusDeath(Pawn pawn)
        {
            if (pawn == null) return;
            AddIncident(pawn.LabelShortCap + " died from terminal Marked Virus collapse.");
            if (pawn.Spawned && pawn.Faction == Faction.OfPlayer)
                Messages.Message(pawn.LabelShortCap + " died from the Marked Virus.", pawn, MessageTypeDefOf.ThreatSmall, false);
        }

        public void NotifyRaidLaunched(float points, List<Pawn> spawnedPawns, Map map)
        {
            totalCrossedRaidsStarted++;
            int spawnedCount = spawnedPawns == null ? 0 : spawnedPawns.Count;
            AddIncident("Marked Men warband detected. Wave " + totalCrossedRaidsStarted + ", " + spawnedCount + " infected, combat pressure " + points.ToString("F0") + ".");
            BeginOrExtendActiveRaid(map, spawnedPawns, points);
        }

        public void NotifyHordeLaunched(int count, float points)
        {
            AddIncident("Marked Men horde reached the colony: " + count + " infected, threat pressure " + points.ToString("F0") + ".");
        }

        public void NotifyProbeLaunched(int count, float points)
        {
            AddIncident("Marked Men scouting pack reached the colony: " + count + " infected, threat pressure " + points.ToString("F0") + ".");
        }

        public float CalculateEscalatedRaidPoints(float points)
        {
            float minimum = Mathf.Max(CADefOf.CrossedRaid?.minThreatPoints ?? 120f, TheMarkedMenMod.Settings?.minimumRaidPoints ?? 120f);
            float basePoints = Mathf.Max(points, minimum);
            return TheMarkedMenSettings.ApplyRaidPointSettings(Mathf.Max(basePoints, basePoints * CurrentRaidEscalationMultiplier()));
        }

        private float CurrentRaidEscalationMultiplier()
        {
            return 1f + Mathf.Min(totalCrossedRaidsStarted * TheMarkedMenSettings.RaidEscalationPerRaid, TheMarkedMenSettings.RaidEscalationMaxBonus);
        }

        private void BeginOrExtendActiveRaid(Map map, List<Pawn> spawnedPawns, float points)
        {
            if (map == null || spawnedPawns == null || spawnedPawns.Count == 0) return;
            if (!activeRaid || activeRaidMap != map)
            {
                activeRaid = true;
                activeRaidMap = map;
                activeRaidStartedTick = Find.TickManager?.TicksGame ?? 0;
                activeRaidWaveCount = 0;
                activeRaidPeakInfected = 0;
                activeRaidPoints = 0f;
                activeRaidPawns.Clear();
                activeRaidColonistsAtStart.Clear();
                IReadOnlyList<Pawn> colonists = map.mapPawns?.FreeColonistsSpawned;
                if (colonists != null)
                {
                    for (int i = 0; i < colonists.Count; i++)
                        activeRaidColonistsAtStart.Add(colonists[i]);
                }
            }
            activeRaidWaveCount++;
            activeRaidPoints += points;
            activeRaidPeakInfected += spawnedPawns.Count;
            for (int i = 0; i < spawnedPawns.Count; i++)
            {
                Pawn pawn = spawnedPawns[i];
                if (pawn != null && !activeRaidPawns.Contains(pawn))
                    activeRaidPawns.Add(pawn);
            }
        }

        private void MonitorActiveRaid(int ticks)
        {
            if (!activeRaid || ticks < nextRaidMonitorTick) return;
            nextRaidMonitorTick = ticks + RaidMonitorIntervalTicks;
            if (activeRaidPawns == null || activeRaidPawns.Count == 0)
            {
                ClearActiveRaid();
                return;
            }
            bool anyThreatRemaining = false;
            for (int i = 0; i < activeRaidPawns.Count; i++)
            {
                Pawn pawn = activeRaidPawns[i];
                if (pawn != null && !pawn.Destroyed && pawn.Spawned && !pawn.Dead && !pawn.Downed)
                {
                    anyThreatRemaining = true;
                    break;
                }
            }
            if (anyThreatRemaining) return;
            CrossedRaidReport report = BuildActiveRaidReport();
            if (report.SurvivingColonists > 0)
            {
                survivedRaidCount++;
                report.RaidsSurvived = survivedRaidCount;
                AddIncident("Colony survived Marked Men raid wave " + totalCrossedRaidsStarted + ": " + report.InfectedKilled + " infected killed, " + report.ColonistCasualties + " colony casualties.");
            }
            else
                AddIncident("Marked Men raid wave " + totalCrossedRaidsStarted + " ended with no standing colony survivors.");
            ClearActiveRaid();
        }

        private CrossedRaidReport BuildActiveRaidReport()
        {
            int infectedKilled = 0;
            int infectedNeutralized = 0;
            for (int i = 0; i < activeRaidPawns.Count; i++)
            {
                Pawn pawn = activeRaidPawns[i];
                if (pawn == null || pawn.Destroyed || pawn.Dead)
                {
                    infectedKilled++;
                    infectedNeutralized++;
                    continue;
                }
                if (!pawn.Spawned || pawn.Downed)
                    infectedNeutralized++;
            }
            int colonistDeaths = 0;
            int colonistsDowned = 0;
            for (int i = 0; i < activeRaidColonistsAtStart.Count; i++)
            {
                Pawn pawn = activeRaidColonistsAtStart[i];
                if (pawn == null || pawn.Destroyed || pawn.Dead)
                    colonistDeaths++;
                else if (pawn.Downed)
                    colonistsDowned++;
            }
            int survivingColonists = 0;
            IReadOnlyList<Pawn> colonists = activeRaidMap?.mapPawns?.FreeColonistsSpawned;
            if (colonists != null)
            {
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn pawn = colonists[i];
                    if (pawn != null && !pawn.Dead)
                        survivingColonists++;
                }
            }
            return new CrossedRaidReport
            {
                WaveCount = activeRaidWaveCount,
                InfectedSpawned = activeRaidPeakInfected,
                InfectedKilled = infectedKilled,
                InfectedNeutralized = infectedNeutralized,
                ColonistDeaths = colonistDeaths,
                ColonistsDowned = colonistsDowned,
                ColonistCasualties = colonistDeaths + colonistsDowned,
                SurvivingColonists = survivingColonists,
                DurationTicks = Mathf.Max(0, (Find.TickManager?.TicksGame ?? 0) - activeRaidStartedTick),
                TotalPoints = activeRaidPoints,
                NextEscalationMultiplier = CurrentRaidEscalationMultiplier(),
                TotalRaidsStarted = totalCrossedRaidsStarted
            };
        }

        private void ClearActiveRaid()
        {
            activeRaid = false;
            activeRaidMap = null;
            activeRaidStartedTick = 0;
            activeRaidWaveCount = 0;
            activeRaidPeakInfected = 0;
            activeRaidPoints = 0f;
            activeRaidPawns.Clear();
            activeRaidColonistsAtStart.Clear();
        }

        public Faction EnsureCrossedFaction(bool allowCreate = true)
        {
            FactionDef factionDef = CADefOf.CrossedFaction;
            if (factionDef == null || Find.FactionManager == null) return null;
            if (factionCreationFailed) return null;
            Faction existing = Find.FactionManager.FirstFactionOfDef(factionDef);
            if (existing != null)
            {
                EnsureFactionHostility(existing);
                return existing;
            }
            if (!allowCreate) return null;
            try
            {
                FactionGenerator.CreateFactionAndAddToManager(factionDef);
                Faction generated = Find.FactionManager.FirstFactionOfDef(factionDef);
                if (generated != null) EnsureFactionHostility(generated);
                return generated;
            }
            catch (Exception ex)
            {
                factionCreationFailed = true;
                Log.Error("[The Marked Men] Failed to create Marked Men faction: " + ex);
                return null;
            }
        }

        private void EnsureCrossedWorldSettlement()
        {
            Faction faction = EnsureCrossedFaction(true);
            if (faction == null || Find.World?.worldObjects == null || WorldObjectDefOf.Settlement == null) return;
            if (HasCrossedSettlement(faction))
            {
                crossedWorldSettlementInitialized = true;
                return;
            }
            if (crossedWorldSettlementInitialized) return;
            try
            {
                PlanetTile tile = TileFinder.RandomSettlementTileFor(faction, true, null);
                if (!tile.Valid)
                    tile = TileFinder.RandomSettlementTileFor(faction, false, null);
                if (!tile.Valid)
                {
                    Log.Warning("[The Marked Men] Could not find a valid world tile for a Marked Men settlement.");
                    return;
                }
                Settlement settlement = WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement) as Settlement;
                if (settlement == null)
                {
                    Log.Warning("[The Marked Men] Could not create a Marked Men settlement world object.");
                    return;
                }
                settlement.SetFaction(faction);
                settlement.Tile = tile;
                if (faction.def?.settlementNameMaker != null)
                    settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement, faction.def.settlementNameMaker);
                else
                    settlement.Name = "Marked Village";
                Find.World.worldObjects.Add(settlement);
                EnsureFactionHostility(faction);
                crossedWorldSettlementInitialized = true;
                Log.Message("[The Marked Men] Added missing Marked Men settlement to the world map.");
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Failed to add missing Marked Men settlement: " + ex.Message);
            }
        }

        private static bool HasCrossedSettlement(Faction faction)
        {
            List<Settlement> settlements = Find.World?.worldObjects?.Settlements;
            if (settlements == null) return false;
            FactionDef factionDef = faction?.def ?? CADefOf.CrossedFaction;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement settlement = settlements[i];
                if (settlement != null && !settlement.Destroyed && settlement.Faction?.def == factionDef)
                    return true;
            }
            return false;
        }

        private static void EnsureFactionHostility(Faction faction)
        {
            if (faction == null || Faction.OfPlayer == null || faction == Faction.OfPlayer) return;
            try
            {
                if (faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Hostile)
                    faction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false, null, default);
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Failed to enforce hostile faction relation: " + ex.Message);
            }
        }

        private void TryFireScheduledRaid(int ticks)
        {
            if (!TheMarkedMenSettings.WarbandsEnabled)
            {
                nextRaidTick = 0;
                return;
            }
            if (!raidScheduleActivated)
            {
                int raidFirstTick = TheMarkedMenSettings.FirstMarkedRaidTick;
                if (ticks < raidFirstTick)
                {
                    nextRaidTick = raidFirstTick;
                    return;
                }
                raidScheduleActivated = true;
                if (nextRaidTick <= 0 || nextRaidTick < ticks)
                    nextRaidTick = ticks;
            }
            if (nextRaidTick <= 0)
            {
                ScheduleNextRaid(ticks);
                return;
            }
            if (ticks < nextRaidTick) return;
            TryFireRaidIncident();
            ScheduleNextRaid(ticks);
        }

        private bool TryFireRaidIncident(bool force = false)
        {
            IncidentDef raidDef = CADefOf.CrossedRaid;
            Map map = FindRaidTargetMap();
            Faction crossed = EnsureCrossedFaction();
            if (raidDef == null || map == null || crossed == null) return false;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(raidDef.category, map);
            parms.target = map;
            parms.faction = crossed;
            parms.points = CalculateStorytellerRaidPoints(map, raidDef, parms.points);
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = TheMarkedMenSettings.MarkedCanTimeoutOrFlee;
            parms.forced = true;
            ApplyMarkedRaidArrivalPattern(parms);
            return (force || raidDef.Worker.CanFireNow(parms)) && raidDef.Worker.TryExecute(parms);
        }

        private void ScheduleNextRaid(int fromTick)
        {
            if (!TheMarkedMenSettings.WarbandsEnabled)
            {
                nextRaidTick = 0;
                return;
            }
            int raidFirstTick = TheMarkedMenSettings.FirstMarkedRaidTick;
            nextRaidTick = !raidScheduleActivated && fromTick < raidFirstTick ? raidFirstTick : fromTick + CalculateAdjustedRaidIntervalTicks(true);
        }

        private void MigrateRaidSchedule(int ticks)
        {
            if (!raidScheduleActivated || ticks < TheMarkedMenSettings.FirstMarkedRaidTick || nextRaidTick <= ticks) return;
            int ticksUntilRaid = nextRaidTick - ticks;
            int adjustedInterval = CalculateAdjustedRaidIntervalTicks(false);
            if (ticksUntilRaid < adjustedInterval)
                nextRaidTick = ticks + adjustedInterval;
        }

        private static Map FindRaidTargetMap()
        {
            return FindHordeTargetMap();
        }

        private static int CurrentHordeFirstTick => TheMarkedMenSettings.FirstMarkedRaidTick + HordeBaseIntervalTicks;

        private static float CalculateStorytellerRaidPoints(Map map, IncidentDef raidDef, float existingPoints)
        {
            float minimum = Mathf.Max(raidDef == null ? 120f : raidDef.minThreatPoints, TheMarkedMenMod.Settings?.minimumRaidPoints ?? 120f);
            float storytellerPoints = map == null ? minimum : StorytellerUtility.DefaultThreatPointsNow(map);
            float points = Mathf.Max(existingPoints, storytellerPoints, minimum);
            float pressure = Mathf.InverseLerp(120f, 3600f, points);
            return Mathf.Max(minimum, points * Mathf.Lerp(0.9f, 1.12f, pressure));
        }

        private void TryFireScheduledHorde(int ticks)
        {
            if (!TheMarkedMenSettings.HordesEnabled)
            {
                nextHordeTick = 0;
                return;
            }
            int hordeFirstTick = CurrentHordeFirstTick;
            if (ticks < hordeFirstTick)
            {
                nextHordeTick = hordeFirstTick;
                return;
            }
            if (nextHordeTick <= 0)
            {
                ScheduleNextHorde(ticks);
                return;
            }
            if (ticks < nextHordeTick) return;
            if (TryFireHordeIncident())
                ScheduleNextHorde(ticks);
            else
                nextHordeTick = ticks + HordeRetryTicks;
        }

        private bool TryFireHordeIncident(bool force = false)
        {
            IncidentDef hordeDef = CADefOf.CrossedHorde;
            Map map = FindHordeTargetMap();
            Faction crossed = EnsureCrossedFaction();
            if (hordeDef == null || map == null || crossed == null) return false;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(hordeDef.category, map);
            parms.target = map;
            parms.faction = crossed;
            parms.points = CalculateStorytellerHordePoints(map, hordeDef, parms.points);
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = TheMarkedMenSettings.MarkedCanTimeoutOrFlee;
            parms.forced = false;
            ApplyMarkedRaidArrivalPattern(parms);
            return (force || hordeDef.Worker.CanFireNow(parms)) && hordeDef.Worker.TryExecute(parms);
        }

        private bool TryFireProbeIncident(bool force = false)
        {
            IncidentDef probeDef = CADefOf.CrossedProbe;
            Map map = FindHordeTargetMap();
            Faction crossed = EnsureCrossedFaction();
            if (probeDef == null || map == null || crossed == null) return false;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(probeDef.category, map);
            parms.target = map;
            parms.faction = crossed;
            parms.points = Mathf.Max(probeDef.minThreatPoints, StorytellerUtility.DefaultThreatPointsNow(map) * 0.45f);
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = TheMarkedMenSettings.MarkedCanTimeoutOrFlee;
            parms.forced = force;
            ApplyMarkedRaidArrivalPattern(parms);
            return (force || probeDef.Worker.CanFireNow(parms)) && probeDef.Worker.TryExecute(parms);
        }

        private static Map FindHordeTargetMap()
        {
            if (Find.Maps == null) return null;
            Map best = null;
            float bestScore = -1f;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map == null || !map.IsPlayerHome || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
                    continue;
                float score = StorytellerUtility.DefaultThreatPointsNow(map);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = map;
                }
            }
            return best;
        }

        public static void ApplyMarkedRaidArrivalPattern(IncidentParms parms)
        {
            if (parms == null) return;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = ChooseMarkedRaidArrivalMode(parms);
        }

        private static readonly List<PawnsArrivalModeDef> arrivalModeCandidates = new List<PawnsArrivalModeDef>(4);

        private static PawnsArrivalModeDef ChooseMarkedRaidArrivalMode(IncidentParms parms)
        {
            PawnsArrivalModeDef fallback = PawnsArrivalModeDefOf.EdgeWalkInGroups;
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (!TheMarkedMenSettings.RandomizeMarkedRaids && (settings == null || settings.allowGroupedEdgeArrival))
                return fallback;
            arrivalModeCandidates.Clear();
            if (settings == null || settings.allowGroupedEdgeArrival)
                AddArrivalCandidate(arrivalModeCandidates, PawnsArrivalModeDefOf.EdgeWalkInGroups, parms);
            if (settings == null || settings.allowDistributedGroupArrival)
                AddArrivalCandidate(arrivalModeCandidates, PawnsArrivalModeDefOf.EdgeWalkInDistributedGroups, parms);
            if (settings == null || settings.allowDistributedArrival)
                AddArrivalCandidate(arrivalModeCandidates, PawnsArrivalModeDefOf.EdgeWalkInDistributed, parms);
            if (settings == null || settings.allowSingleEdgeArrival)
                AddArrivalCandidate(arrivalModeCandidates, PawnsArrivalModeDefOf.EdgeWalkIn, parms);
            if (arrivalModeCandidates.Count == 0) return fallback;
            return arrivalModeCandidates[Rand.RangeInclusive(0, arrivalModeCandidates.Count - 1)];
        }

        private static void AddArrivalCandidate(List<PawnsArrivalModeDef> candidates, PawnsArrivalModeDef mode, IncidentParms parms)
        {
            if (mode == null || candidates.Contains(mode)) return;
            if (mode.Worker == null || mode.Worker.CanUseWith(parms))
                candidates.Add(mode);
        }

        private void ScheduleNextHorde(int fromTick)
        {
            if (!TheMarkedMenSettings.HordesEnabled)
            {
                nextHordeTick = 0;
                return;
            }
            int hordeFirstTick = CurrentHordeFirstTick;
            nextHordeTick = fromTick < hordeFirstTick ? hordeFirstTick : fromTick + CalculateAdjustedHordeIntervalTicks(FindHordeTargetMap(), true);
        }



        private void InitializeStarterLineageResistance()
        {
            if (starterLineageInitialized) return;
            int marked = 0;
            int handledStarterColonists = 0;
            if (Find.Maps != null)
            {
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    Map map = Find.Maps[i];
                    if (map?.mapPawns == null || !map.IsPlayerHome) continue;
                    IReadOnlyList<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
                    for (int j = 0; j < colonists.Count; j++)
                    {
                        if (CrossedUtility.CanSafelyProcessInfectedState(colonists[j]) && !CrossedUtility.IsInfectedPawn(colonists[j]))
                            handledStarterColonists++;
                        if (CrossedUtility.TryMarkStarterLineageResistant(colonists[j]))
                            marked++;
                    }
                }
            }
            if (handledStarterColonists <= 0) return;
            starterLineageInitialized = true;
            if (marked > 0)
                AddIncident("Starter colonists developed marked-virus lineage resistance.");
        }

        private static int CalculateAdjustedRaidIntervalTicks(bool allowRandomize)
        {
            int adjusted = ApplyRaidFrequencyToInterval(RaidIntervalTicks, RaidMinimumIntervalTicks, TheMarkedMenSettings.WarbandFrequencyMultiplier);
            return ApplyRaidRandomization(adjusted, RaidMinimumIntervalTicks, allowRandomize);
        }

        private static int CalculateMaxAdjustedRaidIntervalTicks()
        {
            int adjusted = ApplyRaidFrequencyToInterval(RaidIntervalTicks, RaidMinimumIntervalTicks, TheMarkedMenSettings.WarbandFrequencyMultiplier);
            return ApplyRaidRandomizationMax(adjusted, RaidMinimumIntervalTicks);
        }

        private static int CalculateAdjustedHordeIntervalTicks(Map map, bool allowRandomize)
        {
            float points = map == null ? 120f : StorytellerUtility.DefaultThreatPointsNow(map);
            float pressure = Mathf.InverseLerp(120f, 3600f, points);
            float threatScale = CurrentThreatScale();
            float pressureFactor = Mathf.Lerp(1f, 0.72f, pressure);
            float difficultyFactor = Mathf.Clamp(1f / Mathf.Sqrt(threatScale), 0.75f, 1f);
            int adjusted = Mathf.RoundToInt(HordeBaseIntervalTicks * pressureFactor * difficultyFactor);
            adjusted = Mathf.Clamp(adjusted, HordeMinIntervalTicks, HordeMaxIntervalTicks);
            adjusted = ApplyRaidFrequencyToInterval(adjusted, RaidMinimumIntervalTicks, TheMarkedMenSettings.HordeFrequencyMultiplier);
            return ApplyRaidRandomization(adjusted, RaidMinimumIntervalTicks, allowRandomize);
        }

        private static int CalculateMaxAdjustedHordeIntervalTicks()
        {
            int adjusted = ApplyRaidFrequencyToInterval(HordeMaxIntervalTicks, RaidMinimumIntervalTicks, TheMarkedMenSettings.HordeFrequencyMultiplier);
            return ApplyRaidRandomizationMax(adjusted, RaidMinimumIntervalTicks);
        }

        private static int ApplyRaidFrequencyToInterval(int intervalTicks, int minimumTicks, float multiplier)
        {
            if (multiplier <= 0.001f) return int.MaxValue;
            return Mathf.Max(minimumTicks, Mathf.RoundToInt(intervalTicks / multiplier));
        }

        private static int ApplyRaidRandomization(int intervalTicks, int minimumTicks, bool allowRandomize)
        {
            if (!allowRandomize || !TheMarkedMenSettings.RandomizeMarkedRaids)
                return Mathf.Max(minimumTicks, intervalTicks);
            return Mathf.Max(minimumTicks, Mathf.RoundToInt(intervalTicks * Rand.Range(RandomRaidIntervalMinFactor, RandomRaidIntervalMaxFactor)));
        }

        private static int ApplyRaidRandomizationMax(int intervalTicks, int minimumTicks)
        {
            float factor = TheMarkedMenSettings.RandomizeMarkedRaids ? RandomRaidIntervalMaxFactor : 1f;
            return Mathf.Max(minimumTicks, Mathf.RoundToInt(intervalTicks * factor));
        }

        private static float CalculateStorytellerHordePoints(Map map, IncidentDef hordeDef, float existingPoints)
        {
            float minimum = Mathf.Max(hordeDef == null ? 120f : hordeDef.minThreatPoints, TheMarkedMenMod.Settings?.minimumRaidPoints ?? 120f);
            float storytellerPoints = map == null ? minimum : StorytellerUtility.DefaultThreatPointsNow(map);
            float points = Mathf.Max(existingPoints, storytellerPoints, minimum);
            float pressure = Mathf.InverseLerp(120f, 3600f, points);
            return Mathf.Max(minimum, points * Mathf.Lerp(0.95f, 1.18f, pressure));
        }



        private static float CurrentThreatScale()
        {
            Difficulty difficulty = Find.Storyteller?.difficulty;
            return Mathf.Max(0.1f, difficulty?.threatScale ?? 1f);
        }

        private static void EnsureInfectedStateOnLoadedPawns()
        {
            if (Find.Maps == null) return;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map?.mapPawns == null) continue;
                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int j = 0; j < pawns.Count; j++)
                {
                    CrossedUtility.EnsureStarterLineageResistance(pawns[j]);
                    CrossedUtility.EnsureInfectedState(pawns[j]);
                    CrossedUtility.RemoveMarkedVirusHediffFromFullyTurnedPawn(pawns[j]);
                }
            }
        }
    }
}
