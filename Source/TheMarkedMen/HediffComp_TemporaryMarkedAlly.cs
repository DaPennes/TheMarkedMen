using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    public sealed class HediffCompProperties_TemporaryMarkedAlly : HediffCompProperties
    {
        public int durationTicks = 1800000;

        public HediffCompProperties_TemporaryMarkedAlly()
        {
            compClass = typeof(HediffComp_TemporaryMarkedAlly);
        }
    }

    public sealed class HediffComp_TemporaryMarkedAlly : HediffComp
    {
        private int expiresAtTick;
        private bool expired;
        private bool expiryFailureLogged;

        private HediffCompProperties_TemporaryMarkedAlly Props => (HediffCompProperties_TemporaryMarkedAlly)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;
            if (expired || pawn == null || pawn.Dead)
            {
                return;
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (expiresAtTick <= 0)
            {
                int durationTicks = TheMarkedMenSettings.TemporaryAllyDurationTicks;
                expiresAtTick = currentTick + (durationTicks > 0 ? durationTicks : Props.durationTicks);
                return;
            }

            if (currentTick < expiresAtTick)
            {
                return;
            }

            Faction crossedFaction = CrossedUtility.Component?.EnsureCrossedFaction();
            if (crossedFaction == null)
            {
                if (!expiryFailureLogged)
                {
                    Log.Error("[The Marked Men] A temporary infected ally expired, but the Marked Men faction could not be found; expiration will be retried.");
                    expiryFailureLogged = true;
                }

                expiresAtTick = currentTick + 2500;
                return;
            }

            pawn.guest?.SetGuestStatus(null);
            pawn.SetFaction(crossedFaction, null);
            pawn.mindState?.mentalStateHandler?.Reset();
            pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
            expired = true;
            CrossedUtility.EnsureInfectedHasValidLord(pawn);
            if (pawn.Spawned)
            {
                CrossedTacticalAI.TryAttackNearestNonInfectedPawn(pawn, true, allowRjwJob: false);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref expiresAtTick, "expiresAtTick", 0);
            Scribe_Values.Look(ref expired, "expired", false);
            Scribe_Values.Look(ref expiryFailureLogged, "expiryFailureLogged", false);
        }
    }
}
