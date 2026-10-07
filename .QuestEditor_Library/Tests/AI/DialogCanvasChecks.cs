using QuestEditor_Library;
using UnityEngine;

internal static class DialogCanvasChecks
{
    public static void Run()
    {
        DialogNode node = new();
        node.options.AddRange(Enumerable.Range(0, 5).Select(_ => new DialogOption()));
        DialogOption shared = node.options[0];
        shared.results.Add(new DialogResult());
        foreach (float scale in new[] { 0.25f, 0.5f, 0.85f, 1f, 1.6f })
        {
            CQFDialogCanvasViewport viewport = new() { Zoom = scale, Pan = new Vector2(657f, 189f) };
            Vector2 position = new(140f, 100f);
            CQFDialogNodeLayout nodeLayout = new(node, 80f);
            Rect bounds = viewport.ToScreen(new Rect(position, new Vector2(CQFDialogNodeLayout.Width, nodeLayout.Height)));
            CQFDialogCardLayout card = new(nodeLayout, bounds);
            Check(Near(viewport.ToWorld(bounds.position), position), "canvas coordinates roundtrip after pan and zoom: " + scale);
            Check(Inside(card.Header, card.Title) && Inside(bounds, card.Body) && card.Title.yMax < card.Body.y,
                "node title and dialogue stay inside the card and remain separate: " + scale);
            for (int index = 0; index < node.options.Count; index++)
            {
                Rect row = card.Row(index);
                Vector2 port = card.RowOutput(index);
                Check(Inside(bounds, row) && Inside(row, card.RowText(index)) && card.Body.yMax < row.y && card.RowAt(row.center) == index,
                    "choice drawing and hit testing share the same row: " + scale + "/" + index);
                Check(port.x == bounds.xMax && port.y > row.y && port.y < row.yMax && card.PortHit(port).Contains(port),
                    "choice connection and hit area match the visible row: " + scale + "/" + index);
            }
            CQFDialogNodeLayout optionLayout = new(shared, 80f);
            CQFDialogCardLayout optionCard = new(optionLayout, viewport.ToScreen(new Rect(position + new Vector2(360f, 0f), new Vector2(CQFDialogNodeLayout.Width, optionLayout.Height))));
            Check(Inside(optionCard.Header, optionCard.Title) && Inside(optionCard.Bounds, optionCard.Body)
                && Enumerable.Range(0, shared.results.Count).All(index => Inside(optionCard.Bounds, optionCard.Row(index)) && optionCard.RowAt(optionCard.Row(index).center) == index),
                "shared choice text and result rows use the same card layout: " + scale);
            Vector2 anchor = new(700f, 300f);
            Vector2 before = viewport.ToWorld(anchor);
            viewport.ZoomAt(anchor, 1.5f);
            Check(Near(viewport.ToWorld(anchor), before), "zoom preserves the world point below the cursor: " + scale);
        }
        CQFDialogCanvasViewport clamped = new();
        clamped.Zoom = 100f; Check(clamped.Zoom == 1.6f, "canvas zoom remains bounded when zooming in");
        clamped.Zoom = 0f; Check(clamped.Zoom == 0.25f, "canvas zoom remains positive when zooming out");
        clamped.Reset(); Check(clamped.Zoom == 0.85f && clamped.Pan.x == 35f && clamped.Pan.y == 35f, "viewport reset restores the default view");
    }

    private static bool Inside(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 0.01f && inner.yMin >= outer.yMin - 0.01f
        && inner.xMax <= outer.xMax + 0.01f && inner.yMax <= outer.yMax + 0.01f;
    private static bool Near(Vector2 first, Vector2 second) => Math.Abs(first.x - second.x) < 0.01f && Math.Abs(first.y - second.y) < 0.01f;
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
