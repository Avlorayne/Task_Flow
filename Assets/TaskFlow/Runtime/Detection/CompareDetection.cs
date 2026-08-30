namespace TaskFlow.Detection
{
    public abstract class CompareDetection : BinaryFieldDetection
    {
        public bool IsEqualMode;
        public float Tolerance;

        protected CompareDetection(IDetectionField field0, IDetectionField field1, bool isEqualMode, float tolerance)
            : base(field0, field1)
        {
            IsEqualMode = isEqualMode;
            Tolerance = tolerance;
        }
    }
    
    public class LessThan : CompareDetection
    {
        public LessThan(IDetectionField field0, IDetectionField field1, bool isEqualMode, float tolerance)
            : base(field0, field1, isEqualMode, tolerance) { }

        protected override bool Evaluate()
        {
            var value0 = ToFloat(RoutePropertyValue(Field0));
            var value1 = ToFloat(RoutePropertyValue(Field1));
            return IsEqualMode ? value0 <= value1 + Tolerance : value0 < value1 - Tolerance;
        }
    }

    public class GreaterThan : CompareDetection
    {
        public GreaterThan(IDetectionField field0, IDetectionField field1, bool isEqualMode, float tolerance)
            : base(field0, field1, isEqualMode, tolerance) { }

        protected override bool Evaluate()
        {
            var value0 = ToFloat(RoutePropertyValue(Field0));
            var value1 = ToFloat(RoutePropertyValue(Field1));
            return IsEqualMode ? value0 >= value1 - Tolerance : value0 > value1 + Tolerance;
        }
    }
}