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
            TheMarkedMenAncientUrbanRuinsIntegration.Apply(harmony);
            TheMarkedMenAncientUrbanRuinsSpawnPatch.Apply(harmony);
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
        private const int CurrentSettingsVersion = 12;
        public const float InfectionTransmissionChance = 0.45f;
        public const float DefaultMarkedRaidFrequencyMultiplier = 2f;
        public const float MinMarkedRaidFrequencyMultiplier = 0f;
        public const float MaxMarkedRaidFrequencyMultiplier = 5f;
        public const float DefaultRaidEscalationPerRaid = 0.18f;
        public const float DefaultRaidEscalationMaxBonus = 5f;
        public const float DefaultImmunitySurvivalChance = 0.05f;
        public const float DefaultTerminalTransformationWeight = 0.75f;
        public const float DefaultTerminalDeathWeight = 0.20f;
        public const float DefaultTerminalTransformationChance = DefaultTerminalTransformationWeight / (DefaultTerminalTransformationWeight + DefaultTerminalDeathWeight);
        private const float LegacyDefaultImmunitySurvivalChance = 0.02f;
        private const float LegacyDefaultTerminalTransformationWeight = 0.55f;
        private const float LegacyDefaultTerminalDeathWeight = 0.45f;
        private static float cachedContentHeight;
        private const float PresetButtonHeight = 32f;
        private const float PresetButtonGap = 4f;
        private const string CustomPresetName = "Custom";
        private static readonly Color HelpTextColor = new Color(0.72f, 0.72f, 0.72f);
        private static readonly Color SectionHeaderColor = new Color(0.22f, 0.24f, 0.26f, 1f);
        private static readonly Color SectionHeaderHoverColor = new Color(0.28f, 0.30f, 0.33f, 1f);
        private static readonly Color SectionToggleColor = new Color(0.10f, 0.11f, 0.12f, 1f);
        private static readonly Color SectionToggleHoverColor = new Color(0.16f, 0.17f, 0.19f, 1f);
        private const float OptionRowHeight = 28f;
        private const float SectionHeaderHeight = 38f;
        private const float SectionToggleWidth = 48f;

        public bool infectionEnabled = true;
        public bool warcasketsBlockExposure = true;
        public bool vacsuitBlockExposure = true;
        public bool gasMasksBlockExposure = true;
        public bool sealedArmorBlockExposure = true;
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
        public float raidPointsMultiplier = 2000f;
        public float minimumRaidPoints = 5000f;
        public float raidEscalationPerRaid = DefaultRaidEscalationPerRaid;
        public float raidEscalationMaxBonus = DefaultRaidEscalationMaxBonus;
        public bool allowGroupedEdgeArrival = true;
        public bool allowDistributedGroupArrival = true;
        public bool allowDistributedArrival = true;
        public bool allowSingleEdgeArrival = true;
        public float civilianWeightMultiplier = 1f;
        public float scoutWeightMultiplier = 1f;
        public float hunterWeightMultiplier = 1f;
        public float shooterWeightMultiplier = 1f;
        public float raiderWeightMultiplier = 1f;
        public float soldierWeightMultiplier = 1f;
        public float bruteWeightMultiplier = 1f;
        public float pyromaniacWeightMultiplier = 1f;
        public float alphaWeightMultiplier = 1f;
        public float warlordWeightMultiplier = 1f;
        public float markedManWeightMultiplier = 1f;
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

        public bool meleeTransmissionEnabled = true;
        public bool biteTransmissionEnabled = true;
        public bool clawTransmissionEnabled = true;
        public bool scratchTransmissionEnabled = true;
        public bool punchTransmissionEnabled = true;
        public bool meleeWeaponTransmissionEnabled = true;
        public float biteInfectionChance = InfectionTransmissionChance;
        public float clawInfectionChance = InfectionTransmissionChance;
        public float scratchInfectionChance = InfectionTransmissionChance;
        public float punchInfectionChance = InfectionTransmissionChance;
        public float meleeWeaponInfectionChance = InfectionTransmissionChance;
        public float markedMenInfectionChance = 1f;
        public bool markedMenGuaranteedInfection = true;

        public float infectionProgressionSpeedMultiplier = 1f;
        public float incubationDurationMultiplier = 1f;
        public float immunitySurvivalChance = DefaultImmunitySurvivalChance;
        public float terminalTransformationWeight = DefaultTerminalTransformationWeight;
        public float terminalDeathWeight = DefaultTerminalDeathWeight;
        public float reanimationChance = 1f;
        public int reanimationDelayTicks = 900;
        public float starterLineageBreakthroughChance = 0.04f;

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
        public bool detailedRaidLetters;
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

        public bool bloodlustEnabled = true;
        public float bloodlustDecayRate = 1f;
        public float bloodlustKillGainMultiplier = 1f;
        public float bloodlustCombatGainMultiplier = 1f;
        public bool anticipationEnabled = true;
        public float anticipationGainMultiplier = 1f;
        public float anticipationDecayMultiplier = 1f;

        public bool urbanOutbreaksEnabled = true;
        public float urbanInfectionDensity = 1f;
        public float urbanAmbushFrequency = 1f;
        public bool dormantInfestationsEnabled = true;
        public float dormantInfestationFrequency = 1f;
        public bool cityEpicentersEnabled = true;
        public float epicenterSpawnChance = 0.15f;
        public bool urbanAmbushesEnabled = true;
        public bool survivorEncountersEnabled = true;
        public float survivorEncounterChance = 0.5f;

        public bool aurSpawnPatchEnabled = true;
        public float aurMinimumSpawnDistance = 35f;
        public bool aurPreferEdgeSpawn = true;
        public bool aurSpawnPatchDebugLogging;

        public bool lostSurvivorEnabled = true;
        public float lostSurvivorFrequencyMultiplier = 1f;
        public float dormantMarkMinDays = 8f;
        public float dormantMarkMaxDays = 30f;
        public float dormantMarkTriggerMultiplier = 1f;
        public float dormantMarkAlphaChance = 0.10f;
        public float dormantMarkGroupVariantChance = 0f;

        public bool prisonerInfectionEnabled = true;
        public float prisonerInfectionChance = 0.15f;
        public bool prisonerSelfHarmEnabled = true;
        public float prisonerSelfHarmStageDays = 5f;
        public float prisonerSelfHarmSuicideDays = 15f;
        public float prisonerEscapeAggressionMultiplier = 1f;
        public bool prisonerCosmeticEnabled = true;
        public bool prisonerDebugLogging;

        private int settingsVersion = CurrentSettingsVersion;
        private string currentPreset = "Outbreak simulator";
        private Vector2 scrollPosition;
        private readonly Dictionary<string, string> numericBuffers = new Dictionary<string, string>();
        private Dictionary<string, bool> sectionOpenStates = new Dictionary<string, bool>();
        private bool currentSectionOpen = true;

        public TheMarkedMenSettings()
        {
            ApplyOutbreakDefaults(false);
        }

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

        public static bool DetailedRaidLetters => TheMarkedMenMod.Settings?.detailedRaidLetters ?? false;

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

        public static bool MarkedAlwaysAssault => true;

        public static bool MarkedCanTimeoutOrFlee => false;

        public static bool WarcasketsBlockExposure => TheMarkedMenMod.Settings?.warcasketsBlockExposure != false;
        public static bool VacsuitBlockExposure => TheMarkedMenMod.Settings?.vacsuitBlockExposure != false;
        public static bool GasMasksBlockExposure => TheMarkedMenMod.Settings?.gasMasksBlockExposure != false;
        public static bool SealedArmorBlockExposure => TheMarkedMenMod.Settings?.sealedArmorBlockExposure != false;

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
            Scribe_Values.Look(ref warcasketsBlockExposure, "warcasketsBlockExposure", true);
            Scribe_Values.Look(ref vacsuitBlockExposure, "vacsuitBlockExposure", true);
            Scribe_Values.Look(ref gasMasksBlockExposure, "gasMasksBlockExposure", true);
            Scribe_Values.Look(ref sealedArmorBlockExposure, "sealedArmorBlockExposure", true);
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
            Scribe_Values.Look(ref raidPointsMultiplier, "raidPointsMultiplier", 2f);
            Scribe_Values.Look(ref minimumRaidPoints, "minimumRaidPoints", 5000f);
            Scribe_Values.Look(ref raidEscalationPerRaid, "raidEscalationPerRaid", DefaultRaidEscalationPerRaid);
            Scribe_Values.Look(ref raidEscalationMaxBonus, "raidEscalationMaxBonus", DefaultRaidEscalationMaxBonus);
            Scribe_Values.Look(ref allowGroupedEdgeArrival, "allowGroupedEdgeArrival", true);
            Scribe_Values.Look(ref allowDistributedGroupArrival, "allowDistributedGroupArrival", true);
            Scribe_Values.Look(ref allowDistributedArrival, "allowDistributedArrival", true);
            Scribe_Values.Look(ref allowSingleEdgeArrival, "allowSingleEdgeArrival", true);
            Scribe_Values.Look(ref civilianWeightMultiplier, "civilianWeightMultiplier", 1f);
            Scribe_Values.Look(ref scoutWeightMultiplier, "scoutWeightMultiplier", 1f);
            Scribe_Values.Look(ref hunterWeightMultiplier, "hunterWeightMultiplier", 1f);
            Scribe_Values.Look(ref shooterWeightMultiplier, "shooterWeightMultiplier", 1f);
            Scribe_Values.Look(ref raiderWeightMultiplier, "raiderWeightMultiplier", 1f);
            Scribe_Values.Look(ref soldierWeightMultiplier, "soldierWeightMultiplier", 1f);
            Scribe_Values.Look(ref bruteWeightMultiplier, "bruteWeightMultiplier", 1f);
            Scribe_Values.Look(ref pyromaniacWeightMultiplier, "pyromaniacWeightMultiplier", 1f);
            Scribe_Values.Look(ref alphaWeightMultiplier, "alphaWeightMultiplier", 1f);
            Scribe_Values.Look(ref warlordWeightMultiplier, "warlordWeightMultiplier", 1f);
            Scribe_Values.Look(ref markedManWeightMultiplier, "markedManWeightMultiplier", 1f);
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
            Scribe_Values.Look(ref meleeTransmissionEnabled, "meleeTransmissionEnabled", true);
            Scribe_Values.Look(ref biteTransmissionEnabled, "biteTransmissionEnabled", true);
            Scribe_Values.Look(ref clawTransmissionEnabled, "clawTransmissionEnabled", true);
            Scribe_Values.Look(ref scratchTransmissionEnabled, "scratchTransmissionEnabled", true);
            Scribe_Values.Look(ref punchTransmissionEnabled, "punchTransmissionEnabled", true);
            Scribe_Values.Look(ref meleeWeaponTransmissionEnabled, "meleeWeaponTransmissionEnabled", true);
            Scribe_Values.Look(ref biteInfectionChance, "biteInfectionChance", InfectionTransmissionChance);
            Scribe_Values.Look(ref clawInfectionChance, "clawInfectionChance", InfectionTransmissionChance);
            Scribe_Values.Look(ref scratchInfectionChance, "scratchInfectionChance", InfectionTransmissionChance);
            Scribe_Values.Look(ref punchInfectionChance, "punchInfectionChance", InfectionTransmissionChance);
            Scribe_Values.Look(ref meleeWeaponInfectionChance, "meleeWeaponInfectionChance", InfectionTransmissionChance);
            Scribe_Values.Look(ref markedMenInfectionChance, "markedMenInfectionChance", 1f);
            Scribe_Values.Look(ref markedMenGuaranteedInfection, "markedMenGuaranteedInfection", true);
            Scribe_Values.Look(ref infectionProgressionSpeedMultiplier, "infectionProgressionSpeedMultiplier", 1f);
            Scribe_Values.Look(ref incubationDurationMultiplier, "incubationDurationMultiplier", 1f);
            Scribe_Values.Look(ref immunitySurvivalChance, "immunitySurvivalChance", DefaultImmunitySurvivalChance);
            Scribe_Values.Look(ref terminalTransformationWeight, "terminalTransformationWeight", DefaultTerminalTransformationWeight);
            Scribe_Values.Look(ref terminalDeathWeight, "terminalDeathWeight", DefaultTerminalDeathWeight);
            Scribe_Values.Look(ref reanimationChance, "reanimationChance", 1f);
            Scribe_Values.Look(ref reanimationDelayTicks, "reanimationDelayTicks", 900);
            Scribe_Values.Look(ref starterLineageBreakthroughChance, "starterLineageBreakthroughChance", 0.04f);

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
            Scribe_Values.Look(ref detailedRaidLetters, "detailedRaidLetters", false);
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
            Scribe_Values.Look(ref bloodlustEnabled, "bloodlustEnabled", true);
            Scribe_Values.Look(ref bloodlustDecayRate, "bloodlustDecayRate", 1f);
            Scribe_Values.Look(ref bloodlustKillGainMultiplier, "bloodlustKillGainMultiplier", 1f);
            Scribe_Values.Look(ref bloodlustCombatGainMultiplier, "bloodlustCombatGainMultiplier", 1f);
            Scribe_Values.Look(ref anticipationEnabled, "anticipationEnabled", true);
            Scribe_Values.Look(ref anticipationGainMultiplier, "anticipationGainMultiplier", 1f);
            Scribe_Values.Look(ref anticipationDecayMultiplier, "anticipationDecayMultiplier", 1f);

            Scribe_Values.Look(ref urbanOutbreaksEnabled, "urbanOutbreaksEnabled", true);
            Scribe_Values.Look(ref urbanInfectionDensity, "urbanInfectionDensity", 1f);
            Scribe_Values.Look(ref urbanAmbushFrequency, "urbanAmbushFrequency", 1f);
            Scribe_Values.Look(ref dormantInfestationsEnabled, "dormantInfestationsEnabled", true);
            Scribe_Values.Look(ref dormantInfestationFrequency, "dormantInfestationFrequency", 1f);
            Scribe_Values.Look(ref cityEpicentersEnabled, "cityEpicentersEnabled", true);
            Scribe_Values.Look(ref epicenterSpawnChance, "epicenterSpawnChance", 0.15f);
            Scribe_Values.Look(ref urbanAmbushesEnabled, "urbanAmbushesEnabled", true);
            Scribe_Values.Look(ref survivorEncountersEnabled, "survivorEncountersEnabled", true);
            Scribe_Values.Look(ref survivorEncounterChance, "survivorEncounterChance", 0.5f);
            Scribe_Values.Look(ref aurSpawnPatchEnabled, "aurSpawnPatchEnabled", true);
            Scribe_Values.Look(ref aurMinimumSpawnDistance, "aurMinimumSpawnDistance", 35f);
            Scribe_Values.Look(ref aurPreferEdgeSpawn, "aurPreferEdgeSpawn", true);
            Scribe_Values.Look(ref aurSpawnPatchDebugLogging, "aurSpawnPatchDebugLogging", false);
            Scribe_Values.Look(ref lostSurvivorEnabled, "lostSurvivorEnabled", true);
            Scribe_Values.Look(ref lostSurvivorFrequencyMultiplier, "lostSurvivorFrequencyMultiplier", 1f);
            Scribe_Values.Look(ref dormantMarkMinDays, "dormantMarkMinDays", 8f);
            Scribe_Values.Look(ref dormantMarkMaxDays, "dormantMarkMaxDays", 30f);
            Scribe_Values.Look(ref dormantMarkTriggerMultiplier, "dormantMarkTriggerMultiplier", 1f);
            Scribe_Values.Look(ref dormantMarkAlphaChance, "dormantMarkAlphaChance", 0.10f);
            Scribe_Values.Look(ref dormantMarkGroupVariantChance, "dormantMarkGroupVariantChance", 0f);

            Scribe_Values.Look(ref prisonerInfectionEnabled, "prisonerInfectionEnabled", true);
            Scribe_Values.Look(ref prisonerInfectionChance, "prisonerInfectionChance", 0.15f);
            Scribe_Values.Look(ref prisonerSelfHarmEnabled, "prisonerSelfHarmEnabled", true);
            Scribe_Values.Look(ref prisonerSelfHarmStageDays, "prisonerSelfHarmStageDays", 5f);
            Scribe_Values.Look(ref prisonerSelfHarmSuicideDays, "prisonerSelfHarmSuicideDays", 15f);
            Scribe_Values.Look(ref prisonerEscapeAggressionMultiplier, "prisonerEscapeAggressionMultiplier", 1f);
            Scribe_Values.Look(ref prisonerCosmeticEnabled, "prisonerCosmeticEnabled", true);
            Scribe_Values.Look(ref prisonerDebugLogging, "prisonerDebugLogging", false);

            Scribe_Values.Look(ref currentPreset, "currentPreset", "Outbreak simulator");
            Scribe_Collections.Look(ref sectionOpenStates, "sectionOpenStates", LookMode.Value, LookMode.Value);
            if (sectionOpenStates == null)
            {
                sectionOpenStates = new Dictionary<string, bool>();
            }
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
                    raidPointsMultiplier = 2f;
                    minimumRaidPoints = 5000f;
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

                if (loadedSettingsVersion < 9)
                {
                    bloodlustEnabled = true;
                    bloodlustDecayRate = 1f;
                    bloodlustKillGainMultiplier = 1f;
                    bloodlustCombatGainMultiplier = 1f;
                    anticipationEnabled = true;
                    anticipationGainMultiplier = 1f;
                    anticipationDecayMultiplier = 1f;
                }

                if (loadedSettingsVersion < 10)
                {
                    meleeTransmissionEnabled = true;
                    biteTransmissionEnabled = true;
                    clawTransmissionEnabled = true;
                    scratchTransmissionEnabled = true;
                    punchTransmissionEnabled = true;
                    meleeWeaponTransmissionEnabled = true;
                    biteInfectionChance = infectedAssaultExposureChance;
                    clawInfectionChance = infectedAssaultExposureChance;
                    scratchInfectionChance = infectedAssaultExposureChance;
                    punchInfectionChance = infectedAssaultExposureChance;
                    meleeWeaponInfectionChance = infectedAssaultExposureChance;
                    markedMenInfectionChance = 1f;
                    markedMenGuaranteedInfection = true;
                }

                if (loadedSettingsVersion < 11 && (string.IsNullOrEmpty(currentPreset) || currentPreset == "Default"))
                {
                    ApplyOutbreakDefaults(false);
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
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, inRect.height);

            if (cachedContentHeight <= 0f)
            {
                RemeasureContentHeight(inRect.width);
            }

            viewRect.height = cachedContentHeight;
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            DrawAllSettings(listing);

            listing.End();
            Widgets.EndScrollView();
            ClampSettings();
        }

        private void RemeasureContentHeight(float width)
        {
            Listing_Standard measure = new Listing_Standard();
            measure.Begin(new Rect(0f, 0f, width - 16f, 999999f));
            DrawAllSettings(measure);
            measure.End();
            cachedContentHeight = measure.CurHeight + 10f;
        }

        private void DrawAllSettings(Listing_Standard listing)
        {
            DrawSettingsIntro(listing);
            DrawPresetControls(listing);

            DrawSectionHeader(listing, "Core Rules", "Global switches for infection and compatibility diagnostics. These do not remove existing hediffs from a save.");
            DrawCheckbox(listing, "Allow new Marked Virus infections", ref infectionEnabled, "When disabled, this mod stops creating new Marked Virus exposure events. Existing infections continue to run normally.");
            DrawCheckbox(listing, "Log detected compatibility mods on load", ref verboseCompatibilityLogging, "Writes a short compatibility scan to the RimWorld log after loading. Leave this off unless you are troubleshooting.");

            DrawSectionHeader(listing, "Raid Schedule", "Controls when Marked Men incidents appear and how hard scheduled attacks scale.");
            DrawCheckbox(listing, "Enable scheduled warbands", ref scheduledWarbandsEnabled, "Allows the main timed Marked Men raids that escalate over the colony timeline.");
            DrawCheckbox(listing, "Enable scheduled hordes", ref scheduledHordesEnabled, "Allows larger moving horde events in addition to the main warband schedule.");
            DrawCheckbox(listing, "Enable scouting probes", ref scoutingProbesEnabled, "Allows small scouting packs that test the colony before larger attacks arrive.");
            DrawCheckbox(listing, "Randomize raid timing and arrival patterns", ref randomizeMarkedRaids, "Adds uncertainty to raid intervals and arrival modes. Disable this for predictable testing or calmer pacing.");
            DrawInt(listing, "The appointed time draws near... (days)", ref firstMarkedRaidDay, 1, 600, "firstMarkedRaidDay", "When did the infection begin? The chronometer flickers. The Marked will come when they are ready.");
            DrawFloat(listing, "Global event frequency multiplier", ref markedRaidFrequencyMultiplier, MinMarkedRaidFrequencyMultiplier, MaxMarkedRaidFrequencyMultiplier, "markedRaidFrequencyMultiplier", "Master multiplier for warbands, hordes, and probes. Set this to 0 to stop all scheduled Marked Men incidents.");
            DrawFloat(listing, "Warband frequency multiplier", ref warbandFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "warbandFrequencyMultiplier", "Multiplier for main warband raids after the global multiplier is applied.");
            DrawFloat(listing, "Horde frequency multiplier", ref hordeFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "hordeFrequencyMultiplier", "Multiplier for horde events after the global multiplier is applied.");
            DrawFloat(listing, "Scouting probe frequency multiplier", ref probeFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "probeFrequencyMultiplier", "Multiplier for small probe incidents after the global multiplier is applied.");
            DrawHelp(listing, "Effective frequencies: warbands " + MultiplierText(EffectiveWarbandFrequencyMultiplier) + ", hordes " + MultiplierText(EffectiveHordeFrequencyMultiplier) + ", probes " + MultiplierText(EffectiveProbeFrequencyMultiplier) + ".");
            DrawFloat(listing, "Raid strength multiplier", ref raidPointsMultiplier, 0.05f, 1E+09f, "raidPointsMultiplier", "Scales incident points after the minimum point floor is applied.");
            DrawFloat(listing, "Minimum raid points", ref minimumRaidPoints, 0f, 100000f, "minimumRaidPoints", "Point floor for generated Marked Men attacks. Higher values make even early raids larger.");
            DrawFloat(listing, "Escalation gained per warband", ref raidEscalationPerRaid, 0f, 2f, "raidEscalationPerRaid", "Extra raid strength added after each scheduled warband starts.");
            DrawFloat(listing, "Escalation maximum bonus", ref raidEscalationMaxBonus, 0f, 20f, "raidEscalationMaxBonus", "Maximum accumulated escalation bonus from repeated warbands.");
            DrawCheckbox(listing, "Allow grouped edge arrivals", ref allowGroupedEdgeArrival, "Allows raiders to enter together from one map edge.");
            DrawCheckbox(listing, "Allow split group edge arrivals", ref allowDistributedGroupArrival, "Allows several groups to enter from different edge positions.");
            DrawCheckbox(listing, "Allow scattered edge arrivals", ref allowDistributedArrival, "Allows a wider scattered edge arrival pattern.");
            DrawCheckbox(listing, "Allow single pawn edge arrivals", ref allowSingleEdgeArrival, "Allows single-file edge entry when the incident worker selects it.");

            DrawSectionHeader(listing, "Enemy Mix", "Controls which infected pawn types appear. Weight 0 disables that type; weight 1 is normal; higher values make that type more common.");
            DrawFloat(listing, "Civilian weight", ref civilianWeightMultiplier, 0f, 5f, "civilianWeightMultiplier", "Basic infected with improvised weapons. The most common type.");
            DrawFloat(listing, "Scout weight", ref scoutWeightMultiplier, 0f, 5f, "scoutWeightMultiplier", "Fast reconnaissance infected with light weapons.");
            DrawFloat(listing, "Hunter weight", ref hunterWeightMultiplier, 0f, 5f, "hunterWeightMultiplier", "Tracker infected with hunting weapons.");
            DrawFloat(listing, "Shooter weight", ref shooterWeightMultiplier, 0f, 5f, "shooterWeightMultiplier", "Ranged infected with sidearms and SMGs.");
            DrawFloat(listing, "Raider weight", ref raiderWeightMultiplier, 0f, 5f, "raiderWeightMultiplier", "Former bandits with assault rifles and machetes.");
            DrawFloat(listing, "Soldier weight", ref soldierWeightMultiplier, 0f, 5f, "soldierWeightMultiplier", "Former military infected with military-grade gear.");
            DrawFloat(listing, "Brute weight", ref bruteWeightMultiplier, 0f, 5f, "bruteWeightMultiplier", "Heavy melee infected. Usually appears only when raid points are high enough.");
            DrawFloat(listing, "Pyromaniac weight", ref pyromaniacWeightMultiplier, 0f, 5f, "pyromaniacWeightMultiplier", "Fire-focused infected that spread chaos.");
            DrawFloat(listing, "Alpha weight", ref alphaWeightMultiplier, 0f, 5f, "alphaWeightMultiplier", "Elite command infected that strengthens nearby Marked Men.");
            DrawFloat(listing, "Warlord weight", ref warlordWeightMultiplier, 0f, 5f, "warlordWeightMultiplier", "Regional commander infected. Rare and extremely dangerous.");
            DrawFloat(listing, "Marked Man weight", ref markedManWeightMultiplier, 0f, 5f, "markedManWeightMultiplier", "Ultimate infected. Endgame encounter, extremely rare.");
            DrawInt(listing, "Minimum horde size", ref minimumHordeSize, 1, 50, "minimumHordeSize", "Smallest horde size when a horde incident does not request a specific count.");
            DrawInt(listing, "Maximum horde size", ref maximumHordeSize, 1, 100, "maximumHordeSize", "Largest horde size after threat scaling and variance.");
            DrawInt(listing, "Minimum scouting probe size", ref minimumProbeSize, 1, 20, "minimumProbeSize", "Smallest scouting pack size when the incident does not request a specific count.");
            DrawInt(listing, "Maximum scouting probe size", ref maximumProbeSize, 1, 30, "maximumProbeSize", "Largest scouting pack size after threat scaling and variance.");
            DrawInt(listing, "Maximum alphas per raid", ref maximumAlphasPerRaid, 0, 99, "maximumAlphasPerRaid", "Hard cap for alpha infected in generated raids. Set to 0 to prevent alphas from spawning.");

            DrawSectionHeader(listing, "Virus And Corpses", "Controls exposure chances, infection timing, terminal outcomes, and infected corpse reanimation.");
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

            DrawSectionHeader(listing, "Infection Transmission", "Controls how the Marked Virus spreads through melee combat. Ranged attacks, projectiles, explosions, and turrets never transmit infection.");
            DrawCheckbox(listing, "Enable melee transmission", ref meleeTransmissionEnabled, "Master toggle for all melee-based infection transmission. When disabled, no melee attack can spread the Marked Virus.");
            DrawCheckbox(listing, "Bite attacks transmit", ref biteTransmissionEnabled, "When enabled, bite attacks from infected pawns can transmit the virus.");
            DrawFloat(listing, "Bite infection chance", ref biteInfectionChance, 0f, 1f, "biteInfectionChance", "Chance that a bite attack from an infected pawn transmits the Marked Virus.");
            DrawCheckbox(listing, "Claw attacks transmit", ref clawTransmissionEnabled, "When enabled, claw attacks from infected pawns can transmit the virus.");
            DrawFloat(listing, "Claw infection chance", ref clawInfectionChance, 0f, 1f, "clawInfectionChance", "Chance that a claw or stab attack from an infected pawn transmits the Marked Virus.");
            DrawCheckbox(listing, "Scratch attacks transmit", ref scratchTransmissionEnabled, "When enabled, scratch attacks from infected pawns can transmit the virus.");
            DrawFloat(listing, "Scratch infection chance", ref scratchInfectionChance, 0f, 1f, "scratchInfectionChance", "Chance that a scratch attack from an infected pawn transmits the Marked Virus.");
            DrawCheckbox(listing, "Punch/blunt attacks transmit", ref punchTransmissionEnabled, "When enabled, punch and blunt attacks from infected pawns can transmit the virus.");
            DrawFloat(listing, "Punch infection chance", ref punchInfectionChance, 0f, 1f, "punchInfectionChance", "Chance that a punch or blunt attack from an infected pawn transmits the Marked Virus.");
            DrawCheckbox(listing, "Melee weapon attacks transmit", ref meleeWeaponTransmissionEnabled, "When enabled, melee weapon attacks from infected pawns can transmit the virus.");
            DrawFloat(listing, "Melee weapon infection chance", ref meleeWeaponInfectionChance, 0f, 1f, "meleeWeaponInfectionChance", "Chance that a melee weapon attack from an infected pawn transmits the Marked Virus.");
            DrawCheckbox(listing, "Marked Men guaranteed infection", ref markedMenGuaranteedInfection, "When enabled, fully transformed Marked Men always infect on melee contact.");
            DrawFloat(listing, "Marked Men infection chance", ref markedMenInfectionChance, 0f, 1f, "markedMenInfectionChance", "Chance that a fully transformed Marked Man transmits the virus on melee contact. Used when guaranteed infection is disabled.");

            DrawSectionHeader(listing, "Sealed Apparel Protection", "Toggling a category off removes full viral immunity for that apparel type, reverting to partial resistance instead.");
            DrawCheckbox(listing, "Warcaskets block exposure", ref warcasketsBlockExposure, "When enabled, any warcasket torso shell provides full immunity to direct Marked Virus exposure. Disable this if you want warcasket pawns to still face infection risk.");
            DrawCheckbox(listing, "Vacsuit/EVA suits block exposure", ref vacsuitBlockExposure, "When enabled, wearing both a vacsuit body and helmet provides full immunity via the sealed combo. Disable to make the combo only give partial resistance.");
            DrawCheckbox(listing, "Gas masks block exposure", ref gasMasksBlockExposure, "When enabled, gas masks, HAZMAT masks, and toxin-immune headgear provide full immunity. Disable for partial resistance only.");
            DrawCheckbox(listing, "Sealed armor blocks exposure", ref sealedArmorBlockExposure, "When enabled, HAZMAT suits, sealed undersuits, security armor, and orbital armor provide full immunity. Disable for partial resistance only.");

            DrawSectionHeader(listing, "Infected AI", "Controls how aggressively Marked Men attack, retarget, breach, and terrorize nearby pawns.");
            DrawCheckbox(listing, "Enable tactical retargeting", ref tacticalRetargetingEnabled, "Lets infected pawns periodically switch to better tactical targets.");
            DrawCheckbox(listing, "Enable priority targeting", ref priorityTargetingEnabled, "Lets infected pawns prefer power, food, medical, research, and turret targets when appropriate.");
            DrawCheckbox(listing, "Enable door and wall targeting", ref doorTargetingEnabled, "Allows infected pawns to bash or target barriers when pursuing a colony.");
            DrawFloat(listing, "Marked infighting chance", ref infightingChance, 0f, 1f, "infightingChance", "Chance during each infighting check that infected pawns may turn on each other.");
            DrawFloat(listing, "Panic and social terror strength", ref socialTerrorStrength, 0f, 5f, "socialTerrorStrength", "Scales the radius and strength of Marked Men terror effects. Set to 0 to disable these effects.");

            DrawSectionHeader(listing, "Predatory Instincts", "Controls the bloodlust, kill anticipation, and predator psychology systems for the Marked Ones.");
            DrawCheckbox(listing, "Enable bloodlust system", ref bloodlustEnabled, "When enabled, infected pawns build bloodlust over time and gain mood and combat effects based on their craving for violence.");
            DrawFloat(listing, "Bloodlust decay rate", ref bloodlustDecayRate, 0.1f, 5f, "bloodlustDecayRate", "How quickly bloodlust fades when not in combat. Lower values keep pawns bloodthirsty longer.");
            DrawFloat(listing, "Bloodlust kill gain multiplier", ref bloodlustKillGainMultiplier, 0.1f, 5f, "bloodlustKillGainMultiplier", "How much a kill or down satisfies bloodlust. Higher values mean faster satiation.");
            DrawFloat(listing, "Bloodlust combat gain multiplier", ref bloodlustCombatGainMultiplier, 0f, 5f, "bloodlustCombatGainMultiplier", "How much active combat feeds bloodlust. Set to 0 to disable combat-based bloodlust gain.");
            DrawCheckbox(listing, "Enable kill anticipation system", ref anticipationEnabled, "When enabled, infected pawns gain combat bonuses from anticipating enemies nearby. Effects fade quickly when out of combat.");
            DrawFloat(listing, "Anticipation gain multiplier", ref anticipationGainMultiplier, 0.1f, 5f, "anticipationGainMultiplier", "How quickly kill anticipation builds when enemies are near.");
            DrawFloat(listing, "Anticipation decay multiplier", ref anticipationDecayMultiplier, 0.1f, 5f, "anticipationDecayMultiplier", "How quickly kill anticipation fades after combat ends. Higher values fade faster.");

            DrawSectionHeader(listing, "Messages And Dev Tools", "Controls player-facing alerts, incident history, and optional debug actions.");
            DrawCheckbox(listing, "Show raid countdown alert", ref raidCountdownAlertEnabled, "Shows a gizmo alert when a scheduled Marked Men raid is approaching.");
            DrawFloat(listing, "Countdown visible days", ref raidCountdownVisibleDays, 0f, 999f, "raidCountdownVisibleDays", "How many in-game days before a scheduled raid the countdown alert becomes visible.");
            DrawFloat(listing, "High-priority countdown days", ref raidCountdownHighPriorityDays, 0f, 30f, "raidCountdownHighPriorityDays", "How many in-game days before a scheduled raid the alert becomes high priority.");
            DrawCheckbox(listing, "Use detailed raid letters", ref detailedRaidLetters, "Adds richer raid letter text with pawn counts, points, arrival mode, and tactical warning details.");
            DrawCheckbox(listing, "Record incident log entries", ref incidentLogEnabled, "Stores Marked Men incident history in the game component for debugging and future review.");
            DrawCheckbox(listing, "Enable Dev Mode debug actions", ref debugActionsEnabled, "Adds Dev Mode actions for starting or rescheduling Marked Men incidents while testing.");

            DrawSectionHeader(listing, "Performance", "Controls how often background systems run. Higher intervals reduce CPU work but make reactions less immediate.");
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

            DrawSectionHeader(listing, "Ancient Urban Ruins Outbreak", "Controls Marked Virus outbreaks in Ancient Urban Ruins cities. Only applies when the Ancient Urban Ruins mod is active.");
            DrawCheckbox(listing, "Enable urban outbreaks", ref urbanOutbreaksEnabled, "Toggles whether Ancient Urban Ruins maps become active outbreak zones with infected Marked Men roaming the ruins.");
            DrawFloat(listing, "Urban infection density", ref urbanInfectionDensity, 0f, 5f, "urbanInfectionDensity", "Controls how many buildings become infected and how many Marked Men spawn per building. Higher values mean denser urban populations.");
            DrawFloat(listing, "Urban ambush frequency", ref urbanAmbushFrequency, 0f, 5f, "urbanAmbushFrequency", "How often Marked Men ambushes occur inside the ruins. Higher values mean more frequent attacks.");
            DrawCheckbox(listing, "Enable dormant infestations", ref dormantInfestationsEnabled, "When enabled, some infected buildings start dormant. They burst open with Marked Men when colonists approach.");
            DrawFloat(listing, "Dormant infestation frequency", ref dormantInfestationFrequency, 0f, 5f, "dormantInfestationFrequency", "How many buildings start with dormant infestations waiting to ambush approaching colonists.");
            DrawCheckbox(listing, "Enable city epicenters", ref cityEpicentersEnabled, "When enabled, some urban ruin maps become high-density epicenters with stronger, more frequent spawns and a world-object reinforcement comp.");
            DrawFloat(listing, "Epicenter spawn chance", ref epicenterSpawnChance, 0f, 1f, "epicenterSpawnChance", "Chance that a given urban ruins map becomes an epicenter with doubled initial population and periodic reinforcements.");
            DrawCheckbox(listing, "Enable urban ambush incidents", ref urbanAmbushesEnabled, "Allows incident-driven ambush events on Ancient Urban Ruins maps in addition to map-component ambushes.");
            DrawCheckbox(listing, "Enable survivor encounters", ref survivorEncountersEnabled, "When enabled, survivor encounters in the ruins may be genuine survivors, hidden infected, or traps leading to infestations.");
            DrawFloat(listing, "Survivor encounter chance modifier", ref survivorEncounterChance, 0f, 1f, "survivorEncounterChance", "Multiplier for survivor encounter incident frequency in urban ruins. Higher values mean more survivor events.");

            DrawSectionHeader(listing, "AUR Spawn Protection", "Controls enemy spawn positions on Ancient Urban Ruins maps. Prevents enemies from appearing too close to colonists by redirecting their spawn positions to map edges or a minimum distance away.");
            DrawCheckbox(listing, "Enable spawn distance protection", ref aurSpawnPatchEnabled, "When enabled, enemies spawning on Ancient Urban Ruins maps are placed at a safer minimum distance from colonists, animals, and mechs. Only applies when Ancient Urban Ruins is loaded.");
            DrawFloat(listing, "Minimum spawn distance", ref aurMinimumSpawnDistance, 10f, 100f, "aurMinimumSpawnDistance", "Minimum distance in cells that enemies must be kept from any colonist, animal, or mech. Higher values give more reaction time but may reduce valid spawn positions.");
            DrawCheckbox(listing, "Prefer map edge spawning", ref aurPreferEdgeSpawn, "When enabled, the patch tries to spawn enemies from map edges first. Falls back to distance-based placement if all edge cells are blocked by ruins or mountains.");
            DrawCheckbox(listing, "Debug logging for spawn protection", ref aurSpawnPatchDebugLogging, "Writes detailed spawn protection debug messages to the RimWorld log. Useful for troubleshooting spawn placement issues.");

            DrawSectionHeader(listing, "Lost Survivors", "Controls the Lost Survivor incident. A seemingly normal colonist joins but carries a dormant Marked Virus infection that may activate days or weeks later with unpredictable consequences.");
            DrawCheckbox(listing, "Enable Lost Survivor incidents", ref lostSurvivorEnabled, "When enabled, the storyteller can send a lost survivor carrying a dormant Marked Virus infection. The survivor appears normal until the dormant mark activates.");
            DrawFloat(listing, "Lost Survivor frequency", ref lostSurvivorFrequencyMultiplier, 0f, 5f, "lostSurvivorFrequencyMultiplier", "Multiplier for how often Lost Survivor incidents occur. Set to 0 to disable.");
            DrawFloat(listing, "Min dormant days", ref dormantMarkMinDays, 1f, 60f, "dormantMarkMinDays", "Minimum in-game days before the dormant mark can activate.");
            DrawFloat(listing, "Max dormant days", ref dormantMarkMaxDays, 1f, 120f, "dormantMarkMaxDays", "Maximum in-game days before the dormant mark will activate.");
            DrawFloat(listing, "Trigger sensitivity", ref dormantMarkTriggerMultiplier, 0f, 5f, "dormantMarkTriggerMultiplier", "Multiplier for trigger chances (combat damage, near-death, witnessing other transformations, Crossed signal proximity). Higher values make activation more likely.");
            DrawFloat(listing, "Alpha variant chance", ref dormantMarkAlphaChance, 0f, 1f, "dormantMarkAlphaChance", "Chance that the transformed survivor is an Alpha variant (spawns escorting Crossed on activation). Doubled for prisoner survivors.");
            DrawFloat(listing, "Group variant chance", ref dormantMarkGroupVariantChance, 0f, 1f, "dormantMarkGroupVariantChance", "Chance that multiple dormant carriers activate simultaneously. Set to 0 to disable group activations.");

            DrawSectionHeader(listing, "Marked Prisoners", "Controls how the Marked Virus behaves in captured prisoners. Marked prisoners cannot be recruited, will attack wardens, harm themselves over time, and escape aggressively.");
            DrawCheckbox(listing, "Enable prisoner infection system", ref prisonerInfectionEnabled, "When enabled, Marked prisoners are unrecruitable, attack wardens during interaction, progress through self-harm stages, and escape with aggression.");
            DrawFloat(listing, "Infection chance per warden interaction", ref prisonerInfectionChance, 0f, 1f, "prisonerInfectionChance", "Chance per warden interaction that a Marked prisoner attacks and tries to infect the warden.");
            DrawCheckbox(listing, "Enable self-harm behavior", ref prisonerSelfHarmEnabled, "When enabled, Marked prisoners progressively harm themselves over time, culminating in suicide.");
            DrawFloat(listing, "Days before self-harm stage progression", ref prisonerSelfHarmStageDays, 1f, 60f, "prisonerSelfHarmStageDays", "How many in-game days between self-harm stages. Each stage inflicts worse damage.");
            DrawFloat(listing, "Days before suicide", ref prisonerSelfHarmSuicideDays, 1f, 90f, "prisonerSelfHarmSuicideDays", "How many in-game days before a Marked prisoner attempts suicide with severe self-mutilation.");
            DrawFloat(listing, "Escape aggression multiplier", ref prisonerEscapeAggressionMultiplier, 0f, 5f, "prisonerEscapeAggressionMultiplier", "How aggressively Marked prisoners attack during prison breaks. Higher values mean they target more distant priority targets.");
            DrawCheckbox(listing, "Enable cosmetic behaviors", ref prisonerCosmeticEnabled, "When enabled, Marked prisoners pace, growl, scream, and pound walls. Visual only, no mechanical effects.");
            DrawCheckbox(listing, "Debug logging for prisoner system", ref prisonerDebugLogging, "Writes prisoner infection system debug messages to the RimWorld log.");

            DrawSectionHeader(listing, "Optional RimJobWorld Bridge", "Only applies when RimJobWorld is installed. The bridge adds no hard dependency.");
            DrawHelp(listing, "RimJobWorld detected right now: " + (TheMarkedMenRjwCompatibility.IsRjwLoaded() ? "yes" : "no") + ".");
            DrawCheckbox(listing, "Auto-enable the RimJobWorld bridge when detected", ref rjwAutoEnableWhenInstalled, "Automatically turns on the bridge after RimJobWorld is found in the active mod list.");
            DrawCheckbox(listing, "Enable RimJobWorld Marked Virus bridge", ref rjwIntegrationEnabled, "Allows adult RJW close-contact events to transmit Marked Virus and lets valid infected adults use RJW enemy assault jobs.");
            DrawFloat(listing, "RimJobWorld exposure chance", ref rjwExposureChance, 0f, 1f, "rjwExposureChance", "Chance that a valid RJW close-contact event involving one infected pawn exposes the other pawn.");
        }

        public static float ApplyRaidPointSettings(float points)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null)
            {
                return Mathf.Max(120f, points);
            }

            return Mathf.Max(0f, Mathf.Max(points, settings.minimumRaidPoints) * settings.raidPointsMultiplier);
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
            if (kind == CADefOf.CrossedCivilian)
            {
                return civilianWeightMultiplier;
            }
            if (kind == CADefOf.CrossedScout)
            {
                return scoutWeightMultiplier;
            }
            if (kind == CADefOf.CrossedHunter)
            {
                return hunterWeightMultiplier;
            }
            if (kind == CADefOf.CrossedShooter)
            {
                return shooterWeightMultiplier;
            }
            if (kind == CADefOf.CrossedRaider)
            {
                return raiderWeightMultiplier;
            }
            if (kind == CADefOf.CrossedSoldier)
            {
                return soldierWeightMultiplier;
            }
            if (kind == CADefOf.CrossedBrute)
            {
                return bruteWeightMultiplier;
            }
            if (kind == CADefOf.CrossedPyromaniac)
            {
                return pyromaniacWeightMultiplier;
            }
            if (kind == CADefOf.CrossedAlpha)
            {
                return alphaWeightMultiplier;
            }
            if (kind == CADefOf.CrossedWarlord)
            {
                return warlordWeightMultiplier;
            }
            if (kind == CADefOf.MarkedMan)
            {
                return markedManWeightMultiplier;
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
                cachedContentHeight = 0f;
            }

            TooltipHandler.TipRegion(rect, new TipSignal(tooltip));
        }

        private void DrawSectionHeader(Listing_Standard listing, string title, string description)
        {
            listing.Gap(10f);
            listing.GapLine(6f);
            bool open = IsSectionOpen(title);
            Rect row = listing.GetRect(SectionHeaderHeight, 1f);
            bool hover = Mouse.IsOver(row);
            Widgets.DrawBoxSolid(row, hover ? SectionHeaderHoverColor : SectionHeaderColor);
            Widgets.DrawBox(row, 1, null);

            Rect toggleRect = new Rect(row.x + 4f, row.y + 4f, SectionToggleWidth, row.height - 8f);
            Widgets.DrawBoxSolid(toggleRect, hover ? SectionToggleHoverColor : SectionToggleColor);
            Widgets.DrawBox(toggleRect, 1, null);

            Rect titleRect = new Rect(toggleRect.xMax + 10f, row.y + 4f, row.width - SectionToggleWidth - 22f, row.height - 8f);

            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            TextAnchor oldAnchor = Text.Anchor;
            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(toggleRect, open ? "[-]" : "[+]");
            Text.Anchor = TextAnchor.MiddleLeft;
            DrawFittedSectionTitle(titleRect, title);
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor;

            if (Widgets.ButtonInvisible(row, true))
            {
                sectionOpenStates[title] = !open;
                open = !open;
                cachedContentHeight = 0f;
            }

            TooltipHandler.TipRegion(row, new TipSignal((open ? "Click to collapse. " : "Click to expand. ") + description));
            currentSectionOpen = open;
            if (open)
            {
                DrawHelpTextInternal(listing, description);
            }
        }

        private static void DrawFittedSectionTitle(Rect rect, string title)
        {
            GameFont oldFont = Text.Font;
            Text.Font = GameFont.Medium;
            if (Text.CalcSize(title).x > rect.width)
            {
                Text.Font = GameFont.Small;
            }

            Widgets.Label(rect, title);
            Text.Font = oldFont;
        }

        private void DrawHelp(Listing_Standard listing, string text)
        {
            if (!currentSectionOpen)
            {
                return;
            }

            DrawHelpTextInternal(listing, text);
        }

        private void DrawHelpTextInternal(Listing_Standard listing, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            GUI.color = HelpTextColor;
            Text.Font = GameFont.Small;
            listing.Label(text);
            Text.Font = oldFont;
            GUI.color = oldColor;
            listing.Gap(2f);
        }

        private void DrawCheckbox(Listing_Standard listing, string label, ref bool value, string help)
        {
            if (!currentSectionOpen)
            {
                return;
            }

            bool before = value;
            Rect row = listing.GetRect(OptionRowHeight, 1f);
            Widgets.CheckboxLabeled(row, label, ref value, false, null, null, false);
            TooltipHandler.TipRegion(row, new TipSignal(help));
            if (before != value)
            {
                NoteManualChange();
            }

            DrawHelp(listing, help);
        }

        private void DrawFloat(Listing_Standard listing, string label, ref float value, float min, float max, string key, string help)
        {
            if (!currentSectionOpen)
            {
                return;
            }

            float before = value;
            string buffer = GetBuffer(key);
            Rect row = listing.GetRect(OptionRowHeight, 1f);
            Widgets.TextFieldNumericLabeled(row, label, ref value, ref buffer, min, max);
            TooltipHandler.TipRegion(row, new TipSignal(help + "\nCurrent value: " + FloatValueText(value, min, max) + "."));
            numericBuffers[key] = buffer;
            if (!Mathf.Approximately(before, value))
            {
                NoteManualChange();
            }

            DrawHelp(listing, help + " Current value: " + FloatValueText(value, min, max) + ".");
        }

        private void DrawInt(Listing_Standard listing, string label, ref int value, int min, int max, string key, string help)
        {
            if (!currentSectionOpen)
            {
                return;
            }

            int before = value;
            string buffer = GetBuffer(key);
            Rect row = listing.GetRect(OptionRowHeight, 1f);
            Widgets.TextFieldNumericLabeled(row, label, ref value, ref buffer, min, max);
            TooltipHandler.TipRegion(row, new TipSignal(help + "\nCurrent value: " + IntValueText(value, max) + "."));
            numericBuffers[key] = buffer;
            if (before != value)
            {
                NoteManualChange();
            }

            DrawHelp(listing, help + " Current value: " + IntValueText(value, max) + ".");
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

        private bool IsSectionOpen(string title)
        {
            if (sectionOpenStates == null)
            {
                sectionOpenStates = new Dictionary<string, bool>();
            }

            if (!sectionOpenStates.TryGetValue(title, out bool open))
            {
                open = true;
                sectionOpenStates[title] = true;
            }

            return open;
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
            raidPointsMultiplier = Mathf.Max(0.05f, raidPointsMultiplier);
            minimumRaidPoints = Mathf.Clamp(minimumRaidPoints, 0f, 100000f);
            raidEscalationPerRaid = Mathf.Clamp(raidEscalationPerRaid, 0f, 2f);
            raidEscalationMaxBonus = Mathf.Clamp(raidEscalationMaxBonus, 0f, 20f);
            civilianWeightMultiplier = Mathf.Clamp(civilianWeightMultiplier, 0f, 5f);
            scoutWeightMultiplier = Mathf.Clamp(scoutWeightMultiplier, 0f, 5f);
            hunterWeightMultiplier = Mathf.Clamp(hunterWeightMultiplier, 0f, 5f);
            shooterWeightMultiplier = Mathf.Clamp(shooterWeightMultiplier, 0f, 5f);
            raiderWeightMultiplier = Mathf.Clamp(raiderWeightMultiplier, 0f, 5f);
            soldierWeightMultiplier = Mathf.Clamp(soldierWeightMultiplier, 0f, 5f);
            bruteWeightMultiplier = Mathf.Clamp(bruteWeightMultiplier, 0f, 5f);
            pyromaniacWeightMultiplier = Mathf.Clamp(pyromaniacWeightMultiplier, 0f, 5f);
            alphaWeightMultiplier = Mathf.Clamp(alphaWeightMultiplier, 0f, 5f);
            warlordWeightMultiplier = Mathf.Clamp(warlordWeightMultiplier, 0f, 5f);
            markedManWeightMultiplier = Mathf.Clamp(markedManWeightMultiplier, 0f, 5f);
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
            biteInfectionChance = Mathf.Clamp01(biteInfectionChance);
            clawInfectionChance = Mathf.Clamp01(clawInfectionChance);
            scratchInfectionChance = Mathf.Clamp01(scratchInfectionChance);
            punchInfectionChance = Mathf.Clamp01(punchInfectionChance);
            meleeWeaponInfectionChance = Mathf.Clamp01(meleeWeaponInfectionChance);
            markedMenInfectionChance = Mathf.Clamp01(markedMenInfectionChance);
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
            urbanInfectionDensity = Mathf.Clamp(urbanInfectionDensity, 0f, 5f);
            urbanAmbushFrequency = Mathf.Clamp(urbanAmbushFrequency, 0f, 5f);
            dormantInfestationFrequency = Mathf.Clamp(dormantInfestationFrequency, 0f, 5f);
            epicenterSpawnChance = Mathf.Clamp01(epicenterSpawnChance);
            survivorEncounterChance = Mathf.Clamp01(survivorEncounterChance);
            aurMinimumSpawnDistance = Mathf.Clamp(aurMinimumSpawnDistance, 10f, 100f);
            prisonerInfectionChance = Mathf.Clamp01(prisonerInfectionChance);
            prisonerSelfHarmStageDays = Mathf.Clamp(prisonerSelfHarmStageDays, 1f, 60f);
            prisonerSelfHarmSuicideDays = Mathf.Clamp(prisonerSelfHarmSuicideDays, 1f, 90f);
            prisonerEscapeAggressionMultiplier = Mathf.Clamp(prisonerEscapeAggressionMultiplier, 0f, 5f);
        }

        private void ApplyDefaultPreset(bool updatePreset)
        {
            ApplyOutbreakDefaults(updatePreset);
        }

        private void ApplyBaselinePreset(bool updatePreset)
        {
            scheduledWarbandsEnabled = true;
            scheduledHordesEnabled = true;
            scoutingProbesEnabled = true;
            randomizeMarkedRaids = false;
            markedRaidFrequencyMultiplier = DefaultMarkedRaidFrequencyMultiplier;
            warbandFrequencyMultiplier = 1f;
            hordeFrequencyMultiplier = 1f;
            probeFrequencyMultiplier = 1f;
            firstMarkedRaidDay = 45;
            raidPointsMultiplier = 1f;
            minimumRaidPoints = 9000f;
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
            warcasketsBlockExposure = true;
            vacsuitBlockExposure = true;
            gasMasksBlockExposure = true;
            sealedArmorBlockExposure = true;
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
            ApplyBaselinePreset(false);
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
            meleeTransmissionEnabled = true;
            biteTransmissionEnabled = true;
            clawTransmissionEnabled = true;
            scratchTransmissionEnabled = true;
            punchTransmissionEnabled = true;
            meleeWeaponTransmissionEnabled = true;
            biteInfectionChance = 0.25f;
            clawInfectionChance = 0.25f;
            scratchInfectionChance = 0.25f;
            punchInfectionChance = 0.25f;
            meleeWeaponInfectionChance = 0.25f;
            markedMenInfectionChance = 1f;
            markedMenGuaranteedInfection = true;
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
            ClearNumericBuffers();
        }

        private void ApplyVanillaLikePreset()
        {
            ApplyBaselinePreset(false);
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
            ApplyBaselinePreset(false);
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
            warlordWeightMultiplier = 1.3f;
            bruteWeightMultiplier = 1.4f;
            bloodExposureChance = 0.65f;
            infectedAssaultExposureChance = 0.65f;
            meleeTransmissionEnabled = true;
            biteTransmissionEnabled = true;
            clawTransmissionEnabled = true;
            scratchTransmissionEnabled = true;
            punchTransmissionEnabled = true;
            meleeWeaponTransmissionEnabled = true;
            biteInfectionChance = 0.65f;
            clawInfectionChance = 0.65f;
            scratchInfectionChance = 0.65f;
            punchInfectionChance = 0.65f;
            meleeWeaponInfectionChance = 0.65f;
            markedMenInfectionChance = 1f;
            markedMenGuaranteedInfection = true;
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
            ClearNumericBuffers();
        }

        private void ApplyOutbreakPreset()
        {
            ApplyOutbreakDefaults(true);
        }

        private void ApplyOutbreakDefaults(bool updatePreset)
        {
            ApplyBaselinePreset(false);
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
            civilianWeightMultiplier = 1.5f;
            hunterWeightMultiplier = 0.75f;
            bruteWeightMultiplier = 0.8f;
            alphaWeightMultiplier = 0.6f;
            bloodExposureChance = 0.8f;
            foodExposureChance = 0.7f;
            infectedAssaultExposureChance = 0.8f;
            meleeTransmissionEnabled = true;
            biteTransmissionEnabled = true;
            clawTransmissionEnabled = true;
            scratchTransmissionEnabled = true;
            punchTransmissionEnabled = true;
            meleeWeaponTransmissionEnabled = true;
            biteInfectionChance = 0.8f;
            clawInfectionChance = 0.8f;
            scratchInfectionChance = 0.8f;
            punchInfectionChance = 0.8f;
            meleeWeaponInfectionChance = 0.8f;
            markedMenInfectionChance = 1f;
            markedMenGuaranteedInfection = true;
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
            if (!updatePreset)
            {
                currentPreset = "Outbreak simulator";
            }

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
            civilianWeightMultiplier = 1f;
            scoutWeightMultiplier = 1f;
            hunterWeightMultiplier = 1f;
            shooterWeightMultiplier = 1f;
            raiderWeightMultiplier = 1f;
            soldierWeightMultiplier = 1f;
            bruteWeightMultiplier = 1f;
            pyromaniacWeightMultiplier = 1f;
            alphaWeightMultiplier = 1f;
            warlordWeightMultiplier = 1f;
            markedManWeightMultiplier = 1f;
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
            detailedRaidLetters = false;
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
        private static HediffDef psychicAura;
        private static ThoughtDef crossedSocialTerror;
        private static FactionDef crossedFaction;
        private static InteractionDef crossedPredatoryTaunt;
        private static InteractionDef crossedFalseMercy;
        private static InteractionDef crossedPackLaughter;
        private static InteractionDef crossedInfectionGloat;
        private static PawnKindDef crossedCivilian;
        private static PawnKindDef crossedScout;
        private static PawnKindDef crossedHunter;
        private static PawnKindDef crossedShooter;
        private static PawnKindDef crossedRaider;
        private static PawnKindDef crossedSoldier;
        private static PawnKindDef crossedBrute;
        private static PawnKindDef crossedPyromaniac;
        private static PawnKindDef crossedAlpha;
        private static PawnKindDef crossedWarlord;
        private static PawnKindDef markedMan;
        private static TattooDef crossedFaceTattoo;
        private static TattooDef crossedFaceTattooStage1;
        private static TattooDef crossedFaceTattooStage2;
        private static TattooDef crossedFaceTattooStage3;
        private static TattooDef crossedFaceTattooStage4;
        private static IncidentDef crossedRaid;
        private static IncidentDef crossedHorde;
        private static IncidentDef crossedProbe;
        private static IncidentDef crossedCaravanAmbush;
        private static IncidentDef urbanAmbush;
        private static IncidentDef urbanSurvivor;
        private static XenotypeDef markedOne;
        private static StatDef markedVirusResistance;
        private static HediffDef killAnticipation;
        private static NeedDef markedBloodlustNeed;
        private static GeneDef markedBloodlustGene;
        private static ThoughtDef freshKillSatisfaction;
        private static ThoughtDef bloodthirstyCraving;
        private static ThoughtDef overwhelmingBloodlust;
        private static ThoughtDef predatorPatience;
        private static ThoughtDef witnessedCrossedTransformation;
        private static HediffDef dormantMark;
        private static HediffDef crossedRampage;
        private static HediffDef crossedStrength;
        private static HediffDef warlordTier;
        private static HediffDef alphaTier;
        private static HediffDef markedTier;
        private static IncidentDef lostSurvivor;
        private static ThoughtDef betrayedByColonist;
        private static ThoughtDef betrayedByColonistSocial;
        private static ThoughtDef lovedOneTurned;
        private static ThoughtDef suspiciousOfColonists;
        private static ThoughtDef mercifulKill;

        public static HediffDef CrossVirus => crossVirus ?? (crossVirus = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossVirus"));
        public static HediffDef CrossVirusImmunity => crossVirusImmunity ?? (crossVirusImmunity = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossVirusImmunity"));
        public static HediffDef CrossedRash => crossedRash ?? (crossedRash = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedRash"));
        public static HediffDef BloodRush => bloodRush ?? (bloodRush = DefDatabase<HediffDef>.GetNamedSilentFail("CA_BloodRush"));
        public static HediffDef CommandAura => commandAura ?? (commandAura = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CommandAura"));
        public static HediffDef Panic => panic ?? (panic = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedPanic"));
        public static HediffDef PsychicAura => psychicAura ?? (psychicAura = DefDatabase<HediffDef>.GetNamedSilentFail("CA_PsychicAura"));
        public static ThoughtDef CrossedSocialTerror => crossedSocialTerror ?? (crossedSocialTerror = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_CrossedSocialTerror"));
        public static ThoughtDef CA_WitnessedCrossedTransformation => witnessedCrossedTransformation ?? (witnessedCrossedTransformation = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_WitnessedCrossedTransformation"));
        public static FactionDef CrossedFaction => crossedFaction ?? (crossedFaction = DefDatabase<FactionDef>.GetNamedSilentFail("CA_CrossedFaction"));
        public static InteractionDef CrossedPredatoryTaunt => crossedPredatoryTaunt ?? (crossedPredatoryTaunt = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedPredatoryTaunt"));
        public static InteractionDef CrossedFalseMercy => crossedFalseMercy ?? (crossedFalseMercy = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedFalseMercy"));
        public static InteractionDef CrossedPackLaughter => crossedPackLaughter ?? (crossedPackLaughter = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedPackLaughter"));
        public static InteractionDef CrossedInfectionGloat => crossedInfectionGloat ?? (crossedInfectionGloat = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedInfectionGloat"));
        public static PawnKindDef CrossedCivilian => crossedCivilian ?? (crossedCivilian = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedCivilian"));
        public static PawnKindDef CrossedScout => crossedScout ?? (crossedScout = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedScout"));
        public static PawnKindDef CrossedHunter => crossedHunter ?? (crossedHunter = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedHunter"));
        public static PawnKindDef CrossedShooter => crossedShooter ?? (crossedShooter = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedShooter"));
        public static PawnKindDef CrossedRaider => crossedRaider ?? (crossedRaider = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedRaider"));
        public static PawnKindDef CrossedSoldier => crossedSoldier ?? (crossedSoldier = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedSoldier"));
        public static PawnKindDef CrossedBrute => crossedBrute ?? (crossedBrute = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedBrute"));
        public static PawnKindDef CrossedPyromaniac => crossedPyromaniac ?? (crossedPyromaniac = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedPyromaniac"));
        public static PawnKindDef CrossedAlpha => crossedAlpha ?? (crossedAlpha = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedAlpha"));
        public static PawnKindDef CrossedWarlord => crossedWarlord ?? (crossedWarlord = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedWarlord"));
        public static PawnKindDef MarkedMan => markedMan ?? (markedMan = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_MarkedMan"));
        public static TattooDef CrossedFaceTattoo => crossedFaceTattoo ?? (crossedFaceTattoo = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRash"));
        public static TattooDef CrossedFaceTattooStage1 => crossedFaceTattooStage1 ?? (crossedFaceTattooStage1 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage1"));
        public static TattooDef CrossedFaceTattooStage2 => crossedFaceTattooStage2 ?? (crossedFaceTattooStage2 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage2"));
        public static TattooDef CrossedFaceTattooStage3 => crossedFaceTattooStage3 ?? (crossedFaceTattooStage3 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage3"));
        public static TattooDef CrossedFaceTattooStage4 => crossedFaceTattooStage4 ?? (crossedFaceTattooStage4 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage4"));
        public static IncidentDef CrossedRaid => crossedRaid ?? (crossedRaid = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedRaid"));
        public static IncidentDef CrossedHorde => crossedHorde ?? (crossedHorde = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedHorde"));
        public static IncidentDef CrossedProbe => crossedProbe ?? (crossedProbe = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedProbe"));
        public static IncidentDef CrossedCaravanAmbush => crossedCaravanAmbush ?? (crossedCaravanAmbush = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedCaravanAmbush"));
        public static IncidentDef UrbanAmbush => urbanAmbush ?? (urbanAmbush = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_UrbanAmbush"));
        public static IncidentDef UrbanSurvivor => urbanSurvivor ?? (urbanSurvivor = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_UrbanSurvivor"));
        public static XenotypeDef MarkedOne => markedOne ?? (markedOne = DefDatabase<XenotypeDef>.GetNamedSilentFail("CA_MarkedOne"));
        public static StatDef MarkedVirusResistance => markedVirusResistance ?? (markedVirusResistance = DefDatabase<StatDef>.GetNamedSilentFail("CA_MarkedVirusResistance"));
        public static HediffDef KillAnticipation => killAnticipation ?? (killAnticipation = DefDatabase<HediffDef>.GetNamedSilentFail("CA_KillAnticipation"));
        public static NeedDef MarkedBloodlustNeed => markedBloodlustNeed ?? (markedBloodlustNeed = DefDatabase<NeedDef>.GetNamedSilentFail("CA_MarkedBloodlustNeed"));
        public static GeneDef MarkedBloodlustGene => markedBloodlustGene ?? (markedBloodlustGene = DefDatabase<GeneDef>.GetNamedSilentFail("CA_MarkedBloodlustNeed"));
        public static ThoughtDef FreshKillSatisfaction => freshKillSatisfaction ?? (freshKillSatisfaction = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_FreshKillSatisfaction"));
        public static ThoughtDef BloodthirstyCraving => bloodthirstyCraving ?? (bloodthirstyCraving = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_BloodthirstyCraving"));
        public static ThoughtDef OverwhelmingBloodlust => overwhelmingBloodlust ?? (overwhelmingBloodlust = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_OverwhelmingBloodlust"));
        public static ThoughtDef PredatorPatience => predatorPatience ?? (predatorPatience = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_PredatorPatience"));
        public static HediffDef CA_DormantMark => dormantMark ?? (dormantMark = DefDatabase<HediffDef>.GetNamedSilentFail("CA_DormantMark"));
        public static HediffDef CrossedRampage => crossedRampage ?? (crossedRampage = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedRampage"));
        public static HediffDef CrossedStrength => crossedStrength ?? (crossedStrength = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedStrength"));
        public static HediffDef WarlordTier => warlordTier ?? (warlordTier = DefDatabase<HediffDef>.GetNamedSilentFail("CA_WarlordTier"));
        public static HediffDef AlphaTier => alphaTier ?? (alphaTier = DefDatabase<HediffDef>.GetNamedSilentFail("CA_AlphaTier"));
        public static HediffDef MarkedTier => markedTier ?? (markedTier = DefDatabase<HediffDef>.GetNamedSilentFail("CA_MarkedTier"));
        public static IncidentDef CA_LostSurvivor => lostSurvivor ?? (lostSurvivor = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_LostSurvivor"));
        public static ThoughtDef CA_BetrayedByColonist => betrayedByColonist ?? (betrayedByColonist = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_BetrayedByColonist"));
        public static ThoughtDef CA_BetrayedByColonistSocial => betrayedByColonistSocial ?? (betrayedByColonistSocial = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_BetrayedByColonistSocial"));
        public static ThoughtDef CA_LovedOneTurned => lovedOneTurned ?? (lovedOneTurned = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_LovedOneTurned"));
        public static ThoughtDef CA_SuspiciousOfColonists => suspiciousOfColonists ?? (suspiciousOfColonists = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_SuspiciousOfColonists"));
        public static ThoughtDef CA_MercifulKill => mercifulKill ?? (mercifulKill = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_MercifulKill"));

        public static bool IsCrossedFaceTattoo(TattooDef tattoo)
        {
            return tattoo != null
                && (tattoo == CrossedFaceTattoo
                    || tattoo == CrossedFaceTattooStage1
                    || tattoo == CrossedFaceTattooStage2
                    || tattoo == CrossedFaceTattooStage3
                    || tattoo == CrossedFaceTattooStage4);
        }
    }

    public static class MarkedIdeologyUtility
    {
        private const string LogPrefix = "[The Marked Men] ";
        private const string FixedIconDefName = "Skull";
        private const string FixedColorDefName = "Red";
        private const string FallbackStyleCategoryDefName = "Morbid";

        private static readonly string[] FallbackMemeDefNames =
        {
            "Structure_Ideological",
            "Cannibal",
            "Supremacist",
            "Raider"
        };

        public static void NormalizeMarkedOneIdeology()
        {
            if (!ModsConfig.IdeologyActive)
            {
                return;
            }

            try
            {
                FactionDef factionDef = CADefOf.CrossedFaction;
                Faction faction = factionDef == null ? null : Find.FactionManager?.FirstFactionOfDef(factionDef);
                Ideo ideo = faction?.ideos?.PrimaryIdeo;
                if (factionDef == null || ideo == null)
                {
                    return;
                }

                bool changed = ApplyFixedText(ideo, factionDef);
                changed |= ApplyFixedVisibility(ideo, factionDef);
                changed |= ApplyFixedMemes(ideo, factionDef);
                changed |= ApplyFixedIconAndColor(ideo);
                changed |= ApplyFixedStyles(ideo, factionDef);

                if (changed)
                {
                    RecacheIdeo(ideo);
                    Log.Message(LogPrefix + "Normalized The Marked One ideology.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(LogPrefix + "Skipped The Marked One ideology normalization: " + ex.Message);
            }
        }

        private static bool ApplyFixedText(Ideo ideo, FactionDef factionDef)
        {
            bool changed = false;

            if (!factionDef.ideoName.NullOrEmpty() && ideo.name != factionDef.ideoName)
            {
                ideo.name = factionDef.ideoName;
                changed = true;
            }

            if (!factionDef.ideoDescription.NullOrEmpty() && ideo.description != factionDef.ideoDescription)
            {
                ideo.description = factionDef.ideoDescription;
                changed = true;
            }

            return changed;
        }

        private static bool ApplyFixedVisibility(Ideo ideo, FactionDef factionDef)
        {
            if (ideo.hidden == factionDef.hiddenIdeo)
            {
                return false;
            }

            ideo.hidden = factionDef.hiddenIdeo;
            return true;
        }

        private static bool ApplyFixedMemes(Ideo ideo, FactionDef factionDef)
        {
            if (ideo.memes == null)
            {
                return false;
            }

            List<MemeDef> targetMemes = BuildTargetMemes(factionDef);
            if (targetMemes.Count == 0)
            {
                return false;
            }

            bool alreadyExact = ideo.memes.Count == targetMemes.Count;
            if (alreadyExact)
            {
                for (int i = 0; i < targetMemes.Count; i++)
                {
                    if (!ideo.memes.Contains(targetMemes[i]))
                    {
                        alreadyExact = false;
                        break;
                    }
                }
            }

            if (alreadyExact)
            {
                return false;
            }

            ideo.memes.Clear();
            ideo.memes.AddRange(targetMemes);
            ideo.SortMemesInDisplayOrder();
            return true;
        }

        private static bool ApplyFixedIconAndColor(Ideo ideo)
        {
            IdeoIconDef iconDef = DefDatabase<IdeoIconDef>.GetNamedSilentFail(FixedIconDefName);
            ColorDef colorDef = DefDatabase<ColorDef>.GetNamedSilentFail(FixedColorDefName);
            if (iconDef == null || colorDef == null || ideo.iconDef == iconDef && ideo.colorDef == colorDef && ideo.primaryFactionColor == null)
            {
                return false;
            }

            ideo.SetIcon(iconDef, colorDef, true);
            return true;
        }

        private static bool ApplyFixedStyles(Ideo ideo, FactionDef factionDef)
        {
            List<StyleCategoryDef> targetStyles = BuildTargetStyles(factionDef);
            if (targetStyles.Count == 0)
            {
                return false;
            }

            if (ideo.thingStyleCategories == null)
            {
                ideo.thingStyleCategories = new List<ThingStyleCategoryWithPriority>();
            }

            bool alreadyExact = ideo.thingStyleCategories.Count == targetStyles.Count;
            if (alreadyExact)
            {
                for (int i = 0; i < targetStyles.Count; i++)
                {
                    ThingStyleCategoryWithPriority current = ideo.thingStyleCategories[i];
                    if (current == null || current.category != targetStyles[i])
                    {
                        alreadyExact = false;
                        break;
                    }
                }
            }

            if (alreadyExact)
            {
                return false;
            }

            ideo.thingStyleCategories.Clear();
            for (int i = 0; i < targetStyles.Count; i++)
            {
                ideo.thingStyleCategories.Add(new ThingStyleCategoryWithPriority(targetStyles[i], 1f));
            }

            ideo.SortStyleCategories();
            ideo.style?.RecalculateAvailableStyleItems();
            ideo.style?.EnsureAtLeastOneStyleItemAvailable();
            return true;
        }

        private static List<MemeDef> BuildTargetMemes(FactionDef factionDef)
        {
            List<MemeDef> targetMemes = new List<MemeDef>();
            AddUniqueMemes(targetMemes, factionDef.forcedMemes);
            if (targetMemes.Count == 0)
            {
                for (int i = 0; i < FallbackMemeDefNames.Length; i++)
                {
                    MemeDef meme = DefDatabase<MemeDef>.GetNamedSilentFail(FallbackMemeDefNames[i]);
                    if (meme != null && !targetMemes.Contains(meme))
                    {
                        targetMemes.Add(meme);
                    }
                }
            }

            return targetMemes;
        }

        private static List<StyleCategoryDef> BuildTargetStyles(FactionDef factionDef)
        {
            List<StyleCategoryDef> targetStyles = new List<StyleCategoryDef>();
            AddUniqueStyles(targetStyles, factionDef.styles);
            if (targetStyles.Count == 0)
            {
                StyleCategoryDef style = DefDatabase<StyleCategoryDef>.GetNamedSilentFail(FallbackStyleCategoryDefName);
                if (style != null)
                {
                    targetStyles.Add(style);
                }
            }

            return targetStyles;
        }

        private static void AddUniqueMemes(List<MemeDef> targetMemes, List<MemeDef> sourceMemes)
        {
            if (sourceMemes == null)
            {
                return;
            }

            for (int i = 0; i < sourceMemes.Count; i++)
            {
                MemeDef meme = sourceMemes[i];
                if (meme != null && !targetMemes.Contains(meme))
                {
                    targetMemes.Add(meme);
                }
            }
        }

        private static void AddUniqueStyles(List<StyleCategoryDef> targetStyles, List<StyleCategoryDef> sourceStyles)
        {
            if (sourceStyles == null)
            {
                return;
            }

            for (int i = 0; i < sourceStyles.Count; i++)
            {
                StyleCategoryDef style = sourceStyles[i];
                if (style != null && !targetStyles.Contains(style))
                {
                    targetStyles.Add(style);
                }
            }
        }

        private static void RecacheIdeo(Ideo ideo)
        {
            ideo.RecachePrecepts();
            ideo.RecachePossibleRoles();
            ideo.RecachePossibleBuildings();
            ideo.RecachePossibleBuildables();
            ideo.RecachePossibleMentalBreaks();
            ideo.RecacheNeeds();
        }
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
        private const int CorpseLingeringRequiredTicks = 2500;
        private const float RaidEscalationPerRaid = 0.18f;
        private const float RaidEscalationMaxBonus = 5f;
        private const float RandomRaidIntervalMinFactor = 0.2f;
        private const float RandomRaidIntervalMaxFactor = 2.4f;

        private readonly Game game;
        private int nextMaintenanceTick;
        private int nextReanimationProcessTick;
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
        private List<string> recentIncidents = new List<string>();
        private List<Pawn> pendingReanimationPawns = new List<Pawn>();
        private List<int> pendingReanimationTicks = new List<int>();
        private List<Pawn> activeRaidPawns = new List<Pawn>();
        private List<Pawn> activeRaidColonistsAtStart = new List<Pawn>();
        private List<Pawn> corpseLingeringPawns = new List<Pawn>();
        private List<int> corpseLingeringTicks = new List<int>();
        private List<int> corpseLingeringLastSeenTicks = new List<int>();

        public TheMarkedMenGameComponent(Game game)
        {
            this.game = game;
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
            Scribe_Collections.Look(ref pendingReanimationPawns, "pendingReanimationPawns", LookMode.Reference);
            Scribe_Collections.Look(ref pendingReanimationTicks, "pendingReanimationTicks", LookMode.Value);
            Scribe_Collections.Look(ref activeRaidPawns, "activeRaidPawns", LookMode.Reference);
            Scribe_Collections.Look(ref activeRaidColonistsAtStart, "activeRaidColonistsAtStart", LookMode.Reference);
            Scribe_Collections.Look(ref corpseLingeringPawns, "corpseLingeringPawns", LookMode.Reference);
            Scribe_Collections.Look(ref corpseLingeringTicks, "corpseLingeringTicks", LookMode.Value);
            Scribe_Collections.Look(ref corpseLingeringLastSeenTicks, "corpseLingeringLastSeenTicks", LookMode.Value);
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
            EnsureCorpseLingeringTrackerLists();
        }

        private void EnsureCorpseLingeringTrackerLists()
        {
            if (corpseLingeringPawns == null)
            {
                corpseLingeringPawns = new List<Pawn>();
            }
            if (corpseLingeringTicks == null)
            {
                corpseLingeringTicks = new List<int>();
            }
            if (corpseLingeringLastSeenTicks == null)
            {
                corpseLingeringLastSeenTicks = new List<int>();
            }
            while (corpseLingeringTicks.Count < corpseLingeringPawns.Count)
            {
                corpseLingeringTicks.Add(0);
            }
            while (corpseLingeringLastSeenTicks.Count < corpseLingeringPawns.Count)
            {
                corpseLingeringLastSeenTicks.Add(0);
            }
            while (corpseLingeringTicks.Count > corpseLingeringPawns.Count)
            {
                corpseLingeringTicks.RemoveAt(corpseLingeringTicks.Count - 1);
            }
            while (corpseLingeringLastSeenTicks.Count > corpseLingeringPawns.Count)
            {
                corpseLingeringLastSeenTicks.RemoveAt(corpseLingeringLastSeenTicks.Count - 1);
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

            if (ticks >= nextCorpseExposureTick)
            {
                nextCorpseExposureTick = ticks + TheMarkedMenSettings.CorpseContaminationIntervalTicks;
                CrossedCorpseUtility.TryExposeNearbyPawnsToInfectedCorpses();
                PruneCorpseLingeringTrackers(ticks);
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

        public bool NoteCorpseLingering(Pawn pawn, int currentTick, int observedTicks)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || CrossedUtility.IsInfectedPawn(pawn) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(pawn))
            {
                return false;
            }

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
            {
                corpseLingeringTicks[index] = Mathf.Min(CorpseLingeringRequiredTicks, corpseLingeringTicks[index] + Mathf.Max(0, observedTicks));
            }
            else
            {
                corpseLingeringTicks[index] = Mathf.Max(0, observedTicks);
            }

            corpseLingeringLastSeenTicks[index] = currentTick;
            return corpseLingeringTicks[index] >= CorpseLingeringRequiredTicks;
        }

        public void ResetCorpseLingering(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            EnsureCorpseLingeringTrackerLists();
            int index = corpseLingeringPawns.IndexOf(pawn);
            if (index < 0)
            {
                return;
            }

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
                    if (i < corpseLingeringTicks.Count)
                    {
                        corpseLingeringTicks.RemoveAt(i);
                    }
                    if (i < corpseLingeringLastSeenTicks.Count)
                    {
                        corpseLingeringLastSeenTicks.RemoveAt(i);
                    }
                }
            }
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
                    canTimeoutOrFlee = false,
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

            TryFireRaidIncident(true);
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
            parms.canTimeoutOrFlee = false;
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
            float pressure = Mathf.InverseLerp(5000f, 50000f, points);
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

            if (TryFireHordeIncident(true))
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
            parms.canTimeoutOrFlee = false;
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
            parms.canTimeoutOrFlee = false;
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

            starterLineageInitialized = true;

            if (Find.Scenario?.AllParts?.Any(p => p is ScenPart_MarkedSurvivorState) != true)
            {
                return;
            }

            int marked = 0;
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
                        if (CrossedUtility.TryMarkStarterLineageResistant(colonists[j]))
                        {
                            marked++;
                        }
                    }
                }
            }

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
            float pressure = Mathf.InverseLerp(5000f, 50000f, points);
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
            float pressure = Mathf.InverseLerp(5000f, 50000f, points);
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
            defaultLabel = "Remaining:";
            defaultExplanation = "The chronometer flickers. The Marked will come when they are ready.";
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

            return FormatTimeRemaining(ticksUntilRaid);
        }

        public override TaggedString GetExplanation()
        {
            TheMarkedMenGameComponent component = Current.Game?.GetComponent<TheMarkedMenGameComponent>();
            if (component == null || !component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map _))
            {
                return defaultExplanation;
            }

            return "Something stirs beyond the perimeter. The Marked are gathering. They will come at their appointed time.";
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

        private static string FormatTimeRemaining(int ticksUntilRaid)
        {
            if (ticksUntilRaid <= 0)
            {
                return "imminent";
            }

            int totalSeconds = Mathf.CeilToInt(ticksUntilRaid / 60f);
            int days = totalSeconds / 86400;
            int hours = (totalSeconds % 86400) / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            string result = "";
            if (days > 0) result += days + " day" + (days != 1 ? "s" : "") + ", ";
            if (hours > 0) result += hours + " hour" + (hours != 1 ? "s" : "") + ", ";
            if (minutes > 0) result += minutes + " minute" + (minutes != 1 ? "s" : "") + ", ";
            result += seconds + " second" + (seconds != 1 ? "s" : "");

            return result;
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
        public float immunityChance = TheMarkedMenSettings.DefaultImmunitySurvivalChance;
        public float terminalTransformationChance = TheMarkedMenSettings.DefaultTerminalTransformationChance;
        public float transformedSeverity = 1f;

        public HediffCompProperties_CrossVirus()
        {
            compClass = typeof(HediffComp_CrossVirus);
        }
    }

    public sealed class MarkedVirusProtectionExtension : DefModExtension
    {
        public float resistance;
        public bool sealedAgainstMarkedVirus;
        public bool blocksMarkedVirusExposure;
    }

    public struct MarkedVirusApparelProtection
    {
        public float resistance;
        public bool sealedAgainstMarkedVirus;
        public bool blocksMarkedVirusExposure;

        public MarkedVirusApparelProtection(float resistance, bool sealedAgainstMarkedVirus, bool blocksMarkedVirusExposure = false)
        {
            this.blocksMarkedVirusExposure = blocksMarkedVirusExposure;
            this.resistance = blocksMarkedVirusExposure ? 1f : Mathf.Clamp01(resistance);
            this.sealedAgainstMarkedVirus = (sealedAgainstMarkedVirus || blocksMarkedVirusExposure) && this.resistance > 0f;
        }
    }

    public sealed class StatWorker_MarkedVirusResistance : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            return PawnFor(req) != null || ApparelDefFor(req) != null;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            Pawn pawn = PawnFor(req);
            if (pawn != null)
            {
                return CrossedUtility.GetMarkedVirusApparelResistance(pawn);
            }

            ThingDef apparelDef = ApparelDefFor(req);
            return apparelDef == null ? 0f : CrossedUtility.GetMarkedVirusApparelProtection(apparelDef).resistance;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            Pawn pawn = PawnFor(req);
            if (pawn != null)
            {
                MarkedVirusApparelProtection pawnProtection = CrossedUtility.GetMarkedVirusExposureProtection(pawn);
                string pawnPercent = Mathf.RoundToInt(Mathf.Clamp01(pawnProtection.resistance) * 100f).ToString() + "%";
                if (pawnProtection.blocksMarkedVirusExposure)
                {
                    return "Marked Virus resistance: " + pawnPercent + "\n\nFully sealed protective gear blocks direct Marked Virus exposure. This is separate from toxic environment resistance.";
                }

                return pawnProtection.sealedAgainstMarkedVirus
                    ? "Marked Virus resistance: " + pawnPercent + "\n\nBest worn Marked Virus protection is sealed protective gear. The Marked Virus uses this value separately from toxic environment resistance."
                    : "Marked Virus resistance: " + pawnPercent + "\n\nBest worn Marked Virus protection reduces exposure risk. The Marked Virus uses this value separately from toxic environment resistance.";
            }

            ThingDef apparelDef = ApparelDefFor(req);
            if (apparelDef == null)
            {
                return string.Empty;
            }

            MarkedVirusApparelProtection protection = CrossedUtility.GetMarkedVirusApparelProtection(apparelDef);
            string percent = Mathf.RoundToInt(Mathf.Clamp01(protection.resistance) * 100f).ToString() + "%";
            if (protection.blocksMarkedVirusExposure)
            {
                return "Marked Virus resistance: " + percent + "\n\nThis apparel is fully sealed against direct Marked Virus exposure.";
            }

            return protection.sealedAgainstMarkedVirus
                ? "Marked Virus resistance: " + percent + "\n\nThis apparel resists Marked Virus exposure and treats breakthrough infections as sealed-protection breakthroughs."
                : "Marked Virus resistance: " + percent + "\n\nThis apparel resists Marked Virus exposure but is not sealed protective gear.";
        }

        private static ThingDef ApparelDefFor(StatRequest req)
        {
            Thing thing = req.Thing;
            if (thing?.def?.apparel != null)
            {
                return thing.def;
            }

            if (req.Def is ThingDef thingDef && thingDef.apparel != null)
            {
                return thingDef;
            }

            return null;
        }

        private static Pawn PawnFor(StatRequest req)
        {
            return req.Thing as Pawn;
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
        private float apparelResistanceAtExposure;
        private bool sealedApparelAtExposure;
        private float progressionDelayFactor = 1f;

        private HediffCompProperties_CrossVirus Props => (HediffCompProperties_CrossVirus)props;

        public void NotifyInfector(Pawn infector)
        {
            if (infector != null && infector != parent?.pawn)
            {
                originalInfector = infector;
            }
        }

        public void NotifyExposureProtection(float resistance, bool sealedAgainstMarkedVirus)
        {
            resistance = Mathf.Clamp01(resistance);
            if (resistance <= 0f)
            {
                return;
            }

            float oldDelayFactor = CurrentProgressionDelayFactor();
            float candidateDelayFactor = ProgressionDelayFactorFor(resistance, sealedAgainstMarkedVirus);
            if (resistance > apparelResistanceAtExposure)
            {
                apparelResistanceAtExposure = resistance;
                sealedApparelAtExposure = sealedAgainstMarkedVirus;
            }
            else if (Mathf.Approximately(resistance, apparelResistanceAtExposure))
            {
                sealedApparelAtExposure = sealedApparelAtExposure || sealedAgainstMarkedVirus;
            }

            float newDelayFactor = Mathf.Max(oldDelayFactor, Mathf.Max(candidateDelayFactor, ProgressionDelayFactorFor(apparelResistanceAtExposure, sealedApparelAtExposure)));
            if (newDelayFactor > oldDelayFactor)
            {
                ApplyProgressionDelayIncrease(oldDelayFactor, newDelayFactor);
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
            Scribe_Values.Look(ref apparelResistanceAtExposure, "apparelResistanceAtExposure", 0f);
            Scribe_Values.Look(ref sealedApparelAtExposure, "sealedApparelAtExposure", false);
            Scribe_Values.Look(ref progressionDelayFactor, "progressionDelayFactor", 1f);
            Scribe_References.Look(ref originalInfector, "originalInfector");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && parent?.pawn != null)
            {
                apparelResistanceAtExposure = Mathf.Clamp01(apparelResistanceAtExposure);
                progressionDelayFactor = Mathf.Max(1f, progressionDelayFactor);
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
                immunityChance = AdjustedIncubationSurvivalChance(immunityChance);
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
            return Mathf.Max(1, Mathf.RoundToInt(TheMarkedMenSettings.AdjustInfectionTicks(Rand.RangeInclusive(min, max)) * CurrentProgressionDelayFactor()));
        }

        private int MaxConfiguredTransformationTicks()
        {
            int max = Mathf.Max(Props.commonTransformationMaxTicks, Props.rareTransformationMaxTicks);
            if (max <= 0)
            {
                max = Props.incubationTicks;
            }

            return Mathf.Max(1, Mathf.RoundToInt(TheMarkedMenSettings.AdjustInfectionTicks(Mathf.Max(1, max)) * CurrentProgressionDelayFactor()));
        }

        private void ApplyProgressionDelayIncrease(float oldDelayFactor, float newDelayFactor)
        {
            progressionDelayFactor = Mathf.Max(1f, newDelayFactor);
            int ticks = Find.TickManager?.TicksGame ?? infectionTick;
            if (infectionTick < 0)
            {
                infectionTick = ticks;
            }

            EnsureProgressionTimers(ticks);
            int elapsed = Mathf.Max(0, ticks - infectionTick);
            int remaining = Mathf.Max(1, transformationTicks - elapsed);
            float scale = newDelayFactor / Mathf.Max(1f, oldDelayFactor);
            transformationTicks = Mathf.Max(elapsed + 1, elapsed + Mathf.RoundToInt(remaining * scale));

            if (!incubationResolved)
            {
                int onset = Mathf.RoundToInt(transformationTicks * Mathf.Clamp01(Props.symptomOnsetFraction));
                symptomOnsetTicks = Mathf.Clamp(onset, 1, Mathf.Max(1, transformationTicks));
            }
        }

        private float CurrentProgressionDelayFactor()
        {
            float factorFromExposure = ProgressionDelayFactorFor(apparelResistanceAtExposure, sealedApparelAtExposure);
            return Mathf.Max(1f, Mathf.Max(progressionDelayFactor, factorFromExposure));
        }

        private static float ProgressionDelayFactorFor(float resistance, bool sealedAgainstMarkedVirus)
        {
            resistance = Mathf.Clamp01(resistance);
            if (resistance <= 0f)
            {
                return 1f;
            }

            float delayScale = sealedAgainstMarkedVirus ? 2f : 0.75f;
            return Mathf.Clamp(1f + resistance * delayScale, 1f, 3f);
        }

        private float AdjustedIncubationSurvivalChance(float baseChance)
        {
            float resistance = Mathf.Clamp01(apparelResistanceAtExposure);
            if (resistance <= 0f)
            {
                return Mathf.Clamp01(baseChance);
            }

            float bonusScale = sealedApparelAtExposure ? 0.12f : 0.04f;
            return Mathf.Clamp01(baseChance + resistance * bonusScale);
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
        private const float DefaultApparelVirusResistance = 0.02f;
        private const float MaxWornMarkedVirusResistance = 0.45f;
        private const string StarterLineageResistanceTag = "CA_StarterLineageResistance";
        private const string MarkedVillageFounderTag = "CA_MarkedVillageFounder";
        private const string PersistentCrossedRashTag = "CA_PersistentCrossedRashTattoo";
        private const string MarkedVillageRashRolledTag = "CA_MarkedVillageRashRolled";
        private const float CrossVirusStage2Severity = 0.20f;
        private const float CrossVirusStage3Severity = 0.45f;
        private const float CrossVirusStage4Severity = 0.72f;
        private const float CrossVirusFinalStageSeverity = 0.95f;
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

        public static TattooDef GetCurrentCrossedFaceTattoo(Pawn pawn)
        {
            TattooDef finalTattoo = CADefOf.CrossedFaceTattoo;
            if (pawn == null || IsCrossedFactionPawn(pawn))
            {
                return finalTattoo;
            }

            HediffDef virus = CADefOf.CrossVirus;
            Hediff hediff = virus == null ? null : pawn.health?.hediffSet?.GetFirstHediffOfDef(virus);
            if (hediff == null)
            {
                return finalTattoo;
            }

            return CrossedFaceTattooForSeverity(hediff.Severity) ?? finalTattoo;
        }

        private static TattooDef CrossedFaceTattooForSeverity(float severity)
        {
            if (severity >= CrossVirusFinalStageSeverity)
            {
                return CADefOf.CrossedFaceTattoo;
            }

            if (severity >= CrossVirusStage4Severity)
            {
                return CADefOf.CrossedFaceTattooStage4 ?? CADefOf.CrossedFaceTattoo;
            }

            if (severity >= CrossVirusStage3Severity)
            {
                return CADefOf.CrossedFaceTattooStage3 ?? CADefOf.CrossedFaceTattoo;
            }

            if (severity >= CrossVirusStage2Severity)
            {
                return CADefOf.CrossedFaceTattooStage2 ?? CADefOf.CrossedFaceTattoo;
            }

            return CADefOf.CrossedFaceTattooStage1 ?? CADefOf.CrossedFaceTattoo;
        }

        public static void EnsureFearlessCrossedState(Pawn pawn)
        {
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
            return HasMarkedVillageFounderImmunity(pawn)
                || HasCrossVirusImmunity(pawn) && !HasStarterLineageResistance(pawn)
                || HasSealedMarkedVirusExposureProtection(pawn);
        }

        public static bool HasSealedMarkedVirusExposureProtection(Pawn pawn)
        {
            return GetMarkedVirusExposureProtection(pawn).blocksMarkedVirusExposure;
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

            MarkedVirusApparelProtection exposureProtection = default(MarkedVirusApparelProtection);
            float effectiveChance = Mathf.Clamp01(chance);
            if (starterLineageBreakthrough)
            {
                effectiveChance *= TheMarkedMenSettings.StarterLineageBreakthroughChance;
            }

            if (CanApparelReduceMarkedVirusExposure(source))
            {
                exposureProtection = GetMarkedVirusExposureProtection(pawn);
                if (exposureProtection.blocksMarkedVirusExposure)
                {
                    return false;
                }

                if (exposureProtection.resistance > 0f)
                {
                    effectiveChance *= 1f - exposureProtection.resistance;
                }
            }

            if (effectiveChance <= 0f)
            {
                return false;
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
            comp?.NotifyExposureProtection(exposureProtection.resistance, exposureProtection.sealedAgainstMarkedVirus);
            EnsureInfectedState(pawn);
            if (newlyInfected)
            {
                Component?.NotifyExposure(pawn, source);
            }

            return true;
        }

        public static float GetMarkedVirusApparelResistance(Pawn pawn)
        {
            return GetMarkedVirusExposureProtection(pawn).resistance;
        }

        public static MarkedVirusApparelProtection GetMarkedVirusExposureProtection(Pawn pawn)
        {
            return GetMarkedVirusApparelProtection(pawn);
        }

        public static MarkedVirusApparelProtection GetMarkedVirusApparelProtection(Pawn pawn)
        {
            List<Apparel> wornApparel = pawn?.apparel?.WornApparel;
            if (wornApparel == null || wornApparel.Count == 0)
            {
                return default(MarkedVirusApparelProtection);
            }

            float resistance = 0f;
            bool sealedAgainstMarkedVirus = false;
            bool blocksMarkedVirusExposure = false;
            bool hasVacsuitBody = false;
            bool hasVacsuitHelmet = false;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                ThingDef apparelDef = wornApparel[i]?.def;
                MarkedVirusApparelProtection apparelProtection = GetMarkedVirusProtectionForApparelDef(apparelDef);
                if (apparelProtection.resistance <= 0f)
                {
                    continue;
                }

                blocksMarkedVirusExposure = blocksMarkedVirusExposure || apparelProtection.blocksMarkedVirusExposure;
                hasVacsuitBody = hasVacsuitBody || IsMarkedVirusVacsuitBody(apparelDef);
                hasVacsuitHelmet = hasVacsuitHelmet || IsMarkedVirusVacsuitHelmet(apparelDef);

                if (apparelProtection.resistance > resistance)
                {
                    resistance = apparelProtection.resistance;
                    sealedAgainstMarkedVirus = apparelProtection.sealedAgainstMarkedVirus;
                }
                else if (Mathf.Approximately(apparelProtection.resistance, resistance))
                {
                    sealedAgainstMarkedVirus = sealedAgainstMarkedVirus || apparelProtection.sealedAgainstMarkedVirus;
                }
            }

            if (blocksMarkedVirusExposure || hasVacsuitBody && hasVacsuitHelmet && TheMarkedMenSettings.VacsuitBlockExposure)
            {
                return new MarkedVirusApparelProtection(1f, true, true);
            }

            return ClampMarkedVirusProtection(new MarkedVirusApparelProtection(resistance, sealedAgainstMarkedVirus));
        }

        public static MarkedVirusApparelProtection GetMarkedVirusApparelProtection(ThingDef apparelDef)
        {
            return GetMarkedVirusProtectionForApparelDef(apparelDef);
        }

        public static void ApplyMarkedVirusResistanceEquippedStatOffsets()
        {
            StatDef markedVirusResistance = CADefOf.MarkedVirusResistance;
            if (markedVirusResistance == null)
            {
                return;
            }

            List<ThingDef> allThingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allThingDefs.Count; i++)
            {
                ThingDef apparelDef = allThingDefs[i];
                if (apparelDef?.apparel == null)
                {
                    continue;
                }

                MarkedVirusApparelProtection protection = GetMarkedVirusProtectionForApparelDef(apparelDef);
                float resistance = Mathf.Clamp01(protection.resistance);
                if (resistance <= 0f)
                {
                    continue;
                }

                if (apparelDef.equippedStatOffsets == null)
                {
                    apparelDef.equippedStatOffsets = new List<StatModifier>();
                }

                bool updatedExistingOffset = false;
                for (int offsetIndex = 0; offsetIndex < apparelDef.equippedStatOffsets.Count; offsetIndex++)
                {
                    StatModifier offset = apparelDef.equippedStatOffsets[offsetIndex];
                    if (offset.stat != markedVirusResistance)
                    {
                        continue;
                    }

                    offset.value = resistance;
                    apparelDef.equippedStatOffsets[offsetIndex] = offset;
                    updatedExistingOffset = true;
                    break;
                }

                if (!updatedExistingOffset)
                {
                    apparelDef.equippedStatOffsets.Add(new StatModifier
                    {
                        stat = markedVirusResistance,
                        value = resistance
                    });
                }
            }
        }

        private static MarkedVirusApparelProtection GetMarkedVirusProtectionForApparelDef(ThingDef apparelDef)
        {
            if (apparelDef?.apparel == null)
            {
                return default(MarkedVirusApparelProtection);
            }

            MarkedVirusProtectionExtension extension = apparelDef.GetModExtension<MarkedVirusProtectionExtension>();
            if (extension != null)
            {
                return ClampMarkedVirusProtection(new MarkedVirusApparelProtection(extension.resistance, extension.sealedAgainstMarkedVirus, extension.blocksMarkedVirusExposure));
            }

            return ClampMarkedVirusProtection(InferMarkedVirusApparelProtection(apparelDef));
        }

        private static MarkedVirusApparelProtection ClampMarkedVirusProtection(MarkedVirusApparelProtection protection)
        {
            if (protection.blocksMarkedVirusExposure)
            {
                return new MarkedVirusApparelProtection(1f, true, true);
            }

            float resistance = Mathf.Clamp(protection.resistance, 0f, MaxWornMarkedVirusResistance);
            return new MarkedVirusApparelProtection(resistance, protection.sealedAgainstMarkedVirus);
        }

        private static bool IsMarkedVirusVacsuitBody(ThingDef apparelDef)
        {
            if (apparelDef?.apparel == null || !ApparelCoversBodyPartGroup(apparelDef, "Torso"))
            {
                return false;
            }

            string defName = apparelDef.defName ?? string.Empty;
            string label = apparelDef.label ?? string.Empty;
            return ContainsOrdinalIgnoreCase(defName, "Vacsuit")
                || ContainsOrdinalIgnoreCase(label, "vacsuit")
                || ContainsOrdinalIgnoreCase(defName, "EVAsuit")
                || ContainsOrdinalIgnoreCase(label, "EVA suit");
        }

        private static bool IsMarkedVirusVacsuitHelmet(ThingDef apparelDef)
        {
            if (apparelDef?.apparel == null || !ApparelCoversBodyPartGroup(apparelDef, "FullHead"))
            {
                return false;
            }

            string defName = apparelDef.defName ?? string.Empty;
            string label = apparelDef.label ?? string.Empty;
            return ContainsOrdinalIgnoreCase(defName, "VacsuitHelmet")
                || ContainsOrdinalIgnoreCase(label, "vacsuit helmet");
        }

        private static MarkedVirusApparelProtection InferMarkedVirusApparelProtection(ThingDef apparelDef)
        {
            string defName = apparelDef.defName ?? string.Empty;
            string label = apparelDef.label ?? string.Empty;
            bool fullHead = ApparelCoversBodyPartGroup(apparelDef, "FullHead");
            bool torso = ApparelCoversBodyPartGroup(apparelDef, "Torso");
            bool toxGasImmune = apparelDef.apparel?.immuneToToxGasExposure == true;
            float toxicEnvironmentResistance = EquippedStatOffset(apparelDef, "ToxicEnvironmentResistance");
            float vacuumResistance = EquippedStatOffset(apparelDef, "VacuumResistance");

            // HAZMAT suit (XML: 0.90 sealed)
            if (string.Equals(defName, "VAE_Apparel_HAZMATSuit", StringComparison.OrdinalIgnoreCase))
            {
                return new MarkedVirusApparelProtection(0.90f, true, TheMarkedMenSettings.SealedArmorBlockExposure);
            }

            // Warcasket torso (XML: 0.85 sealed)
            if (ContainsOrdinalIgnoreCase(defName, "Warcasket") && torso
                && !ContainsOrdinalIgnoreCase(defName, "Shoulders")
                && !ContainsOrdinalIgnoreCase(defName, "Bodysuit"))
            {
                return new MarkedVirusApparelProtection(0.85f, true, TheMarkedMenSettings.WarcasketsBlockExposure);
            }

            // Warcasket bodysuit (XML: 0.70 sealed)
            if (ContainsOrdinalIgnoreCase(defName, "Warcasket")
                && ContainsOrdinalIgnoreCase(defName, "Bodysuit") && torso)
            {
                return new MarkedVirusApparelProtection(0.70f, true);
            }

            // WarcasketHelmet (XML: 0.25 sealed)
            if (ContainsOrdinalIgnoreCase(defName, "WarcasketHelmet"))
            {
                return new MarkedVirusApparelProtection(0.25f, true);
            }

            // WarcasketShoulders (XML: 0.15 not sealed)
            if (ContainsOrdinalIgnoreCase(defName, "WarcasketShoulders"))
            {
                return new MarkedVirusApparelProtection(0.15f, false);
            }

            // Other warcasket parts (XML: 0.10 not sealed)
            if (ContainsOrdinalIgnoreCase(defName, "Warcasket"))
            {
                return new MarkedVirusApparelProtection(0.10f, false);
            }

            // Vacsuit/EVA suits (XML: 0.30 sealed)
            if (IsMarkedVirusVacsuitBody(apparelDef))
            {
                return new MarkedVirusApparelProtection(0.30f, true);
            }

            // Sealed undersuits, orbital armor (XML: 0.25 sealed)
            if (torso && (ContainsOrdinalIgnoreCase(defName, "Sealed") || ContainsOrdinalIgnoreCase(label, "sealed")
                || ContainsOrdinalIgnoreCase(defName, "AstroSuit") || ContainsOrdinalIgnoreCase(label, "astrosuit")
                || ContainsOrdinalIgnoreCase(defName, "SecurityArmor") || ContainsOrdinalIgnoreCase(label, "security armor")
                || ContainsOrdinalIgnoreCase(defName, "OrbitalArmor") || ContainsOrdinalIgnoreCase(label, "orbital armor")))
            {
                return new MarkedVirusApparelProtection(0.25f, true, TheMarkedMenSettings.SealedArmorBlockExposure);
            }

            // Vacsuit helmet (XML: 0.25 sealed)
            if (IsMarkedVirusVacsuitHelmet(apparelDef))
            {
                return new MarkedVirusApparelProtection(0.25f, true);
            }

            // Gas masks, HAZMAT masks (XML: 0.25 sealed)
            if (toxGasImmune || ContainsOrdinalIgnoreCase(defName, "GasMask")
                || ContainsOrdinalIgnoreCase(label, "gas mask")
                || ContainsOrdinalIgnoreCase(defName, "HAZMATMask")
                || ContainsOrdinalIgnoreCase(defName, "AstroMask"))
            {
                return new MarkedVirusApparelProtection(0.25f, true, TheMarkedMenSettings.GasMasksBlockExposure);
            }

            // Toxic environment resistance (XML: 0.25 sealed)
            if (toxicEnvironmentResistance >= 0.75f && fullHead)
            {
                return new MarkedVirusApparelProtection(0.25f, true);
            }

            // Vacuum resistance (XML: 0.25 sealed)
            if (vacuumResistance >= 0.30f && torso)
            {
                return new MarkedVirusApparelProtection(0.25f, true);
            }

            // Powered armor bodies (XML: 0.25 not sealed)
            if (IsPoweredArmorBody(defName) || IsPoweredArmorBody(label))
            {
                return new MarkedVirusApparelProtection(0.25f, false);
            }

            // Powered armor helmets (XML: 0.20 not sealed)
            if (IsPoweredArmorHelmet(defName) || IsPoweredArmorHelmet(label))
            {
                return new MarkedVirusApparelProtection(0.20f, false);
            }

            // Plague masks (XML: 0.10 not sealed)
            if (ContainsOrdinalIgnoreCase(defName, "PlagueMask") || ContainsOrdinalIgnoreCase(label, "plague mask"))
            {
                return new MarkedVirusApparelProtection(0.10f, false);
            }

            // Cloth, surgical, face masks (XML: 0.06 not sealed)
            if (ContainsOrdinalIgnoreCase(defName, "ClothMask") || ContainsOrdinalIgnoreCase(defName, "SurgicalMask")
                || ContainsOrdinalIgnoreCase(label, "surgical mask") || ContainsOrdinalIgnoreCase(label, "face mask"))
            {
                return new MarkedVirusApparelProtection(0.06f, false);
            }

            // Suits, armor, flak (XML: 0.08 not sealed)
            if (IsArmorOrSuit(defName) || IsArmorOrSuit(label))
            {
                return new MarkedVirusApparelProtection(0.08f, false);
            }

            // Masks (XML: 0.05 not sealed)
            if (ContainsOrdinalIgnoreCase(defName, "Mask") || ContainsOrdinalIgnoreCase(label, "mask"))
            {
                return new MarkedVirusApparelProtection(0.05f, false);
            }

            // Helmets (XML: 0.05 not sealed)
            if (ContainsOrdinalIgnoreCase(defName, "Helmet") || ContainsOrdinalIgnoreCase(label, "helmet"))
            {
                return new MarkedVirusApparelProtection(0.05f, false);
            }

            return new MarkedVirusApparelProtection(DefaultApparelVirusResistance, false);
        }

        private static bool IsPoweredArmorBody(string text)
        {
            return ContainsOrdinalIgnoreCase(text, "PowerArmor")
                || ContainsOrdinalIgnoreCase(text, "ArmorRecon")
                || ContainsOrdinalIgnoreCase(text, "ArmorMarine")
                || ContainsOrdinalIgnoreCase(text, "ArmorCataphract")
                || ContainsOrdinalIgnoreCase(text, "ArmorLocust")
                || ContainsOrdinalIgnoreCase(text, "ArmorPhoenix")
                || ContainsOrdinalIgnoreCase(text, "MechlordSuit")
                || ContainsOrdinalIgnoreCase(text, "ArmorAbsolver")
                || ContainsOrdinalIgnoreCase(text, "ArmorDeserter")
                || ContainsOrdinalIgnoreCase(text, "privateer armor");
        }

        private static bool IsPoweredArmorHelmet(string text)
        {
            return ContainsOrdinalIgnoreCase(text, "PowerArmorHelmet")
                || ContainsOrdinalIgnoreCase(text, "ArmorHelmetRecon")
                || ContainsOrdinalIgnoreCase(text, "ArmorHelmetCataphract")
                || ContainsOrdinalIgnoreCase(text, "ArmorMarineHelmet")
                || ContainsOrdinalIgnoreCase(text, "ArmorHelmetMech")
                || ContainsOrdinalIgnoreCase(text, "AbsolverHelmet")
                || ContainsOrdinalIgnoreCase(text, "DeserterHelmet")
                || ContainsOrdinalIgnoreCase(text, "JanissaryHelmet")
                || ContainsOrdinalIgnoreCase(text, "power armor helmet")
                || ContainsOrdinalIgnoreCase(text, "marine helmet")
                || ContainsOrdinalIgnoreCase(text, "recon helmet")
                || ContainsOrdinalIgnoreCase(text, "cataphract helmet");
        }

        private static bool IsArmorOrSuit(string text)
        {
            return ContainsOrdinalIgnoreCase(text, "Suit")
                || ContainsOrdinalIgnoreCase(text, "Armor")
                || ContainsOrdinalIgnoreCase(text, "Armour")
                || ContainsOrdinalIgnoreCase(text, "Cuirass")
                || ContainsOrdinalIgnoreCase(text, "Flak")
                || ContainsOrdinalIgnoreCase(text, "WarcasketShoulders")
                || ContainsOrdinalIgnoreCase(text, "warcasket shoulders");
        }

        private static bool ApparelCoversBodyPartGroup(ThingDef apparelDef, string bodyPartGroupDefName)
        {
            List<BodyPartGroupDef> bodyPartGroups = apparelDef?.apparel?.bodyPartGroups;
            if (bodyPartGroups == null)
            {
                return false;
            }

            for (int i = 0; i < bodyPartGroups.Count; i++)
            {
                if (string.Equals(bodyPartGroups[i]?.defName, bodyPartGroupDefName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static float EquippedStatOffset(ThingDef apparelDef, string statDefName)
        {
            List<StatModifier> offsets = apparelDef?.equippedStatOffsets;
            if (offsets == null)
            {
                return 0f;
            }

            float value = 0f;
            for (int i = 0; i < offsets.Count; i++)
            {
                StatDef stat = offsets[i].stat;
                if (string.Equals(stat?.defName, statDefName, StringComparison.OrdinalIgnoreCase))
                {
                    value += offsets[i].value;
                }
            }

            return value;
        }

        private static bool ContainsOrdinalIgnoreCase(string text, string value)
        {
            return !string.IsNullOrEmpty(text)
                && !string.IsNullOrEmpty(value)
                && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool CanApparelReduceMarkedVirusExposure(string source)
        {
            return string.IsNullOrEmpty(source) || source.IndexOf("food", StringComparison.OrdinalIgnoreCase) < 0;
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
            CrossedTacticalAI.TryAttackNearestNonInfectedPawn(pawn, true, allowRjwJob: false);
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
            int alphaCount = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead)
                {
                    continue;
                }

                bool replace = false;
                if (pawn.kindDef == CADefOf.CrossedAlpha)
                {
                    alphaCount++;
                    replace = alphaCount > maxAlphas || TheMarkedMenSettings.AdjustKindWeight(CADefOf.CrossedAlpha, 1f) <= 0f;
                }
                else if (pawn.kindDef == CADefOf.CrossedWarlord)
                {
                    replace = TheMarkedMenSettings.AdjustKindWeight(CADefOf.CrossedWarlord, 1f) <= 0f;
                }
                else if (pawn.kindDef == CADefOf.MarkedMan)
                {
                    replace = TheMarkedMenSettings.AdjustKindWeight(CADefOf.MarkedMan, 1f) <= 0f;
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
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedCivilian, 14f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedScout, 10f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedHunter, 8f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedShooter, 8f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedRaider, 6f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedSoldier, 4f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedBrute, 2f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, 3f);
            return selected ?? CADefOf.CrossedCivilian ?? CADefOf.CrossedScout ?? CADefOf.CrossedHunter ?? CADefOf.CrossedShooter;
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
            RemoveHediffIfPresent(pawn, CADefOf.PsychicAura);
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

            if (ModsConfig.BiotechActive && pawn.genes != null && IsCrossedFactionPawn(pawn) && CADefOf.MarkedOne != null)
            {
                pawn.genes.SetXenotypeDirect(CADefOf.MarkedOne);
            }

            RemoveDeprecatedCrossedRashHediff(pawn);
            if (!ShouldShowCrossedRash(pawn))
            {
                RemoveCrossedRashVisualsIfNotSuccumbed(pawn);
                return;
            }

            EnsureFearlessCrossedState(pawn);
            if (!IsCrossedFactionPawn(pawn) && !HasCrossVirusImmunity(pawn) && !HasMarkedVillageFounderImmunity(pawn))
            {
                HediffDef virus = CADefOf.CrossVirus;
                if (virus != null)
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(virus) ?? pawn.health.AddHediff(virus);
                    hediff.Severity = 1f;
                }
            }

            if (pawn.kindDef == CADefOf.CrossedBrute && CADefOf.BloodRush != null && !pawn.health.hediffSet.HasHediff(CADefOf.BloodRush))
            {
                pawn.health.AddHediff(CADefOf.BloodRush);
            }

            if ((pawn.kindDef == CADefOf.CrossedAlpha || pawn.kindDef == CADefOf.CrossedWarlord || pawn.kindDef == CADefOf.MarkedMan)
                && CADefOf.CommandAura != null && !pawn.health.hediffSet.HasHediff(CADefOf.CommandAura))
            {
                pawn.health.AddHediff(CADefOf.CommandAura);
            }

            if (pawn.kindDef == CADefOf.MarkedMan && CADefOf.BloodRush != null && !pawn.health.hediffSet.HasHediff(CADefOf.BloodRush))
            {
                pawn.health.AddHediff(CADefOf.BloodRush);
            }

            if (IsCrossedFactionPawn(pawn) && CADefOf.CrossedStrength != null && !pawn.health.hediffSet.HasHediff(CADefOf.CrossedStrength))
            {
                pawn.health.AddHediff(CADefOf.CrossedStrength);
            }

            ApplyEliteTierHediff(pawn);

            if (IsCrossedFactionPawn(pawn))
            {
                ApplyRandomBionics(pawn);
                CrossedEquipmentGenerator.AssignEquipment(pawn);
            }

            ApplyInfectedTattoo(pawn);
            EnsureCrossedBasicClothingOnly(pawn);
            EnsureCrossedPyromaniacMolotov(pawn);
        }

        public static void ApplyRandomBionics(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || !IsCrossedFactionPawn(pawn)) return;

            if (pawn.kindDef != CADefOf.MarkedMan) return;

            int maxBionics = 3;
            float perPartChance = 0.5f;

            int installed = 0;
            TryInstallBionic(pawn, "BionicEye", BodyPartDefOf.Eye, perPartChance, ref installed, maxBionics);
            TryInstallBionic(pawn, "BionicArm", BodyPartDefOf.Arm, perPartChance, ref installed, maxBionics);
            TryInstallBionic(pawn, "AdvBionicArm", BodyPartDefOf.Arm, perPartChance * 0.2f, ref installed, maxBionics);
            TryInstallBionic(pawn, "BionicLeg", BodyPartDefOf.Leg, perPartChance, ref installed, maxBionics);
            TryInstallBionic(pawn, "AdvBionicLeg", BodyPartDefOf.Leg, perPartChance * 0.2f, ref installed, maxBionics);
            TryInstallBionic(pawn, "BionicHeart", BodyPartDefOf.Heart, perPartChance * 0.5f, ref installed, maxBionics);
            TryInstallBionic(pawn, "BionicStomach", DefDatabase<BodyPartDef>.GetNamedSilentFail("Stomach"), perPartChance * 0.5f, ref installed, maxBionics);
            TryInstallBionic(pawn, "BionicLung", BodyPartDefOf.Lung, perPartChance * 0.4f, ref installed, maxBionics);
            TryInstallBionic(pawn, "BionicKidney", DefDatabase<BodyPartDef>.GetNamedSilentFail("Kidney"), perPartChance * 0.4f, ref installed, maxBionics);
            TryInstallBionic(pawn, "BionicEar", DefDatabase<BodyPartDef>.GetNamedSilentFail("Ear"), perPartChance * 0.3f, ref installed, maxBionics);
        }

        private static void TryInstallBionic(Pawn pawn, string hediffDefName, BodyPartDef bodyPartDef, float chance, ref int installed, int maxBionics)
        {
            if (installed >= maxBionics || bodyPartDef == null || Rand.Value >= chance) return;

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (hediffDef == null) return;

            IEnumerable<BodyPartRecord> parts = pawn.RaceProps.body.GetPartsWithDef(bodyPartDef);
            if (parts == null || !parts.Any()) return;

            BodyPartRecord part = parts.RandomElement();
            if (pawn.health.hediffSet.HasDirectlyAddedPartFor(part)) return;
            if (pawn.health.hediffSet.PartIsMissing(part)) return;

            Hediff implant = HediffMaker.MakeHediff(hediffDef, pawn, part);
            pawn.health.AddHediff(implant);
            installed++;
        }

        public static void ApplyEliteTierHediff(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || !IsCrossedFactionPawn(pawn)) return;

            HediffDef tierDef = null;
            if (pawn.kindDef == CADefOf.CrossedWarlord) tierDef = CADefOf.WarlordTier;
            else if (pawn.kindDef == CADefOf.CrossedAlpha) tierDef = CADefOf.AlphaTier;
            else if (pawn.kindDef == CADefOf.MarkedMan) tierDef = CADefOf.MarkedTier;

            if (tierDef != null && !pawn.health.hediffSet.HasHediff(tierDef))
            {
                pawn.health.AddHediff(tierDef);
            }
        }

        public static void EnsurePredatorHediffs(Pawn pawn)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (pawn == null || pawn.health == null || !IsInfectedPawn(pawn))
            {
                return;
            }

            if (settings != null && settings.bloodlustEnabled && CADefOf.MarkedBloodlustNeed != null
                && pawn.needs != null && pawn.needs.TryGetNeed<Need_MarkedBloodlust>() == null)
            {
                pawn.needs.AllNeeds.Add(new Need_MarkedBloodlust(pawn));
            }

            if (settings != null && settings.anticipationEnabled && CADefOf.KillAnticipation != null
                && !pawn.health.hediffSet.HasHediff(CADefOf.KillAnticipation))
            {
                pawn.health.AddHediff(CADefOf.KillAnticipation);
            }
        }

        public static void NotifyBloodlustKill(Pawn killer, Pawn victim)
        {
            if (killer == null || killer.needs == null || !IsInfectedPawn(killer))
            {
                return;
            }

            Need_MarkedBloodlust need = killer.needs.TryGetNeed<Need_MarkedBloodlust>();
            if (need != null)
            {
                need.NotifyKilled();
            }

            if (CADefOf.FreshKillSatisfaction != null && killer.needs?.mood != null && victim != null)
            {
                Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(CADefOf.FreshKillSatisfaction);
                killer.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
        }

        public static bool IsCrossedPyromaniac(Pawn pawn)
        {
            return pawn?.kindDef == CADefOf.CrossedPyromaniac;
        }

        public static bool IsMolotovWeapon(ThingDef def)
        {
            return def != null
                && (def.defName == "Weapon_GrenadeMolotov"
                    || def.weaponTags != null && def.weaponTags.Contains("GrenadeMolotov"));
        }

        public static ThingDef GetMolotovWeaponDef()
        {
            ThingDef molotov = DefDatabase<ThingDef>.GetNamedSilentFail("Weapon_GrenadeMolotov");
            if (molotov != null)
                return molotov;

            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                if (IsMolotovWeapon(allDefs[i]))
                    return allDefs[i];
            }

            return null;
        }

        public static bool EnsureCrossedPyromaniacMolotov(Pawn pawn)
        {
            if (!IsCrossedPyromaniac(pawn) || pawn.equipment == null)
            {
                return false;
            }

            ThingWithComps current = pawn.equipment.Primary;
            if (current != null && !current.Destroyed && IsMolotovWeapon(current.def))
            {
                return true;
            }

            ThingDef molotov = GetMolotovWeaponDef();
            if (molotov == null)
            {
                return false;
            }

            List<ThingWithComps> allEquip = pawn.equipment.AllEquipmentListForReading;
            for (int i = allEquip.Count - 1; i >= 0; i--)
            {
                ThingWithComps eq = allEquip[i];
                if (eq == null || eq.Destroyed) continue;
                pawn.equipment.Remove(eq);
                eq.Destroy(DestroyMode.Vanish);
            }

            pawn.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(molotov));
            return true;
        }

        private static void GrantPsycastAbility(Pawn pawn, string abilityDefName)
        {
            AbilityDef abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
            if (abilityDef == null)
            {
                return;
            }

            if (pawn.abilities == null)
            {
                return;
            }

            foreach (Ability ability in pawn.abilities.AllAbilitiesForReading)
            {
                if (ability.def == abilityDef)
                {
                    return;
                }
            }

            pawn.abilities.GainAbility(abilityDef);
        }

        private static void GrantAbility(Pawn pawn, AbilityDef abilityDef)
        {
            if (abilityDef == null || pawn.abilities == null)
            {
                return;
            }

            foreach (Ability ability in pawn.abilities.AllAbilitiesForReading)
            {
                if (ability.def == abilityDef)
                {
                    return;
                }
            }

            pawn.abilities.GainAbility(abilityDef);
        }

        public static void RemoveCrossVirusIfImmune(Pawn pawn)
        {
            HediffDef virus = CADefOf.CrossVirus;
            if (pawn?.health == null || virus == null) return;
            if (!HasCrossVirusImmunity(pawn) && !HasMarkedVillageFounderImmunity(pawn)) return;
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(virus);
            if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
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
            TattooDef tattoo = GetCurrentCrossedFaceTattoo(pawn);
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
            CrossedContagionUtility.TryContagionPulse(pawn);
            CrossedCorpseUtility.TryContaminateNearbyCorpses(pawn);
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
            if (pawn.style != null)
            {
                TattooDef noFaceTattoo = TattooDefOf.NoTattoo_Face;
                if (CADefOf.IsCrossedFaceTattoo(pawn.style.nextFaceTattooDef))
                {
                    pawn.style.nextFaceTattooDef = noFaceTattoo;
                    changed = true;
                }

                if (CADefOf.IsCrossedFaceTattoo(pawn.style.FaceTattoo))
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

        public static void ApplyMarkedPanic(Map map, IntVec3 origin, float radius)
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
            AddKind(CADefOf.CrossedCivilian, 1f);
            AddKind(CADefOf.CrossedScout, 1f);
            AddKind(CADefOf.CrossedHunter, 1f);
            AddKind(CADefOf.CrossedShooter, 1f);
            AddKind(CADefOf.CrossedRaider, 1f);
            AddKind(CADefOf.CrossedSoldier, 1f);
            AddKind(CADefOf.CrossedPyromaniac, 1f);
            if (Rand.Chance(0.12f))
            {
                AddKind(CADefOf.CrossedBrute, 1f);
            }
            if (Rand.Chance(0.02f))
            {
                AddKind(CADefOf.CrossedAlpha, 1f);
            }
            if (Rand.Chance(0.005f))
            {
                AddKind(CADefOf.CrossedWarlord, 1f);
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

            if (HasRampageHediff(pawn))
            {
                return TryIssueRampageJob(pawn);
            }

            bool pyromaniac = CrossedUtility.IsCrossedPyromaniac(pawn);
            if (pyromaniac)
            {
                CrossedUtility.EnsureCrossedPyromaniacMolotov(pawn);
            }

            if (!pyromaniac && TheMarkedMenRjwCompatibility.TryStartBestInfectedIntercourseJob(pawn, true))
            {
                return true;
            }

            JobDef currentJobDef = pawn.CurJob?.def;
            Pawn currentPawnTarget = pawn.CurJob?.targetA.Pawn;
            if (currentPawnTarget != null && CrossedUtility.IsFullyTurnedMarkedPawn(currentPawnTarget) && !IsAttackJob(currentJobDef))
            {
                return TryRetargetAwayFromPawn(pawn, currentPawnTarget, true);
            }

            if (IsAttackJob(currentJobDef) && IsValidNonInfectedPawnTarget(currentPawnTarget, pawn) && !ShouldForceRangedAttack(pawn, currentJobDef))
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
                if (isAttackJob && currentPawnTarget == bestNonInfected && IsValidNonInfectedPawnTarget(currentPawnTarget, pawn) && !ShouldForceRangedAttack(pawn, currentJobDef))
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

        public static bool TryAttackNearestNonInfectedPawn(Pawn pawn, bool forceCurrentJob, bool allowRjwJob = true)
        {
            if (!CanUseTacticalAI(pawn))
            {
                return false;
            }

            if (HasRampageHediff(pawn))
            {
                return TryIssueRampageJob(pawn);
            }

            bool pyromaniac = CrossedUtility.IsCrossedPyromaniac(pawn);
            if (pyromaniac)
            {
                CrossedUtility.EnsureCrossedPyromaniacMolotov(pawn);
            }

            if (!pyromaniac && allowRjwJob && TheMarkedMenRjwCompatibility.TryStartBestInfectedIntercourseJob(pawn, forceCurrentJob))
            {
                return true;
            }

            Pawn target = FindBestNonInfectedPawnTarget(pawn);
            if (target == null)
            {
                return false;
            }

            JobDef currentJobDef = pawn.CurJob?.def;
            if (!forceCurrentJob && IsAttackJob(currentJobDef) && pawn.CurJob?.targetA.Pawn == target && !ShouldForceRangedAttack(pawn, currentJobDef))
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

        internal static bool TryAssignAttackJob(Pawn pawn, Thing target, bool forceCurrentJob = false)
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

            if (TryAssignAbilityAttackJob(pawn, target, forceCurrentJob))
            {
                return true;
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
                attackJob.verbToUse = verb;
                attackJob.expiryInterval = TacticalJobExpiryTicks;
                attackJob.checkOverrideOnExpire = true;
                attackJob.killIncappedTarget = !(target is Pawn attackPawnTarget && TheMarkedMenRjwCompatibility.ShouldKeepIncapacitatedTargetForIntercourse(pawn, attackPawnTarget));
                attackJob.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
                attackJob.canBashFences = true;
                attackJob.attackDoorIfTargetLost = TheMarkedMenSettings.DoorTargetingEnabled;
                attackJob.locomotionUrgency = LocomotionUrgency.Sprint;
                return TryTakeTacticalJob(pawn, attackJob, forceCurrentJob);
            }

            float dist = pawn.Position.DistanceTo(target.Position);
            float range = verb.verbProps.range;
            if (dist <= range * 1.5f || dist <= 12f)
            {
                Job closeJob = JobMaker.MakeJob(JobDefOf.Goto, target.Position, target);
                closeJob.expiryInterval = 30;
                closeJob.checkOverrideOnExpire = true;
                closeJob.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
                closeJob.canBashFences = true;
                closeJob.attackDoorIfTargetLost = TheMarkedMenSettings.DoorTargetingEnabled;
                closeJob.locomotionUrgency = LocomotionUrgency.Sprint;
                return TryTakeTacticalJob(pawn, closeJob, forceCurrentJob);
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

        private static bool TryAssignAbilityAttackJob(Pawn pawn, Thing target, bool forceCurrentJob)
        {
            if (pawn.abilities == null)
            {
                return false;
            }

            foreach (Ability ability in pawn.abilities.AllAbilitiesForReading)
            {
                if (ability == null || ability.verb == null || ability.verb.IsMeleeAttack || !ability.CanCast)
                {
                    continue;
                }

                if (ability.def.defName != "AcidSpray")
                {
                    continue;
                }

                if (ability.verb.CanHitTargetFrom(pawn.Position, target))
                {
                    Job job = ability.GetJob(target, target);
                    if (job == null)
                    {
                        continue;
                    }

                    job.expiryInterval = TacticalJobExpiryTicks;
                    job.checkOverrideOnExpire = true;
                    job.killIncappedTarget = !(target is Pawn attackPawnTarget && TheMarkedMenRjwCompatibility.ShouldKeepIncapacitatedTargetForIntercourse(pawn, attackPawnTarget));
                    job.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
                    job.canBashFences = true;
                    job.attackDoorIfTargetLost = TheMarkedMenSettings.DoorTargetingEnabled;
                    job.locomotionUrgency = LocomotionUrgency.Sprint;
                    return TryTakeTacticalJob(pawn, job, forceCurrentJob);
                }
            }

            return false;
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
            if (CrossedUtility.IsCrossedPyromaniac(pawn))
            {
                CrossedUtility.EnsureCrossedPyromaniacMolotov(pawn);
            }

            Verb verb = pawn?.equipment?.PrimaryEq?.PrimaryVerb;
            return verb != null && !verb.IsMeleeAttack ? verb : null;
        }

        private static bool ShouldForceRangedAttack(Pawn pawn, JobDef currentJobDef)
        {
            return CrossedUtility.IsCrossedPyromaniac(pawn)
                && currentJobDef != JobDefOf.AttackStatic
                && GetRangedVerb(pawn) != null;
        }

        private static bool IsTacticalRangedMoveJob(Job job, Thing target)
        {
            return job?.def == JobDefOf.Goto
                && target != null
                && job.targetB.Thing == target;
        }

        internal static Pawn FindBestNonInfectedPawnTarget(Pawn pawn)
        {
            IReadOnlyList<Pawn> candidates = pawn.Map?.mapPawns?.AllPawnsSpawned;
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            if (!pawn.IsHashIntervalTick(TheMarkedMenSettings.TacticalRetargetIntervalTicks))
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
            IntVec3 pos = pawn.Position;
            float maxDistSq = MaxTacticalTargetDistanceSquared;

            IReadOnlyList<Pawn> vulnerablePawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < vulnerablePawns.Count; i++)
            {
                Pawn candidate = vulnerablePawns[i];
                if (!candidate.Spawned || candidate.Position.DistanceToSquared(pos) > maxDistSq)
                {
                    continue;
                }

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
                if (!candidate.Spawned || candidate.Position.DistanceToSquared(pos) > maxDistSq)
                {
                    continue;
                }

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

            Need_MarkedBloodlust bloodlustNeed = searcher.needs?.TryGetNeed<Need_MarkedBloodlust>();
            if (bloodlustNeed != null)
            {
                score += bloodlustNeed.CurLevel * 15f;
            }

            return score * AggressionScoreMultiplier - Mathf.Sqrt(distanceSquared) * 1.15f;
        }

        private static bool IsIsolatedTarget(Pawn searcher, Pawn target)
        {
            Map map = searcher.Map;
            if (map == null)
            {
                return false;
            }

            const float allyRadiusSquared = 64f;
            int cellRadius = Mathf.CeilToInt(Mathf.Sqrt(allyRadiusSquared));
            CellRect cells = CellRect.CenteredOn(target.Position, cellRadius);
            cells.ClipInsideMap(map);

            for (int ci = cells.minZ; ci <= cells.maxZ; ci++)
            {
                for (int cj = cells.minX; cj <= cells.maxX; cj++)
                {
                    IntVec3 cell = new IntVec3(cj, 0, ci);
                    List<Thing> things = map.thingGrid.ThingsListAt(cell);
                    for (int t = 0; t < things.Count; t++)
                    {
                        Pawn other = things[t] as Pawn;
                        if (other == null || other == target || other.Dead || other.Downed || CrossedUtility.IsInfectedPawn(other))
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
                && TheMarkedMenSettings.TacticalRetargetingEnabled
                && !TheMarkedMenRjwCompatibility.ShouldPreserveCurrentRjwJob(pawn);
        }

        private static bool IsAttackJob(JobDef jobDef)
        {
            return jobDef == JobDefOf.AttackMelee
                || jobDef == JobDefOf.AttackStatic
                || jobDef == JobDefOf.CastAbilityOnThing;
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

        internal static bool HasRampageHediff(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.HasHediff(CADefOf.CrossedRampage) == true;
        }

        private static bool TryIssueRampageJob(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Map == null) return false;

            Pawn best = null;
            float bestDist = float.MaxValue;
            IntVec3 pos = pawn.Position;
            Map map = pawn.Map;

            IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn candidate = allPawns[i];
                if (candidate == pawn || candidate.Dead || candidate.Downed) continue;
                if (candidate.RaceProps == null || !candidate.RaceProps.Humanlike) continue;

                float dist = candidate.Position.DistanceToSquared(pos);
                if (dist >= bestDist) continue;

                best = candidate;
                bestDist = dist;
            }

            if (best == null) return false;

            return TryAssignAttackJob(pawn, best, true);
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

            Map map = source.Map;
            IntVec3 sourcePos = source.Position;
            float radiusSq = ContagionRadiusSquared;
            int radInt = Mathf.CeilToInt(ContagionRadius);
            CellRect rect = CellRect.CenteredOn(sourcePos, radInt);
            int exposedTargets = 0;

            for (int z = rect.minZ; z <= rect.maxZ; z++)
            {
                for (int x = rect.minX; x <= rect.maxX; x++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    float dx = cell.x - sourcePos.x;
                    float dz = cell.z - sourcePos.z;
                    if (dx * dx + dz * dz > radiusSq)
                    {
                        continue;
                    }

                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    List<Thing> things = map.thingGrid.ThingsListAt(cell);
                    for (int t = 0; t < things.Count; t++)
                    {
                        if (things[t] is Pawn target && CanContagionReach(source, target))
                        {
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
        private const float CorpseLingeringExposureRadius = 2.4f;
        private const float CorpseLingeringExposureRadiusSquared = CorpseLingeringExposureRadius * CorpseLingeringExposureRadius;
        private const float CorpseLingeringExposureChance = 0.10f;
        private const int CorpseLingeringMaxObservedTicksPerPulse = 750;
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

        public static void TryExposeNearbyPawnsToInfectedCorpses()
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings != null && !settings.infectionEnabled)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            if (maps == null)
            {
                return;
            }

            for (int i = 0; i < maps.Count; i++)
            {
                TryExposeNearbyPawnsToInfectedCorpses(maps[i]);
            }
        }

        private static void TryExposeNearbyPawnsToInfectedCorpses(Map map)
        {
            if (map?.listerThings == null || map.mapPawns == null)
            {
                return;
            }

            int maxTargets = TheMarkedMenSettings.MaxContagionTargetsPerPulse;
            if (maxTargets <= 0)
            {
                return;
            }

            TheMarkedMenGameComponent component = CrossedUtility.Component;
            if (component == null)
            {
                return;
            }

            List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            if (corpses == null || corpses.Count == 0)
            {
                return;
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            int observedTicks = Mathf.Min(TheMarkedMenSettings.CorpseContaminationIntervalTicks, CorpseLingeringMaxObservedTicksPerPulse);
            int exposedTargets = 0;
            float radiusSq = CorpseLingeringExposureRadiusSquared;
            int radInt = Mathf.CeilToInt(CorpseLingeringExposureRadius);

            for (int corpseIndex = 0; corpseIndex < corpses.Count; corpseIndex++)
            {
                Corpse corpse = corpses[corpseIndex] as Corpse;
                if (!IsInfectiousMarkedVirusCorpse(corpse))
                {
                    continue;
                }

                IntVec3 corpsePos = corpse.Position;
                CellRect rect = CellRect.CenteredOn(corpsePos, radInt);

                for (int z = rect.minZ; z <= rect.maxZ; z++)
                {
                    for (int x = rect.minX; x <= rect.maxX; x++)
                    {
                        IntVec3 cell = new IntVec3(x, 0, z);
                        float dx = cell.x - corpsePos.x;
                        float dz = cell.z - corpsePos.z;
                        if (dx * dx + dz * dz > radiusSq)
                        {
                            continue;
                        }

                        if (!cell.InBounds(map))
                        {
                            continue;
                        }

                        List<Thing> things = map.thingGrid.ThingsListAt(cell);
                        for (int t = 0; t < things.Count; t++)
                        {
                            if (things[t] is Pawn target && CanCorpseExposePawn(corpse, target))
                            {
                                if (!component.NoteCorpseLingering(target, currentTick, observedTicks))
                                {
                                    continue;
                                }

                                component.ResetCorpseLingering(target);
                                if (CrossedUtility.TryExpose(target, CorpseLingeringExposureChance, "lingering near infected corpse", corpse.InnerPawn))
                                {
                                    exposedTargets++;
                                    if (exposedTargets >= maxTargets)
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool IsInfectiousMarkedVirusCorpse(Corpse corpse)
        {
            Pawn innerPawn = corpse?.InnerPawn;
            if (innerPawn == null || corpse.Destroyed || innerPawn.Destroyed || !innerPawn.Dead)
            {
                return false;
            }

            if (innerPawn.RaceProps == null || !innerPawn.RaceProps.Humanlike)
            {
                return false;
            }

            return CrossedUtility.HasMarkedVirusHediff(innerPawn) || CrossedUtility.ShouldReanimateAsCrossed(innerPawn);
        }

        private static bool CanCorpseExposePawn(Corpse corpse, Pawn target)
        {
            if (corpse?.Map == null || target == null || target.Dead || !target.Spawned || target.Map != corpse.Map)
            {
                return false;
            }

            if (target.RaceProps == null || !target.RaceProps.Humanlike || CrossedUtility.IsInfectedPawn(target) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(target))
            {
                return false;
            }

            if (target.Position.DistanceToSquared(corpse.Position) > CorpseLingeringExposureRadiusSquared)
            {
                return false;
            }

            return GenSight.LineOfSight(target.Position, corpse.Position, target.Map);
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

            float chance = Mathf.Clamp01((initiator.kindDef == CADefOf.CrossedAlpha || initiator.kindDef == CADefOf.CrossedWarlord || initiator.kindDef == CADefOf.CrossedPyromaniac ? SocialPulseLeaderChance : SocialPulseBaseChance) * socialStrength);
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

            if (interactionDef == CADefOf.CrossedPackLaughter || initiator.kindDef == CADefOf.CrossedPyromaniac || initiator.kindDef == CADefOf.CrossedAlpha || initiator.kindDef == CADefOf.CrossedWarlord)
            {
                CrossedUtility.ApplyMarkedPanic(recipient.Map, recipient.Position, PackPanicRadius);
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

            if ((initiator.kindDef == CADefOf.CrossedPyromaniac || initiator.kindDef == CADefOf.CrossedAlpha || initiator.kindDef == CADefOf.CrossedWarlord) && CADefOf.CrossedPackLaughter != null && Rand.Chance(0.72f))
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

            if (interaction == CADefOf.CrossedPackLaughter && (initiator.kindDef == CADefOf.CrossedPyromaniac || initiator.kindDef == CADefOf.CrossedAlpha || initiator.kindDef == CADefOf.CrossedWarlord))
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
            return "The Marked have arrived.";
        }

        public static string BuildRaidLetterText(string baseText, List<Pawn> pawns, IncidentParms parms, bool horde)
        {
            return "The chronometer ticks. The Marked are here. Hold the line.";
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
            AddKindCount(parts, pawns, CADefOf.MarkedMan, "Marked Man");
            AddKindCount(parts, pawns, CADefOf.CrossedWarlord, "Warlord");
            AddKindCount(parts, pawns, CADefOf.CrossedAlpha, "Alpha");
            AddKindCount(parts, pawns, CADefOf.CrossedBrute, "Brute");
            AddKindCount(parts, pawns, CADefOf.CrossedSoldier, "Soldier");
            AddKindCount(parts, pawns, CADefOf.CrossedRaider, "Raider");
            AddKindCount(parts, pawns, CADefOf.CrossedHunter, "Hunter");
            AddKindCount(parts, pawns, CADefOf.CrossedShooter, "Shooter");
            AddKindCount(parts, pawns, CADefOf.CrossedPyromaniac, "Pyromaniac");
            AddKindCount(parts, pawns, CADefOf.CrossedScout, "Scout");
            AddKindCount(parts, pawns, CADefOf.CrossedCivilian, "Civilian");
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

            bool hasMarkedMan = HasKind(pawns, CADefOf.MarkedMan);
            bool hasWarlord = HasKind(pawns, CADefOf.CrossedWarlord);
            bool hasAlpha = HasKind(pawns, CADefOf.CrossedAlpha);
            bool hasBrute = HasKind(pawns, CADefOf.CrossedBrute);
            bool hasSoldier = HasKind(pawns, CADefOf.CrossedSoldier);
            bool hasPyromaniac = HasKind(pawns, CADefOf.CrossedPyromaniac);
            List<string> priorities = new List<string>();
            if (hasMarkedMan)
            {
                priorities.Add("Marked Men leading the assault");
            }

            if (hasWarlord)
            {
                priorities.Add("Warlords commanding infected forces");
            }

            if (hasAlpha)
            {
                priorities.Add("Alphas coordinating nearby infected");
            }

            if (hasSoldier)
            {
                priorities.Add("Soldiers maintaining tactical formation");
            }

            if (hasBrute)
            {
                priorities.Add("Brutes breaching doors and lines");
            }

            if (hasPyromaniac)
            {
                priorities.Add("Pyromaniacs spreading fire and chaos");
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
        private const int MinRaidCount = 3;
        private const int MaxRaidCount = 10;

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
            if (map == null)
            {
                return false;
            }

            TheMarkedMenGameComponent component = CrossedUtility.Component;
            parms.faction = crossed;
            if (component != null)
            {
                parms.points = component.CalculateEscalatedRaidPoints(parms.points);
            }

            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = false;
            TheMarkedMenGameComponent.ApplyMarkedRaidArrivalPattern(parms);

            int count = Rand.RangeInclusive(MinRaidCount, MaxRaidCount);
            List<Pawn> pawns = GenerateRaidPawns(count, parms.points, crossed);
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

            CrossedUtility.ApplyGeneratedRaidKindTuning(pawns);
            LordJob lordJob = new LordJob_AssaultColony(crossed, false, false, false, false, false, parms.points >= 700f, true);
            LordMaker.MakeNewLord(crossed, lordJob, map, pawns);
            SendRaidLetter(pawns, parms);
            component?.NotifyRaidLaunched(parms.points, pawns, map);
            return true;
        }

        private static List<Pawn> GenerateRaidPawns(int count, float points, Faction faction)
        {
            List<Pawn> pawns = new List<Pawn>(count + 1);

            Pawn leader = PawnGenerator.GeneratePawn(CADefOf.MarkedMan, faction);
            if (leader != null)
            {
                CrossedUtility.ApplyClassHediffs(leader);
                CrossedUtility.ApplyInfectedTattoo(leader);
                pawns.Add(leader);
            }

            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = PickRaidKind(points, count, false);
                if (kind == null)
                {
                    break;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
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

        private static PawnKindDef PickRaidKind(float points, int count, bool allowAlpha)
        {
            float normalizedThreat = Mathf.InverseLerp(5000f, 50000f, points);
            PawnKindDef selected = null;
            float totalWeight = 0f;

            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedCivilian, 14f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedScout, Mathf.Lerp(3f, 8f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedHunter, Mathf.Lerp(3f, 8f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedShooter, Mathf.Lerp(4f, 10f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedRaider, Mathf.Lerp(2f, 6f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedSoldier, Mathf.Lerp(1f, 5f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedBrute, Mathf.Lerp(0.5f, 3f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, Mathf.Lerp(1f, 3f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedAlpha, allowAlpha && count >= 8 && points >= 1000f ? 0.5f : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedWarlord, allowAlpha && count >= 12 && points >= 1800f ? 0.15f : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.MarkedMan, allowAlpha && count >= 15 && points >= 3000f ? 0.05f : 0f);

            return selected ?? CADefOf.CrossedCivilian ?? CADefOf.CrossedScout ?? CADefOf.CrossedHunter;
        }

        private void SendRaidLetter(List<Pawn> pawns, IncidentParms parms)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return;
            }

            string label = CrossedRaidAlertUtility.BuildRaidLetterLabel(def.letterLabel, null, parms.points);
            string text = CrossedRaidAlertUtility.BuildRaidLetterText(def.letterText, pawns, parms, false);
            LetterDef letterDef = def.letterDef ?? LetterDefOf.ThreatBig;
            Find.LetterStack.ReceiveLetter(label, text, letterDef, pawns[0]);
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

            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, false, false, false, false, false, points >= 700f, true), map, attackers);
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
            parms.canTimeoutOrFlee = false;
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
            LordMaker.MakeNewLord(crossed, new LordJob_AssaultColony(crossed, false, false, false, false, false, parms.points >= 700f, true), map, pawns);
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

            float normalizedThreat = Mathf.InverseLerp(5000f, 50000f, points);
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
            float pressure = Mathf.InverseLerp(5000f, 50000f, points);
            return TheMarkedMenSettings.ApplyRaidPointSettings(Mathf.Max(minThreatPoints, points * Mathf.Lerp(0.95f, 1.18f, pressure)));
        }

        private static float CurrentThreatScale()
        {
            Difficulty difficulty = Find.Storyteller?.difficulty;
            return Mathf.Max(0.1f, difficulty?.threatScale ?? 1f);
        }

        private static List<Pawn> GenerateHordePawns(int count, float points, Faction faction, Map map)
        {
            List<Pawn> pawns = new List<Pawn>(count + 1);

            Pawn leader = PawnGenerator.GeneratePawn(CADefOf.MarkedMan, faction, map.Tile);
            if (leader != null)
            {
                CrossedUtility.ApplyClassHediffs(leader);
                CrossedUtility.ApplyInfectedTattoo(leader);
                pawns.Add(leader);
            }

            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = PickHordeKind(points, count, false);
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

        private static PawnKindDef PickHordeKind(float points, int count, bool allowAlpha)
        {
            float normalizedThreat = Mathf.InverseLerp(5000f, 50000f, points);
            PawnKindDef selected = null;
            float totalWeight = 0f;

            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedCivilian, 14f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedScout, Mathf.Lerp(2f, 6f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedHunter, Mathf.Lerp(2.5f, 8.5f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedShooter, Mathf.Lerp(2f, 6f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedRaider, Mathf.Lerp(1f, 4f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedSoldier, Mathf.Lerp(0.5f, 3f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedBrute, Mathf.Lerp(1f, 4.5f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, 3.5f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedAlpha, allowAlpha && count >= 10 ? 0.55f : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedWarlord, allowAlpha && count >= 15 ? 0.15f : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.MarkedMan, allowAlpha && count >= 20 ? 0.05f : 0f);

            return selected ?? CADefOf.CrossedCivilian ?? CADefOf.CrossedScout ?? CADefOf.CrossedHunter;
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
            parms.canTimeoutOrFlee = false;
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
            LordMaker.MakeNewLord(crossed, new LordJob_AssaultColony(crossed, false, false, false, false, false, false, true), map, pawns);
            CrossedUtility.Component?.NotifyProbeLaunched(pawns.Count, parms.points);
            SendProbeLetter(pawns, parms);
            return true;
        }

        private static float CalculateProbePoints(Map map, float existingPoints, float minThreatPoints)
        {
            float storytellerPoints = map == null ? minThreatPoints : StorytellerUtility.DefaultThreatPointsNow(map);
            float points = Mathf.Max(existingPoints, storytellerPoints * 0.45f, minThreatPoints);
            return TheMarkedMenSettings.ApplyRaidPointSettings(points);
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

            float normalizedThreat = Mathf.InverseLerp(5000f, 50000f, points);
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
            float normalizedThreat = Mathf.InverseLerp(5000f, 50000f, points);
            PawnKindDef selected = null;
            float totalWeight = 0f;

            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedScout, Mathf.Lerp(4f, 6f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedHunter, Mathf.Lerp(3f, 5f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedShooter, Mathf.Lerp(2f, 4f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedCivilian, 3f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, points >= 220f ? Mathf.Lerp(0.5f, 1.75f, normalizedThreat) : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedRaider, points >= 350f ? 0.5f : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedBrute, points >= 500f ? 0.35f : 0f);

            return selected ?? CADefOf.CrossedScout ?? CADefOf.CrossedHunter ?? CADefOf.CrossedCivilian;
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

        [DebugAction(DebugCategory, "Init urban outbreak on this map", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 960)]
        public static void InitUrbanOutbreak()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            bool success = TheMarkedMenAncientUrbanRuinsIntegration.DebugInitializeCurrentMap();
            Report(success, "DevMode: Urban outbreak initialized on this map. Check the ruins for Marked Men.", "DevMode: This is not an Ancient Urban Ruins map or AUR is not loaded.");
        }

        [DebugAction(DebugCategory, "Fire urban ambush incident", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 950)]
        public static void FireUrbanAmbush()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            if (!TheMarkedMenAncientUrbanRuinsIntegration.IsAncientUrbanRuinsMap(Find.CurrentMap))
            {
                Report(false, null, "DevMode: This is not an Ancient Urban Ruins map.");
                return;
            }

            bool success = TheMarkedMenAncientUrbanRuinsIntegration.DebugFireIncident("CA_UrbanAmbush");
            Report(success, "DevMode: Urban ambush incident fired.", "DevMode: Could not fire urban ambush. Ensure the crossed faction exists.");
        }

        [DebugAction(DebugCategory, "Fire survivor encounter incident", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.Action, displayPriority = 940)]
        public static void FireUrbanSurvivor()
        {
            if (!TheMarkedMenSettings.DebugActionsEnabled)
            {
                Report(false, null, "DevMode: The Marked Men debug actions are disabled in mod settings.");
                return;
            }

            if (!TheMarkedMenAncientUrbanRuinsIntegration.IsAncientUrbanRuinsMap(Find.CurrentMap))
            {
                Report(false, null, "DevMode: This is not an Ancient Urban Ruins map.");
                return;
            }

            bool success = TheMarkedMenAncientUrbanRuinsIntegration.DebugFireIncident("CA_UrbanSurvivor");
            Report(success, "DevMode: Survivor encounter incident fired.", "DevMode: Could not fire survivor encounter. Ensure the crossed faction exists.");
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

            Pawn pawn = UI.MouseCell().GetThingList(Find.CurrentMap).OfType<Pawn>().FirstOrDefault();
            if (pawn == null)
            {
                Report(false, null, "DevMode: No pawn at cursor position.");
                return;
            }

            Hediff dormant = pawn.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
            if (dormant == null)
            {
                Report(false, null, "DevMode: Targeted pawn does not have the dormant mark.");
                return;
            }

            HediffComp_DormantMark comp = dormant.TryGetComp<HediffComp_DormantMark>();
            if (comp == null || comp.IsActivated)
            {
                Report(false, null, "DevMode: Dormant mark is already activated or comp missing.");
                return;
            }

            comp.AttemptTransformation("debug force trigger");
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

    public sealed class StorytellerCompProperties_CrossedStoryteller : StorytellerCompProperties
    {
        public float mtbDays = 0.95f;
        public float minRandomDaysPassed = 0.05f;
        public float minSpacingDays = 0.45f;
        public FloatRange pointsFactorRange = new FloatRange(1.05f, 1.85f);
        public float storytellerThreatScaleMultiplier = 5f;

        public StorytellerCompProperties_CrossedStoryteller()
        {
            compClass = typeof(StorytellerComp_CrossedStoryteller);
        }
    }

    public sealed class StorytellerComp_CrossedStoryteller : StorytellerComp
    {
        private int lastIncidentTick = -999999;

        private StorytellerCompProperties_CrossedStoryteller Props => (StorytellerCompProperties_CrossedStoryteller)props;

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            if (!(target is Map map) || !map.IsPlayerHome || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
            {
                yield break;
            }

            if (Find.TickManager == null || Find.TickManager.TicksGame < Mathf.RoundToInt(Props.minRandomDaysPassed * GenDate.TicksPerDay))
            {
                yield break;
            }

            int ticks = Find.TickManager.TicksGame;
            int minSpacingTicks = Mathf.RoundToInt(Mathf.Max(0.1f, Props.minSpacingDays) * GenDate.TicksPerDay);
            if (ticks - lastIncidentTick < minSpacingTicks)
            {
                yield break;
            }

            float frequency = Mathf.Max(0.05f, TheMarkedMenMod.Settings?.EffectiveMarkedRaidFrequencyMultiplier ?? 1f);
            float mtbDays = Mathf.Max(0.15f, Props.mtbDays / frequency);
            if (!Rand.MTBEventOccurs(mtbDays, GenDate.TicksPerDay, 1000f))
            {
                yield break;
            }

            IncidentDef incident = PickRandomMarkedIncident();
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (incident == null || crossed == null)
            {
                yield break;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
            parms.target = map;
            parms.faction = crossed;
            parms.points = BuildRandomIncidentPoints(map, incident, parms.points);
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = false;
            parms.forced = false;
            TheMarkedMenGameComponent.ApplyMarkedRaidArrivalPattern(parms);

            if (!incident.Worker.CanFireNow(parms))
            {
                yield break;
            }

            lastIncidentTick = ticks;
            yield return new FiringIncident(incident, this, parms);
        }

        private IncidentDef PickRandomMarkedIncident()
        {
            IncidentDef selected = null;
            float totalWeight = 0f;
            AddIncidentCandidate(ref selected, ref totalWeight, CADefOf.CrossedProbe, TheMarkedMenSettings.ProbesEnabled ? 4.5f : 0f);
            AddIncidentCandidate(ref selected, ref totalWeight, CADefOf.CrossedRaid, TheMarkedMenSettings.WarbandsEnabled ? 3.0f : 0f);
            AddIncidentCandidate(ref selected, ref totalWeight, CADefOf.CrossedHorde, TheMarkedMenSettings.HordesEnabled ? 1.75f : 0f);
            AddIncidentCandidate(ref selected, ref totalWeight, DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedDownedSurvivor"), 1.15f);
            return selected;
        }

        private static void AddIncidentCandidate(ref IncidentDef selected, ref float totalWeight, IncidentDef incident, float weight)
        {
            if (incident == null || weight <= 0f)
            {
                return;
            }

            totalWeight += weight;
            if (Rand.Value * totalWeight <= weight)
            {
                selected = incident;
            }
        }

        private float BuildRandomIncidentPoints(Map map, IncidentDef incident, float existingPoints)
        {
            float minimum = Mathf.Max(incident?.minThreatPoints ?? 120f, TheMarkedMenMod.Settings?.minimumRaidPoints ?? 120f);
            float storytellerPoints = map == null ? minimum : StorytellerUtility.DefaultThreatPointsNow(map);
            float points = Mathf.Max(existingPoints, storytellerPoints, minimum);
            float pressure = Mathf.InverseLerp(5000f, 50000f, points);
            float pressureFactor = Mathf.Lerp(1.05f, 1.35f, pressure);
            float randomFactor = Props.pointsFactorRange.RandomInRange;
            float storytellerFactor = CalculateStorytellerThreatFactor();
            return TheMarkedMenSettings.ApplyRaidPointSettings(Mathf.Max(minimum, points * pressureFactor * randomFactor * storytellerFactor));
        }

        private float CalculateStorytellerThreatFactor()
        {
            Difficulty difficulty = Find.Storyteller?.difficulty;
            float rawThreatScale = Mathf.Max(0.01f, difficulty?.threatScale ?? 1f);
            float normalizedThreatScale = rawThreatScale > 10f ? rawThreatScale / 100f : rawThreatScale;
            return Mathf.Clamp(normalizedThreatScale * Mathf.Max(1f, Props.storytellerThreatScaleMultiplier), 1f, 20f);
        }
    }

    public sealed class HediffCompProperties_PsychicPulse : HediffCompProperties
    {
        public float radius = 14f;
        public int pulseIntervalTicks = 500;

        public HediffCompProperties_PsychicPulse()
        {
            compClass = typeof(HediffComp_PsychicPulse);
        }
    }

    public sealed class HediffComp_PsychicPulse : HediffComp
    {
        private HediffCompProperties_PsychicPulse Props => (HediffCompProperties_PsychicPulse)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (!parent.pawn.Spawned || !parent.pawn.IsHashIntervalTick(Props.pulseIntervalTicks))
            {
                return;
            }

            Pawn pawn = parent.pawn;
            Map map = pawn.Map;
            if (map == null) return;

            float radius = Props.radius;
            float radiusSq = radius * radius;
            int radInt = Mathf.CeilToInt(radius);
            IntVec3 pos = pawn.Position;
            CellRect rect = CellRect.CenteredOn(pos, radInt);

            for (int z = rect.minZ; z <= rect.maxZ; z++)
            {
                for (int x = rect.minX; x <= rect.maxX; x++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    if (!cell.InBounds(map)) continue;

                    float dx = cell.x - pos.x;
                    float dz = cell.z - pos.z;
                    if (dx * dx + dz * dz > radiusSq)
                    {
                        continue;
                    }

                    List<Thing> things = map.thingGrid.ThingsListAt(cell);
                    for (int i = 0; i < things.Count; i++)
                    {
                        if (things[i] is Pawn other && other != pawn && !other.Dead && other.RaceProps.Humanlike && CrossedUtility.IsInfectedPawn(other))
                        {
                            Hediff hediff = other.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CrossVirus);
                            if (hediff != null)
                            {
                                hediff.Severity = Mathf.Min(hediff.Severity + 0.01f, 1f);
                            }
                        }
                    }
                }
            }
        }
    }



    public sealed class IncidentWorker_CrossedDownedSurvivor : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            if (!(parms.target is Map map) || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned) return false;
            return CrossedUtility.Component?.EnsureCrossedFaction() != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (map == null || crossed == null) return false;

            PawnKindDef kind = CADefOf.CrossedCivilian ?? PawnKindDefOf.SpaceRefugee;
            Pawn survivor = PawnGenerator.GeneratePawn(kind, crossed);
            if (survivor == null) return false;

            IntVec3 dropSpot = CellFinderLoose.RandomCellWith((IntVec3 c) => c.Standable(map) && !c.Fogged(map) && c.DistanceToEdge(map) > 10, map, 100);
            if (dropSpot == IntVec3.Invalid)
            {
                dropSpot = CellFinderLoose.RandomCellWith((IntVec3 c) => c.Standable(map) && !c.Fogged(map), map, 100);
            }

            GenSpawn.Spawn(survivor, dropSpot, map, Rot4.Random);
            HealthUtility.DamageUntilDowned(survivor);
            CrossedUtility.ApplyInfectedTattoo(survivor);

            string label = def.letterLabel ?? "Infected survivor downed";
            string text = def.letterText ?? "A critically infected survivor has collapsed near the colony.";
            Find.LetterStack.ReceiveLetter(label, text, def.letterDef ?? LetterDefOf.ThreatSmall, new LookTargets(survivor));
            return true;
        }
    }

    public sealed class IncidentWorker_CrossedCaravanAmbush : IncidentWorker_Ambush
    {
        private const int MinAmbushCount = 3;
        private const int MaxAmbushCount = 10;

        private static bool IsMarkedManStoryteller => Find.Storyteller?.def?.defName == "CA_TheMarkedMan";

        public override float ChanceFactorNow(IIncidentTarget target)
        {
            float baseChance = base.ChanceFactorNow(target);
            if (IsMarkedManStoryteller)
            {
                return baseChance * 5f;
            }
            return baseChance * 3f;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }

            if (CrossedUtility.Component?.EnsureCrossedFaction() == null)
            {
                return false;
            }

            if (parms.target is Caravan caravan)
            {
                if (caravan.PawnsListForReading.Count < 1)
                {
                    return false;
                }
            }

            return true;
        }

        protected override List<Pawn> GeneratePawns(IncidentParms parms)
        {
            Caravan caravan = parms.target as Caravan;
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (crossed == null)
            {
                return new List<Pawn>();
            }

            float points = CalculateAmbushPoints(parms);
            parms.points = points;
            parms.faction = crossed;

            int count = CalculateAmbushCount(points, parms.pawnCount, caravan);
            List<Pawn> pawns = GenerateAmbushPawns(count, points, crossed);
            return pawns;
        }

        protected override void PostProcessGeneratedPawnsAfterSpawning(List<Pawn> generatedPawns)
        {
            CrossedUtility.ApplyGeneratedRaidKindTuning(generatedPawns);
        }

        protected override LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
        {
            bool useBreachers = IsMarkedManStoryteller || (parms.points >= 600f);
            return new LordJob_AssaultColony(parms.faction, false, false, false, false, false, useBreachers, true);
        }

        protected override string GetLetterLabel(Pawn anyPawn, IncidentParms parms)
        {
            return def.letterLabel ?? "Marked Men ambush";
        }

        protected override string GetLetterText(Pawn anyPawn, IncidentParms parms)
        {
            return def.letterText ?? "A pack of Marked Men has ambushed the caravan! Fight through them or fall back to reform.";
        }

        protected override LetterDef GetLetterDef(Pawn anyPawn, IncidentParms parms)
        {
            return def.letterDef ?? LetterDefOf.ThreatSmall;
        }

        private float CalculateAmbushPoints(IncidentParms parms)
        {
            float basePoints = parms.points;
            if (basePoints <= 0f)
            {
                if (parms.target is Caravan caravan)
                {
                    basePoints = StorytellerUtility.DefaultThreatPointsNow(caravan);
                }
                else if (parms.target is Map map)
                {
                    basePoints = StorytellerUtility.DefaultThreatPointsNow(map);
                }
            }

            float points = Mathf.Max(basePoints, def.minThreatPoints);

            if (IsMarkedManStoryteller)
            {
                points *= 1.8f;
            }

            return TheMarkedMenSettings.ApplyRaidPointSettings(points);
        }

        private int CalculateAmbushCount(float points, int requestedCount, Caravan caravan)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            int minCount = MinAmbushCount;
            int maxCount = MaxAmbushCount;

            if (IsMarkedManStoryteller)
            {
                minCount = Mathf.Max(minCount, 3);
                maxCount = Mathf.Min(maxCount + 2, 14);
            }

            if (requestedCount > 0)
            {
                return Mathf.Clamp(requestedCount, minCount, maxCount);
            }

            float normalizedThreat = Mathf.InverseLerp(5000f, 50000f, points);
            int expected = Mathf.RoundToInt(Mathf.Lerp(minCount, maxCount, normalizedThreat));

            if (IsMarkedManStoryteller)
            {
                expected = Mathf.RoundToInt(expected * 1.4f);
            }

            int variance = Mathf.Clamp(Mathf.RoundToInt(expected * 0.2f), 1, 3);
            return Rand.RangeInclusive(Mathf.Max(minCount, expected - variance), Mathf.Min(maxCount, expected + variance));
        }

        private List<Pawn> GenerateAmbushPawns(int count, float points, Faction faction)
        {
            List<Pawn> pawns = new List<Pawn>(count);
            bool alphaAdded = false;
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = PickAmbushKind(points, count, !alphaAdded);
                if (kind == null)
                {
                    break;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
                if (pawn == null)
                {
                    continue;
                }

                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);

                alphaAdded = alphaAdded || kind == CADefOf.CrossedAlpha || kind == CADefOf.CrossedWarlord || kind == CADefOf.MarkedMan;
                pawns.Add(pawn);
            }

            return pawns;
        }

        private PawnKindDef PickAmbushKind(float points, int count, bool allowAlpha)
        {
            float normalizedThreat = Mathf.InverseLerp(5000f, 50000f, points);
            float storytellerFactor = IsMarkedManStoryteller ? 1.5f : 1f;

            PawnKindDef selected = null;
            float totalWeight = 0f;

            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedCivilian, 8f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedHunter, Mathf.Lerp(3f, 10f, normalizedThreat) * storytellerFactor);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedScout, Mathf.Lerp(2f, 6f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, Mathf.Lerp(1f, 4f, normalizedThreat) * storytellerFactor);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedBrute, Mathf.Lerp(0.5f, 3f, normalizedThreat) * storytellerFactor);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.CrossedAlpha, allowAlpha && count >= 8 ? 0.4f : 0f);

            return selected ?? CADefOf.CrossedCivilian ?? CADefOf.CrossedHunter ?? CADefOf.CrossedScout;
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
    }
}
