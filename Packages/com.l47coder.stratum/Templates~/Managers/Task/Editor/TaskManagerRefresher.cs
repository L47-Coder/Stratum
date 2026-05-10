using Stratum;
using Stratum.Editor;
using UnityEditor;

internal static class TaskManagerRefresher
{
    private const string ConfigAssetPath = "Assets/Game/Manager/Task/TaskManagerConfig.asset";

    [EditorSync]
    public static void Run()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<TaskManagerConfig>(ConfigAssetPath);
        if (cfg == null) return;

        var list = (System.Collections.Generic.List<TaskManagerData>)cfg.GetConfigList();

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
    }
}
