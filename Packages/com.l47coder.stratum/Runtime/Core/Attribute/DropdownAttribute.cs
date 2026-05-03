using System;

namespace Stratum
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DropdownAttribute : Attribute
    {
        public string MethodName { get; }
        public DropdownAttribute(string methodName) => MethodName = methodName;
    }
}
