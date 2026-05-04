using System;

namespace Stratum
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FieldAttribute : Attribute
    {
        public string Title { get; set; }
        public bool Hide { get; set; }
        public bool Readonly { get; set; }
        public float Width { get; set; }
        public string Dropdown { get; set; }
    }
}
