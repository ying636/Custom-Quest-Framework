using System.Xml.Linq;
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
            offset = this.offset
        };
    }

    public void Draw(ref float y, Rect inRect, float x)
    {
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MapBackgroundTexturePath".Translate(), ref this.texPath, x, 360f);
        y += 30f;
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MapBackgroundAlpha".Translate(), ref this.alpha, ref this.bufferAlpha, x, 60f);
        y += 30f;
        this.DrawVector2(ref y, "CQF_MapBackgroundDrawSize".Translate(), ref this.drawSize, ref this.bufferDrawSizeX, ref this.bufferDrawSizeY, x);
        this.DrawVector2(ref y, "CQF_MapBackgroundOffset".Translate(), ref this.offset, ref this.bufferOffsetX, ref this.bufferOffsetY, x);
        if (Widgets.ButtonText(new Rect(x, y, 200f, 25f), "CQF_MapBackgroundColor".Translate(), false))
        {
            Find.WindowStack.Add(new Dialog_RGB(this.color, c => this.color = c));
        }
        y += 30f;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref this.texPath, "texPath");
        Scribe_Values.Look(ref this.color, "color", Color.white);
        Scribe_Values.Look(ref this.alpha, "alpha", 1f);
        Scribe_Values.Look(ref this.drawSize, "drawSize", Vector2.zero);
        Scribe_Values.Look(ref this.offset, "offset", Vector2.zero);
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
        return result;
    }

    private void DrawVector2(ref float y, string label, ref Vector2 vector, ref string bufferX, ref string bufferY, float x)
    {
        Widgets.Label(new Rect(x, y, 350f, 25f), label);
        Rect rect = new Rect(Text.CalcSize(label).x + x + 5f, y, 60f, 25f);
        float xValue = vector.x;
        float yValue = vector.y;
        Widgets.TextFieldNumeric(rect, ref xValue, ref bufferX);
        rect.x += 70f;
        Widgets.TextFieldNumeric(rect, ref yValue, ref bufferY);
        vector = new Vector2(xValue, yValue);
        y += 30f;
    }

    public string texPath = "UI/Null";
    public Color color = Color.white;
    public float alpha = 1f;
    public Vector2 drawSize = Vector2.zero;
    public Vector2 offset = Vector2.zero;

    private string bufferAlpha;
    private string bufferDrawSizeX;
    private string bufferDrawSizeY;
    private string bufferOffsetX;
    private string bufferOffsetY;
}
