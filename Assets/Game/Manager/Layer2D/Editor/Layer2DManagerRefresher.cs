using UnityEditor;
using Stratum;
using Stratum.Editor;

internal static class Layer2DManagerRefresher
{
    private const string ConfigAssetPath = "Assets/Game/Manager/Layer2D/Layer2DManagerConfig.asset";

    [EditorSync]
    public static void Run()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<Layer2DManagerConfig>(ConfigAssetPath);
        if (cfg == null) return;

        var list = (System.Collections.Generic.List<Layer2DManagerData>)cfg.GetConfigList();

        // TODO: implement the custom refresh logic here.

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
    }
}
