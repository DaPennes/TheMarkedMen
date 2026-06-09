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

            IReadOnlyList<Pawn> pawns = source.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            int exposedTargets = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn target = pawns[i];
                if (!CanContagionReach(source, target))
                {
                    continue;
                }

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
