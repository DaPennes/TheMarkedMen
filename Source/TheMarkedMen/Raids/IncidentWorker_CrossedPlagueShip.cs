using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class IncidentWorker_CrossedPlagueShip : IncidentWorker
    {
        private const float ContaminationRadius = 5f;
        private const float ContaminationRadiusSquared = ContaminationRadius * ContaminationRadius;
        private const float ExposureChancePerCheck = 0.06f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms) || !(parms.target is Map map) || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
                return false;
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;

            ThingDef shipChunkDef = ThingDefOf.ShipChunk;
            if (shipChunkDef == null) return false;

            IntVec3 spawnCell = IntVec3.Invalid;
            for (int i = 0; i < 20; i++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                if (candidate.Standable(map) && !candidate.Fogged(map) && candidate.DistanceToEdge(map) > 6f)
                {
                    spawnCell = candidate;
                    break;
                }
            }

            if (!spawnCell.IsValid)
                spawnCell = CellFinder.RandomCell(map);

            Thing shipChunk = GenSpawn.Spawn(shipChunkDef, spawnCell, map, Rot4.Random);
            if (shipChunk == null) return false;
            shipChunk.HitPoints = Mathf.Max(1, shipChunk.HitPoints / 3);

            TheMarkedMenGameComponent component = CrossedUtility.Component;
            if (component != null)
                component.RegisterPlagueShipChunk(shipChunk, map);

            SendLetter(parms, map, spawnCell);
            return true;
        }

        public static void TryContaminateNearby(Map map, int ticks)
        {
            if (map == null || !map.IsHashIntervalTick(500)) return;
            TheMarkedMenGameComponent component = CrossedUtility.Component;
            if (component == null) return;

            List<Thing> chunks = component.GetPlagueShipChunks(map);
            if (chunks == null || chunks.Count == 0) return;

            for (int c = 0; c < chunks.Count; c++)
            {
                Thing chunk = chunks[c];
                if (chunk == null || chunk.Destroyed || chunk.Map != map) continue;

                IntVec3 chunkPos = chunk.Position;
                int numCells = GenRadial.NumCellsInRadius(ContaminationRadius);
                for (int cellIndex = 0; cellIndex < numCells; cellIndex++)
                {
                    IntVec3 cell = chunkPos + GenRadial.ManualRadialPattern[cellIndex];
                    if (!cell.InBounds(map)) continue;
                    List<Thing> cellThings = map.thingGrid.ThingsListAt(cell);
                    for (int j = 0; j < cellThings.Count; j++)
                    {
                        Pawn target = cellThings[j] as Pawn;
                        if (target == null || target.Dead || !target.RaceProps.Humanlike || CrossedUtility.IsInfectedPawn(target) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(target))
                            continue;
                        if (target.Position.DistanceToSquared(chunkPos) <= ContaminationRadiusSquared && Rand.Chance(ExposureChancePerCheck))
                            CrossedUtility.TryExpose(target, ExposureChancePerCheck, "plague ship contamination");
                    }
                }
            }
        }

        private void SendLetter(IncidentParms parms, Map map, IntVec3 cell)
        {
            if (Find.LetterStack == null) return;
            Find.LetterStack.ReceiveLetter(
                def.letterLabel,
                def.letterText,
                def.letterDef ?? LetterDefOf.ThreatBig,
                new LookTargets(cell, map));
        }
    }
}
