using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CustomMapEntrance_Chance : CustomMapEntrance
    {
        public override void SetMapDef(CustomMapDataDef mapDef)
        {
            Dictionary<CustomMapDataDef, float> mapPool = new Dictionary<CustomMapDataDef, float>();
            this.mapDefWithChance.ForEach(m => mapPool.Add(m.def, m.chance));
            List<CustomMapDataDef> defs = DefDatabase<CustomMapDataDef>.AllDefsListForReading;
            foreach (TagWithChance tag in this.tagWithChance)
            {
                defs.FindAll(d => d.tags.Contains(tag.tag)).ForEach(d =>
                {
                    if (!mapPool.ContainsKey(d)) 
                    {
                        mapPool.Add(d, tag.chance * d.commonality);
                    }
                });
            }
            mapPool.RemoveAll(c => 
            {
                if (this.Map.Parent is MapParent_Custom parent0 && parent0.rootSite is CustomSite site0 && site0.GenerationCount.TryGetValue(c.Key,out int v) && v >= c.Key.generationLimit && c.Key.generationLimit != 0)
                {
                    return true;
                }
                return false;
            });
            if (mapPool.Any()) 
            {
                this.mapDef = GenCollection.RandomElementByWeight(mapPool, x => x.Value).Key;
                //if (DebugSettings.godMode)
                //{
                //    for (int i = 0;i<100;i++) 
                //    {
                //        Log.Message(GenCollection.RandomElementByWeight(mapPool, x => x.Value).Key.label);
                //    }
                //}
            }
            if (this.Map.Parent is MapParent_Custom parent && parent.rootSite is CustomSite site) 
            {
                site.GenerationCount.SetOrAdd(this.mapDef,!site.GenerationCount.ContainsKey(this.mapDef) ? 1 : site.GenerationCount[this.mapDef] + 1);
            }
        }
        public override void DrawTab()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapEntrance_Chance.DrawTab()", this, arguments);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.tagWithChance, "CQF_CustomMapEntrance_tagWithChance",LookMode.Deep);
            Scribe_Collections.Look(ref this.mapDefWithChance, "CQF_CustomMapEntrance_mapDefWithChance",LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.tagWithChance ??= new List<TagWithChance>();
                this.mapDefWithChance ??= new List<MapDefWithChance>();
            }
        }
        internal void DrawCopyPasteHeader(ref float y, float x, float width)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapEntrance_Chance.DrawCopyPasteHeader(Ref:float,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawMapPool(ref float y, float x, float width)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapEntrance_Chance.DrawMapPool(Ref:float,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawTagPool(ref float y, float x, float width)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapEntrance_Chance.DrawTagPool(Ref:float,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public List<TagWithChance> tagWithChance = new List<TagWithChance>();
        public List<MapDefWithChance> mapDefWithChance = new List<MapDefWithChance>();
    }

    public class TagWithChance : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.chance, "TagWithChance_chance");
            Scribe_Values.Look(ref this.tag, "TagWithChance_tag");
        }

        public string buffer;
        public float chance = 1;
        public string tag = "defined";
    }
    public class MapDefWithChance : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.chance, "TagWithChance_chance");
            Scribe_Defs.Look(ref this.def, "TagWithChance_def");
        }

        public string buffer;
        public float chance = 1;
        public CustomMapDataDef def = null;
    }
}
