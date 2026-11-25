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
        if (this.texture != null && this.texture.width > 0 && this.texture.height > 0 && this.scale > 0f)
        {
            float maxWidth = inRect.width * Mathf.Min(1f, this.scale);
            float maxHeight = inRect.height > 0f ? Mathf.Min(320f, inRect.height * .75f) : 320f;
            float ratio = Mathf.Min(maxWidth / this.texture.width, maxHeight / this.texture.height);
            float width = this.texture.width * ratio;
            float height = this.texture.height * ratio;
            Widgets.DrawTextureFitted(new Rect(inRect.center.x - width / 2f, y, width, height), this.texture, 1f);
            y += height;
        }
    }

    public Texture2D texture = texture;
    public float scale = scale;
}
