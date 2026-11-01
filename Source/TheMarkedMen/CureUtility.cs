using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    /// <summary>
    /// Pre-transformation identity of a pawn that was turned into a Marked Man.
    /// Stored on the game component so the cure can restore it.
    /// </summary>
    public sealed class CureOriginRecord : IExposable
    {
        public Pawn pawn;
        public Faction faction;
        public PawnKindDef kind;
        public XenotypeDef xenotype;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Defs.Look(ref kind, "kind");
            Scribe_Defs.Look(ref xenotype, "xenotype");
        }
    }

    /// <summary>
    /// Shared cure logic used by the cure syringe item and the
    /// Health -> Operations "administer cure syringe" bill.
    /// </summary>
    public static class CureUtility
    {
        public const string CureSyringeDefName = "TheMarkedMen_Cure_Syringe";
        private const string ModPackageId = "dapennes.themarkedmen";
        private const int RemorseThoughtDurationDays = 90;

        // Hediffs from this mod that must survive a cure.
        private static readonly HashSet<string> PreservedModHediffs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CA_CrossVirusImmunity",
            "CA_Restrained",
            "TheMarkedMen_VictimRemorse",
            "TheMarkedMen_CurePending"
        };

        public static bool IsCureSyringe(Thing thing)
        {
            return thing != null && thing.def != null && thing.def.defName == CureSyringeDefName;
        }

        /// <summary>
        /// True when the pawn is infected, transformed, dormant-marked or a
        /// member of the Marked Men faction.
        /// </summary>
        public static bool IsInfected(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.health?.hediffSet == null)
                return false;

            FactionDef crossedDef = CADefOf.CrossedFaction;
            if (crossedDef != null && pawn.Faction != null && pawn.Faction.def == crossedDef)
                return true;

            return HasHediff(pawn, CADefOf.CrossVirus)
                || HasHediff(pawn, CADefOf.MarkedTurned)
                || HasHediff(pawn, CADefOf.CA_DormantMark)
                || HasHediff(pawn, CADefOf.CrossedStrength);
        }

        public static AcceptanceReport CanCure(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
                return "TheMarkedMen_OperationInvalidTarget".Translate();

            if (pawn.RaceProps == null || pawn.RaceProps.IsMechanoid || !pawn.RaceProps.IsFlesh)
                return "TheMarkedMen_OperationInvalidTarget".Translate();

            if (!IsInfected(pawn))
                return "TheMarkedMen_OperationNotInfected".Translate();

            if (HasPendingCure(pawn))
                return "TheMarkedMen_CureAlreadyWorking".Translate();

            return true;
        }

        public static bool HasPendingCure(Pawn pawn)
        {
            HediffDef pending = CADefOf.CurePending;
            return pending != null
                && pawn?.health?.hediffSet != null
                && pawn.health.hediffSet.HasHediff(pending);
        }

        public static bool CanActorUse(Pawn actor)
        {
            return actor != null
                && !actor.Dead
                && !actor.Downed
                && !actor.Destroyed
                && !actor.InMentalState
                && actor.Spawned
                && actor.Map != null;
        }

        public static int CountCureSyringesInInventory(Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer == null)
                return 0;

            int count = 0;
            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (IsCureSyringe(thing))
                    count += thing.stackCount;
            }

            return count;
        }

        public static Thing FindCureSyringeInInventory(Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer == null)
                return null;

            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (IsCureSyringe(thing))
                    return thing;
            }

            return null;
        }

        /// <summary>
        /// Queues the injection job. If the actor is not already carrying a
        /// cure syringe, the job first picks one up from
        /// <paramref name="cureSource"/>.
        /// </summary>
        public static bool TryStartInject(Pawn actor, Pawn target, Thing cureSource)
        {
            if (!CanActorUse(actor) || cureSource == null)
                return false;

            AcceptanceReport report = CanCure(target);
            if (!report.Accepted)
            {
                Messages.Message(report.Reason.NullOrEmpty()
                        ? "TheMarkedMen_CannotInject".Translate()
                        : report.Reason,
                    target, MessageTypeDefOf.RejectInput);
                return false;
            }

            if (target.MapHeld != actor.Map)
                return false;

            if (CountCureSyringesInInventory(actor) <= 0)
            {
                if (cureSource.DestroyedOrNull()
                    || !cureSource.Spawned
                    || cureSource.stackCount <= 0
                    || cureSource.IsForbidden(Faction.OfPlayer)
                    || !actor.CanReserve(cureSource, 1, 1))
                {
                    Messages.Message("TheMarkedMen_CannotInject".Translate(), actor, MessageTypeDefOf.RejectInput);
                    return false;
                }
            }

            JobDef jobDef = CADefOf.InjectCureJob;
            if (jobDef == null)
            {
                Log.Error("[The Marked Men] Missing JobDef TheMarkedMen_InjectCure; cannot inject the cure.");
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, target, cureSource);
            job.count = 1;
            actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            return true;
        }

        public static void BeginTargeting(Pawn actor, Thing cureSource)
        {
            if (!CanActorUse(actor) || cureSource == null)
                return;

            Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetPawns = true,
                canTargetHumans = true,
                canTargetAnimals = true,
                canTargetMechs = false,
                canTargetSubhumans = true,
                canTargetEntities = false,
                canTargetBuildings = false,
                canTargetFires = false,
                canTargetItems = false,
                canTargetLocations = false,
                canTargetPlants = false,
                validator = target =>
                {
                    Pawn pawn = target.Thing as Pawn;
                    return pawn != null && CanCure(pawn).Accepted;
                }
            }, target =>
            {
                Pawn pawn = target.Thing as Pawn;
                if (pawn != null)
                    TryStartInject(actor, pawn, cureSource);
            });
        }

        /// <summary>
        /// Starts the cure. The pawn gets a "cure working" status that counts
        /// down a random delay before the actual cure is applied.
        /// </summary>
        public static bool TryBeginCure(Pawn pawn, out string failureReason)
        {
            failureReason = null;
            AcceptanceReport report = CanCure(pawn);
            if (!report.Accepted)
            {
                failureReason = report.Reason;
                return false;
            }

            HediffDef pendingDef = CADefOf.CurePending;
            if (pendingDef == null)
            {
                // Fall back to an instant cure if the hediff def is missing.
                ApplyCure(pawn);
                return true;
            }

            // Check if a cure is already pending for this pawn.
            if (HasPendingCure(pawn))
            {
                failureReason = "TheMarkedMen_CureAlreadyWorking".Translate();
                return false;
            }

            Hediff pending = HediffMaker.MakeHediff(pendingDef, pawn);
            pawn.health.AddHediff(pending);
            Messages.Message("TheMarkedMen_MessageCureStarted".Translate(pawn.LabelShort),
                pawn, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        public static bool TryCure(Pawn pawn, out string failureReason)
        {
            failureReason = null;
            AcceptanceReport report = CanCure(pawn);
            if (!report.Accepted)
            {
                failureReason = report.Reason;
                return false;
            }

            ApplyCure(pawn);
            return true;
        }

        /// <summary>
        /// Directly applies the cure without validation checks.
        /// Used internally when the cure is ready to apply.
        /// </summary>
        public static void ApplyCureDirectly(Pawn pawn)
        {
            ApplyCure(pawn);
        }

        /// <summary>
        /// Consumes a single syringe from a stack (or the whole stack when it
        /// only contains one syringe).
        /// </summary>
        public static void ConsumeOne(Thing stack)
        {
            if (stack == null || stack.DestroyedOrNull() || stack.stackCount <= 0)
                return;

            if (stack.stackCount > 1)
                stack.SplitOff(1).Destroy();
            else
                stack.Destroy();
        }

        private static void ApplyCure(Pawn pawn)
        {
            CureOriginRecord origin = CrossedUtility.Component?.TakeCureOrigin(pawn);

            RemoveModHediffs(pawn);
            RemoveRestraint(pawn);

            Faction targetFaction = ResolveCureFaction(origin?.faction);
            if (targetFaction != null && pawn.Faction != targetFaction)
                pawn.SetFaction(targetFaction, null);

            // Pawn.SetFaction already assigns the player's basic member kind
            // when a pawn joins the colony. For other factions restore the kind
            // recorded before the transformation.
            if (origin?.kind != null && pawn.Faction != Faction.OfPlayer && pawn.kindDef != origin.kind)
                pawn.ChangeKind(origin.kind);

            RestoreXenotype(pawn, origin);

            HediffDef remorse = CADefOf.VictimRemorse;
            if (remorse != null && !pawn.health.hediffSet.HasHediff(remorse))
                pawn.health.AddHediff(remorse);

            AddRemorseThought(pawn);

            Messages.Message("TheMarkedMen_MessageCured".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent);
        }

        private static void RemoveModHediffs(Pawn pawn)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            List<Hediff> toRemove = null;

            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (hediff?.def == null || PreservedModHediffs.Contains(hediff.def.defName))
                    continue;

                ModContentPack pack = hediff.def.modContentPack;
                if (pack == null || !string.Equals(pack.PackageId, ModPackageId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (toRemove == null)
                    toRemove = new List<Hediff>();
                toRemove.Add(hediff);
            }

            if (toRemove == null)
                return;

            for (int i = 0; i < toRemove.Count; i++)
                pawn.health.RemoveHediff(toRemove[i]);
        }

        private static void RemoveRestraint(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return;

            HediffDef restraintDef = CADefOf.CA_Restrained;
            if (restraintDef == null)
                return;

            Hediff restraint = pawn.health.hediffSet.GetFirstHediffOfDef(restraintDef);
            if (restraint != null)
            {
                pawn.health.RemoveHediff(restraint);
            }
        }

        private static Faction ResolveCureFaction(Faction recorded)
        {
            Faction player = Faction.OfPlayer;
            if (player == null)
                return recorded;

            if (recorded == null || recorded == player || recorded.defeated || recorded.Hidden)
                return player;

            Faction crossed = Find.FactionManager?.FirstFactionOfDef(CADefOf.CrossedFaction);
            if (recorded == crossed)
                return player;

            return recorded;
        }

        private static void RestoreXenotype(Pawn pawn, CureOriginRecord origin)
        {
            if (!ModsConfig.BiotechActive || pawn.genes == null || pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
                return;

            XenotypeDef target = origin?.xenotype;
            if (target == null)
            {
                XenotypeDef markedOne = CADefOf.MarkedOne;
                if (markedOne != null && pawn.genes.Xenotype == markedOne)
                    target = XenotypeDefOf.Baseliner;
            }

            if (target != null && pawn.genes.Xenotype != target)
                pawn.genes.SetXenotypeDirect(target);
        }

        private static void AddRemorseThought(Pawn pawn)
        {
            ThoughtDef thoughtDef = CADefOf.RememberEverything;
            if (thoughtDef == null || pawn?.needs?.mood?.thoughts?.memories == null)
                return;

            if (pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(thoughtDef) == null)
                pawn.needs.mood.thoughts.memories.TryGainMemoryFast(thoughtDef);

            Thought_Memory memory = pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(thoughtDef);
            if (memory != null)
            {
                memory.permanent = false;
                memory.durationTicksOverride = RemorseThoughtDurationDays * GenDate.TicksPerDay;
            }
        }

        /// <summary>
        /// Converts memories left by the earlier permanent version of the
        /// debuff into 90-day memories. Does not re-add expired memories.
        /// </summary>
        public static void NormalizeRemorseThought(Pawn pawn)
        {
            ThoughtDef thoughtDef = CADefOf.RememberEverything;
            if (thoughtDef == null || pawn?.needs?.mood?.thoughts?.memories == null)
                return;

            Thought_Memory memory = pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(thoughtDef);
            if (memory == null)
                return;

            memory.permanent = false;
            memory.durationTicksOverride = RemorseThoughtDurationDays * GenDate.TicksPerDay;
        }

        private static void HealInjuries(Pawn pawn)
        {
            List<Hediff> hediffs = new List<Hediff>();
            pawn.health.hediffSet.GetHediffs(ref hediffs);

            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff_Injury injury = hediffs[i] as Hediff_Injury;
                if (injury != null)
                    injury.Heal(injury.Severity);
            }
        }

        private static bool HasHediff(Pawn pawn, HediffDef def)
        {
            return def != null && pawn.health.hediffSet.HasHediff(def);
        }
    }
}
