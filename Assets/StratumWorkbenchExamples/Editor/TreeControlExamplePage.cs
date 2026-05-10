using UnityEditor;
using UnityEngine;
using Stratum.Editor;

namespace StratumWorkbenchExamples
{
    internal sealed class TreeControlExamplePage : IWorkbenchExamplePage
    {
        private const float FooterHeight = 54f;
        private const string DefaultAssetsPath = "Assets";

        private EditorWindow _host;
        private readonly TreeControl _tree = new();

        private DefaultAsset _rootFolder;
        private string _treeRootAssetPath = DefaultAssetsPath;
        private Vector2 _logScroll;

        private string _lastEvent = "就绪";
        private string _ignorePatterns = string.Empty;
        private string _stripExtensionsField = ".meta";

        public string TabLabel => "树";

        public void OnEnable(EditorWindow host)
        {
            _host = host;
            SyncRootFolderAssetFromPath();
            _tree.ToolbarButtons = WorkbenchTestWindowUtil.DefaultTestToolbar();
            SyncExcludePatterns();
            SyncHiddenExtensions();
            BindTreeCallbacks();
        }

        public void OnGUI(EditorWindow host)
        {
            _host = host;
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawOptions();
            EditorGUILayout.EndVertical();

            GUILayout.Space(6f);
            WorkbenchTestWindowUtil.Rule();
            var anchorY = GUILayoutUtility.GetLastRect().yMax;

            var body = WorkbenchTestWindowUtil.MainContentRect(host.position.width, host.position.height,
                anchorY, FooterHeight);
            _tree.Draw(body, _treeRootAssetPath);

            WorkbenchTestWindowUtil.DrawMiniLogFooter(ref _logScroll, _lastEvent,
                "操作写磁盘 · 仅用测试目录",
                WorkbenchTestWindowUtil.FooterRect(host.position.width, body.yMax, FooterHeight));
        }

        private void SyncRootFolderAssetFromPath()
        {
            if (string.IsNullOrEmpty(_treeRootAssetPath))
                _treeRootAssetPath = DefaultAssetsPath;
            _rootFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_treeRootAssetPath);
        }

        private void SyncExcludePatterns() =>
            WorkbenchTestWindowUtil.ApplyDelimitedToList(_ignorePatterns, _tree.ExcludePatterns);

        private void SyncHiddenExtensions() =>
            _tree.HiddenExtensions = WorkbenchTestWindowUtil.DelimitedExtensionsOrNull(_stripExtensionsField);

        private void BindTreeCallbacks()
        {
            _tree.OnButtonClick(i =>
            {
                if (i == 0)
                {
                    AssetDatabase.Refresh();
                    Log("DB 刷新");
                }
                else
                    Log($"工具栏 {i}");
            });

            _tree.OnNodeSelect(p => Log($"选 {p}"));
            _tree.OnNodeAdd(p => Log($"+夹 {p}"));
            _tree.OnNodeEdit((o, n) => Log($"{o}→{n}"));
            _tree.OnNodeRemove(p => Log($"- {p}"));
            _tree.OnNodeMove((a, b) => Log($"{a}→{b}"));
            _tree.OnNodeDragOut(p => Log($"拖出 {p}"));
            _tree.OnNodeReceiveDrop((a, b) =>
                Log($"{a} → {b} · {WorkbenchTestWindowUtil.FormatDragPathsForLog()}"));
        }

        private void Log(string line)
        {
            _lastEvent = line;
            _host?.Repaint();
        }

        private void DrawOptions()
        {
            WorkbenchTestWindowUtil.Card(() =>
            {
                WorkbenchTestWindowUtil.SectionTitle("开关");
                using (new EditorGUILayout.HorizontalScope())
                {
                    _tree.CanAdd = EditorGUILayout.ToggleLeft("新建", _tree.CanAdd, GUILayout.Width(48f));
                    _tree.CanRemove = EditorGUILayout.ToggleLeft("删除", _tree.CanRemove, GUILayout.Width(48f));
                    _tree.CanEdit = EditorGUILayout.ToggleLeft("改名", _tree.CanEdit, GUILayout.Width(48f));
                    _tree.CanSelect = EditorGUILayout.ToggleLeft("选中", _tree.CanSelect, GUILayout.Width(48f));
                    _tree.CanReorder = EditorGUILayout.ToggleLeft("树内拖", _tree.CanReorder, GUILayout.Width(56f));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _tree.CanDragOut = EditorGUILayout.ToggleLeft("拖出", _tree.CanDragOut, GUILayout.Width(48f));
                    _tree.CanReceiveDrop =
                        EditorGUILayout.ToggleLeft("收拖入", _tree.CanReceiveDrop, GUILayout.Width(60f));
                    _tree.ShowToolbar = EditorGUILayout.ToggleLeft("工具栏", _tree.ShowToolbar, GUILayout.Width(60f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Rebuild", GUILayout.Width(72f)))
                    {
                        _tree.RebuildTree();
                        Log("RebuildTree");
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("刷新 DB", GUILayout.Width(72f)))
                    {
                        AssetDatabase.Refresh();
                        Log("Refresh");
                    }
                }

                GUILayout.Space(10f);

                WorkbenchTestWindowUtil.SectionTitle("根");
                EditorGUI.BeginChangeCheck();
                var next = EditorGUILayout.ObjectField(_rootFolder, typeof(DefaultAsset), false) as DefaultAsset;
                if (EditorGUI.EndChangeCheck() && next != null)
                    TrySetRootFolder(next);

                EditorGUILayout.SelectableLabel(_treeRootAssetPath, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                GUILayout.Space(8f);

                EditorGUI.BeginChangeCheck();
                _ignorePatterns = EditorGUILayout.TextField("忽略 glob", _ignorePatterns);
                if (EditorGUI.EndChangeCheck())
                    SyncExcludePatterns();

                EditorGUI.BeginChangeCheck();
                _stripExtensionsField = EditorGUILayout.TextField("隐藏扩展名", _stripExtensionsField);
                if (EditorGUI.EndChangeCheck())
                    SyncHiddenExtensions();

                GUILayout.Space(8f);

                if (GUILayout.Button("SelectNode(根)", GUILayout.Width(120f)))
                {
                    var ok = _tree.SelectNode(_treeRootAssetPath);
                    Log(ok ? "SelectNode ✓" : "SelectNode ✗");
                }
            });
        }

        private void TrySetRootFolder(DefaultAsset asset)
        {
            var p = AssetDatabase.GetAssetPath(asset);
            if (!AssetDatabase.IsValidFolder(p))
            {
                Log("选文件夹");
                return;
            }

            _rootFolder = asset;
            _treeRootAssetPath = p;
            SyncRootFolderAssetFromPath();
            Log(p);
        }
    }
}
