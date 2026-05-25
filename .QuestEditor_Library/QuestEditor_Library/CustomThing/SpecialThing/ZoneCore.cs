using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static Verse.PathFinderJob;

namespace QuestEditor_Library
{
    public class ZoneCore : ThingWithComps, IDrawTabable, IPastableData,ICopiableData
    { 
        public List<CustomMapDataDef> Datas 
        {
            get 
            {
                if (!datas.Any()) 
                {
                    datas = DefDatabase<CustomMapDataDef>.AllDefsListForReading.FindAll(d => d.isPart);
                }
                return ZoneCore.datas;
            }
        }
        public Rot4 CoreRotation => this.isCenter ? Rot4.Invalid : this.coreRotation;
        public List<Thing> GenerateZone(Func<ThingDef,bool, ThingDef> getStuff, Quest quest, bool noGenerate = false)
        {
            List<Thing> result = new List<Thing>();
            Map map = this.Map;
            IntVec3 pos = this.Position;
            List<CustomMapDataDef> zoneDatas = this.Datas.FindAll(d => !this.conditions.Exists(c => !c.Satisfied(map,d))
            && (d.Origin.generationLimit == 0 || !GenStep_CustomMap.generatedCount.ContainsKey(d.Origin) || GenStep_CustomMap.generatedCount[d.Origin] < d.Origin.generationLimit)
            && (this.isCenter || (d.zoneCores.Exists(c => c is CustomThingData_ZoneCore coreVar && d.GetDataByCore(coreVar) is CustomMapDataDef data2
            && !this.conditions.Exists(c2 => !c2.SatisfiedForCore(coreVar)) &&
            this.CanGenerate(this.coreRotation, d.size.ToIntVec2, this.Position, this.Map, coreVar)))));

            //从所有自定义地图定义中挑选是“部分”且满足条件，同时这个核心的方向为中心或者有可以生成的核心。
            if (!zoneDatas.Any() || (noGenerate))
            {
                if (this.reserveThing != null)
                {
                    Thing thing = GenSpawn.Spawn(ThingMaker.MakeThing(getStuff(this.reserveThing.def,false), getStuff(this.reserveThing.stuff,true)), this.Position, this.Map);
                }
                if (Prefs.DevMode)
                {
                    Messages.Message("NoSatisfiedCustomMapData".Translate(), MessageTypeDefOf.CautionInput);
                }
                this.Destroy();
                return null;
            }
            CustomMapDataDef data = zoneDatas.RandomElementByWeight(d => d.commonality);
            //if (Prefs.DevMode)
            //{
            //    Log.Message("抽取的地图数据：" +  data.ToString());
            //}
            if (this.isCenter)
            {
                result.AddRange(data.GenerateByCore(pos, map, quest,false,false, this.destroyThings,true));
            }
            else
            {
                CustomThingData_ZoneCore coreData = (CustomThingData_ZoneCore)data.zoneCores.FindAll(x =>
                {
                    return x is CustomThingData_ZoneCore corevar && data.GetDataByCore(corevar) is CustomMapDataDef data2 && !this.conditions.Exists(c2 => !c2.SatisfiedForCore(corevar)) && corevar.coreRotation != Rot4.Invalid
                    && this.CanGenerate(this.CoreRotation, data.size.ToIntVec2, this.Position, this.Map, corevar);
                }).RandomElement();
                if (Prefs.DevMode && !DebugTools.clearGenerationData)
                {
                    CellRect rect = this.GetRect(this.CoreRotation, data.size.ToIntVec2, this.Position, coreData);
                    DebugTools.cells.AddRange(rect.Cells);
                    if (Prefs.DevMode)
                    {
                        Log.Message(this.Position.ToString());
                        Log.Message(coreData.size.ToString());
                        Log.Message(rect.ToString());
                    }
                }
                CustomMapDataDef resolved = data.GetNewDataUseNewOrigih(coreData.position, coreData.coreRotation);
                resolved = resolved.GetRotated(this.coreRotation.Opposite);
                //if (Prefs.DevMode)
                //{
                //    Log.Message(coreData.ToString());
                //    Log.Message(resolved.defName);
                //}
                result.AddRange(resolved.GenerateByCore(pos, map, quest, false, false, this.destroyThings));
            }
            if (this.generationKey != null) 
            {
                if (GenStep_CustomMap.generatedCount_Key.TryGetValue(this.generationKey, out int count))
                {
                    GenStep_CustomMap.generatedCount_Key.SetOrAdd(this.generationKey, count + 1);
                }
                else
                {
                    GenStep_CustomMap.generatedCount_Key.SetOrAdd(this.generationKey, 1);
                }
            }
            if (!this.Destroyed)
            {
                this.Destroy();
            }
            return result;
        }   
        public CellRect GetRect(Rot4 rotation, IntVec2 size, IntVec3 generatePos, CustomThingData_ZoneCore core = null)
        {     
            CellRect? rect = null;
            Rot4 coreRotaion = this.coreRotation;
            if (core is CustomThingData_ZoneCore && core.size is CoreSize coreSize && !coreSize.IsEmpty)
            {
                RotationDirection direction = Rot4.GetRelativeRotation(this.coreRotation.Opposite, core.coreRotation);
                CoreSize coreSize2 = coreSize.GetCopy();
                coreSize2.Rotate(this.coreRotation, direction);
                rect = CellRect.FromLimits(generatePos.x - coreSize2.minX, generatePos.z - coreSize2.minZ, generatePos.x + coreSize2.maxX, generatePos.z + coreSize2.maxZ);
                return rect.Value;
            }
            size = coreRotaion == rotation || coreRotaion == rotation.Opposite ? size : new IntVec2(size.z, size.x); 
            if (rotation == Rot4.North)
            {
                rect = new CellRect(generatePos.x - (size.x / 2), generatePos.z, size.x, size.z);
            }
            if (rotation == Rot4.South)
            {
                rect = new CellRect(generatePos.x - (size.x / 2), generatePos.z - size.z, size.x, size.z);
            }
            if (rotation == Rot4.East)
            {
                rect = new CellRect(generatePos.x, generatePos.z - (size.z / 2), size.x, size.z);
            }
            if (rotation == Rot4.West)
            {
                rect = new CellRect(generatePos.x - size.x, generatePos.z - (size.z / 2), size.x, size.z);
            }
            return rect.Value;
        }
        public bool CanGenerate(Rot4 rotation, IntVec2 size, IntVec3 generatePos, Map map,CustomThingData_ZoneCore core = null)
        {
            if (this.isCenter) 
            {
                return true;
            }
            RotationDirection dir = Rot4.GetRelativeRotation(rotation, core.coreRotation);
            if (this.prohibitRotatingDocking && (dir == RotationDirection.Clockwise || dir == RotationDirection.Counterclockwise)) 
            {
                return false;
            }
            if (this.prohibitFlippingDocking && dir == RotationDirection.None)
            {
                return false;
            }
            CellRect? rect = this.GetRect(rotation, size, generatePos,core);
            if (rect == null)
            {
                return false;
            }
            return !rect.Value.Cells.ToList().Exists(c => !c.InBounds(map) || (GenStep_CustomMap.disgenerate.Contains(c) && !rect.Value.IsOnEdge(c)));
        }
        //参数分别为生成的事物的坐标，被生成的地图的核心的坐标，要返回的实际坐标
        public void DrawTab()
        {
            Widgets.BeginScrollView(new Rect(0f, 5f, 490f, 590f), ref this.scrollPos, new Rect(0f, 5f, 490f, this.height));
            float y = 10f;
            float x = 7f;
            Rect rect = new Rect(x, y, 250f, 25f);
            Func<Rot4, string> GetText = r => r == Rot4.Invalid ? "Rot_Invalid".Translate().ToString() : r.ToStringHuman().Translate().ToString();
            if (Widgets.ButtonText(rect, "CoreZoneRotation".Translate(this.isCenter ? "Rot_Invalid".Translate().ToString() : GetText(this.coreRotation)), false))
            {
                CQFEditorTools.DrawFloatMenu(new List<Rot4>() { Rot4.West, Rot4.East, Rot4.North, Rot4.South, Rot4.Invalid }, (r) =>
                      {
                          this.coreRotation = r;
                          this.isCenter = !r.IsValid;
                      }, (r) => GetText(r));
            }
            TooltipHandler.TipRegion(rect, "CoreZoneRotationTip".Translate());
            Rect rectCP = new Rect(380f, y, 25f, 25f);
            if (Widgets.ButtonImage(rectCP, TexButton.Copy))
            {
                this.CopyData();
            }
            TooltipHandler.TipRegion(rectCP, "Copy".Translate());
            y += 30f;
            Rect reserveRect = new Rect(x, y, 300f, 25f);
            if (Widgets.ButtonText(reserveRect, "ReserveGenerationThing".Translate(this.reserveThing == null ? "NoGenerate".Translate().ToString() : this.reserveThing?.stuff?.label + this.reserveThing?.def?.label), false))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("NoGenerate".Translate(), () => this.reserveThing = null));
                options.Add(new FloatMenuOption("Select".Translate(), () =>
                {
                    this.reserveThing = new ThingData();
                    Find.WindowStack.Add(new Dialog_Select<ThingDef>(Designator_SpawnThing.Bespawnable,
           t => t.uiIcon, t => t.label, "Select".Translate(),
           t =>
           {
               this.reserveThing.def = t;
               this.reserveThing.hitPoint = t.BaseMaxHitPoints;
               if (t.MadeFromStuff)
               {
                   Find.WindowStack.Add(new Dialog_Select<ThingDef>(GenStuff.AllowedStuffsFor(t).ToList(), s => s.uiIcon, s => s.label, "SelectStuff".Translate(), s =>
                   {
                       this.reserveThing.stuff = s;
                       this.reserveThing.hitPoint = (int)(t.BaseMaxHitPoints * (s.stuffProps.statFactors.Find(s2 => s2.stat == StatDefOf.MaxHitPoints) is StatModifier stat ? stat.value : 1f));
                   }, t2 => t2.graphic?.Color ?? Color.white,(d, r) => Widgets.DefIcon(r,d, null)));
               }
           }, t => t.graphic?.Color ?? Color.white));
                }));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(reserveRect, "ReserveGenerationThingTip".Translate());
            Rect copy = new Rect(300f, y, 25f, 25f);
            if (Widgets.ButtonImage(copy, TexButton.Copy)) 
            {
                CQFEditorTools.thingData = this.reserveThing;
            }
            copy.x += 30f;
            if (Widgets.ButtonImage(copy, TexButton.Paste))
            {
                this.reserveThing = CQFEditorTools.thingData;
            }  
            y += 30f; 
            CQFEditorTools.DrawLabelAndText_Line(y,"GenerationKey".Translate(), ref this.generationKey,x,150f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 300f, 25f), "DestroyThingsWhenGeneration".Translate(), ref this.destroyThings);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x,y,300f,25f), "ProhibitRotatingDocking".Translate(),ref this.prohibitRotatingDocking);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 300f, 25f), "ProhibitFlippingDocking".Translate(), ref this.prohibitFlippingDocking);
            y += 30f;
            CQFEditorTools.DrawEditableStringList(this.coreTags,ref y,"CoreTags".Translate(),null,true,x,360f);
            y += 5f;
            CQFEditorTools.DrawIDrawList(ref y, x, this.conditions, new Rect(5f, 5f, 490f, 590f), "ZoneGenerationConditions".Translate());
            y += 40;
            Widgets.EndScrollView();
            this.height = y;
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            yield return new Command_Action()
            {
                defaultLabel = "Try Generate"
                ,
                action = () =>
                {
                    this.GenerateZone((d,t) => d,null);
                }
            };
            yield break;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.isCenter, "ZoneCore_isCenter");
            Scribe_Values.Look(ref this.generationKey, "generationKey");
            Scribe_Values.Look(ref this.prohibitRotatingDocking, "prohibitRotatingDocking");
            Scribe_Values.Look(ref this.prohibitFlippingDocking, "prohibitFlippingDocking");
            Scribe_Values.Look(ref this.coreRotation, "coreRotation");
            Scribe_Values.Look(ref this.destroyThings, "destroyThings");
            Scribe_Deep.Look(ref this.reserveThing, "reserveThing");
            Scribe_Deep.Look(ref this.size, "size");
            Scribe_Collections.Look(ref this.conditions, "ZoneCore_conditions",LookMode.Deep);
            Scribe_Collections.Look(ref this.coreTags, "coreTags",LookMode.Value);
        }

        public void PasteData()
        {
            this.coreRotation = CQFEditorTools.coreRotation;
            this.generationKey = CQFEditorTools.generationKey;
            this.isCenter = CQFEditorTools.isCenter;
            this.reserveThing = CQFEditorTools.reserveThing;
            this.prohibitRotatingDocking = CQFEditorTools.prohibitRotatingDocking;
            this.prohibitFlippingDocking = CQFEditorTools.prohibitFlippingDocking;
            this.conditions.Clear();
            CQFEditorTools.conditions.ForEach(c => this.conditions.Add(c.Copy()));
            this.coreTags = CQFEditorTools.coreTags.ListFullCopy();
        }

        public void CopyData()
        {
            CQFEditorTools.coreRotation = this.coreRotation;
            CQFEditorTools.isCenter = this.isCenter;
            CQFEditorTools.reserveThing = this.reserveThing;
            CQFEditorTools.generationKey = this.generationKey;
            CQFEditorTools.prohibitRotatingDocking = this.prohibitRotatingDocking;
            CQFEditorTools.prohibitFlippingDocking = this.prohibitFlippingDocking;
            CQFEditorTools.destroyThings = this.destroyThings;
            CQFEditorTools.conditions = this.conditions.ListFullCopy();
            CQFEditorTools.coreTags = this.coreTags.ListFullCopy();
        }

        public string generationKey = null;

        public float height = 0f;
        public Vector2 scrollPos;
        public Rot4 coreRotation = Rot4.Invalid;
        public CoreSize size = CoreSize.Empty;
        public bool prohibitRotatingDocking = false;
        public bool prohibitFlippingDocking = false;
        public bool isCenter = true;
        public bool destroyThings = false;
        public ThingData reserveThing = null;
        public List<ZoneCondition> conditions = new List<ZoneCondition>();
        public List<string> coreTags = new List<string>();

        public static List<CustomMapDataDef> datas = new List<CustomMapDataDef>();
    }
    public abstract class ZoneCondition : ISaveable, IDrawable,IExposable
    {
        public abstract bool Satisfied(Map map,CustomMapDataDef def);
        public virtual void Draw(ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(x, y, 150f, 25f);
            Widgets.Label(rect, this.GetType().Name.Translate().Colorize(ColorLibrary.SkyBlue));
            if ((this.GetType().Name + "_Tip").CanTranslate())
            {
                TooltipHandler.TipRegion(rect, (this.GetType().Name + "_Tip").Translate());
            }
            y += 30f;
        }
        public virtual bool SatisfiedForCore(CustomThingData_ZoneCore core) 
        {
            return true;
        }
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", this.GetType().FullName);
            return result;
        }

        public virtual void ExposeData()
        {
          
        }
        public ZoneCondition Copy()
        {
            XElement x = this.SaveToXElement("ZoneCondition");
            XmlNode node = new XmlDocument().ReadNode(x.CreateReader()) as XmlNode;
            ZoneCondition result = DirectXmlToObject.ObjectFromXml<ZoneCondition>(node, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return result;
        }
    }
    public class ZoneCondition_Size : ZoneCondition
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawIntRange(ref y,"RangeOfX".Translate(),ref this.x,ref this.buffer,ref this.buffer1,x,150f);
            CQFEditorTools.DrawIntRange(ref y, "RangeOfZ".Translate(), ref this.z, ref this.buffer2, ref this.buffer3, x, 150f);
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("x",this.x.ToString()));
            result.Add(new XElement("z", this.z.ToString()));
            return result;
        }
        public override bool Satisfied(Map map,CustomMapDataDef def)
        {
            return this.x.min < def.size.x && def.size.x < this.x.max && this.z.min < def.size.z && def.size.z < this.z.max;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.x, "x"); 
            Scribe_Values.Look(ref this.z, "z");
        }

        public string buffer;
        public string buffer1;
        public string buffer2;
        public string buffer3;
        public IntRange x = new IntRange(0,1);
        public IntRange z = new IntRange(0, 1);
    }
    public class ZoneCondition_FactionMeme : ZoneCondition
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x); 
            CQFEditorTools.DrawSelectableText(y, "PawnDataFaction".Translate(), ref this.faction, () => CQFEditorTools.DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll((f) => !f.isPlayer), (f) => this.faction = f.defName, (f) => f.label, new List<FloatMenuOption>()
            {
                new FloatMenuOption("RandomHostile".Translate(),() => this.faction = "RandomHostile"),
                new FloatMenuOption("RandomAlly".Translate(),() => this.faction = "RandomAlly"),
                new FloatMenuOption("RandomNeutral".Translate(),() => this.faction = "RandomNeutral"),
                new FloatMenuOption("PawnDataMapFaction".Translate(),() => this.faction = "MapFaction")
            }), 20f + x, 120f);
            y += 30f;
            if (Widgets.ButtonText(new Rect(x,y,350f,25f), "RequiredMeme".Translate(this.meme?.label),false)) 
            {
                CQFEditorTools.DrawFloatMenu<MemeDef>(DefDatabase<MemeDef>.AllDefs.ToList(), (f) => this.meme = f, (f) => f.label);
            }
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("faction", this.faction.ToString()));
            result.Add(new XElement("meme", this.meme.defName));
            return result;
        }
        public override bool Satisfied(Map map, CustomMapDataDef def)
        {
            return ModsConfig.IdeologyActive && 
                (bool)(GameTools.GetFaction(this.faction,map)?.ideos.PrimaryIdeo.memes.Exists(m => m == this.meme));
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.faction, "faction");
            Scribe_Defs.Look(ref this.meme, "meme");
        }

        public MemeDef meme;
        public string faction;
    }
    public class ZoneCondition_Tag : ZoneCondition
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawEditableStringList(this.tags,ref y, "CustomMapTags".Translate(),null,true,x);
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFEditorTools.SaveList(this.tags,"tags"));
            return result;
        }
        public override bool Satisfied(Map map,CustomMapDataDef def)
        {
            return def.tags.Exists(t => this.tags.Contains(t));
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref this.tags, "tags",LookMode.Value);
        }

        public List<string> tags = new List<string>();
    }
    public class ZoneCondition_CoreTag : ZoneCondition
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawEditableStringList(this.coreTags, ref y, "CoreTags".Translate(), null, true, x);
            Widgets.CheckboxLabeled(new Rect(x,y,250f,25f),"InvertResult".Translate(),ref this.invert);
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFEditorTools.SaveList(this.coreTags, "coreTags"));
            return result;
        }
        public override bool Satisfied(Map map,CustomMapDataDef def)
        {
            return true;
        }

        public override bool SatisfiedForCore(CustomThingData_ZoneCore core)
        {
            bool result = !this.invert ? !core.coreTags.Exists(t => this.coreTags.Contains(t)) : core.coreTags.Exists(t => this.coreTags.Contains(t));
            return result;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.invert, "invert");
            Scribe_Collections.Look(ref this.coreTags, "coreTags", LookMode.Value);
        }

        public List<string> coreTags = new List<string>();
        public bool invert = false;
    }
    public class ZoneCondition_Invert : ZoneCondition
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            this.subCondition?.Draw(ref y, inRect, x + 5f);
            if (Widgets.ButtonText(new Rect(x, y, 150f, 25f), "SelectCondition".Translate(), false))
            {
                CQFEditorTools.DrawFloatMenu(typeof(ZoneCondition).AllSubclassesNonAbstract(), a =>
    this.subCondition = ((ZoneCondition)Activator.CreateInstance(a)), a => a.Name.Translate());
            }
            y += 35f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(this.subCondition.SaveToXElement("subCondition"));
            return result;
        }
        public override bool Satisfied(Map map, CustomMapDataDef def)
        {
            return !this.subCondition.Satisfied(map, def);
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref this.subCondition, "subCondition");
        }

        public ZoneCondition subCondition;
    }


    public class ZoneCondition_Wealth : ZoneCondition
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawIntRange(ref y, "WealthRange".Translate(), ref this.wealth, ref this.buffer, ref this.buffer1, x, 150f);
            this.subCondition?.Draw(ref y, inRect, x + 5f);   
            if (Widgets.ButtonText(new Rect(x, y, 150f, 25f), "SelectCondition".Translate(), false))
            {
                CQFEditorTools.DrawFloatMenu(typeof(ZoneCondition).AllSubclassesNonAbstract(), a =>
    this.subCondition = ((ZoneCondition)Activator.CreateInstance(a)), a => a.Name.Translate());
            }
            y += 35f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("wealth",this.wealth)); 
            result.Add(this.subCondition.SaveToXElement("subCondition"));
            return result;
        }
        public override bool Satisfied(Map map,CustomMapDataDef def)
        {
            float wealth = Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal;
            return wealth < this.wealth.min || wealth > this.wealth.max || (wealth > this.wealth.min && wealth < this.wealth.max && this.subCondition.Satisfied(map,def));
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.buffer, "buffer"); 
            Scribe_Values.Look(ref this.buffer1, "buffer1");
            Scribe_Values.Look(ref this.wealth, "wealth");
            Scribe_Deep.Look(ref this.subCondition, "subCondition");
        }

        public string buffer;
        public string buffer1;
        public IntRange wealth = new IntRange(0,1);
        public ZoneCondition subCondition;
    }

}