using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace TaskFlow.TestModel
{
    /// <summary>
    /// 一次发送后形成的上下文：Channel -> Signal 列表。
    /// </summary>
    public sealed class ModelSignalContext
    {
        private readonly Dictionary<ModelChannel, List<ModelSignal>> _received = new();

        public IReadOnlyDictionary<ModelChannel, List<ModelSignal>> Received => _received;

        public int SignalCount
        {
            get
            {
                int n = 0;
                foreach (var list in _received.Values) n += list.Count;
                return n;
            }
        }

        public void Add(ModelChannel channel, ModelSignal signal)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (signal == null) throw new ArgumentNullException(nameof(signal));
            if (!_received.TryGetValue(channel, out var list))
                _received[channel] = list = new List<ModelSignal>();
            list.Add(signal);
        }

        public bool TryGetSignals(ModelChannel channel, out IReadOnlyList<ModelSignal> signals)
        {
            if (_received.TryGetValue(channel, out var list))
            {
                signals = list;
                return true;
            }
            signals = Array.Empty<ModelSignal>();
            return false;
        }

        public IReadOnlyList<ModelSignal> GetSignals(ModelChannel channel)
            => TryGetSignals(channel, out var signals) ? signals : Array.Empty<ModelSignal>();

        /// <summary>生成便于调试/日志可读的结构。</summary>
        public string Dump()
        {
            var sb = new StringBuilder("{\"channels\":{");
            bool firstChan = true;
            foreach (var pair in _received)
            {
                if (!firstChan) sb.Append(",");
                firstChan = false;
                sb.Append("\"").Append(pair.Key.name).Append("\":[");
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    var s = pair.Value[i];
                    sb.Append("{\"type\":\"").Append(s.GetType().Name).Append("\"");
                    foreach (var f in s.GetType().GetFields(
                                 BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                    {
                        // 保留 Id 与 TimeStamp，让序列化结果能看到 Signal 的唯一标识和发送时间。
                        if (f.IsStatic) continue;
                        sb.Append(",").Append("\"").Append(f.Name).Append("\":")
                          .Append(FormatValue(f.GetValue(s)));
                    }
                    sb.Append("}");
                }
                sb.Append("]");
            }
            sb.Append("}}");
            return sb.ToString();
        }

        private static string FormatValue(object v)
        {
            switch (v)
            {
                case null: return "null";
                case string s: return "\"" + s + "\"";
                case bool b: return b ? "true" : "false";
                case int i: return i.ToString();
                case long l: return l.ToString();
                case float f: return f.ToString("R");
                case Enum e: return "\"" + e + "\"";
                default: return "\"" + v + "\"";
            }
        }
    }
}
