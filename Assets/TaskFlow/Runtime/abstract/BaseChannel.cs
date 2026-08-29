using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TaskFlow
{
    public abstract class BaseChannel : ScriptableObject, IPorter
    {
        public const string ResourcePath = "TaskFlow/Channel";
        
        public abstract void LateUpdate();
        
        public UnityAction OnSignal {get; set; }
    }
}