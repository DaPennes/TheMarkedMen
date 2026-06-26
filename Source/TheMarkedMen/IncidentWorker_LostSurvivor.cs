using System.Collections.Generic;
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

            Find.WorldPawns.PassToWorld(survivor, PawnDiscardDecideMode.KeepForever);

            IntVec3 dropSpot = CellFinder.RandomEdgeCell(map);
            if (!dropSpot.IsValid)
            {
                dropSpot = CellFinderLoose.RandomCellWith(c => c.Walkable(map), map, 100);
            }
            if (!dropSpot.IsValid) return false;

            string title = def.letterLabel ?? "CA_LostSurvivor_Title".Translate();
            string desc = def.letterText ?? "CA_LostSurvivor_Desc".Translate(survivor.Named("PAWN")).Resolve();
            string acceptText = "CA_LostSurvivor_Accept".Translate(survivor.Named("PAWN")).Resolve();
            string rejectText = "CA_LostSurvivor_Reject".Translate();

            ChoiceLetter_LostSurvivor letter = (ChoiceLetter_LostSurvivor)LetterMaker.MakeLetter(
                title, desc,
                DefDatabase<LetterDef>.GetNamed("CA_LostSurvivorLetter"),
                new LookTargets(dropSpot, map));

            letter.pawn = survivor;
            letter.title = title;
            letter.Text = desc;

            Find.LetterStack.ReceiveLetter(letter);

            DiaNode node = new DiaNode(desc);
            DiaOption acceptOpt = new DiaOption(acceptText);
            acceptOpt.action = delegate
            {
                if (Find.WorldPawns.Contains(survivor))
                {
                    Find.WorldPawns.RemovePawn(survivor);
                }
                survivor.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(survivor, dropSpot, map, Rot4.Random);
                ApplyDormantMark(survivor);
            };
            node.options.Add(acceptOpt);

            DiaOption rejectOpt = new DiaOption(rejectText);
            rejectOpt.action = delegate
            {
                if (Find.WorldPawns.Contains(survivor))
                {
                    Find.WorldPawns.RemovePawn(survivor);
                }
                if (!survivor.Destroyed)
                {
                    survivor.Destroy(DestroyMode.Vanish);
                }
            };
            node.options.Add(rejectOpt);

            Dialog_NodeTree dialog = new Dialog_NodeTree(node, false, false, title);
            Find.WindowStack.Add(dialog);

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
