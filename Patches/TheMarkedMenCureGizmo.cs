using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    [HarmonyPatch(typeof(Pawn_GizmoRenderer), "GizmosOnScreen")]
    public static class Pawn_GizmoRenderer_GizmosOnScreen_Patch
    {
        private static void Postfix(ref IEnumerable<Gizmo> __result, PawnRenderer __instance)
        {
            if (__instance.pawn?.health?.hediffSet == null)
                return;

            // Only add gizmo if infected
            if (!__instance.pawn.health.hediffSet.HasHediff(CADefOf.CrossVirus))
                return;

            // Create the cure gizmo
            Gizmo_UseCure cureGizmo = new Gizmo_UseCure(__instance.pawn);

            // Add to the list
            var gizmoList = __result?.ToList() ?? new System.Collections.Generic.List<Gizmo>();
            gizmoList.Add(cureGizmo);

            __result = gizmoList;
        }
    }
}