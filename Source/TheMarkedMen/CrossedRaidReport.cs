using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class CrossedRaidReport
    {
        public int WaveCount;
        public int InfectedSpawned;
        public int InfectedKilled;
        public int InfectedNeutralized;
        public int ColonistDeaths;
        public int ColonistsDowned;
        public int ColonistCasualties;
        public int SurvivingColonists;
        public int DurationTicks;
        public int RaidsSurvived;
        public int TotalRaidsStarted;
        public float TotalPoints;
        public float NextEscalationMultiplier;
    }

    public sealed class Alert_MarkedMenRaidCountdown : Alert
    {
        private const float ImminentDaysThreshold = 0.05f;

        public Alert_MarkedMenRaidCountdown()
        {
            defaultLabel = "Remaining:";
            defaultExplanation = "The chronometer flickers. The Marked will come when they are ready.";
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

            return FormatTimeRemaining(ticksUntilRaid);
        }

        public override TaggedString GetExplanation()
        {
            TheMarkedMenGameComponent component = Current.Game?.GetComponent<TheMarkedMenGameComponent>();
            if (component == null || !component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map _))
            {
                return defaultExplanation;
            }

            return "Something stirs beyond the perimeter. The Marked are gathering. They will come at their appointed time.";
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

        private static string FormatTimeRemaining(int ticksUntilRaid)
        {
            if (ticksUntilRaid <= 0)
            {
                return "imminent";
            }

            int totalSeconds = Mathf.CeilToInt(ticksUntilRaid / 60f);
            int days = totalSeconds / 86400;
            int hours = (totalSeconds % 86400) / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            string result = "";
            if (days > 0) result += days + " day" + (days != 1 ? "s" : "") + ", ";
            if (hours > 0) result += hours + " hour" + (hours != 1 ? "s" : "") + ", ";
            if (minutes > 0) result += minutes + " minute" + (minutes != 1 ? "s" : "") + ", ";
            result += seconds + " second" + (seconds != 1 ? "s" : "");

            return result;
        }
    }
}
