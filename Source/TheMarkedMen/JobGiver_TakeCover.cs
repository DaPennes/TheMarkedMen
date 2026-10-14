using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// JobGiver for infected to take cover when under fire.
    /// Seeks nearby cover that blocks LOS to threats.
    /// </summary>
    public class JobGiver_TakeCover : ThinkNode_JobGiver
    {
        private const int CoverSearchIntervalTicks = 150;
        private const float CoverSearchRadius = 30f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return null;

            // Only seek cover if actually injured or under fire
            if (!IsUnderThreat(pawn))
                return null;

            if (!pawn.IsHashIntervalTick(CoverSearchIntervalTicks))
                return null;

            IntVec3 coverPos = CoverUtility.FindNearestCover(pawn, CoverSearchRadius);
            if (coverPos.IsValid)
            {
                return new Job(JobDefOf.Goto, coverPos) { checkOverrideOnExpire = true };
            }

            return null;
        }

        private bool IsUnderThreat(Pawn pawn)
        {
            if (pawn.health == null || pawn.health.hediffSet == null)
                return false;

            // Check for recent damage (injury hediffs present)
            List<Hediff_Injury> injuries = new List<Hediff_Injury>();
            pawn.health.hediffSet.GetHediffs(ref injuries, null);
            return injuries.Count > 0;
        }
    }
}

