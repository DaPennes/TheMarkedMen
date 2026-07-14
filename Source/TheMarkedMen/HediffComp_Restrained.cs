using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
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
        private const int JobCheckInterval = 15;
        private int nextJobCheck;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || Pawn.Dead || !Pawn.Spawned || Pawn.Downed)
                return;

            if (!Pawn.IsPrisonerOfColony)
                return;

            int tick = Find.TickManager.TicksGame;
            if (tick < nextJobCheck)
                return;

            nextJobCheck = tick + JobCheckInterval;

            Building_Bed bed = Pawn.CurrentBed();
            if (bed == null || !bed.Spawned)
            {
                bed = FindAssignedBed(Pawn);
                if (bed == null || !bed.Spawned)
                {
                    Pawn.health.RemoveHediff(parent);
                    return;
                }
                parent.Severity = 0.01f;
            }
            else
            {
                parent.Severity = 1f;
            }

            Job curJob = Pawn.jobs?.curJob;
            if (curJob != null && curJob.def == JobDefOf.LayDown && Pawn.CurrentBed() != null)
                return;

            Pawn.jobs?.StopAll();
            Pawn.pather?.StopDead();

            Job layDown = JobMaker.MakeJob(JobDefOf.LayDown, bed);
            layDown.expiryInterval = -1;
            Pawn.jobs?.StartJob(layDown, JobCondition.InterruptForced);
        }

        private static Building_Bed FindAssignedBed(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Map == null)
                return null;

            List<Building> allBeds = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBeds.Count; i++)
            {
                Building_Bed bed = allBeds[i] as Building_Bed;
                if (bed == null)
                    continue;

                CompAssignableToPawn assignable = bed.GetComp<CompAssignableToPawn>();
                if (assignable != null && assignable.AssignedPawns.Contains(pawn))
                    return bed;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(ThingWithComps), "GetGizmos")]
    public static class Patch_RestrainBedGizmo
    {
        public static void Postfix(ThingWithComps __instance, ref IEnumerable<Gizmo> __result)
        {
            Building_Bed bed = __instance as Building_Bed;
            if (bed == null)
                return;

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.prisonerInfectionEnabled || !settings.prisonerRestraintEnabled)
                return;

            Pawn pawn = null;
            CompAssignableToPawn assignable = bed.GetComp<CompAssignableToPawn>();
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
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Restrain", false),
                    action = delegate
                    {
                        ToggleRestrain(pawn, true, bed);
                    }
                });
            }
            else
            {
                gizmos.Add(new Command_Action
                {
                    defaultLabel = "CA_ReleasePrisoner".Translate(),
                    defaultDesc = "CA_ReleasePrisonerDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Release", false),
                    action = delegate
                    {
                        ToggleRestrain(pawn, false, bed);
                    }
                });
            }

            __result = gizmos;
        }

        private static void ToggleRestrain(Pawn pawn, bool restrain, Building_Bed bed)
        {
            if (pawn == null || pawn.Dead)
                return;

            if (restrain)
            {
                pawn.health.AddHediff(CADefOf.CA_Restrained);

                if (pawn.Spawned)
                {
                    Job layDown = JobMaker.MakeJob(JobDefOf.LayDown, bed);
                    layDown.expiryInterval = -1;
                    pawn.jobs?.StartJob(layDown, JobCondition.InterruptForced);
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
