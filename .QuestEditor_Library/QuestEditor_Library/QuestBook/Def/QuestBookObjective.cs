using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookObjective : IExposable, ISaveable, IDrawable
    {
        [NoTranslate]
        public string labelKey;
        [NoTranslate]
        public string descriptionKey;
        [NoTranslate]
        public string signal;
        public Type workerClass = typeof(QuestBookObjectiveWorker_Signal);
        public ThingDef targetThingDef;
        public ResearchProjectDef targetResearch;
        public ThingDef iconThing;
        [NoTranslate]
        public string iconPath;
        public int targetCount = 1;
        public bool optional;

        public string Label => labelKey.NullOrEmpty() ? "CQF_QuestBook_Objective".Translate().ToString() : labelKey.CanTranslate() ? labelKey.Translate().ToString() : labelKey;

        public string Description => descriptionKey.NullOrEmpty() ? string.Empty : descriptionKey.CanTranslate() ? descriptionKey.Translate().ToString() : descriptionKey;

        public void ExposeData()
        {
            Scribe_Values.Look(ref labelKey, "labelKey");
            Scribe_Values.Look(ref descriptionKey, "descriptionKey");
            Scribe_Values.Look(ref signal, "signal");
            Scribe_Values.Look(ref workerClass, "workerClass");
            Scribe_Defs.Look(ref targetThingDef, "targetThingDef");
            Scribe_Defs.Look(ref targetResearch, "targetResearch");
            Scribe_Defs.Look(ref iconThing, "iconThing");
            Scribe_Values.Look(ref iconPath, "iconPath");
            Scribe_Values.Look(ref targetCount, "targetCount", 1);
            Scribe_Values.Look(ref optional, "optional");
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("labelKey", labelKey));
            if (!descriptionKey.NullOrEmpty())
            {
                result.Add(new XElement("descriptionKey", descriptionKey));
            }
            if (!signal.NullOrEmpty())
            {
                result.Add(new XElement("signal", signal));
            }
            if (workerClass != null && workerClass != typeof(QuestBookObjectiveWorker_Signal))
            {
                result.Add(new XElement("workerClass", workerClass.FullName));
            }
            if (targetThingDef != null)
            {
                result.Add(new XElement("targetThingDef", targetThingDef.defName));
            }
            if (targetResearch != null)
            {
                result.Add(new XElement("targetResearch", targetResearch.defName));
            }
            if (iconThing != null)
            {
                result.Add(new XElement("iconThing", iconThing.defName));
            }
            if (!iconPath.NullOrEmpty())
            {
                result.Add(new XElement("iconPath", iconPath));
            }
            if (workerClass != typeof(QuestBookObjectiveWorker_Research))
            {
                result.Add(new XElement("targetCount", targetCount));
            }
            result.Add(new XElement("optional", optional));
            return result;
        }

        public void Draw(ref float y, UnityEngine.Rect inRect, float x)
        {
            CQFEditorTools.DrawFieldAndText(ref y, "CQF_QuestBook_ObjectiveName".Translate(), ref labelKey, x, 320f);
            CQFEditorTools.DrawFieldAndText(ref y, "CQF_QuestBook_ObjectiveDescription".Translate(), ref descriptionKey, x, 320f);
            CQFEditorTools.DrawFieldAndText(ref y, "CQF_QuestBook_TriggerSignal".Translate(), ref signal, x, 320f);
            DrawWorkerClass(ref y, x);
            if (workerClass != typeof(QuestBookObjectiveWorker_Research))
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "CQF_QuestBook_TargetCount".Translate(), ref targetCount, ref countBuffer, x, 320f);
                y += 30f;
            }
            Verse.Widgets.CheckboxLabeled(new UnityEngine.Rect(x, y, 300f, 25f), "CQF_QuestBook_Optional".Translate(), ref optional);
        }

        private void DrawWorkerClass(ref float y, float x)
        {
            string workerName = workerClass?.Name ?? "CQF_QuestBook_None".Translate();
            if (Widgets.ButtonText(new UnityEngine.Rect(x, y, 420f, 26f), "CQF_QuestBook_ObjectiveChecker".Translate() + ": " + workerName, false))
            {
                List<Type> workers = typeof(QuestBookObjectiveWorker).AllSubclassesNonAbstract();
                Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(workers,
                    type => type.Name.Translate(), type => workerClass = type, null, null, null,
                    type => type.Name, null, null), "CQF_QuestBook_ObjectiveChecker".Translate()));
            }
            y += 31f;
        }

        public QuestBookObjectiveWorker Worker
        {
            get
            {
                if (workerInt == null || workerTypeInt != workerClass)
                {
                    workerInt = CreateWorker();
                    workerTypeInt = workerClass;
                }
                return workerInt;
            }
        }

        private QuestBookObjectiveWorker CreateWorker()
        {
            if (workerClass == null || !typeof(QuestBookObjectiveWorker).IsAssignableFrom(workerClass))
            {
                Log.Error("CQF task book objective worker class is invalid: " + workerClass);
                return null;
            }
            return Activator.CreateInstance(workerClass) as QuestBookObjectiveWorker;
        }

        private string countBuffer;
        private QuestBookObjectiveWorker workerInt;
        private Type workerTypeInt;
    }
}
