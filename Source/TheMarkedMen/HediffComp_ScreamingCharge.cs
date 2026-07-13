using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class HediffCompProperties_ScreamingCharge : HediffCompProperties
    {
        public float detectionRadius = 20f;
        public float chargeSpeedFactor = 1.4f;
        public float chargeMeleeFactor = 0.7f;
        public string buffHediffDefName = "CA_ScreamingCharge";

        public HediffCompProperties_ScreamingCharge()
        {
            compClass = typeof(HediffComp_ScreamingCharge);
        }
    }

    public sealed class HediffComp_ScreamingCharge : HediffComp
    {
        private int nextScanTick;

        private HediffCompProperties_ScreamingCharge Props => (HediffCompProperties_ScreamingCharge)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed)
            {
                RemoveBuffHediff(pawn);
                return;
            }

            int ticks = Find.TickManager.TicksGame;
            if (ticks < nextScanTick)
            {
                return;
            }

            nextScanTick = ticks + 250;

            bool enemyNearby = HasEnemyInRadius(pawn);
            if (enemyNearby)
            {
                ApplyBuffHediff(pawn);
            }
            else
            {
                RemoveBuffHediff(pawn);
            }
        }

        private bool HasEnemyInRadius(Pawn pawn)
        {
            float radiusSq = Props.detectionRadius * Props.detectionRadius;
            IntVec3 pos = pawn.Position;
            Faction faction = pawn.Faction;

            IReadOnlyList<Pawn> allPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn other = allPawns[i];
                if (other == pawn || other.Dead || other.Downed || !other.RaceProps.Humanlike) continue;
                if (other.Faction == faction || (other.Faction != null && !other.Faction.HostileTo(faction))) continue;
                if (pos.DistanceToSquared(other.Position) > radiusSq) continue;
                return true;
            }

            return false;
        }

        private void ApplyBuffHediff(Pawn pawn)
        {
            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.buffHediffDefName);
            if (buffDef == null) return;
            if (pawn.health.hediffSet.HasHediff(buffDef)) return;
            Hediff buff = HediffMaker.MakeHediff(buffDef, pawn);
            pawn.health.AddHediff(buff);
        }

        private void RemoveBuffHediff(Pawn pawn)
        {
            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.buffHediffDefName);
            if (buffDef == null) return;
            Hediff existing = pawn?.health?.hediffSet?.GetFirstHediffOfDef(buffDef);
            if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref nextScanTick, "nextScanTick", 0);
        }
    }
}
