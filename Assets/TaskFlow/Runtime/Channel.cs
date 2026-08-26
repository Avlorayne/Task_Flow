using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TaskFlow
{
    public class BaseChannel : MonoBehaviour, IPorter {}
    
    public class Channel<T> : BaseChannel where T : Signal
    {
        [SerializeField] private List<Detector> subscribers = new ();
        private readonly ConcurrentQueue<T> _eventQueue = new ();

        private static ChannelManager Manager => ChannelManager.Instance;
        private static Channel<T> _instance;
        public static Channel<T> Instance
        {
            get
            {
                if (_instance != null) return _instance;
                // 在Manager中查找
                _instance ??= ChannelManager.GetChannelByTypeOfSignal<T>();
                // 在场景中查找
                _instance ??= FindObjectOfType<Channel<T>>();
                // 如果还没找到，创建一个新的 GameObject
                if (_instance == null)
                {
                    var go = new GameObject(typeof(Channel<T>).Name);
                    _instance = go.AddComponent<Channel<T>>();
                    ChannelManager.AddChannel(_instance);
                }
                return _instance;
            }
            protected set => _instance = value;
        }

        void Awake()
        {
            if (_instance == null) _instance = this;
        }
        
        void LateUpdate()
        {
            bool currentEvents = _eventQueue.Any();
            // 清理公共上下文
            var queue = Detector.StaticContext[this];
            queue.Clear();
            while(_eventQueue.TryDequeue(out var signal))
                queue.Enqueue(signal);
            
            // 唤醒每个判别器工作
            if(currentEvents)
                foreach (var subscriber in subscribers)
                    AssignSignal(subscriber);
            
            Debug.Log($"[Channel] Called Detectors, count: {subscribers.Count}");
        }

        public void AddDelegate(T signal) => _eventQueue.Enqueue(signal);
        
        private void AssignSignal(Detector detector) => detector.CallDetect();

#if UNITY_EDITOR
        public void AddSubscriber(Detector subscriber) => subscribers.Add(subscriber);
        public void RemoveSubscriber(Detector subscriber) => subscribers.Remove(subscriber);
#endif
    }
}
