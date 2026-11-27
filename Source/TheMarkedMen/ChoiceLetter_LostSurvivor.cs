using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace TheMarkedMen
{
    public class ChoiceLetter_LostSurvivor : ChoiceLetter
    {
        public Pawn pawn;
        
        

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (pawn != null && !pawn.Dead && pawn.Spawned)
                {
                    yield return DiaOptionAccept;
                    yield return DiaOptionReject;
                    
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
                        MakePawnWandererAndSendAway();
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
                        MakePawnWandererAndSendAway();
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
            MakePawnWandererAndSendAway();
        }

        private void MakePawnWandererAndSendAway()
        {
            if (pawn == null) return;

            Map map = pawn.Map;
            if (map == null)
            {
                pawn.Destroy(DestroyMode.Vanish);
                return;
            }

            if (!pawn.Spawned)
            {
                pawn.Destroy(DestroyMode.Vanish);
                return;
            }

            var dormantMark = pawn.health.hediffSet.GetFirstHediffOfDef(CADefOf.CA_DormantMark);
            if (dormantMark != null)
            {
                pawn.health.RemoveHediff(dormantMark);
            }

            pawn.SetFaction(null);
            pawn.guest?.SetGuestStatus(null);
            pawn.mindState?.mentalStateHandler?.Reset();
            pawn.jobs.StopAll(true);
            pawn.lord = null;

            IntVec3 edgeSpot = CellFinder.RandomEdgeCell(map);
            if (edgeSpot.IsValid)
            {
                pawn.Position = edgeSpot;
                pawn.DeSpawn();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
        }
    }
}