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
    public static class CQFAction_SpawnAndAddToInventoryEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_SpawnAndAddToInventory cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rectData = new Rect(x + 5f, y, 600f, 25f);
            float initY = y;
            y += 5f;
            foreach (LootData data in cqfReceiver.datas)
            {
                if (Widgets.ButtonText(rectData, data.dataName, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable((IDrawable)data));
                }

                y += 30f;
                rectData.y += 30f;
            }

            y -= 5f;
            Widgets.DrawBox(new Rect(x, initY, inRect.width - 40f - (2 * x), y - initY), 1, QuestEditor_Dialog.blueTex);
            y += 7f;
            if (Widgets.ButtonText(new Rect(x + 10f, y, 150f, 25f), "AddNewLootData".Translate()))
            {
                cqfReceiver.datas.Add(new LootData());
            }

            if (Widgets.ButtonText(new Rect(x + 174f, y, 150f, 25f), "DeleteLootData".Translate()) && cqfReceiver.datas.Any())
            {
                CQFEditorTools.DrawFloatMenu(cqfReceiver.datas, (d) => cqfReceiver.datas.Remove(d), (d) => d.dataName);
            }

            y += 30f;
        }
    }
}
