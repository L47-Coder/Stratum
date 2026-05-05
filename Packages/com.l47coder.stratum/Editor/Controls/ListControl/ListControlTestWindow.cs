using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed class ListControlTestWindow : EditorWindow
    {
        private const float FooterHeight = 56f;

        private static readonly string[] s_defaultItems =
        {
            "alpha",
            "beta",
            "_prefab",
            "gamma",
            "module_core",
            "module_core",
            "module_io",
            "module_io",
            "module_io",
            "z_last_for_scroll",
        };

        private readonly ListControl _list = new();
        private readonly List<string> _items = new();

        private Vector2 _logScroll;
        private string _lastEvent = "尚无列表事件。";

        private string _ignorePatterns = string.Empty;

        [MenuItem("Tools/Dev Workbench/ListControl Test")]
        private static void Open()
        {
            var w = GetWindow<ListControlTestWindow>("ListControl Test");
            w.minSize = new Vector2(560f, 400f);
            w.Show();
        }

        private void OnEnable()
        {
            if (_items.Count == 0)
                ResetItems(silent: true);

            _list.ToolbarButtons = WorkbenchTestWindowUtil.DefaultTestToolbar();
            SyncExcludePatterns();
            HookListCallbacks();
        }

        private void SyncExcludePatterns() =>
            WorkbenchTestWindowUtil.ApplyDelimitedToList(_ignorePatterns, _list.ExcludePatterns);

        private void HookListCallbacks()
        {
            _list.OnRowSelected(i => Log(LabelAt(i, "选中")));
            _list.OnRowRenamed(i => Log(LabelAt(i, "改名完成")));
            _list.OnRowMoved((from, to) => Log($"移动（数据索引）: {from} → {to}"));
            _list.OnRowRemoved(i => Log(i >= 0 ? $"删除: 原索引 {i}" : "删除: 无效索引"));
            _list.OnRowAdded(i => Log(LabelAt(i, "新建")));
            _list.OnDropOnRow(i =>
                Log(i >= 0 && i < _items.Count
                    ? $"外部拖放到行 #{i}: \"{_items[i]}\""
                    : "外部拖放: 未命中行（dataIndex = -1）"));
            _list.OnButtonClicked(i =>
            {
                if (i == 0)
                {
                    ResetItems(silent: true);
                    Log("工具栏 #0: 已重置示例数据。");
                }
                else
                    Log($"工具栏 #{i}");
            });
        }

        private string LabelAt(int index, string prefix)
        {
            if (index < 0 || index >= _items.Count)
                return $"{prefix}: 索引 {index}（无效）";
            return $"{prefix} #{index}: \"{_items[index]}\"";
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
            _list.Draw(body, _items);

            WorkbenchTestWindowUtil.DrawMiniLogFooter(ref _logScroll, _lastEvent,
                $"数据行数: {_items.Count} · 过滤后可见行由列表内部计算。",
                WorkbenchTestWindowUtil.FooterRect(position.width, body.yMax, FooterHeight));
        }

        private void DrawOptions()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("列表能力", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _list.CanAdd = EditorGUILayout.ToggleLeft("新建", _list.CanAdd, GUILayout.Width(52f));
                    _list.CanRemove = EditorGUILayout.ToggleLeft("删除", _list.CanRemove, GUILayout.Width(52f));
                    _list.CanSelect = EditorGUILayout.ToggleLeft("选中", _list.CanSelect, GUILayout.Width(52f));
                    _list.CanRename = EditorGUILayout.ToggleLeft("改名", _list.CanRename, GUILayout.Width(52f));
                    _list.CanDrag = EditorGUILayout.ToggleLeft("拖拽排序", _list.CanDrag, GUILayout.Width(72f));
                    _list.CanReceiveDrop = EditorGUILayout.ToggleLeft("外部拖入", _list.CanReceiveDrop,
                        GUILayout.Width(72f));
                    _list.ShowToolbar = EditorGUILayout.ToggleLeft("工具栏", _list.ShowToolbar, GUILayout.Width(60f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("重置数据", GUILayout.Width(88f)))
                        ResetItems(silent: false);
                }

                GUILayout.Space(2f);
                GUILayout.Label("搜索 / 过滤", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("忽略 glob", GUILayout.Width(72f));
                    EditorGUI.BeginChangeCheck();
                    _ignorePatterns = EditorGUILayout.TextField(_ignorePatterns);
                    if (EditorGUI.EndChangeCheck())
                        SyncExcludePatterns();
                }

                EditorGUILayout.HelpBox(
                    "支持 glob（*、?）；多项用逗号、分号或换行分隔。示例：_* 隐藏以 _ 开头的条目；再结合数据中的 \"_prefab\"。刷新按钮仅在工具栏 #0 的逻辑中重置数据（或下方「重置数据」）。",
                    MessageType.None);

                GUILayout.Space(2f);
            }
        }

        private void ResetItems(bool silent)
        {
            _items.Clear();
            _items.AddRange(s_defaultItems);

            if (!silent)
            {
                _lastEvent = _list.ExcludePatterns.Count > 0
                    ? "示例数据已重置；当前忽略规则已参与过滤。"
                    : "示例数据已重置。";
                Repaint();
            }
        }
    }
}
