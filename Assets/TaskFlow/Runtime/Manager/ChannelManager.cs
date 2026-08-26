using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow
{
    public class ChannelManager : MonoSingleton<ChannelManager>
    {
        private static HashSet<BaseChannel> channels = new();
        private static Dictionary<Type, BaseChannel> channelLookup = new ();
        
        public static void AddChannel(BaseChannel channel)
        {
            channel.transform.SetParent(Instance.transform);
            channels.Add(channel);
        }
        
        private static void InitLookup()
        {
            channelLookup.Clear();
            
            foreach (var channel in channels)
                if(channelLookup.ContainsValue(channel)) channelLookup.Add(channel.GetType(), channel);
        }

        public static bool TryGetChannelByTypeOfSignal<T>(out Channel<T> foundedChannel) where T : Signal
        {
            if(channels.Count != channelLookup.Count) InitLookup();
            
            foundedChannel = null;
            if (channelLookup.TryGetValue(typeof(T), out var result))
            {
                foundedChannel = result as Channel<T>;
                return true;
            }
            else
                Debug.Log($"No channel found for {typeof(T)}");
            
            return false;
        }

        public static Channel<T> GetChannelByTypeOfSignal<T>() where T : Signal
        {
            if(channels.Count != channelLookup.Count) InitLookup();
            
            if (channelLookup.TryGetValue(typeof(T), out var result))
                return result as Channel<T>;
            else
                Debug.Log($"No channel found for {typeof(T)}");
            
            return null;
        }
    }
}