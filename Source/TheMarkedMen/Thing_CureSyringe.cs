using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public class Thing_CureSyringe : ThingWithComps
    {
        public override Graphic Graphic
        {
            get
            {
                string path = stackCount >= 10
                    ? "Item/Cure/Cure_Syringe_Stack10"
                    : stackCount >= 5
                        ? "Item/Cure/Cure_Syringe_Stack5"
                        : "Item/Cure/Cure_Syringe";

                return GraphicDatabase.Get<Graphic_Single>(path);
            }
        }
    }
}
