using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    
    
    
    
    
    public static class TacticalDisembarkationController
    {
        
        
        
        
        public static float GetDeploymentDistance(Pawn pawn)
        {
            if (pawn == null || pawn.kindDef == null)
            {
                return 35f;  
            }

            
            if (pawn.kindDef == CADefOf.CrossedShooter
                || pawn.kindDef == CADefOf.CrossedSoldier)
            {
                return 60f;  
            }

            
            if (pawn.kindDef == CADefOf.CrossedBrute
                || pawn.kindDef == CADefOf.CrossedRaider)
            {
                return 25f;  
            }

            
            if (pawn.kindDef == CADefOf.CrossedScout
                || pawn.kindDef == CADefOf.CrossedHunter)
            {
                return 40f;  
            }

            
            if (CrossedUtility.IsCrossedPyromaniac(pawn))
            {
                return 45f;
            }

            
            if (pawn.kindDef == CADefOf.CrossedAlpha
                || pawn.kindDef == CADefOf.CrossedWarlord)
            {
                return 35f;  
            }

            
            return 35f;
        }

        
        
        
        
        public static bool TryFindDisembarkCell(
            Pawn vehicle,
            Pawn passenger,
            Pawn nearestEnemy,
            out IntVec3 disembarkCell)
        {
            disembarkCell = IntVec3.Invalid;

            if (vehicle?.Map == null || passenger == null)
            {
                return false;
            }

            float deploymentDistance = GetDeploymentDistance(passenger);

            
            if (nearestEnemy == null)
            {
                return CellFinder.TryFindRandomCellNear(
                    vehicle.Position,
                    vehicle.Map,
                    5,
                    cell => IsSafeDisembarkCell(cell, vehicle.Map),
                    out disembarkCell);
            }

            
            IntVec3 enemyPos = nearestEnemy.Position;
            IntVec3 vehiclePos = vehicle.Position;

            
            Vector3 awayVec = (vehiclePos - enemyPos).ToVector3().normalized;
            IntVec3 awayFromEnemy = new IntVec3(Mathf.RoundToInt(awayVec.x), 0, Mathf.RoundToInt(awayVec.z));

            
            IntVec3 targetCell = enemyPos + (awayFromEnemy * Mathf.RoundToInt(deploymentDistance));

            
            for (int radius = 0; radius <= 10; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        
                        if (radius > 0 && Mathf.Abs(dx) < radius && Mathf.Abs(dz) < radius)
                        {
                            continue;
                        }

                        IntVec3 candidate = targetCell + new IntVec3(dx, 0, dz);

                        if (candidate.InBounds(vehicle.Map)
                            && IsSafeDisembarkCell(candidate, vehicle.Map)
                            && vehicle.CanReachImmediate(candidate, PathEndMode.OnCell))
                        {
                            disembarkCell = candidate;
                            return true;
                        }
                    }
                }
            }

            
            return CellFinder.TryFindRandomCellNear(
                vehicle.Position,
                vehicle.Map,
                8,
                cell => IsSafeDisembarkCell(cell, vehicle.Map),
                out disembarkCell);
        }

        
        
        
        
        private static bool IsSafeDisembarkCell(IntVec3 cell, Map map)
        {
            if (!cell.InBounds(map) || !cell.Walkable(map))
            {
                return false;
            }

            
            Pawn occupant = cell.GetFirstPawn(map);
            if (occupant != null)
            {
                return false;
            }

            
            List<Thing> thingsAtCell = map.thingGrid.ThingsListAt(cell);
            for (int i = 0; i < thingsAtCell.Count; i++)
            {
                Thing thing = thingsAtCell[i];
                if (thing == null)
                {
                    continue;
                }

                
                if (thing.def.defName.Contains("Fire") || thing.def.defName.Contains("Danger"))
                {
                    return false;
                }

                
                if (thing.def.passability == Traversability.Impassable)
                {
                    return false;
                }
            }

            return true;
        }

        
        
        
        
        public static bool CanDisembarkAllPassengers(
            Pawn vehicle,
            List<Pawn> passengers,
            Pawn targetEnemy)
        {
            if (vehicle?.Map == null || passengers == null || passengers.Count == 0)
            {
                return false;
            }

            
            int safeCount = 0;
            for (int i = 0; i < passengers.Count; i++)
            {
                if (TryFindDisembarkCell(vehicle, passengers[i], targetEnemy, out IntVec3 _))
                {
                    safeCount++;
                }
            }

            
            return safeCount >= Mathf.CeilToInt(passengers.Count * 0.5f);
        }

        
        
        
        
        public static bool MarkPassengerAsDisembarked(Pawn passenger)
        {
            if (passenger == null)
            {
                return false;
            }

            
            
            
            Lord currentLord = passenger.GetLord();
            if (currentLord != null && currentLord.LordJob is LordJob_CrossedVehicleAssault)
            {
                
                return false;
            }

            return true;
        }

        
        
        
        
        public static bool CanReachDeploymentArea(Pawn vehicle, Pawn targetEnemy)
        {
            if (vehicle?.Map == null || targetEnemy == null)
            {
                return false;
            }

            
            return vehicle.CanReach(targetEnemy.Position, PathEndMode.Touch, Danger.Deadly, true, true);
        }

        
        
        
        public static string GetDeploymentRoleLabel(Pawn pawn)
        {
            if (pawn?.kindDef == null)
            {
                return "Infantry";
            }

            if (pawn.kindDef == CADefOf.CrossedShooter || pawn.kindDef == CADefOf.CrossedSoldier)
            {
                return "Support Fire";
            }

            if (pawn.kindDef == CADefOf.CrossedBrute || pawn.kindDef == CADefOf.CrossedRaider)
            {
                return "Assault";
            }

            if (pawn.kindDef == CADefOf.CrossedScout || pawn.kindDef == CADefOf.CrossedHunter)
            {
                return "Mobile";
            }

            if (CrossedUtility.IsCrossedPyromaniac(pawn))
            {
                return "Fire Support";
            }

            if (pawn.kindDef == CADefOf.CrossedAlpha || pawn.kindDef == CADefOf.CrossedWarlord)
            {
                return "Commander";
            }

            return "Infantry";
        }
    }
}
