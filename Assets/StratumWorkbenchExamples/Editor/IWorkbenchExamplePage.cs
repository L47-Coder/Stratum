using UnityEditor;

namespace StratumWorkbenchExamples
{
    internal interface IWorkbenchExamplePage
    {
        string TabLabel { get; }
        void OnEnable(EditorWindow host);
        void OnGUI(EditorWindow host);
    }
}
