using System.Reflection;
using QuestEditor_Library;
using UnityEngine;
using Verse;

namespace CQF.OptimizationChecks;

public sealed class OptimizationSelectorWindow : Window
{
    public OptimizationSelectorWindow()
    {
        this.forcePause = true;
        this.doCloseX = false;
        this.text = new TextSelectDrawer<int>(items, index => "CQF_Check_" + index + new string('_', index % 70), _ => { });
        this.icons = new TextureSelectDrawer<int>(items, _ => null!, index => "CQF_Check_" + index, _ => { }, drawAction: (_, _) => drawn++);
        this.labeled = new LabeledTextureSelectDrawer<int>(items, _ => null!, index => "CQF_Check_" + index + new string('_', index % 50), _ => { }, drawAction: (_, _) => drawn++);
    }

    public override Vector2 InitialSize => new(700f, 450f);
    public bool Finished { get; private set; }
    public Exception? Error { get; private set; }

    public override void DoWindowContents(Rect inRect)
    {
        if (Finished || Event.current.type != EventType.Repaint) return;
        try
        {
            using CQFUIScope scope = new(inRect.width, inRect.height);
            SelectDrawer<int> drawer = stage == 0 ? text : stage == 1 ? icons : labeled;
            float width = stage < 3 ? 600f : 320f;
            Vector2 scroll = new(0f, stage == 0 ? 20000f : 1500f);
            Rect viewport = new(0f, 0f, width, 300f);
            Rect content = new(0f, 0f, width - 16f, 100000f);
            Widgets.BeginScrollView(viewport, ref scroll, content);
            try
            {
                drawer.VisibleRect = new Rect(0f, scroll.y, content.width, viewport.height);
                drawn = 0;
                float height = drawer.DrawItems(content, 0f, () => { }, drawer.ts);
                List<Rect> rects = (List<Rect>)typeof(SelectDrawer<int>).GetField("itemRects", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(drawer)!;
                int first = (int)typeof(SelectDrawer<int>).GetMethod("FirstVisibleItem", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(drawer, null)!;
                OptimizationDriver.Check(rects.Count == items.Count && first > 0 && first == rects.FindIndex(rect => rect.yMax >= scroll.y), "selector finds the first visible row with variable item heights, stage " + stage);
                if (stage > 0) OptimizationDriver.Check(drawn > 0 && drawn < 200, "selector draws only visible icons from 2000 items, stage " + stage + ": " + drawn);
                if (stage == 2) wideHeight = height;
                if (stage == 3) OptimizationDriver.Check(height > wideHeight, "resizing a labeled selector rebuilds its layout and full scroll height");
            }
            finally { Widgets.EndScrollView(); }
            stage++;
            Finished = stage == 4;
        }
        catch (Exception error) { Error = error; Finished = true; }
    }

    private readonly List<int> items = Enumerable.Range(0, 2000).ToList();
    private readonly TextSelectDrawer<int> text;
    private readonly TextureSelectDrawer<int> icons;
    private readonly LabeledTextureSelectDrawer<int> labeled;
    private int drawn;
    private int stage;
    private float wideHeight;
}
