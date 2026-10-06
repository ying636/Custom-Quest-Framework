using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class MainPawnSpawnDataEditor
    {
        public static void Draw_0(QuestEditor_Library.MainPawnSpawnData cqfReceiver, ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(16f + x, y + 10f, 500f, 45f);
            cqfReceiver.DrawName(ref y, x, rect);
            cqfReceiver.DrawMainPawnOptions(ref y, inRect, x);
        }

        public static void DrawMainPawnOptions_1(QuestEditor_Library.MainPawnSpawnData cqfReceiver, ref float y, Rect inRect, float x)
        {
            Widgets.CheckboxLabeled(new Rect(20f + x, y, 300f, 25f), "MainPawnAllowGetFromQuestDatabase".Translate(), ref cqfReceiver.allowGetFromQuestDatabase);
            y += 30f;
            if (cqfReceiver.allowGetFromQuestDatabase)
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "MainPawnQuestDatabaseKey".Translate(), ref cqfReceiver.questDatabaseKey, x + 20f, 160f);
                y += 30f;
            }

            Widgets.CheckboxLabeled(new Rect(20f + x, y, 300f, 25f), "MainPawnAllowGetFromGlobalDatabase".Translate(), ref cqfReceiver.allowGetFromGlobalDatabase);
            y += 30f;
            if (cqfReceiver.allowGetFromGlobalDatabase)
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "MainPawnGlobalDatabaseKey".Translate(), ref cqfReceiver.globalDatabaseKey, x + 20f, 160f);
                y += 30f;
            }

            Widgets.CheckboxLabeled(new Rect(20f + x, y, 300f, 25f), "MainPawnRegenerateIfDead".Translate(), ref cqfReceiver.regenerateIfDead);
            y += 30f;
            cqfReceiver.DrawSpawnDataSelector(ref y, x, inRect);
            cqfReceiver.DrawCanSaveWarning(ref y, x, inRect);
            cqfReceiver.DrawConditionList(ref y, x + 20f, inRect, cqfReceiver.generateConditions, "MainPawnGenerateConditions".Translate());
            cqfReceiver.DrawConditionList(ref y, x + 20f, inRect, cqfReceiver.regenerateConditions, "MainPawnRegenerateConditions".Translate());
        }

        public static void DrawSpawnDataSelector_2(QuestEditor_Library.MainPawnSpawnData cqfReceiver, ref float y, float x, Rect inRect)
        {
            if (cqfReceiver.spawnData == null)
            {
                cqfReceiver.spawnData = new PawnSpawnData();
            }

            Rect titleRect = new Rect(20f + x, y, 350f, 25f);
            Widgets.Label(titleRect, "MainPawnSpawnDataSpawnData".Translate().Colorize(ColorLibrary.PaleBlue));
            y += 30f;
            string label = cqfReceiver.spawnData.GetType().Name.Translate() + ": " + cqfReceiver.spawnData.dataName;
            Rect row = new Rect(20f + x, y, Mathf.Max(360f, inRect.width - x - 60f), 25f);
            if (Widgets.ButtonText(row, label, false))
            {
                Find.WindowStack.Add(new Dialog_EditIDrawable(cqfReceiver.spawnData));
            }

            TooltipHandler.TipRegion(row, label);
            y += 30f;
            if (Widgets.ButtonText(new Rect(20f + x, y, 220f, 25f), "MainPawnChangeSubPawnData".Translate(), false))
            {
                List<Type> types = new List<Type>();
                types.Add(typeof(PawnSpawnData));
                types.AddRange(typeof(PawnSpawnData).AllSubclassesNonAbstract().Where(type => type != typeof(MainPawnSpawnData)));
                CQFEditorTools.DrawFloatMenu(types, type => cqfReceiver.spawnData = (PawnSpawnData)Activator.CreateInstance(type), type => type.Name.Translate());
            }

            y += 30f;
        }

        public static void DrawConditionList_3(QuestEditor_Library.MainPawnSpawnData cqfReceiver, ref float y, float x, Rect inRect, List<DialogCondition> conditions, string title)
        {
            Widgets.Label(new Rect(x, y, 255f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            CQFEditorTools.DrawButtonForList_UseIcon(y, conditions, condition => condition.GetType().Name.Translate(), () =>
            {
                List<Type> types = new List<Type>();
                types.AddRange(typeof(DialogCondition).AllSubclassesNonAbstract());
                CQFEditorTools.DrawFloatMenu(types, type => conditions.Add((DialogCondition)Activator.CreateInstance(type)), type => type.Name.Translate());
            }, inRect.width - 95f, 25f, 35f);
            y += 30f;
            foreach (DialogCondition condition in conditions)
            {
                string label = condition.GetType().Name.Translate();
                Rect row = new Rect(x, y, Mathf.Max(300f, inRect.width - x - 115f), 25f);
                if (Widgets.ButtonText(row, label, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(condition));
                }

                y += 30f;
            }
        }
    }
}
