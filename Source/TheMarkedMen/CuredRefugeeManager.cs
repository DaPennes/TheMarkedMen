using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    
    
    
    public class CuredRefugeeRecord : IExposable
    {
        public int pawnId;
        public Faction originalFaction;

        public CuredRefugeeRecord() { }

        public CuredRefugeeRecord(int id, Faction faction)
        {
            pawnId = id;
            originalFaction = faction;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_References.Look(ref originalFaction, "originalFaction");
        }
    }

    
    
    
    
    public class CuredRefugeeManager : MapComponent
    {
        private Dictionary<int, Pawn> refugeeLookup = new Dictionary<int, Pawn>();
        private HashSet<int> pendingRefugees = new HashSet<int>();
        private Dictionary<int, CuredRefugeeRecord> refugeeRecords = new Dictionary<int, CuredRefugeeRecord>();
        private List<(Pawn pawn, Faction faction)> lettersToGenerate = new List<(Pawn, Faction)>();
        private Dictionary<int, int> departureSchedule = new Dictionary<int, int>(); 

        public CuredRefugeeManager(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            
            if (lettersToGenerate.Count > 0)
            {
                foreach (var (pawn, faction) in lettersToGenerate)
                {
                    GenerateRefugeeLetter(pawn, faction);
                }
                lettersToGenerate.Clear();
            }

            
            List<int> toDepart = null;
            foreach (var kvp in departureSchedule)
            {
                int pawnId = kvp.Key;
                int departureTick = kvp.Value;

                if (Find.TickManager.TicksGame >= departureTick)
                {
                    Pawn pawn = FindPawnById(pawnId);
                    if (pawn != null && pawn.Spawned)
                    {
                        
                        pawn.DeSpawn();
                        Messages.Message("TheMarkedMen_MessageRefugeeLeft".Translate(pawn.LabelShort),
                            MessageTypeDefOf.NeutralEvent);
                    }

                    if (toDepart == null) toDepart = new List<int>();
                    toDepart.Add(pawnId);
                }
            }

            if (toDepart != null)
            {
                foreach (int id in toDepart)
                {
                    departureSchedule.Remove(id);
                }
            }

            
            List<int> toRemove = null;
            foreach (int id in pendingRefugees)
            {
                Pawn pawn = FindPawnById(id);
                if (pawn == null || pawn.Dead || !pawn.Spawned)
                {
                    if (toRemove == null) toRemove = new List<int>();
                    toRemove.Add(id);
                }
            }

            if (toRemove != null)
            {
                foreach (int id in toRemove)
                {
                    pendingRefugees.Remove(id);
                    refugeeRecords.Remove(id);
                    refugeeLookup.Remove(id);
                    departureSchedule.Remove(id);
                }
            }
        }

        
        
        
        
        public void RegisterRefugee(Pawn pawn, Faction originalFaction)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned)
                return;

            int id = pawn.thingIDNumber;
            if (pendingRefugees.Contains(id))
                return; 

            
            if (originalFaction == Faction.OfPlayer)
                return;

            pendingRefugees.Add(id);
            refugeeRecords[id] = new CuredRefugeeRecord(id, originalFaction);
            refugeeLookup[id] = pawn;

            
            lettersToGenerate.Add((pawn, originalFaction));
        }

        
        
        
        public void ScheduleRefugeeDeparture(Pawn pawn)
        {
            if (pawn == null)
                return;

            int id = pawn.thingIDNumber;
            
            int daysUntilDeparture = Rand.Range(3, 6);
            int departureTick = Find.TickManager.TicksGame + (daysUntilDeparture * 60000);

            departureSchedule[id] = departureTick;
        }

        
        
        
        public void RemoveRefugee(Pawn pawn)
        {
            if (pawn == null)
                return;

            int id = pawn.thingIDNumber;
            pendingRefugees.Remove(id);
            refugeeRecords.Remove(id);
            if (departureSchedule.ContainsKey(id))
                departureSchedule.Remove(id);
        }

        
        
        
        public Faction GetOriginalFaction(Pawn pawn)
        {
            if (pawn == null)
                return null;

            int id = pawn.thingIDNumber;
            if (refugeeRecords.TryGetValue(id, out CuredRefugeeRecord record))
            {
                return record.originalFaction;
            }

            return null;
        }

        
        
        
        public bool IsRefugee(Pawn pawn)
        {
            return pawn != null && pendingRefugees.Contains(pawn.thingIDNumber);
        }

        
        
        
        private void GenerateRefugeeLetter(Pawn pawn, Faction originalFaction)
        {
            
            if (Find.LetterStack == null)
            {
                return;
            }

            
            string bodyText = "TheMarkedMen_CuredRefugee_LetterBody".Translate(pawn?.LabelShort ?? "Unknown");

            if (originalFaction != null)
            {
                bodyText += "\n\n" + "TheMarkedMen_CuredRefugee_LetterFromFaction".Translate(originalFaction.Name);
            }

            if (pawn?.health != null)
            {
                int healthPercent = (int)(pawn.health.summaryHealth.SummaryHealthPercent * 100);
                bodyText += "\n\n" + "TheMarkedMen_CuredRefugee_HealthStatus".Translate(healthPercent);
            }

            bodyText += "\n\n" + "TheMarkedMen_CuredRefugee_RequestToJoin".Translate();

            ChoiceLetter_CuredRefugee letter = new ChoiceLetter_CuredRefugee
            {
                pawn = pawn,
                originalFaction = originalFaction,
                title = "TheMarkedMen_CuredRefugee_LetterTitle".Translate(pawn?.LabelShort ?? "Unknown"),
                Label = "TheMarkedMen_CuredRefugee_LetterTitle".Translate(pawn?.LabelShort ?? "Unknown"),
                Text = bodyText,
                lookTargets = new LookTargets(pawn),
                def = DefDatabase<LetterDef>.GetNamed("TheMarkedMen_CuredRefugeeLetter", false) ?? LetterDefOf.NeutralEvent
            };

            Find.LetterStack.ReceiveLetter(letter);
        }

        
        
        
        private Pawn FindPawnById(int id)
        {
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (p.thingIDNumber == id)
                    return p;
            }
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pendingRefugees, "pendingRefugees", LookMode.Value);
            if (pendingRefugees == null)
            {
                pendingRefugees = new HashSet<int>();
            }

            Scribe_Collections.Look(ref refugeeRecords, "refugeeRecords", LookMode.Value, LookMode.Deep);
            if (refugeeRecords == null)
            {
                refugeeRecords = new Dictionary<int, CuredRefugeeRecord>();
            }

            Scribe_Collections.Look(ref departureSchedule, "departureSchedule", LookMode.Value, LookMode.Value);
            if (departureSchedule == null)
            {
                departureSchedule = new Dictionary<int, int>();
            }
        }

        
        
        
        public static CuredRefugeeManager GetForMap(Map map)
        {
            if (map == null)
                return null;

            CuredRefugeeManager manager = map.GetComponent<CuredRefugeeManager>();
            if (manager == null)
            {
                manager = new CuredRefugeeManager(map);
                map.components.Add(manager);
            }
            return manager;
        }
    }
}
