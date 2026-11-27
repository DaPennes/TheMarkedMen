using Verse;

namespace TheMarkedMen
{
    public class CompProperties_DrawBlood : CompProperties
    {
        public ThingDef bloodSampleDef;

        
        public int drawDurationTicks = 180;

        public CompProperties_DrawBlood()
        {
            compClass = typeof(CompDrawBlood);
        }
    }
}
