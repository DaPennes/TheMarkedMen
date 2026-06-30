using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace TheMarkedMen
{
    public static class CrossedRaidAlertUtility
    {
        public static string BuildRaidLetterLabel(string fallbackLabel, List<Pawn> pawns, float points)
        {
            return "The Marked have arrived.";
        }

        public static string BuildRaidLetterText(string baseText, List<Pawn> pawns, IncidentParms parms, bool horde)
        {
            return "The chronometer ticks. The Marked are here. Hold the line.";
        }

        public static string DescribeThreatTier(float points)
        {
            if (points >= 2400f)
            {
                return "catastrophic pressure";
            }

            if (points >= 1200f)
            {
                return "heavy pressure";
            }

            if (points >= 500f)
            {
                return "major pressure";
            }

            if (points >= 220f)
            {
                return "organized pressure";
            }

            return "probing pressure";
        }

        private static int CountActivePawns(List<Pawn> pawns)
        {
            if (pawns == null)
            {
                return 0;
            }

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
            AddKindCount(parts, pawns, CADefOf.MarkedMan, "Marked Man");
            AddKindCount(parts, pawns, CADefOf.CrossedWarlord, "Warlord");
            AddKindCount(parts, pawns, CADefOf.CrossedAlpha, "Alpha");
            AddKindCount(parts, pawns, CADefOf.CrossedBrute, "Brute");
            AddKindCount(parts, pawns, CADefOf.CrossedSoldier, "Soldier");
            AddKindCount(parts, pawns, CADefOf.CrossedRaider, "Raider");
            AddKindCount(parts, pawns, CADefOf.CrossedHunter, "Hunter");
            AddKindCount(parts, pawns, CADefOf.CrossedShooter, "Shooter");
            AddKindCount(parts, pawns, CADefOf.CrossedPyromaniac, "Pyromaniac");
            AddKindCount(parts, pawns, CADefOf.CrossedScout, "Scout");
            AddKindCount(parts, pawns, CADefOf.CrossedCivilian, "Civilian");
            return parts.Count == 0 ? "unclassified infected" : string.Join(", ", parts.ToArray());
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

            bool hasMarkedMan = HasKind(pawns, CADefOf.MarkedMan);
            bool hasWarlord = HasKind(pawns, CADefOf.CrossedWarlord);
            bool hasAlpha = HasKind(pawns, CADefOf.CrossedAlpha);
            bool hasBrute = HasKind(pawns, CADefOf.CrossedBrute);
            bool hasSoldier = HasKind(pawns, CADefOf.CrossedSoldier);
            bool hasPyromaniac = HasKind(pawns, CADefOf.CrossedPyromaniac);
            List<string> priorities = new List<string>();
            if (hasMarkedMan)
            {
                priorities.Add("Marked Men leading the assault");
            }

            if (hasWarlord)
            {
                priorities.Add("Warlords commanding infected forces");
            }

            if (hasAlpha)
            {
                priorities.Add("Alphas coordinating nearby infected");
            }

            if (hasSoldier)
            {
                priorities.Add("Soldiers maintaining tactical formation");
            }

            if (hasBrute)
            {
                priorities.Add("Brutes breaching doors and lines");
            }

            if (hasPyromaniac)
            {
                priorities.Add("Pyromaniacs spreading fire and chaos");
            }

            return priorities.Count == 0 ? "closest armed infected and exposed flankers" : string.Join("; ", priorities.ToArray());
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
