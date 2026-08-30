using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace TaskFlow
{
    public class ChannelManager : MonoSingleton<ChannelManager>
    {
        [SerializeField] private BaseChannel[] channels;
        
        /// <summary>
        /// - Key : Signal Type <br/>
        /// - Value : Channel&lt;T&gt;
        /// </summary>
        private Dictionary<Type, BaseChannel> channelLookup = new ();
        
        public SignalContext ChannelContext { get; private set; } = new();

#if UNITY_EDITOR
        private void OnValidate()
        {
            var so = Resources.LoadAll<BaseChannel>(BaseChannel.ResourcePath);
            
            channelLookup.Clear();
            foreach (var channel in so)
                channelLookup.Add(channel.SignalType, channel);
            
            var channelList = channelLookup.Select(item => item.Value).ToList();
            channels = channelList.ToArray();
        }
#endif
        
        protected override void Awake()
        {
            base.Awake();
            var so = Resources.LoadAll<BaseChannel>(BaseChannel.ResourcePath);
            channelLookup.Clear();
            foreach (var channel in so)
                channelLookup.Add(channel.SignalType, channel);
            
            var channelList = channelLookup.Select(item => item.Value).ToList();
            channels = channelList.ToArray();
        }

        void LateUpdate()
        {
            foreach (var channel in channels)
            {
                channel.LateUpdate();
            }
        }
        
        public void AddChannel(BaseChannel channel)
        {
            if(channels.Length != channelLookup.Count) InitLookup();
            
            if(channelLookup.ContainsKey(channel.SignalType)) return;
            
            if(channelLookup.TryAdd(channel.SignalType, channel))
            {
                var list = new List<BaseChannel>(channels) { channel };
                channels =  list.ToArray();
            }
        }
        
        private void InitLookup()
        {
            var channelList = channelLookup.Select(item => item.Value).ToList();
            channels = channelList.ToArray();
        }

        public bool TryGetChannelByTypeOfSignal<T>(out Channel<T> foundedChannel) where T : Signal
        {
            if(channels.Length != channelLookup.Count) InitLookup();
            
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

        public Channel<T> GetChannelByTypeOfSignal<T>() where T : Signal
        {
            if(channels.Length != channelLookup.Count) InitLookup();
            
            if (channelLookup.TryGetValue(typeof(T), out var result))
                return result as Channel<T>;
            else
                Debug.Log($"No channel found for {typeof(T)}");
            
            return null;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void RefreshSignalQueue<T>(Queue<T> newQueue) where T : Signal
        {
            var porter = GetChannelByTypeOfSignal<T>();
            ChannelContext.SetQueue(porter, newQueue);
        }
    }
}