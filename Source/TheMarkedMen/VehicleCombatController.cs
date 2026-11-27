using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using TheMarkedMen; 

namespace TheMarkedMen
{
    
    
    
    
    public static class VehicleCombatController
    {
        
        
        
        private static readonly Dictionary<int, VehicleCombatState> combatStates = new Dictionary<int, VehicleCombatState>();

        
        
        
        private const int TargetScanIntervalTicks = 60;

        
        
        
        private const float MaxCombatEngagementDistance = 150f;

        
        
        
        private enum VehicleTypeCategory
        {
            Tank,
            APC,
            Technical,
            Motorcycle,
            ArmoredCar,
            HalfTrack,
            Airplane,
            Helicopter,
            Unknown
        }

        
        
        
        private class VehicleCombatState
        {
            public Pawn vehicle;
            public Pawn lastTarget;
            public int lastTargetUpdateTick;
            public int lastDutyReassignTick;
            public bool isEngaging;

            public VehicleCombatState(Pawn vehicle)
            {
                this.vehicle = vehicle;
                this.lastTarget = null;
                this.lastTargetUpdateTick = 0;
                this.lastDutyReassignTick = 0;
                this.isEngaging = false;
            }

            public bool IsTargetValid()
            {
                return lastTarget != null
                    && !lastTarget.Destroyed
                    && !lastTarget.Dead
                    && lastTarget.Spawned
                    && lastTarget.Map == vehicle.Map
                    && vehicle.HostileTo(lastTarget);
            }

            public bool ShouldUpdateTarget(int currentTick)
            {
                return currentTick - lastTargetUpdateTick >= TargetScanIntervalTicks;
            }

