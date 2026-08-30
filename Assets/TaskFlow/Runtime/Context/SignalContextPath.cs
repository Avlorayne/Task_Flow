using System;

namespace TaskFlow
{
    public class SignalContextPath : IContextPath
    {
        public IPorter Source { get; }
        public Type SignalType { get; }

        public SignalContextPath(IPorter source, Type signalType)
        {
            Source = source;
            SignalType = signalType;
        }

        public bool Equals(IContextPath other) => other is SignalContextPath p && Equals(p);

        public bool Equals(SignalContextPath other) =>
            other != null && other.Source.Equals(Source) && other.SignalType == SignalType;

        public override bool Equals(object obj) => obj is SignalContextPath other && Equals(other);
        
        public override int GetHashCode() => HashCode.Combine(Source, SignalType);

        // 合并自 DetectionContextPath.GetPath
        public string GetPath() => $"{Source?.GetType().Name ?? "<null>"}.{SignalType.Name}";
        
    }

}