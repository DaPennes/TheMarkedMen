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
                yield return new DiaOption("CA_LostSurvivor_Accept".Translate(pawn.Named("PAWN")).Resolve())
                {
                    action = AcceptAction
                };

                yield return new DiaOption("CA_LostSurvivor_Reject".Translate())
                {
                    action = RejectAction
                };
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref spawnSpot, "spawnSpot");
            Scribe_References.Look(ref spawnMap, "spawnMap");
        }

        private void AcceptAction()
        {
            pawn.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(pawn, spawnSpot, spawnMap, Rot4.Random);
            ApplyDormantMark(pawn);
        }

        private void RejectAction()
        {
            pawn.Destroy(DestroyMode.Vanish);
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
