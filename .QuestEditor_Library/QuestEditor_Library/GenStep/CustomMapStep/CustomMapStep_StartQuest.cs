using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class CustomMapStep_StartQuest : CustomMapStep
{
    public override void Draw(ref float y, Rect inRect, float x)
    {
        float width = inRect.width - x - 12f;
        Widgets.Label(new Rect(x, y, width, 30f), "CustomMapStep_StartQuest".Translate().Colorize(ColorLibrary.SkyBlue));
        y += 35f;
        if (Widgets.ButtonText(new Rect(x, y, width, 30f),
                this.quest?.label ?? this.quest?.defName ?? "CQF_NotSelected".Translate(), false))
        {
            CQFEditorTools.DrawFloatMenu(DefDatabase<QuestScriptDef>.AllDefsListForReading,
                q => this.quest = q, q => q.label ?? q.defName);
        }
        y += 35f;
        Rect letterRect = new Rect(x, y, width, 30f);
        Widgets.DrawHighlightIfMouseover(letterRect);
        Widgets.CheckboxLabeled(letterRect.ContractedBy(6f, 2f), "CQF_StartQuest_SendLetter".Translate(), ref this.sendAvailableLetter);
        y += 35f;
    }

    public override void Generate(Map map, CustomMapDataDef def, CustomSitePartParams param)
    {
        if (this.quest == null)
        {
            return;
        }

        float points = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.GiveQuest, map).points;
        Quest generatedQuest = QuestUtility.GenerateQuestAndMakeAvailable(this.quest, points);
        if (generatedQuest == null)
        {
            return;
        }

        param.quest = generatedQuest;
        if (this.sendAvailableLetter && !generatedQuest.hidden && this.quest.sendAvailableLetter)
        {
            QuestUtility.SendLetterQuestAvailable(generatedQuest);
        }
    }

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        if (this.quest != null)
        {
            result.Add(new XElement("quest", this.quest.defName));
        }
        result.Add(new XElement("sendAvailableLetter", this.sendAvailableLetter));
        return result;
    }

    public QuestScriptDef quest;
    public bool sendAvailableLetter = true;
}
