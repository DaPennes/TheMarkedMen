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
            detonated = true;
            GenExplosion.DoExplosion(
                pawn.Position,
                pawn.Map,
                Props.radius,
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
        }
    }
}
