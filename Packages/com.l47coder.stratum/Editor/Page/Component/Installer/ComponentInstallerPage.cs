using System.Collections.Generic;
using System.Linq;

namespace Stratum.Editor
{
    internal sealed class ComponentInstallerPage : IPage
    {
        public string GroupTitle => _core.GroupTitle;
        public string TabTitle => _core.TabTitle;

        private readonly InstallerPageCore _core;

        public ComponentInstallerPage()
        {
            _core = new InstallerPageCore(
                groupTitle: "Component",
                tabTitle: "Installer",
                introHeader: "Built-in Component templates",
                introBodyText: "Select templates to import. Only Game.Components.asmdef is created by default.",
                emptyHelpBoxText: "No built-in Component templates found. Use the Creator tab to create your own.",
                logTag: "Component",
                invalidateCache: ComponentTemplateInstaller.InvalidateManifestCache,
                loadPackages: LoadPackages,
                checkIsInstalled: ComponentTemplateInstaller.IsPackageInstalled,
                performInstall: ComponentTemplateInstaller.InstallPackages
            );
        }

        public void OnEnter() => _core.OnEnter();
        public void OnGUI(UnityEngine.Rect rect) => _core.OnGUI(rect);
        public void OnLeave() => _core.OnLeave();

        private static IReadOnlyList<InstallerPageCore.PackageInfo> LoadPackages()
            => ComponentTemplateInstaller.LoadManifest()
                .Select(p => new InstallerPageCore.PackageInfo(p.id, p.displayName, p.description, p.recommended))
                .ToArray();
    }
}
