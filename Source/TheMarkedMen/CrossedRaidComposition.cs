using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    internal enum CrossedRaidCompositionRole
    {
        Warband,
        Horde,
        Probe,
        Ambush
    }

    internal sealed class CrossedRaidComposition
    {
        // Weights lerp from early-game to late-game pressure, then the player's
        // Enemy Mix multipliers are applied by AddWeightedKind. Elite counts are
        // capped so a lucky roll cannot flood a raid with commanders.
        private readonly CrossedRaidCompositionRole role;
        private readonly float threat;
        private readonly int maxAlphas;
        private readonly int maxWarlords;
        private readonly int maxMarkedMen;
        private int alphas;
        private int warlords;
        private int markedMen;

        public CrossedRaidComposition(CrossedRaidCompositionRole role, float points)
        {
            this.role = role;
            threat = NormalizeThreat(points);
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            maxAlphas = Mathf.Clamp(settings?.maximumAlphasPerRaid ?? 2, 0, 99);
            maxWarlords = 1;
            maxMarkedMen = role == CrossedRaidCompositionRole.Warband ? 2 : 1;
        }

        public static PawnKindDef PickWarbandLeader()
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (CADefOf.MarkedMan != null
                && (settings == null || settings.KindWeightMultiplier(CADefOf.MarkedMan) > 0f))
            {
                return CADefOf.MarkedMan;
            }
            if (CADefOf.CrossedWarlord != null
                && settings != null
                && settings.KindWeightMultiplier(CADefOf.CrossedWarlord) > 0f)
            {
                return CADefOf.CrossedWarlord;
            }
            if (CADefOf.CrossedAlpha != null
                && settings != null
                && settings.maximumAlphasPerRaid > 0
                && settings.KindWeightMultiplier(CADefOf.CrossedAlpha) > 0f)
            {
                return CADefOf.CrossedAlpha;
            }
            return CADefOf.CrossedCivilian ?? CADefOf.CrossedScout;
        }

        public void Register(PawnKindDef kind)
        {
            if (kind == CADefOf.CrossedAlpha)
            {
                alphas++;
            }
            else if (kind == CADefOf.CrossedWarlord)
            {
                warlords++;
            }
            else if (kind == CADefOf.MarkedMan)
            {
                markedMen++;
            }
        }

        public PawnKindDef Pick()
        {
            PawnKindDef selected = null;
            float totalWeight = 0f;
            bool alphaAllowed = alphas < maxAlphas;
            bool warlordAllowed = warlords < maxWarlords;
            bool markedManAllowed = markedMen < maxMarkedMen;

            if (role == CrossedRaidCompositionRole.Probe)
            {
                Add(ref selected, ref totalWeight, CADefOf.CrossedScout, W(5f, 6f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedHunter, W(3f, 5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedShooter, W(2f, 4f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedCivilian, W(3f, 2.5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, W(0.5f, 1.75f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedRaider, W(0.5f, 1.5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedSoldier, W(0.25f, 1f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedBrute, W(0.35f, 1.25f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedAlpha, alphaAllowed ? W(0.05f, 0.3f) : 0f);
                Add(ref selected, ref totalWeight, CADefOf.CrossedWarlord, warlordAllowed ? W(0.01f, 0.08f) : 0f);
                Add(ref selected, ref totalWeight, CADefOf.MarkedMan, markedManAllowed ? W(0.01f, 0.03f) : 0f);
            }
            else if (role == CrossedRaidCompositionRole.Horde)
            {
                Add(ref selected, ref totalWeight, CADefOf.CrossedCivilian, W(14f, 7f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedScout, W(2f, 5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedHunter, W(3f, 7f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedShooter, W(2f, 5.5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedRaider, W(1.5f, 5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedSoldier, W(0.5f, 3.5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedBrute, W(1.5f, 5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, W(2f, 4f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedAlpha, alphaAllowed ? W(0.2f, 1.2f) : 0f);
                Add(ref selected, ref totalWeight, CADefOf.CrossedWarlord, warlordAllowed ? W(0.05f, 0.5f) : 0f);
                Add(ref selected, ref totalWeight, CADefOf.MarkedMan, markedManAllowed ? W(0.02f, 0.25f) : 0f);
            }
            else if (role == CrossedRaidCompositionRole.Ambush)
            {
                Add(ref selected, ref totalWeight, CADefOf.CrossedCivilian, W(8f, 4f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedHunter, W(4f, 8f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedScout, W(3f, 5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedShooter, W(2f, 4.5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedRaider, W(2f, 4.5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedSoldier, W(1f, 3f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, W(1.5f, 3.5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedBrute, W(0.8f, 3f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedAlpha, alphaAllowed ? W(0.1f, 0.7f) : 0f);
                Add(ref selected, ref totalWeight, CADefOf.CrossedWarlord, warlordAllowed ? W(0.02f, 0.25f) : 0f);
                Add(ref selected, ref totalWeight, CADefOf.MarkedMan, markedManAllowed ? W(0.01f, 0.1f) : 0f);
            }
            else
            {
                Add(ref selected, ref totalWeight, CADefOf.CrossedCivilian, W(16f, 6f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedScout, W(3f, 6f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedHunter, W(3f, 6.5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedShooter, W(2.5f, 7f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedRaider, W(2f, 6f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedSoldier, W(1f, 5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedBrute, W(0.8f, 3.5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedPyromaniac, W(1f, 2.5f));
                Add(ref selected, ref totalWeight, CADefOf.CrossedAlpha, alphaAllowed ? W(0.15f, 1.1f) : 0f);
                Add(ref selected, ref totalWeight, CADefOf.CrossedWarlord, warlordAllowed ? W(0.03f, 0.45f) : 0f);
                Add(ref selected, ref totalWeight, CADefOf.MarkedMan, markedManAllowed ? W(0.01f, 0.2f) : 0f);
            }

            if (selected == null)
            {
                selected = role == CrossedRaidCompositionRole.Probe
                    ? CADefOf.CrossedScout ?? CADefOf.CrossedCivilian
                    : CADefOf.CrossedCivilian ?? CADefOf.CrossedScout;
            }

            Register(selected);
            return selected;
        }

        private float W(float low, float high)
        {
            return Mathf.Lerp(low, high, threat);
        }

        private static void Add(ref PawnKindDef selected, ref float totalWeight, PawnKindDef kind, float weight)
        {
            TheMarkedMenSettings.AddWeightedKind(ref selected, ref totalWeight, kind, weight);
        }

        private static float NormalizeThreat(float points)
        {
            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            float minimum = Mathf.Max(120f, settings?.minimumRaidPoints ?? 2000f);
            float maximum = Mathf.Max(minimum + 1f, settings?.maximumRaidPoints ?? 10000f);
            return Mathf.Clamp01((points - minimum) / (maximum - minimum));
        }
    }
}
