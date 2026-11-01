using RimWorld;
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
        private bool countdownStarted;

        public HediffCompProperties_CureCountdown Props => (HediffCompProperties_CureCountdown)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            if (!countdownStarted)
            {
                ticksRemaining = Rand.Range(Props.minTicks, Props.maxTicks);
                countdownStarted = true;
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
                countdownStarted = true;
            }

            ticksRemaining--;

            if (ticksRemaining <= 0)
            {
                // Apply the cure
                string failureReason;
                if (CureUtility.TryCure(Pawn, out failureReason))
                {
                    // Remove the pending cure hediff after cure is applied
                    Pawn.health.RemoveHediff(parent);
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);
            Scribe_Values.Look(ref countdownStarted, "countdownStarted", false);
        }
    }
}
