using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static class CrossedContagionUtility
    {
        private const float ContagionRadius = 2.9f;
        private const float ContagionRadiusSquared = ContagionRadius * ContagionRadius;
        private const int ContagionPulseIntervalTicks = 500;
        private const int MaxContagionTargetsPerPulse = 3;

        public static void TryContagionPulse(Pawn source)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings != null && !settings.infectionEnabled)
            {
                return;
            }

            if (source == null || source.Dead || !source.Spawned || source.Map == null || !CrossedUtility.IsInfectedPawn(source))
            {
                return;
            }

            int maxTargets = TheMarkedMenSettings.MaxContagionTargetsPerPulse;
            if (maxTargets <= 0 || !source.IsHashIntervalTick(TheMarkedMenSettings.ContagionPulseIntervalTicks))
            {
                return;
            }

            Map map = source.Map;
            IntVec3 sourcePos = source.Position;
            float radiusSq = ContagionRadiusSquared;
            int radInt = Mathf.CeilToInt(ContagionRadius);
            CellRect rect = CellRect.CenteredOn(sourcePos, radInt);
            int exposedTargets = 0;

            for (int z = rect.minZ; z <= rect.maxZ; z++)
            {
                for (int x = rect.minX; x <= rect.maxX; x++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    float dx = cell.x - sourcePos.x;
                    float dz = cell.z - sourcePos.z;
                    if (dx * dx + dz * dz > radiusSq)
                    {
                        continue;
                    }

                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    List<Thing> things = map.thingGrid.ThingsListAt(cell);
                    for (int t = 0; t < things.Count; t++)
                    {
                        if (things[t] is Pawn target && CanContagionReach(source, target))
                        {
                            if (CrossedUtility.TryExpose(target, TheMarkedMenSettings.CloseContactExposureChance, "contagious Marked Virus contact", source))
                            {
                                exposedTargets++;
                                if (exposedTargets >= maxTargets)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool CanContagionReach(Pawn source, Pawn target)
        {
            if (target == null || target == source || target.Dead || !target.Spawned || target.Map != source.Map)
            {
                return false;
            }

            if (target.RaceProps == null || !target.RaceProps.Humanlike || CrossedUtility.IsInfectedPawn(target) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(target))
            {
                return false;
            }

            if (source.Position.DistanceToSquared(target.Position) > ContagionRadiusSquared)
            {
                return false;
            }

            return GenSight.LineOfSight(source.Position, target.Position, source.Map);
        }
    }
}
