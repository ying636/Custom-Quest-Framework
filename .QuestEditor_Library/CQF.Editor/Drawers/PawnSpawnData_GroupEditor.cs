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
    public static class PawnSpawnData_GroupEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnSpawnData_Group cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            Rect rect = new Rect(20f + x, y, 250f, 25f);
            if (CQFUIStyle.ButtonText(rect, "CQF_PawnGroupDef".Translate(cqfReceiver.group?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu<GroupDataDef>(DefDatabase<GroupDataDef>.AllDefsListForReading, (k) => cqfReceiver.group = k, (k) =>
                {
                    return k.label;
                });
            }

            y += 30f;
            cqfReceiver.DrawCanSaveWarning(ref y, x, inRect);
        }
    }
}
