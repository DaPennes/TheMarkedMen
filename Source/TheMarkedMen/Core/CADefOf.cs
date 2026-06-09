using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public static class CADefOf
    {
        private static HediffDef crossVirus;
        private static HediffDef crossVirusImmunity;
        private static HediffDef crossedRash;
        private static HediffDef bloodRush;
        private static HediffDef commandAura;
        private static HediffDef panic;
        private static ThoughtDef crossedSocialTerror;
        private static FactionDef crossedFaction;
        private static InteractionDef crossedPredatoryTaunt;
        private static InteractionDef crossedFalseMercy;
        private static InteractionDef crossedPackLaughter;
        private static InteractionDef crossedInfectionGloat;
        private static PawnKindDef berserker;
        private static PawnKindDef hunter;
        private static PawnKindDef brute;
        private static PawnKindDef stalker;
        private static PawnKindDef screamer;
        private static PawnKindDef alpha;
        private static PawnKindDef child;
        private static TattooDef crossedFaceTattoo;
        private static TattooDef crossedFaceTattooStage1;
        private static TattooDef crossedFaceTattooStage2;
        private static TattooDef crossedFaceTattooStage3;
        private static TattooDef crossedFaceTattooStage4;
        private static IncidentDef crossedRaid;
        private static IncidentDef crossedHorde;
        private static IncidentDef crossedProbe;
        private static StatDef markedVirusResistance;

        public static HediffDef CrossVirus => crossVirus ?? (crossVirus = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossVirus"));
        public static HediffDef CrossVirusImmunity => crossVirusImmunity ?? (crossVirusImmunity = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossVirusImmunity"));
        public static HediffDef CrossedRash => crossedRash ?? (crossedRash = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedRash"));
        public static HediffDef BloodRush => bloodRush ?? (bloodRush = DefDatabase<HediffDef>.GetNamedSilentFail("CA_BloodRush"));
        public static HediffDef CommandAura => commandAura ?? (commandAura = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CommandAura"));
        public static HediffDef Panic => panic ?? (panic = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedPanic"));
        public static ThoughtDef CrossedSocialTerror => crossedSocialTerror ?? (crossedSocialTerror = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_CrossedSocialTerror"));
        public static FactionDef CrossedFaction => crossedFaction ?? (crossedFaction = DefDatabase<FactionDef>.GetNamedSilentFail("CA_CrossedFaction"));
        public static InteractionDef CrossedPredatoryTaunt => crossedPredatoryTaunt ?? (crossedPredatoryTaunt = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedPredatoryTaunt"));
        public static InteractionDef CrossedFalseMercy => crossedFalseMercy ?? (crossedFalseMercy = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedFalseMercy"));
        public static InteractionDef CrossedPackLaughter => crossedPackLaughter ?? (crossedPackLaughter = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedPackLaughter"));
        public static InteractionDef CrossedInfectionGloat => crossedInfectionGloat ?? (crossedInfectionGloat = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedInfectionGloat"));
        public static PawnKindDef Berserker => berserker ?? (berserker = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedBerserker"));
        public static PawnKindDef Hunter => hunter ?? (hunter = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedHunter"));
        public static PawnKindDef Brute => brute ?? (brute = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedBrute"));
        public static PawnKindDef Stalker => stalker ?? (stalker = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedStalker"));
        public static PawnKindDef Screamer => screamer ?? (screamer = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedScreamer"));
        public static PawnKindDef Alpha => alpha ?? (alpha = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedAlpha"));
        public static PawnKindDef Child => child ?? (child = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedChild"));
        public static TattooDef CrossedFaceTattoo => crossedFaceTattoo ?? (crossedFaceTattoo = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRash"));
        public static TattooDef CrossedFaceTattooStage1 => crossedFaceTattooStage1 ?? (crossedFaceTattooStage1 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage1"));
        public static TattooDef CrossedFaceTattooStage2 => crossedFaceTattooStage2 ?? (crossedFaceTattooStage2 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage2"));
        public static TattooDef CrossedFaceTattooStage3 => crossedFaceTattooStage3 ?? (crossedFaceTattooStage3 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage3"));
        public static TattooDef CrossedFaceTattooStage4 => crossedFaceTattooStage4 ?? (crossedFaceTattooStage4 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage4"));
        public static IncidentDef CrossedRaid => crossedRaid ?? (crossedRaid = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedRaid"));
        public static IncidentDef CrossedHorde => crossedHorde ?? (crossedHorde = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedHorde"));
        public static IncidentDef CrossedProbe => crossedProbe ?? (crossedProbe = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedProbe"));
        public static StatDef MarkedVirusResistance => markedVirusResistance ?? (markedVirusResistance = DefDatabase<StatDef>.GetNamedSilentFail("CA_MarkedVirusResistance"));

        public static bool IsCrossedFaceTattoo(TattooDef tattoo)
        {
            return tattoo != null
                && (tattoo == CrossedFaceTattoo
                    || tattoo == CrossedFaceTattooStage1
                    || tattoo == CrossedFaceTattooStage2
                    || tattoo == CrossedFaceTattooStage3
                    || tattoo == CrossedFaceTattooStage4);
        }
    }
}
