using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public class HediffGiver_RoleByKind : HediffGiver
    {
        public List<RoleHediffEntry> roleHediffs;

        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            TryApplyRoleHediff(pawn);
        }

        public void TryApplyRoleHediff(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.kindDef == null) return;
            if (roleHediffs == null) return;

            for (int i = 0; i < roleHediffs.Count; i++)
            {
                RoleHediffEntry entry = roleHediffs[i];
                if (entry.pawnKind == null || entry.hediff == null) continue;
                if (pawn.kindDef != entry.pawnKind) continue;
                if (pawn.health.hediffSet.HasHediff(entry.hediff)) continue;

                pawn.health.AddHediff(entry.hediff);
                return;
            }
        }

        public override bool OnHediffAdded(Pawn pawn, Hediff hediff)
        {
            return false;
        }
    }

    public class RoleHediffEntry
    {
        public PawnKindDef pawnKind;
        public HediffDef hediff;
    }
}
