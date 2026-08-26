using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow
{
    public class ChannelManager : MonoSingleton<ChannelManager>
    {
        private static HashSet<BaseChannel> channels = new();
        public static void AddChannel(BaseChannel channel)
        {
            channel.transform.SetParent(Instance.transform);
            channels.Add(channel);
        }

        public static bool TryGetChannelByTypeOfSignal<T>(out BaseChannel foundedChannel) where T : Signal
        {
            foundedChannel = null;
            foreach (var channel in channels)
                if (channel is Channel<T> converted)
                {
                    foundedChannel = converted;
                    return true;
                }
            
            return false;
        }

        public static Channel<T> GetChannelByTypeOfSignal<T>() where T : Signal
        {
            foreach (var channel in channels)
                if (channel is Channel<T> converted)
                    return converted;
            
            return null;
        }
    }
}