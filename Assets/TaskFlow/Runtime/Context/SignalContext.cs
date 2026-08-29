using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TaskFlow
{
    public struct SignalContextPath: IEquatable<SignalContextPath>
    {
        public IPorter Source;
        public Type SignalType;
        public SignalContextPath(IPorter source, Type signalType)
        {
            Source = source;
            SignalType = signalType;
        }

        public bool Equals(SignalContextPath other)
        {
            return other.Source == Source && other.SignalType == SignalType;
        }
    }
    
    public class SignalContext
    {
        private Dictionary<SignalContextPath, object> _context;
        
        public Queue<T> GetQueue<T>(IPorter source) where T : Signal
        {
            var key = new SignalContextPath(source, typeof(T));
            if (!_context.TryGetValue(key, out var box))
                _context[key] = box = new Queue<T>();
            return (Queue<T>)box;
        }

        public Queue<Signal> GetQueue(SignalContextPath path)
        {
            if (!_context.TryGetValue(path, out var box))
                _context[path] = box = new Queue<Signal>();
            return (Queue<Signal>)box;
        }

        public KeyValuePair<SignalContextPath, object>[] GetContextItemsByPorter(IPorter source)
        {
            var selected = _context.Where(p =>p.Key.Source == source).ToArray();
            return selected;
        }

        public void EnqueueSignal<T>(IPorter source, T signal) where  T : Signal
            => GetQueue<T>(source).Enqueue(signal);
        
        public void SetQueue<T>(IPorter source, Queue<T> signalQueue) where T : Signal
        => _context[new SignalContextPath(source, typeof(T))] = new Queue<T>(signalQueue);
        
        public void SetQueue<T>(IPorter source, ConcurrentQueue<T> signalQueue) where T : Signal
        => _context[new SignalContextPath(source, typeof(T))] = new Queue<T>(signalQueue);
        
        public void SetQueue(SignalContextPath path, Queue<Signal> signalQueue)
        =>  _context[path] = signalQueue;

        public void SetQueue(IPorter source, Type signalType, Queue<Signal> signalQueue)
        {
            var key = new SignalContextPath(source, signalType);
            _context[key] = signalQueue;
        }
        
        public void ClearQueue<T>(IPorter source) where T : Signal
            => GetQueue<T>(source).Clear();
    }
}