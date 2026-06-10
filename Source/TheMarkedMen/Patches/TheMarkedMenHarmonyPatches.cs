using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace TheMarkedMen
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    public static class Patch_BloodExposure
    {
        private const float BloodExposureRadiusSquared = 25f;

        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null || totalDamageDealt <= 0f)
            {
                return;
            }

            Pawn instigatorPawn = dinfo.Instigator as Pawn;
            TryExposeInstigatorToInfectedBlood(__instance, instigatorPawn);
            TryExposeVictimToInfectedAssault(__instance, instigatorPawn);
            TryExposeVictimToSpitterAcid(__instance, instigatorPawn);
        }

        private static void TryExposeInstigatorToInfectedBlood(Pawn damagedPawn, Pawn instigatorPawn)
        {
            if (!CrossedUtility.IsInfectedPawn(damagedPawn))
            {
                return;
            }

            if (instigatorPawn == null || instigatorPawn == damagedPawn || instigatorPawn.Dead || instigatorPawn.RaceProps == null || !instigatorPawn.RaceProps.Humanlike)
            {
                return;
            }

            if (CrossedUtility.IsInfectedPawn(instigatorPawn) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(instigatorPawn))
            {
                return;
            }

            if (damagedPawn.Spawned && instigatorPawn.Spawned && damagedPawn.Map == instigatorPawn.Map && damagedPawn.Position.DistanceToSquared(instigatorPawn.Position) > BloodExposureRadiusSquared)
            {
                return;
            }

            float chance = TheMarkedMenMod.Settings?.bloodExposureChance ?? TheMarkedMenSettings.InfectionTransmissionChance;
            CrossedUtility.TryExpose(instigatorPawn, chance, "infected blood exposure", damagedPawn);
        }

        private static void TryExposeVictimToInfectedAssault(Pawn damagedPawn, Pawn instigatorPawn)
        {
            if (damagedPawn == null || instigatorPawn == null || damagedPawn == instigatorPawn || !CrossedUtility.IsInfectedPawn(instigatorPawn))
            {
                return;
            }

            if (damagedPawn.Dead || damagedPawn.RaceProps == null || !damagedPawn.RaceProps.Humanlike)
            {
                return;
            }

            if (CrossedUtility.IsInfectedPawn(damagedPawn) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(damagedPawn))
            {
                return;
            }

            CrossedUtility.TryExpose(damagedPawn, TheMarkedMenSettings.InfectedAssaultExposureChance, "infected assault contact", instigatorPawn);
        }

        private static void TryExposeVictimToSpitterAcid(Pawn damagedPawn, Pawn instigatorPawn)
        {
            if (damagedPawn == null || instigatorPawn == null || damagedPawn == instigatorPawn)
            {
                return;
            }

            if (instigatorPawn.health?.hediffSet?.HasHediff(CADefOf.SpitterGlands) != true)
            {
                return;
            }

            if (damagedPawn.Dead || damagedPawn.RaceProps == null || !damagedPawn.RaceProps.Humanlike)
            {
                return;
            }

            if (CrossedUtility.IsInfectedPawn(damagedPawn) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(damagedPawn))
            {
                return;
            }

            CrossedUtility.TryExpose(damagedPawn, 1f, "spitter acid attack", instigatorPawn);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_InfectedDeathReanimation
    {
        public static void Prefix(Pawn __instance)
        {
            if (__instance == null) return;

            Hediff bomberCharge = __instance.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.BomberCharge);
            if (bomberCharge != null)
            {
                HediffComp_DeathExplosion comp = bomberCharge.TryGetComp<HediffComp_DeathExplosion>();
                comp?.Detonate(__instance);
            }
        }

        public static void Postfix(Pawn __instance)
        {
            if (__instance == null) return;

            if (!CrossedUtility.HasMarkedVirusHediff(__instance))
            {
                return;
            }

            CrossedUtility.ApplyInfectedTattoo(__instance);
            CrossedReanimationManager.QueueCrossedReanimation(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TickRare))]
    public static class Patch_CrossedTacticalAI
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || __instance.RaceProps == null || !__instance.RaceProps.Humanlike)
            {
                return;
            }

            CrossedTacticalAI.TryIssueTacticalJob(__instance);
            if (!__instance.IsHashIntervalTick(TheMarkedMenSettings.InfectedStateMaintenanceIntervalTicks))
            {
                return;
            }

            CrossedUtility.EnsureFearlessCrossedState(__instance);
            CrossedUtility.ApplyInfectedTattooIfInfected(__instance);
            CrossedUtility.RemoveMarkedVirusHediffFromFullyTurnedPawn(__instance);
        }
    }

    [HarmonyPatch(typeof(Lord), nameof(Lord.LordTick))]
    public static class Patch_CrossedLordInvalidPawnCleanup
    {
        public static void Prefix(Lord __instance)
        {
            CrossedLordCleanupUtility.RemoveInvalidOwnedPawns(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn_StyleTracker), "set_FaceTattoo")]
    public static class Patch_InfectedFaceTattooLock
    {
        private static bool enforcing;

        public static void Postfix(Pawn_StyleTracker __instance)
        {
            if (enforcing || __instance?.pawn == null || !CrossedUtility.ShouldShowCrossedRash(__instance.pawn))
            {
                return;
            }

            TattooDef tattoo = CrossedUtility.GetCurrentCrossedFaceTattoo(__instance.pawn);
            if (tattoo == null || __instance.FaceTattoo == tattoo)
            {
                return;
            }

            enforcing = true;
            try
            {
                __instance.nextFaceTattooDef = tattoo;
                __instance.FaceTattoo = tattoo;
                __instance.Notify_StyleItemChanged();
                __instance.pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
            finally
            {
                enforcing = false;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_StyleTracker), nameof(Pawn_StyleTracker.SetupNextLookChangeData))]
    public static class Patch_InfectedPlannedFaceTattooLock
    {
        public static void Postfix(Pawn_StyleTracker __instance)
        {
            if (__instance?.pawn == null || !CrossedUtility.ShouldShowCrossedRash(__instance.pawn))
            {
                return;
            }

            TattooDef tattoo = CrossedUtility.GetCurrentCrossedFaceTattoo(__instance.pawn);
            if (tattoo != null)
            {
                __instance.nextFaceTattooDef = tattoo;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    public static class Patch_InfectedTattooOnFactionChange
    {
        private static bool warningLogged;

        public static void Postfix(Pawn __instance)
        {
            try
            {
                if (!CrossedUtility.CanSafelyProcessInfectedState(__instance))
                {
                    return;
                }

                CrossedUtility.EnsureStarterLineageResistance(__instance);
                CrossedUtility.ApplyInfectedTattooIfInfected(__instance);
            }
            catch (Exception ex)
            {
                if (!warningLogged)
                {
                    warningLogged = true;
                    Log.Warning("[The Marked Men] Faction-change infected tattoo update skipped: " + ex.Message);
                }
            }
        }
    }

    public static class CrossedOptionalHarmonyPatches
    {
        private static bool setFactionDirectWarningLogged;
        private static readonly MethodInfo CachedSetFactionDirectTarget;
        private static readonly MethodInfo CachedSetFactionDirectPostfix;

        static CrossedOptionalHarmonyPatches()
        {
            try
            {
                CachedSetFactionDirectTarget = AccessTools.Method(typeof(Thing), "SetFactionDirect", new[] { typeof(Faction) });
                CachedSetFactionDirectPostfix = AccessTools.Method(typeof(CrossedOptionalHarmonyPatches), nameof(Postfix_SetFactionDirect));
            }
            catch
            {
            }
        }

        public static void Apply(Harmony harmony)
        {
            if (harmony == null)
            {
                return;
            }

            try
            {
                if (CachedSetFactionDirectTarget != null && CachedSetFactionDirectPostfix != null)
                {
                    harmony.Patch(CachedSetFactionDirectTarget, postfix: new HarmonyMethod(CachedSetFactionDirectPostfix));
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Optional SetFactionDirect tattoo patch skipped: " + ex.Message);
            }

            TheMarkedMenWorldbuilderCompatibility.Apply(harmony);
            LongEventHandler.ExecuteWhenFinished(() => TheMarkedMenRjwCompatibility.Apply(harmony));
        }

        public static void Postfix_SetFactionDirect(Thing __instance)
        {
            try
            {
                Pawn pawn = __instance as Pawn;
                if (!CrossedUtility.CanSafelyProcessInfectedState(pawn))
                {
                    return;
                }

                CrossedUtility.ApplyInfectedTattooIfInfected(pawn);
            }
            catch (Exception ex)
            {
                if (!setFactionDirectWarningLogged)
                {
                    setFactionDirectWarningLogged = true;
                    Log.Warning("[The Marked Men] Direct faction infected tattoo update skipped: " + ex.Message);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_InfectedTattooOnSpawn
    {
        public static void Postfix(Pawn __instance)
        {
            CrossedUtility.ApplyInfectedTattooIfInfected(__instance);
        }
    }

    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.AddFoodPoisoningHediff))]
    public static class Patch_ContaminatedFoodExposure
    {
        public static void Postfix(Pawn pawn, Thing ingestible)
        {
            if (pawn == null || ingestible == null)
            {
                return;
            }

            string label = ingestible.def?.defName ?? string.Empty;
            if (label.IndexOf("Human", StringComparison.OrdinalIgnoreCase) >= 0 || label.IndexOf("Meat", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                float chance = TheMarkedMenMod.Settings?.foodExposureChance ?? TheMarkedMenSettings.InfectionTransmissionChance;
                CrossedUtility.TryExpose(pawn, chance, "contaminated food");
            }
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits")]
    public static class Patch_CrossedForcedTraits
    {
        private static readonly Dictionary<string, List<(string defName, int degree)>> KindTraits = new Dictionary<string, List<(string, int)>>
        {
            { "CA_CrossedBerserker", new List<(string, int)> { ("Bloodlust", 0) } },
            { "CA_CrossedHunter", new List<(string, int)> { ("Bloodlust", 0), ("CarefulShooter", 0) } },
            { "CA_CrossedBrute", new List<(string, int)> { ("Bloodlust", 0), ("Tough", 0) } },
            { "CA_CrossedStalker", new List<(string, int)> { ("Bloodlust", 0), ("Nimble", 0) } },
            { "CA_CrossedScreamer", new List<(string, int)> { ("Bloodlust", 0), ("Psychopath", 0) } },
            { "CA_CrossedAlpha", new List<(string, int)> { ("Bloodlust", 0), ("Tough", 0) } },
            { "CA_CrossedCharger", new List<(string, int)> { ("Bloodlust", 0), ("Brawler", 0) } },
            { "CA_CrossedSpitter", new List<(string, int)> { ("Bloodlust", 0), ("Nimble", 0) } },
            { "CA_CrossedBomber", new List<(string, int)> { ("Bloodlust", 0), ("Psychopath", 0), ("Brawler", 0) } },
            { "CA_CrossedAlphaPsychic", new List<(string, int)> { ("Bloodlust", 0), ("Tough", 0) } },
        };

        public static void Postfix(Pawn pawn)
        {
            if (pawn?.story?.traits == null || pawn.kindDef?.defName == null)
            {
                return;
            }
            if (!KindTraits.TryGetValue(pawn.kindDef.defName, out var traits))
            {
                return;
            }
            foreach (var (defName, degree) in traits)
            {
                TraitDef traitDef = DefDatabase<TraitDef>.GetNamedSilentFail(defName);
                if (traitDef == null)
                {
                    continue;
                }
                if (!pawn.story.traits.HasTrait(traitDef))
                {
                    pawn.story.traits.GainTrait(new Trait(traitDef, degree));
                }
            }
        }
    }
}
