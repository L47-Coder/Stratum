using System;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class DropdownPopup
    {
        /// <summary>是否多选。多选时关闭弹窗才一次性提交。</summary>
        public bool Multi { get; set; }

        /// <summary>多选时各项之间的分隔符；用于解析当前值与拼接最终值。</summary>
        public string Separator { get; set; } = ", ";

        /// <summary>弹窗关闭时回调，参数为最终值（多选时已用 Separator 拼接）。</summary>
        public void OnConfirmed(Action<string> callback) => _onConfirmed = callback;

        /// <param name="anchorRect">锚点矩形，泡泡从该矩形下方弹出。</param>
        /// <param name="items">候选项列表。</param>
        /// <param name="current">当前值。多选时按 Separator 拆分成已选集合。</param>
        public void Show(Rect anchorRect, string[] items, string current)
            => ShowCore(anchorRect, items, current);
    }
}
