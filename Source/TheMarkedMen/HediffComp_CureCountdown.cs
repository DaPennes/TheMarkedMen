using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public class HediffCompProperties_CureCountdown : HediffCompProperties
    {
        public int minTicks = 3600;
        public int maxTicks = 18000;

        public HediffCompProperties_CureCountdown()
        {
            compClass = typeof(HediffComp_CureCountdown);
        }
    }

    public class HediffComp_CureCountdown : HediffComp
    {
        private int ticksRemaining;
        private int ticksTotal;
        private bool countdownStarted;

        public HediffCompProperties_CureCountdown Props => (HediffCompProperties_CureCountdown)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            if (!countdownStarted)
            {
                ticksRemaining = Rand.Range(Props.minTicks, Props.maxTicks);
                ticksTotal = ticksRemaining;
                countdownStarted = true;
                UpdateSeverity();
                Log.Message($"[The Marked Men] Cure countdown started for {Pawn.LabelShort}. Duration: {ticksRemaining} ticks (~{ticksRemaining / 2500f:F1} days)");
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || Pawn.Dead || !Pawn.Spawned)
                return;

            if (!countdownStarted)
            {
                ticksRemaining = Rand.Range(Props.minTicks, Props.maxTicks);
                ticksTotal = ticksRemaining;
                countdownStarted = true;
                UpdateSeverity();
                Log.Message($"[The Marked Men] Cure countdown started for {Pawn.LabelShort}. Duration: {ticksRemaining} ticks (~{ticksRemaining / 2500f:F1} days)");
            }

            ticksRemaining--;
            UpdateSeverity();

            if (ticksRemaining <= 0)
            {
                Log.Message($"[The Marked Men] Cure countdown completed for {Pawn.LabelShort}. Applying cure...");
                // Apply the cure directly without validation since countdown is already validated
                if (Pawn != null && !Pawn.Dead && Pawn.Spawned)
                {
                    CureUtility.ApplyCureDirectly(Pawn);
                    // Remove the pending cure hediff after cure is applied
                    Pawn.health.RemoveHediff(parent);
                    Log.Message($"[The Marked Men] Cure applied and pending hediff removed for {Pawn.LabelShort}");
                }
            }
        }

        private void UpdateSeverity()
        {
            if (parent == null || ticksTotal <= 0)
                return;

            // Progress from 0.01 to 1 based on remaining ticks
            float progress = 0.01f + (0.99f * (1f - ((float)ticksRemaining / ticksTotal)));
            parent.Severity = Mathf.Clamp01(progress);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);
            Scribe_Values.Look(ref ticksTotal, "ticksTotal", 0);
            Scribe_Values.Look(ref countdownStarted, "countdownStarted", false);
        }
    }
}
