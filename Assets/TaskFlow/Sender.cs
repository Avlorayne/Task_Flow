using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow
{
    public class Sender : MonoBehaviour
    {
        [SerializeField] public HashSet<BaseChannel> Channels;
        private Dictionary<Type, BaseChannel> _channelLookup = new ();

        private void InitLookup()
        {
            _channelLookup.Clear();
            
            foreach (var channel in Channels)
                _channelLookup.Add(channel.GetType(), channel);
        }
        
        public void Send<T>(T signal) where T : Signal
        {
            if(Channels.Count != _channelLookup.Count) InitLookup();
            
            var channel = _channelLookup[typeof(Channel<T>)] as Channel<T>;
            if (channel != null) channel.AddDelegate(signal);
        }
    }    
}
