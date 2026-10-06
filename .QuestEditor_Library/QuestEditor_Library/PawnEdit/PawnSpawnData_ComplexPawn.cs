using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class PawnSpawnData_ComplexPawn : PawnSpawnData
    {
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnSpawnData_ComplexPawn.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override bool CanSaveToMap()
        {
            return this.pawnDef != null;
        }

        public override Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null, bool setLord = true)
        {
            if (this.pawnDef == null || !Rand.Chance(this.generationChance) || map == null || !position.InBounds(map))
            {
                return null;
            }
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            if (setLord && lord == null && !this.lordDataName.NullOrEmpty())
            {
                MapComponent_CustomMapData.GetComp(map)?.TryGetLord(this.lordDataName, out lord);
            }
            int count = this.count.RandomInRange;
            List<Pawn> pawns = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = this.pawnDef.GetPawn();
                if (pawn == null)
                {
                    continue;
                }
                if (pawn.Spawned)
                {
                    result.SetOrAdd(this.dataName + "." + i, pawn);
                    continue;
                }
                pawns.Add(pawn);
                this.ActionAfterGeneration(pawn, quest, i, questTag);
                lord?.AddPawn(pawn);
                result.SetOrAdd(this.dataName + "." + i, pawn);
            }
            this.SpawnPnaw(pawns, position, map);
            foreach (Pawn pawn in pawns)
            {
                this.pawnDef.NotifyPawnSpawned(pawn, quest);
            }
            if (this.dataName != "undefined")
            {
                GameComponent_Editor.Instance.GetQuestData(quest)?.AddGroup(this.dataName, result.Values.Select(target => target.Thing as Pawn).Where(pawn => pawn != null).ToList());
            }
            return result;
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.pawnDef != null)
            {
                result.Add(new XElement("pawnDef", this.pawnDef.defName));
            }
            if (!this.lordDataName.NullOrEmpty())
            {
                result.Add(new XElement("lordDataName", this.lordDataName));
            }
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.pawnDef, "pawnDef");
        }
        internal string PawnDisplayName(ComplexPawnDef def)
        {
            return def?.label.NullOrEmpty() == false ? def.label : def?.defName;
        }
        internal void OpenLordSelector()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnSpawnData_ComplexPawn.OpenLordSelector()", this, arguments);
        }
        public ComplexPawnDef pawnDef;
    }
}
