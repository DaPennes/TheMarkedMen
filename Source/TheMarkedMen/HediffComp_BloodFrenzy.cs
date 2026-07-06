using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class HediffCompProperties_BloodFrenzy : HediffCompProperties
    {
        public int checkIntervalTicks = 180;
        public float severityPerKill = 0.25f;
        public float maxSeverity = 1f;
        public float decayPerCheck = 0.05f;
        public string frenzyHediffDefName = "CA_BloodFrenzy";

        public HediffCompProperties_BloodFrenzy()
        {
            compClass = typeof(HediffComp_BloodFrenzy);
        }
    }

    public sealed class HediffComp_BloodFrenzy : HediffComp
    {
        private int nextCheckTick;

        private HediffCompProperties_BloodFrenzy Props => (HediffCompProperties_BloodFrenzy)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map == null) return;

            int ticks = Find.TickManager.TicksGame;
            if (ticks < nextCheckTick) return;
            nextCheckTick = ticks + Props.checkIntervalTicks;

            HediffDef frenzyDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.frenzyHediffDefName);
            if (frenzyDef == null) return;

            Hediff frenzy = pawn.health.hediffSet.GetFirstHediffOfDef(frenzyDef);
            if (frenzy == null)
            {
                frenzy = HediffMaker.MakeHediff(frenzyDef, pawn);
                pawn.health.AddHediff(frenzy);
            }

            float decay = Props.decayPerCheck;
            frenzy.Severity = Mathf.Max(0f, frenzy.Severity - decay);
            if (frenzy.Severity <= 0f)
            {
                pawn.health.RemoveHediff(frenzy);
            }
        }

        public void NotifyKill()
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Dead || pawn.health == null) return;

            HediffDef frenzyDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.frenzyHediffDefName);
            if (frenzyDef == null) return;

            Hediff frenzy = pawn.health.hediffSet.GetFirstHediffOfDef(frenzyDef);
            if (frenzy == null)
            {
                frenzy = HediffMaker.MakeHediff(frenzyDef, pawn);
                pawn.health.AddHediff(frenzy);
            }

            frenzy.Severity = Mathf.Min(Props.maxSeverity, frenzy.Severity + Props.severityPerKill);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref nextCheckTick, "nextCheckTick", 0);
        }
    }
}
