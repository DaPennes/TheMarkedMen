using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheMarkedMen
{
    public enum MapClass
    {
        Unknown,
        PlayerColony,
        Settlement,
        QuestMap,
        TemporaryMap,
        AbandonedMap,
        UrbanRuinMap,
        AncientFacility,
        SpecialScenarioMap
    }

    public static class MapClassificationService
    {
        private static readonly Dictionary<Map, MapClass> cache = new Dictionary<Map, MapClass>();

        public static MapClass GetMapClass(Map map)
        {
            if (map == null)
            {
                return MapClass.Unknown;
            }

            if (cache.TryGetValue(map, out MapClass cached))
            {
                return cached;
            }

            MapClass result = Classify(map);
            cache[map] = result;

            if (TheMarkedMenMod.Settings?.verboseCompatibilityLogging == true)
            {
                Log.Message($"[TheMarkedMen] MapClassificationService: class={result}, " +
                    $"IsPlayerHome={map.IsPlayerHome}, ParentType={map.Parent?.GetType().FullName ?? "null"}");
            }

            return result;
        }

        public static void Invalidate(Map map)
        {
            if (map != null)
            {
                cache.Remove(map);
            }
        }

        public static void InvalidateAll()
        {
            cache.Clear();
        }

        public static void PruneDestroyedMaps()
        {
            List<Map> toRemove = null;
            foreach (KeyValuePair<Map, MapClass> kvp in cache)
            {
                Map map = kvp.Key;
                if (map == null || map.info == null || !Find.Maps.Contains(map))
                {
                    if (toRemove == null)
                    {
                        toRemove = new List<Map>();
                    }
                    toRemove.Add(map);
                }
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    cache.Remove(toRemove[i]);
                }
            }
        }

        public static bool IsUrbanRuinMap(Map map)
        {
            return GetMapClass(map) == MapClass.UrbanRuinMap;
        }

        public static bool IsPlayerColony(Map map)
        {
            MapClass cls = GetMapClass(map);
            return cls == MapClass.PlayerColony || cls == MapClass.Settlement;
        }

        private static MapClass Classify(Map map)
        {
            if (map == null)
            {
                return MapClass.Unknown;
            }

            if (map.IsPlayerHome)
            {
                return MapClass.PlayerColony;
            }

            if (map.ParentFaction == Faction.OfPlayer)
            {
                return MapClass.PlayerColony;
            }

            if (map.Parent is Settlement settlement && settlement.Faction == Faction.OfPlayer)
            {
                return MapClass.PlayerColony;
            }

            if (map.Parent != null)
            {
                string parentTypeName = map.Parent.GetType().FullName;
                if (parentTypeName.StartsWith("AncientMarket_Libraray.", StringComparison.Ordinal))
                {
                    return MapClass.UrbanRuinMap;
                }
            }

            return MapClass.Unknown;
        }
    }
}
