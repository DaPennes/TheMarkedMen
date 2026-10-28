using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TheMarkedMen.UI
{
    /// <summary>
    /// Vanilla-style raid countdown drawn in the same slot the game uses for
    /// timed raid warnings: between the letter stack and the time/environment
    /// readout in the bottom-right corner.
    /// </summary>
    public static class RaidTimerUIOverlay
    {
        private const float RowHeight = 26f;
        private const float LetterStackGap = 10f;
        private const float RightPadding = 7f;

        /// <summary>
        /// Draws the countdown and moves the letter stack up by one row so the
        /// countdown does not overlap either the letters or the environment HUD.
        /// </summary>
        public static void Draw(ref float letterStackBaseY)
        {
            TheMarkedMenGameComponent component = CrossedUtility.Component;
            if (component == null || !component.TryGetRaidCountdownForHud(out int ticksUntilRaid))
            {
                return;
            }

            float visibleDays = TheMarkedMenSettings.RaidCountdownVisibleDays;
            if (visibleDays < 999f && ticksUntilRaid > visibleDays * GenDate.TicksPerDay)
            {
                return;
            }

            // Vanilla reserves one row for its own timed-raid countdown and moves
            // the letter stack up by that row. Do the same so the HUD stays
            // perfectly aligned with the date/time readout.
            letterStackBaseY -= RowHeight;

            float columnWidth = WorldRendererUtility.WorldSelected ? 250f : 200f;
            Rect rect = new Rect(
                Verse.UI.screenWidth - columnWidth,
                letterStackBaseY + LetterStackGap,
                columnWidth - RightPadding,
                RowHeight);

            bool highPriority = TheMarkedMenSettings.RaidCountdownHighPriorityDays > 0f
                && ticksUntilRaid <= TheMarkedMenSettings.RaidCountdownHighPriorityDays * GenDate.TicksPerDay;

            string remaining = ticksUntilRaid < GenDate.TicksPerHour
                ? null
                : ticksUntilRaid.ToStringTicksToPeriod(allowSeconds: false, allowYears: false);

            string label = remaining == null
                ? "MarkedMen_RaidCountdownHUDImminent".Translate().Resolve()
                : "MarkedMen_RaidCountdownHUD".Translate(remaining).Resolve();

            string tooltip = remaining == null
                ? "MarkedMen_RaidCountdownHUDTooltipImminent".Translate().Resolve()
                : "MarkedMen_RaidCountdownHUDTooltip".Translate(remaining).Resolve();

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;

            try
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleRight;

                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }

                GUI.color = highPriority ? ColorLibrary.RedReadable : Color.white;
                Widgets.Label(rect, label);
                TooltipHandler.TipRegion(rect, tooltip);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                GUI.color = oldColor;
            }
        }
    }
}
