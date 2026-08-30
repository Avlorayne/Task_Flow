using System.Collections.Generic;

namespace TaskFlow.Detection
{
    public abstract class BinaryFieldDetection : AtomicDetection
    {
        public IDetectionField Field0;
        public IDetectionField Field1;

        protected BinaryFieldDetection(IDetectionField field0, IDetectionField field1)
        {
            Field0 = field0;
            Field1 = field1;
        }

        public override DetectionContext GetContext()
        {
            var pairs = new List<KeyValuePair<DetectionContextPath, Signal>>();
            CollectFieldPath(Field0, pairs);
            CollectFieldPath(Field1, pairs);
            return new DetectionContext(pairs);
        }
    }
    
    public class Equal : BinaryFieldDetection
    {
        public Equal(IDetectionField field0, IDetectionField field1) : base(field0, field1) { }

        protected override bool Evaluate()
        {
            var value0 = RoutePropertyValue(Field0);
            var value1 = RoutePropertyValue(Field1);
            return Equals(value0, value1);
        }
    }

    public class NotEqual : BinaryFieldDetection
    {
        public NotEqual(IDetectionField field0, IDetectionField field1) : base(field0, field1) { }

        protected override bool Evaluate()
        {
            var value0 = RoutePropertyValue(Field0);
            var value1 = RoutePropertyValue(Field1);
            return !Equals(value0, value1);
        }
    }
}