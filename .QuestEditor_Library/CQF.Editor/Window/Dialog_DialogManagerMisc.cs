using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_DialogManagerMisc : Window
    {
        public Dialog_DialogManagerMisc(DialogManagerDef manager)
        {
            this.manager = manager;
            this.doCloseX = true;
            this.draggable = true;
            this.resizeable = true;
        }

        public override Vector2 InitialSize => new Vector2(660f, 720f);

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width, inRect.height);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 36f), "Misc".Translate().Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            Rect viewport = new Rect(0f, 46f, inRect.width, inRect.height - 46f);
            float width = viewport.width - 20f;
            float y = 0f;
            Widgets.BeginScrollView(viewport, ref this.pos, new Rect(0f, 0f, width, Mathf.Max(viewport.height, this.height)));
            try
            {
                Widgets.Label(new Rect(0f, y, width - 38f, 28f), "Tags".Translate().Colorize(CQFUIStyle.Accent));
                if (CQFUIStyle.ButtonImage(new Rect(width - 28f, y, 28f, 28f), TexButton.Plus, tooltip: "Add".Translate())) this.manager.tags.Add(string.Empty);
                y += 36f;
                for (int i = 0; i < this.manager.tags.Count; i++)
                {
                    this.manager.tags[i] = Widgets.TextField(new Rect(0f, y, width - 38f, 32f), this.manager.tags[i] ?? string.Empty);
                    if (CQFUIStyle.ButtonImage(new Rect(width - 28f, y + 2f, 28f, 28f), TexButton.Delete, tooltip: "Delete".Translate())) this.manager.tags.RemoveAt(i--);
                    y += 40f;
                }
                y += 12f;
                CQFEditorInlineLayout.Draw(this.manager.forcedTraits, ref y, 0f, width, rect =>
                {
                    float traitsY = 0f;
                    TraitData.DrawList(this.manager.forcedTraits, ref traitsY, "ForcedTraits".Translate(), defaultWidth: rect.width - 12f, x: 0f);
                    return traitsY + 8f;
                });
                y += 12f;
                Rect color = new Rect(0f, y, width - 44f, 32f);
                if (CQFUIStyle.ButtonText(color, "QuestIconColor".Translate(), overrideTextAnchor: TextAnchor.MiddleLeft))
                {
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                    {
                        new FloatMenuOption("Colorbase".Translate(), () => Find.WindowStack.Add(new Dialog_ChooseColor("Select".Translate(), this.manager.iconColor, DefDatabase<ColorDef>.AllDefsListForReading.Select(def => def.color).ToList(), value => this.manager.iconColor = value))),
                        new FloatMenuOption("Hex".Translate(), () => Find.WindowStack.Add(new Dialog_RGB(this.manager.iconColor, value => this.manager.iconColor = value)))
                    }));
                }
                Widgets.ColorBox(new Rect(width - 32f, y, 32f, 32f), ref this.manager.iconColor, this.manager.iconColor);
                y += 48f;
                CQFEditorTools.DrawIDrawList_UseWindow(ref y, 0f, this.manager.genrationConditions, new Rect(0f, 0f, width, viewport.height), "genrationConditions".Translate(), condition => condition.GetType().Name.Translate());
                y += 12f;
                Widgets.CheckboxLabeled(new Rect(0f, y, width, 32f), "RemoveWhenThingDespawned".Translate(), ref this.manager.removeWhenThingDespawned);
                y += 40f;
                Widgets.CheckboxLabeled(new Rect(0f, y, width, 32f), "RemoveWhenPawnDied".Translate(), ref this.manager.removeWhenPawnDied);
                this.height = y + 44f;
            }
            finally { Widgets.EndScrollView(); }
        }

        public string buffer;
        public float height;
        public DialogManagerDef manager;
        public Vector2 pos = Vector2.zero;
    }
}
