using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModDef : Def
    {
        public PawnModWorker Worker
        {
            get
            {
                if (this.workerInt == null)
                {
                    this.workerInt = (PawnModWorker)Activator.CreateInstance(this.workerClass);
                    this.workerInt.def = this;
                }
                return this.workerInt;
            }
        }

        public string EditorLabel => this.TranslateOrFallback(this.defName + ".label", this.LabelCap);

        public string EditorDescription => this.TranslateOrFallback(this.defName + ".description", this.description);

        public Type workerClass = typeof(PawnModWorker);
        public int order;
        private PawnModWorker workerInt;

        private string TranslateOrFallback(string key, string fallback)
        {
            return key.CanTranslate() ? key.Translate().ToString() : fallback;
        }
    }
}
