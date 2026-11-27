using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    
    
    
    
    
    public static class VehicleCrewManager
    {
        
        
        
        public enum CrewRolePriority
        {
            Driver = 100,
            Gunner = 50,
            Passenger = 10
        }

        
        
        
        
        public static int GetCrewRolePriority(Pawn pawn, string handlingTypeName)
        {
            if (pawn == null || string.IsNullOrEmpty(handlingTypeName))
            {
                return 0;
            }

            int baseScore = 0;

            
            if (handlingTypeName.Equals("Movement", StringComparison.OrdinalIgnoreCase))
            {
                baseScore = (int)CrewRolePriority.Driver;
                
                baseScore += pawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
                baseScore += pawn.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            }
            else if (handlingTypeName.Equals("Turret", StringComparison.OrdinalIgnoreCase))
            {
                baseScore = (int)CrewRolePriority.Gunner;
                
                baseScore += (pawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0) * 3;
            }
            else
            {
                baseScore = (int)CrewRolePriority.Passenger;
            }

            
            if (pawn.health != null && pawn.health.capacities != null)
            {
                float combatReadiness = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
                baseScore += Mathf.RoundToInt(combatReadiness * 10f);
            }

            
            if (pawn.kindDef == CADefOf.CrossedShooter
                || pawn.kindDef == CADefOf.CrossedSoldier
                || pawn.kindDef == CADefOf.CrossedRaider
                || pawn.kindDef == CADefOf.CrossedAlpha)
            {
                baseScore += 5;
            }

            return baseScore;
        }

        
        
        
        
        public static bool IsValidCrewCandidate(Pawn pawn)
        {
            return pawn != null
                && !pawn.Destroyed
                && pawn.RaceProps.Humanlike
                && pawn.kindDef != CADefOf.MarkedMan  
                && !pawn.Dead
                && !pawn.Downed;
        }

        
        
        
        
        public static int CountAvailableCrewCandidates(List<Pawn> pawns)
        {
            if (pawns == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (IsValidCrewCandidate(pawn)
                    && !TheMarkedMenVehicleFrameworkCompatibility.IsVehiclePawn(pawn))
                {
                    count++;
                }
            }

            return count;
        }

        
        
        
        
        public static Pawn FindBestCrewForRole(
            List<Pawn> candidates,
            string roleName,
            Func<Pawn, bool> additionalFilter = null)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            Pawn bestPawn = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn pawn = candidates[i];
                if (!IsValidCrewCandidate(pawn))
                {
                    continue;
                }

                if (additionalFilter != null && !additionalFilter(pawn))
                {
                    continue;
                }

                int score = GetCrewRolePriority(pawn, roleName);
                if (score > bestScore)
                {
                    bestPawn = pawn;
                    bestScore = score;
                }
            }

            return bestPawn;
        }

        
        
        
        
        public static bool IsCrewAssignmentValid(Pawn pawn)
        {
            return pawn != null
                && !pawn.Destroyed
                && !pawn.Dead
                && !pawn.Downed
                && pawn.RaceProps.Humanlike;
        }

        
        
        
        public static string GetRoleLabel(string handlingTypeName)
        {
            if (handlingTypeName.Equals("Movement", StringComparison.OrdinalIgnoreCase))
            {
                return "Driver";
            }

            if (handlingTypeName.Equals("Turret", StringComparison.OrdinalIgnoreCase))
            {
                return "Gunner";
            }

            return "Crew";
        }
    }
}
