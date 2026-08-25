using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow
{
    public class Detector : MonoBehaviour, IPorter
    {
        [SerializeField] private List<Detector> Subscribers = new();

        [SerializeField] private IHandler Handler;

        public bool SelfActive { get; private set; } = true;
        
        /// 公共静态上下文，只在第一层Detector使用。
        public static readonly DetectionContext StaticContext = new();
        
        /// 本地上下文，第一层和往后都需要使用的，只筛选对自己有效的上下文信息。
        /// 在判定时筛选
        public Queue<Signal> LocalContext { get; private set; } = new();
        
        public Detection RootDetection;

        private Coroutine _waitCoroutine;

        public void StartHeadDetect()
        {
            // 添加显示判别，如果已经被call过了，就不再重复调用
            if (_waitCoroutine != null) return;
            
            _waitCoroutine = StartCoroutine(HeadDetect());
            return;

            IEnumerator HeadDetect()
            {
                yield return new WaitForEndOfFrame();
                Detect();
                _waitCoroutine = null;
            }
        }
        
        private void Detect()
        {
            if (RootDetection is not { Result: true }) return;
            // 判定后发送给subscribers
            if (Subscribers.Count > 0)
                foreach (var detector in Subscribers)
                {
                    if(!detector.SelfActive) continue;
                    // 注入上下文
                
                    // 唤醒处理
                    detector.Detect();
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
