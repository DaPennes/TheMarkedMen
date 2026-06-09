using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    public static partial class CrossedUtility
    {
        public static void EnsureFearlessCrossedState(Pawn pawn)
        {
            if (!TheMarkedMenSettings.MarkedAlwaysAssault || TheMarkedMenSettings.MarkedCanTimeoutOrFlee)
            {
                RestoreFleeStateForMarkedPawn(pawn);
                return;
            }
            if (!IsInfectedPawn(pawn) || pawn.mindState == null) return;
            MarkFearlessDueToCrossVirus(pawn);
            Pawn_MindState mindState = pawn.mindState;
            mindState.canFleeIndividual = false;
            mindState.exitMapAfterTick = -1;
            mindState.meleeThreat = null;
            MentalStateHandler handler = mindState.mentalStateHandler;
            if (handler == null) return;
            handler.neverFleeIndividual = true;
            if (IsFearOrWithdrawalMentalState(handler.CurStateDef))
                handler.Reset();
        }

        public static void RestoreFleeStateIfRecovered(Pawn pawn)
        {
            if (pawn == null || IsInfectedPawn(pawn) || !RemoveFearlessDueToCrossVirusTag(pawn) || pawn.mindState == null) return;
            Pawn_MindState mindState = pawn.mindState;
            mindState.canFleeIndividual = true;
            MentalStateHandler handler = mindState.mentalStateHandler;
            if (handler != null) handler.neverFleeIndividual = false;
        }

        private static void RestoreFleeStateForMarkedPawn(Pawn pawn)
        {
            if (pawn?.mindState == null) return;
            RemoveFearlessDueToCrossVirusTag(pawn);
            pawn.mindState.canFleeIndividual = true;
            MentalStateHandler handler = pawn.mindState.mentalStateHandler;
            if (handler != null) handler.neverFleeIndividual = false;
        }

        private static void MarkFearlessDueToCrossVirus(Pawn pawn)
        {
            if (pawn == null || IsCrossedFactionPawn(pawn)) return;
            if (pawn.questTags == null) pawn.questTags = new List<string>();
            if (!pawn.questTags.Contains(FearlessDueToCrossVirusTag))
                pawn.questTags.Add(FearlessDueToCrossVirusTag);
        }

        private static bool RemoveFearlessDueToCrossVirusTag(Pawn pawn)
        {
            List<string> tags = pawn?.questTags;
            return tags != null && tags.Remove(FearlessDueToCrossVirusTag);
        }

        private static bool IsFearOrWithdrawalMentalState(MentalStateDef def)
        {
            if (def == null) return false;
            return def == MentalStateDefOf.PanicFlee
                || def == MentalStateDefOf.PanicFleeFire
                || def == MentalStateDefOf.Terror
                || def == MentalStateDefOf.Wander_Psychotic
                || def == MentalStateDefOf.Wander_Sad
                || def == MentalStateDefOf.Wander_OwnRoom
                || def == MentalStateDefOf.Roaming;
        }
    }
}
