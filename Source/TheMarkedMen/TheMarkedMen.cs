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
        private const int CurrentSettingsVersion = 14;
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
        private const string DefaultPresetName = "Normal";
        private const string OutbreakPresetName = "Very Hard";
        private const string CasualPresetName = "Very Easy";
        private const string VanillaLikePresetName = "Easy";
        private const string BrutalPresetName = "Hard";
        private static readonly Color HelpTextColor = new Color(0.72f, 0.72f, 0.72f);
        private static readonly Color SectionHeaderColor = new Color(0.22f, 0.24f, 0.26f, 1f);
        private static readonly Color SectionHeaderHoverColor = new Color(0.28f, 0.30f, 0.33f, 1f);
        private static readonly Color SectionToggleColor = new Color(0.10f, 0.11f, 0.12f, 1f);
        private static readonly Color SectionToggleHoverColor = new Color(0.16f, 0.17f, 0.19f, 1f);
        private const float OptionRowHeight = 28f;
        private const float SectionHeaderHeight = 38f;
        private const float SectionToggleWidth = 48f;

        public bool infectionEnabled = true;
        public bool colonistsCanBeInfected = true;
        public bool alliesCanBeInfected = true;
        public bool enemiesCanBeInfected = true;
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
        public float minimumRaidPoints = 2000f;
        public float maximumRaidPoints = 10000f;
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
        public bool rangedTransmissionEnabled;
        public float rangedInfectionChance = InfectionTransmissionChance;

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
        public bool markedPanicEnabled = true;
        public float markedPanicRadius = 12f;
        public int markedPanicDurationTicks = 18000;
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
        public bool prisonerRestraintEnabled = true;
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

        public static bool RangedTransmissionEnabled => TheMarkedMenMod.Settings?.rangedTransmissionEnabled == true;

        public static float RangedInfectionChance => Mathf.Clamp01(TheMarkedMenMod.Settings?.rangedInfectionChance ?? InfectionTransmissionChance);

        public static bool WarcasketsBlockExposure => TheMarkedMenMod.Settings?.warcasketsBlockExposure != false;
        public static bool VacsuitBlockExposure => TheMarkedMenMod.Settings?.vacsuitBlockExposure != false;
        public static bool GasMasksBlockExposure => TheMarkedMenMod.Settings?.gasMasksBlockExposure != false;
        public static bool SealedArmorBlockExposure => TheMarkedMenMod.Settings?.sealedArmorBlockExposure != false;

        public static bool TacticalRetargetingEnabled => TheMarkedMenMod.Settings?.tacticalRetargetingEnabled != false;

        public static bool PriorityTargetingEnabled => TheMarkedMenMod.Settings?.priorityTargetingEnabled != false;

        public static bool DoorTargetingEnabled => TheMarkedMenMod.Settings?.doorTargetingEnabled != false;

        public static float InfightingChance => Mathf.Clamp01(TheMarkedMenMod.Settings?.infightingChance ?? 0.12f);

        public static float SocialTerrorStrength => Mathf.Clamp(TheMarkedMenMod.Settings?.socialTerrorStrength ?? 1f, 0f, 5f);

        public static bool MarkedPanicEnabled => TheMarkedMenMod.Settings?.markedPanicEnabled != false;

        public static float MarkedPanicRadius => Mathf.Clamp(TheMarkedMenMod.Settings?.markedPanicRadius ?? 12f, 0f, 100f);

        public static int MarkedPanicDurationTicks => Mathf.Clamp(TheMarkedMenMod.Settings?.markedPanicDurationTicks ?? 18000, 60, GenDate.TicksPerDay * 30);

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
            Scribe_Values.Look(ref colonistsCanBeInfected, "colonistsCanBeInfected", true);
            Scribe_Values.Look(ref alliesCanBeInfected, "alliesCanBeInfected", true);
            Scribe_Values.Look(ref enemiesCanBeInfected, "enemiesCanBeInfected", true);
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
            Scribe_Values.Look(ref minimumRaidPoints, "minimumRaidPoints", 2000f);
            Scribe_Values.Look(ref maximumRaidPoints, "maximumRaidPoints", 10000f);
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
            Scribe_Values.Look(ref rangedTransmissionEnabled, "rangedTransmissionEnabled", false);
            Scribe_Values.Look(ref rangedInfectionChance, "rangedInfectionChance", InfectionTransmissionChance);
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
            Scribe_Values.Look(ref markedPanicEnabled, "markedPanicEnabled", true);
            Scribe_Values.Look(ref markedPanicRadius, "markedPanicRadius", 12f);
            Scribe_Values.Look(ref markedPanicDurationTicks, "markedPanicDurationTicks", 18000);
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
            Scribe_Values.Look(ref prisonerRestraintEnabled, "prisonerRestraintEnabled", true);
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
                    minimumRaidPoints = 2000f;
                    maximumRaidPoints = 10000f;
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
                    ApplyBaselinePreset(false);
                }

                if (loadedSettingsVersion < 13)
                {
                    maximumRaidPoints = 10000f;
                }

                if (loadedSettingsVersion < 14)
                {
                    markedPanicEnabled = true;
                    markedPanicRadius = 12f;
                    markedPanicDurationTicks = 18000;
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

            DrawSectionHeader(listing, "MarkedMen_Settings_Core".Translate(), "MarkedMen_Settings_CoreDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_AllowNewInfections".Translate(), ref infectionEnabled, "MarkedMen_Settings_AllowNewInfectionsDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_InfectColonists".Translate(), ref colonistsCanBeInfected, "MarkedMen_Settings_InfectColonistsDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_InfectAllies".Translate(), ref alliesCanBeInfected, "MarkedMen_Settings_InfectAlliesDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_InfectEnemies".Translate(), ref enemiesCanBeInfected, "MarkedMen_Settings_InfectEnemiesDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_LogCompatibilityMods".Translate(), ref verboseCompatibilityLogging, "MarkedMen_Settings_LogCompatibilityModsDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_RaidSchedule".Translate(), "MarkedMen_Settings_RaidScheduleDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_EnableWarbands".Translate(), ref scheduledWarbandsEnabled, "MarkedMen_Settings_EnableWarbandsDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_EnableHordes".Translate(), ref scheduledHordesEnabled, "MarkedMen_Settings_EnableHordesDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_EnableProbes".Translate(), ref scoutingProbesEnabled, "MarkedMen_Settings_EnableProbesDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_RandomizeRaids".Translate(), ref randomizeMarkedRaids, "MarkedMen_Settings_RandomizeRaidsDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_firstMarkedRaidDay".Translate(), ref firstMarkedRaidDay, 1, 600, "firstMarkedRaidDay", "MarkedMen_Settings_firstMarkedRaidDayDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_markedRaidFrequencyMultiplier".Translate(), ref markedRaidFrequencyMultiplier, MinMarkedRaidFrequencyMultiplier, MaxMarkedRaidFrequencyMultiplier, "markedRaidFrequencyMultiplier", "MarkedMen_Settings_markedRaidFrequencyMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_warbandFrequencyMultiplier".Translate(), ref warbandFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "warbandFrequencyMultiplier", "MarkedMen_Settings_warbandFrequencyMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_hordeFrequencyMultiplier".Translate(), ref hordeFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "hordeFrequencyMultiplier", "MarkedMen_Settings_hordeFrequencyMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_probeFrequencyMultiplier".Translate(), ref probeFrequencyMultiplier, 0f, MaxMarkedRaidFrequencyMultiplier, "probeFrequencyMultiplier", "MarkedMen_Settings_probeFrequencyMultiplierDesc".Translate());
            DrawHelp(listing, "MarkedMen_Settings_EffectiveFrequencies".Translate(MultiplierText(EffectiveWarbandFrequencyMultiplier), MultiplierText(EffectiveHordeFrequencyMultiplier), MultiplierText(EffectiveProbeFrequencyMultiplier)));
            DrawFloat(listing, "MarkedMen_Settings_raidPointsMultiplier".Translate(), ref raidPointsMultiplier, 0.05f, 1E+09f, "raidPointsMultiplier", "MarkedMen_Settings_raidPointsMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_minimumRaidPoints".Translate(), ref minimumRaidPoints, 0f, maximumRaidPoints, "minimumRaidPoints", "MarkedMen_Settings_minimumRaidPointsDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_maximumRaidPoints".Translate(), ref maximumRaidPoints, minimumRaidPoints, 100000f, "maximumRaidPoints", "MarkedMen_Settings_maximumRaidPointsDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_raidEscalationPerRaid".Translate(), ref raidEscalationPerRaid, 0f, 2f, "raidEscalationPerRaid", "MarkedMen_Settings_raidEscalationPerRaidDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_raidEscalationMaxBonus".Translate(), ref raidEscalationMaxBonus, 0f, 20f, "raidEscalationMaxBonus", "MarkedMen_Settings_raidEscalationMaxBonusDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_GroupedEdgeArrivals".Translate(), ref allowGroupedEdgeArrival, "MarkedMen_Settings_GroupedEdgeArrivalsDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_SplitGroupEdgeArrivals".Translate(), ref allowDistributedGroupArrival, "MarkedMen_Settings_SplitGroupEdgeArrivalsDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_ScatteredEdgeArrivals".Translate(), ref allowDistributedArrival, "MarkedMen_Settings_ScatteredEdgeArrivalsDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_SingleEdgeArrivals".Translate(), ref allowSingleEdgeArrival, "MarkedMen_Settings_SingleEdgeArrivalsDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_EnemyMix".Translate(), "MarkedMen_Settings_EnemyMixDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_civilianWeightMultiplier".Translate(), ref civilianWeightMultiplier, 0f, 5f, "civilianWeightMultiplier", "MarkedMen_Settings_civilianWeightMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_scoutWeightMultiplier".Translate(), ref scoutWeightMultiplier, 0f, 5f, "scoutWeightMultiplier", "MarkedMen_Settings_scoutWeightMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_hunterWeightMultiplier".Translate(), ref hunterWeightMultiplier, 0f, 5f, "hunterWeightMultiplier", "MarkedMen_Settings_hunterWeightMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_shooterWeightMultiplier".Translate(), ref shooterWeightMultiplier, 0f, 5f, "shooterWeightMultiplier", "MarkedMen_Settings_shooterWeightMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_raiderWeightMultiplier".Translate(), ref raiderWeightMultiplier, 0f, 5f, "raiderWeightMultiplier", "MarkedMen_Settings_raiderWeightMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_soldierWeightMultiplier".Translate(), ref soldierWeightMultiplier, 0f, 5f, "soldierWeightMultiplier", "MarkedMen_Settings_soldierWeightMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_bruteWeightMultiplier".Translate(), ref bruteWeightMultiplier, 0f, 5f, "bruteWeightMultiplier", "MarkedMen_Settings_bruteWeightMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_pyromaniacWeightMultiplier".Translate(), ref pyromaniacWeightMultiplier, 0f, 5f, "pyromaniacWeightMultiplier", "MarkedMen_Settings_pyromaniacWeightMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_alphaWeightMultiplier".Translate(), ref alphaWeightMultiplier, 0f, 5f, "alphaWeightMultiplier", "MarkedMen_Settings_alphaWeightMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_warlordWeightMultiplier".Translate(), ref warlordWeightMultiplier, 0f, 5f, "warlordWeightMultiplier", "MarkedMen_Settings_warlordWeightMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_markedManWeightMultiplier".Translate(), ref markedManWeightMultiplier, 0f, 5f, "markedManWeightMultiplier", "MarkedMen_Settings_markedManWeightMultiplierDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_minimumHordeSize".Translate(), ref minimumHordeSize, 1, 50, "minimumHordeSize", "MarkedMen_Settings_minimumHordeSizeDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_maximumHordeSize".Translate(), ref maximumHordeSize, 1, 100, "maximumHordeSize", "MarkedMen_Settings_maximumHordeSizeDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_minimumProbeSize".Translate(), ref minimumProbeSize, 1, 20, "minimumProbeSize", "MarkedMen_Settings_minimumProbeSizeDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_maximumProbeSize".Translate(), ref maximumProbeSize, 1, 30, "maximumProbeSize", "MarkedMen_Settings_maximumProbeSizeDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_maximumAlphasPerRaid".Translate(), ref maximumAlphasPerRaid, 0, 99, "maximumAlphasPerRaid", "MarkedMen_Settings_maximumAlphasPerRaidDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_VirusAndCorpses".Translate(), "MarkedMen_Settings_VirusAndCorpsesDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_bloodExposureChance".Translate(), ref bloodExposureChance, 0f, 1f, "bloodExposureChance", "MarkedMen_Settings_bloodExposureChanceDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_foodExposureChance".Translate(), ref foodExposureChance, 0f, 1f, "foodExposureChance", "MarkedMen_Settings_foodExposureChanceDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_infectedAssaultExposureChance".Translate(), ref infectedAssaultExposureChance, 0f, 1f, "infectedAssaultExposureChance", "MarkedMen_Settings_infectedAssaultExposureChanceDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_closeContactExposureChance".Translate(), ref closeContactExposureChance, 0f, 1f, "closeContactExposureChance", "MarkedMen_Settings_closeContactExposureChanceDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_corpseContaminationChance".Translate(), ref corpseContaminationChance, 0f, 1f, "corpseContaminationChance", "MarkedMen_Settings_corpseContaminationChanceDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_infectionProgressionSpeedMultiplier".Translate(), ref infectionProgressionSpeedMultiplier, 0.05f, 10f, "infectionProgressionSpeedMultiplier", "MarkedMen_Settings_infectionProgressionSpeedMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_incubationDurationMultiplier".Translate(), ref incubationDurationMultiplier, 0.05f, 10f, "incubationDurationMultiplier", "MarkedMen_Settings_incubationDurationMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_immunitySurvivalChance".Translate(), ref immunitySurvivalChance, 0f, 1f, "immunitySurvivalChance", "MarkedMen_Settings_immunitySurvivalChanceDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_terminalTransformationWeight".Translate(), ref terminalTransformationWeight, 0f, 10f, "terminalTransformationWeight", "MarkedMen_Settings_terminalTransformationWeightDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_terminalDeathWeight".Translate(), ref terminalDeathWeight, 0f, 10f, "terminalDeathWeight", "MarkedMen_Settings_terminalDeathWeightDesc".Translate());
            DrawHelp(listing, "MarkedMen_Settings_TerminalOutcome".Translate(PercentText(CurrentTerminalTransformationChance(null)), PercentText(1f - CurrentTerminalTransformationChance(null))));
            DrawFloat(listing, "MarkedMen_Settings_reanimationChance".Translate(), ref reanimationChance, 0f, 1f, "reanimationChance", "MarkedMen_Settings_reanimationChanceDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_reanimationDelayTicks".Translate(), ref reanimationDelayTicks, 60, GenDate.TicksPerDay * 30, "reanimationDelayTicks", "MarkedMen_Settings_reanimationDelayTicksDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_starterLineageBreakthroughChance".Translate(), ref starterLineageBreakthroughChance, 0f, 1f, "starterLineageBreakthroughChance", "MarkedMen_Settings_starterLineageBreakthroughChanceDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_InfectionTransmission".Translate(), "MarkedMen_Settings_InfectionTransmissionDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_EnableMeleeTransmission".Translate(), ref meleeTransmissionEnabled, "MarkedMen_Settings_EnableMeleeTransmissionDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_BiteTransmission".Translate(), ref biteTransmissionEnabled, "MarkedMen_Settings_BiteTransmissionDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_biteInfectionChance".Translate(), ref biteInfectionChance, 0f, 1f, "biteInfectionChance", "MarkedMen_Settings_biteInfectionChanceDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_ClawTransmission".Translate(), ref clawTransmissionEnabled, "MarkedMen_Settings_ClawTransmissionDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_clawInfectionChance".Translate(), ref clawInfectionChance, 0f, 1f, "clawInfectionChance", "MarkedMen_Settings_clawInfectionChanceDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_ScratchTransmission".Translate(), ref scratchTransmissionEnabled, "MarkedMen_Settings_ScratchTransmissionDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_scratchInfectionChance".Translate(), ref scratchInfectionChance, 0f, 1f, "scratchInfectionChance", "MarkedMen_Settings_scratchInfectionChanceDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_PunchTransmission".Translate(), ref punchTransmissionEnabled, "MarkedMen_Settings_PunchTransmissionDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_punchInfectionChance".Translate(), ref punchInfectionChance, 0f, 1f, "punchInfectionChance", "MarkedMen_Settings_punchInfectionChanceDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_MeleeWeaponTransmission".Translate(), ref meleeWeaponTransmissionEnabled, "MarkedMen_Settings_MeleeWeaponTransmissionDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_meleeWeaponInfectionChance".Translate(), ref meleeWeaponInfectionChance, 0f, 1f, "meleeWeaponInfectionChance", "MarkedMen_Settings_meleeWeaponInfectionChanceDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_MarkedMenGuaranteed".Translate(), ref markedMenGuaranteedInfection, "MarkedMen_Settings_MarkedMenGuaranteedDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_markedMenInfectionChance".Translate(), ref markedMenInfectionChance, 0f, 1f, "markedMenInfectionChance", "MarkedMen_Settings_markedMenInfectionChanceDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_EnableRangedTransmission".Translate(), ref rangedTransmissionEnabled, "MarkedMen_Settings_EnableRangedTransmissionDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_rangedInfectionChance".Translate(), ref rangedInfectionChance, 0f, 1f, "rangedInfectionChance", "MarkedMen_Settings_rangedInfectionChanceDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_SealedApparel".Translate(), "MarkedMen_Settings_SealedApparelDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_WarcasketsBlock".Translate(), ref warcasketsBlockExposure, "MarkedMen_Settings_WarcasketsBlockDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_VacsuitBlock".Translate(), ref vacsuitBlockExposure, "MarkedMen_Settings_VacsuitBlockDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_GasMasksBlock".Translate(), ref gasMasksBlockExposure, "MarkedMen_Settings_GasMasksBlockDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_SealedArmorBlock".Translate(), ref sealedArmorBlockExposure, "MarkedMen_Settings_SealedArmorBlockDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_InfectedAI".Translate(), "MarkedMen_Settings_InfectedAIDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_TacticalRetargeting".Translate(), ref tacticalRetargetingEnabled, "MarkedMen_Settings_TacticalRetargetingDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_PriorityTargeting".Translate(), ref priorityTargetingEnabled, "MarkedMen_Settings_PriorityTargetingDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_DoorTargeting".Translate(), ref doorTargetingEnabled, "MarkedMen_Settings_DoorTargetingDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_infightingChance".Translate(), ref infightingChance, 0f, 1f, "infightingChance", "MarkedMen_Settings_infightingChanceDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_socialTerrorStrength".Translate(), ref socialTerrorStrength, 0f, 5f, "socialTerrorStrength", "MarkedMen_Settings_socialTerrorStrengthDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_MarkedPanicEnabled".Translate(), ref markedPanicEnabled, "MarkedMen_Settings_MarkedPanicEnabledDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_markedPanicRadius".Translate(), ref markedPanicRadius, 0f, 100f, "markedPanicRadius", "MarkedMen_Settings_markedPanicRadiusDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_markedPanicDurationTicks".Translate(), ref markedPanicDurationTicks, 60, GenDate.TicksPerDay * 30, "markedPanicDurationTicks", "MarkedMen_Settings_markedPanicDurationTicksDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_PredatoryInstincts".Translate(), "MarkedMen_Settings_PredatoryInstinctsDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_BloodlustSystem".Translate(), ref bloodlustEnabled, "MarkedMen_Settings_BloodlustSystemDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_bloodlustDecayRate".Translate(), ref bloodlustDecayRate, 0.1f, 5f, "bloodlustDecayRate", "MarkedMen_Settings_bloodlustDecayRateDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_bloodlustKillGainMultiplier".Translate(), ref bloodlustKillGainMultiplier, 0.1f, 5f, "bloodlustKillGainMultiplier", "MarkedMen_Settings_bloodlustKillGainMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_bloodlustCombatGainMultiplier".Translate(), ref bloodlustCombatGainMultiplier, 0f, 5f, "bloodlustCombatGainMultiplier", "MarkedMen_Settings_bloodlustCombatGainMultiplierDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_KillAnticipation".Translate(), ref anticipationEnabled, "MarkedMen_Settings_KillAnticipationDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_anticipationGainMultiplier".Translate(), ref anticipationGainMultiplier, 0.1f, 5f, "anticipationGainMultiplier", "MarkedMen_Settings_anticipationGainMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_anticipationDecayMultiplier".Translate(), ref anticipationDecayMultiplier, 0.1f, 5f, "anticipationDecayMultiplier", "MarkedMen_Settings_anticipationDecayMultiplierDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_MessagesAndDevTools".Translate(), "MarkedMen_Settings_MessagesAndDevToolsDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_RaidCountdown".Translate(), ref raidCountdownAlertEnabled, "MarkedMen_Settings_RaidCountdownDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_raidCountdownVisibleDays".Translate(), ref raidCountdownVisibleDays, 0f, 999f, "raidCountdownVisibleDays", "MarkedMen_Settings_raidCountdownVisibleDaysDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_raidCountdownHighPriorityDays".Translate(), ref raidCountdownHighPriorityDays, 0f, 30f, "raidCountdownHighPriorityDays", "MarkedMen_Settings_raidCountdownHighPriorityDaysDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_DetailedLetters".Translate(), ref detailedRaidLetters, "MarkedMen_Settings_DetailedLettersDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_IncidentLog".Translate(), ref incidentLogEnabled, "MarkedMen_Settings_IncidentLogDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_DebugActions".Translate(), ref debugActionsEnabled, "MarkedMen_Settings_DebugActionsDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_Performance".Translate(), "MarkedMen_Settings_PerformanceDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_contagionPulseIntervalTicks".Translate(), ref contagionPulseIntervalTicks, 60, GenDate.TicksPerDay, "contagionPulseIntervalTicks", "MarkedMen_Settings_contagionPulseIntervalTicksDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_maxContagionTargetsPerPulse".Translate(), ref maxContagionTargetsPerPulse, 0, 50, "maxContagionTargetsPerPulse", "MarkedMen_Settings_maxContagionTargetsPerPulseDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_corpseContaminationIntervalTicks".Translate(), ref corpseContaminationIntervalTicks, 60, GenDate.TicksPerDay, "corpseContaminationIntervalTicks", "MarkedMen_Settings_corpseContaminationIntervalTicksDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_maxCorpsesPerPulse".Translate(), ref maxCorpsesPerPulse, 0, 50, "maxCorpsesPerPulse", "MarkedMen_Settings_maxCorpsesPerPulseDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_tacticalRetargetIntervalTicks".Translate(), ref tacticalRetargetIntervalTicks, 1, 2500, "tacticalRetargetIntervalTicks", "MarkedMen_Settings_tacticalRetargetIntervalTicksDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_infightingCheckIntervalTicks".Translate(), ref infightingCheckIntervalTicks, 60, GenDate.TicksPerDay, "infightingCheckIntervalTicks", "MarkedMen_Settings_infightingCheckIntervalTicksDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_lordCleanupIntervalTicks".Translate(), ref lordCleanupIntervalTicks, 60, GenDate.TicksPerDay, "lordCleanupIntervalTicks", "MarkedMen_Settings_lordCleanupIntervalTicksDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_infectedStateMaintenanceIntervalTicks".Translate(), ref infectedStateMaintenanceIntervalTicks, 60, GenDate.TicksPerDay, "infectedStateMaintenanceIntervalTicks", "MarkedMen_Settings_infectedStateMaintenanceIntervalTicksDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_reanimationProcessIntervalTicks".Translate(), ref reanimationProcessIntervalTicks, 60, GenDate.TicksPerDay, "reanimationProcessIntervalTicks", "MarkedMen_Settings_reanimationProcessIntervalTicksDesc".Translate());
            DrawInt(listing, "MarkedMen_Settings_maxPendingReanimationsPerTick".Translate(), ref maxPendingReanimationsPerTick, 1, 500, "maxPendingReanimationsPerTick", "MarkedMen_Settings_maxPendingReanimationsPerTickDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_UrbanOutbreak".Translate(), "MarkedMen_Settings_UrbanOutbreakDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_UrbanOutbreaks".Translate(), ref urbanOutbreaksEnabled, "MarkedMen_Settings_UrbanOutbreaksDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_urbanInfectionDensity".Translate(), ref urbanInfectionDensity, 0f, 5f, "urbanInfectionDensity", "MarkedMen_Settings_urbanInfectionDensityDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_urbanAmbushFrequency".Translate(), ref urbanAmbushFrequency, 0f, 5f, "urbanAmbushFrequency", "MarkedMen_Settings_urbanAmbushFrequencyDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_DormantInfestations".Translate(), ref dormantInfestationsEnabled, "MarkedMen_Settings_DormantInfestationsDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_dormantInfestationFrequency".Translate(), ref dormantInfestationFrequency, 0f, 5f, "dormantInfestationFrequency", "MarkedMen_Settings_dormantInfestationFrequencyDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_CityEpicenters".Translate(), ref cityEpicentersEnabled, "MarkedMen_Settings_CityEpicentersDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_epicenterSpawnChance".Translate(), ref epicenterSpawnChance, 0f, 1f, "epicenterSpawnChance", "MarkedMen_Settings_epicenterSpawnChanceDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_UrbanAmbushes".Translate(), ref urbanAmbushesEnabled, "MarkedMen_Settings_UrbanAmbushesDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_SurvivorEncounters".Translate(), ref survivorEncountersEnabled, "MarkedMen_Settings_SurvivorEncountersDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_survivorEncounterChance".Translate(), ref survivorEncounterChance, 0f, 1f, "survivorEncounterChance", "MarkedMen_Settings_survivorEncounterChanceDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_AURSpawnProtection".Translate(), "MarkedMen_Settings_AURSpawnProtectionDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_SpawnProtection".Translate(), ref aurSpawnPatchEnabled, "MarkedMen_Settings_SpawnProtectionDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_aurMinimumSpawnDistance".Translate(), ref aurMinimumSpawnDistance, 10f, 100f, "aurMinimumSpawnDistance", "MarkedMen_Settings_aurMinimumSpawnDistanceDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_PreferEdgeSpawn".Translate(), ref aurPreferEdgeSpawn, "MarkedMen_Settings_PreferEdgeSpawnDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_SpawnDebugLogging".Translate(), ref aurSpawnPatchDebugLogging, "MarkedMen_Settings_SpawnDebugLoggingDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_LostSurvivors".Translate(), "MarkedMen_Settings_LostSurvivorsDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_LostSurvivorIncidents".Translate(), ref lostSurvivorEnabled, "MarkedMen_Settings_LostSurvivorIncidentsDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_lostSurvivorFrequencyMultiplier".Translate(), ref lostSurvivorFrequencyMultiplier, 0f, 5f, "lostSurvivorFrequencyMultiplier", "MarkedMen_Settings_lostSurvivorFrequencyMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_dormantMarkMinDays".Translate(), ref dormantMarkMinDays, 1f, 60f, "dormantMarkMinDays", "MarkedMen_Settings_dormantMarkMinDaysDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_dormantMarkMaxDays".Translate(), ref dormantMarkMaxDays, 1f, 120f, "dormantMarkMaxDays", "MarkedMen_Settings_dormantMarkMaxDaysDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_dormantMarkTriggerMultiplier".Translate(), ref dormantMarkTriggerMultiplier, 0f, 5f, "dormantMarkTriggerMultiplier", "MarkedMen_Settings_dormantMarkTriggerMultiplierDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_dormantMarkAlphaChance".Translate(), ref dormantMarkAlphaChance, 0f, 1f, "dormantMarkAlphaChance", "MarkedMen_Settings_dormantMarkAlphaChanceDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_dormantMarkGroupVariantChance".Translate(), ref dormantMarkGroupVariantChance, 0f, 1f, "dormantMarkGroupVariantChance", "MarkedMen_Settings_dormantMarkGroupVariantChanceDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_MarkedPrisoners".Translate(), "MarkedMen_Settings_MarkedPrisonersDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_PrisonerInfection".Translate(), ref prisonerInfectionEnabled, "MarkedMen_Settings_PrisonerInfectionDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_PrisonerRestraint".Translate(), ref prisonerRestraintEnabled, "MarkedMen_Settings_PrisonerRestraintDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_prisonerInfectionChance".Translate(), ref prisonerInfectionChance, 0f, 1f, "prisonerInfectionChance", "MarkedMen_Settings_prisonerInfectionChanceDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_PrisonerSelfHarm".Translate(), ref prisonerSelfHarmEnabled, "MarkedMen_Settings_PrisonerSelfHarmDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_prisonerSelfHarmStageDays".Translate(), ref prisonerSelfHarmStageDays, 1f, 60f, "prisonerSelfHarmStageDays", "MarkedMen_Settings_prisonerSelfHarmStageDaysDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_prisonerSelfHarmSuicideDays".Translate(), ref prisonerSelfHarmSuicideDays, 1f, 90f, "prisonerSelfHarmSuicideDays", "MarkedMen_Settings_prisonerSelfHarmSuicideDaysDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_prisonerEscapeAggressionMultiplier".Translate(), ref prisonerEscapeAggressionMultiplier, 0f, 5f, "prisonerEscapeAggressionMultiplier", "MarkedMen_Settings_prisonerEscapeAggressionMultiplierDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_PrisonerCosmetic".Translate(), ref prisonerCosmeticEnabled, "MarkedMen_Settings_PrisonerCosmeticDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_prisonerEscapeChance".Translate(), ref prisonerEscapeChance, 0f, 1f, "prisonerEscapeChance", "MarkedMen_Settings_prisonerEscapeChanceDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_PrisonerDebugLogging".Translate(), ref prisonerDebugLogging, "MarkedMen_Settings_PrisonerDebugLoggingDesc".Translate());

            DrawSectionHeader(listing, "MarkedMen_Settings_RJW_Bridge".Translate(), "MarkedMen_Settings_RJW_BridgeDesc".Translate());
            DrawHelp(listing, "MarkedMen_Settings_RJW_Detected".Translate(TheMarkedMenRjwCompatibility.IsRjwLoaded() ? "MarkedMen_Settings_Yes".Translate() : "MarkedMen_Settings_No".Translate()));
            DrawCheckbox(listing, "MarkedMen_Settings_RJW_AutoEnable".Translate(), ref rjwAutoEnableWhenInstalled, "MarkedMen_Settings_RJW_AutoEnableDesc".Translate());
            DrawCheckbox(listing, "MarkedMen_Settings_RJW_Enable".Translate(), ref rjwIntegrationEnabled, "MarkedMen_Settings_RJW_EnableDesc".Translate());
            DrawFloat(listing, "MarkedMen_Settings_rjwExposureChance".Translate(), ref rjwExposureChance, 0f, 1f, "rjwExposureChance", "MarkedMen_Settings_rjwExposureChanceDesc".Translate());
        }

        public static float ApplyRaidPointSettings(float points)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null)
            {
                return Mathf.Max(120f, points);
            }

            return Mathf.Clamp(Mathf.Max(points, settings.minimumRaidPoints) * settings.raidPointsMultiplier, 0f, settings.maximumRaidPoints);
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
            listing.Label("MarkedMen_Settings_Title".Translate());
            Text.Font = oldFont;
            DrawHelp(listing, "MarkedMen_Settings_Intro".Translate());
        }

        private void DrawPresetControls(Listing_Standard listing)
        {
            listing.Gap(6f);
            listing.Label("MarkedMen_Settings_ActivePreset".Translate(TranslatedPresetLabel(PresetLabel())));
            DrawHelp(listing, "MarkedMen_Settings_PresetHelp".Translate());

            Rect row = listing.GetRect(PresetButtonHeight, 1f);
            float buttonWidth = (row.width - (PresetButtonGap * 4f)) / 5f;
            DrawPresetButton(new Rect(row.x, row.y, buttonWidth, row.height), "MarkedMen_Settings_Preset_VeryEasy".Translate(), "MarkedMen_Settings_Preset_VeryEasyTip".Translate(), ApplyCasualPreset);
            DrawPresetButton(new Rect(row.x + (buttonWidth + PresetButtonGap), row.y, buttonWidth, row.height), "MarkedMen_Settings_Preset_Easy".Translate(), "MarkedMen_Settings_Preset_EasyTip".Translate(), ApplyVanillaLikePreset);
            DrawPresetButton(new Rect(row.x + ((buttonWidth + PresetButtonGap) * 2f), row.y, buttonWidth, row.height), "MarkedMen_Settings_Preset_Normal".Translate(), "MarkedMen_Settings_Preset_NormalTip".Translate(), () => ApplyDefaultPreset(true));
            DrawPresetButton(new Rect(row.x + ((buttonWidth + PresetButtonGap) * 3f), row.y, buttonWidth, row.height), "MarkedMen_Settings_Preset_Hard".Translate(), "MarkedMen_Settings_Preset_HardTip".Translate(), ApplyBrutalPreset);
            DrawPresetButton(new Rect(row.x + ((buttonWidth + PresetButtonGap) * 4f), row.y, buttonWidth, row.height), "MarkedMen_Settings_Preset_VeryHard".Translate(), "MarkedMen_Settings_Preset_VeryHardTip".Translate(), ApplyOutbreakPreset);
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

        private string TranslatedPresetLabel(string presetName)
        {
            switch (presetName)
            {
                case "Very Easy":
                case "Casual": return "MarkedMen_Settings_Preset_VeryEasy".Translate();
                case "Easy":
                case "Vanilla-like": return "MarkedMen_Settings_Preset_Easy".Translate();
                case "Normal":
                case "Default": return "MarkedMen_Settings_Preset_Normal".Translate();
                case "Hard":
                case "Brutal": return "MarkedMen_Settings_Preset_Hard".Translate();
                case "Very Hard":
                case "Outbreak simulator":
                case "Outbreak": return "MarkedMen_Settings_Preset_VeryHard".Translate();
                default: return "MarkedMen_Settings_Preset_Custom".Translate();
            }
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
            minimumRaidPoints = Mathf.Clamp(minimumRaidPoints, 0f, maximumRaidPoints);
            maximumRaidPoints = Mathf.Clamp(maximumRaidPoints, minimumRaidPoints, 100000f);
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
            rangedInfectionChance = Mathf.Clamp01(rangedInfectionChance);
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
            markedPanicRadius = Mathf.Clamp(markedPanicRadius, 0f, 100f);
            markedPanicDurationTicks = Mathf.Clamp(markedPanicDurationTicks, 60, GenDate.TicksPerDay * 30);
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
            ApplyBaselinePreset(updatePreset);
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
            minimumRaidPoints = 2000f;
            maximumRaidPoints = 10000f;
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
            markedPanicEnabled = true;
            markedPanicRadius = 12f;
            markedPanicDurationTicks = 18000;
            ResetStoryDefaults();
            ResetPerformanceDefaults();
            if (updatePreset)
            {
                currentPreset = DefaultPresetName;
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
        private int countdownCacheTick = -1;
        private int countdownCachedNextTick;
        private int countdownCachedTicksUntilRaid;
        private Map countdownCachedTargetMap;
        private bool countdownCachedValid;
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
            TryFireScheduledRaid(ticks);
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

        public void InvalidateRaidCountdownCache()
        {
            countdownCacheTick = -1;
        }

        public bool TryGetRaidCountdownForAlert(out int nextTick, out int ticksUntilRaid, out Map targetMap)
        {
            int currentTick = Find.TickManager?.TicksGame ?? -1;

            if (currentTick == countdownCacheTick && countdownCacheTick >= 0)
            {
                nextTick = countdownCachedNextTick;
                ticksUntilRaid = countdownCachedTicksUntilRaid;
                targetMap = countdownCachedTargetMap;
                return countdownCachedValid;
            }

            nextTick = 0;
            ticksUntilRaid = 0;
            targetMap = null;
            countdownCachedValid = false;

            if (currentTick < 0 || activeRaid || CADefOf.CrossedRaid == null || !TheMarkedMenSettings.WarbandsEnabled)
            {
                countdownCacheTick = currentTick;
                countdownCachedNextTick = 0;
                countdownCachedTicksUntilRaid = 0;
                countdownCachedTargetMap = null;
                return false;
            }

            targetMap = FindRaidTargetMap();
            if (targetMap == null)
            {
                countdownCacheTick = currentTick;
                countdownCachedNextTick = 0;
                countdownCachedTicksUntilRaid = 0;
                countdownCachedTargetMap = null;
                return false;
            }

            int raidFirstTick = TheMarkedMenSettings.FirstMarkedRaidTick;
            if (!raidScheduleActivated && currentTick < raidFirstTick)
            {
                nextTick = raidFirstTick;
            }
            else
            {
                nextTick = nextRaidTick;
                if (nextTick <= 0)
                {
                    nextTick = currentTick + CalculateAdjustedRaidIntervalTicks(false);
                }
            }

            if (nextTick < currentTick)
            {
                nextTick = currentTick;
            }

            ticksUntilRaid = Mathf.Max(0, nextTick - currentTick);
            countdownCachedValid = true;

            countdownCacheTick = currentTick;
            countdownCachedNextTick = nextTick;
            countdownCachedTicksUntilRaid = ticksUntilRaid;
            countdownCachedTargetMap = targetMap;

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
                InvalidateRaidCountdownCache();
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
            InvalidateRaidCountdownCache();
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
                Log.Warning("[The Marked Men] Failed to transform infected corpse: " + ex.Message);
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
            InvalidateRaidCountdownCache();
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

            IntVec3 _;
            bool needsEdge = parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkIn
                || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkInGroups
                || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkInDistributed
                || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkInDistributedGroups;

            if (needsEdge && !CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map), map, 0f, out _))
            {
                for (int i = 0; i < pawns.Count; i++)
                    pawns[i]?.Destroy(DestroyMode.Vanish);
                return false;
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

            IntVec3 _;
            bool needsEdge = parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkIn
                || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkInGroups
                || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkInDistributed
                || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkInDistributedGroups;

            if (needsEdge && !CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map), map, 0f, out _))
            {
                for (int i = 0; i < pawns.Count; i++)
                    pawns[i]?.Destroy(DestroyMode.Vanish);
                return false;
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

            IntVec3 _;
            bool needsEdge = parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkIn
                || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkInGroups
                || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkInDistributed
                || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkInDistributedGroups;

            if (needsEdge && !CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map), map, 0f, out _))
            {
                for (int i = 0; i < pawns.Count; i++)
                    pawns[i]?.Destroy(DestroyMode.Vanish);
                return false;
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
