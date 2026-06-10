using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public class HediffComp_PsychicPulse : HediffComp
    {
        private int nextPulseTick;

        public HediffCompProperties_PsychicPulse Props => (HediffCompProperties_PsychicPulse)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Map == null || pawn.Destroyed || !pawn.Spawned || pawn.Dead)
            {
                return;
            }
            int ticks = Find.TickManager.TicksGame;
            if (ticks < nextPulseTick)
            {
                return;
            }
            nextPulseTick = ticks + Props.pulseIntervalTicks;
            EmitPulse(pawn);
        }

        private void EmitPulse(Pawn pawn)
        {
            Map map = pawn.Map;
            HediffDef panicDef = Props.appliedHediff ?? CADefOf.Panic;
            if (map == null || panicDef == null)
            {
                return;
            }
            float radius = Props.radius;
            int numCells = Mathf.Min(GenRadial.NumCellsInRadius(radius), GenRadial.ManualRadialPattern.Length);
            for (int cellIndex = 0; cellIndex < numCells; cellIndex++)
            {
                IntVec3 cell = pawn.Position + GenRadial.ManualRadialPattern[cellIndex];
                if (!cell.InBounds(map))
                {
                    continue;
                }
                List<Thing> things = map.thingGrid.ThingsListAt(cell);
                for (int thingIndex = 0; thingIndex < things.Count; thingIndex++)
                {
                    Pawn target = things[thingIndex] as Pawn;
                    if (target == null || target == pawn || target.Dead || !target.RaceProps.Humanlike || target.Faction == pawn.Faction)
                    {
                        continue;
                    }
                    if (CrossedUtility.IsInfectedPawn(target) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(target))
                    {
                        continue;
                    }
                    if (!target.health.hediffSet.HasHediff(panicDef))
                    {
                        target.health.AddHediff(panicDef);
                    }
                }
            }
        }
    }
}
