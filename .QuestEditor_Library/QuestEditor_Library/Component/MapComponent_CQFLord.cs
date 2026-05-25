using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class MapComponent_CQFLord : MapComponent
    {
        public MapComponent_CQFLord(Map map) : base(map)
        {
        }
        public MetaLordManager Manager
        {
            get
            {
                return GetManager();
            }
        }

        private MetaLordManager GetManager()
        {
            MetaLordManager result = null;
            if (this.map.PocketMapParent is PocketMapParent parent
                && parent.sourceMap is Map map && map.GetComponent<MapComponent_CQFLord>()
                is MapComponent_CQFLord compSource)
            {
                result = compSource.GetManager();
            }
            else
            {
                if (this.manager == null)
                {
                    this.manager = new MetaLordManager();
                }
                result = this.manager;
            }

            return result;
        }
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (this.manager != null)
            {
                this.manager.Tick();
            }
        }
        public override void MapGenerated()
        {
            base.MapGenerated();
            if (!this.map.IsPocketMap) 
            {
                this.manager = new MetaLordManager();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref this.manager, "manager");
        }

        public MetaLordManager manager;
    }
}
