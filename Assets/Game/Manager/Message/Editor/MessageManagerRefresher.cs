using UnityEditor;
using Stratum;
using Stratum.Editor;

internal static class MessageManagerRefresher
{
    private const string ConfigAssetPath = "Assets/Game/Manager/Message/MessageManagerConfig.asset";

    [EditorSync]
    public static void Run()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<MessageManagerConfig>(ConfigAssetPath);
        if (cfg == null) return;

        var list = (System.Collections.Generic.List<MessageManagerData>)cfg.GetConfigList();

        // TODO: implement the custom refresh logic here.

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
    }
}
