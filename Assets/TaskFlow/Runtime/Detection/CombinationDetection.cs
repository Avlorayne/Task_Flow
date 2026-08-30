using System.Collections.Generic;
using System.Linq;

namespace TaskFlow.Detection
{
    public abstract class CombinationDetection : Detection
    {
        /// 序列化注入
        public List<Detection> SubDetections = new ();

        public override DetectionContext GetContext()
        {
            var contextList = new List<DetectionContext>();
            foreach (var subDetection in SubDetections)
                contextList.Add(subDetection.GetContext());
            
            return new DetectionContext(contextList.ToArray());
        }
    }

    /// 组合判定：与运算
    public class And : CombinationDetection
    {
        public override bool Result(IContextReader contextReader) => SubDetections.Any() && SubDetections.All(subDetection => subDetection.Result(contextReader));
    }

    /// 组合判定：或运算
    public class Or : CombinationDetection
    {
        public override bool Result(IContextReader contextReader) => SubDetections.Any() && SubDetections.Any(subDetection => subDetection.Result(contextReader));
    }
    
    /// 组合判定：非运算
    public class Not : CombinationDetection
    {
        private Detection SubDetection => SubDetections.First();
        public override bool Result(IContextReader contextReader) => SubDetection is not null && !SubDetection.Result(contextReader);
    }
}