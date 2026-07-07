using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    [HarmonyPatch(typeof(TattooDef), nameof(TattooDef.GraphicFor))]
    public static class Patch_TattooDef_GraphicFor_AlienRaceFix
    {
        public static bool Prefix(TattooDef __instance, Pawn pawn, Color color, ref Graphic __result)
        {
            string defName = __instance?.defName;
            if (string.IsNullOrEmpty(defName) || !defName.StartsWith("CA_Face_CrossedRash"))
            {
                return true;
            }

            if (pawn?.story == null || __instance.texPath.NullOrEmpty())
            {
                __result = null;
                return false;
            }

            __result = GraphicDatabase.Get<Graphic_Multi>(
                __instance.texPath,
                ShaderTypeDefOf.Cutout.Shader,
                Vector2.one,
                color);

            return false;
        }
    }
}
