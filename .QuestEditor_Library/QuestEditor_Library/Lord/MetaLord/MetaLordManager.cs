using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class MetaLordManager : IExposable
    {
        public void Tick() 
        {
            foreach (var lord in metaLords)
            {
                lord.Tick();
            }
        }
        public MetaLord MakeLord(MetaLordEventDef eventDef) 
        {
            MetaLord lord = new MetaLord();
            lord.loadID = Find.UniqueIDsManager.GetNextLordID();
            lord.eventDef = eventDef;
            this.metaLords.Add(lord);
            return lord;
        }
        public void ExposeData()
        { 
            Scribe_Collections.Look(ref this.metaLords, "metaLords", LookMode.Deep);
        }

        List<MetaLord> metaLords = new List<MetaLord>();
    }
}
