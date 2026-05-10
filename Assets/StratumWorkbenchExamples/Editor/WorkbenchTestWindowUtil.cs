using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StratumWorkbenchExamples
{
    internal static class WorkbenchTestWindowUtil
    {
        private static readonly char[] Delimiters = { ',', ';', '\n' };

        private static GUIStyle _paddedColumn;
        private static GUIStyle _mutedHint;

        internal static GUIStyle PaddedColumn =>
            _paddedColumn ??= new GUIStyle { padding = new RectOffset(14, 14, 6, 8) };

        internal static GUIStyle MutedHint
        {
            get
            {
                if (_mutedHint != null) return _mutedHint;
                var s = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                {
                    normal =
                    {
                        textColor = EditorGUIUtility.isProSkin
                            ? new Color(0.72f, 0.72f, 0.76f)
                            : new Color(0.35f, 0.35f, 0.38f),
                    },
                };
                _mutedHint = s;
                return s;
            }
        }

        internal static void Card(Action draw)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(2f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            draw();
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2f);
            EditorGUILayout.EndVertical();
        }

        internal static void SectionTitle(string text)
        {
            EditorGUILayout.LabelField(text, EditorStyles.miniBoldLabel);
            GUILayout.Space(4f);
        }

        internal static void Rule()
        {
            var r = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(r, EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.08f)
                : new Color(0f, 0f, 0f, 0.1f));
        }

        internal static void HintLine(string text)
        {
            EditorGUILayout.LabelField(text, MutedHint);
        }

        internal static void ApplyDelimitedToList(string field, IList<string> target)
        {
            target.Clear();
            if (string.IsNullOrWhiteSpace(field)) return;
            foreach (var part in field.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = part.Trim();
                if (t.Length > 0) target.Add(t);
            }
        }

        internal static List<string> DelimitedExtensionsOrNull(string field)
        {
            if (string.IsNullOrWhiteSpace(field)) return null;
            var list = new List<string>();
            foreach (var part in field.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = part.Trim();
                if (t.Length > 0) list.Add(t);
            }
            return list.Count > 0 ? list : null;
        }

        internal static Rect MainContentRect(float winW, float winH, float optionsBottom, float footerH,
            float minBodyHeight = 120f, float topPad = 6f, float bottomPad = 8f)
        {
            var y = optionsBottom + topPad;
            return new Rect(0f, y, winW, Mathf.Max(minBodyHeight, winH - y - footerH - bottomPad));
        }

        internal static Rect FooterRect(float winW, float bodyBottom, float footerH, float horizontalMargin = 14f,
            float topPad = 8f)
        {
            return new Rect(horizontalMargin, bodyBottom + topPad, winW - horizontalMargin * 2f, footerH);
        }

        internal static List<GUIContent> DefaultTestToolbar()
        {
            return new List<GUIContent>
            {
                EditorGUIUtility.IconContent("Refresh"),
                EditorGUIUtility.IconContent("d_SettingsIcon"),
            };
        }

        internal static void DrawMiniLogFooter(ref Vector2 scroll, string primaryLine, string secondaryLine,
            Rect areaRect)
        {
            GUILayout.BeginArea(areaRect);
            scroll = EditorGUILayout.BeginScrollView(scroll,
                GUILayout.Width(areaRect.width),
                GUILayout.Height(areaRect.height));
            GUILayout.Label(primaryLine, EditorStyles.wordWrappedMiniLabel, GUILayout.MaxWidth(areaRect.width - 8f));
            if (!string.IsNullOrEmpty(secondaryLine))
                GUILayout.Label(secondaryLine, EditorStyles.wordWrappedMiniLabel,
                    GUILayout.MaxWidth(areaRect.width - 8f));
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        internal static string FormatDragPathsForLog()
        {
            var paths = DragAndDrop.paths;
            if (paths == null || paths.Length == 0) return "(无 paths)";
            return string.Join(", ", paths);
        }
    }
}
