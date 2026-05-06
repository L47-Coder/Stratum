using System;

namespace Stratum
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DropdownAttribute : Attribute
    {
        public string Method { get; }
        public bool Multi { get; set; }
        public string Separator { get; set; } = ", ";

        public DropdownAttribute(string method) => Method = method;
    }
}
