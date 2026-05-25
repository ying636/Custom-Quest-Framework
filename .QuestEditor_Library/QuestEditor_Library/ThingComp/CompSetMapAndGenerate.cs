using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class CompPropertiesSetMapAndGenerate : CompProperties
    {
        public CompPropertiesSetMapAndGenerate() 
        {
            this.compClass = typeof(CompSetMapAndGenerate);
        }

        public string key;
        public CustomMapGenerationSet map;
    }
    public class CompSetMapAndGenerate : ThingComp
    {
        public CompPropertiesSetMapAndGenerate Props => (CompPropertiesSetMapAndGenerate)this.props;
        public override void PostPostMake()
        {
            base.PostPostMake();
            if (this.Props?.map?.GetMap() is CustomMapDataDef def
                && this.parent is CustomMapEntrance entrance) 
            {
                entrance.exitName = this.Props.key;
                entrance.SetMapDef(def);
            }
        }
    }
}
