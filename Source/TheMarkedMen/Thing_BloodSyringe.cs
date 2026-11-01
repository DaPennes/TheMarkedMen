using Verse;

namespace TheMarkedMen
{
    public class Thing_BloodSyringe : ThingWithComps
    {
        public override Graphic Graphic
        {
            get
            {
                string path = stackCount >= 3
                    ? "Item/Syringe/Syringe_craft_3"
                    : stackCount == 2
                        ? "Item/Syringe/Syringe_craft_2"
                        : "Item/Syringe/Syringe_craft_1";

                return GraphicDatabase.Get<Graphic_Single>(path);
            }
        }
    }
}
