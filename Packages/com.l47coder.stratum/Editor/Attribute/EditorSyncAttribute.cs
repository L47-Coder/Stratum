using System;

namespace DevWorkbench.Editor
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class EditorSyncAttribute : Attribute
    {
        public int Order { get; }
        public EditorSyncAttribute(int order = 0) { Order = order; }
    }
}
