using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Choice letter for a pawn that has been cured and is seeking to rejoin society as a refugee.
    /// Shows the pawn's backstory and offers to recruit them or let them leave as a wanderer.
    /// </summary>
    public class ChoiceLetter_CuredRefugee : ChoiceLetter
    {
        public Pawn pawn;
        public Faction originalFaction;

        public override bool CanDismissWithRightClick => false;

        public ChoiceLetter_CuredRefugee()
        {
            // Initialize required letter properties
            def = DefDatabase<LetterDef>.GetNamed("TheMarkedMen_CuredRefugeeLetter", false) ?? LetterDefOf.NeutralEvent;
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (pawn != null && !pawn.Dead && pawn.Spawned)
                {
                    yield return DiaOptionAccept;
                    yield return DiaOptionReject;
                }
                yield return Option_Close;
            }
        }

        private DiaOption DiaOptionAccept
        {
            get
            {
                DiaOption diaOption = new DiaOption("TheMarkedMen_CuredRefugee_Accept".Translate());
                diaOption.action = () =>
                {
                    if (pawn != null && !pawn.Dead && pawn.Spawned)
                    {
                        // Apply faction relation penalty for acceptance (you're taking their pawn)
                        if (originalFaction != null && !originalFaction.defeated)
                        {
                            Faction playerFaction = Faction.OfPlayer;
                            if (playerFaction != null && originalFaction != playerFaction)
                            {
                                FactionRelation relation = playerFaction.RelationWith(originalFaction);
                                if (relation != null)
                                {
                                    relation.baseGoodwill -= 1;
                                }
                            }
                        }

                        // Set pawn to player faction
                        pawn.SetFaction(Faction.OfPlayer);

                        // Add a positive thought about being accepted
                        if (pawn.needs?.mood?.thoughts?.memories != null)
                        {
                            ThoughtDef accepted = DefDatabase<ThoughtDef>.GetNamedSilentFail("TheMarkedMen_RefugeeAccepted");
                            if (accepted != null)
                            {
                                pawn.needs.mood.thoughts.memories.TryGainMemory(accepted);
                            }
                        }

                        // Notify the player
                        Messages.Message("TheMarkedMen_MessageRefugeeAccepted".Translate(pawn.LabelShort),
                            pawn, MessageTypeDefOf.PositiveEvent);

                        // Clean up the refugee marker
                        CuredRefugeeManager.GetForMap(pawn.Map)?.RemoveRefugee(pawn);
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
                DiaOption diaOption = new DiaOption("TheMarkedMen_CuredRefugee_Reject".Translate());
                diaOption.action = () =>
                {
                    if (pawn != null && !pawn.Destroyed)
                    {
                        // Apply faction relation bonus for rejection (you're letting them go back)
                        if (originalFaction != null && !originalFaction.defeated)
                        {
                            Faction playerFaction = Faction.OfPlayer;
                            if (playerFaction != null && originalFaction != playerFaction)
                            {
                                FactionRelation relation = playerFaction.RelationWith(originalFaction);
                                if (relation != null)
                                {
                                    relation.baseGoodwill += 5;
                                }
                            }
                        }

                        // Keep pawn as wanderer (null faction) - do NOT return to original faction
                        // This ensures they don't turn back into Marked Men
                        pawn.SetFaction(null);

                        // Add rejection thought
                        if (pawn.needs?.mood?.thoughts?.memories != null)
                        {
                            ThoughtDef rejected = DefDatabase<ThoughtDef>.GetNamedSilentFail("TheMarkedMen_RefugeeRejected");
                            if (rejected != null)
                            {
                                pawn.needs.mood.thoughts.memories.TryGainMemory(rejected);
                            }
                        }

                        // Schedule the pawn to leave the map after a few days
                        CuredRefugeeManager.GetForMap(pawn.Map)?.ScheduleRefugeeDeparture(pawn);

                        // Notify the player
                        Messages.Message("TheMarkedMen_MessageRefugeeRejected".Translate(pawn.LabelShort),
                            pawn, MessageTypeDefOf.NeutralEvent);

                        // Clean up the refugee marker
                        CuredRefugeeManager.GetForMap(pawn.Map)?.RemoveRefugee(pawn);
                    }

                    Find.LetterStack.RemoveLetter(this);
                };
                diaOption.resolveTree = true;
                return diaOption;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref originalFaction, "originalFaction");
        }
    }
}
