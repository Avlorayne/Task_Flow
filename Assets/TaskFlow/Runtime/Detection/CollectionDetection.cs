using System.Collections.Generic;
using System.Linq;

namespace TaskFlow.Detection
{
    public abstract class CollectionDetection : AtomicDetection
    {
        public IDetectionField Field;
        public object[] Collection;

        protected CollectionDetection(IDetectionField field, object[] collection)
        {
            Field = field;
            Collection = collection;
        }

        public override DetectionContext GetContext()
        {
            var pairs = new List<KeyValuePair<DetectionContextPath, Signal>>();
            CollectFieldPath(Field, pairs);
            return new DetectionContext(pairs);
        }
    }
    
    public class Contain : CollectionDetection
    {
        public Contain(IDetectionField field, object[] collection) : base(field, collection) { }

        protected override bool Evaluate()
        {
            var value = RoutePropertyValue(Field);
            return Collection.Contains(value);
        }
    }

    public class NotContain : CollectionDetection
    {
        public NotContain(IDetectionField field, object[] collection) : base(field, collection) { }

        protected override bool Evaluate()
        {
            var value = RoutePropertyValue(Field);
            return !Collection.Contains(value);
        }
    }
}