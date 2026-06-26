using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheMarkedMen
{
    public class IncidentWorker_LostSurvivor : IncidentWorker
    {
        private const int MaxSurvivorPawnRetries = 10;

        private static TheMarkedMenGameComponent Component => CrossedUtility.Component;

        public override float ChanceFactorNow(IIncidentTarget target)
        {
            float baseChance = base.ChanceFactorNow(target);
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.lostSurvivorEnabled)
            {
                return 0f;
            }
            float freq = settings.lostSurvivorFrequencyMultiplier;
            if (freq <= 0f)
            {
                return 0f;
            }
            return baseChance * freq * GetStorytellerFactor();
        }

        private static float GetStorytellerFactor()
        {
            string storyteller = Find.Storyteller?.def?.defName;
            if (storyteller == "CA_TheMarkedMan") return 2.5f;
            if (storyteller == "RandyRandom") return 1.5f;
            if (storyteller == "CassandraClassic") return 0.8f;
            if (storyteller == "PhoebeFriendly") return 0.5f;
            return 1f;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.lostSurvivorEnabled)
            {
                return false;
            }
            if (!(parms.target is Map map) || map.IsPlayerHome == false)
            {
                return false;
            }
            if (map.mapPawns.FreeColonistsSpawnedCount <= 0)
            {
                return false;
            }
            if (CrossedUtility.Component?.EnsureCrossedFaction() == null)
            {
                return false;
            }
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;

            if (CrossedUtility.Component?.EnsureCrossedFaction() == null) return false;

            Pawn survivor = TryGenerateSurvivor(map);
            if (survivor == null) return false;

            IntVec3 dropSpot = CellFinder.RandomEdgeCell(map);
            if (!dropSpot.IsValid)
            {
                dropSpot = CellFinderLoose.RandomCellWith(c => c.Walkable(map), map, 100);
            }
            if (!dropSpot.IsValid) return false;

            GenSpawn.Spawn(survivor, dropSpot, map, Rot4.Random);

            ApplyDormantMark(survivor);

            string label = def.letterLabel.Formatted(survivor.Named("PAWN")).AdjustedFor(survivor, "PAWN");
            string text = def.letterText.Formatted(survivor.Named("PAWN")).AdjustedFor(survivor, "PAWN");

            SendStandardLetter(label, text, LetterDefOf.PositiveEvent, parms, survivor);

            return true;
        }

        private Pawn TryGenerateSurvivor(Map map)
        {
            for (int i = 0; i < MaxSurvivorPawnRetries; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                if (pawn == null) continue;
                if (pawn.Dead) continue;
                if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike) continue;
                if (pawn.IsQuestLodger()) continue;
                if (pawn.IsMutant) continue;
                if (CrossedUtility.IsInfectedPawn(pawn)) continue;
                return pawn;
            }
            return null;
        }

        private void ApplyDormantMark(Pawn pawn)
        {
            if (pawn == null || pawn.health == null)
            {
                return;
            }
            if (pawn.health.hediffSet.HasHediff(CADefOf.CA_DormantMark))
            {
                return;
            }
            Hediff dormantMark = HediffMaker.MakeHediff(CADefOf.CA_DormantMark, pawn);
            pawn.health.AddHediff(dormantMark);
        }
    }
}
