using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_NameAndBodyEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_NameAndBody cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            PawnModData_NameAndBody data = pawnDef.DataFor<PawnModData_NameAndBody>();
            Rect row = cqfReceiver.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_Name".Translate(), 140f);
            float nameWidth = Mathf.Min(150f, (row.width - 20f) / 3f);
            data.firstName = Widgets.TextField(new Rect(row.x, row.y, nameWidth, 30f), data.firstName);
            data.nickName = Widgets.TextField(new Rect(row.x + nameWidth + 10f, row.y, nameWidth, 30f), data.nickName);
            data.lastName = Widgets.TextField(new Rect(row.x + (nameWidth + 10f) * 2f, row.y, nameWidth, 30f), data.lastName);
            cqfReceiver.EndRow(ref y);
            Rect buttonRect = cqfReceiver.DrawRowLabel(ref y, inRect, x, "", 140f);
            if (cqfReceiver.DrawCommandText(new Rect(buttonRect.x, buttonRect.y, 180f, 30f), "CQF_PawnEditor_RandomizeName".Translate()))
            {
                cqfReceiver.RandomizeDefName(pawnDef, data);
            }

            cqfReceiver.EndRow(ref y);
            Widgets.CheckboxLabeled(new Rect(x, y, 260f, 30f), "CQF_PawnEditor_RandomNameOnGeneration".Translate(), ref data.randomName);
            cqfReceiver.EndRow(ref y);
            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_NameMaker".Translate(cqfReceiver.ValueOrNone(cqfReceiver.NameMakerLabel(data.nameMaker)))))
            {
                cqfReceiver.OpenNameMakerSelector(pawnDef, data);
            }

            row = cqfReceiver.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_BioAge".Translate(), 140f);
            Widgets.TextFieldNumeric(new Rect(row.x, row.y, 120f, 30f), ref data.bioAge, ref cqfReceiver.bioAgeBuffer);
            cqfReceiver.EndRow(ref y);
            row = cqfReceiver.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_ChronologicalAge".Translate(), 140f);
            Widgets.TextFieldNumeric(new Rect(row.x, row.y, 120f, 30f), ref data.chrAge, ref cqfReceiver.chrAgeBuffer);
            cqfReceiver.EndRow(ref y);
            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_Gender".Translate(data.gender.ToString().Translate())))
            {
                CQFEditorTools.DrawFloatMenu(new List<Gender> { Gender.None, Gender.Male, Gender.Female }, gender => data.gender = gender, gender => gender.ToString().Translate());
            }
        }
    }
}
