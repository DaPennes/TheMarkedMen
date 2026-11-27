using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheMarkedMen
{
    public class IncidentWorker_LostSurvivor : IncidentWorker
    {
        private const int MaxSurvivorPawnRetries = 10;
        private const int RandomSpawnMaxAttempts = 200;

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

            IntVec3 dropSpot;
            if (!TryFindValidSpawnCell(map, out dropSpot))
            {
                return false;
            }

            // Set guest status before spawning so the pawn is a guest from the moment it appears
            SetGuestOfPlayer(survivor);
            ApplyDormantMark(survivor);

            GenSpawn.Spawn(survivor, dropSpot, map, Rot4.Random);

            // Abort if the pawn did not actually appear on the map; a letter with no
            // spawned pawn would only offer a Close button.
            if (survivor == null || !survivor.Spawned)
            {
                Log.Warning("[TheMarkedMen] Lost survivor did not spawn at " + dropSpot);
                return false;
            }

            Letter choiceLetter = ChoiceLetter_LostSurvivor.Create(def.letterLabel, def.letterText, survivor);
            Find.LetterStack.ReceiveLetter(choiceLetter);

            return true;
        }

        private static void SetGuestOfPlayer(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }
            pawn.SetFaction(null);
            if (pawn.guest != null)
            {
                pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Guest);
            }
        }

        private bool TryFindValidSpawnCell(Map map, out IntVec3 result)
        {
            if (CellFinder.TryFindRandomEdgeCellWith(
                (IntVec3 c) => IsValidSpawnCell(map, c),
                map,
                0f,
                out IntVec3 edgeResult))
            {
                result = edgeResult;
                return true;
            }

            for (int i = 0; i < RandomSpawnMaxAttempts; i++)
            {
                IntVec3 cell = CellFinder.RandomCell(map);
                if (IsValidSpawnCell(map, cell))
                {
                    result = cell;
                    return true;
                }
            }

            result = IntVec3.Invalid;
            return false;
        }

        private static bool IsValidSpawnCell(Map map, IntVec3 cell)
        {
            if (!cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            if (cell.Fogged(map))
            {
                return false;
            }

            if (!cell.Standable(map))
            {
                return false;
            }

            if (!cell.Walkable(map))
            {
                return false;
            }

            if (cell.Impassable(map))
            {
                return false;
            }

            Building building = cell.GetEdifice(map);
            if (building != null)
            {
                if (building.Faction == Faction.OfPlayer)
                {
                    return false;
                }

                if (building is Building_Door)
                {
                    return false;
                }

                if (building is Building_Trap)
                {
                    return false;
                }
            }

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain != null && terrain.passability == Traversability.Impassable)
            {
                return false;
            }

            return true;
        }

        private Pawn TryGenerateSurvivor(Map map)
        {
            for (int i = 0; i < MaxSurvivorPawnRetries; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, null);
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