using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TaskFlow.Utility;
using UnityEngine;

namespace TaskFlow
{
    [CreateAssetMenu(fileName = "new Channel", menuName = "TaskFlow/Channel")]
    public class Channel<T> : BaseChannel where T : Signal
    {
        public override Type SignalType { get; } =  typeof(T);
        [SerializeField, InspectorReadOnly] public string signalType = typeof(T).Name.SplitCamelCase();
        
        private static Channel<T> _instance;
        public static Channel<T> Instance
        {
            get
            {
                _instance ??= ChannelManager.Instance.GetChannelByTypeOfSignal<T>();
                if(_instance == null)
                {
                    var load = Resources.Load<Channel<T>>(ResourcePath);
                    _instance = load != null ? load : CreateInstance<Channel<T>>(); 
                    ChannelManager.Instance.AddChannel(_instance);
                }
                return _instance;
            }
            protected set => _instance = value;
        }
        
        private readonly Queue<T> _eventQueue = new ();
        
        public override void LateUpdate()
        {
            if (!_eventQueue.Any()) return;
            // 更新公共上下文
            ChannelManager.Instance.RefreshSignalQueue(_eventQueue);
            _eventQueue.Clear();
            // 唤醒每个判别器工作
            OnSignal?.Invoke();
        }

        public void AddMessage(T signal) => _eventQueue.Enqueue(signal);
    }
}
