using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TheMarkedMen
{
    public sealed class IncidentWorker_CrossedInfectedCaravan : IncidentWorker
    {
        private const int MinCaravanCount = 3;
        private const int MaxCaravanCount = 6;

        public override float ChanceFactorNow(IIncidentTarget target)
        {
            return base.ChanceFactorNow(target) * TheMarkedMenSettings.ProbeFrequencyMultiplier;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms) || !(parms.target is Map map) || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
                return false;
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (map == null || crossed == null) return false;

            parms.faction = crossed;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = TheMarkedMenSettings.MarkedCanTimeoutOrFlee;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;

            int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(MinCaravanCount, MaxCaravanCount, Mathf.InverseLerp(100f, 600f, parms.points))), MinCaravanCount, MaxCaravanCount);
            List<Pawn> pawns = new List<Pawn>(count);

            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = PickCaravanKind(parms.points);
                if (kind == null) continue;

                Pawn pawn = PawnGenerator.GeneratePawn(kind, crossed, map.Tile);
                if (pawn == null) continue;

                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                pawns.Add(pawn);
            }

            if (pawns.Count == 0) return false;

            parms.pawnCount = pawns.Count;
            parms.raidArrivalMode.Worker.Arrive(pawns, parms);

            pawns = CrossedLordCleanupUtility.CollectValidSpawnedLordPawns(pawns, map, crossed);
            if (pawns.Count == 0) return false;

            LordMaker.MakeNewLord(crossed, new LordJob_AssaultColony(crossed, false, TheMarkedMenSettings.MarkedCanTimeoutOrFlee, false, false, false, false, true), map, pawns);

            SendLetter(pawns, parms);
            return true;
        }

        private static PawnKindDef PickCaravanKind(float points)
        {
            float normalized = Mathf.InverseLerp(100f, 600f, points);
            PawnKindDef selected = null;
            float totalWeight = 0f;
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Hunter, Mathf.Lerp(3f, 6f, normalized));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Stalker, Mathf.Lerp(2f, 4f, normalized));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Berserker, 4f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Brute, points >= 400f ? Mathf.Lerp(0.5f, 2f, normalized) : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Alpha, points >= 600f ? 0.3f : 0f);
            return selected ?? CADefOf.Hunter ?? CADefOf.Berserker;
        }

        private static void AddWeightedKind(ref PawnKindDef selected, ref float totalWeight, PawnKindDef kind, float weight)
        {
            weight = TheMarkedMenSettings.AdjustKindWeight(kind, weight);
            if (kind == null || weight <= 0f) return;
            totalWeight += weight;
            if (Rand.Value * totalWeight <= weight)
                selected = kind;
        }

        private void SendLetter(List<Pawn> pawns, IncidentParms parms)
        {
            if (Find.LetterStack == null) return;
            string label = "Suspicious travelers approaching";
            string text = "A group of travelers approaches the colony. Something is wrong with them — their movements are too coordinated, their gazes too fixed. They are not here to trade.";
            Find.LetterStack.ReceiveLetter(label, text, def.letterDef ?? LetterDefOf.ThreatSmall, new LookTargets(pawns));
        }
    }
}
