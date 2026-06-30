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
        public override Vector2 InitialSize => new Vector2(600f, 500f);
        public override void DoWindowContents(Rect inRect)
        {      
            float y = 0f;
            Rect save = new Rect(inRect.width - 135f, y, 30f, 30f);
            if (Widgets.ButtonImage(save, CQFEditorTools.icon_Save))
            {
                Find.WindowStack.Add(new Dialog_RenameForQE(d =>
                {
                    LongEventHandler.QueueLongEvent(() =>
    {
        ReplacementDataDef def = new ReplacementDataDef();
        def.defName = d;
        def.datas = this.map.replaces.ListFullCopy();
        DefDatabase<ReplacementDataDef>.Add(def);
        string path = Path.Combine(Page_QuestEditor.Path, "Data", d + ".xml");
        XElement defsxml = new XElement("Defs");
        XElement defXml = new XElement("QuestEditor_Library.ReplacementDataDef");
        defXml.Add(new XElement("defName", d));
        defXml.Add(CQFEditorTools.SaveList_Saveable(this.map.replaces, "datas"));
        defsxml.Add(defXml);
        defsxml.Save(path);
        Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
    }, "SavingAsDef".Translate(), true, e => Log.Message(e.Message));
                }, "SaveAsDef".Translate()));
            }
            TooltipHandler.TipRegion(save, "SaveAsDef".Translate());
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref this.pos, new Rect(0f, 0f, inRect.width, this.height + 10f));
            List<Type> types = typeof(ReplaceData).AllSubclassesNonAbstract().ListFullCopy();
            types.Add(typeof(ReplaceData));
            CQFEditorTools.DrawIDrawList(ref y,0f,this.map.replaces,inRect, "ReplaceDatas".Translate().Colorize(ColorLibrary.SkyBlue), () => CQFEditorTools.DrawFloatMenu(types, t => this.map.replaces.Add((ReplaceData)Activator.CreateInstance(t)), t => t.Name.Translate()), t => t.ToString());
            Widgets.EndScrollView();
            this.height = y + 5f;
        }

        public CustomMapDataDef map;
        public float height;
        public Vector2 pos = Vector2.zero;
    }
}
