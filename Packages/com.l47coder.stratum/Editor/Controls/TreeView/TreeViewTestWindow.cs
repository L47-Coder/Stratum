#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed class TreeViewTestWindow : EditorWindow
    {
        private const float FooterHeight = 56f;
        private const string DefaultAssetsPath = "Assets";

        private readonly TreeView _tree = new();

        private DefaultAsset _rootFolder;

        private string _treeRootAssetPath = DefaultAssetsPath;

        private Vector2 _logScroll;
        private string _lastEvent = "尚无树事件。";

        private string _ignorePatterns = string.Empty;
        private string _stripExtensionsField = ".meta";

        [MenuItem("Tools/Dev Workbench/TreeView Test")]
        private static void Open()
        {
            var w = GetWindow<TreeViewTestWindow>("TreeView Test");
            w.minSize = new Vector2(780f, 440f);
            w.Show();
        }

        private void OnEnable()
        {
            ResolveRootAssetIfStale();
            _tree.ToolbarButtons = WorkbenchTestWindowUtil.DefaultTestToolbar();
            SyncExcludePatterns();
            SyncHiddenExtensions();
            HookTreeCallbacks();
        }

        private void ResolveRootAssetIfStale()
        {
            if (_rootFolder != null)
            {
                var p = AssetDatabase.GetAssetPath(_rootFolder);
                if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p) && _treeRootAssetPath == p)
                    return;

                var next = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_treeRootAssetPath);
                _rootFolder = next;
                return;
            }

            var loaded = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_treeRootAssetPath);
            if (loaded != null)
                _rootFolder = loaded;
        }

        private void SyncExcludePatterns() =>
            WorkbenchTestWindowUtil.ApplyDelimitedToList(_ignorePatterns, _tree.ExcludePatterns);

        private void SyncHiddenExtensions() =>
            _tree.HiddenExtensions = WorkbenchTestWindowUtil.DelimitedExtensionsOrNull(_stripExtensionsField);

        private void HookTreeCallbacks()
        {
            _tree.OnButtonClicked(i =>
            {
                if (i == 0)
                {
                    AssetDatabase.Refresh();
                    Log("工具栏 #0: AssetDatabase.Refresh 已完成（根目录仍为当前所选）。");
                }
                else
                    Log($"工具栏 #{i}");
            });
            _tree.OnNodeSelected(p => Log($"选中: {p}"));
            _tree.OnNodeAdded(p => Log($"新建文件夹: {p}"));
            _tree.OnNodeRenamed((o, n) => Log($"重命名: {o} → {n}"));
            _tree.OnNodeRemoved(p => Log($"已删除: {p}"));
            _tree.OnNodeMoved((src, dst) => Log($"移动: {src} → {dst}"));
        }

        private void Log(string line)
        {
            _lastEvent = line;
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawOptions();
            EditorGUILayout.EndVertical();

            var optionsBottom = GUILayoutUtility.GetLastRect().yMax;
            var body = WorkbenchTestWindowUtil.MainContentRect(position.width, position.height, optionsBottom,
                FooterHeight);
            _tree.Draw(body, _treeRootAssetPath);

            WorkbenchTestWindowUtil.DrawMiniLogFooter(ref _logScroll, _lastEvent, $"树根: {_treeRootAssetPath}",
                WorkbenchTestWindowUtil.FooterRect(position.width, body.yMax, FooterHeight));
        }

        private void DrawOptions()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("树操作能力", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _tree.CanAdd = EditorGUILayout.ToggleLeft("新建", _tree.CanAdd, GUILayout.Width(52f));
                    _tree.CanRemove = EditorGUILayout.ToggleLeft("删除", _tree.CanRemove, GUILayout.Width(52f));
                    _tree.CanRename = EditorGUILayout.ToggleLeft("改名", _tree.CanRename, GUILayout.Width(52f));
                    _tree.CanSelect = EditorGUILayout.ToggleLeft("选中", _tree.CanSelect, GUILayout.Width(52f));
                    _tree.CanDrag = EditorGUILayout.ToggleLeft("拖拽移动", _tree.CanDrag, GUILayout.Width(72f));
                    _tree.ShowToolbar = EditorGUILayout.ToggleLeft("工具栏", _tree.ShowToolbar, GUILayout.Width(60f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("刷新 DB", GUILayout.Width(80f)))
                    {
                        AssetDatabase.Refresh();
                        Log("已执行 AssetDatabase.Refresh。");
                    }
                }

                GUILayout.Space(4f);
                GUILayout.Label("数据源", EditorStyles.miniBoldLabel);
                EditorGUI.BeginChangeCheck();
                var next = EditorGUILayout.ObjectField("根文件夹", _rootFolder, typeof(DefaultAsset), false) as
                    DefaultAsset;
                if (EditorGUI.EndChangeCheck() && next != null)
                    TrySetRootFolder(next);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("忽略 glob", GUILayout.Width(72f));
                    EditorGUI.BeginChangeCheck();
                    _ignorePatterns = EditorGUILayout.TextField(_ignorePatterns);
                    if (EditorGUI.EndChangeCheck())
                        SyncExcludePatterns();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("隐藏扩展名", GUILayout.Width(72f));
                    EditorGUI.BeginChangeCheck();
                    _stripExtensionsField = EditorGUILayout.TextField(_stripExtensionsField);
                    if (EditorGUI.EndChangeCheck())
                        SyncHiddenExtensions();
                }

                EditorGUILayout.HelpBox(
                    "树直接映射工程资源目录；新建 / 改名 / 删除 / 移动会写入磁盘。请在测试用子目录或备份工程中操作。忽略规则与 ListView 相同；「隐藏扩展名」支持逗号、分号或换行，例如 .meta。",
                    MessageType.Warning);
            }
        }

        private void TrySetRootFolder(DefaultAsset asset)
        {
            var p = AssetDatabase.GetAssetPath(asset);
            if (!AssetDatabase.IsValidFolder(p))
            {
                Log("请选择文件夹资源（工程内有效目录）。");
                return;
            }

            _rootFolder = asset;
            _treeRootAssetPath = p;
            Log($"树根已切换: {_treeRootAssetPath}");
        }
    }
}
#endif
