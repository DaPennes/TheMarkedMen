using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class Alert_MarkedMenRaidCountdown : Alert
    {
        private const float ImminentDaysThreshold = 0.05f;

        public Alert_MarkedMenRaidCountdown()
        {
            defaultLabel = "Marked Men raid scheduled";
            defaultExplanation = "A scheduled Marked Men raid is approaching.";
            defaultPriority = AlertPriority.Medium;
        }

        public override AlertPriority Priority
        {
            get
            {
                TheMarkedMenGameComponent component = Current.Game?.GetComponent<TheMarkedMenGameComponent>();
                if (component != null && component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map _) && ticksUntilRaid <= Mathf.RoundToInt(TheMarkedMenSettings.RaidCountdownHighPriorityDays * GenDate.TicksPerDay))
                {
                    return AlertPriority.High;
                }

                return AlertPriority.Medium;
            }
        }

        public override AlertReport GetReport()
        {
            TheMarkedMenGameComponent component = Current.Game?.GetComponent<TheMarkedMenGameComponent>();
            if (!TheMarkedMenSettings.RaidCountdownAlertEnabled || component == null || !component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map targetMap))
            {
                return AlertReport.Inactive;
            }

            if (ticksUntilRaid > Mathf.RoundToInt(TheMarkedMenSettings.RaidCountdownVisibleDays * GenDate.TicksPerDay))
            {
                return AlertReport.Inactive;
            }

            return AlertReport.CulpritIs(new GlobalTargetInfo(targetMap.Center, targetMap, false));
        }

        public override string GetLabel()
        {
            TheMarkedMenGameComponent component = Current.Game?.GetComponent<TheMarkedMenGameComponent>();
            if (component == null || !component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map _))
            {
                return defaultLabel;
            }

            return "Marked Men raid " + FormatLabelTimeRemaining(ticksUntilRaid);
        }

        public override TaggedString GetExplanation()
        {
            TheMarkedMenGameComponent component = Current.Game?.GetComponent<TheMarkedMenGameComponent>();
            if (component == null || !component.TryGetRaidCountdownForAlert(out int nextTick, out int ticksUntilRaid, out Map targetMap))
            {
                return defaultExplanation;
            }

            int scheduledDay = Mathf.FloorToInt(nextTick / (float)GenDate.TicksPerDay);
            string mapLabel = targetMap?.Parent?.LabelCap ?? targetMap?.ToString() ?? "the colony";
            float estimatedPoints = component.EstimateUpcomingRaidPoints(targetMap);
            return "A scheduled Marked Men raid will begin on day " + scheduledDay + " at " + mapLabel + ".\n\n"
                + "Time remaining: " + FormatPreciseDaysRemaining(ticksUntilRaid) + ".\n"
                + "Estimated threat pressure: " + estimatedPoints.ToString("F0") + " (" + CrossedRaidAlertUtility.DescribeThreatTier(estimatedPoints) + ").\n"
                + "Expected pattern: immediate edge assault in groups; no kidnapping, theft, timeout, or retreat.\n\n"
                + "Prepare sealed fallback positions, medical capacity, fire lanes, and containment for infected blood. Prioritize Alphas and Screamers if they appear.";
        }

        private static string FormatLabelTimeRemaining(int ticksUntilRaid)
        {
            if (ticksUntilRaid <= 0)
            {
                return "imminent";
            }

            float days = ticksUntilRaid / (float)GenDate.TicksPerDay;
            if (days < ImminentDaysThreshold)
            {
                return "in less than 0.1 days";
            }

            int wholeDays = Mathf.CeilToInt(days);
            return "in " + wholeDays + " " + (wholeDays == 1 ? "day" : "days");
        }

        private static string FormatPreciseDaysRemaining(int ticksUntilRaid)
        {
            if (ticksUntilRaid <= 0)
            {
                return "imminent";
            }

            float days = ticksUntilRaid / (float)GenDate.TicksPerDay;
            if (days < ImminentDaysThreshold)
            {
                return "less than 0.1 days";
            }

            string format = days >= 10f ? "0" : "0.0";
            return days.ToString(format) + " " + (Mathf.Abs(days - 1f) < 0.05f ? "day" : "days");
        }
    }
}
