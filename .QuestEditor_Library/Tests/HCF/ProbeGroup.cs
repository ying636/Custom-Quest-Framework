using HiveCreatureFramework;
using Verse;

internal sealed class ProbeGroup : UnitGroup
{
    public override void AcceptUnit(Pawn unit)
    {
        this.JoinCalls++;
        this.units.Add(unit);
        HCFGameUtility.GetUnitComp(unit).group = this;
        this.unitCost += HCFGameUtility.GetUnitComp(unit).Props.groupCost;
    }

    public override void SetMode(GroupWorkModeDef mode, bool isPlayer = true)
    {
        this.ModeCalls++;
        this.PlayerCommand = isPlayer;
        this.workModeDef = mode;
    }

    public int JoinCalls;
    public int ModeCalls;
    public bool PlayerCommand;
}
