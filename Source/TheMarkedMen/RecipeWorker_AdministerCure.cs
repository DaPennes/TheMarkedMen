using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public class RecipeWorker_AdministerCure : RecipeWorker
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);

            // Find and consume cure syringe from ingredients
            Thing cure = null;
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i].def.defName == "TheMarkedMen_Cure_Syringe")
                {
                    cure = ingredients[i];
                    break;
                }
            }

            if (cure == null)
            {
                Messages.Message("TheMarkedMen_MissingCure".Translate(), billDoer, MessageTypeDefOf.RejectInput);
                return;
            }

            cure.SplitOff(cure.stackCount).Destroy();

            // Check if patient has CrossVirus
            if (!pawn.health.hediffSet.HasHediff(CADefOf.CrossVirus))
            {
                Messages.Message("TheMarkedMen_NotInfected".Translate(), billDoer, MessageTypeDefOf.RejectInput);
                return;
            }

            ApplyCureEffects(pawn);
        }

        private void ApplyCureEffects(Pawn pawn)
        {
            // Remove CrossVirus infection
            if (pawn.health.hediffSet.HasHediff(CADefOf.CrossVirus))
            {
                Hediff virus = pawn.health.hediffSet.GetFirstHediffOfDef(CADefOf.CrossVirus);
                if (virus != null)
                {
                    pawn.health.RemoveHediff(virus);
                }
            }

            // Remove MarkedTurned status
            if (pawn.health.hediffSet.HasHediff(CADefOf.MarkedTurned))
            {
                Hediff turned = pawn.health.hediffSet.GetFirstHediffOfDef(CADefOf.MarkedTurned);
                if (turned != null)
                {
                    pawn.health.RemoveHediff(turned);
                }
            }

            // Restore original faction
            if (pawn.Faction != Faction.OfPlayer && pawn.Faction != null)
            {
                pawn.SetFaction(Faction.OfPlayer);
            }

            // Remove all predator stats/effects
            if (pawn.health.hediffSet.HasHediff(CADefOf.BloodRush))
            {
                pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(CADefOf.BloodRush));
            }

            // Heal all injuries
            if (pawn.health.hediffSet == null) return;

            List<Hediff> allHediffs = new List<Hediff>();
            pawn.health.hediffSet.GetHediffs(ref allHediffs);
            for (int i = 0; i < allHediffs.Count; i++)
            {
                Hediff_Injury injury = allHediffs[i] as Hediff_Injury;
                if (injury != null)
                {
                    injury.Heal(injury.Severity);
                }
            }

            Messages.Message("TheMarkedMen_MessageCured".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
            Log.Message($"Cured {pawn.Name} of Marked Virus.");
        }
    }
}
