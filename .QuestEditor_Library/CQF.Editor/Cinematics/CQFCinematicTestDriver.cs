using UnityEngine;

namespace QuestEditor_Library;

public sealed class CQFCinematicTestDriver : MonoBehaviour
{
    public void Initialize(CQFCinematicTestSession session) => this.session = session;

    public void Release()
    {
        session = null;
        Destroy(gameObject);
    }

    private void Update() => session?.Update();

    private void OnApplicationQuit() => session?.NotifyApplicationQuit();

    private void OnDestroy()
    {
        CQFCinematicTestSession? closing = session;
        session = null;
        closing?.NotifyDriverLost();
    }

    private CQFCinematicTestSession? session;
}
