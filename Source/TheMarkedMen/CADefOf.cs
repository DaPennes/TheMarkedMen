using System;
using System.Collections.Generic;
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
        private static HediffDef psychicAura;
        private static ThoughtDef crossedSocialTerror;
        private static FactionDef crossedFaction;
        private static InteractionDef crossedPredatoryTaunt;
        private static InteractionDef crossedFalseMercy;
        private static InteractionDef crossedPackLaughter;
        private static InteractionDef crossedInfectionGloat;
        private static PawnKindDef crossedCivilian;
        private static PawnKindDef crossedScout;
        private static PawnKindDef crossedHunter;
        private static PawnKindDef crossedShooter;
        private static PawnKindDef crossedRaider;
        private static PawnKindDef crossedSoldier;
        private static PawnKindDef crossedBrute;
        private static PawnKindDef crossedPyromaniac;
        private static PawnKindDef crossedAlpha;
        private static PawnKindDef crossedWarlord;
        private static PawnKindDef markedMan;
        private static TattooDef crossedFaceTattoo;
        private static TattooDef crossedFaceTattooStage1;
        private static TattooDef crossedFaceTattooStage2;
        private static TattooDef crossedFaceTattooStage3;
        private static TattooDef crossedFaceTattooStage4;
        private static IncidentDef crossedRaid;
        private static IncidentDef crossedHorde;
        private static IncidentDef crossedProbe;
        private static IncidentDef crossedCaravanAmbush;
        private static IncidentDef urbanAmbush;
        private static IncidentDef urbanSurvivor;
        private static XenotypeDef markedOne;
        private static StatDef markedVirusResistance;
        private static HediffDef killAnticipation;
        private static NeedDef markedBloodlustNeed;
        private static GeneDef markedBloodlustGene;
        private static ThoughtDef freshKillSatisfaction;
        private static ThoughtDef bloodthirstyCraving;
        private static ThoughtDef overwhelmingBloodlust;
        private static ThoughtDef predatorPatience;
        private static ThoughtDef witnessedCrossedTransformation;
        private static HediffDef dormantMark;
        private static HediffDef crossedRampage;
        private static HediffDef crossedStrength;
        private static HediffDef warlordTier;
        private static HediffDef alphaTier;
        private static HediffDef markedTier;
        private static IncidentDef lostSurvivor;
        private static ThoughtDef betrayedByColonist;
        private static ThoughtDef betrayedByColonistSocial;
        private static ThoughtDef lovedOneTurned;
        private static ThoughtDef suspiciousOfColonists;
        private static ThoughtDef mercifulKill;

        public static HediffDef CrossVirus => crossVirus ?? (crossVirus = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossVirus"));
        public static HediffDef CrossVirusImmunity => crossVirusImmunity ?? (crossVirusImmunity = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossVirusImmunity"));
        public static HediffDef CrossedRash => crossedRash ?? (crossedRash = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedRash"));
        public static HediffDef BloodRush => bloodRush ?? (bloodRush = DefDatabase<HediffDef>.GetNamedSilentFail("CA_BloodRush"));
        public static HediffDef CommandAura => commandAura ?? (commandAura = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CommandAura"));
        public static HediffDef Panic => panic ?? (panic = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedPanic"));
        public static HediffDef PsychicAura => psychicAura ?? (psychicAura = DefDatabase<HediffDef>.GetNamedSilentFail("CA_PsychicAura"));
        public static ThoughtDef CrossedSocialTerror => crossedSocialTerror ?? (crossedSocialTerror = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_CrossedSocialTerror"));
        public static ThoughtDef CA_WitnessedCrossedTransformation => witnessedCrossedTransformation ?? (witnessedCrossedTransformation = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_WitnessedCrossedTransformation"));
        public static FactionDef CrossedFaction => crossedFaction ?? (crossedFaction = DefDatabase<FactionDef>.GetNamedSilentFail("CA_CrossedFaction"));
        public static InteractionDef CrossedPredatoryTaunt => crossedPredatoryTaunt ?? (crossedPredatoryTaunt = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedPredatoryTaunt"));
        public static InteractionDef CrossedFalseMercy => crossedFalseMercy ?? (crossedFalseMercy = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedFalseMercy"));
        public static InteractionDef CrossedPackLaughter => crossedPackLaughter ?? (crossedPackLaughter = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedPackLaughter"));
        public static InteractionDef CrossedInfectionGloat => crossedInfectionGloat ?? (crossedInfectionGloat = DefDatabase<InteractionDef>.GetNamedSilentFail("CA_CrossedInfectionGloat"));
        public static PawnKindDef CrossedCivilian => crossedCivilian ?? (crossedCivilian = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedCivilian"));
        public static PawnKindDef CrossedScout => crossedScout ?? (crossedScout = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedScout"));
        public static PawnKindDef CrossedHunter => crossedHunter ?? (crossedHunter = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedHunter"));
        public static PawnKindDef CrossedShooter => crossedShooter ?? (crossedShooter = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedShooter"));
        public static PawnKindDef CrossedRaider => crossedRaider ?? (crossedRaider = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedRaider"));
        public static PawnKindDef CrossedSoldier => crossedSoldier ?? (crossedSoldier = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedSoldier"));
        public static PawnKindDef CrossedBrute => crossedBrute ?? (crossedBrute = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedBrute"));
        public static PawnKindDef CrossedPyromaniac => crossedPyromaniac ?? (crossedPyromaniac = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedPyromaniac"));
        public static PawnKindDef CrossedAlpha => crossedAlpha ?? (crossedAlpha = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedAlpha"));
        public static PawnKindDef CrossedWarlord => crossedWarlord ?? (crossedWarlord = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_CrossedWarlord"));
        public static PawnKindDef MarkedMan => markedMan ?? (markedMan = DefDatabase<PawnKindDef>.GetNamedSilentFail("CA_MarkedMan"));
        public static TattooDef CrossedFaceTattoo => crossedFaceTattoo ?? (crossedFaceTattoo = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRash"));
        public static TattooDef CrossedFaceTattooStage1 => crossedFaceTattooStage1 ?? (crossedFaceTattooStage1 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage1"));
        public static TattooDef CrossedFaceTattooStage2 => crossedFaceTattooStage2 ?? (crossedFaceTattooStage2 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage2"));
        public static TattooDef CrossedFaceTattooStage3 => crossedFaceTattooStage3 ?? (crossedFaceTattooStage3 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage3"));
        public static TattooDef CrossedFaceTattooStage4 => crossedFaceTattooStage4 ?? (crossedFaceTattooStage4 = DefDatabase<TattooDef>.GetNamedSilentFail("CA_Face_CrossedRashStage4"));
        public static IncidentDef CrossedRaid => crossedRaid ?? (crossedRaid = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedRaid"));
        public static IncidentDef CrossedHorde => crossedHorde ?? (crossedHorde = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedHorde"));
        public static IncidentDef CrossedProbe => crossedProbe ?? (crossedProbe = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedProbe"));
        public static IncidentDef CrossedCaravanAmbush => crossedCaravanAmbush ?? (crossedCaravanAmbush = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedCaravanAmbush"));
        public static IncidentDef UrbanAmbush => urbanAmbush ?? (urbanAmbush = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_UrbanAmbush"));
        public static IncidentDef UrbanSurvivor => urbanSurvivor ?? (urbanSurvivor = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_UrbanSurvivor"));
        public static XenotypeDef MarkedOne => markedOne ?? (markedOne = DefDatabase<XenotypeDef>.GetNamedSilentFail("CA_MarkedOne"));
        public static StatDef MarkedVirusResistance => markedVirusResistance ?? (markedVirusResistance = DefDatabase<StatDef>.GetNamedSilentFail("CA_MarkedVirusResistance"));
        public static HediffDef KillAnticipation => killAnticipation ?? (killAnticipation = DefDatabase<HediffDef>.GetNamedSilentFail("CA_KillAnticipation"));
        public static NeedDef MarkedBloodlustNeed => markedBloodlustNeed ?? (markedBloodlustNeed = DefDatabase<NeedDef>.GetNamedSilentFail("CA_MarkedBloodlustNeed"));
        public static GeneDef MarkedBloodlustGene => markedBloodlustGene ?? (markedBloodlustGene = DefDatabase<GeneDef>.GetNamedSilentFail("CA_MarkedBloodlustNeed"));
        public static ThoughtDef FreshKillSatisfaction => freshKillSatisfaction ?? (freshKillSatisfaction = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_FreshKillSatisfaction"));
        public static ThoughtDef BloodthirstyCraving => bloodthirstyCraving ?? (bloodthirstyCraving = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_BloodthirstyCraving"));
        public static ThoughtDef OverwhelmingBloodlust => overwhelmingBloodlust ?? (overwhelmingBloodlust = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_OverwhelmingBloodlust"));
        public static ThoughtDef PredatorPatience => predatorPatience ?? (predatorPatience = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_PredatorPatience"));
        public static HediffDef CA_DormantMark => dormantMark ?? (dormantMark = DefDatabase<HediffDef>.GetNamedSilentFail("CA_DormantMark"));
        public static HediffDef CrossedRampage => crossedRampage ?? (crossedRampage = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedRampage"));
        public static HediffDef CrossedStrength => crossedStrength ?? (crossedStrength = DefDatabase<HediffDef>.GetNamedSilentFail("CA_CrossedStrength"));
        public static HediffDef WarlordTier => warlordTier ?? (warlordTier = DefDatabase<HediffDef>.GetNamedSilentFail("CA_WarlordTier"));
        public static HediffDef AlphaTier => alphaTier ?? (alphaTier = DefDatabase<HediffDef>.GetNamedSilentFail("CA_AlphaTier"));
        public static HediffDef MarkedTier => markedTier ?? (markedTier = DefDatabase<HediffDef>.GetNamedSilentFail("CA_MarkedTier"));
        public static IncidentDef CA_LostSurvivor => lostSurvivor ?? (lostSurvivor = DefDatabase<IncidentDef>.GetNamedSilentFail("CA_LostSurvivor"));
        public static ThoughtDef CA_BetrayedByColonist => betrayedByColonist ?? (betrayedByColonist = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_BetrayedByColonist"));
        public static ThoughtDef CA_BetrayedByColonistSocial => betrayedByColonistSocial ?? (betrayedByColonistSocial = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_BetrayedByColonistSocial"));
        public static ThoughtDef CA_LovedOneTurned => lovedOneTurned ?? (lovedOneTurned = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_LovedOneTurned"));
        public static ThoughtDef CA_SuspiciousOfColonists => suspiciousOfColonists ?? (suspiciousOfColonists = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_SuspiciousOfColonists"));
        public static ThoughtDef CA_MercifulKill => mercifulKill ?? (mercifulKill = DefDatabase<ThoughtDef>.GetNamedSilentFail("CA_MercifulKill"));

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
