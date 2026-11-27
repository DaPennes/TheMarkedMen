using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{




    public class ChoiceLetter_CuredRefugee : ChoiceLetter
    {
        public Pawn pawn;
        public Faction originalFaction;

        public override bool CanDismissWithRightClick => false;

        public ChoiceLetter_CuredRefugee()
        {

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
                    if (pawn == null || pawn.Dead || !pawn.Spawned)
                    {
                        Find.LetterStack.RemoveLetter(this);
                        return;
                    }

                    try
                    {
                        Faction playerFaction = Faction.OfPlayer;

                        // Validate original faction and its relation with player
                        if (originalFaction != null
                            && !originalFaction.defeated
                            && originalFaction != playerFaction
                            && playerFaction != null
                            && pawn.Faction != null)
                        {
                            FactionRelation relation = playerFaction.RelationWith(originalFaction);
                            if (relation != null)
                            {
                                relation.baseGoodwill -= 1;
                            }
                        }

                        if (pawn.Faction == null || pawn.Faction != Faction.OfPlayer)
                        {
                            pawn.SetFaction(Faction.OfPlayer);
                        }

                        if (pawn.needs?.mood?.thoughts?.memories != null)
                        {
                            ThoughtDef accepted = DefDatabase<ThoughtDef>.GetNamedSilentFail("TheMarkedMen_RefugeeAccepted");
                            if (accepted != null)
                            {
                                pawn.needs.mood.thoughts.memories.TryGainMemory(accepted);
                            }
                        }

                        Messages.Message("TheMarkedMen_MessageRefugeeAccepted".Translate(pawn.LabelShort),
                            pawn, MessageTypeDefOf.PositiveEvent);

                        CuredRefugeeManager.GetForMap(pawn.Map)?.RemoveRefugee(pawn);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning("[The Marked Men] Error during refugee acceptance: " + ex.Message);
                    }
                    finally
                    {
                        Find.LetterStack.RemoveLetter(this);
                    }
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
                    if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
                    {
                        Find.LetterStack.RemoveLetter(this);
                        return;
                    }

                    try
                    {
                        Faction playerFaction = Faction.OfPlayer;

                        // Validate original faction and its relation with player
                        if (originalFaction != null
                            && !originalFaction.defeated
                            && originalFaction != playerFaction
                            && playerFaction != null)
                        {
                            FactionRelation relation = playerFaction.RelationWith(originalFaction);
                            if (relation != null)
                            {
                                relation.baseGoodwill += 5;
                            }
                        }

                        // Only clear faction if pawn currently has a non-player faction
                        if (pawn.Faction != null)
                        {
                            pawn.SetFaction(null);
                        }

                        if (pawn.needs?.mood?.thoughts?.memories != null)
                        {
                            ThoughtDef rejected = DefDatabase<ThoughtDef>.GetNamedSilentFail("TheMarkedMen_RefugeeRejected");
                            if (rejected != null)
                            {
                                pawn.needs.mood.thoughts.memories.TryGainMemory(rejected);
                            }
                        }

                        CuredRefugeeManager.GetForMap(pawn.Map)?.ScheduleRefugeeDeparture(pawn);

                        Messages.Message("TheMarkedMen_MessageRefugeeRejected".Translate(pawn.LabelShort),
                            pawn, MessageTypeDefOf.NeutralEvent);

                        CuredRefugeeManager.GetForMap(pawn.Map)?.RemoveRefugee(pawn);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning("[The Marked Men] Error during refugee rejection: " + ex.Message);
                    }
                    finally
                    {
                        Find.LetterStack.RemoveLetter(this);
                    }
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
