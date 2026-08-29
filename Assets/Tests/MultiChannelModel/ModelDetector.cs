using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow.TestModel
{
    /// <summary>
    /// 独立测试 Detector：订阅一组 Channel。
    /// Receive(fullContext) 时只保留自己订阅通道里的信号，并记录在 LastContext。
    /// </summary>
    [CreateAssetMenu(fileName = "New ModelDetector", menuName = "TaskFlow/TestModel/Model Detector")]
    public sealed class ModelDetector : ScriptableObject
    {
        [SerializeField] private List<ModelChannel> _subscribedChannels = new();

        public IReadOnlyList<ModelChannel> SubscribedChannels => _subscribedChannels;
        public ModelSignalContext LastContext { get; private set; }
        public int ReceiveCount { get; private set; }

        public void Subscribe(ModelChannel channel)
        {
            if (channel != null && !_subscribedChannels.Contains(channel))
                _subscribedChannels.Add(channel);
        }

        public void Unsubscribe(ModelChannel channel) => _subscribedChannels.Remove(channel);

        public bool Subscribes(ModelChannel channel) => channel != null && _subscribedChannels.Contains(channel);

        /// <summary>接收一次全量上下文，按订阅关系过滤。</summary>
        public void Receive(ModelSignalContext fullContext)
        {
            if (fullContext == null) throw new ArgumentNullException(nameof(fullContext));

            var scoped = new ModelSignalContext();
            foreach (var channel in _subscribedChannels)
            {
                if (channel == null) continue;
                if (fullContext.TryGetSignals(channel, out var signals))
                    foreach (var s in signals)
                        scoped.Add(channel, s);
            }

            LastContext = scoped;
            ReceiveCount++;

            var parts = new List<string>();
            foreach (var pair in scoped.Received)
                foreach (var s in pair.Value)
                    parts.Add($"Channel[{pair.Key.name}] <- {s.ToDebugString()}");

            Debug.Log($"[TestModel] Detector '{name}' (第 {ReceiveCount} 次) 收到 {parts.Count} 条信号: " +
                      (parts.Count == 0 ? "<empty>" : string.Join("; ", parts)), this);
        }

        public IReadOnlyList<ModelSignal> ReceivedSignals(ModelChannel channel)
        {
            return LastContext == null ? Array.Empty<ModelSignal>() : LastContext.GetSignals(channel);
        }
    }
}
