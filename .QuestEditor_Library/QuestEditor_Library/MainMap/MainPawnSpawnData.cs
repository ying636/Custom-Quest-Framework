using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class MainPawnSpawnData : PawnSpawnData
    {
        public override Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null, bool setLord = true)
        {
            MainSite site = MainMapGenerationContext.CurrentSite;
            if (this.dataName.NullOrEmpty() || this.dataName == "undefined")
            {
                return this.SpawnNewPawn(position, map, questTag, quest, lord, setLord);
            }
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            if (!this.ConditionsSatisfied(this.generateConditions, quest))
            {
                return result;
            }
            if (site != null && site.TryGetMainPawn(this.dataName, out Pawn cachedPawn))
            {
                if (this.TrySpawnCachedPawn(cachedPawn, position, map, questTag, quest, lord, result))
                {
                    return result;
                }
                if (!this.ShouldRegenerate(cachedPawn, quest))
                {
                    return result;
                }
                site.RemoveMainPawnCache(this.dataName);
            }
            if (this.TryGetPawnFromDatabase(quest, out Pawn databasePawn))
            {
                site?.SetMainPawn(this.dataName, databasePawn);
                this.TrySpawnCachedPawn(databasePawn, position, map, questTag, quest, lord, result);
                return result;
            }
            Dictionary<string, TargetInfo> generated = this.SpawnNewPawn(position, map, questTag, quest, lord, setLord);
            Pawn pawn = this.GetFirstPawn(generated);
            if (site != null && pawn != null)
            {
                site.SetMainPawn(this.dataName, pawn);
            }
            return generated;
        }

        public override bool CanSaveToMap()
        {
            return this.spawnData != null && this.spawnData.CanSaveToMap();
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.allowGetFromQuestDatabase)
            {
                result.Add(new XElement("allowGetFromQuestDatabase", this.allowGetFromQuestDatabase));
            }
            if (this.allowGetFromGlobalDatabase)
            {
                result.Add(new XElement("allowGetFromGlobalDatabase", this.allowGetFromGlobalDatabase));
            }
            if (!this.questDatabaseKey.NullOrEmpty())
            {
                result.Add(new XElement("questDatabaseKey", this.questDatabaseKey));
            }
            if (!this.globalDatabaseKey.NullOrEmpty())
            {
                result.Add(new XElement("globalDatabaseKey", this.globalDatabaseKey));
            }
            if (this.spawnData != null)
            {
                result.Add(this.spawnData.SaveToXElement("spawnData"));
            }
            if (this.regenerateIfDead)
            {
                result.Add(new XElement("regenerateIfDead", this.regenerateIfDead));
            }
            if (!this.generateConditions.NullOrEmpty())
            {
                result.Add(CQFSerialization.SaveList_Saveable<DialogCondition>(this.generateConditions, "generateConditions"));
            }
            if (!this.regenerateConditions.NullOrEmpty())
            {
                result.Add(CQFSerialization.SaveList_Saveable<DialogCondition>(this.regenerateConditions, "regenerateConditions"));
            }
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.allowGetFromQuestDatabase, "allowGetFromQuestDatabase");
            Scribe_Values.Look(ref this.allowGetFromGlobalDatabase, "allowGetFromGlobalDatabase");
            Scribe_Values.Look(ref this.questDatabaseKey, "questDatabaseKey");
            Scribe_Values.Look(ref this.globalDatabaseKey, "globalDatabaseKey");
            Scribe_Deep.Look(ref this.spawnData, "spawnData");
            Scribe_Values.Look(ref this.regenerateIfDead, "regenerateIfDead");
            Scribe_Collections.Look(ref this.generateConditions, "generateConditions", LookMode.Deep);
            Scribe_Collections.Look(ref this.regenerateConditions, "regenerateConditions", LookMode.Deep);
        }

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.MainPawnSpawnData.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawMainPawnOptions(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.MainPawnSpawnData.DrawMainPawnOptions(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawSpawnDataSelector(ref float y, float x, Rect inRect)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                inRect
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.MainPawnSpawnData.DrawSpawnDataSelector(Ref:float,None:float,None:UnityEngine.Rect)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawConditionList(ref float y, float x, Rect inRect, List<DialogCondition> conditions, string title)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                inRect,
                conditions,
                title
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.MainPawnSpawnData.DrawConditionList(Ref:float,None:float,None:UnityEngine.Rect,None:System.Collections.Generic.List<QuestEditor_Library.DialogCondition>,None:string)", this, arguments);
            y = (float)arguments[0];
        }
        private bool ConditionsSatisfied(List<DialogCondition> conditions, Quest quest)
        {
            if (conditions.NullOrEmpty())
            {
                return true;
            }
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            foreach (DialogCondition condition in conditions)
            {
                if (condition != null && !condition.Satisfied(targets, out string reason, quest))
                {
                    return false;
                }
            }
            return true;
        }

        private bool ShouldRegenerate(Pawn pawn, Quest quest)
        {
            if (pawn == null)
            {
                return true;
            }
            if (pawn.Spawned)
            {
                return false;
            }
            if (pawn.Dead && !this.regenerateIfDead)
            {
                return false;
            }
            return !this.regenerateConditions.NullOrEmpty() && this.ConditionsSatisfied(this.regenerateConditions, quest);
        }

        private bool TryGetPawnFromDatabase(Quest quest, out Pawn pawn)
        {
            pawn = null;
            string questKey = this.questDatabaseKey.NullOrEmpty() ? this.dataName : this.questDatabaseKey;
            string globalKey = this.globalDatabaseKey.NullOrEmpty() ? this.dataName : this.globalDatabaseKey;
            if (this.allowGetFromQuestDatabase)
            {
                TargetInfo target = GameTools.GetTargetFromQuestDatabase(quest, questKey);
                pawn = target.Thing as Pawn;
                if (pawn != null)
                {
                    return true;
                }
            }
            if (this.allowGetFromGlobalDatabase)
            {
                TargetInfo target = GameTools.GetTargetFromGlobalDatabase(quest, globalKey);
                pawn = target.Thing as Pawn;
                if (pawn != null)
                {
                    return true;
                }
            }
            return false;
        }

        private Dictionary<string, TargetInfo> SpawnNewPawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord, bool setLord)
        {
            if (this.spawnData == null)
            {
                this.spawnData = new PawnSpawnData();
            }
            string oldDataName = this.spawnData.dataName;
            this.spawnData.dataName = this.dataName;
            Dictionary<string, TargetInfo> result = this.spawnData.Spawn(position, map, questTag, quest, lord, setLord);
            this.spawnData.dataName = oldDataName;
            return result;
        }

        private bool TrySpawnCachedPawn(Pawn pawn, IntVec3 position, Map map, string questTag, Quest quest, Lord lord, Dictionary<string, TargetInfo> result)
        {
            if (pawn == null || pawn.Spawned || pawn.Dead || map == null || !position.InBounds(map))
            {
                return false;
            }
            GenSpawn.Spawn(pawn, position, map);
            lord?.AddPawn(pawn);
            result.SetOrAdd(this.dataName + ".0", pawn);
            GameComponent_Editor.Instance.GetQuestData(quest)?.AddGroup(this.dataName, new List<Pawn> { pawn });
            return true;
        }

        private Pawn GetFirstPawn(Dictionary<string, TargetInfo> targets)
        {
            if (targets == null)
            {
                return null;
            }
            return targets.Values.Select(target => target.Thing as Pawn).FirstOrDefault(pawn => pawn != null);
        }

        public bool allowGetFromQuestDatabase;
        public bool allowGetFromGlobalDatabase;
        public string questDatabaseKey;
        public string globalDatabaseKey;
        public PawnSpawnData spawnData;
        public bool regenerateIfDead;
        public List<DialogCondition> generateConditions = new List<DialogCondition>();
        public List<DialogCondition> regenerateConditions = new List<DialogCondition>();
    }
}
