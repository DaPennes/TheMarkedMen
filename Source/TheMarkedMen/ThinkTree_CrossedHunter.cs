using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    /// <summary>
    /// Dedicated ThinkTree for infected hunters.
    /// Provides priority-based job issuance for tactical hunting, cover usage, and infection-focused AI.
    /// </summary>
    public class ThinkTree_CrossedHunter : ThinkNode
    {
        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned)
                return ThinkResult.NoJob;

            // If in a Lord, defer to LordJob
            Lord lord = pawn.GetLord();
            if (lord != null && lord.faction == pawn.Faction)
            {
                return ThinkResult.NoJob;
            }

            // Find best target
            Pawn targetPawn = CrossedTacticalAI.FindBestNonInfectedPawnTarget(pawn);
            if (targetPawn != null)
            {
                Job job = new Job(JobDefOf.AttackMelee, targetPawn) { checkOverrideOnExpire = true };
                return new ThinkResult(job, this);
            }

            // Seek cover if injured
            if (ShouldSeekCover(pawn))
            {
                IntVec3 coverPos = CoverUtility.FindNearestCover(pawn, null);
                if (coverPos.IsValid)
                {
                    Job job = new Job(JobDefOf.Goto, coverPos) { checkOverrideOnExpire = true };
                    return new ThinkResult(job, this);
                }
            }

            // No action--let other thinks run
            return ThinkResult.NoJob;
        }

        private bool ShouldSeekCover(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return false;

            var healthTracker = pawn.health;
            if (healthTracker == null || healthTracker.hediffSet == null)
                return false;

            List<Hediff_Injury> injuries = new List<Hediff_Injury>();
            healthTracker.hediffSet.GetHediffs<Hediff_Injury>(ref injuries);
            return injuries.Count > 0;
        }
    }
}
