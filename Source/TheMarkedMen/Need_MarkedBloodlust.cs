using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace TheMarkedMen
{
    public class Need_MarkedBloodlust : Need
    {
        private const float MinBerserkInterval = 3000f;
        private const float StarvingThreshold = 0.15f;
        private const float BaseDecayPerInterval = 0.015f;
        private const float KillGain = 0.30f;
        private const float CombatGainPerInterval = 0.01f;
        private const float CombatScanRadius = 30f;

        private int lastBerserkTick;
        private int nextCombatScanTick;

        public Need_MarkedBloodlust(Pawn pawn) : base(pawn)
        {
        }

        public override void NeedInterval()
        {
            if (IsFrozen)
            {
                return;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.bloodlustEnabled)
            {
                return;
            }

            float decay = BaseDecayPerInterval * settings.bloodlustDecayRate;
            CurLevel = Mathf.Max(0f, CurLevel - decay);

            if (pawn.Spawned && pawn.Map != null && !pawn.Downed && !pawn.Dead)
            {
                int ticks = Find.TickManager.TicksGame;
                if (ticks >= nextCombatScanTick)
                {
                    nextCombatScanTick = ticks + 150;
                    if (IsInCombat())
                    {
                        CurLevel = Mathf.Min(1f, CurLevel + CombatGainPerInterval * settings.bloodlustCombatGainMultiplier);
                    }
                }
            }

            if (CurLevel < StarvingThreshold && pawn.Spawned && !pawn.Downed && !pawn.Dead && pawn.mindState != null)
            {
                int ticks = Find.TickManager.TicksGame;
                if (ticks - lastBerserkTick >= MinBerserkInterval)
                {
                    lastBerserkTick = ticks;
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "bloodlust starvation");
                }
            }
        }

        public void NotifyKilled()
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            float gain = KillGain * (settings?.bloodlustKillGainMultiplier ?? 1f);
            CurLevel = Mathf.Min(1f, CurLevel + gain);
        }

        private bool IsInCombat()
        {
            float radiusSq = CombatScanRadius * CombatScanRadius;
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

                if (pos.DistanceToSquared(other.Position) <= radiusSq)
                {
                    return true;
                }
            }

            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastBerserkTick, "lastBerserkTick", 0);
            Scribe_Values.Look(ref nextCombatScanTick, "nextCombatScanTick", 0);
        }
    }
}
