using System.Collections.Generic;
using TaskFlow.Detection;
using UnityEngine;

namespace TaskFlow
{
    
    public class Detector : ScriptableObject, IPorter
    {
        [Header("订阅关系")]
        [SerializeField] private List<Detector> Subscribers = new();
        [SerializeField] private IHandler Handler;
        
        /// 公共静态上下文，只在第一层Detector使用。
        public static readonly DetectionContext StaticContext = new();
        /// 本地上下文，第一层和往后都需要使用的，只筛选对自己有效的上下文信息。
        /// 在判定时筛选
        private Queue<Signal> LocalContext { get; set; } = new();
        
        public Detection.Detection RootDetection;

        public bool Valid { get; private set; } = true;
        public bool SelfActive { get; private set; } = true;
        public bool Called { get; private set; } = false;

        public void CallDetect() => Called = true;
        
        /// Called at the End of Frame
        public void HeadDetect()
        {
            DetectImmediately();
            Called = false;
        }
        
        private void DetectImmediately()
        {
            if (RootDetection is not { Result: true }) return;
            // 判定后发送给subscribers
            if (Subscribers.Count > 0)
                foreach (var detector in Subscribers)
                {
                    if(!detector.SelfActive) continue;
                    // 注入上下文
                
                    // 唤醒处理
                    detector.DetectImmediately();
                }
            
            // 判定后发送给IHandler
            if (Handler != null)
            {
                // 注入上下文
                
                // 唤醒处理
                Handler.Handle(LocalContext);
            } 
        }
        
#if UNITY_EDITOR
        public void AddSubscriber(Detector subscriber) => Subscribers.Add(subscriber);
        
        public void RemoveSubscriber(Detector subscriber) => Subscribers.Remove(subscriber);
#endif
    }
}
