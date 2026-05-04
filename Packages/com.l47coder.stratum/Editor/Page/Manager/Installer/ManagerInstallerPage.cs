using System.Collections.Generic;
using System.Linq;

namespace Stratum.Editor
{
    internal sealed class ManagerInstallerPage : IPage
    {
        public string GroupTitle => _core.GroupTitle;
        public string TabTitle   => _core.TabTitle;

        private readonly InstallerPageCore _core;

        public ManagerInstallerPage()
        {
            _core = new InstallerPageCore(
                groupTitle:      "Manager",
                tabTitle:        "Installer",
                introHeader:     "Built-in Manager templates",
                introBodyText:   "Pick the Manager templates you want and click Import. These templates are optional; only the Game.Managers.asmdef container is created by the framework itself.",
                emptyHelpBoxText:$"No built-in Manager templates ship with this package version. Use the Creator tab to scaffold your own Manager classes under {WorkbenchPaths.ManagerRoot}/.",
                logTag:          "Manager",
                invalidateCache: ManagerTemplateInstaller.InvalidateManifestCache,
                loadPackages:    LoadPackages,
                checkIsInstalled:ManagerTemplateInstaller.IsPackageInstalled,
                performInstall:  ManagerTemplateInstaller.InstallPackages
            );
        }

        public void OnEnter()  => _core.OnEnter();
        public void OnGUI(UnityEngine.Rect rect) => _core.OnGUI(rect);
        public void OnLeave() => _core.OnLeave();

        private static IReadOnlyList<InstallerPageCore.PackageInfo> LoadPackages()
            => ManagerTemplateInstaller.LoadManifest()
                .Select(p => new InstallerPageCore.PackageInfo(p.id, p.displayName, p.description, p.recommended))
                .ToArray();
    }
}
