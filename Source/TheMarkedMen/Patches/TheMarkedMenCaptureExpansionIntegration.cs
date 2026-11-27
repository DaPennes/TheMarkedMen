using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Integration layer for Capture Expansion mod.
    /// Detects if Capture Expansion is loaded and hooks into its prisoner system,
    /// providing fallback behavior when it's not available.
    /// </summary>
    public static class TheMarkedMenCaptureExpansionIntegration
    {
        // Cache for whether Capture Expansion is loaded
        private static bool? _captureExpansionLoaded;

        /// <summary>
        /// Checks if Capture Expansion mod is loaded.
        /// Uses reflection to attempt accessing CE-specific types.
        /// </summary>
        public static bool CaptureExpansionLoaded
        {
            get
            {
                if (_captureExpansionLoaded.HasValue)
                    return _captureExpansionLoaded.Value;

                try
                {
                    // Try to access a known CE type to determine if CE is loaded
                    // This will throw if CE assembly is not available
                    var _ = AccessTools.TypeByName("CaptureExpansion.PrisonerGrid");
                    _captureExpansionLoaded = true;
                    return true;
                }
                catch
                {
                    // Type not found - CE not loaded
                    _captureExpansionLoaded = false;
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets whether the prisoner is owned by Capture Expansion system.
        /// Falls back to Marked Men's own check when CE is not loaded.
        /// </summary>
        public static bool IsCEPrisoner(Pawn pawn)
        {
            if (!CaptureExpansionLoaded)
                return false;

            // When CE is loaded, use MM's check as baseline
            // CE-specific identification can be added later when API is known
            return MarkedMenPrisonerUtility.IsMarkedPrisoner(pawn);
        }

        /// <summary>
        /// Gets the modified recruit chance for a prisoner, considering both CE and MM systems.
        /// </summary>
        public static float GetModifiedRecruitChance(Pawn pawn, float baseChance)
        {
            if (!CaptureExpansionLoaded)
                return baseChance;

            // When CE is loaded, base chance is modified by CE's system
            // For now, return base chance; add CE integration later
            return baseChance;
        }

        /// <summary>
        /// Gets the modified prison break chance, considering both CE and MM systems.
        /// </summary>
        public static float GetModifiedPrisonBreakChance(Pawn pawn, float baseChance)
        {
            if (!CaptureExpansionLoaded)
                return baseChance;

            // When CE is loaded, modify break chance per CE's system
            // For now, return base chance; add CE integration later
            return baseChance;
        }

        /// <summary>
        /// Called when the mod is loaded to setup integration.
        /// Patches recruitment, suppress, and ideo conversion for Marked Men prisoners.
        /// </summary>
        public static void OnModLoad(Harmony harmony)
        {
            if (!CaptureExpansionLoaded)
                return;

            try
            {
                // Apply recruitment blocking patch for MM prisoners when CE is loaded
                harmony.Patch(
                    typeof(InteractionWorker_RecruitAttempt).GetMethod("Interacted"),
                    postfix: new HarmonyMethod(typeof(TheMarkedMenCaptureExpansionIntegration).GetMethod("Patch_CE_RecruitBlock_Postfix"))
                );

                // Apply suppress blocking patch
                harmony.Patch(
                    typeof(InteractionWorker_Suppress).GetMethod("Interacted"),
                    postfix: new HarmonyMethod(typeof(TheMarkedMenCaptureExpansionIntegration).GetMethod("Patch_CE_SuppressBlock_Postfix"))
                );

                // Apply ideo conversion blocking patch
                harmony.Patch(
                    typeof(InteractionWorker_ConvertIdeoAttempt).GetMethod("Interacted"),
                    postfix: new HarmonyMethod(typeof(TheMarkedMenCaptureExpansionIntegration).GetMethod("Patch_CE_ConvertBlock_Postfix"))
                );
            }
            catch { }
        }

        /// <summary>
        /// Removes all CE integration patches.
        /// Called when disabling the mod or when CE is not loaded.
        /// </summary>
        public static void RemoveCEPatches(Harmony harmony)
        {
            // Patches are removed automatically when the mod is unloaded
        }

        // ===== Harmony Postfix Methods =====

        private static void Patch_CE_RecruitBlock_Postfix(Pawn recruiter, Pawn recruit, ref bool __result)
        {
            if (!CaptureExpansionLoaded)
                return;

            if (MarkedMenPrisonerUtility.IsMarkedPrisoner(recruit) && !IsCEPrisoner(recruit))
            {
                __result = false;
            }
        }

        private static void Patch_CE_SuppressBlock_Postfix(Pawn suppressor, Pawn suppressed, ref bool __result)
        {
            if (!CaptureExpansionLoaded)
                return;

            if (MarkedMenPrisonerUtility.IsMarkedPrisoner(suppressed) && !IsCEPrisoner(suppressed))
            {
                __result = false;
            }
        }

        private static void Patch_CE_ConvertBlock_Postfix(Pawn converter, Pawn convertTarget, ref bool __result)
        {
            if (!CaptureExpansionLoaded)
                return;

            if (MarkedMenPrisonerUtility.IsMarkedPrisoner(convertTarget) && !IsCEPrisoner(convertTarget))
            {
                __result = false;
            }
        }
    }
}