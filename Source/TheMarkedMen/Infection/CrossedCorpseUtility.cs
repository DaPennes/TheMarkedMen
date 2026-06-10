using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static class CrossedCorpseUtility
    {
        private const float CorpseContaminationRadius = 3.2f;
        private const float CorpseContaminationRadiusSquared = CorpseContaminationRadius * CorpseContaminationRadius;
        private const float CorpseLingeringExposureRadius = 2.4f;
        private const float CorpseLingeringExposureRadiusSquared = CorpseLingeringExposureRadius * CorpseLingeringExposureRadius;
        private const float CorpseLingeringExposureChance = 0.10f;
        private const int CorpseLingeringMaxObservedTicksPerPulse = 750;

        public static void TryContaminateNearbyCorpses(Pawn source)
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

            int maxCorpses = TheMarkedMenSettings.MaxCorpsesPerPulse;
            if (maxCorpses <= 0 || !source.IsHashIntervalTick(TheMarkedMenSettings.CorpseContaminationIntervalTicks))
            {
                return;
            }

            int contaminated = 0;
            int numCells = GenRadial.NumCellsInRadius(CorpseContaminationRadius);
            for (int cellIndex = 0; cellIndex < numCells; cellIndex++)
            {
                IntVec3 cell = source.Position + GenRadial.ManualRadialPattern[cellIndex];
                if (!cell.InBounds(source.Map))
                {
                    continue;
                }

                List<Thing> things = source.Map.thingGrid.ThingsListAt(cell);
                for (int thingIndex = 0; thingIndex < things.Count; thingIndex++)
                {
                    Corpse corpse = things[thingIndex] as Corpse;
                    if (corpse == null || !CanContaminateCorpse(source, corpse))
                    {
                        continue;
                    }

                    if (Rand.Chance(TheMarkedMenSettings.CorpseContaminationChance) && TryContaminateCorpse(source, corpse))
                    {
                        contaminated++;
                        if (contaminated >= maxCorpses)
                        {
                            return;
                        }
                    }
                }
            }
        }

        public static void TryExposeNearbyPawnsToInfectedCorpses()
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings != null && !settings.infectionEnabled)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            if (maps == null)
            {
                return;
            }

            for (int i = 0; i < maps.Count; i++)
            {
                TryExposeNearbyPawnsToInfectedCorpses(maps[i]);
            }
        }

        private static void TryExposeNearbyPawnsToInfectedCorpses(Map map)
        {
            if (map?.listerThings == null || map.mapPawns == null)
            {
                return;
            }

            int maxTargets = TheMarkedMenSettings.MaxContagionTargetsPerPulse;
            if (maxTargets <= 0)
            {
                return;
            }

            TheMarkedMenGameComponent component = CrossedUtility.Component;
            if (component == null)
            {
                return;
            }

            List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            if (corpses == null || corpses.Count == 0)
            {
                return;
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            int observedTicks = Mathf.Min(TheMarkedMenSettings.CorpseContaminationIntervalTicks, CorpseLingeringMaxObservedTicksPerPulse);
            int exposedTargets = 0;
            int numCells = GenRadial.NumCellsInRadius(CorpseLingeringExposureRadius);
            for (int corpseIndex = 0; corpseIndex < corpses.Count; corpseIndex++)
            {
                Corpse corpse = corpses[corpseIndex] as Corpse;
                if (!IsInfectiousMarkedVirusCorpse(corpse))
                {
                    continue;
                }

                for (int cellIndex = 0; cellIndex < numCells; cellIndex++)
                {
                    IntVec3 cell = corpse.Position + GenRadial.ManualRadialPattern[cellIndex];
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    List<Thing> things = map.thingGrid.ThingsListAt(cell);
                    for (int thingIndex = 0; thingIndex < things.Count; thingIndex++)
                    {
                        Pawn target = things[thingIndex] as Pawn;
                        if (!CanCorpseExposePawn(corpse, target))
                        {
                            continue;
                        }

                        if (!component.NoteCorpseLingering(target, currentTick, observedTicks))
                        {
                            continue;
                        }

                        component.ResetCorpseLingering(target);
                        if (CrossedUtility.TryExpose(target, CorpseLingeringExposureChance, "lingering near infected corpse", corpse.InnerPawn))
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

        private static bool IsInfectiousMarkedVirusCorpse(Corpse corpse)
        {
            Pawn innerPawn = corpse?.InnerPawn;
            if (innerPawn == null || corpse.Destroyed || innerPawn.Destroyed || !innerPawn.Dead)
            {
                return false;
            }

            if (innerPawn.RaceProps == null || !innerPawn.RaceProps.Humanlike)
            {
                return false;
            }

            return CrossedUtility.HasMarkedVirusHediff(innerPawn) || CrossedUtility.ShouldReanimateAsCrossed(innerPawn);
        }

        private static bool CanCorpseExposePawn(Corpse corpse, Pawn target)
        {
            if (corpse?.Map == null || target == null || target.Dead || !target.Spawned || target.Map != corpse.Map)
            {
                return false;
            }

            if (target.RaceProps == null || !target.RaceProps.Humanlike || CrossedUtility.IsInfectedPawn(target) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(target))
            {
                return false;
            }

            if (target.Position.DistanceToSquared(corpse.Position) > CorpseLingeringExposureRadiusSquared)
            {
                return false;
            }

            return GenSight.LineOfSight(target.Position, corpse.Position, target.Map);
        }

        private static bool CanContaminateCorpse(Pawn source, Corpse corpse)
        {
            Pawn innerPawn = corpse?.InnerPawn;
            if (innerPawn == null || corpse.Destroyed || innerPawn.Destroyed || !innerPawn.Dead)
            {
                return false;
            }

            if (innerPawn.RaceProps == null || !innerPawn.RaceProps.Humanlike || CrossedUtility.HasCrossVirusImmunity(innerPawn))
            {
                return false;
            }

            if (CrossedUtility.ShouldReanimateAsCrossed(innerPawn))
            {
                CrossedReanimationManager.QueueCrossedReanimation(innerPawn);
                return false;
            }

            if (source.Position.DistanceToSquared(corpse.Position) > CorpseContaminationRadiusSquared)
            {
                return false;
            }

            return GenSight.LineOfSight(source.Position, corpse.Position, source.Map);
        }

        private static bool TryContaminateCorpse(Pawn source, Corpse corpse)
        {
            Pawn innerPawn = corpse?.InnerPawn;
            HediffDef virus = CADefOf.CrossVirus;
            if (innerPawn?.health == null || virus == null)
            {
                return false;
            }

            Hediff hediff = innerPawn.health.hediffSet.GetFirstHediffOfDef(virus) ?? innerPawn.health.AddHediff(virus);
            hediff.Severity = Mathf.Max(hediff.Severity, 1f);
            hediff.TryGetComp<HediffComp_CrossVirus>()?.NotifyInfector(source);
            CrossedReanimationManager.QueueCrossedReanimation(innerPawn);
            if (innerPawn.Faction == Faction.OfPlayer)
            {
                CrossedUtility.Component?.AddIncident(innerPawn.LabelShortCap + "'s corpse was contaminated by Marked Virus exposure.");
            }

            return true;
        }
    }
}
