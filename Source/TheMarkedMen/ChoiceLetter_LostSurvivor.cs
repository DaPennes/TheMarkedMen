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

        private List<DiaOption> cachedChoices;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (cachedChoices == null)
                {
                    cachedChoices = new List<DiaOption>();

                    string acceptText = "CA_LostSurvivor_Accept".Translate(pawn.Named("PAWN")).Resolve();
                    string rejectText = "CA_LostSurvivor_Reject".Translate();

                    cachedChoices.Add(new DiaOption(acceptText)
                    {
                        action = delegate
                        {
                            try
                            {
                                pawn.SetFaction(Faction.OfPlayer);
                                GenSpawn.Spawn(pawn, spawnSpot, spawnMap, Rot4.Random);
                                ApplyDormantMark(pawn);
                            }
                            catch (System.Exception ex)
                            {
                                Log.Error("[TheMarkedMen] Accept action error: " + ex);
                            }
                            Find.LetterStack.RemoveLetter(this);
                        }
                    });

                    cachedChoices.Add(new DiaOption(rejectText)
                    {
                        action = delegate
                        {
                            try
                            {
                                pawn.Destroy(DestroyMode.Vanish);
                            }
                            catch (System.Exception ex)
                            {
                                Log.Error("[TheMarkedMen] Reject action error: " + ex);
                            }
                            Find.LetterStack.RemoveLetter(this);
                        }
                    });
                }
                return cachedChoices;
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
