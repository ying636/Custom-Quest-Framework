using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public abstract class PawnModData : ISaveable
    {
        public abstract PawnModDef ModDef { get; }

        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XAttribute("Class", this.GetType().FullName));
            return result;
        }

        protected PawnModDef NamedModDef(string defName)
        {
            return DefDatabase<PawnModDef>.GetNamedSilentFail(defName);
        }
    }

    public class PawnModData_Empty : PawnModData
    {
        public override PawnModDef ModDef => null;
    }

    public class PawnModData_Basic : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_Basic");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.unique)
            {
                result.Add(new XElement("unique", this.unique));
            }
            if (this.kindDef != null)
            {
                result.Add(new XElement("kindDef", this.kindDef.defName));
            }
            if (this.faction != null)
            {
                result.Add(new XElement("faction", this.faction.defName));
            }
            return result;
        }

        public bool unique;
        public PawnKindDef kindDef;
        public FactionDef faction;
    }

    public class PawnModData_NameAndBody : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_NameAndBody");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.randomName)
            {
                result.Add(new XElement("randomName", this.randomName));
            }
            if (!this.firstName.NullOrEmpty())
            {
                result.Add(new XElement("firstName", this.firstName));
            }
            if (!this.nickName.NullOrEmpty())
            {
                result.Add(new XElement("nickName", this.nickName));
            }
            if (!this.lastName.NullOrEmpty())
            {
                result.Add(new XElement("lastName", this.lastName));
            }
            if (this.nameMaker != null)
            {
                result.Add(new XElement("nameMaker", this.nameMaker.defName));
            }
            if (this.gender != Gender.None)
            {
                result.Add(new XElement("gender", this.gender));
            }
            if (this.bioAge != 14)
            {
                result.Add(new XElement("bioAge", this.bioAge));
            }
            if (this.chrAge != 14)
            {
                result.Add(new XElement("chrAge", this.chrAge));
            }
            return result;
        }

        public bool randomName;
        public string firstName = "";
        public string nickName = "";
        public string lastName = "";
        public RulePackDef nameMaker;
        public int bioAge = 14;
        public int chrAge = 14;
        public Gender gender = Gender.Male;
    }

    public class PawnModData_Appearance : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_Appearance");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            this.AddDef(result, "hair", this.hair);
            this.AddDef(result, "head", this.head);
            this.AddDef(result, "bodyType", this.bodyType);
            this.AddColor(result, "hairColor", this.hairColor);
            this.AddColor(result, "skinColor", this.skinColor);
            return result;
        }

        private void AddDef(XElement root, string name, Def def)
        {
            if (def != null)
            {
                root.Add(new XElement(name, def.defName));
            }
        }

        private void AddColor(XElement root, string name, Color? value)
        {
            if (value != null)
            {
                Color color = value.Value;
                root.Add(new XElement(name, $"({color.r}, {color.g}, {color.b}, {color.a})"));
            }
        }

        public Color? hairColor = Color.white;
        public HairDef hair = HairDefOf.Bald;
        public Color? skinColor;
        public HeadTypeDef head;
        public BodyTypeDef bodyType;
    }

    public class PawnModData_Genes : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_Genes");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.xenotype != null)
            {
                result.Add(new XElement("xenotype", this.xenotype.defName));
            }
            if (!this.customGenes.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList(this.customGenes, "customGenes"));
            }
            return result;
        }

        public XenotypeDef xenotype;
        public List<GeneDef> customGenes = new List<GeneDef>();
    }

    public class PawnModData_Backstory : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_Backstory");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.childhood != null)
            {
                result.Add(new XElement("childhood", this.childhood.defName));
            }
            if (this.adulthood != null)
            {
                result.Add(new XElement("adulthood", this.adulthood.defName));
            }
            return result;
        }

        public BackstoryDef childhood;
        public BackstoryDef adulthood;
    }

    public class PawnModData_Traits : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_Traits");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!this.traits.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.traits, "traits"));
            }
            return result;
        }

        public List<TraitData> traits = new List<TraitData>();
    }

    public class PawnModData_Skills : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_Skills");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!this.skills.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.skills, "skills"));
            }
            return result;
        }

        public List<SkillData> skills = new List<SkillData>();
    }

    public class PawnModData_Abilities : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_Abilities");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!this.abilities.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.abilities, "abilities"));
            }
            return result;
        }

        public List<AbilityData> abilities = new List<AbilityData>();
    }

    public class PawnModData_Apparel : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_Apparel");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!this.apparels.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.apparels, "apparels"));
            }
            return result;
        }

        public List<ThingData> apparels = new List<ThingData>();
    }

    public class PawnModData_Weapon : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_Weapon");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.weapon != null)
            {
                result.Add(this.weapon.SaveToXElement("weapon"));
            }
            return result;
        }

        public ThingData weapon;
    }

    public class PawnModData_Hediff : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_Hediff");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!this.hediffs.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.hediffs, "hediffs"));
            }
            return result;
        }

        public List<HediffData> hediffs = new List<HediffData>();
    }

    public class PawnModData_Dialog : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_Dialog");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.dialogManager != null)
            {
                result.Add(new XElement("dialogManager", this.dialogManager.defName));
            }
            return result;
        }

        public DialogManagerDef dialogManager;
    }

    public class PawnModData_ActionTrigger : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_ActionTrigger");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!this.actionTriggers.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.actionTriggers, "actionTriggers"));
            }
            return result;
        }

        public List<PawnActionTriggerData> actionTriggers = new List<PawnActionTriggerData>();
    }

    public class PawnModData_DutyMap : PawnModData
    {
        public override PawnModDef ModDef => this.NamedModDef("CQF_PawnMod_DutyMap");

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.dutyMap != null)
            {
                result.Add(new XElement("dutyMap", this.dutyMap.defName));
            }
            if (!this.dutyMapStartNodeId.NullOrEmpty())
            {
                result.Add(new XElement("dutyMapStartNodeId", this.dutyMapStartNodeId));
            }
            return result;
        }

        public DutyMapDef dutyMap;
        public string dutyMapStartNodeId;
    }

    public class TraitData : ISaveable
    {
        public static void DrawList(List<TraitData> list, ref float y, string title = null, string tip = null, bool needBox = false, float x = 10f, float defaultWidth = 180f)
        {
            List<KeyValuePair<TraitDef, TraitDegreeData>> stagets = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
            DefDatabase<TraitDef>.AllDefsListForReading.ForEach(t =>
            {
                t.degreeDatas.ForEach(s =>
                {
                    stagets.Add(new KeyValuePair<TraitDef, TraitDegreeData>(t, s));
                });
            });
            float initY = y;
            float width = defaultWidth;
            if (title != null)
            {
                y += 5f;
                Text.Font = GameFont.Medium;
                Rect rectTitle = new Rect(x + 10, y, 1020f, 35f);
                Widgets.Label(rectTitle, title);
                if (tip != null)
                {
                    TooltipHandler.TipRegionByKey(rectTitle, tip);
                }
                Text.Font = GameFont.Small;
                y += 40f;
                float textWidth = Text.CalcSize(title).x + 20f;
                width = textWidth > width ? textWidth : width;
            }
            for (int i = 0; i < list.Count; i++)
            {
                TraitData data = list[i];
                CQFEditorTools.DrawSelectablePercent(y, data.def?.DataAtDegree(data.degree)?.label, ref data.chance, ref data.buffer, () =>
                    Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, null, t => t.Value.label, "CQF_PawnEditor_Select".Translate(), t =>
                    {
                        data.def = t.Key;
                        data.degree = t.Value.degree;
                    })), x + 5f);
                y += 30f;
            }
            y += 5f;
            if (needBox)
            {
                Widgets.DrawBox(new Rect(x, initY, width, y - initY), 1, QuestEditor_Dialog.blueTex);
            }
            y += 10f;
            CQFEditorTools.DrawButtonForList(ref y, list, t => t.def?.DataAtDegree(t.degree)?.label,
                () => Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, null, t => t.Value.label, "CQF_PawnEditor_Select".Translate(), t =>
                {
                    list.Add(new TraitData() { def = t.Key, degree = t.Value.degree, chance = 1f });
                })), x - 5f, width - 70f, new Vector2(70f, 25f));
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("def", this.def?.defName));
            result.Add(new XElement("degree", this.degree));
            result.Add(new XElement("chance", this.chance));
            return result;
        }

        public TraitDef def;
        public int degree;
        public float chance = 1f;
        public string buffer;
    }

    public class SkillData : ISaveable
    {
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("def", this.def?.defName));
            result.Add(new XElement("level", this.level));
            result.Add(new XElement("passion", this.passion));
            return result;
        }

        public SkillDef def;
        public int level;
        public Passion passion = Passion.None;
        public string levelBuffer;
    }

    public class AbilityData : ISaveable
    {
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("def", this.def?.defName));
            return result;
        }

        public AbilityDef def;
    }

    public class HediffData : ISaveable
    {
        public void SetPart(ComplexPawnDef pawnDef, BodyPartRecord record)
        {
            this.part = record?.def;
            this.partLabel = record?.untranslatedCustomLabel;
            this.partIndex = record == null ? -1 : pawnDef.KindDef?.race?.race?.body?.AllParts.IndexOf(record) ?? -1;
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("def", this.def?.defName));
            result.Add(new XElement("part", this.part?.defName));
            result.Add(new XElement("partLabel", this.partLabel));
            result.Add(new XElement("partIndex", this.partIndex));
            result.Add(new XElement("severity", this.severity));
            return result;
        }

        public HediffDef def;
        public BodyPartDef part;
        public string partLabel;
        public int partIndex = -1;
        public float severity = 1f;
        public string buffer;
    }

    public class PawnActionTriggerData : ISaveable
    {
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("key", this.key));
            result.Add(new XElement("mode", this.mode));
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            return result;
        }

        public string key;
        public ActionTriggerMode mode = ActionTriggerMode.Damaged;
        public List<CQFAction> actions = new List<CQFAction>();
    }
}
