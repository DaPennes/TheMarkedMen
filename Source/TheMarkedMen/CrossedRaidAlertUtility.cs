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
            // Return the fallback label (from incident def letterLabel) for vanilla-style raid alert
            return fallbackLabel;
        }

        public static string BuildRaidLetterText(string baseText, List<Pawn> pawns, IncidentParms parms, bool horde)
        {
            // Build detailed raid text similar to vanilla
            if (baseText.NullOrEmpty())
            {
                baseText = "A group of infected is attacking your colony.";
            }
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(baseText);
            sb.AppendLine();
            
            string threatTier = DescribeThreatTier(parms?.points ?? 0f);
            sb.AppendLine("Threat level: " + threatTier.CapitalizeFirst());
            
            string approach = DescribeApproach(Find.CurrentMap, pawns);
            sb.AppendLine("Approach: " + approach);
            
            string strategy = DescribeAssaultPattern(parms, horde);
            sb.AppendLine("Strategy: " + strategy);
            
            string composition = DescribeComposition(pawns);
            if (!composition.NullOrEmpty())
            {
                sb.AppendLine("Composition: " + composition);
            }
            
            string priority = DescribePriorityTargets(pawns);
            if (!priority.NullOrEmpty())
            {
                sb.AppendLine("Priority targets: " + priority);
            }
            
            return sb.ToString();
        }

        public static string DescribeThreatTier(float points)
        {
            if (points >= 2400f)
            {
                return "MarkedMen_ThreatTier_Catastrophic".Translate().Resolve();
            }

            if (points >= 1200f)
            {
                return "MarkedMen_ThreatTier_Heavy".Translate().Resolve();
            }

            if (points >= 500f)
            {
                return "MarkedMen_ThreatTier_Major".Translate().Resolve();
            }

            if (points >= 220f)
            {
                return "MarkedMen_ThreatTier_Organized".Translate().Resolve();
            }

            return "MarkedMen_ThreatTier_Probing".Translate().Resolve();
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

            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && pawns[i].kindDef == CADefOf.CrossedCivilian)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return "unclassified infected";
            }
            return count + " infected" + (count == 1 ? "" : "s");
        }

        private static string DescribePriorityTargets(List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return null;
            }

            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && pawns[i].kindDef == CADefOf.CrossedCivilian)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return "closest armed infected and exposed flankers";
            }
            return "infected forces approaching";
        }
    }
}
