using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Adds a "Draw blood from..." gizmo to player-controlled colonists who
    /// are carrying a syringe, so the action can be used without a syringe
    /// lying on the ground.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_PawnDrawBloodGizmo
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance == null
                || !__instance.IsColonistPlayerControlled
                || __instance.Dead
                || __instance.Downed
                || __instance.InMentalState
                || !__instance.Spawned
                || !BloodDrawUtility.ResearchCompleted)
                return;

            Thing syringe = BloodDrawUtility.FindInInventory(__instance, CADefOf.Syringe);
            if (syringe == null)
                return;

            List<Gizmo> gizmos = new List<Gizmo>();
            if (__result != null)
            {
                foreach (Gizmo gizmo in __result)
                    gizmos.Add(gizmo);
            }

            gizmos.Add(new Command_Action
            {
                defaultLabel = "TheMarkedMen_DrawBloodChooseTarget".Translate(),
                defaultDesc = "TheMarkedMen_DrawBloodGizmoDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Item/Syringe/Syringe_craft_1", false),
                action = delegate { BloodDrawUtility.BeginTargeting(__instance, syringe); }
            });

            __result = gizmos;
        }
    }
}
