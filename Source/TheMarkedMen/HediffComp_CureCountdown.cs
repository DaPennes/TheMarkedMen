using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public class HediffCompProperties_CureCountdown : HediffCompProperties
    {
        /// <summary>Shortest delay in ticks (3600 = one minute at normal speed).</summary>
        public int minTicks = 3600;

        /// <summary>Longest delay in ticks (18000 = five minutes at normal speed).</summary>
        public int maxTicks = 18000;

        public HediffCompProperties_CureCountdown()
        {
            compClass = typeof(HediffComp_CureCountdown);
        }
    }

    /// <summary>
    /// Counts down a random cure delay, then turns the pawn back into a
    /// regular pawn. While this hediff is present the Marked Virus is not
    /// allowed to progress, so the pawn cannot transform or die from it
    /// before the cure finishes.
    /// </summary>
    public class HediffComp_CureCountdown : HediffComp
    {
        private int ticksRemaining;
        private int totalTicks;

        private HediffCompProperties_CureCountdown Props =>
            (HediffCompProperties_CureCountdown)props;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);
            Scribe_Values.Look(ref totalTicks, "totalTicks", 0);
        }

        public override void CompPostMake()
        {
            base.CompPostMake();
            RollDuration();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (totalTicks <= 0 || ticksRemaining <= 0)
                RollDuration();

            ticksRemaining--;
            if (ticksRemaining > 0)
                return;

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead || pawn.Destroyed)
                return;

            // Remove this hediff first so the cure check accepts the pawn.
            pawn.health.RemoveHediff(parent);

            string reason;
            CureUtility.TryCure(pawn, out reason);
        }

        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (totalTicks <= 0 || ticksRemaining <= 0)
                    return null;

                float progress = 1f - (float)ticksRemaining / totalTicks;
                return Mathf.Clamp01(progress).ToStringPercent();
            }
        }

        private void RollDuration()
        {
            int min = Mathf.Max(1, Props.minTicks);
            int max = Mathf.Max(min, Props.maxTicks);
            totalTicks = Rand.RangeInclusive(min, max);
            ticksRemaining = totalTicks;
        }
    }
}
