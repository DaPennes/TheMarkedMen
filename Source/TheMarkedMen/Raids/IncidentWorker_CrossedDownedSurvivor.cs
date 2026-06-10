using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    public sealed class IncidentWorker_CrossedDownedSurvivor : IncidentWorker
    {
        private const int MaxSurvivors = 2;
        private const float InitialVirusSeverity = 0.25f;

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
            if (map == null) return false;

            int count = Rand.RangeInclusive(1, MaxSurvivors);
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = PickSurvivorKind(parms.points);
                if (kind == null) continue;

                Pawn pawn = PawnGenerator.GeneratePawn(kind, Faction.OfPlayerSilentFail, map.Tile);
                if (pawn == null) continue;

                if (!CellFinder.TryFindRandomEdgeCellWith(
                        c => c.Standable(map) && !c.Fogged(map) && c.GetEdifice(map) == null && pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly),
                        map, 0f, out IntVec3 spawnCell))
                    spawnCell = CellFinder.RandomEdgeCell(map);

                GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
                pawn.health.DropBloodFilth();

                HediffDef virus = CADefOf.CrossVirus;
                if (virus != null)
                {
                    Hediff hediff = pawn.health.AddHediff(virus);
                    hediff.Severity = InitialVirusSeverity;
                }

                HealthUtility.DamageUntilDowned(pawn, true);
                pawn.ClearAllReservations();

                CrossedUtility.ApplyInfectedTattoo(pawn);
            }

            SendLetter(parms, map);
            return true;
        }

        private static PawnKindDef PickSurvivorKind(float points)
        {
            PawnKindDef selected = null;
            float totalWeight = 0f;
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Hunter, 3f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Stalker, 3f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Berserker, 5f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Screamer, points >= 150f ? 1.5f : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Brute, points >= 300f ? 0.5f : 0f);
            return selected ?? CADefOf.Berserker ?? CADefOf.Hunter;
        }

        private static void AddWeightedKind(ref PawnKindDef selected, ref float totalWeight, PawnKindDef kind, float weight)
        {
            weight = TheMarkedMenSettings.AdjustKindWeight(kind, weight);
            if (kind == null || weight <= 0f) return;
            totalWeight += weight;
            if (Rand.Value * totalWeight <= weight)
                selected = kind;
        }

        private void SendLetter(IncidentParms parms, Map map)
        {
            if (Find.LetterStack == null) return;
            Find.LetterStack.ReceiveLetter(
                def.letterLabel,
                def.letterText,
                def.letterDef ?? LetterDefOf.ThreatSmall,
                new LookTargets(CellFinder.RandomEdgeCell(map), map));
        }
    }
}
