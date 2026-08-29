using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TaskFlow.Detection
{
    public class SignalContext
    {
        private Dictionary<IPorter, Queue<Signal>> _context;

        public Queue<Signal> this[IPorter source] => _context.GetValueOrDefault(source);

        public void EnqueueSignal(IPorter source, Signal signal)
        {
            if (!_context.TryGetValue(source, out var queue))
            {
                queue = new Queue<Signal>();
                _context.Add(source, queue);
            }
            queue.Enqueue(signal);
        }

        public void ReplaceQueue(IPorter source, Queue<Signal> signalQueue)
        {
            if(_context.TryGetValue(source, out var queue))
            {
                queue.Clear();
                _context[source] = signalQueue;
            }
            else
            {
                _context.Add(source, signalQueue);
            }
        }

        public void ClearQueue(IPorter source)
        {
            if (_context.TryGetValue(source, out var queue))
                queue.Clear();
        }
    }
}