using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static partial class CrossedUtility
    {
        private const string ReanimatedQuestTag = "CA_ReanimatedAsCrossed";
        private const string MarkedVirusFatalityQuestTag = "CA_MarkedVirusFatalityNoReanimation";
        private const string FearlessDueToCrossVirusTag = "CA_FearlessDueToCrossVirus";
        private const string StarterLineageResistanceTag = "CA_StarterLineageResistance";
        private const string MarkedVillageFounderTag = "CA_MarkedVillageFounder";
        private const string PersistentCrossedRashTag = "CA_PersistentCrossedRashTattoo";
        private const string MarkedVillageRashRolledTag = "CA_MarkedVillageRashRolled";
        private const float CrossVirusStage2Severity = 0.20f;
        private const float CrossVirusStage3Severity = 0.45f;
        private const float CrossVirusStage4Severity = 0.72f;
        private const float CrossVirusFinalStageSeverity = 0.95f;
        private const float MarkedVillageRashChance = 0.5f;

        public static TheMarkedMenGameComponent Component => Current.Game?.GetComponent<TheMarkedMenGameComponent>();

        public static bool IsCrossedPawn(Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.health?.hediffSet?.HasHediff(CADefOf.CrossVirus) == true && pawn.health.hediffSet.GetFirstHediffOfDef(CADefOf.CrossVirus)?.Severity >= 0.92f) return true;
            FactionDef crossed = CADefOf.CrossedFaction;
            return crossed != null && pawn.Faction?.def == crossed;
        }

        public static bool IsInfectedPawn(Pawn pawn)
        {
            if (pawn == null || pawn.def?.race == null || !pawn.def.race.Humanlike || pawn.health == null || pawn.health.Dead) return false;
            HediffDef virus = CADefOf.CrossVirus;
            if (virus != null && pawn.health?.hediffSet?.HasHediff(virus) == true) return true;
            FactionDef crossed = CADefOf.CrossedFaction;
            return crossed != null && pawn.Faction?.def == crossed;
        }

        public static bool HasMarkedVirusHediff(Pawn pawn)
        {
            HediffDef virus = CADefOf.CrossVirus;
            return pawn?.health?.hediffSet != null && virus != null && pawn.health.hediffSet.HasHediff(virus);
        }

        public static bool IsFullyTurnedMarkedPawn(Pawn pawn)
        {
            return IsCrossedFactionPawn(pawn);
        }

        public static bool IsPartiallyMarkedPawn(Pawn pawn)
        {
            return HasMarkedVirusHediff(pawn) && !IsFullyTurnedMarkedPawn(pawn);
        }

        public static bool CanSafelyProcessInfectedState(Pawn pawn)
        {
            return pawn != null && !pawn.Destroyed && pawn.def?.race != null && pawn.def.race.Humanlike && pawn.health?.hediffSet != null && !pawn.health.Dead;
        }

        public static bool HasCrossVirusImmunity(Pawn pawn)
        {
            HediffDef immunity = CADefOf.CrossVirusImmunity;
            return pawn?.health?.hediffSet != null && immunity != null && pawn.health.hediffSet.HasHediff(immunity);
        }

        public static bool IsFullyProtectedFromCrossVirusExposure(Pawn pawn)
        {
            return HasMarkedVillageFounderImmunity(pawn)
                || HasCrossVirusImmunity(pawn) && !HasStarterLineageResistance(pawn)
                || HasSealedMarkedVirusExposureProtection(pawn);
        }

        public static bool HasSealedMarkedVirusExposureProtection(Pawn pawn)
        {
            return GetMarkedVirusExposureProtection(pawn).blocksMarkedVirusExposure;
        }

        public static void GrantCrossVirusImmunity(Pawn pawn)
        {
            HediffDef immunity = CADefOf.CrossVirusImmunity;
            if (pawn?.health == null || immunity == null || pawn.health.hediffSet.HasHediff(immunity)) return;
            pawn.health.AddHediff(immunity);
        }

        private static void RemoveCrossVirusImmunity(Pawn pawn)
        {
            HediffDef immunity = CADefOf.CrossVirusImmunity;
            Hediff existing = immunity == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(immunity);
            if (existing != null) pawn.health.RemoveHediff(existing);
        }

        public static bool HasStarterLineageResistance(Pawn pawn)
        {
            return pawn?.questTags != null && pawn.questTags.Contains(StarterLineageResistanceTag);
        }

        public static bool HasMarkedVillageFounderImmunity(Pawn pawn)
        {
            return pawn?.questTags != null && pawn.questTags.Contains(MarkedVillageFounderTag);
        }

        public static bool HasPersistentCrossedRashTattoo(Pawn pawn)
        {
            return pawn?.questTags != null && pawn.questTags.Contains(PersistentCrossedRashTag);
        }

        public static bool GrantMarkedVillageFounderState(Pawn pawn)
        {
            if (!CanReceiveMarkedVillageFounderState(pawn)) return false;
            if (pawn.questTags == null) pawn.questTags = new List<string>();
            bool changed = AddQuestTagIfMissing(pawn, MarkedVillageFounderTag);
            if (!pawn.questTags.Contains(MarkedVillageRashRolledTag))
            {
                changed |= AddQuestTagIfMissing(pawn, MarkedVillageRashRolledTag);
                if (HasPersistentCrossedRashTattoo(pawn) || Rand.Chance(MarkedVillageRashChance))
                {
                    changed |= AddQuestTagIfMissing(pawn, PersistentCrossedRashTag);
                }
            }
            GrantCrossVirusImmunity(pawn);
            if (HasPersistentCrossedRashTattoo(pawn))
                ApplyInfectedTattoo(pawn);
            else
                RemoveCrossedRashVisualsIfNotSuccumbed(pawn);
            return changed;
        }

        public static bool TryMarkStarterLineageResistant(Pawn pawn)
        {
            if (!CanReceiveStarterLineageResistance(pawn)) return false;
            if (pawn.questTags == null) pawn.questTags = new List<string>();
            bool added = !pawn.questTags.Contains(StarterLineageResistanceTag);
            if (added) pawn.questTags.Add(StarterLineageResistanceTag);
            GrantCrossVirusImmunity(pawn);
            return added;
        }

        public static void EnsureStarterLineageResistance(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.Humanlike || IsInfectedPawn(pawn)) return;
            if (HasStarterLineageResistance(pawn))
            {
                GrantCrossVirusImmunity(pawn);
                return;
            }
            if (HasStarterLineageParent(pawn))
                TryMarkStarterLineageResistant(pawn);
        }

        private static bool CanReceiveStarterLineageResistance(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && pawn.RaceProps != null && pawn.RaceProps.Humanlike
                && !HasMarkedVillageFounderImmunity(pawn) && !IsInfectedPawn(pawn) && !IsCrossedPawn(pawn);
        }

        private static bool CanReceiveMarkedVillageFounderState(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && pawn.RaceProps != null && pawn.RaceProps.Humanlike
                && !IsInfectedPawn(pawn) && !IsCrossedPawn(pawn);
        }

        private static bool AddQuestTagIfMissing(Pawn pawn, string tag)
        {
            if (pawn?.questTags == null || pawn.questTags.Contains(tag)) return false;
            pawn.questTags.Add(tag);
            return true;
        }

        private static bool HasStarterLineageParent(Pawn pawn)
        {
            List<DirectPawnRelation> relations = pawn?.relations?.DirectRelations;
            if (relations == null) return false;
            for (int i = 0; i < relations.Count; i++)
            {
                DirectPawnRelation relation = relations[i];
                if (relation?.otherPawn != null && IsParentRelation(relation.def) && HasStarterLineageResistance(relation.otherPawn))
                    return true;
            }
            return false;
        }

        private static bool IsParentRelation(PawnRelationDef relationDef)
        {
            if (relationDef == null) return false;
            return relationDef == PawnRelationDefOf.Parent
                || string.Equals(relationDef.defName, "ParentBirth", StringComparison.Ordinal);
        }

        private static bool IsCrossedFactionPawn(Pawn pawn)
        {
            FactionDef crossed = CADefOf.CrossedFaction;
            return pawn != null && crossed != null && pawn.Faction?.def == crossed;
        }

        public static void EnsureInfectedState(Pawn pawn)
        {
            if (!CanSafelyProcessInfectedState(pawn)) return;
            RemoveDeprecatedCrossedRashHediff(pawn);
            if (IsCrossedFactionPawn(pawn))
            {
                ApplyClassHediffs(pawn);
                return;
            }
            HediffDef virus = CADefOf.CrossVirus;
            Hediff hediff = virus == null ? null : pawn.health?.hediffSet?.GetFirstHediffOfDef(virus);
            if (hediff == null)
            {
                RestoreFleeStateIfRecovered(pawn);
                RemoveCrossedRashVisualsIfNotSuccumbed(pawn);
                return;
            }
            hediff.Severity = Mathf.Max(hediff.Severity, InitialCrossVirusSeverity(virus));
            EnsureFearlessCrossedState(pawn);
            ApplyInfectedTattoo(pawn);
            CrossedContagionUtility.TryContagionPulse(pawn);
            CrossedCorpseUtility.TryContaminateNearbyCorpses(pawn);
        }

        public static void RemoveMarkedVirusHediffFromFullyTurnedPawn(Pawn pawn)
        {
            HediffDef virus = IsCrossedFactionPawn(pawn) ? CADefOf.CrossVirus : null;
            Hediff existing = virus == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(virus);
            if (existing != null) pawn.health.RemoveHediff(existing);
        }

        public static void ApplyInfectedTattooIfInfected(Pawn pawn)
        {
            EnsureInfectedState(pawn);
        }

        public static void RemoveDeprecatedCrossedRashHediff(Pawn pawn)
        {
            HediffDef rash = CADefOf.CrossedRash;
            Hediff existingRash = rash == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(rash);
            if (existingRash != null)
            {
                pawn.health.RemoveHediff(existingRash);
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        public static void RemoveCrossedRashVisualsIfNotSuccumbed(Pawn pawn)
        {
            if (pawn == null || ShouldShowCrossedRash(pawn)) return;
            if (pawn.style != null)
            {
                TattooDef noFaceTattoo = TattooDefOf.NoTattoo_Face;
                if (CADefOf.IsCrossedFaceTattoo(pawn.style.nextFaceTattooDef))
                {
                    pawn.style.nextFaceTattooDef = noFaceTattoo;
                }
                if (CADefOf.IsCrossedFaceTattoo(pawn.style.FaceTattoo))
                {
                    pawn.style.FaceTattoo = noFaceTattoo;
                    pawn.style.Notify_StyleItemChanged();
                }
            }
        }

        private static float InitialCrossVirusSeverity(HediffDef virus)
        {
            return Mathf.Clamp(virus?.initialSeverity ?? 0.08f, 0.001f, 1f);
        }
    }
}
