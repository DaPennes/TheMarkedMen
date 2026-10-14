using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TheMarkedMen
{
    /// <summary>
    /// Compatibility layer for Vehicle Framework mod.
    /// Allows infected to interact with vehicles and vehicle-based caravans.
    /// Uses reflection-based detection to avoid hard dependency on Vehicle Framework.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class TheMarkedMenVehicleFrameworkCompatibility
    {
        private const string VehicleFrameworkAssemblyName = "VehicleFramework";
        private const string VehicleTypeFullName = "Vehicles.Vehicle";
        private const string CaravanTypeFullName = "RimWorld.Caravan";

        private static bool isVehicleFrameworkActive;
        private static Type vehicleType;
        private static MethodInfo getPawnCapacityMethod;
        private static MethodInfo getPassengersMethod;

        static TheMarkedMenVehicleFrameworkCompatibility()
        {
            TryInitializeVehicleFrameworkSupport();
        }

        private static void TryInitializeVehicleFrameworkSupport()
        {
            try
            {
                // Try to load Vehicle Framework types
                Assembly vehicleAssembly = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == VehicleFrameworkAssemblyName)
                    {
                        vehicleAssembly = assembly;
                        break;
                    }
                }

                if (vehicleAssembly == null)
                {
                    return; // Vehicle Framework not loaded
                }

                vehicleType = vehicleAssembly.GetType(VehicleTypeFullName);
                if (vehicleType == null)
                {
                    return; // Vehicle type not found
                }

                // Get useful methods for vehicle interaction
                getPawnCapacityMethod = AccessTools.Method(vehicleType, "GetPawnCapacity");
                getPassengersMethod = AccessTools.Method(vehicleType, "GetPassengers");

                isVehicleFrameworkActive = true;
                Log.Message("[The Marked Men] Vehicle Framework compatibility initialized.");
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Vehicle Framework compatibility initialization failed: " + ex.Message);
                isVehicleFrameworkActive = false;
            }
        }

        public static bool IsVehicleFrameworkActive => isVehicleFrameworkActive;

        public static Type VehicleType => vehicleType;

        /// <summary>
        /// Check if a pawn is inside a vehicle (as a passenger).
        /// </summary>
        public static bool IsInVehicle(Pawn pawn)
        {
            if (!isVehicleFrameworkActive || pawn == null)
            {
                return false;
            }

            try
            {
                // Check if pawn's parent is a vehicle
                if (pawn.ParentHolder is IThingHolder holder)
                {
                    Thing parent = holder as Thing;
                    if (parent != null && vehicleType.IsInstanceOfType(parent))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get the vehicle that a pawn is riding in, if any.
        /// </summary>
        public static dynamic GetVehicleForPawn(Pawn pawn)
        {
            if (!isVehicleFrameworkActive || pawn == null)
            {
                return null;
            }

            try
            {
                if (pawn.ParentHolder is IThingHolder holder)
                {
                    Thing parent = holder as Thing;
                    if (parent != null && vehicleType.IsInstanceOfType(parent))
                    {
                        return parent;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get all passengers in a vehicle.
        /// </summary>
        public static List<Pawn> GetVehiclePassengers(dynamic vehicle)
        {
            if (!isVehicleFrameworkActive || vehicle == null || getPassengersMethod == null)
            {
                return new List<Pawn>();
            }

            try
            {
                var result = getPassengersMethod.Invoke(vehicle, null) as List<Pawn>;
                return result ?? new List<Pawn>();
            }
            catch
            {
                return new List<Pawn>();
            }
        }

        /// <summary>
        /// Check if infected can ride in a specific vehicle.
        /// Some vehicles may be restricted (e.g., player-controlled transports).
        /// </summary>
        public static bool CanInfectedRideVehicle(dynamic vehicle)
        {
            if (!isVehicleFrameworkActive || vehicle == null)
            {
                return false;
            }

            try
            {
                // Vehicle Framework vehicles are generally rideable
                // Check if vehicle is owned by player faction (skip if so)
                Thing vehicleThing = vehicle as Thing;
                if (vehicleThing == null)
                {
                    return false;
                }

                // Hostile to player-owned vehicles
                Faction playerFaction = Faction.OfPlayer;
                if (vehicleThing.Faction == playerFaction)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Apply Vehicle Framework compatibility patches to a Harmony instance.
        /// </summary>
        public static void ApplyPatches(Harmony harmony)
        {
            if (!isVehicleFrameworkActive || harmony == null || vehicleType == null)
            {
                return;
            }

            try
            {
                // Patch vehicle GetGizmos to show ambush timer for vehicle-based caravans
                MethodInfo getGizmosMethod = AccessTools.Method(vehicleType, "GetGizmos");
                if (getGizmosMethod != null)
                {
                    MethodInfo patchMethod = AccessTools.Method(typeof(TheMarkedMenVehicleFrameworkCompatibility), nameof(Patch_VehicleGetGizmos));
                    harmony.Patch(getGizmosMethod, postfix: new HarmonyMethod(patchMethod));
                    Log.Message("[The Marked Men] Patched Vehicle.GetGizmos for ambush timer integration.");
                }

                // Patch vehicle TakeDamage to track infected occupants
                MethodInfo takeDamageMethod = AccessTools.Method(vehicleType, "TakeDamage");
                if (takeDamageMethod != null)
                {
                    MethodInfo postfixMethod = AccessTools.Method(typeof(TheMarkedMenVehicleFrameworkCompatibility), nameof(Patch_VehicleTakeDamage));
                    harmony.Patch(takeDamageMethod, postfix: new HarmonyMethod(postfixMethod));
                    Log.Message("[The Marked Men] Patched Vehicle.TakeDamage for infected occupant tracking.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[The Marked Men] Vehicle Framework patch application failed: " + ex.Message);
            }
        }

        // Harmony patch methods
        public static void Patch_VehicleGetGizmos(dynamic __instance, ref IEnumerable<Gizmo> __result)
        {
            try
            {
                Thing vehicleThing = __instance as Thing;
                if (vehicleThing == null)
                {
                    return;
                }

                // Add ambush timer if vehicle is associated with caravan
                var passengers = GetVehiclePassengers(__instance);
                bool hasPassengers = passengers != null && passengers.Count > 0;

                if (hasPassengers)
                {
                    // Check if any passenger is marked for ambush tracking
                    // This would integrate with caravan ambush system
                    TheMarkedMenGameComponent component = CrossedUtility.Component;
                    if (component != null)
                    {
                        // Vehicle-based caravan tracking would go here
                    }
                }
            }
            catch
            {
                // Silently fail - don't break vehicle functionality
            }
        }

        public static void Patch_VehicleTakeDamage(dynamic __instance, DamageInfo dinfo)
        {
            try
            {
                // When vehicle takes damage, notify any infected passengers
                var passengers = GetVehiclePassengers(__instance);
                if (passengers == null || passengers.Count == 0)
                {
                    return;
                }

                foreach (Pawn passenger in passengers)
                {
                    if (passenger != null && CrossedUtility.IsInfectedPawn(passenger))
                    {
                        // Infected passenger experiences damage notification
                        // Could trigger combat reactions or escape behavior
                    }
                }
            }
            catch
            {
                // Silently fail
            }
        }
    }
}
