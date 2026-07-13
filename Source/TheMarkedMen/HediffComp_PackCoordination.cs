using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class HediffCompProperties_PackCoordination : HediffCompProperties
    {
        public float scanRadius = 20f;
        public int scanIntervalTicks = 180;
        public string packHediffDefName = "CA_PackCoordination";

        public HediffCompProperties_PackCoordination()
        {
            compClass = typeof(HediffComp_PackCoordination);
        }
    }

    public sealed class HediffComp_PackCoordination : HediffComp
    {
        private int nextScanTick;

        private HediffCompProperties_PackCoordination Props => (HediffCompProperties_PackCoordination)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed) return;

            int ticks = Find.TickManager.TicksGame;
            if (ticks < nextScanTick) return;
            nextScanTick = ticks + Props.scanIntervalTicks;

            int nearbyAllies = CountNearbyAllies(pawn);
            float severity = Mathf.Clamp01((float)nearbyAllies / 3f);

            HediffDef packDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.packHediffDefName);
            if (packDef == null) return;

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(packDef);
            if (nearbyAllies >= 2)
            {
                if (existing == null)
                {
                    existing = HediffMaker.MakeHediff(packDef, pawn);
                    pawn.health.AddHediff(existing);
                }

                existing.Severity = severity;
                if (existing.Severity <= 0f)
                {
                    pawn.health.RemoveHediff(existing);
                }
            }
            else
            {
                if (existing != null)
                {
                    pawn.health.RemoveHediff(existing);
                }
            }
        }

        private int CountNearbyAllies(Pawn pawn)
        {
            float radiusSq = Props.scanRadius * Props.scanRadius;
            IntVec3 pos = pawn.Position;
            Faction faction = pawn.Faction;
            int count = 0;

            IReadOnlyList<Pawn> allPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn other = allPawns[i];
                if (other == pawn || other.Dead || other.Downed || !other.RaceProps.Humanlike) continue;
                if (other.Faction != faction) continue;
                if (pos.DistanceToSquared(other.Position) > radiusSq) continue;
                count++;
            }

            return count;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref nextScanTick, "nextScanTick", 0);
        }
    }
}
