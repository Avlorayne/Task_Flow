using System.Collections.Generic;

namespace TaskFlow
{
    public class Channel : Porter
    {
        public readonly Dictionary<string, object> ProtocolParams = new Dictionary<string, object>();
        
        public override void Raise(Signal signal)
        {
            OnRaised.Invoke(signal);
        }
        
        public bool ProtocolHashError(Signal signal)
        {
            return false;
        }
    }
}