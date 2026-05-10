using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace StratumWorkbenchExamples
{
    internal sealed class WorkbenchExamplesHubWindow : EditorWindow
    {
        private IWorkbenchExamplePage[] _pages;
        private string[] _tabLabels;
        private int _tab;

        [MenuItem("Tools/Stratum/Test")]
        private static void MenuOpen()
        {
            var window = GetWindow<WorkbenchExamplesHubWindow>("Test", false);
            window.position = SearchUtils.GetMainWindowCenteredPosition(new Vector2(900f, 620f));
        }

        private void OnEnable()
        {
            EnsurePages();
            foreach (var p in _pages)
                p.OnEnable(this);
            ClampTab();
        }

        private void EnsurePages()
        {
            if (_pages != null && _pages.Length != 0) return;
            _pages = new IWorkbenchExamplePage[]
            {
                new ListControlExamplePage(),
                new TableControlExamplePage(),
                new TreeControlExamplePage(),
                new TextControlExamplePage(),
                new DropdownPopupExamplePage(),
                new FieldPopupExamplePage(),
            };
            _tabLabels = new string[_pages.Length];
            for (var i = 0; i < _pages.Length; i++)
                _tabLabels[i] = _pages[i].TabLabel;
        }

        private void ClampTab()
        {
            if (_pages == null || _pages.Length == 0) return;
            if (_tab < 0 || _tab >= _pages.Length)
                _tab = 0;
        }

        private void OnGUI()
        {
            EnsurePages();
            ClampTab();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var next = GUILayout.Toolbar(_tab, _tabLabels, EditorStyles.toolbarButton);
                if (next != _tab)
                {
                    _tab = next;
                    GUI.FocusControl(null);
                }

                GUILayout.FlexibleSpace();
            }

            WorkbenchTestWindowUtil.Rule();

            EditorGUILayout.BeginVertical(WorkbenchTestWindowUtil.PaddedColumn);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_pages[_tab].TabLabel, EditorStyles.boldLabel, GUILayout.Width(120f));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Stratum.Editor", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6f);
            _pages[_tab].OnGUI(this);
            EditorGUILayout.EndVertical();
        }
    }
}
