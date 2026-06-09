using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static class CrossedReanimationManager
    {
        private static int nextReanimationProcessTick;
        private static List<Pawn> pendingReanimationPawns = new List<Pawn>();
        private static List<int> pendingReanimationTicks = new List<int>();

        public static event Action<string> IncidentRecorded;

        public static void ExposeData()
        {
            Scribe_Values.Look(ref nextReanimationProcessTick, "nextReanimationProcessTick", 0);
            Scribe_Collections.Look(ref pendingReanimationPawns, "pendingReanimationPawns", LookMode.Reference);
            Scribe_Collections.Look(ref pendingReanimationTicks, "pendingReanimationTicks", LookMode.Value);
            if (pendingReanimationPawns == null) pendingReanimationPawns = new List<Pawn>();
            if (pendingReanimationTicks == null) pendingReanimationTicks = new List<int>();
            while (pendingReanimationTicks.Count < pendingReanimationPawns.Count)
                pendingReanimationTicks.Add(0);
            while (pendingReanimationTicks.Count > pendingReanimationPawns.Count)
                pendingReanimationTicks.RemoveAt(pendingReanimationTicks.Count - 1);
        }

        public static void Tick(int currentTick, int processIntervalTicks)
        {
            if (currentTick >= nextReanimationProcessTick)
            {
                nextReanimationProcessTick = currentTick + processIntervalTicks;
                ProcessPendingReanimations();
            }
        }

        public static void QueueCrossedReanimation(Pawn pawn)
        {
            if (!CrossedUtility.ShouldReanimateAsCrossed(pawn)) return;
            if (pendingReanimationPawns.Contains(pawn)) return;
            if (!Rand.Chance(TheMarkedMenSettings.ReanimationChance))
            {
                CrossedUtility.MarkDiedFromMarkedVirus(pawn);
                return;
            }
            pendingReanimationPawns.Add(pawn);
            pendingReanimationTicks.Add((Find.TickManager?.TicksGame ?? 0) + TheMarkedMenSettings.ReanimationDelayTicks);
            NotifyReanimationQueued(pawn);
        }

        private static void ProcessPendingReanimations()
        {
            int ticks = Find.TickManager?.TicksGame ?? 0;
            int processed = 0;
            int maxProcessed = TheMarkedMenSettings.MaxPendingReanimationsPerTick;
            for (int i = pendingReanimationPawns.Count - 1; i >= 0; i--)
            {
                if (processed >= maxProcessed) return;
                Pawn pawn = pendingReanimationPawns[i];
                int readyTick = i < pendingReanimationTicks.Count ? pendingReanimationTicks[i] : 0;
                if (ticks < readyTick) continue;
                if (pawn == null || pawn.Destroyed || !pawn.Dead || !CrossedUtility.ShouldReanimateAsCrossed(pawn))
                {
                    RemovePendingReanimationAt(i);
                    continue;
                }
                Corpse corpse = pawn.Corpse;
                if (corpse == null || corpse.Destroyed)
                {
                    RemovePendingReanimationAt(i);
                    continue;
                }
                if (TryReanimatePawn(pawn))
                {
                    RemovePendingReanimationAt(i);
                    processed++;
                }
                else
                {
                    pendingReanimationTicks[i] = ticks + TheMarkedMenSettings.ReanimationProcessIntervalTicks;
                    processed++;
                }
            }
        }

        private static bool TryReanimatePawn(Pawn pawn)
        {
            try
            {
                ResurrectionParams parms = new ResurrectionParams
                {
                    gettingScarsChance = 0f,
                    removeDiedThoughts = false,
                    restoreMissingParts = false,
                    canPickUpOpportunisticWeapons = true,
                    canTimeoutOrFlee = TheMarkedMenSettings.MarkedCanTimeoutOrFlee,
                    canKidnap = false,
                    canSteal = false,
                    useAvoidGridSmart = false
                };
                if (!ResurrectionUtility.TryResurrect(pawn, parms))
                    return false;
                CrossedUtility.MarkReanimatedAsCrossed(pawn);
                CrossedUtility.TransformPawn(pawn, true);
                CrossedUtility.ApplyClassHediffs(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
                NotifyReanimated(pawn);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Failed to reanimate infected corpse: " + ex.Message);
                return false;
            }
        }

        private static void RemovePendingReanimationAt(int index)
        {
            pendingReanimationPawns.RemoveAt(index);
            if (index < pendingReanimationTicks.Count)
                pendingReanimationTicks.RemoveAt(index);
        }

        private static void NotifyReanimationQueued(Pawn pawn)
        {
            if (pawn != null && pawn.Faction == Faction.OfPlayer)
                IncidentRecorded?.Invoke(pawn.LabelShortCap + " died while infected. Reanimation is likely.");
        }

        private static void NotifyReanimated(Pawn pawn)
        {
            IncidentRecorded?.Invoke(pawn.LabelShortCap + " rose from death as one of the Marked Men.");
        }
    }
}
