using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen.UI
{
    /// <summary>
    /// Always-visible raid timer UI overlay.
    /// Displays next raid countdown and threat level on screen.
    /// Draws persistently across all game screens (world map, base, etc).
    /// </summary>
    [StaticConstructorOnStartup]
    public class RaidTimerUIOverlay
    {
        private static RaidTimerUIOverlay instance;
        private static bool initialized;

        private bool visible = true;
        private Vector2 screenPosition = new Vector2(20f, 80f); // Top-left corner, below vanilla UI
        private float panelWidth = 280f;
        private float panelHeight = 100f;

        // Styling
        private static readonly Color bgColor = new Color(0.1f, 0.1f, 0.12f, 0.85f);
        private static readonly Color borderColor = new Color(0.4f, 0.4f, 0.45f, 0.6f);
        private static readonly Color headerColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        private static readonly Color textColor = Color.white;
        private static readonly Color urgencyGreen = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color urgencyYellow = new Color(1f, 0.8f, 0.2f);
        private static readonly Color urgencyRed = new Color(1f, 0.2f, 0.2f);

        private const float BorderWidth = 2f;
        private const float Padding = 8f;

        static RaidTimerUIOverlay()
        {
            instance = new RaidTimerUIOverlay();
            initialized = true;
        }

        public static void DrawRaidTimerUI()
        {
            if (!initialized || instance == null || !instance.visible)
            {
                return;
            }

            instance.DrawPanel();
        }

        public static void ToggleVisibility()
        {
            if (instance != null)
            {
                instance.visible = !instance.visible;
                Log.Message($"[The Marked Men] Raid timer UI {(instance.visible ? "shown" : "hidden")}");
            }
        }

        private void DrawPanel()
        {
            TheMarkedMenGameComponent component = CrossedUtility.Component;
            if (component == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;

            // Calculate next raid timing
            (int nextRaidTick, float raidPoints) = component.GetNextRaidTiming();
            int ticksUntilRaid = nextRaidTick - currentTick;

            // Convert to readable format
            string timeDisplay = FormatTicksToTime(ticksUntilRaid);
            string threatDisplay = DescribeThreatLevel(raidPoints);
            Color urgencyColor = GetUrgencyColor(ticksUntilRaid);

            Rect panelRect = new Rect(screenPosition.x, screenPosition.y, panelWidth, panelHeight);

            // Draw background panel
            Widgets.DrawBoxSolid(panelRect, bgColor);
            Widgets.DrawBox(panelRect, 2);

            // Draw header section
            Rect headerRect = new Rect(panelRect.x, panelRect.y, panelRect.width, 28f);
            Widgets.DrawBoxSolid(headerRect, headerColor);
            GUI.Label(headerRect, "RAID ALERT", new GUIStyle(Text.CurFontStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 0, 0, 0),
                fontStyle = FontStyle.Bold,
                fontSize = 14
            });

            // Draw content section
            Rect contentRect = new Rect(panelRect.x + Padding, headerRect.yMax + Padding, panelRect.width - Padding * 2, panelRect.height - headerRect.height - Padding * 2);

            // Time until raid
            GUI.color = urgencyColor;
            GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 24f),
                $"Next Raid: {timeDisplay}",
                new GUIStyle(Text.CurFontStyle) { fontSize = 13, fontStyle = FontStyle.Bold });

            // Threat level
            GUI.color = textColor;
            GUI.Label(new Rect(contentRect.x, contentRect.y + 28f, contentRect.width, 20f),
                $"Threat: {threatDisplay}",
                new GUIStyle(Text.CurFontStyle) { fontSize = 12 });

            // Urgency indicator
            GUI.color = urgencyColor;
            string urgencyText = GetUrgencyText(ticksUntilRaid);
            GUI.Label(new Rect(contentRect.x, contentRect.y + 48f, contentRect.width, 20f),
                $"Status: {urgencyText}",
                new GUIStyle(Text.CurFontStyle) { fontSize = 11, fontStyle = FontStyle.Italic });

            GUI.color = Color.white;
        }

        private string FormatTicksToTime(int ticks)
        {
            if (ticks <= 0)
            {
                return "IMMINENT";
            }

            float hours = ticks / (float)GenDate.TicksPerHour;
            if (hours < 1f)
            {
                return $"{Mathf.CeilToInt(ticks / 60f)}m"; // 60 ticks per in-game minute
            }

            float days = hours / 24f;
            if (days < 1f)
            {
                return $"{Mathf.CeilToInt(hours)}h";
            }

            return $"{Mathf.CeilToInt(days)}d";
        }

        private string DescribeThreatLevel(float points)
        {
            if (points >= 2400f)
            {
                return "Catastrophic";
            }

            if (points >= 1200f)
            {
                return "Severe";
            }

            if (points >= 500f)
            {
                return "Major";
            }

            if (points >= 220f)
            {
                return "Moderate";
            }

            return "Minor";
        }

        private Color GetUrgencyColor(int ticksUntilRaid)
        {
            float hours = ticksUntilRaid / (float)GenDate.TicksPerHour;

            if (hours <= 2f)
            {
                return urgencyRed; // Red: imminent
            }

            if (hours <= 24f)
            {
                return urgencyYellow; // Yellow: elevated
            }

            return urgencyGreen; // Green: normal
        }

        private string GetUrgencyText(int ticksUntilRaid)
        {
            float hours = ticksUntilRaid / (float)GenDate.TicksPerHour;

            if (hours <= 2f)
            {
                return "IMMINENT";
            }

            if (hours <= 24f)
            {
                return "ELEVATED";
            }

            if (hours <= 168f)
            {
                return "ALERT";
            }

            return "NORMAL";
        }
    }
}
