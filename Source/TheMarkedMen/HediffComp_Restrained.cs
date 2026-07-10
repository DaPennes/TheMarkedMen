using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace TheMarkedMen
{
    public class HediffCompProperties_Restrained : HediffCompProperties
    {
        public HediffCompProperties_Restrained()
        {
            compClass = typeof(HediffComp_Restrained);
        }
    }

    public class HediffComp_Restrained : HediffComp
    {
        private const int JobCheckInterval = 30;
        private int nextJobCheck;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || Pawn.Dead || !Pawn.Spawned)
                return;

            if (Pawn.IsPrisonerOfColony && Pawn.Spawned)
            {
                Building_Bed bed = Pawn.CurrentBed();
                if (bed == null || !bed.Spawned)
                {
                    Pawn.health.RemoveHediff(parent);
                    return;
                }

                if (Pawn.Downed)
                    return;

                int tick = Find.TickManager.TicksGame;
                if (tick >= nextJobCheck)
                {
                    nextJobCheck = tick + JobCheckInterval;
                    Pawn.jobs?.StopAll();
                    Pawn.pather?.StopDead();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Building_Bed), "GetGizmos")]
    public static class Patch_RestrainBedGizmo
    {
        public static void Postfix(Building_Bed __instance, ref IEnumerable<Gizmo> __result)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.prisonerInfectionEnabled || !settings.prisonerRestraintEnabled)
                return;

            Pawn pawn = null;
            CompAssignableToPawn assignable = __instance.GetComp<CompAssignableToPawn>();
            if (assignable != null)
            {
                foreach (Pawn assigned in assignable.AssignedPawns)
                {
                    if (assigned != null && assigned.IsPrisonerOfColony && CrossedUtility.IsCrossedPawn(assigned) && !assigned.Dead)
                    {
                        pawn = assigned;
                        break;
                    }
                }
            }

            if (pawn == null)
                return;

            bool isRestrained = pawn.health?.hediffSet?.HasHediff(CADefOf.CA_Restrained) ?? false;

            List<Gizmo> gizmos = new List<Gizmo>();
            foreach (Gizmo gizmo in __result)
            {
                gizmos.Add(gizmo);
            }

            if (!isRestrained)
            {
                gizmos.Add(new Command_Action
                {
                    defaultLabel = "CA_RestrainPrisoner".Translate(),
                    defaultDesc = "CA_RestrainPrisonerDesc".Translate(),
                    icon = TexCommand.DesirePower,
                    action = delegate
                    {
                        ToggleRestrain(pawn, true);
                    },
                    hotKey = KeyBindingDefOf.Misc4
                });
            }
            else
            {
                gizmos.Add(new Command_Action
                {
                    defaultLabel = "CA_ReleasePrisoner".Translate(),
                    defaultDesc = "CA_ReleasePrisonerDesc".Translate(),
                    icon = TexCommand.DesirePower,
                    action = delegate
                    {
                        ToggleRestrain(pawn, false);
                    },
                    hotKey = KeyBindingDefOf.Misc4
                });
            }

            __result = gizmos;
        }

        private static void ToggleRestrain(Pawn pawn, bool restrain)
        {
            if (pawn == null || pawn.Dead)
                return;

            if (restrain)
            {
                pawn.health.AddHediff(CADefOf.CA_Restrained);
                if (pawn.Spawned)
                {
                    pawn.jobs.StopAll();
                    pawn.pather.StopDead();
                }
                Messages.Message("CA_PrisonerRestrained".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Hediff hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CA_Restrained);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
                Messages.Message("CA_PrisonerReleased".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}
