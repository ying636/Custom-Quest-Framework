using UnityEngine;
using Verse;

namespace CQF.MapLifetimeChecks;

[StaticConstructorOnStartup]
public static class MapLifetimeChecksStartup
{
    static MapLifetimeChecksStartup()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Application.runInBackground = true;
            GameObject runner = new("CQF_MapLifetimeChecks");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<MapLifetimeChecksDriver>();
        });
    }
}
