using UnityEditor;
using UnityEngine;
using Stratum.Editor;

namespace StratumWorkbenchExamples
{
    internal sealed class DropdownPopupExamplePage : IWorkbenchExamplePage
    {
        private const int OptionCount = 80;

        private EditorWindow _host;

        private static readonly string[] SingleOptions = BuildNumbered("Item", OptionCount);
        private static readonly string[] MultiOptions = BuildNumbered("Tag", OptionCount);

        private string _singleValue = "Item 010";
        private string _multiValue = "Tag 001, Tag 050";
        private bool _multiSearch = true;

        public string TabLabel => "下拉";

        public void OnEnable(EditorWindow host) => _host = host;

        public void OnGUI(EditorWindow host)
        {
            _host = host;
            WorkbenchTestWindowUtil.Card(() =>
            {
                WorkbenchTestWindowUtil.SectionTitle("单选");
                var singleRow = EditorGUILayout.GetControlRect(false, 22f);
                if (GUI.Button(singleRow, "打开"))
                {
                    var p = new DropdownPopup();
                    p.OnConfirmed(v =>
                    {
                        _singleValue = v;
                        _host?.Repaint();
                    });
                    p.Show(singleRow, SingleOptions, _singleValue);
                }

                EditorGUILayout.SelectableLabel(_singleValue, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                GUILayout.Space(12f);

                WorkbenchTestWindowUtil.SectionTitle("多选");
                _multiSearch = EditorGUILayout.ToggleLeft("Search", _multiSearch);

                var multiRow = EditorGUILayout.GetControlRect(false, 22f);
                if (GUI.Button(multiRow, "打开"))
                {
                    var p = new DropdownPopup { Multi = true, Separator = ", ", Search = _multiSearch };
                    p.OnConfirmed(v =>
                    {
                        _multiValue = v;
                        _host?.Repaint();
                    });
                    p.Show(multiRow, MultiOptions, _multiValue);
                }

                EditorGUILayout.SelectableLabel(_multiValue, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                GUILayout.Space(8f);
                WorkbenchTestWindowUtil.HintLine($"各 {OptionCount} 项 · GetControlRect 作锚点");
            });
        }

        private static string[] BuildNumbered(string prefix, int count)
        {
            var a = new string[count];
            for (var i = 0; i < count; i++) a[i] = $"{prefix} {i + 1:D3}";
            return a;
        }
    }
}
