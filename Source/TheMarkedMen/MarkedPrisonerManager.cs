using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    public class MarkedPrisonerManager : MapComponent
    {
        private const int FastTickInterval = 120;
        private const int RareTickInterval = 2500;
        private const int SelfHarmCheckInterval = GenDate.TicksPerDay;
        private const int CosmeticCheckInterval = 180;
        private const int PrisonerRefreshInterval = 250;
        private const int MinPrisonerDistanceForAttack = 4;
        private const float PoundDoorNoiseStrength = 0.35f;
        private const int PoundDoorNoiseDecay = 800;
        private const float PoundWallNoiseStrength = 0.25f;
        private const int PoundWallNoiseDecay = 600;

        private int nextFastTick;
        private int nextRareTick;
        private int nextSelfHarmTick;
        private int nextCosmeticTick;
        private int nextPrisonerRefreshTick;

        private List<Pawn> cachedMarkedPrisoners = new List<Pawn>();
        private Dictionary<int, int> imprisonmentStartTick = new Dictionary<int, int>();
        private Dictionary<int, int> lastSuccessfulAttackTick = new Dictionary<int, int>();
        private Dictionary<int, int> selfHarmStage = new Dictionary<int, int>();
        private Dictionary<int, int> nextSelfHarmStageTick = new Dictionary<int, int>();
        private Dictionary<int, int> lastCosmeticTick = new Dictionary<int, int>();

        private static readonly float[] SelfHarmStageDays =
        {
            3f, 5f, 6.5f
        };

        public MarkedPrisonerManager(Map map) : base(map)
        {
        }

        private TheMarkedMenSettings Settings => TheMarkedMenMod.Settings;

        private int nextCleanupTick;
        private const int CleanupInterval = 600;

        public override void MapComponentTick()
        {
            int tick = Find.TickManager.TicksGame;

            if (tick >= nextFastTick)
            {
                nextFastTick = tick + FastTickInterval;
                TickFast();
            }

            if (tick >= nextRareTick)
            {
                nextRareTick = tick + RareTickInterval;
                TickRare();
            }

            if (tick >= nextSelfHarmTick)
            {
                nextSelfHarmTick = tick + SelfHarmCheckInterval;
                TickSelfHarm();
            }

            if (tick >= nextCosmeticTick)
            {
                nextCosmeticTick = tick + CosmeticCheckInterval;
                TickCosmetic();
            }

            if (tick >= nextPrisonerRefreshTick)
            {
                nextPrisonerRefreshTick = tick + PrisonerRefreshInterval;
                RefreshPrisoners();
            }

            if (tick >= nextCleanupTick)
            {
                nextCleanupTick = tick + CleanupInterval;
                PruneStaleEntries();
            }
        }

        private void PruneStaleEntries()
        {
            HashSet<int> validIds = new HashSet<int>();
            for (int i = 0; i < cachedMarkedPrisoners.Count; i++)
            {
                Pawn pawn = cachedMarkedPrisoners[i];
                if (pawn != null && !pawn.Dead && pawn.Spawned && pawn.IsPrisonerOfColony)
                {
                    validIds.Add(pawn.thingIDNumber);
                }
            }

            PruneDict(imprisonmentStartTick, validIds);
            PruneDict(lastSuccessfulAttackTick, validIds);
            PruneDict(selfHarmStage, validIds);
            PruneDict(nextSelfHarmStageTick, validIds);
            PruneDict(lastCosmeticTick, validIds);
        }

        private static void PruneDict<T>(Dictionary<int, T> dict, HashSet<int> validIds)
        {
            List<int> toRemove = null;
            foreach (int key in dict.Keys)
            {
                if (!validIds.Contains(key))
                {
                    if (toRemove == null)
                    {
                        toRemove = new List<int>();
                    }
                    toRemove.Add(key);
                }
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    dict.Remove(toRemove[i]);
                }
            }
        }

        private void TickFast()
        {
            if (cachedMarkedPrisoners.Count == 0)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;
            for (int i = 0; i < cachedMarkedPrisoners.Count; i++)
            {
                Pawn pawn = cachedMarkedPrisoners[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned || !pawn.IsPrisonerOfColony)
                {
                    continue;
                }

                CheckNearbyInteraction(pawn, tick);
            }
        }

        private void TickRare()
        {
            if (cachedMarkedPrisoners.Count == 0 || Settings == null)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;
            for (int i = 0; i < cachedMarkedPrisoners.Count; i++)
            {
                Pawn pawn = cachedMarkedPrisoners[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned || !pawn.IsPrisonerOfColony)
                {
                    continue;
                }

                if (!imprisonmentStartTick.ContainsKey(pawn.thingIDNumber))
                {
                    imprisonmentStartTick[pawn.thingIDNumber] = tick;
                }

                EnsurePrisonerMindState(pawn);
                TryStartEscape(pawn, tick);
            }
        }

        private void TryStartEscape(Pawn pawn, int tick)
        {
            if (Settings == null || !Settings.prisonerInfectionEnabled)
            {
                return;
            }

            if (pawn.Downed || pawn.Dead || !pawn.Spawned)
            {
                return;
            }

            if (PrisonBreakUtility.IsPrisonBreaking(pawn))
            {
                return;
            }

            if (!PrisonBreakUtility.CanParticipateInPrisonBreak(pawn))
            {
                return;
            }

            float chancePerRareTick = Settings.prisonerEscapeChance / (60000f / RareTickInterval);
            if (!Rand.Chance(chancePerRareTick))
            {
                return;
            }

            PrisonBreakUtility.StartPrisonBreak(pawn);
        }

        private void TickSelfHarm()
        {
            if (cachedMarkedPrisoners.Count == 0 || Settings == null || !Settings.prisonerSelfHarmEnabled)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;
            for (int i = 0; i < cachedMarkedPrisoners.Count; i++)
            {
                Pawn pawn = cachedMarkedPrisoners[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned || !pawn.IsPrisonerOfColony)
                {
                    continue;
                }

                int id = pawn.thingIDNumber;
                if (!imprisonmentStartTick.TryGetValue(id, out int startTick))
                {
                    imprisonmentStartTick[id] = tick;
                    startTick = tick;
                }

                int lastAttack = lastSuccessfulAttackTick.TryGetValue(id, out int lastAtk) ? lastAtk : startTick;
                int daysSinceAttack = Mathf.FloorToInt((tick - Math.Max(startTick, lastAttack)) / (float)GenDate.TicksPerDay);

                float selfHarmDays = Settings.prisonerSelfHarmStageDays;
                float suicideDays = Settings.prisonerSelfHarmSuicideDays;

                if (daysSinceAttack >= suicideDays)
                {
                    if (selfHarmStage.TryGetValue(id, out int stage) && stage >= 3)
                    {
                        PerformSuicide(pawn);
                        continue;
                    }
                }

                if (daysSinceAttack >= SelfHarmStageDays[2] && daysSinceAttack >= SelfHarmStageDays[2])
                {
                    int stage = DetermineSelfHarmStage(daysSinceAttack, SelfHarmStageDays);
                    if (stage > 0)
                    {
                        int currentStage = selfHarmStage.TryGetValue(id, out int cur) ? cur : 0;
                        if (stage > currentStage)
                        {
                            selfHarmStage[id] = stage;
                            ApplySelfHarmDamage(pawn, stage);
                        }
                        else if (stage == currentStage && stage > 0)
                        {
                            ApplySelfHarmMaintenance(pawn, stage);
                        }
                    }
                }

                if (daysSinceAttack >= suicideDays)
                {
                    int advancedStage = DetermineSelfHarmStage(daysSinceAttack, new[] { suicideDays * 0.6f, suicideDays * 0.8f, suicideDays * 0.95f });
                    if (advancedStage >= 3)
                    {
                        selfHarmStage[id] = 3;
                        PerformSuicide(pawn);
                    }
                }
            }
        }

        private void PerformSuicide(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            string deathReason = "self-inflicted fatal trauma";
            int roll = Rand.RangeInclusive(0, 3);
            DamageDef damageDef;

            switch (roll)
            {
                case 0:
                    damageDef = DamageDefOf.Blunt;
                    deathReason = "snapped own neck with inhuman force";
                    break;
                case 1:
                    damageDef = DamageDefOf.Scratch;
                    deathReason = "tore open own throat";
                    break;
                case 2:
                    damageDef = DamageDefOf.Blunt;
                    deathReason = "repeatedly struck own head against the wall until brain death";
                    break;
                default:
                    damageDef = DamageDefOf.Cut;
                    deathReason = "ripped open own femoral artery";
                    break;
            }

            BodyPartRecord brain = pawn.health?.hediffSet?.GetBrain();
            if (brain != null && damageDef != DamageDefOf.Scratch)
            {
                pawn.TakeDamage(new DamageInfo(damageDef, 9999f, instigator: pawn, hitPart: brain));
            }
            else
            {
                pawn.TakeDamage(new DamageInfo(damageDef, 9999f, instigator: pawn));
            }

            Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.BloodLoss, pawn);
            hediff.Severity = 1f;
            pawn.health.AddHediff(hediff);

            if (!pawn.Dead)
            {
                pawn.Kill(new DamageInfo(DamageDefOf.Blunt, 9999f, instigator: pawn), null);
            }

            if (Settings != null && Settings.prisonerDebugLogging)
            {
                Log.Message($"[TheMarkedMen] Marked prisoner {pawn.LabelShort} committed suicide: {deathReason}");
            }
        }

        private static int DetermineSelfHarmStage(int daysSinceLastAttack, float[] stageDays)
        {
            if (daysSinceLastAttack >= stageDays[2]) return 3;
            if (daysSinceLastAttack >= stageDays[1]) return 2;
            if (daysSinceLastAttack >= stageDays[0]) return 1;
            return 0;
        }

        private void ApplySelfHarmDamage(Pawn pawn, int stage)
        {
            if (pawn == null || pawn.Dead || pawn.health == null)
            {
                return;
            }

            switch (stage)
            {
                case 1:
                    ApplyBruises(pawn);
                    break;
                case 2:
                    ApplyBruises(pawn);
                    ApplyBleedingWounds(pawn);
                    TryRemoveDigit(pawn);
                    break;
                case 3:
                    ApplyBruises(pawn);
                    ApplyBleedingWounds(pawn);
                    ApplyOrganDamage(pawn);
                    break;
            }
        }

        private void ApplySelfHarmMaintenance(Pawn pawn, int stage)
        {
            if (pawn == null || pawn.Dead || pawn.health == null)
            {
                return;
            }

            if (stage >= 2 && Rand.Chance(0.15f))
            {
                ApplyBleedingWounds(pawn);
            }
            if (stage >= 3 && Rand.Chance(0.10f))
            {
                ApplyOrganDamage(pawn);
            }
        }

        private void ApplyBruises(Pawn pawn)
        {
            for (int i = 0; i < 3; i++)
            {
                BodyPartRecord part = pawn.RaceProps.body.AllParts.RandomElement();
                if (part != null && !pawn.health.hediffSet.PartIsMissing(part))
                {
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Rand.Range(2f, 5f), 0.5f, -1f, pawn, part));
                }
            }
        }

        private void ApplyBleedingWounds(Pawn pawn)
        {
            for (int i = 0; i < 2; i++)
            {
                BodyPartRecord part = pawn.RaceProps.body.AllParts.RandomElement();
                if (part != null && !pawn.health.hediffSet.PartIsMissing(part))
                {
                    Hediff cut = HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, part);
                    cut.Severity = 0.1f;
                    pawn.health.AddHediff(cut);

                    Hediff bloodLoss = HediffMaker.MakeHediff(HediffDefOf.BloodLoss, pawn);
                    bloodLoss.Severity = Mathf.Min(1f, (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss)?.Severity ?? 0f) + 0.08f);
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss) != null)
                    {
                        pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss).Severity = bloodLoss.Severity;
                    }
                    else
                    {
                        pawn.health.AddHediff(bloodLoss);
                    }
                }
            }
        }

        private void TryRemoveDigit(Pawn pawn)
        {
            if (!Rand.Chance(0.25f))
            {
                return;
            }

            BodyPartRecord hand = pawn.RaceProps.body.AllParts.Find(p => p.def == BodyPartDefOf.Hand);
            if (hand == null || pawn.health.hediffSet.PartIsMissing(hand))
            {
                return;
            }

            List<BodyPartRecord> fingers = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
                .Where(p => p.parent == hand && p.def.defName.Contains("Finger")).ToList();
            if (fingers.Count > 0)
            {
                BodyPartRecord finger = fingers.RandomElement();
                pawn.TakeDamage(new DamageInfo(DamageDefOf.Cut, 20f, instigator: pawn, hitPart: finger));
            }
        }

        private void ApplyOrganDamage(Pawn pawn)
        {
            List<BodyPartRecord> vitalParts = pawn.RaceProps.body.AllParts
                .Where(p => p.def == BodyPartDefOf.Heart || p.def == BodyPartDefOf.Lung || p.def.defName.Contains("Stomach") || p.def.defName.Contains("Kidney") || p.def.defName.Contains("Liver")).ToList();
            if (vitalParts.Count > 0)
            {
                BodyPartRecord part = vitalParts.RandomElement();
                if (!pawn.health.hediffSet.PartIsMissing(part))
                {
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Rand.Range(8f, 15f), 1f, -1f, pawn, part));
                }
            }
        }

        private void CheckNearbyInteraction(Pawn pawn, int tick)
        {
            if (Settings == null || !Settings.prisonerInfectionEnabled)
            {
                return;
            }

            int id = pawn.thingIDNumber;
            int lastCosmetic = lastCosmeticTick.TryGetValue(id, out int lastCos) ? lastCos : 0;
            if (tick - lastCosmetic < 120)
            {
                return;
            }

            IntVec3 pos = pawn.Position;
            Map pawnMap = pawn.Map;
            if (pawnMap == null)
            {
                return;
            }

            float closestDistSq = MinPrisonerDistanceForAttack * MinPrisonerDistanceForAttack;

            IReadOnlyList<Pawn> allPawns = pawnMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn other = allPawns[i];
                if (other == pawn || other.Dead || other.Downed || !other.RaceProps.Humanlike)
                {
                    continue;
                }

                if (!other.IsColonist && !other.IsPrisonerOfColony && !other.IsSlaveOfColony && !other.IsFreeColonist)
                {
                    continue;
                }

                if (other.IsPrisonerOfColony && !CrossedUtility.IsCrossedPawn(other))
                {
                    continue;
                }

                float distSq = pos.DistanceToSquared(other.Position);
                if (distSq > 100f)
                {
                    continue;
                }

                if (distSq <= closestDistSq)
                {
                    lastCosmeticTick[id] = tick;
                    TryWardenAttack(pawn, other, tick);
                    return;
                }
            }
        }

        private void TryWardenAttack(Pawn prisoner, Pawn target, int tick)
        {
            if (Settings == null || !Settings.prisonerInfectionEnabled)
            {
                return;
            }

            float chance = Settings.prisonerInfectionChance;
            if (!Rand.Chance(chance))
            {
                return;
            }

            if (target.Downed || target.Dead)
            {
                return;
            }

            string attackType = PickAttackType();
            DamageInfo dinfo = CreateAttackDamage(prisoner, target, attackType);

            if (dinfo.Def != null)
            {
                target.TakeDamage(dinfo);
                lastSuccessfulAttackTick[prisoner.thingIDNumber] = tick;

                float infectionChance = CrossedDamageUtility.GetInfectChanceForAttack(dinfo, target);
                CrossedUtility.TryExpose(target, infectionChance, "marked prisoner attack", prisoner);

                if (dinfo.Def == DamageDefOf.Bite)
                {
                    Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.Bite, target);
                    target.health.AddHediff(hediff);
                }

                MoteMaker.ThrowText(target.DrawPos, target.Map, "Attacked!", Color.red, 2f);

                if (Settings.prisonerDebugLogging)
                {
                    Log.Message($"[TheMarkedMen] Marked prisoner {prisoner.LabelShort} attacked {target.LabelShort} with {attackType}");
                }
            }
        }

        private static string PickAttackType()
        {
            float roll = Rand.Value;
            if (roll < 0.30f) return "bite";
            if (roll < 0.55f) return "scratch";
            if (roll < 0.75f) return "grab";
            if (roll < 0.90f) return "spit";
            return "struggle";
        }

        private static DamageInfo CreateAttackDamage(Pawn prisoner, Pawn target, string attackType)
        {
            BodyPartRecord hitPart = target.RaceProps.body.AllParts.RandomElement();
            float severity;

            switch (attackType)
            {
                case "bite":
                    severity = Rand.Range(8f, 18f);
                    return new DamageInfo(DamageDefOf.Bite, severity, 2f, -1f, prisoner, hitPart);
                case "scratch":
                    severity = Rand.Range(4f, 10f);
                    return new DamageInfo(DamageDefOf.Scratch, severity, 1f, -1f, prisoner, hitPart);
                case "grab":
                    severity = Rand.Range(3f, 8f);
                    DamageInfo grabDinfo = new DamageInfo(DamageDefOf.Blunt, severity, 1f, -1f, prisoner, hitPart);
                    return grabDinfo;
                case "spit":
                    severity = Rand.Range(2f, 5f);
                    return new DamageInfo(DamageDefOf.Blunt, severity, 0.5f, -1f, prisoner, hitPart);
                case "struggle":
                    severity = Rand.Range(5f, 12f);
                    return new DamageInfo(DamageDefOf.Blunt, severity, 1.5f, -1f, prisoner, hitPart);
                default:
                    return new DamageInfo(DamageDefOf.Blunt, 5f, 1f, -1f, prisoner, hitPart);
            }
        }

        private void TickCosmetic()
        {
            if (cachedMarkedPrisoners.Count == 0 || Settings == null || !Settings.prisonerCosmeticEnabled)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;
            for (int i = 0; i < cachedMarkedPrisoners.Count; i++)
            {
                Pawn pawn = cachedMarkedPrisoners[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned || !pawn.IsPrisonerOfColony || pawn.Downed)
                {
                    continue;
                }

                int id = pawn.thingIDNumber;
                int lastCos = lastCosmeticTick.TryGetValue(id, out int val) ? val : 0;
                if (tick - lastCos < CosmeticCheckInterval)
                {
                    continue;
                }

                lastCosmeticTick[id] = tick;

                if (Rand.Chance(0.20f))
                {
                    DoPacingBehavior(pawn);
                }
                else if (Rand.Chance(0.15f))
                {
                    DoVocalization(pawn);
                }
                else if (Rand.Chance(0.10f))
                {
                    DoStructurePound(pawn);
                }
            }
        }

        private void DoPacingBehavior(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Map == null || pawn.Downed)
            {
                return;
            }

            IntVec3 dest;
            if (CellFinder.TryFindRandomCellNear(pawn.Position, pawn.Map, 3, c =>
                c.InBounds(pawn.Map) && c.Walkable(pawn.Map) && c != pawn.Position && !c.IsForbidden(pawn), out dest))
            {
                Job job = JobMaker.MakeJob(JobDefOf.Goto, dest);
                job.expiryInterval = 120;
                job.locomotionUrgency = LocomotionUrgency.Walk;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }

        private void DoVocalization(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Map == null)
            {
                return;
            }

            string text;
            float roll = Rand.Value;
            if (roll < 0.35f)
            {
                text = "Grrr...";
            }
            else if (roll < 0.60f)
            {
                text = "*hysterical laughter*";
            }
            else if (roll < 0.80f)
            {
                text = "*scream of rage*";
            }
            else
            {
                text = "*guttural growl*";
            }

            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, text, Color.gray, 2f);

            MarkedMenMemoryGrid memory = MarkedMenMemoryGrid.GetForMap(pawn.Map);
            memory?.AddNoise(pawn.Position, 0.3f, 500);
        }

        private void DoStructurePound(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Map == null)
            {
                return;
            }

            Building door = pawn.Position.GetEdifice(pawn.Map);
            if (door is Building_Door)
            {
                if (door.Position.DistanceToSquared(pawn.Position) <= 9f)
                {
                    MoteMaker.ThrowText(door.DrawPos, pawn.Map, "*bang*", Color.gray, 1.5f);
                    MarkedMenMemoryGrid memory = MarkedMenMemoryGrid.GetForMap(pawn.Map);
                    memory?.AddNoise(door.Position, PoundDoorNoiseStrength, PoundDoorNoiseDecay);
                }
            }
            else
            {
                IntVec3 wallPos = pawn.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(1, 4)];
                if (wallPos.InBounds(pawn.Map))
                {
                    MoteMaker.ThrowText(wallPos.ToVector3Shifted(), pawn.Map, "*thud*", Color.gray, 1.5f);
                    MarkedMenMemoryGrid memory = MarkedMenMemoryGrid.GetForMap(pawn.Map);
                    memory?.AddNoise(wallPos, PoundWallNoiseStrength, PoundWallNoiseDecay);
                }
            }
        }

        private static void EnsurePrisonerMindState(Pawn pawn)
        {
            if (pawn == null || pawn.mindState == null)
            {
                return;
            }

            if (!CrossedUtility.IsCrossedPawn(pawn))
            {
                return;
            }

            Pawn_MindState mindState = pawn.mindState;
            mindState.canFleeIndividual = false;
            mindState.exitMapAfterTick = -1;

            MentalStateHandler handler = mindState.mentalStateHandler;
            if (handler != null)
            {
                handler.neverFleeIndividual = true;
            }
        }

        private void RefreshPrisoners()
        {
            cachedMarkedPrisoners.Clear();
            IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            int tick = Find.TickManager.TicksGame;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned)
                {
                    continue;
                }

                if (pawn.IsPrisonerOfColony && CrossedUtility.IsCrossedPawn(pawn))
                {
                    cachedMarkedPrisoners.Add(pawn);
                    if (!imprisonmentStartTick.ContainsKey(pawn.thingIDNumber))
                    {
                        imprisonmentStartTick[pawn.thingIDNumber] = tick;
                    }
                }
            }
        }

        public void NotifyPrisonerAttacked(Pawn prisoner, Pawn target, int tick)
        {
            if (prisoner != null)
            {
                lastSuccessfulAttackTick[prisoner.thingIDNumber] = tick;
            }
        }

        public static MarkedPrisonerManager GetForMap(Map map)
        {
            if (map == null)
            {
                return null;
            }

            MarkedPrisonerManager comp = map.GetComponent<MarkedPrisonerManager>();
            if (comp == null)
            {
                comp = new MarkedPrisonerManager(map);
                map.components.Add(comp);
            }
            return comp;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextFastTick, "nextFastTick", 0);
            Scribe_Values.Look(ref nextRareTick, "nextRareTick", 0);
            Scribe_Values.Look(ref nextSelfHarmTick, "nextSelfHarmTick", 0);
            Scribe_Values.Look(ref nextCosmeticTick, "nextCosmeticTick", 0);
            Scribe_Values.Look(ref nextPrisonerRefreshTick, "nextPrisonerRefreshTick", 0);
            Scribe_Values.Look(ref nextCleanupTick, "nextCleanupTick", 0);

            Scribe_Collections.Look(ref imprisonmentStartTick, "imprisonmentStartTick", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref lastSuccessfulAttackTick, "lastSuccessfulAttackTick", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref selfHarmStage, "selfHarmStage", LookMode.Value, LookMode.Value);

            if (imprisonmentStartTick == null)
            {
                imprisonmentStartTick = new Dictionary<int, int>();
            }
            if (lastSuccessfulAttackTick == null)
            {
                lastSuccessfulAttackTick = new Dictionary<int, int>();
            }
            if (selfHarmStage == null)
            {
                selfHarmStage = new Dictionary<int, int>();
            }
        }
    }
}
