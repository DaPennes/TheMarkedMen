using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
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
        private const int CurrentSettingsVersion = 7;
        public const float InfectionTransmissionChance = 0.45f;
        public const float DefaultMarkedRaidFrequencyMultiplier = 1f;
        public const float MinMarkedRaidFrequencyMultiplier = 0f;
        public const float MaxMarkedRaidFrequencyMultiplier = 5f;
        public const float DefaultRaidEscalationPerRaid = 0.18f;
        public const float DefaultRaidEscalationMaxBonus = 5f;

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
        public float immunitySurvivalChance = 0.02f;
        public float terminalTransformationWeight = 0.55f;
        public float terminalDeathWeight = 0.45f;
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
            Scribe_Values.Look(ref immunitySurvivalChance, "immunitySurvivalChance", 0.02f);
            Scribe_Values.Look(ref terminalTransformationWeight, "terminalTransformationWeight", 0.55f);
            Scribe_Values.Look(ref terminalDeathWeight, "terminalDeathWeight", 0.45f);
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
                    immunitySurvivalChance = 0.02f;
                    terminalTransformationWeight = 0.55f;
                    terminalDeathWeight = 0.45f;
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

                settingsVersion = CurrentSettingsVersion;
            }

            ClampSettings();
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
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 2900f);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.Label("Preset: " + currentPreset);
            if (listing.ButtonText("Casual", null, 1f))
            {
                ApplyCasualPreset();
            }
            if (listing.ButtonText("Vanilla-like", null, 1f))
            {
                ApplyVanillaLikePreset();
            }
            if (listing.ButtonText("Default", null, 1f))
            {
                ApplyDefaultPreset(true);
            }
            if (listing.ButtonText("Brutal", null, 1f))
            {
                ApplyBrutalPreset();
            }
            if (listing.ButtonText("Outbreak simulator", null, 1f))
            {
                ApplyOutbreakPreset();
            }
            listing.Gap();

            listing.CheckboxLabeled("Enable Marked Virus transmission", ref infectionEnabled);
            listing.CheckboxLabeled("Verbose compatibility log on load", ref verboseCompatibilityLogging);
            listing.Gap();

            listing.Label("Raid tuning");
            listing.CheckboxLabeled("Enable scheduled warbands", ref scheduledWarbandsEnabled);
            listing.CheckboxLabeled("Enable scheduled hordes", ref scheduledHordesEnabled);
            listing.CheckboxLabeled("Enable scouting probes", ref scoutingProbesEnabled);
            listing.CheckboxLabeled("Randomize Marked raid timing and arrivals", ref randomizeMarkedRaids);
            DrawInt(listing, "First Marked raid day", ref firstMarkedRaidDay, 1, 600, "firstMarkedRaidDay");
            DrawFloat(listing, "Global Marked raid frequency", ref markedRaidFrequencyMultiplier, MinMarkedRaidFrequencyMultiplier, MaxMarkedRaidFrequencyMultiplier, "markedRaidFrequencyMultiplier");
            DrawFloat(listing, "Warband frequency", ref warbandFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "warbandFrequencyMultiplier");
            DrawFloat(listing, "Horde frequency", ref hordeFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "hordeFrequencyMultiplier");
            DrawFloat(listing, "Probe storyteller chance", ref probeFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "probeFrequencyMultiplier");
            DrawFloat(listing, "Raid points multiplier", ref raidPointsMultiplier, 0.05f, 10f, "raidPointsMultiplier");
            DrawFloat(listing, "Minimum raid points", ref minimumRaidPoints, 0f, 10000f, "minimumRaidPoints");
            DrawFloat(listing, "Maximum raid points (0 = uncapped)", ref maximumRaidPoints, 0f, 50000f, "maximumRaidPoints");
            DrawFloat(listing, "Raid escalation per warband", ref raidEscalationPerRaid, 0f, 2f, "raidEscalationPerRaid");
            DrawFloat(listing, "Raid escalation cap", ref raidEscalationMaxBonus, 0f, 20f, "raidEscalationMaxBonus");
            listing.CheckboxLabeled("Allow grouped edge arrival", ref allowGroupedEdgeArrival);
            listing.CheckboxLabeled("Allow distributed group arrival", ref allowDistributedGroupArrival);
            listing.CheckboxLabeled("Allow distributed edge arrival", ref allowDistributedArrival);
            listing.CheckboxLabeled("Allow single edge arrival", ref allowSingleEdgeArrival);
            listing.Gap();

            listing.Label("Enemy composition");
            DrawFloat(listing, "Berserker weight", ref berserkerWeightMultiplier, 0f, 5f, "berserkerWeightMultiplier");
            DrawFloat(listing, "Hunter weight", ref hunterWeightMultiplier, 0f, 5f, "hunterWeightMultiplier");
            DrawFloat(listing, "Stalker weight", ref stalkerWeightMultiplier, 0f, 5f, "stalkerWeightMultiplier");
            DrawFloat(listing, "Screamer weight", ref screamerWeightMultiplier, 0f, 5f, "screamerWeightMultiplier");
            DrawFloat(listing, "Brute weight", ref bruteWeightMultiplier, 0f, 5f, "bruteWeightMultiplier");
            DrawFloat(listing, "Alpha weight", ref alphaWeightMultiplier, 0f, 5f, "alphaWeightMultiplier");
            listing.CheckboxLabeled("Allow Marked children", ref allowMarkedChildren);
            DrawInt(listing, "Minimum horde size", ref minimumHordeSize, 1, 50, "minimumHordeSize");
            DrawInt(listing, "Maximum horde size", ref maximumHordeSize, 1, 100, "maximumHordeSize");
            DrawInt(listing, "Minimum probe size", ref minimumProbeSize, 1, 20, "minimumProbeSize");
            DrawInt(listing, "Maximum probe size", ref maximumProbeSize, 1, 30, "maximumProbeSize");
            DrawInt(listing, "Maximum alphas per raid", ref maximumAlphasPerRaid, 0, 99, "maximumAlphasPerRaid");
            listing.Gap();

            listing.Label("Disease tuning");
            DrawFloat(listing, "Blood exposure chance", ref bloodExposureChance, 0f, 1f, "bloodExposureChance");
            DrawFloat(listing, "Contaminated food chance", ref foodExposureChance, 0f, 1f, "foodExposureChance");
            DrawFloat(listing, "Infected assault contact chance", ref infectedAssaultExposureChance, 0f, 1f, "infectedAssaultExposureChance");
            DrawFloat(listing, "Close-contact contagion chance", ref closeContactExposureChance, 0f, 1f, "closeContactExposureChance");
            DrawFloat(listing, "Corpse contamination chance", ref corpseContaminationChance, 0f, 1f, "corpseContaminationChance");
            DrawFloat(listing, "Infection progression speed", ref infectionProgressionSpeedMultiplier, 0.05f, 10f, "infectionProgressionSpeedMultiplier");
            DrawFloat(listing, "Incubation duration multiplier", ref incubationDurationMultiplier, 0.05f, 10f, "incubationDurationMultiplier");
            DrawFloat(listing, "Immunity survival chance", ref immunitySurvivalChance, 0f, 1f, "immunitySurvivalChance");
            DrawFloat(listing, "Terminal transformation weight", ref terminalTransformationWeight, 0f, 10f, "terminalTransformationWeight");
            DrawFloat(listing, "Terminal death weight", ref terminalDeathWeight, 0f, 10f, "terminalDeathWeight");
            DrawFloat(listing, "Reanimation chance", ref reanimationChance, 0f, 1f, "reanimationChance");
            DrawInt(listing, "Reanimation delay ticks", ref reanimationDelayTicks, 60, GenDate.TicksPerDay * 30, "reanimationDelayTicks");
            DrawFloat(listing, "Starter-lineage breakthrough chance", ref starterLineageBreakthroughChance, 0f, 1f, "starterLineageBreakthroughChance");
            listing.Gap();

            listing.Label("AI and behavior");
            listing.CheckboxLabeled("Marked pawns always assault", ref markedAlwaysAssault);
            listing.CheckboxLabeled("Marked pawns can time out or flee", ref markedCanTimeoutOrFlee);
            listing.CheckboxLabeled("Enable tactical retargeting", ref tacticalRetargetingEnabled);
            listing.CheckboxLabeled("Enable power, food, doctor, and turret priority targeting", ref priorityTargetingEnabled);
            listing.CheckboxLabeled("Enable door and wall targeting", ref doorTargetingEnabled);
            DrawFloat(listing, "Infighting chance", ref infightingChance, 0f, 1f, "infightingChance");
            DrawFloat(listing, "Panic/social terror strength", ref socialTerrorStrength, 0f, 5f, "socialTerrorStrength");
            listing.Gap();

            listing.Label("Story and UI");
            listing.CheckboxLabeled("Enable raid countdown alert", ref raidCountdownAlertEnabled);
            DrawFloat(listing, "Countdown alert visible days", ref raidCountdownVisibleDays, 0f, 999f, "raidCountdownVisibleDays");
            DrawFloat(listing, "Countdown high-priority days", ref raidCountdownHighPriorityDays, 0f, 30f, "raidCountdownHighPriorityDays");
            listing.CheckboxLabeled("Detailed raid letters", ref detailedRaidLetters);
            listing.CheckboxLabeled("Record incident log entries", ref incidentLogEnabled);
            listing.CheckboxLabeled("Enable Dev Mode debug actions", ref debugActionsEnabled);
            listing.Gap();

            listing.Label("Performance");
            DrawInt(listing, "Contagion pulse interval ticks", ref contagionPulseIntervalTicks, 60, GenDate.TicksPerDay, "contagionPulseIntervalTicks");
            DrawInt(listing, "Max contagion targets per pulse", ref maxContagionTargetsPerPulse, 0, 50, "maxContagionTargetsPerPulse");
            DrawInt(listing, "Corpse contamination interval ticks", ref corpseContaminationIntervalTicks, 60, GenDate.TicksPerDay, "corpseContaminationIntervalTicks");
            DrawInt(listing, "Max corpses per pulse", ref maxCorpsesPerPulse, 0, 50, "maxCorpsesPerPulse");
            DrawInt(listing, "Tactical retarget interval ticks", ref tacticalRetargetIntervalTicks, 1, 2500, "tacticalRetargetIntervalTicks");
            DrawInt(listing, "Infighting check interval ticks", ref infightingCheckIntervalTicks, 60, GenDate.TicksPerDay, "infightingCheckIntervalTicks");
            DrawInt(listing, "Lord cleanup interval ticks", ref lordCleanupIntervalTicks, 60, GenDate.TicksPerDay, "lordCleanupIntervalTicks");
            DrawInt(listing, "Infected-state maintenance interval ticks", ref infectedStateMaintenanceIntervalTicks, 60, GenDate.TicksPerDay, "infectedStateMaintenanceIntervalTicks");
            DrawInt(listing, "Reanimation process interval ticks", ref reanimationProcessIntervalTicks, 60, GenDate.TicksPerDay, "reanimationProcessIntervalTicks");
            DrawInt(listing, "Max pending reanimations per tick", ref maxPendingReanimationsPerTick, 1, 500, "maxPendingReanimationsPerTick");
            listing.Gap();

            listing.Label("RJW compatibility");
            listing.CheckboxLabeled("Auto-enable RJW bridge when RJW is installed", ref rjwAutoEnableWhenInstalled);
            listing.CheckboxLabeled("Enable RJW Marked Virus bridge", ref rjwIntegrationEnabled);
            listing.Label("When RJW is installed, the bridge can transmit Marked Virus through adult RJW close-contact events, gives infected adult pawns a 75% chance to start valid RJW forced enemy-intercourse jobs with adult humanlike targets, preserves active RJW jobs, and adds no hard dependency.");
            DrawFloat(listing, "RJW exposure chance", ref rjwExposureChance, 0f, 1f, "rjwExposureChance");
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
                return Mathf.Clamp01(props?.terminalTransformationChance ?? 0.55f);
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

        private void DrawFloat(Listing_Standard listing, string label, ref float value, float min, float max, string key)
        {
            string buffer = GetBuffer(key);
            listing.TextFieldNumericLabeled(label, ref value, ref buffer, min, max);
            numericBuffers[key] = buffer;
        }

        private void DrawInt(Listing_Standard listing, string label, ref int value, int min, int max, string key)
        {
            string buffer = GetBuffer(key);
            listing.TextFieldNumericLabeled(label, ref value, ref buffer, min, max);
            numericBuffers[key] = buffer;
        }

        private string GetBuffer(string key)
        {
            return numericBuffers.TryGetValue(key, out string buffer) ? buffer : null;
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
            immunitySurvivalChance = 0.02f;
            terminalTransformationWeight = 0.55f;
            terminalDeathWeight = 0.45f;
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
            terminalTransformationWeight = 0.35f;
            terminalDeathWeight = 0.65f;
            reanimationChance = 0.35f;
            minimumHordeSize = 2;
            maximumHordeSize = 6;
            maximumAlphasPerRaid = 1;
            socialTerrorStrength = 0.5f;
            ClampSettings();
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
            terminalTransformationWeight = 0.75f;
            terminalDeathWeight = 0.25f;
            reanimationChance = 1f;
            reanimationDelayTicks = 600;
            starterLineageBreakthroughChance = 0.08f;
            socialTerrorStrength = 1.5f;
            ClampSettings();
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
            terminalTransformationWeight = 0.9f;
            terminalDeathWeight = 0.1f;
            reanimationChance = 1f;
            reanimationDelayTicks = 300;
            starterLineageBreakthroughChance = 0.12f;
            contagionPulseIntervalTicks = 300;
            maxContagionTargetsPerPulse = 6;
            corpseContaminationIntervalTicks = 360;
            maxCorpsesPerPulse = 5;
            socialTerrorStrength = 1.25f;
            ClampSettings();
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

    public static class CADefOf
    {
        private static HediffDef crossVirus;
        private static HediffDef crossVirusImmunity;
        private static HediffDef crossedRash;
        private static HediffDef bloodRush;
        private static HediffDef commandAura;
        private static HediffDef panic;
        private static ThoughtDef crossedSocialTerror;
        private static FactionDef crossedFaction;
        private static InteractionDef crossedPredatoryTaunt;
        private static InteractionDef crossedFalseMercy;
        private static InteractionDef crossedPackLaughter;
        private static InteractionDef crossedInfectionGloat;
        private static PawnKindDef berserker;
        private static PawnKindDef hunter;
        private static PawnKindDef brute;
        private static PawnKindDef stalker;
        private static PawnKindDef screamer;
        private static PawnKindDef alpha;
        private static PawnKindDef child;
        private static TattooDef crossedFaceTattoo;
        private static IncidentDef crossedRaid;
        private static IncidentDef crossedHorde;
        private static IncidentDef crossedProbe;

        public static HediffDef CrossVirus => crossVirus ?? (crossVirus = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossVirus"));
        public static HediffDef CrossVirusImmunity => crossVirusImmunity ?? (crossVirusImmunity = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossVirusImmunity"));
        public static HediffDef CrossedRash => crossedRash ?? (crossedRash = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedRash"));
        public static HediffDef BloodRush => bloodRush ?? (bloodRush = DefDatabase<HediffDef>.GetNamedSilentFail("CA_BloodRush"));
        public static HediffDef CommandAura => commandAura ?? (commandAura = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CommandAura"));
        public static HediffDef Panic => panic ?? (panic = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedPanic"));
        public static ThoughtDef CrossedSocialTerror => crossedSocialTerror ?? (crossedSocialTerror = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_CrossedSocialTerror"));
        public static FactionDef CrossedFaction => crossedFaction ?? (crossedFaction = DefDatabase<FactionDef>.GetNamedSilentFail("CA_CrossedFaction"));
        public static InteractionDef CrossedPredatoryTaunt => crossedPredatoryTaunt ?? (crossedPredatoryTaunt = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedPredatoryTaunt"));
        public static InteractionDef CrossedFalseMercy => crossedFalseMercy ?? (crossedFalseMercy = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedFalseMercy"));
        public static InteractionDef CrossedPackLaughter => crossedPackLaughter ?? (crossedPackLaughter = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedPackLaughter"));
        public static InteractionDef CrossedInfectionGloat => crossedInfectionGloat ?? (crossedInfectionGloat = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedInfectionGloat"));
        public static PawnKindDef Berserker => berserker ?? (berserker = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedBerserker"));
        public static PawnKindDef Hunter => hunter ?? (hunter = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedHunter"));
        public static PawnKindDef Brute => brute ?? (brute = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedBrute"));
        public static PawnKindDef Stalker => stalker ?? (stalker = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedStalker"));
        public static PawnKindDef Screamer => screamer ?? (screamer = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedScreamer"));
        public static PawnKindDef Alpha => alpha ?? (alpha = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedAlpha"));
        public static PawnKindDef Child => child ?? (child = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedChild"));
        public static TattooDef CrossedFaceTattoo => crossedFaceTattoo ?? (crossedFaceTattoo = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRash"));
        public static IncidentDef CrossedRaid => crossedRaid ?? (crossedRaid = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedRaid"));
        public static IncidentDef CrossedHorde => crossedHorde ?? (crossedHorde = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedHorde"));
        public static IncidentDef CrossedProbe => crossedProbe ?? (crossedProbe = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedProbe"));
    }

    public sealed class TheMarkedMenGameComponent : GameComponent
    {
        private const int MaintenanceTickInterval = 2500;
        private const int RaidMonitorIntervalTicks = 250;
        private const int ReanimationDelayTicks = 900;
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
        private const float RaidEscalationPerRaid = 0.18f;
        private const float RaidEscalationMaxBonus = 5f;
        private const float RandomRaidIntervalMinFactor = 0.6f;
        private const float RandomRaidIntervalMaxFactor = 1.6f;

        private readonly Game game;
        private int nextMaintenanceTick;
        private int nextReanimationProcessTick;
        private int nextRaidMonitorTick;
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
        private List<string> recentIncidents = new List<string>();
        private List<Pawn> pendingReanimationPawns = new List<Pawn>();
        private List<int> pendingReanimationTicks = new List<int>();
        private List<Pawn> activeRaidPawns = new List<Pawn>();
        private List<Pawn> activeRaidColonistsAtStart = new List<Pawn>();

        public TheMarkedMenGameComponent(Game game)
        {
            this.game = game;
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            EnsureCrossedFaction(false);
            EnsureCrossedWorldSettlement();
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
            InitializeStarterLineageResistance();
            EnsureInfectedStateOnLoadedPawns();
            int ticks = Find.TickManager?.TicksGame ?? 0;
            int raidFirstTick = TheMarkedMenSettings.FirstMarkedRaidTick;
            int hordeFirstTick = CurrentHordeFirstTick;
            if (ticks >= raidFirstTick)
            {
                raidScheduleActivated = true;
            }
            else
            {
                raidScheduleActivated = false;
            }

            if (TheMarkedMenSettings.WarbandsEnabled)
            {
                bool raidTimerInvalid = nextRaidTick <= 0
                    || !raidScheduleActivated && ticks < raidFirstTick && nextRaidTick != raidFirstTick
                    || raidScheduleActivated && nextRaidTick - ticks > CalculateMaxAdjustedRaidIntervalTicks();
                if (raidTimerInvalid)
                {
                    ScheduleNextRaid(ticks);
                }
                else if (raidScheduleVersion < RaidScheduleVersion)
                {
                    MigrateRaidSchedule(ticks);
                }
            }
            else
            {
                nextRaidTick = 0;
            }

            raidScheduleVersion = RaidScheduleVersion;

            if (TheMarkedMenSettings.HordesEnabled)
            {
                bool hordeTimerInvalid = nextHordeTick <= 0
                    || ticks < hordeFirstTick && nextHordeTick != hordeFirstTick
                    || ticks >= hordeFirstTick && nextHordeTick - ticks > CalculateMaxAdjustedHordeIntervalTicks();
                if (hordeTimerInvalid)
                {
                    ScheduleNextHorde(ticks);
                }
            }
            else
            {
                nextHordeTick = 0;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref nextMaintenanceTick, "nextMaintenanceTick", 0);
            Scribe_Values.Look(ref nextReanimationProcessTick, "nextReanimationProcessTick", 0);
            Scribe_Values.Look(ref nextRaidMonitorTick, "nextRaidMonitorTick", 0);
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
            Scribe_Collections.Look(ref pendingReanimationPawns, "pendingReanimationPawns", LookMode.Reference);
            Scribe_Collections.Look(ref pendingReanimationTicks, "pendingReanimationTicks", LookMode.Value);
            Scribe_Collections.Look(ref activeRaidPawns, "activeRaidPawns", LookMode.Reference);
            Scribe_Collections.Look(ref activeRaidColonistsAtStart, "activeRaidColonistsAtStart", LookMode.Reference);
            if (recentIncidents == null)
            {
                recentIncidents = new List<string>();
            }
            if (pendingReanimationPawns == null)
            {
                pendingReanimationPawns = new List<Pawn>();
            }
            if (pendingReanimationTicks == null)
            {
                pendingReanimationTicks = new List<int>();
            }
            while (pendingReanimationTicks.Count < pendingReanimationPawns.Count)
            {
                pendingReanimationTicks.Add(0);
            }
            while (pendingReanimationTicks.Count > pendingReanimationPawns.Count)
            {
                pendingReanimationTicks.RemoveAt(pendingReanimationTicks.Count - 1);
            }
            if (activeRaidPawns == null)
            {
                activeRaidPawns = new List<Pawn>();
            }
            if (activeRaidColonistsAtStart == null)
            {
                activeRaidColonistsAtStart = new List<Pawn>();
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null)
            {
                return;
            }

            int ticks = Find.TickManager.TicksGame;
            TryFireScheduledRaid(ticks);
            MonitorActiveRaid(ticks);
            if (ticks >= nextReanimationProcessTick)
            {
                nextReanimationProcessTick = ticks + TheMarkedMenSettings.ReanimationProcessIntervalTicks;
                ProcessPendingReanimations();
            }

            if (ticks < nextMaintenanceTick)
            {
                return;
            }

            nextMaintenanceTick = ticks + MaintenanceTickInterval;
            InitializeStarterLineageResistance();
            EnsureInfectedStateOnLoadedPawns();
            TryFireScheduledHorde(ticks);
        }

        public bool TryGetRaidCountdownForAlert(out int nextTick, out int ticksUntilRaid, out Map targetMap)
        {
            nextTick = 0;
            ticksUntilRaid = 0;
            targetMap = null;

            if (Find.TickManager == null || activeRaid || CADefOf.CrossedRaid == null || !TheMarkedMenSettings.WarbandsEnabled)
            {
                return false;
            }

            targetMap = FindRaidTargetMap();
            if (targetMap == null)
            {
                return false;
            }

            int ticks = Find.TickManager.TicksGame;
            int raidFirstTick = TheMarkedMenSettings.FirstMarkedRaidTick;
            if (!raidScheduleActivated && ticks < raidFirstTick)
            {
                nextTick = raidFirstTick;
            }
            else
            {
                nextTick = nextRaidTick;
                if (nextTick <= 0)
                {
                    nextTick = ticks + CalculateAdjustedRaidIntervalTicks(false);
                }
            }

            if (nextTick < ticks)
            {
                nextTick = ticks;
            }

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
            {
                return false;
            }

            int ticks = Find.TickManager.TicksGame;
            raidScheduleActivated = true;
            nextRaidTick = ticks + DebugEarlyRaidDelayTicks;
            AddIncident("DevMode moved the next Marked Men raid to one in-game hour from now.");
            return true;
        }

        public bool DebugFireRaidNow()
        {
            if (Find.TickManager == null)
            {
                return false;
            }

            int ticks = Find.TickManager.TicksGame;
            raidScheduleActivated = true;
            nextRaidTick = ticks;
            bool fired = TryFireRaidIncident(true);
            if (fired)
            {
                ScheduleNextRaid(ticks);
            }

            return fired;
        }

        public bool DebugFireHordeNow()
        {
            if (Find.TickManager == null)
            {
                return false;
            }

            bool fired = TryFireHordeIncident(true);
            if (fired)
            {
                ScheduleNextHorde(Find.TickManager.TicksGame);
            }

            return fired;
        }

        public bool DebugFireProbeNow()
        {
            return TryFireProbeIncident(true);
        }

        public void AddIncident(string text)
        {
            if (!TheMarkedMenSettings.IncidentLogEnabled)
            {
                return;
            }

            if (text.NullOrEmpty())
            {
                return;
            }

            string day = GenDate.DaysPassed.ToString();
            recentIncidents.Insert(0, "Day " + day + ": " + text);
            while (recentIncidents.Count > RecentIncidentLimit)
            {
                recentIncidents.RemoveAt(recentIncidents.Count - 1);
            }
        }

        public void NotifyExposure(Pawn pawn, string source)
        {
        }

        public void NotifyDiseaseActivated(Pawn pawn)
        {
            if (pawn == null || pawn.Faction != Faction.OfPlayer)
            {
                return;
            }

            AddIncident(pawn.LabelShortCap + "'s Marked Virus incubation ended with active symptoms.");
            if (pawn.Spawned)
            {
                Messages.Message(pawn.LabelShortCap + " is showing active Marked Virus symptoms.", pawn, MessageTypeDefOf.ThreatSmall, false);
            }
        }

        public void NotifyIncubationSurvived(Pawn pawn)
        {
            if (pawn == null || pawn.Faction != Faction.OfPlayer)
            {
                return;
            }

            AddIncident(pawn.LabelShortCap + " survived Marked Virus incubation and developed immunity.");
            if (pawn.Spawned)
            {
                Messages.Message(pawn.LabelShortCap + " resisted the Marked Virus and developed immunity.", pawn, MessageTypeDefOf.PositiveEvent, false);
            }
        }

        public void NotifyTransformation(Pawn pawn)
        {
            AddIncident(pawn.LabelShortCap + " transformed into one of the Marked Men.");
        }

        public void NotifyVirusDeath(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            AddIncident(pawn.LabelShortCap + " died from terminal Marked Virus collapse.");
            if (pawn.Spawned && pawn.Faction == Faction.OfPlayer)
            {
                Messages.Message(pawn.LabelShortCap + " died from the Marked Virus.", pawn, MessageTypeDefOf.ThreatSmall, false);
            }
        }

        public void NotifyReanimationQueued(Pawn pawn)
        {
            if (pawn != null && pawn.Faction == Faction.OfPlayer)
            {
                AddIncident(pawn.LabelShortCap + " died while infected. Reanimation is likely.");
            }
        }

        public void NotifyReanimated(Pawn pawn)
        {
            AddIncident(pawn.LabelShortCap + " rose from death as one of the Marked Men.");
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
            if (map == null || spawnedPawns == null || spawnedPawns.Count == 0)
            {
                return;
            }

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
                    {
                        activeRaidColonistsAtStart.Add(colonists[i]);
                    }
                }
            }

            activeRaidWaveCount++;
            activeRaidPoints += points;
            activeRaidPeakInfected += spawnedPawns.Count;
            for (int i = 0; i < spawnedPawns.Count; i++)
            {
                Pawn pawn = spawnedPawns[i];
                if (pawn != null && !activeRaidPawns.Contains(pawn))
                {
                    activeRaidPawns.Add(pawn);
                }
            }
        }

        private void MonitorActiveRaid(int ticks)
        {
            if (!activeRaid || ticks < nextRaidMonitorTick)
            {
                return;
            }

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

            if (anyThreatRemaining)
            {
                return;
            }

            CrossedRaidReport report = BuildActiveRaidReport();
            if (report.SurvivingColonists > 0)
            {
                survivedRaidCount++;
                report.RaidsSurvived = survivedRaidCount;
                AddIncident("Colony survived Marked Men raid wave " + totalCrossedRaidsStarted + ": " + report.InfectedKilled + " infected killed, " + report.ColonistCasualties + " colony casualties.");
            }
            else
            {
                AddIncident("Marked Men raid wave " + totalCrossedRaidsStarted + " ended with no standing colony survivors.");
            }

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
                {
                    infectedNeutralized++;
                }
            }

            int colonistDeaths = 0;
            int colonistsDowned = 0;
            for (int i = 0; i < activeRaidColonistsAtStart.Count; i++)
            {
                Pawn pawn = activeRaidColonistsAtStart[i];
                if (pawn == null || pawn.Destroyed || pawn.Dead)
                {
                    colonistDeaths++;
                }
                else if (pawn.Downed)
                {
                    colonistsDowned++;
                }
            }

            int survivingColonists = 0;
            IReadOnlyList<Pawn> colonists = activeRaidMap?.mapPawns?.FreeColonistsSpawned;
            if (colonists != null)
            {
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn pawn = colonists[i];
                    if (pawn != null && !pawn.Dead)
                    {
                        survivingColonists++;
                    }
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
            if (factionDef == null || Find.FactionManager == null)
            {
                return null;
            }

            Faction existing = Find.FactionManager.FirstFactionOfDef(factionDef);
            if (existing != null)
            {
                EnsureFactionHostility(existing);
                return existing;
            }

            if (!allowCreate)
            {
                return null;
            }

            try
            {
                FactionGenerator.CreateFactionAndAddToManager(factionDef);
                Faction generated = Find.FactionManager.FirstFactionOfDef(factionDef);
                if (generated != null)
                {
                    EnsureFactionHostility(generated);
                }

                return generated;
            }
            catch (Exception ex)
            {
                Log.Error("[The Marked Men] Failed to create Marked Men faction: " + ex);
                return null;
            }
        }

        private void EnsureCrossedWorldSettlement()
        {
            Faction faction = EnsureCrossedFaction(true);
            if (faction == null || Find.World?.worldObjects == null || WorldObjectDefOf.Settlement == null)
            {
                return;
            }

            if (HasCrossedSettlement(faction))
            {
                crossedWorldSettlementInitialized = true;
                return;
            }

            if (crossedWorldSettlementInitialized)
            {
                return;
            }

            try
            {
                PlanetTile tile = TileFinder.RandomSettlementTileFor(faction, true, null);
                if (!tile.Valid)
                {
                    tile = TileFinder.RandomSettlementTileFor(faction, false, null);
                }

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
                {
                    settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement, faction.def.settlementNameMaker);
                }
                else
                {
                    settlement.Name = "Marked Village";
                }

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
            if (settlements == null)
            {
                return false;
            }

            FactionDef factionDef = faction?.def ?? CADefOf.CrossedFaction;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement settlement = settlements[i];
                if (settlement != null && !settlement.Destroyed && settlement.Faction?.def == factionDef)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFactionHostility(Faction faction)
        {
            if (faction == null || Faction.OfPlayer == null || faction == Faction.OfPlayer)
            {
                return;
            }

            try
            {
                if (faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Hostile)
                {
                    faction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false, null, default);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Failed to enforce hostile faction relation: " + ex.Message);
            }
        }

        public void QueueCrossedReanimation(Pawn pawn)
        {
            if (!CrossedUtility.ShouldReanimateAsCrossed(pawn))
            {
                return;
            }

            if (pendingReanimationPawns.Contains(pawn))
            {
                return;
            }

            if (!Rand.Chance(TheMarkedMenSettings.ReanimationChance))
            {
                CrossedUtility.MarkDiedFromMarkedVirus(pawn);
                return;
            }

            pendingReanimationPawns.Add(pawn);
            pendingReanimationTicks.Add((Find.TickManager?.TicksGame ?? 0) + TheMarkedMenSettings.ReanimationDelayTicks);
            NotifyReanimationQueued(pawn);
        }

        private void ProcessPendingReanimations()
        {
            int ticks = Find.TickManager?.TicksGame ?? 0;
            int processed = 0;
            int maxProcessed = TheMarkedMenSettings.MaxPendingReanimationsPerTick;
            for (int i = pendingReanimationPawns.Count - 1; i >= 0; i--)
            {
                if (processed >= maxProcessed)
                {
                    return;
                }

                Pawn pawn = pendingReanimationPawns[i];
                int readyTick = i < pendingReanimationTicks.Count ? pendingReanimationTicks[i] : 0;
                if (ticks < readyTick)
                {
                    continue;
                }

                if (pawn == null || pawn.Destroyed || !pawn.Dead || !CrossedUtility.ShouldReanimateAsCrossed(pawn))
                {
                    RemovePendingReanimationAt(i);
                    continue;
                }

                Corpse corpse = pawn.Corpse;
                if (corpse == null || corpse.Destroyed)
                {
                    RemovePendingReanimationAt(i);
                    continue;
                }

                if (TryReanimatePawn(pawn))
                {
                    RemovePendingReanimationAt(i);
                    processed++;
                }
                else
                {
                    pendingReanimationTicks[i] = ticks + TheMarkedMenSettings.ReanimationProcessIntervalTicks;
                    processed++;
                }
            }
        }

        private bool TryReanimatePawn(Pawn pawn)
        {
            try
            {
                ResurrectionParams parms = new ResurrectionParams
                {
                    gettingScarsChance = 0f,
                    removeDiedThoughts = false,
                    restoreMissingParts = false,
                    canPickUpOpportunisticWeapons = true,
                    canTimeoutOrFlee = TheMarkedMenSettings.MarkedCanTimeoutOrFlee,
                    canKidnap = false,
                    canSteal = false,
                    useAvoidGridSmart = false
                };

                if (!ResurrectionUtility.TryResurrect(pawn, parms))
                {
                    return false;
                }

                CrossedUtility.MarkReanimatedAsCrossed(pawn);
                CrossedUtility.TransformPawn(pawn, true);
                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                NotifyReanimated(pawn);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Failed to reanimate infected corpse: " + ex.Message);
                return false;
            }
        }

        private void RemovePendingReanimationAt(int index)
        {
            pendingReanimationPawns.RemoveAt(index);
            if (index < pendingReanimationTicks.Count)
            {
                pendingReanimationTicks.RemoveAt(index);
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
                {
                    nextRaidTick = ticks;
                }
            }

            if (nextRaidTick <= 0)
            {
                ScheduleNextRaid(ticks);
                return;
            }

            if (ticks < nextRaidTick)
            {
                return;
            }

            TryFireRaidIncident();
            ScheduleNextRaid(ticks);
        }

        private bool TryFireRaidIncident(bool force = false)
        {
            IncidentDef raidDef = CADefOf.CrossedRaid;
            Map map = FindRaidTargetMap();
            Faction crossed = EnsureCrossedFaction();
            if (raidDef == null || map == null || crossed == null)
            {
                return false;
            }

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
            if (!raidScheduleActivated || ticks < TheMarkedMenSettings.FirstMarkedRaidTick || nextRaidTick <= ticks)
            {
                return;
            }

            int ticksUntilRaid = nextRaidTick - ticks;
            int adjustedInterval = CalculateAdjustedRaidIntervalTicks(false);
            if (ticksUntilRaid < adjustedInterval)
            {
                nextRaidTick = ticks + adjustedInterval;
            }
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

            if (ticks < nextHordeTick)
            {
                return;
            }

            if (TryFireHordeIncident())
            {
                ScheduleNextHorde(ticks);
            }
            else
            {
                nextHordeTick = ticks + HordeRetryTicks;
            }
        }

        private bool TryFireHordeIncident(bool force = false)
        {
            IncidentDef hordeDef = CADefOf.CrossedHorde;
            Map map = FindHordeTargetMap();
            Faction crossed = EnsureCrossedFaction();
            if (hordeDef == null || map == null || crossed == null)
            {
                return false;
            }

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
            if (probeDef == null || map == null || crossed == null)
            {
                return false;
            }

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
            if (Find.Maps == null)
            {
                return null;
            }

            Map best = null;
            float bestScore = -1f;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map == null || !map.IsPlayerHome || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
                {
                    continue;
                }

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
            if (parms == null)
            {
                return;
            }

            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = ChooseMarkedRaidArrivalMode(parms);
        }

        private static PawnsArrivalModeDef ChooseMarkedRaidArrivalMode(IncidentParms parms)
        {
            PawnsArrivalModeDef fallback = PawnsArrivalModeDefOf.EdgeWalkInGroups;
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (!TheMarkedMenSettings.RandomizeMarkedRaids && (settings == null || settings.allowGroupedEdgeArrival))
            {
                return fallback;
            }

            List<PawnsArrivalModeDef> candidates = new List<PawnsArrivalModeDef>(4);
            if (settings == null || settings.allowGroupedEdgeArrival)
            {
                AddArrivalCandidate(candidates, PawnsArrivalModeDefOf.EdgeWalkInGroups, parms);
            }
            if (settings == null || settings.allowDistributedGroupArrival)
            {
                AddArrivalCandidate(candidates, PawnsArrivalModeDefOf.EdgeWalkInDistributedGroups, parms);
            }
            if (settings == null || settings.allowDistributedArrival)
            {
                AddArrivalCandidate(candidates, PawnsArrivalModeDefOf.EdgeWalkInDistributed, parms);
            }
            if (settings == null || settings.allowSingleEdgeArrival)
            {
                AddArrivalCandidate(candidates, PawnsArrivalModeDefOf.EdgeWalkIn, parms);
            }

            if (candidates.Count == 0)
            {
                return fallback;
            }

            return candidates[Rand.RangeInclusive(0, candidates.Count - 1)];
        }

        private static void AddArrivalCandidate(List<PawnsArrivalModeDef> candidates, PawnsArrivalModeDef mode, IncidentParms parms)
        {
            if (mode == null || candidates.Contains(mode))
            {
                return;
            }

            if (mode.Worker == null || mode.Worker.CanUseWith(parms))
            {
                candidates.Add(mode);
            }
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
            if (starterLineageInitialized)
            {
                return;
            }

            int marked = 0;
            int handledStarterColonists = 0;
            if (Find.Maps != null)
            {
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    Map map = Find.Maps[i];
                    if (map?.mapPawns == null || !map.IsPlayerHome)
                    {
                        continue;
                    }

                    IReadOnlyList<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
                    for (int j = 0; j < colonists.Count; j++)
                    {
                        if (CrossedUtility.CanSafelyProcessInfectedState(colonists[j]) && !CrossedUtility.IsInfectedPawn(colonists[j]))
                        {
                            handledStarterColonists++;
                        }

                        if (CrossedUtility.TryMarkStarterLineageResistant(colonists[j]))
                        {
                            marked++;
                        }
                    }
                }
            }

            if (handledStarterColonists <= 0)
            {
                return;
            }

            starterLineageInitialized = true;
            if (marked > 0)
            {
                AddIncident("Starter colonists developed marked-virus lineage resistance.");
            }
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
            if (multiplier <= 0.001f)
            {
                return int.MaxValue;
            }

            return Mathf.Max(minimumTicks, Mathf.RoundToInt(intervalTicks / multiplier));
        }

        private static int ApplyRaidRandomization(int intervalTicks, int minimumTicks, bool allowRandomize)
        {
            if (!allowRandomize || !TheMarkedMenSettings.RandomizeMarkedRaids)
            {
                return Mathf.Max(minimumTicks, intervalTicks);
            }

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
            if (Find.Maps == null)
            {
                return;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map?.mapPawns == null)
                {
                    continue;
                }

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

    public sealed class CrossedRaidReport
    {
        public int WaveCount;
        public int InfectedSpawned;
        public int InfectedKilled;
        public int InfectedNeutralized;
        public int ColonistDeaths;
        public int ColonistsDowned;
        public int ColonistCasualties;
        public int SurvivingColonists;
        public int DurationTicks;
        public int RaidsSurvived;
        public int TotalRaidsStarted;
        public float TotalPoints;
        public float NextEscalationMultiplier;
    }

    public sealed class Alert_MarkedMenRaidCountdown : Alert
    {
        private const float ImminentDaysThreshold = 0.05f;

        public Alert_MarkedMenRaidCountdown()
        {
            defaultLabel = "Marked Men raid scheduled";
            defaultExplanation = "A scheduled Marked Men raid is approaching.";
            defaultPriority = AlertPriority.Medium;
        }

        public override AlertPriority Priority
        {
            get
            {
                TheMarkedMenGameComponent component = Current.Game?.GetComponent<TheMarkedMenGameComponent>();
                if (component != null && component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map _) && ticksUntilRaid <= Mathf.RoundToInt(TheMarkedMenSettings.RaidCountdownHighPriorityDays * GenDate.TicksPerDay))
                {
                    return AlertPriority.High;
                }

                return AlertPriority.Medium;
            }
        }

        public override AlertReport GetReport()
        {
            TheMarkedMenGameComponent component = Current.Game?.GetComponent<TheMarkedMenGameComponent>();
            if (!TheMarkedMenSettings.RaidCountdownAlertEnabled || component == null || !component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map targetMap))
            {
                return AlertReport.Inactive;
            }

            if (ticksUntilRaid > Mathf.RoundToInt(TheMarkedMenSettings.RaidCountdownVisibleDays * GenDate.TicksPerDay))
            {
                return AlertReport.Inactive;
            }

            return AlertReport.CulpritIs(new RimWorld.Planet.GlobalTargetInfo(targetMap.Center, targetMap, false));
        }

        public override string GetLabel()
        {
            TheMarkedMenGameComponent component = Current.Game?.GetComponent<TheMarkedMenGameComponent>();
            if (component == null || !component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map _))
            {
                return defaultLabel;
            }

            return "Marked Men raid " + FormatLabelTimeRemaining(ticksUntilRaid);
        }

        public override TaggedString GetExplanation()
        {
            TheMarkedMenGameComponent component = Current.Game?.GetComponent<TheMarkedMenGameComponent>();
            if (component == null || !component.TryGetRaidCountdownForAlert(out int nextTick, out int ticksUntilRaid, out Map targetMap))
            {
                return defaultExplanation;
            }

            int scheduledDay = Mathf.FloorToInt(nextTick / (float)GenDate.TicksPerDay);
            string mapLabel = targetMap?.Parent?.LabelCap ?? targetMap?.ToString() ?? "the colony";
            float estimatedPoints = component.EstimateUpcomingRaidPoints(targetMap);
            return "A scheduled Marked Men raid will begin on day " + scheduledDay + " at " + mapLabel + ".\n\n"
                + "Time remaining: " + FormatPreciseDaysRemaining(ticksUntilRaid) + ".\n"
                + "Estimated threat pressure: " + estimatedPoints.ToString("F0") + " (" + CrossedRaidAlertUtility.DescribeThreatTier(estimatedPoints) + ").\n"
                + "Expected pattern: immediate edge assault in groups; no kidnapping, theft, timeout, or retreat.\n\n"
                + "Prepare sealed fallback positions, medical capacity, fire lanes, and containment for infected blood. Prioritize Alphas and Screamers if they appear.";
        }

        private static string FormatLabelTimeRemaining(int ticksUntilRaid)
        {
            if (ticksUntilRaid <= 0)
            {
                return "imminent";
            }

            float days = ticksUntilRaid / (float)GenDate.TicksPerDay;
            if (days < ImminentDaysThreshold)
            {
                return "in less than 0.1 days";
            }

            int wholeDays = Mathf.CeilToInt(days);
            return "in " + wholeDays + " " + (wholeDays == 1 ? "day" : "days");
        }

        private static string FormatPreciseDaysRemaining(int ticksUntilRaid)
        {
            if (ticksUntilRaid <= 0)
            {
                return "imminent";
            }

            float days = ticksUntilRaid / (float)GenDate.TicksPerDay;
            if (days < ImminentDaysThreshold)
            {
                return "less than 0.1 days";
            }

            string format = days >= 10f ? "0" : "0.0";
            return days.ToString(format) + " " + (Mathf.Abs(days - 1f) < 0.05f ? "day" : "days");
        }
    }

    public sealed class HediffCompProperties_CrossVirus : HediffCompProperties
    {
        public int incubationTicks = 180;
        public int commonTransformationMinTicks = 180;
        public int commonTransformationMaxTicks = 480;
        public float rareSlowProgressionChance = 0.02f;
        public int rareTransformationMinTicks = 600;
        public int rareTransformationMaxTicks = 900;
        public float symptomOnsetFraction = 0.15f;
        public float immunityChance = 0.02f;
        public float terminalTransformationChance = 0.55f;
        public float transformedSeverity = 1f;

        public HediffCompProperties_CrossVirus()
        {
            compClass = typeof(HediffComp_CrossVirus);
        }
    }

    public sealed class HediffComp_CrossVirus : HediffComp
    {
        private const int ProgressTickInterval = 250;
        private const int TerminalOutcomeUnset = -1;
        private const int TerminalOutcomeDeath = 0;
        private const int TerminalOutcomeTransformation = 1;

        private bool transformed;
        private bool incubationResolved;
        private int infectionTick = -1;
        private int transformationTicks = -1;
        private int symptomOnsetTicks = -1;
        private int nextProgressTick;
        private int terminalOutcome = TerminalOutcomeUnset;
        private Pawn originalInfector;

        private HediffCompProperties_CrossVirus Props => (HediffCompProperties_CrossVirus)props;

        public void NotifyInfector(Pawn infector)
        {
            if (infector != null && infector != parent?.pawn)
            {
                originalInfector = infector;
            }
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref transformed, "transformed", false);
            Scribe_Values.Look(ref incubationResolved, "incubationResolved", false);
            Scribe_Values.Look(ref infectionTick, "infectionTick", -1);
            Scribe_Values.Look(ref transformationTicks, "transformationTicks", -1);
            Scribe_Values.Look(ref symptomOnsetTicks, "symptomOnsetTicks", -1);
            Scribe_Values.Look(ref nextProgressTick, "nextProgressTick", 0);
            Scribe_Values.Look(ref terminalOutcome, "terminalOutcome", TerminalOutcomeUnset);
            Scribe_References.Look(ref originalInfector, "originalInfector");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && parent?.pawn != null)
            {
                EnsureProgressionTimers(Find.TickManager?.TicksGame ?? infectionTick);
            }
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            Pawn pawn = parent?.pawn;
            if (CrossedUtility.IsFullyTurnedMarkedPawn(pawn))
            {
                transformed = true;
                incubationResolved = true;
                parent.Severity = Mathf.Max(parent.Severity, Props.transformedSeverity);
                return;
            }

            if (infectionTick < 0)
            {
                infectionTick = Find.TickManager?.TicksGame ?? 0;
            }

            EnsureProgressionTimers(infectionTick);
            CrossedUtility.EnsureInfectedState(pawn);
        }

        public override void CompPostPostRemoved()
        {
            CrossedUtility.RestoreFleeStateIfRecovered(parent?.pawn);
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            Pawn pawn = parent?.pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            int ticks = Find.TickManager?.TicksGame ?? 0;
            if (infectionTick < 0)
            {
                infectionTick = ticks;
            }

            EnsureProgressionTimers(ticks);

            if (CrossedUtility.IsFullyTurnedMarkedPawn(pawn))
            {
                transformed = true;
                incubationResolved = true;
                parent.Severity = Mathf.Max(parent.Severity, Props.transformedSeverity);
                return;
            }

            if (ticks < nextProgressTick)
            {
                return;
            }

            nextProgressTick = ticks + ProgressTickInterval;
            CrossedUtility.EnsureInfectedState(pawn);

            if (transformed)
            {
                return;
            }

            if (parent.Severity >= Props.transformedSeverity)
            {
                ResolveTerminalOutcome(pawn);
                return;
            }

            int elapsed = Mathf.Max(0, ticks - infectionTick);
            float progress = transformationTicks <= 0 ? 1f : Mathf.Clamp01((float)elapsed / transformationTicks);
            parent.Severity = Mathf.Clamp(Mathf.Max(parent.Severity, Mathf.Lerp(InitialSeverityFloor(), Props.transformedSeverity, progress)), 0f, Props.transformedSeverity);

            if (!incubationResolved && elapsed >= symptomOnsetTicks)
            {
                incubationResolved = true;
                float immunityChance = Mathf.Clamp01(TheMarkedMenMod.Settings?.immunitySurvivalChance ?? Props.immunityChance);
                if (Rand.Chance(immunityChance))
                {
                    CrossedUtility.GrantCrossVirusImmunity(pawn);
                    CrossedUtility.Component?.NotifyIncubationSurvived(pawn);
                    pawn.health.RemoveHediff(parent);
                    CrossedUtility.RestoreFleeStateIfRecovered(pawn);
                    return;
                }

                parent.Severity = Mathf.Max(parent.Severity, 0.20f);
                CrossedUtility.Component?.NotifyDiseaseActivated(pawn);
            }

            if (parent.Severity >= Props.transformedSeverity || elapsed >= transformationTicks)
            {
                ResolveTerminalOutcome(pawn);
            }
        }

        private void EnsureProgressionTimers(int ticks)
        {
            if (infectionTick < 0)
            {
                infectionTick = ticks;
            }

            if (transformationTicks <= 0)
            {
                transformationTicks = RandomTransformationTicks();
            }
            else
            {
                transformationTicks = Mathf.Min(transformationTicks, MaxConfiguredTransformationTicks());
            }

            if (symptomOnsetTicks <= 0 || symptomOnsetTicks > transformationTicks)
            {
                int onset = Mathf.RoundToInt(transformationTicks * Mathf.Clamp01(Props.symptomOnsetFraction));
                symptomOnsetTicks = Mathf.Clamp(onset, 1, Mathf.Max(1, transformationTicks));
            }

            if (terminalOutcome == TerminalOutcomeUnset)
            {
                terminalOutcome = Rand.Chance(TheMarkedMenSettings.CurrentTerminalTransformationChance(Props))
                    ? TerminalOutcomeTransformation
                    : TerminalOutcomeDeath;
            }
        }

        private int RandomTransformationTicks()
        {
            bool rareSlowCase = Rand.Chance(Mathf.Clamp01(Props.rareSlowProgressionChance));
            int min = rareSlowCase ? Props.rareTransformationMinTicks : Props.commonTransformationMinTicks;
            int max = rareSlowCase ? Props.rareTransformationMaxTicks : Props.commonTransformationMaxTicks;

            if (min <= 0 && max <= 0)
            {
                min = Props.incubationTicks;
                max = Props.incubationTicks;
            }

            min = Mathf.Max(1, min);
            max = Mathf.Max(min, max);
            return TheMarkedMenSettings.AdjustInfectionTicks(Rand.RangeInclusive(min, max));
        }

        private int MaxConfiguredTransformationTicks()
        {
            int max = Mathf.Max(Props.commonTransformationMaxTicks, Props.rareTransformationMaxTicks);
            if (max <= 0)
            {
                max = Props.incubationTicks;
            }

            return TheMarkedMenSettings.AdjustInfectionTicks(Mathf.Max(1, max));
        }

        private float InitialSeverityFloor()
        {
            float initialSeverity = parent?.def == null ? 0.08f : parent.def.initialSeverity;
            return Mathf.Clamp(initialSeverity, 0f, Props.transformedSeverity);
        }

        private void ResolveTerminalOutcome(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || transformed)
            {
                return;
            }

            transformed = true;
            parent.Severity = Props.transformedSeverity;
            if (terminalOutcome == TerminalOutcomeDeath)
            {
                CrossedUtility.ApplyInfectedTattoo(pawn);
                CrossedUtility.MarkDiedFromMarkedVirus(pawn);
                CrossedUtility.Component?.NotifyVirusDeath(pawn);
                DamageInfo? dinfo = null;
                pawn.Kill(dinfo, parent);
                return;
            }

            CrossedUtility.TransformPawn(pawn, false, originalInfector);
        }
    }

    public static class CrossedUtility
    {
        private const string ReanimatedQuestTag = "CA_ReanimatedAsCrossed";
        private const string MarkedVirusFatalityQuestTag = "CA_MarkedVirusFatalityNoReanimation";
        private const string ArmorStripDueTagPrefix = "CA_CrossedArmorStripDue:";
        private const string FearlessDueToCrossVirusTag = "CA_FearlessDueToCrossVirus";
        private const string StarterLineageResistanceTag = "CA_StarterLineageResistance";
        private const string MarkedVillageFounderTag = "CA_MarkedVillageFounder";
        private const string PersistentCrossedRashTag = "CA_PersistentCrossedRashTattoo";
        private const string MarkedVillageRashRolledTag = "CA_MarkedVillageRashRolled";
        private const float MarkedVillageRashChance = 0.5f;

        private static readonly List<KeyValuePair<PawnKindDef, float>> TransformationKinds = new List<KeyValuePair<PawnKindDef, float>>();

        public static TheMarkedMenGameComponent Component => Current.Game?.GetComponent<TheMarkedMenGameComponent>();

        public static bool IsCrossedPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.health?.hediffSet?.HasHediff(CADefOf.CrossVirus) == true && pawn.health.hediffSet.GetFirstHediffOfDef(CADefOf.CrossVirus)?.Severity >= 0.92f)
            {
                return true;
            }

            FactionDef crossed = CADefOf.CrossedFaction;
            return crossed != null && pawn.Faction?.def == crossed;
        }

        public static bool IsInfectedPawn(Pawn pawn)
        {
            if (pawn == null || pawn.def?.race == null || !pawn.def.race.Humanlike || pawn.health == null || pawn.health.Dead)
            {
                return false;
            }

            HediffDef virus = CADefOf.CrossVirus;
            if (virus != null && pawn.health?.hediffSet?.HasHediff(virus) == true)
            {
                return true;
            }

            FactionDef crossed = CADefOf.CrossedFaction;
            return crossed != null && pawn.Faction?.def == crossed;
        }

        public static bool HasMarkedVirusHediff(Pawn pawn)
        {
            HediffDef virus = CADefOf.CrossVirus;
            return pawn?.health?.hediffSet != null && virus != null && pawn.health.hediffSet.HasHediff(virus);
        }

        public static bool IsFullyTurnedMarkedPawn(Pawn pawn)
        {
            return IsCrossedFactionPawn(pawn);
        }

        public static bool IsPartiallyMarkedPawn(Pawn pawn)
        {
            return HasMarkedVirusHediff(pawn) && !IsFullyTurnedMarkedPawn(pawn);
        }

        public static bool ShouldShowCrossedRash(Pawn pawn)
        {
            return HasMarkedVirusHediff(pawn) || IsCrossedFactionPawn(pawn) || HasPersistentCrossedRashTattoo(pawn);
        }

        public static void EnsureFearlessCrossedState(Pawn pawn)
        {
            if (!TheMarkedMenSettings.MarkedAlwaysAssault || TheMarkedMenSettings.MarkedCanTimeoutOrFlee)
            {
                RestoreFleeStateForMarkedPawn(pawn);
                return;
            }

            if (!IsInfectedPawn(pawn) || pawn.mindState == null)
            {
                return;
            }

            MarkFearlessDueToCrossVirus(pawn);
            Pawn_MindState mindState = pawn.mindState;
            mindState.canFleeIndividual = false;
            mindState.exitMapAfterTick = -1;
            mindState.meleeThreat = null;

            MentalStateHandler handler = mindState.mentalStateHandler;
            if (handler == null)
            {
                return;
            }

            handler.neverFleeIndividual = true;
            if (IsFearOrWithdrawalMentalState(handler.CurStateDef))
            {
                handler.Reset();
            }
        }

        public static void RestoreFleeStateIfRecovered(Pawn pawn)
        {
            if (pawn == null || IsInfectedPawn(pawn) || !RemoveFearlessDueToCrossVirusTag(pawn) || pawn.mindState == null)
            {
                return;
            }

            Pawn_MindState mindState = pawn.mindState;
            mindState.canFleeIndividual = true;
            MentalStateHandler handler = mindState.mentalStateHandler;
            if (handler != null)
            {
                handler.neverFleeIndividual = false;
            }
        }

        private static void RestoreFleeStateForMarkedPawn(Pawn pawn)
        {
            if (pawn?.mindState == null)
            {
                return;
            }

            RemoveFearlessDueToCrossVirusTag(pawn);
            pawn.mindState.canFleeIndividual = true;
            MentalStateHandler handler = pawn.mindState.mentalStateHandler;
            if (handler != null)
            {
                handler.neverFleeIndividual = false;
            }
        }

        private static void MarkFearlessDueToCrossVirus(Pawn pawn)
        {
            if (pawn == null || IsCrossedFactionPawn(pawn))
            {
                return;
            }

            if (pawn.questTags == null)
            {
                pawn.questTags = new List<string>();
            }

            if (!pawn.questTags.Contains(FearlessDueToCrossVirusTag))
            {
                pawn.questTags.Add(FearlessDueToCrossVirusTag);
            }
        }

        private static bool RemoveFearlessDueToCrossVirusTag(Pawn pawn)
        {
            List<string> tags = pawn?.questTags;
            if (tags == null)
            {
                return false;
            }

            return tags.Remove(FearlessDueToCrossVirusTag);
        }

        private static bool IsFearOrWithdrawalMentalState(MentalStateDef def)
        {
            if (def == null)
            {
                return false;
            }

            return def == MentalStateDefOf.PanicFlee
                || def == MentalStateDefOf.PanicFleeFire
                || def == MentalStateDefOf.Terror
                || def == MentalStateDefOf.Wander_Psychotic
                || def == MentalStateDefOf.Wander_Sad
                || def == MentalStateDefOf.Wander_OwnRoom
                || def == MentalStateDefOf.Roaming;
        }

        public static bool ShouldReanimateAsCrossed(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.RaceProps == null || !pawn.RaceProps.Humanlike || WasReanimatedAsCrossed(pawn) || DiedFromMarkedVirusWithoutReanimation(pawn))
            {
                return false;
            }

            HediffDef virus = CADefOf.CrossVirus;
            if (virus != null && pawn.health?.hediffSet?.HasHediff(virus) == true)
            {
                return true;
            }

            FactionDef crossed = CADefOf.CrossedFaction;
            return crossed != null && pawn.Faction?.def == crossed;
        }

        public static void MarkReanimatedAsCrossed(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.questTags == null)
            {
                pawn.questTags = new List<string>();
            }

            if (!pawn.questTags.Contains(ReanimatedQuestTag))
            {
                pawn.questTags.Add(ReanimatedQuestTag);
            }
        }

        public static void MarkDiedFromMarkedVirus(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.questTags == null)
            {
                pawn.questTags = new List<string>();
            }

            if (!pawn.questTags.Contains(MarkedVirusFatalityQuestTag))
            {
                pawn.questTags.Add(MarkedVirusFatalityQuestTag);
            }
        }

        private static bool WasReanimatedAsCrossed(Pawn pawn)
        {
            return pawn?.questTags != null && pawn.questTags.Contains(ReanimatedQuestTag);
        }

        private static bool DiedFromMarkedVirusWithoutReanimation(Pawn pawn)
        {
            return pawn?.questTags != null && pawn.questTags.Contains(MarkedVirusFatalityQuestTag);
        }

        public static bool HasCrossVirusImmunity(Pawn pawn)
        {
            HediffDef immunity = CADefOf.CrossVirusImmunity;
            return pawn?.health?.hediffSet != null && immunity != null && pawn.health.hediffSet.HasHediff(immunity);
        }

        public static bool IsFullyProtectedFromCrossVirusExposure(Pawn pawn)
        {
            return HasMarkedVillageFounderImmunity(pawn) || HasCrossVirusImmunity(pawn) && !HasStarterLineageResistance(pawn);
        }

        public static void GrantCrossVirusImmunity(Pawn pawn)
        {
            HediffDef immunity = CADefOf.CrossVirusImmunity;
            if (pawn?.health == null || immunity == null || pawn.health.hediffSet.HasHediff(immunity))
            {
                return;
            }

            pawn.health.AddHediff(immunity);
        }

        private static void RemoveCrossVirusImmunity(Pawn pawn)
        {
            HediffDef immunity = CADefOf.CrossVirusImmunity;
            Hediff existing = immunity == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(immunity);
            if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        public static bool HasStarterLineageResistance(Pawn pawn)
        {
            return pawn?.questTags != null && pawn.questTags.Contains(StarterLineageResistanceTag);
        }

        public static bool HasMarkedVillageFounderImmunity(Pawn pawn)
        {
            return pawn?.questTags != null && pawn.questTags.Contains(MarkedVillageFounderTag);
        }

        public static bool HasPersistentCrossedRashTattoo(Pawn pawn)
        {
            return pawn?.questTags != null && pawn.questTags.Contains(PersistentCrossedRashTag);
        }

        public static bool GrantMarkedVillageFounderState(Pawn pawn)
        {
            if (!CanReceiveMarkedVillageFounderState(pawn))
            {
                return false;
            }

            if (pawn.questTags == null)
            {
                pawn.questTags = new List<string>();
            }

            bool changed = AddQuestTagIfMissing(pawn, MarkedVillageFounderTag);
            if (!pawn.questTags.Contains(MarkedVillageRashRolledTag))
            {
                changed |= AddQuestTagIfMissing(pawn, MarkedVillageRashRolledTag);
                if (HasPersistentCrossedRashTattoo(pawn) || Rand.Chance(MarkedVillageRashChance))
                {
                    changed |= AddQuestTagIfMissing(pawn, PersistentCrossedRashTag);
                }
            }

            GrantCrossVirusImmunity(pawn);
            if (HasPersistentCrossedRashTattoo(pawn))
            {
                ApplyInfectedTattoo(pawn);
            }
            else
            {
                RemoveCrossedRashVisualsIfNotSuccumbed(pawn);
            }
            return changed;
        }

        public static bool TryMarkStarterLineageResistant(Pawn pawn)
        {
            if (!CanReceiveStarterLineageResistance(pawn))
            {
                return false;
            }

            if (pawn.questTags == null)
            {
                pawn.questTags = new List<string>();
            }

            bool added = !pawn.questTags.Contains(StarterLineageResistanceTag);
            if (added)
            {
                pawn.questTags.Add(StarterLineageResistanceTag);
            }

            GrantCrossVirusImmunity(pawn);
            return added;
        }

        public static void EnsureStarterLineageResistance(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.Humanlike || IsInfectedPawn(pawn))
            {
                return;
            }

            if (HasStarterLineageResistance(pawn))
            {
                GrantCrossVirusImmunity(pawn);
                return;
            }

            if (HasStarterLineageParent(pawn))
            {
                TryMarkStarterLineageResistant(pawn);
            }
        }

        private static bool CanReceiveStarterLineageResistance(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && pawn.RaceProps != null
                && pawn.RaceProps.Humanlike
                && !HasMarkedVillageFounderImmunity(pawn)
                && !IsInfectedPawn(pawn)
                && !IsCrossedPawn(pawn);
        }

        private static bool CanReceiveMarkedVillageFounderState(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && pawn.RaceProps != null
                && pawn.RaceProps.Humanlike
                && !IsInfectedPawn(pawn)
                && !IsCrossedPawn(pawn);
        }

        private static bool AddQuestTagIfMissing(Pawn pawn, string tag)
        {
            if (pawn?.questTags == null || pawn.questTags.Contains(tag))
            {
                return false;
            }

            pawn.questTags.Add(tag);
            return true;
        }

        private static bool HasStarterLineageParent(Pawn pawn)
        {
            List<DirectPawnRelation> relations = pawn?.relations?.DirectRelations;
            if (relations == null)
            {
                return false;
            }

            for (int i = 0; i < relations.Count; i++)
            {
                DirectPawnRelation relation = relations[i];
                if (relation?.otherPawn != null && IsParentRelation(relation.def) && HasStarterLineageResistance(relation.otherPawn))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsParentRelation(PawnRelationDef relationDef)
        {
            if (relationDef == null)
            {
                return false;
            }

            return relationDef == PawnRelationDefOf.Parent
                || string.Equals(relationDef.defName, "ParentBirth", StringComparison.Ordinal);
        }

        private static void NotifyInfectionRetarget(Pawn infected, Pawn infector)
        {
            if (infected?.Map == null)
            {
                return;
            }

            if (infector != null && infector.Spawned && infector.Map == infected.Map && IsInfectedPawn(infector))
            {
                CrossedTacticalAI.TryRetargetAwayFromPawn(infector, infected, true);
            }

            IReadOnlyList<Pawn> pawns = infected.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && pawn != infector && IsInfectedPawn(pawn))
                {
                    CrossedTacticalAI.TryRetargetAwayFromPawn(pawn, infected, false);
                }
            }
        }

        public static bool TryExpose(Pawn pawn, float chance, string source, Pawn infector = null)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings != null && !settings.infectionEnabled)
            {
                return false;
            }

            HediffDef virus = CADefOf.CrossVirus;
            if (pawn == null || virus == null || pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.Humanlike || IsCrossedPawn(pawn))
            {
                return false;
            }

            EnsureStarterLineageResistance(pawn);
            if (HasMarkedVillageFounderImmunity(pawn))
            {
                GrantCrossVirusImmunity(pawn);
                ApplyInfectedTattoo(pawn);
                return false;
            }

            bool starterLineageBreakthrough = HasCrossVirusImmunity(pawn) && HasStarterLineageResistance(pawn);
            if (HasCrossVirusImmunity(pawn) && !starterLineageBreakthrough)
            {
                return false;
            }

            float effectiveChance = Mathf.Clamp01(chance);
            if (starterLineageBreakthrough)
            {
                effectiveChance *= TheMarkedMenSettings.StarterLineageBreakthroughChance;
            }

            if (!Rand.Chance(effectiveChance))
            {
                return false;
            }

            if (starterLineageBreakthrough)
            {
                RemoveCrossVirusImmunity(pawn);
            }

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(virus);
            bool newlyInfected = existing == null;
            if (newlyInfected)
            {
                existing = pawn.health.AddHediff(virus);
                existing.Severity = Mathf.Max(existing.Severity, InitialCrossVirusSeverity(virus));
            }
            else
            {
                existing.Severity = Mathf.Max(existing.Severity, InitialCrossVirusSeverity(virus));
            }

            HediffComp_CrossVirus comp = existing.TryGetComp<HediffComp_CrossVirus>();
            comp?.NotifyInfector(infector);
            EnsureInfectedState(pawn);
            if (newlyInfected)
            {
                Component?.NotifyExposure(pawn, source);
            }

            return true;
        }

        public static void TransformPawn(Pawn pawn, bool suppressNotification = false, Pawn infector = null)
        {
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            Faction faction = Component?.EnsureCrossedFaction();
            PawnKindDef kind = PickTransformationKind(pawn);
            if (kind != null && pawn.kindDef != kind)
            {
                pawn.ChangeKind(kind);
            }

            if (faction != null && pawn.Faction != faction)
            {
                pawn.SetFaction(faction, null);
            }

            pawn.guest?.SetGuestStatus(null);
            pawn.mindState?.mentalStateHandler?.Reset();
            EnsureFearlessCrossedState(pawn);
            ApplyClassHediffs(pawn);
            EnsureCrossedBasicClothingOnly(pawn);
            if (pawn.Drawer?.renderer != null)
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }

            ApplyInfectedTattoo(pawn);
            if (suppressNotification)
            {
                return;
            }

            Component?.NotifyTransformation(pawn);
        }

        public static void ApplyGeneratedRaidKindTuning(List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            int maxAlphas = Mathf.Clamp(settings?.maximumAlphasPerRaid ?? 99, 0, 99);
            bool allowChildren = settings?.allowMarkedChildren == true;
            int alphaCount = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead)
                {
                    continue;
                }

                bool replace = false;
                if (pawn.kindDef == CADefOf.Alpha)
                {
                    alphaCount++;
                    replace = alphaCount > maxAlphas || TheMarkedMenSettings.AdjustKindWeight(CADefOf.Alpha, 1f) <= 0f;
                }
                else if (pawn.kindDef == CADefOf.Child)
                {
                    replace = !allowChildren;
                }

                if (replace)
                {
                    PawnKindDef replacement = PickReplacementMarkedKind();
                    if (replacement != null && replacement != pawn.kindDef)
                    {
                        pawn.ChangeKind(replacement);
                        RemoveClassHediffs(pawn);
                        ApplyClassHediffs(pawn);
                        ApplyInfectedTattoo(pawn);
                    }
                }
            }
        }

        private static PawnKindDef PickReplacementMarkedKind()
        {
            PawnKindDef selected = null;
            float totalWeight = 0f;
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Berserker, 12f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Hunter, 8f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Stalker, 4f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Screamer, 3f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Brute, 2f);
            return selected ?? CADefOf.Berserker ?? CADefOf.Hunter ?? CADefOf.Stalker ?? CADefOf.Screamer ?? CADefOf.Brute;
        }

        private static void AddReplacementKind(ref PawnKindDef selected, ref float totalWeight, PawnKindDef kind, float baseWeight)
        {
            float weight = TheMarkedMenSettings.AdjustKindWeight(kind, baseWeight);
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

        private static void RemoveClassHediffs(Pawn pawn)
        {
            RemoveHediffIfPresent(pawn, CADefOf.BloodRush);
            RemoveHediffIfPresent(pawn, CADefOf.CommandAura);
        }

        private static void RemoveHediffIfPresent(Pawn pawn, HediffDef def)
        {
            Hediff existing = def == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(def);
            if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        public static void ApplyClassHediffs(Pawn pawn)
        {
            if (pawn == null || pawn.health == null)
            {
                return;
            }

            RemoveDeprecatedCrossedRashHediff(pawn);
            if (!ShouldShowCrossedRash(pawn))
            {
                RemoveCrossedRashVisualsIfNotSuccumbed(pawn);
                return;
            }

            EnsureFearlessCrossedState(pawn);
            if (!IsCrossedFactionPawn(pawn))
            {
                HediffDef virus = CADefOf.CrossVirus;
                if (virus != null)
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(virus) ?? pawn.health.AddHediff(virus);
                    hediff.Severity = 1f;
                }
            }

            if (pawn.kindDef == CADefOf.Berserker && CADefOf.BloodRush != null && !pawn.health.hediffSet.HasHediff(CADefOf.BloodRush))
            {
                pawn.health.AddHediff(CADefOf.BloodRush);
            }
            else if (pawn.kindDef == CADefOf.Alpha && CADefOf.CommandAura != null && !pawn.health.hediffSet.HasHediff(CADefOf.CommandAura))
            {
                pawn.health.AddHediff(CADefOf.CommandAura);
            }

            ApplyInfectedTattoo(pawn);
            EnsureCrossedBasicClothingOnly(pawn);
        }

        public static void RemoveMarkedVirusHediffFromFullyTurnedPawn(Pawn pawn)
        {
            HediffDef virus = IsCrossedFactionPawn(pawn) ? CADefOf.CrossVirus : null;
            Hediff existing = virus == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(virus);
            if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        public static void ApplyInfectedTattoo(Pawn pawn)
        {
            TattooDef tattoo = CADefOf.CrossedFaceTattoo;
            if (pawn?.style == null || tattoo == null || !ShouldShowCrossedRash(pawn))
            {
                return;
            }

            if (pawn.style.nextFaceTattooDef != tattoo)
            {
                pawn.style.nextFaceTattooDef = tattoo;
            }

            if (pawn.style.FaceTattoo != tattoo)
            {
                pawn.style.FaceTattoo = tattoo;
                pawn.style.Notify_StyleItemChanged();
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        public static void ApplyInfectedTattooIfInfected(Pawn pawn)
        {
            EnsureInfectedState(pawn);
        }

        public static void EnsureInfectedState(Pawn pawn)
        {
            if (!CanSafelyProcessInfectedState(pawn))
            {
                return;
            }

            RemoveDeprecatedCrossedRashHediff(pawn);
            if (IsCrossedFactionPawn(pawn))
            {
                ApplyClassHediffs(pawn);
                return;
            }

            HediffDef virus = CADefOf.CrossVirus;
            Hediff hediff = virus == null ? null : pawn.health?.hediffSet?.GetFirstHediffOfDef(virus);
            if (hediff == null)
            {
                RestoreFleeStateIfRecovered(pawn);
                RemoveCrossedRashVisualsIfNotSuccumbed(pawn);
                return;
            }

            hediff.Severity = Mathf.Max(hediff.Severity, InitialCrossVirusSeverity(virus));
            EnsureFearlessCrossedState(pawn);
            ApplyInfectedTattoo(pawn);
        }

        public static bool CanSafelyProcessInfectedState(Pawn pawn)
        {
            return pawn != null
                && !pawn.Destroyed
                && pawn.def?.race != null
                && pawn.def.race.Humanlike
                && pawn.health?.hediffSet != null
                && !pawn.health.Dead;
        }

        private static float InitialCrossVirusSeverity(HediffDef virus)
        {
            return Mathf.Clamp(virus?.initialSeverity ?? 0.08f, 0.001f, 1f);
        }

        public static void RemoveDeprecatedCrossedRashHediff(Pawn pawn)
        {
            HediffDef rash = CADefOf.CrossedRash;
            Hediff existingRash = rash == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(rash);
            if (existingRash != null)
            {
                pawn.health.RemoveHediff(existingRash);
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        public static void RemoveCrossedRashVisualsIfNotSuccumbed(Pawn pawn)
        {
            if (pawn == null || ShouldShowCrossedRash(pawn))
            {
                return;
            }

            bool changed = false;
            TattooDef crossedTattoo = CADefOf.CrossedFaceTattoo;
            if (pawn.style != null && crossedTattoo != null)
            {
                TattooDef noFaceTattoo = TattooDefOf.NoTattoo_Face;
                if (pawn.style.nextFaceTattooDef == crossedTattoo)
                {
                    pawn.style.nextFaceTattooDef = noFaceTattoo;
                    changed = true;
                }

                if (pawn.style.FaceTattoo == crossedTattoo)
                {
                    pawn.style.FaceTattoo = noFaceTattoo;
                    pawn.style.Notify_StyleItemChanged();
                    changed = true;
                }
            }

            if (changed)
            {
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        public static void EnsureCrossedBasicClothingOnly(Pawn pawn)
        {
            ClearArmorStripDueTick(pawn);
        }

        private static void ClearArmorStripDueTick(Pawn pawn)
        {
            List<string> tags = pawn?.questTags;
            if (tags == null)
            {
                return;
            }

            for (int i = tags.Count - 1; i >= 0; i--)
            {
                string tag = tags[i];
                if (!tag.NullOrEmpty() && tag.StartsWith(ArmorStripDueTagPrefix, StringComparison.Ordinal))
                {
                    tags.RemoveAt(i);
                }
            }
        }

        private static bool IsCrossedFactionPawn(Pawn pawn)
        {
            FactionDef crossed = CADefOf.CrossedFaction;
            return pawn != null && crossed != null && pawn.Faction?.def == crossed;
        }

        public static void ApplyScreamerPanic(Map map, IntVec3 origin, float radius)
        {
            HediffDef panic = CADefOf.Panic;
            if (map == null || panic == null)
            {
                return;
            }

            float effectiveRadius = Mathf.Max(0f, radius * Mathf.Sqrt(TheMarkedMenSettings.SocialTerrorStrength));
            if (effectiveRadius <= 0f)
            {
                return;
            }

            foreach (Pawn pawn in map.mapPawns.FreeColonistsAndPrisonersSpawned)
            {
                if (pawn.Position.InHorDistOf(origin, effectiveRadius) && !pawn.health.hediffSet.HasHediff(panic))
                {
                    pawn.health.AddHediff(panic);
                }
            }
        }

        private static PawnKindDef PickTransformationKind(Pawn pawn)
        {
            TransformationKinds.Clear();
            AddKind(CADefOf.Berserker, 1f);
            AddKind(CADefOf.Hunter, 1f);
            AddKind(CADefOf.Stalker, 1f);
            AddKind(CADefOf.Screamer, 1f);
            if (Rand.Chance(0.12f))
            {
                AddKind(CADefOf.Brute, 1f);
            }
            if (Rand.Chance(0.02f))
            {
                AddKind(CADefOf.Alpha, 1f);
            }
            if (TheMarkedMenMod.Settings?.allowMarkedChildren == true && pawn?.ageTracker != null && pawn.ageTracker.AgeBiologicalYearsFloat < 13f)
            {
                AddKind(CADefOf.Child, 1f);
            }

            if (TransformationKinds.Count == 0)
            {
                return pawn.kindDef;
            }

            return PickWeightedKind(TransformationKinds) ?? pawn.kindDef;
        }

        private static void AddKind(PawnKindDef kind, float baseWeight)
        {
            float weight = TheMarkedMenSettings.AdjustKindWeight(kind, baseWeight);
            if (kind != null && weight > 0f)
            {
                TransformationKinds.Add(new KeyValuePair<PawnKindDef, float>(kind, weight));
            }
        }

        private static PawnKindDef PickWeightedKind(List<KeyValuePair<PawnKindDef, float>> kinds)
        {
            float totalWeight = 0f;
            for (int i = 0; i < kinds.Count; i++)
            {
                totalWeight += Mathf.Max(0f, kinds[i].Value);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float pick = Rand.Value * totalWeight;
            for (int i = 0; i < kinds.Count; i++)
            {
                pick -= Mathf.Max(0f, kinds[i].Value);
                if (pick <= 0f)
                {
                    return kinds[i].Key;
                }
            }

            return kinds[kinds.Count - 1].Key;
        }
    }

    public static class CrossedTacticalAI
    {
        private const int TacticalRetargetInterval = 6;
        private const int TacticalJobExpiryTicks = 90;
        private const int TacticalMoveExpiryTicks = 180;
        private const int RangedCastSearchMaxRegions = 80;
        private const float MaxTacticalTargetDistance = 120f;
        private const float MaxTacticalTargetDistanceSquared = MaxTacticalTargetDistance * MaxTacticalTargetDistance;
        private const float AggressionScoreMultiplier = 100f;
        private const float PartialInfectionTargetBonus = 10000f;
        private const int InfightingCheckInterval = 1000;
        private const float InfightingChance = 0.12f;
        private const float MaxInfightingTargetDistanceSquared = 2500f;
        private const string WaitDownedJobDefName = "Wait_Downed";

        public static bool TryIssueTacticalJob(Pawn pawn)
        {
            if (!CanUseTacticalAI(pawn))
            {
                return false;
            }

            CrossedUtility.EnsureFearlessCrossedState(pawn);
            if (TheMarkedMenRjwCompatibility.TryStartBestInfectedIntercourseJob(pawn, true))
            {
                return true;
            }

            JobDef currentJobDef = pawn.CurJob?.def;
            Pawn currentPawnTarget = pawn.CurJob?.targetA.Pawn;
            if (currentPawnTarget != null && CrossedUtility.IsFullyTurnedMarkedPawn(currentPawnTarget) && !IsAttackJob(currentJobDef))
            {
                return TryRetargetAwayFromPawn(pawn, currentPawnTarget, true);
            }

            if (IsAttackJob(currentJobDef) && IsValidNonInfectedPawnTarget(currentPawnTarget, pawn))
            {
                return false;
            }

            if (IsTacticalRangedMoveJob(pawn.CurJob, currentPawnTarget) && IsValidNonInfectedPawnTarget(currentPawnTarget, pawn))
            {
                return false;
            }

            if (TryStartInfighting(pawn, currentJobDef, currentPawnTarget))
            {
                return true;
            }

            Pawn bestNonInfected = FindBestNonInfectedPawnTarget(pawn);
            if (bestNonInfected != null)
            {
                bool isAttackJob = IsAttackJob(currentJobDef);
                if (isAttackJob && currentPawnTarget == bestNonInfected && IsValidNonInfectedPawnTarget(currentPawnTarget, pawn))
                {
                    return false;
                }

                if (IsTacticalRangedMoveJob(pawn.CurJob, bestNonInfected) && IsValidNonInfectedPawnTarget(bestNonInfected, pawn))
                {
                    return false;
                }

                return TryAssignAttackJob(pawn, bestNonInfected, true);
            }

            if (!pawn.IsHashIntervalTick(TheMarkedMenSettings.TacticalRetargetIntervalTicks) || IsAttackJob(currentJobDef))
            {
                return false;
            }

            if (!TheMarkedMenSettings.PriorityTargetingEnabled && !TheMarkedMenSettings.DoorTargetingEnabled)
            {
                return false;
            }

            Thing target = FindPriorityTarget(pawn);
            if (target == null)
            {
                return false;
            }

            return TryAssignAttackJob(pawn, target);
        }

        public static bool TryAttackNearestNonInfectedPawn(Pawn pawn, bool forceCurrentJob)
        {
            if (!CanUseTacticalAI(pawn))
            {
                return false;
            }

            CrossedUtility.EnsureFearlessCrossedState(pawn);
            if (TheMarkedMenRjwCompatibility.TryStartBestInfectedIntercourseJob(pawn, forceCurrentJob))
            {
                return true;
            }

            Pawn target = FindBestNonInfectedPawnTarget(pawn);
            if (target == null)
            {
                return false;
            }

            JobDef currentJobDef = pawn.CurJob?.def;
            if (!forceCurrentJob && IsAttackJob(currentJobDef) && pawn.CurJob?.targetA.Pawn == target)
            {
                return false;
            }

            if (!forceCurrentJob && IsTacticalRangedMoveJob(pawn.CurJob, target))
            {
                return false;
            }

            bool shouldForceCurrentJob = forceCurrentJob || !IsAttackJob(currentJobDef) || pawn.CurJob?.targetA.Pawn != target;
            return TryAssignAttackJob(pawn, target, shouldForceCurrentJob);
        }

        public static bool TryRetargetAwayFromPawn(Pawn pawn, Pawn invalidTarget, bool forceEndCurrentJob)
        {
            if (!CanUseTacticalAI(pawn) || invalidTarget == null || !CrossedUtility.IsFullyTurnedMarkedPawn(invalidTarget))
            {
                return false;
            }

            bool currentJobTargetsInvalidPawn = pawn.CurJob?.targetA.Pawn == invalidTarget || pawn.CurJob?.targetB.Pawn == invalidTarget || pawn.CurJob?.targetC.Pawn == invalidTarget;
            if (!forceEndCurrentJob && !currentJobTargetsInvalidPawn)
            {
                return false;
            }

            if (currentJobTargetsInvalidPawn && pawn.jobs?.curJob != null)
            {
                if (!CanSafelyInterruptCurrentJob(pawn))
                {
                    return false;
                }

                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false, true);
            }

            Pawn nearest = FindBestNonInfectedPawnTarget(pawn);
            return nearest != null && TryAssignAttackJob(pawn, nearest, true);
        }

        private static bool TryStartInfighting(Pawn pawn, JobDef currentJobDef, Pawn currentPawnTarget)
        {
            if (!pawn.IsHashIntervalTick(TheMarkedMenSettings.InfightingCheckIntervalTicks) || !Rand.Chance(TheMarkedMenSettings.InfightingChance))
            {
                return false;
            }

            if (IsAttackJob(currentJobDef) && IsValidInfightingTarget(currentPawnTarget, pawn))
            {
                return false;
            }

            Pawn target = FindBestInfightingTarget(pawn);
            return target != null && TryAssignAttackJob(pawn, target, true);
        }

        private static bool TryAssignAttackJob(Pawn pawn, Thing target, bool forceCurrentJob = false)
        {
            if (pawn?.jobs == null || target == null || target.Destroyed)
            {
                return false;
            }

            Verb rangedVerb = GetRangedVerb(pawn);
            if (rangedVerb != null)
            {
                return TryAssignRangedAttackJob(pawn, target, rangedVerb, forceCurrentJob);
            }

            if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly, true, true))
            {
                return false;
            }

            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.expiryInterval = TacticalJobExpiryTicks;
            job.checkOverrideOnExpire = true;
            job.killIncappedTarget = !(target is Pawn attackPawnTarget && TheMarkedMenRjwCompatibility.ShouldKeepIncapacitatedTargetForIntercourse(pawn, attackPawnTarget));
            job.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
            job.canBashFences = true;
            job.attackDoorIfTargetLost = TheMarkedMenSettings.DoorTargetingEnabled;
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            return TryTakeTacticalJob(pawn, job, forceCurrentJob);
        }

        private static bool TryAssignRangedAttackJob(Pawn pawn, Thing target, Verb verb, bool forceCurrentJob)
        {
            if (verb.CanHitTargetFrom(pawn.Position, target))
            {
                Job attackJob = JobMaker.MakeJob(JobDefOf.AttackStatic, target);
                attackJob.expiryInterval = TacticalJobExpiryTicks;
                attackJob.checkOverrideOnExpire = true;
                attackJob.killIncappedTarget = !(target is Pawn attackPawnTarget && TheMarkedMenRjwCompatibility.ShouldKeepIncapacitatedTargetForIntercourse(pawn, attackPawnTarget));
                attackJob.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
                attackJob.canBashFences = true;
                attackJob.attackDoorIfTargetLost = TheMarkedMenSettings.DoorTargetingEnabled;
                attackJob.locomotionUrgency = LocomotionUrgency.Sprint;
                return TryTakeTacticalJob(pawn, attackJob, forceCurrentJob);
            }

            if (!TryFindRangedCastPosition(pawn, target, verb, out IntVec3 castPosition))
            {
                return false;
            }

            Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, castPosition, target);
            moveJob.expiryInterval = TacticalMoveExpiryTicks;
            moveJob.checkOverrideOnExpire = true;
            moveJob.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
            moveJob.canBashFences = true;
            moveJob.attackDoorIfTargetLost = TheMarkedMenSettings.DoorTargetingEnabled;
            moveJob.locomotionUrgency = LocomotionUrgency.Sprint;
            return TryTakeTacticalJob(pawn, moveJob, forceCurrentJob);
        }

        private static bool TryTakeTacticalJob(Pawn pawn, Job job, bool forceCurrentJob)
        {
            if (pawn?.jobs == null || job == null)
            {
                return false;
            }

            if (forceCurrentJob && pawn.jobs.curJob != null)
            {
                if (!CanSafelyInterruptCurrentJob(pawn))
                {
                    return false;
                }

                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false, true);
            }

            return pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
        }

        private static bool TryFindRangedCastPosition(Pawn pawn, Thing target, Verb verb, out IntVec3 castPosition)
        {
            castPosition = IntVec3.Invalid;
            if (pawn?.Map == null || target == null || target.Destroyed || verb == null)
            {
                return false;
            }

            CastPositionRequest request = new CastPositionRequest
            {
                caster = pawn,
                target = target,
                verb = verb,
                maxRangeFromCaster = MaxTacticalTargetDistance,
                maxRangeFromTarget = Mathf.Max(verb.EffectiveRange, 1f),
                wantCoverFromTarget = false,
                maxRegions = RangedCastSearchMaxRegions,
                validator = cell => IsValidRangedCastPosition(pawn, target, verb, cell)
            };

            return CastPositionFinder.TryFindCastPosition(request, out castPosition) && castPosition.IsValid;
        }

        private static bool IsValidRangedCastPosition(Pawn pawn, Thing target, Verb verb, IntVec3 cell)
        {
            Map map = pawn?.Map;
            return map != null
                && target != null
                && !target.Destroyed
                && cell.IsValid
                && cell != pawn.Position
                && cell.InBounds(map)
                && !cell.Fogged(map)
                && cell.Standable(map)
                && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly, true, true)
                && verb.CanHitTargetFrom(cell, target);
        }

        private static Verb GetRangedVerb(Pawn pawn)
        {
            Verb verb = pawn?.equipment?.PrimaryEq?.PrimaryVerb;
            return verb != null && !verb.IsMeleeAttack ? verb : null;
        }

        private static bool IsTacticalRangedMoveJob(Job job, Thing target)
        {
            return job?.def == JobDefOf.Goto
                && target != null
                && job.targetB.Thing == target;
        }

        private static Pawn FindBestNonInfectedPawnTarget(Pawn pawn)
        {
            IReadOnlyList<Pawn> candidates = pawn.Map?.mapPawns?.AllPawnsSpawned;
            if (candidates == null)
            {
                return null;
            }

            Pawn best = null;
            float bestScore = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn candidate = candidates[i];
                float score = ScorePawnTarget(pawn, candidate);
                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static Pawn FindBestInfightingTarget(Pawn pawn)
        {
            IReadOnlyList<Pawn> candidates = pawn.Map?.mapPawns?.AllPawnsSpawned;
            if (candidates == null)
            {
                return null;
            }

            Pawn best = null;
            float bestScore = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn candidate = candidates[i];
                float score = ScoreInfightingTarget(pawn, candidate);
                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static Thing FindPriorityTarget(Pawn pawn)
        {
            Map map = pawn.Map;
            Thing best = null;
            float bestScore = 0f;

            IReadOnlyList<Pawn> vulnerablePawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < vulnerablePawns.Count; i++)
            {
                Pawn candidate = vulnerablePawns[i];
                float score = ScorePawnTarget(pawn, candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building candidate = buildings[i];
                float score = ScoreBuildingTarget(pawn, candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static float ScorePawnTarget(Pawn searcher, Pawn target)
        {
            if (!IsValidNonInfectedPawnTarget(target, searcher))
            {
                return 0f;
            }

            float distanceSquared = searcher.Position.DistanceToSquared(target.Position);
            if (distanceSquared > MaxTacticalTargetDistanceSquared)
            {
                return 0f;
            }

            float score = 95f;
            if (CrossedUtility.IsPartiallyMarkedPawn(target))
            {
                score += PartialInfectionTargetBonus;
            }

            if (target.Downed)
            {
                score += 90f;
            }

            if (target.health?.hediffSet != null)
            {
                score += Mathf.Clamp(target.health.hediffSet.PainTotal * 75f, 0f, 65f);
                score += Mathf.Clamp(target.health.hediffSet.BleedRateTotal * 25f, 0f, 55f);
            }

            if (target.health?.capacities != null)
            {
                float moving = target.health.capacities.GetLevel(PawnCapacityDefOf.Moving);
                if (moving < 0.65f)
                {
                    score += Mathf.Lerp(60f, 0f, Mathf.Clamp01(moving / 0.65f));
                }
            }

            SkillRecord medicine = target.skills?.GetSkill(SkillDefOf.Medicine);
            if (TheMarkedMenSettings.PriorityTargetingEnabled && medicine != null && medicine.Level >= 8)
            {
                score += 45f;
            }

            if (IsIsolatedTarget(searcher, target))
            {
                score += 35f;
            }

            if (target.Faction == Faction.OfPlayer || target.HostFaction == Faction.OfPlayer || target.IsColonistPlayerControlled)
            {
                score += 20f;
            }

            return score * AggressionScoreMultiplier - Mathf.Sqrt(distanceSquared) * 1.15f;
        }

        private static bool IsIsolatedTarget(Pawn searcher, Pawn target)
        {
            IReadOnlyList<Pawn> pawns = searcher.Map?.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return false;
            }

            const float allyRadiusSquared = 64f;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn other = pawns[i];
                if (other == null || other == target || other.Dead || other.Downed || other.Map != target.Map || CrossedUtility.IsInfectedPawn(other))
                {
                    continue;
                }

                if (other.RaceProps == null || !other.RaceProps.Humanlike)
                {
                    continue;
                }

                bool allied = other.Faction == target.Faction || other.HostFaction == target.Faction || target.HostFaction == other.Faction;
                if (allied && other.Position.DistanceToSquared(target.Position) <= allyRadiusSquared)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidNonInfectedPawnTarget(Pawn target, Pawn searcher)
        {
            return target != null
                && target != searcher
                && target.Spawned
                && !target.Dead
                && target.RaceProps != null
                && target.RaceProps.Humanlike
                && !CrossedUtility.IsFullyTurnedMarkedPawn(target);
        }

        private static float ScoreInfightingTarget(Pawn searcher, Pawn target)
        {
            if (!IsValidInfightingTarget(target, searcher))
            {
                return 0f;
            }

            float distanceSquared = searcher.Position.DistanceToSquared(target.Position);
            if (distanceSquared > MaxInfightingTargetDistanceSquared)
            {
                return 0f;
            }

            float score = 200f;
            if (CrossedUtility.IsPartiallyMarkedPawn(target))
            {
                score += 400f;
            }

            if (target.health?.hediffSet != null)
            {
                score += Mathf.Clamp(target.health.hediffSet.PainTotal * 45f, 0f, 40f);
                score += Mathf.Clamp(target.health.hediffSet.BleedRateTotal * 20f, 0f, 35f);
            }

            return score - Mathf.Sqrt(distanceSquared) * 1.5f;
        }

        private static bool IsValidInfightingTarget(Pawn target, Pawn searcher)
        {
            return target != null
                && target != searcher
                && target.Spawned
                && !target.Dead
                && !target.Downed
                && target.Map == searcher.Map
                && target.RaceProps != null
                && target.RaceProps.Humanlike
                && CrossedUtility.IsInfectedPawn(target);
        }

        private static bool CanUseTacticalAI(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && !pawn.Dead
                && !pawn.Downed
                && pawn.Map != null
                && CrossedUtility.IsInfectedPawn(pawn)
                && TheMarkedMenSettings.MarkedAlwaysAssault
                && TheMarkedMenSettings.TacticalRetargetingEnabled
                && !TheMarkedMenRjwCompatibility.ShouldPreserveCurrentRjwJob(pawn);
        }

        private static bool IsAttackJob(JobDef jobDef)
        {
            return jobDef == JobDefOf.AttackMelee || jobDef == JobDefOf.AttackStatic;
        }

        private static bool CanSafelyInterruptCurrentJob(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.jobs == null)
            {
                return false;
            }

            if (IsRecoveryWaitJob(pawn.CurJob) || IsRecoveryWaitJob(pawn.jobs.curDriver?.job))
            {
                return false;
            }

            return pawn.jobs.curJob == null || pawn.jobs.curDriver != null;
        }

        private static bool IsRecoveryWaitJob(Job job)
        {
            return string.Equals(job?.def?.defName, WaitDownedJobDefName, StringComparison.Ordinal);
        }

        private static float ScoreBuildingTarget(Pawn searcher, Building target)
        {
            if (target == null || target.Destroyed || target.Faction != Faction.OfPlayer)
            {
                return 0f;
            }

            float distanceSquared = searcher.Position.DistanceToSquared(target.Position);
            if (distanceSquared > MaxTacticalTargetDistanceSquared)
            {
                return 0f;
            }

            string defName = target.def?.defName ?? string.Empty;
            string label = target.Label ?? string.Empty;
            float score = 0f;

            if (TheMarkedMenSettings.PriorityTargetingEnabled && target.TryGetComp<CompPowerTrader>() != null)
            {
                score += 90f;
            }

            if (TheMarkedMenSettings.PriorityTargetingEnabled && ContainsAny(defName, "Battery", "Generator", "Solar", "Geothermal", "Power", "Comms", "Console"))
            {
                score += 70f;
            }

            if (TheMarkedMenSettings.PriorityTargetingEnabled && ContainsAny(defName, "Hospital", "Bed", "Research", "Lab", "Scanner"))
            {
                score += 60f;
            }

            if (TheMarkedMenSettings.PriorityTargetingEnabled && (ContainsAny(defName, "Nutrient", "Hydroponics", "Cooler", "Freezer", "Food") || label.IndexOf("food", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                score += 45f;
            }

            if (TheMarkedMenSettings.PriorityTargetingEnabled && ContainsAny(defName, "Turret", "Mortar"))
            {
                score += 55f;
            }

            if (TheMarkedMenSettings.DoorTargetingEnabled && ContainsAny(defName, "Door", "Wall", "Gate"))
            {
                score += 30f;
            }

            if (score <= 0f)
            {
                return 0f;
            }

            return score - Mathf.Sqrt(distanceSquared) * 0.75f;
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
    }

    public static class CrossedContagionUtility
    {
        private const float ContagionRadius = 2.9f;
        private const float ContagionRadiusSquared = ContagionRadius * ContagionRadius;
        private const int ContagionPulseIntervalTicks = 500;
        private const int MaxContagionTargetsPerPulse = 3;

        public static void TryContagionPulse(Pawn source)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings != null && !settings.infectionEnabled)
            {
                return;
            }

            if (source == null || source.Dead || !source.Spawned || source.Map == null || !CrossedUtility.IsInfectedPawn(source))
            {
                return;
            }

            int maxTargets = TheMarkedMenSettings.MaxContagionTargetsPerPulse;
            if (maxTargets <= 0 || !source.IsHashIntervalTick(TheMarkedMenSettings.ContagionPulseIntervalTicks))
            {
                return;
            }

            IReadOnlyList<Pawn> pawns = source.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            int exposedTargets = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn target = pawns[i];
                if (!CanContagionReach(source, target))
                {
                    continue;
                }

                if (CrossedUtility.TryExpose(target, TheMarkedMenSettings.CloseContactExposureChance, "contagious Marked Virus contact", source))
                {
                    exposedTargets++;
                    if (exposedTargets >= maxTargets)
                    {
                        return;
                    }
                }
            }
        }

        private static bool CanContagionReach(Pawn source, Pawn target)
        {
            if (target == null || target == source || target.Dead || !target.Spawned || target.Map != source.Map)
            {
                return false;
            }

            if (target.RaceProps == null || !target.RaceProps.Humanlike || CrossedUtility.IsInfectedPawn(target) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(target))
            {
                return false;
            }

            if (source.Position.DistanceToSquared(target.Position) > ContagionRadiusSquared)
            {
                return false;
            }

            return GenSight.LineOfSight(source.Position, target.Position, source.Map);
        }
    }

    public static class CrossedCorpseUtility
    {
        private const float CorpseContaminationRadius = 3.2f;
        private const float CorpseContaminationRadiusSquared = CorpseContaminationRadius * CorpseContaminationRadius;
        private const int CorpseContaminationIntervalTicks = 750;
        private const int MaxCorpsesPerPulse = 2;

        public static void TryContaminateNearbyCorpses(Pawn source)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings != null && !settings.infectionEnabled)
            {
                return;
            }

            if (source == null || source.Dead || !source.Spawned || source.Map == null || !CrossedUtility.IsInfectedPawn(source))
            {
                return;
            }

            int maxCorpses = TheMarkedMenSettings.MaxCorpsesPerPulse;
            if (maxCorpses <= 0 || !source.IsHashIntervalTick(TheMarkedMenSettings.CorpseContaminationIntervalTicks))
            {
                return;
            }

            List<Thing> corpses = source.Map.listerThings?.ThingsInGroup(ThingRequestGroup.Corpse);
            if (corpses == null || corpses.Count == 0)
            {
                return;
            }

            int contaminated = 0;
            for (int i = 0; i < corpses.Count; i++)
            {
                Corpse corpse = corpses[i] as Corpse;
                if (!CanContaminateCorpse(source, corpse))
                {
                    continue;
                }

                if (Rand.Chance(TheMarkedMenSettings.CorpseContaminationChance) && TryContaminateCorpse(source, corpse))
                {
                    contaminated++;
                    if (contaminated >= maxCorpses)
                    {
                        return;
                    }
                }
            }
        }

        private static bool CanContaminateCorpse(Pawn source, Corpse corpse)
        {
            Pawn innerPawn = corpse?.InnerPawn;
            if (innerPawn == null || corpse.Destroyed || innerPawn.Destroyed || !innerPawn.Dead)
            {
                return false;
            }

            if (innerPawn.RaceProps == null || !innerPawn.RaceProps.Humanlike || CrossedUtility.HasCrossVirusImmunity(innerPawn))
            {
                return false;
            }

            if (CrossedUtility.ShouldReanimateAsCrossed(innerPawn))
            {
                CrossedUtility.Component?.QueueCrossedReanimation(innerPawn);
                return false;
            }

            if (source.Position.DistanceToSquared(corpse.Position) > CorpseContaminationRadiusSquared)
            {
                return false;
            }

            return GenSight.LineOfSight(source.Position, corpse.Position, source.Map);
        }

        private static bool TryContaminateCorpse(Pawn source, Corpse corpse)
        {
            Pawn innerPawn = corpse?.InnerPawn;
            HediffDef virus = CADefOf.CrossVirus;
            if (innerPawn?.health == null || virus == null)
            {
                return false;
            }

            Hediff hediff = innerPawn.health.hediffSet.GetFirstHediffOfDef(virus) ?? innerPawn.health.AddHediff(virus);
            hediff.Severity = Mathf.Max(hediff.Severity, 1f);
            hediff.TryGetComp<HediffComp_CrossVirus>()?.NotifyInfector(source);
            CrossedUtility.Component?.QueueCrossedReanimation(innerPawn);
            if (innerPawn.Faction == Faction.OfPlayer)
            {
                CrossedUtility.Component?.AddIncident(innerPawn.LabelShortCap + "'s corpse was contaminated by Marked Virus exposure.");
            }

            return true;
        }
    }

    public static class CrossedSocialUtility
    {
        private const int SocialPulseInterval = 1800;
        private const float SocialPulseBaseChance = 0.42f;
        private const float SocialPulseLeaderChance = 0.78f;
        private const float MaxSocialTargetDistanceSquared = 400f;
        private const float PackPanicRadius = 12f;

        public static void TryHostileSocialPulse(Pawn initiator)
        {
            if (initiator == null || !initiator.Spawned || initiator.Dead || initiator.Downed || initiator.Map == null || !CrossedUtility.IsCrossedPawn(initiator))
            {
                return;
            }

            float socialStrength = TheMarkedMenSettings.SocialTerrorStrength;
            if (socialStrength <= 0f || !initiator.IsHashIntervalTick(SocialPulseInterval))
            {
                return;
            }

            float chance = Mathf.Clamp01((initiator.kindDef == CADefOf.Alpha || initiator.kindDef == CADefOf.Screamer ? SocialPulseLeaderChance : SocialPulseBaseChance) * socialStrength);
            if (!Rand.Chance(chance))
            {
                return;
            }

            Pawn recipient = FindBestRecipient(initiator);
            if (recipient == null)
            {
                return;
            }

            InteractionDef interactionDef = PickInteraction(initiator, recipient);
            if (interactionDef != null)
            {
                TriggerInteraction(initiator, recipient, interactionDef);
            }
        }

        public static bool CanCrossedSocialInteract(Pawn initiator, Pawn recipient)
        {
            if (initiator == null || recipient == null || initiator == recipient || initiator.Map == null || recipient.Map != initiator.Map)
            {
                return false;
            }

            if (!CrossedUtility.IsCrossedPawn(initiator) || CrossedUtility.IsCrossedPawn(recipient))
            {
                return false;
            }

            return IsHumanlikeActivePawn(recipient) && IsPlayerAligned(recipient);
        }

        public static void ApplyCrossedSocialEffect(Pawn initiator, Pawn recipient, InteractionDef interactionDef)
        {
            if (!CanCrossedSocialInteract(initiator, recipient))
            {
                return;
            }

            if (TheMarkedMenSettings.SocialTerrorStrength <= 0f)
            {
                return;
            }

            ThoughtDef terror = CADefOf.CrossedSocialTerror;
            if (terror != null)
            {
                recipient.needs?.mood?.thoughts?.memories?.TryGainMemory(terror, initiator);
            }

            HediffDef panic = CADefOf.Panic;
            if (panic != null && recipient.health?.hediffSet != null && !recipient.health.hediffSet.HasHediff(panic))
            {
                recipient.health.AddHediff(panic);
            }

            if (interactionDef == CADefOf.CrossedPackLaughter || initiator.kindDef == CADefOf.Screamer || initiator.kindDef == CADefOf.Alpha)
            {
                CrossedUtility.ApplyScreamerPanic(recipient.Map, recipient.Position, PackPanicRadius);
            }
        }

        public static void TriggerInteraction(Pawn initiator, Pawn recipient, InteractionDef interactionDef)
        {
            if (interactionDef == null || !CanCrossedSocialInteract(initiator, recipient))
            {
                return;
            }

            ApplyCrossedSocialEffect(initiator, recipient, interactionDef);
            if (Find.PlayLog != null)
            {
                Find.PlayLog.Add(new PlayLogEntry_Interaction(interactionDef, initiator, recipient, new List<RulePackDef>()));
            }
        }

        private static Pawn FindBestRecipient(Pawn initiator)
        {
            Map map = initiator.Map;
            if (map?.mapPawns == null)
            {
                return null;
            }

            Pawn best = null;
            float bestScore = 0f;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!CanCrossedSocialInteract(initiator, candidate))
                {
                    continue;
                }

                float distanceSquared = initiator.Position.DistanceToSquared(candidate.Position);
                if (distanceSquared > MaxSocialTargetDistanceSquared)
                {
                    continue;
                }

                float score = 120f - Mathf.Sqrt(distanceSquared) * 4f;
                if (candidate.Downed)
                {
                    score += 70f;
                }

                if (candidate.ageTracker != null && candidate.ageTracker.AgeBiologicalYears < 18)
                {
                    score += 35f;
                }

                SkillRecord medicine = candidate.skills?.GetSkill(SkillDefOf.Medicine);
                if (medicine != null && medicine.Level >= 8)
                {
                    score += 35f;
                }

                if (!GenSight.LineOfSight(initiator.Position, candidate.Position, map))
                {
                    score *= 0.75f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static InteractionDef PickInteraction(Pawn initiator, Pawn recipient)
        {
            if (recipient.Downed && CADefOf.CrossedInfectionGloat != null)
            {
                return CADefOf.CrossedInfectionGloat;
            }

            if ((initiator.kindDef == CADefOf.Screamer || initiator.kindDef == CADefOf.Alpha) && CADefOf.CrossedPackLaughter != null && Rand.Chance(0.72f))
            {
                return CADefOf.CrossedPackLaughter;
            }

            float value = Rand.Value;
            if (value < 0.28f && CADefOf.CrossedFalseMercy != null)
            {
                return CADefOf.CrossedFalseMercy;
            }

            if (value < 0.74f && CADefOf.CrossedPredatoryTaunt != null)
            {
                return CADefOf.CrossedPredatoryTaunt;
            }

            if (CADefOf.CrossedInfectionGloat != null)
            {
                return CADefOf.CrossedInfectionGloat;
            }

            return CADefOf.CrossedPredatoryTaunt ?? CADefOf.CrossedFalseMercy ?? CADefOf.CrossedPackLaughter;
        }

        private static bool IsHumanlikeActivePawn(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && pawn.RaceProps != null && pawn.RaceProps.Humanlike;
        }

        private static bool IsPlayerAligned(Pawn pawn)
        {
            return pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer || pawn.IsColonistPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony;
        }
    }

    public class InteractionWorker_CrossedSocial : InteractionWorker
    {
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            if (!CrossedSocialUtility.CanCrossedSocialInteract(initiator, recipient))
            {
                return 0f;
            }

            if (interaction == CADefOf.CrossedPackLaughter && (initiator.kindDef == CADefOf.Screamer || initiator.kindDef == CADefOf.Alpha))
            {
                return 1.6f;
            }

            if (interaction == CADefOf.CrossedInfectionGloat && recipient.Downed)
            {
                return 1.8f;
            }

            return 0.7f;
        }

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            base.Interacted(initiator, recipient, extraSentencePacks, out letterText, out letterLabel, out letterDef, out lookTargets);
            CrossedSocialUtility.ApplyCrossedSocialEffect(initiator, recipient, interaction);
        }
    }

    public static class CrossedRaidAlertUtility
    {
        public static string BuildRaidLetterLabel(string fallbackLabel, List<Pawn> pawns, float points)
        {
            int count = CountActivePawns(pawns);
            string pressure = DescribeThreatTier(points);
            string fallback = fallbackLabel.NullOrEmpty() ? "Marked Men warband" : fallbackLabel;
            if (count <= 0)
            {
                return points > 0f ? fallback + ": " + pressure : fallback;
            }

            return fallback + ": " + count + " infected, " + pressure;
        }

        public static string BuildRaidLetterText(string baseText, List<Pawn> pawns, IncidentParms parms, bool horde)
        {
            float points = Mathf.Max(0f, parms?.points ?? 0f);
            Map map = parms?.target as Map;
            string text = baseText.NullOrEmpty()
                ? "A group of infected Marked Men has reached the colony."
                : baseText;

            if (!TheMarkedMenSettings.DetailedRaidLetters)
            {
                return text;
            }

            List<string> details = new List<string>();
            details.Add("Detected infected: " + CountActivePawns(pawns));
            details.Add("Threat pressure: " + points.ToString("F0") + " (" + DescribeThreatTier(points) + ")");
            details.Add("Approach: " + DescribeApproach(map, pawns));
            details.Add("Assault pattern: " + DescribeAssaultPattern(parms, horde));

            string composition = DescribeComposition(pawns);
            if (!composition.NullOrEmpty())
            {
                details.Add("Composition: " + composition);
            }

            string priority = DescribePriorityTargets(pawns);
            if (!priority.NullOrEmpty())
            {
                details.Add("Priority targets: " + priority);
            }

            details.Add("Containment: keep wounded and doctors away from melee contact, isolate infected blood, and hold sealed fallback doors.");

            return text + "\n\n" + string.Join("\n", details.ToArray());
        }

        public static string DescribeThreatTier(float points)
        {
            if (points >= 2400f)
            {
                return "catastrophic pressure";
            }

            if (points >= 1200f)
            {
                return "heavy pressure";
            }

            if (points >= 500f)
            {
                return "major pressure";
            }

            if (points >= 220f)
            {
                return "organized pressure";
            }

            return "probing pressure";
        }

        private static int CountActivePawns(List<Pawn> pawns)
        {
            if (pawns == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && !pawn.Destroyed && !pawn.Dead)
                {
                    count++;
                }
            }

            return count;
        }

        private static string DescribeApproach(Map map, List<Pawn> pawns)
        {
            if (map == null || pawns == null || pawns.Count == 0)
            {
                return "edge approach, direction unknown";
            }

            float x = 0f;
            float z = 0f;
            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Map != map)
                {
                    continue;
                }

                x += pawn.Position.x;
                z += pawn.Position.z;
                count++;
            }

            if (count == 0)
            {
                return "edge approach, direction unknown";
            }

            x /= count;
            z /= count;
            float dx = x - map.Size.x * 0.5f;
            float dz = z - map.Size.z * 0.5f;
            float absX = Mathf.Abs(dx);
            float absZ = Mathf.Abs(dz);
            if (absX < 8f && absZ < 8f)
            {
                return "near the colony interior";
            }

            if (absX > absZ * 1.35f)
            {
                return dx >= 0f ? "eastern edge" : "western edge";
            }

            if (absZ > absX * 1.35f)
            {
                return dz >= 0f ? "northern edge" : "southern edge";
            }

            string northSouth = dz >= 0f ? "north" : "south";
            string eastWest = dx >= 0f ? "east" : "west";
            return northSouth + eastWest + " edge";
        }

        private static string DescribeAssaultPattern(IncidentParms parms, bool horde)
        {
            string strategy = parms?.raidStrategy?.LabelCap.ToString();
            string arrival = parms?.raidArrivalMode?.LabelCap.ToString();
            if (strategy.NullOrEmpty())
            {
                strategy = "immediate attack";
            }

            if (arrival.NullOrEmpty())
            {
                arrival = "edge walk-in groups";
            }

            return strategy + ", " + arrival + (horde ? ", horde pressure" : ", no kidnapping/theft/retreat");
        }

        private static string DescribeComposition(List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return null;
            }

            List<string> parts = new List<string>();
            AddKindCount(parts, pawns, CADefOf.Alpha, "Alpha");
            AddKindCount(parts, pawns, CADefOf.Brute, "Brute");
            AddKindCount(parts, pawns, CADefOf.Screamer, "Screamer");
            AddKindCount(parts, pawns, CADefOf.Stalker, "Stalker");
            AddKindCount(parts, pawns, CADefOf.Hunter, "Hunter");
            AddKindCount(parts, pawns, CADefOf.Berserker, "Berserker");
            return parts.Count == 0 ? "unclassified infected" : string.Join(", ", parts.ToArray());
        }

        private static void AddKindCount(List<string> parts, List<Pawn> pawns, PawnKindDef kind, string label)
        {
            if (parts == null || pawns == null || kind == null)
            {
                return;
            }

            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i]?.kindDef == kind)
                {
                    count++;
                }
            }

            if (count > 0)
            {
                parts.Add(count + " " + label + (count == 1 ? "" : "s"));
            }
        }

        private static string DescribePriorityTargets(List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return null;
            }

            bool hasAlpha = HasKind(pawns, CADefOf.Alpha);
            bool hasScreamer = HasKind(pawns, CADefOf.Screamer);
            bool hasBrute = HasKind(pawns, CADefOf.Brute);
            List<string> priorities = new List<string>();
            if (hasAlpha)
            {
                priorities.Add("Alphas coordinating nearby infected");
            }

            if (hasScreamer)
            {
                priorities.Add("Screamers disrupting morale");
            }

            if (hasBrute)
            {
                priorities.Add("Brutes breaching doors and lines");
            }

            return priorities.Count == 0 ? "closest armed infected and exposed flankers" : string.Join("; ", priorities.ToArray());
        }

        private static bool HasKind(List<Pawn> pawns, PawnKindDef kind)
        {
            if (pawns == null || kind == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i]?.kindDef == kind)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class CrossedLordCleanupUtility
    {
        private const int CleanupIntervalTicks = 250;
        private const string WaitDownedJobDefName = "Wait_Downed";

        public static List<Pawn> CollectValidSpawnedLordPawns(IEnumerable<Pawn> pawns, Map map, Faction faction)
        {
            List<Pawn> valid = new List<Pawn>();
            if (pawns == null || map == null)
            {
                return valid;
            }

            foreach (Pawn pawn in pawns)
            {
                if (IsValidSpawnedLordPawn(pawn, map, faction))
                {
                    valid.Add(pawn);
                }
            }

            return valid;
        }

        public static void RemoveInvalidOwnedPawns(Lord lord)
        {
            if (lord?.ownedPawns == null || lord.ownedPawns.Count == 0)
            {
                return;
            }

            int ticks = Find.TickManager?.TicksGame ?? 0;
            int hash = lord.GetHashCode() & int.MaxValue;
            if ((ticks + hash) % TheMarkedMenSettings.LordCleanupIntervalTicks != 0)
            {
                return;
            }

            if (!IsCrossedLord(lord))
            {
                return;
            }

            Map map = lord.Map;
            Faction faction = lord.faction;
            for (int i = lord.ownedPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = lord.ownedPawns[i];
                if (!IsValidSpawnedLordPawn(pawn, map, faction))
                {
                    if (pawn == null)
                    {
                        lord.ownedPawns.RemoveAt(i);
                    }
                    else
                    {
                        lord.RemovePawn(pawn);
                    }
                }
            }
        }

        public static bool IsValidSpawnedLordPawn(Pawn pawn, Map map, Faction faction)
        {
            return pawn != null
                && !pawn.Destroyed
                && !pawn.Dead
                && !pawn.Downed
                && pawn.Spawned
                && pawn.Map == map
                && (faction == null || pawn.Faction == faction)
                && !HasRecoveryWaitJob(pawn);
        }

        private static bool IsCrossedLord(Lord lord)
        {
            if (lord?.faction?.def == CADefOf.CrossedFaction)
            {
                return true;
            }

            if (lord?.ownedPawns == null)
            {
                return false;
            }

            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                if (lord.ownedPawns[i]?.Faction?.def == CADefOf.CrossedFaction)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasRecoveryWaitJob(Pawn pawn)
        {
            return string.Equals(pawn?.CurJob?.def?.defName, WaitDownedJobDefName, StringComparison.Ordinal)
                || string.Equals(pawn?.jobs?.curDriver?.job?.def?.defName, WaitDownedJobDefName, StringComparison.Ordinal);
        }
    }

    public sealed class IncidentWorker_CrossedRaid : IncidentWorker_RaidEnemy
    {
        public override float ChanceFactorNow(IIncidentTarget target)
        {
            return base.ChanceFactorNow(target) * TheMarkedMenSettings.WarbandFrequencyMultiplier;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && CrossedUtility.Component?.EnsureCrossedFaction() != null;
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
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (crossed == null)
            {
                return false;
            }

            Map map = parms.target as Map;
            HashSet<Pawn> existingCrossed = CaptureExistingCrossed(map);
            TheMarkedMenGameComponent component = CrossedUtility.Component;
            parms.faction = crossed;
            if (component != null)
            {
                parms.points = component.CalculateEscalatedRaidPoints(parms.points);
            }

            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = TheMarkedMenSettings.MarkedCanTimeoutOrFlee;
            TheMarkedMenGameComponent.ApplyMarkedRaidArrivalPattern(parms);

            bool result = base.TryExecuteWorker(parms);
            if (result && map != null)
            {
                List<Pawn> spawned = FindNewCrossed(map, existingCrossed);
                MarkSpawnedCrossed(spawned);
                CrossedUtility.ApplyGeneratedRaidKindTuning(spawned);
                ForceImmediateAssaultLord(crossed, map, spawned, parms.points);
                component?.NotifyRaidLaunched(parms.points, spawned, map);
            }

            return result;
        }

        private static HashSet<Pawn> CaptureExistingCrossed(Map map)
        {
            FactionDef crossed = CADefOf.CrossedFaction;
            if (map?.mapPawns == null || crossed == null)
            {
                return new HashSet<Pawn>();
            }

            HashSet<Pawn> existing = new HashSet<Pawn>();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Faction?.def == crossed)
                {
                    existing.Add(pawn);
                }
            }

            return existing;
        }

        private static List<Pawn> FindNewCrossed(Map map, HashSet<Pawn> existing)
        {
            List<Pawn> spawned = new List<Pawn>();
            FactionDef crossed = CADefOf.CrossedFaction;
            if (map?.mapPawns == null || crossed == null)
            {
                return spawned;
            }

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Faction?.def == crossed && (existing == null || !existing.Contains(pawn)))
                {
                    spawned.Add(pawn);
                }
            }

            return spawned;
        }

        private static void MarkSpawnedCrossed(List<Pawn> pawns)
        {
            if (pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                CrossedUtility.ApplyClassHediffs(pawns[i]);
            }
        }

        private static void ForceImmediateAssaultLord(Faction faction, Map map, List<Pawn> pawns, float points)
        {
            if (faction == null || map == null || pawns == null || pawns.Count == 0)
            {
                return;
            }

            List<Pawn> attackers = CrossedLordCleanupUtility.CollectValidSpawnedLordPawns(pawns, map, faction);
            for (int i = 0; i < attackers.Count; i++)
            {
                Pawn pawn = attackers[i];
                if (LordUtility.TryGetLord(pawn, out Lord existingLord))
                {
                    existingLord.RemovePawn(pawn);
                }
            }

            if (attackers.Count == 0)
            {
                return;
            }

            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, false, TheMarkedMenSettings.MarkedCanTimeoutOrFlee, false, false, false, points >= 700f, true), map, attackers);
            for (int i = 0; i < attackers.Count; i++)
            {
                Pawn pawn = attackers[i];
                if (pawn?.jobs == null)
                {
                    continue;
                }

                if (pawn.jobs.curJob != null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                }
                else
                {
                    pawn.jobs.CheckForJobOverride(0f, true);
                }
            }
        }
    }

    public sealed class IncidentWorker_CrossedHorde : IncidentWorker
    {
        private const int MinHordeCount = 3;
        private const int MaxHordeCount = 12;

        public override float ChanceFactorNow(IIncidentTarget target)
        {
            return base.ChanceFactorNow(target) * TheMarkedMenSettings.HordeFrequencyMultiplier;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms) || !(parms.target is Map map) || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
            {
                return false;
            }

            Difficulty difficulty = Find.Storyteller?.difficulty;
            if (difficulty != null && !difficulty.allowBigThreats)
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

            parms.faction = crossed;
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = TheMarkedMenSettings.MarkedCanTimeoutOrFlee;
            TheMarkedMenGameComponent.ApplyMarkedRaidArrivalPattern(parms);
            parms.points = CalculateIncidentHordePoints(map, parms.points, def.minThreatPoints);

            int count = CalculateHordeCount(parms.points, parms.pawnCount, map);
            List<Pawn> pawns = GenerateHordePawns(count, parms.points, crossed, map);
            if (pawns.Count == 0)
            {
                return false;
            }

            parms.pawnCount = pawns.Count;
            if (parms.raidArrivalMode?.Worker == null || !parms.raidArrivalMode.Worker.CanUseWith(parms))
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            }

            parms.raidArrivalMode.Worker.Arrive(pawns, parms);
            pawns = CrossedLordCleanupUtility.CollectValidSpawnedLordPawns(pawns, map, crossed);
            if (pawns.Count == 0)
            {
                return false;
            }

            parms.pawnCount = pawns.Count;
            LordMaker.MakeNewLord(crossed, new LordJob_AssaultColony(crossed, false, TheMarkedMenSettings.MarkedCanTimeoutOrFlee, false, false, false, parms.points >= 700f, true), map, pawns);
            CrossedUtility.Component?.NotifyHordeLaunched(pawns.Count, parms.points);
            SendHordeLetter(pawns, parms);
            return true;
        }

        private static int CalculateHordeCount(float points, int requestedCount, Map map)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            int minCount = settings?.minimumHordeSize ?? MinHordeCount;
            int maxCount = settings?.maximumHordeSize ?? MaxHordeCount;
            minCount = Mathf.Clamp(minCount, 1, 50);
            maxCount = Mathf.Clamp(maxCount, minCount, 100);
            if (requestedCount > 0)
            {
                return Mathf.Clamp(requestedCount, minCount, maxCount);
            }

            float normalizedThreat = Mathf.InverseLerp(120f, 3600f, points);
            float threatScale = CurrentThreatScale();
            float storytellerCountFactor = Mathf.Clamp(Mathf.Sqrt(threatScale), 0.7f, 1.35f);
            int expected = Mathf.RoundToInt(Mathf.Lerp(minCount, maxCount, normalizedThreat) * storytellerCountFactor);
            int threatFloor = Mathf.RoundToInt(Mathf.Lerp(minCount, Mathf.Min(maxCount, 10f), normalizedThreat));
            expected = Mathf.Clamp(Mathf.Max(expected, threatFloor), minCount, maxCount);
            int variance = Mathf.Clamp(Mathf.RoundToInt(expected * 0.18f), 1, 5);
            return Rand.RangeInclusive(Mathf.Max(minCount, expected - variance), Mathf.Min(maxCount, expected + variance));
        }

        private static float CalculateIncidentHordePoints(Map map, float existingPoints, float minThreatPoints)
        {
            float storytellerPoints = map == null ? minThreatPoints : StorytellerUtility.DefaultThreatPointsNow(map);
            float points = Mathf.Max(existingPoints, storytellerPoints, minThreatPoints);
            float pressure = Mathf.InverseLerp(120f, 3600f, points);
            return TheMarkedMenSettings.ApplyRaidPointSettings(Mathf.Max(minThreatPoints, points * Mathf.Lerp(0.95f, 1.18f, pressure)));
        }

        private static float CurrentThreatScale()
        {
            Difficulty difficulty = Find.Storyteller?.difficulty;
            return Mathf.Max(0.1f, difficulty?.threatScale ?? 1f);
        }

        private static List<Pawn> GenerateHordePawns(int count, float points, Faction faction, Map map)
        {
            List<Pawn> pawns = new List<Pawn>(count);
            bool alphaAdded = false;
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = PickHordeKind(points, count, !alphaAdded);
                if (kind == null)
                {
                    break;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction, map.Tile);
                if (pawn == null)
                {
                    continue;
                }

                alphaAdded = alphaAdded || kind == CADefOf.Alpha;
                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                pawns.Add(pawn);
            }

            CrossedUtility.ApplyGeneratedRaidKindTuning(pawns);
            return pawns;
        }

        private static PawnKindDef PickHordeKind(float points, int count, bool allowAlpha)
        {
            float normalizedThreat = Mathf.InverseLerp(120f, 2400f, points);
            PawnKindDef selected = null;
            float totalWeight = 0f;

            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Berserker, 12f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Hunter, Mathf.Lerp(2.5f, 8.5f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Stalker, points >= 220f ? Mathf.Lerp(1.5f, 4.5f, normalizedThreat) : 0.75f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Screamer, points >= 300f ? 3.5f : 1.25f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Brute, points >= 500f ? Mathf.Lerp(1f, 4.5f, Mathf.InverseLerp(500f, 2400f, points)) : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Alpha, allowAlpha && count >= 10 && points >= 1200f ? 0.55f : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Child, TheMarkedMenMod.Settings?.allowMarkedChildren == true && points < 500f ? 0.35f : 0f);

            return selected ?? CADefOf.Berserker ?? CADefOf.Hunter ?? CADefOf.Stalker;
        }

        private static void AddWeightedKind(ref PawnKindDef selected, ref float totalWeight, PawnKindDef kind, float weight)
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

        private void SendHordeLetter(List<Pawn> pawns, IncidentParms parms)
        {
            if (Find.LetterStack == null)
            {
                return;
            }

            IncidentParms letterParms = new IncidentParms
            {
                points = parms?.points ?? 0f,
                target = pawns.Count > 0 ? pawns[0].Map : null,
                raidStrategy = parms?.raidStrategy ?? RaidStrategyDefOf.ImmediateAttack,
                raidArrivalMode = parms?.raidArrivalMode ?? PawnsArrivalModeDefOf.EdgeWalkInGroups
            };
            string label = CrossedRaidAlertUtility.BuildRaidLetterLabel(def.letterLabel.NullOrEmpty() ? "Marked Men horde" : def.letterLabel, pawns, letterParms.points);
            string text = CrossedRaidAlertUtility.BuildRaidLetterText(def.letterText, pawns, letterParms, true);
            Find.LetterStack.ReceiveLetter(label, text, def.letterDef ?? LetterDefOf.ThreatBig, new LookTargets(pawns));
        }
    }

    public sealed class IncidentWorker_CrossedProbe : IncidentWorker
    {
        private const int MinProbeCount = 2;
        private const int MaxProbeCount = 4;

        public override float ChanceFactorNow(IIncidentTarget target)
        {
            return base.ChanceFactorNow(target) * TheMarkedMenSettings.ProbeFrequencyMultiplier;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms) || !(parms.target is Map map) || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
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

            parms.faction = crossed;
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = TheMarkedMenSettings.MarkedCanTimeoutOrFlee;
            TheMarkedMenGameComponent.ApplyMarkedRaidArrivalPattern(parms);
            parms.points = CalculateProbePoints(map, parms.points, def.minThreatPoints);

            int count = CalculateProbeCount(parms.points, parms.pawnCount);
            List<Pawn> pawns = GenerateProbePawns(count, parms.points, crossed, map);
            if (pawns.Count == 0)
            {
                return false;
            }

            parms.pawnCount = pawns.Count;
            if (parms.raidArrivalMode?.Worker == null || !parms.raidArrivalMode.Worker.CanUseWith(parms))
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            }

            parms.raidArrivalMode.Worker.Arrive(pawns, parms);
            pawns = CrossedLordCleanupUtility.CollectValidSpawnedLordPawns(pawns, map, crossed);
            if (pawns.Count == 0)
            {
                return false;
            }

            parms.pawnCount = pawns.Count;
            LordMaker.MakeNewLord(crossed, new LordJob_AssaultColony(crossed, false, TheMarkedMenSettings.MarkedCanTimeoutOrFlee, false, false, false, false, true), map, pawns);
            CrossedUtility.Component?.NotifyProbeLaunched(pawns.Count, parms.points);
            SendProbeLetter(pawns, parms);
            return true;
        }

        private static float CalculateProbePoints(Map map, float existingPoints, float minThreatPoints)
        {
            float storytellerPoints = map == null ? minThreatPoints : StorytellerUtility.DefaultThreatPointsNow(map);
            float points = Mathf.Max(existingPoints, storytellerPoints * 0.45f, minThreatPoints);
            return TheMarkedMenSettings.ApplyRaidPointSettings(Mathf.Clamp(points, minThreatPoints, 650f));
        }

        private static int CalculateProbeCount(float points, int requestedCount)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            int minCount = settings?.minimumProbeSize ?? MinProbeCount;
            int maxCount = settings?.maximumProbeSize ?? MaxProbeCount;
            minCount = Mathf.Clamp(minCount, 1, 20);
            maxCount = Mathf.Clamp(maxCount, minCount, 30);
            if (requestedCount > 0)
            {
                return Mathf.Clamp(requestedCount, minCount, maxCount);
            }

            float normalizedThreat = Mathf.InverseLerp(80f, 650f, points);
            int expected = Mathf.RoundToInt(Mathf.Lerp(minCount, maxCount, normalizedThreat));
            int variance = Mathf.Clamp(Mathf.RoundToInt(expected * 0.2f), 1, 2);
            return Rand.RangeInclusive(Mathf.Max(minCount, expected - variance), Mathf.Min(maxCount, expected + variance));
        }

        private static List<Pawn> GenerateProbePawns(int count, float points, Faction faction, Map map)
        {
            List<Pawn> pawns = new List<Pawn>(count);
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = PickProbeKind(points);
                if (kind == null)
                {
                    break;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction, map.Tile);
                if (pawn == null)
                {
                    continue;
                }

                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                pawns.Add(pawn);
            }

            CrossedUtility.ApplyGeneratedRaidKindTuning(pawns);
            return pawns;
        }

        private static PawnKindDef PickProbeKind(float points)
        {
            float normalizedThreat = Mathf.InverseLerp(80f, 650f, points);
            PawnKindDef selected = null;
            float totalWeight = 0f;

            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Stalker, Mathf.Lerp(4f, 6f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Hunter, Mathf.Lerp(3f, 5f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Berserker, 3f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Screamer, points >= 220f ? Mathf.Lerp(0.5f, 1.75f, normalizedThreat) : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Brute, points >= 500f ? 0.35f : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Child, TheMarkedMenMod.Settings?.allowMarkedChildren == true && points < 220f ? 0.35f : 0f);

            return selected ?? CADefOf.Stalker ?? CADefOf.Hunter ?? CADefOf.Berserker;
        }

        private static void AddWeightedKind(ref PawnKindDef selected, ref float totalWeight, PawnKindDef kind, float weight)
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

        private void SendProbeLetter(List<Pawn> pawns, IncidentParms parms)
        {
            if (Find.LetterStack == null)
            {
                return;
            }

            IncidentParms letterParms = new IncidentParms
            {
                points = parms?.points ?? 0f,
                target = pawns.Count > 0 ? pawns[0].Map : null,
                raidStrategy = parms?.raidStrategy ?? RaidStrategyDefOf.ImmediateAttack,
                raidArrivalMode = parms?.raidArrivalMode ?? PawnsArrivalModeDefOf.EdgeWalkInGroups
            };
            string label = CrossedRaidAlertUtility.BuildRaidLetterLabel(def.letterLabel.NullOrEmpty() ? "Marked Men scouting pack" : def.letterLabel, pawns, letterParms.points);
            string text = CrossedRaidAlertUtility.BuildRaidLetterText(def.letterText, pawns, letterParms, false);
            Find.LetterStack.ReceiveLetter(label, text, def.letterDef ?? LetterDefOf.ThreatSmall, new LookTargets(pawns));
        }
    }

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

        private static TheMarkedMenGameComponent Component => Current.Game?.GetComponent<TheMarkedMenGameComponent>();

        private static void Report(bool success, string successText, string failureText)
        {
            Messages.Message(success ? successText : failureText, success ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput, false);
        }
    }

    public static class CrossedCompatibility
    {
        private static readonly string[] KnownExactPackages =
        {
            "ceteam.combatextended",
            "kentington.saveourship2",
            "dubwise.dubsbadhygiene",
            "dubwise.dubsperformanceanalyzer.steam",
            "orion.hospitality",
            "roolo.runandgun",
            "krkr.rocketman",
            "taranchuk.performancefish",
            "taranchuk.performanceoptimizer",
            "taranchuk.fastergameloading",
            "edria.performancesuperboosterultimate",
            "fluxxfield.defloadcache",
            "dev.soeur.imageopt",
            "rwmt.multiplayer",
            "rim.job.world",
            "zetrith.prepatcher",
            "daniledman.combatupdate",
            "oskarpotocki.vanillafactionsexpanded.core",
            "ferny.worldbuilder",
            "c0ffee.rimworld.animations",
            "smashphil.xmlpatchhelper",
            "v1024.visibleerrorlogs",
            "imranfish.patchoperationstacktraces",
            "mlie.logafterdeferror",
            "ludeon.rimworld.royalty",
            "ludeon.rimworld.ideology",
            "ludeon.rimworld.biotech",
            "ludeon.rimworld.anomaly",
            "ludeon.rimworld.odyssey"
        };

        private static readonly string[] KnownPackagePrefixes =
        {
            "vanillaexpanded."
        };

        public static void LogDetectedMods()
        {
            if (TheMarkedMenMod.Settings != null && !TheMarkedMenMod.Settings.verboseCompatibilityLogging)
            {
                return;
            }

            try
            {
                HashSet<string> activePackages = new HashSet<string>();
                foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
                {
                    string packageId = mod?.PackageIdPlayerFacing;
                    if (!string.IsNullOrEmpty(packageId))
                    {
                        activePackages.Add(packageId.ToLowerInvariant());
                    }
                }

                List<string> detected = new List<string>();
                for (int i = 0; i < KnownExactPackages.Length; i++)
                {
                    string packageId = KnownExactPackages[i];
                    if (activePackages.Contains(packageId))
                    {
                        detected.Add(packageId);
                    }
                }

                foreach (string activePackage in activePackages)
                {
                    for (int i = 0; i < KnownPackagePrefixes.Length; i++)
                    {
                        string prefix = KnownPackagePrefixes[i];
                        if (activePackage.StartsWith(prefix, StringComparison.Ordinal) && !detected.Contains(prefix + "*"))
                        {
                            detected.Add(prefix + "*");
                        }
                    }
                }

                Log.Message("[The Marked Men] Compatibility scan detected: " + (detected.Count == 0 ? "no tracked packages" : string.Join(", ", detected.ToArray())));
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Compatibility scan skipped: " + ex.Message);
            }
        }
    }
}