            public bool CanReassignDuty(int currentTick)
            {
                return currentTick - lastDutyReassignTick >= Constants.VehicleDutyReassignmentCooldownTicks;
            }
        }

        
        
        
        private static VehicleCombatState GetCombatState(Pawn vehicle)
        {
            if (vehicle == null)
            {
                return null;
            }

            int vehicleId = vehicle.thingIDNumber;
            if (!combatStates.TryGetValue(vehicleId, out VehicleCombatState state))
            {
                state = new VehicleCombatState(vehicle);
                combatStates[vehicleId] = state;
            }

            
            
            
            if (state?.lastTarget != null
                && (!state.lastTarget.Spawned
                    || state.lastTarget.Dead
                    || state.lastTarget.Downed
                    || state.lastTarget.Map != vehicle.Map
                    || !vehicle.HostileTo(state.lastTarget)))
            {
                Log.Message("[The Marked Men] Clearing invalid combat target for vehicle " + vehicle.def.defName);
                state.lastTarget = null;
                state.lastTargetUpdateTick = 0;
            }

            return state;
        }

        
        
        
        private static bool HandleHealthDisengagement(Pawn vehicle, Pawn target, VehicleCombatState state, float preferredRange, int currentTick)
        {
            if (ShouldDisengageDueToHealth(vehicle, isVehicle: true))
            {
                if (state.lastTarget != null)
                {
                    Log.Message("[The Marked Men] Vehicle " + vehicle.def.defName + " disengaging due to low health (" +
                        Mathf.RoundToInt(GetPawnHealthPercent(vehicle) * 100) + "%)");
                }
                state.lastTarget = null;
                state.lastTargetUpdateTick = 0;
                state.isEngaging = false;

                PawnDuty disengageDuty = null;
                if (preferredRange > 0f)
                {
                    IntVec3 standoffCell = FindStandoffPosition(vehicle, target, preferredRange);
                    disengageDuty = standoffCell.IsValid
                        ? new PawnDuty(DutyDefOf.AssaultColony, standoffCell, preferredRange * 0.5f)
                        : new PawnDuty(DutyDefOf.AssaultColony);
                }
                else
                {
                    disengageDuty = new PawnDuty(DutyDefOf.AssaultColony);
                }

                if (AIConflictPrevention.CanAssignDuty(vehicle, disengageDuty))
                {
                    vehicle.mindState.duty = disengageDuty;
                    state.lastDutyReassignTick = currentTick;
                }
                return true;
            }
            return false;
        }

        
        
        
        private static PawnDuty DetermineVehicleDuty(Pawn vehicle, Pawn target, VehicleTypeCategory vtype, float actualDistance, float preferredRange)
        {
            if (vtype == VehicleTypeCategory.Technical || vtype == VehicleTypeCategory.Motorcycle)
            {
                if (actualDistance < 25f)
                {
                    IntVec3 standoffCell = FindStandoffPosition(vehicle, target, 20f);
                    return standoffCell.IsValid ? new PawnDuty(DutyDefOf.AssaultColony, standoffCell, 15f) : new PawnDuty(DutyDefOf.AssaultColony);
                }
                return new PawnDuty(DutyDefOf.AssaultColony);
            }

            float thresholdMultiplier = 1.0f;
            switch (vtype)
            {
                case VehicleTypeCategory.Tank:
                case VehicleTypeCategory.APC:
                case VehicleTypeCategory.ArmoredCar:
                    thresholdMultiplier = 1.3f;
                    break;
                case VehicleTypeCategory.HalfTrack:
                case VehicleTypeCategory.Helicopter:
                    thresholdMultiplier = 1.2f;
                    break;
                case VehicleTypeCategory.Airplane:
                    thresholdMultiplier = 1.1f;
                    break;
                default:
                    thresholdMultiplier = 0.8f;
                    break;
            }

            if (preferredRange > 0f && actualDistance < preferredRange * thresholdMultiplier)
            {
                IntVec3 standoffCell = FindStandoffPosition(vehicle, target, preferredRange);
                return standoffCell.IsValid ? new PawnDuty(DutyDefOf.AssaultColony, standoffCell, preferredRange * 0.5f) : new PawnDuty(DutyDefOf.AssaultColony);
            }

            return new PawnDuty(DutyDefOf.AssaultColony);
        }


        
        
        
        public static void UpdateVehicleCombatBehavior(Pawn vehicle)
        {
            if (vehicle == null || !vehicle.Spawned || vehicle.Dead || vehicle.Map == null)
            {
                return;
            }

            VehicleCombatState state = GetCombatState(vehicle);
            if (state == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;

            
            if (state.ShouldUpdateTarget(currentTick))
            {
                Pawn bestTarget = FindBestCombatTarget(vehicle);
                state.lastTarget = bestTarget;
                state.lastTargetUpdateTick = currentTick;
            }

            
            if (state.lastTarget != null && !state.IsTargetValid())
            {
                state.lastTarget = null;
            }

            
            if (state.lastTarget != null)
            {
                ApplyCombatBehavior(vehicle, state.lastTarget, state, currentTick);
            }
            else
            {
                ApplyAssaultBehavior(vehicle, state, currentTick);
            }
        }

        
        
        
        
        
        private static Pawn FindBestCombatTarget(Pawn vehicle)
        {
            if (vehicle?.Map?.mapPawns == null)
            {
                return null;
            }

            IReadOnlyList<Pawn> allPawns = vehicle.Map.mapPawns.AllPawnsSpawned;
            Pawn bestTarget = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn == null
                    || pawn.Dead
                    || pawn.Downed
                    || !pawn.HostileTo(vehicle)
                    || pawn.Position.DistanceToSquared(vehicle.Position) > MaxCombatEngagementDistance * MaxCombatEngagementDistance)
                {
                    continue;
                }

                float score = CalculateTargetScore(pawn, vehicle);
                if (score > bestScore)
                {
                    bestTarget = pawn;
                    bestScore = score;
                }
            }

