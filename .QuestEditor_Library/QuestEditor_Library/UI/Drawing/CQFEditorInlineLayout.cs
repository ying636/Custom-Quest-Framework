using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFEditorInlineLayout
    {
        public static void Draw(object owner, ref float y, float x, float width, Func<Rect, float> draw)
        {
            CQFEditorInlineLayout layout = layouts.GetValue(owner, key => new CQFEditorInlineLayout());
            layout.DrawContents(ref y, x, width, draw);
        }

        private void DrawContents(ref float y, float x, float width, Func<Rect, float> draw)
        {
            width = Mathf.Max(120f, width);
            float contentWidth = width;
            float allocatedHeight = this.height;
            Rect outRect = new Rect(x, y, width, allocatedHeight);
            Rect viewRect = new Rect(0f, 0f, contentWidth, this.height);
            Widgets.BeginGroup(outRect);
            using CQFUIScope contentScope = new CQFUIScope(viewRect.width);
            try
            {
                this.height = Mathf.Max(1f, draw(viewRect));
            }
            finally
            {
                Widgets.EndGroup();
            }

            y += allocatedHeight;
        }

        private float height = 64f;
        private static readonly ConditionalWeakTable<object, CQFEditorInlineLayout> layouts = new ConditionalWeakTable<object, CQFEditorInlineLayout>();
    }
}
