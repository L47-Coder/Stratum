using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class FieldViewTestWindow : EditorWindow
    {
        [MenuItem("Tools/Dev Workbench/Test/FieldView")]
        private static void Open() =>
            GetWindow<FieldViewTestWindow>("FieldView Test").Show();

        // ── 示例数据 ─────────────────────────────────────────────────────────────

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

        // ── 只读示例 ─────────────────────────────────────────────────────────────

        [Serializable]
        private sealed class SampleDataReadonly
        {
            [Field(Title = "键")]
            public string Key = "jump";

            [Field(Title = "配置地址")]
            public string ConfigAddress = "ComponentConfig/Rigidbody2D";
        }

        // ── 状态 ─────────────────────────────────────────────────────────────────

        private readonly FieldView _fieldView         = new();
        private readonly FieldView _fieldViewReadonly = new() { Readonly = true };
        private readonly SampleData         _sample         = new();
        private readonly SampleDataReadonly _sampleReadonly = new();

        private float   _splitterY     = 300f;
        private bool    _dragging;
        private Vector2 _scrollPos;

        private const float SplitterH      = 1f;
        private const float SplitterHitH   = 6f;
        private static readonly Color SplitterColor = new(0.11f, 0.11f, 0.11f);

        // ── GUI ──────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            var fullRect = new Rect(0f, 0f, position.width, position.height);

            HandleSplitter(fullRect);

            var topRect    = new Rect(fullRect.x, fullRect.y, fullRect.width, _splitterY);
            var divRect    = new Rect(fullRect.x, _splitterY, fullRect.width, SplitterH);
            var bottomRect = new Rect(fullRect.x, _splitterY + SplitterH,
                                      fullRect.width, fullRect.height - _splitterY - SplitterH);

            DrawSection(topRect,    "可编辑", _fieldView,         _sample);
            EditorGUI.DrawRect(divRect, SplitterColor);
            DrawSection(bottomRect, "只读",   _fieldViewReadonly, _sampleReadonly);

            if (GUI.changed) Repaint();
        }

        private static void DrawSection<T>(Rect rect, string title, FieldView view, T data)
        {
            const float TitleH = 20f;
            EditorGUI.LabelField(
                new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, TitleH - 2f),
                title, EditorStyles.boldLabel);

            var viewRect = new Rect(rect.x, rect.y + TitleH, rect.width, rect.height - TitleH);
            view.Draw(viewRect, data);
        }

        // ── 分割条拖拽 ───────────────────────────────────────────────────────────

        private void HandleSplitter(Rect fullRect)
        {
            var hitRect = new Rect(fullRect.x, _splitterY - SplitterHitH * 0.5f,
                                   fullRect.width, SplitterHitH);
            EditorGUIUtility.AddCursorRect(hitRect, MouseCursor.ResizeVertical);

            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.MouseDown when hitRect.Contains(evt.mousePosition):
                    _dragging = true;
                    evt.Use();
                    break;
                case EventType.MouseDrag when _dragging:
                    _splitterY = Mathf.Clamp(evt.mousePosition.y, 80f, fullRect.height - 80f);
                    evt.Use();
                    Repaint();
                    break;
                case EventType.MouseUp when _dragging:
                    _dragging = false;
                    evt.Use();
                    break;
            }
        }
    }
}
