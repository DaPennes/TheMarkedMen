using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Gives the blood syringe a right-click float menu option to draw blood
    /// from an infected pawn. The drawing pawn picks up a syringe (if it is
    /// not already carrying one), spends a few seconds drawing, then consumes
    /// one syringe from its inventory and creates an infected blood sample at
    /// the target's position.
    /// </summary>
    public class CompDrawBlood : ThingComp
    {
        private const string BloodSampleDefName = "TheMarkedMen_InfectedBloodSample";

        public CompProperties_DrawBlood Props => (CompProperties_DrawBlood)props;

        private ThingDef BloodSampleDef
        {
            get
            {
                if (Props.bloodSampleDef != null)
                    return Props.bloodSampleDef;

                return DefDatabase<ThingDef>.GetNamedSilentFail(BloodSampleDefName);
            }
        }

        private ThingDef SyringeDef => parent?.def ?? CADefOf.Syringe;

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn))
                yield return option;

            if (!CanUseNow())
                yield break;

            if (!BloodDrawUtility.ResearchCompleted)
            {
                yield return new FloatMenuOption("TheMarkedMen_DrawBloodLocked".Translate(), null);
                yield break;
            }

            if (!BloodDrawUtility.CanActorUse(selPawn))
                yield break;

            AcceptanceReport report = BloodDrawUtility.CanDrawFrom(selPawn);
            string label = "TheMarkedMen_DrawBloodFrom".Translate(selPawn.LabelShort).Resolve();

            if (report.Accepted)
                yield return new FloatMenuOption(label, delegate { BloodDrawUtility.TryStartDraw(selPawn, selPawn, parent); });
            else
                yield return new FloatMenuOption(label + " (" + report.Reason + ")", null);

            yield return new FloatMenuOption("TheMarkedMen_DrawBloodChooseTarget".Translate(), delegate { BloodDrawUtility.BeginTargeting(selPawn, parent); });
        }

        public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
        {
            foreach (FloatMenuOption option in base.CompMultiSelectFloatMenuOptions(selPawns))
                yield return option;

            if (!CanUseNow())
                yield break;

            if (!BloodDrawUtility.ResearchCompleted)
            {
                yield return new FloatMenuOption("TheMarkedMen_DrawBloodLocked".Translate(), null);
                yield break;
            }

            Pawn actor = FirstPawn(selPawns);
            if (BloodDrawUtility.CanActorUse(actor))
                yield return new FloatMenuOption("TheMarkedMen_DrawBloodChooseTarget".Translate(), delegate { BloodDrawUtility.BeginTargeting(actor, parent); });
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

        /// <summary>
        /// Called by <see cref="JobDriver_DrawBlood"/> after the draw time has
        /// elapsed. Revalidates everything, then consumes one syringe from the
        /// drawing pawn's inventory and places the sample.
        /// </summary>
        public void PerformDraw(Pawn actor, Pawn target)
        {
            if (!BloodDrawUtility.ResearchCompleted || !BloodDrawUtility.CanActorUse(actor))
                return;

            AcceptanceReport report = BloodDrawUtility.CanDrawFrom(target);
            if (!report.Accepted)
            {
                Messages.Message(report.Reason.NullOrEmpty()
                        ? "TheMarkedMen_CannotDrawBlood".Translate()
                        : report.Reason,
                    target, MessageTypeDefOf.RejectInput);
                return;
            }

            ThingDef sampleDef = BloodSampleDef;
            if (sampleDef == null)
            {
                Messages.Message("TheMarkedMen_CannotDrawBlood".Translate(), target, MessageTypeDefOf.RejectInput);
                return;
            }

            Thing syringe = BloodDrawUtility.FindInInventory(actor, SyringeDef);
            if (syringe == null)
            {
                Messages.Message("TheMarkedMen_NeedsSyringeInInventory".Translate(actor.LabelShort),
                    actor, MessageTypeDefOf.RejectInput);
                return;
            }

            Map map = target.MapHeld ?? actor.MapHeld;
            IntVec3 cell = target.PositionHeld.IsValid ? target.PositionHeld : actor.PositionHeld;
            Thing sample = ThingMaker.MakeThing(sampleDef);
            sample.stackCount = 1;

            if (map == null || !cell.IsValid || !GenPlace.TryPlaceThing(sample, cell, map, ThingPlaceMode.Near))
            {
                Messages.Message("TheMarkedMen_NoRoomForSample".Translate(), target, MessageTypeDefOf.RejectInput);
                return;
            }

            CureUtility.ConsumeOne(syringe);
            Messages.Message("TheMarkedMen_MessageBloodDrawn".Translate(target.LabelShort), target, MessageTypeDefOf.PositiveEvent);
        }
    }
}
