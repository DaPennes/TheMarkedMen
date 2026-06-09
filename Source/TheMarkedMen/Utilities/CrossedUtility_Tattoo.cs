using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static partial class CrossedUtility
    {
        public static bool ShouldShowCrossedRash(Pawn pawn)
        {
            return HasMarkedVirusHediff(pawn) || IsCrossedFactionPawn(pawn) || HasPersistentCrossedRashTattoo(pawn);
        }

        public static TattooDef GetCurrentCrossedFaceTattoo(Pawn pawn)
        {
            TattooDef finalTattoo = CADefOf.CrossedFaceTattoo;
            if (pawn == null || IsCrossedFactionPawn(pawn)) return finalTattoo;
            HediffDef virus = CADefOf.CrossVirus;
            Hediff hediff = virus == null ? null : pawn.health?.hediffSet?.GetFirstHediffOfDef(virus);
            if (hediff == null) return finalTattoo;
            return CrossedFaceTattooForSeverity(hediff.Severity) ?? finalTattoo;
        }

        private static TattooDef CrossedFaceTattooForSeverity(float severity)
        {
            if (severity >= CrossVirusFinalStageSeverity) return CADefOf.CrossedFaceTattoo;
            if (severity >= CrossVirusStage4Severity) return CADefOf.CrossedFaceTattooStage4 ?? CADefOf.CrossedFaceTattoo;
            if (severity >= CrossVirusStage3Severity) return CADefOf.CrossedFaceTattooStage3 ?? CADefOf.CrossedFaceTattoo;
            if (severity >= CrossVirusStage2Severity) return CADefOf.CrossedFaceTattooStage2 ?? CADefOf.CrossedFaceTattoo;
            return CADefOf.CrossedFaceTattooStage1 ?? CADefOf.CrossedFaceTattoo;
        }

        public static void ApplyInfectedTattoo(Pawn pawn)
        {
            TattooDef tattoo = GetCurrentCrossedFaceTattoo(pawn);
            if (pawn?.style == null || tattoo == null || !ShouldShowCrossedRash(pawn)) return;
            if (pawn.style.nextFaceTattooDef != tattoo)
                pawn.style.nextFaceTattooDef = tattoo;
            if (pawn.style.FaceTattoo != tattoo)
            {
                pawn.style.FaceTattoo = tattoo;
                pawn.style.Notify_StyleItemChanged();
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }
    }
}
