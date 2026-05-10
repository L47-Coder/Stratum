using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Stratum.Editor;

namespace StratumWorkbenchExamples
{
    internal sealed class ListControlExamplePage : IWorkbenchExamplePage
    {
        private const float FooterHeight = 54f;

        private static readonly string[] DefaultItems =
        {
            "alpha", "beta", "_prefab", "gamma",
            "module_core", "module_core", "module_io", "module_io", "module_io",
            "z_last_for_scroll",
        };

        private EditorWindow _host;

        private readonly ListControl _list = new();
        private readonly List<string> _items = new();
        private Vector2 _logScroll;

        private string _lastEvent = "就绪";
        private string _ignorePatterns = string.Empty;
        private int _selectDataIndex;

        public string TabLabel => "列表";

        public void OnEnable(EditorWindow host)
        {
            _host = host;
            if (_items.Count == 0)
                ResetItems(silent: true);
            _list.ToolbarButtons = WorkbenchTestWindowUtil.DefaultTestToolbar();
            SyncExcludePatterns();
            BindListCallbacks();
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
            _list.Draw(body, _items);

            WorkbenchTestWindowUtil.DrawMiniLogFooter(ref _logScroll, _lastEvent,
                "搜索在控件内 · glob 黑名单 · 索引均为数据行号",
                WorkbenchTestWindowUtil.FooterRect(host.position.width, body.yMax, FooterHeight));
        }

        private void BindListCallbacks()
        {
            _list.OnRowSelect(i => Log(LabelAt(i, "选中")));
            _list.OnRowEdit(i => Log(LabelAt(i, "改名")));
            _list.OnRowMove((from, to) => Log($"{from} → {to}"));
            _list.OnRowRemove(i => Log(i >= 0 ? $"删 {i}" : "删（无效）"));
            _list.OnRowAdd(i => Log(LabelAt(i, "新建")));
            _list.OnRowDragOut(i =>
                Log(i >= 0 ? $"拖出 {i} \"{_items[i]}\"" : $"拖出 {i}"));
            _list.OnRowReceiveDrop(i =>
            {
                var p = $" · {WorkbenchTestWindowUtil.FormatDragPathsForLog()}";
                Log(i >= 0 && i < _items.Count ? $"拖入 行{i} {_items[i]}{p}" : $"拖入 未命中 {i}{p}");
            });
            _list.OnButtonClick(i =>
            {
                if (i == 0)
                {
                    ResetItems(silent: true);
                    Log("已重置示例");
                }
                else
                    Log($"工具栏 {i}");
            });
        }

        private void SyncExcludePatterns() =>
            WorkbenchTestWindowUtil.ApplyDelimitedToList(_ignorePatterns, _list.ExcludePatterns);

        private string LabelAt(int index, string verb)
        {
            if (index < 0 || index >= _items.Count)
                return $"{verb} ?{index}";
            return $"{verb} [{index}] {_items[index]}";
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
                    _list.CanAdd = EditorGUILayout.ToggleLeft("新建", _list.CanAdd, GUILayout.Width(48f));
                    _list.CanRemove = EditorGUILayout.ToggleLeft("删除", _list.CanRemove, GUILayout.Width(48f));
                    _list.CanSelect = EditorGUILayout.ToggleLeft("选中", _list.CanSelect, GUILayout.Width(48f));
                    _list.CanEdit = EditorGUILayout.ToggleLeft("改名", _list.CanEdit, GUILayout.Width(48f));
                    _list.CanReorder = EditorGUILayout.ToggleLeft("排序", _list.CanReorder, GUILayout.Width(48f));
                    _list.CanDragOut = EditorGUILayout.ToggleLeft("拖出", _list.CanDragOut, GUILayout.Width(48f));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _list.CanReceiveDrop =
                        EditorGUILayout.ToggleLeft("收拖入", _list.CanReceiveDrop, GUILayout.Width(60f));
                    _list.ShowToolbar = EditorGUILayout.ToggleLeft("工具栏", _list.ShowToolbar, GUILayout.Width(60f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("重置", GUILayout.Width(64f)))
                        ResetItems(silent: false);
                }

                GUILayout.Space(10f);

                WorkbenchTestWindowUtil.SectionTitle("SelectRow");
                using (new EditorGUILayout.HorizontalScope())
                {
                    _selectDataIndex = EditorGUILayout.IntField(_selectDataIndex, GUILayout.Width(48f));
                    if (GUILayout.Button("执行", GUILayout.Width(52f)))
                    {
                        var ok = _list.SelectRow(_selectDataIndex);
                        Log(ok ? $"SelectRow({_selectDataIndex}) ✓" : $"SelectRow({_selectDataIndex}) ✗");
                    }
                }

                GUILayout.Space(10f);

                WorkbenchTestWindowUtil.SectionTitle("glob 黑名单");
                EditorGUI.BeginChangeCheck();
                _ignorePatterns = EditorGUILayout.TextField(_ignorePatterns);
                if (EditorGUI.EndChangeCheck())
                    SyncExcludePatterns();

                GUILayout.Space(6f);
                WorkbenchTestWindowUtil.HintLine("拖出列表外触发 OnRowDragOut；Project 拖入触发 OnRowReceiveDrop。");
            });
        }

        private void ResetItems(bool silent)
        {
            _items.Clear();
            _items.AddRange(DefaultItems);
            if (!silent)
            {
                _lastEvent = _list.ExcludePatterns.Count > 0 ? "已重置（有过滤）" : "已重置";
                _host?.Repaint();
            }
        }
    }
}
