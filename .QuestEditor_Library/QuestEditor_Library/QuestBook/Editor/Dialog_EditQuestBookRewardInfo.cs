using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditQuestBookRewardInfo : Window
    {
        public Dialog_EditQuestBookRewardInfo(QuestBookRewardInfo info)
        {
            this.info = info ?? new QuestBookRewardInfo();
            if (this.info.labelKey.CanTranslate())
            {
                this.info.labelKey = this.info.labelKey.Translate().ToString();
            }
            if (this.info.descriptionKey.CanTranslate())
            {
                this.info.descriptionKey = this.info.descriptionKey.Translate().ToString();
            }
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new Vector2(560f, 300f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            float x = 12f;
            float y = 8f;
            float width = inRect.width - 24f;
            Widgets.Label(new Rect(x, y, width, 28f), "CQF_QuestBook_RewardInfoEditor".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 38f;
            Rect iconRect = new Rect(x, y, 82f, 82f);
            Widgets.DrawBox(iconRect, 1);
            DrawIcon(iconRect.ContractedBy(10f));
            if (Widgets.ButtonImage(new Rect(iconRect.x, iconRect.yMax + 6f, 26f, 26f), TexButton.Delete))
            {
                info.iconThing = null;
                info.iconPath = null;
            }
            TooltipHandler.TipRegion(new Rect(iconRect.x, iconRect.yMax + 6f, 26f, 26f), "CQF_QuestBook_Clear".Translate());
            float fieldX = iconRect.xMax + 16f;
            float fieldWidth = width - fieldX + x;
            Widgets.Label(new Rect(fieldX, y, fieldWidth, 20f), "CQF_QuestBook_RewardInfoName".Translate().Colorize(ColorLibrary.PaleBlue));
            info.labelKey = Widgets.TextField(new Rect(fieldX, y + 22f, fieldWidth, 26f), info.labelKey ?? string.Empty);
            Widgets.Label(new Rect(fieldX, y + 56f, fieldWidth, 20f), "CQF_QuestBook_RewardInfoDescription".Translate().Colorize(ColorLibrary.PaleBlue));
            info.descriptionKey = Widgets.TextField(new Rect(fieldX, y + 78f, fieldWidth, 26f), info.descriptionKey ?? string.Empty);
            float buttonY = y + 116f;
            float buttonWidth = (fieldWidth - 8f) * 0.5f;
            if (Widgets.ButtonText(new Rect(fieldX, buttonY, buttonWidth, 28f), "CQF_QuestBook_SelectThingIcon".Translate(), false))
            {
                SelectThingIcon();
            }
            if (Widgets.ButtonText(new Rect(fieldX + buttonWidth + 8f, buttonY, buttonWidth, 28f), "CQF_QuestBook_SelectImageIcon".Translate(), false))
            {
                SelectImageIcon();
            }
        }

        private void DrawIcon(Rect rect)
        {
            if (info.iconThing != null)
            {
                Widgets.DefIcon(rect, info.iconThing);
                return;
            }
            Texture2D texture = info.iconPath.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(info.iconPath, false);
            if (texture != null)
            {
                Widgets.DrawTextureFitted(rect, texture, 1f);
            }
        }

        private void SelectThingIcon()
        {
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.uiIcon != null && !def.uiIcon.NullOrBad() && def.uiIcon != BaseContent.PlaceholderImage
                    && def.category != ThingCategory.Mote && def.mote == null && def.projectile == null
                    && def.skyfaller == null && def.pawnFlyer == null && def.gas == null && def.filth == null
                    && def.thingClass != null && !typeof(Mote).IsAssignableFrom(def.thingClass))
                .OrderBy(def => def.label)
                .ToList();
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(new LabeledTextureSelectDrawer<ThingDef>(
                defs, def => def.uiIcon, def => def.label,
                selected =>
                {
                    info.iconThing = selected;
                    info.iconPath = null;
                }, null, (def, rect) => Widgets.DrawTextureFitted(rect, def.uiIcon, 1f)), "CQF_QuestBook_SelectThingIcon".Translate()));
        }

        private void SelectImageIcon()
        {
            Find.WindowStack.Add(new Dialog_SelectDialogImage(path =>
            {
                info.iconPath = path;
                info.iconThing = null;
            }, info.iconPath));
        }

        private readonly QuestBookRewardInfo info;
    }
}
