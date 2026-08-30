using System;
using UnityEngine;
using UnityEngine.Events;

namespace TaskFlow
{
    [Serializable]
    public abstract class BaseChannel : ScriptableObject, IPorter
    {
        public abstract Type SignalType { get; }
        
        public const string ResourcePath = "TaskFlow/Channel";
        
        public abstract void LateUpdate();
        
        public UnityAction OnSignal {get; set; }
    }
}