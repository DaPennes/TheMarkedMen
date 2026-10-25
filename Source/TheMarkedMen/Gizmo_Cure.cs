using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using TheMarkedMen;

namespace TheMarkedMen
{
    public class Gizmo_UseCure : Gizmo
    {
        private Pawn owner;

        public Gizmo_UseCure(Pawn owner)
        {
            this.owner = owner;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float width, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, width, 75f);
            Texture2D cureTexture = ContentFinder<Texture2D>.Get("Item/Cure/Cure_Syringe", false);

            if (cureTexture != null)
            {
                GUI.DrawTexture(rect, cureTexture);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                ApplyCureEffects(owner);
            }

            return new GizmoResult(GizmoState.Clear);
        }

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(75f, maxWidth);
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
            foreach (Hediff hediff in allHediffs)
            {
                if (hediff.def == CADefOf.CrossedRash || hediff.def.defName.Contains("Marked"))
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }

            // Heal all injuries
            List<Hediff> allHediffs2 = new List<Hediff>();
            pawn.health.hediffSet.GetHediffs(ref allHediffs2);
            for (int i = 0; i < allHediffs2.Count; i++)
            {
                Hediff_Injury injury = allHediffs2[i] as Hediff_Injury;
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
