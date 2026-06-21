using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace TheMarkedMen
{
    public class Need_MarkedBloodlust : Need
    {
        private const float CravingThreshold = 0.30f;
        private const float SatisfiedThreshold = 0.80f;
        private const float BaseDecayPerInterval = 0.005f;
        private const float KillGain = 0.30f;
        private const float CombatGainPerInterval = 0.01f;
        private const float CombatScanRadius = 30f;

        private int nextCombatScanTick;
        private int nextCravingRefreshTick;
        private int nextBloodlustRefreshTick;

        public Need_MarkedBloodlust(Pawn pawn) : base(pawn)
        {
        }

        public override bool ShowOnNeedList => CrossedUtility.IsInfectedPawn(pawn);

        public override void NeedInterval()
        {
            if (IsFrozen)
            {
                return;
            }

            if (!CrossedUtility.IsInfectedPawn(pawn))
            {
                CurLevel = 0f;
                return;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.bloodlustEnabled)
            {
                CurLevel = 0f;
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

            UpdateMoodThoughts();
        }

        public void NotifyKilled()
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            float gain = KillGain * (settings?.bloodlustKillGainMultiplier ?? 1f);
            CurLevel = Mathf.Min(1f, CurLevel + gain);
        }

        private void UpdateMoodThoughts()
        {
            if (pawn.needs?.mood?.thoughts?.memories == null)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;

            if (CurLevel < CravingThreshold)
            {
                if (tick >= nextCravingRefreshTick && CADefOf.BloodthirstyCraving != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory((Thought_Memory)ThoughtMaker.MakeThought(CADefOf.BloodthirstyCraving));
                    nextCravingRefreshTick = tick + 60000;
                }
            }

            if (CurLevel > SatisfiedThreshold)
            {
                if (tick >= nextBloodlustRefreshTick && CADefOf.OverwhelmingBloodlust != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory((Thought_Memory)ThoughtMaker.MakeThought(CADefOf.OverwhelmingBloodlust));
                    nextBloodlustRefreshTick = tick + 60000;
                }
            }
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
            Scribe_Values.Look(ref nextCombatScanTick, "nextCombatScanTick", 0);
            Scribe_Values.Look(ref nextCravingRefreshTick, "nextCravingRefreshTick", 0);
            Scribe_Values.Look(ref nextBloodlustRefreshTick, "nextBloodlustRefreshTick", 0);
        }
    }
}
