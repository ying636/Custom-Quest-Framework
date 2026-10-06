using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;
using System.Text;

namespace QuestEditor_Library
{
    public class QuestNode_RandomCustomMap : QuestNode_Root_CustomMap
    {
        public override CustomMapDataDef GetMap()
        {
            Dictionary<CustomMapDataDef,float> datas = new Dictionary<CustomMapDataDef, float>();
            this.datas.ToList().ForEach(x => datas.Add(DefDatabase<CustomMapDataDef>.GetNamed(x.Key),x.Value));
            if (this.tags != null) 
            {
                DefDatabase<CustomMapDataDef>.AllDefsListForReading.ForEach(d =>
                {
                    d.tags.ForEach(t =>
                    {
                        if (this.tags.TryGetValue(t,out float weight))
                        {
                            datas.SetOrAdd(d,weight);
                        }
                    });
                });
            }
            return GenCollection.RandomElementByWeight(datas.Keys,d => datas[d]);
        }

        public override void Draw(ref float y, Rect inRect,float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestNode_RandomCustomMap.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public Dictionary<string, float> tags = new Dictionary<string, float>();
        public Dictionary<string,float> datas = new Dictionary<string, float>();
    }

}
