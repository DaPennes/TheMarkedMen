using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Centralized constants for the mod to avoid magic numbers.
    /// </summary>
    public static class Constants
    {
        // Vehicle combat constants
        public const float VehicleMinEngageDistance = 5f;
        public const float VehicleMaxEngageDistance = 60f;
        public const int VehicleDutyReassignmentCooldownTicks = 60;
        public const int VehicleHealthDisengagePercent = 20;

        // Combat distance constants
        public const float CloseCombatDistance = 4f;
        public const float MediumCombatDistance = 15f;
        public const float FarCombatDistance = 30f;

        // Outnumbering thresholds
        public const int HeavyOutnumberThreshold = 3;
        public const int LightOutnumberThreshold = 2;

        // Sadistic AI thresholds
        public const float IsolatedTargetDistance = 15f;
        public const float MemoryRevengeRange = 15f;

        // AI learning constants
        public const float LearningAvoidChance = 0.6f;
        public const int RecentTacticsWindowTicks = 2000;

        // Target priority scoring
        public const float BaseTargetScore = 100f;
        public const float IsolatedTargetBonus = 150f;
        public const float HeavyDefensePenalty = 100f;
        public const float ColonistHatredBonus = 300f;
        public const float DownedEnjoymentBonus = 200f;
        public const float RangedTortureBonus = 100f;
        public const float IsolatedMiseryBonus = 150f;
        public const float MemoryRevengeBonus = 250f;
        public const float TurretPriorityBonus = 180f;
        public const float ColonistMetaGameBonus = 120f;

        // Memory system
        public const int MaxRememberedTicks = 2000;
    }
}