using System.Collections.Generic;
using RimWorld;
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
                if (pawn != null && !pawn.Dead && pawn.Spawned)
                {
                    yield return DiaOptionAccept;
                    yield return DiaOptionReject;
                }
                yield return Option_Close;
            }
        }

        private DiaOption DiaOptionAccept
        {
            get
            {
                DiaOption diaOption = new DiaOption("CA_LostSurvivor_Accept".Translate());
                diaOption.action = () =>
                {
                    if (pawn != null && !pawn.Dead && pawn.Spawned)
                    {
                        pawn.SetFaction(Faction.OfPlayer);
                    }
                };
                diaOption.resolveTree = true;
                return diaOption;
            }
        }

        private DiaOption DiaOptionReject
        {
            get
            {
                DiaOption diaOption = new DiaOption("CA_LostSurvivor_Reject".Translate());
                diaOption.action = () =>
                {
                    if (pawn != null && !pawn.Destroyed)
                    {
                        if (pawn.Spawned)
                        {
                            pawn.DeSpawn();
                        }
                        pawn.Destroy(DestroyMode.Vanish);
                    }
                };
                diaOption.resolveTree = true;
                return diaOption;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
        }
    }
}
