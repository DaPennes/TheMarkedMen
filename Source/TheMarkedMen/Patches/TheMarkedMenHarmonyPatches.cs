using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
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

            if (TheMarkedMenMod.Settings != null && !TheMarkedMenMod.Settings.infectionEnabled)
            {
                return;
            }

            MarkedMenMemoryGrid memory = __instance.Spawned ? MarkedMenMemoryGrid.GetForMap(__instance.Map) : null;

            bool victimInfected = CrossedUtility.IsInfectedPawn(__instance);

            if (victimInfected && memory != null)
            {
                float scentStrength = Mathf.Clamp(totalDamageDealt / 50f, 0.05f, 1f);
                memory.AddScent(__instance.Position, scentStrength, __instance);
            }

            if (memory != null && dinfo.Def != null && dinfo.Def.isRanged)
            {
                memory.AddNoise(__instance.Position, 0.7f, 1500);
            }

            if (memory != null && dinfo.Def != null && dinfo.Def.isExplosive)
            {
                memory.AddNoise(__instance.Position, 1f, 2500);
            }

            if (!CrossedDamageUtility.IsValidMeleeInfectionSource(dinfo))
            {
                return;
            }

            Pawn instigatorPawn = dinfo.Instigator as Pawn;
            TryExposeInstigatorToInfectedBlood(__instance, instigatorPawn, victimInfected);
            TryExposeVictimToInfectedAssault(__instance, instigatorPawn, dinfo);
        }

        private static void TryExposeInstigatorToInfectedBlood(Pawn damagedPawn, Pawn instigatorPawn, bool victimInfected)
        {
            if (!victimInfected)
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

        private static void TryExposeVictimToInfectedAssault(Pawn damagedPawn, Pawn instigatorPawn, DamageInfo dinfo)
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

            float chance = CrossedDamageUtility.GetInfectChanceForAttack(dinfo, damagedPawn);
            CrossedUtility.TryExpose(damagedPawn, chance, "infected assault contact", instigatorPawn);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill), new[] { typeof(DamageInfo?), typeof(Hediff) })]
    public static class Patch_InfectedDeathReanimation
    {
        public static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            CrossedUtility.ApplyInfectedTattoo(__instance);
            CrossedUtility.Component?.QueueCrossedReanimation(__instance);
            Pawn killer = dinfo?.Instigator as Pawn;
            if (killer != null)
            {
                CrossedUtility.NotifyBloodlustKill(killer, __instance);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TickRare))]
    public static class Patch_CrossedTacticalAI
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || !CrossedUtility.IsInfectedPawn(__instance))
            {
                return;
            }

            CrossedUtility.EnsureFearlessCrossedState(__instance);
            CrossedTacticalAI.TryIssueTacticalJob(__instance);
            if (!__instance.IsHashIntervalTick(TheMarkedMenSettings.InfectedStateMaintenanceIntervalTicks))
            {
                return;
            }

            CrossedUtility.EnsurePredatorHediffs(__instance);
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

        public static void Apply(Harmony harmony)
        {
            if (harmony == null)
            {
                return;
            }

            try
            {
                MethodInfo target = AccessTools.Method(typeof(Thing), "SetFactionDirect", new[] { typeof(Faction) });
                MethodInfo postfix = AccessTools.Method(typeof(CrossedOptionalHarmonyPatches), nameof(Postfix_SetFactionDirect));
                if (target != null && postfix != null)
                {
                    harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Optional SetFactionDirect tattoo patch skipped: " + ex.Message);
            }

            Patch_DebugOverlay.Apply(harmony);
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
            CrossedUtility.ApplyClassHediffs(__instance);
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

            if (TheMarkedMenMod.Settings != null && !TheMarkedMenMod.Settings.infectionEnabled)
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

    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
    public static class Patch_CrossedPyromaniacWeapons
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __result)
        {
            CrossedEquipmentGenerator.StripEquipment(__result);
            CrossedUtility.EnsureCrossedPyromaniacMolotov(__result);
        }
    }

    [HarmonyPatch]
    public static class Patch_DoorNoise
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Building_Door), "StartManualOpenBy");
        }

        [HarmonyPostfix]
        public static void Postfix(Building_Door __instance, Pawn opener)
        {
            if (__instance == null || opener == null || !__instance.Spawned)
            {
                return;
            }

            MarkedMenMemoryGrid memory = MarkedMenMemoryGrid.GetForMap(__instance.Map);
            if (memory == null)
            {
                return;
            }

            float noiseStrength = 0.3f;
            int decayTicks = 600;
            memory.AddNoise(__instance.Position, noiseStrength, decayTicks);
        }
    }

    [HarmonyPatch]
    public static class Patch_RangedNoise
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Verb_Shoot), "TryCastShot");
        }

        [HarmonyPostfix]
        public static void Postfix(Verb_Shoot __instance)
        {
            if (__instance == null || __instance.caster == null || !__instance.caster.Spawned || __instance.caster.Map == null)
            {
                return;
            }

            MarkedMenMemoryGrid memory = MarkedMenMemoryGrid.GetForMap(__instance.caster.Map);
            if (memory == null)
            {
                return;
            }

            float noiseStrength = 0.5f;
            int decayTicks = 1000;
            memory.AddNoise(__instance.caster.Position, noiseStrength, decayTicks);
        }
    }

    public static class Patch_DebugOverlay
    {
        public static void Apply(Harmony harmony)
        {
            if (harmony == null)
            {
                return;
            }

            try
            {
                MethodInfo target = AccessTools.Method(typeof(CameraDriver), "OnGUI");
                if (target == null)
                {
                    target = AccessTools.Method(typeof(Root), "OnGUI");
                }

                if (target == null)
                {
                    Log.Warning("[The Marked Men] Debug overlay patch skipped: no valid target method found.");
                    return;
                }

                MethodInfo postfix = AccessTools.Method(typeof(Patch_DebugOverlay), nameof(Postfix));
                if (postfix != null)
                {
                    harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                    Log.Message("[The Marked Men] Debug overlay patched onto " + target.DeclaringType.Name + "." + target.Name);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Debug overlay patch skipped due to error: " + ex.Message);
            }
        }

        public static void Postfix()
        {
            if (!MarkedMenDebugOverlay.Active)
            {
                return;
            }

            MarkedMenDebugOverlay.Draw();
        }
    }
}
