using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheMarkedMen
{
    public class IncidentWorker_UrbanAmbush : IncidentWorker
    {
        public override float ChanceFactorNow(IIncidentTarget target)
        {
            float baseChance = base.ChanceFactorNow(target);
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.urbanAmbushesEnabled)
            {
                return 0f;
            }
            float freq = settings.urbanAmbushFrequency;
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
            if (settings == null || !settings.urbanAmbushesEnabled)
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

            Pawn attacker = TryGenerateAttacker(map);
            if (attacker == null) return false;

            Pawn victim = TryGenerateVictim(map, attacker);
            if (victim == null) return false;

            IntVec3 dropSpot = CellFinder.RandomEdgeCell(map);
            if (!dropSpot.IsValid)
            {
                dropSpot = CellFinderLoose.RandomCellWith(c => c.Walkable(map), map, 100);
            }
            if (!dropSpot.IsValid) return false;

            GenSpawn.Spawn(attacker, dropSpot, map, Rot4.Random);
            GenSpawn.Spawn(victim, dropSpot, map, Rot4.Random);

            // Use standard letter instead of custom ChoiceLetter
            LetterMaker.MakeLetter(def.letterLabel, def.letterText, def.letterDef, new LookTargets(attacker, victim));

            return true;
        }

        private Pawn TryGenerateAttacker(Map map)
        {
            for (int i = 0; i < 20; i++)
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

        private Pawn TryGenerateVictim(Map map, Pawn attacker)
        {
            for (int i = 0; i < 20; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                if (pawn == null) continue;
                if (pawn.Dead) continue;
                if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike) continue;
                if (pawn.IsQuestLodger()) continue;
                if (pawn.IsMutant) continue;
                if (CrossedUtility.IsInfectedPawn(pawn)) continue;
                if (pawn == attacker) continue;
                return pawn;
            }
            return null;
        }
    }
}