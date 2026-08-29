using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TaskFlow.Detection
{
    public abstract class CombinationDetection : Detection
    {
        /// 序列化注入
        public List<Detection> SubDetections = new ();

        public override KeyValuePair<DetectionContextPath, Signal>[] GetContext()
        {
            var context = SubDetections.Select(d => d.GetContext()).SelectMany(p => p).ToArray();
            foreach (var pair in context)
            {
                var path = pair.Key;
                path.DetectionPath.Push(this);
            }
            return context.Select(pair => pair).ToArray();
        }
    }

    /// 组合判定：与运算
    public class And : CombinationDetection
    {
        public override bool Result(DetectionContext context) => SubDetections.Any() && SubDetections.All(subDetection => subDetection.Result(context));
        public override bool Result(SignalContext context) => SubDetections.Any() && SubDetections.All(subDetection => subDetection.Result(context));
    }

    /// 组合判定：或运算
    public class Or : CombinationDetection
    {
        public override bool Result(DetectionContext context) => SubDetections.Any() && SubDetections.Any(subDetection => subDetection.Result(context));
        public override bool Result(SignalContext context) => SubDetections.Any() && SubDetections.Any(subDetection => subDetection.Result(context));
        
    }
    
    /// 组合判定：非运算
    public class Not : CombinationDetection
    {
        private Detection SubDetection => SubDetections.First();
        public override bool Result(DetectionContext context) => SubDetection is not null && !SubDetection.Result(context);
        public override bool Result(SignalContext context) => SubDetection is not null && !SubDetection.Result(context);
    }
}