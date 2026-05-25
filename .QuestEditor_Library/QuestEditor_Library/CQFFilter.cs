using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Expando;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFFilter : ISaveable, IDrawable, IExposable
    {
        public void Draw(ref float y, Rect inRect, float x)
        {
            throw new NotImplementedException();
        }

        public void ExposeData()
        {
            throw new NotImplementedException();
        }

        public XElement SaveToXElement(string nodeName)
        {
            throw new NotImplementedException();
        }

        public TechLevel techLevel = TechLevel.Undefined;
        public float massLimit = 0f;
        public float massRequired = 0f;
        public float count = 0f;
        public List<ThingDef> allowedThing = new List<ThingDef>();
        public List<ThingCategoryDef> allowedCategory = new List<ThingCategoryDef>();
    }
}
