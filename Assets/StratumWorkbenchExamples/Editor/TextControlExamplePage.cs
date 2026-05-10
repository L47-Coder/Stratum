using System.Text;
using UnityEditor;
using UnityEngine;
using Stratum.Editor;

namespace StratumWorkbenchExamples
{
    internal sealed class TextControlExamplePage : IWorkbenchExamplePage
    {
        private const float FooterHeight = 58f;

        private EditorWindow _host;
        private readonly TextControl _textView = new();
        private string _sampleText = string.Empty;
        private Vector2 _logScroll;

        private bool _useCustomColor;
        private Color _customColor = new(0.4f, 0.85f, 1f, 1f);

        public string TabLabel => "文本";

        public void OnEnable(EditorWindow host)
        {
            _host = host;
            if (string.IsNullOrEmpty(_sampleText))
                ApplySample(BuildLongSample());
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

            _textView.TextColor = _useCustomColor ? _customColor : null;
            _textView.Draw(body, _sampleText);

            var lc = LineCountApprox(_sampleText);
            WorkbenchTestWindowUtil.DrawMiniLogFooter(ref _logScroll,
                $"{_sampleText.Length} 字符 · ~{lc} 行",
                $"{(EditorGUIUtility.isProSkin ? "Dark" : "Light")} · 只读 Copy",
                WorkbenchTestWindowUtil.FooterRect(host.position.width, body.yMax, FooterHeight));
        }

        private void ApplySample(string text)
        {
            _sampleText = text;
            _host?.Repaint();
        }

        private void DrawOptions()
        {
            WorkbenchTestWindowUtil.Card(() =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _textView.WordWrap = EditorGUILayout.ToggleLeft("换行", _textView.WordWrap, GUILayout.Width(44f));
                    _useCustomColor = EditorGUILayout.ToggleLeft("着色", _useCustomColor, GUILayout.Width(44f));
                    if (_useCustomColor)
                        _customColor = EditorGUILayout.ColorField(GUIContent.none, _customColor, GUILayout.Width(100f));

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("字号", GUILayout.Width(28f));
                    _textView.FontSize = EditorGUILayout.Slider(_textView.FontSize, 9f, 24f, GUILayout.Width(140f));

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("长文", GUILayout.Width(44f)))
                        ApplySample(BuildLongSample());
                }

                GUILayout.Space(8f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("单行", GUILayout.Width(44f)))
                        ApplySample("单行示例。");
                    if (GUILayout.Button("多行", GUILayout.Width(44f)))
                        ApplySample(BuildLongSample());
                    if (GUILayout.Button("富文本", GUILayout.Width(52f)))
                    {
                        ApplySample(
                            "<b>粗</b> <color=#88FF88>色</color> <i>斜</i>\n<size=16>内嵌字号</size>");
                    }

                    if (GUILayout.Button("空", GUILayout.Width(36f)))
                        ApplySample(string.Empty);
                }
            });
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

        private static string BuildLongSample()
        {
            var sb = new StringBuilder(1024);
            sb.AppendLine("TextControl");
            sb.AppendLine("中 / 日 / 한");
            sb.AppendLine("Assets\\Scripts");
            sb.AppendLine("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
            for (var i = 1; i <= 14; i++)
                sb.AppendLine($"L{i:00}");
            return sb.ToString();
        }
    }
}
