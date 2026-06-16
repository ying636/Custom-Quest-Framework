using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class ComplexPawnDef : Def, ISaveable
    {
        public Pawn GetPawn()
        {
            if (this.Unique && GameComponent_Editor.Instance is GameComponent_Editor component
                && component.pawns.TryGetValue(this.defName, out Pawn result))
            {
                return result;
            }
            return this.Spawn();
        }

        public Pawn Spawn()
        {
            return this.CreatePawn(true);
        }

        public Pawn CreatePreviewPawn()
        {
            return this.CreatePawn(false);
        }

        private Pawn CreatePawn(bool cacheUnique)
        {
            if (this.KindDef == null)
            {
                Log.Error("QuestEditorError:Spawn ComplexPawnDef without PawnKindDef");
                return null;
            }
            PawnGenerationRequest request = new PawnGenerationRequest(this.KindDef, this.GetFaction());
            foreach (PawnModDef mod in this.AvailableMods())
            {
                mod.Worker.ModifyGenerationRequest(this, ref request);
            }
            Pawn result = PawnGenerator.GeneratePawn(request);
            this.ApplyModsToPawn(result, false);
            if (cacheUnique && this.Unique && GameComponent_Editor.Instance is GameComponent_Editor component)
            {
                component.pawns.SetOrAdd(this.defName, result);
            }
            return result;
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("defName", this.defName));
            if (!this.label.NullOrEmpty())
            {
                result.Add(new XElement("label", this.label));
            }
            List<PawnModData> datas = this.modDatas.Where(data => data?.ModDef != null).OrderBy(data => data.ModDef.order).ToList();
            if (!datas.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(datas, "modDatas"));
            }
            return result;
        }

        public void LoadModData(XmlNode node)
        {
            if (node.SelectSingleNode("modDatas") != null)
            {
                return;
            }
            foreach (PawnModDef mod in this.AvailableMods())
            {
                mod.Worker.LoadData(this, node);
            }
        }

        public List<PawnModDef> AvailableMods()
        {
            return DefDatabase<PawnModDef>.AllDefsListForReading
                .Where(def => def.Worker.CanAddFor(this))
                .OrderBy(def => def.order)
                .ToList();
        }

        public T DataFor<T>() where T : PawnModData, new()
        {
            T result = this.modDatas.OfType<T>().FirstOrDefault();
            if (result == null)
            {
                result = new T();
                this.modDatas.Add(result);
            }
            return result;
        }

        public PawnModData DataFor(PawnModDef mod)
        {
            PawnModData result = this.modDatas.FirstOrDefault(data => data?.ModDef == mod);
            if (result == null)
            {
                result = mod.Worker.CreateData();
                this.modDatas.Add(result);
            }
            return result;
        }

        public void ApplyModsToPawn(Pawn pawn, bool preview)
        {
            foreach (PawnModDef mod in this.AvailableMods())
            {
                mod.Worker.ApplyToPawn(this, pawn, preview);
            }
        }

        public void NotifyPawnSpawned(Pawn pawn, Quest quest)
        {
            foreach (PawnModDef mod in this.AvailableMods())
            {
                mod.Worker.OnPawnSpawned(this, pawn, quest);
            }
        }

        private Faction GetFaction()
        {
            FactionDef faction = this.DataFor<PawnModData_Basic>().faction;
            if (faction == null)
            {
                return null;
            }
            return faction.isPlayer ? Find.FactionManager.OfPlayer : Find.FactionManager.FirstFactionOfDef(faction);
        }

        public bool Unique => this.DataFor<PawnModData_Basic>().unique;

        public PawnKindDef KindDef => this.DataFor<PawnModData_Basic>().kindDef;

        public List<PawnModData> modDatas = new List<PawnModData>();
    }
}

