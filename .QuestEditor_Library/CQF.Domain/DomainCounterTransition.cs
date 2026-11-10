using UnityEngine;
using Verse;

namespace QuestEditor_Library;

#if RIMWORLD_1_7
public sealed class DomainCounterTransition : DomainTransition
{
    public DomainCounterTransition(DomainOpening opening) : base(false)
    {
        incoming = opening.Definition;
        origin = opening.Position;
        initialRadius = opening.Radius;
    }

    public override float Duration => 2.4f;
    protected override float CoverProgress => 0.58f;
    protected override float RevealProgress => 0.64f;
    protected override bool ReadyToCommit => fullScreenFrame >= 0;

    protected override void PreparePresentation()
    {
        if (incoming.surface?.screenShader != null) incomingSurface = new DomainSurfaceVisuals(incoming.surface);
        Vector3 low = UI.UIToMapPosition(0f, 0f);
        Vector3 high = UI.UIToMapPosition(UI.screenWidth, UI.screenHeight);
        view = new Vector4(low.x, low.z, high.x - low.x, high.z - low.z);
        maximumRadius = Mathf.Max(initialRadius, new Vector2(
            Mathf.Max(Mathf.Abs(low.x - origin.x - 0.5f), Mathf.Abs(high.x - origin.x - 0.5f)),
            Mathf.Max(Mathf.Abs(low.z - origin.z - 0.5f), Mathf.Abs(high.z - origin.z - 0.5f))).magnitude) + 4f;
        surfaceTime = GenTicks.TicksGame / 60f;
    }

    protected override void DrawEffect(float progress)
    {
        float cover = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / CoverProgress));
        float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RevealProgress, 0.98f, progress));
        if (incomingSurface != null)
        {
            incomingSurface.DrawScreen(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), view,
                new Vector4(origin.x + 0.5f, origin.z + 0.5f, Mathf.Lerp(initialRadius, maximumRadius, cover), 0f), default,
                new Vector4(view.x + view.z * 0.5f, view.y + view.w * 0.5f,
                    Mathf.Lerp(-4f, new Vector2(view.z, view.w).magnitude + 4f, reveal), 1f),
                surfaceTime + progress * Duration + 6f, 1f, new Vector4(0f, 0f, 0f, 3f));
        }
        else
        {
            Color color = incoming.contestColor;
            color.a = cover * (1f - reveal);
            Widgets.DrawBoxSolid(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), color);
        }
        if (progress >= CoverProgress) fullScreenFrame = Time.frameCount;
    }

    protected override void ReleaseVisuals()
    {
        incomingSurface?.Dispose();
        incomingSurface = null;
    }

    private readonly DomainDef incoming;
    private readonly IntVec3 origin;
    private readonly float initialRadius;
    private DomainSurfaceVisuals? incomingSurface;
    private Vector4 view;
    private float maximumRadius;
    private float surfaceTime;
    private int fullScreenFrame = -1;
}
#endif
