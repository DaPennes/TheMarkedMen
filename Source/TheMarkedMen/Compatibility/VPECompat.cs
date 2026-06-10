using System.Linq;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public static class VPECompat
    {
        private const string VPEPackageId = "vanillaexpanded.vpsycastse";

        public static bool IsActive { get; private set; }

        public static void Initialize()
        {
            IsActive = ModsConfig.ActiveModsInLoadOrder
                .Any(m => m.PackageIdPlayerFacing?.ToLowerInvariant() == VPEPackageId);
            if (IsActive)
                Log.Message("[The Marked Men] Vanilla Psycasts Expanded detected — Alpha Psychic will receive psylink compatibility");
        }

        public static void TryApplyPsylink(Pawn pawn)
        {
            if (!IsActive || pawn?.health?.hediffSet == null) return;
            HediffDef psylinkDef = HediffDefOf.PsychicAmplifier;
            if (psylinkDef == null) return;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(psylinkDef) != null) return;

            BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
            if (brain == null) return;

            Hediff_Psylink psylink = HediffMaker.MakeHediff(psylinkDef, pawn, brain) as Hediff_Psylink;
            if (psylink == null) return;

            pawn.health.AddHediff(psylink);
        }
    }
}
