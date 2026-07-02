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
        private const string DefaultPresetName = "Default";
        private const string OutbreakPresetName = "Outbreak simulator";
        private const string CasualPresetName = "Casual";
        private const string VanillaLikePresetName = "Vanilla-like";
        private const string BrutalPresetName = "Brutal";
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
        public int tacticalRetargetIntervalTicks = 60;
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
        public float prisonerEscapeChance = 0.04f;

        private int settingsVersion = CurrentSettingsVersion;
        private string currentPreset = OutbreakPresetName;
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

        public static int TacticalRetargetIntervalTicks => Mathf.Clamp(TheMarkedMenMod.Settings?.tacticalRetargetIntervalTicks ?? 60, 1, 2500);

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
            Scribe_Values.Look(ref tacticalRetargetIntervalTicks, "tacticalRetargetIntervalTicks", 60);
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
            Scribe_Values.Look(ref prisonerEscapeChance, "prisonerEscapeChance", 0.04f);

            Scribe_Values.Look(ref currentPreset, "currentPreset", OutbreakPresetName);
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
                    currentPreset = DefaultPresetName;
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

                if (loadedSettingsVersion < 11 && (string.IsNullOrEmpty(currentPreset) || currentPreset == DefaultPresetName))
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
            DrawInt(listing, "Ticks between tactical retarget checks", ref tacticalRetargetIntervalTicks, 1, 2500, "tacticalRetargetIntervalTicks", "How often infected pawns can reconsider tactical targets. Higher values improve performance by reducing scans for new targets.");
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
            DrawFloat(listing, "Daily escape chance", ref prisonerEscapeChance, 0f, 1f, "prisonerEscapeChance", "Base chance per day that a Marked prisoner independently attempts to break out. They attack and try to infect anyone nearby during the escape.");
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
            DrawPresetButton(new Rect(row.x, row.y, buttonWidth, row.height), CasualPresetName, "Slower raids, lower exposure risk, smaller hordes, and more immune survivors.", ApplyCasualPreset);
            DrawPresetButton(new Rect(row.x + (buttonWidth + PresetButtonGap), row.y, buttonWidth, row.height), VanillaLikePresetName, "Keeps the faction dangerous while staying closer to ordinary RimWorld pacing.", ApplyVanillaLikePreset);
            DrawPresetButton(new Rect(row.x + ((buttonWidth + PresetButtonGap) * 2f), row.y, buttonWidth, row.height), DefaultPresetName, "Restores the intended baseline tuning for the mod.", () => ApplyDefaultPreset(true));
            DrawPresetButton(new Rect(row.x + ((buttonWidth + PresetButtonGap) * 3f), row.y, buttonWidth, row.height), BrutalPresetName, "Faster, harder, less forgiving raids with stronger infection pressure.", ApplyBrutalPreset);
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
            prisonerEscapeChance = Mathf.Clamp01(prisonerEscapeChance);
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
            currentPreset = CasualPresetName;
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
            currentPreset = VanillaLikePresetName;
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
            currentPreset = BrutalPresetName;
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
            currentPreset = OutbreakPresetName;
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
                currentPreset = OutbreakPresetName;
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
            tacticalRetargetIntervalTicks = 60;
            infightingCheckIntervalTicks = 1000;
            lordCleanupIntervalTicks = 250;
            infectedStateMaintenanceIntervalTicks = 2500;
            reanimationProcessIntervalTicks = 2500;
            maxPendingReanimationsPerTick = 24;
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
            MapClassificationService.PruneDestroyedMaps();
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
