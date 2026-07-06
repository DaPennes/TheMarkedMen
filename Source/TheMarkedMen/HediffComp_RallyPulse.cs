using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public sealed class HediffCompProperties_RallyPulse : HediffCompProperties
    {
        public float radius = 16f;
        public int pulseIntervalTicks = 400;
        public string buffHediffDefName = "CA_RallyBuff";

        public HediffCompProperties_RallyPulse()
        {
            compClass = typeof(HediffComp_RallyPulse);
        }
    }

    public sealed class HediffComp_RallyPulse : HediffComp
    {
        private HediffCompProperties_RallyPulse Props => (HediffCompProperties_RallyPulse)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed) return;

            if (!pawn.IsHashIntervalTick(Props.pulseIntervalTicks)) return;

            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.buffHediffDefName);
            if (buffDef == null) return;

            float radiusSq = Props.radius * Props.radius;
            IntVec3 pos = pawn.Position;
            Faction faction = pawn.Faction;

            IReadOnlyList<Pawn> allPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn other = allPawns[i];
                if (other == pawn || other.Dead || other.Downed || !other.RaceProps.Humanlike) continue;
                if (other.Faction != faction) continue;
                if (pos.DistanceToSquared(other.Position) > radiusSq) continue;

                if (other.health.hediffSet.HasHediff(buffDef)) continue;

                Hediff buff = HediffMaker.MakeHediff(buffDef, other);
                other.health.AddHediff(buff);
            }
        }
    }
}
