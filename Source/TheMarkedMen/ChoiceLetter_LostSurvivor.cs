using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public class ChoiceLetter_LostSurvivor : ChoiceLetter
    {
        public Pawn pawn;
        public IntVec3 spawnSpot;
        public Map spawnMap;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                string acceptText = "CA_LostSurvivor_Accept".Translate(pawn.Named("PAWN")).Resolve();
                string rejectText = "CA_LostSurvivor_Reject".Translate();

                List<DiaOption> list = new List<DiaOption>();

                list.Add(new DiaOption(acceptText)
                {
                    action = delegate
                    {
                        if (Find.WorldPawns.Contains(pawn))
                        {
                            Find.WorldPawns.RemovePawn(pawn);
                        }
                        pawn.SetFaction(Faction.OfPlayer);
                        GenSpawn.Spawn(pawn, spawnSpot, spawnMap, Rot4.Random);
                        ApplyDormantMark(pawn);
                        Find.LetterStack.RemoveLetter(this);
                    }
                });

                list.Add(new DiaOption(rejectText)
                {
                    action = delegate
                    {
                        if (Find.WorldPawns.Contains(pawn))
                        {
                            Find.WorldPawns.RemovePawn(pawn);
                        }
                        if (!pawn.Destroyed)
                        {
                            pawn.Destroy(DestroyMode.Vanish);
                        }
                        Find.LetterStack.RemoveLetter(this);
                    }
                });

                return list;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref spawnSpot, "spawnSpot");
            Scribe_References.Look(ref spawnMap, "spawnMap");
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
