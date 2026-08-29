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
        
        public void Inject(SignalContext context)
        {
            foreach (var pair in _porters.Select(context.GetContextItemsByPorter)
                         .SelectMany(selected => selected))
                LocalContext.SetQueue(pair.Key, (Queue<Signal>)pair.Value);
        }
        
        public override void Inject(DetectionContext context) { }
        
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
                receiver.Inject(new DetectionContext(newContextPairs));
                // 唤醒处理
                receiver.Handle();
            }
            Called = false;
        }
    }
}