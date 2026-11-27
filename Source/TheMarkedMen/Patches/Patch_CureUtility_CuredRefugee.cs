using HarmonyLib;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    
    
    
    
    [HarmonyPatch(typeof(CureUtility), nameof(CureUtility.ApplyCureDirectly))]
    public static class Patch_CureUtility_CuredRefugee
    {
        
        
        
        
        static void Prefix(Pawn pawn, out Faction __state)
        {
            
            __state = pawn?.Faction;
        }

        
        
        
        
        static void Postfix(Pawn pawn, Faction __state)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map == null)
                return;

            
            Faction originalFaction = __state;

            
            if (originalFaction == Faction.OfPlayer)
                return;

            
            
            pawn.SetFaction(null);

            
            CuredRefugeeManager manager = CuredRefugeeManager.GetForMap(pawn.Map);
            if (manager != null)
            {
                manager.RegisterRefugee(pawn, originalFaction);
            }
        }
    }
}
