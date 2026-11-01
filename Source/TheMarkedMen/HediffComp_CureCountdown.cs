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
            }

            ticksRemaining--;
            UpdateSeverity();

            if (ticksRemaining <= 0)
            {
                // Apply the cure
                string failureReason;
                if (CureUtility.TryCure(Pawn, out failureReason))
                {
                    // Remove the pending cure hediff after cure is applied
                    Pawn.health.RemoveHediff(parent);
                }
                else
                {
                    // If cure failed, log the reason
                    Log.Warning($"[The Marked Men] Cure failed for {Pawn.LabelShort}: {failureReason}");
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
