using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ManagerOrderPage : IPage
    {
        public string GroupTitle => "Manager";
        public string TabTitle => "Order";

        private readonly TableControl _tableView = new() { CanAdd = false, CanRemove = false, CanEdit = false };
        private ManagerOrderConfig _config;

        public void OnFirstEnter()
        {
            _tableView.KeyField = "Manager";
            _config = AssetDatabase.LoadAssetAtPath<ManagerOrderConfig>(WorkbenchPaths.ManagerOrder);
            _tableView.OnRowMove((_, _) => EditorUtility.SetDirty(_config));
        }

        public void OnEnter()
        {
            if (_config != null) ManagerOrderSync.Sync(_config);
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
