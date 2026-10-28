using System;
using System.Collections.Generic;
using System.Linq;
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

            Dictionary<PawnKindDef, int> counts = new Dictionary<PawnKindDef, int>();
            for (int i = 0; i < pawns.Count; i++)
            {
                PawnKindDef kind = pawns[i]?.kindDef;
                if (kind == null)
                {
                    continue;
                }

                counts.TryGetValue(kind, out int count);
                counts[kind] = count + 1;
            }

            if (counts.Count == 0)
            {
                return "unclassified infected";
            }

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<PawnKindDef, int> pair in counts.OrderByDescending(p => p.Value).ThenBy(p => p.Key.label))
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                string label = pair.Key.GetLabelPlural(pair.Value);
                if (label.NullOrEmpty())
                {
                    label = pair.Key.defName;
                }

                sb.Append(pair.Value).Append(' ').Append(label);
            }

            return sb.ToString();
        }

        private static string DescribePriorityTargets(List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return null;
            }

            bool commanders = false;
            bool ranged = false;
            for (int i = 0; i < pawns.Count; i++)
            {
                PawnKindDef kind = pawns[i]?.kindDef;
                if (kind == CADefOf.CrossedAlpha || kind == CADefOf.CrossedWarlord || kind == CADefOf.MarkedMan)
                {
                    commanders = true;
                }
                else if (kind == CADefOf.CrossedShooter || kind == CADefOf.CrossedSoldier
                    || kind == CADefOf.CrossedRaider || kind == CADefOf.CrossedHunter)
                {
                    ranged = true;
                }
            }

            if (commanders)
            {
                return "elite infected commanders and their escorts";
            }
            if (ranged)
            {
                return "ranged infected and exposed flanks";
            }
            return "closest armed infected and exposed flankers";
        }
    }
}
