using TaskFlow.Detection;
using UnityEngine;

namespace TaskFlow
{
    [CreateAssetMenu(fileName = "Body Detector", menuName = "TaskFlow/Body Detector")]
    public sealed class BodyDetector : Detector
    {
        /// 在判定前接收
        private DetectionContext LocalContext { get; set; } = new();
        
        public override void Inject<T>(T context)
        {
            if (context is not DetectionContext detectionContext)
            {
                Debug.LogError($"context is not a {nameof(DetectionContext)}");
                return;
            }

            LocalContext = detectionContext.CloneSelf();
        }

        /// Called by the Previous
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