using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
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
}
