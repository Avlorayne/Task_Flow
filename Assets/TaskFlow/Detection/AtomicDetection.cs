using System.Collections.Concurrent;
using System.Linq;
using TMPro;
using UnityEngine.UI;

namespace TaskFlow
{
    public interface IDetectionProperty {}
    public struct PropertyPath : IDetectionProperty
    {
        public PropertyPath(Porter port, string name)
        {
            SourcePort = port;
            PropertyName = name;
        }
        
        public Porter SourcePort;
        public string PropertyName;
    }

    public struct CustomProperty : IDetectionProperty
    {
        public CustomProperty(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    public abstract class AtomicDetection : Detection
    {
        protected string RoutePropertyValue(IDetectionProperty property)
        {
            return property switch
            {
                PropertyPath propertyPath => Context.GetProperty(propertyPath, 0)?.Trim(),
                CustomProperty customProperty => customProperty.Value.Trim(),
                _ => string.Empty
            };
        }
    }

    public class Equal : AtomicDetection
    {
        public IDetectionProperty Property0;
        public IDetectionProperty Property1;
        public bool NotEqual0_Equal1;
        public override bool Result
        {
            get
            {
                var value0 = RoutePropertyValue(Property0);
                var value1 = RoutePropertyValue(Property1);

                if (NotEqual0_Equal1)
                    return value0 == value1;
                else
                    return value0 != value1;
            }
        }
    }

    public class Compare : AtomicDetection
    {
        public IDetectionProperty Property0;
        public IDetectionProperty Property1;
        public float Tolerance;
        public bool LessThan0_GreaterThan1;
        public bool UseEqual;
        public override bool Result
        {
            get
            {
                var numberString0 = RoutePropertyValue(Property0);
                var numberString1 = RoutePropertyValue(Property1);
                var value0 = float.Parse(numberString0);
                var value1 = float.Parse(numberString1);
                return (LessThan0_GreaterThan1, UseEqual) switch
                {
                    (true, true) => value0 >= value1 + Tolerance,
                    (true, false) => value0 > value1 + Tolerance,
                    (false, true) => value0 <= value1 + Tolerance,
                    (false, false) => value0 < value1 + Tolerance
                };
            }
        }
    }

    public class Contain : AtomicDetection
    {
        public IDetectionProperty Property;
        public string[] Collection;
        public bool NotContain0_Contain1;

        public override bool Result
        {
            get
            {
                var value =  RoutePropertyValue(Property);
                if(NotContain0_Contain1)
                    return Collection.Contains(value);
                else
                    return !Collection.Contains(value);
            }
        }
    }
}