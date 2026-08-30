using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskFlow
{
    public class SignalContext : IContext<SignalContextPath, Queue<Signal>>, IContextReader
    {
        // 修复：原字段未初始化，首次访问会 NRE
        private readonly Dictionary<SignalContextPath, Queue<Signal>> _context = new ();
        private readonly Dictionary<IPorter, KeyValuePair<SignalContextPath, Queue<Signal>>[]> _porterLookup = new ();

        // ---------- IContext 实现 ----------
        public bool TryGet(SignalContextPath path, out Queue<Signal> value)
            => _context.TryGetValue(path, out value);

        public Queue<Signal> GetOrCreate(SignalContextPath path)
        {
            if (!_context.TryGetValue(path, out var box))
            {
                _context[path] = box = new Queue<Signal>();
                UpdatePorterLookup(path, box);
            }
            return box;
        }

        public void Set(SignalContextPath path, Queue<Signal> value)
        {
            Clear(path);
            _context[path] = value;
            UpdatePorterLookup(path, value);
        }

        public void Clear(SignalContextPath path)
        {
            if (_context.TryGetValue(path, out var queue))
                queue.Clear();
        }

        public KeyValuePair<SignalContextPath, Queue<Signal>>[] GetItemsByPorter(IPorter source)
            => _porterLookup[source];
        
        public bool TryReadValue(IContextPath path, string fieldName, out object value)
        {
            value = null;
            if (path is not SignalContextPath p) return false;
            if (!TryGet(p, out var queue) || queue == null || queue.Count == 0) return false;
            value = queue.Dequeue();   // 读取即消费，沿用原语义；想改"观察"语义换 Peek()
            return true;
        }

        // ---------- 类特有的便捷重载（内部统一走 IContext 方法）----------
        private void UpdatePorterLookup(SignalContextPath path, Queue<Signal> newQueue)
        {
            var source = path.Source;
            var originalQueue = _context[path];
            
            var queues = _porterLookup[source];
            var list = new List<KeyValuePair<SignalContextPath, Queue<Signal>>>(queues);

            foreach (var pair in list.ToList())
            {
                if(pair.Key.Equals((IContextPath)path) && pair.Value == originalQueue)
                    list.Remove(pair);
            }
            list.Add(new KeyValuePair<SignalContextPath, Queue<Signal>>(path, newQueue));
            
            _porterLookup[source] = list.ToArray();
        }
        
        public Queue<Signal> GetQueue<T>(IPorter source) where T : Signal
            => GetOrCreate(new SignalContextPath(source, typeof(T)));

        public void EnqueueSignal<T>(IPorter source, T signal) where T : Signal
            => GetQueue<T>(source).Enqueue(signal);

        public void SetQueue<T>(IPorter source, Queue<T> signalQueue) where T : Signal
            => Set(new SignalContextPath(source, typeof(T)), new Queue<Signal>(signalQueue));

        // 语义统一：与其他 SetQueue 一致，先清后写
        public void SetQueue(IPorter source, Type signalType, Queue<Signal> signalQueue)
            => Set(new SignalContextPath(source, signalType), signalQueue);
    }
}