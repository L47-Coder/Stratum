using System;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class FieldViewTestWindow : EditorWindow
    {
        [MenuItem("Tools/Dev Workbench/Test/FieldView")]
        private static void Open() =>
            GetWindow<FieldViewTestWindow>("FieldView Test").Show();

        [Serializable]
        private sealed class SampleData
        {
            [Field(Title = "名称")]
            public string Name = "Hero";

            [Field(Title = "启用")]
            public bool Enabled = true;

            [Field(Title = "血量")]
            public int Health = 100;

            [Field(Title = "速度")]
            public float Speed = 5.5f;

            [Field(Title = "类别", Dropdown = nameof(GetCategories))]
            public string Category = "Player";

            [Field(Title = "颜色")]
            public Color Tint = Color.white;

            [Field(Title = "位置")]
            public Vector3 Position;

            [Field(Title = "朝向")]
            public Quaternion Rotation = Quaternion.identity;

            [Field(Title = "图标")]
            public Sprite Icon;

            [Field(Title = "ID（只读）", Readonly = true)]
            public string Id = "entity-001";

            [Field(Hide = true)]
            public string _internal = "此字段被隐藏";

            private static string[] GetCategories() =>
                new[] { "Player", "Enemy", "NPC", "Neutral" };
        }

        [Serializable]
        private sealed class SampleDataReadonly
        {
            [Field(Title = "键")]
            public string Key = "jump";

            [Field(Title = "配置地址")]
            public string ConfigAddress = "ComponentConfig/Rigidbody2D";
        }

        private readonly SampleData         _sample         = new();
        private readonly SampleDataReadonly _sampleReadonly = new();

        private readonly FieldViewPopup _popup         = new();
        private readonly FieldViewPopup _popupReadonly = new() { Readonly = true };

        private const float BtnH    = 28f;
        private const float BtnW    = 200f;
        private const float Padding = 16f;
        private const float Gap     = 12f;

        private void OnGUI()
        {
            var x = Padding;
            var y = Padding;

            EditorGUI.LabelField(new Rect(x, y, position.width - x * 2f, 18f),
                "点击按钮弹出 FieldViewPopup 泡泡", EditorStyles.centeredGreyMiniLabel);
            y += 22f;

            var editBtnRect = new Rect(x, y, BtnW, BtnH);
            if (GUI.Button(editBtnRect, "打开可编辑面板"))
                _popup.Show(editBtnRect, _sample, onChanged: Repaint);
            y += BtnH + Gap;

            var roBtnRect = new Rect(x, y, BtnW, BtnH);
            if (GUI.Button(roBtnRect, "打开只读面板"))
                _popupReadonly.Show(roBtnRect, _sampleReadonly);
        }
    }
}
