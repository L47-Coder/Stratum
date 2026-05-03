using System.Linq;
using UnityEditor.Search;
using UnityEngine;

namespace Stratum.Editor
{
    internal sealed partial class DevWindow
    {
        internal static bool IsOpen => _instance != null;
        internal static IPage Current => _instance != null ? _instance._currentPage : null;

        internal static void Open()
        {
            WorkbenchInitializer.Ensure();

            var window = GetWindow<DevWindow>("Dev Workbench", false);
            window.minSize = new Vector2(MenuWidth, MenuWidth);
            window.position = SearchUtils.GetMainWindowCenteredPosition(new Vector2(StartWidth, StartHeight));
        }

        internal static void Refresh()
        {
            if (_instance != null)
                _instance.Repaint();
        }

        internal static void GoTo(string groupTitle, string tabTitle = null)
        {
            if (!IsOpen) Open();
            if (_instance == null) return;
            var group = _instance._groups.FirstOrDefault(g => g.Title == groupTitle);
            if (group == null) return;
            var page = tabTitle != null
                ? group.Pages.FirstOrDefault(p => p.TabTitle == tabTitle) ?? group.Pages[0]
                : group.Pages[0];
            _instance.SelectPage(page);
        }
    }
}
