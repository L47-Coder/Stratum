using UnityEditor;
using Stratum;
using Stratum.Editor;

internal static class EventManagerRefresher
{
    private const string ConfigAssetPath = "Assets/Game/Manager/Event/EventManagerConfig.asset";

    [EditorSync]
    public static void Run()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<EventManagerConfig>(ConfigAssetPath);
        if (cfg == null) return;

        var list = (System.Collections.Generic.List<EventManagerData>)cfg.GetConfigList();

        // TODO: implement the custom refresh logic here.

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
    }
}
