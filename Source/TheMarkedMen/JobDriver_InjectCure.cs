using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// Picks up a cure syringe (unless the pawn already carries one), walks to
    /// the infected target and injects it. The cure is taken from the pawn's
    /// inventory and consumed only after the injection succeeds.
    /// </summary>
    public class JobDriver_InjectCure : JobDriver
    {
        private Pawn TargetPawn => job.GetTarget(TargetIndex.A).Thing as Pawn;

        private bool HasCureInInventory()
        {
            return CureUtility.FindCureSyringeInInventory(pawn) != null;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (HasCureInInventory())
                return true;

            return pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, 1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.AddFailCondition(() => TargetPawn == null
                || TargetPawn.Dead
                || TargetPawn.Destroyed
                || !TargetPawn.Spawned);

            this.AddFailCondition(() => !CureUtility.CanCure(TargetPawn).Accepted);

            if (!HasCureInInventory())
            {
                Toil goToCure = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
                goToCure.FailOnDespawnedNullOrForbidden(TargetIndex.B);
                yield return goToCure;

                Toil takeCure = Toils_Haul.TakeToInventory(TargetIndex.B, 1);
                takeCure.FailOnDespawnedNullOrForbidden(TargetIndex.B);
                yield return takeCure;
            }

            // The injecting pawn may be the infected target itself, in which
            // case there is nowhere to walk.
            if (pawn != TargetPawn)
            {
                Toil goToTarget = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
                goToTarget.FailOnDespawnedOrNull(TargetIndex.A);
                goToTarget.FailOn(() => !HasCureInInventory());
                yield return goToTarget;
            }

            Toil inject = Toils_General.Do(Inject);
            inject.FailOn(() => !HasCureInInventory());
            yield return inject;
        }

        private void Inject()
        {
            Thing cure = CureUtility.FindCureSyringeInInventory(pawn);
            if (cure == null)
            {
                Messages.Message("TheMarkedMen_NeedsCureInInventory".Translate(pawn.LabelShort),
                    pawn, MessageTypeDefOf.RejectInput);
                return;
            }

            string reason;
            if (!CureUtility.TryCure(TargetPawn, out reason))
            {
                Messages.Message(reason.NullOrEmpty() ? "TheMarkedMen_CannotInject".Translate() : reason,
                    TargetPawn, MessageTypeDefOf.RejectInput);
                return;
            }

            CureUtility.ConsumeOne(cure);
        }
    }
}