            return bestTarget;
        }

        
        
        
        private static bool IsThreatBuilding(Pawn target)
        {
            
            
            if (target.equipment?.Primary != null)
            {
                
                return true;
            }

            return false;
        }

        
        
        
        
        
        private static float CalculateTargetScore(Pawn target, Pawn vehicle, float crewEffectiveness = 1.0f)
        {
            float score = 0f;

            
            if (IsThreatBuilding(target))
            {
                score += 1000f;
            }

            
            if (target.equipment?.Primary != null)
            {
                score += 500f;
            }

            
            float distance = target.Position.DistanceTo(vehicle.Position);
            VehicleTypeCategory vtype = GetVehicleTypeCategory(vehicle);
            float distanceWeight = 200f;

            
            
            switch (vtype)
            {
                case VehicleTypeCategory.Tank:
                case VehicleTypeCategory.APC:
                    distanceWeight = 300f; 
                    break;

                case VehicleTypeCategory.Motorcycle:
                case VehicleTypeCategory.Technical:
                    distanceWeight = 150f; 
                    break;

                default:
                    distanceWeight = 200f;
                    break;
            }

            score += Mathf.Max(0f, distanceWeight - distance) * crewEffectiveness;

            
            if (target.health != null)
            {
                float healthPercent = target.health.summaryHealth.SummaryHealthPercent;
                score += (healthPercent * 100f) * crewEffectiveness;
            }

            
            score += ApplyVehicleTargetPriority(target, vehicle);

            return score;
        }

        
        
        
        
        private static float ApplyVehicleTargetPriority(Pawn target, Pawn vehicle)
        {
            float score = 0f;
            VehicleTypeCategory vtype = GetVehicleTypeCategory(vehicle);

            
            if (vtype == VehicleTypeCategory.Tank)
            {
                if (target.Faction == Faction.OfPlayer || target.IsColonist || target.IsPrisonerOfColony)
                {
                    score += 200f;
                }
            }

            
            if (vtype == VehicleTypeCategory.APC)
            {
                if (target.IsColonist || target.IsPrisonerOfColony || target.IsFreeColonist)
                {
                    score += 150f;
                }
            }

            
            if (vtype == VehicleTypeCategory.Technical)
            {
                if (target.equipment?.Primary == null)
                {
                    score += 100f; 
                }
            }

            
            if (vtype == VehicleTypeCategory.Motorcycle)
            {
                if (target.IsColonist || target.IsPrisonerOfColony || target.IsFreeColonist)
                {
                    score += 125f;
                }
                
                float distance = target.Position.DistanceTo(vehicle.Position);
                if (distance < 30f)
                {
                    score += 75f;
                }
            }

            
            if (vtype == VehicleTypeCategory.ArmoredCar)
            {
                if (target.Faction == Faction.OfPlayer || target.IsColonist)
                {
                    score += 100f;
                }
            }

            
            if (vtype == VehicleTypeCategory.HalfTrack)
            {
                if (target.RaceProps.Humanlike && !target.IsColonist)
                {
                    score += 100f;
                }
            }

            
            if (vtype == VehicleTypeCategory.Airplane)
            {
                if (target.health != null)
                {
                    float healthPercent = target.health.summaryHealth.SummaryHealthPercent;
                    score += healthPercent * 50f;
                }
            }

            
            if (vtype == VehicleTypeCategory.Helicopter)
            {
                if (target.IsColonist || target.IsPrisonerOfColony || target.IsFreeColonist)
                {
                    score += 80f;
                }
            }

            return score;
        }

        
        
        
        private static float GetPawnHealthPercent(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                return 0f;
            }

            if (pawn.health != null && pawn.Spawned)
            {
                return pawn.health.summaryHealth.SummaryHealthPercent;
            }

