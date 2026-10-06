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
    public static class PawnSpawnData_RandomEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnSpawnData_Random cqfReceiver, ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(16f + x, y + 10f, 500f, 45f);
            cqfReceiver.DrawName(ref y, x, rect);
            CQFEditorTools.DrawPawnDataList_UseWindow_UseIcon(ref y, 16f + x, cqfReceiver.datas, inRect, "PawnSpawnDatas".Translate(), d => d.dataName);
            cqfReceiver.DrawCanSaveWarning(ref y, x, inRect);
        }
    }
}
