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

    public CustomMapBackgroundData Copy()
    {
        return new CustomMapBackgroundData()
        {
            texPath = this.texPath,
            color = this.color,
            alpha = this.alpha,
            drawSize = this.drawSize,
            offset = this.offset,
            enableTerrainEdges = this.enableTerrainEdges,
            backgroundEffects = this.backgroundEffects.ListFullCopy()
        };
    }

    public void Draw(ref float y, Rect inRect, float x)
    {
        Widgets.Label(new Rect(x, y, inRect.width - 40f, 30f), "CustomMapStep_MapBackground".Translate().Colorize(ColorLibrary.SkyBlue));
        y += 35f;
        this.DrawPreview(new Rect(x, y, 430f, 240f));
        y += 255f;
        this.DrawPathField(ref y, x, 430f);
        this.DrawPercentField(ref y, x);
        this.DrawVector2(ref y, "CQF_MapBackgroundDrawSize".Translate(), ref this.drawSize, ref this.bufferDrawSizeX, ref this.bufferDrawSizeY, x);
        this.DrawVector2(ref y, "CQF_MapBackgroundOffset".Translate(), ref this.offset, ref this.bufferOffsetX, ref this.bufferOffsetY, x);
        Rect backgroundMapRect = new Rect(x, y, 430f, 25f);
        Widgets.CheckboxLabeled(backgroundMapRect, "CQF_MapBackgroundIsBackgroundMap".Translate(), ref this.enableTerrainEdges);
        TooltipHandler.TipRegion(backgroundMapRect, "CQF_MapBackgroundIsBackgroundMapTip".Translate());
        y += 35f;
        CQFEditorTools.DrawSelectColorButtons(ref y, "CQF_MapBackgroundColor".Translate(), this.color, c => this.color = c, x + 120f);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref this.texPath, "texPath");
        Scribe_Values.Look(ref this.color, "color", Color.white);
        Scribe_Values.Look(ref this.alpha, "alpha", 1f);
        Scribe_Values.Look(ref this.drawSize, "drawSize", Vector2.zero);
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
        if (this.drawSize != Vector2.zero)
        {
            result.Add(new XElement("drawSize", this.drawSize));
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

    private void DrawPathField(ref float y, float x, float width)
    {
        Rect labelRect = new Rect(x, y, 120f, 25f);
        if (Widgets.ButtonText(labelRect, "CQF_MapBackgroundTexturePath".Translate(), false))
        {
            Find.WindowStack.Add(new Dialog_SelectMapBackgroundImage(path => this.texPath = path, this.texPath));
        }
        this.texPath = Widgets.TextField(new Rect(x + 125f, y, width - 125f, 25f), this.texPath);
        y += 35f;
    }

    private void DrawPercentField(ref float y, float x)
    {
        Widgets.Label(new Rect(x, y, 120f, 25f), "CQF_MapBackgroundAlpha".Translate());
        Widgets.TextFieldPercent(new Rect(x + 125f, y, 70f, 25f), ref this.alpha, ref this.bufferAlpha);
        y += 35f;
    }

    private void DrawPreview(Rect rect)
    {
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 25f), "CQF_MapBackgroundPreview".Translate().Colorize(ColorLibrary.SkyBlue));
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
        Widgets.DrawBox(imageRect);
    }

    private void DrawVector2(ref float y, string label, ref Vector2 vector, ref string bufferX, ref string bufferY, float x)
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

    public string texPath = "UI/Null";
    public Color color = Color.white;
    public float alpha = 1f;
    public Vector2 drawSize = Vector2.zero;
    public Vector2 offset = Vector2.zero;
    public bool enableTerrainEdges; 
    public List<CustomMapBackgroundEffectDef> backgroundEffects = new List<CustomMapBackgroundEffectDef>();

    private string bufferAlpha;
    private string bufferDrawSizeX;
    private string bufferDrawSizeY;
    private string bufferOffsetX;
    private string bufferOffsetY;
}
