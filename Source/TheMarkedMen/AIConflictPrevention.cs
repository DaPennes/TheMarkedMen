using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// Prevents AI conflicts and janky behavior by managing pawn state transitions.
    /// Implements cooldowns, state validation, and conflict detection.
    /// </summary>
    public static class AIConflictPrevention
    {
        /// <summary>
        /// AI behavior state for a single pawn.
        /// </summary>
        public enum AIBehaviorState
        {
            Idle,              // No active behavior
            Engaging,          // In combat with target
            Repositioning,     // Moving to better position
            Disembarking,      // Exiting vehicle
            Fleeing,           // Tactical retreat
            Following,         // Following commander
            Breaching,         // Breaking through walls
        }

        /// <summary>
        /// Tracks AI state per pawn to prevent conflicts.
        /// </summary>
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

        /// <summary>
        /// Cached state trackers per pawn ID.
        /// </summary>
        private static readonly Dictionary<int, AIStateTracker> stateTrackers = new Dictionary<int, AIStateTracker>();

        /// <summary>
        /// Minimum ticks between duty assignments for the same pawn.
        /// </summary>
        private const int DutyChangeCooldownTicks = 60;

        /// <summary>
        /// Minimum ticks between state changes.
        /// </summary>
        private const int StateChangeCooldownTicks = 30;

        /// <summary>
        /// Maximum consecutive jobs of the same type before forcing an evaluation.
        /// </summary>
        private const int MaxRepeatedJobCount = 3;

        /// <summary>
        /// Gets or creates state tracker for a pawn.
        /// </summary>
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

        /// <summary>
        /// Validates that a job assignment is safe and won't cause conflicts.
        /// </summary>
        public static bool CanAssignJob(Pawn pawn, Job job)
        {
            if (pawn == null || job == null)
            {
                return false;
            }

            AIStateTracker tracker = GetStateTracker(pawn);
            if (tracker == null)
            {
                return true;  // If no tracker, allow assignment
            }

            int currentTick = Find.TickManager.TicksGame;

            // Don't assign new jobs too rapidly (prevent thrashing)
            if (!tracker.CanChangeDuty(currentTick, DutyChangeCooldownTicks))
            {
                return false;
            }

            // If pawn already has a valid job of the same type, don't interrupt
            JobDef currentJobDef = pawn.CurJob?.def;
            if (currentJobDef == job.def && IsValidJobContinuation(pawn.CurJob, job))
            {
                return false;  // Job already appropriate, don't change
            }

            return true;
        }

        /// <summary>
        /// Checks if continuing the current job is valid rather than switching.
        /// </summary>
        private static bool IsValidJobContinuation(Job currentJob, Job proposedJob)
        {
            if (currentJob == null || proposedJob == null)
            {
                return false;
            }

            // If both are attack jobs on the same target, continue current
            if (currentJob.def == JobDefOf.AttackStatic && proposedJob.def == JobDefOf.AttackStatic)
            {
                return currentJob.targetA == proposedJob.targetA;
            }

            // If both are goto jobs near same location, continue current
            if (currentJob.def == JobDefOf.Goto && proposedJob.def == JobDefOf.Goto)
            {
                return currentJob.targetA.Cell.DistanceToSquared(proposedJob.targetA.Cell) < 25f;
            }

            return false;
        }

        /// <summary>
        /// Notifies the system that a pawn changed jobs.
        /// Used to track and prevent job spam.
        /// </summary>
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

        /// <summary>
        /// Validates and updates target for a pawn, preventing invalid engagements.
        /// </summary>
        public static bool CanEngageTarget(Pawn pawn, Pawn target)
        {
            if (pawn == null || target == null)
            {
                return false;
            }

            // Basic validity checks
            if (target.Dead || target.Destroyed || !target.Spawned)
            {
                return false;
            }

            // Must be hostile
            if (!pawn.HostileTo(target))
            {
                return false;
            }

            // Don't engage already-engaged targets repeatedly
            AIStateTracker tracker = GetStateTracker(pawn);
            if (tracker != null)
            {
                int currentTick = Find.TickManager.TicksGame;

                // If target is same as current, allow engagement
                if (tracker.currentTarget == target && tracker.IsTargetValid())
                {
                    return true;
                }

                // If target is too old (stale cache), allow re-engagement
                if (tracker.TargetTooOld(currentTick, 750))
                {
                    tracker.UpdateTarget(target, currentTick);
                    return true;
                }

                // Don't switch targets too rapidly
                if (!tracker.CanChangeState(currentTick, StateChangeCooldownTicks))
                {
                    return tracker.currentTarget != null && tracker.IsTargetValid();
                }

                tracker.UpdateTarget(target, currentTick);
            }

            return true;
        }

        /// <summary>
        /// Detects if a pawn is experiencing job thrashing (rapid job changes).
        /// </summary>
        public static bool IsExperiencingJobThrashing(Pawn pawn)
        {
            if (pawn == null || pawn.jobs == null)
            {
                return false;
            }

            // Check job history for rapid changes
            // Note: RimWorld doesn't expose full job history, so we track via state
            AIStateTracker tracker = GetStateTracker(pawn);
            if (tracker == null)
            {
                return false;
            }

            int currentTick = Find.TickManager.TicksGame;

            // If more than 3 state changes in 90 ticks, likely thrashing
            int ticksSinceLastChange = currentTick - tracker.lastStateChangeTick;
            return ticksSinceLastChange < 30 && tracker.currentState != AIBehaviorState.Idle;
        }

        /// <summary>
        /// Prevents duty assignment conflicts by validating against current state.
        /// </summary>
        public static bool CanAssignDuty(Pawn pawn, PawnDuty newDuty)
        {
            if (pawn == null)
            {
                return true;
            }

            // Don't assign duty if pawn is downed, dead, or unconscious
            if (pawn.Downed || pawn.Dead || !pawn.Awake())
            {
                return false;
            }

            // Check cooldown before assigning new duty
            AIStateTracker tracker = GetStateTracker(pawn);
            if (tracker != null)
            {
                int currentTick = Find.TickManager.TicksGame;

                // If still in appropriate duty, don't change
                PawnDuty currentDuty = pawn.mindState?.duty;
                if (currentDuty != null && currentDuty.def == newDuty.def)
                {
                    // Same duty type, allow
                    return true;
                }

                // Enforce cooldown
                if (!tracker.CanChangeDuty(currentTick, DutyChangeCooldownTicks))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Clears state when a pawn is destroyed or removed from raid.
        /// </summary>
        public static void NotifyPawnRemoved(Pawn pawn)
        {
            if (pawn != null)
            {
                stateTrackers.Remove(pawn.thingIDNumber);
            }
        }

        /// <summary>
        /// Clears all tracked state (use when raid ends or map changes).
        /// </summary>
        public static void ClearAllState()
        {
            stateTrackers.Clear();
        }

        /// <summary>
        /// Gets diagnostic information about a pawn's AI state.
        /// Useful for debugging and logging.
        /// </summary>
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
