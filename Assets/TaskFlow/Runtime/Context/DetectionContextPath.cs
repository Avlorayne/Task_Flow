using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using TaskFlow.Detection;

namespace TaskFlow
{
   public class DetectionContextPath : IContextPath
    {
        public IPorter Source { get; }
        public readonly Stack<Detection.Detection> DetectionPath;
        public FieldPath Slot;

        public DetectionContextPath(AtomicDetection atomicDetection, FieldPath slot)
        {
            Source = null;
            DetectionPath = new Stack<Detection.Detection>();
            DetectionPath.Push(atomicDetection);
            Slot = slot;
        }

        public DetectionContextPath(IPorter source, AtomicDetection detection, FieldPath slot)
        {
            Source = source;
            DetectionPath = new Stack<Detection.Detection>(); 
            DetectionPath.Push(detection);
            Slot = slot;
        }

        public bool Equals(IContextPath other) => other is DetectionContextPath p && Equals(p);

        public bool Equals([CanBeNull] DetectionContextPath other)
        {
            if (other == null) return false;
            if (!Equals(Source, other.Source)) return false;      // Source 可为 null，防 NRE
            if (!Equals(Slot, other.Slot)) return false;

            var thisPath = DetectionPath.ToArray();
            var otherPath = other.DetectionPath.ToArray();
            if (thisPath.Length != otherPath.Length) return false; // 补充长度校验

            for (int i = 0; i < thisPath.Length; i++)
                if (!thisPath[i].Equals(otherPath[i])) return false;
            return true;
        }
        
        public override bool Equals(object obj) => obj is DetectionContextPath other && Equals(other);

        // 哈希不再包含 DetectionPath：AddPath 会原地修改它，
        // 若计入哈希，键在 Dictionary 中会"失踪"。
        // 哈希只需保证"相等者哈希相等"，路径内容比较交给 Equals。
        public override int GetHashCode() => HashCode.Combine(Source, Slot);

        public string GetPath()
        {
            if (DetectionPath == null) return string.Empty;
            var pathStack = new Stack<Detection.Detection>(DetectionPath.ToArray());
            var path = new StringBuilder();
            while (pathStack.TryPop(out var item))
                path.Append($"{item.GetType().Name}.");
            return path.ToString();
        }
    }
}