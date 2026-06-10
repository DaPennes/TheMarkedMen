using Verse;

namespace TheMarkedMen
{
    public class HediffCompProperties_PsychicPulse : HediffCompProperties
    {
        public float radius = 12f;
        public int pulseIntervalTicks = 500;
        public HediffDef appliedHediff;

        public HediffCompProperties_PsychicPulse()
        {
            compClass = typeof(HediffComp_PsychicPulse);
        }
    }
}
