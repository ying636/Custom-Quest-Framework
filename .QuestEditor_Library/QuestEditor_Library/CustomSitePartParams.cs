using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using RimWorld.Planet;
using RimWorld.QuestGen;

namespace QuestEditor_Library
{
    public class CustomSitePartParams : SitePartParams
    {
        public IntVec3 spot;
        public CustomMapDataDef mapData;
        public Quest quest;
        public bool isSubMap = false;
        public bool replaceMapGeneration = false;
        public bool dev = false;
    }
}
