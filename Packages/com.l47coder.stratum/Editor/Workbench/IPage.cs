using UnityEngine;

namespace Stratum.Editor
{
    public interface IPage
    {
        string GroupTitle { get; }
        string TabTitle { get; }
        void OnFirstEnter() { }
        void OnEnter() { }
        void OnGUI(Rect rect) { }
        void OnLeave() { }
    }
}
