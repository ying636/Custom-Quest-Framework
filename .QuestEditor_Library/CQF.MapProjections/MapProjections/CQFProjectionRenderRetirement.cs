#if RIMWORLD_1_7
using UnityEngine;

namespace QuestEditor_Library;

public sealed class CQFProjectionRenderRetirement : MonoBehaviour
{
    public static int PendingCount => instance?.pending.Count ?? 0;

    public static void Retire(RenderTexture? texture, Material? material)
    {
        if (texture == null && material == null) return;
        if (instance == null)
        {
            GameObject host = new("CQF_ProjectionRenderRetirement");
            UnityEngine.Object.DontDestroyOnLoad(host);
            instance = host.AddComponent<CQFProjectionRenderRetirement>();
        }
        instance.pending.Enqueue((Time.frameCount + 3, texture, material));
    }

    private void Update()
    {
        while (pending.TryPeek(out var item) && item.Frame <= Time.frameCount) ReleaseNext();
        if (pending.Count != 0) return;
        if (ReferenceEquals(instance, this)) instance = null;
        UnityEngine.Object.Destroy(gameObject);
    }

    private void OnDestroy()
    {
        while (pending.Count > 0) ReleaseNext();
        if (ReferenceEquals(instance, this)) instance = null;
    }

    private void ReleaseNext()
    {
        var item = pending.Dequeue();
        if (item.Texture != null)
        {
            item.Texture.Release();
            UnityEngine.Object.Destroy(item.Texture);
        }
        if (item.Material != null) UnityEngine.Object.Destroy(item.Material);
    }

    private readonly Queue<(int Frame, RenderTexture? Texture, Material? Material)> pending = new();
    private static CQFProjectionRenderRetirement? instance;
}
#endif
