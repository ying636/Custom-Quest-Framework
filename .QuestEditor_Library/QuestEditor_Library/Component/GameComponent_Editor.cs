using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    [StaticConstructorOnStartup]
    public class GameComponent_Editor : GameComponent
    {
        public GameComponent_Editor(Game game)
        {
            Instance = this;
        }
        public static GameComponent_Editor Component => Instance;
        public List<ExecutiveRequest> Request 
        {
            get 
            {
                if (this.requests == null)
                {
                    this.requests = new List<ExecutiveRequest>();
                }
                return this.requests;
            }
        }
        public QuestData GlobalDatabase
        {
            get
            {
                if (this.globalData == null)
                {
                    this.globalData = new QuestData();
                }
                return this.globalData;
            }
        }
        public QuestData TemporaryDatabase
        {
            get
            {
                if (this.temporaryData == null)
                {
                    this.temporaryData = new QuestData();
                }
                return this.temporaryData;
            }
        }
        public Dictionary<Thing, DialogManagerDef> Dialogs
        {
            get
            {
                if (this.dialogsWithTargets == null)
                {
                    this.dialogsWithTargets = new Dictionary<Thing, DialogManagerDef>();
                }
                return this.dialogsWithTargets;
            }
        }
        public Dictionary<CaravanActionDef, CD> CACDS
        {
            get
            {
                if (this.CACDs == null)
                {
                    this.CACDs = new Dictionary<CaravanActionDef, CD>();
                }
                return this.CACDs;
            }
        }
        public Dictionary<int, QuestData> Datas
        {
            get
            {
                if (this.datas == null)
                {
                    this.datas = new Dictionary<int, QuestData>();
                }
                return this.datas;
            }
        }
        public bool IsAvailable(CaravanActionDef def)
        {
            return !this.CACDS.ContainsKey(def);
        }
        public void AddExecutiveRequest(ExecutiveRequest request)
        {
            this.Request.Add(request);
        }
        public void AddExecutiveRequest(int delay,CQFAction action,Quest quest,Dictionary<string,TargetInfo> targets) 
        {
            this.Request.Add(new ExecutiveRequest(action,quest,targets,delay));
        }
        public bool GetBool(string name)
        {
            return this.GlobalDatabase.GetBool(name);
        }
        public void SetBool(string name, bool value)
        {
            this.GlobalDatabase.SetBool(name, value);
        }
        public void ResetTemporaryDatabase()
        {
            this.temporaryData = new QuestData();
        }
        public void ClearTemporaryDatabase()
        {
            this.TemporaryDatabase.Clear();
        }
        public QuestData GetQuestData(Quest quest)
        {
            if (quest == null)
            {
                return null;
            }
            if (!this.Datas.ContainsKey(quest.id))
            {
                this.Datas.Add(quest.id, new QuestData());
            }
            return this.Datas.TryGetValue(quest.id);
        }
        public void RemoveQuestData(Quest quest)
        {
            if (quest == null)
            {
                return;
            }
            if (this.Datas.ContainsKey(quest.id))
            {
                this.Datas.Remove(quest.id);
            } 
        }
        public Material GetDialogIconMaterial(Color c)
        {
            if (!this.materialPool_Dialog.ContainsKey(c))
            {
                Material material = new Material(QuestIconMat);
                material.color = c;
                this.materialPool_Dialog.Add(c, material);
            }
            return this.materialPool_Dialog[c];
        }
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            this.Request.ForEach(r =>
            {
                r.delayTime--;
                if (r.delayTime <= 0) 
                {
                    r.Execute();
                }
            });
            this.Request.RemoveAll(r => r.delayTime <= 0);
            this.CACDS.RemoveAll(r => Find.TickManager.TicksGame - r.Value.curTick >= r.Value.time);
        }
        public override void GameComponentUpdate()
        {
            if (Current.Game?.World != null)
            {
                if (Find.World?.renderer?.wantedMode == WorldRenderMode.None)
                {
                    this.Dialogs.ToList().ForEach(d =>
                    {
                        if (d.Key != null && d.Key.Spawned && d.Key.Map == Find.CurrentMap && d.Value != null && !d.Key.Fogged())
                        {
                            Vector3 drawPos = d.Key.DrawPos;
                            drawPos.y = BaseAlt;
                            if (d.Key is Pawn)
                            {
                                drawPos.x += (float)d.Key.def.size.x - 0.52f;
                                drawPos.z += (float)d.Key.def.size.z - 0.45f;
                            }
                            float num = ((float)Math.Sin((double)((Time.realtimeSinceStartup + 397f * (float)(
            d.Key.thingIDNumber % 571)) * 4f)) + 1f) * 0.5f;
                            num = 0.3f + num * 0.7f;
                            Material material = FadedMaterialPool.FadedVersionOf(
                                GetDialogIconMaterial(d.Value.iconColor), num);
                            Color c = d.Value.iconColor;
                            c.a = material.color.a;
                            material.color = c;
                            drawBatch.DrawMesh(MeshPool.plane05,
                                Matrix4x4.TRS(drawPos, Quaternion.identity, Vector3.one * 1.2f), material, 0, true);
                        }
                    });
                    if (showCells)
                    {
                        GenDraw.DrawFieldEdges(GenStep_CustomMap.disgenerate, Color.red);
                        GenDraw.DrawFieldEdges(DebugTools.cells, Color.blue);
                    }
                    this.drawBatch.Flush();
                }
            }
        }
        private static readonly Material QuestionMarkMat = MaterialPool.MatFrom("UI/Overlays/QuestionMark", ShaderDatabase.MetaOverlay);
        public void AddDialog(Thing thing, DialogManagerDef def)
        {
            if (!this.Dialogs.ContainsKey(thing))
            {
                this.dialogsWithTargets.SetOrAdd(thing, def);
                if (thing is Pawn p && p.RaceProps.Humanlike && def.forcedTraits is List<TraitData> datas && datas.Any())
                {
                    datas.ForEach(d =>
                    {
                        if (Rand.Chance(d.chance))
                        {
                            p.story.traits.GainTrait(new Trait(d.def, d.degree));
                        }
                    });
                }
            }
        }
        public void RemoveDialog(Thing thing)
        {
            if (this.Dialogs.ContainsKey(thing))
            {
                this.dialogsWithTargets.Remove(thing);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.dialogsWithTargets.RemoveAll(d => (d.Value.removeWhenThingDespawned && !d.Key.Spawned) || (d.Value.removeWhenPawnDied && d.Key is Pawn p && p.Dead));
                List<Quest> qs = Find.QuestManager.QuestsListForReading.FindAll(q => q.Historical);
                this.datas.RemoveAll(v => qs.Exists(q=> q.id == v.Key));
            } 
            Scribe_Deep.Look(ref this.globalData, "globalData");
            Scribe_Collections.Look(ref this.requests, "QE_GameComponent_Editor_requests",LookMode.Deep);
            Scribe_Collections.Look(ref this.dialogsWithTargets, "QE_GameComponent_Editor_dialogsWithTargets", LookMode.Reference, LookMode.Def, ref this.tmpdialogsThings, ref this.tmpdialogsDialogManagerDefs);
            Scribe_Collections.Look(ref this.pawns, "QE_GameComponent_Editor_pawns", LookMode.Value, LookMode.Reference, ref this.tmpPawnIDs, ref this.tmpPawns);
            Scribe_Collections.Look(ref this.datas, "QE_GameComponent_Editor_datas", LookMode.Value, LookMode.Deep, ref this.tmpQuestIndex, ref this.tmpQuestData);

            Scribe_Collections.Look(ref this.CACDs, "CACDs", LookMode.Def, LookMode.Deep, ref this.tmpCACDs, ref this.tmpCACDs_CD);
        }

        private QuestData globalData = new QuestData();
        private QuestData temporaryData = new QuestData();

        public static GameComponent_Editor Instance;

        public Dictionary<Color, Material> materialPool_Dialog = new Dictionary<Color, Material>();

        public Dictionary<Thing, DialogManagerDef> dialogsWithTargets = new Dictionary<Thing, DialogManagerDef>();
        public List<Thing> tmpdialogsThings;
        public List<DialogManagerDef> tmpdialogsDialogManagerDefs;
        public Dictionary<string, Pawn> pawns = new Dictionary<string, Pawn>();
        public List<string> tmpPawnIDs;
        public List<Pawn> tmpPawns;

        private List<ExecutiveRequest> requests = new List<ExecutiveRequest>();

        private Dictionary<int, QuestData> datas = new Dictionary<int, QuestData>();
        List<int> tmpQuestIndex;
        List<QuestData> tmpQuestData; 
        private Dictionary<CaravanActionDef, CD> CACDs = new Dictionary<CaravanActionDef, CD>();
        private List<CaravanActionDef> tmpCACDs;
        private List<CD> tmpCACDs_CD;

        public static bool showCells = false;

        private static readonly Material QuestIconMat = MaterialPool.MatFrom("UI/Icons/Icon_Dialog", ShaderDatabase.MetaOverlay);

        private static readonly float BaseAlt = AltitudeLayer.MetaOverlays.AltitudeFor() - 0.243243232f;

        private DrawBatch drawBatch = new DrawBatch();
    }
    public class CD : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.curTick,"tick");
            Scribe_Values.Look(ref this.time, "time");
        }
        public int time;
        public int curTick;
    }
    public struct Route : IExposable
    {
        public void ExposeData()
        {
            Scribe_Collections.Look(ref this.route, "QE_GameComponent_Route_route", LookMode.Value);
        }
        public List<IntVec3> route;
    }
    public class QuestData : IExposable
    {
        public Dictionary<string, Lord> Lords
        {
            get
            {
                if (this.lords == null)
                {
                    this.lords = new Dictionary<string, Lord>();
                }
                return this.lords;
            }
        }
        public List<TargetWithKey> TargetDatas
        {
            get
            {
                if (this.targetDatas == null)
                {
                    this.targetDatas = new List<TargetWithKey>();
                }
                return this.targetDatas;
            }
        }
        public void RecordTarget(string name, TargetInfo target)
        {
            if (this.TargetDatas.Find(d => d.key == name) is TargetWithKey t)
            {
                t.target = target;
            }
            else
            {
                this.TargetDatas.Add(new TargetWithKey(name, target));
            }
        }
        public TargetInfo GetTarget(string name)
        {
            if (this.TargetDatas.Find(t => t.key == name) is TargetWithKey target)
            {
                return target.target;
            }
            return TargetInfo.Invalid;
        }
        public bool TargetExists(string name, bool needSpawned)
        {
            return this.TargetDatas.Exists(d => d.key == name &&
                (!needSpawned || (d.target.HasThing && d.target.Thing.Spawned)));
        }
        public void AddGroup(string name, List<Pawn> pawns)
        {
            
            if (this.GetGroup(name) is { } ps)
            {
                ps.AddRange(pawns);
                return;
            }
            this.pawnGroups.Add(new QuestPawnGroup(name, pawns));
        }
        public void AddGroup(string name, List<Thing> pawns)
        {
            if (this.GetGroup(name) is { } ps)
            {
                ps.AddRange(pawns);
                return;
            }
            this.pawnGroups.Add(new QuestPawnGroup(name, pawns));
        }
        public List<Thing> GetGroup(string name)
        {
            if (this.pawnGroups.Find(g => g.groupName == name) is { } group)
            {
                return group.inner;
            }
            return null;
        }
        public bool GetBool(string name)
        {
            return this.values_B.ContainsKey(name) && this.values_B[name];
        }
        public void SetBool(string name, bool value)
        {
            this.values_B.SetOrAdd(name, value);
        }
        public int GetValue(string name, int defaultValue = 0)
        {
            return this.values.TryGetValue(name, out int value) ? value : defaultValue;
        }
        public void SetValue(string name, int value)
        {
            this.values.SetOrAdd(name, value);
        }
        public void Clear()
        {
            this.Lords.Clear();
            this.pawnGroups.Clear();
            this.TargetDatas.Clear();
            this.values.Clear();
            this.values_B.Clear();
        }
        public void ExposeData()
        {
            Scribe_Collections.Look(ref this.pawnGroups, "QuestData_pawnGroups", LookMode.Deep);
            Scribe_Collections.Look(ref this.targetDatas, "QuestData_targetDatas", LookMode.Deep);
            Scribe_Collections.Look(ref this.values, "QuestData_values", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref this.values_B, "QuestData_values_B", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref this.lords, "QuestData_lords", LookMode.Value, LookMode.Reference, ref this.tmpNames, ref this.tmpLord);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("PawnGroups".Translate());
            this.pawnGroups.ForEach(g => result.AppendLine(g.ToString()));
            result.AppendLine("TargetDatas".Translate());
            this.targetDatas.ToList().ForEach(g => result.AppendLine(g.ToString()));
            result.AppendLine("-------");
            this.values_B.ToList().ForEach(g => result.AppendLine(g.ToString()));
            this.values.ToList().ForEach(g => result.AppendLine(g.ToString()));
            return result.ToString().Trim();
        }


        Dictionary<string, Lord> lords = new Dictionary<string, Lord>();
        public List<string> tmpNames;
        public List<Lord> tmpLord;
        List<QuestPawnGroup> pawnGroups = new List<QuestPawnGroup>();
        List<TargetWithKey> targetDatas = new List<TargetWithKey>();
        Dictionary<string, bool> values_B = new Dictionary<string, bool>();
        Dictionary<string, int> values = new Dictionary<string, int>();
    }

    public class TargetWithKey : IExposable
    {
        public TargetWithKey() { }
        public TargetWithKey(string key, TargetInfo target)
        {
            this.key = key;
            this.target = target;
        }
        public override string ToString()
        {
            return $"Key:{this.key},Targt:{this.target}";
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.key, "key");
            Scribe_TargetInfo.Look(ref this.target, "target");
        }
        public string key;
        public TargetInfo target;
    }
    public class QuestPawnGroup : IExposable
    {
        public QuestPawnGroup() { }
        public QuestPawnGroup(string name, List<Thing> things)
        {
            this.groupName = name;
            this.inner = things;
        }
        public QuestPawnGroup(string name, List<Pawn> pawns)
        {
            this.groupName = name;
            foreach (var pawn in pawns)
            {
                this.inner.Add(pawn);
            }
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.groupName, "QuestPawnGroup_groupName");
            Scribe_Collections.Look(ref this.inner, "QuestPawnGroup_innerPawns", LookMode.Reference);
        }
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine(("-" + this.groupName + "-"));
            this.inner.ForEach(g => result.AppendLine(g?.Label ?? "Null"));
            return result.ToString().Trim();
        }

        public string groupName = "";
        public List<Thing> inner = new List<Thing>();
    }
   
    public class ExecutiveRequest : IExposable
    {
        public ExecutiveRequest()
        {
        }
        public ExecutiveRequest(CQFAction action,Quest quest,Dictionary<string, TargetInfo> parameters,int delayTime)
        {
            this.action = action;
            this.quest = quest;
            this.parameters = new List<TargetWithKey>();
            parameters.ToList().ForEach(p => this.parameters.Add(new TargetWithKey(p.Key,p.Value)));
            this.delayTime = delayTime;
        }
        public virtual void Execute() 
        {
            Dictionary<string, TargetInfo> parameters = new Dictionary<string, TargetInfo>();
            this.parameters.ForEach(p => parameters.Add(p.key,p.target));
            this.action.Work(parameters,quest);
        }
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine(this.action.ToString());
            result.AppendLine(this.quest.id.ToString());
            this.parameters.ToList().ForEach(p => result.AppendLine(p.ToString()));
            result.AppendLine(this.delayTime.ToString());
            return result.ToString().Trim();
        }
        public virtual void ExposeData()
        {
            Scribe_Deep.Look(ref this.action, "action");
            Scribe_References.Look(ref this.quest, "quest");
            Scribe_Collections.Look(ref this.parameters, "parameters",LookMode.Deep);
            Scribe_Values.Look(ref this.delayTime, "delayTime");
        }

        CQFAction action;
        Quest quest;
        List<TargetWithKey> parameters;
        public int delayTime;
    }
    public class ExecutiveRequest_DestroySubMap : ExecutiveRequest 
    {
        public override void Execute()
        {
            PocketMapUtility.DestroyPocketMap(this.map);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref this.map,"map");
        }

        public Map map;
    }
}
