using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// Shared validation, inventory helpers and job setup for drawing blood
    /// from an infected pawn with a syringe.
    /// </summary>
    public static class BloodDrawUtility
    {
        public static bool ResearchCompleted
        {
            get
            {
                ResearchProjectDef research = CADefOf.MarkedVirusResearch;
                return research != null && research.IsFinished;
            }
        }

        public static AcceptanceReport CanDrawFrom(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
                return "TheMarkedMen_OperationInvalidTarget".Translate();

            if (pawn.RaceProps == null || pawn.RaceProps.IsMechanoid || !pawn.RaceProps.IsFlesh)
                return "TheMarkedMen_OperationInvalidTarget".Translate();

            if (!CureUtility.IsInfected(pawn))
                return "TheMarkedMen_OperationNotInfected".Translate();

            return true;
        }

        public static bool CanActorUse(Pawn actor)
        {
            return actor != null
                && !actor.Dead
                && !actor.Downed
                && !actor.Destroyed
                && actor.Spawned
                && actor.Map != null;
        }

        public static int CountInInventory(Pawn pawn, ThingDef def)
        {
            if (pawn?.inventory?.innerContainer == null || def == null)
                return 0;

            int count = 0;
            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (thing.def == def)
                    count += thing.stackCount;
            }

            return count;
        }

        public static Thing FindInInventory(Pawn pawn, ThingDef def)
        {
            if (pawn?.inventory?.innerContainer == null || def == null)
                return null;

            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (thing.def == def)
                    return thing;
            }

            return null;
        }

        public static void BeginTargeting(Pawn actor, Thing syringeSource)
        {
            if (!ResearchCompleted || !CanActorUse(actor) || syringeSource == null)
                return;

            Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetPawns = true,
                canTargetHumans = true,
                canTargetAnimals = true,
                canTargetMechs = false,
                canTargetSubhumans = true,
                canTargetEntities = false,
                canTargetBuildings = false,
                canTargetFires = false,
                canTargetItems = false,
                canTargetLocations = false,
                canTargetPlants = false,
                validator = target =>
                {
                    Pawn pawn = target.Thing as Pawn;
                    return pawn != null && CanDrawFrom(pawn).Accepted;
                }
            }, target =>
            {
                Pawn pawn = target.Thing as Pawn;
                if (pawn != null)
                    TryStartDraw(actor, pawn, syringeSource);
            });
        }

        /// <summary>
        /// Queues the timed blood-draw job. If the actor is not already
        /// carrying a syringe, the job first picks one up from
        /// <paramref name="syringeSource"/>.
        /// </summary>
        public static bool TryStartDraw(Pawn actor, Pawn target, Thing syringeSource)
        {
            if (!ResearchCompleted || !CanActorUse(actor) || syringeSource == null)
                return false;

            AcceptanceReport report = CanDrawFrom(target);
            if (!report.Accepted)
            {
                Messages.Message(report.Reason.NullOrEmpty()
                        ? "TheMarkedMen_CannotDrawBlood".Translate()
                        : report.Reason,
                    target, MessageTypeDefOf.RejectInput);
                return false;
            }

            if (target.MapHeld != actor.Map)
                return false;

            bool carrying = CountInInventory(actor, syringeSource.def) > 0;
            if (!carrying)
            {
                if (syringeSource.DestroyedOrNull()
                    || !syringeSource.Spawned
                    || syringeSource.stackCount <= 0
                    || syringeSource.IsForbidden(Faction.OfPlayer)
                    || !actor.CanReserve(syringeSource, 1, 1))
                {
                    Messages.Message("TheMarkedMen_CannotDrawBlood".Translate(), actor, MessageTypeDefOf.RejectInput);
                    return false;
                }
            }

            JobDef jobDef = CADefOf.DrawBloodJob;
            if (jobDef == null)
            {
                Log.Error("[The Marked Men] Missing JobDef TheMarkedMen_DrawBlood; cannot draw blood.");
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, target, syringeSource);
            job.count = 1;
            actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            return true;
        }
    }
}
