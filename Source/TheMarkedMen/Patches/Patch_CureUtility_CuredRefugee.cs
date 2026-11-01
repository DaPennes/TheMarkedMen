using HarmonyLib;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Harmony patch to integrate the cure-to-refugee system.
    /// When a pawn is cured, they become a refugee seeking acceptance into the colony.
    /// </summary>
    [HarmonyPatch(typeof(CureUtility), nameof(CureUtility.ApplyCureDirectly))]
    public static class Patch_CureUtility_CuredRefugee
    {
        /// <summary>
        /// Prefix that captures the original faction before cure is applied.
        /// This runs BEFORE the cure logic executes.
        /// </summary>
        static void Prefix(Pawn pawn, out Faction __state)
        {
            // Capture original faction before it changes
            __state = pawn?.Faction;
        }

        /// <summary>
        /// Postfix that triggers refugee status after cure is applied.
        /// This runs AFTER the cure logic completes.
        /// </summary>
        static void Postfix(Pawn pawn, Faction __state)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map == null)
                return;

            // __state contains the ORIGINAL faction before cure was applied
            Faction originalFaction = __state;

            // Only show refugee letter if original faction was NOT player
            if (originalFaction == Faction.OfPlayer)
                return;

            // Convert to null faction (wanderer/independent) instead of player faction
            // This makes them a wandering pawn until player accepts them
            pawn.SetFaction(null);

            // Register the pawn as a refugee seeking to join
            CuredRefugeeManager manager = CuredRefugeeManager.GetForMap(pawn.Map);
            if (manager != null)
            {
                manager.RegisterRefugee(pawn, originalFaction);
            }
        }
    }
}
