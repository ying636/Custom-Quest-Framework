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
            float contentWidth = Mathf.Max(620f, width);
            float allocatedHeight = this.height + (contentWidth > width ? 18f : 0f);
            Rect outRect = new Rect(x, y, width, allocatedHeight);
            Rect viewRect = new Rect(0f, 0f, contentWidth, this.height);
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
            try
            {
                this.height = Mathf.Max(1f, draw(viewRect));
            }
            finally
            {
                Widgets.EndScrollView();
            }
            this.scrollPosition.y = 0f;
            y += allocatedHeight;
        }

        private float height = 64f;
        private Vector2 scrollPosition;
        private static readonly ConditionalWeakTable<object, CQFEditorInlineLayout> layouts = new ConditionalWeakTable<object, CQFEditorInlineLayout>();
    }
}
