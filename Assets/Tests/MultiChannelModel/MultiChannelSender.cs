using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TaskFlow.TestModel
{
    /// <summary>
    /// 可挂载到 GameObject 上的发送器。
    /// 持有一组 Channel，可向指定 Channel 发布 Signal，并把一次发送汇总成 ModelSignalContext。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MultiChannelSender : MonoBehaviour
    {
        [SerializeField] private List<ModelChannel> _channels = new();

        public IReadOnlyList<ModelChannel> Channels => _channels;

        public void AddChannel(ModelChannel channel)
        {
            if (channel != null && !_channels.Contains(channel))
                _channels.Add(channel);
        }

        public void RemoveChannel(ModelChannel channel) => _channels.Remove(channel);

        public bool TryGetChannel(string idOrName, out ModelChannel channel)
        {
            channel = _channels.FirstOrDefault(c =>
                c != null &&
                (string.Equals(c.name, idOrName, StringComparison.Ordinal) ||
                 string.Equals(c.DisplayName, idOrName, StringComparison.Ordinal)));
            return channel != null;
        }

        public void Send(ModelChannel channel, ModelSignal signal)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (signal == null) throw new ArgumentNullException(nameof(signal));
            if (!_channels.Contains(channel))
            {
                Debug.LogWarning($"[TestModel] Sender '{name}' 未持有 channel '{channel.name}'，忽略发送。", this);
                return;
            }
            channel.Publish(signal);
        }

        public void Send(string channelIdOrName, ModelSignal signal)
        {
            if (!TryGetChannel(channelIdOrName, out var channel))
            {
                Debug.LogError($"[TestModel] 找不到 channel '{channelIdOrName}'。", this);
                return;
            }
            Send(channel, signal);
        }

        /// <summary>
        /// 把所有持有 channel 中当前累积的信号汇总成一次 context，并清空待发队列。
        /// </summary>
        public ModelSignalContext FlushAll()
        {
            var context = new ModelSignalContext();
            foreach (var channel in _channels)
            {
                if (channel == null) continue;
                foreach (var signal in channel.DrainPending())
                    context.Add(channel, signal);
            }
            return context;
        }
    }
}