            return 1f; 
        }

        
        
        
        
        
        
        private static bool ShouldDisengageDueToHealth(Pawn entity, bool isVehicle = false)
        {
            float healthPercent;

            if (isVehicle)
            {
                
                if (entity == null || entity.Dead || entity.Destroyed)
                {
                    return false;
                }

                if (entity.health != null && entity.Spawned)
                {
                    healthPercent = entity.health.summaryHealth.SummaryHealthPercent;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                healthPercent = GetPawnHealthPercent(entity);
            }

            
            if (healthPercent <= Constants.VehicleHealthDisengagePercent / 100f)
            {
                return true;
            }

            return false;
        }

        
        
        
        
        private static float GetCrewCombatEffectiveness(Pawn vehicle)
        {
            if (vehicle == null || !vehicle.Spawned || vehicle.Map == null)
            {
                return 1f;
            }

            float totalEffectiveness = 1f;
            int crewCount = 0;

            
            if (TheMarkedMenVehicleFrameworkCompatibility.IsVehicleFrameworkLoaded)
            {
                IEnumerable<Pawn> passengers = TheMarkedMenVehicleFrameworkCompatibility.GetVehiclePassengers(vehicle);
                foreach (Pawn passenger in passengers)
                {
                    if (passenger != null && !passenger.Dead && !passenger.Downed && passenger.Spawned)
                    {
                        totalEffectiveness += CalculatePassengerCombatEffectiveness(passenger);
                        crewCount++;
                    }
                }
            }
            else if (TheMarkedMenVehicleFrameworkCompatibility.IsVehiclePawn(vehicle))
            {
                
                totalEffectiveness = 1f;
                crewCount = 1;
            }

            
            if (crewCount > 0)
            {
                totalEffectiveness /= crewCount;
            }

            
            return Mathf.Clamp01(totalEffectiveness);
        }

        
        
        
        private static float CalculatePassengerCombatEffectiveness(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned)
            {
                return 0f;
            }

            float effectiveness = 1f; 

            
            if (pawn.skills != null)
            {
                float shootingLevel = pawn.skills.GetSkill(SkillDefOf.Shooting).Level;
                
                effectiveness += shootingLevel * 0.15f;
            }

            
            return Mathf.Clamp01(effectiveness);
        }

        
        
        
        
        private static void ApplyCombatBehavior(Pawn vehicle, Pawn target, VehicleCombatState state, int currentTick)
        {
            float preferredRange = GetPreferredCombatRange(vehicle);

            
            if (HandleHealthDisengagement(vehicle, target, state, preferredRange, currentTick))
            {
                return;
            }

            if (target == null || !state.CanReassignDuty(currentTick))
            {
                return;
            }

            
            float crewEffectiveness = GetCrewCombatEffectiveness(vehicle);
            float actualDistance = vehicle.Position.DistanceTo(target.Position);

            
            float score = CalculateTargetScore(target, vehicle, crewEffectiveness);

            
            if (crewEffectiveness < 0.5f && score < 200f)
            {
                
            }

            
            VehicleTypeCategory vtype = GetVehicleTypeCategory(vehicle);
            PawnDuty newDuty = DetermineVehicleDuty(vehicle, target, vtype, actualDistance, preferredRange);

            
            if (newDuty != null && AIConflictPrevention.CanAssignDuty(vehicle, newDuty))
            {
                vehicle.mindState.duty = newDuty;
                state.lastDutyReassignTick = currentTick;
                state.isEngaging = true;
            }
        }

        
        
        
        private static void ApplyAssaultBehavior(Pawn vehicle, VehicleCombatState state, int currentTick)
        {
            if (!state.CanReassignDuty(currentTick))
            {
                return;
            }

            PawnDuty newDuty = new PawnDuty(DutyDefOf.AssaultColony);

            
            if (AIConflictPrevention.CanAssignDuty(vehicle, newDuty))
            {
                vehicle.mindState.duty = newDuty;
                state.lastDutyReassignTick = currentTick;
                state.isEngaging = false;
            }
        }

        
        
        
        
        private static float GetPreferredCombatRange(Pawn vehicle)
        {
            VehicleTypeCategory type = GetVehicleTypeCategory(vehicle);
            float baseRange = 35f; 

            switch (type)
            {
                case VehicleTypeCategory.Tank:
                case VehicleTypeCategory.APC:
                    
                    return 60f;

                case VehicleTypeCategory.Technical:
                case VehicleTypeCategory.Motorcycle:
                    
                    return 20f;

                case VehicleTypeCategory.ArmoredCar:
                    
                    return 40f;

                case VehicleTypeCategory.HalfTrack:
                    
                    return 35f;

                case VehicleTypeCategory.Airplane:
                    
                    return 100f;

                case VehicleTypeCategory.Helicopter:
                    
                    return 50f;

                default:
                    
                    return baseRange;
            }
        }

        
        
        
        private static VehicleTypeCategory GetVehicleTypeCategory(Pawn vehicle)
        {
            if (vehicle == null || vehicle.def == null)
            {
                return VehicleTypeCategory.Unknown;
            }

            string defName = vehicle.def.defName;

            
            defName = defName.ToLower();

            
            if (defName.Contains("tank")
                || defName.Contains("heavy")
                || defName.Contains("main battle"))
            {
                return VehicleTypeCategory.Tank;
            }

            
            if (defName.Contains("apc")
                || defName.Contains("personal carriage"))
            {
                return VehicleTypeCategory.APC;
            }

            
            if (defName.Contains("technical")
                || defName.Contains("pickup")
                || defName.Contains("hilux"))
            {
                return VehicleTypeCategory.Technical;
            }

            
            if (defName.Contains("motorcycle")
                || defName.Contains("bike")
                || defName.Contains("quad"))
            {
                return VehicleTypeCategory.Motorcycle;
            }

            
            if (defName.Contains("armored")
                || defName.Contains("car")
                || defName.Contains("sedan"))
            {
                return VehicleTypeCategory.ArmoredCar;
            }

            
            if (defName.Contains("half-track")
                || defName.Contains("halftrack")
                || defName.Contains("scout"))
            {
                return VehicleTypeCategory.HalfTrack;
            }

            
            if (defName.Contains("plane")
                || defName.Contains("aircraft")
                || defName.Contains("jet")
                || defName.Contains("skyfaller"))
            {
                return VehicleTypeCategory.Airplane;
            }

            
            if (defName.Contains("heli")
                || defName.Contains("helicopter")
                || defName.Contains("chopper"))
            {
                return VehicleTypeCategory.Helicopter;
            }

            return VehicleTypeCategory.Unknown;
        }

        
        
        
        
        private static IntVec3 FindStandoffPosition(Pawn vehicle, Pawn target, float preferredRange)
        {
            if (vehicle?.Map == null || target == null)
            {
                return IntVec3.Invalid;
            }

            VehicleTypeCategory type = GetVehicleTypeCategory(vehicle);
            float standoffMultiplier = 1f;

            
            switch (type)
            {
                case VehicleTypeCategory.Tank:
                case VehicleTypeCategory.APC:
                    standoffMultiplier = 1.2f; 
                    break;

                case VehicleTypeCategory.Motorcycle:
                case VehicleTypeCategory.Technical:
                    standoffMultiplier = 0.8f; 
                    break;

                case VehicleTypeCategory.Helicopter:
                    standoffMultiplier = 1.5f; 
                    break;

                default:
                    standoffMultiplier = 1f;
                    break;
            }

            
            IntVec3 vehiclePos = vehicle.Position;
            IntVec3 targetPos = target.Position;

            int attempts = 0;
            int maxAttempts = 12; 

            while (attempts < maxAttempts)
            {
                
                Vector3 directionVec = (vehiclePos - targetPos).ToVector3().normalized;
                IntVec3 direction = new IntVec3(Mathf.RoundToInt(directionVec.x), 0, Mathf.RoundToInt(directionVec.z));
                IntVec3 candidateCell = targetPos + (direction * Mathf.RoundToInt(preferredRange * standoffMultiplier * 0.9f));

                
                if (candidateCell.InBounds(vehicle.Map)
                    && candidateCell.Walkable(vehicle.Map)
                    && vehicle.CanReachImmediate(candidateCell, PathEndMode.OnCell))
                {
                    return candidateCell;
                }

                
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        IntVec3 nearby = candidateCell + new IntVec3(dx, 0, dz);
                        if (nearby.InBounds(vehicle.Map)
                            && nearby.Walkable(vehicle.Map)
                            && vehicle.CanReachImmediate(nearby, PathEndMode.OnCell))
                        {
                            return nearby;
                        }
                    }
                }

                attempts++;
            }

            return IntVec3.Invalid;
        }

        
        
        
        public static void ClearAllCombatStates()
        {
            combatStates.Clear();
        }
    }
}