using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheMarkedMen
{
    public class ChoiceLetter_LostSurvivor : ChoiceLetter
    {
        public Pawn pawn;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                var options = new List<DiaOption>();

                options.Add(new DiaOption("CA_LostSurvivor_Accept".Translate(pawn.Named("PAWN")).Resolve())
                {
                    action = delegate
                    {
                        pawn.SetFaction(Faction.OfPlayer);
                        ApplyDormantMark(pawn);
                    }
                });

                options.Add(new DiaOption("CA_LostSurvivor_Reject".Translate())
                {
                    action = delegate
                    {
                        if (pawn.Spawned)
                        {
                            pawn.DeSpawn();
                        }
                        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                    }
                });

                return options;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
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
