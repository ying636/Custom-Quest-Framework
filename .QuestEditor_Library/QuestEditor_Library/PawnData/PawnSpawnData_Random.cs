using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class PawnSpawnData_Random : PawnSpawnData
    {
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnSpawnData_Random.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override bool CanSaveToMap()
        {
            return !this.datas.NullOrEmpty() && this.datas.Any(data => data != null && data.CanSaveToMap());
        }
        public override Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null, bool setLord = true)
        {
            if (!this.datas.Any())
            {
                Log.Error("Custom Quset Framework Error:Pawn data list of PawnSpawnData_Random is empty");
                return null;
            }
            return this.datas.RandomElement().Spawn(position, map, questTag, quest, lord,setLord);
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFSerialization.SaveList_Saveable<PawnSpawnData>(this.datas, "datas"));
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.datas, "PawnSpawnData_Random_datas", LookMode.Deep);
        }

        public List<PawnSpawnData> datas = new List<PawnSpawnData>();
    }
}
