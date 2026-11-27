using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    
    
    
    
    public class JobGiver_HuntWounded : ThinkNode_JobGiver
    {
        private const int WoundCheckIntervalTicks = 150;
        private const float MaxWoundSearchDistance = 25f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return null;

            
            if (!pawn.IsHashIntervalTick(WoundCheckIntervalTicks))
                return null;

            Pawn woundedTarget = FindBestWoundedTarget(pawn);
            if (woundedTarget == null)
                return null;

            
            return new Job(JobDefOf.AttackMelee, woundedTarget) { checkOverrideOnExpire = true };
        }

        private Pawn FindBestWoundedTarget(Pawn pawn)
        {
            IReadOnlyList<Pawn> candidates = pawn.Map.mapPawns.AllPawnsSpawned;
            Pawn bestTarget = null;
            float bestScore = 0f;

            foreach (Pawn candidate in candidates)
            {
                if (candidate == null || candidate.Dead || candidate == pawn)
                    continue;

                if (CrossedUtility.IsInfectedPawn(candidate))
                    continue;

                
                if (!pawn.CanReach(candidate, PathEndMode.Touch, Danger.Deadly))
                    continue;

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance > MaxWoundSearchDistance)
                    continue;

                
                float score = 1f;

                
                if (candidate.Downed)
                    score += 200f;
                
                else if (candidate.health?.hediffSet != null)
                {
                    List<Hediff_Injury> injuries = new List<Hediff_Injury>();
                    candidate.health.hediffSet.GetHediffs(ref injuries, null);
                    float woundPct = Mathf.InverseLerp(0f, 1f, injuries.Count / (float)candidate.health.hediffSet.hediffs.Count);
                    score += woundPct * 100f;
                }

                
                if (candidate.IsColonist)
                    score += 50f;

                
                score += Mathf.Max(0f, 20f - distance);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }
    }
}