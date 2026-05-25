using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;

namespace QuestEditor_Library
{
    public class StructureFaller : Skyfaller
    {
        protected List<Thing> Spawn()
        {
            if (this.mapData != null) 
            {
                IntVec3 pos = this.Position - (this.mapData.size / 2);
                return this.mapData.Generate(pos,this.Map,null);
            }
            return null;
        }
        protected override void Impact()
        {
            this.hasImpacted = true;
            List<Thing> things = this.Spawn();
            //if (this.def.skyfaller.CausesExplosion)
            //{
            //    IntVec3 position = base.Position;
            //    Map map = base.Map;
            //    float explosionRadius = this.def.skyfaller.explosionRadius;
            //    if (this.mapData != null) 
            //    {
            //        explosionRadius = Math.Max(this.mapData.size.x, this.mapData.size.z) + 2;
            //    }
            //    DamageDef explosionDamage = this.def.skyfaller.explosionDamage;
            //    Thing instigator = null;
            //    int damAmount = GenMath.RoundRandom((float)this.def.skyfaller.explosionDamage.defaultDamage * this.def.skyfaller.explosionDamageFactor);
            //    float armorPenetration = -1f;
            //    SoundDef explosionSound = null;
            //    ThingDef weapon = null;
            //    ThingDef projectile = null;
            //    Thing intendedTarget = null;
            //    ThingDef postExplosionSpawnThingDef = null;
            //    float postExplosionSpawnChance = 0f;
            //    int postExplosionSpawnThingCount = 1;
            //    List<Thing> ignoredThings = things;
            //    GenExplosion.DoExplosion(position, map, explosionRadius, explosionDamage, instigator, damAmount, armorPenetration, explosionSound, weapon, projectile, intendedTarget, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, null, null, 255, false, null, 0f, 1, 0f, false, null, ignoredThings, null, true, 1f, 0f, true, null, 1f, null, null, null, null);
            //}
            this.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
            CellRect cellRect = this.OccupiedRect();
            for (int i = 0; i < cellRect.Area * this.def.skyfaller.motesPerCell; i++)
            {
                FleckMaker.ThrowDustPuff(cellRect.RandomVector3, base.Map, 2f);
            }
            if (this.def.skyfaller.MakesShrapnel)
            {
                SkyfallerShrapnelUtility.MakeShrapnel(base.Position, base.Map, this.shrapnelDirection, this.def.skyfaller.shrapnelDistanceFactor, this.def.skyfaller.metalShrapnelCountRange.RandomInRange, this.def.skyfaller.rubbleShrapnelCountRange.RandomInRange, true);
            }
            if (this.def.skyfaller.cameraShake > 0f && base.Map == Find.CurrentMap)
            {
                Find.CameraDriver.shaker.DoShake(this.def.skyfaller.cameraShake);
            }
            if (this.def.skyfaller.impactSound != null)
            {
                this.def.skyfaller.impactSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(base.Position, base.Map, false), MaintenanceType.None));
            }
            if (this.impactLetter != null)
            {
                Find.LetterStack.ReceiveLetter(this.impactLetter, null, 0, true);
            }
            Map map2 = base.Map;
            if (!this.Destroyed)
            {
                this.Destroy(DestroyMode.Vanish);
            }
            if (this.def.skyfaller.spawnThing != null)
            {
                Thing thing;
                GenSpawn.TrySpawn(this.def.skyfaller.spawnThing, base.Position, map2, out thing, WipeMode.Vanish, true);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.mapData, "mapData");
        }

        public CustomMapDataDef mapData;
    }
}
