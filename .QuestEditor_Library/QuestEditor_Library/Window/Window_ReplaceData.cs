using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Window_ReplaceData : Window
    {
        public Window_ReplaceData(CustomMapDataDef map)
        {
            this.map = map;
            this.doCloseX = true;
        }

        public override Vector2 InitialSize => new Vector2(620f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            const float headerHeight = 42f;
            float contentWidth = inRect.width - 20f;
            Widgets.Label(new Rect(8f, 5f, contentWidth - 124f, 30f),
                "ReplaceDatas".Translate().Colorize(ColorLibrary.PaleBlue));

            Rect saveRect = new Rect(contentWidth - 100f, 2f, 28f, 28f);
            if (Widgets.ButtonImage(saveRect, CQFEditorTools.icon_Save))
            {
                this.SaveAsDef();
            }
            TooltipHandler.TipRegion(saveRect, "SaveAsDef".Translate());

            Rect addRect = new Rect(contentWidth - 64f, 2f, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                this.OpenAddMenu();
            }
            TooltipHandler.TipRegion(addRect, "Add".Translate());

            Rect removeRect = new Rect(contentWidth - 28f, 2f, 28f, 28f);
            if (Widgets.ButtonImage(removeRect, TexButton.Delete))
            {
                CQFEditorTools.DrawFloatMenu(this.map.replaces, data => this.map.replaces.Remove(data), this.GetDataLabel);
            }
            TooltipHandler.TipRegion(removeRect, "Remove".Translate());

            Rect outRect = new Rect(0f, headerHeight, inRect.width, inRect.height - headerHeight);
            Rect viewRect = new Rect(0f, 0f, contentWidth, Mathf.Max(this.height, outRect.height));
            Widgets.BeginScrollView(outRect, ref this.pos, viewRect);
            float y = 0f;
            float sectionHeight = 18f + Math.Max(1, this.map.replaces.Count) * 38f;
            Widgets.DrawMenuSection(new Rect(0f, 0f, contentWidth, sectionHeight));
            if (!this.map.replaces.Any())
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(12f, 10f, contentWidth - 24f, 30f), "-");
                Text.Anchor = TextAnchor.UpperLeft;
            }
            foreach (ReplaceData data in this.map.replaces)
            {
                Rect rowRect = new Rect(10f, y + 9f, contentWidth - 20f, 30f);
                if (Widgets.ButtonText(rowRect, this.GetDataLabel(data), false))
                {
                    if (data is ReplaceData_Def defData)
                    {
                        CQFEditorTools.DrawFloatMenu(DefDatabase<ReplacementDataDef>.AllDefsListForReading,
                            def => defData.def = def, def => def.defName);
                    }
                    else
                    {
                        Find.WindowStack.Add(new Dialog_EditIDrawable(data));
                    }
                }
                TooltipHandler.TipRegion(rowRect, data.GetType().Name.Translate());
                y += 38f;
            }
            Widgets.EndScrollView();
            this.height = sectionHeight + 8f;
        }

        private string GetDataLabel(ReplaceData data)
        {
            return data.DataName.NullOrEmpty() ? data.GetType().Name.Translate() : data.DataName;
        }

        private void OpenAddMenu()
        {
            List<Type> types = typeof(ReplaceData).AllSubclassesNonAbstract().ListFullCopy();
            types.Insert(0, typeof(ReplaceData));
            CQFEditorTools.DrawFloatMenu(types,
                type => this.map.replaces.Add((ReplaceData)Activator.CreateInstance(type)),
                type => type.Name.Translate());
        }

        private void SaveAsDef()
        {
            Find.WindowStack.Add(new Dialog_RenameForQE(name =>
            {
                LongEventHandler.QueueLongEvent(() =>
                {
                    ReplacementDataDef def = new ReplacementDataDef
                    {
                        defName = name,
                        datas = this.map.replaces.ListFullCopy()
                    };
                    DefDatabase<ReplacementDataDef>.Add(def);
                    string path = Path.Combine(Page_QuestEditor.Path, "Data", name + ".xml");
                    XElement defsXml = new XElement("Defs");
                    XElement defXml = new XElement("QuestEditor_Library.ReplacementDataDef");
                    defXml.Add(new XElement("defName", name));
                    defXml.Add(CQFEditorTools.SaveList_Saveable(this.map.replaces, "datas"));
                    defsXml.Add(defXml);
                    defsXml.Save(path);
                    Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                }, "SavingAsDef".Translate(), true, e => Log.Message(e.Message));
            }, "SaveAsDef".Translate()));
        }

        public CustomMapDataDef map;
        public float height;
        public Vector2 pos = Vector2.zero;
    }
}
