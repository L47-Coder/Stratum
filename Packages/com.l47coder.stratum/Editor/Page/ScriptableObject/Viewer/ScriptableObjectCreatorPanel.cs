using UnityEngine;

namespace Stratum.Editor
{
    internal sealed class ScriptableObjectCreatorPanel
    {
        private readonly ScriptableObjectCreatorState _state = new();
        private readonly CreatorLayout<ScriptableObjectCreatorState> _layout;

        public ScriptableObjectCreatorPanel()
        {
            _layout = new CreatorLayout<ScriptableObjectCreatorState>(
                _state, "Create ScriptableObject",
                s => ScriptableObjectCreationService.CreateScriptableObjectType(s));
        }

        public void Retarget(string parentFolderAssetPath) =>
            _state.SetParentFolder(parentFolderAssetPath);

        public void OnGUI(Rect rect) => _layout.OnGUI(rect);
    }
}
