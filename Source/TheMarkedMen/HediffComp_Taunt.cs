using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class HediffCompProperties_Taunt : HediffCompProperties
    {
        public int checkIntervalTicks = 120;
        public int tauntCooldownTicks = 600;
        public float tauntChance = 0.4f;
        public float tauntRadius = 5f;

        public HediffCompProperties_Taunt()
        {
            compClass = typeof(HediffComp_Taunt);
        }
    }

    public sealed class HediffComp_Taunt : HediffComp
    {
        private int nextCheckTick;
        private int nextTauntTick;

        private HediffCompProperties_Taunt Props => (HediffCompProperties_Taunt)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed) return;

            int ticks = Find.TickManager.TicksGame;
            if (ticks < nextCheckTick) return;
            nextCheckTick = ticks + Props.checkIntervalTicks;

            if (ticks < nextTauntTick) return;

            if (!Rand.Chance(Props.tauntChance)) return;

            Pawn target = FindTauntTarget(pawn);
            if (target == null) return;

            nextTauntTick = ticks + Props.tauntCooldownTicks;
            CrossedSocialUtility.TriggerInteraction(pawn, target, CADefOf.CrossedPredatoryTaunt);
        }

        private Pawn FindTauntTarget(Pawn pawn)
        {
            float radiusSq = Props.tauntRadius * Props.tauntRadius;
            IntVec3 pos = pawn.Position;
            Faction faction = pawn.Faction;

            IReadOnlyList<Pawn> allPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            Pawn best = null;
            float bestScore = 0f;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn other = allPawns[i];
                if (other == pawn || other.Dead || other.Downed || !other.RaceProps.Humanlike) continue;
                if (other.Faction == faction || (other.Faction != null && !other.Faction.HostileTo(faction))) continue;

                float distSq = pos.DistanceToSquared(other.Position);
                if (distSq > radiusSq) continue;

                float score = 1f / Mathf.Max(distSq, 1f);
                if (other.Downed) score *= 2f;
                if (score > bestScore)
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
            Scribe_Values.Look(ref nextCheckTick, "nextCheckTick", 0);
            Scribe_Values.Look(ref nextTauntTick, "nextTauntTick", 0);
        }
    }
}
