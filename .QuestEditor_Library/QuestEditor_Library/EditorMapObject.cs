using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using RimWorld.Planet;

namespace QuestEditor_Library
{
    public class EditorMapObject : MapParent
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.mapDef, "mapDef");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (!CQFEditorBridge.IsLoaded) yield break;
            yield return new Command_Action
            {
                defaultLabel = "DEV:Destroy this",
                action = delegate ()
                {
                    this.Destroy();
                }
            };
            yield break;
        }
        public CustomMapDataDef mapDef;
    }
}
