using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace TheMarkedMen
{
    public enum MarkedPursuitState
    {
        Idle,
        Searching,
        Hunting,
        Frenzy
    }

    public class MarkedMenAIManager : MapComponent
    {
        private const int FastTickInterval = 6;
        private const int NormalTickInterval = 60;
        private const int SlowTickInterval = 250;
        private const int RareTickInterval = 2500;
        private const int ScentCheckRadius = 20;
        private const int NoiseCheckRadius = 30;
        private const int MemoryCheckRadius = 15;
        private const float ScentInvestigateThreshold = 0.15f;
        private const float NoiseInvestigateThreshold = 0.2f;
        private const int MaxInvestigateDist = 40;
        private const int InvestigateJobExpiry = 450;
        private const int InvestigateCooldownTicks = 600;
        private const int InvestigateSearchSpread = 6;
        private const float FrenzyBloodlustThreshold = 0.9f;
        private const float TargetLostMemoryTicks = 600;
        private const int InterceptionLookAheadTicks = 120;
        private const int FlankingCheckInterval = 180;

        private int nextFastTick;
        private int nextNormalTick;
        private int nextSlowTick;
        private int nextRareTick;

        private List<Pawn> cachedInfectedPawns = new List<Pawn>();
        private int lastInfectedRefreshTick;
        private const int InfectedRefreshInterval = 120;

        private Dictionary<int, MarkedPursuitState> pursuitStates = new Dictionary<int, MarkedPursuitState>();
        private Dictionary<int, int> lastTargetSeenTick = new Dictionary<int, int>();
        private Dictionary<int, IntVec3> lastKnownTargetPos = new Dictionary<int, IntVec3>();
        private Dictionary<int, int> lastFlankTick = new Dictionary<int, int>();
        private Dictionary<int, int> lastScentInvestigation = new Dictionary<int, int>();
        private Dictionary<int, int> lastNoiseInvestigation = new Dictionary<int, int>();
        private Dictionary<int, int> lastMemoryInvestigation = new Dictionary<int, int>();

        public MarkedMenAIManager(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            int tick = Find.TickManager.TicksGame;

            if (tick >= nextFastTick)
            {
                nextFastTick = tick + FastTickInterval;
                TickFast();
            }

            if (tick >= nextNormalTick)
            {
                nextNormalTick = tick + NormalTickInterval;
                TickNormal();
            }

            if (tick >= nextSlowTick)
            {
                nextSlowTick = tick + SlowTickInterval;
                TickSlow();
            }

            if (tick >= nextRareTick)
            {
                nextRareTick = tick + RareTickInterval;
                TickRare();
            }
        }

        private void TickFast()
        {
            List<Pawn> infected = GetInfectedPawns();
            if (infected.Count == 0)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;
            for (int i = 0; i < infected.Count; i++)
            {
                Pawn pawn = infected[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Downed)
                {
                    continue;
                }

                UpdateTargetMemory(pawn, tick);
            }
        }

        private void TickNormal()
        {
            MarkedMenMemoryGrid memory = MarkedMenMemoryGrid.GetForMap(map);
            if (memory == null)
            {
                return;
            }

            List<Pawn> infected = GetInfectedPawns();
            if (infected.Count == 0)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;
            for (int i = 0; i < infected.Count; i++)
            {
                Pawn pawn = infected[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Downed || pawn.jobs == null)
                {
                    continue;
                }

                if (pawn.IsHashIntervalTick(NormalTickInterval * (2 + (i % 3))))
                {
                    TryScentInvestigation(pawn, memory, tick);
                    TryNoiseInvestigation(pawn, memory, tick);
                }

                EvaluatePursuitState(pawn, tick);
            }
        }

        private void TickSlow()
        {
            List<Pawn> infected = GetInfectedPawns();
            if (infected.Count == 0)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;
            for (int i = 0; i < infected.Count; i++)
            {
                Pawn pawn = infected[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Downed || pawn.jobs == null)
                {
                    continue;
                }

                TryMemoryInvestigation(pawn, tick);
            }
        }

        private void TickRare()
        {
            List<Pawn> infected = GetInfectedPawns();
            if (infected.Count == 0)
            {
                return;
            }

            for (int i = 0; i < infected.Count; i++)
            {
                Pawn pawn = infected[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Downed)
                {
                    continue;
                }

                ApplyBloodlustFrenzy(pawn);
            }
        }

        private List<Pawn> GetInfectedPawns()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick - lastInfectedRefreshTick > InfectedRefreshInterval)
            {
                lastInfectedRefreshTick = tick;
                cachedInfectedPawns.Clear();
                IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    if (CrossedUtility.IsInfectedPawn(allPawns[i]))
                    {
                        cachedInfectedPawns.Add(allPawns[i]);
                    }
                }
            }
            return cachedInfectedPawns;
        }

        private void TryScentInvestigation(Pawn pawn, MarkedMenMemoryGrid memory, int tick)
        {
            if (!CanTakeAIJob(pawn) || CrossedUtility.IsCrossedPyromaniac(pawn))
            {
                return;
            }

            if (HasOffensiveJob(pawn))
            {
                return;
            }

            int id = pawn.thingIDNumber;
            int lastScent = lastScentInvestigation.TryGetValue(id, out int scentVal) ? scentVal : 0;
            if (tick - lastScent < InvestigateCooldownTicks)
            {
                return;
            }

            float scent = memory.GetScentStrengthAt(pawn.Position, ScentCheckRadius);
            if (scent < ScentInvestigateThreshold)
            {
                return;
            }

            if (!memory.TryGetStrongestScentSource(pawn.Position, ScentCheckRadius * 2, out IntVec3 scentSource))
            {
                return;
            }

            int dist = scentSource.DistanceToSquared(pawn.Position);
            if (dist > MaxInvestigateDist * MaxInvestigateDist)
            {
                return;
            }

            lastScentInvestigation[id] = tick;
            StartInvestigateJob(pawn, scentSource);
        }

        private void TryNoiseInvestigation(Pawn pawn, MarkedMenMemoryGrid memory, int tick)
        {
            if (!CanTakeAIJob(pawn))
            {
                return;
            }

            if (HasOffensiveJob(pawn))
            {
                return;
            }

            int id = pawn.thingIDNumber;
            int lastNoise = lastNoiseInvestigation.TryGetValue(id, out int noiseVal) ? noiseVal : 0;
            if (tick - lastNoise < InvestigateCooldownTicks)
            {
                return;
            }

            float noise = memory.GetNoiseAt(pawn.Position, NoiseCheckRadius);
            if (noise < NoiseInvestigateThreshold)
            {
                return;
            }

            if (!memory.TryGetLoudestNoiseDirection(pawn.Position, NoiseCheckRadius * 2, out IntVec3 noiseSource))
            {
                return;
            }

            int dist = noiseSource.DistanceToSquared(pawn.Position);
            if (dist > MaxInvestigateDist * MaxInvestigateDist || dist < 25f)
            {
                return;
            }

            lastNoiseInvestigation[id] = tick;
            StartInvestigateJob(pawn, noiseSource);
        }

        private void TryMemoryInvestigation(Pawn pawn, int tick)
        {
            if (!CanTakeAIJob(pawn))
            {
                return;
            }

            int id = pawn.thingIDNumber;
            int lastMem = lastMemoryInvestigation.TryGetValue(id, out int memVal) ? memVal : 0;
            if (tick - lastMem < InvestigateCooldownTicks)
            {
                return;
            }

            MarkedMenMemoryGrid memory = MarkedMenMemoryGrid.GetForMap(map);
            if (memory == null)
            {
                return;
            }

            MemoryEvent? recent = memory.GetMostRecentMemory(pawn.Position, MemoryCheckRadius);
            if (recent == null)
            {
                return;
            }

            int age = tick - recent.Value.lastSeenTick;
            if (age > TargetLostMemoryTicks || age < 120)
            {
                return;
            }

            if (HasOffensiveJob(pawn))
            {
                return;
            }

            IntVec3 memPos = recent.Value.position;
            if (memPos.DistanceToSquared(pawn.Position) > MaxInvestigateDist * MaxInvestigateDist)
            {
                return;
            }

            lastMemoryInvestigation[id] = tick;
            StartInvestigateJob(pawn, memPos);
        }

        private static void StartInvestigateJob(Pawn pawn, IntVec3 basePos)
        {
            IntVec3 dest = GetSearchDestination(basePos, pawn.Map);
            if (!dest.IsValid)
            {
                return;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Goto, dest);
            job.expiryInterval = InvestigateJobExpiry;
            job.checkOverrideOnExpire = true;
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            job.canBashDoors = true;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private static IntVec3 GetSearchDestination(IntVec3 basePos, Map map)
        {
            for (int i = 0; i < 4; i++)
            {
                IntVec3 dest = basePos + new IntVec3(
                    Rand.RangeInclusive(-InvestigateSearchSpread, InvestigateSearchSpread),
                    0,
                    Rand.RangeInclusive(-InvestigateSearchSpread, InvestigateSearchSpread));
                if (dest.InBounds(map) && dest.Walkable(map))
                {
                    return dest;
                }
            }
            return basePos;
        }

        private void EvaluatePursuitState(Pawn pawn, int tick)
        {
            if (!CrossedUtility.IsInfectedPawn(pawn))
            {
                return;
            }

            int id = pawn.thingIDNumber;
            MarkedPursuitState current = IdleOrForID(id);

            Pawn currentTarget = FindCurrentTarget(pawn);
            if (currentTarget != null && !currentTarget.Dead && currentTarget.Spawned && !currentTarget.Downed)
            {
                lastKnownTargetPos[id] = currentTarget.Position;
                lastTargetSeenTick[id] = tick;

                if (current == MarkedPursuitState.Idle || current == MarkedPursuitState.Searching)
                {
                    current = MarkedPursuitState.Hunting;
                }
            }
            else if (current == MarkedPursuitState.Hunting)
            {
                int timeSinceTarget = tick - GetLastTargetSeenTick(id);
                if (timeSinceTarget > TargetLostMemoryTicks)
                {
                    current = MarkedPursuitState.Searching;
                }
            }

            Need_MarkedBloodlust need = pawn.needs?.TryGetNeed<Need_MarkedBloodlust>();
            if (need != null && need.CurLevel >= FrenzyBloodlustThreshold)
            {
                current = MarkedPursuitState.Frenzy;
            }

            pursuitStates[id] = current;
        }

        private void ApplyBloodlustFrenzy(Pawn pawn)
        {
            MarkedPursuitState state = IdleOrForID(pawn.thingIDNumber);
            if (state != MarkedPursuitState.Frenzy)
            {
                return;
            }

            if (!CanTakeAIJob(pawn))
            {
                return;
            }

            if (!pawn.IsHashIntervalTick(300))
            {
                return;
            }

            if (HasOffensiveJob(pawn))
            {
                return;
            }

            Pawn target = CrossedTacticalAI.FindBestNonInfectedPawnTarget(pawn);
            if (target != null)
            {
                CrossedTacticalAI.TryAssignAttackJob(pawn, target, true);
            }
        }

        private void UpdateTargetMemory(Pawn pawn, int tick)
        {
            Pawn target = FindCurrentTarget(pawn);
            if (target != null && !target.Dead && target.Spawned)
            {
                int id = pawn.thingIDNumber;
                lastKnownTargetPos[id] = target.Position;
                lastTargetSeenTick[id] = tick;

                MarkedMenMemoryGrid memory = MarkedMenMemoryGrid.GetForMap(map);
                if (memory != null)
                {
                    memory.AddMemory(target.Position, target);
                }
            }
        }

        private static Pawn FindCurrentTarget(Pawn pawn)
        {
            if (pawn?.CurJob == null)
            {
                return null;
            }

            Pawn targetA = pawn.CurJob.targetA.Pawn;
            if (targetA != null && targetA != pawn && !targetA.Dead)
            {
                return targetA;
            }

            Pawn targetB = pawn.CurJob.targetB.Pawn;
            if (targetB != null && targetB != pawn && !targetB.Dead)
            {
                return targetB;
            }

            return null;
        }

        public bool TryAssignInterceptJob(Pawn pawn, Pawn target)
        {
            if (pawn == null || target == null || !target.Spawned || target.Dead)
            {
                return false;
            }

            IntVec3 interceptPos = PredictInterceptPosition(pawn, target);
            if (!interceptPos.IsValid || !interceptPos.InBounds(map))
            {
                return false;
            }

            if (!pawn.CanReach(interceptPos, PathEndMode.OnCell, Danger.Deadly, true, true))
            {
                return false;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Goto, interceptPos);
            job.expiryInterval = 180;
            job.checkOverrideOnExpire = true;
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            job.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
            return pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public static IntVec3 PredictInterceptPosition(Pawn pawn, Pawn target)
        {
            if (pawn == null || target == null || !target.Spawned)
            {
                return IntVec3.Invalid;
            }

            IntVec3 pawnPos = pawn.Position;
            IntVec3 targetPos = target.Position;
            if (pawnPos == targetPos)
            {
                return IntVec3.Invalid;
            }

            float dist = pawnPos.DistanceTo(targetPos);
            float pawnSpeed = GetMoveSpeed(pawn);
            float targetSpeed = GetMoveSpeed(target);
            if (pawnSpeed <= 0f || targetSpeed <= 0f)
            {
                return IntVec3.Invalid;
            }

            float relativeSpeed = pawnSpeed - targetSpeed;
            if (relativeSpeed <= 0f)
            {
                return targetPos;
            }

            float timeToIntercept = dist / relativeSpeed;
            int ticksToIntercept = Mathf.RoundToInt(timeToIntercept * 60f);
            ticksToIntercept = Mathf.Clamp(ticksToIntercept, 0, InterceptionLookAheadTicks);

            Vector3 targetOffset = (targetPos - pawnPos).ToVector3Shifted().normalized * (targetSpeed * ticksToIntercept / 60f);
            IntVec3 interceptPos = targetPos + new IntVec3(
                Mathf.RoundToInt(targetOffset.x),
                0,
                Mathf.RoundToInt(targetOffset.z));

            if (interceptPos.InBounds(pawn.Map) && interceptPos.Standable(pawn.Map))
            {
                return interceptPos;
            }

            return targetPos;
        }

        public bool TryAssignFlankingJob(Pawn pawn, Pawn target)
        {
            if (pawn == null || target == null || !CanTakeAIJob(pawn))
            {
                return false;
            }

            int id = pawn.thingIDNumber;
            int tick = Find.TickManager.TicksGame;
            int lastFlank = lastFlankTick.TryGetValue(id, out int val) ? val : 0;
            if (tick - lastFlank < FlankingCheckInterval)
            {
                return false;
            }

            IntVec3 flankPos = GetFlankingPosition(pawn, target);
            if (!flankPos.IsValid || !flankPos.InBounds(map))
            {
                return false;
            }

            if (!pawn.CanReach(flankPos, PathEndMode.OnCell, Danger.Deadly, true, true))
            {
                return false;
            }

            lastFlankTick[id] = tick;
            Job job = JobMaker.MakeJob(JobDefOf.Goto, flankPos);
            job.expiryInterval = 180;
            job.checkOverrideOnExpire = true;
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            job.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
            return pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public static IntVec3 GetFlankingPosition(Pawn pawn, Pawn target)
        {
            if (pawn == null || target == null || !target.Spawned)
            {
                return IntVec3.Invalid;
            }

            IntVec3 pawnPos = pawn.Position;
            IntVec3 targetPos = target.Position;

            Vector3 toTarget = (targetPos - pawnPos).ToVector3Shifted().normalized;
            Vector3 perpendicular = new Vector3(-toTarget.z, 0f, toTarget.x);

            if (Rand.Value < 0.5f)
            {
                perpendicular = -perpendicular;
            }

            int flankDist = 4 + Rand.RangeInclusive(0, 3);
            Vector3 flankOffset = toTarget * 2f + perpendicular * flankDist;
            IntVec3 flankPos = targetPos + new IntVec3(
                Mathf.RoundToInt(flankOffset.x),
                0,
                Mathf.RoundToInt(flankOffset.z));

            if (!flankPos.InBounds(pawn.Map) || !flankPos.Standable(pawn.Map))
            {
                flankPos = targetPos + new IntVec3(
                    Mathf.RoundToInt(perpendicular.x * flankDist),
                    0,
                    Mathf.RoundToInt(perpendicular.z * flankDist));
                if (!flankPos.InBounds(pawn.Map) || !flankPos.Standable(pawn.Map))
                {
                    return IntVec3.Invalid;
                }
            }

            return flankPos;
        }

        public MarkedPursuitState GetPursuitState(Pawn pawn)
        {
            return IdleOrForID(pawn?.thingIDNumber ?? 0);
        }

        public IntVec3? GetLastKnownTargetPosition(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            if (lastKnownTargetPos.TryGetValue(pawn.thingIDNumber, out IntVec3 pos))
            {
                return pos;
            }

            return null;
        }

        private MarkedPursuitState IdleOrForID(int id)
        {
            return pursuitStates.TryGetValue(id, out MarkedPursuitState state) ? state : MarkedPursuitState.Idle;
        }

        private int GetLastTargetSeenTick(int id)
        {
            return lastTargetSeenTick.TryGetValue(id, out int tick) ? tick : 0;
        }

        private static bool CanTakeAIJob(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && !pawn.Dead
                && !pawn.Downed
                && pawn.jobs != null
                && pawn.mindState != null
                && CrossedUtility.IsInfectedPawn(pawn);
        }

        private static bool HasOffensiveJob(Pawn pawn)
        {
            if (pawn?.CurJob?.def == null)
            {
                return false;
            }

            JobDef def = pawn.CurJob.def;
            return def == JobDefOf.AttackMelee
                || def == JobDefOf.AttackStatic
                || def == JobDefOf.CastAbilityOnThing
                || def == JobDefOf.Wait_MaintainPosture;
        }

        private static float GetMoveSpeed(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.capacities == null)
            {
                return 4.5f;
            }

            float moving = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving);
            float baseSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
            return baseSpeed * moving;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextFastTick, "nextFastTick", 0);
            Scribe_Values.Look(ref nextNormalTick, "nextNormalTick", 0);
            Scribe_Values.Look(ref nextSlowTick, "nextSlowTick", 0);
            Scribe_Values.Look(ref nextRareTick, "nextRareTick", 0);
        }

        public static MarkedMenAIManager GetForMap(Map map)
        {
            if (map == null)
            {
                return null;
            }

            MarkedMenAIManager comp = map.GetComponent<MarkedMenAIManager>();
            if (comp == null)
            {
                comp = new MarkedMenAIManager(map);
                map.components.Add(comp);
            }
            return comp;
        }
    }
}
