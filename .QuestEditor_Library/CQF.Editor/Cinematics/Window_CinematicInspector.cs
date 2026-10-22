using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Window_CinematicInspector : Window
{
    public Window_CinematicInspector(QuestEditor_Cinematics owner)
    {
        this.owner = owner;
        layer = WindowLayer.GameUI;
        doCloseX = true;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        closeOnAccept = false;
        closeOnCancel = false;
        focusWhenOpened = false;
        drawShadow = false;
    }

    public override Vector2 InitialSize => new(Mathf.Min(342f, UI.screenWidth * 0.38f), Mathf.Max(170f, Mathf.Min(480f, owner.windowRect.y - 54f)));
    protected override float Margin => 10f;

    public void RefreshValues()
    {
        buffers.Clear();
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (!owner.IsOpen) Close(false);
    }

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width, inRect.height);
        CQFCinematicStep? step = owner.Selected;
        if (!ReferenceEquals(previousStep, step))
        {
            previousStep = step;
            buffers.Clear();
            scroll = Vector2.zero;
            advanced = false;
        }
        string title = step == null ? "CQF_Cinematic_Inspector".Translate()
            : ("CQF_Cinematic_Step_" + step.GetType().Name.Replace("CQFCinematicStep_", string.Empty)).Translate();
        Widgets.LabelEllipses(new Rect(0f, 0f, inRect.width - 26f, 28f), title.Colorize(CQFUIStyle.Accent));
        Rect viewport = new(0f, 34f, inRect.width, Mathf.Max(35f, inRect.height - 34f));
        Rect content = new(0f, 0f, viewport.width - 18f, Mathf.Max(viewport.height, contentHeight));
        Widgets.BeginScrollView(viewport, ref scroll, content);
        using CQFUIScope contentScope = new(content.width, content.height);
        float y = 0f;
        if (step == null) DrawProject(ref y, content.width);
        else DrawStep(step, ref y, content.width);
        contentHeight = y + 8f;
        Widgets.EndScrollView();
    }

    protected override void SetInitialSizeAndPosition()
    {
        Vector2 size = InitialSize;
        windowRect = new Rect(UI.screenWidth - size.x - 8f, Mathf.Max(8f, owner.windowRect.y - size.y - 8f), size.x, size.y);
    }

    private void DrawProject(ref float y, float width)
    {
        CQFCinematicDef definition = owner.Project.Definition;
        if (Button(ref y, width, owner.Project.Quest?.name ?? "CQF_Cinematic_NoQuest".Translate().ToString()))
        {
            List<FloatMenuOption> options = Find.QuestManager.QuestsListForReading.Where(quest => !quest.Historical)
                .Select(quest => new FloatMenuOption(quest.name, () => owner.Project.Quest = quest)).ToList();
            options.Insert(0, new FloatMenuOption("CQF_Cinematic_NoQuest".Translate(), () => owner.Project.Quest = null));
            Find.WindowStack.Add(new FloatMenu(options));
        }
        Checkbox(ref y, width, "allowSkip", ref definition.allowSkip);
        Checkbox(ref y, width, "restoreCamera", ref definition.restoreCamera);
        Checkbox(ref y, width, "lockInput", ref definition.lockInput);
        Checkbox(ref y, width, "pauseGame", ref definition.pauseGame);
        DrawNumber(ref y, width, "maxDuration", ref definition.maxDuration, 1f, 86400f);
        if (Button(ref y, width, "CQF_Cinematic_Advanced".Translate()))
            Find.WindowStack.Add(new Dialog_EditCinematicStep(definition));
    }

    private void DrawStep(CQFCinematicStep step, ref float y, float width)
    {
        if (step is not CQFCinematicStep_PawnMove and not CQFCinematicStep_PawnJob and not CQFCinematicStep_Sequence and not CQFCinematicStep_Parallel)
            DrawNumber(ref y, width, "duration", ref step.duration, 0f, 86400f);
        if (step is CQFCinematicStep_Subtitle subtitle)
        {
            CQFCinematicSubtitleText text = owner.Project.GetSubtitle(subtitle);
            Widgets.Label(new Rect(0f, y, width, 24f), "CQF_Cinematic_SubtitleChinese".Translate());
            y += 26f;
            string chinese = Widgets.TextArea(new Rect(0f, y, width, 76f), text.Chinese);
            y += 84f;
            Widgets.Label(new Rect(0f, y, width, 24f), "CQF_Cinematic_SubtitleEnglish".Translate());
            y += 26f;
            string english = Widgets.TextArea(new Rect(0f, y, width, 76f), text.English);
            y += 84f;
            if (chinese != text.Chinese || english != text.English) owner.Project.SetSubtitle(subtitle, chinese, english);
        }
        else if (step is CQFCinematicStep_Camera camera)
        {
            if (Button(ref y, width, "CQF_Cinematic_CaptureCamera".Translate())) owner.RecordCamera(camera);
            TargetButton(ref y, width, camera.targetKey, value => camera.targetKey = value);
            DrawNumber(ref y, width, "size", ref camera.size, 1f, 200f);
            Checkbox(ref y, width, "follow", ref camera.follow);
        }
        else if (step is CQFCinematicStep_PawnMove move)
        {
            PawnButton(ref y, width, move.pawnKey, value => move.pawnKey = value);
            if (Button(ref y, width, "CQF_Cinematic_RecordMove".Translate() + " · " + owner.TargetLabel(move.targetKey))) owner.RecordPawnMove(move);
            DrawNumber(ref y, width, "timeout", ref move.timeout, 0.1f, 86400f);
            DrawEnum(ref y, width, "urgency", move.urgency, value => move.urgency = (Verse.AI.LocomotionUrgency)value);
        }
        else if (step is CQFCinematicStep_PawnFace face)
        {
            PawnButton(ref y, width, face.pawnKey, value => face.pawnKey = value);
            if (Button(ref y, width, "CQF_Cinematic_RecordFacing".Translate() + " · " + owner.TargetLabel(face.targetKey))) owner.RecordPawnFacing(face);
        }
        else if (step is CQFCinematicStep_Image image)
        {
            if (!image.imagePath.NullOrEmpty())
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(image.imagePath, false);
                if (texture != null) Widgets.DrawTextureFitted(new Rect(0f, y, width, 112f), texture, 1f);
                y += 118f;
            }
            if (Button(ref y, width, "CQF_Cinematic_ChooseImage".Translate() + (image.imagePath.NullOrEmpty() ? string.Empty : " · " + Path.GetFileName(image.imagePath))))
                OpenImagePicker(value => image.imagePath = value);
        }
        else if (step is CQFCinematicStep_Sound sound)
        {
            if (Button(ref y, width, "CQF_Cinematic_ChooseSound".Translate() + (sound.sound == null ? string.Empty : " · " + DefinitionLabel(sound.sound))))
                OpenDefPicker(typeof(SoundDef), value => sound.sound = (SoundDef)value, "CQF_Cinematic_ChooseSound");
            DrawNumber(ref y, width, "volume", ref sound.volume, 0f, 5f);
            Checkbox(ref y, width, "onCamera", ref sound.onCamera);
            if (!sound.onCamera) TargetButton(ref y, width, sound.targetKey, value => sound.targetKey = value);
        }
        else if (step is CQFCinematicStep_Fade fade)
        {
            float opacity = fade.color.a;
            DrawNumber(ref y, width, "color", ref opacity, 0f, 1f, "CQF_Cinematic_Alpha");
            fade.color = new Color(fade.color.r, fade.color.g, fade.color.b, opacity);
            Checkbox(ref y, width, "fromCurrent", ref fade.fromCurrent);
        }
        else if (step is CQFCinematicStep_Letterbox bars)
        {
            DrawNumber(ref y, width, "height", ref bars.height, 0f, 0.45f);
            Checkbox(ref y, width, "fromCurrent", ref bars.fromCurrent);
        }
        else if (step is CQFCinematicStep_Shake shake)
            DrawNumber(ref y, width, "magnitude", ref shake.magnitude, 0f, 10f);
        else if (step is not CQFCinematicStep_Sequence and not CQFCinematicStep_Parallel)
            DrawFields(step, ref y, width);
        if (Button(ref y, width, "CQF_Cinematic_Advanced".Translate())) advanced = !advanced;
        if (!advanced) return;
        DrawEnum(ref y, width, "clock", step.clock, value => step.clock = (CQFCinematicClock)value);
        if (Button(ref y, width, "CQF_Cinematic_Parameters".Translate()))
            Find.WindowStack.Add(new Dialog_EditCinematicStep(step));
    }

    private void DrawFields(CQFCinematicStep step, ref float y, float width)
    {
        foreach (FieldInfo field in step.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(field => field.DeclaringType != typeof(CQFCinematicStep)))
        {
            object? value = field.GetValue(step);
            if (field.FieldType == typeof(string))
            {
                string current = value as string ?? string.Empty;
                if (field.Name.EndsWith("Key", StringComparison.Ordinal))
                {
                    if (field.Name is "pawnKey" or "interviewerKey" or "intervieweeKey") PawnButton(ref y, width, current, text => field.SetValue(step, text));
                    else TargetButton(ref y, width, current, text => field.SetValue(step, text));
                }
                else
                {
                    Widgets.LabelEllipses(new Rect(0f, y, width, 24f), ("CQF_Cinematic_Field_" + field.Name).Translate());
                    y += 25f;
                    field.SetValue(step, Widgets.TextField(new Rect(0f, y, width, 28f), current));
                    y += 35f;
                }
            }
            else if (typeof(Def).IsAssignableFrom(field.FieldType))
            {
                string label = value is Def definition ? DefinitionLabel(definition) : "CQF_Cinematic_None".Translate().ToString();
                if (Button(ref y, width, ("CQF_Cinematic_Field_" + field.Name).Translate() + " · " + label))
                    OpenDefPicker(field.FieldType, selected => field.SetValue(step, selected), "CQF_Cinematic_Field_" + field.Name);
            }
            else if (field.FieldType == typeof(float))
            {
                float number = value is float present ? present : 0f;
                DrawNumber(ref y, width, field.Name, ref number, 0f, 86400f);
                field.SetValue(step, number);
            }
            else if (field.FieldType == typeof(int))
            {
                float number = value is int present ? present : 0;
                DrawNumber(ref y, width, field.Name, ref number, 0f, 1000000f);
                field.SetValue(step, Mathf.RoundToInt(number));
            }
            else if (field.FieldType == typeof(bool))
            {
                bool flag = value is true;
                Checkbox(ref y, width, field.Name, ref flag);
                field.SetValue(step, flag);
            }
            else if (field.FieldType.IsEnum)
                DrawEnum(ref y, width, field.Name, (Enum)value!, selected => field.SetValue(step, selected));
            else if (field.FieldType == typeof(List<CQFAction>))
            {
                List<CQFAction> actions = (List<CQFAction>?)value ?? new();
                field.SetValue(step, actions);
                CQFActionListEditor.Draw(ref y, 0f, width, new Rect(0f, 0f, width, 100000f), ("CQF_Cinematic_Field_" + field.Name).Translate(), actions);
                y += 8f;
            }
            else if (field.FieldType == typeof(List<DialogCondition>))
            {
                List<DialogCondition> conditions = (List<DialogCondition>?)value ?? new();
                field.SetValue(step, conditions);
                CQFConditionListEditor.Draw(ref y, 0f, width, new Rect(0f, 0f, width, 100000f), ("CQF_Cinematic_Field_" + field.Name).Translate(), conditions);
                y += 8f;
            }
        }
    }

    private void DrawNumber(ref float y, float width, string field, ref float value, float minimum, float maximum, string? labelKey = null)
    {
        string label = (labelKey ?? "CQF_Cinematic_Field_" + field).Translate();
        float labelWidth = Mathf.Min(width * 0.63f, width - 76f);
        Rect labelRect = new(0f, y, labelWidth - 6f, 28f);
        Widgets.LabelEllipses(labelRect, label);
        TooltipHandler.TipRegion(labelRect, label);
        string buffer = buffers.TryGetValue(field, out string? existing) ? existing : value.ToString(CultureInfo.InvariantCulture);
        Widgets.TextFieldNumeric(new Rect(labelWidth, y, width - labelWidth, 28f), ref value, ref buffer, minimum, maximum);
        buffers[field] = buffer;
        y += 35f;
    }

    private void Checkbox(ref float y, float width, string field, ref bool value)
    {
        Rect rect = new(0f, y, width, 28f);
        string label = ("CQF_Cinematic_Field_" + field).Translate();
        Widgets.CheckboxLabeled(rect, label.Truncate(Mathf.Max(20f, width - 32f)), ref value);
        TooltipHandler.TipRegion(rect, label);
        y += 35f;
    }

    private void DrawEnum(ref float y, float width, string field, Enum current, Action<object> accept)
    {
        string prefix = "CQF_Cinematic_" + current.GetType().Name.Replace("CQFCinematic", string.Empty) + "_";
        if (!Button(ref y, width, ("CQF_Cinematic_Field_" + field).Translate() + " · " + (prefix + current).Translate())) return;
        Find.WindowStack.Add(new FloatMenu(Enum.GetValues(current.GetType()).Cast<object>()
            .Select(value => new FloatMenuOption((prefix + value).Translate(), () => accept(value))).ToList()));
    }

    private bool Button(ref float y, float width, string label)
    {
        Rect rect = new(0f, y, width, 28f);
        bool clicked = CQFUIStyle.ButtonText(rect, label);
        TooltipHandler.TipRegion(rect, label);
        y += 35f;
        return clicked;
    }

    private void PawnButton(ref float y, float width, string key, Action<string> accept)
    {
        owner.Project.EnsureRoles();
        if (Button(ref y, width, "CQF_Cinematic_MapSelectPawn".Translate() + " · " + owner.Project.RoleLabel(key)))
        {
            List<FloatMenuOption> options = owner.Project.Definition.roles.Select(role =>
                new FloatMenuOption(owner.Project.RoleLabel(role.key), () => accept(role.key))).ToList();
            if (owner.Project.Definition.roles.Any(role => role.key == key))
                options.Add(new FloatMenuOption("CQF_Cinematic_ChooseRehearsalPawn".Translate(), () => owner.PickRehearsalPawn(key)));
            options.Add(new FloatMenuOption("CQF_Cinematic_NewRole".Translate(), () => owner.BindPawn(accept)));
            Find.WindowStack.Add(new FloatMenu(options));
        }
        if (Button(ref y, width, "CQF_Cinematic_ActorBindings".Translate())) owner.OpenRoleBindings(key);
    }

    private void TargetButton(ref float y, float width, string key, Action<string> accept)
    {
        if (Button(ref y, width, "CQF_Cinematic_TargetName".Translate() + " · " + owner.TargetLabel(key))) owner.BindMapTarget(accept);
    }

    private void OpenImagePicker(Action<string> accept)
    {
        var images = LoadedModManager.RunningModsListForReading.SelectMany(mod => mod.GetContentHolder<Texture2D>().contentList
                .Select(item => (Path: item.Key, Texture: item.Value, Mod: mod.Name)))
            .GroupBy(item => item.Path, StringComparer.OrdinalIgnoreCase).Select(group => group.Last()).OrderBy(item => item.Path).ToList();
        Find.WindowStack.Add(new Dialog_Select<(string Path, Texture2D Texture, string Mod)>(
            new LabeledTextureSelectDrawer<(string Path, Texture2D Texture, string Mod)>(images, item => item.Texture,
                item => Path.GetFileName(item.Path), item => accept(item.Path), getTip: item => item.Mod + "\n" + item.Path,
                getRawText: item => item.Path + " " + item.Mod), "CQF_Cinematic_ChooseImage".Translate()));
    }

    private void OpenDefPicker(Type type, Action<Def> accept, string title)
    {
        List<Def> choices = GenDefDatabase.GetAllDefsInDatabaseForDef(type).OrderBy(DefinitionLabel).ToList();
        Find.WindowStack.Add(new Dialog_Select<Def>(new TextSelectDrawer<Def>(choices, DefinitionLabel, accept,
            getTip: definition => definition.defName, getRawText: definition => DefinitionLabel(definition) + " " + definition.defName), title.Translate()));
    }

    private string DefinitionLabel(Def definition)
    {
        return definition.label.NullOrEmpty() ? definition.defName : definition.LabelCap.ToString();
    }

    private readonly QuestEditor_Cinematics owner;
    private readonly Dictionary<string, string> buffers = new();
    private CQFCinematicStep? previousStep;
    private Vector2 scroll;
    private float contentHeight;
    private bool advanced;
}
