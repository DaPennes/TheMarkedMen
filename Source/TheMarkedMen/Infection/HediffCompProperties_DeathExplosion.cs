using RimWorld;
using Verse;

namespace TheMarkedMen
{
    public class HediffCompProperties_DeathExplosion : HediffCompProperties
    {
        public float radius = 1.9f;
        public DamageDef damageDef;
        public float damageAmount = 15f;
        public float armorPenetration = 15f;
        public ThingDef postExplosionSpawnThingDef;
        public float postExplosionSpawnChance;
        public int postExplosionSpawnThingCount = 1;
        public float chanceToStartFire;
        public float explosionSoundVolume = 1f;

        public HediffCompProperties_DeathExplosion()
        {
            compClass = typeof(HediffComp_DeathExplosion);
        }
    }
}
