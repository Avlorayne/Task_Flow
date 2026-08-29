using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TaskFlow.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace TaskFlow
{
    [CreateAssetMenu(fileName = "new Channel", menuName = "TaskFlow/Channel")]
    public class Channel<T> : BaseChannel where T : Signal
    {
        [SerializeField, InspectorReadOnly] public string signalType = typeof(T).Name.SplitCamelCase();
        
        private static Channel<T> _instance;
        public static Channel<T> Instance
        {
            get
            {
                _instance ??= ChannelManager.GetChannelByTypeOfSignal<T>();
                if(_instance == null)
                {
                    _instance = Resources.Load<Channel<T>>(ResourcePath);
                    ChannelManager.AddChannel(_instance);
                }
                return _instance;
            }
            protected set => _instance = value;
        }
        
        private readonly ConcurrentQueue<T> _eventQueue = new ();
        
        public override void LateUpdate()
        {
            bool currentEvents = _eventQueue.Any();
            // 清理公共上下文
            ChannelManager.RefreshSignalQueue(_eventQueue);
            // 唤醒每个判别器工作
            OnSignal?.Invoke();
        }

        public void AddMessage(T signal) => _eventQueue.Enqueue(signal);
    }
}
