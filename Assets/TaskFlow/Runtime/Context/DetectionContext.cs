using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace TaskFlow.Detection
{
    public class DetectionContext : IContext<DetectionContextPath, Signal>, IContextReader
    {
        /// <summary>
        /// - Key : Path
        /// - Value : Captured Signal
        /// </summary>
        private readonly Dictionary<DetectionContextPath, Signal> _items = new ();

        public DetectionContext() { }

        /*
        public DetectionContext(KeyValuePair<DetectionContextPath, Signal>[] items)
            : this((IEnumerable<KeyValuePair<DetectionContextPath, Signal>>)items) { }

        public DetectionContext(List<KeyValuePair<DetectionContextPath, Signal>> items)
            : this((IEnumerable<KeyValuePair<DetectionContextPath, Signal>>)items) { }
            */

        public DetectionContext(IEnumerable<KeyValuePair<DetectionContextPath, Signal>> items)
        {
            if (items == null) return;
            foreach (var item in items)
                _items[item.Key] = item.Value;   // 修复：Dictionary 没有 TryAdd
        }

        public DetectionContext(DetectionContext[] contexts)
        {
            if (contexts == null) return;
            foreach (var context in contexts)
                foreach (var item in context._items)
                    _items[item.Key] = item.Value;
        }

        // ---------- IContext 实现 ----------

        public bool TryGet(DetectionContextPath path, out Signal value)
            => _items.TryGetValue(path, out value);

        public Signal GetOrCreate(DetectionContextPath path)
        {
            if (!_items.TryGetValue(path, out var value))
                _items[path] = value = null;     // Signal 为引用类型
            return value;
        }

        public void Set(DetectionContextPath path, Signal value)
            => _items[path] = value;

        public void Clear(DetectionContextPath path)
            => _items.Remove(path);

        public KeyValuePair<DetectionContextPath, Signal>[] GetItemsByPorter(IPorter source)
            => _items.Where(p => Equals(p.Key.Source, source)).ToArray();
        
        public bool TryReadValue(IContextPath path, string fieldName, out object value)
        {
            value = null;
            if (path is not DetectionContextPath p) return false;
            if (!TryGet(p, out var signal) || signal == null) return false;
            value = signal.GetField(fieldName);
            return true;
        }

        // ---------- 类特有 ----------

        /// <summary>保留原索引器语义：不存在时抛 KeyNotFoundException</summary>
        public Signal this[DetectionContextPath path] => _items[path];

        public void AddPath(Detection detection)
        {
            foreach (var key in _items.Keys.ToArray())  // 先快照，避免遍历中键内容变化
                key.DetectionPath.Push(detection);
        }
        
        /// <summary>克隆自身的副本</summary>
        public DetectionContext CloneSelf() 
            => new(_items);
        
    }
}