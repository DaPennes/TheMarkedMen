using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    public sealed class HediffCompProperties_MarkedHunt : HediffCompProperties
    {
        public int scanIntervalTicks = 150;

        public HediffCompProperties_MarkedHunt()
        {
            compClass = typeof(HediffComp_MarkedHunt);
        }
    }

    public sealed class HediffComp_MarkedHunt : HediffComp
    {
        private const float MaxHuntDistance = 30f;
        private int nextScanTick;

        private HediffCompProperties_MarkedHunt Props => (HediffCompProperties_MarkedHunt)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed) return;
            if (pawn.mindState == null) return;

            int ticks = Find.TickManager.TicksGame;
            if (ticks < nextScanTick) return;
            nextScanTick = ticks + Props.scanIntervalTicks;

            Pawn target = FindHuntTarget(pawn);
            if (target == null) return;

            pawn.mindState.meleeThreat = target;
        }

        private Pawn FindHuntTarget(Pawn pawn)
        {
            float radiusSq = MaxHuntDistance * MaxHuntDistance;
            IntVec3 pos = pawn.Position;
            Faction faction = pawn.Faction;

            IReadOnlyList<Pawn> allPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            Pawn best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn other = allPawns[i];
                if (other == pawn || other.Dead || other.Downed || !other.RaceProps.Humanlike) continue;
                if (other.Faction == faction || (other.Faction != null && !other.Faction.HostileTo(faction))) continue;

                float distSq = pos.DistanceToSquared(other.Position);
                if (distSq > radiusSq) continue;

                float woundedPenalty = other.health.summaryHealth.SummaryHealthPercent * 100f;
                float score = distSq + woundedPenalty;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = other;
                }
            }

            return best;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref nextScanTick, "nextScanTick", 0);
        }
    }
}
