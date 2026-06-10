using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TheMarkedMen
{
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute))]
    public static class Patch_MixedInfectedRaids
    {
        private static bool mixing;

        public static void Postfix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            if (!__result || mixing || parms?.target == null)
            {
                return;
            }

            if (!CrossedStorytellerUtility.IsTheMarkedManActive)
            {
                return;
            }

            if (!(parms.target is Map map) || !map.IsPlayerHome)
            {
                return;
            }

            if (parms.faction?.def == CADefOf.CrossedFaction)
            {
                return;
            }

            IncidentDef incidentDef = __instance.def;
            if (incidentDef?.category == null)
            {
                return;
            }

            if (incidentDef.category != IncidentCategoryDefOf.ThreatBig
                && incidentDef.category != IncidentCategoryDefOf.ThreatSmall)
            {
                return;
            }

            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (crossed == null)
            {
                return;
            }

            mixing = true;
            try
            {
                SpawnMixedInfected(map, parms, crossed);
            }
            finally
            {
                mixing = false;
            }
        }

        private static void SpawnMixedInfected(Map map, IncidentParms parms, Faction crossed)
        {
            float points = parms.points * 0.35f;
            int count = Mathf.Clamp(Mathf.RoundToInt(points / 60f), 1, 6);
            List<PawnKindDef> availableKinds = new List<PawnKindDef>
            {
                CADefOf.Stalker,
                CADefOf.Hunter,
                CADefOf.Berserker
            };

            if (points >= 300f)
            {
                availableKinds.Add(CADefOf.Screamer);
            }

            if (points >= 400f)
            {
                availableKinds.Add(CADefOf.Spitter);
                availableKinds.Add(CADefOf.Charger);
            }

            List<Pawn> pawns = new List<Pawn>(count);
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = availableKinds.RandomElement();
                Pawn pawn = PawnGenerator.GeneratePawn(kind, crossed, map.Tile);
                if (pawn == null)
                {
                    continue;
                }

                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                pawns.Add(pawn);
            }

            if (pawns.Count == 0)
            {
                return;
            }

            CrossedUtility.ApplyGeneratedRaidKindTuning(pawns);
            IntVec3 spawnCenter = parms.spawnCenter.IsValid
                ? parms.spawnCenter
                : map.Center;
            for (int k = 0; k < pawns.Count; k++)
            {
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(spawnCenter, map, 12);
                GenSpawn.Spawn(pawns[k], cell, map, Rot4.Random);
            }

            List<Pawn> attackers = CrossedLordCleanupUtility.CollectValidSpawnedLordPawns(pawns, map, crossed);
            if (attackers.Count > 0)
            {
                LordMaker.MakeNewLord(crossed, new LordJob_AssaultColony(crossed, false, false, false, false, false, points >= 700f, true), map, attackers);
            }
        }
    }
}
