using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// Picks up a syringe (unless the pawn already carries one), walks to the
    /// infected target and spends a few seconds drawing blood before the
    /// sample is produced and one inventory syringe is consumed.
    /// </summary>
    public class JobDriver_DrawBlood : JobDriver
    {
        /// <summary>Default draw time: three seconds at 60 ticks per second.</summary>
        public const int DefaultDrawDurationTicks = 180;

        private Pawn TargetPawn => job.GetTarget(TargetIndex.A).Thing as Pawn;

        private Thing SyringeThing => job.GetTarget(TargetIndex.B).Thing;

        private ThingDef SyringeDef => SyringeThing?.def ?? CADefOf.Syringe;

        private CompDrawBlood SyringeComp
        {
            get
            {
                // Prefer the syringe actually carried by the pawn; the job's
                // source thing may already be gone if the stack was merged or
                // consumed by another pawn.
                Thing syringe = BloodDrawUtility.FindInInventory(pawn, SyringeDef) ?? SyringeThing;
                return (syringe as ThingWithComps)?.GetComp<CompDrawBlood>();
            }
        }

        private bool HasSyringeInInventory()
        {
            return BloodDrawUtility.CountInInventory(pawn, SyringeDef) > 0;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (HasSyringeInInventory())
                return true;

            return pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, 1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.AddFailCondition(() => TargetPawn == null
                || TargetPawn.Dead
                || TargetPawn.Destroyed
                || !TargetPawn.Spawned);

            this.AddFailCondition(() => !BloodDrawUtility.CanDrawFrom(TargetPawn).Accepted);

            if (!HasSyringeInInventory())
            {
                Toil goToSyringe = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
                goToSyringe.FailOnDespawnedNullOrForbidden(TargetIndex.B);
                yield return goToSyringe;

                Toil takeSyringe = Toils_Haul.TakeToInventory(TargetIndex.B, 1);
                takeSyringe.FailOnDespawnedNullOrForbidden(TargetIndex.B);
                yield return takeSyringe;
            }

            // The drawing pawn may be the infected target itself, in which
            // case there is nowhere to walk.
            if (pawn != TargetPawn)
            {
                Toil goToTarget = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
                goToTarget.FailOnDespawnedOrNull(TargetIndex.A);
                goToTarget.FailOn(() => !HasSyringeInInventory());
                yield return goToTarget;
            }

            CompDrawBlood comp = SyringeComp;
            int duration = comp?.Props?.drawDurationTicks ?? DefaultDrawDurationTicks;
            if (duration <= 0)
                duration = DefaultDrawDurationTicks;

            Toil draw = Toils_General.Wait(duration, pawn == TargetPawn ? TargetIndex.None : TargetIndex.A);
            draw.FailOn(() => !HasSyringeInInventory());
            draw.WithProgressBar(TargetIndex.A, () => 1f - (float)ticksLeftThisToil / duration);
            yield return draw;

            yield return Toils_General.Do(FinishDraw);
        }

        private void FinishDraw()
        {
            CompDrawBlood comp = SyringeComp;
            if (comp == null)
                return;

            comp.PerformDraw(pawn, TargetPawn);
        }
    }
}
