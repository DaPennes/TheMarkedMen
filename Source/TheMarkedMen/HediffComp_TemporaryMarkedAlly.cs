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
        private bool ownershipStateNormalized;
        private bool ownershipFailureLogged;

        private HediffCompProperties_TemporaryMarkedAlly Props => (HediffCompProperties_TemporaryMarkedAlly)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent.pawn;
            if (expired || pawn == null || pawn.Dead)
            {
                return;
            }

            if (!ownershipStateNormalized && !NormalizePlayerOwnership(pawn))
            {
                return;
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (expiresAtTick <= 0)
            {
                int durationTicks = TheMarkedMenSettings.TemporaryAllyDurationTicks;
                durationTicks = durationTicks > 0 ? durationTicks : Props.durationTicks;
                expiresAtTick = currentTick + System.Math.Max(1, durationTicks);
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

        private bool NormalizePlayerOwnership(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                ownershipStateNormalized = true;
                return true;
            }

            if (!ScenPart_MarkedMenPlayerState.IsActive)
            {
                ownershipStateNormalized = true;
                return true;
            }

            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction == null || pawn.guest == null)
            {
                if (!ownershipFailureLogged)
                {
                    Log.Error("[The Marked Men] Could not restore temporary ally control after loading because the player faction or pawn guest tracker is missing; retrying.");
                    ownershipFailureLogged = true;
                }

                return false;
            }

            if (pawn.Faction == playerFaction && CrossedEquipmentGenerator.IsCrossedKind(pawn.kindDef))
            {
                pawn.health?.RemoveHediff(parent);
                ownershipStateNormalized = true;
                return true;
            }

            if (pawn.Faction != playerFaction)
            {
                pawn.SetFaction(playerFaction, null);
            }

            if (ModsConfig.IdeologyActive)
            {
                if (pawn.guest.GuestStatus != GuestStatus.Slave || pawn.guest.HostFaction != playerFaction)
                {
                    pawn.guest.SetGuestStatus(playerFaction, GuestStatus.Slave);
                }
            }
            else
            {
                pawn.guest.SetGuestStatus(null);
            }

            ownershipStateNormalized = true;
            return true;
        }

        public void EnsureOwnershipStateAfterLoad()
        {
            if (!expired && !ownershipStateNormalized)
            {
                NormalizePlayerOwnership(parent?.pawn);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref expiresAtTick, "expiresAtTick", 0);
            Scribe_Values.Look(ref expired, "expired", false);
            Scribe_Values.Look(ref expiryFailureLogged, "expiryFailureLogged", false);
            Scribe_Values.Look(ref ownershipStateNormalized, "ownershipStateNormalized", false);
            Scribe_Values.Look(ref ownershipFailureLogged, "ownershipFailureLogged", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && expiresAtTick < 0)
            {
                expiresAtTick = 0;
            }
        }
    }
}
