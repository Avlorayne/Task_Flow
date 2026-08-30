namespace TaskFlow
{
    /// <summary>
    /// 统一的"按路径读值"契约：DetectionContext / SignalContext 各自实现，
    /// 取值差异（GetField / Dequeue）封装在 Context 内部，Detection 侧不再感知。
    /// </summary>
    public interface IContextReader
    {
        /// <summary>尝试读取。路径种类不匹配或条目缺失返回 false；读取本身无副作用。</summary>
        bool TryReadValue(IContextPath path, string fieldName, out object value);
    }
}