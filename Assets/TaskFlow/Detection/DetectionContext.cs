using System.Collections.Generic;

namespace TaskFlow
{
    public class DetectionContext
    {
        public Dictionary<Porter, List<Signal>> context;

        public string GetProperty(PropertyPath path, int index)
        {
            if (context.TryGetValue(path.SourcePort, out var list))
            {
                var signal = list[index];
                if (signal.PropertyHeader.TryGetValue(path.PropertyName, out var value))
                    return value;
            }

            return null;
        }
    }
}