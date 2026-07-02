using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public sealed class StorytellerCompProperties_CrossedStoryteller : StorytellerCompProperties
    {
        public float mtbDays = 0.95f;
        public float minRandomDaysPassed = 0.05f;
        public float minSpacingDays = 0.45f;
        public FloatRange pointsFactorRange = new FloatRange(1.05f, 1.85f);
        public float storytellerThreatScaleMultiplier = 1f;

        public StorytellerCompProperties_CrossedStoryteller()
        {
            compClass = typeof(StorytellerComp_CrossedStoryteller);
        }
    }

    public sealed class StorytellerComp_CrossedStoryteller : StorytellerComp
    {
        private int lastIncidentTick = -999999;

        private StorytellerCompProperties_CrossedStoryteller Props => (StorytellerCompProperties_CrossedStoryteller)props;

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            if (!(target is Map map) || !map.IsPlayerHome || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
            {
                yield break;
            }

            if (Find.TickManager == null || Find.TickManager.TicksGame < Mathf.RoundToInt(Props.minRandomDaysPassed * GenDate.TicksPerDay))
            {
                yield break;
            }

            int ticks = Find.TickManager.TicksGame;
            int minSpacingTicks = Mathf.RoundToInt(Mathf.Max(0.1f, Props.minSpacingDays) * GenDate.TicksPerDay);
            if (ticks - lastIncidentTick < minSpacingTicks)
            {
                yield break;
            }

            float frequency = Mathf.Max(0.05f, TheMarkedMenMod.Settings?.EffectiveMarkedRaidFrequencyMultiplier ?? 1f);
            float mtbDays = Mathf.Max(0.15f, Props.mtbDays / frequency);
            if (!Rand.MTBEventOccurs(mtbDays, GenDate.TicksPerDay, 1000f))
            {
                yield break;
            }

            IncidentDef incident = PickRandomMarkedIncident();
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (incident == null || crossed == null)
            {
                yield break;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
            parms.target = map;
            parms.faction = crossed;
            parms.points = BuildRandomIncidentPoints(map, incident, parms.points);
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = false;
            parms.forced = false;
            TheMarkedMenGameComponent.ApplyMarkedRaidArrivalPattern(parms);

            if (!incident.Worker.CanFireNow(parms))
            {
                yield break;
            }

            lastIncidentTick = ticks;
            yield return new FiringIncident(incident, this, parms);
        }

        private IncidentDef PickRandomMarkedIncident()
        {
            IncidentDef selected = null;
            float totalWeight = 0f;
            AddIncidentCandidate(ref selected, ref totalWeight, CADefOf.CrossedProbe, TheMarkedMenSettings.ProbesEnabled ? 4.5f : 0f);
            AddIncidentCandidate(ref selected, ref totalWeight, CADefOf.CrossedRaid, TheMarkedMenSettings.WarbandsEnabled ? 3.0f : 0f);
            AddIncidentCandidate(ref selected, ref totalWeight, CADefOf.CrossedHorde, TheMarkedMenSettings.HordesEnabled ? 1.75f : 0f);
            AddIncidentCandidate(ref selected, ref totalWeight, DefDatabase<IncidentDef>.GetNamedSilentFail("CA_CrossedDownedSurvivor"), 1.15f);
            return selected;
        }

        private static void AddIncidentCandidate(ref IncidentDef selected, ref float totalWeight, IncidentDef incident, float weight)
        {
            if (incident == null || weight <= 0f)
            {
                return;
            }

            totalWeight += weight;
            if (Rand.Value * totalWeight <= weight)
            {
                selected = incident;
            }
        }

        private float BuildRandomIncidentPoints(Map map, IncidentDef incident, float existingPoints)
        {
            float minimum = Mathf.Max(incident?.minThreatPoints ?? 120f, TheMarkedMenMod.Settings?.minimumRaidPoints ?? 120f);
            float storytellerPoints = map == null ? minimum : StorytellerUtility.DefaultThreatPointsNow(map);
            float points = Mathf.Max(existingPoints, storytellerPoints, minimum);
            float pressure = Mathf.InverseLerp(5000f, 50000f, points);
            float pressureFactor = Mathf.Lerp(1.05f, 1.35f, pressure);
            float randomFactor = Props.pointsFactorRange.RandomInRange;
            float storytellerFactor = CalculateStorytellerThreatFactor();
            return TheMarkedMenSettings.ApplyRaidPointSettings(Mathf.Max(minimum, points * pressureFactor * randomFactor * storytellerFactor));
        }

        private float CalculateStorytellerThreatFactor()
        {
            Difficulty difficulty = Find.Storyteller?.difficulty;
            float rawThreatScale = Mathf.Max(0.01f, difficulty?.threatScale ?? 1f);
            float normalizedThreatScale = rawThreatScale > 10f ? rawThreatScale / 100f : rawThreatScale;
            return Mathf.Clamp(normalizedThreatScale * Mathf.Max(1f, Props.storytellerThreatScaleMultiplier), 1f, 20f);
        }
    }
}
