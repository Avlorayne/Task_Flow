using System;
using UnityEngine;

namespace TaskFlow.TestModel
{
    /// <summary>
    /// 独立测试模型：信号基类。不依赖、不修改 Runtime 中的 Signal。
    /// </summary>
    public abstract class ModelSignal
    {
        public string Id = Guid.NewGuid().ToString("N");
        public float TimeStamp = Time.realtimeSinceStartup;

        public virtual string ToDebugString()
            => $"{GetType().Name}(id={Id}, t={TimeStamp:F3})";
    }

    /// <summary>字符串信号。</summary>
    public class ModelSignalA : ModelSignal
    {
        public string Text = string.Empty;
        public override string ToDebugString() => $"{base.ToDebugString()}, Text={Text}";
    }

    /// <summary>数值信号。</summary>
    public class ModelSignalB : ModelSignal
    {
        public int Value;
        public override string ToDebugString() => $"{base.ToDebugString()}, Value={Value}";
    }

    /// <summary>布尔信号。</summary>
    public class ModelSignalC : ModelSignal
    {
        public bool Enabled;
        public override string ToDebugString() => $"{base.ToDebugString()}, Enabled={Enabled}";
    }
}
