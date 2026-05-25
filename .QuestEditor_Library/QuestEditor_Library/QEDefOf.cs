using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    [StaticConstructorOnStartup]
    [DefOf]
    public class QEDefOf
    {
        public static DesignationDef QE_Disdestroy;
        public static DesignationDef QE_Disgenerate;
        public static DesignationDef QE_MoveIn;
        public static DesignationDef QE_MoveOut;
        public static DesignationDef QE_MoveToRoot;

        public static SitePartDef QE_CustomSite;
        public static ThingDef QE_Spawner_Editor;
        public static ThingDef QE_ZoneCore;
        public static ThingDef QE_GenerationActionWorker;

        public static JobDef QE_EnterOrExitSubMap;
        public static JobDef QE_StartDialog;
        public static JobDef QE_Patrol;
        public static JobDef QE_Open;
        public static JobDef QE_DisarmTrap;
        public static JobDef QE_InteractingWithTarget;
        public static JobDef QE_MoveInTargetToSubMap;
        public static JobDef QE_MoveTargetOutOfSubMap;
        public static JobDef QE_Landfill;

        public static DutyDef QE_Duty_Guard;
        public static DutyDef QE_Duty_Waiter;
        public static MapGeneratorDef QE_CustomMap_Editor_Generator;
        public static MapGeneratorDef CQF_SpecialMapGenerator;
        public static MapGeneratorDef CQF_Base_Player;
        public static WorldObjectDef QE_CustomMap_SubMap; 
        public static WorldObjectDef CQF_CustomSite;
        [MayRequireOdyssey]
        public static WorldObjectDef QE_SpaceCustomSite;
        [MayRequireOdyssey]
        public static WorldObjectDef QE_CustomMap_SpaceSubMap;
        
        public static GenStepDef QE_CustomSite_GenStep;

        public static DutyDef QE_Duty_MoveLevel;


        public static DrawStyleCategoryDef CQF_Areas;
    }
}
