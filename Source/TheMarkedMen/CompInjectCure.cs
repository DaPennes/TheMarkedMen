using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Gives cure syringes a "Inject cure" gizmo and a right-click float menu
    /// option. Injecting consumes one syringe from the stack.
    /// </summary>
    public class CompInjectCure : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            if (!CanUseNow())
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "TheMarkedMen_InjectCureGizmoLabel".Translate(),
                defaultDesc = "TheMarkedMen_InjectCureGizmoDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Item/Cure/Cure_Syringe", false),
                action = BeginTargeting
            };
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn))
                yield return option;

            if (!CanUseNow())
                yield break;

            if (selPawn != null && !selPawn.Dead)
            {
                AcceptanceReport report = CureUtility.CanCure(selPawn);
                string label = "TheMarkedMen_InjectCureInto".Translate(selPawn.LabelShort).Resolve();

                if (report.Accepted)
                    yield return new FloatMenuOption(label, delegate { InjectInto(selPawn); });
                else
                    yield return new FloatMenuOption(label + " (" + report.Reason + ")", null);
            }

            yield return new FloatMenuOption("TheMarkedMen_InjectCureChooseTarget".Translate(), BeginTargeting);
        }

        public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
        {
            foreach (FloatMenuOption option in base.CompMultiSelectFloatMenuOptions(selPawns))
                yield return option;

            if (!CanUseNow())
                yield break;

            yield return new FloatMenuOption("TheMarkedMen_InjectCureChooseTarget".Translate(), BeginTargeting);
        }

        private bool CanUseNow()
        {
            return parent != null
                && !parent.DestroyedOrNull()
                && parent.Spawned
                && parent.Map != null
                && parent.stackCount > 0
                && !parent.IsForbidden(Faction.OfPlayer);
        }

        private void BeginTargeting()
        {
            if (!CanUseNow())
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
                    return pawn != null && CureUtility.CanCure(pawn).Accepted;
                }
            }, target =>
            {
                Pawn pawn = target.Thing as Pawn;
                if (pawn != null)
                    InjectInto(pawn);
            });
        }

        private void InjectInto(Pawn target)
        {
            if (!CanUseNow())
                return;

            string reason;
            if (!CureUtility.TryCure(target, out reason))
            {
                Messages.Message(reason.NullOrEmpty() ? "TheMarkedMen_CannotInject".Translate() : reason,
                    target, MessageTypeDefOf.RejectInput);
                return;
            }

            CureUtility.ConsumeOne(parent);
        }
    }
}
