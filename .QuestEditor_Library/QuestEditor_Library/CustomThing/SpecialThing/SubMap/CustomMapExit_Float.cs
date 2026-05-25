using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class CustomMapExit_Float : CustomMapExit
{
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        drawLoc.z += Mathf.Sin(Find.TickManager.TicksGame / 60f)/ 4f;
        base.DrawAt(drawLoc, flip);
    }
}