using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Basic : PawnModWorker
    {
        public override PawnModData CreateData()
        {
            return new PawnModData_Basic();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                pawnDef,
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Basic.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
        }
        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            PawnModData_Basic data = pawnDef.DataFor<PawnModData_Basic>();
            data.unique = ParseHelper.FromString<bool>(node["unique"]?.InnerText ?? "false");
            data.kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(node["kindDef"]?.InnerText);
            data.faction = DefDatabase<FactionDef>.GetNamedSilentFail(node["faction"]?.InnerText);
        }
        internal void OpenPawnKindSelector(Action<PawnKindDef> action)
        {
            List<PawnKindDef> kinds = DefDatabase<PawnKindDef>.AllDefsListForReading;
            Find.WindowStack.Add(new Dialog_Select<PawnKindDef>(
                new TextSelectDrawer<PawnKindDef>(
                    kinds,
                    kind => kind.label,
                    action,
                    null,
                    null,
                    null,
                    kind => kind.defName,
                    null,
                    this.MakeFleshTypeFilters(kinds),
                    this.MakeFleshTypeTips(kinds)),
                "CQF_PawnEditor_SelectPawnKind".Translate()));
        }

        private Dictionary<string, Func<PawnKindDef, bool>> MakeFleshTypeFilters(List<PawnKindDef> kinds)
        {
            Dictionary<string, Func<PawnKindDef, bool>> result = new Dictionary<string, Func<PawnKindDef, bool>>();
            foreach (FleshTypeDef fleshType in kinds.Select(this.FleshTypeFor).Where(type => type != null).Distinct())
            {
                FleshTypeDef capturedType = fleshType;
                string filterLabel = this.FleshTypeLabel(capturedType);
                result[filterLabel] = kind => this.FleshTypeFor(kind) == capturedType;
            }
            return result;
        }

        private Dictionary<string, string> MakeFleshTypeTips(List<PawnKindDef> kinds)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (FleshTypeDef fleshType in kinds.Select(this.FleshTypeFor).Where(type => type != null).Distinct())
            {
                string filterLabel = this.FleshTypeLabel(fleshType);
                result[filterLabel] = fleshType.description;
            }
            return result;
        }

        private string FleshTypeLabel(FleshTypeDef fleshType)
        {
            if (!fleshType.LabelCap.NullOrEmpty())
            {
                return fleshType.LabelCap;
            }
            string key = "CQF_PawnEditor_FleshType_" + fleshType.defName;
            return key.CanTranslate() ? key.Translate() : fleshType.defName;
        }

        private FleshTypeDef FleshTypeFor(PawnKindDef kind)
        {
            return kind?.RaceProps?.FleshType;
        }
    }
}
