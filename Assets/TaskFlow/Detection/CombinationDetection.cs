using System.Collections.Concurrent;
using System.Linq;

namespace TaskFlow
{
    public abstract class CombinationDetection : Detection
    {
        public ConcurrentBag<Detection> SubDetections;
    }

    public class And : CombinationDetection
    {
        public override bool Result
        {
            get
            {
                if (SubDetections.IsEmpty)
                    return false;

                return SubDetections.All(subDetection => subDetection.Result);
            }
        }
    }

    public class Or : CombinationDetection
    {
        public override bool Result
        {
            get {
                if (SubDetections.IsEmpty)
                    return false;

                return SubDetections.Any(subDetection => subDetection.Result);
            }
        }
    }

    public class Not : CombinationDetection
    {
        private Detection SubDetection => SubDetections.FirstOrDefault();
        public override bool Result =>!SubDetection.Result;
    }
}