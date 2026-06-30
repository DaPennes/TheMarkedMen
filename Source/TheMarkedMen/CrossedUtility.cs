using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TheMarkedMen
{
	public static class CrossedUtility
	{
		private const string ReanimatedQuestTag = "CA_ReanimatedAsCrossed";
		private const string MarkedVirusFatalityQuestTag = "CA_MarkedVirusFatalityNoReanimation";
		private const string ArmorStripDueTagPrefix = "CA_CrossedArmorStripDue:";
		private const string FearlessDueToCrossVirusTag = "CA_FearlessDueToCrossVirus";
		private const float DefaultApparelVirusResistance = 0.02f;
		private const float MaxWornMarkedVirusResistance = 0.45f;
		private const string StarterLineageResistanceTag = "CA_StarterLineageResistance";
		private const string MarkedVillageFounderTag = "CA_MarkedVillageFounder";
		private const string PersistentCrossedRashTag = "CA_PersistentCrossedRashTattoo";
		private const string MarkedVillageRashRolledTag = "CA_MarkedVillageRashRolled";
		private const float CrossVirusStage2Severity = 0.20f;
		private const float CrossVirusStage3Severity = 0.45f;
		private const float CrossVirusStage4Severity = 0.72f;
		private const float CrossVirusFinalStageSeverity = 0.95f;
		private const float MarkedVillageRashChance = 0.5f;

		private static readonly List<KeyValuePair<PawnKindDef, float>> TransformationKinds = new List<KeyValuePair<PawnKindDef, float>>();

		public static TheMarkedMenGameComponent Component => Current.Game?.GetComponent<TheMarkedMenGameComponent>();

		public static bool IsCrossedPawn(Pawn pawn)
		{
			if (pawn == null)
			{
				return false;
			}

			if (pawn.health?.hediffSet?.HasHediff(CADefOf.CrossVirus) == true && pawn.health.hediffSet.GetFirstHediffOfDef(CADefOf.CrossVirus)?.Severity >= 0.92f)
			{
				return true;
			}

			FactionDef crossed = CADefOf.CrossedFaction;
			return crossed != null && pawn.Faction?.def == crossed;
		}

		public static bool IsInfectedPawn(Pawn pawn)
		{
			if (pawn == null || pawn.def?.race == null || !pawn.def.race.Humanlike || pawn.health == null || pawn.health.Dead)
			{
				return false;
			}

			HediffDef virus = CADefOf.CrossVirus;
			if (virus != null && pawn.health?.hediffSet?.HasHediff(virus) == true)
			{
				return true;
			}

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

		public static bool ShouldShowCrossedRash(Pawn pawn)
		{
			return HasMarkedVirusHediff(pawn) || IsCrossedFactionPawn(pawn) || HasPersistentCrossedRashTattoo(pawn);
		}

		public static TattooDef GetCurrentCrossedFaceTattoo(Pawn pawn)
		{
			TattooDef finalTattoo = CADefOf.CrossedFaceTattoo;
			if (pawn == null || IsCrossedFactionPawn(pawn))
			{
				return finalTattoo;
			}

			HediffDef virus = CADefOf.CrossVirus;
			Hediff hediff = virus == null ? null : pawn.health?.hediffSet?.GetFirstHediffOfDef(virus);
			if (hediff == null)
			{
				return finalTattoo;
			}

			return CrossedFaceTattooForSeverity(hediff.Severity) ?? finalTattoo;
		}

		private static TattooDef CrossedFaceTattooForSeverity(float severity)
		{
			if (severity >= CrossVirusFinalStageSeverity)
			{
				return CADefOf.CrossedFaceTattoo;
			}

			if (severity >= CrossVirusStage4Severity)
			{
				return CADefOf.CrossedFaceTattooStage4 ?? CADefOf.CrossedFaceTattoo;
			}

			if (severity >= CrossVirusStage3Severity)
			{
				return CADefOf.CrossedFaceTattooStage3 ?? CADefOf.CrossedFaceTattoo;
			}

			if (severity >= CrossVirusStage2Severity)
			{
				return CADefOf.CrossedFaceTattooStage2 ?? CADefOf.CrossedFaceTattoo;
			}

			return CADefOf.CrossedFaceTattooStage1 ?? CADefOf.CrossedFaceTattoo;
		}

		public static void EnsureFearlessCrossedState(Pawn pawn)
		{
			if (!IsInfectedPawn(pawn) || pawn.mindState == null)
			{
				return;
			}

			MarkFearlessDueToCrossVirus(pawn);
			Pawn_MindState mindState = pawn.mindState;
			mindState.canFleeIndividual = false;
			mindState.exitMapAfterTick = -1;
			mindState.meleeThreat = null;

			MentalStateHandler handler = mindState.mentalStateHandler;
			if (handler == null)
			{
				return;
			}

			handler.neverFleeIndividual = true;
			if (IsFearOrWithdrawalMentalState(handler.CurStateDef))
			{
				handler.Reset();
			}
		}

		public static void RestoreFleeStateIfRecovered(Pawn pawn)
		{
			if (pawn == null || IsInfectedPawn(pawn) || !RemoveFearlessDueToCrossVirusTag(pawn) || pawn.mindState == null)
			{
				return;
			}

			Pawn_MindState mindState = pawn.mindState;
			mindState.canFleeIndividual = true;
			MentalStateHandler handler = mindState.mentalStateHandler;
			if (handler != null)
			{
				handler.neverFleeIndividual = false;
			}
		}

        private static void MarkFearlessDueToCrossVirus(Pawn pawn)
        {
            if (pawn == null || IsCrossedFactionPawn(pawn))
            {
                return;
            }

            AddQuestTagIfMissing(pawn, FearlessDueToCrossVirusTag);
        }

        private static bool RemoveFearlessDueToCrossVirusTag(Pawn pawn)
		{
			List<string> tags = pawn?.questTags;
			if (tags == null)
			{
				return false;
			}

			return tags.Remove(FearlessDueToCrossVirusTag);
		}

		private static bool IsFearOrWithdrawalMentalState(MentalStateDef def)
		{
			if (def == null)
			{
				return false;
			}

			return def == MentalStateDefOf.PanicFlee
				|| def == MentalStateDefOf.PanicFleeFire
				|| def == MentalStateDefOf.Terror
				|| def == MentalStateDefOf.Wander_Psychotic
				|| def == MentalStateDefOf.Wander_Sad
				|| def == MentalStateDefOf.Wander_OwnRoom
				|| def == MentalStateDefOf.Roaming;
		}

		public static bool ShouldReanimateAsCrossed(Pawn pawn)
		{
			if (pawn == null || pawn.Destroyed || pawn.RaceProps == null || !pawn.RaceProps.Humanlike || WasReanimatedAsCrossed(pawn) || DiedFromMarkedVirusWithoutReanimation(pawn))
			{
				return false;
			}

			HediffDef virus = CADefOf.CrossVirus;
			if (virus != null && pawn.health?.hediffSet?.HasHediff(virus) == true)
			{
				return true;
			}

			FactionDef crossed = CADefOf.CrossedFaction;
			return crossed != null && pawn.Faction?.def == crossed;
		}

		public static void MarkReanimatedAsCrossed(Pawn pawn)
		{
			if (pawn == null)
			{
				return;
			}

            AddQuestTagIfMissing(pawn, ReanimatedQuestTag);
        }

        public static void MarkDiedFromMarkedVirus(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            AddQuestTagIfMissing(pawn, MarkedVirusFatalityQuestTag);
        }

		private static bool WasReanimatedAsCrossed(Pawn pawn)
		{
			return pawn?.questTags != null && pawn.questTags.Contains(ReanimatedQuestTag);
		}

		private static bool DiedFromMarkedVirusWithoutReanimation(Pawn pawn)
		{
			return pawn?.questTags != null && pawn.questTags.Contains(MarkedVirusFatalityQuestTag);
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
			if (pawn?.health == null || immunity == null || pawn.health.hediffSet.HasHediff(immunity))
			{
				return;
			}

			pawn.health.AddHediff(immunity);
		}

		private static void RemoveCrossVirusImmunity(Pawn pawn)
		{
			HediffDef immunity = CADefOf.CrossVirusImmunity;
			Hediff existing = immunity == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(immunity);
			if (existing != null)
			{
				pawn.health.RemoveHediff(existing);
			}
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
			if (!CanReceiveMarkedVillageFounderState(pawn))
			{
				return false;
			}

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
			{
				ApplyInfectedTattoo(pawn);
			}
			else
			{
				RemoveCrossedRashVisualsIfNotSuccumbed(pawn);
			}
			return changed;
		}

		public static bool TryMarkStarterLineageResistant(Pawn pawn)
		{
			if (!CanReceiveStarterLineageResistance(pawn))
			{
				return false;
			}

            bool added = AddQuestTagIfMissing(pawn, StarterLineageResistanceTag);

            GrantCrossVirusImmunity(pawn);
			return added;
		}

		public static void EnsureStarterLineageResistance(Pawn pawn)
		{
			if (pawn == null || pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.Humanlike || IsInfectedPawn(pawn))
			{
				return;
			}

			if (HasStarterLineageResistance(pawn))
			{
				GrantCrossVirusImmunity(pawn);
				return;
			}

			if (HasStarterLineageParent(pawn))
			{
				TryMarkStarterLineageResistant(pawn);
			}
		}

		private static bool CanReceiveStarterLineageResistance(Pawn pawn)
		{
			return pawn != null
				&& !pawn.Dead
				&& pawn.RaceProps != null
				&& pawn.RaceProps.Humanlike
				&& !HasMarkedVillageFounderImmunity(pawn)
				&& !IsInfectedPawn(pawn)
				&& !IsCrossedPawn(pawn);
		}

		private static bool CanReceiveMarkedVillageFounderState(Pawn pawn)
		{
			return pawn != null
				&& !pawn.Dead
				&& pawn.RaceProps != null
				&& pawn.RaceProps.Humanlike
				&& !IsInfectedPawn(pawn)
				&& !IsCrossedPawn(pawn);
		}

        private static bool AddQuestTagIfMissing(Pawn pawn, string tag)
        {
            if (pawn == null) return false;
            if (pawn.questTags == null) pawn.questTags = new List<string>();
            if (pawn.questTags.Contains(tag)) return false;
            pawn.questTags.Add(tag);
            return true;
        }

		private static bool HasStarterLineageParent(Pawn pawn)
		{
			List<DirectPawnRelation> relations = pawn?.relations?.DirectRelations;
			if (relations == null)
			{
				return false;
			}

			for (int i = 0; i < relations.Count; i++)
			{
				DirectPawnRelation relation = relations[i];
				if (relation?.otherPawn != null && IsParentRelation(relation.def) && HasStarterLineageResistance(relation.otherPawn))
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsParentRelation(PawnRelationDef relationDef)
		{
			if (relationDef == null)
			{
				return false;
			}

			return relationDef == PawnRelationDefOf.Parent
				|| string.Equals(relationDef.defName, "ParentBirth", StringComparison.Ordinal);
		}

		private static void NotifyInfectionRetarget(Pawn infected, Pawn infector)
		{
			if (infected?.Map == null)
			{
				return;
			}

			if (infector != null && infector.Spawned && infector.Map == infected.Map && IsInfectedPawn(infector))
			{
				CrossedTacticalAI.TryRetargetAwayFromPawn(infector, infected, true);
			}

			IReadOnlyList<Pawn> pawns = infected.Map.mapPawns?.AllPawnsSpawned;
			if (pawns == null)
			{
				return;
			}

			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn pawn = pawns[i];
				if (pawn != null && pawn != infector && IsInfectedPawn(pawn))
				{
					CrossedTacticalAI.TryRetargetAwayFromPawn(pawn, infected, false);
				}
			}
		}

		public static bool TryExpose(Pawn pawn, float chance, string source, Pawn infector = null)
		{
			TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
			if (settings != null && !settings.infectionEnabled)
			{
				return false;
			}

			HediffDef virus = CADefOf.CrossVirus;
			if (pawn == null || virus == null || pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.Humanlike || IsCrossedPawn(pawn))
			{
				return false;
			}

			EnsureStarterLineageResistance(pawn);
			if (HasMarkedVillageFounderImmunity(pawn))
			{
				GrantCrossVirusImmunity(pawn);
				ApplyInfectedTattoo(pawn);
				return false;
			}

			bool starterLineageBreakthrough = HasCrossVirusImmunity(pawn) && HasStarterLineageResistance(pawn);
			if (HasCrossVirusImmunity(pawn) && !starterLineageBreakthrough)
			{
				return false;
			}

			MarkedVirusApparelProtection exposureProtection = default(MarkedVirusApparelProtection);
			float effectiveChance = Mathf.Clamp01(chance);
			if (starterLineageBreakthrough)
			{
				effectiveChance *= TheMarkedMenSettings.StarterLineageBreakthroughChance;
			}

			if (CanApparelReduceMarkedVirusExposure(source))
			{
				exposureProtection = GetMarkedVirusExposureProtection(pawn);
				if (exposureProtection.blocksMarkedVirusExposure)
				{
					return false;
				}

				if (exposureProtection.resistance > 0f)
				{
					effectiveChance *= 1f - exposureProtection.resistance;
				}
			}

			if (effectiveChance <= 0f)
			{
				return false;
			}

			if (!Rand.Chance(effectiveChance))
			{
				return false;
			}

			if (starterLineageBreakthrough)
			{
				RemoveCrossVirusImmunity(pawn);
			}

			Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(virus);
			bool newlyInfected = existing == null;
			if (newlyInfected)
			{
				existing = pawn.health.AddHediff(virus);
				existing.Severity = Mathf.Max(existing.Severity, InitialCrossVirusSeverity(virus));
			}
			else
			{
				existing.Severity = Mathf.Max(existing.Severity, InitialCrossVirusSeverity(virus));
			}

			HediffComp_CrossVirus comp = existing.TryGetComp<HediffComp_CrossVirus>();
			comp?.NotifyInfector(infector);
			comp?.NotifyExposureProtection(exposureProtection.resistance, exposureProtection.sealedAgainstMarkedVirus);
			EnsureInfectedState(pawn);
			if (newlyInfected)
			{
				Component?.NotifyExposure(pawn, source);
			}

			return true;
		}

		public static float GetMarkedVirusApparelResistance(Pawn pawn)
		{
			return GetMarkedVirusExposureProtection(pawn).resistance;
		}

		public static MarkedVirusApparelProtection GetMarkedVirusExposureProtection(Pawn pawn)
		{
			return GetMarkedVirusApparelProtection(pawn);
		}

		public static MarkedVirusApparelProtection GetMarkedVirusApparelProtection(Pawn pawn)
		{
			List<Apparel> wornApparel = pawn?.apparel?.WornApparel;
			if (wornApparel == null || wornApparel.Count == 0)
			{
				return default(MarkedVirusApparelProtection);
			}

			float resistance = 0f;
			bool sealedAgainstMarkedVirus = false;
			bool blocksMarkedVirusExposure = false;
			bool hasVacsuitBody = false;
			bool hasVacsuitHelmet = false;
			for (int i = 0; i < wornApparel.Count; i++)
			{
				ThingDef apparelDef = wornApparel[i]?.def;
				MarkedVirusApparelProtection apparelProtection = GetMarkedVirusProtectionForApparelDef(apparelDef);
				if (apparelProtection.resistance <= 0f)
				{
					continue;
				}

				blocksMarkedVirusExposure = blocksMarkedVirusExposure || apparelProtection.blocksMarkedVirusExposure;
				hasVacsuitBody = hasVacsuitBody || IsMarkedVirusVacsuitBody(apparelDef);
				hasVacsuitHelmet = hasVacsuitHelmet || IsMarkedVirusVacsuitHelmet(apparelDef);

				if (apparelProtection.resistance > resistance)
				{
					resistance = apparelProtection.resistance;
					sealedAgainstMarkedVirus = apparelProtection.sealedAgainstMarkedVirus;
				}
				else if (Mathf.Approximately(apparelProtection.resistance, resistance))
				{
					sealedAgainstMarkedVirus = sealedAgainstMarkedVirus || apparelProtection.sealedAgainstMarkedVirus;
				}
			}

			if (blocksMarkedVirusExposure || hasVacsuitBody && hasVacsuitHelmet && TheMarkedMenSettings.VacsuitBlockExposure)
			{
				return new MarkedVirusApparelProtection(1f, true, true);
			}

			return ClampMarkedVirusProtection(new MarkedVirusApparelProtection(resistance, sealedAgainstMarkedVirus));
		}

		public static MarkedVirusApparelProtection GetMarkedVirusApparelProtection(ThingDef apparelDef)
		{
			return GetMarkedVirusProtectionForApparelDef(apparelDef);
		}

		public static void ApplyMarkedVirusResistanceEquippedStatOffsets()
		{
			StatDef markedVirusResistance = CADefOf.MarkedVirusResistance;
			if (markedVirusResistance == null)
			{
				return;
			}

			List<ThingDef> allThingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
			for (int i = 0; i < allThingDefs.Count; i++)
			{
				ThingDef apparelDef = allThingDefs[i];
				if (apparelDef?.apparel == null)
				{
					continue;
				}

				MarkedVirusApparelProtection protection = GetMarkedVirusProtectionForApparelDef(apparelDef);
				float resistance = Mathf.Clamp01(protection.resistance);
				if (resistance <= 0f)
				{
					continue;
				}

				if (apparelDef.equippedStatOffsets == null)
				{
					apparelDef.equippedStatOffsets = new List<StatModifier>();
				}

				bool updatedExistingOffset = false;
				for (int offsetIndex = 0; offsetIndex < apparelDef.equippedStatOffsets.Count; offsetIndex++)
				{
					StatModifier offset = apparelDef.equippedStatOffsets[offsetIndex];
					if (offset.stat != markedVirusResistance)
					{
						continue;
					}

					offset.value = resistance;
					apparelDef.equippedStatOffsets[offsetIndex] = offset;
					updatedExistingOffset = true;
					break;
				}

				if (!updatedExistingOffset)
				{
					apparelDef.equippedStatOffsets.Add(new StatModifier
					{
						stat = markedVirusResistance,
						value = resistance
					});
				}
			}
		}

		private static MarkedVirusApparelProtection GetMarkedVirusProtectionForApparelDef(ThingDef apparelDef)
		{
			if (apparelDef?.apparel == null)
			{
				return default(MarkedVirusApparelProtection);
			}

			MarkedVirusProtectionExtension extension = apparelDef.GetModExtension<MarkedVirusProtectionExtension>();
			if (extension != null)
			{
				return ClampMarkedVirusProtection(new MarkedVirusApparelProtection(extension.resistance, extension.sealedAgainstMarkedVirus, extension.blocksMarkedVirusExposure));
			}

			return ClampMarkedVirusProtection(InferMarkedVirusApparelProtection(apparelDef));
		}

		private static MarkedVirusApparelProtection ClampMarkedVirusProtection(MarkedVirusApparelProtection protection)
		{
			if (protection.blocksMarkedVirusExposure)
			{
				return new MarkedVirusApparelProtection(1f, true, true);
			}

			float resistance = Mathf.Clamp(protection.resistance, 0f, MaxWornMarkedVirusResistance);
			return new MarkedVirusApparelProtection(resistance, protection.sealedAgainstMarkedVirus);
		}

		private static bool IsMarkedVirusVacsuitBody(ThingDef apparelDef)
		{
			if (apparelDef?.apparel == null || !ApparelCoversBodyPartGroup(apparelDef, "Torso"))
			{
				return false;
			}

			string defName = apparelDef.defName ?? string.Empty;
			string label = apparelDef.label ?? string.Empty;
			return ContainsOrdinalIgnoreCase(defName, "Vacsuit")
				|| ContainsOrdinalIgnoreCase(label, "vacsuit")
				|| ContainsOrdinalIgnoreCase(defName, "EVAsuit")
				|| ContainsOrdinalIgnoreCase(label, "EVA suit");
		}

		private static bool IsMarkedVirusVacsuitHelmet(ThingDef apparelDef)
		{
			if (apparelDef?.apparel == null || !ApparelCoversBodyPartGroup(apparelDef, "FullHead"))
			{
				return false;
			}

			string defName = apparelDef.defName ?? string.Empty;
			string label = apparelDef.label ?? string.Empty;
			return ContainsOrdinalIgnoreCase(defName, "VacsuitHelmet")
				|| ContainsOrdinalIgnoreCase(label, "vacsuit helmet");
		}

		private static MarkedVirusApparelProtection InferMarkedVirusApparelProtection(ThingDef apparelDef)
		{
			string defName = apparelDef.defName ?? string.Empty;
			string label = apparelDef.label ?? string.Empty;
			bool fullHead = ApparelCoversBodyPartGroup(apparelDef, "FullHead");
			bool torso = ApparelCoversBodyPartGroup(apparelDef, "Torso");
			bool toxGasImmune = apparelDef.apparel?.immuneToToxGasExposure == true;
			float toxicEnvironmentResistance = EquippedStatOffset(apparelDef, "ToxicEnvironmentResistance");
			float vacuumResistance = EquippedStatOffset(apparelDef, "VacuumResistance");

			// HAZMAT suit (XML: 0.90 sealed)
			if (string.Equals(defName, "VAE_Apparel_HAZMATSuit", StringComparison.OrdinalIgnoreCase))
			{
				return new MarkedVirusApparelProtection(0.90f, true, TheMarkedMenSettings.SealedArmorBlockExposure);
			}

			// Warcasket torso (XML: 0.85 sealed)
			if (ContainsOrdinalIgnoreCase(defName, "Warcasket") && torso
				&& !ContainsOrdinalIgnoreCase(defName, "Shoulders")
				&& !ContainsOrdinalIgnoreCase(defName, "Bodysuit"))
			{
				return new MarkedVirusApparelProtection(0.85f, true, TheMarkedMenSettings.WarcasketsBlockExposure);
			}

			// Warcasket bodysuit (XML: 0.70 sealed)
			if (ContainsOrdinalIgnoreCase(defName, "Warcasket")
				&& ContainsOrdinalIgnoreCase(defName, "Bodysuit") && torso)
			{
				return new MarkedVirusApparelProtection(0.70f, true);
			}

			// WarcasketHelmet (XML: 0.25 sealed)
			if (ContainsOrdinalIgnoreCase(defName, "WarcasketHelmet"))
			{
				return new MarkedVirusApparelProtection(0.25f, true);
			}

			// WarcasketShoulders (XML: 0.15 not sealed)
			if (ContainsOrdinalIgnoreCase(defName, "WarcasketShoulders"))
			{
				return new MarkedVirusApparelProtection(0.15f, false);
			}

			// Other warcasket parts (XML: 0.10 not sealed)
			if (ContainsOrdinalIgnoreCase(defName, "Warcasket"))
			{
				return new MarkedVirusApparelProtection(0.10f, false);
			}

			// Vacsuit/EVA suits (XML: 0.30 sealed)
			if (IsMarkedVirusVacsuitBody(apparelDef))
			{
				return new MarkedVirusApparelProtection(0.30f, true);
			}

			// Sealed undersuits, orbital armor (XML: 0.25 sealed)
			if (torso && (ContainsOrdinalIgnoreCase(defName, "Sealed") || ContainsOrdinalIgnoreCase(label, "sealed")
				|| ContainsOrdinalIgnoreCase(defName, "AstroSuit") || ContainsOrdinalIgnoreCase(label, "astrosuit")
				|| ContainsOrdinalIgnoreCase(defName, "SecurityArmor") || ContainsOrdinalIgnoreCase(label, "security armor")
				|| ContainsOrdinalIgnoreCase(defName, "OrbitalArmor") || ContainsOrdinalIgnoreCase(label, "orbital armor")))
			{
				return new MarkedVirusApparelProtection(0.25f, true, TheMarkedMenSettings.SealedArmorBlockExposure);
			}

			// Vacsuit helmet (XML: 0.25 sealed)
			if (IsMarkedVirusVacsuitHelmet(apparelDef))
			{
				return new MarkedVirusApparelProtection(0.25f, true);
			}

			// Gas masks, HAZMAT masks (XML: 0.25 sealed)
			if (toxGasImmune || ContainsOrdinalIgnoreCase(defName, "GasMask")
				|| ContainsOrdinalIgnoreCase(label, "gas mask")
				|| ContainsOrdinalIgnoreCase(defName, "HAZMATMask")
				|| ContainsOrdinalIgnoreCase(defName, "AstroMask"))
			{
				return new MarkedVirusApparelProtection(0.25f, true, TheMarkedMenSettings.GasMasksBlockExposure);
			}

			// Toxic environment resistance (XML: 0.25 sealed)
			if (toxicEnvironmentResistance >= 0.75f && fullHead)
			{
				return new MarkedVirusApparelProtection(0.25f, true);
			}

			// Vacuum resistance (XML: 0.25 sealed)
			if (vacuumResistance >= 0.30f && torso)
			{
				return new MarkedVirusApparelProtection(0.25f, true);
			}

			// Powered armor bodies (XML: 0.25 not sealed)
			if (IsPoweredArmorBody(defName) || IsPoweredArmorBody(label))
			{
				return new MarkedVirusApparelProtection(0.25f, false);
			}

			// Powered armor helmets (XML: 0.20 not sealed)
			if (IsPoweredArmorHelmet(defName) || IsPoweredArmorHelmet(label))
			{
				return new MarkedVirusApparelProtection(0.20f, false);
			}

			// Plague masks (XML: 0.10 not sealed)
			if (ContainsOrdinalIgnoreCase(defName, "PlagueMask") || ContainsOrdinalIgnoreCase(label, "plague mask"))
			{
				return new MarkedVirusApparelProtection(0.10f, false);
			}

			// Cloth, surgical, face masks (XML: 0.06 not sealed)
			if (ContainsOrdinalIgnoreCase(defName, "ClothMask") || ContainsOrdinalIgnoreCase(defName, "SurgicalMask")
				|| ContainsOrdinalIgnoreCase(label, "surgical mask") || ContainsOrdinalIgnoreCase(label, "face mask"))
			{
				return new MarkedVirusApparelProtection(0.06f, false);
			}

			// Suits, armor, flak (XML: 0.08 not sealed)
			if (IsArmorOrSuit(defName) || IsArmorOrSuit(label))
			{
				return new MarkedVirusApparelProtection(0.08f, false);
			}

			// Masks (XML: 0.05 not sealed)
			if (ContainsOrdinalIgnoreCase(defName, "Mask") || ContainsOrdinalIgnoreCase(label, "mask"))
			{
				return new MarkedVirusApparelProtection(0.05f, false);
			}

			// Helmets (XML: 0.05 not sealed)
			if (ContainsOrdinalIgnoreCase(defName, "Helmet") || ContainsOrdinalIgnoreCase(label, "helmet"))
			{
				return new MarkedVirusApparelProtection(0.05f, false);
			}

			return new MarkedVirusApparelProtection(DefaultApparelVirusResistance, false);
		}

		private static bool IsPoweredArmorBody(string text)
		{
			return ContainsOrdinalIgnoreCase(text, "PowerArmor")
				|| ContainsOrdinalIgnoreCase(text, "ArmorRecon")
				|| ContainsOrdinalIgnoreCase(text, "ArmorMarine")
				|| ContainsOrdinalIgnoreCase(text, "ArmorCataphract")
				|| ContainsOrdinalIgnoreCase(text, "ArmorLocust")
				|| ContainsOrdinalIgnoreCase(text, "ArmorPhoenix")
				|| ContainsOrdinalIgnoreCase(text, "MechlordSuit")
				|| ContainsOrdinalIgnoreCase(text, "ArmorAbsolver")
				|| ContainsOrdinalIgnoreCase(text, "ArmorDeserter")
				|| ContainsOrdinalIgnoreCase(text, "privateer armor");
		}

		private static bool IsPoweredArmorHelmet(string text)
		{
			return ContainsOrdinalIgnoreCase(text, "PowerArmorHelmet")
				|| ContainsOrdinalIgnoreCase(text, "ArmorHelmetRecon")
				|| ContainsOrdinalIgnoreCase(text, "ArmorHelmetCataphract")
				|| ContainsOrdinalIgnoreCase(text, "ArmorMarineHelmet")
				|| ContainsOrdinalIgnoreCase(text, "ArmorHelmetMech")
				|| ContainsOrdinalIgnoreCase(text, "AbsolverHelmet")
				|| ContainsOrdinalIgnoreCase(text, "DeserterHelmet")
				|| ContainsOrdinalIgnoreCase(text, "JanissaryHelmet")
				|| ContainsOrdinalIgnoreCase(text, "power armor helmet")
				|| ContainsOrdinalIgnoreCase(text, "marine helmet")
				|| ContainsOrdinalIgnoreCase(text, "recon helmet")
				|| ContainsOrdinalIgnoreCase(text, "cataphract helmet");
		}

		private static bool IsArmorOrSuit(string text)
		{
			return ContainsOrdinalIgnoreCase(text, "Suit")
				|| ContainsOrdinalIgnoreCase(text, "Armor")
				|| ContainsOrdinalIgnoreCase(text, "Armour")
				|| ContainsOrdinalIgnoreCase(text, "Cuirass")
				|| ContainsOrdinalIgnoreCase(text, "Flak")
				|| ContainsOrdinalIgnoreCase(text, "WarcasketShoulders")
				|| ContainsOrdinalIgnoreCase(text, "warcasket shoulders");
		}

		private static bool ApparelCoversBodyPartGroup(ThingDef apparelDef, string bodyPartGroupDefName)
		{
			List<BodyPartGroupDef> bodyPartGroups = apparelDef?.apparel?.bodyPartGroups;
			if (bodyPartGroups == null)
			{
				return false;
			}

			for (int i = 0; i < bodyPartGroups.Count; i++)
			{
				if (string.Equals(bodyPartGroups[i]?.defName, bodyPartGroupDefName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private static float EquippedStatOffset(ThingDef apparelDef, string statDefName)
		{
			List<StatModifier> offsets = apparelDef?.equippedStatOffsets;
			if (offsets == null)
			{
				return 0f;
			}

			float value = 0f;
			for (int i = 0; i < offsets.Count; i++)
			{
				StatDef stat = offsets[i].stat;
				if (string.Equals(stat?.defName, statDefName, StringComparison.OrdinalIgnoreCase))
				{
					value += offsets[i].value;
				}
			}

			return value;
		}

		private static bool ContainsOrdinalIgnoreCase(string text, string value)
		{
			return !string.IsNullOrEmpty(text)
				&& !string.IsNullOrEmpty(value)
				&& text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static bool CanApparelReduceMarkedVirusExposure(string source)
		{
			return string.IsNullOrEmpty(source) || source.IndexOf("food", StringComparison.OrdinalIgnoreCase) < 0;
		}

		public static void TransformPawn(Pawn pawn, bool suppressNotification = false, Pawn infector = null)
		{
			if (pawn == null || pawn.Dead)
			{
				return;
			}

			Faction faction = Component?.EnsureCrossedFaction();
			PawnKindDef kind = PickTransformationKind(pawn);
			if (kind != null && pawn.kindDef != kind)
			{
				pawn.ChangeKind(kind);
			}

			if (faction != null && pawn.Faction != faction)
			{
				pawn.SetFaction(faction, null);
			}

			pawn.guest?.SetGuestStatus(null);
			pawn.mindState?.mentalStateHandler?.Reset();
			EnsureFearlessCrossedState(pawn);
			ApplyClassHediffs(pawn);
			EnsureCrossedBasicClothingOnly(pawn);
			if (pawn.Drawer?.renderer != null)
			{
				pawn.Drawer.renderer.SetAllGraphicsDirty();
			}

			ApplyInfectedTattoo(pawn);
			CrossedTacticalAI.TryAttackNearestNonInfectedPawn(pawn, true, allowRjwJob: false);
			if (suppressNotification)
			{
				return;
			}

			Component?.NotifyTransformation(pawn);
		}

		public static void ApplyGeneratedRaidKindTuning(List<Pawn> pawns)
		{
			if (pawns == null || pawns.Count == 0)
			{
				return;
			}

			TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
			int maxAlphas = Mathf.Clamp(settings?.maximumAlphasPerRaid ?? 99, 0, 99);
			int alphaCount = 0;
			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn pawn = pawns[i];
				if (pawn == null || pawn.Dead)
				{
					continue;
				}

				bool replace = false;
				if (pawn.kindDef == CADefOf.CrossedAlpha)
				{
					alphaCount++;
					replace = alphaCount > maxAlphas || TheMarkedMenSettings.AdjustKindWeight(CADefOf.CrossedAlpha, 1f) <= 0f;
				}
				else if (pawn.kindDef == CADefOf.CrossedWarlord)
				{
					replace = TheMarkedMenSettings.AdjustKindWeight(CADefOf.CrossedWarlord, 1f) <= 0f;
				}
				else if (pawn.kindDef == CADefOf.MarkedMan)
				{
					replace = TheMarkedMenSettings.AdjustKindWeight(CADefOf.MarkedMan, 1f) <= 0f;
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
			AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedCivilian, 14f);
			AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedScout, 10f);
			AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedHunter, 8f);
			AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedShooter, 8f);
			AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedRaider, 6f);
			AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedSoldier, 4f);
			AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedBrute, 2f);
			AddReplacementKind(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, 3f);
			return selected ?? CADefOf.CrossedCivilian ?? CADefOf.CrossedScout ?? CADefOf.CrossedHunter ?? CADefOf.CrossedShooter;
		}

		private static void AddReplacementKind(ref PawnKindDef selected, ref float totalWeight, PawnKindDef kind, float baseWeight)
		{
			float weight = TheMarkedMenSettings.AdjustKindWeight(kind, baseWeight);
			if (kind == null || weight <= 0f)
			{
				return;
			}

			totalWeight += weight;
			if (Rand.Value * totalWeight <= weight)
			{
				selected = kind;
			}
		}

		private static void RemoveClassHediffs(Pawn pawn)
		{
			RemoveHediffIfPresent(pawn, CADefOf.BloodRush);
			RemoveHediffIfPresent(pawn, CADefOf.CommandAura);
			RemoveHediffIfPresent(pawn, CADefOf.PsychicAura);
		}

		private static void RemoveHediffIfPresent(Pawn pawn, HediffDef def)
		{
			Hediff existing = def == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(def);
			if (existing != null)
			{
				pawn.health.RemoveHediff(existing);
			}
		}

		public static void ApplyClassHediffs(Pawn pawn)
		{
			if (pawn == null || pawn.health == null)
			{
				return;
			}

			if (ModsConfig.BiotechActive && pawn.genes != null && IsCrossedFactionPawn(pawn) && CADefOf.MarkedOne != null)
			{
				pawn.genes.SetXenotypeDirect(CADefOf.MarkedOne);
			}

			RemoveDeprecatedCrossedRashHediff(pawn);
			if (!ShouldShowCrossedRash(pawn))
			{
				RemoveCrossedRashVisualsIfNotSuccumbed(pawn);
				return;
			}

			EnsureFearlessCrossedState(pawn);
			if (!IsCrossedFactionPawn(pawn) && !HasCrossVirusImmunity(pawn) && !HasMarkedVillageFounderImmunity(pawn))
			{
				HediffDef virus = CADefOf.CrossVirus;
				if (virus != null)
				{
					Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(virus) ?? pawn.health.AddHediff(virus);
					hediff.Severity = 1f;
				}
			}

			if (pawn.kindDef == CADefOf.CrossedBrute && CADefOf.BloodRush != null && !pawn.health.hediffSet.HasHediff(CADefOf.BloodRush))
			{
				pawn.health.AddHediff(CADefOf.BloodRush);
			}

			if ((pawn.kindDef == CADefOf.CrossedAlpha || pawn.kindDef == CADefOf.CrossedWarlord || pawn.kindDef == CADefOf.MarkedMan)
				&& CADefOf.CommandAura != null && !pawn.health.hediffSet.HasHediff(CADefOf.CommandAura))
			{
				pawn.health.AddHediff(CADefOf.CommandAura);
			}

			if (pawn.kindDef == CADefOf.MarkedMan && CADefOf.BloodRush != null && !pawn.health.hediffSet.HasHediff(CADefOf.BloodRush))
			{
				pawn.health.AddHediff(CADefOf.BloodRush);
			}

			if (IsCrossedFactionPawn(pawn) && CADefOf.CrossedStrength != null && !pawn.health.hediffSet.HasHediff(CADefOf.CrossedStrength))
			{
				pawn.health.AddHediff(CADefOf.CrossedStrength);
			}

			ApplyEliteTierHediff(pawn);

			if (IsCrossedFactionPawn(pawn))
			{
				ApplyRandomBionics(pawn);
				CrossedEquipmentGenerator.AssignEquipment(pawn);
			}

			ApplyInfectedTattoo(pawn);
			EnsureCrossedBasicClothingOnly(pawn);
			EnsureCrossedPyromaniacMolotov(pawn);
		}

        private static readonly BodyPartDef StomachBodyPartDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Stomach");
        private static readonly BodyPartDef KidneyBodyPartDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Kidney");
        private static readonly BodyPartDef EarBodyPartDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Ear");

        private static readonly HediffDef BionicEyeHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicEye");
        private static readonly HediffDef BionicArmHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicArm");
        private static readonly HediffDef AdvBionicArmHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("AdvBionicArm");
        private static readonly HediffDef BionicLegHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicLeg");
        private static readonly HediffDef AdvBionicLegHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("AdvBionicLeg");
        private static readonly HediffDef BionicHeartHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicHeart");
        private static readonly HediffDef BionicStomachHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicStomach");
        private static readonly HediffDef BionicLungHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicLung");
        private static readonly HediffDef BionicKidneyHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicKidney");
        private static readonly HediffDef BionicEarHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicEar");

        public static void ApplyRandomBionics(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || !IsCrossedFactionPawn(pawn)) return;

            if (pawn.kindDef != CADefOf.MarkedMan) return;

            int maxBionics = 3;
            float perPartChance = 0.5f;

            int installed = 0;
            TryInstallBionic(pawn, BionicEyeHediffDef, BodyPartDefOf.Eye, perPartChance, ref installed, maxBionics);
            TryInstallBionic(pawn, BionicArmHediffDef, BodyPartDefOf.Arm, perPartChance, ref installed, maxBionics);
            TryInstallBionic(pawn, AdvBionicArmHediffDef, BodyPartDefOf.Arm, perPartChance * 0.2f, ref installed, maxBionics);
            TryInstallBionic(pawn, BionicLegHediffDef, BodyPartDefOf.Leg, perPartChance, ref installed, maxBionics);
            TryInstallBionic(pawn, AdvBionicLegHediffDef, BodyPartDefOf.Leg, perPartChance * 0.2f, ref installed, maxBionics);
            TryInstallBionic(pawn, BionicHeartHediffDef, BodyPartDefOf.Heart, perPartChance * 0.5f, ref installed, maxBionics);
            TryInstallBionic(pawn, BionicStomachHediffDef, StomachBodyPartDef, perPartChance * 0.5f, ref installed, maxBionics);
            TryInstallBionic(pawn, BionicLungHediffDef, BodyPartDefOf.Lung, perPartChance * 0.4f, ref installed, maxBionics);
            TryInstallBionic(pawn, BionicKidneyHediffDef, KidneyBodyPartDef, perPartChance * 0.4f, ref installed, maxBionics);
            TryInstallBionic(pawn, BionicEarHediffDef, EarBodyPartDef, perPartChance * 0.3f, ref installed, maxBionics);
        }

        private static void TryInstallBionic(Pawn pawn, HediffDef hediffDef, BodyPartDef bodyPartDef, float chance, ref int installed, int maxBionics)
        {
            if (installed >= maxBionics || bodyPartDef == null || hediffDef == null || Rand.Value >= chance) return;

            IEnumerable<BodyPartRecord> parts = pawn.RaceProps.body.GetPartsWithDef(bodyPartDef);
			if (parts == null || !parts.Any()) return;

			BodyPartRecord part = parts.RandomElement();
			if (pawn.health.hediffSet.HasDirectlyAddedPartFor(part)) return;
			if (pawn.health.hediffSet.PartIsMissing(part)) return;

			Hediff implant = HediffMaker.MakeHediff(hediffDef, pawn, part);
			pawn.health.AddHediff(implant);
			installed++;
		}

		public static void ApplyEliteTierHediff(Pawn pawn)
		{
			if (pawn == null || pawn.health == null || !IsCrossedFactionPawn(pawn)) return;

			HediffDef tierDef = null;
			if (pawn.kindDef == CADefOf.CrossedWarlord) tierDef = CADefOf.WarlordTier;
			else if (pawn.kindDef == CADefOf.CrossedAlpha) tierDef = CADefOf.AlphaTier;
			else if (pawn.kindDef == CADefOf.MarkedMan) tierDef = CADefOf.MarkedTier;

			if (tierDef != null && !pawn.health.hediffSet.HasHediff(tierDef))
			{
				pawn.health.AddHediff(tierDef);
			}
		}

		public static void EnsurePredatorHediffs(Pawn pawn)
		{
			TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
			if (pawn == null || pawn.health == null || !IsInfectedPawn(pawn))
			{
				return;
			}

			if (settings != null && settings.bloodlustEnabled && CADefOf.MarkedBloodlustNeed != null
				&& pawn.needs != null && pawn.needs.TryGetNeed<Need_MarkedBloodlust>() == null)
			{
				pawn.needs.AllNeeds.Add(new Need_MarkedBloodlust(pawn));
			}

			if (settings != null && settings.anticipationEnabled && CADefOf.KillAnticipation != null
				&& !pawn.health.hediffSet.HasHediff(CADefOf.KillAnticipation))
			{
				pawn.health.AddHediff(CADefOf.KillAnticipation);
			}
		}

		public static void NotifyBloodlustKill(Pawn killer, Pawn victim)
		{
			if (killer == null || killer.needs == null || !IsInfectedPawn(killer))
			{
				return;
			}

			Need_MarkedBloodlust need = killer.needs.TryGetNeed<Need_MarkedBloodlust>();
			if (need != null)
			{
				need.NotifyKilled();
			}

			if (CADefOf.FreshKillSatisfaction != null && killer.needs?.mood != null && victim != null)
			{
				Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(CADefOf.FreshKillSatisfaction);
				killer.needs.mood.thoughts.memories.TryGainMemory(thought);
			}
		}

		public static bool IsCrossedPyromaniac(Pawn pawn)
		{
			return pawn?.kindDef == CADefOf.CrossedPyromaniac;
		}

		public static bool IsMolotovWeapon(ThingDef def)
		{
			return def != null
				&& (def.defName == "Weapon_GrenadeMolotov"
					|| def.weaponTags != null && def.weaponTags.Contains("GrenadeMolotov"));
		}

        private static ThingDef _cachedMolotovDef;

        public static ThingDef GetMolotovWeaponDef()
        {
            if (_cachedMolotovDef != null) return _cachedMolotovDef;

            _cachedMolotovDef = DefDatabase<ThingDef>.GetNamedSilentFail("Weapon_GrenadeMolotov");
            if (_cachedMolotovDef != null)
                return _cachedMolotovDef;

            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                if (IsMolotovWeapon(allDefs[i]))
                {
                    _cachedMolotovDef = allDefs[i];
                    return _cachedMolotovDef;
                }
            }

            return null;
        }

		public static bool EnsureCrossedPyromaniacMolotov(Pawn pawn)
		{
			if (!IsCrossedPyromaniac(pawn) || pawn.equipment == null)
			{
				return false;
			}

			ThingWithComps current = pawn.equipment.Primary;
			if (current != null && !current.Destroyed && IsMolotovWeapon(current.def))
			{
				return true;
			}

			ThingDef molotov = GetMolotovWeaponDef();
			if (molotov == null)
			{
				return false;
			}

			List<ThingWithComps> allEquip = pawn.equipment.AllEquipmentListForReading;
			for (int i = allEquip.Count - 1; i >= 0; i--)
			{
				ThingWithComps eq = allEquip[i];
				if (eq == null || eq.Destroyed) continue;
				pawn.equipment.Remove(eq);
				eq.Destroy(DestroyMode.Vanish);
			}

			pawn.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(molotov));
			return true;
		}

		private static void GrantPsycastAbility(Pawn pawn, string abilityDefName)
		{
			AbilityDef abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
			if (abilityDef == null)
			{
				return;
			}

			if (pawn.abilities == null)
			{
				return;
			}

			foreach (Ability ability in pawn.abilities.AllAbilitiesForReading)
			{
				if (ability.def == abilityDef)
				{
					return;
				}
			}

			pawn.abilities.GainAbility(abilityDef);
		}

		private static void GrantAbility(Pawn pawn, AbilityDef abilityDef)
		{
			if (abilityDef == null || pawn.abilities == null)
			{
				return;
			}

			foreach (Ability ability in pawn.abilities.AllAbilitiesForReading)
			{
				if (ability.def == abilityDef)
				{
					return;
				}
			}

			pawn.abilities.GainAbility(abilityDef);
		}

		public static void RemoveCrossVirusIfImmune(Pawn pawn)
		{
			HediffDef virus = CADefOf.CrossVirus;
			if (pawn?.health == null || virus == null) return;
			if (!HasCrossVirusImmunity(pawn) && !HasMarkedVillageFounderImmunity(pawn)) return;
			Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(virus);
			if (existing != null)
			{
				pawn.health.RemoveHediff(existing);
			}
		}

		public static void RemoveMarkedVirusHediffFromFullyTurnedPawn(Pawn pawn)
		{
			HediffDef virus = IsCrossedFactionPawn(pawn) ? CADefOf.CrossVirus : null;
			Hediff existing = virus == null ? null : pawn?.health?.hediffSet?.GetFirstHediffOfDef(virus);
			if (existing != null)
			{
				pawn.health.RemoveHediff(existing);
			}
		}

		public static void ApplyInfectedTattoo(Pawn pawn)
		{
			TattooDef tattoo = GetCurrentCrossedFaceTattoo(pawn);
			if (pawn?.style == null || tattoo == null || !ShouldShowCrossedRash(pawn))
			{
				return;
			}

			if (pawn.style.nextFaceTattooDef != tattoo)
			{
				pawn.style.nextFaceTattooDef = tattoo;
			}

			if (pawn.style.FaceTattoo != tattoo)
			{
				pawn.style.FaceTattoo = tattoo;
				pawn.style.Notify_StyleItemChanged();
				pawn.Drawer?.renderer?.SetAllGraphicsDirty();
			}
		}

		public static void ApplyInfectedTattooIfInfected(Pawn pawn)
		{
			EnsureInfectedState(pawn);
		}

		public static void EnsureInfectedState(Pawn pawn)
		{
			if (!CanSafelyProcessInfectedState(pawn))
			{
				return;
			}

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

		public static bool CanSafelyProcessInfectedState(Pawn pawn)
		{
			return pawn != null
				&& !pawn.Destroyed
				&& pawn.def?.race != null
				&& pawn.def.race.Humanlike
				&& pawn.health?.hediffSet != null
				&& !pawn.health.Dead;
		}

		private static float InitialCrossVirusSeverity(HediffDef virus)
		{
			return Mathf.Clamp(virus?.initialSeverity ?? 0.08f, 0.001f, 1f);
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
			if (pawn == null || ShouldShowCrossedRash(pawn))
			{
				return;
			}

			bool changed = false;
			if (pawn.style != null)
			{
				TattooDef noFaceTattoo = TattooDefOf.NoTattoo_Face;
				if (CADefOf.IsCrossedFaceTattoo(pawn.style.nextFaceTattooDef))
				{
					pawn.style.nextFaceTattooDef = noFaceTattoo;
					changed = true;
				}

				if (CADefOf.IsCrossedFaceTattoo(pawn.style.FaceTattoo))
				{
					pawn.style.FaceTattoo = noFaceTattoo;
					pawn.style.Notify_StyleItemChanged();
					changed = true;
				}
			}

			if (changed)
			{
				pawn.Drawer?.renderer?.SetAllGraphicsDirty();
			}
		}

		public static void EnsureCrossedBasicClothingOnly(Pawn pawn)
		{
			ClearArmorStripDueTick(pawn);
		}

		private static void ClearArmorStripDueTick(Pawn pawn)
		{
			List<string> tags = pawn?.questTags;
			if (tags == null)
			{
				return;
			}

			for (int i = tags.Count - 1; i >= 0; i--)
			{
				string tag = tags[i];
				if (!tag.NullOrEmpty() && tag.StartsWith(ArmorStripDueTagPrefix, StringComparison.Ordinal))
				{
					tags.RemoveAt(i);
				}
			}
		}

		private static bool IsCrossedFactionPawn(Pawn pawn)
		{
			FactionDef crossed = CADefOf.CrossedFaction;
			return pawn != null && crossed != null && pawn.Faction?.def == crossed;
		}

		public static void ApplyMarkedPanic(Map map, IntVec3 origin, float radius)
		{
			HediffDef panic = CADefOf.Panic;
			if (map == null || panic == null)
			{
				return;
			}

			float effectiveRadius = Mathf.Max(0f, radius * Mathf.Sqrt(TheMarkedMenSettings.SocialTerrorStrength));
			if (effectiveRadius <= 0f)
			{
				return;
			}

			foreach (Pawn pawn in map.mapPawns.FreeColonistsAndPrisonersSpawned)
			{
				if (pawn.Position.InHorDistOf(origin, effectiveRadius) && !pawn.health.hediffSet.HasHediff(panic))
				{
					pawn.health.AddHediff(panic);
				}
			}
		}

		private static PawnKindDef PickTransformationKind(Pawn pawn)
		{
			TransformationKinds.Clear();
			AddKind(CADefOf.CrossedCivilian, 1f);
			AddKind(CADefOf.CrossedScout, 1f);
			AddKind(CADefOf.CrossedHunter, 1f);
			AddKind(CADefOf.CrossedShooter, 1f);
			AddKind(CADefOf.CrossedRaider, 1f);
			AddKind(CADefOf.CrossedSoldier, 1f);
			AddKind(CADefOf.CrossedPyromaniac, 1f);
			if (Rand.Chance(0.12f))
			{
				AddKind(CADefOf.CrossedBrute, 1f);
			}
			if (Rand.Chance(0.02f))
			{
				AddKind(CADefOf.CrossedAlpha, 1f);
			}
			if (Rand.Chance(0.005f))
			{
				AddKind(CADefOf.CrossedWarlord, 1f);
			}

			if (TransformationKinds.Count == 0)
			{
				return pawn.kindDef;
			}

			return PickWeightedKind(TransformationKinds) ?? pawn.kindDef;
		}

		private static void AddKind(PawnKindDef kind, float baseWeight)
		{
			float weight = TheMarkedMenSettings.AdjustKindWeight(kind, baseWeight);
			if (kind != null && weight > 0f)
			{
				TransformationKinds.Add(new KeyValuePair<PawnKindDef, float>(kind, weight));
			}
		}

		private static PawnKindDef PickWeightedKind(List<KeyValuePair<PawnKindDef, float>> kinds)
		{
			float totalWeight = 0f;
			for (int i = 0; i < kinds.Count; i++)
			{
				totalWeight += Mathf.Max(0f, kinds[i].Value);
			}

			if (totalWeight <= 0f)
			{
				return null;
			}

			float pick = Rand.Value * totalWeight;
			for (int i = 0; i < kinds.Count; i++)
			{
				pick -= Mathf.Max(0f, kinds[i].Value);
				if (pick <= 0f)
				{
					return kinds[i].Key;
				}
			}

			return kinds[kinds.Count - 1].Key;
		}
	}
}
