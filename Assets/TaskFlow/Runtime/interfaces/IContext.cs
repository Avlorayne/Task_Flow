using System.Collections.Generic;

namespace TaskFlow
{
    /// <summary>
    /// 统一 Context 接口：以 IContextPath 派生类型为键的存取容器。
    /// SignalContext    : IContext&lt;SignalContextPath, Queue&lt;Signal&gt;&gt;
    /// DetectionContext : IContext&lt;DetectionContextPath, Signal&gt;
    /// </summary>
    public interface IContext<TKey, TValue> where TKey : IContextPath
    {
        /// <summary>尝试读取，不存在返回 false</summary>
        bool TryGet(TKey path, out TValue value);

        /// <summary>读取，不存在时创建并返回默认值（原 GetQueue 的语义）</summary>
        TValue GetOrCreate(TKey path);

        /// <summary>写入/替换条目（统一"先清后写"）</summary>
        void Set(TKey path, TValue value);

        /// <summary>清空条目（SignalContext 清队列内容；DetectionContext 移除键值对）</summary>
        void Clear(TKey path);

        /// <summary>取属于某个 Porter 的全部条目（原 GetContextItemsByPorter，两个 Context 共用）</summary>
        KeyValuePair<TKey, TValue>[] GetItemsByPorter(IPorter source);
    }
}