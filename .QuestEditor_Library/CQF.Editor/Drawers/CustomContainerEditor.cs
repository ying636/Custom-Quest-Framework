using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CustomContainerEditor
    {
        public static void DrawTab_0(QuestEditor_Library.CustomContainer cqfReceiver)
        {
            Rect inRect = new Rect(0f, 0f, 500f, 500f);
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref cqfReceiver.pos, new Rect(0f, 0f, inRect.width - 20f, cqfReceiver.height + 10f));
            float x = 10f;
            float y = 15f;
            Widgets.Label(new Rect(x, y, 250f, 25f), "InnerThings".Translate());
            y += 30f;
            Rect rectData = new Rect(x, y + 3f, 600f, 25f);
            foreach (LootData data in cqfReceiver.innerThings)
            {
                if (Widgets.ButtonText(rectData, data.dataName + "  " + data.chance * 100f + "%", false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(data));
                }

                y += 30f;
                rectData.y += 30f;
            }

            y += 10f;
            if (Widgets.ButtonText(new Rect(x, y, 100f, 38f), "AddNewLootData".Translate()))
            {
                cqfReceiver.innerThings.Add(new LootData());
            }

            if (Widgets.ButtonText(new Rect(x + 150f, y, 100f, 38f), "Paste".Translate()) && CQFEditorTools.lootData != null)
            {
                cqfReceiver.innerThings.Add(CQFEditorTools.lootData.Copy());
            }

            if (Widgets.ButtonText(new Rect(x + 300f, y, 100f, 38f), "DeleteLootData".Translate()) && cqfReceiver.innerThings.Any())
            {
                CQFEditorTools.DrawFloatMenu(cqfReceiver.innerThings, (x2) => cqfReceiver.innerThings.Remove(x2), (x2) => x2.dataName);
            }

            y += 45f;
            CQFEditorTools.DrawLabelAndText_Line(y, "TickToOpen".Translate(), ref cqfReceiver.tickToOpen, ref cqfReceiver.buffer, x, 100f);
            y += 30f;
            CQFEditorTools.DrawActionList(ref y, x, cqfReceiver.openingActions, inRect, "OpeningActions".Translate().Colorize(ColorLibrary.SkyBlue), true, "OpeningActionsTip".Translate().ToString());
            Widgets.Label(new Rect(x, y, 150f, 25f), "OpeningConditions".Translate().Colorize(ColorLibrary.PaleBlue));
            CQFEditorTools.DrawButtonWithIcon(y, () => Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(typeof(DialogCondition).AllSubclassesNonAbstract(), c => c.Name.Translate(), c => cqfReceiver.openingConditions.Add((DialogCondition)Activator.CreateInstance(c)), null, null, null, null, null, null), "Select".Translate())), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.openingConditions, c => cqfReceiver.openingConditions.Remove(c), c => c.GetType().Name.Translate()), inRect.width - 150f, 30);
            y += 30f;
            foreach (DialogCondition c in cqfReceiver.openingConditions)
            {
                c.Draw(ref y, inRect, x);
            }

            cqfReceiver.height = y;
            Widgets.EndScrollView();
        }
    }
}
