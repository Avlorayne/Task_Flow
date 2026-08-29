using System;
using System.Collections.Generic;
using TaskFlow.Detection;
using UnityEngine;
using UnityEngine.Events;

namespace TaskFlow
{
    [CreateAssetMenu(fileName = "New Detector", menuName = "TaskFlow/Detector")]
    public sealed class Detector : ScriptableObject, IPorter, IReceiver
    {
        [Header("订阅关系")]
        [SerializeField] private HashSet<IPorter> _porters = new ();
        [SerializeField] private HashSet<IReceiver> _receivers = new();
        
        /// 在判定时筛选
        private SignalContext LocalContext { get; set; } = new();
        
        public Detection.Detection RootDetection;

        public UnityAction OnSignal { get; set; }
        public bool Valid { get; private set; } = true;
        public bool SelfActive { get; private set; } = true;
        public bool Called { get; private set; } = false;

        private void CallDetect() => Called = true;

        public void Inject(SignalContext context)
        {
            foreach (var porter in _porters)
                LocalContext.ReplaceQueue(porter, context[porter]);
        }
        
        /// Called at the End of Frame
        public void Handle()
        {
            if (!RootDetection.Result()) return;
            // TODO: 判定完成后更新本地上下文0
            // 判定后发送给Receivers
            foreach (var receiver in _receivers)
            {
                if(!receiver.SelfActive) continue;
                // 注入上下文
                receiver.Inject(LocalContext);
                // 唤醒处理
                receiver.Handle();
            }
            Called = false;
        }

        void OnEnable()
        {
            foreach (var porter in _porters)
                porter.OnSignal += CallDetect;
        }

        void OnDisable()
        {
            foreach (var porter in _porters)
                porter.OnSignal -= CallDetect;
        }


#if UNITY_EDITOR
        public void AddSubscriber(IPorter porter)
        {
            porter.OnSignal += CallDetect;
            _porters.Add(porter);
        }

        public void RemoveSubscriber(IPorter porter)
        {
            porter.OnSignal -= CallDetect;
            _porters.Remove(porter);
        }
#endif
    }
}
