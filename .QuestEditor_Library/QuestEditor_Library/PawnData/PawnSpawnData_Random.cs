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
            Rect rect = new Rect(16f + x, y + 10f, 500f, 45f);
            this.DrawName(ref y, x, rect);
            CQFEditorTools.DrawPawnDataList_UseWindow_UseIcon(ref y, 16f + x, this.datas, inRect, "PawnSpawnDatas".Translate(), d => d.dataName);
            this.DrawCanSaveWarning(ref y, x, inRect);
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
            result.Add(CQFEditorTools.SaveList_Saveable<PawnSpawnData>(this.datas, "datas"));
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


