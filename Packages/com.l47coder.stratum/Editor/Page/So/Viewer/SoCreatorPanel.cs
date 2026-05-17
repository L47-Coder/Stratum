using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class SoCreatorPanel
    {
        private readonly SoCreatorState _state = new();
        private readonly CreatorLayout<SoCreatorState> _layout;

        public SoCreatorPanel()
        {
            _layout = new CreatorLayout<SoCreatorState>(
                _state, "Create SO Type",
                s => SoCreationService.CreateSoType(s));
        }

        public void Retarget(string parentFolderAssetPath) =>
            _state.SetParentFolder(parentFolderAssetPath);

        public void OnGUI(Rect rect) => _layout.OnGUI(rect);
    }
}
