using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    public sealed class IncidentWorker_CrossedProbe : IncidentWorker
    {
        private const int MinProbeCount = 2;
        private const int MaxProbeCount = 4;

        public override float ChanceFactorNow(IIncidentTarget target)
        {
            return base.ChanceFactorNow(target) * TheMarkedMenSettings.ProbeFrequencyMultiplier;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms) || !(parms.target is Map map) || map.mapPawns == null || !map.mapPawns.AnyFreeColonistSpawned)
            {
                return false;
            }

            return CrossedUtility.Component?.EnsureCrossedFaction() != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (map == null || crossed == null)
            {
                return false;
            }

            parms.faction = crossed;
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = TheMarkedMenSettings.MarkedCanTimeoutOrFlee;
            TheMarkedMenGameComponent.ApplyMarkedRaidArrivalPattern(parms);
            parms.points = CalculateProbePoints(map, parms.points, def.minThreatPoints);

            int count = CalculateProbeCount(parms.points, parms.pawnCount);
            List<Pawn> pawns = GenerateProbePawns(count, parms.points, crossed, map);
            if (pawns.Count == 0)
            {
                return false;
            }

            parms.pawnCount = pawns.Count;
            if (parms.raidArrivalMode?.Worker == null || !parms.raidArrivalMode.Worker.CanUseWith(parms))
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            }

            parms.raidArrivalMode.Worker.Arrive(pawns, parms);
            pawns = CrossedLordCleanupUtility.CollectValidSpawnedLordPawns(pawns, map, crossed);
            if (pawns.Count == 0)
            {
                return false;
            }

            parms.pawnCount = pawns.Count;
            LordMaker.MakeNewLord(crossed, new LordJob_AssaultColony(crossed, false, TheMarkedMenSettings.MarkedCanTimeoutOrFlee, false, false, false, false, true), map, pawns);
            CrossedUtility.Component?.NotifyProbeLaunched(pawns.Count, parms.points);
            SendProbeLetter(pawns, parms);
            return true;
        }

        private static float CalculateProbePoints(Map map, float existingPoints, float minThreatPoints)
        {
            float storytellerPoints = map == null ? minThreatPoints : StorytellerUtility.DefaultThreatPointsNow(map);
            float points = Mathf.Max(existingPoints, storytellerPoints * 0.45f, minThreatPoints);
            return TheMarkedMenSettings.ApplyRaidPointSettings(Mathf.Clamp(points, minThreatPoints, 650f));
        }

        private static int CalculateProbeCount(float points, int requestedCount)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            int minCount = settings?.minimumProbeSize ?? MinProbeCount;
            int maxCount = settings?.maximumProbeSize ?? MaxProbeCount;
            minCount = Mathf.Clamp(minCount, 1, 20);
            maxCount = Mathf.Clamp(maxCount, minCount, 30);
            if (requestedCount > 0)
            {
                return Mathf.Clamp(requestedCount, minCount, maxCount);
            }

            float normalizedThreat = Mathf.InverseLerp(80f, 650f, points);
            int expected = Mathf.RoundToInt(Mathf.Lerp(minCount, maxCount, normalizedThreat));
            int variance = Mathf.Clamp(Mathf.RoundToInt(expected * 0.2f), 1, 2);
            return Rand.RangeInclusive(Mathf.Max(minCount, expected - variance), Mathf.Min(maxCount, expected + variance));
        }

        private static List<Pawn> GenerateProbePawns(int count, float points, Faction faction, Map map)
        {
            List<Pawn> pawns = new List<Pawn>(count);
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = PickProbeKind(points);
                if (kind == null)
                {
                    break;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction, map.Tile);
                if (pawn == null)
                {
                    continue;
                }

                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                pawns.Add(pawn);
            }

            CrossedUtility.ApplyGeneratedRaidKindTuning(pawns);
            return pawns;
        }

        private static PawnKindDef PickProbeKind(float points)
        {
            float normalizedThreat = Mathf.InverseLerp(80f, 650f, points);
            PawnKindDef selected = null;
            float totalWeight = 0f;

            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Stalker, Mathf.Lerp(4f, 6f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Hunter, Mathf.Lerp(3f, 5f, normalizedThreat));
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Berserker, 3f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Screamer, points >= 220f ? Mathf.Lerp(0.5f, 1.75f, normalizedThreat) : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Spitter, points >= 180f ? Mathf.Lerp(0.5f, 1.5f, normalizedThreat) : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Charger, points >= 200f ? Mathf.Lerp(0.5f, 1f, normalizedThreat) : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Brute, points >= 500f ? 0.35f : 0f);
            AddWeightedKind(ref selected, ref totalWeight, CADefOf.Child, TheMarkedMenMod.Settings?.allowMarkedChildren == true && points < 220f ? 0.35f : 0f);

            return selected ?? CADefOf.Stalker ?? CADefOf.Hunter ?? CADefOf.Berserker;
        }

        private static void AddWeightedKind(ref PawnKindDef selected, ref float totalWeight, PawnKindDef kind, float weight)
        {
            weight = TheMarkedMenSettings.AdjustKindWeight(kind, weight);
            if (kind == null || weight <= 0f)
            {
                return;
            }

            totalWeight += weight;
            if (Rand.Value * totalWeight <= weight)
            {
                selected = kind;
            }
        }

        private void SendProbeLetter(List<Pawn> pawns, IncidentParms parms)
        {
            if (Find.LetterStack == null)
            {
                return;
            }

            IncidentParms letterParms = new IncidentParms
            {
                points = parms?.points ?? 0f,
                target = pawns.Count > 0 ? pawns[0].Map : null,
                raidStrategy = parms?.raidStrategy ?? RaidStrategyDefOf.ImmediateAttack,
                raidArrivalMode = parms?.raidArrivalMode ?? PawnsArrivalModeDefOf.EdgeWalkInGroups
            };
            string label = CrossedRaidAlertUtility.BuildRaidLetterLabel(def.letterLabel.NullOrEmpty() ? "Marked Men scouting pack" : def.letterLabel, pawns, letterParms.points);
            string text = CrossedRaidAlertUtility.BuildRaidLetterText(def.letterText, pawns, letterParms, false);
            Find.LetterStack.ReceiveLetter(label, text, def.letterDef ?? LetterDefOf.ThreatSmall, new LookTargets(pawns));
        }
    }
}
