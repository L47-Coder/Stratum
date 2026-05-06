using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed class TableControlTestWindow : EditorWindow
    {
        private const float FooterHeight = 56f;

        private readonly TableControl _table = new();
        private readonly List<TestRow> _rows = new();

        private Vector2 _logScroll;
        private string _lastEvent = "尚无表格事件。";

        [MenuItem("Tools/Dev Workbench/TableControl Test")]
        private static void Open()
        {
            var w = GetWindow<TableControlTestWindow>("TableControl Test");
            w.minSize = new Vector2(780f, 440f);
            w.Show();
        }

        private void OnEnable()
        {
            if (_rows.Count == 0)
                ResetRows(silent: true);

            _table.KeyField = "Key";
            _table.ToolbarButtons = WorkbenchTestWindowUtil.DefaultTestToolbar();
            HookTableCallbacks();
        }

        private void HookTableCallbacks()
        {
            _table.OnRowSelected(i => Log(RowLine("选中", i)));
            _table.OnRowRenamed(i => Log(RowLine("改名完成", i)));
            _table.OnRowAdded(i => Log(RowLine("新建", i)));
            _table.OnRowRemoved(i => Log(RowLine("删除", i)));
            _table.OnRowMoved((from, to) => Log($"移动: {from} → {to}"));
            _table.OnButtonClicked(i =>
            {
                if (i == 0)
                {
                    ResetRows(silent: true);
                    Log("工具栏 #0: 已重置示例数据。");
                }
                else
                    Log($"工具栏 #{i}");
            });
        }

        private string RowLine(string verb, int index)
        {
            if (index < 0 || index >= _rows.Count)
                return $"{verb}: 索引 {index}（无效）";
            var r = _rows[index];
            return $"{verb} #{index}: {r.Key} · {r.DisplayName}";
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
            _table.Draw(body, _rows);

            WorkbenchTestWindowUtil.DrawMiniLogFooter(ref _logScroll, _lastEvent, $"数据行数: {_rows.Count}",
                WorkbenchTestWindowUtil.FooterRect(position.width, body.yMax, FooterHeight));
        }

        private void DrawOptions()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("表格能力", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _table.CanAdd = EditorGUILayout.ToggleLeft("新建", _table.CanAdd, GUILayout.Width(52f));
                    _table.CanRemove = EditorGUILayout.ToggleLeft("删除", _table.CanRemove, GUILayout.Width(52f));
                    _table.CanSelect = EditorGUILayout.ToggleLeft("选中", _table.CanSelect, GUILayout.Width(52f));
                    _table.CanRename = EditorGUILayout.ToggleLeft("单元格编辑", _table.CanRename, GUILayout.Width(88f));
                    _table.CanDrag = EditorGUILayout.ToggleLeft("拖拽排序", _table.CanDrag, GUILayout.Width(72f));
                    _table.ShowToolbar = EditorGUILayout.ToggleLeft("工具栏", _table.ShowToolbar, GUILayout.Width(60f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("重置数据", GUILayout.Width(88f)))
                        ResetRows(silent: false);
                }

                GUILayout.Space(2f);
                GUILayout.Label("主键与查重", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(nameof(TestRow.Key), GUILayout.Width(82f));
                    _table.KeyField = EditorGUILayout.TextField(_table.KeyField);
                    _table.MarkDuplicates = EditorGUILayout.ToggleLeft("重复主键高亮", _table.MarkDuplicates,
                        GUILayout.Width(100f));
                }

                EditorGUILayout.HelpBox(
                    "示例中包含两行 Key 均为 \"enemy\"，用于验证「重复主键高亮」。工具栏首个图标（Refresh）绑定为重置示例数据。",
                    MessageType.None);
            }
        }

        private void ResetRows(bool silent)
        {
            _rows.Clear();
            _rows.Add(new TestRow
            {
                Key = "hero",
                DisplayName = "Hero",
                Enabled = true,
                Count = 1,
                Speed = 4.25f,
                Category = TestCategory.Player,
                Tint = new Color(0.35f, 0.65f, 1f),
                Offset = new Vector3(0f, 1f, 0f),
                Tags = new List<string> { "player", "protagonist" },
            });
            _rows.Add(new TestRow
            {
                Key = "enemy",
                DisplayName = "Grunt",
                Enabled = true,
                Count = 6,
                Speed = 2.1f,
                Category = TestCategory.Npc,
                Tint = new Color(1f, 0.45f, 0.35f),
                Offset = new Vector3(3f, 0f, -1f),
                Tags = new List<string> { "ai", "melee" },
            });
            _rows.Add(new TestRow
            {
                Key = "enemy",
                DisplayName = "Elite（与 Grunt 同 Key）",
                Enabled = false,
                Count = 1,
                Speed = 1.8f,
                Category = TestCategory.Npc,
                Tint = new Color(0.95f, 0.35f, 0.5f),
                Offset = new Vector3(-2f, 0.5f, 2f),
                Tags = new List<string> { "ai", "duplicate-key-demo" },
            });
            _rows.Add(new TestRow
            {
                Key = "pickup_coin",
                DisplayName = "Coin Pickup",
                Enabled = true,
                Count = 20,
                Speed = 0f,
                Category = TestCategory.Prop,
                Tint = new Color(0.95f, 0.85f, 0.2f),
                Offset = new Vector3(0.25f, 0.1f, 0f),
                Tags = new List<string> { "loot", "audio" },
            });
            _rows.Add(new TestRow
            {
                Key = "system_rng",
                DisplayName = "RNG Service",
                Enabled = true,
                Count = 0,
                Speed = 0f,
                Category = TestCategory.System,
                Tint = new Color(0.55f, 0.95f, 0.55f),
                Offset = Vector3.zero,
                Tags = new List<string> { "singleton", "no-serialize" },
            });

            if (!silent)
            {
                _lastEvent = _table.MarkDuplicates
                    ? "示例数据已重置；重复 Key「enemy」应显示高亮。"
                    : "示例数据已重置；查重已关闭。";
                Repaint();
            }
        }

        [Serializable]
        public sealed class TestRow
        {
            [Field(Title = "Key", Width = 160f)]
            public string Key = "new-row";

            [Field(Title = "Name", Width = 200f)]
            [Dropdown(nameof(GetDisplayNameOptions))]
            public string DisplayName = "Hero";

            [Field(Title = "On")]
            public bool Enabled = true;

            [Field(Title = "Count")]
            public int Count;

            [Field(Title = "Speed")]
            public float Speed = 1f;

            [Field(Title = "Category")]
            public TestCategory Category;

            [Field(Title = "Tint")]
            public Color Tint = Color.white;

            [Field(Title = "Offset")]
            public Vector3 Offset;

            [Field(Title = "Tags", Width = 220f)]
            public List<string> Tags = new();

            private static string[] GetDisplayNameOptions() => new[]
            {
                "Hero",
                "Grunt",
                "Elite（与 Grunt 同 Key）",
                "Coin Pickup",
                "RNG Service",
            };
        }

        public enum TestCategory
        {
            Player,
            Npc,
            Prop,
            System,
        }
    }
}
