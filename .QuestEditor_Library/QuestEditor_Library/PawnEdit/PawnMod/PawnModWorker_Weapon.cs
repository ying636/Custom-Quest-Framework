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
            PawnModData_Weapon modData = pawnDef.DataFor<PawnModData_Weapon>();
            if (modData.weapon == null)
            {
                modData.weapon = new ThingData();
            }
            Rect row = new Rect(x, y, inRect.width - x - 20f, 30f);
            Rect iconRect = new Rect(row.x, row.y + 3f, 24f, 24f);
            if (modData.weapon.def?.uiIcon != null)
            {
                Widgets.DrawTextureFitted(iconRect, modData.weapon.def.uiIcon, 1f);
            }
            if (this.DrawTextButton(new Rect(iconRect.xMax + 8f, row.y, row.width - 120f, 30f), "CQF_PawnEditor_Weapon".Translate(this.ThingLabel(modData.weapon))))
            {
                this.OpenSelectDialog(modData.weapon);
            }
            if (this.DrawCommandText(new Rect(row.xMax - 100f, row.y, 100f, 30f), "CQF_PawnEditor_Delete".Translate()))
            {
                modData.weapon = null;
            }
            this.EndRow(ref y);
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

        private void OpenSelectDialog(ThingData data)
        {
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsWeapon).ToList();
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(defs, def => def.uiIcon, def => def.label, "CQF_PawnEditor_Select".Translate(), def =>
            {
                if (def.MadeFromStuff)
                {
                    this.OpenStuffDialog(data, def);
                    return;
                }
                this.SetThingData(data, def, null);
            }, def => def.graphic?.Color ?? Color.white));
        }

        private void OpenStuffDialog(ThingData data, ThingDef def)
        {
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(GenStuff.AllowedStuffsFor(def).ToList(), stuff => stuff.uiIcon, stuff => stuff.label, "CQF_PawnEditor_SelectStuff".Translate(), stuff =>
            {
                this.SetThingData(data, def, stuff);
            }, stuff => stuff.graphic?.Color ?? Color.white));
        }

        private void SetThingData(ThingData data, ThingDef def, ThingDef stuff)
        {
            data.def = def;
            data.hitPoint = def.BaseMaxHitPoints;
            data.stuff = def.MadeFromStuff ? stuff : null;
        }

        private string ThingLabel(ThingData data)
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
