using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_ManageRoute : Window 
    {
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(450f, 400f);
            }
        }


        public override void DoWindowContents(Rect inRect)
        {
            float y = 30f;
            if (Widgets.CloseButtonFor(inRect))
            {
                this.Close();
            }
            List<string> remove = new List<string>();
            foreach (KeyValuePair<string,Route> curRoute in Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route) 
            {
                Widgets.Label(new Rect(3f,y,150f,25f),curRoute.Key);
                if (Widgets.ButtonText(new Rect(250f,y,70f,25f), "RemoveRoute".Translate())) 
                {
                    remove.Add(curRoute.Key);
                }
                if (Widgets.ButtonText(new Rect(330f, y, 70f, 25f), "Rename".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_RenameForQE(n => this.rename.Add(curRoute.Key, n)));
                }
                y += 30f;
            }
            Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.RemoveAll((x) => remove.Contains(x.Key));
            foreach (KeyValuePair<string, string> curRoute in this.rename)
            {
                if (Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.TryGetValue(curRoute.Key, out Route value))
                {
                    Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.Add(curRoute.Value,new Route() {route = value.route.ListFullCopy()} );
                    Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.Remove(curRoute.Key);
                }
            }
            this.rename.Clear();
            if (Widgets.ButtonText(new Rect(3f, y, 100f, 25f), "AddRoute".Translate())) 
            {
                bool added = false;
                int i = 0;
                while (!added) 
                {
                    string name = "DesignatorRoute".Translate() + i;
                    if (Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.ContainsKey(name)) 
                    {
                        i++;
                        continue;
                    }
                    Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.Add(name,new Route() {route = new List<IntVec3>()} );
                    added = true;
                }
            }
        }

        public Dictionary<string, string> rename = new Dictionary<string, string>();
    }
}
