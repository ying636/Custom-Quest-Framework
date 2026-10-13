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
            if (System.IO.File.Exists(System.IO.Path.Combine(GenFilePaths.SaveDataFolderPath, "version17")))
                gameObject.AddComponent<Version17RuntimeChecksDriver>();
            else gameObject.AddComponent<RuntimeChecksDriver>();
        });
    }
}
