using Verse;

namespace TheMarkedMen
{
    public class Thing_InfectedBloodSample : ThingWithComps
    {
        public override Graphic Graphic
        {
            get
            {
                string path = stackCount >= 3
                    ? "Item/Infected Sample/Infected-Sample_blood3"
                    : stackCount == 2
                        ? "Item/Infected Sample/Infected-Sample_blood2"
                        : "Item/Infected Sample/Infected-Sample_blood1";

                return GraphicDatabase.Get<Graphic_Single>(path);
            }
        }
    }
}
