using System.Text;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed class TextControlTestWindow : EditorWindow
    {
        private const float FooterHeight = 64f;

        private readonly TextControl _textView = new();

        private string _sampleText = string.Empty;
        private Vector2 _logScroll;

        private bool _useCustomColor;
        private Color _customColor = new(0.4f, 0.85f, 1f, 1f);

        [MenuItem("Tools/Dev Workbench/TextControl Test")]
        private static void Open()
        {
            var w = GetWindow<TextControlTestWindow>("TextControl Test");
            w.minSize = new Vector2(520f, 400f);
            w.Show();
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_sampleText))
                ApplySample(BuildLongSample());
        }

        private void ApplySample(string text)
        {
            _sampleText = text;
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

            _textView.TextColor = _useCustomColor ? _customColor : null;
            _textView.Draw(body, _sampleText);

            var lc = LineCountApprox(_sampleText);
            WorkbenchTestWindowUtil.DrawMiniLogFooter(ref _logScroll,
                $"字符串长度（C# Length / UTF‑16 码元）: {_sampleText.Length} · 粗算行数: {lc}",
                $"{SkinLabel()} · 可复制：选中文本后 Ctrl+C（右键亦可，富文本会被剥离）。",
                WorkbenchTestWindowUtil.FooterRect(position.width, body.yMax, FooterHeight));
        }

        private void DrawOptions()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("排版与颜色", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _textView.WordWrap = EditorGUILayout.ToggleLeft("自动换行", _textView.WordWrap,
                        GUILayout.Width(84f));
                    _useCustomColor = EditorGUILayout.ToggleLeft("自定义颜色", _useCustomColor,
                        GUILayout.Width(88f));
                    if (_useCustomColor)
                        _customColor = EditorGUILayout.ColorField(GUIContent.none, _customColor,
                            GUILayout.Width(120f));

                    GUILayout.FlexibleSpace();

                    EditorGUILayout.LabelField("字号", GUILayout.Width(36f));
                    _textView.FontSize = EditorGUILayout.Slider(_textView.FontSize, 9f, 24f,
                        GUILayout.Width(160f));

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("长文重置", GUILayout.Width(88f)))
                        ApplySample(BuildLongSample());
                }

                GUILayout.Space(4f);
                GUILayout.Label("内容预设（覆盖当前编辑区文本）", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("单行", GUILayout.Width(64f)))
                        ApplySample("单行示例文本。");

                    if (GUILayout.Button("多行（本地化）", GUILayout.Width(104f)))
                        ApplySample(BuildLongSample());

                    if (GUILayout.Button("富文本标签", GUILayout.Width(96f)))
                    {
                        ApplySample(
                            "<b>粗体</b> <color=#88FF88>绿色</color> <i>斜体</i> 与普通字符混排。\n" +
                            "<size=16>较大字号段落</size> 与同窗口「字号」滑条独立。");
                    }

                    if (GUILayout.Button("空文档", GUILayout.Width(72f)))
                        ApplySample(string.Empty);

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.HelpBox(
                    "TextControl 为只读展示区；适合做日志、说明或生成结果预览。拖动窗口下缘可拉伸可视区域验证滚动条。",
                    MessageType.None);
            }
        }

        private static int LineCountApprox(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            var n = 1;
            for (var i = 0; i < s.Length; i++)
            {
                if (s[i] == '\n')
                    n++;
            }

            return n;
        }

        private static string SkinLabel()
        {
            var skin = EditorGUIUtility.isProSkin ? "Dark" : "Light";
            return $"当前编辑器皮肤: {skin}";
        }

        private static string BuildLongSample()
        {
            var sb = new StringBuilder(1024);
            sb.AppendLine("Dev Workbench — TextControl");
            sb.AppendLine("本节：中英文混排 · 日文「テスト」· 韩文 테스트");
            sb.AppendLine("路径示意: Assets\\Scripts 与 Assets/Textures 在项目里都很常见（反斜杠与正斜杠）。");
            sb.AppendLine("第三行 ASCII 令牌: ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
            sb.AppendLine("连续空白字符:    ← 四个空格后接制表。\t结尾。");
            for (var i = 1; i <= 14; i++)
                sb.AppendLine($"条目{i:00}: Vivamus porta condimentum nisl.");
            return sb.ToString();
        }
    }
}
