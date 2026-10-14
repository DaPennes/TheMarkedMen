using UnityEngine;
using Verse;

namespace TheMarkedMen.UI
{
    public class Gizmo_CaravanAmbushTimer : Gizmo
    {
        private Caravan caravan;
        private int ambushTick;

        public Gizmo_CaravanAmbushTimer(Caravan caravan, int ambushTick)
        {
            this.caravan = caravan;
            this.ambushTick = ambushTick;
        }

        public override void Draw(GUIRect rect)
        {
            int currentTick = Find.TickManager.TicksGame;
            int ticksUntilAmbush = ambushTick - currentTick;

            if (ticksUntilAmbush < 0)
            {
                // Ambush already occurred or passed
                Text.Font = GameFont.Compact;
                GUI.Label(rect, "No imminent threat", TexturesBarrett.GrayText);
                return;
            }

            float hours = ticksUntilAmbush / (float)(60 * 60);
            string label = $"Ambush Warning: {Mathf.CeilToInt(hours)} hours until contact";

            // Determine color based on urgency
            Color color = Color.white;
            if (hours <= 24f) color = Color.red;
            else if (hours <= 168f) color = Color.yellow;
            else color = Color.white;

            Text.Font = GameFont.Small;
            GUI.Label(rect, label, color);
        }
    }
}