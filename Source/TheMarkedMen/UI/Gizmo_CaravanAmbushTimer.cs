using RimWorld;
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

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float width, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, width, 75f);
            int currentTick = Find.TickManager.TicksGame;
            int ticksUntilAmbush = ambushTick - currentTick;

            if (ticksUntilAmbush < 0)
            {
                // Ambush already occurred or passed
                Text.Font = GameFont.Compact;
                GUI.Label(rect, "No imminent threat");
                return new GizmoResult(GizmoState.Clear);
            }

            float hours = ticksUntilAmbush / (float)GenDate.TicksPerHour;
            string label = $"Ambush Warning: {Mathf.CeilToInt(hours)} hours until contact";

            // Determine color based on urgency
            GUIStyle style = new GUIStyle(TextLibrary.SmallFont);
            Color color = Color.white;
            if (hours <= 24f) color = Color.red;
            else if (hours <= 168f) color = Color.yellow;
            else color = Color.white;

            Text.Font = GameFont.Small;
            GUIStyle colored = new GUIStyle(style);
            colored.normal.textColor = color;
            GUI.Label(rect, label, colored);
            return new GizmoResult(GizmoState.Active);
        }

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(200f, maxWidth);
        }
    }
}