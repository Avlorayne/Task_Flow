using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaskFlow.Detection;
using Unity.VisualScripting;
using UnityEngine;

namespace TaskFlow
{
    public class ChannelManager : MonoSingleton<ChannelManager>
    {
        private static HashSet<BaseChannel> channels = new();
        private static Dictionary<Type, BaseChannel> channelLookup = new ();
        
        public static SignalContext ChannelContext { get; private set; } = new();

#if UNITY_EDITOR
        private void OnValidate()
        {
            var so = Resources.LoadAll<BaseChannel>(BaseChannel.ResourcePath);
            channels.Clear();
            channels.AddRange(so);
            
        }
#endif
        
        protected override void Awake()
        {
            base.Awake();
            var so = Resources.LoadAll<BaseChannel>(BaseChannel.ResourcePath);
            channels.Clear();
            channels.AddRange(so);
        }

        void LateUpdate()
        {
            foreach (var channel in channels)
            {
                channel.LateUpdate();
            }
        }
        
        public static void AddChannel(BaseChannel channel)
        {
            channels.Add(channel);
        }
        
        private static void InitLookup()
        {
            // 可能是Lookup里面的内容不全
            if (channels.Count > channelLookup.Count)
            {
                channelLookup.Clear();
                foreach (var channel in channels)
                    channelLookup.Add(channel.GetType(), channel);
                // 还不一样？八成是Channel里面混了两个同Type的实例
                if (channels.Count > channelLookup.Count)
                {
                    channels.Clear();
                    var channelList = channelLookup.Select(item => item.Value).ToList();
                    channels.AddRange(channelList);    
                }
            }
            // 不知道是什么情况
            else if (channels.Count < channelLookup.Count)
            {
                channels.Clear();
                var channelList = channelLookup.Select(item => item.Value).ToList();
                channels.AddRange(channelList);
            }
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

        // ReSharper disable Unity.PerformanceAnalysis
        public static void RefreshSignalQueue<T>(ConcurrentQueue<T> newQueue) where T : Signal
        {
            var porter = GetChannelByTypeOfSignal<T>();
            ChannelContext.ClearQueue(porter);
            while (newQueue.TryDequeue(out var signal))
                ChannelContext.EnqueueSignal(porter, signal);
        }
    }
}