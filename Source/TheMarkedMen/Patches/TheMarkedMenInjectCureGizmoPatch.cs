using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Adds an "Inject cure into..." gizmo to player-controlled colonists who
    /// are carrying a cure syringe, so the cure can be used without a cure
    /// syringe lying on the ground.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_PawnInjectCureGizmo
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance == null
                || !__instance.IsColonistPlayerControlled
                || __instance.Dead
                || __instance.Downed
                || __instance.InMentalState
                || !__instance.Spawned)
                return;

            Thing cure = CureUtility.FindCureSyringeInInventory(__instance);
            if (cure == null)
                return;

            List<Gizmo> gizmos = new List<Gizmo>();
            if (__result != null)
            {
                foreach (Gizmo gizmo in __result)
                    gizmos.Add(gizmo);
            }

            gizmos.Add(new Command_Action
            {
                defaultLabel = "TheMarkedMen_InjectCureChooseTarget".Translate(),
                defaultDesc = "TheMarkedMen_InjectCureGizmoDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Item/Cure/Cure_Syringe", false),
                action = delegate { CureUtility.BeginTargeting(__instance, cure); }
            });

            __result = gizmos;
        }
    }
}
