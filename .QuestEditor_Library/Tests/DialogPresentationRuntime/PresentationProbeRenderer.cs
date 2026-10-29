using QuestEditor_Library;
using UnityEngine;

namespace CQF.DialogPresentationChecks;

public sealed class PresentationProbeRenderer : CQFDialogRenderer
{
    public static CQFDialogFrame? LastFrame { get; private set; }
    public static int Disposals { get; private set; }

    public override Vector2 GetInitialSize(Vector2 screenSize) => new(800f, 600f);
    public override void Draw(Rect rect, CQFDialogFrame frame) { LastFrame = frame; }
    public override void Dispose() { Disposals++; }
}
