using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
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
