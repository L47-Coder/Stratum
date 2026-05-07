using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ComponentOrderPage : IPage
    {
        public string GroupTitle => "Component";
        public string TabTitle   => "Order";

        private readonly TableControl _tableView = new() { CanAdd = false, CanRemove = false, CanEdit = false };
        private ComponentOrderConfig _config;

        public void OnFirstEnter()
        {
            _tableView.KeyField = "Component";
            _config = AssetDatabase.LoadAssetAtPath<ComponentOrderConfig>(WorkbenchPaths.ComponentOrder);
            _tableView.OnRowMove((_, _) => EditorUtility.SetDirty(_config));
        }

        public void OnEnter()
        {
            if (_config != null) ComponentOrderSync.Sync(_config);
        }

        public void OnGUI(Rect rect)
        {
            if (_config == null)
            {
                GUI.Label(rect, "Failed to load config.", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            _tableView.Draw(rect, _config.Entries);
        }
    }
}
