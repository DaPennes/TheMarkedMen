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

            // Process departures and cleanup all stale entries in one pass
            List<int> toRemove = null;

            // Check departure schedule
            foreach (var kvp in departureSchedule)
            {
                int pawnId = kvp.Key;
                int departureTick = kvp.Value;

                if (Find.TickManager.TicksGame >= departureTick)
                {
                    Pawn pawn = FindPawnById(pawnId);
                    string pawnLabel = pawn?.LabelShort ?? "Unknown";

                    if (pawn == null || pawn.Dead || pawn.Destroyed)
                    {
                        // Pawn is gone - clean up silently
                        if (toRemove == null) toRemove = new List<int>();
                        toRemove.Add(pawnId);
                    }
                    else if (pawn.Spawned)
                    {
                        pawn.DeSpawn();
                        pawn.Destroy(DestroyMode.Vanish);
                        Messages.Message("TheMarkedMen_MessageRefugeeLeft".Translate(pawnLabel),
                            MessageTypeDefOf.NeutralEvent);

                        if (toRemove == null) toRemove = new List<int>();
                        toRemove.Add(pawnId);
                    }
                    else if (!pawn.Spawned && pawn != null)
                    {
                        // Pawn exists but not spawned - just clean up
                        if (toRemove == null) toRemove = new List<int>();
                        toRemove.Add(pawnId);
                    }
                }
            }

            // Check pending refugees for invalid pawns
            foreach (int id in pendingRefugees)
            {
                Pawn pawn = FindPawnById(id);
                if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
                {
                    if (toRemove == null) toRemove = new List<int>();
                    toRemove.Add(id);
                }
            }

            // Atomic cleanup of all stale entries
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
            refugeeLookup.Remove(id);
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
            // First check the cache (O(1))
            if (refugeeLookup.TryGetValue(id, out Pawn cachedPawn))
            {
                return cachedPawn;
            }

            // Fall back to linear search (O(n)) for pawns that may have been moved to cache
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (p.thingIDNumber == id)
                {
                    // Update cache for future lookups
                    refugeeLookup[id] = p;
                    return p;
                }
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

            // Rebuild refugeeLookup cache after load (it's a transient cache of spawned pawns)
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RebuildRefugeeLookupCache();
            }
        }

        private void RebuildRefugeeLookupCache()
        {
            refugeeLookup.Clear();
            foreach (int id in pendingRefugees)
            {
                Pawn pawn = FindPawnById(id);
                if (pawn != null && pawn.Spawned)
                {
                    refugeeLookup[id] = pawn;
                }
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
