using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static partial class CrossedUtility
    {
        private const string ArmorStripDueTagPrefix = "CA_CrossedArmorStripDue:";

        private static readonly List<KeyValuePair<PawnKindDef, float>> TransformationKinds = new List<KeyValuePair<PawnKindDef, float>>();

        public static void TransformPawn(Pawn pawn, bool suppressNotification = false, Pawn infector = null)
        {
            if (pawn == null || pawn.Dead) return;
            Faction faction = Component?.EnsureCrossedFaction();
            PawnKindDef kind = PickTransformationKind(pawn);
            if (kind != null && pawn.kindDef != kind)
                pawn.ChangeKind(kind);
            if (faction != null && pawn.Faction != faction)
                pawn.SetFaction(faction, null);
            pawn.guest?.SetGuestStatus(null);
            pawn.mindState?.mentalStateHandler?.Reset();
            EnsureFearlessCrossedState(pawn);
            ApplyClassHediffs(pawn);
            EnsureCrossedBasicClothingOnly(pawn);
            if (pawn.Drawer?.renderer != null)
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            ApplyInfectedTattoo(pawn);
            CrossedTacticalAI.TryAttackNearestNonInfectedPawn(pawn, true, allowRjwJob: false);
            if (suppressNotification) return;
            Component?.NotifyTransformation(pawn);
        }

        public static void ApplyGeneratedRaidKindTuning(List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0) return;
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            int maxAlphas = Mathf.Clamp(settings?.maximumAlphasPerRaid ?? 99, 0, 99);
            bool allowChildren = settings?.allowMarkedChildren == true;
            int alphaCount = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead) continue;
                bool replace = false;
                if (pawn.kindDef == CADefOf.Alpha || pawn.kindDef == CADefOf.AlphaPsychic)
                {
                    alphaCount++;
                    replace = alphaCount > maxAlphas || TheMarkedMenSettings.AdjustKindWeight(pawn.kindDef, 1f) <= 0f;
                }
                else if (pawn.kindDef == CADefOf.Child)
                {
                    replace = !allowChildren;
                }
                if (replace)
                {
                    PawnKindDef replacement = PickReplacementMarkedKind();
                    if (replacement != null && replacement != pawn.kindDef)
                    {
                        pawn.ChangeKind(replacement);
                        RemoveClassHediffs(pawn);
                        ApplyClassHediffs(pawn);
                        ApplyInfectedTattoo(pawn);
                    }
                }
            }
        }

        private static PawnKindDef PickReplacementMarkedKind()
        {
            PawnKindDef selected = null;
            float totalWeight = 0f;
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Berserker, 12f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Hunter, 8f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Stalker, 4f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Screamer, 3f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Charger, 3f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Spitter, 2.5f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Bomber, 2f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Brute, 2f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.Alpha, 0.5f);
            AddReplacementKind(ref selected, ref totalWeight, CADefOf.AlphaPsychic, 0.25f);
            return selected ?? CADefOf.Berserker ?? CADefOf.Hunter ?? CADefOf.Stalker ?? CADefOf.Screamer ?? CADefOf.Charger ?? CADefOf.Brute;
        }

        private static void AddReplacementKind(ref PawnKindDef selected, ref float totalWeight, PawnKindDef kind, float baseWeight)
        {
            float weight = TheMarkedMenSettings.AdjustKindWeight(kind, baseWeight);
            if (kind == null || weight <= 0f) return;
            totalWeight += weight;
            if (Rand.Value * totalWeight <= weight)
                selected = kind;
        }

        public static void RemoveClassHediffs(Pawn pawn)
        {
            RemoveHediffIfPresent(pawn, CADefOf.BloodRush);
            RemoveHediffIfPresent(pawn, CADefOf.CommandAura);
            RemoveHediffIfPresent(pawn, CADefOf.PsychicAura);
            RemoveHediffIfPresent(pawn, CADefOf.SpitterGlands);
            RemoveHediffIfPresent(pawn, CADefOf.BomberCharge);
        }

        private static void RemoveHediffIfPresent(Pawn pawn, HediffDef def)
        {
            Hediff existing = def == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(def);
            if (existing != null)
                pawn.health.RemoveHediff(existing);
        }

        public static void ApplyClassHediffs(Pawn pawn)
        {
            if (pawn == null || pawn.health == null) return;
            RemoveDeprecatedCrossedRashHediff(pawn);
            if (!ShouldShowCrossedRash(pawn))
            {
                RemoveCrossedRashVisualsIfNotSuccumbed(pawn);
                return;
            }
            EnsureFearlessCrossedState(pawn);
            if (!IsCrossedFactionPawn(pawn))
            {
                HediffDef virus = CADefOf.CrossVirus;
                if (virus != null)
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(virus) ?? pawn.health.AddHediff(virus);
                    hediff.Severity = 1f;
                }
            }
            if (pawn.kindDef == CADefOf.Berserker && CADefOf.BloodRush != null && !pawn.health.hediffSet.HasHediff(CADefOf.BloodRush))
                pawn.health.AddHediff(CADefOf.BloodRush);
            else if (pawn.kindDef == CADefOf.Alpha && CADefOf.CommandAura != null && !pawn.health.hediffSet.HasHediff(CADefOf.CommandAura))
                pawn.health.AddHediff(CADefOf.CommandAura);
            else if (pawn.kindDef == CADefOf.AlphaPsychic && CADefOf.PsychicAura != null && !pawn.health.hediffSet.HasHediff(CADefOf.PsychicAura))
            {
                pawn.health.AddHediff(CADefOf.PsychicAura);
                VPECompat.TryApplyPsylink(pawn);
            }
            else if (pawn.kindDef == CADefOf.Spitter && CADefOf.SpitterGlands != null && !pawn.health.hediffSet.HasHediff(CADefOf.SpitterGlands))
                pawn.health.AddHediff(CADefOf.SpitterGlands);
            else if (pawn.kindDef == CADefOf.Bomber && CADefOf.BomberCharge != null && !pawn.health.hediffSet.HasHediff(CADefOf.BomberCharge))
                pawn.health.AddHediff(CADefOf.BomberCharge);
            ApplyInfectedTattoo(pawn);
            EnsureCrossedBasicClothingOnly(pawn);
        }

        public static void EnsureCrossedBasicClothingOnly(Pawn pawn)
        {
            ClearArmorStripDueTick(pawn);
        }

        private static void ClearArmorStripDueTick(Pawn pawn)
        {
            List<string> tags = pawn?.questTags;
            if (tags == null) return;
            for (int i = tags.Count - 1; i >= 0; i--)
            {
                string tag = tags[i];
                if (!tag.NullOrEmpty() && tag.StartsWith(ArmorStripDueTagPrefix, System.StringComparison.Ordinal))
                    tags.RemoveAt(i);
            }
        }

        public static void ApplyScreamerPanic(Map map, IntVec3 origin, float radius)
        {
            HediffDef panic = CADefOf.Panic;
            if (map == null || panic == null) return;
            float effectiveRadius = Mathf.Max(0f, radius * Mathf.Sqrt(TheMarkedMenSettings.SocialTerrorStrength));
            if (effectiveRadius <= 0f) return;
            foreach (Pawn pawn in map.mapPawns.FreeColonistsAndPrisonersSpawned)
            {
                if (pawn.Position.InHorDistOf(origin, effectiveRadius) && !pawn.health.hediffSet.HasHediff(panic))
                    pawn.health.AddHediff(panic);
            }
        }

        private static PawnKindDef PickTransformationKind(Pawn pawn)
        {
            TransformationKinds.Clear();
            AddKind(CADefOf.Berserker, 1f);
            AddKind(CADefOf.Hunter, 1f);
            AddKind(CADefOf.Stalker, 1f);
            AddKind(CADefOf.Screamer, 1f);
            if (Rand.Chance(0.12f))
                AddKind(CADefOf.Brute, 1f);
            if (Rand.Chance(0.08f))
                AddKind(CADefOf.Charger, 1f);
            if (Rand.Chance(0.06f))
                AddKind(CADefOf.Spitter, 1f);
            if (Rand.Chance(0.04f))
                AddKind(CADefOf.Bomber, 1f);
            if (Rand.Chance(0.02f))
                AddKind(CADefOf.Alpha, 1f);
            if (Rand.Chance(0.01f))
                AddKind(CADefOf.AlphaPsychic, 1f);
            if (TheMarkedMenMod.Settings?.allowMarkedChildren == true && pawn?.ageTracker != null && pawn.ageTracker.AgeBiologicalYearsFloat < 13f)
                AddKind(CADefOf.Child, 1f);
            if (TransformationKinds.Count == 0)
                return pawn.kindDef;
            return PickWeightedKind(TransformationKinds) ?? pawn.kindDef;
        }

        private static void AddKind(PawnKindDef kind, float baseWeight)
        {
            float weight = TheMarkedMenSettings.AdjustKindWeight(kind, baseWeight);
            if (kind != null && weight > 0f)
                TransformationKinds.Add(new KeyValuePair<PawnKindDef, float>(kind, weight));
        }

        private static PawnKindDef PickWeightedKind(List<KeyValuePair<PawnKindDef, float>> kinds)
        {
            float totalWeight = 0f;
            for (int i = 0; i < kinds.Count; i++)
                totalWeight += Mathf.Max(0f, kinds[i].Value);
            if (totalWeight <= 0f) return null;
            float pick = Rand.Value * totalWeight;
            for (int i = 0; i < kinds.Count; i++)
            {
                pick -= Mathf.Max(0f, kinds[i].Value);
                if (pick <= 0f)
                    return kinds[i].Key;
            }
            return kinds[kinds.Count - 1].Key;
        }

        public static bool ShouldReanimateAsCrossed(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.RaceProps == null || !pawn.RaceProps.Humanlike || WasReanimatedAsCrossed(pawn) || DiedFromMarkedVirusWithoutReanimation(pawn))
                return false;
            HediffDef virus = CADefOf.CrossVirus;
            if (virus != null && pawn.health?.hediffSet?.HasHediff(virus) == true) return true;
            FactionDef crossed = CADefOf.CrossedFaction;
            return crossed != null && pawn.Faction?.def == crossed;
        }

        public static void MarkReanimatedAsCrossed(Pawn pawn)
        {
            if (pawn == null) return;
            if (pawn.questTags == null) pawn.questTags = new List<string>();
            if (!pawn.questTags.Contains(ReanimatedQuestTag))
                pawn.questTags.Add(ReanimatedQuestTag);
        }

        public static void MarkDiedFromMarkedVirus(Pawn pawn)
        {
            if (pawn == null) return;
            if (pawn.questTags == null) pawn.questTags = new List<string>();
            if (!pawn.questTags.Contains(MarkedVirusFatalityQuestTag))
                pawn.questTags.Add(MarkedVirusFatalityQuestTag);
        }

        private static bool WasReanimatedAsCrossed(Pawn pawn)
        {
            return pawn?.questTags != null && pawn.questTags.Contains(ReanimatedQuestTag);
        }

        private static bool DiedFromMarkedVirusWithoutReanimation(Pawn pawn)
        {
            return pawn?.questTags != null && pawn.questTags.Contains(MarkedVirusFatalityQuestTag);
        }

        internal static void NotifyInfectionRetarget(Pawn infected, Pawn infector)
        {
            if (infected?.Map == null) return;
            if (infector != null && infector.Spawned && infector.Map == infected.Map && IsInfectedPawn(infector))
                CrossedTacticalAI.TryRetargetAwayFromPawn(infector, infected, true);
            Map map = infected.Map;
            int numCells = GenRadial.NumCellsInRadius(12f);
            for (int cellIndex = 0; cellIndex < numCells; cellIndex++)
            {
                IntVec3 cell = infected.Position + GenRadial.ManualRadialPattern[cellIndex];
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = map.thingGrid.ThingsListAt(cell);
                for (int thingIndex = 0; thingIndex < things.Count; thingIndex++)
                {
                    Pawn pawn = things[thingIndex] as Pawn;
                    if (pawn != null && pawn != infector && pawn != infected && IsInfectedPawn(pawn))
                        CrossedTacticalAI.TryRetargetAwayFromPawn(pawn, infected, false);
                }
            }
        }
    }
}
