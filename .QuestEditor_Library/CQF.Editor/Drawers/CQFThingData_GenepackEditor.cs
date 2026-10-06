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
    public static class CQFThingData_GenepackEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFThingData_Genepack cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawDefList(cqfReceiver.genes, "Genes".Translate(), ref y, x + 5f);
        }

        public static void DrawIcon_1(QuestEditor_Library.CQFThingData_Genepack cqfReceiver, ref float y)
        {
        }
    }
}
