using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class DialogElement_Image(Texture2D texture, float scale) : IDialogElement
{
    public string GetText()
    {
        return "DialogImage_Element".Translate();
    }

    public virtual void Draw(ref float y, Rect inRect)
    {
        if (this.texture != null)
        {
            float width = inRect.width * this.scale;
            float height = width / this.texture.width * this.texture.height;
            Widgets.DrawTextureFitted(new Rect(inRect.x, y, width, height), this.texture, 1f);
            y += height + 5f;
            return;
        }
        y += 30f;
    }

    public Texture2D texture = texture;
    public float scale = scale;
}
