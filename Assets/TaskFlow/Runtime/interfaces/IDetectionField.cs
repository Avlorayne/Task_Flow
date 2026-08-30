using System;
using JetBrains.Annotations;
using TaskFlow.Detection;

namespace TaskFlow
{
    public interface IDetectionField { }

    [Serializable]
    public struct CustomField : IDetectionField
    {
        public CustomField(object value) { Value = value; }
        public readonly object Value;
    }
    
    [Serializable]
    public class FieldPath : IDetectionField, IEquatable<FieldPath>
    {
        public enum PathType { DetectionContextPath, SignalContextPath }

        public string fieldName;

        // 统一后的唯一路径：运行时类型即判别器（取代 pathType + 两个可能互相矛盾的路径字段）
        public IContextPath Path;

        // 兼容读取：不再是可写状态（外部误赋值会编译报错，这正是要暴露的）
        public PathType pathType => Path switch
        {
            DetectionContextPath => PathType.DetectionContextPath,
            SignalContextPath    => PathType.SignalContextPath,
            _ => throw new InvalidOperationException("FieldPath 未设置路径")
        };

        public FieldPath(IPorter source, AtomicDetection detection, FieldPath slot, string fieldName)
            : this(new DetectionContextPath(source, detection, slot), fieldName) { }

        public FieldPath(DetectionContextPath path, string fieldName)
        {
            Path = path;
            this.fieldName = fieldName;
        }

        public FieldPath(SignalContextPath path, string fieldName)
        {
            Path = path;
            this.fieldName = fieldName;
        }

        public FieldPath(IPorter source, Type signalType, string fieldName)
            : this(new SignalContextPath(source, signalType), fieldName) { }

        public bool Equals([CanBeNull] FieldPath other)
        {
            if (other is null) return false;
            if (!string.Equals(fieldName, other.fieldName)) return false;
            return Path switch
            {
                null => other.Path is null,
                DetectionContextPath d => other.Path is DetectionContextPath od && d.Equals(od),
                SignalContextPath s    => other.Path is SignalContextPath os && s.Equals(os),
                _ => Path.Equals(other.Path)
            };
        }

        public override bool Equals(object obj) => obj is FieldPath other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(fieldName, Path);
    }
}