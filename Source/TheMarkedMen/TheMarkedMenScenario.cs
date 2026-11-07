using System.Collections.Generic;
using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace TheMarkedMen
{
    public sealed class ScenPart_MarkedMenPlayerState : ScenPart
    {
        private static readonly string[] StartingKindDefNames =
        {
            "CA_CrossedCivilian",
            "CA_CrossedScout",
            "CA_CrossedHunter",
            "CA_CrossedShooter",
            "CA_CrossedRaider",
            "CA_CrossedSoldier",
            "CA_CrossedBrute",
            "CA_CrossedPyromaniac",
            "CA_CrossedAlpha",
            "CA_CrossedWarlord",
            "CA_MarkedMan"
        };

        private int nextStartingKindIndex;
        private static bool worldGenerationSettingsCaptured;
        private static bool originalFactionHidden;
        private static int originalRequiredCountAtGameStart;
        private static int originalMaxConfigurableAtWorldCreation;
        private static int originalStartingCountAtWorldCreation;
        private static float originalSettlementGenerationWeight;

        public static bool IsActive
        {
            get
            {
                IEnumerable<ScenPart> parts = Find.Scenario?.AllParts;
                if (parts == null)
                {
                    return false;
                }

                foreach (ScenPart part in parts)
                {
                    if (part is ScenPart_MarkedMenPlayerState)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override void Notify_PawnGenerated(Pawn pawn, PawnGenerationContext context, bool redressed)
        {
            base.Notify_PawnGenerated(pawn, context, redressed);
            if (context == PawnGenerationContext.PlayerStarter)
            {
                PreparePlayerStarter(pawn);
            }
        }

        public override void PostGameStart()
        {
            base.PostGameStart();
            RemoveCrossedSettlements();
            PrepareExistingPlayerStarters();
            SetFactionWorldGenerationSuppressed(false);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextStartingKindIndex, "nextStartingKindIndex", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && nextStartingKindIndex < 0)
            {
                nextStartingKindIndex = 0;
            }
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            yield return "Start as a colony of player-controlled infected without generating an enemy Marked Men faction. Transformed victims serve for a limited time set in the mod settings before returning to the Marked Men.";
        }

        public static void SetFactionWorldGenerationSuppressed(bool suppress)
        {
            FactionDef factionDef = CADefOf.CrossedFaction;
            if (factionDef == null)
            {
                Log.Error("[The Marked Men] Could not update Marked Men world generation settings because the faction definition is missing.");
                return;
            }

            if (!worldGenerationSettingsCaptured)
            {
                originalFactionHidden = factionDef.hidden;
                originalRequiredCountAtGameStart = factionDef.requiredCountAtGameStart;
                originalMaxConfigurableAtWorldCreation = factionDef.maxConfigurableAtWorldCreation;
                originalStartingCountAtWorldCreation = factionDef.startingCountAtWorldCreation;
                originalSettlementGenerationWeight = factionDef.settlementGenerationWeight;
                worldGenerationSettingsCaptured = true;
            }

            factionDef.hidden = suppress || originalFactionHidden;
            factionDef.requiredCountAtGameStart = suppress ? 0 : originalRequiredCountAtGameStart;
            factionDef.maxConfigurableAtWorldCreation = suppress ? 0 : originalMaxConfigurableAtWorldCreation;
            factionDef.startingCountAtWorldCreation = suppress ? 0 : originalStartingCountAtWorldCreation;
            factionDef.settlementGenerationWeight = suppress ? 0f : originalSettlementGenerationWeight;
        }

        private void PrepareExistingPlayerStarters()
        {
            if (Find.Maps == null)
            {
                return;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                IReadOnlyList<Pawn> colonists = Find.Maps[i]?.mapPawns?.FreeColonistsSpawned;
                if (colonists == null)
                {
                    continue;
                }

                for (int j = 0; j < colonists.Count; j++)
                {
                    PreparePlayerStarter(colonists[j]);
                }
            }
        }

        private void PreparePlayerStarter(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Faction != Faction.OfPlayer || pawn.health?.hediffSet == null)
            {
                return;
            }

            HediffDef markedTurned = CADefOf.MarkedTurned;
            if (markedTurned == null)
            {
                Log.Error("[The Marked Men] Could not prepare a player starter because CA_MarkedTurned is missing.");
                return;
            }

            if (!pawn.health.hediffSet.HasHediff(markedTurned))
            {
                PawnKindDef kind = GetNextStartingKind();
                if (kind != null && pawn.kindDef != kind)
                {
                    pawn.ChangeKind(kind);
                }

                pawn.SetFaction(Faction.OfPlayer, null);
                pawn.health.AddHediff(markedTurned);
                if (ModsConfig.BiotechActive && pawn.genes != null && CADefOf.MarkedOne != null)
                {
                    pawn.genes.SetXenotypeDirect(CADefOf.MarkedOne);
                }

                CrossedEquipmentGenerator.AssignEquipment(pawn);
                CrossedUtility.ApplyInfectedTattoo(pawn);
            }
        }

        private PawnKindDef GetNextStartingKind()
        {
            for (int i = 0; i < StartingKindDefNames.Length; i++)
            {
                int index = nextStartingKindIndex % StartingKindDefNames.Length;
                nextStartingKindIndex = nextStartingKindIndex == int.MaxValue ? 0 : nextStartingKindIndex + 1;
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(StartingKindDefNames[index]);
                if (kind != null)
                {
                    return kind;
                }
            }

            Log.Error("[The Marked Men] Could not find any infected pawn kinds for the playable infected scenario.");
            return null;
        }

        private static void RemoveCrossedSettlements()
        {
            List<Settlement> settlements = Find.World?.worldObjects?.Settlements;
            if (settlements == null)
            {
                return;
            }

            for (int i = settlements.Count - 1; i >= 0; i--)
            {
                Settlement settlement = settlements[i];
                if (settlement != null && settlement.Faction?.def == CADefOf.CrossedFaction)
                {
                    settlement.Destroy();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Page_CreateWorldParams), "ResetFactionCounts")]
    public static class Patch_MarkedMenWorldGenerationPopulation
    {
        public static void Prefix()
        {
            ScenPart_MarkedMenPlayerState.SetFactionWorldGenerationSuppressed(ScenPart_MarkedMenPlayerState.IsActive);
        }
    }

    public sealed class ScenPart_MarkedSurvivorState : ScenPart
    {
        public override void Notify_PawnGenerated(Pawn pawn, PawnGenerationContext context, bool redressed)
        {
            base.Notify_PawnGenerated(pawn, context, redressed);
            if (context == PawnGenerationContext.PlayerStarter)
            {
                CrossedUtility.GrantMarkedVillageFounderState(pawn);
            }
        }

        public override void PostGameStart()
        {
            base.PostGameStart();
            ApplyFounderStateToPlayerStarters();
            CleanupVirusFromImmuneStarters();
        }

        private static void CleanupVirusFromImmuneStarters()
        {
            if (Find.Maps == null) return;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                IReadOnlyList<Pawn> colonists = map?.mapPawns?.FreeColonistsSpawned;
                if (colonists == null) continue;
                for (int j = 0; j < colonists.Count; j++)
                {
                    CrossedUtility.RemoveCrossVirusIfImmune(colonists[j]);
                }
            }
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            yield return "Three immune survivors start with basic supplies and industrial technology. Each has a 50% chance to carry the visible marked rash.";
        }

        private static void ApplyFounderStateToPlayerStarters()
        {
            if (Find.Maps == null)
            {
                return;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                IReadOnlyList<Pawn> colonists = map?.mapPawns?.FreeColonistsSpawned;
                if (colonists == null)
                {
                    continue;
                }

                for (int j = 0; j < colonists.Count; j++)
                {
                    CrossedUtility.GrantMarkedVillageFounderState(colonists[j]);
                }
            }
        }
    }
}
