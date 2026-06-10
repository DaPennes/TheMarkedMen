using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public class StorytellerCompProperties_CrossedStoryteller : StorytellerCompProperties
    {
        public StorytellerCompProperties_CrossedStoryteller()
        {
            compClass = typeof(StorytellerComp_CrossedStoryteller);
        }
    }

    public class StorytellerComp_CrossedStoryteller : StorytellerComp
    {
        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            yield break;
        }
    }

    public static class CrossedStorytellerUtility
    {
        public static bool IsTheMarkedManActive
        {
            get
            {
                Storyteller storyteller = Find.Storyteller;
                return storyteller?.def?.defName == "CA_TheMarkedMan";
            }
        }

        public static int EffectiveFirstMarkedRaidDay => IsTheMarkedManActive ? 1 : TheMarkedMenSettings.FirstMarkedRaidDay;
        public static int EffectiveFirstMarkedRaidTick => IsTheMarkedManActive ? 2500 : EffectiveFirstMarkedRaidDay * GenDate.TicksPerDay;

        public static float EffectiveWarbandFrequencyMultiplier
        {
            get
            {
                float baseMultiplier = TheMarkedMenSettings.WarbandFrequencyMultiplier;
                return IsTheMarkedManActive ? Mathf.Max(baseMultiplier, 5f) : baseMultiplier;
            }
        }

        public static float EffectiveHordeFrequencyMultiplier
        {
            get
            {
                float baseMultiplier = TheMarkedMenSettings.HordeFrequencyMultiplier;
                return IsTheMarkedManActive ? Mathf.Max(baseMultiplier, 5f) : baseMultiplier;
            }
        }

        public static float EffectiveProbeFrequencyMultiplier
        {
            get
            {
                float baseMultiplier = TheMarkedMenSettings.ProbeFrequencyMultiplier;
                return IsTheMarkedManActive ? Mathf.Max(baseMultiplier, 5f) : baseMultiplier;
            }
        }

        public static float EffectiveRaidPointsMultiplier
        {
            get
            {
                TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
                float baseMultiplier = settings?.raidPointsMultiplier ?? 1f;
                return IsTheMarkedManActive ? Mathf.Max(baseMultiplier, 2f) : baseMultiplier;
            }
        }

        public static float EffectiveMinimumRaidPoints
        {
            get
            {
                TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
                float baseMin = settings?.minimumRaidPoints ?? 120f;
                return IsTheMarkedManActive ? Mathf.Max(baseMin, 500f) : baseMin;
            }
        }

        public static float EffectiveRaidEscalationPerRaid
        {
            get
            {
                float baseEscalation = TheMarkedMenSettings.RaidEscalationPerRaid;
                return IsTheMarkedManActive ? Mathf.Max(baseEscalation, 0.5f) : baseEscalation;
            }
        }

        public static float EffectiveRaidEscalationMaxBonus
        {
            get
            {
                float baseMax = TheMarkedMenSettings.RaidEscalationMaxBonus;
                return IsTheMarkedManActive ? Mathf.Max(baseMax, 10f) : baseMax;
            }
        }

        public static bool IsRaidCountdownVisible
        {
            get
            {
                if (IsTheMarkedManActive)
                {
                    return true;
                }
                return TheMarkedMenSettings.RaidCountdownAlertEnabled;
            }
        }
    }
}
