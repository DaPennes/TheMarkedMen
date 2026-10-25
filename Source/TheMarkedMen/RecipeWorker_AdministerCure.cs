using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Health -> Operations bill that injects a cure syringe into a patient.
    /// The cure ingredient is consumed by the medical bill itself before this
    /// worker runs, so no ingredient handling is done here.
    /// </summary>
    public class RecipeWorker_AdministerCure : Recipe_Surgery
    {
        public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord part = null)
        {
            AcceptanceReport baseReport = base.AvailableReport(thing, part);
            if (!baseReport.Accepted)
                return baseReport;

            return CureUtility.CanCure(thing as Pawn);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);

            if (ingredients == null || !ingredients.Any(CureUtility.IsCureSyringe))
            {
                Log.Warning("[TheMarkedMen] Administer cure operation finished without a cure syringe ingredient; curing anyway.");
            }

            string reason;
            if (!CureUtility.TryCure(pawn, out reason))
            {
                Pawn target = billDoer ?? pawn;
                Messages.Message(reason.NullOrEmpty() ? "TheMarkedMen_CannotInject".Translate() : reason,
                    target, MessageTypeDefOf.RejectInput);
            }
        }
    }
}
