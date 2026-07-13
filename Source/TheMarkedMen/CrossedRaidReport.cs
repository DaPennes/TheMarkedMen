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
        private const string LabelPrefix = "The Marked: ";
        private const string ImminentLabel = "The Marked: imminent!";
        private const string DefaultExplanationText = "The chronometer flickers. The Marked will come when they are ready.";

        public Alert_MarkedMenRaidCountdown()
        {
            defaultLabel = ImminentLabel;
            defaultExplanation = DefaultExplanationText;
            defaultPriority = AlertPriority.Medium;
        }

        private static TheMarkedMenGameComponent GetComponent()
        {
            return Current.Game?.GetComponent<TheMarkedMenGameComponent>();
        }

        private static int DaysToTicks(float days)
        {
            return Mathf.RoundToInt(days * GenDate.TicksPerDay);
        }

        public override AlertPriority Priority
        {
            get
            {
                TheMarkedMenGameComponent component = GetComponent();
                if (component != null && component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map _)
                    && ticksUntilRaid <= DaysToTicks(TheMarkedMenSettings.RaidCountdownHighPriorityDays))
                {
                    return AlertPriority.High;
                }

                return AlertPriority.Medium;
            }
        }

        public override AlertReport GetReport()
        {
            TheMarkedMenGameComponent component = GetComponent();
            if (!TheMarkedMenSettings.RaidCountdownAlertEnabled || component == null
                || !component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map targetMap))
            {
                return AlertReport.Inactive;
            }

            if (ticksUntilRaid > DaysToTicks(TheMarkedMenSettings.RaidCountdownVisibleDays))
            {
                return AlertReport.Inactive;
            }

            return AlertReport.CulpritIs(new GlobalTargetInfo(targetMap.Center, targetMap, false));
        }

        public override string GetLabel()
        {
            TheMarkedMenGameComponent component = GetComponent();
            if (component == null || !component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map _))
            {
                return defaultLabel;
            }

            return FormatTimeSpan(ticksUntilRaid);
        }

        public override TaggedString GetExplanation()
        {
            TheMarkedMenGameComponent component = GetComponent();
            if (component == null || !component.TryGetRaidCountdownForAlert(out int _, out int ticksUntilRaid, out Map targetMap))
            {
                return defaultExplanation;
            }

            string mapName = targetMap?.Parent?.Label ?? "unknown location";
            return "The Marked are mustering at " + mapName + ". They will attack when the chronometer reaches zero.\n\n"
                + DescribeTimeRemaining(ticksUntilRaid);
        }

        private static string FormatTimeSpan(int ticksUntilRaid)
        {
            if (ticksUntilRaid <= 0)
                return ImminentLabel;

            int totalSeconds = Mathf.CeilToInt(ticksUntilRaid / 60f);
            int days = totalSeconds / 86400;
            int hours = (totalSeconds % 86400) / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            string formatted;
            if (days > 0)
                formatted = days + "d " + hours + "h";
            else if (hours > 0)
                formatted = hours + "h " + minutes + "m";
            else
                formatted = minutes.ToString("D2") + ":" + seconds.ToString("D2");

            return LabelPrefix + formatted;
        }

        private static string DescribeTimeRemaining(int ticksUntilRaid)
        {
            if (ticksUntilRaid <= 0)
                return "The Marked are here.";

            int totalSeconds = Mathf.CeilToInt(ticksUntilRaid / 60f);
            int days = totalSeconds / 86400;
            int hours = (totalSeconds % 86400) / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            string result = "Time remaining: ";
            if (days > 0) result += days + " day" + (days != 1 ? "s" : "") + ", ";
            if (hours > 0) result += hours + " hour" + (hours != 1 ? "s" : "") + ", ";
            if (minutes > 0) result += minutes + " minute" + (minutes != 1 ? "s" : "") + ", ";
            result += seconds + " second" + (seconds != 1 ? "s" : "");

            return result;
        }
    }
}
