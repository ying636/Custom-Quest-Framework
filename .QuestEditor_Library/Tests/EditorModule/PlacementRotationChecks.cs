using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using QuestEditor_Library;
using UnityEngine;
using Verse;

internal static class PlacementRotationChecks
{
    public static void Run(Assembly editor)
    {
        Harmony harmony = new Harmony("CQF.Checks.PlacementRotation");
        Event previousEvent = Event.current;
        DrawStyleCategoryDef previousAreas = QEDefOf.CQF_Areas;
        try
        {
            QEDefOf.CQF_Areas = new DrawStyleCategoryDef { defName = "CQF_Check_Areas" };
            harmony.Patch(AccessTools.Method(typeof(Designator_Place), nameof(Designator_Place.SelectedProcessInput)),
                prefix: new HarmonyMethod(typeof(PlacementRotationChecks), nameof(RecordNativeInput)));
            harmony.Patch(AccessTools.Method(typeof(Designator), nameof(Designator.SelectedProcessInput)),
                prefix: new HarmonyMethod(typeof(PlacementRotationChecks), nameof(RecordStyleInput)));
            harmony.Patch(AccessTools.Method(typeof(Designator_Place), nameof(Designator_Place.DoExtraGuiControls)),
                prefix: new HarmonyMethod(typeof(PlacementRotationChecks), nameof(RecordRotationGui)));
            harmony.Patch(AccessTools.Method(typeof(Designator), nameof(Designator.DoExtraGuiControls)),
                prefix: new HarmonyMethod(typeof(PlacementRotationChecks), nameof(RecordStyleGui)));
            FieldInfo rotation = AccessTools.Field(typeof(Designator_Place), "placingRot");
            foreach (string name in new[] { "Designator_SpawnThing", "Designator_CQFTools" })
            {
                Type type = editor.GetType("QuestEditor_Library." + name, true)!;
                Designator placement = (Designator)RuntimeHelpers.GetUninitializedObject(type);
                AccessTools.Field(type.BaseType, "areaControls").SetValue(placement,
                    RuntimeHelpers.GetUninitializedObject(editor.GetType("QuestEditor_Library.Designator_CQFAreaControls", true)!));
                FieldInfo selection = type.GetField("thing")!;
                object? previous = selection.GetValue(null);
                try
                {
                    ThingDef item = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                    item.defName = "CQF_Check_RotationItem";
                    item.rotatable = true;
                    selection.SetValue(null, item);
                    rotation.SetValue(placement, Rot4.North);
                    nativeCalls = 0;
                    styleCalls = 0;
                    rotationGuiCalls = 0;
                    styleGuiCalls = 0;
                    foreach (ThingCategory category in new[] { ThingCategory.Item, ThingCategory.Plant, ThingCategory.Filth })
                    {
                        item.category = category;
                        styleCalls = 0;
                        styleGuiCalls = 0;
                        Check(placement.DrawStyleCategory == QEDefOf.CQF_Areas,
                            name + " uses area styles for " + category + " with rotatable true");
                        Input(EventType.KeyDown, KeyCode.E);
                        Input(EventType.KeyDown, KeyCode.Q);
                        Input(EventType.MouseDown, KeyCode.None, 2);
                        Input(EventType.MouseUp, KeyCode.None, 2);
                        Check(nativeCalls == 0 && styleCalls == 4 && (Rot4)rotation.GetValue(placement)! == Rot4.North,
                            name + " routes all " + category + " input to draw styles without rotating");
                        placement.DoExtraGuiControls(0f, 100f);
                        Check(styleGuiCalls == 1 && rotationGuiCalls == 0,
                            name + " shows draw style controls for " + category);
                        Check(item.rotatable && item.category == category,
                            name + " preserves original " + category + " definition");
                    }
                    item.category = ThingCategory.Building;
                    item.building = new BuildingProperties { isAttachment = true };
                    Input(EventType.KeyDown, KeyCode.E);
                    Check(nativeCalls == 1, name + " preserves native wall attachment input");
                    placement.DoExtraGuiControls(0f, 100f);
                    Check(styleGuiCalls == 1 && rotationGuiCalls == 1 && placement.DrawStyleCategory == null,
                        name + " buildings keep rotation controls without area controls");
                    item.rotatable = false;
                    styleCalls = 0;
                    Input(EventType.KeyDown, KeyCode.E);
                    placement.DoExtraGuiControls(0f, 100f);
                    Check(nativeCalls == 1 && styleCalls == 1 && styleGuiCalls == 2 && rotationGuiCalls == 1,
                        name + " nonrotatable buildings use draw styles for controls and input");
                }
                finally
                {
                    selection.SetValue(null, previous);
                }

                void Input(EventType eventType, KeyCode key, int button = 0)
                {
                    Event.current = new Event { type = eventType, keyCode = key, button = button };
                    placement.SelectedProcessInput(Event.current);
                }
            }
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
            Event.current = previousEvent;
            QEDefOf.CQF_Areas = previousAreas;
        }
    }

    private static bool RecordNativeInput()
    {
        nativeCalls++;
        return false;
    }

    private static bool RecordStyleInput(Designator __instance)
    {
        Check(__instance.DrawStyleCategory == QEDefOf.CQF_Areas, "style input uses CQF area category");
        styleCalls++;
        return false;
    }

    private static bool RecordRotationGui()
    {
        rotationGuiCalls++;
        return false;
    }

    private static bool RecordStyleGui(Designator __instance)
    {
        Check(__instance.DrawStyleCategory == QEDefOf.CQF_Areas, "style controls use CQF area category");
        styleGuiCalls++;
        return false;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }

    private static int nativeCalls;
    private static int styleCalls;
    private static int rotationGuiCalls;
    private static int styleGuiCalls;
}
