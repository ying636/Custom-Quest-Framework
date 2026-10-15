using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public abstract class DomainTransition : IDisposable
{
    protected DomainTransition(bool returning)
    {
        game = Current.Game;
#if RIMWORLD_1_7
        sourceMap = Find.CurrentMap;
#endif
        IsReturning = returning;
        GameObject owner = new("CQF_DomainTransitionLifetime");
        UnityEngine.Object.DontDestroyOnLoad(owner);
        lifetime = owner.AddComponent<DomainTransitionLifetime>();
        lifetime.Initialize(this);
    }

    public bool IsReturning { get; }

    public abstract float Duration { get; }

    protected abstract float CoverProgress { get; }

    protected abstract float RevealProgress { get; }

    public float Progress
    {
        get
        {
            if (displayMap == null) return 0f;
            float progress = Mathf.Clamp01((Time.realtimeSinceStartup - startedAt) / Duration);
#if RIMWORLD_1_7
            if (!IsReturning && !revealReady) return Mathf.Min(progress, RevealProgress);
#endif
            return progress;
        }
    }

    public bool Finished => disposed || displayMap != null
        && (displayMap.Disposed || (!IsReturning &&
#if RIMWORLD_1_7
            (opening != null ? !opening.Active : domain?.Active != true)
#else
            domain?.Active != true
#endif
            ) || Progress >= 1f);

#if RIMWORLD_1_7
    public void BindOpening(DomainOpening owner)
    {
        opening = owner;
        displayMap = sourceMap;
        startedAt = Time.realtimeSinceStartup;
    }
#endif

    public void Bind(Map realm)
    {
#if RIMWORLD_1_7
        if (displayMap == null) startedAt = Time.realtimeSinceStartup;
#endif
        domain = realm.GetComponent<MapComponent_Domain>();
        displayMap = realm;
        domain.BeginArrivalTransition();
        Find.CameraDriver.shaker.DoShake(0.18f);
#if !RIMWORLD_1_7
        startedAt = Time.realtimeSinceStartup;
#endif
    }

    public void BindReturn(Map destination, IntVec3 focus, IEnumerable<Pawn> pawns)
    {
        displayMap = destination;
        destinationFocus = focus;
        returnLock = destination.GetComponent<MapComponent_DomainTransitionLock>();
        returnLock.Begin(pawns);
        Find.CameraDriver.shaker.DoShake(0.12f);
        startedAt = Time.realtimeSinceStartup;
    }

#if RIMWORLD_1_7
    public void Update()
    {
        if (IsReturning || displayMap == null || Finished) return;
        if (Find.CurrentMap != (entered ? displayMap : sourceMap))
        {
            Dispose();
            return;
        }
        if (!entered && coveredFrame >= 0 && Time.frameCount > coveredFrame)
        {
            if (opening != null)
            {
                DomainOpening owner = opening;
                opening = null;
                owner.CompleteTransfer();
                if (Finished)
                {
                    Dispose();
                    return;
                }
            }
            Current.Game.CurrentMap = displayMap;
            Find.Selector.ClearSelection();
            if (domain?.Caster is Pawn pawn && pawn.Spawned && pawn.Map == displayMap)
            {
                Find.CameraDriver.JumpToCurrentMapLoc(pawn.Position);
                Find.Selector.Select(pawn);
            }
            entered = true;
            switchedFrame = Time.frameCount;
        }
        else if (entered && !revealReady)
        {
            if (terrainReadyFrame < 0 && domain!.LastTerrainRenderedFrame > switchedFrame)
                terrainReadyFrame = Time.frameCount;
            if (terrainReadyFrame >= 0 && Time.frameCount > terrainReadyFrame)
            {
                revealReady = true;
                startedAt = Time.realtimeSinceStartup - Duration * RevealProgress;
            }
        }
    }
#endif

    public void Draw()
    {
        if (displayMap == null || Finished
#if RIMWORLD_1_7
            || Find.CurrentMap != (!IsReturning && !entered ? sourceMap : displayMap)
#else
            || Find.CurrentMap != displayMap
#endif
            || !WorldRendererUtility.DrawingMap || Event.current.type != EventType.Repaint) return;
        float progress = Progress;
        DrawEffect(progress);
#if RIMWORLD_1_7
        if (!IsReturning && !entered && progress >= CoverProgress && coveredFrame < 0)
            coveredFrame = Time.frameCount;
#endif
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (Current.Game == game && Current.ProgramState == ProgramState.Playing)
        {
#if RIMWORLD_1_7
            if (opening != null && !opening.Destroyed) opening.Destroy();
            opening = null;
#endif
            domain?.EndArrivalTransition();
            returnLock?.End();
        }
        ReleaseVisuals();
        domain = null;
        returnLock = null;
        displayMap = null;
        lifetime?.Release();
        lifetime = null;
    }

    public void DisposeIfMap(Map map)
    {
        if (displayMap == map || domain?.map == map || returnLock?.map == map
#if RIMWORLD_1_7
            || sourceMap == map
#endif
            ) Dispose();
    }

    protected abstract void DrawEffect(float progress);

    protected abstract void ReleaseVisuals();

    protected IntVec3 destinationFocus;

    private MapComponent_Domain? domain;
    private MapComponent_DomainTransitionLock? returnLock;
    private Map? displayMap;
#if RIMWORLD_1_7
    private readonly Map? sourceMap;
    private DomainOpening? opening;
    private bool entered;
    private bool revealReady;
    private int coveredFrame = -1;
    private int switchedFrame;
    private int terrainReadyFrame = -1;
#endif
    private float startedAt;
    private bool disposed;
    private DomainTransitionLifetime? lifetime;
    private readonly Game game;
}
