using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Gives cure syringes a right-click float menu option to inject.
    /// Injecting consumes one syringe from the stack.
    /// </summary>
    public class CompInjectCure : ThingComp
    {
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
