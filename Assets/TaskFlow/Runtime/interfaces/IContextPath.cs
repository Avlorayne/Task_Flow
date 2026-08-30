using System;
using System.Collections.Generic;

namespace TaskFlow
{
    /// <summary>
    /// 统一路径接口：SignalContextPath / DetectionContextPath 的公共契约。
    /// </summary>
    public interface IContextPath : IEquatable<IContextPath>
    {
        /// <summary>条目来源 Porter（DetectionContextPath 的全局路径允许为 null）</summary>
        IPorter Source { get; }

        /// <summary>路径的可读表示，用于调试/日志（原 DetectionContextPath 独有，提升为公共契约）</summary>
        string GetPath();
    }

    
}