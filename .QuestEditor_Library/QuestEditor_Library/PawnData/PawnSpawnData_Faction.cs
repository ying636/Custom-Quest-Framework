using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
public class PawnSpawnData_Faction : PawnSpawnData
    {
        public override void DrawKind(float x, ref float y)
        {
            Rect rect = new Rect(20f + x, y, 250f, 25f);
            if (Widgets.ButtonText(rect, "CQF_PawnGroupMaker".Translate(this.kindDef?.defName), false) && !this.faction.NullOrEmpty() && FactionDef.Named(this.faction) is FactionDef factionDef && !factionDef.pawnGroupMakers.NullOrEmpty())
            {
                CQFEditorTools.DrawFloatMenu(factionDef.pawnGroupMakers, (k) => this.kindDef = k.kindDef, (k) =>
                {
                    return k.kindDef.defName + ":" + k.commonality;
                });
            }
            TooltipHandler.TipRegion(rect, "CQF_PawnGroupMaker_Tip".Translate());
            y += 30f;
            CQFEditorTools.DrawIntRange(ref y, "SpawmPoint".Translate(), ref this.point, ref buffer1, ref buffer2, x + 20f, 80f);
        }
        public override bool CanSaveToMap()
        {
            return !this.faction.NullOrEmpty() && this.point.max >= 1;
        }
        public override Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null,bool setLord = true)
        {
            if (GameTools.GetFaction(this.faction, map) is Faction f)
            {
                List<PawnGroupMaker> makers = f.def.pawnGroupMakers.FindAll(g => this.kindDef == null ? true : g.kindDef == this.kindDef);
                if (makers.Any() && makers.RandomElementByWeight(m => m.commonality) is PawnGroupMaker maker)
                {
                    if (!Rand.Chance(this.generationChance) || !position.InBounds(map))
                    {
                        return null;
                    }
                    Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
                    if (!position.Fogged(map) && this.spawnMessage != null && !this.spawnMessage.NullOrEmpty())
                    {
                        Messages.Message(this.spawnMessage.Translate(), new LookTargets(position, map), MessageTypeDefOf.NeutralEvent);
                    }
                    PawnGroupMakerParms pawnGroupMakerParms = new PawnGroupMakerParms();
                    pawnGroupMakerParms.groupKind = this.kindDef;
                    pawnGroupMakerParms.tile = map.Tile;
                    pawnGroupMakerParms.faction = f;
                    pawnGroupMakerParms.points = Mathf.Max(this.point.RandomInRange, f.def.MinPointsToGeneratePawnGroup(this.kindDef, null));
                    int i = 0;
                    List<Pawn> pawns = new List<Pawn>();
                    maker.GeneratePawns(pawnGroupMakerParms).ToList().ForEach(p =>
                    {
                        pawns.Add(p);
                        this.ActionAfterGeneration(p, quest, i, questTag);
                        if (lord != null)
                        {
                            lord.AddPawn(p);
                            PawnDuty duty = new PawnDuty(this.duty);
                            duty.overrideFacing = this.rotation;
                            duty.focus = new LocalTargetInfo(position);
                            p.mindState.duty = duty;
                            if (lord.LordJob is LordJob_Custom job)
                            {
                                job.pawnDutyDatas.Add(p, this.duty);
                            }
                        }
                        if (p.kindDef == this.kind)
                        {
                            result.SetOrAdd(this.dataName + "." + i, p);
                        }
                        else
                        {
                            result.SetOrAdd(this.dataName + "_" + p.kindDef.defName + "." + i, p);
                        }
                        i++;
                    });
                    this.SpawnPnaw(pawns, position, map);
                    if (this.dataName != "undefined")
                    {
                        List<Pawn> ps = new List<Pawn>();
                        result.Values.ToList().ForEach(t => ps.Add(t.Thing as Pawn));
                        GameComponent_Editor.Component.GetQuestData(quest)?.AddGroup(this.dataName, ps);
                    }
                    return result;
                }
            }
            Log.Error("Custom Quset Framework Error:Pawn data lack faction");
            return null;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("point", this.point.ToString()));
            if (this.kindDef != null)
            {
                result.Add(new XElement("kindDef", this.kindDef.defName));
            }
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.point, "point");
            Scribe_Defs.Look(ref this.kindDef, "kindDef");
        }

        public IntRange point = new IntRange();
        public PawnGroupKindDef kindDef;
        public string buffer1;
        public string buffer2;
    }
}

