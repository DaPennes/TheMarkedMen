using Verse;

namespace TheMarkedMen
{
    public class CompProperties_DrawBlood : CompProperties
    {
        public ThingDef bloodSampleDef;

        /// <summary>How long drawing blood takes, in ticks. 180 ticks is 3 seconds.</summary>
        public int drawDurationTicks = 180;

        public CompProperties_DrawBlood()
        {
            compClass = typeof(CompDrawBlood);
        }
    }
}
