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
    [StaticConstructorOnStartup]
    public static class CQFEditorTools
    {
        public static readonly Texture2D TipIcon = ContentFinder<Texture2D>.Get("UI/TipIcon", true);

        public static List<string> TargetTexts => new List<string>()
        { "Interviewee", "Interviewer", "CustomThing", "Trigger", "Captured", "Position","Inner","Target" };
        public static List<ThingDef> MapExitDefs
        {
            get
            {
                if (!CQFEditorTools.customMapExitDefs.Any())
                {
                    DefDatabase<ThingDef>.AllDefsListForReading.ForEach(x =>
                    {
                        if (x.thingClass.IsSubclassOf(typeof(CustomMapExit)) || x.thingClass == typeof(CustomMapExit))
                        {
                            CQFEditorTools.customMapExitDefs.Add(x);
                        }
                    });
                }
                return CQFEditorTools.customMapExitDefs;
            }
        }
        public static void AddOrSetObjectToListFromDictionary<T, D>(Dictionary<T, List<D>> dic, T t, D d)
        {
            if (dic.TryGetValue(t, out List<D> ds))
            {
                ds.Add(d);
            }
            else
            {
                dic.Add(t, new List<D>() { d });
            }
        }
        public static void DrawLabelAndText_SlateRef_Line(float y, string label, ref SlateRef<string> text, float x = 0f, float width = 60f)
        {
            Widgets.Label(new Rect(x, y, 350f, 20f), label);
            string bufferText = Widgets.TextField(new Rect(Text.CalcSize(label).x + x + 5f, y, width, 20f), text.ToString());
            text = text.ToString() == bufferText ? text : new SlateRef<string>(bufferText);
        }
        public static void DrawLabelAndText_SlateRef_Line<T>(float y, string label, ref SlateRef<T> text, float x = 0f, float width = 60f)
        {
            Widgets.Label(new Rect(x, y, 350f, 25f), label);
            string bufferText = Widgets.TextField(new Rect(Text.CalcSize(label).x + x + 5f, y, width, 25f), text.ToString());
            text = text.ToString() == bufferText ? text : new SlateRef<T>(bufferText);
        }
        public static void DrawLabelAndText_SlateRef_Line<T>(float y, string label, ref SlateRef<T>? text, float x = 0f, float width = 60f)
        {
            Widgets.Label(new Rect(x, y, 350f, 25f), label);
            string bufferText = Widgets.TextField(new Rect(Text.CalcSize(label).x + x + 5f, y, width, 25f), text?.ToString());
            text = text?.ToString() == bufferText ? text : new SlateRef<T>(bufferText);
        }
        public static void DrawLabelAndText_Line<T>(float y, string label, ref T text, ref string buffer, float x = 0f, float width = 60f) where T : struct
        {
            Widgets.Label(new Rect(x, y, 350f, 25f), label);
            Widgets.TextFieldNumeric<T>(new Rect(Text.CalcSize(label).x + x + 5f, y, width, 25f), ref text, ref buffer,-9999);
        }
        public static void DrawLabelAndText_Line(float y, string label, ref float text, ref string buffer, float x = 0f, float width = 60f)
        {
            Widgets.Label(new Rect(x, y, 350f, 25f), label);
            Widgets.TextFieldPercent(new Rect(Text.CalcSize(label).x + x + 5f, y, width, 25f), ref text, ref buffer);
        }
        public static void DrawLabelAndText_Line(float y, string label, ref string text, float x = 0f, float width = 60f)
        {
            bool nullText = label.NullOrEmpty();
            if (!nullText)
            {
                Widgets.Label(new Rect(x, y, 350f, 25f), label);
            }
            text = Widgets.TextField(new Rect(nullText ? x + 5f : Text.CalcSize(label).x + x + 5f, y, width, 25f), text);
        }
        public static void DrawSelectableText(float y, string label, ref string text, Action selectAction, float x = 0f, float width = 60f)
        {
            bool nullText = label.NullOrEmpty();
            if (!nullText)
            {
                if (Widgets.ButtonText(new Rect(x, y, Text.CalcSize(label).x, 25f), label, false))
                {
                    selectAction();
                }
            }
            text = Widgets.TextField(new Rect(nullText ? x + 5f : Text.CalcSize(label).x + x + 5f, y, width, 25f), text);
        }
        public static void DrawFactionSelectableText(float y, string label, ref string faction, Action<string> setFaction, float x = 0f, float width = 60f)
        {
            DrawSelectableText(y, label, ref faction, () => DrawFactionFloatMenu(setFaction), x, width);
        }
        public static void DrawFactionFloatMenu(Action<string> setFaction)
        {
            DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll(f => !f.isPlayer),
                f => setFaction(f.defName), f => f.label, new List<FloatMenuOption>()
                {
                    new FloatMenuOption("RandomHostile".Translate(), () => setFaction("RandomHostile")),
                    new FloatMenuOption("RandomAlly".Translate(), () => setFaction("RandomAlly")),
                    new FloatMenuOption("RandomNeutral".Translate(), () => setFaction("RandomNeutral")),
                    new FloatMenuOption("PawnDataMapFaction".Translate(), () => setFaction("MapFaction"))
                });
        }
        public static void OpenDutySelect(Action<DutyDef> acceptAction)
        {
            List<DutyDef> duties = DefDatabase<DutyDef>.AllDefsListForReading;
            Find.WindowStack.Add(new Dialog_Select<DutyDef>(new TextSelectDrawer<DutyDef>(duties, CQFEditorTools.DutyLabel, acceptAction, null, CQFEditorTools.DutyTip, CQFEditorTools.DutyPriority, duty => duty.defName, null, CQFEditorTools.MakeDutyModFilters(duties), null), "Select".Translate()));
        }

        public static string DutyLabel(DutyDef duty)
        {
            if (duty == null)
            {
                return null;
            }
            if (!duty.label.NullOrEmpty())
            {
                return duty.label;
            }
            return duty.defName.CanTranslate() ? duty.defName.Translate().ToString() : duty.defName;
        }
        public static void DrawSelectableText(float y, string label, ref SlateRef<string> text, Action selectAction, float x = 0f, float width = 60f)
        {
            bool nullText = label.NullOrEmpty();
            if (!nullText)
            {
                if (Widgets.ButtonText(new Rect(x, y, Text.CalcSize(label).x, 25f), label, false))
                {
                    selectAction();
                }
            }
            text = Widgets.TextField(new Rect(nullText ? x + 5f : Text.CalcSize(label).x + x + 5f, y, width, 25f), text.ToString());
        }
        public static void DrawSelectableNumber<T>(float y, string label, ref T text, ref string buffer, Action selectAction, float x = 0f, float width = 60f) where T : struct
        {
            bool nullText = label.NullOrEmpty();
            if (!nullText)
            {
                if (Widgets.ButtonText(new Rect(x, y, Text.CalcSize(label).x, 25f), label, false))
                {
                    selectAction();
                }
            }
            Widgets.TextFieldNumeric(new Rect(nullText ? x + 5f : Text.CalcSize(label).x + x + 5f, y, width, 25f), ref text, ref buffer);
        }
        public static void DrawSelectablePercent(float y, string label, ref float text, ref string buffer, Action selectAction, float x = 0f, float width = 60f)
        {
            bool nullText = label.NullOrEmpty();
            if (!nullText)
            {
                if (Widgets.ButtonText(new Rect(x, y, Text.CalcSize(label).x, 25f), label, false))
                {
                    selectAction();
                }
            }
            Widgets.TextFieldPercent(new Rect(nullText ? x + 5f : Text.CalcSize(label).x + x + 5f, y, width, 25f), ref text, ref buffer);
        }
        public static void DrawFieldAndText(ref float y, string label, ref string text, float x = 0f, float width = 350f)
        {
            Widgets.Label(new Rect(x, y, width, 25f), label);
            y += 25f;
            text = Widgets.TextField(new Rect(x, y, width, 25f), text);
        }
        public static void DrawIntRange(ref float y, string label, ref IntRange num, ref string bufferMin, ref string bufferMax, float x = 0f, float width = 30f)
        {
            Widgets.Label(new Rect(x, y, 350f, 25f), label);
            int min = num.min;
            int max = num.max;
            Rect rect = new Rect(Text.CalcSize(label).x + x + 5f, y, width, 25f);
            Widgets.TextFieldNumeric(rect, ref min, ref bufferMin);
            rect.x += width;
            Widgets.Label(rect, "~");
            rect.x += 7f;
            Widgets.TextFieldNumeric(rect, ref max, ref bufferMax);
            num = new IntRange(min, max);
            y += 30f;
        }

        public static void DrawFloatRange(ref float y, string label, ref FloatRange num, ref string bufferMin, ref string bufferMax, float x = 0f, float width = 30f)
        {
            Widgets.Label(new Rect(x, y, 350f, 25f), label);
            float min = num.min;
            float max = num.max;
            Rect rect = new Rect(Text.CalcSize(label).x + x + 5f, y, width, 25f);
            Widgets.TextFieldNumeric(rect, ref min, ref bufferMin);
            rect.x += width;
            Widgets.Label(rect, "~");
            rect.x += 7f;
            Widgets.TextFieldNumeric(rect, ref max, ref bufferMax);
            num = new FloatRange(min, max);
        }

        public static void DrawVector(ref float y0, string label, ref Vector3 vector, ref string bufferX, ref string bufferZ, ref string bufferY, float x0 = 0f, float width = 30f)
        {
            Widgets.Label(new Rect(x0, y0, 350f, 25f), label);
            float x = vector.x;
            float z = vector.z;
            float y = vector.y;
            Rect rect = new Rect(Text.CalcSize(label).x + x0 + 5f, y0, width, 25f);
            Widgets.TextFieldNumeric(rect, ref x, ref bufferX);
            rect.x += width;
            Widgets.Label(rect, "~");
            rect.x += 7f;
            Widgets.TextFieldNumeric(rect, ref y, ref bufferY);
            rect.x += width;
            Widgets.Label(rect, "~");
            rect.x += 7f;
            Widgets.TextFieldNumeric(rect, ref z, ref bufferZ);
            vector = new Vector3(x, y, z);
        }
        public static void DrawIntVector(ref float y0, string label,
            ref IntVec3 vector,
            ref string bufferX, ref string bufferZ, ref string bufferY, float x0 = 0f, float width = 30f)
        {
            Widgets.Label(new Rect(x0, y0, 350f, 25f), label);
            int x = vector.x;
            int z = vector.z;
            int y = vector.y;
            Rect rect = new Rect(Text.CalcSize(label).x + x0 + 5f, y0, width, 25f);
            Widgets.TextFieldNumeric(rect, ref x, ref bufferX);
            rect.x += width;
            Widgets.Label(rect, "~");
            rect.x += 7f;
            Widgets.TextFieldNumeric(rect, ref y, ref bufferY);
            rect.x += width;
            Widgets.Label(rect, "~");
            rect.x += 7f;
            Widgets.TextFieldNumeric(rect, ref z, ref bufferZ);
            vector = new IntVec3(x, y, z);
        }

        public static void DrawButtonAndText(ref float y, string text, string buttonText, Action buttonAction, float x = 0f)
        {
            Widgets.Label(new Rect(x, y, 300f, 25f), text);
            y += 30f;
            if (Widgets.ButtonText(new Rect(x, y, 200f, 25f), buttonText))
            {
                buttonAction();
            }
            y += 30f;
        }
        public static void DrawSelectableField<T>(float x, ref float y, string label,
            List<T> list, Action<T> action, Func<T, string> text,Vector2 size, List<FloatMenuOption> extra = null, Func<T, bool> validator = null)
        {
            if (Widgets.ButtonText(new Rect(x,y,size.x,size.y),label,false))
            {
                DrawFloatMenu(list,action,text,extra,validator);
            }
            y += size.y + 5f;
        }
        public static void DrawFloatMenu<T>(List<T> list, Action<T> action, Func<T, string> text, List<FloatMenuOption> extra = null, Func<T, bool> validator = null)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (extra != null)
            {
                options.AddRange(extra);
            }
            foreach (T t in list)
            {
                if (validator == null || validator(t))
                {
                    FloatMenuOption option = new FloatMenuOption(text(t), () =>
                    {
                        action(t);
                    });
                    options.Add(option);
                }
            }
            if (options.Any())
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
        public static List<FloatMenuOption> DrawFloatMenuWithRsult<T>(List<T> list, Action<T> action, Func<T, string> text, List<FloatMenuOption> extra = null, Func<T, bool> validator = null)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (extra != null)
            {
                options.AddRange(extra);
            }
            foreach (T t in list)
            {
                if (validator == null || validator(t))
                {
                    FloatMenuOption option = new FloatMenuOption(text(t), () =>
                    {
                        action(t);
                    });
                    options.Add(option);
                }
            }
            return options;
        }
        public static void DrawFloatMenu<T, V>(Dictionary<T, V> dictionary, Action<T, V> action, Func<T, V, string> text, List<FloatMenuOption> extra = null, Func<T, V, bool> validator = null)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (extra != null)
            {
                options.AddRange(extra);
            }
            foreach (KeyValuePair<T, V> pair in dictionary)
            {
                if (validator == null || validator(pair.Key, pair.Value))
                {
                    FloatMenuOption option = new FloatMenuOption(text(pair.Key, pair.Value), () =>
                     {
                         action(pair.Key, pair.Value);
                     });
                    options.Add(option);
                }
            }
            if (options.Any())
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public static void OpenCQFActionSelect(Action<Type> acceptAction)
        {
            Dictionary<string, Func<CQFAction, bool>> typeFilters = new Dictionary<string, Func<CQFAction, bool>>();
            Dictionary<string, string> typeTips = new Dictionary<string, string>();
            foreach (CQFActionCategory category in Enum.GetValues(typeof(CQFActionCategory)))
            {
                CQFActionCategory capturedCategory = category;
                string label = CQFEditorTools.GetCQFActionCategoryLabel(category);
                typeFilters[label] = action => action.ActionCategory == capturedCategory;
                typeTips[label] = CQFEditorTools.GetCQFActionCategoryTip(category);
            }
            Find.WindowStack.Add(new Dialog_Select<CQFAction>(
                new TextSelectDrawer<CQFAction>(
                    CQFEditorTools.GetCQFActions(),
                    t => t.GetType().Name.Translate(),
                    action => acceptAction(action.GetType()),
                    null,
                    t => (t.GetType().Name + "_Tip").CanTranslate() ? (t.GetType().Name + "_Tip").Translate().ToString() : "",
                    null,
                    null,
                    null,
                    typeFilters,
                    typeTips),
                "Select".Translate()));
        }

        public static List<Type> GetCQFActionTypes()
        {
            return typeof(CQFAction).AllSubclassesNonAbstract();
        }

        public static List<CQFAction> GetCQFActions()
        {
            return CQFEditorTools.GetCQFActionTypes().Select(type => (CQFAction)Activator.CreateInstance(type)).ToList();
        }

        public static Dictionary<string, Func<CQFAction, bool>> GetCQFActionTypeFilters()
        {
            Dictionary<string, Func<CQFAction, bool>> result = new Dictionary<string, Func<CQFAction, bool>>();
            foreach (CQFActionCategory category in Enum.GetValues(typeof(CQFActionCategory)))
            {
                CQFActionCategory capturedCategory = category;
                result[CQFEditorTools.GetCQFActionCategoryLabel(category)] = action => action.ActionCategory == capturedCategory;
            }
            return result;
        }

        public static string GetCQFActionCategoryLabel(CQFActionCategory category)
        {
            string key = "CQFActionCategory_" + category;
            return key.CanTranslate() ? key.Translate().ToString() : category.ToString();
        }

        public static string GetCQFActionCategoryTip(CQFActionCategory category)
        {
            string key = "CQFActionCategory_" + category + "_Tip";
            return key.CanTranslate() ? key.Translate().ToString() : null;
        }

        public static void DrawSelectButton<T>(float x, ref float y, string title,
            List<T> list, Action<T> addAction, Func<T, string> getText, List<FloatMenuOption> extraOptions = null)
        {
            if (Widgets.ButtonText(new Rect(x, y, 800f, 25f), title, false))
            {
                CQFEditorTools.DrawFloatMenu<T>(list, (d) => addAction(d), (d) => getText(d), extraOptions);
            }
            y += 30f;
        }
        public static void DrawSelectButton(float x, ref float y,string title, List<Type> list, Action<Type> addAction, Func<Type, string> getText)
        {
            if (Widgets.ButtonText(new Rect(x, y, 800f, 25f), title, false))
            {
                CQFEditorTools.DrawFloatMenu<Type>(list, (d) => addAction(d), (d) => getText(d));
            }
            y += 30f;
        }
        public static void DrawSelectButton(float x,ref float y,List<Type> list,Action<Type> addAction, Func<Type,string> getText)
        {
            if (Widgets.ButtonText(new Rect(x, y, 800f, 25f), "SelectCondition".Translate(), false))
            {
                CQFEditorTools.DrawFloatMenu<Type>(list, (d) => addAction(d), (d) => getText(d));
            }
            y += 30f;
        }
        public static void DrawButtonForList(ref float y, List<string> list, Func<string, string> getText, float x = 10f, float interval = 290f, Vector2? size = null)
        {
            if (size == null)
            {
                size = new Vector2(120f, 25f);
            }
            if (Widgets.ButtonText(new Rect(x + 5f, y, size.Value.x, size.Value.y), "Add".Translate()))
            {
                list.Add("undefined");
            }
            if (Widgets.ButtonText(new Rect(x + 5f + interval, y, size.Value.x, size.Value.y), "Remove".Translate()) && list.Any())
            {
                CQFEditorTools.DrawFloatMenu(list, (d) => list.Remove(d), (d) => getText(d));
            }
            y += size.Value.y + 5f;
        }
        public static void DrawButtonForList<T>(ref float y, List<T> list
            ,float x = 10f, float interval = 290f, Vector2? size = null) where T : Def
        {
            if (size == null)
            {
                size = new Vector2(120f, 35f);
            }
            if (Widgets.ButtonText(new Rect(x + 5f, y, size.Value.x, size.Value.y), "Add".Translate()))
            {
                CQFEditorTools.DrawFloatMenu<T>(DefDatabase<T>.AllDefsListForReading,
                    (d) => list.Add(d), (d) => d.label);
            }
            if (Widgets.ButtonText(new Rect(x + 5f + interval, y, size.Value.x, size.Value.y), "Remove".Translate()) && list.Any())
            {
                CQFEditorTools.DrawFloatMenu<T>(list, (d) => list.Remove(d), (d) => d.label);
            }
            y += 40f;
        }
        public static void DrawButtonForList<T>(ref float y, List<T> list, Func<T, string> getText, float x = 10f, float interval = 290f, Vector2? size = null) where T : new()
        {
            if (size == null)
            {
                size = new Vector2(120f, 35f);
            }
            if (Widgets.ButtonText(new Rect(x + 5f, y, size.Value.x, size.Value.y), "Add".Translate()))
            {
                list.Add(new T());
            }
            if (Widgets.ButtonText(new Rect(x + 5f + interval, y, size.Value.x, size.Value.y), "Remove".Translate()) && list.Any())
            {
                CQFEditorTools.DrawFloatMenu<T>(list, (d) => list.Remove(d), (d) => getText(d));
            }
            y += 40f;
        }
        public static void DrawButtonForList(ref float y, List<CQFAction> list,
            Func<CQFAction, string> getText, float x = 10f, float interval = 290f, Vector2? size = null)
        {
            if (size == null)
            {
                size = new Vector2(120f, 35f);
            }
            if (Widgets.ButtonText(new Rect(x + 5f, y, size.Value.x, size.Value.y), "Add".Translate()))
            {
                CQFEditorTools.OpenCQFActionSelect(t => list.Add((CQFAction)Activator.CreateInstance(t)));
            }
            if (Widgets.ButtonText(new Rect(x + 5f + interval, y, size.Value.x, size.Value.y), "Remove".Translate()) && list.Any())
            {
                CQFEditorTools.DrawFloatMenu(list, (d) => list.Remove(d), (d) => getText(d));
            }
            y += 40f;
        }
        public static void DrawButtonForList<T>(ref float y, List<T> list, Func<T, string> getText,
            Action addAction, float x = 10f, float interval = 290f, Vector2? size = null)
        {
            if (size == null)
            {
                size = new Vector2(120f, 35f);
            }
            if (Widgets.ButtonText(new Rect(x + 5f, y, size.Value.x, size.Value.y), "Add".Translate()))
            {
                addAction();
            }
            if (Widgets.ButtonText(new Rect(x + 5f + interval, y, size.Value.x, size.Value.y), "Remove".Translate()) && list.Any())
            {
                CQFEditorTools.DrawFloatMenu<T>(list, (d) => list.Remove(d), (d) => getText(d));
            }
            y += 40f;
        }
        public static void DrawButtonForList_UseIcon<T>(float y, List<T> list, Func<T, string> getText, Action addAction, float x = 10f,float iconSize = 25f, float interval = 35f, Vector2? size = null)
        {
            if (Widgets.ButtonImage(new Rect(x, y, iconSize, iconSize), TexButton.Plus))
            {
                addAction();
            }
            if (Widgets.ButtonImage(new Rect(x + interval, y, iconSize, iconSize), TexButton.Delete))
            {
                CQFEditorTools.DrawFloatMenu<T>(list, (d) => list.Remove(d), (d) => getText(d));
            }
        }

        public static void DrawButtonForList<T>(ref float y, List<T> list, Func<T, string> getText, Action<T> addAction, Action removeAction, float x = 10f)
        {
            if (Widgets.ButtonText(new Rect(x + 5f, y, 120f, 35f), "Add".Translate()))
            {
                CQFEditorTools.DrawFloatMenu<T>(list, (d) => addAction(d), (d) => getText(d));
            }
            if (Widgets.ButtonText(new Rect(x + 295f, y, 120f, 35f), "Remove".Translate()) && list.Any())
            {
                removeAction();
            }
            y += 40f;
        }
        public static void DrawButtonWithIcon(float y,Action addAction,Action removeAction, float x = 10f, float iconSize = 25f, float interval = 35f, Vector2? size = null)
        {
            if (Widgets.ButtonImage(new Rect(x, y, iconSize, iconSize), TexButton.Plus))
            {
                addAction();
            }
            if (Widgets.ButtonImage(new Rect(x + interval, y, iconSize, iconSize), TexButton.Delete))
            {
                removeAction();
            }
        }
        public static void DrawButtonForPawnData(float y, List<PawnSpawnData> list, float x = 10f)
        {
            if (Widgets.ButtonText(new Rect(x + 5f, y, 120f, 38f), "AddNewPawns".Translate()))
            {
                List<Type> types = new List<Type>();
                types.Add(typeof(PawnSpawnData));
                types.AddRange(typeof(PawnSpawnData).AllSubclassesNonAbstract());
                CQFEditorTools.DrawFloatMenu(types, a =>
     list.Add((PawnSpawnData)Activator.CreateInstance(a)), a => a.Name.Translate());
            }
            if (Widgets.ButtonText(new Rect(x + 150f, y, 120f, 38f), "PastePawns".Translate()) && CQFEditorTools.data != null)
            {
                list.Add(CQFEditorTools.data.Copy());
            }
            if (Widgets.ButtonText(new Rect(x + 295f, y, 120f, 38f), "DeleteNewPawns".Translate()) && list.Any())
            {
                CQFEditorTools.DrawFloatMenu<PawnSpawnData>(list, (d) => list.Remove(d), (d) => d.dataName);
            }
        }
        public static void DrawButtonForPawnData_UseIcon(float y, List<PawnSpawnData> list,float iconSize = 25f, float interval = 35f, float x = 10f)
        {
            if (Widgets.ButtonImage(new Rect(x, y, iconSize, iconSize), TexButton.Plus))
            {
                List<Type> types = new List<Type>();
                types.Add(typeof(PawnSpawnData));
                types.AddRange(typeof(PawnSpawnData).AllSubclassesNonAbstract());
                CQFEditorTools.DrawFloatMenu(types, a =>
     list.Add((PawnSpawnData)Activator.CreateInstance(a)), a => a.Name.Translate());
            }
            if (Widgets.ButtonImage(new Rect(x + interval, y, iconSize, iconSize), TexButton.Paste) && CQFEditorTools.data != null)
            {
                list.Add(CQFEditorTools.data.Copy());
            }
            if (Widgets.ButtonImage(new Rect(x + interval + interval, y, iconSize, iconSize), TexButton.Delete))
            {
                CQFEditorTools.DrawFloatMenu<PawnSpawnData>(list, (d) => list.Remove(d), (d) => d.dataName);
            }
        }
        public static void DrawEditableStringList(List<string> list, ref float y, string title = null, string tip = null, bool needBox = false, float x = 10f, float width = 180f)
        {
            float initY = y;
            if (title != null)
            {
                y += 5f;
                Text.Font = GameFont.Medium;
                Rect rectTitle = new Rect(x + 10, y, 1020f, 35f);
                Widgets.Label(rectTitle, title.Colorize(ColorLibrary.SkyBlue));
                if (tip != null)
                {
                    TooltipHandler.TipRegionByKey(rectTitle, tip);
                }
                Text.Font = GameFont.Small;
                y += 40f;
                float textWidth = Text.CalcSize(title).x + 20f;
                width = textWidth > width ? textWidth : width;
            }
            Rect textField = new Rect(x + 10, y, 150f, 25f);
            for (int i = 0; i < list.Count; i++)
            {
                string text = list[i];
                list[i] = Widgets.TextField(textField, text);
                y += 30f;
                textField.y += 30f;
            }
            y += 5f;
            if (needBox)
            {
                Widgets.DrawBox(new Rect(x, initY, width, y - initY), 1, QuestEditor_Dialog.blueTex);
            }
            y += 10f;
            CQFEditorTools.DrawButtonForList(ref y, list, t => t, x - 5f, width - 70f, new Vector2(70f, 25f));
        }
        public static void DrawSelectableStringList(List<string> list, ref float y, Action<Rect, string, int> drawAction, string title = null, string tip = null, bool needBox = false, float x = 10f, float defaultWidth = 200f)
        {
            float initY = y;
            float width = defaultWidth;
            if (title != null)
            {
                y += 5f;
                Text.Font = GameFont.Medium;
                Rect rectTitle = new Rect(x + 10, y, 1020f, 35f);
                Widgets.Label(rectTitle, title.Colorize(ColorLibrary.SkyBlue));
                if (tip != null)
                {
                    TooltipHandler.TipRegionByKey(rectTitle, tip);
                }
                Text.Font = GameFont.Small;
                y += 40f;
                float textWidth = Text.CalcSize(title).x + 20f;
                width = textWidth > width ? textWidth : width;
            }
            Rect textField = new Rect(x + 10, y, 150f, 25f);
            for (int i = 0; i < list.Count; i++)
            {
                drawAction(textField, list[i], i);
                y += 30f;
                textField.y += 30f;
            }
            y += 5f;
            if (needBox)
            {
                Widgets.DrawBox(new Rect(x, initY, width, y - initY), 1, QuestEditor_Dialog.blueTex);
            }
            y += 10f;
            CQFEditorTools.DrawButtonForList(ref y, list, t => t, x - 5f, width - 70f, new Vector2(70f, 25f));
        }
        public static void DrawDefList<T>(List<T> list,string title, ref float y,float x) where T : Def
        {
            Rect rect = new Rect(x, y, 150f, 25f);
            Widgets.Label(rect,title);
            rect.y += 30f;
            foreach (Def d in list)
            {
                Widgets.Label(rect, d.label);
                rect.y += 30f;
            }
            y = rect.y;
            DrawButtonForList<T>(ref y,list,x);
        }
        public static void DrawEditableList<T>(List<T> list, ref float y, Action<Rect, T> drawAction, Func<T, string> getText, string title = null, string tip = null, bool needBox = false, float x = 10f, float defaultWidth = 180f) where T : new()
        {
            float initY = y;
            float width = defaultWidth;
            if (title != null)
            {
                y += 5f;
                Text.Font = GameFont.Medium;
                Rect rectTitle = new Rect(x + 10, y, 1020f, 35f);
                Widgets.Label(rectTitle, title.Colorize(ColorLibrary.SkyBlue));
                if (tip != null)
                {
                    TooltipHandler.TipRegionByKey(rectTitle, tip);
                }
                Text.Font = GameFont.Small;
                y += 40f;
                float textWidth = Text.CalcSize(title).x + 20f;
                width = textWidth > width ? textWidth : width;
            }
            Rect textField = new Rect(x + 10, y, 150f, 25f);
            for (int i = 0; i < list.Count; i++)
            {
                drawAction(textField, list[i]);
                y += 30f;
                textField.y += 30f;
            }
            y += 5f;
            if (needBox)
            {
                Widgets.DrawBox(new Rect(x, initY, width, y - initY), 1, QuestEditor_Dialog.blueTex);
            }
            y += 10f;
            CQFEditorTools.DrawButtonForList<T>(ref y, list, t => getText(t), x - 5f, width - 70f, new Vector2(70f, 25f));
        }
        public static void DrawEditableList<T>(List<T> list, ref float y, Action<Rect, T> drawAction, Func<T, string> getText, Action addAction, string title = null, string tip = null, bool needBox = false, float x = 10f, float defaultWidth = 180f) where T : new()
        {
            float initY = y;
            float width = defaultWidth;
            if (title != null)
            {
                y += 5f;
                Text.Font = GameFont.Medium;
                Rect rectTitle = new Rect(x + 10, y, 1020f, 35f);
                Widgets.Label(rectTitle, title.Colorize(ColorLibrary.SkyBlue));
                if (tip != null)
                {
                    TooltipHandler.TipRegionByKey(rectTitle, tip);
                }
                Text.Font = GameFont.Small;
                y += 40f;
                float textWidth = Text.CalcSize(title).x + 20f;
                width = textWidth > width ? textWidth : width;
            }
            Rect textField = new Rect(x + 10, y, 150f, 25f);
            for (int i = 0; i < list.Count; i++)
            {
                drawAction(textField, list[i]);
                y += 30f;
                textField.y += 30f;
            }
            y += 5f;
            if (needBox)
            {
                Widgets.DrawBox(new Rect(x, initY, width, y - initY), 1, QuestEditor_Dialog.blueTex);
            }
            y += 10f;
            CQFEditorTools.DrawButtonForList<T>(ref y, list, t => getText(t), addAction, x - 5f, width - 70f, new Vector2(70f, 25f));
        }
        public static void DrawActionList_UseWindow(ref float y, float x, List<CQFAction> list, Rect inRect, string title, Func<CQFAction, string> getString)
        {
            CQFActionListEditor.Draw(ref y, x, inRect.width - x - 25f, inRect, title, list);
        }
        public static void DrawIDrawList_UseWindow<T>(ref float y, float x, List<T> list,
            Rect inRect, string title, Func<T, string> getString,Action<T> extraAction = null) where T : IDrawable
        {
            if (list is List<DialogCondition> conditions && extraAction == null)
            {
                CQFConditionListEditor.Draw(ref y, x, inRect.width - x - 25f, inRect, title, conditions);
                return;
            }
            Widgets.Label(new Rect(x, y, 255f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            y += 30f;
            foreach (T d in list)
            {
                if (Widgets.ButtonText(new Rect(x, y, 600f, 25f), getString(d), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(d));
                }
                y += 30f;
            }
            y += 5f;
            List<Type> types = new List<Type>();
            if (!typeof(T).IsAbstract)
            {
                types.Add(typeof(T));
            }
            types.AddRange(typeof(T).AllSubclassesNonAbstract());
            CQFEditorTools.DrawButtonForList(ref y, list,getString, () => CQFEditorTools.DrawFloatMenu(types,
                a =>
                {
                    T t = (T)Activator.CreateInstance(a);
                    list.Add(t);
                    extraAction?.Invoke(t);
                }, a => a.Name.Translate()));
        }
        public static void DrawIDrawList_UseWindow<T>(ref float y, float x, List<T> list, Rect inRect, string title
            ,Action addaction,Func<T, string> getString) where T : IDrawable
        {
            if (list is List<DialogCondition> conditions)
            {
                CQFConditionListEditor.Draw(ref y, x, inRect.width - x - 25f, inRect, title, conditions, addaction);
                return;
            }
            Widgets.Label(new Rect(x, y, 255f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            y += 30f;
            foreach (T d in list)
            {
                if (Widgets.ButtonText(new Rect(x, y, 600f, 25f), getString(d), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(d));
                }
                y += 30f;
            }
            y += 5f;
            CQFEditorTools.DrawButtonForList(ref y, list, d =>
                d.GetType().Name.Translate(), () => addaction(),x);
        }
        public static void DrawIDrawList_UseWindow_UseIcon<T>(ref float y, float x, List<T> list, Rect inRect, string title, Func<T, string> getString) where T : IDrawable
        {
            Widgets.Label(new Rect(x, y, 255f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            Rect button = new Rect(inRect.width - 150f, y, 30f, 30f);
            if (Widgets.ButtonImage(button, TexButton.Plus))
            {
                List<Type> types = new List<Type>();
                if (!typeof(T).IsAbstract)
                {
                    types.Add(typeof(T));
                }
                types.AddRange(typeof(T).AllSubclassesNonAbstract());
                CQFEditorTools.DrawFloatMenu(types, a =>
                  list.Add((T)Activator.CreateInstance(a)), a => a.Name.Translate());
            }
            button.x += 40f;
            button.x += 40f;
            if (Widgets.ButtonImage(button, TexButton.Delete))
            {
                CQFEditorTools.DrawFloatMenu<T>(list, (d) => list.Remove(d), (d) => getString(d));
            }
            y += 30f;
            foreach (T d in list)
            {
                if (Widgets.ButtonText(new Rect(x, y, 600f, 25f), getString(d), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(d));
                }
                y += 30f;
            }
            y += 5f;
        }
        public static void DrawPawnDataList_UseWindow(ref float y, float x, List<PawnSpawnData> list, Rect inRect, string title, Func<PawnSpawnData, string> getString)
        {
            Widgets.Label(new Rect(x, y, 255f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            y += 30f;
            foreach (PawnSpawnData d in list)
            {
                if (Widgets.ButtonText(new Rect(x, y, 600f, 25f), getString(d), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(d));
                }
                y += 30f;
            }
            y += 5f;
            List<Type> types = new List<Type>();
            if (!typeof(PawnSpawnData).IsAbstract)
            {
                types.Add(typeof(PawnSpawnData));
            }
            types.AddRange(typeof(PawnSpawnData).AllSubclassesNonAbstract());
            CQFEditorTools.DrawButtonForPawnData(y, list, x);
        }
        public static void DrawPawnDataList_UseWindow_UseIcon(ref float y, float x, List<PawnSpawnData> list, Rect inRect, string title, Func<PawnSpawnData, string> getString)
        {
            Widgets.Label(new Rect(x, y, 255f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            Rect button = new Rect(inRect.width - 150f, y, 30f, 30f);
            if (Widgets.ButtonImage(button, TexButton.Plus))
            {
                List<Type> types = new List<Type>();
                types.Add(typeof(PawnSpawnData));
                types.AddRange(typeof(PawnSpawnData).AllSubclassesNonAbstract());
                CQFEditorTools.DrawFloatMenu(types, a =>
     list.Add((PawnSpawnData)Activator.CreateInstance(a)), a => a.Name.Translate());
            }
            button.x += 40f;
            if (Widgets.ButtonImage(button, TexButton.Paste))
            {
                list.Add(CQFEditorTools.data.Copy());
            }
            button.x += 40f;
            if (Widgets.ButtonImage(button, TexButton.Delete))
            {
                CQFEditorTools.DrawFloatMenu<PawnSpawnData>(list, (d) => list.Remove(d), (d) => d.dataName);
            }
            y += 30f;
            foreach (PawnSpawnData d in list)
            {
                if (Widgets.ButtonText(new Rect(x, y, 600f, 25f), getString(d), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(d));
                }
                y += 30f;
            }
            y += 5f;
        }
        public static void DrawIDraw<T>(ref float y, float x,ref T t, Rect inRect, string title) where T : IDrawable
        {
            Widgets.Label(new Rect(x, y, 255f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            y += 30f;
            Vector2 start = new Vector2(x, y);
            Vector2 end = new Vector2(inRect.width - (x * 2) - 10f, y);
            if (t != null)
            {
                Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
                y += 5f;
                Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
            }

        }
        public static void DrawActionList(ref float y, float x, List<CQFAction> list, Rect inRect, string title, bool drawLine = true,string tip = null)
        {
            CQFActionListEditor.Draw(ref y, x, inRect.width - x - 25f, inRect, title, list, tip);
        }
        public static void DrawIDrawList<T>(ref float y, float x, List<T> list, Rect inRect, string title) where T : IDrawable
        {
            if (list is List<DialogCondition> conditions)
            {
                CQFConditionListEditor.Draw(ref y, x, inRect.width - x - 25f, inRect, title, conditions);
                return;
            }
            Widgets.Label(new Rect(x, y, 255f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            CQFEditorTools.DrawButtonForList_UseIcon(y, list, d => d.GetType().Name.Translate(), () => CQFEditorTools.DrawFloatMenu(typeof(T).AllSubclassesNonAbstract(), a =>
list.Add((T)Activator.CreateInstance(a)), a => a.Name.Translate()),inRect.width - 150f);
            y += 30f;
            Vector2 start = new Vector2(x, y);
            Vector2 end = new Vector2(inRect.width - (x * 2) - 10f, y);
            Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
            foreach (IDrawable d in list)
            {
                y += 3f;
                d.Draw(ref y, inRect, x);
                y += 3f;
                start.y = y;
                end.y = y;
                Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
            }
            y += 25f;
        }
        public static void DrawIDrawList<T>(ref float y, float x, List<T> list, Rect inRect, string title, Action addAction, Func<T, string> getText) where T : IDrawable
        {
            if (list is List<DialogCondition> conditions)
            {
                CQFConditionListEditor.Draw(ref y, x, inRect.width - x - 25f, inRect, title, conditions, addAction);
                return;
            }
            Widgets.Label(new Rect(x, y, 255f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            CQFEditorTools.DrawButtonForList_UseIcon(y, list, d => getText(d), () => addAction(), x + 220f);
            y += 30f;
            Vector2 start = new Vector2(x, y);
            Vector2 end = new Vector2(inRect.width - (x * 2) - 10f, y);
            Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
            foreach (IDrawable d in list)
            {
                y += 3f;
                d.Draw(ref y, inRect, x);
                y += 3f;
                start.y = y;
                end.y = y;
                Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
            }
            y += 25f;
        }
        public static void DrawIDrawList<T>(ref float y, float x, List<T> list, Rect inRect, string title, Action addAction, Func<T, string> getText, Func<T, float, Rect, float, float> drawAction)
        {
            Widgets.Label(new Rect(x, y, 255f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            y += 30f;
            Vector2 start = new Vector2(x, y);
            Vector2 end = new Vector2(inRect.width - (x * 2) - 10f, y);
            Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
            foreach (T d in list)
            {
                y += 3f;
                y = drawAction(d, y, inRect, x);
                y += 3f;
                start.y = y;
                end.y = y;
                Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
            }
            y += 25f;
            CQFEditorTools.DrawButtonForList(ref y, list, getText, addAction);
        }

        public static void DrawCQFConditionList(ref float y, float x, float width, Rect inRect, string title, List<DialogCondition> conditions)
        {
            CQFConditionListEditor.Draw(ref y, x, width, inRect, title, conditions);
        }

        public static void DrawSelectColorButtons(ref float y,string label,Color color,Action<Color> apply,float x = 200f)
        {
            Rect colorRect = new Rect(x, y, 30f, 30f);
            Widgets.DrawBoxSolid(colorRect, color);
            Widgets.DrawBox(colorRect);
            if (Widgets.ButtonText(new Rect(x + 35f, y + 2.5f, 130f, 25f), label,false))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("Colorbase".Translate(),() =>
                    Find.WindowStack.Add(new Dialog_ChooseColor(label, color, (from c in DefDatabase<ColorDef>.AllDefsListForReading
                        select c.color).ToList<Color>(),apply))
                ));
                options.Add(new FloatMenuOption("Hex".Translate(), () =>
                    Find.WindowStack.Add(new Dialog_RGB(color,apply))
                ));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            y += 30f;
        }
        public static void DrawButtonToSelectWithoutBackground<T>(ref float y, float x,string buttonText,List<T> list,Action<T> action,Func<T,string> getText)
        {
            if (Widgets.ButtonText(new Rect(x,y,250f,25f),buttonText,false))
            {
                CQFEditorTools.DrawFloatMenu(list,action,getText);
            }
            y += 30f;
        }

        public static List<T> GetObject<T>(string path, string objectName)
        {
            List<T> result = new List<T>();
            DirectoryInfo ruleDir = new DirectoryInfo(path);
            foreach (FileInfo file in ruleDir.GetFiles("*.xml"))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(file.FullName);
                foreach (XmlNode xmlNode in xml.SelectNodes(objectName))
                {
                    result.Add(DirectXmlToObject.ObjectFromXml<T>(xmlNode, false));
                }
            }
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return result;
        }
        public static string GetSaveValue<T>(T t)
        {
            return t is Def def ? def.defName : t.ToString();
        }
        public static XElement SaveDictionary<T, K>(Dictionary<T, K> dictionary, string nodeName)
        {
            XElement result = new XElement(nodeName);
            XElement li = new XElement("li");
            foreach (KeyValuePair<T, K> value in dictionary)
            {
                li.Add(new XElement("key", GetSaveValue(value.Key)));
                li.Add(new XElement("value", GetSaveValue(value.Value)));
            }
            result.Add(result);
            return result;
        }
        public static XElement SaveDictionary_Saveable<T, K>(Dictionary<T, K> dictionary, string nodeName) where K : ISaveable
        {
            XElement result = new XElement(nodeName);
            XElement li = new XElement("li");
            foreach (KeyValuePair<T, K> value in dictionary)
            {
                li.Add(new XElement("key", GetSaveValue(value.Key)));
                li.Add(value.Value.SaveToXElement("value"));
            }
            result.Add(result);
            return result;
        }
        public static XElement SaveDictionary_List<T, K>(Dictionary<T, List<K>> dictionary, string nodeName)
        {
            XElement result = new XElement(nodeName);
            foreach (KeyValuePair<T, List<K>> value in dictionary)
            {
                XElement li = new XElement("li");
                li.Add(new XElement("key", GetSaveValue(value.Key)));
                XElement valueX = new XElement("value");
                value.Value.ForEach(v => valueX.Add(new XElement("li", GetSaveValue(v))));
                li.Add(valueX);
                result.Add(li);
            }
            return result;
        }
        public static XElement SaveDictionary_Saveable_List<T, K>(Dictionary<T, List<K>> dictionary, string nodeName) where K : ISaveable
        {
            XElement result = new XElement(nodeName);
            foreach (KeyValuePair<T, List<K>> value in dictionary)
            {
                XElement li = new XElement("li");
                li.Add(new XElement("key", GetSaveValue(value.Key)));
                XElement valueX = new XElement("value");
                value.Value.ForEach(v => valueX.Add(v.SaveToXElement("li")));
                li.Add(valueX);
                result.Add(li);
            }
            return result;
        }
        public static XElement SaveList<T>(List<T> list, string nodeName)
        {
            XElement result = new XElement(nodeName);
            list.ForEach(x => result.Add(new XElement("li", GetSaveValue(x))));
            return result;
        }
        public static XElement SaveList_Saveable<T>(List<T> list, string nodeName) where T : ISaveable
        {
            XElement result = new XElement(nodeName);
            list.ForEach(x => result.Add(x.SaveToXElement("li")));
            return result;
        }

        private static string DutyTip(DutyDef duty)
        {
            if (duty == null)
            {
                return null;
            }
            string description = duty.description.NullOrEmpty() ? string.Empty : "\n" + duty.description;
            return "CQF_DutyThinkNodeMod".Translate(ModTypeUtility.GetModName(duty), duty.defName) + description;
        }

        private static int DutyPriority(DutyDef duty)
        {
            if (ModTypeUtility.IsCQFDef(duty))
            {
                return -100;
            }
            return duty.defName.CanTranslate() || !duty.label.NullOrEmpty() ? 0 : 10;
        }

        private static Dictionary<string, Func<DutyDef, bool>> MakeDutyModFilters(List<DutyDef> duties)
        {
            Dictionary<string, Func<DutyDef, bool>> result = new Dictionary<string, Func<DutyDef, bool>>();
            List<string> modNames = duties
                .Select(duty => ModTypeUtility.GetModName(duty))
                .Distinct()
                .OrderBy(name => duties.Any(duty => ModTypeUtility.GetModName(duty) == name && ModTypeUtility.IsCQFDef(duty)) ? 0 : 1)
                .ThenBy(name => name)
                .ToList();
            foreach (string modName in modNames)
            {
                string capturedName = modName;
                result[capturedName] = duty => ModTypeUtility.GetModName(duty) == capturedName;
            }
            return result;
        }


        public static string exitName;
        public static List<TagWithChance> tagWithChance = new List<TagWithChance>();
        public static List<MapDefWithChance> mapDefWithChance = new List<MapDefWithChance>();

        public static InteractionOperation operation = null;
        public static PawnSpawnData data = null;
        public static LootData lootData = null;
        public static ActionComp actionComp = null;
        public static List<TrapComp> copyTrapComps = new List<TrapComp>();

        public static List<InteractionOperation> operations = new List<InteractionOperation>();
        public static List<InteractionDataDef> operationDefs = new List<InteractionDataDef>();

        public static string lootBoxName = "Undefined";
        public static int tickToOpen = 100;
        public static bool destroyAfterOpening = false;
        public static string openReport = "OpenLoot";
        public static List<LootData> loots = new List<LootData>();
        public static string buffer;
        public static bool useLootDef = true;
        public static LootDataDef lootDef = null;
        public static bool openWhenDestroyed = true;

        public static ThingData thingData = null;

        public static List<ThingDef> customMapExitDefs = new List<ThingDef>();

        public static Rot4 coreRotation = Rot4.Invalid;
        public static string generationKey = null;
        public static bool isCenter = true;
        public static ThingData reserveThing = null;
        public static List<ZoneCondition> conditions = new List<ZoneCondition>();
        public static bool destroyThings = false;
        public static List<string> coreTags = new List<string>();
        public static bool prohibitRotatingDocking = false;
        public static bool prohibitFlippingDocking = false;

        public static List<CQFAction> actions = new List<CQFAction>();

        public static readonly Texture2D icon_Save = ContentFinder<Texture2D>.Get("UI/Icon_MoveOut");
        public static readonly Texture2D icon_Border = ContentFinder<Texture2D>.Get("UI/Border");
        public static readonly Texture2D icon_DestroyThing = ContentFinder<Texture2D>.Get("UI/Icons/Icon_DestroyThing");
        public static readonly Texture2D icon_Route = ContentFinder<Texture2D>.Get("UI/Icons/Icon_Route");
        public static readonly Texture2D showIcon = ContentFinder<Texture2D>.Get("UI/Show");
        public static readonly Texture2D hideIcon = ContentFinder<Texture2D>.Get("UI/Hide");

    }
}
