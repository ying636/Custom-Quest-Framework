using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainTransitionLifetime : MonoBehaviour
{
    public void Initialize(DomainTransition owner)
    {
        transition = owner;
        game = Current.Game;
    }

    public void Release()
    {
        transition = null;
        game = null;
        Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (transition != null && (Current.Game != game || Current.ProgramState != ProgramState.Playing))
            transition.Dispose();
    }

    private void OnDestroy()
    {
        transition?.Dispose();
        transition = null;
        game = null;
    }

    private DomainTransition? transition;
    private Game? game;
}
