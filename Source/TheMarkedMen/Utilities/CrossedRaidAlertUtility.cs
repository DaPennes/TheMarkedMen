using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static class CrossedRaidAlertUtility
    {
        public static string BuildRaidLetterLabel(string fallbackLabel, List<Pawn> pawns, float points)
        {
            int count = CountActivePawns(pawns);
            string pressure = DescribeThreatTier(points);
            string fallback = fallbackLabel.NullOrEmpty() ? "Marked Men warband" : fallbackLabel;
            if (count <= 0)
            {
                return points > 0f ? fallback + ": " + pressure : fallback;
            }

            return fallback + ": " + count + " infected, " + pressure;
        }

        public static string BuildRaidLetterText(string baseText, List<Pawn> pawns, IncidentParms parms, bool horde)
        {
            float points = Mathf.Max(0f, parms?.points ?? 0f);
            Map map = parms?.target as Map;
            string text = baseText.NullOrEmpty()
                ? "A group of infected Marked Men has reached the colony."
                : baseText;

            if (!TheMarkedMenSettings.DetailedRaidLetters)
            {
                return text;
            }

            List<string> details = new List<string>();
            details.Add("Detected infected: " + CountActivePawns(pawns));
            details.Add("Threat pressure: " + points.ToString("F0") + " (" + DescribeThreatTier(points) + ")");
            details.Add("Approach: " + DescribeApproach(map, pawns));
            details.Add("Assault pattern: " + DescribeAssaultPattern(parms, horde));

            string composition = DescribeComposition(pawns);
            if (!composition.NullOrEmpty())
            {
                details.Add("Composition: " + composition);
            }

            string priority = DescribePriorityTargets(pawns);
            if (!priority.NullOrEmpty())
            {
                details.Add("Priority targets: " + priority);
            }

            details.Add("Containment: keep wounded and doctors away from melee contact, isolate infected blood, and hold sealed fallback doors.");

            return text + "\n\n" + string.Join("\n", details);
        }

        public static string DescribeThreatTier(float points)
        {
            if (points >= 2400f) return "catastrophic pressure";
            if (points >= 1200f) return "heavy pressure";
            if (points >= 500f) return "major pressure";
            if (points >= 220f) return "organized pressure";
            return "probing pressure";
        }

        private static int CountActivePawns(List<Pawn> pawns)
        {
            if (pawns == null) return 0;

            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && !pawn.Destroyed && !pawn.Dead)
                {
                    count++;
                }
            }

            return count;
        }

        private static string DescribeApproach(Map map, List<Pawn> pawns)
        {
            if (map == null || pawns == null || pawns.Count == 0)
            {
                return "edge approach, direction unknown";
            }

            float x = 0f;
            float z = 0f;
            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Map != map)
                {
                    continue;
                }

                x += pawn.Position.x;
                z += pawn.Position.z;
                count++;
            }

            if (count == 0)
            {
                return "edge approach, direction unknown";
            }

            x /= count;
            z /= count;
            float dx = x - map.Size.x * 0.5f;
            float dz = z - map.Size.z * 0.5f;
            float absX = Mathf.Abs(dx);
            float absZ = Mathf.Abs(dz);
            if (absX < 8f && absZ < 8f)
            {
                return "near the colony interior";
            }

            if (absX > absZ * 1.35f)
            {
                return dx >= 0f ? "eastern edge" : "western edge";
            }

            if (absZ > absX * 1.35f)
            {
                return dz >= 0f ? "northern edge" : "southern edge";
            }

            string northSouth = dz >= 0f ? "north" : "south";
            string eastWest = dx >= 0f ? "east" : "west";
            return northSouth + eastWest + " edge";
        }

        private static string DescribeAssaultPattern(IncidentParms parms, bool horde)
        {
            string strategy = parms?.raidStrategy?.LabelCap.ToString();
            string arrival = parms?.raidArrivalMode?.LabelCap.ToString();
            if (strategy.NullOrEmpty())
            {
                strategy = "immediate attack";
            }

            if (arrival.NullOrEmpty())
            {
                arrival = "edge walk-in groups";
            }

            return strategy + ", " + arrival + (horde ? ", horde pressure" : ", no kidnapping/theft/retreat");
        }

        private static string DescribeComposition(List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return null;
            }

            List<string> parts = new List<string>();
            AddKindCount(parts, pawns, CADefOf.Alpha, "Alpha");
            AddKindCount(parts, pawns, CADefOf.Brute, "Brute");
            AddKindCount(parts, pawns, CADefOf.Screamer, "Screamer");
            AddKindCount(parts, pawns, CADefOf.Stalker, "Stalker");
            AddKindCount(parts, pawns, CADefOf.Hunter, "Hunter");
            AddKindCount(parts, pawns, CADefOf.Berserker, "Berserker");
            return parts.Count == 0 ? "unclassified infected" : string.Join(", ", parts);
        }

        private static void AddKindCount(List<string> parts, List<Pawn> pawns, PawnKindDef kind, string label)
        {
            if (parts == null || pawns == null || kind == null)
            {
                return;
            }

            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i]?.kindDef == kind)
                {
                    count++;
                }
            }

            if (count > 0)
            {
                parts.Add(count + " " + label + (count == 1 ? "" : "s"));
            }
        }

        private static string DescribePriorityTargets(List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return null;
            }

            bool hasAlpha = HasKind(pawns, CADefOf.Alpha);
            bool hasScreamer = HasKind(pawns, CADefOf.Screamer);
            bool hasBrute = HasKind(pawns, CADefOf.Brute);
            List<string> priorities = new List<string>();
            if (hasAlpha) priorities.Add("Alphas coordinating nearby infected");
            if (hasScreamer) priorities.Add("Screamers disrupting morale");
            if (hasBrute) priorities.Add("Brutes breaching doors and lines");
            return priorities.Count == 0 ? "closest armed infected and exposed flankers" : string.Join("; ", priorities);
        }

        private static bool HasKind(List<Pawn> pawns, PawnKindDef kind)
        {
            if (pawns == null || kind == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i]?.kindDef == kind)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
