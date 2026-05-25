using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class CustomTrap_Dev : CustomTrap
    {
        public override string Label => DebugSettings.godMode ? base.Label : "";
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (DebugSettings.godMode)
            {
                base.DrawAt(drawLoc, flip);
            }
        }
    }
}
