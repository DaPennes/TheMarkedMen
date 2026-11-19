using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// Defines tactical behavior profiles and decision-making logic for each infected pawn type.
    /// Each profile describes the pawn's combat priorities, engagement distances, and tactical preferences.
    /// </summary>
    public static class InfectedAIProfiles
    {
        /// <summary>
        /// Behavior profile for a single infected type.
        /// </summary>
        public class InfectedBehaviorProfile
        {
            public string Name { get; set; }
            public PawnKindDef KindDef { get; set; }

            /// <summary>
            /// Minimum combat distance this pawn prefers to maintain from enemies.
            /// </summary>
            public float PreferredMinRange { get; set; }

            /// <summary>
            /// Maximum combat distance this pawn will engage from.
            /// </summary>
            public float PreferredMaxRange { get; set; }

            /// <summary>
            /// Primary tactical role (offense, support, tank, scout, etc).
            /// </summary>
            public TacticalRole Role { get; set; }

            /// <summary>
            /// Priority targets for this pawn type (in order of preference).
            /// </summary>
            public List<TargetPriority> TargetPriorities { get; set; }

            /// <summary>
            /// Whether this pawn prefers melee or ranged engagement.
            /// </summary>
            public CombatStyle CombatStyle { get; set; }
        }

        /// <summary>
        /// Tactical roles for infected types.
        /// </summary>
        public enum TacticalRole
        {
            Scout,           // Reconnaissance, mobility
            Striker,         // Melee assault
            Gunner,          // Ranged support
            Specialist,      // Special abilities (fire, psychic, etc)
            Commander,       // Coordination, leadership
            Tank,            // Heavy defense breaking
            Infiltrator      // Stealth, hit-and-run
        }

        /// <summary>
        /// Combat style preference.
        /// </summary>
        public enum CombatStyle
        {
            MeleePreferred,
            RangedPreferred,
            Flexible,
            FleeIfLosing
        }

        /// <summary>
        /// Target priority entry.
        /// </summary>
        public class TargetPriority
        {
            public string Name { get; set; }
            public int Priority { get; set; }  // Higher = higher priority
        }

        // Cached profiles per kind
        private static readonly Dictionary<PawnKindDef, InfectedBehaviorProfile> profiles =
            new Dictionary<PawnKindDef, InfectedBehaviorProfile>();

        static InfectedAIProfiles()
        {
            InitializeProfiles();
        }

        private static void InitializeProfiles()
        {
            // Scout Profile
            if (CADefOf.CrossedScout != null)
            {
                profiles[CADefOf.CrossedScout] = new InfectedBehaviorProfile
                {
                    Name = "Scout",
                    KindDef = CADefOf.CrossedScout,
                    PreferredMinRange = 8f,
                    PreferredMaxRange = 50f,
                    Role = TacticalRole.Scout,
                    CombatStyle = CombatStyle.FleeIfLosing,
                    TargetPriorities = new List<TargetPriority>
                    {
                        new TargetPriority { Name = "Isolated Enemy", Priority = 100 },
                        new TargetPriority { Name = "Ranged Threat", Priority = 80 },
                        new TargetPriority { Name = "Fleeing Target", Priority = 70 }
                    }
                };
            }

            // Hunter Profile
            if (CADefOf.CrossedHunter != null)
            {
                profiles[CADefOf.CrossedHunter] = new InfectedBehaviorProfile
                {
                    Name = "Hunter",
                    KindDef = CADefOf.CrossedHunter,
                    PreferredMinRange = 2f,
                    PreferredMaxRange = 35f,
                    Role = TacticalRole.Striker,
                    CombatStyle = CombatStyle.MeleePreferred,
                    TargetPriorities = new List<TargetPriority>
                    {
                        new TargetPriority { Name = "Downed Enemy", Priority = 120 },
                        new TargetPriority { Name = "Isolated Melee", Priority = 100 },
                        new TargetPriority { Name = "Wounded Target", Priority = 90 }
                    }
                };
            }

            // Shooter Profile
            if (CADefOf.CrossedShooter != null)
            {
                profiles[CADefOf.CrossedShooter] = new InfectedBehaviorProfile
                {
                    Name = "Shooter",
                    KindDef = CADefOf.CrossedShooter,
                    PreferredMinRange = 15f,
                    PreferredMaxRange = 80f,
                    Role = TacticalRole.Gunner,
                    CombatStyle = CombatStyle.RangedPreferred,
                    TargetPriorities = new List<TargetPriority>
                    {
                        new TargetPriority { Name = "Armed Ranged", Priority = 100 },
                        new TargetPriority { Name = "Defensive Structure", Priority = 90 },
                        new TargetPriority { Name = "Exposed Target", Priority = 70 }
                    }
                };
            }

            // Raider Profile
            if (CADefOf.CrossedRaider != null)
            {
                profiles[CADefOf.CrossedRaider] = new InfectedBehaviorProfile
                {
                    Name = "Raider",
                    KindDef = CADefOf.CrossedRaider,
                    PreferredMinRange = 3f,
                    PreferredMaxRange = 40f,
                    Role = TacticalRole.Striker,
                    CombatStyle = CombatStyle.Flexible,
                    TargetPriorities = new List<TargetPriority>
                    {
                        new TargetPriority { Name = "Any Threat", Priority = 100 },
                        new TargetPriority { Name = "Defensive Position", Priority = 85 },
                        new TargetPriority { Name = "Multiple Enemies", Priority = 75 }
                    }
                };
            }

            // Soldier Profile
            if (CADefOf.CrossedSoldier != null)
            {
                profiles[CADefOf.CrossedSoldier] = new InfectedBehaviorProfile
                {
                    Name = "Soldier",
                    KindDef = CADefOf.CrossedSoldier,
                    PreferredMinRange = 10f,
                    PreferredMaxRange = 70f,
                    Role = TacticalRole.Gunner,
                    CombatStyle = CombatStyle.RangedPreferred,
                    TargetPriorities = new List<TargetPriority>
                    {
                        new TargetPriority { Name = "Threat to Ally", Priority = 110 },
                        new TargetPriority { Name = "Armed Threat", Priority = 100 },
                        new TargetPriority { Name = "Defensive Structure", Priority = 80 }
                    }
                };
            }

            // Brute Profile
            if (CADefOf.CrossedBrute != null)
            {
                profiles[CADefOf.CrossedBrute] = new InfectedBehaviorProfile
                {
                    Name = "Brute",
                    KindDef = CADefOf.CrossedBrute,
                    PreferredMinRange = 1f,
                    PreferredMaxRange = 30f,
                    Role = TacticalRole.Tank,
                    CombatStyle = CombatStyle.MeleePreferred,
                    TargetPriorities = new List<TargetPriority>
                    {
                        new TargetPriority { Name = "Defensive Structure", Priority = 120 },
                        new TargetPriority { Name = "Heavy Armor", Priority = 100 },
                        new TargetPriority { Name = "Close Range Threat", Priority = 90 }
                    }
                };
            }

            // Pyromaniac Profile
            if (CADefOf.CrossedPyromaniac != null)
            {
                profiles[CADefOf.CrossedPyromaniac] = new InfectedBehaviorProfile
                {
                    Name = "Pyromaniac",
                    KindDef = CADefOf.CrossedPyromaniac,
                    PreferredMinRange = 8f,
                    PreferredMaxRange = 50f,
                    Role = TacticalRole.Specialist,
                    CombatStyle = CombatStyle.RangedPreferred,
                    TargetPriorities = new List<TargetPriority>
                    {
                        new TargetPriority { Name = "Clustered Enemies", Priority = 110 },
                        new TargetPriority { Name = "Resource Building", Priority = 100 },
                        new TargetPriority { Name = "Defensive Structure", Priority = 90 }
                    }
                };
            }

            // Alpha Profile
            if (CADefOf.CrossedAlpha != null)
            {
                profiles[CADefOf.CrossedAlpha] = new InfectedBehaviorProfile
                {
                    Name = "Alpha",
                    KindDef = CADefOf.CrossedAlpha,
                    PreferredMinRange = 5f,
                    PreferredMaxRange = 60f,
                    Role = TacticalRole.Commander,
                    CombatStyle = CombatStyle.Flexible,
                    TargetPriorities = new List<TargetPriority>
                    {
                        new TargetPriority { Name = "Critical Threat", Priority = 120 },
                        new TargetPriority { Name = "Leader Enemy", Priority = 105 },
                        new TargetPriority { Name = "High Value Target", Priority = 95 }
                    }
                };
            }

            // Warlord Profile
            if (CADefOf.CrossedWarlord != null)
            {
                profiles[CADefOf.CrossedWarlord] = new InfectedBehaviorProfile
                {
                    Name = "Warlord",
                    KindDef = CADefOf.CrossedWarlord,
                    PreferredMinRange = 10f,
                    PreferredMaxRange = 70f,
                    Role = TacticalRole.Commander,
                    CombatStyle = CombatStyle.Flexible,
                    TargetPriorities = new List<TargetPriority>
                    {
                        new TargetPriority { Name = "Strategic Objective", Priority = 130 },
                        new TargetPriority { Name = "Leader Enemy", Priority = 115 },
                        new TargetPriority { Name = "Force Multiplier", Priority = 105 }
                    }
                };
            }

            // Marked Man Profile (if available)
            if (CADefOf.MarkedMan != null)
            {
                profiles[CADefOf.MarkedMan] = new InfectedBehaviorProfile
                {
                    Name = "Marked Man",
                    KindDef = CADefOf.MarkedMan,
                    PreferredMinRange = 2f,
                    PreferredMaxRange = 50f,
                    Role = TacticalRole.Commander,
                    CombatStyle = CombatStyle.Flexible,
                    TargetPriorities = new List<TargetPriority>
                    {
                        new TargetPriority { Name = "Any Threat", Priority = 100 },
                        new TargetPriority { Name = "Multiple Targets", Priority = 90 },
                        new TargetPriority { Name = "Vulnerable Target", Priority = 85 }
                    }
                };
            }

            // Civilian Profile
            if (CADefOf.CrossedCivilian != null)
            {
                profiles[CADefOf.CrossedCivilian] = new InfectedBehaviorProfile
                {
                    Name = "Civilian",
                    KindDef = CADefOf.CrossedCivilian,
                    PreferredMinRange = 5f,
                    PreferredMaxRange = 40f,
                    Role = TacticalRole.Striker,
                    CombatStyle = CombatStyle.Flexible,
                    TargetPriorities = new List<TargetPriority>
                    {
                        new TargetPriority { Name = "Any Target", Priority = 100 },
                        new TargetPriority { Name = "Nearest Threat", Priority = 90 },
                        new TargetPriority { Name = "Grouped Enemy", Priority = 80 }
                    }
                };
            }
        }

        /// <summary>
        /// Gets the behavior profile for a pawn type.
        /// Returns a default profile if the specific type is not defined.
        /// </summary>
        public static InfectedBehaviorProfile GetProfile(Pawn pawn)
        {
            if (pawn?.kindDef == null)
            {
                return GetDefaultProfile();
            }

            if (profiles.TryGetValue(pawn.kindDef, out InfectedBehaviorProfile profile))
            {
                return profile;
            }

            return GetDefaultProfile();
        }

        /// <summary>
        /// Gets the default profile for undefined pawn types.
        /// </summary>
        private static InfectedBehaviorProfile GetDefaultProfile()
        {
            return new InfectedBehaviorProfile
            {
                Name = "Default",
                KindDef = null,
                PreferredMinRange = 5f,
                PreferredMaxRange = 40f,
                Role = TacticalRole.Striker,
                CombatStyle = CombatStyle.Flexible,
                TargetPriorities = new List<TargetPriority>
                {
                    new TargetPriority { Name = "Any Target", Priority = 100 }
                }
            };
        }

        /// <summary>
        /// Determines if a pawn should maintain ranged distance from a target.
        /// </summary>
        public static bool ShouldMaintainRange(Pawn pawn, Pawn target, float currentDistance)
        {
            InfectedBehaviorProfile profile = GetProfile(pawn);
            if (profile == null)
            {
                return false;
            }

            // If already within preferred range, maintain it
            if (currentDistance >= profile.PreferredMinRange && currentDistance <= profile.PreferredMaxRange)
            {
                return true;
            }

            // Ranged specialists always want distance
            if (profile.CombatStyle == CombatStyle.RangedPreferred && currentDistance < profile.PreferredMinRange)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a pawn should engage a target based on their profile.
        /// </summary>
        public static bool ShouldEngageTarget(Pawn pawn, Pawn target)
        {
            if (pawn == null || target == null)
            {
                return false;
            }

            InfectedBehaviorProfile profile = GetProfile(pawn);

            // Never engage allies or downed enemies
            if (!pawn.HostileTo(target) || target.Downed)
            {
                return false;
            }

            // Fleeing types should avoid enemies they can't handle
            if (profile.CombatStyle == CombatStyle.FleeIfLosing)
            {
                // Don't engage if outnumbered or heavily outgunned
                int nearbyThreats = 0;
                for (int i = 0; i < pawn.Map.mapPawns.AllPawnsSpawned.Count; i++)
                {
                    Pawn other = pawn.Map.mapPawns.AllPawnsSpawned[i];
                    if (other != null && other.HostileTo(pawn) && other.Position.DistanceTo(pawn.Position) < 30f)
                    {
                        nearbyThreats++;
                    }
                }

                if (nearbyThreats > 3)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a score for prioritizing a target based on the pawn's profile.
        /// </summary>
        public static float GetTargetPriorityScore(Pawn pawn, Pawn target)
        {
            if (pawn?.kindDef == null || target == null)
            {
                return 0f;
            }

            InfectedBehaviorProfile profile = GetProfile(pawn);
            float score = 100f;  // Base score

            // Apply distance penalty
            float distance = pawn.Position.DistanceTo(target.Position);
            score -= distance * 0.5f;

            // Apply threat assessment bonus
            if (target.equipment?.Primary != null)
            {
                score += 50f;  // Armed targets are more important
            }

            // Apply health modifier
            if (target.health != null)
            {
                score += (1f - target.health.summaryHealth.SummaryHealthPercent) * 100f;
            }

            // Apply profile-specific modifiers
            if (profile?.Role == TacticalRole.Tank)
            {
                // Tanks prioritize defensive structures/armored enemies
                if (target.equipment?.Primary != null)
                {
                    score += 75f;
                }
            }

            if (profile?.Role == TacticalRole.Scout)
            {
                // Scouts prioritize isolated enemies
                int nearbyAllie = 0;
                for (int i = 0; i < pawn.Map.mapPawns.AllPawnsSpawned.Count; i++)
                {
                    Pawn other = pawn.Map.mapPawns.AllPawnsSpawned[i];
                    if (other != null && other.HostileTo(pawn) && other.Position.DistanceTo(target.Position) < 15f)
                    {
                        nearbyAllie++;
                    }
                }

                if (nearbyAllie <= 1)
                {
                    score += 100f;  // Isolated target bonus
                }
            }

            return score;
        }
    }
}
