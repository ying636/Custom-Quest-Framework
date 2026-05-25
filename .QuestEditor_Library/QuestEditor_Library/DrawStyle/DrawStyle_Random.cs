using RimWorld;
using Verse;

namespace QuestEditor_Library;

public class DrawStyle_RandomLittleShape : DrawStyle
{
    public override void Update(IntVec3 origin, IntVec3 target, List<IntVec3> buffer)
    {
        buffer.AddRange(GridShapeMaker.UnnaturalShape(origin, Find.CurrentMap,Rand.Range(3,6)));
    }
}

public class DrawStyle_RandomMediumShape : DrawStyle
{
    public override void Update(IntVec3 origin, IntVec3 target, List<IntVec3> buffer)
    {
        buffer.AddRange(GridShapeMaker.UnnaturalShape(origin, Find.CurrentMap,Rand.Range(8,16)));
    }
}


public class DrawStyle_RandomLittleLump : DrawStyle
{
    public override void Update(IntVec3 origin, IntVec3 target, List<IntVec3> buffer)
    {
        buffer.AddRange(GridShapeMaker.IrregularLump(origin, Find.CurrentMap,Rand.Range(3,6)));
    }
}

public class DrawStyle_RandomMediumLump : DrawStyle
{
    public override void Update(IntVec3 origin, IntVec3 target, List<IntVec3> buffer)
    {
        buffer.AddRange(GridShapeMaker.IrregularLump(origin, Find.CurrentMap,Rand.Range(8,16)));
    }
}