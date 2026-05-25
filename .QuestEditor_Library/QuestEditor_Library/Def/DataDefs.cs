using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class LootDataDef : Def
    {
        public List<LootData> loots = new List<LootData>();
    }
    public class InteractionDataDef : Def
    {
        public List<InteractionOperation> interactions = new List<InteractionOperation>();
    }
}
