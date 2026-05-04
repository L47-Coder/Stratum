using System.IO;
using UnityEditor;

namespace Stratum.Editor
{
    internal static class ManagerRefresherLocator
    {
        private const string RefresherSuffix = "ManagerRefresher";

        public static MonoScript FindRefresherScript(string managerName, string assetPath)
        {
            if (string.IsNullOrEmpty(managerName) || string.IsNullOrEmpty(assetPath)) return null;

            var folder   = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder)) return null;

            var fileName = $"{managerName}{RefresherSuffix}.cs";

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>($"{folder}/Editor/{fileName}");
            if (script != null) return script;

            return AssetDatabase.LoadAssetAtPath<MonoScript>($"{folder}/{fileName}");
        }
    }
}
