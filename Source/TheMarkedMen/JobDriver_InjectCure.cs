using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    
    
    
    
    
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
            if (!CureUtility.TryBeginCure(TargetPawn, out reason))
            {
                Messages.Message(reason.NullOrEmpty() ? "TheMarkedMen_CannotInject".Translate() : reason,
                    TargetPawn, MessageTypeDefOf.RejectInput);
                return;
            }

            CureUtility.ConsumeOne(cure);
        }
    }
}
