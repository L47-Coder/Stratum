using System;
using Stratum;
using Stratum.Editor;
using UnityEditor;
using UnityEngine;

namespace StratumWorkbenchExamples
{
    internal sealed class FieldPopupExamplePage : IWorkbenchExamplePage
    {
        private EditorWindow _host;

        private readonly SampleDatum _sample = new();
        private readonly SampleDatumReadonly _sampleReadonly = new();
        private readonly FieldPopup _popupEdit = new();
        private readonly FieldPopup _popupReadonly = new() { Readonly = true };

        public string TabLabel => "字段";

        public void OnEnable(EditorWindow host) => _host = host;

        public void OnGUI(EditorWindow host)
        {
            _host = host;
            WorkbenchTestWindowUtil.Card(() =>
            {
                var editRow = EditorGUILayout.GetControlRect(false, 22f);
                if (GUI.Button(editRow, "可编辑"))
                {
                    _popupEdit.OnClosed(() => _host?.Repaint());
                    _popupEdit.Show(editRow, _sample);
                }

                GUILayout.Space(6f);

                var roRow = EditorGUILayout.GetControlRect(false, 22f);
                if (GUI.Button(roRow, "只读"))
                {
                    _popupReadonly.OnClosed(() => _host?.Repaint());
                    _popupReadonly.Show(roRow, _sampleReadonly);
                }

                GUILayout.Space(12f);

                WorkbenchTestWindowUtil.SectionTitle("快照");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(_sample.Name);
                    EditorGUILayout.TextField(_sample.Badge);
                    EditorGUILayout.Toggle(_sample.Enabled);
                }

                GUILayout.Space(6f);
                WorkbenchTestWindowUtil.HintLine("与 TableControl 列共用 Field / Dropdown 元数据");
            });
        }

        [Serializable]
        private sealed class SampleDatum
        {
            [Field(Title = "名称")]
            public string Name = "Hero";

            [Field(Title = "启用")]
            public bool Enabled = true;

            [Field(Title = "血量")]
            public int Health = 100;

            [Field(Title = "速度")]
            public float Speed = 5.5f;

            [Field(Title = "类别")]
            [Dropdown(nameof(GetCategories))]
            public string Category = "Player";

            [Field(Title = "徽记")]
            [Dropdown(nameof(GetShortList), Search = false)]
            public string Badge = "A";

            [Field(Title = "颜色")]
            public Color Tint = Color.white;

            [Field(Title = "位置")]
            public Vector3 Position;

            [Field(Title = "朝向")]
            public Quaternion Rotation = Quaternion.identity;

            [Field(Title = "图标")]
            public Sprite Icon;

            [Field(Title = "ID", Readonly = true)]
            public string Id = "entity-001";

            [Field(Hide = true)]
            public string InternalNote = "";

            private static string[] GetCategories() => new[] { "Player", "Enemy", "NPC", "Neutral" };
            private static string[] GetShortList() => new[] { "A", "B", "C" };
        }

        [Serializable]
        private sealed class SampleDatumReadonly
        {
            [Field(Title = "键")]
            public string Key = "jump";

            [Field(Title = "配置")]
            public string ConfigAddress = "ComponentConfig/Rigidbody2D";
        }
    }
}
