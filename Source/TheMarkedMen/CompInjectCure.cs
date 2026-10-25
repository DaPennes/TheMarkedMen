using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public class CompInjectCure : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
                yield return g;

            if (parent.IsForbidden(Faction.OfPlayer))
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "Inject Cure",
                defaultDesc = "Inject the Marked Virus cure into a selected target pawn.",
                icon = ContentFinder<Texture2D>.Get("Item/Cure/Cure_Syringe", false),
                action = delegate
                {
                    List<Pawn> targets = Find.Maps.SelectMany(m => m.mapPawns.AllPawns)
                        .Where(p => p.RaceProps.Humanlike && !p.Dead)
                        .ToList();

                    if (targets.Count == 0)
                    {
                        Messages.Message("No valid targets found.", MessageTypeDefOf.RejectInput);
                        return;
                    }

                    Find.Targeter.BeginTargeting(new TargetingParameters
                    {
                        canTargetPawns = true,
                        canTargetLocations = false,
                        validator = p => p.Thing is Pawn pawn && pawn.RaceProps.Humanlike && !pawn.Dead
                    }, target =>
                    {
                        Pawn pawn = target.Thing as Pawn;
                        if (pawn != null)
                        {
                            ApplyCure(pawn);
                        }
                    });
                }
            };
        }

        private void ApplyCure(Pawn pawn)
        {
            // Consume 1 cure syringe from stack
            if (parent.stackCount > 1)
            {
                parent.SplitOff(1).Destroy();
            }
            else
            {
                parent.Destroy();
            }

            // Remove all Marked Men hediff
            List<Hediff> toRemove = new List<Hediff>();
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h.def == CADefOf.CrossVirus ||
                    h.def == CADefOf.MarkedTurned ||
                    h.def == CADefOf.BloodRush ||
                    h.def == CADefOf.CrossedRash ||
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
        }
    }
}
