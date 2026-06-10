using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public static partial class CrossedUtility
    {
        private const float DefaultApparelVirusResistance = 0.02f;
        private const float MaxWornMarkedVirusResistance = 0.90f;

        public static bool TryExpose(Pawn pawn, float chance, string source, Pawn infector = null)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings != null && !settings.infectionEnabled) return false;
            HediffDef virus = CADefOf.CrossVirus;
            if (pawn == null || virus == null || pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.Humanlike || IsCrossedPawn(pawn)) return false;
            EnsureStarterLineageResistance(pawn);
            if (HasMarkedVillageFounderImmunity(pawn))
            {
                GrantCrossVirusImmunity(pawn);
                ApplyInfectedTattoo(pawn);
                return false;
            }
            bool starterLineageBreakthrough = HasCrossVirusImmunity(pawn) && HasStarterLineageResistance(pawn);
            if (HasCrossVirusImmunity(pawn) && !starterLineageBreakthrough) return false;
            MarkedVirusApparelProtection exposureProtection = default;
            float effectiveChance = Mathf.Clamp01(chance);
            if (starterLineageBreakthrough)
                effectiveChance *= TheMarkedMenSettings.StarterLineageBreakthroughChance;
            if (CanApparelReduceMarkedVirusExposure(source))
            {
                exposureProtection = GetMarkedVirusExposureProtection(pawn);
                if (exposureProtection.blocksMarkedVirusExposure) return false;
                if (exposureProtection.resistance > 0f)
                    effectiveChance *= 1f - exposureProtection.resistance;
            }
            if (effectiveChance <= 0f) return false;
            if (!Rand.Chance(effectiveChance)) return false;
            if (starterLineageBreakthrough)
                RemoveCrossVirusImmunity(pawn);
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
                Component?.NotifyExposure(pawn, source);
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
            if (wornApparel == null || wornApparel.Count == 0) return default;
            float resistance = 0f;
            bool sealedAgainstMarkedVirus = false;
            bool blocksMarkedVirusExposure = false;
            bool hasVacsuitBody = false;
            bool hasVacsuitHelmet = false;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                ThingDef apparelDef = wornApparel[i]?.def;
                MarkedVirusApparelProtection apparelProtection = GetMarkedVirusProtectionForApparelDef(apparelDef);
                if (apparelProtection.resistance <= 0f) continue;
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
            if (blocksMarkedVirusExposure || (hasVacsuitBody && hasVacsuitHelmet))
                return new MarkedVirusApparelProtection(1f, true, true);
            return ClampMarkedVirusProtection(new MarkedVirusApparelProtection(resistance, sealedAgainstMarkedVirus));
        }

        public static MarkedVirusApparelProtection GetMarkedVirusApparelProtection(ThingDef apparelDef)
        {
            return GetMarkedVirusProtectionForApparelDef(apparelDef);
        }

        public static void ApplyMarkedVirusResistanceEquippedStatOffsets()
        {
            StatDef markedVirusResistance = CADefOf.MarkedVirusResistance;
            if (markedVirusResistance == null) return;
            List<ThingDef> allThingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allThingDefs.Count; i++)
            {
                ThingDef apparelDef = allThingDefs[i];
                if (apparelDef?.apparel == null) continue;
                MarkedVirusApparelProtection protection = GetMarkedVirusProtectionForApparelDef(apparelDef);
                float resistance = Mathf.Clamp01(protection.resistance);
                if (resistance <= 0f) continue;
                if (apparelDef.equippedStatOffsets == null)
                    apparelDef.equippedStatOffsets = new List<StatModifier>();
                bool updatedExistingOffset = false;
                for (int offsetIndex = 0; offsetIndex < apparelDef.equippedStatOffsets.Count; offsetIndex++)
                {
                    StatModifier offset = apparelDef.equippedStatOffsets[offsetIndex];
                    if (offset.stat != markedVirusResistance) continue;
                    offset.value = resistance;
                    apparelDef.equippedStatOffsets[offsetIndex] = offset;
                    updatedExistingOffset = true;
                    break;
                }
                if (!updatedExistingOffset)
                {
                    apparelDef.equippedStatOffsets.Add(new StatModifier { stat = markedVirusResistance, value = resistance });
                }
            }
        }

        private static MarkedVirusApparelProtection GetMarkedVirusProtectionForApparelDef(ThingDef apparelDef)
        {
            if (apparelDef?.apparel == null) return default;
            MarkedVirusProtectionExtension extension = apparelDef.GetModExtension<MarkedVirusProtectionExtension>();
            if (extension != null)
                return ClampMarkedVirusProtection(new MarkedVirusApparelProtection(extension.resistance, extension.sealedAgainstMarkedVirus, extension.blocksMarkedVirusExposure));
            return ClampMarkedVirusProtection(InferMarkedVirusApparelProtection(apparelDef));
        }

        private static MarkedVirusApparelProtection ClampMarkedVirusProtection(MarkedVirusApparelProtection protection)
        {
            if (protection.blocksMarkedVirusExposure) return new MarkedVirusApparelProtection(1f, true, true);
            float resistance = Mathf.Clamp(protection.resistance, 0f, MaxWornMarkedVirusResistance);
            return new MarkedVirusApparelProtection(resistance, protection.sealedAgainstMarkedVirus);
        }

        private static bool IsMarkedVirusFullSealApparel(ThingDef apparelDef)
        {
            if (apparelDef?.apparel == null) return false;
            string defName = apparelDef.defName ?? string.Empty;
            string label = apparelDef.label ?? string.Empty;
            bool torso = ApparelCoversBodyPartGroup(apparelDef, "Torso");
            return IsMarkedVirusWarcasketBody(apparelDef)
                || torso && ContainsOrdinalIgnoreCase(defName, "HAZMAT") && (ContainsOrdinalIgnoreCase(defName, "Suit") || ContainsOrdinalIgnoreCase(label, "suit"));
        }

        private static bool IsMarkedVirusWarcasketBody(ThingDef apparelDef)
        {
            if (apparelDef?.apparel == null) return false;
            string defName = apparelDef.defName ?? string.Empty;
            return ContainsOrdinalIgnoreCase(defName, "Warcasket")
                && ApparelCoversBodyPartGroup(apparelDef, "Torso")
                && !ContainsOrdinalIgnoreCase(defName, "Shoulders")
                && !ContainsOrdinalIgnoreCase(defName, "Bodysuit");
        }

        private static bool IsMarkedVirusVacsuitBody(ThingDef apparelDef)
        {
            if (apparelDef?.apparel == null || !ApparelCoversBodyPartGroup(apparelDef, "Torso")) return false;
            string defName = apparelDef.defName ?? string.Empty;
            string label = apparelDef.label ?? string.Empty;
            return ContainsOrdinalIgnoreCase(defName, "Vacsuit")
                || ContainsOrdinalIgnoreCase(label, "vacsuit")
                || ContainsOrdinalIgnoreCase(defName, "EVAsuit")
                || ContainsOrdinalIgnoreCase(label, "EVA suit");
        }

        private static bool IsMarkedVirusVacsuitHelmet(ThingDef apparelDef)
        {
            if (apparelDef?.apparel == null || !ApparelCoversBodyPartGroup(apparelDef, "FullHead")) return false;
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
            if (IsMarkedVirusFullSealApparel(apparelDef)) return new MarkedVirusApparelProtection(0.85f, true, false);
            if (IsMarkedVirusVacsuitBody(apparelDef)) return new MarkedVirusApparelProtection(0.30f, true);
            if ((ContainsOrdinalIgnoreCase(defName, "Sealed") || ContainsOrdinalIgnoreCase(label, "sealed") || ContainsOrdinalIgnoreCase(defName, "AstroSuit") || ContainsOrdinalIgnoreCase(label, "astrosuit")) && torso)
                return new MarkedVirusApparelProtection(0.25f, true);
            if (IsMarkedVirusVacsuitHelmet(apparelDef)) return new MarkedVirusApparelProtection(0.25f, true);
            if (toxGasImmune || ContainsOrdinalIgnoreCase(defName, "GasMask") || ContainsOrdinalIgnoreCase(label, "gas mask") || ContainsOrdinalIgnoreCase(defName, "HAZMATMask") || ContainsOrdinalIgnoreCase(defName, "WarcasketHelmet") || ContainsOrdinalIgnoreCase(defName, "AstroMask"))
                return new MarkedVirusApparelProtection(0.25f, true);
            if (toxicEnvironmentResistance >= 0.75f && fullHead) return new MarkedVirusApparelProtection(0.20f, true);
            if (vacuumResistance >= 0.30f && torso) return new MarkedVirusApparelProtection(0.25f, true);
            if (IsPoweredArmorHelmet(defName) || IsPoweredArmorHelmet(label)) return new MarkedVirusApparelProtection(0.20f, false);
            if (IsPoweredArmorBody(defName) || IsPoweredArmorBody(label)) return new MarkedVirusApparelProtection(0.25f, false);
            if (ContainsOrdinalIgnoreCase(defName, "PlagueMask") || ContainsOrdinalIgnoreCase(label, "plague mask")) return new MarkedVirusApparelProtection(0.10f, false);
            if (ContainsOrdinalIgnoreCase(defName, "ClothMask") || ContainsOrdinalIgnoreCase(defName, "SurgicalMask") || ContainsOrdinalIgnoreCase(label, "surgical mask") || ContainsOrdinalIgnoreCase(label, "face mask"))
                return new MarkedVirusApparelProtection(0.06f, false);
            if (IsArmorOrSuit(defName) || IsArmorOrSuit(label)) return new MarkedVirusApparelProtection(0.08f, false);
            if (ContainsOrdinalIgnoreCase(defName, "Mask") || ContainsOrdinalIgnoreCase(label, "mask")) return new MarkedVirusApparelProtection(0.05f, false);
            if (ContainsOrdinalIgnoreCase(defName, "Helmet") || ContainsOrdinalIgnoreCase(label, "helmet")) return new MarkedVirusApparelProtection(0.05f, false);
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
            if (bodyPartGroups == null) return false;
            for (int i = 0; i < bodyPartGroups.Count; i++)
            {
                if (string.Equals(bodyPartGroups[i]?.defName, bodyPartGroupDefName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static float EquippedStatOffset(ThingDef apparelDef, string statDefName)
        {
            List<StatModifier> offsets = apparelDef?.equippedStatOffsets;
            if (offsets == null) return 0f;
            float value = 0f;
            for (int i = 0; i < offsets.Count; i++)
            {
                StatDef stat = offsets[i].stat;
                if (string.Equals(stat?.defName, statDefName, StringComparison.OrdinalIgnoreCase))
                    value += offsets[i].value;
            }
            return value;
        }

        private static bool ContainsOrdinalIgnoreCase(string text, string value)
        {
            return !string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(value)
                && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool CanApparelReduceMarkedVirusExposure(string source)
        {
            return string.IsNullOrEmpty(source) || source.IndexOf("food", StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}
