using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.AI;
using System.Xml.Linq;
using System.Xml;

namespace QuestEditor_Library
{
    public static class CQFThingCategoryCountEditor
    {
        public static void DrawIcon_0(QuestEditor_Library.CQFThingCategoryCount cqfReceiver, ref float y)
        {
            Rect rect = new Rect(20f, y, 35f, 35f);
            Widgets.DrawTextureFitted(new Rect(20f, y, 35f, 35f), cqfReceiver.category.icon, 1f);
            if (Mouse.IsOver(rect))
            {
                Vector3 mouse = Input.mousePosition;
                Widgets.DrawBox(new Rect(mouse.x, mouse.y, 70f, 40f));
                Widgets.Label(rect, cqfReceiver.category.label);
            }
        }
    }
}
