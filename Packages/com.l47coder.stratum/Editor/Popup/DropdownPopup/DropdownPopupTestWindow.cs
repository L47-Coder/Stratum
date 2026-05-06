using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class DropdownPopupTestWindow : EditorWindow
    {
        [MenuItem("Tools/Dev Workbench/Test/DropdownPopup")]
        private static void Open() => GetWindow<DropdownPopupTestWindow>("DropdownPopup Test").Show();

        private const int OptionCount = 80;

        private static readonly string[] SingleOptions = BuildNumbered("Item", OptionCount);
        private static readonly string[] MultiOptions = BuildNumbered("Tag", OptionCount);

        private string _singleValue = "Item 010";
        private string _multiValue = "Tag 001, Tag 050";

        private static string[] BuildNumbered(string prefix, int count)
        {
            var a = new string[count];
            for (var i = 0; i < count; i++) a[i] = $"{prefix} {i + 1:D3}";
            return a;
        }

        private const float BtnH = 28f;
        private const float BtnW = 220f;
        private const float Padding = 16f;
        private const float Gap = 12f;

        private void OnGUI()
        {
            var x = Padding;
            var y = Padding;

            EditorGUI.LabelField(new Rect(x, y, position.width - x * 2f, 18f),
                $"DropdownPopup（纯 UI，每类 {OptionCount} 项，可测滚动）",
                EditorStyles.centeredGreyMiniLabel);
            y += 22f;

            var singleBtn = new Rect(x, y, BtnW, BtnH);
            if (GUI.Button(singleBtn, "单选下拉"))
            {
                var popup = new DropdownPopup();
                popup.OnConfirmed(final => { _singleValue = final; Repaint(); });
                popup.Show(singleBtn, SingleOptions, _singleValue);
            }
            y += BtnH + 4f;
            EditorGUI.LabelField(new Rect(x, y, position.width - x * 2f, EditorGUIUtility.singleLineHeight),
                $"当前值：{_singleValue}", EditorStyles.miniLabel);
            y += EditorGUIUtility.singleLineHeight + Gap;

            var multiBtn = new Rect(x, y, BtnW, BtnH);
            if (GUI.Button(multiBtn, "多选下拉（逗号拼接）"))
            {
                var popup = new DropdownPopup { Multi = true, Separator = ", " };
                popup.OnConfirmed(final => { _multiValue = final; Repaint(); });
                popup.Show(multiBtn, MultiOptions, _multiValue);
            }
            y += BtnH + 4f;
            EditorGUI.LabelField(new Rect(x, y, position.width - x * 2f, EditorGUIUtility.singleLineHeight),
                $"当前值：{_multiValue}", EditorStyles.miniLabel);
            y += EditorGUIUtility.singleLineHeight + Gap;

            EditorGUI.LabelField(new Rect(x, y, position.width - x * 2f, 40f),
                "DropdownPopup 是纯 UI 组件，不知道任何特性 / 反射 / 业务概念。",
                EditorStyles.wordWrappedMiniLabel);
        }
    }
}
