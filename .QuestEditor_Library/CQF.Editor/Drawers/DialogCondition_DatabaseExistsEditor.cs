using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Xml;
using System.Xml.Linq;
using RimWorld.QuestGen;
using Verse.Grammar;
using System.Reflection;
using UnityEngine;
using System.Collections;
using Verse.AI;
using Verse.AI.Group;
using System.IO;
using Unity.Collections;
using RimWorld.Planet;
using System.Net.NetworkInformation;
using System.Text;

namespace QuestEditor_Library
{
    public static class DialogCondition_DatabaseExistsEditor
    {
        public static void Draw_0(QuestEditor_Library.DialogCondition_DatabaseExists cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            DialogConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFTargetKeyEditor.DrawBookField(y, "TargetKey".Translate(), cqfReceiver.targetKey, value => cqfReceiver.targetKey = value, x, 100f, inRect.width - x - 20f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 200f, 25f), "NeedSpawned".Translate(), ref cqfReceiver.needSpawned);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 200f, 25f), "CheckGlobalDatabase".Translate(), ref cqfReceiver.checkGlobalDatabase);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 200f, 25f), "CheckTemporaryDatabase".Translate(), ref cqfReceiver.checkTemporaryDatabase);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 200f, 25f), "CheckQuestDatabase".Translate(), ref cqfReceiver.checkQuestDatabase);
            y += 30f;
        }
    }
}
