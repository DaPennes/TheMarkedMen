using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    public static class TheMarkedMenRjwCompatibility
    {
        private const string RjwPackageId = "rim.job.world";
        private const string RjwSexUtilityTypeName = "rjw.SexUtility";
        private const string RjwSexDriverBaseTypeName = "rjw.JobDriver_Sex";
        private const string RjwUtilityTypeName = "rjw.xxx";
        private const string RjwRapeEnemyJobDefName = "RapeEnemy";
        private const string RjwReceiverRapedJobDefName = "GettinRaped";
        private const float MinimumAdultAgeYears = 18f;
        private const float InfectedForcedIntercourseChance = 0.75f;
        private const int DefaultRjwMaxPartnersPerTarget = 6;
        private const int ForcedIntercourseRetryCooldownTicks = 300;
        private const string WaitDownedJobDefName = "Wait_Downed";

        private static bool patchAttempted;
        private static bool patchApplied;
        private static bool rjwIntercourseWarningLogged;
        private static readonly Dictionary<int, int> nextForcedIntercourseAttemptTickByPawnId = new Dictionary<int, int>();
        private static FieldInfo sexPropsPawnField;
        private static FieldInfo sexPropsPartnerField;
        private static JobDef rjwRapeEnemyJobDef;
        private static JobDef rjwReceiverRapedJobDef;
        private static MethodInfo rjwCanRapeMethod;
        private static MethodInfo rjwCanFuckMethod;
        private static MethodInfo rjwCanBeFuckedMethod;
        private static FieldInfo rjwMaxPartnersPerTargetField;

        public static bool Active => patchApplied && TheMarkedMenMod.Settings?.rjwIntegrationEnabled == true;

        private static bool IntercourseJobsEnabled => TheMarkedMenMod.Settings?.rjwIntegrationEnabled == true && IsRjwLoaded();

        public static void Apply(Harmony harmony)
        {
            if (harmony == null || patchAttempted)
            {
                return;
            }

            patchAttempted = true;
            try
            {
                Type sexUtilityType = AccessTools.TypeByName(RjwSexUtilityTypeName);
                if (sexUtilityType == null)
                {
                    return;
                }

                PatchRjwRapeTargetAlert(harmony, sexUtilityType);

                MethodInfo processSex = AccessTools.Method(sexUtilityType, "ProcessSex");
                if (processSex == null)
                {
                    Log.Warning("[The Marked Men] RJW bridge skipped: rjw.SexUtility.ProcessSex was not found.");
                    return;
                }

                ParameterInfo[] parameters = processSex.GetParameters();
                if (parameters.Length != 1)
                {
                    Log.Warning("[The Marked Men] RJW bridge skipped: unexpected ProcessSex signature.");
                    return;
                }

                TheMarkedMenMod.Settings?.AutoEnableRjwIntegrationIfInstalled();
                CacheSexPropsFields(parameters[0].ParameterType);
                if (sexPropsPawnField == null || sexPropsPartnerField == null)
                {
                    Log.Warning("[The Marked Men] RJW bridge skipped: expected SexProps pawn fields were not found.");
                    return;
                }

                MethodInfo postfix = AccessTools.Method(typeof(TheMarkedMenRjwCompatibility), nameof(Postfix_RjwProcessSex));
                if (postfix == null)
                {
                    Log.Warning("[The Marked Men] RJW bridge skipped: compatibility postfix was not found.");
                    return;
                }

                harmony.Patch(processSex, postfix: new HarmonyMethod(postfix));
                patchApplied = true;
                LogVerbose("[The Marked Men] RJW bridge active.");
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] RJW bridge skipped: " + ex.Message);
            }
        }

        public static bool ShouldPreserveCurrentRjwJob(Pawn pawn)
        {
            if (!IntercourseJobsEnabled || pawn?.jobs?.curDriver == null)
            {
                return false;
            }

            JobDriver driver = pawn.jobs.curDriver;
            if (!IsRjwEncounterDriver(driver))
            {
                return false;
            }

            Pawn partner = GetRjwDriverPartner(driver);
            return IsAdultHumanlike(pawn) && IsAdultHumanlike(partner);
        }

        public static bool TryStartBestInfectedIntercourseJob(Pawn pawn, bool forceCurrentJob)
        {
            if (!CanUseInfectedIntercourseActor(pawn))
            {
                return false;
            }

            if (!CanAttemptForcedIntercourseNow(pawn))
            {
                return false;
            }

            IReadOnlyList<Pawn> candidates = pawn.Map?.mapPawns?.AllPawnsSpawned;
            if (candidates == null)
            {
                return false;
            }

            Pawn bestTarget = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn candidate = candidates[i];
                if (!CanStartInfectedIntercourseJob(pawn, candidate))
                {
                    continue;
                }

                float score = ScoreInfectedIntercourseTarget(pawn, candidate);
                if (score > bestScore)
                {
                    bestTarget = candidate;
                    bestScore = score;
                }
            }

            if (bestTarget == null || !Rand.Chance(InfectedForcedIntercourseChance))
            {
                return false;
            }

            return TryStartInfectedIntercourseJob(pawn, bestTarget, forceCurrentJob);
        }

        public static bool ShouldKeepIncapacitatedTargetForIntercourse(Pawn pawn, Pawn target)
        {
            return CanUseInfectedIntercourseActor(pawn)
                && IsAdultHumanlike(target)
                && target.Spawned
                && !target.Downed
                && !HasRecoveryWaitJob(target)
                && pawn.Map == target.Map
                && !CrossedUtility.IsFullyTurnedMarkedPawn(target);
        }

        public static bool TryStartInfectedIntercourseJob(Pawn pawn, Pawn target, bool forceCurrentJob)
        {
            if (!CanStartInfectedIntercourseJob(pawn, target) || !CanAttemptForcedIntercourseNow(pawn))
            {
                return false;
            }

            try
            {
                if (forceCurrentJob && !CanSafelyInterruptForForcedIntercourse(pawn))
                {
                    ThrottleForcedIntercourse(pawn);
                    return false;
                }

                if (pawn.CurJob?.def == RjwRapeEnemyJobDef && pawn.CurJob.targetA.Pawn == target)
                {
                    ThrottleForcedIntercourse(pawn);
                    return true;
                }

                if (forceCurrentJob && pawn.jobs?.curJob != null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false, true);
                }

                Job job = JobMaker.MakeJob(RjwRapeEnemyJobDef, target);
                job.checkOverrideOnExpire = true;
                job.canBashDoors = true;
                job.attackDoorIfTargetLost = true;
                job.locomotionUrgency = LocomotionUrgency.Sprint;
                ThrottleForcedIntercourse(pawn);
                return pawn.jobs != null && pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
            }
            catch (Exception ex)
            {
                ThrottleForcedIntercourse(pawn);
                LogRjwIntercourseWarning("RJW infected intercourse job failed: " + ex.Message);
                return false;
            }
        }

        public static bool Prefix_RjwRapeTargetAlert(Pawn rapist, Pawn target)
        {
            return !ShouldSuppressRjwRapeTargetAlert(rapist, target);
        }

        public static void Postfix_RjwProcessSex(object __0)
        {
            if (!Active)
            {
                return;
            }

            try
            {
                NotifyRjwProcessSex(__0);
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] RJW bridge exposure handling failed: " + ex.Message);
            }
        }

        private static void PatchRjwRapeTargetAlert(Harmony harmony, Type sexUtilityType)
        {
            try
            {
                MethodInfo target = AccessTools.Method(sexUtilityType, "RapeTargetAlert", new[] { typeof(Pawn), typeof(Pawn) });
                MethodInfo prefix = AccessTools.Method(typeof(TheMarkedMenRjwCompatibility), nameof(Prefix_RjwRapeTargetAlert));
                if (target != null && prefix != null)
                {
                    harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                    LogVerbose("[The Marked Men] RJW forced-intercourse alert suppression active.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] RJW forced-intercourse alert suppression skipped: " + ex.Message);
            }
        }

        private static bool ShouldSuppressRjwRapeTargetAlert(Pawn rapist, Pawn target)
        {
            return IntercourseJobsEnabled
                && IsAdultHumanlike(rapist)
                && IsAdultHumanlike(target)
                && CrossedUtility.IsInfectedPawn(rapist);
        }

        private static void CacheSexPropsFields(Type sexPropsType)
        {
            if (sexPropsType == null)
            {
                return;
            }

            sexPropsPawnField = AccessTools.Field(sexPropsType, "pawn");
            sexPropsPartnerField = AccessTools.Field(sexPropsType, "partner");
        }

        private static void NotifyRjwProcessSex(object sexProps)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.infectionEnabled || !settings.rjwIntegrationEnabled || sexProps == null)
            {
                return;
            }

            Pawn pawn = GetPawnField(sexProps, sexPropsPawnField);
            Pawn partner = GetPawnField(sexProps, sexPropsPartnerField);
            if (!IsAdultHumanlike(pawn) || !IsAdultHumanlike(partner))
            {
                return;
            }

            bool pawnInfected = CrossedUtility.IsInfectedPawn(pawn);
            bool partnerInfected = CrossedUtility.IsInfectedPawn(partner);
            if (pawnInfected == partnerInfected)
            {
                return;
            }

            float chance = Mathf.Clamp01(settings.rjwExposureChance);
            string source = "RJW close contact";
            if (pawnInfected)
            {
                CrossedUtility.TryExpose(partner, chance, source, pawn);
            }

            if (partnerInfected)
            {
                CrossedUtility.TryExpose(pawn, chance, source, partner);
            }
        }

        private static Pawn GetPawnField(object instance, FieldInfo field)
        {
            return field == null ? null : field.GetValue(instance) as Pawn;
        }

        private static bool CanStartInfectedIntercourseJob(Pawn pawn, Pawn target)
        {
            if (!CanUseInfectedIntercourseActor(pawn)
                || target == pawn
                || !IsAdultHumanlike(target)
                || !target.Spawned
                || target.Downed
                || HasRecoveryWaitJob(target)
                || target.Map != pawn.Map
                || IsBurning(target)
                || !CrossedUtility.IsInfectedPawn(pawn)
                || CrossedUtility.IsFullyTurnedMarkedPawn(target)
                || IsPawnInRjwEncounter(target))
            {
                return false;
            }

            if (!CanRjwBeFucked(target))
            {
                return false;
            }

            int maxPartnersPerTarget = GetRjwMaxPartnersPerTarget();
            return pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Some, maxPartnersPerTarget, 0);
        }

        private static bool CanUseInfectedIntercourseActor(Pawn pawn)
        {
            return IntercourseJobsEnabled
                && IsAdultHumanlike(pawn)
                && pawn.Spawned
                && !pawn.Downed
                && pawn.Map != null
                && !pawn.Drafted
                && !IsBurning(pawn)
                && CrossedUtility.IsInfectedPawn(pawn)
                && !IsPawnInRjwEncounter(pawn)
                && RjwRapeEnemyJobDef != null
                && CanRjwUsePawnForIntercourse(pawn);
        }

        private static float ScoreInfectedIntercourseTarget(Pawn searcher, Pawn target)
        {
            float distanceSquared = searcher.Position.DistanceToSquared(target.Position);
            float score = 1000f;
            if (CrossedUtility.IsPartiallyMarkedPawn(target))
            {
                score += 500f;
            }

            if (target.Faction == Faction.OfPlayer || target.HostFaction == Faction.OfPlayer || target.IsColonistPlayerControlled)
            {
                score += 100f;
            }

            return score - Mathf.Sqrt(distanceSquared);
        }

        private static bool CanAttemptForcedIntercourseNow(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            int tick = CurrentGameTick;
            return !nextForcedIntercourseAttemptTickByPawnId.TryGetValue(pawn.thingIDNumber, out int nextTick) || tick >= nextTick;
        }

        private static void ThrottleForcedIntercourse(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            nextForcedIntercourseAttemptTickByPawnId[pawn.thingIDNumber] = CurrentGameTick + ForcedIntercourseRetryCooldownTicks;
        }

        private static int CurrentGameTick => Find.TickManager?.TicksGame ?? 0;

        private static bool CanSafelyInterruptForForcedIntercourse(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.jobs == null)
            {
                return false;
            }

            if (HasRecoveryWaitJob(pawn))
            {
                return false;
            }

            return pawn.jobs.curJob == null || pawn.jobs.curDriver != null;
        }

        private static bool HasRecoveryWaitJob(Pawn pawn)
        {
            return IsRecoveryWaitJob(pawn?.CurJob) || IsRecoveryWaitJob(pawn?.jobs?.curDriver?.job);
        }

        private static bool IsRecoveryWaitJob(Job job)
        {
            return string.Equals(job?.def?.defName, WaitDownedJobDefName, StringComparison.Ordinal);
        }

        private static bool IsBurning(Thing thing)
        {
            return thing?.GetAttachment(ThingDefOf.Fire) != null;
        }

        private static bool IsAdultHumanlike(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && pawn.RaceProps != null
                && pawn.RaceProps.Humanlike
                && pawn.ageTracker != null
                && pawn.ageTracker.AgeBiologicalYearsFloat >= MinimumAdultAgeYears;
        }

        private static bool IsPawnInRjwEncounter(Pawn pawn)
        {
            JobDriver driver = pawn?.jobs?.curDriver;
            if (driver != null && IsRjwEncounterDriver(driver))
            {
                return true;
            }

            JobDef jobDef = pawn?.CurJob?.def;
            return jobDef == RjwRapeEnemyJobDef || jobDef == RjwReceiverRapedJobDef;
        }

        private static bool IsRjwEncounterDriver(JobDriver driver)
        {
            Type type = driver?.GetType();
            while (type != null)
            {
                string name = type.FullName;
                if (name == RjwSexDriverBaseTypeName)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static Pawn GetRjwDriverPartner(JobDriver driver)
        {
            if (driver == null)
            {
                return null;
            }

            try
            {
                PropertyInfo partnerProperty = AccessTools.Property(driver.GetType(), "Partner");
                Pawn partner = partnerProperty?.GetValue(driver, null) as Pawn;
                if (partner != null)
                {
                    return partner;
                }
            }
            catch
            {
            }

            return driver.job?.GetTarget(TargetIndex.A).Pawn;
        }

        private static JobDef RjwRapeEnemyJobDef
        {
            get
            {
                if (rjwRapeEnemyJobDef == null)
                {
                    rjwRapeEnemyJobDef = DefDatabase<JobDef>.GetNamedSilentFail(RjwRapeEnemyJobDefName);
                }

                return rjwRapeEnemyJobDef;
            }
        }

        private static JobDef RjwReceiverRapedJobDef
        {
            get
            {
                if (rjwReceiverRapedJobDef == null)
                {
                    rjwReceiverRapedJobDef = DefDatabase<JobDef>.GetNamedSilentFail(RjwReceiverRapedJobDefName);
                }

                return rjwReceiverRapedJobDef;
            }
        }

        private static bool CanRjwRape(Pawn pawn)
        {
            MethodInfo method = RjwCanRapeMethod;
            if (method == null)
            {
                return false;
            }

            try
            {
                return method.Invoke(null, new object[] { pawn, true }) is bool result && result;
            }
            catch (Exception ex)
            {
                LogRjwIntercourseWarning("RJW can_rape check failed: " + ex.Message);
                return false;
            }
        }

        private static bool CanRjwUsePawnForIntercourse(Pawn pawn)
        {
            return CanRjwRape(pawn) || CanRjwFuck(pawn) || CanRjwBeFucked(pawn);
        }

        private static bool CanRjwFuck(Pawn pawn)
        {
            MethodInfo method = RjwCanFuckMethod;
            if (method == null)
            {
                return false;
            }

            try
            {
                return method.Invoke(null, new object[] { pawn }) is bool result && result;
            }
            catch (Exception ex)
            {
                LogRjwIntercourseWarning("RJW can_fuck check failed: " + ex.Message);
                return false;
            }
        }

        private static bool CanRjwBeFucked(Pawn pawn)
        {
            MethodInfo method = RjwCanBeFuckedMethod;
            if (method == null)
            {
                return false;
            }

            try
            {
                return method.Invoke(null, new object[] { pawn }) is bool result && result;
            }
            catch (Exception ex)
            {
                LogRjwIntercourseWarning("RJW can_be_fucked check failed: " + ex.Message);
                return false;
            }
        }

        private static MethodInfo RjwCanRapeMethod
        {
            get
            {
                if (rjwCanRapeMethod == null)
                {
                    Type utilityType = AccessTools.TypeByName(RjwUtilityTypeName);
                    rjwCanRapeMethod = AccessTools.Method(utilityType, "can_rape", new[] { typeof(Pawn), typeof(bool) });
                }

                return rjwCanRapeMethod;
            }
        }

        private static MethodInfo RjwCanFuckMethod
        {
            get
            {
                if (rjwCanFuckMethod == null)
                {
                    Type utilityType = AccessTools.TypeByName(RjwUtilityTypeName);
                    rjwCanFuckMethod = AccessTools.Method(utilityType, "can_fuck", new[] { typeof(Pawn) });
                }

                return rjwCanFuckMethod;
            }
        }

        private static MethodInfo RjwCanBeFuckedMethod
        {
            get
            {
                if (rjwCanBeFuckedMethod == null)
                {
                    Type utilityType = AccessTools.TypeByName(RjwUtilityTypeName);
                    rjwCanBeFuckedMethod = AccessTools.Method(utilityType, "can_be_fucked", new[] { typeof(Pawn) });
                }

                return rjwCanBeFuckedMethod;
            }
        }

        private static int GetRjwMaxPartnersPerTarget()
        {
            try
            {
                if (rjwMaxPartnersPerTargetField == null)
                {
                    Type utilityType = AccessTools.TypeByName(RjwUtilityTypeName);
                    rjwMaxPartnersPerTargetField = AccessTools.Field(utilityType, "max_rapists_per_prisoner");
                }

                object value = rjwMaxPartnersPerTargetField?.GetValue(null);
                if (value is int maxPartners && maxPartners > 0)
                {
                    return maxPartners;
                }
            }
            catch (Exception ex)
            {
                LogRjwIntercourseWarning("RJW reservation limit lookup failed: " + ex.Message);
            }

            return DefaultRjwMaxPartnersPerTarget;
        }

        private static bool? rjwLoadedCache;

        public static bool IsRjwLoaded()
        {
            if (rjwLoadedCache.HasValue)
            {
                return rjwLoadedCache.Value;
            }

            if (AccessTools.TypeByName(RjwSexUtilityTypeName) != null)
            {
                rjwLoadedCache = true;
                return true;
            }

            try
            {
                bool found = false;
                foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
                {
                    if (string.Equals(mod.PackageIdPlayerFacing, RjwPackageId, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                rjwLoadedCache = found;
                return found;
            }
            catch
            {
                rjwLoadedCache = false;
                return false;
            }
        }

        private static void LogVerbose(string message)
        {
            if (TheMarkedMenMod.Settings?.verboseCompatibilityLogging == true)
            {
                Log.Message(message);
            }
        }

        private static void LogRjwIntercourseWarning(string message)
        {
            if (rjwIntercourseWarningLogged)
            {
                return;
            }

            rjwIntercourseWarningLogged = true;
            Log.Warning("[The Marked Men] " + message);
        }
    }
}
