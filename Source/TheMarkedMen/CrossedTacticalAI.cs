using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    public static class CrossedTacticalAI
    {
        private const int TacticalJobExpiryTicks = 90;
        private const int TacticalMoveExpiryTicks = 180;
        private const int RangedCastSearchMaxRegions = 80;
        private const float MaxTacticalTargetDistance = 120f;
        private const float MaxTacticalTargetDistanceSquared = MaxTacticalTargetDistance * MaxTacticalTargetDistance;
        private const float AggressionScoreMultiplier = 100f;
        private const float PartialInfectionTargetBonus = 10000f;
        private const int InfightingCheckInterval = 1000;
        private const float InfightingChance = 0.12f;
        private const float MaxInfightingTargetDistanceSquared = 2500f;
        private const string WaitDownedJobDefName = "Wait_Downed";

        private static readonly string[] InfrastructureDefNames = { "Battery", "Generator", "Solar", "Geothermal", "Power", "Comms", "Console" };
        private static readonly string[] MedicalDefNames = { "Hospital", "Bed", "Research", "Lab", "Scanner" };
        private static readonly string[] FoodDefNames = { "Nutrient", "Hydroponics", "Cooler", "Freezer", "Food" };
        private static readonly string[] DefensiveDefNames = { "Turret", "Mortar" };
        private static readonly string[] DoorDefNames = { "Door", "Wall", "Gate" };

        public static bool TryIssueTacticalJob(Pawn pawn)
        {
            if (!CanUseTacticalAI(pawn))
            {
                return false;
            }

            if (HasRampageHediff(pawn))
            {
                return TryIssueRampageJob(pawn);
            }

            bool pyromaniac = CrossedUtility.IsCrossedPyromaniac(pawn);
            if (pyromaniac)
            {
                CrossedUtility.EnsureCrossedPyromaniacMolotov(pawn);
            }

            if (!pyromaniac && TheMarkedMenRjwCompatibility.TryStartBestInfectedIntercourseJob(pawn, true))
            {
                return true;
            }

            JobDef currentJobDef = pawn.CurJob?.def;
            Pawn currentPawnTarget = pawn.CurJob?.targetA.Pawn;
            if (currentPawnTarget != null && CrossedUtility.IsFullyTurnedMarkedPawn(currentPawnTarget) && !IsAttackJob(currentJobDef))
            {
                return TryRetargetAwayFromPawn(pawn, currentPawnTarget, true);
            }

            if (IsAttackJob(currentJobDef) && IsValidNonInfectedPawnTarget(currentPawnTarget, pawn) && !ShouldForceRangedAttack(pawn, currentJobDef))
            {
                return false;
            }

            if (IsTacticalRangedMoveJob(pawn.CurJob, currentPawnTarget) && IsValidNonInfectedPawnTarget(currentPawnTarget, pawn))
            {
                return false;
            }

            if (TryStartInfighting(pawn, currentJobDef, currentPawnTarget))
            {
                return true;
            }

            Pawn bestNonInfected = FindBestNonInfectedPawnTarget(pawn);
            if (bestNonInfected != null)
            {
                bool isAttackJob = IsAttackJob(currentJobDef);
                if (isAttackJob && currentPawnTarget == bestNonInfected && IsValidNonInfectedPawnTarget(currentPawnTarget, pawn) && !ShouldForceRangedAttack(pawn, currentJobDef))
                {
                    return false;
                }

                if (IsTacticalRangedMoveJob(pawn.CurJob, bestNonInfected) && IsValidNonInfectedPawnTarget(bestNonInfected, pawn))
                {
                    return false;
                }

                return TryAssignAttackJob(pawn, bestNonInfected, true);
            }

            if (!pawn.IsHashIntervalTick(TheMarkedMenSettings.TacticalRetargetIntervalTicks) || IsAttackJob(currentJobDef))
            {
                return false;
            }

            if (!TheMarkedMenSettings.PriorityTargetingEnabled && !TheMarkedMenSettings.DoorTargetingEnabled)
            {
                return false;
            }

            Thing target = FindPriorityTarget(pawn);
            if (target == null)
            {
                return false;
            }

            return TryAssignAttackJob(pawn, target);
        }

        public static bool TryAttackNearestNonInfectedPawn(Pawn pawn, bool forceCurrentJob, bool allowRjwJob = true)
        {
            if (!CanUseTacticalAI(pawn))
            {
                return false;
            }

            if (HasRampageHediff(pawn))
            {
                return TryIssueRampageJob(pawn);
            }

            bool pyromaniac = CrossedUtility.IsCrossedPyromaniac(pawn);
            if (pyromaniac)
            {
                CrossedUtility.EnsureCrossedPyromaniacMolotov(pawn);
            }

            if (!pyromaniac && allowRjwJob && TheMarkedMenRjwCompatibility.TryStartBestInfectedIntercourseJob(pawn, forceCurrentJob))
            {
                return true;
            }

            Pawn target = FindBestNonInfectedPawnTarget(pawn);
            if (target == null)
            {
                return false;
            }

            JobDef currentJobDef = pawn.CurJob?.def;
            if (!forceCurrentJob && IsAttackJob(currentJobDef) && pawn.CurJob?.targetA.Pawn == target && !ShouldForceRangedAttack(pawn, currentJobDef))
            {
                return false;
            }

            if (!forceCurrentJob && IsTacticalRangedMoveJob(pawn.CurJob, target))
            {
                return false;
            }

            bool shouldForceCurrentJob = forceCurrentJob || !IsAttackJob(currentJobDef) || pawn.CurJob?.targetA.Pawn != target;
            return TryAssignAttackJob(pawn, target, shouldForceCurrentJob);
        }

        public static bool TryRetargetAwayFromPawn(Pawn pawn, Pawn invalidTarget, bool forceEndCurrentJob)
        {
            if (!CanUseTacticalAI(pawn) || invalidTarget == null || !CrossedUtility.IsFullyTurnedMarkedPawn(invalidTarget))
            {
                return false;
            }

            bool currentJobTargetsInvalidPawn = pawn.CurJob?.targetA.Pawn == invalidTarget || pawn.CurJob?.targetB.Pawn == invalidTarget || pawn.CurJob?.targetC.Pawn == invalidTarget;
            if (!forceEndCurrentJob && !currentJobTargetsInvalidPawn)
            {
                return false;
            }

            if (currentJobTargetsInvalidPawn && pawn.jobs?.curJob != null)
            {
                if (!CanSafelyInterruptCurrentJob(pawn))
                {
                    return false;
                }

                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false, true);
            }

            Pawn nearest = FindBestNonInfectedPawnTarget(pawn);
            return nearest != null && TryAssignAttackJob(pawn, nearest, true);
        }

        private static bool TryStartInfighting(Pawn pawn, JobDef currentJobDef, Pawn currentPawnTarget)
        {
            if (!pawn.IsHashIntervalTick(TheMarkedMenSettings.InfightingCheckIntervalTicks) || !Rand.Chance(TheMarkedMenSettings.InfightingChance))
            {
                return false;
            }

            if (IsAttackJob(currentJobDef) && IsValidInfightingTarget(currentPawnTarget, pawn))
            {
                return false;
            }

            Pawn target = FindBestInfightingTarget(pawn);
            return target != null && TryAssignAttackJob(pawn, target, true);
        }

        internal static bool TryAssignAttackJob(Pawn pawn, Thing target, bool forceCurrentJob = false)
        {
            if (pawn?.jobs == null || target == null || target.Destroyed)
            {
                return false;
            }

            Verb rangedVerb = GetRangedVerb(pawn);
            if (rangedVerb != null)
            {
                return TryAssignRangedAttackJob(pawn, target, rangedVerb, forceCurrentJob);
            }

            if (TryAssignAbilityAttackJob(pawn, target, forceCurrentJob))
            {
                return true;
            }

            if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly, true, true))
            {
                return false;
            }

            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.expiryInterval = TacticalJobExpiryTicks;
            job.checkOverrideOnExpire = true;
            job.killIncappedTarget = !(target is Pawn attackPawnTarget && TheMarkedMenRjwCompatibility.ShouldKeepIncapacitatedTargetForIntercourse(pawn, attackPawnTarget));
            job.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
            job.canBashFences = true;
            job.attackDoorIfTargetLost = TheMarkedMenSettings.DoorTargetingEnabled;
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            return TryTakeTacticalJob(pawn, job, forceCurrentJob);
        }

        private static bool TryAssignRangedAttackJob(Pawn pawn, Thing target, Verb verb, bool forceCurrentJob)
        {
            if (verb.CanHitTargetFrom(pawn.Position, target))
            {
                Job attackJob = JobMaker.MakeJob(JobDefOf.AttackStatic, target);
                attackJob.verbToUse = verb;
                attackJob.expiryInterval = TacticalJobExpiryTicks;
                attackJob.checkOverrideOnExpire = true;
                attackJob.killIncappedTarget = !(target is Pawn attackPawnTarget && TheMarkedMenRjwCompatibility.ShouldKeepIncapacitatedTargetForIntercourse(pawn, attackPawnTarget));
                attackJob.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
                attackJob.canBashFences = true;
                attackJob.attackDoorIfTargetLost = TheMarkedMenSettings.DoorTargetingEnabled;
                attackJob.locomotionUrgency = LocomotionUrgency.Sprint;
                return TryTakeTacticalJob(pawn, attackJob, forceCurrentJob);
            }

            float dist = pawn.Position.DistanceTo(target.Position);
            float range = verb.verbProps.range;
            if (dist <= range * 1.5f || dist <= 12f)
            {
                Job closeJob = JobMaker.MakeJob(JobDefOf.Goto, target.Position, target);
                closeJob.expiryInterval = 30;
                closeJob.checkOverrideOnExpire = true;
                closeJob.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
                closeJob.canBashFences = true;
                closeJob.attackDoorIfTargetLost = TheMarkedMenSettings.DoorTargetingEnabled;
                closeJob.locomotionUrgency = LocomotionUrgency.Sprint;
                return TryTakeTacticalJob(pawn, closeJob, forceCurrentJob);
            }

            if (!TryFindRangedCastPosition(pawn, target, verb, out IntVec3 castPosition))
            {
                return false;
            }

            Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, castPosition, target);
            moveJob.expiryInterval = TacticalMoveExpiryTicks;
            moveJob.checkOverrideOnExpire = true;
            moveJob.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
            moveJob.canBashFences = true;
            moveJob.attackDoorIfTargetLost = TheMarkedMenSettings.DoorTargetingEnabled;
            moveJob.locomotionUrgency = LocomotionUrgency.Sprint;
            return TryTakeTacticalJob(pawn, moveJob, forceCurrentJob);
        }

        private static bool TryAssignAbilityAttackJob(Pawn pawn, Thing target, bool forceCurrentJob)
        {
            if (pawn.abilities == null)
            {
                return false;
            }

            foreach (Ability ability in pawn.abilities.AllAbilitiesForReading)
            {
                if (ability == null || ability.verb == null || ability.verb.IsMeleeAttack || !ability.CanCast)
                {
                    continue;
                }

                if (ability.def.defName != "AcidSpray")
                {
                    continue;
                }

                if (ability.verb.CanHitTargetFrom(pawn.Position, target))
                {
                    Job job = ability.GetJob(target, target);
                    if (job == null)
                    {
                        continue;
                    }

                    job.expiryInterval = TacticalJobExpiryTicks;
                    job.checkOverrideOnExpire = true;
                    job.killIncappedTarget = !(target is Pawn attackPawnTarget && TheMarkedMenRjwCompatibility.ShouldKeepIncapacitatedTargetForIntercourse(pawn, attackPawnTarget));
                    job.canBashDoors = TheMarkedMenSettings.DoorTargetingEnabled;
                    job.canBashFences = true;
                    job.attackDoorIfTargetLost = TheMarkedMenSettings.DoorTargetingEnabled;
                    job.locomotionUrgency = LocomotionUrgency.Sprint;
                    return TryTakeTacticalJob(pawn, job, forceCurrentJob);
                }
            }

            return false;
        }

        private static bool TryTakeTacticalJob(Pawn pawn, Job job, bool forceCurrentJob)
        {
            if (pawn?.jobs == null || job == null)
            {
                return false;
            }

            if (forceCurrentJob && pawn.jobs.curJob != null)
            {
                if (!CanSafelyInterruptCurrentJob(pawn))
                {
                    return false;
                }

                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false, true);
            }

            return pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
        }

        private static bool TryFindRangedCastPosition(Pawn pawn, Thing target, Verb verb, out IntVec3 castPosition)
        {
            castPosition = IntVec3.Invalid;
            if (pawn?.Map == null || target == null || target.Destroyed || verb == null)
            {
                return false;
            }

            CastPositionRequest request = new CastPositionRequest
            {
                caster = pawn,
                target = target,
                verb = verb,
                maxRangeFromCaster = MaxTacticalTargetDistance,
                maxRangeFromTarget = Mathf.Max(verb.EffectiveRange, 1f),
                wantCoverFromTarget = false,
                maxRegions = RangedCastSearchMaxRegions,
                validator = cell => IsValidRangedCastPosition(pawn, target, verb, cell)
            };

            return CastPositionFinder.TryFindCastPosition(request, out castPosition) && castPosition.IsValid;
        }

        private static bool IsValidRangedCastPosition(Pawn pawn, Thing target, Verb verb, IntVec3 cell)
        {
            Map map = pawn?.Map;
            return map != null
                && target != null
                && !target.Destroyed
                && cell.IsValid
                && cell != pawn.Position
                && cell.InBounds(map)
                && !cell.Fogged(map)
                && cell.Standable(map)
                && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly, true, true)
                && verb.CanHitTargetFrom(cell, target);
        }

        private static Verb GetRangedVerb(Pawn pawn)
        {
            if (CrossedUtility.IsCrossedPyromaniac(pawn))
            {
                CrossedUtility.EnsureCrossedPyromaniacMolotov(pawn);
            }

            Verb verb = pawn?.equipment?.PrimaryEq?.PrimaryVerb;
            return verb != null && !verb.IsMeleeAttack ? verb : null;
        }

        private static bool ShouldForceRangedAttack(Pawn pawn, JobDef currentJobDef)
        {
            return CrossedUtility.IsCrossedPyromaniac(pawn)
                && currentJobDef != JobDefOf.AttackStatic
                && GetRangedVerb(pawn) != null;
        }

        private static bool IsTacticalRangedMoveJob(Job job, Thing target)
        {
            return job?.def == JobDefOf.Goto
                && target != null
                && job.targetB.Thing == target;
        }

        internal static Pawn FindBestNonInfectedPawnTarget(Pawn pawn)
        {
            IReadOnlyList<Pawn> candidates = pawn.Map?.mapPawns?.AllPawnsSpawned;
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            if (!pawn.IsHashIntervalTick(TheMarkedMenSettings.TacticalRetargetIntervalTicks))
            {
                return null;
            }

            Pawn best = null;
            float bestScore = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn candidate = candidates[i];
                float score = ScorePawnTarget(pawn, candidate);
                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static Pawn FindBestInfightingTarget(Pawn pawn)
        {
            IReadOnlyList<Pawn> candidates = pawn.Map?.mapPawns?.AllPawnsSpawned;
            if (candidates == null)
            {
                return null;
            }

            Pawn best = null;
            float bestScore = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn candidate = candidates[i];
                float score = ScoreInfightingTarget(pawn, candidate);
                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static Thing FindPriorityTarget(Pawn pawn)
        {
            Map map = pawn.Map;
            Thing best = null;
            float bestScore = 0f;
            IntVec3 pos = pawn.Position;
            float maxDistSq = MaxTacticalTargetDistanceSquared;

            IReadOnlyList<Pawn> vulnerablePawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < vulnerablePawns.Count; i++)
            {
                Pawn candidate = vulnerablePawns[i];
                if (!candidate.Spawned || candidate.Position.DistanceToSquared(pos) > maxDistSq)
                {
                    continue;
                }

                float score = ScorePawnTarget(pawn, candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building candidate = buildings[i];
                if (!candidate.Spawned || candidate.Position.DistanceToSquared(pos) > maxDistSq)
                {
                    continue;
                }

                float score = ScoreBuildingTarget(pawn, candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static float ScorePawnTarget(Pawn searcher, Pawn target)
        {
            if (!IsValidNonInfectedPawnTarget(target, searcher))
            {
                return 0f;
            }

            float distanceSquared = searcher.Position.DistanceToSquared(target.Position);
            if (distanceSquared > MaxTacticalTargetDistanceSquared)
            {
                return 0f;
            }

            float score = 95f;
            if (CrossedUtility.IsPartiallyMarkedPawn(target))
            {
                score += PartialInfectionTargetBonus;
            }

            if (target.Downed)
            {
                score += 90f;
            }

            if (target.health?.hediffSet != null)
            {
                score += Mathf.Clamp(target.health.hediffSet.PainTotal * 75f, 0f, 65f);
                score += Mathf.Clamp(target.health.hediffSet.BleedRateTotal * 25f, 0f, 55f);
            }

            if (target.health?.capacities != null)
            {
                float moving = target.health.capacities.GetLevel(PawnCapacityDefOf.Moving);
                if (moving < 0.65f)
                {
                    score += Mathf.Lerp(60f, 0f, Mathf.Clamp01(moving / 0.65f));
                }
            }

            SkillRecord medicine = target.skills?.GetSkill(SkillDefOf.Medicine);
            if (TheMarkedMenSettings.PriorityTargetingEnabled && medicine != null && medicine.Level >= 8)
            {
                score += 45f;
            }

            if (IsIsolatedTarget(searcher, target))
            {
                score += 35f;
            }

            if (target.Faction == Faction.OfPlayer || target.HostFaction == Faction.OfPlayer || target.IsColonistPlayerControlled)
            {
                score += 20f;
            }

            Need_MarkedBloodlust bloodlustNeed = searcher.needs?.TryGetNeed<Need_MarkedBloodlust>();
            if (bloodlustNeed != null)
            {
                score += bloodlustNeed.CurLevel * 15f;
            }

            return score * AggressionScoreMultiplier - Mathf.Sqrt(distanceSquared) * 1.15f;
        }

        private static bool IsIsolatedTarget(Pawn searcher, Pawn target)
        {
            Map map = searcher.Map;
            if (map == null)
            {
                return false;
            }

            const float allyRadiusSquared = 64f;
            int cellRadius = Mathf.CeilToInt(Mathf.Sqrt(allyRadiusSquared));
            CellRect cells = CellRect.CenteredOn(target.Position, cellRadius);
            cells.ClipInsideMap(map);

            for (int ci = cells.minZ; ci <= cells.maxZ; ci++)
            {
                for (int cj = cells.minX; cj <= cells.maxX; cj++)
                {
                    IntVec3 cell = new IntVec3(cj, 0, ci);
                    List<Thing> things = map.thingGrid.ThingsListAt(cell);
                    for (int t = 0; t < things.Count; t++)
                    {
                        Pawn other = things[t] as Pawn;
                        if (other == null || other == target || other.Dead || other.Downed || CrossedUtility.IsInfectedPawn(other))
                        {
                            continue;
                        }

                        if (other.RaceProps == null || !other.RaceProps.Humanlike)
                        {
                            continue;
                        }

                        bool allied = other.Faction == target.Faction || other.HostFaction == target.Faction || target.HostFaction == other.Faction;
                        if (allied && other.Position.DistanceToSquared(target.Position) <= allyRadiusSquared)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool IsValidNonInfectedPawnTarget(Pawn target, Pawn searcher)
        {
            return target != null
                && target != searcher
                && target.Spawned
                && !target.Dead
                && target.RaceProps != null
                && target.RaceProps.Humanlike
                && !CrossedUtility.IsFullyTurnedMarkedPawn(target);
        }

        private static float ScoreInfightingTarget(Pawn searcher, Pawn target)
        {
            if (!IsValidInfightingTarget(target, searcher))
            {
                return 0f;
            }

            float distanceSquared = searcher.Position.DistanceToSquared(target.Position);
            if (distanceSquared > MaxInfightingTargetDistanceSquared)
            {
                return 0f;
            }

            float score = 200f;
            if (CrossedUtility.IsPartiallyMarkedPawn(target))
            {
                score += 400f;
            }

            if (target.health?.hediffSet != null)
            {
                score += Mathf.Clamp(target.health.hediffSet.PainTotal * 45f, 0f, 40f);
                score += Mathf.Clamp(target.health.hediffSet.BleedRateTotal * 20f, 0f, 35f);
            }

            return score - Mathf.Sqrt(distanceSquared) * 1.5f;
        }

        private static bool IsValidInfightingTarget(Pawn target, Pawn searcher)
        {
            return target != null
                && target != searcher
                && target.Spawned
                && !target.Dead
                && !target.Downed
                && target.Map == searcher.Map
                && target.RaceProps != null
                && target.RaceProps.Humanlike
                && CrossedUtility.IsInfectedPawn(target);
        }

        private static bool CanUseTacticalAI(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && !pawn.Dead
                && !pawn.Downed
                && pawn.Map != null
                && CrossedUtility.IsInfectedPawn(pawn)
                && TheMarkedMenSettings.TacticalRetargetingEnabled
                && !TheMarkedMenRjwCompatibility.ShouldPreserveCurrentRjwJob(pawn);
        }

        private static bool IsAttackJob(JobDef jobDef)
        {
            return jobDef == JobDefOf.AttackMelee
                || jobDef == JobDefOf.AttackStatic
                || jobDef == JobDefOf.CastAbilityOnThing;
        }

        private static bool CanSafelyInterruptCurrentJob(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.jobs == null)
            {
                return false;
            }

            if (IsRecoveryWaitJob(pawn.CurJob) || IsRecoveryWaitJob(pawn.jobs.curDriver?.job))
            {
                return false;
            }

            return pawn.jobs.curJob == null || pawn.jobs.curDriver != null;
        }

        private static bool IsRecoveryWaitJob(Job job)
        {
            return string.Equals(job?.def?.defName, WaitDownedJobDefName, StringComparison.Ordinal);
        }

        private static float ScoreBuildingTarget(Pawn searcher, Building target)
        {
            if (target == null || target.Destroyed || target.Faction != Faction.OfPlayer)
            {
                return 0f;
            }

            float distanceSquared = searcher.Position.DistanceToSquared(target.Position);
            if (distanceSquared > MaxTacticalTargetDistanceSquared)
            {
                return 0f;
            }

            string defName = target.def?.defName ?? string.Empty;
            string label = target.Label ?? string.Empty;
            float score = 0f;

            if (TheMarkedMenSettings.PriorityTargetingEnabled && target.TryGetComp<CompPowerTrader>() != null)
            {
                score += 90f;
            }

            if (TheMarkedMenSettings.PriorityTargetingEnabled && ContainsAny(defName, InfrastructureDefNames))
            {
                score += 70f;
            }

            if (TheMarkedMenSettings.PriorityTargetingEnabled && ContainsAny(defName, MedicalDefNames))
            {
                score += 60f;
            }

            if (TheMarkedMenSettings.PriorityTargetingEnabled && (ContainsAny(defName, FoodDefNames) || label.IndexOf("food", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                score += 45f;
            }

            if (TheMarkedMenSettings.PriorityTargetingEnabled && ContainsAny(defName, DefensiveDefNames))
            {
                score += 55f;
            }

            if (TheMarkedMenSettings.DoorTargetingEnabled && ContainsAny(defName, DoorDefNames))
            {
                score += 30f;
            }

            if (score <= 0f)
            {
                return 0f;
            }

            return score - Mathf.Sqrt(distanceSquared) * 0.75f;
        }

        private static bool ContainsAny(string value, string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
            {
                if (value.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasRampageHediff(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.HasHediff(CADefOf.CrossedRampage) == true;
        }

        private static bool TryIssueRampageJob(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Map == null) return false;

            Pawn best = null;
            float bestDist = float.MaxValue;
            IntVec3 pos = pawn.Position;
            Map map = pawn.Map;

            IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn candidate = allPawns[i];
                if (candidate == pawn || candidate.Dead || candidate.Downed) continue;
                if (candidate.RaceProps == null || !candidate.RaceProps.Humanlike) continue;

                float dist = candidate.Position.DistanceToSquared(pos);
                if (dist >= bestDist) continue;

                best = candidate;
                bestDist = dist;
            }

            if (best == null) return false;

            return TryAssignAttackJob(pawn, best, true);
        }
    }
}
