using System.Collections.Concurrent;
using System.Linq;

namespace TaskFlow.Detection
{
    public abstract class CombinationDetection : Detection
    {
        /// 序列化注入
        public ConcurrentBag<Detection> SubDetections = new ();
    }

    /// 组合判定：与运算
    public class And : CombinationDetection
    {
        public override bool Result 
            => !SubDetections.IsEmpty && SubDetections.All(subDetection => subDetection.Result);
    }

    /// 组合判定：或运算
    public class Or : CombinationDetection
    {
        public override bool Result 
            => !SubDetections.IsEmpty && SubDetections.Any(subDetection => subDetection.Result);
    }
    
    /// 组合判定：非运算
    public class Not : CombinationDetection
    {
        private Detection SubDetection => SubDetections.FirstOrDefault();
        public override bool Result =>!SubDetection.Result;
    }
}