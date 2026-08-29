using TaskFlow.Detection;
using UnityEngine.Events;

namespace TaskFlow
{
    public interface IPorter
    {
        public UnityAction OnSignal {get; set;}
    }

    public interface IReceiver
    {
        bool SelfActive { get; }
        void Inject(SignalContext  context);
        void Handle();
        void AddSubscriber(IPorter porter);
        void RemoveSubscriber(IPorter porter);
    }
    
    public interface IHandler : IReceiver { }
}