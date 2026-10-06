using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static Verse.PathFinderJob;

namespace QuestEditor_Library
{
    public static class ZoneCondition_WealthEditor
    {
        public static void Draw_0(QuestEditor_Library.ZoneCondition_Wealth cqfReceiver, ref float y, Rect inRect, float x)
        {
            ZoneConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawIntRange(ref y, "WealthRange".Translate(), ref cqfReceiver.wealth, ref cqfReceiver.buffer, ref cqfReceiver.buffer1, x, 150f);
            cqfReceiver.subCondition?.Draw(ref y, inRect, x + 5f);
            if (Widgets.ButtonText(new Rect(x, y, 150f, 25f), "SelectCondition".Translate(), false))
            {
                CQFEditorTools.DrawFloatMenu(typeof(ZoneCondition).AllSubclassesNonAbstract(), a => cqfReceiver.subCondition = ((ZoneCondition)Activator.CreateInstance(a)), a => a.Name.Translate());
            }

            y += 35f;
        }
    }
}
