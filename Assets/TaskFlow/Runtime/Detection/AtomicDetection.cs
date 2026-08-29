using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TaskFlow.Detection
{
    public interface IDetectionField {}
    
    [Serializable]
    public struct FieldPath : IDetectionField
    {
        public enum PathType
        {
            DetectionContextPath,
            SignalContextPath
        }

        public PathType pathType;
        public DetectionContextPath DetectionContextPath;
        public SignalContextPath SignalContextPath;
        public string FieldName;
        
        public FieldPath(IPorter source, AtomicDetection detection, string fieldName)
        {
            DetectionContextPath = new DetectionContextPath(source, detection);
            FieldName = fieldName;
            SignalContextPath = default;
            pathType = PathType.DetectionContextPath;
        }
        
        public FieldPath(DetectionContextPath path, string fieldName)
        {
            DetectionContextPath = path;
            FieldName = fieldName;
            SignalContextPath = default;
            pathType = PathType.DetectionContextPath;
        }

        public FieldPath(SignalContextPath path, string fieldName)
        {
            SignalContextPath = path;
            FieldName = fieldName;
            pathType = PathType.SignalContextPath;
            DetectionContextPath = default;
        }

        public FieldPath(IPorter source, Type signaType, string fieldName)
        {
            SignalContextPath = new SignalContextPath(source, signaType);
            FieldName = fieldName;
            pathType = PathType.SignalContextPath;
            DetectionContextPath = default;
        }
    }

    [Serializable]
    public struct CustomField : IDetectionField
    {
        public CustomField(object value)
        {
            Value = value;
        }

        public readonly object Value;
    }

    public abstract class AtomicDetection : Detection
    {
        protected object RoutePropertyValueFromDetectionContext(IDetectionField field)
        {
            return field switch
            {
                FieldPath fieldPath => DetectionContext[fieldPath.DetectionContextPath].GetField(fieldPath.FieldName),
                CustomField customField => customField.Value,
                _ => string.Empty
            };
        }

        protected object RoutePropertyValueFromSignalContext(IDetectionField field)
        {
            return field switch
            {
                FieldPath fieldPath => SignalContext.GetQueue(fieldPath.SignalContextPath).Dequeue(),
                CustomField customField => customField.Value,
                _ => string.Empty
            };
        }
    }

    public class Equal : AtomicDetection
    {
        public IDetectionField Field0;
        public IDetectionField Field1;
        public bool NotEqual0_Equal1;

        public override bool Result(DetectionContext context)
        {
            var value0 = RoutePropertyValueFromDetectionContext(Field0);
            var value1 = RoutePropertyValueFromDetectionContext(Field1);

            if (NotEqual0_Equal1)
                return value0 == value1;
            else
                return value0 != value1;
        }

        public override bool Result(SignalContext context)
        {
            var value0 = RoutePropertyValueFromSignalContext(Field0);
            var value1 = RoutePropertyValueFromSignalContext(Field1);

            if (NotEqual0_Equal1)
                return value0 == value1;
            else
                return value0 != value1;
        }

        public override KeyValuePair<DetectionContextPath, Signal>[] GetContext()
        {
            List<KeyValuePair<DetectionContextPath, Signal>> pairs = new();
            if (Field0 is FieldPath propertyPath)
            {
                var path = propertyPath.DetectionContextPath;
                pairs.Add(new KeyValuePair<DetectionContextPath, Signal>(path, DetectionContext[path]));
            }

            if (Field1 is FieldPath property1)
            {
                var path = property1.DetectionContextPath;
                pairs.Add(new KeyValuePair<DetectionContextPath, Signal>(path, DetectionContext[path]));
            }
            
            return pairs.ToArray();
        }
    }

    public class Compare : AtomicDetection
    {
        public IDetectionField Field0;
        public IDetectionField Field1;
        public float Tolerance;
        public bool LessThan0_GreaterThan1;
        public bool UseEqual;

        public override bool Result(DetectionContext context)
        {
            var value0 = (float)RoutePropertyValueFromDetectionContext(Field0);
            var value1 = (float)RoutePropertyValueFromDetectionContext(Field1);
            return (LessThan0_GreaterThan1, UseEqual) switch
            {
                (true, true) => value0 >= value1 + Tolerance,
                (true, false) => value0 > value1 + Tolerance,
                (false, true) => value0 <= value1 + Tolerance,
                (false, false) => value0 < value1 + Tolerance
            };
        }

        public override bool Result(SignalContext context)
        {
            var value0 = (float)RoutePropertyValueFromSignalContext(Field0);
            var value1 = (float)RoutePropertyValueFromSignalContext(Field1);
            return (LessThan0_GreaterThan1, UseEqual) switch
            {
                (true, true) => value0 >= value1 + Tolerance,
                (true, false) => value0 > value1 + Tolerance,
                (false, true) => value0 <= value1 + Tolerance,
                (false, false) => value0 < value1 + Tolerance
            };
        }

        public override KeyValuePair<DetectionContextPath, Signal>[] GetContext()
        {
            List<KeyValuePair<DetectionContextPath, Signal>> pairs = new();
            if (Field0 is FieldPath propertyPath)
            {
                var path = propertyPath.DetectionContextPath;
                pairs.Add(new KeyValuePair<DetectionContextPath, Signal>(path, DetectionContext[path]));
            }

            if (Field1 is FieldPath property1)
            {
                var path = property1.DetectionContextPath;
                pairs.Add(new KeyValuePair<DetectionContextPath, Signal>(path, DetectionContext[path]));
            }
            
            return pairs.ToArray();
        }
    }

    public class Contain : AtomicDetection
    {
        public IDetectionField Field;
        public object[] Collection;
        public bool NotContain0_Contain1;

        public override bool Result(DetectionContext context)
        {
            var value = RoutePropertyValueFromDetectionContext(Field);
            if (NotContain0_Contain1)
                return Collection.Contains(value);
            else
                return !Collection.Contains(value);
        }

        public override bool Result(SignalContext context)
        {
            var value = RoutePropertyValueFromSignalContext(Field);
            if (NotContain0_Contain1)
                return Collection.Contains(value);
            else
                return !Collection.Contains(value);
        }

        public override KeyValuePair<DetectionContextPath, Signal>[] GetContext()
        {
            List<KeyValuePair<DetectionContextPath, Signal>> pairs = new();
            if (Field is FieldPath fieldPath)
            {
                var path = fieldPath.DetectionContextPath;
                pairs.Add(new KeyValuePair<DetectionContextPath, Signal>(new DetectionContextPath(this), DetectionContext[path]));
            }
            return pairs.ToArray();
        }
    }
}
