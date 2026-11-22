using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace TheMarkedMen
{
    public class ChoiceLetter_LostSurvivor : ChoiceLetter
    {
        public Pawn pawn;
        // Color field moved to lazy initialization to avoid static Unity type resolution at load time
        // private Color color = new Color(1f, 0.85f, 0f); // REMOVED - causing TypeLoadException

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (pawn != null && !pawn.Dead && pawn.Spawned)
                {
                    yield return DiaOptionAccept;
                    yield return DiaOptionReject;
                    // If the "send away" option is enabled, add a second reject option
                    if (TheMarkedMenMod.Settings?.survivorRefuseSendsAway ?? true)
                    {
                        yield return DiaOptionSendAway;
                    }
                }
                yield return Option_Close;
            }
        }

        private DiaOption DiaOptionAccept
        {
            get
            {
                DiaOption diaOption = new DiaOption("CA_LostSurvivor_Accept".Translate());
                diaOption.action = () =>
                {
                    if (pawn != null && !pawn.Dead && pawn.Spawned)
                    {
                        pawn.SetFaction(Faction.OfPlayer);
                    }
                };
                diaOption.resolveTree = true;
                return diaOption;
            }
        }

        private DiaOption DiaOptionReject
        {
            get
            {
                DiaOption diaOption = new DiaOption("CA_LostSurvivor_Reject".Translate());
                diaOption.action = () =>
                {
                    IfPawnExistsAndNotDestroyed(() =>
                    {
                        if (TheMarkedMenMod.Settings?.survivorRefuseSendsAway ?? true)
                        {
                            SendPawnAwayAtEdge();
                        }
                        else
                        {
                            DestroyPawn();
                        }
                    });
                };
                diaOption.resolveTree = true;
                return diaOption;
            }
        }

        private DiaOption DiaOptionSendAway
        {
            get
            {
                DiaOption diaOption = new DiaOption("CA_LostSurvivor_SendAway".Translate());
                diaOption.action = () =>
                {
                    IfPawnExistsAndNotDestroyed(() =>
                    {
                        SendPawnAwayAtEdge();
                    });
                };
                diaOption.resolveTree = true;
                return diaOption;
            }
        }

        private void IfPawnExistsAndNotDestroyed(System.Action action)
        {
            if (pawn != null && !pawn.Destroyed)
            {
                action?.Invoke();
            }
        }

        private void DestroyPawn()
        {
            if (pawn.Spawned)
            {
                pawn.DeSpawn();
            }
            pawn.Destroy(DestroyMode.Vanish);
        }

        private void SendPawnAwayAtEdge()
        {
            if (pawn == null) return;

            // If on a map, find the edge cell closest to where they spawned
            // and de-spawn them there so they can walk off the map naturally
            Map map = pawn.Map;
            if (map != null && pawn.Spawned)
            {
                // Mark them as a guest with no faction to leave naturally
                pawn.SetFaction(null);
                pawn.guest?.SetGuestStatus(null);
                pawn.mindState?.mentalStateHandler?.Reset();

                // Find an edge cell to de-spawn them
                IntVec3 edgeSpot = CellFinder.RandomEdgeCell(map);
                if (edgeSpot.IsValid)
                {
                    pawn.Position = edgeSpot;
                }
                pawn.DeSpawn();
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
        }
    }
}