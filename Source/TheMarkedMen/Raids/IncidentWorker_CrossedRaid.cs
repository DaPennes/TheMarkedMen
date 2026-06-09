using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    public sealed class IncidentWorker_CrossedRaid : IncidentWorker_RaidEnemy
    {
        public override float ChanceFactorNow(IIncidentTarget target)
        {
            return base.ChanceFactorNow(target) * TheMarkedMenSettings.WarbandFrequencyMultiplier;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && CrossedUtility.Component?.EnsureCrossedFaction() != null;
        }

        protected override string GetLetterLabel(IncidentParms parms)
        {
            return CrossedRaidAlertUtility.BuildRaidLetterLabel(def.letterLabel, null, parms?.points ?? 0f);
        }

        protected override string GetLetterText(IncidentParms parms, List<Pawn> pawns)
        {
            return CrossedRaidAlertUtility.BuildRaidLetterText(def.letterText, pawns, parms, false);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Faction crossed = CrossedUtility.Component?.EnsureCrossedFaction();
            if (crossed == null)
            {
                return false;
            }

            Map map = parms.target as Map;
            HashSet<Pawn> existingCrossed = CaptureExistingCrossed(map);
            TheMarkedMenGameComponent component = CrossedUtility.Component;
            parms.faction = crossed;
            if (component != null)
            {
                parms.points = component.CalculateEscalatedRaidPoints(parms.points);
            }

            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.canKidnap = false;
            parms.canSteal = false;
            parms.canTimeoutOrFlee = TheMarkedMenSettings.MarkedCanTimeoutOrFlee;
            TheMarkedMenGameComponent.ApplyMarkedRaidArrivalPattern(parms);

            bool result = base.TryExecuteWorker(parms);
            if (result && map != null)
            {
                List<Pawn> spawned = FindNewCrossed(map, existingCrossed);
                MarkSpawnedCrossed(spawned);
                CrossedUtility.ApplyGeneratedRaidKindTuning(spawned);
                ForceImmediateAssaultLord(crossed, map, spawned, parms.points);
                component?.NotifyRaidLaunched(parms.points, spawned, map);
            }

            return result;
        }

        private static HashSet<Pawn> CaptureExistingCrossed(Map map)
        {
            FactionDef crossed = CADefOf.CrossedFaction;
            if (map?.mapPawns == null || crossed == null)
            {
                return new HashSet<Pawn>();
            }

            HashSet<Pawn> existing = new HashSet<Pawn>();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Faction?.def == crossed)
                {
                    existing.Add(pawn);
                }
            }

            return existing;
        }

        private static List<Pawn> FindNewCrossed(Map map, HashSet<Pawn> existing)
        {
            List<Pawn> spawned = new List<Pawn>();
            FactionDef crossed = CADefOf.CrossedFaction;
            if (map?.mapPawns == null || crossed == null)
            {
                return spawned;
            }

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Faction?.def == crossed && (existing == null || !existing.Contains(pawn)))
                {
                    spawned.Add(pawn);
                }
            }

            return spawned;
        }

        private static void MarkSpawnedCrossed(List<Pawn> pawns)
        {
            if (pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                CrossedUtility.ApplyClassHediffs(pawns[i]);
            }
        }

        private static void ForceImmediateAssaultLord(Faction faction, Map map, List<Pawn> pawns, float points)
        {
            if (faction == null || map == null || pawns == null || pawns.Count == 0)
            {
                return;
            }

            List<Pawn> attackers = CrossedLordCleanupUtility.CollectValidSpawnedLordPawns(pawns, map, faction);
            for (int i = 0; i < attackers.Count; i++)
            {
                Pawn pawn = attackers[i];
                if (LordUtility.TryGetLord(pawn, out Lord existingLord))
                {
                    existingLord.RemovePawn(pawn);
                }
            }

            if (attackers.Count == 0)
            {
                return;
            }

            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, false, TheMarkedMenSettings.MarkedCanTimeoutOrFlee, false, false, false, points >= 700f, true), map, attackers);
            for (int i = 0; i < attackers.Count; i++)
            {
                Pawn pawn = attackers[i];
                if (pawn?.jobs == null)
                {
                    continue;
                }

                if (pawn.jobs.curJob != null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                }
                else
                {
                    pawn.jobs.CheckForJobOverride(0f, true);
                }
            }
        }
    }
}
