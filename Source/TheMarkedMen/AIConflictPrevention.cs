using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    
    
    
    
    public static class AIConflictPrevention
    {
        
        
        
        public enum AIBehaviorState
        {
            Idle,              
            Engaging,          
            Repositioning,     
            Disembarking,      
            Fleeing,           
            Following,         
            Breaching,         
        }

        
        
        
        private class AIStateTracker
        {
            public Pawn pawn;
            public AIBehaviorState currentState;
            public int lastStateChangeTick;
            public int lastDutyChangeTick;
            public Pawn currentTarget;
            public int targetValidationTick;
            public bool isInConflict;

            public AIStateTracker(Pawn pawn)
            {
                this.pawn = pawn;
                this.currentState = AIBehaviorState.Idle;
                this.lastStateChangeTick = 0;
                this.lastDutyChangeTick = 0;
                this.currentTarget = null;
                this.targetValidationTick = 0;
                this.isInConflict = false;
            }

            public bool CanChangeState(int currentTick, int minCooldownTicks = 30)
            {
                return currentTick - lastStateChangeTick >= minCooldownTicks;
            }

            public bool CanChangeDuty(int currentTick, int minCooldownTicks = 60)
            {
                return currentTick - lastDutyChangeTick >= minCooldownTicks;
            }

            public void ChangeState(AIBehaviorState newState, int currentTick)
            {
                if (currentState != newState)
                {
                    currentState = newState;
                    lastStateChangeTick = currentTick;
                }
            }

            public void ChangeDuty(int currentTick)
            {
                lastDutyChangeTick = currentTick;
            }

            public bool IsTargetValid()
            {
                return currentTarget != null
                    && !currentTarget.Destroyed
                    && !currentTarget.Dead
                    && currentTarget.Spawned
                    && pawn != null
                    && pawn.Map == currentTarget.Map
                    && pawn.HostileTo(currentTarget);
            }

            public void UpdateTarget(Pawn newTarget, int currentTick)
            {
                if (currentTarget != newTarget)
                {
                    currentTarget = newTarget;
                    targetValidationTick = currentTick;
                }
            }

            public bool TargetTooOld(int currentTick, int maxAgeTicks = 750)
            {
                return currentTick - targetValidationTick >= maxAgeTicks;
            }
        }

        
        
        
        private static readonly Dictionary<int, AIStateTracker> stateTrackers = new Dictionary<int, AIStateTracker>();

        
        
        
        private const int DutyChangeCooldownTicks = 60;

        
        
        
        private const int StateChangeCooldownTicks = 30;

        
        
        
        private const int MaxRepeatedJobCount = 3;

        
        
        
        private static AIStateTracker GetStateTracker(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            int pawnId = pawn.thingIDNumber;
            if (!stateTrackers.TryGetValue(pawnId, out AIStateTracker tracker))
            {
                tracker = new AIStateTracker(pawn);
                stateTrackers[pawnId] = tracker;
            }

            return tracker;
        }

        
        
        
        public static bool CanAssignJob(Pawn pawn, Job job)
        {
            if (pawn == null || job == null)
            {
                return false;
            }

            AIStateTracker tracker = GetStateTracker(pawn);
            if (tracker == null)
            {
                return true;  
            }

            int currentTick = Find.TickManager.TicksGame;

            
            if (!tracker.CanChangeDuty(currentTick, DutyChangeCooldownTicks))
            {
                return false;
            }

            
            JobDef currentJobDef = pawn.CurJob?.def;
            if (currentJobDef == job.def && IsValidJobContinuation(pawn.CurJob, job))
            {
                return false;  
            }

            return true;
        }

        
        
        
        private static bool IsValidJobContinuation(Job currentJob, Job proposedJob)
        {
            if (currentJob == null || proposedJob == null)
            {
                return false;
            }

            
            if (currentJob.def == JobDefOf.AttackStatic && proposedJob.def == JobDefOf.AttackStatic)
            {
                return currentJob.targetA == proposedJob.targetA;
            }

            
            if (currentJob.def == JobDefOf.Goto && proposedJob.def == JobDefOf.Goto)
            {
                return currentJob.targetA.Cell.Equals(proposedJob.targetA.Cell);
            }

            return false;
        }

        
        
        
        
        public static void NotifyJobAssigned(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            AIStateTracker tracker = GetStateTracker(pawn);
            if (tracker != null)
            {
                tracker.ChangeDuty(Find.TickManager.TicksGame);
            }
        }

        
        
        
        public static bool CanEngageTarget(Pawn pawn, Pawn target)
        {
            if (pawn == null || target == null)
            {
                return false;
            }

            
            if (target.Dead || target.Destroyed || !target.Spawned)
            {
                return false;
            }

            
            if (!pawn.HostileTo(target))
            {
                return false;
            }

            
            AIStateTracker tracker = GetStateTracker(pawn);
            if (tracker != null)
            {
                int currentTick = Find.TickManager.TicksGame;

                
                if (tracker.currentTarget == target && tracker.IsTargetValid())
                {
                    return true;
                }

                
                if (tracker.TargetTooOld(currentTick, 750))
                {
                    tracker.UpdateTarget(target, currentTick);
                    return true;
                }

                
                if (!tracker.CanChangeState(currentTick, StateChangeCooldownTicks))
                {
                    return tracker.currentTarget != null && tracker.IsTargetValid();
                }

                tracker.UpdateTarget(target, currentTick);
            }

            return true;
        }

        
        
        
        public static bool IsExperiencingJobThrashing(Pawn pawn)
        {
            if (pawn == null || pawn.jobs == null)
            {
                return false;
            }

            
            
            AIStateTracker tracker = GetStateTracker(pawn);
            if (tracker == null)
            {
                return false;
            }

            int currentTick = Find.TickManager.TicksGame;

            
            int ticksSinceLastChange = currentTick - tracker.lastStateChangeTick;
            return ticksSinceLastChange < 30 && tracker.currentState != AIBehaviorState.Idle;
        }

        
        
        
        public static bool CanAssignDuty(Pawn pawn, PawnDuty newDuty)
        {
            if (pawn == null)
            {
                return true;
            }

            
            if (pawn.Downed || pawn.Dead || !pawn.Awake())
            {
                return false;
            }

            
            AIStateTracker tracker = GetStateTracker(pawn);
            if (tracker != null)
            {
                int currentTick = Find.TickManager.TicksGame;

                
                PawnDuty currentDuty = pawn.mindState?.duty;
                if (currentDuty != null && currentDuty.def == newDuty.def)
                {
                    
                    return true;
                }

                
                if (!tracker.CanChangeDuty(currentTick, DutyChangeCooldownTicks))
                {
                    return false;
                }
            }

            return true;
        }

        
        
        
        public static void NotifyPawnRemoved(Pawn pawn)
        {
            if (pawn != null)
            {
                stateTrackers.Remove(pawn.thingIDNumber);
            }
        }

        
        
        
        public static void ClearAllState()
        {
            stateTrackers.Clear();
        }

        
        
        
        
        public static string GetDiagnosticInfo(Pawn pawn)
        {
            if (pawn == null)
            {
                return "null";
            }

            AIStateTracker tracker = GetStateTracker(pawn);
            if (tracker == null)
            {
                return "No tracker";
            }

            int currentTick = Find.TickManager.TicksGame;
            return $"State:{tracker.currentState} Target:{tracker.currentTarget?.LabelShort ?? "none"} " +
                $"LastStateChange:{currentTick - tracker.lastStateChangeTick} " +
                $"LastDutyChange:{currentTick - tracker.lastDutyChangeTick}";
        }
    }
}
