using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow
{
    public class ISend
    {
        public void Send<T>(T signal) where T : Signal
        {
            if (ChannelManager.TryGetChannelByTypeOfSignal<T>(out var channel))
            {
                if (channel != null) channel.AddDelegate(signal);
            }
            else
                Debug.LogError($"Channel {typeof(T).Name} not found!");
        }
    }    
}
