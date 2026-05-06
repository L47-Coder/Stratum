using System;

namespace Stratum
{
    /// <summary>
    /// 在 string 字段上声明此特性后，渲染层会用 DropdownPopup 弹出候选列表供用户选择。
    /// 候选源由 <see cref="Method"/> 指定的静态方法提供（返回 string[] / List&lt;string&gt; / IEnumerable&lt;string&gt;）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DropdownAttribute : Attribute
    {
        /// <summary>提供候选项的静态方法名（与字段同声明类型上）。</summary>
        public string Method { get; }

        /// <summary>是否多选。多选时所选项以 <see cref="Separator"/> 拼接成单个字符串写回字段。</summary>
        public bool Multi { get; set; }

        /// <summary>多选时各项之间的分隔符；解析回选中集合时也按此切分。</summary>
        public string Separator { get; set; } = ", ";

        public DropdownAttribute(string method) => Method = method;
    }
}
