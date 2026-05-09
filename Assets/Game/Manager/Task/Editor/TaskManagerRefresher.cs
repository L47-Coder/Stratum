using UnityEditor;
using Stratum;
using Stratum.Editor;

internal static class TaskManagerRefresher
{
    private const string ConfigAssetPath = "Assets/Game/Manager/Task/TaskManagerConfig.asset";

    [EditorSync]
    public static void Run()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<TaskManagerConfig>(ConfigAssetPath);
        if (cfg == null) return;

        var list = (System.Collections.Generic.List<TaskManagerData>)cfg.GetConfigList();

        // TODO: implement the custom refresh logic here.

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
    }
}
