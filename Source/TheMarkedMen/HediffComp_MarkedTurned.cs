using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public sealed class HediffCompProperties_MarkedTurned : HediffCompProperties
    {
        public float moveSpeedFactor = 1.05f;
        public float meleeAccuracyBonus = 0.1f;
        public float rangedAccuracyBonus = 0.05f;
        public float healthRegenPerTick = 0.001f;
        public float painFactor = 0.0f;
        public float hungerFactor = 0.0f;
        public float fatiguePassPerTick = -0.001f;
        public float recreationPassPerTick = 0.0f;

        public HediffCompProperties_MarkedTurned()
        {
            compClass = typeof(HediffComp_MarkedTurned);
        }
    }

    public sealed class HediffComp_MarkedTurned : HediffComp
    {
        private int nextRegenTick;

        private HediffCompProperties_MarkedTurned Props => (HediffCompProperties_MarkedTurned)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed)
            {
                return;
            }

            int ticks = Find.TickManager.TicksGame;
            if (ticks < nextRegenTick)
            {
                return;
            }

            nextRegenTick = ticks + 250;

            if (Props.healthRegenPerTick > 0f)
            {
                float regenAmount = Props.healthRegenPerTick * 250f;
                List<Hediff> hediffs = new List<Hediff>();
                pawn.health.hediffSet.GetHediffs(ref hediffs);
                for (int i = 0; i < hediffs.Count; i++)
                {
                    Hediff_Injury injury = hediffs[i] as Hediff_Injury;
                    if (injury != null)
                    {
                        injury.Heal(regenAmount);
                    }
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref nextRegenTick, "nextRegenTick", 0);
        }
    }
}