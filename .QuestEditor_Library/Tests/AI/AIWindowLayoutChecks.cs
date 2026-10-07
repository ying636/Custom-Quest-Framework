using QuestEditor_Library;
using UnityEngine;

internal static class AIWindowLayoutChecks
{
    public static void Run()
    {
        foreach (float width in new[] { 304f, 384f, 420f, 564f, 960f })
        {
            foreach (int statusRows in new[] { 0, 1, 2 })
            {
                Rect bounds = new(0f, 0f, width, 324f);
                CQFAIWindowLayout layout = new(bounds, 500f, statusRows);
                int buttons = layout.Compact ? 5 : 6;
                Rect leftmost = layout.HeaderButton(buttons - 1);
                Check(layout.Title.xMax + 8f <= leftmost.x && layout.Title.width >= 100f, "chat title keeps space beside its buttons: " + width + "/" + statusRows);
                Check(layout.History.height >= 60f && layout.History.yMax <= layout.Status.y && layout.Status.yMax < layout.Composer.y,
                    "chat history, status and composer do not overlap at minimum height: " + width + "/" + statusRows);
                Check(layout.Input.width > 0f && layout.Input.height >= 32f && layout.Input.yMax < layout.Send.y && bounds.Contains(layout.Send.center),
                    "multiline input remains separate from the send button: " + width + "/" + statusRows);
                Check(Enumerable.Range(0, buttons).All(index => bounds.Contains(layout.HeaderButton(index).center)), "all toolbar buttons stay inside the chat: " + width + "/" + statusRows);
            }
        }
        foreach (float width in new[] { 360f, 640f, 800f, 900f, 1484f })
        {
            Rect bounds = new(0f, 0f, width, 600f);
            CQFDialogEditorLayout layout = new(bounds, true);
            Check(layout.Panel.width >= 340f && layout.Panel.xMax <= width && layout.Canvas.width >= 340f,
                "dialogue fields retain a usable width at narrow and wide window sizes: " + width);
            Check(layout.Overlay == (width < 840f) && (layout.Overlay || layout.Canvas.xMax + 12f <= layout.Panel.x),
                "dialogue side panel overlays narrow canvases and separates on wide canvases: " + width);
            CQFDialogEditorLayout hidden = new(bounds, false);
            Check(hidden.Panel.width == 0f && hidden.Canvas.width == width, "closing dialogue details restores the whole canvas: " + width);
        }
    }

    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
