using UnityEngine.Events;

namespace TaskFlow
{
    public interface IPorter
    {
        public UnityAction OnSignal {get; set;}
    }

    
    
    
}