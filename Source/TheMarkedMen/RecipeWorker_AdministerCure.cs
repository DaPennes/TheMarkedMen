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

            cure.SplitOff(1).Destroy();

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
            // Remove ALL Marked Men hediff
            List<Hediff> toRemove = new List<Hediff>();
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h.def == CADefOf.CrossVirus ||
                    h.def == CADefOf.MarkedTurned ||
                    h.def == CADefOf.BloodRush ||
                    h.def == CADefOf.CrossedRash ||
                    h.def == CADefOf.CrossedStrength ||
                    h.def == CADefOf.CrossedRampage ||
                    h.def == CADefOf.ScoutAdrenaline ||
                    h.def == CADefOf.HunterFocus ||
                    h.def == CADefOf.ShooterRush ||
                    h.def == CADefOf.RaiderFury ||
                    h.def == CADefOf.SoldierDiscipline ||
                    h.def == CADefOf.FireHunger ||
                    h.def == CADefOf.WarlordPresence ||
                    h.def == CADefOf.WarlordTier ||
                    h.def == CADefOf.AlphaTier ||
                    h.def == CADefOf.MarkedTier ||
                    h.def == CADefOf.CommandAura ||
                    h.def == CADefOf.PsychicAura ||
                    h.def.defName.Contains("Marked") ||
                    h.def.defName.Contains("Crossed"))
                {
                    toRemove.Add(h);
                }
            }
            foreach (Hediff h in toRemove)
                pawn.health.RemoveHediff(h);

            // Restore to player faction
            if (pawn.Faction != Faction.OfPlayer && pawn.Faction != null)
            {
                pawn.SetFaction(Faction.OfPlayer);
            }

            // Add Victim Remorse hediff
            if (CADefOf.VictimRemorse != null)
            {
                pawn.health.AddHediff(CADefOf.VictimRemorse);
            }

            // Heal all injuries
            List<Hediff> injuries = new List<Hediff>();
            pawn.health.hediffSet.GetHediffs(ref injuries);
            for (int i = 0; i < injuries.Count; i++)
            {
                Hediff_Injury injury = injuries[i] as Hediff_Injury;
                if (injury != null)
                    injury.Heal(injury.Severity);
            }

            Messages.Message("TheMarkedMen_MessageCured".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
            Log.Message($"Cured {pawn.Name} of Marked Virus.");
        }
    }
}
