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
                Find.WindowStack.Add(new Dialog_Select<Type>(typeof(CQFAction).AllSubclassesNonAbstract(),null,a => a.Name.Translate(),"Select".Translate(),t => list.Add((CQFAction)Activator.CreateInstance(t)),null,null,t => (t.Name + "_Tip").CanTranslate() ? (t.Name + "_Tip").Translate().ToString() : ""));
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
            Widgets.Label(new Rect(x, y, 255f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            y += 30f;
            foreach (CQFAction d in list)
            {
                if (Widgets.ButtonText(new Rect(x, y, 600f, 25f), getString(d), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(d));
                }
                y += 30f;
            }
            y += 5f;
            CQFEditorTools.DrawButtonForList(ref y,list, a => a.GetType().Name.Translate(), 10, 150f);
        }
        public static void DrawIDrawList_UseWindow<T>(ref float y, float x, List<T> list, 
            Rect inRect, string title, Func<T, string> getString,Action<T> extraAction = null) where T : IDrawable
        {
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
            Rect titleRect = new Rect(x, y, 255f, 25f);
            Widgets.Label(titleRect, title.Colorize(ColorLibrary.PaleBlue));
            if (tip != null) 
            {
                TooltipHandler.TipRegion(titleRect, tip);
            }
            CQFEditorTools.DrawButtonWithIcon(y,
                () => Find.WindowStack.Add(new Dialog_Select<Type>(typeof(CQFAction).AllSubclassesNonAbstract(),null,t => t.Name.Translate(),"Select".Translate(),t => list.Add((CQFAction)Activator.CreateInstance(t)),
                null,null,t => (t.Name + "_Tip").CanTranslate() ? (t.Name + "_Tip").Translate().ToString() : "")),
                () => CQFEditorTools.DrawFloatMenu(list,a => list.Remove(a),a => a.GetType().Name.Translate()),inRect.width - 150f,30f);
            y += 30f;
            Vector2 start = new Vector2(x, y);
            Vector2 end = new Vector2(inRect.width - (x * 2) - 10f, y);
            if (drawLine) 
            {
                Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
            }
            foreach (IDrawable d in list)
            {
                y += 3f;
                d.Draw(ref y, inRect, x);
                y += 3f;
                start.y = y;
                end.y = y;
                if (drawLine)
                {
                    Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
                }
            }
            y += 5f;
        }
        public static void DrawIDrawList<T>(ref float y, float x, List<T> list, Rect inRect, string title) where T : IDrawable
        {
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

        public static bool disgenerateByCore = false;
    }
    //
    //
    //
    //
    //
    //
    //
    public static class GameTools
    {
        public static void AddTemporaryTagret(string name,TargetInfo target) 
        {
            temporaryTargets.SetOrAdd(name,target);
        }
        public static Map GenerateSubMap(IntVec3 size,PocketMapParent parent, MapGeneratorDef generatorDef, IEnumerable<GenStepWithParams> extraGenStepDefs, Map sourceMap)
        {
            parent.sourceMap = sourceMap;
            Map result = MapGenerator.GenerateMap(size, parent, generatorDef, extraGenStepDefs, null, true);
            Find.World.pocketMaps.Add(parent);
            return result;
        }
        public static void FogMap(Map map) 
        {
            map.fogGrid.Refog(CellRect.WholeMap(map));
            if (Current.ProgramState == ProgramState.Playing)
            {
                map.roofGrid.Drawer.SetDirty();
            }
        }
        public static Thing MakeThingWithoutID(ThingDef def, ThingDef stuff = null)
        {
            if (stuff != null && !stuff.IsStuff)
            {
                stuff = GenStuff.DefaultStuffFor(def);
            }
            if (def.MadeFromStuff && stuff == null)
            {
                stuff = GenStuff.DefaultStuffFor(def);
            }
            if (!def.MadeFromStuff && stuff != null)
            {
                stuff = null;
            }
            Thing thing = (Thing)Activator.CreateInstance(def.thingClass);
            thing.def = def;
            if (thing is ThingWithComps thingWithComp)
            {
                thingWithComp.InitializeComps();
            }
            if (thing.def.useHitPoints)
            {
                thing.HitPoints = Mathf.RoundToInt((float)thing.MaxHitPoints * Mathf.Clamp01(thing.def.startingHpRange.RandomInRange));
            }
            thing.SetStuffDirect(stuff);
            return thing;
        }
        public static Dictionary<string, TargetInfo> GetTargets(Dictionary<string, TargetInfo> targets, Quest quest, List<string> targetTexts)
        {
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            targetTexts.ForEach(t =>
            {
                if (GetTarget(targets, quest, t) is TargetInfo target) 
                {
                    result.Add(t, target);
                }
                int i = 0;
                GetTargetsFromGroup(quest, t)?.ForEach(p => 
                {
                    result.Add(t + i, p);
                    i ++;
                });
            });
            return result;
        }
        public static TargetInfo GetTarget(Dictionary<string, TargetInfo> targets, Quest quest, string targetText)
        {
            if (targetText == null)
            {
                return null;
            }
            TargetInfo target = null;
            if (targets != null)
            {
                targets.ToList().ForEach(t =>
                {
                    if (t.Key == targetText)
                    {
                        target = t.Value;
                    }
                });
            }
            if (target == null && GameTools.GetTargetFromQuestDatabase(quest, targetText) is TargetInfo target2)
            {
                target = target2;
            }
            if (target == null && temporaryTargets.TryGetValue(targetText,out TargetInfo target3))
            {
                target = target3;
            }
            if (target == null && GameTools.GetTargetFromGlobalDatabase(quest, targetText) is TargetInfo target4)
            {
                target = target4;
            }
            return target;
        }
        public static TargetInfo GetTargetFromQuestDatabase(Quest quest, string targetText)
        {
            if (quest == null || targetText == null) 
            {
                return null;
            }
            
            TargetInfo? result = null;
            string[] ts = targetText.Split(new char[] { '.' });
            if (ts.Count() >= 2 && int.TryParse(ts.Last(), out int index0))
            {
                result = result ?? GetTargetWithIndex(quest, ts.First(), index0);
            }
            QuestData data = GameComponent_Editor.Component.GetQuestData(quest);
            if (data != null)
            {
                if (data.GetGroup(targetText) is {} ps)
                {
                    result = result ?? ps.First();
                }
                if (data.TargetDatas.Find(t => t.key == targetText) is TargetWithKey target)
                {
                    result = result ?? target.target;
                }
            }
            if (DebugSettings.godMode)
            {
                StringBuilder debug = new StringBuilder();
                debug.AppendLine($"正在搜索目标，使用文本：{targetText}"); 
                ts.ToList().ForEach(t0 =>debug.AppendLine(t0));
                debug.AppendLine($"结果：{result}");
                Log.Message(debug.ToString().Trim());
            }
            return result ?? TargetInfo.Invalid;
        }
        public static TargetInfo GetTargetFromGlobalDatabase(Quest quest, string targetText)
        {
            if (quest == null)
            {
                return null;
            }
            TargetInfo? result = null;
            string[] ts = targetText.Split(new char[] { '.' });
            if (DebugSettings.godMode)
            {
                Log.Message(targetText);
                ts.ToList().ForEach(t0 => Log.Message(t0));
            }
            if (ts.Count() >= 2 && int.TryParse(ts.Last(), out int index0))
            {
                result = result ?? GetTargetWithIndex(quest, ts.First(), index0);
            }
            QuestData data = GameComponent_Editor.Component.GlobalDatabase;
            if (data != null)
            {
                if (data.GetGroup(targetText) is {} ps)
                {
                    result = result ?? ps.First();
                }
                if (data.TargetDatas.Find(t => t.key == targetText) is TargetWithKey target)
                {
                    result = result ?? target.target;
                }
            }
            return result ?? TargetInfo.Invalid;
        }
        public static List<TargetInfo> GetTargetsFromGroup(Quest quest, string targetText)
        {
            QuestData data = GameComponent_Editor.Component.GetQuestData(quest) ?? GameComponent_Editor.Component.GlobalDatabase;
            if (DebugSettings.godMode && data != null)
            {
                Log.Message(data.ToString());
            }
            if (data != null && data.GetGroup(targetText) is {} ps)
            {
                List<TargetInfo> result = new List<TargetInfo>();
                ps.ForEach(p => result.Add(p));
                return result;
            }
            return null;
        }
        public static TargetInfo GetTargetWithIndex(Quest quest, string targetText, int index)
        {
            TargetInfo? result = null;
            QuestData data = GameComponent_Editor.Component.GetQuestData(quest);
            if (DebugSettings.godMode && data != null)
            {
                Log.Message(data.ToString());
            }
            if (data != null && data.GetGroup(targetText) is {} ps)
            {
                result = result ?? new TargetInfo(ps[index]);
            }
            return result.Value;
        }
        public static Quest GetQuestFromMap(Map map)
        {
            Quest result = null;
            if (map == null)
            {
                return result;
            }
            if (map.Parent is CustomSite site)
            {
                result = result ?? site.quest;
            }
            if (map.Parent is MapParent_Custom parent)
            {
                result = result ?? parent.quest;
            }
            return result;
        }
        public static Quest GetQuestFromThing(Thing t)
        {
            Quest result = null;
            if (t == null)
            {
                return result;
            }
            if (t.questTags != null && t.questTags.Any())
            {
                List<string> tags = t.questTags.FindAll(t2 => t2.StartsWith("Quest"));
                foreach (string t3 in tags)
                {
                    string t4 = t3.Split('.').First().Remove(0, 5);
                    if (int.TryParse(t4, out int id) && Find.QuestManager.QuestsListForReading.Find(q => q.id == id) is Quest quest)
                    {
                        result = result ?? quest;
                    }
                }


            }
            if (t.Spawned && result == null)
            {
                result = GetQuestFromMap(t.Map);
            }
            return result;
        }
        public static List<Thing> AllConsumableThing(Map map)
        {
            return map.listerThings.AllThings.FindAll(t => !t.IsForbidden(Faction.OfPlayer) && !t.Position.Fogged(map)).ListFullCopy();
        }
        public static List<Thing> AllConsumableThingForDef(ThingDef def, Map map)
        {
            return map.listerThings.ThingsOfDef(def).FindAll(t => !t.IsForbidden(Faction.OfPlayer) && !t.Position.Fogged(map)).ListFullCopy();
        }
        public static bool CheckRequiredThings(List<CQFThingData> requiredThings, List<Thing> things
            , out ThingDef def, out int count,out int limit)
        {
            Dictionary<ThingDef, int> counts = new Dictionary<ThingDef, int>();
            requiredThings.ForEach(d => counts.Add(((CQFThingDefCount)d).thing, d.count.min));
            foreach (Thing t in things)
            {
                if (counts.ContainsKey(t.def))
                {
                    counts[t.def] -= t.stackCount;
                }
            }
            if (counts.ToList().Find(c => c.Value >= 1) 
                    is KeyValuePair<ThingDef, int> thing && thing.Key != null)
            {
                def = thing.Key;
                limit = requiredThings.Find(t => t is CQFThingDefCount tc && tc.thing == thing.Key).count.min;
                count = limit - thing.Value;
                return false;
            }
            def = null;
            count = 0;
            limit = 0;
            return true;
        }
        public static void ConsumeRequiredThings(Pawn interviewer, Pawn interviewee, List<CQFThingData> requiredThings)
        {
            if (requiredThings.Any() && interviewee != null)
            {
                if (interviewee.Map.IsPlayerHome)
                {
                    requiredThings.ForEach(d =>
                    {
                        GameTools.ConsumeThings(((CQFThingDefCount)d).thing, d.count.min, interviewee.Map, null);
                    });
                }
                else
                {
                    Dictionary<ThingCategoryDef, int> categoryAndCount = new Dictionary<ThingCategoryDef, int>();
                    foreach (CQFThingData data in requiredThings)
                    {
                        if (data is CQFThingDefCount tData)
                        {
                            interviewee?.inventory?.innerContainer.Take(interviewee?.inventory.
                                innerContainer.ToList().Find(i => i.def == tData.thing), tData.count.min).Destroy();
                        }
                        if (data is CQFThingCategoryCount cData)
                        {
                            categoryAndCount.Add(cData.category, cData.count.min);
                        }
                    }
                }
            }
        }
        public static void ConsumeThings(ThingDef def, int count, Map map, Pawn receiver = null)
        {
            foreach (Thing t in AllConsumableThingForDef(def, map))
            {
                int spliteCount = t.stackCount <= count ? t.stackCount : count;
                Thing thing = t.SplitOff(spliteCount);
                count -= spliteCount;
                if (receiver == null)
                {
                    thing.Destroy();
                }
                else
                {
                    receiver.inventory.TryAddAndUnforbid(thing);
                }
                if (count <= 0)
                {
                    break;
                }
            };
        }
        public static string GetDialogText(string text, Thing interviewer, Thing interviewee,DialogTreeDef dialog,Quest quest)
        {
            TaggedString result = text.CanTranslate() ? text.Translate() : new TaggedString(text);
            List<NamedArgument> names = new List<NamedArgument>() { interviewer.Named("Interviewer"), interviewee.Named("Interviewee") }; 
            Find.FactionManager.AllFactions.ToList().ForEach(f =>
            {
                if (!names.Exists(n => n.label == f.def.defName)) 
                {
                    names.Add(f.Named(f.def.defName));
                }
            });
            if (dialog != null && dialog.extraThingRefers.Any())
            {
                foreach (string key in dialog.extraThingRefers) 
                {
                    if (GameTools.GetTarget(new Dictionary<string, TargetInfo>(), quest, key).Thing is Thing t) 
                    {
                        names.Add(t.Named(key));
                    }
                }
             
            }
            result = result.Formatted(names);
            if (interviewer is Pawn interviwerPawn)
            {
                result.AdjustedFor(interviwerPawn, "Interviewer", true).Resolve();
            }
            if (interviewee is Pawn interviweePawn)
            {
                result.AdjustedFor(interviweePawn, "Interviewee", true).Resolve();
            }
            return result.Resolve();
        }

        public static Faction GetFaction(FactionDef faction)
        {
            return faction.isPlayer ? Find.FactionManager.OfPlayer : Find.FactionManager.FirstFactionOfDef(faction);
        }
        public static Faction GetFaction(string faction, Map map,bool humanlike = true)
        {
            if (faction == null)
            {
                return null;
            }
            if (faction == "RandomHostile")
            {
                return Find.FactionManager.AllFactionsListForReading.ToList().FindAll(f => !f.IsPlayer && f.PlayerRelationKind == FactionRelationKind.Hostile && (!humanlike || f.def.humanlikeFaction)).RandomElement();
            }
            if (faction == "RandomAlly")
            {
                return Find.FactionManager.AllFactions.ToList().FindAll(f => !f.IsPlayer && f.PlayerRelationKind == FactionRelationKind.Ally && (!humanlike || f.def.humanlikeFaction)).RandomElement();
            }
            if (faction == "RandomNeutral")
            {
                return Find.FactionManager.AllFactions.ToList().FindAll(f => !f.IsPlayer && f.PlayerRelationKind == FactionRelationKind.Neutral && (!humanlike || f.def.humanlikeFaction)).RandomElement();
            }
            if (faction == "MapFaction" && map != null)
            { 
                return map.Parent.Faction;
            }
            return faction.NullOrEmpty() ? null : Find.FactionManager.FirstFactionOfDef(FactionDef.Named(faction));
        }

        public static bool isGeneratingMap = false;
        public static Dictionary<string, TargetInfo> temporaryTargets = new Dictionary<string, TargetInfo>();
    }
    public class ThingData : IExposable, ISaveable
    {
        public void OpenSelectDialog()
        {
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(Designator_SpawnThing.Bespawnable,
t => t.uiIcon, t => t.label, "Select".Translate(),
t =>
{
    this.def = t;
    this.hitPoint = t.BaseMaxHitPoints;
    if (t.MadeFromStuff)
    {
        Find.WindowStack.Add(new Dialog_Select<ThingDef>(GenStuff.AllowedStuffsFor(t).ToList(), s => s.uiIcon, s => s.label, "SelectStuff".Translate(), s =>
        {
            this.stuff = s;
            this.hitPoint = (int)(t.BaseMaxHitPoints * (s.stuffProps.statFactors.Find(s2 => s2.stat == StatDefOf.MaxHitPoints) is StatModifier stat ? stat.value : 1f));
        }, t2 => t2.graphic?.Color ?? Color.white));
    }
}, t => t.graphic?.Color ?? Color.white));
        }
        public ThingData() { }
        public ThingData(Thing thing, IntVec3 pos)
        {
            this.def = thing.def;
            this.rotation = thing.Rotation;
            this.position = pos;
            this.count = thing.stackCount;
            this.stuff = thing.Stuff;
            this.style = thing.StyleDef;
            this.faction = thing.Faction?.def;
            if (thing.TryGetQuality(out QualityCategory q)) 
            {
                this.quality = q;
            }
            if (thing is Plant plant)
            {
                this.growth = plant.Growth;
            }
            if (thing.def.useHitPoints && thing.MaxHitPoints != thing.HitPoints)
            {
                this.hitPoint = thing.HitPoints;
            }
            if (thing.TryGetComp<CompPowerBattery>() is CompPowerBattery compB)
            {
                this.storedEnergy = compB.StoredEnergy;
            }
            if (thing.TryGetComp<CompRefuelable>() is CompRefuelable compR) 
            {
                this.storedEnergy = compR.Fuel;
            }
            if (thing.TryGetComp<CompColorable>() is CompColorable color)
            {
                this.color = color.Color;
            }
            if (thing is Building b && b.PaintColorDef != null) 
            {
                this.colorDef = b.PaintColorDef;
            }
        }
        public Thing Spawn(Map map, IntVec3 pos, Func<ThingDef,bool, ThingDef> getDef, ThingDef forcedStuff = null, Rot4? forcedRot = null)
        {
            ThingDef def = getDef(this.def,false);
            if (def == null)
            {
                Log.Error("Spawn thing data error:" + this.ToString());
                return null;
            }
            Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? forcedStuff ?? getDef(this.stuff,true) : null);
            thing.stackCount = this.count;
            thing.StyleDef = this.style;
            thing.Rotation = this.rotation;
            thing.stackCount = this.count;
            if (thing.TryGetComp<CompQuality>() is CompQuality compQ) 
            {
                compQ.SetQuality(this.quality,null);
            }
            if (thing.TryGetComp<CompPowerBattery>() is CompPowerBattery compB)
            {
                compB.AddEnergy(this.storedEnergy);
            }
            if (thing.TryGetComp<CompRefuelable>() is CompRefuelable compR)
            {
                compR.Refuel(this.storedEnergy);
            }
            if (thing.def.useHitPoints && this.hitPoint != -1)
            {
                thing.HitPoints = (int)(((float)this.hitPoint / (float)this.def.GetStatValueAbstract(StatDefOf.MaxHitPoints, this.stuff ?? GenStuff.DefaultStuffFor(this.def)) * thing.MaxHitPoints));
            }
            if (thing is Plant plant)
            {
                plant.Growth = this.growth;
            }
            if (this.faction != null && Find.FactionManager.FirstFactionOfDef(this.faction) is Faction faction)
            {
                thing.SetFaction(faction);
            }
            if (thing.TryGetComp<CompColorable>() is CompColorable color)
            {
                color.SetColor(this.color);
            }
            if (thing is Building b) 
            {
                b.ChangePaint(this.colorDef);
            }
            return GenSpawn.Spawn(thing, pos, map, forcedRot ?? this.rotation);
        }
        public XElement SaveToXElement(string nodeName)
        {
            if (this.def == null) 
            {
                return null;
            }
            XElement result = new XElement(nodeName);
            result.Add(new XElement("def", this.def?.defName));
            if (this.stuff != null)
            {
                result.Add(new XElement("stuff", this.stuff?.defName));
            }
            if (this.style != null)
            {
                result.Add(new XElement("style", this.style?.defName));
            }
            if (this.rotation != Rot4.North)
            {
                result.Add(new XElement("rotation", this.rotation.AsInt));
            }
            if (this.count > 1)
            {
                result.Add(new XElement("count", this.count));
            }
            if (this.faction != null)
            {
                result.Add(new XElement("faction", this.faction.defName));
            }
            if (this.growth != 0f)
            {
                result.Add(new XElement("growth", this.growth));
            }
            if (this.storedEnergy != 0f)
            {
                result.Add(new XElement("storedEnergy", this.storedEnergy));
            }
            if (this.quality != QualityCategory.Normal)
            {
                result.Add(new XElement("quality", this.quality));
            }
            if (this.def.useHitPoints && this.hitPoint != -1)
            {
                result.Add(new XElement("hitPoint", this.hitPoint));
            }
            if (this.color != Color.white)
            {
                result.Add(new XElement("color", this.color.ToString()));
            }
            if (this.colorDef != null)
            {
                result.Add(new XElement("colorDef", this.colorDef.defName));
            }
            if (this.allRect != null && this.allRect.Any())
            {
                result.Add(CQFEditorTools.SaveList(this.allRect, "allRect"));
            }
            else
            {
                XElement pos = new XElement("position", $"({this.position.x},{this.position.y},{this.position.z})");
                result.Add(pos);
            }
            return result;
        }
        public ThingData Copy()
        {
            ThingData result = new ThingData();
            result.def = this.def;
            result.stuff = this.stuff;
            result.style = this.style;
            result.faction = this.faction;
            result.rotation = this.rotation;
            result.position = this.position;
            result.count = this.count;
            result.growth = this.growth;
            result.quality = this.quality;
            result.hitPoint = this.hitPoint;
            result.storedEnergy = this.storedEnergy;
            result.color = this.color;
            result.colorDef = this.colorDef;
            result.allPositions = this.allPositions.ListFullCopy();
            result.allRect = this.allRect.ListFullCopy();
            return result;
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref this.def, "def");
            Scribe_Defs.Look(ref this.style, "style");
            Scribe_Defs.Look(ref this.stuff, "stuff");
            Scribe_Defs.Look(ref this.faction, "faction"); 
            Scribe_Defs.Look(ref this.colorDef, "colorDef");
            Scribe_Values.Look(ref this.hitPoint, "QE_ThingData_hitPoint");
            Scribe_Values.Look(ref this.growth, "QE_ThingData_growth");
            Scribe_Values.Look(ref this.rotation, "QE_ThingData_rotation"); 
            Scribe_Values.Look(ref this.color, "color");
            Scribe_Values.Look(ref this.count, "QE_ThingData_count");
            Scribe_Values.Look(ref this.storedEnergy, "QE_ThingData_storedEnergy");
            Scribe_Values.Look(ref this.quality, "quality");
            Scribe_Collections.Look(ref this.allPositions, "positions", LookMode.Value);
            Scribe_Collections.Look(ref this.allRect, "allRect", LookMode.Value);
        }

        public bool Equals_Def(ThingData data)
        {
            return data.def == this.def && data.stuff == this.stuff && data.style == this.style && data.faction == this.faction && data.rotation == this.rotation && data.count == this.count
               && data.hitPoint == this.hitPoint && this.growth == data.growth && this.storedEnergy == data.storedEnergy && this.color == data.color && this.colorDef == data.colorDef;
        }

        public ThingDef def = null;
        public ThingDef stuff = null;
        public ThingStyleDef style = null;
        public FactionDef faction = null;
        public Rot4 rotation = Rot4.North;
        public IntVec3 position = IntVec3.Zero;
        public Color color = Color.white;
        public ColorDef colorDef;
        public List<IntVec3> allPositions = new List<IntVec3>();
        public List<CellRect> allRect = new List<CellRect>();
        public QualityCategory quality = QualityCategory.Normal;
        public int count = 1;
        public float growth = 0f;
        public int hitPoint = -1;
        public float storedEnergy;
    }
    public class RuleData
    {
        public RulePack GetRulePack()
        {
            RulePack result = new RulePack();
            if (this.rulesFiles != null)
            {
                typeof(RulePack).GetField("rulesFiles", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(result, this.rulesFiles);
            }
            typeof(RulePack).GetField("rulesStrings", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(result, this.stringRules);
            return result;
        }
        public RulePackDef GetRulePackDef()
        {
            RulePackDef result = new RulePackDef();
            result.defName = this.ruleName;
            RulePack pack = new RulePack();
            if (this.rulesFiles != null)
            {
                typeof(RulePack).GetField("rulesFiles", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pack, this.rulesFiles);
            }
            typeof(RulePack).GetField("rulesStrings", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pack, this.stringRules);
            typeof(RulePackDef).GetField("rulePack", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(result, pack);
            return result;
        }
        public XElement GetXmlNode(string nodeName)
        {
            XElement result = new XElement(nodeName);
            XElement ruleStrings = new XElement("rulesStrings");
            foreach (string rule in this.stringRules)
            {
                XElement li = new XElement("li", @rule);
                ruleStrings.Add(li);
            }
            result.Add(ruleStrings);
            return result;
        }

        public string ruleName = "";
        public List<string> rulesFiles;
        public List<string> stringRules = new List<string>() { "" };
    }
}


