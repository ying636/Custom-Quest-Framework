using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicLifetime : MonoBehaviour
{
    public void Initialize(GameComponent_Cinematics owner, Game game)
    {
        host = owner;
        this.game = game;
    }

    public void Release()
    {
        host = null;
        game = null;
        Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (host == null) return;
        if (!ReferenceEquals(Current.Game, game) || Current.ProgramState != ProgramState.Playing)
        {
            host.Dispose();
            return;
        }
        CQFCinematicInstance? active = host.Active;
        if (active == null) return;
        CQFCinematicPresentation view = active.Presentation;
        if (view.HasCamera)
            Find.CameraDriver.SetRootPosAndSize(view.CameraPosition + view.ShakeOffset, view.CameraSize);
    }

    private void OnDestroy()
    {
        host?.Dispose();
        host = null;
        game = null;
    }

    private GameComponent_Cinematics? host;
    private Game? game;
}
