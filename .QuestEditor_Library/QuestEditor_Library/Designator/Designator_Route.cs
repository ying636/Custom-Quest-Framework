using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_Route : Designator
    {
        public Designator_Route()
        {
            this.defaultLabel = "DesignatorRoute".Translate();
            this.icon = CQFEditorTools.icon_Route;
            this.defaultDesc = "DesignatorRouteDesc".Translate();
            this.useMouseIcon = true;
            this.tutorTag = "Route";
        }
        public Material RoutePoint 
        {
            get 
            {
                if (this.routePoint == null) 
                {
                    this.routePoint = MaterialPool.MatFrom("UI/Icons/Icon_Route");
                }
                return this.routePoint;
            }
        }
        public override bool Visible => DebugSettings.godMode;
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (loc.InBounds(Find.CurrentMap))
            {
                if (this.curRoute.Contains(loc)) 
                {
                    this.curRoute.Remove(loc);
                    return;
                }
                this.curRoute.Add(loc);      
            }
        }
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions 
        {
            get 
            {
                yield return new FloatMenuOption("ManageRoute".Translate(),() => Find.WindowStack.Add(new Dialog_ManageRoute()));
                foreach (KeyValuePair<string, Route> route in Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route) 
                {
                    yield return new FloatMenuOption(route.Key, () => { this.curRoute = route.Value.route;Find.DesignatorManager.Select(this); });
                }
                yield break;
            }
        }
        public override void ProcessInput(UnityEngine.Event ev)
        {
            return;
        }
        public override void SelectedUpdate()
        {
            base.SelectedUpdate();
            Vector3? last = null;
            Matrix4x4 matrix4X4 = default(Matrix4x4);
            foreach (IntVec3 intvec in this.curRoute)
            {
                Vector3 pos = intvec.ToVector3();
                pos.x += 0.5f;
                pos.z += 0.5f;
                pos.y += 0.5f;
                matrix4X4.SetTRS(pos, Quaternion.identity, new Vector3(1f,5f,1f));
                Graphics.DrawMesh(MeshPool.plane10, matrix4X4,this.RoutePoint, 1);
                if (last == null)
                {
                    last = new Vector3(intvec.x + 0.5f, intvec.y + 5f, intvec.z + 0.5f);
                    continue;
                }
                GenDraw.DrawLineBetween(last.Value, pos);
                last = pos;
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public Material routePoint = null;
        public List<IntVec3> curRoute;
    }
}
