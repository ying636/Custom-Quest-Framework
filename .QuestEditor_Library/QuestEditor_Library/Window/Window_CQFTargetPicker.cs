using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Window_CQFTargetPicker : Window
    {
        public Window_CQFTargetPicker(Map map, Thing source, Action<string> assign)
        {
            this.map = map;
            this.source = source;
            this.assign = assign;
            this.entries = CQFTargetSelectionSession.GetAvailableTargets(map);
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new Vector2(700f, 620f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 35f, 35f), "CQF_TargetPickerTitle".Translate());
            Text.Font = GameFont.Small;
            this.search = Widgets.TextField(new Rect(0f, 42f, inRect.width - 190f, 30f), this.search);
            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && this.map != null;
            if (Widgets.ButtonText(new Rect(inRect.width - 182f, 42f, 182f, 30f), "CQF_TargetPickOnMap".Translate()))
            {
                this.Close(false);
                new CQFTargetSelectionSession(this.map, this.source, this.assign).Begin();
            }
            GUI.enabled = oldEnabled;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 18f, this.height);
            Widgets.BeginScrollView(new Rect(0f, 82f, inRect.width, inRect.height - 82f), ref this.scroll, viewRect);
            float y = 0f;
            Widgets.Label(new Rect(0f, y, viewRect.width, 28f), "CQF_TargetContextEntries".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 30f;
            foreach (string key in CQFEditorTools.TargetTexts)
            {
                string label = key.Translate() + " [" + key + "]";
                if (!this.Matches(label))
                {
                    continue;
                }
                TargetInfo target = key == "CustomThing" && this.source != null ? new TargetInfo(this.source) : TargetInfo.Invalid;
                this.DrawEntry(ref y, viewRect.width, key, label, target);
            }
            y += 10f;
            Widgets.Label(new Rect(0f, y, viewRect.width, 28f), "CQF_TargetMapEntries".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 30f;
            foreach (TargetWithKey entry in this.entries)
            {
                string label = entry.key + " — " + entry.target.Label;
                if (this.Matches(label))
                {
                    this.DrawEntry(ref y, viewRect.width, entry.key, label, entry.target, true);
                }
            }
            this.height = y + 8f;
            Widgets.EndScrollView();
        }

        private void DrawEntry(ref float y, float width, string key, string label, TargetInfo target, bool register = false)
        {
            if (Widgets.ButtonText(new Rect(0f, y, width - 115f, 28f), label, false))
            {
                if (register && CQFTargetSelectionSession.CanPersistThing(target.Thing) &&
                    !target.Map.GetComponent<MapComponent_CQFTargets>().TryRegister(key, target.Thing))
                {
                    return;
                }
                this.assign(key);
                this.Close();
            }
            if (target.IsValid && target.Map != null)
            {
                Widgets.Label(new Rect(10f, y + 28f, width - 125f, 25f), target.Cell.ToString().Colorize(Color.gray));
                if (Widgets.ButtonText(new Rect(width - 110f, y + 10f, 110f, 28f), "CQF_TargetLocate".Translate()))
                {
                    CQFTargetSelectionSession.Locate(target);
                }
                y += 56f;
            }
            else
            {
                y += 34f;
            }
        }

        private bool Matches(string text)
        {
            return this.search.NullOrEmpty() || text.IndexOf(this.search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private readonly Map map;
        private readonly Thing source;
        private readonly Action<string> assign;
        private readonly List<TargetWithKey> entries;
        private string search = string.Empty;
        private Vector2 scroll;
        private float height;
    }
}
