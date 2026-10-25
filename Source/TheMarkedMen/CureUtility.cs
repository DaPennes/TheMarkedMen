using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

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

        // Hediffs from this mod that must survive a cure.
        private static readonly HashSet<string> PreservedModHediffs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CA_CrossVirusImmunity",
            "CA_Restrained",
            "TheMarkedMen_VictimRemorse"
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

            Faction targetFaction = ResolveCureFaction(origin?.faction);
            if (targetFaction != null && pawn.Faction != targetFaction)
                pawn.SetFaction(targetFaction, null);

            // Pawn.SetFaction already assigns the player's basic member kind
            // when a pawn joins the colony. For other factions restore the kind
            // recorded before the transformation.
            if (origin?.kind != null && pawn.Faction != Faction.OfPlayer && pawn.kindDef != origin.kind)
                pawn.ChangeKind(origin.kind);

            RestoreXenotype(pawn, origin);
            CrossedUtility.RemoveCrossedRashVisualsIfNotSuccumbed(pawn);

            HediffDef remorse = CADefOf.VictimRemorse;
            if (remorse != null && !pawn.health.hediffSet.HasHediff(remorse))
                pawn.health.AddHediff(remorse);

            HealInjuries(pawn);
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();

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
