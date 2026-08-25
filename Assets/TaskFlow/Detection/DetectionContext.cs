using System.Collections.Generic;

namespace TaskFlow
{
    public class DetectionContext
    {
        private Dictionary<IPorter, Queue<Signal>> _context;

        public Queue<Signal> this[IPorter source] => _context.GetValueOrDefault(source);

        public string DequeueProperty(PropertyPath path)
        {
            if (_context.TryGetValue(path.SourcePort, out var queue))
            {
                var signal = queue.Dequeue();
                // if (signal.PropertyHeader.TryGetValue(path.PropertyName, out var value))
                //     return value;
            }

            return null;
        }

        public void EnqueueSignal(IPorter source, Signal signal)
        {
            if (_context.TryGetValue(source, out var queue))
            {
                queue.Enqueue(signal);
            }
            else
            {
                var newList = new Queue<Signal>();
                newList.Enqueue(signal);
                _context.Add(source, newList);
            }
        }
    }
}