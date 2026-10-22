using System;
using System.IO;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Dialog_CinematicXml : Window
{
    public Dialog_CinematicXml(string path, string xml, Func<string, string> apply, bool export, Action? saved = null)
    {
        this.path = path;
        this.xml = xml;
        this.apply = apply;
        this.export = export;
        this.saved = saved;
        doCloseX = true;
        absorbInputAroundWindow = true;
        resizeable = true;
        draggable = true;
    }

    public override Vector2 InitialSize => new(Mathf.Min(UI.screenWidth - 40f, 1000f), Mathf.Min(UI.screenHeight - 60f, 760f));

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        windowRect.width = Mathf.Clamp(windowRect.width, Mathf.Min(480f, UI.screenWidth - 20f), UI.screenWidth - 20f);
        windowRect.height = Mathf.Clamp(windowRect.height, Mathf.Min(300f, UI.screenHeight - 20f), UI.screenHeight - 20f);
        windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, UI.screenWidth - windowRect.width));
        windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, UI.screenHeight - windowRect.height));
    }

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width, inRect.height);
        Widgets.LabelEllipses(new Rect(0f, 0f, inRect.width - 32f, 30f), (export ? "CQF_Cinematic_Export" : "CQF_Cinematic_Import").Translate().Colorize(CQFUIStyle.Accent));
        float labelWidth = Mathf.Min(180f, inRect.width * 0.25f);
        float loadWidth = Mathf.Min(200f, inRect.width * 0.28f);
        Widgets.LabelEllipses(new Rect(0f, 38f, labelWidth, 28f), "CQF_Cinematic_Path".Translate());
        TooltipHandler.TipRegion(new Rect(0f, 38f, labelWidth, 28f), "CQF_Cinematic_Path".Translate());
        path = Widgets.TextField(new Rect(labelWidth + 6f, 38f, Mathf.Max(40f, inRect.width - labelWidth - loadWidth - 18f), 28f), path);
        if (CQFUIStyle.ButtonText(new Rect(inRect.width - loadWidth, 38f, loadWidth, 28f), "CQF_Cinematic_LoadFile".Translate()))
            LoadFile();
        Rect viewport = new(0f, 76f, inRect.width, Mathf.Max(60f, inRect.height - 124f));
        Rect content = new(0f, 0f, viewport.width - 18f, Mathf.Max(viewport.height, Text.CalcHeight(xml, viewport.width - 30f) + 28f));
        Widgets.BeginScrollView(viewport, ref scroll, content);
        xml = Widgets.TextArea(content, xml);
        Widgets.EndScrollView();
        if (CQFUIStyle.ButtonText(new Rect(0f, inRect.height - 38f, inRect.width, 32f), (export ? "CQF_Cinematic_SaveFile" : "CQF_Cinematic_ApplyXml").Translate()))
            Apply();
    }

    private void LoadFile()
    {
        try
        {
            FileInfo file = new(Path.GetFullPath(path));
            if (!file.Exists || file.Length > 2097152)
                throw new InvalidDataException("CQF_Cinematic_InvalidFile");
            xml = File.ReadAllText(file.FullName, Encoding.UTF8);
            scroll = Vector2.zero;
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Cinematic XML load failed: " + error);
            Messages.Message("CQF_Cinematic_ImportFailed".Translate() + ": " + (error.Message.CanTranslate() ? error.Message.Translate().ToString() : error.Message), MessageTypeDefOf.RejectInput);
        }
    }

    private void Apply()
    {
        try
        {
            string normalized = apply(xml);
            if (export)
            {
                string destination = Path.GetFullPath(path);
                if (!string.Equals(Path.GetExtension(destination), ".xml", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("CQF_Cinematic_InvalidFile");
                string directory = Path.GetDirectoryName(destination) ?? throw new InvalidDataException("CQF_Cinematic_InvalidFile");
                Directory.CreateDirectory(directory);
                string temporary = Path.Combine(directory, "." + Path.GetFileName(destination) + "." + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    File.WriteAllText(temporary, normalized, new UTF8Encoding(false));
                    if (File.Exists(destination)) File.Replace(temporary, destination, null);
                    else File.Move(temporary, destination);
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
                saved?.Invoke();
                Messages.Message("CQF_Cinematic_ExportSucceeded".Translate() + ": " + destination, MessageTypeDefOf.PositiveEvent);
            }
            else
                Messages.Message("CQF_Cinematic_ImportSucceeded".Translate(), MessageTypeDefOf.PositiveEvent);
            Close();
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Cinematic XML apply failed: " + error);
            Messages.Message((export ? "CQF_Cinematic_ExportFailed" : "CQF_Cinematic_ImportFailed").Translate() + ": " + (error.Message.CanTranslate() ? error.Message.Translate().ToString() : error.Message), MessageTypeDefOf.RejectInput);
        }
    }

    private readonly Func<string, string> apply;
    private readonly bool export;
    private readonly Action? saved;
    private string path;
    private string xml;
    private Vector2 scroll;
}
