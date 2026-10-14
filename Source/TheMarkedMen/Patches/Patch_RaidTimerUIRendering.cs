using HarmonyLib;
using RimWorld;
using TheMarkedMen.UI;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Patches for raid timer UI rendering.
    /// Injects the overlay drawing into the game's OnGUI loop.
    /// </summary>
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
    public static class Patch_RaidTimerUIRendering
    {
        public static void Postfix()
        {
            // Draw raid timer UI after all other UI elements
            if (TheMarkedMenMod.Settings?.showRaidTimerUI ?? false)
            {
                RaidTimerUIOverlay.DrawRaidTimerUI();
            }
        }
    }
}
