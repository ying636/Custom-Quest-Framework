using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFCompositePortraitInteraction
{
    internal CQFCompositePortraitInteraction(GameComponent_MapManager manager, Action? onFocus = null)
    {
        this.manager = manager;
        this.onFocus = onFocus;
    }

    internal void Draw(Rect portrait, Pawn pawn, float scale)
    {
        Selector selector = Find.Selector;
        if (Event.current.type == EventType.Repaint)
        {
            using CQFUIScope scope = new();
            Text.WordWrap = false;
            Matrix4x4 previous = GUI.matrix;
            try
            {
                if (scale < 1f)
                {
                    Vector2 offset = (GUIUtility.GUIToScreenPoint(Vector2.zero) + portrait.position) * (1f - scale);
                    GUI.matrix = previous * Matrix4x4.TRS(new Vector3(offset.x, offset.y, 0f),
                        Quaternion.identity, new Vector3(scale, scale, 1f));
                }
                ColonistBar bar = Find.ColonistBar;
                bar.drawer.DrawColonist(new Rect(portrait.position, bar.Size), pawn, pawn.MapHeld,
                    highlight: windowSelection == pawn, reordering: false);
            }
            finally
            {
                GUI.matrix = previous;
            }
        }
        TooltipHandler.TipRegion(portrait, pawn.LabelShortCap);

        Event current = Event.current;
        if (current.type == EventType.MouseDown && current.button == 0 && portrait.Contains(current.mousePosition))
        {
            clickedPawn = pawn;
            clickCount = current.clickCount;
        }

        if (!Widgets.ButtonInvisible(portrait, doMouseoverSound: false) || clickedPawn != pawn) return;
        clickedPawn = null;
        if (pawn.Destroyed || pawn.Dead || !pawn.SpawnedOrAnyParentSpawned ||
            pawn.MapHeld is not { Disposed: false } map) return;

        bool focused = clickCount == 2 && manager.FocusCompositeColonist(pawn);
        if (!focused && map != Find.CurrentMap)
        {
            windowSelection = current.shift && windowSelection == pawn ? null : pawn;
            return;
        }

        windowSelection = null;
        if (!focused && current.shift)
        {
            if (selector.IsSelected(pawn)) selector.Deselect(pawn);
            else selector.Select(pawn);
            return;
        }

        selector.ClearSelection();
        selector.Select(pawn);
        if (focused) onFocus?.Invoke();
    }

    private readonly GameComponent_MapManager manager;
    private readonly Action? onFocus;
    private Pawn? clickedPawn;
    private Pawn? windowSelection;
    private int clickCount;
}
