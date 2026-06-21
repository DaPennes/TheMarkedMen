using System;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public static class SpawnValidationService
    {
        public static bool CanSpawnUrbanOutbreak(Map map)
        {
            if (map == null)
            {
                return false;
            }

            if (!MapClassificationService.IsUrbanRuinMap(map))
            {
                LogVerbose("[TheMarkedMen] SpawnValidation: UrbanOutbreak denied — map is not an Urban Ruin map.");
                return false;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.urbanOutbreaksEnabled)
            {
                return false;
            }

            return true;
        }

        public static bool CanSpawnUrbanAmbush(Map map, IntVec3? position = null)
        {
            if (map == null)
            {
                return false;
            }

            if (!MapClassificationService.IsUrbanRuinMap(map))
            {
                LogVerbose("[TheMarkedMen] SpawnValidation: UrbanAmbush denied — map is not an Urban Ruin map.");
                return false;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.urbanOutbreaksEnabled || !settings.urbanAmbushesEnabled)
            {
                return false;
            }

            if (position.HasValue)
            {
                IntVec3 cell = position.Value;

                if (!cell.IsValid)
                {
                    return false;
                }

                if (cell.Fogged(map))
                {
                    return false;
                }

                if (IsInsidePlayerBuilding(map, cell))
                {
                    LogVerbose("[TheMarkedMen] SpawnValidation: UrbanAmbush denied — position is inside a player building.");
                    return false;
                }

                if (IsInsideHomeArea(map, cell))
                {
                    LogVerbose("[TheMarkedMen] SpawnValidation: UrbanAmbush denied — position is inside Home Area.");
                    return false;
                }
            }

            return true;
        }

        public static bool CanSpawnDormantInfestation(Map map, IntVec3? position = null)
        {
            if (map == null)
            {
                return false;
            }

            if (!MapClassificationService.IsUrbanRuinMap(map))
            {
                LogVerbose("[TheMarkedMen] SpawnValidation: DormantInfestation denied — map is not an Urban Ruin map.");
                return false;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.urbanOutbreaksEnabled || !settings.dormantInfestationsEnabled)
            {
                return false;
            }

            if (position.HasValue)
            {
                IntVec3 cell = position.Value;

                if (!cell.IsValid)
                {
                    return false;
                }

                if (cell.Fogged(map))
                {
                    return false;
                }

                if (IsInsidePlayerBuilding(map, cell))
                {
                    LogVerbose("[TheMarkedMen] SpawnValidation: DormantInfestation denied — position is inside a player building.");
                    return false;
                }

                if (IsInsideHomeArea(map, cell))
                {
                    LogVerbose("[TheMarkedMen] SpawnValidation: DormantInfestation denied — position is inside Home Area.");
                    return false;
                }

                if (IsInsideIdeologyRoom(map, cell))
                {
                    LogVerbose("[TheMarkedMen] SpawnValidation: DormantInfestation denied — position is inside an ideology room.");
                    return false;
                }
            }

            return true;
        }

        public static bool CanSpawnHiddenInfected(Map map, IntVec3? position = null)
        {
            if (map == null)
            {
                return false;
            }

            if (!MapClassificationService.IsUrbanRuinMap(map))
            {
                LogVerbose("[TheMarkedMen] SpawnValidation: HiddenInfected denied — map is not an Urban Ruin map.");
                return false;
            }

            if (position.HasValue)
            {
                IntVec3 cell = position.Value;

                if (!cell.IsValid)
                {
                    return false;
                }

                if (cell.Fogged(map))
                {
                    return false;
                }

                if (IsInsidePlayerBuilding(map, cell))
                {
                    LogVerbose("[TheMarkedMen] SpawnValidation: HiddenInfected denied — position is inside a player building.");
                    return false;
                }

                if (IsInsideIdeologyRoom(map, cell))
                {
                    LogVerbose("[TheMarkedMen] SpawnValidation: HiddenInfected denied — position is inside an ideology room.");
                    return false;
                }
            }

            return true;
        }

        public static bool CanSpawnRuinEncounter(Map map, IntVec3? position = null)
        {
            if (map == null)
            {
                return false;
            }

            if (!MapClassificationService.IsUrbanRuinMap(map))
            {
                return false;
            }

            TheMarkedMenSettings settings = TheMarkedMenMod.Settings;
            if (settings == null || !settings.urbanOutbreaksEnabled || !settings.survivorEncountersEnabled)
            {
                return false;
            }

            if (position.HasValue)
            {
                IntVec3 cell = position.Value;

                if (!cell.IsValid)
                {
                    return false;
                }

                if (cell.Fogged(map))
                {
                    return false;
                }

                if (IsInsidePlayerBuilding(map, cell))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsInsidePlayerBuilding(Map map, IntVec3 cell)
        {
            Building building = cell.GetEdifice(map);
            if (building == null)
            {
                return false;
            }

            if (building.Faction == Faction.OfPlayer)
            {
                return true;
            }

            return false;
        }

        private static bool IsInsideHomeArea(Map map, IntVec3 cell)
        {
            if (map.areaManager?.Home == null)
            {
                return false;
            }

            return map.areaManager.Home[cell];
        }

        private static bool IsInsideIdeologyRoom(Map map, IntVec3 cell)
        {
            Room room = cell.GetRoom(map);
            if (room == null)
            {
                return false;
            }

            RoomRoleDef role = room.Role;
            if (role == null)
            {
                return false;
            }

            string roleDefName = role.defName;

            if (roleDefName.IndexOf("Temple", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Altar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Ritual", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Ideo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Sanct", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Church", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Throne", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Prison", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Bedroom", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Hospital", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Kitchen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Dining", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("RecRoom", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("School", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Barn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roleDefName.IndexOf("Tomb", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        private static void LogVerbose(string message)
        {
            if (TheMarkedMenMod.Settings?.verboseCompatibilityLogging == true)
            {
                Log.Message(message);
            }
        }
    }
}
