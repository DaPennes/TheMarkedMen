using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    
    
    
    
    
    
    
    
    public sealed class LordJob_CrossedAssault : LordJob
    {
        private Faction faction;
        private bool breachers;
        private int siegeTicks = 6000;

        public override bool GuiltyOnDowned => true;

        public override bool LostImportantReferenceDuringLoading => faction == null;

        public LordJob_CrossedAssault()
        {
        }

        public LordJob_CrossedAssault(Faction faction, bool breachers, int siegeTicks)
        {
            this.faction = faction;
            this.breachers = breachers;
            this.siegeTicks = Mathf.Clamp(siegeTicks, 1200, 30000);
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();

            LordToil_CrossedBreach breach = null;
            if (breachers)
            {
                breach = new LordToil_CrossedBreach
                {
                    useAvoidGrid = true
                };
                graph.AddToil(breach);
            }

            LordToil_CrossedAssault assault = new LordToil_CrossedAssault()
            {
                useAvoidGrid = true
            };
            graph.AddToil(assault);

            LordToil_ExitMap exit = new LordToil_ExitMap(LocomotionUrgency.Jog, canDig: false, interruptCurrentJob: true)
            {
                useAvoidGrid = true
            };
            graph.AddToil(exit);

            if (breach != null)
            {
                
                
                Transition toAssault = new Transition(breach, assault);
                toAssault.AddTrigger(new Trigger_CrossedBreachComplete());
                toAssault.AddTrigger(new Trigger_TicksPassed(siegeTicks));
                graph.AddTransition(toAssault);
            }

            
            
            
            
            Transition leave = new Transition(assault, exit);
            if (breach != null)
            {
                leave.AddSource(breach);
            }
            leave.AddTrigger(new Trigger_BecameNonHostileToPlayer());
            if (faction != null)
            {
                leave.AddPreAction(new TransitionAction_Message(
                    "MessageRaidersLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
            }
            graph.AddTransition(leave);

            return graph;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref breachers, "breachers", false);
            Scribe_Values.Look(ref siegeTicks, "siegeTicks", 6000);
        }
    }

    
    
    
    
    
    public sealed class LordToil_CrossedBreach : LordToil_AssaultColonyBreaching
    {
        public bool ReachedBreachDestination;

        public override void UpdateAllDuties()
        {
            base.UpdateAllDuties();
            if (lord?.ownedPawns == null)
            {
                return;
            }

            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                if (pawn?.mindState != null && !pawn.Dead && !pawn.Downed)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                }
            }
        }

        public override void Notify_ReachedDutyLocation(Pawn pawn)
        {
            base.Notify_ReachedDutyLocation(pawn);
            ReachedBreachDestination = true;
        }

        
        
        public bool AnyPawnAtBreachDestination()
        {
            IntVec3 dest = Data.breachDest;
            Map map = Map;
            if (!dest.IsValid || map == null || !dest.InBounds(map))
            {
                return false;
            }

            Room destinationRoom = dest.GetRoom(map);
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                if (pawn == null || !pawn.Spawned || pawn.Dead)
                {
                    continue;
                }

                if (pawn.Position.DistanceToSquared(dest) < 25f
                    && pawn.Position.GetRoom(map) == destinationRoom)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class LordToil_CrossedAssault : LordToil_AssaultColony
    {
        public LordToil_CrossedAssault()
            : base(attackDownedIfStarving: false, canPickUpOpportunisticWeapons: true)
        {
        }

        public override void UpdateAllDuties()
        {
            base.UpdateAllDuties();
            if (lord?.ownedPawns == null)
            {
                return;
            }

            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                if (pawn?.mindState != null && !pawn.Dead && !pawn.Downed)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                }
            }
        }

        public override void LordToilTick()
        {
            base.LordToilTick();
            if ((lord?.ticksInToil ?? 0) % 30 != 0 || lord?.ownedPawns == null)
            {
                return;
            }

            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                if (pawn?.mindState != null
                    && !pawn.Dead
                    && !pawn.Downed
                    && pawn.mindState.duty == null)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                }
            }
        }
    }

    public sealed class LordJob_CrossedVehicleAssault : LordJob
    {
        private Faction faction;

        public override bool GuiltyOnDowned => true;

        public override bool LostImportantReferenceDuringLoading => faction == null;

        public LordJob_CrossedVehicleAssault()
        {
        }

        public LordJob_CrossedVehicleAssault(Faction faction)
        {
            this.faction = faction;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            LordToil_CrossedVehicleAssault assault = new LordToil_CrossedVehicleAssault
            {
                useAvoidGrid = true
            };
            graph.AddToil(assault);

            LordToil_ExitMap exit = new LordToil_ExitMap(LocomotionUrgency.Sprint, canDig: false, interruptCurrentJob: true)
            {
                useAvoidGrid = true
            };
            graph.AddToil(exit);

            Transition leave = new Transition(assault, exit);
            leave.AddTrigger(new Trigger_BecameNonHostileToPlayer());
            if (faction != null)
            {
                leave.AddPreAction(new TransitionAction_Message(
                    "MessageRaidersLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
            }
            graph.AddTransition(leave);
            return graph;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
        }
    }

    public sealed class LordToil_CrossedVehicleAssault : LordToil_AssaultColony
    {
        public override void UpdateAllDuties()
        {
            base.UpdateAllDuties();
            if (lord?.ownedPawns == null)
            {
                return;
            }

            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                if (pawn?.mindState != null && TheMarkedMenVehicleFrameworkCompatibility.IsVehiclePawn(pawn))
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                }
            }
        }

        public override void LordToilTick()
        {
            base.LordToilTick();
            if ((lord?.ticksInToil ?? 0) % 30 != 0
                || lord?.ownedPawns == null)
            {
                return;
            }

            float landRange = TheMarkedMenMod.Settings?.landVehicleDisembarkRange ?? 28f;
            float airRange = TheMarkedMenMod.Settings?.airborneVehicleDisembarkRange ?? 38f;
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn vehicle = lord.ownedPawns[i];
                if (!TheMarkedMenVehicleFrameworkCompatibility.IsVehiclePawn(vehicle))
                {
                    if (vehicle?.mindState != null
                        && !vehicle.Dead
                        && !vehicle.Downed
                        && vehicle.mindState.duty == null)
                    {
                        vehicle.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                    }
                    continue;
                }

                TheMarkedMenVehicleFrameworkCompatibility.MaintainMarkedMenVehicleControl(vehicle);
                if (vehicle?.Faction?.def != CADefOf.CrossedFaction)
                {
                    continue;
                }

                TheMarkedMenVehicleFrameworkCompatibility.ApplyVehicleCombatBehavior(vehicle);
                if (TheMarkedMenMod.Settings?.vehiclePassengerDisembarkEnabled == true)
                {
                    TheMarkedMenVehicleFrameworkCompatibility.TryDisembarkRaidPassengers(vehicle, landRange, airRange);
                }
            }
        }
    }

    public sealed class Trigger_CrossedBreachComplete : Trigger
    {
        public override bool ActivateOn(Lord lord, TriggerSignal signal)
        {
            if (signal.type != TriggerSignalType.Tick)
            {
                return false;
            }

            LordToil_CrossedBreach breach = lord?.CurLordToil as LordToil_CrossedBreach;
            if (breach == null)
            {
                return false;
            }

            if (breach.ReachedBreachDestination)
            {
                return true;
            }

            
            
            return lord.ticksInToil % 30 == 0 && breach.AnyPawnAtBreachDestination();
        }
    }

    public static class CrossedRaidTactics
    {
        public const int MinSiegeTicks = 4000;
        public const int MaxSiegeTicks = 8000;
        public const float BreacherThresholdPoints = 700f;

        public static int GetRemainingRaidPawnCapacity(Map map)
        {
            int configuredMaximum = Mathf.Clamp(
                TheMarkedMenMod.Settings?.maxActiveRaidPawns ?? 80,
                12,
                180);
            if (map?.mapPawns == null)
            {
                return configuredMaximum;
            }

            int activeCrossedPawns = 0;
            IReadOnlyList<Pawn> spawnedPawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < spawnedPawns.Count; i++)
            {
                Pawn pawn = spawnedPawns[i];
                if (pawn != null
                    && !pawn.Destroyed
                    && !pawn.Dead
                    && pawn.Faction?.def == CADefOf.CrossedFaction
                    && pawn.HostileTo(Faction.OfPlayer))
                {
                    activeCrossedPawns++;
                    if (TheMarkedMenVehicleFrameworkCompatibility.IsVehiclePawn(pawn))
                    {
                        foreach (Pawn passenger in TheMarkedMenVehicleFrameworkCompatibility.GetVehiclePassengers(pawn))
                        {
                            if (passenger != null
                                && !passenger.Spawned
                                && !passenger.Destroyed
                                && !passenger.Dead
                                && passenger.Faction?.def == CADefOf.CrossedFaction
                                && passenger.HostileTo(Faction.OfPlayer))
                            {
                                activeCrossedPawns++;
                            }
                        }
                    }
                }
            }

            int inboundAircraft = TheMarkedMenVehicleFrameworkCompatibility.CountInboundMarkedMenAircraft(map);
            return Mathf.Max(0, configuredMaximum - activeCrossedPawns - inboundAircraft);
        }

        public static LordJob CreateAssaultLordJob(Faction faction, float points, IReadOnlyList<Pawn> pawns = null)
        {
            
            
            bool breachers = points >= BreacherThresholdPoints && HasGoodBreacher(pawns);
            return new LordJob_CrossedAssault(faction, breachers, GetSiegeDurationTicks(points));
        }

        public static void AssignAssaultDuty(IEnumerable<Pawn> pawns)
        {
            if (pawns == null)
            {
                return;
            }

            foreach (Pawn pawn in pawns)
            {
                if (pawn?.mindState != null && !pawn.Dead && !pawn.Downed)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                }
            }
        }

        private static bool HasGoodBreacher(IReadOnlyList<Pawn> pawns)
        {
            if (pawns == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead)
                {
                    continue;
                }

                Verb verb = BreachingUtility.FindVerbToUseForBreaching(pawn);
                if (verb != null && verb.verbProps.ai_IsBuildingDestroyer)
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetSiegeDurationTicks(float points)
        {
            float pressure = Mathf.InverseLerp(1500f, 12000f, Mathf.Max(0f, points));
            return Mathf.RoundToInt(Mathf.Lerp(MinSiegeTicks, MaxSiegeTicks, pressure));
        }
    }
}
