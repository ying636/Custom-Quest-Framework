using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CustomMapBackgroundDataEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomMapBackgroundData cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            Widgets.Label(new Rect(x, y, inRect.width - 40f, 30f), "CustomMapStep_MapBackground".Translate().Colorize(CQFUIStyle.Accent));
            y += 35f;
            cqfReceiver.DrawPreview(new Rect(x, y, 430f, 240f));
            y += 255f;
            cqfReceiver.DrawPathField(ref y, x, 430f);
            cqfReceiver.DrawPercentField(ref y, x);
            cqfReceiver.DrawScopeField(ref y, x);
            if (cqfReceiver.DrawOnCameraVisibleArea)
            {
                cqfReceiver.DrawFitModeField(ref y, x);
                if (cqfReceiver.fitMode == CustomMapBackgroundFitMode.Tile)
                {
                    cqfReceiver.DrawScaleField(ref y, x);
                }
            }
            else
            {
                cqfReceiver.DrawVector2(ref y, "CQF_MapBackgroundDrawSize".Translate(), ref cqfReceiver.drawSize, ref cqfReceiver.bufferDrawSizeX, ref cqfReceiver.bufferDrawSizeY, x);
            }

            cqfReceiver.DrawVector2(ref y, "CQF_MapBackgroundOffset".Translate(), ref cqfReceiver.offset, ref cqfReceiver.bufferOffsetX, ref cqfReceiver.bufferOffsetY, x);
            Rect backgroundMapRect = new Rect(x, y, 430f, 25f);
            Widgets.CheckboxLabeled(backgroundMapRect, "CQF_MapBackgroundIsBackgroundMap".Translate(), ref cqfReceiver.enableTerrainEdges);
            TooltipHandler.TipRegion(backgroundMapRect, "CQF_MapBackgroundIsBackgroundMapTip".Translate());
            y += 35f;
            CQFEditorTools.DrawSelectColorButtons(ref y, "CQF_MapBackgroundColor".Translate(), cqfReceiver.color, c => cqfReceiver.color = c, x + 120f);
        }

        public static void DrawPathField_1(QuestEditor_Library.CustomMapBackgroundData cqfReceiver, ref float y, float x, float width)
        {
            Rect labelRect = new Rect(x, y, 120f, 25f);
            if (CQFUIStyle.ButtonText(labelRect, "CQF_MapBackgroundTexturePath".Translate(), false))
            {
                Find.WindowStack.Add(new Dialog_SelectMapBackgroundImage(path => cqfReceiver.texPath = path, cqfReceiver.texPath));
            }

            cqfReceiver.texPath = Widgets.TextField(new Rect(x + 125f, y, width - 125f, 25f), cqfReceiver.texPath);
            y += 35f;
        }

        public static void DrawPercentField_2(QuestEditor_Library.CustomMapBackgroundData cqfReceiver, ref float y, float x)
        {
            Widgets.Label(new Rect(x, y, 120f, 25f), "CQF_MapBackgroundAlpha".Translate());
            Widgets.TextFieldPercent(new Rect(x + 125f, y, 70f, 25f), ref cqfReceiver.alpha, ref cqfReceiver.bufferAlpha);
            y += 35f;
        }

        public static void DrawScopeField_3(QuestEditor_Library.CustomMapBackgroundData cqfReceiver, ref float y, float x)
        {
            Widgets.Label(new Rect(x, y, 120f, 25f), "CQF_MapBackgroundDrawScope".Translate());
            if (CQFUIStyle.ButtonText(new Rect(x + 125f, y, 160f, 25f), cqfReceiver.DrawScopeLabel, false))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> { new FloatMenuOption("CQF_MapBackgroundDrawScope_Map".Translate(), () => cqfReceiver.drawScope = CustomMapBackgroundDrawScope.Map), new FloatMenuOption("CQF_MapBackgroundDrawScope_CameraVisible".Translate(), () => cqfReceiver.drawScope = CustomMapBackgroundDrawScope.CameraVisible) }));
            }

            y += 35f;
        }

        public static void DrawFitModeField_4(QuestEditor_Library.CustomMapBackgroundData cqfReceiver, ref float y, float x)
        {
            Widgets.Label(new Rect(x, y, 120f, 25f), "CQF_MapBackgroundFitMode".Translate());
            if (CQFUIStyle.ButtonText(new Rect(x + 125f, y, 160f, 25f), cqfReceiver.FitModeLabel, false))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> { new FloatMenuOption("CQF_MapBackgroundFitMode_Tile".Translate(), () => cqfReceiver.fitMode = CustomMapBackgroundFitMode.Tile), new FloatMenuOption("CQF_MapBackgroundFitMode_Stretch".Translate(), () => cqfReceiver.fitMode = CustomMapBackgroundFitMode.Stretch), new FloatMenuOption("CQF_MapBackgroundFitMode_Cover".Translate(), () => cqfReceiver.fitMode = CustomMapBackgroundFitMode.Cover) }));
            }

            y += 35f;
        }

        public static void DrawScaleField_5(QuestEditor_Library.CustomMapBackgroundData cqfReceiver, ref float y, float x)
        {
            Widgets.Label(new Rect(x, y, 120f, 25f), "CQF_MapBackgroundScale".Translate());
            Widgets.TextFieldNumeric(new Rect(x + 125f, y, 70f, 25f), ref cqfReceiver.scale, ref cqfReceiver.bufferScale, 0.01f);
            y += 35f;
        }

        public static void DrawVector2_6(QuestEditor_Library.CustomMapBackgroundData cqfReceiver, ref float y, string label, ref Vector2 vector, ref string bufferX, ref string bufferY, float x)
        {
            Widgets.Label(new Rect(x, y, 120f, 25f), label);
            Rect rect = new Rect(x + 125f, y, 70f, 25f);
            float xValue = vector.x;
            float yValue = vector.y;
            Widgets.TextFieldNumeric(rect, ref xValue, ref bufferX);
            rect.x += 80f;
            Widgets.TextFieldNumeric(rect, ref yValue, ref bufferY);
            vector = new Vector2(xValue, yValue);
            y += 35f;
        }
    }
}
