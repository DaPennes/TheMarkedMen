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

		public static bool IsMarkedTurnedPawn(Pawn pawn)
		{
			if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
				return false;
			return pawn.health.hediffSet.HasHediff(CADefOf.MarkedTurned);
		}

		public static bool IsPartiallyMarkedPawn(Pawn pawn)
		{
			return HasMarkedVirusHediff(pawn) && !IsFullyTurnedMarkedPawn(pawn);
		}

		public static bool IsShambler(Pawn pawn)
		{
			if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
			{
				return false;
			}

			HediffDef shamblerHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Shambler");
			if (shamblerHediff == null)
			{
				return false;
			}

			return pawn.health.hediffSet.HasHediff(shamblerHediff);
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

		public static bool IsValidInfectionTargetByFaction(Pawn pawn, TheMarkedMenSettings settings)
		{
			if (pawn.IsPrisoner)
			{
				return true;
			}

			if (pawn.IsColonist)
			{
				return settings.colonistsCanBeInfected;
			}

			if (pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer))
			{
				return settings.enemiesCanBeInfected;
			}

			return settings.alliesCanBeInfected;
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

			if (IsShambler(pawn))
			{
				return false;
			}

			HediffDef virus = CADefOf.CrossVirus;
			if (pawn == null || virus == null || pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.Humanlike || IsCrossedPawn(pawn))
			{
				return false;
			}

			if (settings != null && !IsValidInfectionTargetByFaction(pawn, settings))
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
			float initialSeverity = 0.15f;
			if (newlyInfected)
			{
				existing = pawn.health.AddHediff(virus);
				existing.SetVisible(); // Force visibility so stages show on clean pawns
				existing.Severity = Mathf.Max(existing.Severity, initialSeverity);
			}
			else
			{
				existing.Severity = Mathf.Max(existing.Severity, initialSeverity);
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

		private const string MarkedVirusSealedTag = "MarkedVirusSealed";
		private const string MarkedVirusWarcasketBodyTag = "MarkedVirusWarcasketBody";
		private const string MarkedVirusWarcasketBodysuitTag = "MarkedVirusWarcasketBodysuit";
		private const string MarkedVirusWarcasketHelmetTag = "MarkedVirusWarcasketHelmet";
		private const string MarkedVirusWarcasketShouldersTag = "MarkedVirusWarcasketShoulders";
		private const string MarkedVirusWarcasketPartTag = "MarkedVirusWarcasketPart";
		private const string MarkedVirusVacsuitBodyTag = "MarkedVirusVacsuitBody";
		private const string MarkedVirusVacsuitHelmetTag = "MarkedVirusVacsuitHelmet";
		private const string MarkedVirusGasMaskTag = "MarkedVirusGasMask";

		private sealed class TaggedProtection
		{
			public float resistance;
			public bool sealedAgainstMarkedVirus;
			public bool blocksMarkedVirusExposure;
			public MarkedVirusBlockCategory blockCategory;
		}

		private static readonly Dictionary<string, TaggedProtection> MarkedVirusTaggedProtections = new Dictionary<string, TaggedProtection>
		{
			{ MarkedVirusSealedTag, new TaggedProtection { resistance = 0.90f, sealedAgainstMarkedVirus = true, blocksMarkedVirusExposure = true, blockCategory = MarkedVirusBlockCategory.SealedArmor } },
			{ MarkedVirusWarcasketBodyTag, new TaggedProtection { resistance = 0.85f, sealedAgainstMarkedVirus = true, blocksMarkedVirusExposure = true, blockCategory = MarkedVirusBlockCategory.Warcasket } },
			{ MarkedVirusWarcasketBodysuitTag, new TaggedProtection { resistance = 0.70f, sealedAgainstMarkedVirus = true } },
			{ MarkedVirusWarcasketHelmetTag, new TaggedProtection { resistance = 0.25f, sealedAgainstMarkedVirus = true } },
			{ MarkedVirusWarcasketShouldersTag, new TaggedProtection { resistance = 0.15f } },
			{ MarkedVirusWarcasketPartTag, new TaggedProtection { resistance = 0.10f } },
			{ MarkedVirusVacsuitBodyTag, new TaggedProtection { resistance = 0.30f, sealedAgainstMarkedVirus = true } },
			{ MarkedVirusVacsuitHelmetTag, new TaggedProtection { resistance = 0.25f, sealedAgainstMarkedVirus = true } },
			{ MarkedVirusGasMaskTag, new TaggedProtection { resistance = 0.25f, sealedAgainstMarkedVirus = true, blocksMarkedVirusExposure = true, blockCategory = MarkedVirusBlockCategory.GasMask } }
		};

		private static bool IsBlockCategoryEnabled(MarkedVirusBlockCategory category)
		{
			switch (category)
			{
				case MarkedVirusBlockCategory.SealedArmor:
					return TheMarkedMenSettings.SealedArmorBlockExposure;
				case MarkedVirusBlockCategory.Warcasket:
					return TheMarkedMenSettings.WarcasketsBlockExposure;
				case MarkedVirusBlockCategory.Vacsuit:
					return TheMarkedMenSettings.VacsuitBlockExposure;
				case MarkedVirusBlockCategory.GasMask:
					return TheMarkedMenSettings.GasMasksBlockExposure;
				default:
					return true;
			}
		}

		private static MarkedVirusApparelProtection? GetMarkedVirusTaggedProtection(ThingDef apparelDef)
		{
			List<string> tags = apparelDef?.apparel?.tags;
			if (tags == null)
			{
				return null;
			}

			for (int i = 0; i < tags.Count; i++)
			{
				TaggedProtection tagged;
				if (!MarkedVirusTaggedProtections.TryGetValue(tags[i], out tagged))
				{
					continue;
				}

				return new MarkedVirusApparelProtection(tagged.resistance, tagged.sealedAgainstMarkedVirus, tagged.blocksMarkedVirusExposure && IsBlockCategoryEnabled(tagged.blockCategory));
			}

			return null;
		}

		private static bool ApparelHasTag(ThingDef apparelDef, string tag)
		{
			List<string> tags = apparelDef?.apparel?.tags;
			if (tags == null)
			{
				return false;
			}

			for (int i = 0; i < tags.Count; i++)
			{
				if (string.Equals(tags[i], tag, StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
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
				bool blocks = extension.blocksMarkedVirusExposure && IsBlockCategoryEnabled(extension.blockCategory);
				return ClampMarkedVirusProtection(new MarkedVirusApparelProtection(extension.resistance, extension.sealedAgainstMarkedVirus, blocks));
			}

			MarkedVirusApparelProtection? taggedProtection = GetMarkedVirusTaggedProtection(apparelDef);
			if (taggedProtection.HasValue)
			{
				return ClampMarkedVirusProtection(taggedProtection.Value);
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

			if (ApparelHasTag(apparelDef, MarkedVirusVacsuitBodyTag))
			{
				return true;
			}

			MarkedVirusProtectionExtension extension = apparelDef.GetModExtension<MarkedVirusProtectionExtension>();
			return extension != null && extension.blockCategory == MarkedVirusBlockCategory.Vacsuit;
		}

		private static bool IsMarkedVirusVacsuitHelmet(ThingDef apparelDef)
		{
			if (apparelDef?.apparel == null || !ApparelCoversBodyPartGroup(apparelDef, "FullHead"))
			{
				return false;
			}

			if (ApparelHasTag(apparelDef, MarkedVirusVacsuitHelmetTag))
			{
				return true;
			}

			MarkedVirusProtectionExtension extension = apparelDef.GetModExtension<MarkedVirusProtectionExtension>();
			return extension != null && extension.blockCategory == MarkedVirusBlockCategory.Vacsuit;
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

			// Sealed undersuits, orbital armor (XML: 0.25 sealed)
			if (torso && (ContainsOrdinalIgnoreCase(defName, "Sealed") || ContainsOrdinalIgnoreCase(label, "sealed")
				|| ContainsOrdinalIgnoreCase(defName, "AstroSuit") || ContainsOrdinalIgnoreCase(label, "astrosuit")
				|| ContainsOrdinalIgnoreCase(defName, "SecurityArmor") || ContainsOrdinalIgnoreCase(label, "security armor")
				|| ContainsOrdinalIgnoreCase(defName, "OrbitalArmor") || ContainsOrdinalIgnoreCase(label, "orbital armor")))
			{
				return new MarkedVirusApparelProtection(0.25f, true, TheMarkedMenSettings.SealedArmorBlockExposure);
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
			if (pawn == null || pawn.Dead || IsShambler(pawn))
			{
				return;
			}

			Component?.RecordCureOrigin(pawn);
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
			CrossedEquipmentGenerator.AssignEquipment(pawn);
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

			// All infected pawns are now CA_CrossedCivilian, no tuning needed
		}

		private static PawnKindDef PickReplacementMarkedKind()
		{
			// Always use CA_CrossedCivilian for replacement infected
			return CADefOf.CrossedCivilian;
		}

		private static void RemoveClassHediffs(Pawn pawn)
		{
			// No class-specific hediffs to remove for CA_CrossedCivilian
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

			if (IsShambler(pawn))
			{
				return;
			}

			// Skip all class/role hediff assignment for CA_MarkedTurned victims (virus-transformed original-kind pawns)
			if (IsMarkedTurnedPawn(pawn))
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

			// Apply base infected strength buff to all crossed faction pawns
			if (IsCrossedFactionPawn(pawn) && CADefOf.CrossedStrength != null
				&& !pawn.health.hediffSet.HasHediff(CADefOf.CrossedStrength))
			{
				pawn.health.AddHediff(CADefOf.CrossedStrength);
			}

			if (IsCrossedFactionPawn(pawn))
			{
				ApplyRandomBionics(pawn);
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
            // No bionics for CA_CrossedCivilian infected
            return;
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
			// No tier-specific hediffs for CA_CrossedCivilian infected
			return;
		}

		public static void EnsurePredatorHediffs(Pawn pawn)
		{
			TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
			if (pawn == null || pawn.health == null || !IsInfectedPawn(pawn) || IsShambler(pawn))
			{
				return;
			}

			// Skip predator hediffs for CA_MarkedTurned victims (virus-transformed original-kind pawns are mindless, not predatory)
			if (IsMarkedTurnedPawn(pawn))
			{
				return;
			}

			if (settings != null && settings.bloodlustEnabled && CADefOf.MarkedBloodlustNeed != null
				&& pawn.needs != null && pawn.needs.TryGetNeed<Need_MarkedBloodlust>() == null)
			{
				Need_MarkedBloodlust need = new Need_MarkedBloodlust(pawn);
				need.def = CADefOf.MarkedBloodlustNeed;
				pawn.needs.AllNeeds.Add(need);
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
			return false; // No more role-specific pawns
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

		public static void TransformPawnNoKindChange(Pawn pawn, bool suppressNotification = false, Pawn infector = null)
{
	if (pawn == null || pawn.Dead || IsShambler(pawn))
	{
		return;
	}

	Component?.RecordCureOrigin(pawn);

	// Apply CA_MarkedTurned hediff to mark this pawn as virus-transformed
	HediffDef markedTurned = CADefOf.MarkedTurned;
	if (markedTurned != null && !pawn.health.hediffSet.HasHediff(markedTurned))
	{
		Hediff hediff = HediffMaker.MakeHediff(markedTurned, pawn);
		pawn.health.AddHediff(hediff);
	}

	Faction faction = Component?.EnsureCrossedFaction();
	if (faction != null && pawn.Faction != faction)
	{
		pawn.SetFaction(faction, null);
	}

	pawn.guest?.SetGuestStatus(null);
	pawn.mindState?.mentalStateHandler?.Reset();
	EnsureFearlessCrossedState(pawn);
	ApplyInfectedTattoo(pawn);

	if (pawn.Drawer?.renderer != null)
	{
		pawn.Drawer.renderer.SetAllGraphicsDirty();
	}

	CrossedTacticalAI.TryAttackNearestNonInfectedPawn(pawn, true, allowRjwJob: false);

	if (!suppressNotification)
	{
		Component?.NotifyTransformation(pawn);
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
			// Tattoos require Ideology DLC
			if (!ModLister.IdeologyInstalled)
			{
				return;
			}

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

			// Handle CA_MarkedTurned victims (virus-transformed original-kind pawns)
			if (IsMarkedTurnedPawn(pawn))
			{
				EnsureFearlessCrossedState(pawn);
				ApplyInfectedTattoo(pawn);
				CrossedContagionUtility.TryContagionPulse(pawn);
				CrossedCorpseUtility.TryContaminateNearbyCorpses(pawn);
				return;
			}

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

			hediff.Severity = Mathf.Max(hediff.Severity, 0.15f);
			EnsureFearlessCrossedState(pawn);
			ApplyInfectedTattoo(pawn);
			CrossedContagionUtility.TryContagionPulse(pawn);
			CrossedCorpseUtility.TryContaminateNearbyCorpses(pawn);
		}

		public static bool CanSafelyProcessInfectedState(Pawn pawn)
		{
			if (pawn == null || pawn.Destroyed || pawn.def?.race == null || !pawn.def.race.Humanlike
				|| pawn.health?.hediffSet == null || pawn.health.Dead)
			{
				return false;
			}

			return !IsShambler(pawn);
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
			if (!TheMarkedMenSettings.MarkedPanicEnabled || map == null)
			{
				return;
			}

			HediffDef panic = CADefOf.Panic;
			if (panic == null)
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
				if (pawn.Position.InHorDistOf(origin, effectiveRadius))
				{
					ApplyMarkedPanicToPawn(pawn);
				}
			}
		}

		public static void ApplyMarkedPanicToPawn(Pawn pawn)
		{
			if (!TheMarkedMenSettings.MarkedPanicEnabled || pawn?.health?.hediffSet == null)
			{
				return;
			}

			HediffDef panic = CADefOf.Panic;
			if (panic == null || pawn.health.hediffSet.HasHediff(panic))
			{
				return;
			}

			Hediff hediff = HediffMaker.MakeHediff(panic, pawn);
			HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
			if (disappears != null)
			{
				disappears.ticksToDisappear = TheMarkedMenSettings.MarkedPanicDurationTicks;
			}

			pawn.health.AddHediff(hediff);
		}

		private static PawnKindDef PickTransformationKind(Pawn pawn)
		{
			// Always transform colonists to CA_CrossedCivilian infected
			return CADefOf.CrossedCivilian;
		}

		/// <summary>
		/// Ensure infected pawn has a valid Lord assignment.
		/// Called on dev-spawn and raid setup to prevent map-exit bug and enable tactical AI.
		/// </summary>
		public static void EnsureInfectedHasValidLord(Pawn pawn)
		{
			if (pawn == null || pawn.Dead)
				return;

			// Ensure faction is set to Crossed
			Faction crossed = Find.FactionManager.FirstFactionOfDef(CADefOf.CrossedFaction);
			if (crossed == null)
				crossed = Component?.EnsureCrossedFaction();

			if (pawn.Faction != crossed && crossed != null)
				pawn.SetFaction(crossed);

			// Check if pawn has a valid Lord already
			Lord currentLord = pawn.GetLord();
			if (currentLord != null && currentLord.faction == crossed)
				return; // Already has valid Lord

			// Try to assign to existing raid Lord on map
			Map map = pawn.Map;
			if (map != null && crossed != null)
			{
				foreach (Lord lord in map.lordManager.lords)
				{
					if (lord.faction == crossed &&
						(lord.LordJob is LordJob_AssaultColony ||
						 lord.LordJob is LordJob_Siege))
					{
						lord.AddPawn(pawn);
						return;
					}
				}
			}

			// Create new Lord if none found
			if (map != null && crossed != null)
			{
				try
				{
					LordMaker.MakeNewLord(
						crossed,
						new LordJob_AssaultColony(crossed, false, false, false, false, false, false, true),
						map,
						new List<Pawn> { pawn }
					);
				}
				catch (Exception ex)
				{
					Log.Warning($"[The Marked Men] Failed to assign Lord to infected pawn {pawn.LabelShort}: {ex.Message}");
				}
			}
		}

		/// <summary>
		/// Setup infected pawn with initial behavior state and prevent map-exit.
		/// </summary>
		public static void SetupInfectedPawn(Pawn pawn)
		{
			if (pawn == null || pawn.Dead)
				return;

			// Ensure valid Lord
			EnsureInfectedHasValidLord(pawn);

			// Prevent vanilla map-exit behavior
			if (pawn.mindState != null)
			{
				pawn.mindState.exitMapAfterTick = -1;
			}

			// Issue initial tactical job
			CrossedTacticalAI.TryIssueTacticalJob(pawn);
		}
	}
}
