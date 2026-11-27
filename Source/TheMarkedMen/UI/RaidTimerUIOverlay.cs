using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TheMarkedMen.UI
{
    
    
    
    
    
    public static class RaidTimerUIOverlay
    {
        private const float RowHeight = 26f;
        private const float LetterStackGap = 10f;
        private const float RightPadding = 7f;

        
        
        
        
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
