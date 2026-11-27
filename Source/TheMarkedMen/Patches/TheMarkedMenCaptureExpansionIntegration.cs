using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    
    
    
    
    
    public static class TheMarkedMenCaptureExpansionIntegration
    {
        
        private static bool? _captureExpansionLoaded;

        
        
        
        
        public static bool CaptureExpansionLoaded
        {
            get
            {
                if (_captureExpansionLoaded.HasValue)
                    return _captureExpansionLoaded.Value;

                try
                {
                    
                    
                    var _ = AccessTools.TypeByName("CaptureExpansion.PrisonerGrid");
                    _captureExpansionLoaded = true;
                    return true;
                }
                catch
                {
                    
                    _captureExpansionLoaded = false;
                    return false;
                }
            }
        }

        
        
        
        
        public static bool IsCEPrisoner(Pawn pawn)
        {
            if (!CaptureExpansionLoaded)
                return false;

            
            
            return MarkedMenPrisonerUtility.IsMarkedPrisoner(pawn);
        }

        
        
        
        public static float GetModifiedRecruitChance(Pawn pawn, float baseChance)
        {
            if (!CaptureExpansionLoaded)
                return baseChance;

            
            
            return baseChance;
        }

        
        
        
        public static float GetModifiedPrisonBreakChance(Pawn pawn, float baseChance)
        {
            if (!CaptureExpansionLoaded)
                return baseChance;

            
            
            return baseChance;
        }

        
        
        
        
        public static void OnModLoad(Harmony harmony)
        {
            if (!CaptureExpansionLoaded)
                return;

            try
            {
                
                harmony.Patch(
                    typeof(InteractionWorker_RecruitAttempt).GetMethod("Interacted"),
                    postfix: new HarmonyMethod(typeof(TheMarkedMenCaptureExpansionIntegration).GetMethod("Patch_CE_RecruitBlock_Postfix"))
                );

                
                harmony.Patch(
                    typeof(InteractionWorker_Suppress).GetMethod("Interacted"),
                    postfix: new HarmonyMethod(typeof(TheMarkedMenCaptureExpansionIntegration).GetMethod("Patch_CE_SuppressBlock_Postfix"))
                );

                
                harmony.Patch(
                    typeof(InteractionWorker_ConvertIdeoAttempt).GetMethod("Interacted"),
                    postfix: new HarmonyMethod(typeof(TheMarkedMenCaptureExpansionIntegration).GetMethod("Patch_CE_ConvertBlock_Postfix"))
                );
            }
            catch { }
        }

        
        
        
        
        public static void RemoveCEPatches(Harmony harmony)
        {
            
        }

        

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