using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheMarkedMen
{
    public class HediffComp_DeathExplosion : HediffComp
    {
        private bool detonated;

        public HediffCompProperties_DeathExplosion Props => (HediffCompProperties_DeathExplosion)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (detonated) return;
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Map == null || pawn.Destroyed) return;
            if (!pawn.Dead) return;
            Detonate(pawn);
        }

        public override void CompPostPostRemoved()
        {
            if (detonated) return;
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Destroyed) return;
            if (pawn.Map == null) return;
            if (!pawn.Dead) return;
            Detonate(pawn);
        }

        public void Detonate(Pawn pawn)
        {
            if (detonated) return;
            if (pawn == null || pawn.Map == null)
            {
                return;
            }
            detonated = true;
            float radius = Props.radius;
            GenExplosion.DoExplosion(
                pawn.Position,
                pawn.Map,
                radius,
                Props.damageDef ?? DamageDefOf.Bomb,
                pawn,
                Mathf.RoundToInt(Props.damageAmount),
                Props.armorPenetration,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: pawn,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 1,
                postExplosionGasType: null,
                postExplosionGasRadiusOverride: null,
                postExplosionGasAmount: 0,
                applyDamageToExplosionCellsNeighbors: false,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 1,
                chanceToStartFire: Props.chanceToStartFire,
                damageFalloff: false,
                direction: null,
                ignoredThings: null,
                affectedAngle: null,
                doVisualEffects: true,
                propagationSpeed: 0f,
                excludeRadius: 0f,
                doSoundEffects: true,
                postExplosionSpawnThingDefWater: null,
                screenShakeFactor: 0f,
                flammabilityChanceCurve: null,
                overrideCells: null,
                postExplosionSpawnSingleThingDef: null,
                preExplosionSpawnSingleThingDef: null);
            TryContaminateExplosionRadius(pawn, radius);
        }

        private static void TryContaminateExplosionRadius(Pawn pawn, float radius)
        {
            Map map = pawn.Map;
            if (map == null) return;
            int numCells = GenRadial.NumCellsInRadius(radius);
            float exposureChance = TheMarkedMenMod.Settings?.bloodExposureChance ?? 0.45f;
            for (int cellIndex = 0; cellIndex < numCells; cellIndex++)
            {
                IntVec3 cell = pawn.Position + GenRadial.ManualRadialPattern[cellIndex];
                if (!cell.InBounds(map)) continue;
                List<Thing> things = map.thingGrid.ThingsListAt(cell);
                for (int thingIndex = 0; thingIndex < things.Count; thingIndex++)
                {
                    Pawn target = things[thingIndex] as Pawn;
                    if (target == null || target == pawn || target.Dead || !target.RaceProps.Humanlike)
                    {
                        continue;
                    }
                    if (CrossedUtility.IsInfectedPawn(target) || CrossedUtility.IsFullyProtectedFromCrossVirusExposure(target))
                    {
                        continue;
                    }
                    CrossedUtility.TryExpose(target, exposureChance, "bomber explosion contamination", pawn);
                }
            }
        }
    }
}
