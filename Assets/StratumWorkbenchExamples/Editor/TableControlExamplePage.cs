using System;
using System.Collections.Generic;
using Stratum;
using Stratum.Editor;
using UnityEditor;
using UnityEngine;

namespace StratumWorkbenchExamples
{
    internal sealed class TableControlExamplePage : IWorkbenchExamplePage
    {
        private const float FooterHeight = 54f;

        private EditorWindow _host;
        private readonly TableControl _table = new();
        private readonly FieldPopup _expandPayloadPopup = new();
        private readonly List<TableDemoRow> _rows = new();
        private Vector2 _logScroll;

        private string _lastEvent = "就绪";

        public string TabLabel => "表格";

        public void OnEnable(EditorWindow host)
        {
            _host = host;
            if (_rows.Count == 0)
                ResetRows(silent: true);
            _table.KeyField = nameof(TableDemoRow.Key);
            _table.ToolbarButtons = WorkbenchTestWindowUtil.DefaultTestToolbar();
            BindTableCallbacks();
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
            _table.Draw(body, _rows);

            WorkbenchTestWindowUtil.DrawMiniLogFooter(ref _logScroll, _lastEvent,
                "Payload 列为 Expandable · 重复 Key「enemy」测查重",
                WorkbenchTestWindowUtil.FooterRect(host.position.width, body.yMax, FooterHeight));
        }

        private void BindTableCallbacks()
        {
            _table.OnRowSelect(i => Log(RowLine("选", i)));
            _table.OnRowEdit(i => Log(RowLine("写", i)));
            _table.OnRowAdd(i => Log(RowLine("+", i)));
            _table.OnRowRemove(i => Log(RowLine("-", i)));
            _table.OnRowMove((a, b) => Log($"排 {a}→{b}"));
            _table.OnRowDragOut(i => Log(i >= 0 ? $"拖出 {i}" : $"拖出 {i}"));
            _table.OnRowReceiveDrop(i =>
            {
                var p = $" · {WorkbenchTestWindowUtil.FormatDragPathsForLog()}";
                Log(i >= 0 && i < _rows.Count ? $"拖入 行{i} {_rows[i].Key}{p}" : $"拖入 未命中 {i}{p}");
            });
            _table.OnRowExpandField((rowIndex, fieldName, anchor) =>
            {
                if (rowIndex < 0 || rowIndex >= _rows.Count) return;
                if (fieldName != nameof(TableDemoRow.Payload))
                {
                    Log($"Expand {fieldName}");
                    return;
                }

                _expandPayloadPopup.OnClosed(() => _host?.Repaint());
                _expandPayloadPopup.Show(anchor, _rows[rowIndex].Payload);
                Log($"Payload 行{rowIndex}");
            });
            _table.OnButtonClick(i =>
            {
                if (i == 0)
                {
                    ResetRows(silent: true);
                    Log("已重置");
                }
                else
                    Log($"工具栏 {i}");
            });
        }

