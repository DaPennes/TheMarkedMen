using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class HediffCompProperties_PsychicPulse : HediffCompProperties
    {
        public float radius = 14f;
        public int pulseIntervalTicks = 500;

        public HediffCompProperties_PsychicPulse()
        {
            compClass = typeof(HediffComp_PsychicPulse);
        }
    }

    public sealed class HediffComp_PsychicPulse : HediffComp
    {
        private HediffCompProperties_PsychicPulse Props => (HediffCompProperties_PsychicPulse)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (!parent.pawn.Spawned || !parent.pawn.IsHashIntervalTick(Props.pulseIntervalTicks))
            {
                return;
            }

            Pawn pawn = parent.pawn;
            Map map = pawn.Map;
            if (map == null) return;

            float radius = Props.radius;
            float radiusSq = radius * radius;
            int radInt = Mathf.CeilToInt(radius);
            IntVec3 pos = pawn.Position;
            CellRect rect = CellRect.CenteredOn(pos, radInt);

            for (int z = rect.minZ; z <= rect.maxZ; z++)
            {
                for (int x = rect.minX; x <= rect.maxX; x++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    if (!cell.InBounds(map)) continue;

                    float dx = cell.x - pos.x;
                    float dz = cell.z - pos.z;
                    if (dx * dx + dz * dz > radiusSq)
                    {
                        continue;
                    }

                    List<Thing> things = map.thingGrid.ThingsListAt(cell);
                    for (int i = 0; i < things.Count; i++)
                    {
                        if (things[i] is Pawn other && other != pawn && !other.Dead && other.RaceProps.Humanlike && CrossedUtility.IsInfectedPawn(other))
                        {
                            Hediff hediff = other.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CrossVirus);
                            if (hediff != null)
                            {
                                hediff.Severity = Mathf.Min(hediff.Severity + 0.01f, 1f);
                            }
                        }
                    }
                }
            }
        }
    }
}
