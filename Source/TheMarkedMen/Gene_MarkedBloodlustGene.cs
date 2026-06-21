using Verse;
using RimWorld;

namespace TheMarkedMen
{
    public class Gene_MarkedBloodlustGene : Gene
    {
        private Need_MarkedBloodlust cachedNeed;

        public Need_MarkedBloodlust CachedNeed
        {
            get
            {
                if (cachedNeed == null && pawn.needs != null)
                {
                    cachedNeed = pawn.needs.TryGetNeed<Need_MarkedBloodlust>();
                }
                return cachedNeed;
            }
        }

        public override void PostAdd()
        {
            base.PostAdd();
            if (pawn.needs != null && CADefOf.MarkedBloodlustNeed != null && pawn.needs.TryGetNeed<Need_MarkedBloodlust>() == null)
            {
                Need_MarkedBloodlust need = new Need_MarkedBloodlust(pawn);
                pawn.needs.AllNeeds.Add(need);
                cachedNeed = need;
            }
        }

        public override void PostRemove()
        {
            base.PostRemove();
            if (cachedNeed != null && pawn.needs != null)
            {
                pawn.needs.AllNeeds.Remove(cachedNeed);
                cachedNeed = null;
            }
        }
    }
}
