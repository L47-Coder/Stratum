namespace Stratum
{
    /// <summary>
    /// 标记接口。字段值实现此接口时，TableView 会在该列渲染一个展开按钮，
    /// 点击后触发 OnExpandField 回调，供外部打开属性面板（如 FieldView 窗体）。
    /// </summary>
    public interface IFieldExpandable { }
}
