using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class DialogManagerDefEditor
    {
        public static void Draw_0(QuestEditor_Library.DialogManagerDef cqfReceiver, ref float y)
        {
            float last = 80f;
            Dictionary<int, DialogTreeAndConditions> replaces = new Dictionary<int, DialogTreeAndConditions>();
            foreach (DialogTreeAndConditions tree in cqfReceiver.trees)
            {
                int index = cqfReceiver.trees.IndexOf(tree);
                Rect rect = new Rect(10f, y, 350f, 40f);
                Widgets.DrawBox(new Rect(5f, y - 5f, 350f, cqfReceiver.heights.ContainsKey(index) ? cqfReceiver.heights[index] - 40f : 0f), 1, QuestEditor_Dialog.blueTex);
                y += 10f;
                rect.height = 30f;
                if (Widgets.ButtonText(rect, "DialogTree".Translate(tree.tree?.defName), false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<DialogTreeDef>.AllDefsListForReading, (x) =>
                    {
                        replaces.Add(index, new DialogTreeAndConditions(x, tree.conditions));
                    }, (x) => x.defName);
                }

                y += 30;
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(10f, y, 100f, 30f), "Conditions".Translate().Colorize(ColorLibrary.SkyBlue));
                Text.Font = GameFont.Small;
                y += 40f;
                foreach (DialogCondition condition in tree.conditions)
                {
                    condition.Draw(ref y, rect, 10f);
                }

                y += 5f;
                Rect button = new Rect(10f, y, 100f, 30f);
                if (Widgets.ButtonText(button, "Add".Translate()))
                {
                    CQFEditorTools.DrawFloatMenu<Type>(typeof(DialogCondition).AllSubclassesNonAbstract(), (x) =>
                    {
                        DialogCondition c = (DialogCondition)Activator.CreateInstance(x);
                        tree.conditions.Add(c);
                    }, x => x.Name.Translate());
                }

                button.x += 110f;
                if (Widgets.ButtonText(button, "Delete".Translate()) && tree.conditions.Any())
                {
                    CQFEditorTools.DrawFloatMenu<DialogCondition>(tree.conditions, (x) =>
                    {
                        tree.conditions.Remove(x);
                    }, x => x.GetType().Name.Translate());
                }

                y += 90f;
                cqfReceiver.heights.SetOrAdd(index, y - last);
                last = y;
            }

            foreach (KeyValuePair<int, DialogTreeAndConditions> replace in replaces)
            {
                cqfReceiver.trees[replace.Key] = replace.Value;
            }
        }
    }
}
