using UnityEngine;
using Verse;

namespace CQF.RuntimeChecks;

[StaticConstructorOnStartup]
public static class RuntimeChecksStartup
{
    static RuntimeChecksStartup()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject gameObject = new GameObject("CQF_RuntimeChecks");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<RuntimeChecksDriver>();
        });
    }
}
