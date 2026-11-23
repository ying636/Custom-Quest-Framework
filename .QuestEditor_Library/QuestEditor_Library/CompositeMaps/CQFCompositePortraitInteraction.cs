using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFCompositePortraitInteraction
{
    internal CQFCompositePortraitInteraction(GameComponent_MapManager manager, Action? onFocus = null,
        bool useColonistBarFrame = false)
    {
        this.manager = manager;
        this.onFocus = onFocus;
        this.useColonistBarFrame = useColonistBarFrame;
    }

    internal void Draw(Rect portrait, Pawn pawn)
    {
        Selector selector = Find.Selector;
        if (Event.current.type == EventType.Repaint)
        {
            bool selected = windowSelection == pawn || (windowSelection == null && selector.IsSelected(pawn));
            if (useColonistBarFrame)
            {
                GUI.DrawTexture(portrait, ColonistBar.BGTex);
                if (selected)
                {
                    float scale = portrait.width / ColonistBar.BaseSize.x;
                    SelectionDrawerUtility.DrawSelectionOverlayOnGUI(pawn, portrait.ExpandedBy(2f * scale),
                        ColonistBar.BaseSelectedTexScale * scale, ColonistBar.BaseSelectedTexJump * scale);
                }
            }
            else if (selected)
            {
                CQFUIStyle.DrawHighlightSelected(portrait);
            }
            GUI.DrawTexture(portrait, PortraitsCache.Get(pawn, portrait.size, Rot4.South,
                ColonistBarColonistDrawer.PawnTextureCameraOffset, 1.28205f));
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
    private readonly bool useColonistBarFrame;
    private Pawn? clickedPawn;
    private Pawn? windowSelection;
    private int clickCount;
}
