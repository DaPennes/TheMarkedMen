using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    
    
    
    
    public static class InfectedAIProfiles
    {
        
        
        
        public class InfectedBehaviorProfile
        {
            public string Name { get; set; }
            public PawnKindDef KindDef { get; set; }

            
            
            
            public float PreferredMinRange { get; set; }

            
            
            
            public float PreferredMaxRange { get; set; }

            
            
            
            public TacticalRole Role { get; set; }

            
            
            
            public List<TargetPriority> TargetPriorities { get; set; }

            
            
            
            public CombatStyle CombatStyle { get; set; }
        }

        
        
        
        public enum TacticalRole
        {
            Scout,           
            Striker,         
            Gunner,          
            Specialist,      
            Commander,       
            Tank,            
            Infiltrator      
        }

        
        
        
        public enum CombatStyle
        {
            MeleePreferred,
            RangedPreferred,
            Flexible,
            FleeIfLosing
        }

        
        
        
        public class TargetPriority
        {
            public string Name { get; set; }
            public int Priority { get; set; }  
        }

        
        private static readonly Dictionary<PawnKindDef, InfectedBehaviorProfile> profiles =
            new Dictionary<PawnKindDef, InfectedBehaviorProfile>();

        static InfectedAIProfiles()
        {
            InitializeProfiles();
        }

        private static void InitializeProfiles()
        {
            
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

        
        
        
        public static bool ShouldMaintainRange(Pawn pawn, Pawn target, float currentDistance)
        {
            InfectedBehaviorProfile profile = GetProfile(pawn);
            if (profile == null)
            {
                return false;
            }

            
            if (currentDistance >= profile.PreferredMinRange && currentDistance <= profile.PreferredMaxRange)
            {
                return true;
            }

            
            if (profile.CombatStyle == CombatStyle.RangedPreferred && currentDistance < profile.PreferredMinRange)
            {
                return true;
            }

            return false;
        }

        
        
        
        public static bool ShouldEngageTarget(Pawn pawn, Pawn target)
        {
            if (pawn == null || target == null)
            {
                return false;
            }

            InfectedBehaviorProfile profile = GetProfile(pawn);

            
            if (!pawn.HostileTo(target) || target.Downed)
            {
                return false;
            }

            
            if (profile.CombatStyle == CombatStyle.FleeIfLosing)
            {
                
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

        
        
        
        public static float GetTargetPriorityScore(Pawn pawn, Pawn target)
        {
            if (pawn?.kindDef == null || target == null)
            {
                return 0f;
            }

            InfectedBehaviorProfile profile = GetProfile(pawn);
            float score = 100f;  

            
            float distance = pawn.Position.DistanceTo(target.Position);
            score -= distance * 0.5f;

            
            if (target.equipment?.Primary != null)
            {
                score += 50f;  
            }

            
            if (target.health != null)
            {
                score += (1f - target.health.summaryHealth.SummaryHealthPercent) * 100f;
            }

            
            if (profile?.Role == TacticalRole.Tank)
            {
                
                if (target.equipment?.Primary != null)
                {
                    score += 75f;
                }
            }

            if (profile?.Role == TacticalRole.Scout)
            {
                
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
                    score += 100f;  
                }
            }

            return score;
        }
    }
}
