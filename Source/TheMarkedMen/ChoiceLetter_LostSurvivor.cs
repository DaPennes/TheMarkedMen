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
                return new List<DiaOption> { Option_Close };
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
        }
    }
}
