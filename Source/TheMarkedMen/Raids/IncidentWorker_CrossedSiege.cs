using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TheMarkedMen
{
    public sealed class IncidentWorker_CrossedSiege : IncidentWorker
    {
        private const int MinSiegeCount = 4;
        private const int MaxSiegeCount = 10;

        public override float ChanceFactorNow(IIncidentTarget target)
        {
            return base.ChanceFactorNow(target) * TheMarkedMenSettings.HordeFrequencyMultiplier;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms) || !(parms.target is Map map) || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
                return false;
            Difficulty difficulty = Find.Storyteller?.difficulty;
            if (difficulty != null && !difficulty.allowBigThreats)
                return false;
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (map == null || crossed == null) return false;

            parms.faction = crossed;
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = false;
            parms.raidStrategy = DefDatabase<RaidStrategyDef>.GetNamedSilentFail("Siege") ?? RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;

            int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(MinSiegeCount, MaxSiegeCount, Mathf.InverseLerp(200f, 3000f, parms.points))), MinSiegeCount, MaxSiegeCount);
            List<Pawn> pawns = new List<Pawn>(count);

            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = PickSiegeKind(parms.points, count);
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

            LordJob_AssaultColony siegeJob = new LordJob_AssaultColony(crossed, false, false, false, false, false, true, true);
            LordMaker.MakeNewLord(crossed, siegeJob, map, pawns);

            SendLetter(pawns, parms);
            return true;
        }

        private static PawnKindDef PickSiegeKind(float points, int count)
        {
            float normalized = Mathf.InverseLerp(200f, 3000f, points);
            PawnKindDef selected = null;
            float totalWeight = 0f;

            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Hunter, Mathf.Lerp(4f, 8f, normalized));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Berserker, 6f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Stalker, Mathf.Lerp(1f, 3f, normalized));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Screamer, points >= 300f ? 2f : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Brute, points >= 500f ? Mathf.Lerp(0.5f, 3f, Mathf.InverseLerp(500f, 3000f, points)) : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Alpha, count >= 6 && points >= 1000f ? 0.4f : 0f);

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
            Find.LetterStack.ReceiveLetter(
                def.letterLabel,
                def.letterText,
                def.letterDef ?? LetterDefOf.ThreatBig,
                new LookTargets(pawns));
        }
    }
}
