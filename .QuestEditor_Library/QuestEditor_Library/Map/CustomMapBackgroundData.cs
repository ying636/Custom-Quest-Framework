using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

    public class CustomMapBackgroundData : IExposable, ISaveable, IDrawable
{
    public CustomMapBackgroundData()
    {
    }

    public bool Enabled => !this.texPath.NullOrEmpty();

    public bool DrawOnMap => this.drawScope == CustomMapBackgroundDrawScope.Map;

    public bool DrawOnCameraVisibleArea => this.drawScope == CustomMapBackgroundDrawScope.CameraVisible;

    public CustomMapBackgroundData Copy()
    {
        return new CustomMapBackgroundData()
        {
            texPath = this.texPath,
            color = this.color,
            alpha = this.alpha,
            drawScope = this.drawScope,
            fitMode = this.fitMode,
            drawSize = this.drawSize,
            scale = this.scale,
            offset = this.offset,
            enableTerrainEdges = this.enableTerrainEdges,
            backgroundEffects = this.backgroundEffects.ListFullCopy()
        };
    }

    public void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapBackgroundData.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
    public void ExposeData()
    {
        Scribe_Values.Look(ref this.texPath, "texPath");
        Scribe_Values.Look(ref this.color, "color", Color.white);
        Scribe_Values.Look(ref this.alpha, "alpha", 1f);
        Scribe_Values.Look(ref this.drawScope, "drawScope", CustomMapBackgroundDrawScope.Map);
        Scribe_Values.Look(ref this.fitMode, "fitMode", CustomMapBackgroundFitMode.Tile);
        Scribe_Values.Look(ref this.drawSize, "drawSize", Vector2.zero);
        Scribe_Values.Look(ref this.scale, "scale", 1f);
        Scribe_Values.Look(ref this.offset, "offset", Vector2.zero);
        Scribe_Values.Look(ref this.enableTerrainEdges, "enableTerrainEdges");
        Scribe_Collections.Look(ref this.backgroundEffects, "backgroundEffects", LookMode.Def);
        this.backgroundEffects ??= new List<CustomMapBackgroundEffectDef>();
    }

    public XElement SaveToXElement(string nodeName)
    {
        XElement result = new XElement(nodeName);
        if (!this.texPath.NullOrEmpty())
        {
            result.Add(new XElement("texPath", this.texPath));
        }
        result.Add(new XElement("color", this.color));
        result.Add(new XElement("alpha", this.alpha));
        if (this.drawScope != CustomMapBackgroundDrawScope.Map)
        {
            result.Add(new XElement("drawScope", this.drawScope));
        }
        if (this.fitMode != CustomMapBackgroundFitMode.Tile)
        {
            result.Add(new XElement("fitMode", this.fitMode));
        }
        if (this.drawSize != Vector2.zero)
        {
            result.Add(new XElement("drawSize", this.drawSize));
        }
        if (this.scale != 1f)
        {
            result.Add(new XElement("scale", this.scale));
        }
        if (this.offset != Vector2.zero)
        {
            result.Add(new XElement("offset", this.offset));
        }
        if (this.enableTerrainEdges)
        {
            result.Add(new XElement("enableTerrainEdges", this.enableTerrainEdges));
        }
        return result;
    }
        internal void DrawPathField(ref float y, float x, float width)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapBackgroundData.DrawPathField(Ref:float,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawPercentField(ref float y, float x)

        {
            object[] arguments = new object[]
            {
                y,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapBackgroundData.DrawPercentField(Ref:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawScopeField(ref float y, float x)

        {
            object[] arguments = new object[]
            {
                y,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapBackgroundData.DrawScopeField(Ref:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawFitModeField(ref float y, float x)

        {
            object[] arguments = new object[]
            {
                y,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapBackgroundData.DrawFitModeField(Ref:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawPreview(Rect rect)
    {
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 25f), "CQF_MapBackgroundPreview".Translate().Colorize(CQFUIStyle.Accent));
        Rect imageRect = new Rect(rect.x, rect.y + 30f, rect.width, rect.height - 30f);
        Widgets.DrawBoxSolid(imageRect, Color.black);
        Texture2D texture = this.texPath.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(this.texPath, false);
        if (texture != null)
        {
            Color oldColor = GUI.color;
            GUI.color = this.color;
            GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, GUI.color.a * this.alpha);
            Widgets.DrawTextureFitted(imageRect.ContractedBy(4f), texture, 1f);
            GUI.color = oldColor;
        }
        else
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(imageRect, "CQF_MapBackgroundNoPreview".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }
        CQFUIStyle.DrawBox(imageRect);
    }
        internal void DrawScaleField(ref float y, float x)

        {
            object[] arguments = new object[]
            {
                y,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapBackgroundData.DrawScaleField(Ref:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawVector2(ref float y, string label, ref Vector2 vector, ref string bufferX, ref string bufferY, float x)

        {
            object[] arguments = new object[]
            {
                y,
                label,
                vector,
                bufferX,
                bufferY,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapBackgroundData.DrawVector2(Ref:float,None:string,Ref:UnityEngine.Vector2,Ref:string,Ref:string,None:float)", this, arguments);
            y = (float)arguments[0];
            vector = (UnityEngine.Vector2)arguments[2];
            bufferX = (string)arguments[3];
            bufferY = (string)arguments[4];
        }
        internal string DrawScopeLabel
    {
        get
        {
            switch (this.drawScope)
            {
                case CustomMapBackgroundDrawScope.CameraVisible:
                    return "CQF_MapBackgroundDrawScope_CameraVisible".Translate();
                default:
                    return "CQF_MapBackgroundDrawScope_Map".Translate();
            }
        }
    }
        internal string FitModeLabel
    {
        get
        {
            switch (this.fitMode)
            {
                case CustomMapBackgroundFitMode.Stretch:
                    return "CQF_MapBackgroundFitMode_Stretch".Translate();
                case CustomMapBackgroundFitMode.Cover:
                    return "CQF_MapBackgroundFitMode_Cover".Translate();
                default:
                    return "CQF_MapBackgroundFitMode_Tile".Translate();
            }
        }
    }

    public string texPath = "UI/Null";
    public Color color = Color.white;
    public float alpha = 1f;
    public CustomMapBackgroundDrawScope drawScope = CustomMapBackgroundDrawScope.Map;
    public CustomMapBackgroundFitMode fitMode = CustomMapBackgroundFitMode.Tile;
    public Vector2 drawSize = Vector2.zero;
    public float scale = 1f;
    public Vector2 offset = Vector2.zero;
    public bool enableTerrainEdges;
    public List<CustomMapBackgroundEffectDef> backgroundEffects = new List<CustomMapBackgroundEffectDef>();
        internal string bufferAlpha;
        internal string bufferScale;
        internal string bufferDrawSizeX;
        internal string bufferDrawSizeY;
        internal string bufferOffsetX;
        internal string bufferOffsetY;
}

    public enum CustomMapBackgroundDrawScope
{
    Map,
    CameraVisible
}

    public enum CustomMapBackgroundFitMode
{
    Tile,
    Stretch,
    Cover
}
