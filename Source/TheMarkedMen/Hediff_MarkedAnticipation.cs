using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace TheMarkedMen
{
    public class HediffCompProperties_KillAnticipation : HediffCompProperties
    {
        public float scanRadius = 25f;
        public int scanIntervalTicks = 60;
        public float maxSeverity = 1f;
        public float decayPerInterval = 0.05f;
        public float baseGainPerEnemy = 0.015f;
        public float proximityBonus = 0.04f;
        public float proximityThreshold = 12f;

        public HediffCompProperties_KillAnticipation()
        {
            compClass = typeof(HediffComp_KillAnticipation);
        }
    }

    public class HediffComp_KillAnticipation : HediffComp
    {
        private int nextScanTick;

        public HediffCompProperties_KillAnticipation Props => (HediffCompProperties_KillAnticipation)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                severityAdjustment -= Props.decayPerInterval;
                return;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null)
            {
                severityAdjustment -= Props.decayPerInterval;
                return;
            }

            int ticks = Find.TickManager.TicksGame;
            if (ticks < nextScanTick)
            {
                return;
            }

            nextScanTick = ticks + Props.scanIntervalTicks;

            float totalGain = 0f;
            int enemyCount = 0;
            float radiusSq = Props.scanRadius * Props.scanRadius;
            float proximitySq = Props.proximityThreshold * Props.proximityThreshold;
            IntVec3 pos = pawn.Position;
            Faction pawnFaction = pawn.Faction;

            IReadOnlyList<Pawn> allPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn other = allPawns[i];
                if (other == pawn || other.Dead || other.Downed || !other.RaceProps.Humanlike)
                {
                    continue;
                }

                Faction otherFaction = other.Faction;
                if (otherFaction == null || pawnFaction == null)
                {
                    continue;
                }

                if (!otherFaction.HostileTo(pawnFaction))
                {
                    continue;
                }

                float distSq = pos.DistanceToSquared(other.Position);
                if (distSq > radiusSq)
                {
                    continue;
                }

                enemyCount++;
                totalGain += Props.baseGainPerEnemy;
                if (distSq <= proximitySq)
                {
                    totalGain += Props.proximityBonus;
                }
            }

            if (enemyCount > 0)
            {
                severityAdjustment = Mathf.Min(Props.maxSeverity, parent.Severity + totalGain * settings.anticipationGainMultiplier) - parent.Severity;
            }
            else
            {
                severityAdjustment -= Props.decayPerInterval * settings.anticipationDecayMultiplier;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref nextScanTick, "nextScanTick", 0);
        }
    }
}
