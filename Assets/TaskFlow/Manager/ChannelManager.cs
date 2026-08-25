using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow
{
    public class ChannelManager : MonoSingleton<ChannelManager>
    {
        private HashSet<BaseChannel> channels = new();
        public void AddChannel(BaseChannel channel)
        {
            channel.transform.SetParent(this.transform);
            channels.Add(channel);
        }

        public bool TryGetChannelSingleton<T>(out BaseChannel foundChannel) where T : Signal
        {
            foundChannel = null;
            foreach (var channel in channels)
            {
                if (channel is Channel<T> converted)
                {
                    foundChannel = converted;
                    return true;
                }
            }
            return false;
        }

        public Channel<T> GetChannelSingleton<T>() where T : Signal
        {
            foreach (var channel in channels)
            {
                if (channel is Channel<T> converted)
                {
                    return converted;
                }
            }
            return null;
        }
    }
}