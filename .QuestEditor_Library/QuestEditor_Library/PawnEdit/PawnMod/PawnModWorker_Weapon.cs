using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Weapon : PawnModWorker
    {
        public override PawnModData CreateData()
        {
            return new PawnModData_Weapon();
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
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Weapon.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
        }
        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.equipment == null)
            {
                return;
            }
            pawn.equipment.DestroyAllEquipment();
            ThingData weapon = pawnDef.DataFor<PawnModData_Weapon>().weapon;
            if (weapon?.def == null)
            {
                return;
            }
            ThingWithComps equipment = ThingMaker.MakeThing(weapon.def, this.StuffFor(weapon.def, weapon.stuff)) as ThingWithComps;
            if (equipment != null)
            {
                pawn.equipment.AddEquipment(equipment);
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            if (node["weapon"] != null)
            {
                pawnDef.DataFor<PawnModData_Weapon>().weapon = DirectXmlToObject.ObjectFromXml<ThingData>(node["weapon"], false);
            }
        }
        internal void OpenSelectDialog(ThingData data)
        {
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsWeapon).ToList();
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(new LabeledTextureSelectDrawer<ThingDef>(defs, def => def.uiIcon, def => def.label, def =>
            {
                if (def.MadeFromStuff)
                {
                    this.OpenStuffDialog(data, def);
                    return;
                }
                this.SetThingData(data, def, null);
            }, def => def.MadeFromStuff ? def.GetColorForStuff(GenStuff.DefaultStuffFor(def)) : def.uiIconColor, null, null, null, def => def.defName, null, null, null), "CQF_PawnEditor_Select".Translate()));
        }

        private void OpenStuffDialog(ThingData data, ThingDef def)
        {
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(new LabeledTextureSelectDrawer<ThingDef>(GenStuff.AllowedStuffsFor(def).ToList(), stuff => stuff.uiIcon, stuff => stuff.label, stuff =>
            {
                this.SetThingData(data, def, stuff);
            }, stuff => stuff.uiIconColor, null, null, null, stuff => stuff.defName, null, null, null), "CQF_PawnEditor_SelectStuff".Translate()));
        }

        private void SetThingData(ThingData data, ThingDef def, ThingDef stuff)
        {
            data.def = def;
            data.hitPoint = def.BaseMaxHitPoints;
            data.stuff = def.MadeFromStuff ? stuff : null;
        }
        internal string ThingLabel(ThingData data)
        {
            if (data?.def == null)
            {
                return "CQF_PawnEditor_None".Translate();
            }
            if (data.def.MadeFromStuff && data.stuff != null)
            {
                return data.def.label + " - " + data.stuff.label;
            }
            return data.def.label;
        }

        private ThingDef StuffFor(ThingDef def, ThingDef stuff)
        {
            return def.MadeFromStuff ? stuff ?? GenStuff.DefaultStuffFor(def) : null;
        }
    }
}
