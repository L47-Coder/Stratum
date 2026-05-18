using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ManagerOrderPage : IPage
    {
        public string GroupTitle => "Manager";
        public string TabTitle => "Order";

        private readonly TableControl _tableView = new() { CanAdd = false, CanRemove = false, CanEdit = false, ShowToolbar = false };
        private ManagerOrderConfig _config;

        public void OnFirstEnter()
        {
            _tableView.KeyField = "Manager";
            _tableView.OnRowMove((_, _) => SaveOrder());
        }

        public void OnEnter()
        {
            ReloadOrder();
        }

        public void OnGUI(Rect rect)
        {
            if (_config == null)
            {
                GUI.Label(rect, "Failed to load config.", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            _tableView.Draw(rect);
        }

        private void ReloadOrder()
        {
            _config = ManagerOrderSync.EnsureAndSyncAsset();
            _tableView.Items = _config != null ? _config.Entries : null;
        }

        private void SaveOrder()
        {
            if (_config == null) return;

            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
        }
    }
}
