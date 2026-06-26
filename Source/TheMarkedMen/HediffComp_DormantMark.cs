using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    internal static class DormantMarkLog
    {
        internal static void Verbose(string message)
        {
            if (TheMarkedMenMod.Settings?.verboseCompatibilityLogging == true)
            {
                Log.Message(message);
            }
        }
    }

    public class Hediff_DormantMark : HediffWithComps
    {
        public override bool Visible => false;
    }

    public class HediffCompProperties_DormantMark : HediffCompProperties
    {
        public float dormantMinDays = 8f;
        public float dormantMaxDays = 30f;
        public float triggerOnDamageThreshold = 0.15f;
        public float triggerOnWitnessChance = 0.10f;
        public float triggerOnNearDeathChance = 0.40f;
        public float triggerOnSignalChance = 0.25f;
        public float socialHediffRadius = 12f;
        public int socialHediffDurationTicks = 60000;
        public float alphaChance = 0.10f;

        public HediffCompProperties_DormantMark()
        {
            compClass = typeof(HediffComp_DormantMark);
        }
    }

    public class HediffComp_DormantMark : HediffComp
    {
        private int activationTick = -1;
        private bool activated;
        private bool triggerLoggedThisTick;
        private int nextSocialTick;

        public HediffCompProperties_DormantMark Props => (HediffCompProperties_DormantMark)props;

        public bool IsActivated => activated;

        public int TicksUntilActivation
        {
            get
            {
                if (activationTick < 0 || activated) return 0;
                return Math.Max(0, activationTick - Find.TickManager.TicksGame);
            }
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (!parent.pawn.IsColonist) return;
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            float minDays = settings?.dormantMarkMinDays ?? Props.dormantMinDays;
            float maxDays = settings?.dormantMarkMaxDays ?? Props.dormantMaxDays;
            minDays *= settings?.dormantMarkTriggerMultiplier ?? 1f;
            maxDays *= settings?.dormantMarkTriggerMultiplier ?? 1f;
            float days = Rand.Range(minDays, maxDays);
            activationTick = Find.TickManager.TicksGame + (int)(days * GenDate.TicksPerDay);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (activated) return;
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned) return;

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.lostSurvivorEnabled) return;

            int tick = Find.TickManager.TicksGame;
            triggerLoggedThisTick = false;

            if (activationTick > 0 && tick >= activationTick)
            {
                AttemptTransformation("dormancy timer expired");
                return;
            }

            if (!pawn.IsHashIntervalTick(2500)) return;

            CheckTransformationTriggers(pawn, settings);
            ApplyAuraAnxiety(pawn, tick);
        }

        private void CheckTransformationTriggers(Pawn pawn, TheMarkedMenSettings settings)
        {
            float sensitivity = settings?.dormantMarkTriggerMultiplier ?? 1f;

            Hediff bloodLoss = pawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
            if (bloodLoss != null && bloodLoss.Severity >= Props.triggerOnDamageThreshold * sensitivity)
            {
                if (Rand.Chance(Props.triggerOnNearDeathChance * sensitivity))
                {
                    AttemptTransformation("critical blood loss");
                    return;
                }
            }

            HediffDef plagueDef = DefDatabase<HediffDef>.GetNamedSilentFail("Plague");
            Hediff plague = plagueDef != null ? pawn.health?.hediffSet?.GetFirstHediffOfDef(plagueDef) : null;
            if (plague != null)
            {
                if (Rand.Chance(Props.triggerOnNearDeathChance * sensitivity * 0.5f))
                {
                    AttemptTransformation("severe illness");
                }
            }
        }

        private void ApplyAuraAnxiety(Pawn pawn, int tick)
        {
            if (tick < nextSocialTick) return;
            nextSocialTick = tick + 6000;

            if (!pawn.Spawned || pawn.Map == null) return;

            List<Pawn> colonists = pawn.Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn other = colonists[i];
                if (other == pawn || other.Dead || other.needs?.mood == null) continue;
                if (other.Position.DistanceToSquared(pawn.Position) > Props.socialHediffRadius * Props.socialHediffRadius) continue;

                Hediff hediff = HediffMaker.MakeHediff(CADefOf.Panic, other);
                other.health.AddHediff(hediff);
            }
        }

        private static bool cascadeInProgress;

        public void AttemptTransformation(string trigger = "unknown")
        {
            if (activated) return;
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Dead) return;

            activated = true;

            Map map = pawn.Spawned ? pawn.Map : null;

            if (map != null)
            {
                for (int i = 0; i < map.mapPawns.FreeColonistsSpawned.Count; i++)
                {
                    Pawn witness = map.mapPawns.FreeColonistsSpawned[i];
                    if (witness == pawn || witness.Dead || witness.needs?.mood == null) continue;

                    Thought_Memory thought = ThoughtMaker.MakeThought(CADefOf.CA_WitnessedCrossedTransformation) as Thought_Memory;
                    if (thought != null)
                    {
                        witness.needs.mood.thoughts.memories.TryGainMemory(thought);
                    }

                    if (RelationsUtility.PawnsKnowEachOther(witness, pawn))
                    {
                        Thought_MemorySocial social = ThoughtMaker.MakeThought(CADefOf.CrossedSocialTerror) as Thought_MemorySocial;
                        if (social != null)
                        {
                            witness.needs.mood.thoughts.memories.TryGainMemory(social, pawn);
                        }
                    }
                }
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            bool isAlpha = settings != null && Rand.Chance(settings.dormantMarkAlphaChance * (pawn.IsPrisonerOfColony ? 2f : 1f));
            bool isGroup = settings != null && Rand.Chance(settings.dormantMarkGroupVariantChance);

            if (isAlpha && map != null)
            {
                TrySpawnEscorts(pawn);
            }

            pawn.health.RemoveHediff(parent);

            CrossedUtility.TransformPawn(pawn, suppressNotification: false, infector: null);

            ApplyRampageHediff(pawn);

            if (map != null)
            {
                string letterLabel = "CA_LostSurvivor_Betrayal_Label".Translate();
                string letterText = "CA_LostSurvivor_Betrayal_Text".Translate(pawn.Named("PAWN")).Resolve();
                Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.ThreatBig, new LookTargets(pawn));
            }

            DormantMarkLog.Verbose($"[TheMarkedMen] Dormant Mark activated on {pawn.LabelShort}: {trigger}");

            if (!cascadeInProgress && map != null)
            {
                CascadeAwakenings(map);
            }
        }

        private static void CascadeAwakenings(Map map)
        {
            cascadeInProgress = true;
            try
            {
                IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    Pawn other = allPawns[i];
                    if (other == null || other.Dead || other.health == null) continue;

                    Hediff dormant = other.health.hediffSet.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
                    if (dormant == null) continue;

                    HediffComp_DormantMark comp = dormant.TryGetComp<HediffComp_DormantMark>();
                    if (comp == null || comp.activated) continue;

                    comp.AttemptTransformation("cascade: another dormant carrier awakened nearby");
                }
            }
            finally
            {
                cascadeInProgress = false;
            }
        }

        private static void ApplyRampageHediff(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null) return;
            if (pawn.health.hediffSet.HasHediff(CADefOf.CrossedRampage)) return;

            Hediff rampage = HediffMaker.MakeHediff(CADefOf.CrossedRampage, pawn);
            pawn.health.AddHediff(rampage);
        }

        private void TrySpawnEscorts(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Map == null) return;
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (crossed == null) return;

            int count = Rand.RangeInclusive(1, 3);
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = CADefOf.CrossedCivilian ?? PawnKindDefOf.SpaceRefugee;
                Pawn escort = PawnGenerator.GeneratePawn(kind, crossed);
                if (escort == null) continue;

                IntVec3 spot = CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 5);
                if (spot.IsValid && spot.Standable(pawn.Map))
                {
                    GenSpawn.Spawn(escort, spot, pawn.Map, Rot4.Random);
                    CrossedUtility.ApplyClassHediffs(escort);
                    CrossedUtility.ApplyInfectedTattoo(escort);
                }
            }
        }

        public void NotifyDamaged(float damageFraction)
        {
            if (activated || triggerLoggedThisTick) return;
            triggerLoggedThisTick = true;
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            float threshold = Props.triggerOnDamageThreshold * (settings?.dormantMarkTriggerMultiplier ?? 1f);
            float chance = damageFraction >= threshold
                ? Props.triggerOnNearDeathChance * (settings?.dormantMarkTriggerMultiplier ?? 1f) * 0.5f
                : 0f;
            if (chance > 0f && Rand.Chance(chance))
            {
                AttemptTransformation("combat damage");
            }
        }

        public void NotifyWitnessedTransformation()
        {
            if (activated || triggerLoggedThisTick) return;
            triggerLoggedThisTick = true;
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            float chance = Props.triggerOnWitnessChance * (settings?.dormantMarkTriggerMultiplier ?? 1f);
            if (Rand.Chance(chance))
            {
                AttemptTransformation("witnessed another transformation");
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref activationTick, "activationTick", -1);
            Scribe_Values.Look(ref activated, "activated", false);
        }
    }
}
