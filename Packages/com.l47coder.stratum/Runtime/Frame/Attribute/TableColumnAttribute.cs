using System;

namespace Stratum
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TableColumnAttribute : Attribute
    {
        public string Header { get; set; }
        public bool Visible { get; set; } = true;
        public bool Editable { get; set; } = true;
        public float InitialWidth { get; set; }
    }
}
