using System.Reflection;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFComponentOverride : IExposable
    {
        public CQFComponentOverride() { }
        public CQFComponentOverride(ThingComp component, int index)
        {
            thing = component.parent; componentIndex = index; componentType = component.GetType().FullName ?? "";
            if (component is CompTriggerDialog trigger) { triggerSignal = trigger.Props.triggerSignal; dialog = trigger.Props.dialog; }
            else if (component is CompHackOutcome hack) outcomes = hack.Props.outcoomes;
            else if (component is CompLandFillable fill) { tickToFill = fill.Props.tickToFill; filled = fill.Props.filled; iconPath = fill.Props.iconPath; landfillText = fill.Props.landfillText; }
            else if (component is CompSetMapAndGenerate generate) { exitKey = generate.Props.key; mapChoice = generate.Props.map; }
            else throw new InvalidOperationException("CQF_ComponentOverrideInvalid: " + componentType);
        }
        public bool Apply()
        {
            if (thing == null || thing.Destroyed || componentIndex < 0 || componentIndex >= thing.AllComps.Count) return false;
            ThingComp component = thing.AllComps[componentIndex];
            if (component.GetType().FullName != componentType || !Supported(component)) return false;
            CompProperties clone = (CompProperties)typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(component.props, null)!;
            if (component is CompTriggerDialog) { CompPropertiesTriggerDialog props = (CompPropertiesTriggerDialog)clone; props.triggerSignal = triggerSignal!; props.dialog = dialog!; }
            else if (component is CompHackOutcome) ((CompPropertiesHackOutcome)clone).outcoomes = outcomes ?? new List<CQFAction>();
            else if (component is CompLandFillable)
            {
                if (tickToFill < 1 || filled == null) return false;
                CompPropertiesLandFillable props = (CompPropertiesLandFillable)clone; props.tickToFill = tickToFill; props.filled = filled; props.iconPath = iconPath!; props.landfillText = landfillText!;
                typeof(CompPropertiesLandFillable).GetField("icon", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(props, null);
            }
            else if (component is CompSetMapAndGenerate) { CompPropertiesSetMapAndGenerate props = (CompPropertiesSetMapAndGenerate)clone; props.key = exitKey!; props.map = mapChoice!; }
            component.props = clone;
            return true;
        }
        public void ExposeData()
        {
            Scribe_References.Look(ref thing, "thing"); Scribe_Values.Look(ref componentIndex, "componentIndex", -1); Scribe_Values.Look(ref componentType, "componentType", "");
            Scribe_Values.Look(ref triggerSignal, "triggerSignal"); Scribe_Defs.Look(ref dialog, "dialog"); Scribe_Collections.Look(ref outcomes, "outcomes", LookMode.Deep);
            Scribe_Values.Look(ref tickToFill, "tickToFill"); Scribe_Defs.Look(ref filled, "filled"); Scribe_Values.Look(ref iconPath, "iconPath"); Scribe_Values.Look(ref landfillText, "landfillText");
            Scribe_Values.Look(ref exitKey, "exitKey"); Scribe_Deep.Look(ref mapChoice, "mapChoice");
        }
        public static bool Supported(ThingComp component) => component is CompTriggerDialog or CompHackOutcome or CompLandFillable or CompSetMapAndGenerate;
        public ThingWithComps? thing;
        public int componentIndex = -1;
        public string componentType = "";
        public string? triggerSignal;
        public DialogTreeDef? dialog;
        public List<CQFAction>? outcomes;
        public int tickToFill;
        public ThingDef? filled;
        public string? iconPath;
        public string? landfillText;
        public string? exitKey;
        public CustomMapGenerationSet? mapChoice;
    }
}
