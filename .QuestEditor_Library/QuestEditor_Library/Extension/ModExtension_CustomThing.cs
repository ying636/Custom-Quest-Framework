using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class ModExtension_CustomThing : DefModExtension
    {
        public bool showInnerThings = true;
        public GraphicData openedGraphicdata;
        [MustTranslate]
        public string openedDesc;
        public GraphicData captureTrapGraphicdata_Back;
        public GraphicData captureTrapGraphicdata_Front;
        
        
        public Vector3 caturedDrawOffset;
        public FloatRange pawnAngle = FloatRange.Zero;
    }
}
