using System.Collections.Generic;
using System.Linq;
using TaskFlow.Detection;
using UnityEngine;

namespace TaskFlow
{
    [CreateAssetMenu(fileName = "New Head Detector", menuName = "TaskFlow/Head Detector")]
    public sealed class HeadDetector : Detector
    {
        /// 在判定前接收
        private SignalContext LocalContext { get; set; } = new();
        
        public override void Inject<T>(T context)
        {
            if(context is not SignalContext signalContext)
            {
                Debug.LogError($"context is not a {nameof(SignalContext)}");
                return;
            }
            foreach (var pair in _porters.Select(signalContext.GetItemsByPorter).SelectMany(selected => selected))
            {
                var queue = new Queue<Signal>(pair.Value);
                LocalContext.SetQueue(pair.Key.Source, pair.Key.SignalType, queue);
            }
        }
        
        /// Called After 'ScriptRunBehaviourLateUpdate'
        public override void Handle()
        {
            if (!RootDetection.Result(LocalContext)) return;
            var newContextPairs = RootDetection.GetContext();
            // 判定后发送给Receivers
            foreach (var receiver in _receivers)
            {
                if(!receiver.SelfActive) continue;
                // 注入上下文
                receiver.Inject(newContextPairs);
                // 唤醒处理
                receiver.Handle();
            }
            Called = false;
        }
    }
}