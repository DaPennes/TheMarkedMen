using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public struct ScentMarker
    {
        public IntVec3 position;
        public float strength;
        public int createdTick;
        public int sourcePawnId;
    }

    public struct NoiseEvent
    {
        public IntVec3 position;
        public float strength;
        public int createdTick;
        public int decayTicks;
    }

    public struct MemoryEvent
    {
        public IntVec3 position;
        public int lastSeenTick;
        public int pawnId;
        public bool wasHostile;
    }

    public class MarkedMenMemoryGrid : MapComponent
    {
        private const int MaxScentMarkers = 300;
        private const int MaxNoiseEvents = 60;
        private const int MaxMemoryEvents = 150;
        private const int DecayIntervalTicks = 250;
        private const int ScentLifetimeTicks = 7500;
        private const int NoiseLifetimeTicks = 3000;
        private const int MemoryLifetimeTicks = 30000;
        private const float ScentSpreadRadius = 3f;
        private const int ScentSpreadIntervalTicks = 1500;
        private const float MinScentStrength = 0.01f;
        private const int CellGridSize = 8;

        private List<ScentMarker> scentMarkers = new List<ScentMarker>();
        private List<NoiseEvent> noiseEvents = new List<NoiseEvent>();
        private List<MemoryEvent> memoryEvents = new List<MemoryEvent>();
        private int nextDecayTick;
        private int nextScentSpreadTick;
        private int nextGCTick;

        public MarkedMenMemoryGrid(Map map) : base(map)
        {
        }

        public void AddScent(IntVec3 position, float strength, Pawn source)
        {
            if (!position.InBounds(map) || strength <= 0f)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;
            int sourceId = source?.thingIDNumber ?? 0;

            for (int i = 0; i < scentMarkers.Count; i++)
            {
                ScentMarker existing = scentMarkers[i];
                if (existing.position == position && existing.sourcePawnId == sourceId)
                {
                    existing.strength = Mathf.Min(existing.strength + strength, 1f);
                    existing.createdTick = tick;
                    scentMarkers[i] = existing;
                    return;
                }
            }

            if (scentMarkers.Count >= MaxScentMarkers)
            {
                RemoveOldestScent();
            }

            scentMarkers.Add(new ScentMarker
            {
                position = position,
                strength = Mathf.Clamp01(strength),
                createdTick = tick,
                sourcePawnId = sourceId
            });
        }

        public void AddNoise(IntVec3 position, float strength, int decayTicks)
        {
            if (!position.InBounds(map) || strength <= 0f)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;

            if (noiseEvents.Count >= MaxNoiseEvents)
            {
                RemoveOldestNoise();
            }

            noiseEvents.Add(new NoiseEvent
            {
                position = position,
                strength = Mathf.Clamp01(strength),
                createdTick = tick,
                decayTicks = Mathf.Max(decayTicks, NoiseLifetimeTicks)
            });
        }

        public void AddMemory(IntVec3 position, Pawn target)
        {
            if (!position.InBounds(map) || target == null)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;

            for (int i = 0; i < memoryEvents.Count; i++)
            {
                MemoryEvent existing = memoryEvents[i];
                if (existing.pawnId == target.thingIDNumber)
                {
                    existing.position = position;
                    existing.lastSeenTick = tick;
                    existing.wasHostile = target.HostileTo(Faction.OfPlayer);
                    memoryEvents[i] = existing;
                    return;
                }
            }

            if (memoryEvents.Count >= MaxMemoryEvents)
            {
                memoryEvents.RemoveAt(memoryEvents.Count - 1);
            }

            memoryEvents.Add(new MemoryEvent
            {
                position = position,
                lastSeenTick = tick,
                pawnId = target.thingIDNumber,
                wasHostile = target.HostileTo(Faction.OfPlayer)
            });
        }

        public float GetScentStrengthAt(IntVec3 position, float radius)
        {
            if (!position.InBounds(map))
            {
                return 0f;
            }

            float total = 0f;
            float radiusSq = radius * radius;
            int tick = Find.TickManager.TicksGame;

            for (int i = 0; i < scentMarkers.Count; i++)
            {
                ScentMarker marker = scentMarkers[i];
                float age = (tick - marker.createdTick) / (float)ScentLifetimeTicks;
                if (age >= 1f)
                {
                    continue;
                }

                float distSq = marker.position.DistanceToSquared(position);
                if (distSq > radiusSq)
                {
                    continue;
                }

                float distanceFactor = 1f - Mathf.Sqrt(distSq) / radius;
                float ageFactor = 1f - age;
                total += marker.strength * distanceFactor * ageFactor;
            }

            return Mathf.Clamp01(total);
        }

        public bool TryGetScentDirection(IntVec3 position, float radius, out IntVec3 direction)
        {
            direction = IntVec3.Invalid;
            if (!position.InBounds(map))
            {
                return false;
            }

            float strongest = 0f;
            float radiusSq = radius * radius;
            int tick = Find.TickManager.TicksGame;
            Vector3 weightedDir = Vector3.zero;
            bool found = false;

            for (int i = 0; i < scentMarkers.Count; i++)
            {
                ScentMarker marker = scentMarkers[i];
                float age = (tick - marker.createdTick) / (float)ScentLifetimeTicks;
                if (age >= 1f)
                {
                    continue;
                }

                float distSq = marker.position.DistanceToSquared(position);
                if (distSq > radiusSq || distSq < 1f)
                {
                    continue;
                }

                float distanceFactor = 1f - Mathf.Sqrt(distSq) / radius;
                float ageFactor = 1f - age;
                float scent = marker.strength * distanceFactor * ageFactor;
                if (scent > strongest)
                {
                    strongest = scent;
                }

                Vector3 toMarker = (marker.position - position).ToVector3Shifted();
                weightedDir += toMarker.normalized * scent;
                found = true;
            }

            if (!found || strongest < MinScentStrength)
            {
                return false;
            }

            weightedDir.y = 0f;
            if (weightedDir.sqrMagnitude < 0.01f)
            {
                return false;
            }

            Vector3 normalized = weightedDir.normalized;
            direction = (position + new IntVec3(
                Mathf.RoundToInt(normalized.x),
                0,
                Mathf.RoundToInt(normalized.z)));
            return direction.InBounds(map);
        }

        public bool TryGetStrongestScentSource(IntVec3 position, float radius, out IntVec3 source)
        {
            source = IntVec3.Invalid;
            if (!position.InBounds(map))
            {
                return false;
            }

            float strongest = 0f;
            float radiusSq = radius * radius;
            int tick = Find.TickManager.TicksGame;

            for (int i = 0; i < scentMarkers.Count; i++)
            {
                ScentMarker marker = scentMarkers[i];
                float age = (tick - marker.createdTick) / (float)ScentLifetimeTicks;
                if (age >= 1f)
                {
                    continue;
                }

                float distSq = marker.position.DistanceToSquared(position);
                if (distSq > radiusSq)
                {
                    continue;
                }

                float distanceFactor = 1f - Mathf.Sqrt(distSq) / radius;
                float ageFactor = 1f - age;
                float scent = marker.strength * distanceFactor * ageFactor;
                if (scent > strongest)
                {
                    strongest = scent;
                    source = marker.position;
                }
            }

            return strongest > MinScentStrength;
        }

        public float GetNoiseAt(IntVec3 position, float radius)
        {
            if (!position.InBounds(map))
            {
                return 0f;
            }

            float total = 0f;
            float radiusSq = radius * radius;
            int tick = Find.TickManager.TicksGame;

            for (int i = 0; i < noiseEvents.Count; i++)
            {
                NoiseEvent noise = noiseEvents[i];
                float age = noise.decayTicks > 0
                    ? (tick - noise.createdTick) / (float)noise.decayTicks
                    : 1f;
                if (age >= 1f)
                {
                    continue;
                }

                float distSq = noise.position.DistanceToSquared(position);
                if (distSq > radiusSq)
                {
                    continue;
                }

                float distanceFactor = 1f - Mathf.Sqrt(distSq) / radius;
                float ageFactor = 1f - age;
                total += noise.strength * distanceFactor * ageFactor;
            }

            return Mathf.Clamp01(total);
        }

        public bool TryGetLoudestNoiseDirection(IntVec3 position, float radius, out IntVec3 direction)
        {
            direction = IntVec3.Invalid;
            if (!position.InBounds(map))
            {
                return false;
            }

            float strongest = 0f;
            IntVec3 bestPos = IntVec3.Invalid;
            float radiusSq = radius * radius;
            int tick = Find.TickManager.TicksGame;

            for (int i = 0; i < noiseEvents.Count; i++)
            {
                NoiseEvent noise = noiseEvents[i];
                float age = noise.decayTicks > 0
                    ? (tick - noise.createdTick) / (float)noise.decayTicks
                    : 1f;
                if (age >= 1f)
                {
                    continue;
                }

                float distSq = noise.position.DistanceToSquared(position);
                if (distSq > radiusSq)
                {
                    continue;
                }

                float distanceFactor = 1f - Mathf.Sqrt(distSq) / radius;
                float ageFactor = 1f - age;
                float volume = noise.strength * distanceFactor * ageFactor;
                if (volume > strongest)
                {
                    strongest = volume;
                    bestPos = noise.position;
                }
            }

            if (bestPos.IsValid && strongest > 0f)
            {
                direction = bestPos;
                return true;
            }

            return false;
        }

        public MemoryEvent? GetMostRecentMemory(IntVec3 position, float radius)
        {
            MemoryEvent? best = null;
            float radiusSq = radius * radius;
            int mostRecent = 0;

            for (int i = 0; i < memoryEvents.Count; i++)
            {
                MemoryEvent mem = memoryEvents[i];
                float distSq = mem.position.DistanceToSquared(position);
                if (distSq > radiusSq)
                {
                    continue;
                }

                if (mem.lastSeenTick > mostRecent)
                {
                    mostRecent = mem.lastSeenTick;
                    best = mem;
                }
            }

            return best;
        }

        public bool HasRecentMemoryAt(IntVec3 position, float radius, int maxAgeTicks)
        {
            int tick = Find.TickManager.TicksGame;
            float radiusSq = radius * radius;

            for (int i = 0; i < memoryEvents.Count; i++)
            {
                MemoryEvent mem = memoryEvents[i];
                if (tick - mem.lastSeenTick > maxAgeTicks)
                {
                    continue;
                }

                float distSq = mem.position.DistanceToSquared(position);
                if (distSq <= radiusSq)
                {
                    return true;
                }
            }

            return false;
        }

        public override void MapComponentTick()
        {
            int tick = Find.TickManager.TicksGame;

            if (tick >= nextDecayTick)
            {
                nextDecayTick = tick + DecayIntervalTicks;
                DecayMarkers();
            }

            if (tick >= nextScentSpreadTick)
            {
                nextScentSpreadTick = tick + ScentSpreadIntervalTicks;
                SpreadScent();
            }

            if (tick >= nextGCTick)
            {
                nextGCTick = tick + 6000;
                GarbageCollect();
            }
        }

        private void DecayMarkers()
        {
            int tick = Find.TickManager.TicksGame;

            for (int i = scentMarkers.Count - 1; i >= 0; i--)
            {
                ScentMarker marker = scentMarkers[i];
                float age = (tick - marker.createdTick) / (float)ScentLifetimeTicks;
                marker.strength *= 0.95f;
                if (age >= 1f || marker.strength < MinScentStrength)
                {
                    scentMarkers.RemoveAt(i);
                }
                else
                {
                    scentMarkers[i] = marker;
                }
            }

            for (int i = noiseEvents.Count - 1; i >= 0; i--)
            {
                NoiseEvent noise = noiseEvents[i];
                float age = noise.decayTicks > 0
                    ? (tick - noise.createdTick) / (float)noise.decayTicks
                    : 1f;
                if (age >= 1f)
                {
                    noiseEvents.RemoveAt(i);
                }
            }

            for (int i = memoryEvents.Count - 1; i >= 0; i--)
            {
                MemoryEvent mem = memoryEvents[i];
                if (tick - mem.lastSeenTick > MemoryLifetimeTicks)
                {
                    memoryEvents.RemoveAt(i);
                }
            }
        }

        private void SpreadScent()
        {
            int tick = Find.TickManager.TicksGame;
            List<ScentMarker> newMarkers = new List<ScentMarker>();

            for (int i = 0; i < scentMarkers.Count; i++)
            {
                ScentMarker marker = scentMarkers[i];
                float age = (tick - marker.createdTick) / (float)ScentLifetimeTicks;
                if (age >= 0.8f)
                {
                    continue;
                }

                IntVec3 spreadPos = marker.position + new IntVec3(
                    Rand.RangeInclusive(-1, 1),
                    0,
                    Rand.RangeInclusive(-1, 1));
                if (!spreadPos.InBounds(map))
                {
                    continue;
                }

                float spreadStrength = marker.strength * 0.3f;
                if (spreadStrength > MinScentStrength)
                {
                    newMarkers.Add(new ScentMarker
                    {
                        position = spreadPos,
                        strength = spreadStrength,
                        createdTick = tick,
                        sourcePawnId = marker.sourcePawnId
                    });
                }
            }

            for (int i = 0; i < newMarkers.Count && scentMarkers.Count < MaxScentMarkers; i++)
            {
                scentMarkers.Add(newMarkers[i]);
            }
        }

        private void GarbageCollect()
        {
            if (scentMarkers.Count > MaxScentMarkers * 0.9f)
            {
                scentMarkers.Sort((a, b) => a.createdTick.CompareTo(b.createdTick));
                while (scentMarkers.Count > MaxScentMarkers)
                {
                    scentMarkers.RemoveAt(0);
                }
            }
        }

        private void RemoveOldestScent()
        {
            int oldestTick = int.MaxValue;
            int oldestIndex = 0;
            for (int i = 0; i < scentMarkers.Count; i++)
            {
                if (scentMarkers[i].createdTick < oldestTick)
                {
                    oldestTick = scentMarkers[i].createdTick;
                    oldestIndex = i;
                }
            }
            scentMarkers.RemoveAt(oldestIndex);
        }

        private void RemoveOldestNoise()
        {
            int oldestTick = int.MaxValue;
            int oldestIndex = 0;
            for (int i = 0; i < noiseEvents.Count; i++)
            {
                if (noiseEvents[i].createdTick < oldestTick)
                {
                    oldestTick = noiseEvents[i].createdTick;
                    oldestIndex = i;
                }
            }
            noiseEvents.RemoveAt(oldestIndex);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextDecayTick, "nextDecayTick", 0);
            Scribe_Values.Look(ref nextScentSpreadTick, "nextScentSpreadTick", 0);
            Scribe_Collections.Look(ref scentMarkers, "scentMarkers", LookMode.Deep);
            Scribe_Collections.Look(ref noiseEvents, "noiseEvents", LookMode.Deep);
            Scribe_Collections.Look(ref memoryEvents, "memoryEvents", LookMode.Deep);
        }

        public int ScentCount => scentMarkers.Count;
        public ScentMarker GetScent(int index) => scentMarkers[index];
        public int NoiseCount => noiseEvents.Count;
        public NoiseEvent GetNoise(int index) => noiseEvents[index];
        public int MemoryCount => memoryEvents.Count;
        public MemoryEvent GetMemory(int index) => memoryEvents[index];

        public static MarkedMenMemoryGrid GetForMap(Map map)
        {
            if (map == null)
            {
                return null;
            }

            MarkedMenMemoryGrid comp = map.GetComponent<MarkedMenMemoryGrid>();
            if (comp == null)
            {
                comp = new MarkedMenMemoryGrid(map);
                map.components.Add(comp);
            }
            return comp;
        }
    }
}
