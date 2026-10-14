using RimWorld;
using UnityEngine;
using Verse;
using System;

namespace TheMarkedMen.UI
{
    public class Gizmo_CaravanAmbushTimer : Gizmo
    {
        private dynamic caravan;
        private int ambushTick;

        public Gizmo_CaravanAmbushTimer(dynamic caravan, int ambushTick)
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
                Text.Font = GameFont.Tiny;
                GUI.Label(rect, "No imminent threat");
                return new GizmoResult(GizmoState.Clear);
            }

            float hours = ticksUntilAmbush / (float)GenDate.TicksPerHour;
            string label = $"Ambush Warning: {Mathf.CeilToInt(hours)}h";

            // Determine color based on urgency
            Color color = Color.white;
            if (hours <= 24f) color = Color.red;
            else if (hours <= 168f) color = Color.yellow;

            Text.Font = GameFont.Small;
            GUI.color = color;
            GUI.Label(rect, label);
            GUI.color = Color.white;

            return new GizmoResult(GizmoState.Clear);
        }

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(200f, maxWidth);
        }
    }
}