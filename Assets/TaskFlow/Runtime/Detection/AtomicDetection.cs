using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace TaskFlow.Detection
{
    public abstract class AtomicDetection : Detection
    {
        // 类型化引用：GetContext 没有 context 参数，仍依赖 Result 时留下的引用，
        // 由传入的 context 用 as 同步（类型不匹配即为 null，天然清除残留状态）
        protected DetectionContext DetectionContext;
        protected SignalContext SignalContext;
    
        // 本次评估使用的读取器 = Result 传入的那个 Context
        private IContextReader _reader;
    
        // 基类唯一抽象入口的唯一实现：子类只写 Evaluate
        public sealed override bool Result(IContextReader context)
        {
            _reader = context ?? throw new ArgumentNullException(nameof(context));
            DetectionContext = context as DetectionContext;   // 供 GetContext/CollectFieldPath 使用
            SignalContext    = context as SignalContext;
            return Evaluate();
        }
    
        /// <summary>子类唯一需要实现的方法：通过 RoutePropertyValue 取值，与 Context 种类无关。</summary>
        protected abstract bool Evaluate();
    
        // ---------- 统一路由 ----------
    
        protected object RoutePropertyValue(IDetectionField field)
        {
            switch (field)
            {
                case FieldPath fp:
                    if (_reader != null && _reader.TryReadValue(fp.Path, fp.fieldName, out var value))
                        return value;
                    throw new KeyNotFoundException(
                        $"[{GetType().Name}] 无法解析字段 '{fp.fieldName}'，" +
                        $"路径 {fp.Path?.GetPath() ?? "<null>"} 不在当前 Context 中");
    
                case CustomField customField:
                    return customField.Value;
    
                default:
                    return string.Empty;
            }
        }
    
        protected static float ToFloat(object value) => value switch
        {
            float f  => f,
            double d => (float)d,
            int i    => i,
            long l   => l,
            _        => Convert.ToSingle(value)
        };
    
        // ---------- GetContext 收集（不变，依赖上面同步的类型化字段） ----------
    
        protected void CollectFieldPath(IDetectionField field, List<KeyValuePair<DetectionContextPath, Signal>> pairs)
        {
            if (field is not FieldPath fp) return;
    
            Signal value = fp.Path switch
            {
                DetectionContextPath dcp =>
                    DetectionContext != null && DetectionContext.TryGet(dcp, out var signal) ? signal : null,
                SignalContextPath scp =>
                    SignalContext != null && SignalContext.TryGet(scp, out var queue) && queue.Count > 0
                        ? queue.Peek()
                        : null,
                _ => null
            };
    
            pairs.Add(new KeyValuePair<DetectionContextPath, Signal>(new DetectionContextPath(this, fp), value));
        }
    }
}
