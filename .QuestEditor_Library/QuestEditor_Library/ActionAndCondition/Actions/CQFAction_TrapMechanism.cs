#if RIMWORLD_1_7
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public enum CQFTrapCommand
{
    Start,
    Disable,
    Enable
}

public sealed class CQFAction_TrapMechanism : CQFAction_Target
{
    public CQFAction_TrapMechanism()
    {
        this.targetsText = new List<string> { "CustomThing" };
    }

    public override CQFActionCategory ActionCategory => CQFActionCategory.ThingChange;

    public override void Draw(ref float y, Rect inRect, float x)
    {
        base.Draw(ref y, inRect, x);
        Rect button = new Rect(x, y, Mathf.Clamp(inRect.xMax - x - 12f, 80f, 320f), 26f);
        if (CQFUIStyle.ButtonText(button, ("CQF_TrapCommand_" + this.command).Translate()))
        {
            CQFEditorTools.DrawFloatMenu(new List<CQFTrapCommand>(Enum.GetValues<CQFTrapCommand>()),
                command => this.command = command, command => ("CQF_TrapCommand_" + command).Translate().ToString());
        }
        y += 32f;

        if (this.command != CQFTrapCommand.Start)
            return;
        button.y = y;
        if (CQFUIStyle.ButtonText(button, this.program?.LabelCap.ToString() ?? "CQF_TrapDefaultProgram".Translate().ToString()))
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_TrapDefaultProgram".Translate(), () => this.program = null)
            };
            foreach (CQFTrapProgramDef program in DefDatabase<CQFTrapProgramDef>.AllDefsListForReading)
                options.Add(new FloatMenuOption(program.LabelCap, () => this.program = program));
            Find.WindowStack.Add(new FloatMenu(options));
        }
        y += 32f;
    }

    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        Pawn? trigger = targets != null && (targets.TryGetValue("Trigger", out TargetInfo context)
            || targets.TryGetValue(SignalContactKey, out context)) ? context.Thing as Pawn : null;
        this.Apply(GameTools.GetTargets(targets, quest, this.targetsText), trigger);
    }

    public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        this.Apply(targets, null);
    }

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        result.Add(new XElement("command", this.command));
        if (this.program != null)
            result.Add(new XElement("program", this.program.defName));
        return result;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref this.command, "command");
        Scribe_Defs.Look(ref this.program, "program");
    }

    private void Apply(Dictionary<string, TargetInfo> targets, Pawn? trigger)
    {
        foreach (TargetInfo target in targets.Values)
        {
            if (target.Thing is not Thing thing)
            {
                Log.WarningOnce("CQF_TrapTargetNotThing".Translate(), 1943810423);
                continue;
            }
            Comp_CQFTrapMechanism mechanism = thing.TryGetComp<Comp_CQFTrapMechanism>();
            if (mechanism == null)
            {
                Log.WarningOnce("CQF_TrapTargetNotMechanism".Translate(thing.def.defName), thing.def.shortHash ^ 1943810424);
                continue;
            }
            switch (this.command)
            {
                case CQFTrapCommand.Start:
                    mechanism.Start(this.program, trigger);
                    break;
                case CQFTrapCommand.Disable:
                    mechanism.SetEnabled(false);
                    break;
                case CQFTrapCommand.Enable:
                    mechanism.SetEnabled(true);
                    break;
            }
        }
        if (targets.Count == 0)
            Log.WarningOnce("CQF_TrapTargetMissing".Translate(), 1943810425);
    }

    public CQFTrapCommand command;
    public CQFTrapProgramDef? program;
    internal const string SignalContactKey = "CQF_TrapContact";
}
#endif
