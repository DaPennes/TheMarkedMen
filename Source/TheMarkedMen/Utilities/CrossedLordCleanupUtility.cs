using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
    public static class CrossedLordCleanupUtility
    {
        public static List<Pawn> CollectValidSpawnedLordPawns(IEnumerable<Pawn> pawns, Map map, Faction faction)
        {
            List<Pawn> valid = new List<Pawn>();
            if (pawns == null || map == null)
            {
                return valid;
            }

            foreach (Pawn pawn in pawns)
            {
                if (IsValidSpawnedLordPawn(pawn, map, faction))
                {
                    valid.Add(pawn);
                }
            }

            return valid;
        }

        public static void RemoveInvalidOwnedPawns(Lord lord)
        {
            if (lord?.ownedPawns == null || lord.ownedPawns.Count == 0)
            {
                return;
            }

            int ticks = Find.TickManager?.TicksGame ?? 0;
            int hash = lord.GetHashCode() & int.MaxValue;
            if ((ticks + hash) % TheMarkedMenSettings.LordCleanupIntervalTicks != 0)
            {
                return;
            }

            if (!IsCrossedLord(lord))
            {
                return;
            }

            Map map = lord.Map;
            Faction faction = lord.faction;
            for (int i = lord.ownedPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = lord.ownedPawns[i];
                if (!IsValidSpawnedLordPawn(pawn, map, faction))
                {
                    if (pawn == null)
                    {
                        lord.ownedPawns.RemoveAt(i);
                    }
                    else
                    {
                        lord.RemovePawn(pawn);
                    }
                }
            }
        }

        public static bool IsValidSpawnedLordPawn(Pawn pawn, Map map, Faction faction)
        {
            return pawn != null
                && !pawn.Destroyed
                && !pawn.Dead
                && !pawn.Downed
                && pawn.Spawned
                && pawn.Map == map
                && (faction == null || pawn.Faction == faction)
                && !HasRecoveryWaitJob(pawn);
        }

        private static bool IsCrossedLord(Lord lord)
        {
            if (lord?.faction?.def == CADefOf.CrossedFaction)
            {
                return true;
            }

            if (lord?.ownedPawns == null)
            {
                return false;
            }

            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                if (lord.ownedPawns[i]?.Faction?.def == CADefOf.CrossedFaction)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasRecoveryWaitJob(Pawn pawn)
        {
            return string.Equals(pawn?.CurJob?.def?.defName, "Wait_Downed", System.StringComparison.Ordinal)
                || string.Equals(pawn?.jobs?.curDriver?.job?.def?.defName, "Wait_Downed", System.StringComparison.Ordinal);
        }

        public static bool IsRecoveryWaitJob(Job job)
        {
            return string.Equals(job?.def?.defName, "Wait_Downed", System.StringComparison.Ordinal);
        }
    }
}
