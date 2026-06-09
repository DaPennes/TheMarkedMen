using Verse;

namespace TheMarkedMen
{
    public sealed class HediffCompProperties_CrossVirus : HediffCompProperties
    {
        public int incubationTicks = 180;
        public int commonTransformationMinTicks = 180;
        public int commonTransformationMaxTicks = 480;
        public float rareSlowProgressionChance = 0.02f;
        public int rareTransformationMinTicks = 600;
        public int rareTransformationMaxTicks = 900;
        public float symptomOnsetFraction = 0.15f;
        public float immunityChance = TheMarkedMenSettings.DefaultImmunitySurvivalChance;
        public float terminalTransformationChance = TheMarkedMenSettings.DefaultTerminalTransformationChance;
        public float transformedSeverity = 1f;

        public HediffCompProperties_CrossVirus()
        {
            compClass = typeof(HediffComp_CrossVirus);
        }
    }
}
