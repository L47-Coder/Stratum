#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    internal static class WorkbenchTestWindowUtil
    {
        private static readonly char[] s_delimiters = { ',', ';', '\n' };

        internal static void ApplyDelimitedToList(string field, IList<string> target)
        {
            target.Clear();
            if (string.IsNullOrWhiteSpace(field)) return;
            foreach (var part in field.Split(s_delimiters, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = part.Trim();
                if (t.Length > 0) target.Add(t);
            }
        }

        internal static List<string> DelimitedExtensionsOrNull(string field)
        {
            if (string.IsNullOrWhiteSpace(field)) return null;
            var list = new List<string>();
            foreach (var part in field.Split(s_delimiters, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = part.Trim();
                if (t.Length > 0) list.Add(t);
            }
            return list.Count > 0 ? list : null;
        }

        internal static Rect MainContentRect(float winW, float winH, float optionsBottom, float footerH,
            float minBodyHeight = 120f, float topPad = 4f, float bottomPad = 8f)
        {
            var y = optionsBottom + topPad;
            return new Rect(0f, y, winW, Mathf.Max(minBodyHeight, winH - y - footerH - bottomPad));
        }

        internal static Rect FooterRect(float winW, float bodyBottom, float footerH, float horizontalMargin = 8f,
            float topPad = 4f)
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
            using (var scope = new EditorGUILayout.ScrollViewScope(scroll, EditorStyles.helpBox))
            {
                scroll = scope.scrollPosition;
                GUILayout.Label(primaryLine, EditorStyles.miniLabel, GUILayout.MaxWidth(areaRect.width - 16f));
                if (!string.IsNullOrEmpty(secondaryLine))
                    GUILayout.Label(secondaryLine, EditorStyles.miniLabel,
                        GUILayout.MaxWidth(areaRect.width - 16f));
            }

            GUILayout.EndArea();
        }
    }
}
#endif
