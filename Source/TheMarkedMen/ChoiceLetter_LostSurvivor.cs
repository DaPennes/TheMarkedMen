using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    public class ChoiceLetter_LostSurvivor : ChoiceLetter
    {
        public Pawn pawn;

        public ChoiceLetter_LostSurvivor()
        {
            def = DefDatabase<LetterDef>.GetNamed("CA_LostSurvivorLetter", false) ?? LetterDefOf.NeutralEvent;
        }

        // Builds the choice letter in code instead of LetterMaker.MakeLetter so the
        // letter instance is guaranteed to carry the choice logic and the pawn reference.
        public static ChoiceLetter_LostSurvivor Create(string label, string text, Pawn survivor)
        {
            return new ChoiceLetter_LostSurvivor
            {
                pawn = survivor,
                title = label,
                Label = label,
                Text = text,
                lookTargets = new LookTargets(survivor),
                ID = Find.UniqueIDsManager.GetNextLetterID()
            };
        }

        // Right-click dismissal is disabled while the decision is pending, so the
        // letter cannot be resolved accidentally; it returns once the pawn is gone.
        public override bool CanDismissWithRightClick => pawn == null || pawn.Dead || !pawn.Spawned;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (pawn != null && !pawn.Dead && pawn.Spawned)
                {
                    yield return DiaOptionAccept;
                    yield return DiaOptionReject;
                }
                // Close dismisses the dialog only: the letter stays in the stack and
                // the guest remains on the map until the player decides.
                yield return Option_Close;
            }
        }

        private DiaOption DiaOptionAccept
        {
            get
            {
                DiaOption diaOption = new DiaOption("CA_LostSurvivor_Accept".Translate());
                diaOption.action = () =>
                {
                    if (pawn != null && !pawn.Dead && pawn.Spawned)
                    {
                        pawn.SetFaction(Faction.OfPlayer, null);
                        pawn.guest?.SetGuestStatus(null);
                        pawn.mindState?.mentalStateHandler?.Reset();
                        pawn.jobs?.StopAll(true);
                        pawn.lord = null;

                        ArmDormantMark(pawn);

                        Messages.Message("TheMarkedMen_MessageLostSurvivorAccepted".Translate(pawn.LabelShort),
                            pawn, MessageTypeDefOf.PositiveEvent);
                    }
                    Find.LetterStack.RemoveLetter(this);
                };
                diaOption.resolveTree = true;
                return diaOption;
            }
        }

        private DiaOption DiaOptionReject
        {
            get
            {
                DiaOption diaOption = new DiaOption("CA_LostSurvivor_Reject".Translate());
                diaOption.action = () =>
                {
                    if (pawn != null && !pawn.Dead)
                    {
                        Messages.Message("TheMarkedMen_MessageLostSurvivorRejected".Translate(pawn.LabelShort),
                            pawn, MessageTypeDefOf.NeutralEvent);

                        if (pawn.Faction != null)
                        {
                            pawn.SetFaction(null);
                        }

                        pawn.guest?.SetGuestStatus(null);
                        RemoveDormantMark(pawn);
                        pawn.mindState?.mentalStateHandler?.Reset();
                        pawn.jobs?.StopAll(true);
                        pawn.lord = null;

                        if (pawn.Map != null && pawn.mindState != null)
                        {
                            // Vanilla exit mechanism: the pawn walks to the map edge
                            // when its exit tick arrives instead of being destroyed.
                            int daysUntilDeparture = Rand.Range(3, 6);
                            pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + daysUntilDeparture * GenDate.TicksPerDay;
                            pawn.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBest);
                        }
                    }
                    Find.LetterStack.RemoveLetter(this);
                };
                diaOption.resolveTree = true;
                return diaOption;
            }
        }

        // CompPostPostAdd skips non-colonist pawns, so the dormancy window must be
        // armed manually once the pawn becomes a colonist.
        private static void ArmDormantMark(Pawn pawn)
        {
            Hediff dormant = pawn.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
            if (dormant == null)
            {
                return;
            }
            HediffComp_DormantMark comp = dormant.TryGetComp<HediffComp_DormantMark>();
            comp?.ArmDormancyWindow();
        }

        private static void RemoveDormantMark(Pawn pawn)
        {
            Hediff dormant = pawn.health?.hediffSet?.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
            if (dormant != null)
            {
                pawn.health.RemoveHediff(dormant);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
        }
    }
}
