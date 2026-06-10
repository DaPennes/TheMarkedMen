using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    public sealed class TheMarkedMenMod : Mod
    {
        public static TheMarkedMenSettings Settings;

        public TheMarkedMenMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<TheMarkedMenSettings>();
            Harmony harmony = new Harmony("edria.themarkedmen");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            CrossedOptionalHarmonyPatches.Apply(harmony);
            LongEventHandler.ExecuteWhenFinished(() => Settings?.AutoEnableRjwIntegrationIfInstalled());
            LongEventHandler.ExecuteWhenFinished(CrossedUtility.ApplyMarkedVirusResistanceEquippedStatOffsets);
            LongEventHandler.ExecuteWhenFinished(CrossedCompatibility.LogDetectedMods);
        }

        public override string SettingsCategory()
        {
            return "The Marked Men";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }
    }

    public sealed class TheMarkedMenSettings : ModSettings
    {
        private const int CurrentSettingsVersion = 8;
        public const float InfectionTransmissionChance = 0.45f;
        public const float DefaultMarkedRaidFrequencyMultiplier = 1f;
        public const float MinMarkedRaidFrequencyMultiplier = 0f;
        public const float MaxMarkedRaidFrequencyMultiplier = 5f;
        public const float DefaultRaidEscalationPerRaid = 0.18f;
        public const float DefaultRaidEscalationMaxBonus = 5f;
        public const float DefaultImmunitySurvivalChance = 0.05f;
        public const float DefaultTerminalTransformationWeight = 0.85f;
        public const float DefaultTerminalDeathWeight = 0.15f;
        public const float DefaultTerminalTransformationChance = DefaultTerminalTransformationWeight / (DefaultTerminalTransformationWeight + DefaultTerminalDeathWeight);
        private const float LegacyDefaultImmunitySurvivalChance = 0.02f;
        private const float LegacyDefaultTerminalTransformationWeight = 0.55f;
        private const float LegacyDefaultTerminalDeathWeight = 0.45f;
        private const float SettingsViewHeight = 5600f;
        private const float PresetButtonHeight = 32f;
        private const float PresetButtonGap = 4f;
        private const string CustomPresetName = "Custom";
        private static readonly Color HelpTextColor = new Color(0.72f, 0.72f, 0.72f);

        public bool infectionEnabled = true;
        public bool verboseCompatibilityLogging;
        public bool rjwAutoEnableWhenInstalled = true;
        public bool rjwIntegrationEnabled = true;
        public bool scheduledWarbandsEnabled = true;
        public bool scheduledHordesEnabled = true;
        public bool scoutingProbesEnabled = true;
        public bool randomizeMarkedRaids;
        public float markedRaidFrequencyMultiplier = DefaultMarkedRaidFrequencyMultiplier;
        public float warbandFrequencyMultiplier = 1f;
        public float hordeFrequencyMultiplier = 1f;
        public float probeFrequencyMultiplier = 1f;
        public int firstMarkedRaidDay = 45;
        public float raidPointsMultiplier = 1f;
        public float minimumRaidPoints = 120f;
        public float maximumRaidPoints;
        public float raidEscalationPerRaid = DefaultRaidEscalationPerRaid;
        public float raidEscalationMaxBonus = DefaultRaidEscalationMaxBonus;
        public bool allowGroupedEdgeArrival = true;
        public bool allowDistributedGroupArrival = true;
        public bool allowDistributedArrival = true;
        public bool allowSingleEdgeArrival = true;
        public float berserkerWeightMultiplier = 1f;
        public float hunterWeightMultiplier = 1f;
        public float stalkerWeightMultiplier = 1f;
        public float screamerWeightMultiplier = 1f;
        public float bruteWeightMultiplier = 1f;
        public float alphaWeightMultiplier = 1f;
        public float chargerWeightMultiplier = 1f;
        public float spitterWeightMultiplier = 1f;
        public float bomberWeightMultiplier = 1f;
        public float alphaPsychicWeightMultiplier = 1f;
        public bool allowMarkedChildren;
        public int minimumHordeSize = 3;
        public int maximumHordeSize = 12;
        public int minimumProbeSize = 2;
        public int maximumProbeSize = 4;
        public int maximumAlphasPerRaid = 99;
        public float bloodExposureChance = InfectionTransmissionChance;
        public float foodExposureChance = InfectionTransmissionChance;
        public float rjwExposureChance = InfectionTransmissionChance;
        public float infectedAssaultExposureChance = InfectionTransmissionChance;
        public float closeContactExposureChance = InfectionTransmissionChance;
        public float corpseContaminationChance = 1f;
        public float infectionProgressionSpeedMultiplier = 1f;
        public float incubationDurationMultiplier = 1f;
        public float immunitySurvivalChance = DefaultImmunitySurvivalChance;
        public float terminalTransformationWeight = DefaultTerminalTransformationWeight;
        public float terminalDeathWeight = DefaultTerminalDeathWeight;
        public float reanimationChance = 1f;
        public int reanimationDelayTicks = 900;
        public float starterLineageBreakthroughChance = 0.04f;
        public float severityPerDay = 0.34f;
        public bool markedAlwaysAssault = true;
        public bool markedCanTimeoutOrFlee;
        public bool tacticalRetargetingEnabled = true;
        public bool priorityTargetingEnabled = true;
        public bool doorTargetingEnabled = true;
        public float infightingChance = 0.12f;
        public float socialTerrorStrength = 1f;
        public bool raidCountdownAlertEnabled = true;
        public float raidCountdownVisibleDays = 999f;
        public float raidCountdownHighPriorityDays = 1f;
        public bool detailedRaidLetters = true;
        public bool incidentLogEnabled = true;
        public bool debugActionsEnabled = true;
        public int contagionPulseIntervalTicks = 500;
        public int maxContagionTargetsPerPulse = 3;
        public int corpseContaminationIntervalTicks = 750;
        public int maxCorpsesPerPulse = 2;
        public int tacticalRetargetIntervalTicks = 6;
        public int infightingCheckIntervalTicks = 1000;
        public int lordCleanupIntervalTicks = 250;
        public int infectedStateMaintenanceIntervalTicks = 2500;
        public int reanimationProcessIntervalTicks = 2500;
        public int maxPendingReanimationsPerTick = 24;

        private int settingsVersion = CurrentSettingsVersion;
        private string currentPreset = "Default";
        private Vector2 scrollPosition;
        private readonly Dictionary<string, string> numericBuffers = new Dictionary<string, string>();
        private bool coreRulesExpanded = true;
        private bool raidScheduleExpanded = true;
        private bool enemyMixExpanded = true;
        private bool virusCorpsesExpanded = true;
        private bool infectedAIExpanded = true;
        private bool messagesDevExpanded = true;
        private bool performanceExpanded = true;
        private bool rjwBridgeExpanded = true;

        public float EffectiveMarkedRaidFrequencyMultiplier => Mathf.Clamp(markedRaidFrequencyMultiplier, MinMarkedRaidFrequencyMultiplier, MaxMarkedRaidFrequencyMultiplier);

        public float EffectiveWarbandFrequencyMultiplier => EffectiveEventFrequency(scheduledWarbandsEnabled, warbandFrequencyMultiplier);

        public float EffectiveHordeFrequencyMultiplier => EffectiveEventFrequency(scheduledHordesEnabled, hordeFrequencyMultiplier);

        public float EffectiveProbeFrequencyMultiplier => EffectiveEventFrequency(scoutingProbesEnabled, probeFrequencyMultiplier);

        public static bool WarbandsEnabled => (TheMarkedMenMod.Settings?.EffectiveWarbandFrequencyMultiplier ?? DefaultMarkedRaidFrequencyMultiplier) > 0.001f;

        public static bool HordesEnabled => (TheMarkedMenMod.Settings?.EffectiveHordeFrequencyMultiplier ?? DefaultMarkedRaidFrequencyMultiplier) > 0.001f;

        public static bool ProbesEnabled => (TheMarkedMenMod.Settings?.EffectiveProbeFrequencyMultiplier ?? DefaultMarkedRaidFrequencyMultiplier) > 0.001f;

        public static float WarbandFrequencyMultiplier => TheMarkedMenMod.Settings?.EffectiveWarbandFrequencyMultiplier ?? DefaultMarkedRaidFrequencyMultiplier;

        public static float HordeFrequencyMultiplier => TheMarkedMenMod.Settings?.EffectiveHordeFrequencyMultiplier ?? DefaultMarkedRaidFrequencyMultiplier;

        public static float ProbeFrequencyMultiplier => TheMarkedMenMod.Settings?.EffectiveProbeFrequencyMultiplier ?? DefaultMarkedRaidFrequencyMultiplier;

        public static bool RandomizeMarkedRaids => TheMarkedMenMod.Settings?.randomizeMarkedRaids == true;

        public static bool DetailedRaidLetters => TheMarkedMenMod.Settings?.detailedRaidLetters != false;

        public static bool IncidentLogEnabled => TheMarkedMenMod.Settings?.incidentLogEnabled != false;

        public static bool DebugActionsEnabled => TheMarkedMenMod.Settings?.debugActionsEnabled != false;

        public static int FirstMarkedRaidDay => Mathf.Clamp(TheMarkedMenMod.Settings?.firstMarkedRaidDay ?? 45, 1, 600);

        public static int FirstMarkedRaidTick => FirstMarkedRaidDay * GenDate.TicksPerDay;

        public static float RaidEscalationPerRaid => Mathf.Clamp(TheMarkedMenMod.Settings?.raidEscalationPerRaid ?? DefaultRaidEscalationPerRaid, 0f, 2f);

        public static float RaidEscalationMaxBonus => Mathf.Clamp(TheMarkedMenMod.Settings?.raidEscalationMaxBonus ?? DefaultRaidEscalationMaxBonus, 0f, 20f);

        public static float StarterLineageBreakthroughChance => Mathf.Clamp01(TheMarkedMenMod.Settings?.starterLineageBreakthroughChance ?? 0.04f);

        public static float InfectedAssaultExposureChance => Mathf.Clamp01(TheMarkedMenMod.Settings?.infectedAssaultExposureChance ?? InfectionTransmissionChance);

        public static float CloseContactExposureChance => Mathf.Clamp01(TheMarkedMenMod.Settings?.closeContactExposureChance ?? InfectionTransmissionChance);

        public static float CorpseContaminationChance => Mathf.Clamp01(TheMarkedMenMod.Settings?.corpseContaminationChance ?? 1f);

        public static float ReanimationChance => Mathf.Clamp01(TheMarkedMenMod.Settings?.reanimationChance ?? 1f);

        public static int ReanimationDelayTicks => Mathf.Clamp(TheMarkedMenMod.Settings?.reanimationDelayTicks ?? 900, 60, GenDate.TicksPerDay * 30);

        public static int ReanimationProcessIntervalTicks => Mathf.Clamp(TheMarkedMenMod.Settings?.reanimationProcessIntervalTicks ?? 2500, 60, GenDate.TicksPerDay);

        public static int MaxPendingReanimationsPerTick => Mathf.Clamp(TheMarkedMenMod.Settings?.maxPendingReanimationsPerTick ?? 24, 1, 500);

        public static int ContagionPulseIntervalTicks => Mathf.Clamp(TheMarkedMenMod.Settings?.contagionPulseIntervalTicks ?? 500, 60, GenDate.TicksPerDay);

        public static int MaxContagionTargetsPerPulse => Mathf.Clamp(TheMarkedMenMod.Settings?.maxContagionTargetsPerPulse ?? 3, 0, 50);

        public static int CorpseContaminationIntervalTicks => Mathf.Clamp(TheMarkedMenMod.Settings?.corpseContaminationIntervalTicks ?? 750, 60, GenDate.TicksPerDay);

        public static int MaxCorpsesPerPulse => Mathf.Clamp(TheMarkedMenMod.Settings?.maxCorpsesPerPulse ?? 2, 0, 50);

        public static int TacticalRetargetIntervalTicks => Mathf.Clamp(TheMarkedMenMod.Settings?.tacticalRetargetIntervalTicks ?? 6, 1, 2500);

        public static int InfightingCheckIntervalTicks => Mathf.Clamp(TheMarkedMenMod.Settings?.infightingCheckIntervalTicks ?? 1000, 60, GenDate.TicksPerDay);

        public static int LordCleanupIntervalTicks => Mathf.Clamp(TheMarkedMenMod.Settings?.lordCleanupIntervalTicks ?? 250, 60, GenDate.TicksPerDay);

        public static int InfectedStateMaintenanceIntervalTicks => Mathf.Clamp(TheMarkedMenMod.Settings?.infectedStateMaintenanceIntervalTicks ?? 2500, 60, GenDate.TicksPerDay);

        public static bool MarkedAlwaysAssault => TheMarkedMenMod.Settings?.markedAlwaysAssault != false;

        public static bool MarkedCanTimeoutOrFlee => TheMarkedMenMod.Settings?.markedCanTimeoutOrFlee == true;

        public static bool TacticalRetargetingEnabled => TheMarkedMenMod.Settings?.tacticalRetargetingEnabled != false;

        public static bool PriorityTargetingEnabled => TheMarkedMenMod.Settings?.priorityTargetingEnabled != false;

        public static bool DoorTargetingEnabled => TheMarkedMenMod.Settings?.doorTargetingEnabled != false;

        public static float InfightingChance => Mathf.Clamp01(TheMarkedMenMod.Settings?.infightingChance ?? 0.12f);

        public static float SocialTerrorStrength => Mathf.Clamp(TheMarkedMenMod.Settings?.socialTerrorStrength ?? 1f, 0f, 5f);

        public static bool RaidCountdownAlertEnabled => TheMarkedMenMod.Settings?.raidCountdownAlertEnabled != false;

        public static float RaidCountdownVisibleDays => Mathf.Clamp(TheMarkedMenMod.Settings?.raidCountdownVisibleDays ?? 999f, 0f, 999f);

        public static float RaidCountdownHighPriorityDays => Mathf.Clamp(TheMarkedMenMod.Settings?.raidCountdownHighPriorityDays ?? 1f, 0f, 30f);

        private float EffectiveEventFrequency(bool enabled, float eventMultiplier)
        {
            if (!enabled)
            {
                return 0f;
            }

            return Mathf.Clamp(EffectiveMarkedRaidFrequencyMultiplier * Mathf.Clamp(eventMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier), 0f, 10f);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref settingsVersion, "settingsVersion", 0);
            int loadedSettingsVersion = settingsVersion;
            Scribe_Values.Look(ref infectionEnabled, "infectionEnabled", true);
            Scribe_Values.Look(ref verboseCompatibilityLogging, "verboseCompatibilityLogging", false);
            Scribe_Values.Look(ref rjwAutoEnableWhenInstalled, "rjwAutoEnableWhenInstalled", true);
            Scribe_Values.Look(ref rjwIntegrationEnabled, "rjwIntegrationEnabled", true);
            Scribe_Values.Look(ref scheduledWarbandsEnabled, "scheduledWarbandsEnabled", true);
            Scribe_Values.Look(ref scheduledHordesEnabled, "scheduledHordesEnabled", true);
            Scribe_Values.Look(ref scoutingProbesEnabled, "scoutingProbesEnabled", true);
            Scribe_Values.Look(ref randomizeMarkedRaids, "randomizeMarkedRaids", false);
            Scribe_Values.Look(ref markedRaidFrequencyMultiplier, "markedRaidFrequencyMultiplier", DefaultMarkedRaidFrequencyMultiplier);
            Scribe_Values.Look(ref warbandFrequencyMultiplier, "warbandFrequencyMultiplier", 1f);
            Scribe_Values.Look(ref hordeFrequencyMultiplier, "hordeFrequencyMultiplier", 1f);
            Scribe_Values.Look(ref probeFrequencyMultiplier, "probeFrequencyMultiplier", 1f);
            Scribe_Values.Look(ref firstMarkedRaidDay, "firstMarkedRaidDay", 45);
            Scribe_Values.Look(ref raidPointsMultiplier, "raidPointsMultiplier", 1f);
            Scribe_Values.Look(ref minimumRaidPoints, "minimumRaidPoints", 120f);
            Scribe_Values.Look(ref maximumRaidPoints, "maximumRaidPoints", 0f);
            Scribe_Values.Look(ref raidEscalationPerRaid, "raidEscalationPerRaid", DefaultRaidEscalationPerRaid);
            Scribe_Values.Look(ref raidEscalationMaxBonus, "raidEscalationMaxBonus", DefaultRaidEscalationMaxBonus);
            Scribe_Values.Look(ref allowGroupedEdgeArrival, "allowGroupedEdgeArrival", true);
            Scribe_Values.Look(ref allowDistributedGroupArrival, "allowDistributedGroupArrival", true);
            Scribe_Values.Look(ref allowDistributedArrival, "allowDistributedArrival", true);
            Scribe_Values.Look(ref allowSingleEdgeArrival, "allowSingleEdgeArrival", true);
            Scribe_Values.Look(ref berserkerWeightMultiplier, "berserkerWeightMultiplier", 1f);
            Scribe_Values.Look(ref hunterWeightMultiplier, "hunterWeightMultiplier", 1f);
            Scribe_Values.Look(ref stalkerWeightMultiplier, "stalkerWeightMultiplier", 1f);
            Scribe_Values.Look(ref screamerWeightMultiplier, "screamerWeightMultiplier", 1f);
            Scribe_Values.Look(ref bruteWeightMultiplier, "bruteWeightMultiplier", 1f);
            Scribe_Values.Look(ref alphaWeightMultiplier, "alphaWeightMultiplier", 1f);
            Scribe_Values.Look(ref allowMarkedChildren, "allowMarkedChildren", false);
            Scribe_Values.Look(ref minimumHordeSize, "minimumHordeSize", 3);
            Scribe_Values.Look(ref maximumHordeSize, "maximumHordeSize", 12);
            Scribe_Values.Look(ref minimumProbeSize, "minimumProbeSize", 2);
            Scribe_Values.Look(ref maximumProbeSize, "maximumProbeSize", 4);
            Scribe_Values.Look(ref maximumAlphasPerRaid, "maximumAlphasPerRaid", 99);
            Scribe_Values.Look(ref bloodExposureChance, "bloodExposureChance", InfectionTransmissionChance);
            Scribe_Values.Look(ref foodExposureChance, "foodExposureChance", InfectionTransmissionChance);
            Scribe_Values.Look(ref rjwExposureChance, "rjwExposureChance", InfectionTransmissionChance);
            Scribe_Values.Look(ref infectedAssaultExposureChance, "infectedAssaultExposureChance", InfectionTransmissionChance);
            Scribe_Values.Look(ref closeContactExposureChance, "closeContactExposureChance", InfectionTransmissionChance);
            Scribe_Values.Look(ref corpseContaminationChance, "corpseContaminationChance", 1f);
            Scribe_Values.Look(ref infectionProgressionSpeedMultiplier, "infectionProgressionSpeedMultiplier", 1f);
            Scribe_Values.Look(ref incubationDurationMultiplier, "incubationDurationMultiplier", 1f);
            Scribe_Values.Look(ref immunitySurvivalChance, "immunitySurvivalChance", DefaultImmunitySurvivalChance);
            Scribe_Values.Look(ref terminalTransformationWeight, "terminalTransformationWeight", DefaultTerminalTransformationWeight);
            Scribe_Values.Look(ref terminalDeathWeight, "terminalDeathWeight", DefaultTerminalDeathWeight);
            Scribe_Values.Look(ref reanimationChance, "reanimationChance", 1f);
            Scribe_Values.Look(ref reanimationDelayTicks, "reanimationDelayTicks", 900);
            Scribe_Values.Look(ref starterLineageBreakthroughChance, "starterLineageBreakthroughChance", 0.04f);
            Scribe_Values.Look(ref severityPerDay, "severityPerDay", 0.34f);
            Scribe_Values.Look(ref markedAlwaysAssault, "markedAlwaysAssault", true);
            Scribe_Values.Look(ref markedCanTimeoutOrFlee, "markedCanTimeoutOrFlee", false);
            Scribe_Values.Look(ref tacticalRetargetingEnabled, "tacticalRetargetingEnabled", true);
            Scribe_Values.Look(ref priorityTargetingEnabled, "priorityTargetingEnabled", true);
            Scribe_Values.Look(ref doorTargetingEnabled, "doorTargetingEnabled", true);
            Scribe_Values.Look(ref infightingChance, "infightingChance", 0.12f);
            Scribe_Values.Look(ref socialTerrorStrength, "socialTerrorStrength", 1f);
            Scribe_Values.Look(ref raidCountdownAlertEnabled, "raidCountdownAlertEnabled", true);
            Scribe_Values.Look(ref raidCountdownVisibleDays, "raidCountdownVisibleDays", 999f);
            Scribe_Values.Look(ref raidCountdownHighPriorityDays, "raidCountdownHighPriorityDays", 1f);
            Scribe_Values.Look(ref detailedRaidLetters, "detailedRaidLetters", true);
            Scribe_Values.Look(ref incidentLogEnabled, "incidentLogEnabled", true);
            Scribe_Values.Look(ref debugActionsEnabled, "debugActionsEnabled", true);
            Scribe_Values.Look(ref contagionPulseIntervalTicks, "contagionPulseIntervalTicks", 500);
            Scribe_Values.Look(ref maxContagionTargetsPerPulse, "maxContagionTargetsPerPulse", 3);
            Scribe_Values.Look(ref corpseContaminationIntervalTicks, "corpseContaminationIntervalTicks", 750);
            Scribe_Values.Look(ref maxCorpsesPerPulse, "maxCorpsesPerPulse", 2);
            Scribe_Values.Look(ref tacticalRetargetIntervalTicks, "tacticalRetargetIntervalTicks", 6);
            Scribe_Values.Look(ref infightingCheckIntervalTicks, "infightingCheckIntervalTicks", 1000);
            Scribe_Values.Look(ref lordCleanupIntervalTicks, "lordCleanupIntervalTicks", 250);
            Scribe_Values.Look(ref infectedStateMaintenanceIntervalTicks, "infectedStateMaintenanceIntervalTicks", 2500);
            Scribe_Values.Look(ref reanimationProcessIntervalTicks, "reanimationProcessIntervalTicks", 2500);
            Scribe_Values.Look(ref maxPendingReanimationsPerTick, "maxPendingReanimationsPerTick", 24);
            Scribe_Values.Look(ref currentPreset, "currentPreset", "Default");
            Scribe_Values.Look(ref coreRulesExpanded, "coreRulesExpanded", true);
            Scribe_Values.Look(ref raidScheduleExpanded, "raidScheduleExpanded", true);
            Scribe_Values.Look(ref enemyMixExpanded, "enemyMixExpanded", true);
            Scribe_Values.Look(ref virusCorpsesExpanded, "virusCorpsesExpanded", true);
            Scribe_Values.Look(ref infectedAIExpanded, "infectedAIExpanded", true);
            Scribe_Values.Look(ref messagesDevExpanded, "messagesDevExpanded", true);
            Scribe_Values.Look(ref performanceExpanded, "performanceExpanded", true);
            Scribe_Values.Look(ref rjwBridgeExpanded, "rjwBridgeExpanded", true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && loadedSettingsVersion < CurrentSettingsVersion)
            {
                if (loadedSettingsVersion < 3)
                {
                    bloodExposureChance = InfectionTransmissionChance;
                    foodExposureChance = InfectionTransmissionChance;
                }

                if (loadedSettingsVersion < 4)
                {
                    rjwIntegrationEnabled = true;
                    rjwExposureChance = InfectionTransmissionChance;
                }

                if (loadedSettingsVersion < 5)
                {
                    rjwAutoEnableWhenInstalled = true;
                    rjwIntegrationEnabled = true;
                }

                if (loadedSettingsVersion < 6)
                {
                    randomizeMarkedRaids = false;
                    markedRaidFrequencyMultiplier = DefaultMarkedRaidFrequencyMultiplier;
                }

                if (loadedSettingsVersion < 7)
                {
                    scheduledWarbandsEnabled = true;
                    scheduledHordesEnabled = true;
                    scoutingProbesEnabled = true;
                    warbandFrequencyMultiplier = 1f;
                    hordeFrequencyMultiplier = 1f;
                    probeFrequencyMultiplier = 1f;
                    firstMarkedRaidDay = 45;
                    raidPointsMultiplier = 1f;
                    minimumRaidPoints = 120f;
                    maximumRaidPoints = 0f;
                    raidEscalationPerRaid = DefaultRaidEscalationPerRaid;
                    raidEscalationMaxBonus = DefaultRaidEscalationMaxBonus;
                    ResetArrivalDefaults();
                    ResetCompositionDefaults();
                    infectedAssaultExposureChance = InfectionTransmissionChance;
                    closeContactExposureChance = InfectionTransmissionChance;
                    corpseContaminationChance = 1f;
                    infectionProgressionSpeedMultiplier = 1f;
                    incubationDurationMultiplier = 1f;
                    immunitySurvivalChance = DefaultImmunitySurvivalChance;
                    terminalTransformationWeight = DefaultTerminalTransformationWeight;
                    terminalDeathWeight = DefaultTerminalDeathWeight;
                    reanimationChance = 1f;
                    reanimationDelayTicks = 900;
                    starterLineageBreakthroughChance = 0.04f;
                    markedAlwaysAssault = true;
                    markedCanTimeoutOrFlee = false;
                    tacticalRetargetingEnabled = true;
                    priorityTargetingEnabled = true;
                    doorTargetingEnabled = true;
                    infightingChance = 0.12f;
                    socialTerrorStrength = 1f;
                    ResetStoryDefaults();
                    ResetPerformanceDefaults();
                    currentPreset = "Default";
                }

                if (loadedSettingsVersion < 8 && UsesLegacyDefaultVirusOutcome())
                {
                    immunitySurvivalChance = DefaultImmunitySurvivalChance;
                    terminalTransformationWeight = DefaultTerminalTransformationWeight;
                    terminalDeathWeight = DefaultTerminalDeathWeight;
                }

                settingsVersion = CurrentSettingsVersion;
            }

            ClampSettings();
        }

        private bool UsesLegacyDefaultVirusOutcome()
        {
            return Mathf.Approximately(immunitySurvivalChance, LegacyDefaultImmunitySurvivalChance)
                && Mathf.Approximately(terminalTransformationWeight, LegacyDefaultTerminalTransformationWeight)
                && Mathf.Approximately(terminalDeathWeight, LegacyDefaultTerminalDeathWeight);
        }

        public bool AutoEnableRjwIntegrationIfInstalled()
        {
            if (!rjwAutoEnableWhenInstalled || !TheMarkedMenRjwCompatibility.IsRjwLoaded())
            {
                return false;
            }

            bool changed = !rjwIntegrationEnabled;
            rjwIntegrationEnabled = true;
            return changed;
        }

        public void DoWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, SettingsViewHeight);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            DrawSettingsIntro(listing);
            DrawPresetControls(listing);

            DrawSectionHeader(listing, "Core Rules", "Global switches for infection and compatibility diagnostics. These do not remove existing hediffs from a save.", ref coreRulesExpanded);
            if (coreRulesExpanded)
            {
                DrawCheckbox(listing, "Allow new Marked Virus infections", ref infectionEnabled, "When disabled, this mod stops creating new Marked Virus exposure events. Existing infections continue to run normally.");
                DrawCheckbox(listing, "Log detected compatibility mods on load", ref verboseCompatibilityLogging, "Writes a short compatibility scan to the RimWorld log after loading. Leave this off unless you are troubleshooting.");
            }

            DrawSectionHeader(listing, "Raid Schedule", "Controls when Marked Men incidents appear and how hard scheduled attacks scale.", ref raidScheduleExpanded);
            if (raidScheduleExpanded)
            {
                DrawCheckbox(listing, "Enable scheduled warbands", ref scheduledWarbandsEnabled, "Allows the main timed Marked Men raids that escalate over the colony timeline.");
                DrawCheckbox(listing, "Enable scheduled hordes", ref scheduledHordesEnabled, "Allows larger moving horde events in addition to the main warband schedule.");
                DrawCheckbox(listing, "Enable scouting probes", ref scoutingProbesEnabled, "Allows small scouting packs that test the colony before larger attacks arrive.");
                DrawCheckbox(listing, "Randomize raid timing and arrival patterns", ref randomizeMarkedRaids, "Adds uncertainty to raid intervals and arrival modes. Disable this for predictable testing or calmer pacing.");
                DrawInt(listing, "First scheduled raid day", ref firstMarkedRaidDay, 1, 600, "firstMarkedRaidDay", "Earliest colony day when scheduled Marked Men raids can begin.");
                DrawFloat(listing, "Global event frequency multiplier", ref markedRaidFrequencyMultiplier, MinMarkedRaidFrequencyMultiplier, MaxMarkedRaidFrequencyMultiplier, "markedRaidFrequencyMultiplier", "Master multiplier for warbands, hordes, and probes. Set this to 0 to stop all scheduled Marked Men incidents.");
                DrawFloat(listing, "Warband frequency multiplier", ref warbandFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "warbandFrequencyMultiplier", "Multiplier for main warband raids after the global multiplier is applied.");
                DrawFloat(listing, "Horde frequency multiplier", ref hordeFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "hordeFrequencyMultiplier", "Multiplier for horde events after the global multiplier is applied.");
                DrawFloat(listing, "Scouting probe frequency multiplier", ref probeFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "probeFrequencyMultiplier", "Multiplier for small probe incidents after the global multiplier is applied.");
                DrawHelp(listing, "Effective frequencies: warbands " + MultiplierText(EffectiveWarbandFrequencyMultiplier) + ", hordes " + MultiplierText(EffectiveHordeFrequencyMultiplier) + ", probes " + MultiplierText(EffectiveProbeFrequencyMultiplier) + ".");
                DrawFloat(listing, "Raid strength multiplier", ref raidPointsMultiplier, 0.05f, 10f, "raidPointsMultiplier", "Scales incident points after the minimum point floor is applied.");
                DrawFloat(listing, "Minimum raid points", ref minimumRaidPoints, 0f, 10000f, "minimumRaidPoints", "Point floor for generated Marked Men attacks. Higher values make even early raids larger.");
                DrawFloat(listing, "Maximum raid points", ref maximumRaidPoints, 0f, 50000f, "maximumRaidPoints", "Point cap for Marked Men attacks. Use 0 for no cap.");
                DrawFloat(listing, "Escalation gained per warband", ref raidEscalationPerRaid, 0f, 2f, "raidEscalationPerRaid", "Extra raid strength added after each scheduled warband starts.");
                DrawFloat(listing, "Escalation maximum bonus", ref raidEscalationMaxBonus, 0f, 20f, "raidEscalationMaxBonus", "Maximum accumulated escalation bonus from repeated warbands.");
                DrawCheckbox(listing, "Allow grouped edge arrivals", ref allowGroupedEdgeArrival, "Allows raiders to enter together from one map edge.");
                DrawCheckbox(listing, "Allow split group edge arrivals", ref allowDistributedGroupArrival, "Allows several groups to enter from different edge positions.");
                DrawCheckbox(listing, "Allow scattered edge arrivals", ref allowDistributedArrival, "Allows a wider scattered edge arrival pattern.");
                DrawCheckbox(listing, "Allow single pawn edge arrivals", ref allowSingleEdgeArrival, "Allows single-file edge entry when the incident worker selects it.");
            }

            DrawSectionHeader(listing, "Enemy Mix", "Controls which infected pawn types appear. Weight 0 disables that type; weight 1 is normal; higher values make that type more common.", ref enemyMixExpanded);
            if (enemyMixExpanded)
            {
                DrawFloat(listing, "Berserker weight", ref berserkerWeightMultiplier, 0f, 5f, "berserkerWeightMultiplier", "Basic aggressive infected. This is the safest fallback type when other weights are low.");
                DrawFloat(listing, "Hunter weight", ref hunterWeightMultiplier, 0f, 5f, "hunterWeightMultiplier", "Fast pressure unit used more often as raid points rise.");
                DrawFloat(listing, "Stalker weight", ref stalkerWeightMultiplier, 0f, 5f, "stalkerWeightMultiplier", "Flanking and probe-focused infected.");
                DrawFloat(listing, "Screamer weight", ref screamerWeightMultiplier, 0f, 5f, "screamerWeightMultiplier", "Disruptive infected used in stronger attacks.");
                DrawFloat(listing, "Brute weight", ref bruteWeightMultiplier, 0f, 5f, "bruteWeightMultiplier", "Heavy infected. Usually appears only when raid points are high enough.");
                DrawFloat(listing, "Alpha weight", ref alphaWeightMultiplier, 0f, 5f, "alphaWeightMultiplier", "Command infected that strengthens nearby Marked Men. Also limited by the maximum alpha setting.");
                DrawCheckbox(listing, "Allow child Marked pawns", ref allowMarkedChildren, "Allows child infected pawn kinds in eligible low-point raids. Disable this if you do not want child enemies.");
                DrawInt(listing, "Minimum horde size", ref minimumHordeSize, 1, 50, "minimumHordeSize", "Smallest horde size when a horde incident does not request a specific count.");
                DrawInt(listing, "Maximum horde size", ref maximumHordeSize, 1, 100, "maximumHordeSize", "Largest horde size after threat scaling and variance.");
                DrawInt(listing, "Minimum scouting probe size", ref minimumProbeSize, 1, 20, "minimumProbeSize", "Smallest scouting pack size when the incident does not request a specific count.");
                DrawInt(listing, "Maximum scouting probe size", ref maximumProbeSize, 1, 30, "maximumProbeSize", "Largest scouting pack size after threat scaling and variance.");
                DrawInt(listing, "Maximum alphas per raid", ref maximumAlphasPerRaid, 0, 99, "maximumAlphasPerRaid", "Hard cap for alpha infected in generated raids. Set to 0 to prevent alphas from spawning.");
            }

            DrawSectionHeader(listing, "Virus And Corpses", "Controls exposure chances, infection timing, terminal outcomes, and infected corpse reanimation.", ref virusCorpsesExpanded);
            if (virusCorpsesExpanded)
            {
                DrawFloat(listing, "Blood exposure chance", ref bloodExposureChance, 0f, 1f, "bloodExposureChance", "Chance that infected blood exposure creates a Marked Virus exposure event.");
                DrawFloat(listing, "Contaminated food exposure chance", ref foodExposureChance, 0f, 1f, "foodExposureChance", "Chance that eating contaminated food creates a Marked Virus exposure event.");
                DrawFloat(listing, "Infected melee contact chance", ref infectedAssaultExposureChance, 0f, 1f, "infectedAssaultExposureChance", "Chance that direct infected assault contact creates a Marked Virus exposure event.");
                DrawFloat(listing, "Close-contact contagion chance", ref closeContactExposureChance, 0f, 1f, "closeContactExposureChance", "Chance per valid nearby target during a contagion pulse from an infected pawn.");
                DrawFloat(listing, "Corpse contamination chance", ref corpseContaminationChance, 0f, 1f, "corpseContaminationChance", "Chance that an infected pawn contaminates a nearby corpse during a corpse contamination pulse.");
                DrawFloat(listing, "Infection progression speed", ref infectionProgressionSpeedMultiplier, 0.05f, 10f, "infectionProgressionSpeedMultiplier", "Higher values make the disease advance faster. Lower values give victims more time.");
                DrawFloat(listing, "Incubation duration multiplier", ref incubationDurationMultiplier, 0.05f, 10f, "incubationDurationMultiplier", "Multiplies infection stage durations before progression speed is applied.");
                DrawFloat(listing, "Immune survivor chance", ref immunitySurvivalChance, 0f, 1f, "immunitySurvivalChance", "Chance that a terminal infection ends in lasting immunity instead of transformation or viral death.");
                DrawFloat(listing, "Terminal transformation weight", ref terminalTransformationWeight, 0f, 10f, "terminalTransformationWeight", "Relative weight for becoming one of the Marked Men when immunity does not save the pawn.");
                DrawFloat(listing, "Terminal death weight", ref terminalDeathWeight, 0f, 10f, "terminalDeathWeight", "Relative weight for dying from viral collapse when immunity does not save the pawn.");
                DrawHelp(listing, "Current terminal outcome after the immunity roll: " + PercentText(CurrentTerminalTransformationChance(null)) + " transform, " + PercentText(1f - CurrentTerminalTransformationChance(null)) + " die.");
                DrawFloat(listing, "Corpse reanimation chance", ref reanimationChance, 0f, 1f, "reanimationChance", "Chance that a valid infected corpse queues for reanimation after death.");
                DrawInt(listing, "Corpse reanimation delay ticks", ref reanimationDelayTicks, 60, GenDate.TicksPerDay * 30, "reanimationDelayTicks", "Delay before queued infected corpses can reanimate. 60,000 ticks equals one in-game day.");
                DrawFloat(listing, "Founder-lineage breakthrough chance", ref starterLineageBreakthroughChance, 0f, 1f, "starterLineageBreakthroughChance", "Chance that special starter-lineage resistance fails after direct exposure.");
            }

            DrawSectionHeader(listing, "Infected AI", "Controls how aggressively Marked Men attack, retarget, breach, and terrorize nearby pawns.", ref infectedAIExpanded);
            if (infectedAIExpanded)
            {
                DrawCheckbox(listing, "Force Marked pawns to keep assaulting", ref markedAlwaysAssault, "Keeps generated Marked Men focused on attacking instead of behaving like ordinary raiders.");
                DrawCheckbox(listing, "Allow Marked pawns to time out or flee", ref markedCanTimeoutOrFlee, "Allows Marked Men lords to retreat or time out. Leave disabled for relentless pressure.");
                DrawCheckbox(listing, "Enable tactical retargeting", ref tacticalRetargetingEnabled, "Lets infected pawns periodically switch to better tactical targets.");
                DrawCheckbox(listing, "Enable priority targeting", ref priorityTargetingEnabled, "Lets infected pawns prefer power, food, medical, research, and turret targets when appropriate.");
                DrawCheckbox(listing, "Enable door and wall targeting", ref doorTargetingEnabled, "Allows infected pawns to bash or target barriers when pursuing a colony.");
                DrawFloat(listing, "Marked infighting chance", ref infightingChance, 0f, 1f, "infightingChance", "Chance during each infighting check that infected pawns may turn on each other.");
                DrawFloat(listing, "Panic and social terror strength", ref socialTerrorStrength, 0f, 5f, "socialTerrorStrength", "Scales the radius and strength of Marked Men terror effects. Set to 0 to disable these effects.");
            }

            DrawSectionHeader(listing, "Messages And Dev Tools", "Controls player-facing alerts, incident history, and optional debug actions.", ref messagesDevExpanded);
            if (messagesDevExpanded)
            {
                DrawCheckbox(listing, "Show raid countdown alert", ref raidCountdownAlertEnabled, "Shows a gizmo alert when a scheduled Marked Men raid is approaching.");
                DrawFloat(listing, "Countdown visible days", ref raidCountdownVisibleDays, 0f, 999f, "raidCountdownVisibleDays", "How many in-game days before a scheduled raid the countdown alert becomes visible.");
                DrawFloat(listing, "High-priority countdown days", ref raidCountdownHighPriorityDays, 0f, 30f, "raidCountdownHighPriorityDays", "How many in-game days before a scheduled raid the alert becomes high priority.");
                DrawCheckbox(listing, "Use detailed raid letters", ref detailedRaidLetters, "Adds richer raid letter text with pawn counts, points, arrival mode, and tactical warning details.");
                DrawCheckbox(listing, "Record incident log entries", ref incidentLogEnabled, "Stores Marked Men incident history in the game component for debugging and future review.");
                DrawCheckbox(listing, "Enable Dev Mode debug actions", ref debugActionsEnabled, "Adds Dev Mode actions for starting or rescheduling Marked Men incidents while testing.");
            }

            DrawSectionHeader(listing, "Performance", "Controls how often background systems run. Higher intervals reduce CPU work but make reactions less immediate.", ref performanceExpanded);
            if (performanceExpanded)
            {
                DrawInt(listing, "Ticks between contagion pulses", ref contagionPulseIntervalTicks, 60, GenDate.TicksPerDay, "contagionPulseIntervalTicks", "How often infected pawns try nearby close-contact exposure checks.");
                DrawInt(listing, "Max contagion targets per pulse", ref maxContagionTargetsPerPulse, 0, 50, "maxContagionTargetsPerPulse", "Maximum nearby pawns checked by each contagion pulse. Set to 0 to disable pulse-based close-contact spread.");
                DrawInt(listing, "Ticks between corpse contamination pulses", ref corpseContaminationIntervalTicks, 60, GenDate.TicksPerDay, "corpseContaminationIntervalTicks", "How often infected pawns try to contaminate nearby corpses.");
                DrawInt(listing, "Max corpses checked per pulse", ref maxCorpsesPerPulse, 0, 50, "maxCorpsesPerPulse", "Maximum nearby corpses checked during each corpse contamination pulse. Set to 0 to disable corpse contamination.");
                DrawInt(listing, "Ticks between tactical retarget checks", ref tacticalRetargetIntervalTicks, 1, 2500, "tacticalRetargetIntervalTicks", "How often infected pawns can reconsider tactical targets.");
                DrawInt(listing, "Ticks between infighting checks", ref infightingCheckIntervalTicks, 60, GenDate.TicksPerDay, "infightingCheckIntervalTicks", "How often infected pawns can roll for infighting behavior.");
                DrawInt(listing, "Ticks between lord cleanup checks", ref lordCleanupIntervalTicks, 60, GenDate.TicksPerDay, "lordCleanupIntervalTicks", "How often the mod cleans up invalid raid lord state.");
                DrawInt(listing, "Ticks between infected-state maintenance", ref infectedStateMaintenanceIntervalTicks, 60, GenDate.TicksPerDay, "infectedStateMaintenanceIntervalTicks", "How often infected pawns refresh state such as faction-specific visuals.");
                DrawInt(listing, "Ticks between reanimation processing", ref reanimationProcessIntervalTicks, 60, GenDate.TicksPerDay, "reanimationProcessIntervalTicks", "How often queued corpse reanimations are processed.");
                DrawInt(listing, "Max queued reanimations processed per tick", ref maxPendingReanimationsPerTick, 1, 500, "maxPendingReanimationsPerTick", "Limits burst work when many infected corpses are waiting to reanimate.");
            }

            DrawSectionHeader(listing, "Optional RimJobWorld Bridge", "Only applies when RimJobWorld is installed. The bridge adds no hard dependency.", ref rjwBridgeExpanded);
            if (rjwBridgeExpanded)
            {
                DrawHelp(listing, "RimJobWorld detected right now: " + (TheMarkedMenRjwCompatibility.IsRjwLoaded() ? "yes" : "no") + ".");
                DrawCheckbox(listing, "Auto-enable the RimJobWorld bridge when detected", ref rjwAutoEnableWhenInstalled, "Automatically turns on the bridge after RimJobWorld is found in the active mod list.");
                DrawCheckbox(listing, "Enable RimJobWorld Marked Virus bridge", ref rjwIntegrationEnabled, "Allows adult RJW close-contact events to transmit Marked Virus and lets valid infected adults use RJW enemy assault jobs.");
                DrawFloat(listing, "RimJobWorld exposure chance", ref rjwExposureChance, 0f, 1f, "rjwExposureChance", "Chance that a valid RJW close-contact event involving one infected pawn exposes the other pawn.");
            }
            listing.End();
            Widgets.EndScrollView();
            ClampSettings();
        }

        public static float ApplyRaidPointSettings(float points)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null)
            {
                return Mathf.Max(120f, points);
            }

            float adjusted = Mathf.Max(points, settings.minimumRaidPoints) * settings.raidPointsMultiplier;
            if (settings.maximumRaidPoints > 0f)
            {
                adjusted = Mathf.Min(adjusted, settings.maximumRaidPoints);
            }

            return Mathf.Max(0f, adjusted);
        }

        public static float CurrentTerminalTransformationChance(HediffCompProperties_CrossVirus props)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null)
            {
                return Mathf.Clamp01(props?.terminalTransformationChance ?? DefaultTerminalTransformationChance);
            }

            float total = Mathf.Max(0f, settings.terminalTransformationWeight) + Mathf.Max(0f, settings.terminalDeathWeight);
            if (total <= 0.001f)
            {
                return 1f;
            }

            return Mathf.Clamp01(settings.terminalTransformationWeight / total);
        }

        public static int AdjustInfectionTicks(int ticks)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null)
            {
                return Mathf.Max(1, ticks);
            }

            float duration = Mathf.Max(1f, ticks * settings.incubationDurationMultiplier);
            duration /= Mathf.Max(0.05f, settings.infectionProgressionSpeedMultiplier);
            return Mathf.Clamp(Mathf.RoundToInt(duration), 1, GenDate.TicksPerDay * 120);
        }

        public float KindWeightMultiplier(PawnKindDef kind)
        {
            if (kind == CADefOf.Berserker)
            {
                return berserkerWeightMultiplier;
            }
            if (kind == CADefOf.Hunter)
            {
                return hunterWeightMultiplier;
            }
            if (kind == CADefOf.Stalker)
            {
                return stalkerWeightMultiplier;
            }
            if (kind == CADefOf.Screamer)
            {
                return screamerWeightMultiplier;
            }
            if (kind == CADefOf.Brute)
            {
                return bruteWeightMultiplier;
            }
            if (kind == CADefOf.Alpha)
            {
                return alphaWeightMultiplier;
            }
            if (kind == CADefOf.Child)
            {
                return allowMarkedChildren ? Mathf.Max(0.01f, berserkerWeightMultiplier) : 0f;
            }

            if (kind == CADefOf.Charger)
            {
                return chargerWeightMultiplier;
            }

            if (kind == CADefOf.Spitter)
            {
                return spitterWeightMultiplier;
            }

            if (kind == CADefOf.Bomber)
            {
                return bomberWeightMultiplier;
            }

            if (kind == CADefOf.AlphaPsychic)
            {
                return alphaPsychicWeightMultiplier;
            }

            return 1f;
        }

        public static float AdjustKindWeight(PawnKindDef kind, float baseWeight)
        {
            if (baseWeight <= 0f)
            {
                return 0f;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            return baseWeight * Mathf.Clamp(settings?.KindWeightMultiplier(kind) ?? 1f, 0f, 5f);
        }

        private void DrawSettingsIntro(Listing_Standard listing)
        {
            GameFont oldFont = Text.Font;
            Text.Font = GameFont.Medium;
            listing.Label("The Marked Men Settings");
            Text.Font = oldFont;
            DrawHelp(listing, "Use presets for a quick difficulty pass, or tune each section directly. Editing any field changes the active preset to Custom.");
        }

        private void DrawPresetControls(Listing_Standard listing)
        {
            listing.Gap(6f);
            listing.Label("Active preset: " + PresetLabel());
            DrawHelp(listing, "Presets rewrite the schedule, enemy mix, virus behavior, AI pressure, story UI, and performance settings at once.");

            Rect row = listing.GetRect(PresetButtonHeight, 1f);
            float buttonWidth = (row.width - (PresetButtonGap * 4f)) / 5f;
            DrawPresetButton(new Rect(row.x, row.y, buttonWidth, row.height), "Casual", "Slower raids, lower exposure risk, smaller hordes, and more immune survivors.", ApplyCasualPreset);
            DrawPresetButton(new Rect(row.x + (buttonWidth + PresetButtonGap), row.y, buttonWidth, row.height), "Vanilla-like", "Keeps the faction dangerous while staying closer to ordinary RimWorld pacing.", ApplyVanillaLikePreset);
            DrawPresetButton(new Rect(row.x + ((buttonWidth + PresetButtonGap) * 2f), row.y, buttonWidth, row.height), "Default", "Restores the intended baseline tuning for the mod.", () => ApplyDefaultPreset(true));
            DrawPresetButton(new Rect(row.x + ((buttonWidth + PresetButtonGap) * 3f), row.y, buttonWidth, row.height), "Brutal", "Faster, harder, less forgiving raids with stronger infection pressure.", ApplyBrutalPreset);
            DrawPresetButton(new Rect(row.x + ((buttonWidth + PresetButtonGap) * 4f), row.y, buttonWidth, row.height), "Outbreak", "Large outbreak pressure with faster spread, larger hordes, and faster corpse cycling.", ApplyOutbreakPreset);
            listing.Gap(6f);
        }

        private void DrawPresetButton(Rect rect, string label, string tooltip, Action applyPreset)
        {
            if (Widgets.ButtonText(rect, label, true, true, true, null))
            {
                applyPreset();
                ClearNumericBuffers();
            }

            TooltipHandler.TipRegion(rect, new TipSignal(tooltip));
        }

        private void DrawSectionHeader(Listing_Standard listing, string title, string description, ref bool expanded)
        {
            listing.Gap(10f);
            listing.GapLine(6f);
            Rect headerRect = listing.GetRect(30f);
            string label = expanded ? "[-] " : "[+] ";
            label += title;
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, label);
            Widgets.DrawHighlightIfMouseover(headerRect);
            if (Widgets.ButtonInvisible(headerRect))
            {
                expanded = !expanded;
            }
            Text.Font = GameFont.Small;
            if (expanded && !string.IsNullOrEmpty(description))
            {
                DrawHelp(listing, description);
            }
        }

        private void DrawHelp(Listing_Standard listing, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            GUI.color = HelpTextColor;
            listing.Label(text);
            GUI.color = Color.white;
            listing.Gap(2f);
        }

        private void DrawCheckbox(Listing_Standard listing, string label, ref bool value, string help)
        {
            bool before = value;
            listing.CheckboxLabeled(label, ref value, help, 0f, 1f);
            if (before != value)
            {
                NoteManualChange();
            }
        }

        private void DrawFloat(Listing_Standard listing, string label, ref float value, float min, float max, string key, string help)
        {
            float before = value;
            string buffer = GetBuffer(key);
            float labelHeight = Text.CalcHeight(label, listing.ColumnWidth * 0.42f);
            Rect rowRect = listing.GetRect(labelHeight);
            Rect labelRect = rowRect.LeftPartPixels(listing.ColumnWidth * 0.42f).Rounded();
            Rect fieldRect = rowRect.RightPartPixels(listing.ColumnWidth - listing.ColumnWidth * 0.42f).Rounded();
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = anchor;
            Widgets.TextFieldNumeric(fieldRect, ref value, ref buffer, min, max);
            TooltipHandler.TipRegion(rowRect, help + " Current value: " + FloatValueText(value, min, max) + ".");
            numericBuffers[key] = buffer;
            if (!Mathf.Approximately(before, value))
            {
                NoteManualChange();
            }
        }

        private void DrawInt(Listing_Standard listing, string label, ref int value, int min, int max, string key, string help)
        {
            int before = value;
            string buffer = GetBuffer(key);
            float labelHeight = Text.CalcHeight(label, listing.ColumnWidth * 0.42f);
            Rect rowRect = listing.GetRect(labelHeight);
            Rect labelRect = rowRect.LeftPartPixels(listing.ColumnWidth * 0.42f).Rounded();
            Rect fieldRect = rowRect.RightPartPixels(listing.ColumnWidth - listing.ColumnWidth * 0.42f).Rounded();
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = anchor;
            Widgets.TextFieldNumeric(fieldRect, ref value, ref buffer, min, max);
            TooltipHandler.TipRegion(rowRect, help + " Current value: " + IntValueText(value, max) + ".");
            numericBuffers[key] = buffer;
            if (before != value)
            {
                NoteManualChange();
            }
        }

        private string GetBuffer(string key)
        {
            return numericBuffers.TryGetValue(key, out string buffer) ? buffer : null;
        }

        private string PresetLabel()
        {
            return string.IsNullOrEmpty(currentPreset) ? CustomPresetName : currentPreset;
        }

        private void NoteManualChange()
        {
            currentPreset = CustomPresetName;
        }

        private void ClearNumericBuffers()
        {
            numericBuffers.Clear();
        }

        private static string FloatValueText(float value, float min, float max)
        {
            if (min >= 0f && max <= 1f)
            {
                return PercentText(value) + " (" + value.ToString("0.###") + ")";
            }

            return value.ToString("0.###");
        }

        private static string IntValueText(int value, int max)
        {
            if (max >= GenDate.TicksPerDay)
            {
                return value + " ticks (" + (value / (float)GenDate.TicksPerDay).ToString("0.##") + " days)";
            }

            return value.ToString();
        }

        private static string PercentText(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }

        private static string MultiplierText(float value)
        {
            return Mathf.Max(0f, value).ToString("0.##") + "x";
        }

        private void ClampSettings()
        {
            markedRaidFrequencyMultiplier = Mathf.Clamp(markedRaidFrequencyMultiplier, MinMarkedRaidFrequencyMultiplier, MaxMarkedRaidFrequencyMultiplier);
            warbandFrequencyMultiplier = Mathf.Clamp(warbandFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier);
            hordeFrequencyMultiplier = Mathf.Clamp(hordeFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier);
            probeFrequencyMultiplier = Mathf.Clamp(probeFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier);
            firstMarkedRaidDay = Mathf.Clamp(firstMarkedRaidDay, 1, 600);
            raidPointsMultiplier = Mathf.Clamp(raidPointsMultiplier, 0.05f, 10f);
            minimumRaidPoints = Mathf.Clamp(minimumRaidPoints, 0f, 10000f);
            maximumRaidPoints = Mathf.Clamp(maximumRaidPoints, 0f, 50000f);
            raidEscalationPerRaid = Mathf.Clamp(raidEscalationPerRaid, 0f, 2f);
            raidEscalationMaxBonus = Mathf.Clamp(raidEscalationMaxBonus, 0f, 20f);
            berserkerWeightMultiplier = Mathf.Clamp(berserkerWeightMultiplier, 0f, 5f);
            hunterWeightMultiplier = Mathf.Clamp(hunterWeightMultiplier, 0f, 5f);
            stalkerWeightMultiplier = Mathf.Clamp(stalkerWeightMultiplier, 0f, 5f);
            screamerWeightMultiplier = Mathf.Clamp(screamerWeightMultiplier, 0f, 5f);
            bruteWeightMultiplier = Mathf.Clamp(bruteWeightMultiplier, 0f, 5f);
            alphaWeightMultiplier = Mathf.Clamp(alphaWeightMultiplier, 0f, 5f);
            minimumHordeSize = Mathf.Clamp(minimumHordeSize, 1, 50);
            maximumHordeSize = Mathf.Clamp(maximumHordeSize, minimumHordeSize, 100);
            minimumProbeSize = Mathf.Clamp(minimumProbeSize, 1, 20);
            maximumProbeSize = Mathf.Clamp(maximumProbeSize, minimumProbeSize, 30);
            maximumAlphasPerRaid = Mathf.Clamp(maximumAlphasPerRaid, 0, 99);
            bloodExposureChance = Mathf.Clamp01(bloodExposureChance);
            foodExposureChance = Mathf.Clamp01(foodExposureChance);
            rjwExposureChance = Mathf.Clamp01(rjwExposureChance);
            infectedAssaultExposureChance = Mathf.Clamp01(infectedAssaultExposureChance);
            closeContactExposureChance = Mathf.Clamp01(closeContactExposureChance);
            corpseContaminationChance = Mathf.Clamp01(corpseContaminationChance);
            infectionProgressionSpeedMultiplier = Mathf.Clamp(infectionProgressionSpeedMultiplier, 0.05f, 10f);
            incubationDurationMultiplier = Mathf.Clamp(incubationDurationMultiplier, 0.05f, 10f);
            immunitySurvivalChance = Mathf.Clamp01(immunitySurvivalChance);
            terminalTransformationWeight = Mathf.Clamp(terminalTransformationWeight, 0f, 10f);
            terminalDeathWeight = Mathf.Clamp(terminalDeathWeight, 0f, 10f);
            reanimationChance = Mathf.Clamp01(reanimationChance);
            reanimationDelayTicks = Mathf.Clamp(reanimationDelayTicks, 60, GenDate.TicksPerDay * 30);
            starterLineageBreakthroughChance = Mathf.Clamp01(starterLineageBreakthroughChance);
            infightingChance = Mathf.Clamp01(infightingChance);
            socialTerrorStrength = Mathf.Clamp(socialTerrorStrength, 0f, 5f);
            raidCountdownVisibleDays = Mathf.Clamp(raidCountdownVisibleDays, 0f, 999f);
            raidCountdownHighPriorityDays = Mathf.Clamp(raidCountdownHighPriorityDays, 0f, 30f);
            contagionPulseIntervalTicks = Mathf.Clamp(contagionPulseIntervalTicks, 60, GenDate.TicksPerDay);
            maxContagionTargetsPerPulse = Mathf.Clamp(maxContagionTargetsPerPulse, 0, 50);
            corpseContaminationIntervalTicks = Mathf.Clamp(corpseContaminationIntervalTicks, 60, GenDate.TicksPerDay);
            maxCorpsesPerPulse = Mathf.Clamp(maxCorpsesPerPulse, 0, 50);
            tacticalRetargetIntervalTicks = Mathf.Clamp(tacticalRetargetIntervalTicks, 1, 2500);
            infightingCheckIntervalTicks = Mathf.Clamp(infightingCheckIntervalTicks, 60, GenDate.TicksPerDay);
            lordCleanupIntervalTicks = Mathf.Clamp(lordCleanupIntervalTicks, 60, GenDate.TicksPerDay);
            infectedStateMaintenanceIntervalTicks = Mathf.Clamp(infectedStateMaintenanceIntervalTicks, 60, GenDate.TicksPerDay);
            reanimationProcessIntervalTicks = Mathf.Clamp(reanimationProcessIntervalTicks, 60, GenDate.TicksPerDay);
            maxPendingReanimationsPerTick = Mathf.Clamp(maxPendingReanimationsPerTick, 1, 500);
        }

        private void ApplyDefaultPreset(bool updatePreset)
        {
            scheduledWarbandsEnabled = true;
            scheduledHordesEnabled = true;
            scoutingProbesEnabled = true;
            randomizeMarkedRaids = false;
            markedRaidFrequencyMultiplier = 1f;
            warbandFrequencyMultiplier = 1f;
            hordeFrequencyMultiplier = 1f;
            probeFrequencyMultiplier = 1f;
            firstMarkedRaidDay = 45;
            raidPointsMultiplier = 1f;
            minimumRaidPoints = 120f;
            maximumRaidPoints = 0f;
            raidEscalationPerRaid = DefaultRaidEscalationPerRaid;
            raidEscalationMaxBonus = DefaultRaidEscalationMaxBonus;
            ResetArrivalDefaults();
            ResetCompositionDefaults();
            bloodExposureChance = InfectionTransmissionChance;
            foodExposureChance = InfectionTransmissionChance;
            rjwExposureChance = InfectionTransmissionChance;
            infectedAssaultExposureChance = InfectionTransmissionChance;
            closeContactExposureChance = InfectionTransmissionChance;
            corpseContaminationChance = 1f;
            infectionProgressionSpeedMultiplier = 1f;
            incubationDurationMultiplier = 1f;
            immunitySurvivalChance = DefaultImmunitySurvivalChance;
            terminalTransformationWeight = DefaultTerminalTransformationWeight;
            terminalDeathWeight = DefaultTerminalDeathWeight;
            reanimationChance = 1f;
            reanimationDelayTicks = 900;
            starterLineageBreakthroughChance = 0.04f;
            markedAlwaysAssault = true;
            markedCanTimeoutOrFlee = false;
            tacticalRetargetingEnabled = true;
            priorityTargetingEnabled = true;
            doorTargetingEnabled = true;
            infightingChance = 0.12f;
            socialTerrorStrength = 1f;
            ResetStoryDefaults();
            ResetPerformanceDefaults();
            if (updatePreset)
            {
                currentPreset = "Default";
            }

            ClearNumericBuffers();
        }

        private void ApplyCasualPreset()
        {
            ApplyDefaultPreset(false);
            currentPreset = "Casual";
            markedRaidFrequencyMultiplier = 0.5f;
            warbandFrequencyMultiplier = 0.7f;
            hordeFrequencyMultiplier = 0.5f;
            probeFrequencyMultiplier = 0.8f;
            firstMarkedRaidDay = 60;
            raidPointsMultiplier = 0.7f;
            raidEscalationPerRaid = 0.08f;
            raidEscalationMaxBonus = 1.5f;
            bloodExposureChance = 0.22f;
            foodExposureChance = 0.15f;
            infectedAssaultExposureChance = 0.25f;
            closeContactExposureChance = 0.2f;
            corpseContaminationChance = 0.35f;
            infectionProgressionSpeedMultiplier = 0.55f;
            immunitySurvivalChance = 0.08f;
            terminalTransformationWeight = 0.85f;
            terminalDeathWeight = 0.15f;
            reanimationChance = 0.35f;
            minimumHordeSize = 2;
            maximumHordeSize = 6;
            maximumAlphasPerRaid = 1;
            socialTerrorStrength = 0.5f;
            ClampSettings();
            ClearNumericBuffers();
        }

        private void ApplyVanillaLikePreset()
        {
            ApplyDefaultPreset(false);
            currentPreset = "Vanilla-like";
            markedRaidFrequencyMultiplier = 0.75f;
            hordeFrequencyMultiplier = 0.6f;
            firstMarkedRaidDay = 50;
            raidPointsMultiplier = 0.9f;
            raidEscalationPerRaid = 0.1f;
            raidEscalationMaxBonus = 2f;
            corpseContaminationChance = 0.65f;
            reanimationChance = 0.7f;
            socialTerrorStrength = 0.75f;
            ClampSettings();
            ClearNumericBuffers();
        }

        private void ApplyBrutalPreset()
        {
            ApplyDefaultPreset(false);
            currentPreset = "Brutal";
            randomizeMarkedRaids = true;
            markedRaidFrequencyMultiplier = 1.6f;
            warbandFrequencyMultiplier = 1.4f;
            hordeFrequencyMultiplier = 1.8f;
            probeFrequencyMultiplier = 1.5f;
            firstMarkedRaidDay = 30;
            raidPointsMultiplier = 1.25f;
            raidEscalationPerRaid = 0.3f;
            raidEscalationMaxBonus = 8f;
            minimumHordeSize = 5;
            maximumHordeSize = 18;
            maximumProbeSize = 6;
            alphaWeightMultiplier = 1.6f;
            bruteWeightMultiplier = 1.4f;
            bloodExposureChance = 0.65f;
            infectedAssaultExposureChance = 0.65f;
            closeContactExposureChance = 0.65f;
            corpseContaminationChance = 1f;
            infectionProgressionSpeedMultiplier = 1.5f;
            immunitySurvivalChance = 0.01f;
            terminalTransformationWeight = 0.85f;
            terminalDeathWeight = 0.15f;
            reanimationChance = 1f;
            reanimationDelayTicks = 600;
            starterLineageBreakthroughChance = 0.08f;
            socialTerrorStrength = 1.5f;
            ClampSettings();
            ClearNumericBuffers();
        }

        private void ApplyOutbreakPreset()
        {
            ApplyDefaultPreset(false);
            currentPreset = "Outbreak simulator";
            scheduledWarbandsEnabled = true;
            scheduledHordesEnabled = true;
            scoutingProbesEnabled = true;
            randomizeMarkedRaids = true;
            markedRaidFrequencyMultiplier = 1.2f;
            hordeFrequencyMultiplier = 2.2f;
            probeFrequencyMultiplier = 1.6f;
            firstMarkedRaidDay = 20;
            raidPointsMultiplier = 0.85f;
            minimumHordeSize = 8;
            maximumHordeSize = 30;
            berserkerWeightMultiplier = 1.5f;
            hunterWeightMultiplier = 0.75f;
            stalkerWeightMultiplier = 1.35f;
            screamerWeightMultiplier = 1.4f;
            bruteWeightMultiplier = 0.8f;
            alphaWeightMultiplier = 0.6f;
            bloodExposureChance = 0.8f;
            foodExposureChance = 0.7f;
            infectedAssaultExposureChance = 0.8f;
            closeContactExposureChance = 0.9f;
            corpseContaminationChance = 1f;
            infectionProgressionSpeedMultiplier = 2.2f;
            immunitySurvivalChance = 0.005f;
            terminalTransformationWeight = 0.85f;
            terminalDeathWeight = 0.15f;
            reanimationChance = 1f;
            reanimationDelayTicks = 300;
            starterLineageBreakthroughChance = 0.12f;
            contagionPulseIntervalTicks = 300;
            maxContagionTargetsPerPulse = 6;
            corpseContaminationIntervalTicks = 360;
            maxCorpsesPerPulse = 5;
            socialTerrorStrength = 1.25f;
            ClampSettings();
            ClearNumericBuffers();
        }

        private void ResetArrivalDefaults()
        {
            allowGroupedEdgeArrival = true;
            allowDistributedGroupArrival = true;
            allowDistributedArrival = true;
            allowSingleEdgeArrival = true;
        }

        private void ResetCompositionDefaults()
        {
            berserkerWeightMultiplier = 1f;
            hunterWeightMultiplier = 1f;
            stalkerWeightMultiplier = 1f;
            screamerWeightMultiplier = 1f;
            bruteWeightMultiplier = 1f;
            alphaWeightMultiplier = 1f;
            allowMarkedChildren = false;
            minimumHordeSize = 3;
            maximumHordeSize = 12;
            minimumProbeSize = 2;
            maximumProbeSize = 4;
            maximumAlphasPerRaid = 99;
        }

        private void ResetStoryDefaults()
        {
            raidCountdownAlertEnabled = true;
            raidCountdownVisibleDays = 999f;
            raidCountdownHighPriorityDays = 1f;
            detailedRaidLetters = true;
            incidentLogEnabled = true;
            debugActionsEnabled = true;
        }

        private void ResetPerformanceDefaults()
        {
            contagionPulseIntervalTicks = 500;
            maxContagionTargetsPerPulse = 3;
            corpseContaminationIntervalTicks = 750;
            maxCorpsesPerPulse = 2;
            tacticalRetargetIntervalTicks = 6;
            infightingCheckIntervalTicks = 1000;
            lordCleanupIntervalTicks = 250;
            infectedStateMaintenanceIntervalTicks = 2500;
            reanimationProcessIntervalTicks = 2500;
            maxPendingReanimationsPerTick = 24;
        }
    }



    // Moved to Core/MarkedIdeologyUtility.cs





    // Moved to Core/Alert_MarkedMenRaidCountdown.cs



    // Moved to Infection/HediffComp_CrossVirus.cs



    // Moved to AI/CrossedTacticalAI.cs











    // Moved to Raids/IncidentWorker_CrossedRaid.cs
    // Moved to Raids/IncidentWorker_CrossedHorde.cs
    // Moved to Raids/IncidentWorker_CrossedProbe.cs
}
