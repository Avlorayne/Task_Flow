using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow
{
    public static class Sender
    {
        public static void Send<T>(T signal) where T : Signal
        {
            if (ChannelManager.TryGetChannelByTypeOfSignal<T>(out var channel))
            {
                if (channel != null) channel.AddMessage(signal);
            }
            else
                Debug.LogError($"Channel {typeof(T).Name} not found!");
        }
    }    
}