        private string RowLine(string tag, int index)
        {
            if (index < 0 || index >= _rows.Count)
                return $"{tag} ?{index}";
            var r = _rows[index];
            return $"{tag} [{index}] {r.Key}";
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
                    _table.CanAdd = EditorGUILayout.ToggleLeft("新建", _table.CanAdd, GUILayout.Width(48f));
                    _table.CanRemove = EditorGUILayout.ToggleLeft("删除", _table.CanRemove, GUILayout.Width(48f));
                    _table.CanSelect = EditorGUILayout.ToggleLeft("选中", _table.CanSelect, GUILayout.Width(48f));
                    _table.CanEdit = EditorGUILayout.ToggleLeft("编辑", _table.CanEdit, GUILayout.Width(48f));
                    _table.CanReorder = EditorGUILayout.ToggleLeft("重排", _table.CanReorder, GUILayout.Width(48f));
                    _table.CanDragOut = EditorGUILayout.ToggleLeft("拖出", _table.CanDragOut, GUILayout.Width(48f));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _table.CanReceiveDrop =
                        EditorGUILayout.ToggleLeft("收拖入", _table.CanReceiveDrop, GUILayout.Width(60f));
                    _table.ShowToolbar = EditorGUILayout.ToggleLeft("工具栏", _table.ShowToolbar, GUILayout.Width(60f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("重置", GUILayout.Width(64f)))
                        ResetRows(silent: false);
                }

                GUILayout.Space(10f);

                WorkbenchTestWindowUtil.SectionTitle("主键");
                using (new EditorGUILayout.HorizontalScope())
                {
                    _table.KeyField = EditorGUILayout.TextField(_table.KeyField);
                    _table.MarkDuplicates = EditorGUILayout.ToggleLeft("查重", _table.MarkDuplicates, GUILayout.Width(52f));
                }

                GUILayout.Space(6f);
                WorkbenchTestWindowUtil.HintLine("点击 Payload 列类型名打开 FieldPopup。");
            });
        }

        private static TableDemoPayload Payload(string note, float weight) =>
            new() { Note = note, Weight = weight };

        private void ResetRows(bool silent)
        {
            _rows.Clear();
            _rows.Add(new TableDemoRow
            {
                Key = "hero",
                DisplayName = "Hero",
                Enabled = true,
                Count = 1,
                Speed = 4.25f,
                Category = DemoCategory.Player,
                Tint = new Color(0.35f, 0.65f, 1f),
                Offset = new Vector3(0f, 1f, 0f),
                Tags = new List<string> { "player", "protagonist" },
                Payload = Payload("主角", 1f),
            });
            _rows.Add(new TableDemoRow
            {
                Key = "enemy",
                DisplayName = "Grunt",
                Enabled = true,
                Count = 6,
                Speed = 2.1f,
                Category = DemoCategory.Npc,
                Tint = new Color(1f, 0.45f, 0.35f),
                Offset = new Vector3(3f, 0f, -1f),
                Tags = new List<string> { "ai", "melee" },
                Payload = Payload("小怪", 0.85f),
            });
            _rows.Add(new TableDemoRow
            {
                Key = "enemy",
                DisplayName = "Elite",
                Enabled = false,
                Count = 1,
                Speed = 1.8f,
                Category = DemoCategory.Npc,
                Tint = new Color(0.95f, 0.35f, 0.5f),
                Offset = new Vector3(-2f, 0.5f, 2f),
                Tags = new List<string> { "ai", "dup" },
                Payload = Payload("精英", 1.35f),
            });
            _rows.Add(new TableDemoRow
            {
                Key = "pickup_coin",
                DisplayName = "Coin",
                Enabled = true,
                Count = 20,
                Speed = 0f,
                Category = DemoCategory.Prop,
                Tint = new Color(0.95f, 0.85f, 0.2f),
                Offset = new Vector3(0.25f, 0.1f, 0f),
                Tags = new List<string> { "loot" },
                Payload = Payload("掉落", 0.2f),
            });
            _rows.Add(new TableDemoRow
            {
                Key = "system_rng",
                DisplayName = "RNG",
                Enabled = true,
                Count = 0,
                Speed = 0f,
                Category = DemoCategory.System,
                Tint = new Color(0.55f, 0.95f, 0.55f),
                Offset = Vector3.zero,
                Tags = new List<string> { "svc" },
                Payload = Payload("系统", 0f),
            });

            if (!silent)
            {
                _lastEvent = _table.MarkDuplicates ? "已重置 · enemy 碰撞" : "已重置";
                _host?.Repaint();
            }
        }

        [Serializable]
        public sealed class TableDemoPayload
        {
            [Field(Title = "备注")]
            public string Note = "";

            [Field(Title = "权重")]
            public float Weight = 1f;
        }

        [Serializable]
        public sealed class TableDemoRow
        {
            [Field(Title = "Key", Width = 140f)]
            public string Key = "new-row";

            [Field(Title = "Name", Width = 176f)]
            [Dropdown(nameof(GetDisplayNameOptions))]
            public string DisplayName = "Hero";

            [Field(Title = "On")]
            public bool Enabled = true;

            [Field(Title = "Ct")]
            public int Count;

            [Field(Title = "Spd")]
            public float Speed = 1f;

            [Field(Title = "Cat")]
            public DemoCategory Category;

            [Field(Title = "Tint")]
            public Color Tint = Color.white;

            [Field(Title = "Off")]
            public Vector3 Offset;

            [Field(Title = "Tags", Width = 160f)]
            public List<string> Tags = new();

            [Expandable]
            [Field(Title = "Payload", Width = 128f)]
            public TableDemoPayload Payload = new();

            private static string[] GetDisplayNameOptions() =>
                new[] { "Hero", "Grunt", "Elite", "Coin", "RNG" };
        }

        public enum DemoCategory
        {
            Player,
            Npc,
            Prop,
            System,
        }
    }
}
