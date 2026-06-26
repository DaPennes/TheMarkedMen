using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public class IncidentWorker_LostSurvivor : IncidentWorker
    {
        private const float MinDormancyDays = 8f;
        private const float MaxDormancyDays = 30f;
        private const int MaxSurvivorPawnRetries = 10;
        private const int MinColonistsForSurvivor = 0;

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
            if (map.mapPawns.FreeColonistsSpawnedCount < MinColonistsForSurvivor)
            {
                return false;
            }
            if (CrossedUtility.Component?.EnsureCrossedFaction() == null)
            {
                return false;
            }
            if (CanAddPawn(map) == false)
            {
                return false;
            }
            return true;
        }

        private bool CanAddPawn(Map map)
        {
            int pawnCount = map.mapPawns.FreeColonistsSpawnedCount;
            if (pawnCount <= 0) return false;
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;

            Faction crossed = Component?.EnsureCrossedFaction();
            if (crossed == null) return false;

            Pawn survivor = TryGenerateSurvivor(parms, map, crossed);
            if (survivor == null) return false;

            IntVec3 dropSpot = FindDropSpot(map);
            if (dropSpot == IntVec3.Invalid) return false;

            GenSpawn.Spawn(survivor, dropSpot, map, Rot4.Random);
            ApplyDormantMark(survivor);

            string label = def.letterLabel ?? "CA_LostSurvivor_Title".Translate();
            string text = def.letterText ?? "CA_LostSurvivor_Desc".Translate(survivor.Named("PAWN")).Resolve();
            Find.LetterStack.ReceiveLetter(label, text, def.letterDef ?? LetterDefOf.NeutralEvent, new LookTargets(survivor));

            return true;
        }

        private Pawn TryGenerateSurvivor(IncidentParms parms, Map map, Faction faction)
        {
            for (int i = 0; i < MaxSurvivorPawnRetries; i++)
            {
                PawnKindDef kind = PawnKindDefOf.Colonist;
                Faction survivorFaction = Faction.OfPlayer;
                Pawn pawn = PawnGenerator.GeneratePawn(kind, survivorFaction);
                if (pawn == null) continue;

                if (!CanBeSurvivor(pawn, map)) continue;

                pawn.SetFaction(Faction.OfPlayer);
                return pawn;
            }
            return null;
        }

        private bool CanBeSurvivor(Pawn pawn, Map map)
        {
            if (pawn == null || pawn.Dead) return false;
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike) return false;
            if (pawn.IsQuestLodger()) return false;
            if (pawn.IsMutant) return false;
            if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer) return false;
            if (CrossedUtility.IsInfectedPawn(pawn)) return false;
            if (pawn.health?.hediffSet?.HasHediff(CADefOf.CA_DormantMark) == true) return false;
            return true;
        }

        private void ApplyDormantMark(Pawn pawn)
        {
            if (pawn == null || pawn.health == null) return;
            Hediff dormantMark = HediffMaker.MakeHediff(CADefOf.CA_DormantMark, pawn);
            pawn.health.AddHediff(dormantMark);
        }

        private IntVec3 FindDropSpot(Map map)
        {
            IntVec3 spot = CellFinderLoose.RandomCellWith(
                c => c.Standable(map) && !c.Fogged(map) && c.GetRoom(map) != null && !c.GetRoom(map).IsPrisonCell,
                map, 100);
            if (spot == IntVec3.Invalid)
            {
                spot = CellFinderLoose.RandomCellWith(
                    c => c.Standable(map) && !c.Fogged(map),
                    map, 100);
            }
            return spot;
        }
    }
}
