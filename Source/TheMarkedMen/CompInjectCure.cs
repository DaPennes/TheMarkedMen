using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Gives cure syringes a right-click float menu option to inject. The
    /// injecting pawn must carry a cure syringe in their inventory (picking
    /// one up from this stack first if needed), and one syringe is consumed
    /// from that inventory after the injection succeeds.
    /// </summary>
    public class CompInjectCure : ThingComp
    {
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn))
                yield return option;

            if (!CanUseNow())
                yield break;

            if (!CureUtility.CanActorUse(selPawn))
                yield break;

            AcceptanceReport report = CureUtility.CanCure(selPawn);
            string label = "TheMarkedMen_InjectCureInto".Translate(selPawn.LabelShort).Resolve();

            if (report.Accepted)
                yield return new FloatMenuOption(label, delegate { CureUtility.TryStartInject(selPawn, selPawn, parent); });
            else
                yield return new FloatMenuOption(label + " (" + report.Reason + ")", null);

            yield return new FloatMenuOption("TheMarkedMen_InjectCureChooseTarget".Translate(),
                delegate { CureUtility.BeginTargeting(selPawn, parent); });
        }

        public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
        {
            foreach (FloatMenuOption option in base.CompMultiSelectFloatMenuOptions(selPawns))
                yield return option;

            if (!CanUseNow())
                yield break;

            Pawn actor = FirstPawn(selPawns);
            if (CureUtility.CanActorUse(actor))
                yield return new FloatMenuOption("TheMarkedMen_InjectCureChooseTarget".Translate(),
                    delegate { CureUtility.BeginTargeting(actor, parent); });
        }

        private static Pawn FirstPawn(IEnumerable<Pawn> pawns)
        {
            if (pawns == null)
                return null;

            foreach (Pawn pawn in pawns)
                return pawn;

            return null;
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
    }
}
