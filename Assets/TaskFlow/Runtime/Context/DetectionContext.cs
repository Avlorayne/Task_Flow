using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskFlow.Detection
{
    public struct DetectionContextPath : IEquatable<DetectionContextPath>
    {
        public IPorter Source;
        public Stack<Detection> DetectionPath;

        public DetectionContextPath(AtomicDetection atomicDetection)
        {
            Source = null;
            DetectionPath = new Stack<Detection>();
            DetectionPath.Push(atomicDetection);
        }

        public DetectionContextPath(IPorter source,  AtomicDetection detection)
        {
            Source = source;
            DetectionPath = new Stack<Detection>();
            DetectionPath.Push(detection);
        }

        public bool Equals(DetectionContextPath other)
            => Source.Equals(other.Source) && DetectionPath.Equals(other.DetectionPath);
    }
    
    public class DetectionContext
    {
        private Dictionary<DetectionContextPath, Signal> Items { get; set; } = new();

        public Signal this[DetectionContextPath path] => Items[path];
        
        public DetectionContext() { }
        
        public DetectionContext(KeyValuePair<DetectionContextPath, Signal>[] items)
        {
            Items.Clear();
            foreach (var item in items)
                Items.Add(item.Key, item.Value);    
        }

        public DetectionContext(List<KeyValuePair<DetectionContextPath, Signal>> items)
        {
            Items.Clear();
            foreach (var item in items)
                Items.Add(item.Key, item.Value);    
        }
        
        public DetectionContext(DetectionContext[] contexts)
        {
            foreach (var context in contexts)
                foreach (var item in context.Items)
                    Items.TryAdd(item.Key, item.Value);
        }

        public void AddPath(Detection detection)
        {
            foreach (var item in Items)
                item.Key.DetectionPath.Push(detection);
        }
    }
}