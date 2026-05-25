using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
namespace QuestEditor_Library
{
    public class ComplexPawnData
    {
        public XElement SaveToNode(string nodeName) 
        {
            XElement result = new XElement(nodeName);
            foreach (FieldInfo field in typeof(ComplexPawnData).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                Type type = field.FieldType;   
                if (field.GetValue(this) == null) 
                {
                    continue;
                }
                if (type == typeof(Color?))
                {
                    Color color = (Color)field.GetValue(this);
                    result.Add(new XElement(field.Name, string.Format("({0}, {1}, {2}, {3})", new object[]
                {
                color.r,
                color.g,
                color.b,
                color.a
                })));

                }
                if (DirectXmlSaver.IsSimpleTextType(type)) 
                {
                    result.Add(new XElement(field.Name,field.GetValue(this)));
                }
                if (GenTypes.IsDef(type)) 
                {
                    result.Add(new XElement(field.Name,((Def)field.GetValue(this)).defName));
                }
                if (type == typeof(ThingData)) 
                {
                    result.Add(((ThingData)field.GetValue(this)).SaveToXElement(field.Name));
                }
                if (field.Name == "apparels" && this.apparels.Any())
                {
                    XElement node = new XElement(field.Name);
                    foreach (ThingData data in this.apparels) 
                    {
                        node.Add(data.SaveToXElement("li"));
                    }
                }
                if (field.Name == "traits" && this.traits.Any()) 
                {
                    XElement node = new XElement(field.Name);
                    foreach (TraitData data in this.traits)
                    {
                        node.Add(data.SaveToXElement("li"));
                    }
                }
            }
            return result;
        }
        public Pawn GetPawn() 
        {
            if (this.unique && Current.Game.GetComponent<GameComponent_Editor>() is GameComponent_Editor component && component.pawns.TryGetValue(this.dataName,out Pawn result)) 
            {
                return result;
            }
            return this.Spawn();
        }
        public Pawn Spawn()
        {
            if (this.kindDef == null)
            {
                Log.Error("QuestEditorError:Spawn ComplexPawnData without PawnKindDef");
                return null;
            }
            PawnGenerationRequest request = new PawnGenerationRequest(this.kindDef, Find.FactionManager.FirstFactionOfDef(this.faction));
            if (this.kindDef.race.race.Humanlike)
            {
                request.FixedBiologicalAge = this.bioAge;
                request.FixedChronologicalAge = this.chrAge;
                if (this.gender != Gender.None)
                {
                    request.FixedGender = this.gender;
                }

            }
            if (!this.lastName.NullOrEmpty())
            {
                request.SetFixedLastName(this.lastName);
            }
            Pawn result = PawnGenerator.GeneratePawn(request);
            if (result.story != null)
            {
                result.story.hairDef = this.hair ?? result.story.hairDef;
                result.story.headType = this.head ?? result.story.headType;
                if (this.hairColor != null)
                {
                    result.story.HairColor = this.hairColor.Value;
                }
            }
            result.health.hediffSet.Clear();
            result.apparel.DestroyAll();
            foreach (ThingData data in this.apparels) 
            {
                result.apparel.Wear((Apparel)ThingMaker.MakeThing(data.def,data.stuff));
            }
            result.equipment.DestroyAllEquipment();
            if (this.weapon == null) 
            {
                result.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(this.weapon.def,this.weapon.stuff));
            }
            if (this.traits.Any()) 
            {
                result.story.traits.allTraits.ForEach(x => result.story.traits.RemoveTrait(x));
                this.traits.ForEach(x =>
                {
                    if (Rand.Chance(x.chance))
                    {
                        result.story.traits.GainTrait(new Trait(x.def, x.degree));
                    }
                });
            }
            if (this.childhood != null)
            {
                result.story.Childhood = this.childhood;
            }
            if (this.adulthood != null)
            {
                result.story.Adulthood = this.adulthood;
            }
            if (!this.randomName)
            {
                result.Name = new NameTriple(this.firstName, this.nickName, this.lastName);
            }
            if (this.unique && Current.Game.GetComponent<GameComponent_Editor>() is GameComponent_Editor component)
            {
                component.pawns.Add(this.dataName,result);
            }
            return result;
        }
        public string dataName = "Undefined";
        public bool unique = false;
        public bool randomName = false;
        public string firstName = "";
        public string nickName = "";
        public string lastName = "";
        public Color? hairColor = Color.white;
        public HairDef hair = HairDefOf.Bald;
        public Color? skinColor = Color.white;
        public HeadTypeDef head;
        public BodyTypeDef bodyType;
        public int bioAge = 14;
        public int chrAge = 14;
        public Gender gender = Gender.None;
        public PawnKindDef kindDef;
        public FactionDef faction;
        public BackstoryDef childhood;
        public BackstoryDef adulthood;
        public List<TraitData> traits = new List<TraitData>();
        public List<ThingData> apparels = new List<ThingData>();
        public ThingData weapon;
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
                Widgets.Label(rectTitle, title.Colorize(ColorLibrary.SkyBlue));
                if (tip != null)
                {
                    TooltipHandler.TipRegionByKey(rectTitle, tip);
                }
                Text.Font = GameFont.Small;
                y += 40f;
                float textWidth = Text.CalcSize(title).x + 20f;
                width = textWidth > width ? textWidth : width;
            }
            Rect textField = new Rect(x + 10, y, 150f, 25f);
            for (int i = 0; i < list.Count; i++)
            {
                TraitData data = list[i];
                CQFEditorTools.DrawSelectablePercent(y, data.def?.DataAtDegree(data.degree)?.label, ref data.chance, ref data.buffer, () =>
                                  Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, null, t => t.Value.label, "Select".Translate(), t =>
                                  {
                                      data.def = t.Key;
                                      data.degree = t.Key.degreeDatas.IndexOf(t.Value);
                                  })),x + 5f);
                y += 30f;
                textField.y += 30f;
            }
            y += 5f;
            if (needBox)
            {
                Widgets.DrawBox(new Rect(x, initY, width, y - initY), 1, QuestEditor_Dialog.blueTex);
            }
            y += 10f;
            CQFEditorTools.DrawButtonForList(ref y, list, t => t.def?.DataAtDegree(t.degree)?.label,
                () => Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, null, t => t.Value.label, "Select".Translate(), t =>
            {
                list.Add(new TraitData() { def = t.Key, degree = t.Key.degreeDatas.IndexOf(t.Value) });
            })), x - 5f, width - 70f, new Vector2(70f, 25f));
        }
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("def", this.def?.defName));
            result.Add(new XElement("degree", this.degree));
            result.Add(new XElement("chance", this.chance));

            result.Add(new XElement("buffer", this.buffer));
            return result;
        }

        public TraitDef def;
        public int degree = 0;
        public float chance;

        public string buffer;
    }
}
