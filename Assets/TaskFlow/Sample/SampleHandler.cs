using TaskFlow.Detection;
using UnityEngine;

namespace TaskFlow.Sample
{
    public class SampleHandler : MonoBehaviour, IHandler
    {
        public bool SelfActive { get; }
        public void Inject(SignalContext context)
        {
            throw new System.NotImplementedException();
        }

        public void Handle()
        {
            throw new System.NotImplementedException();
        }

        public void AddSubscriber(IPorter porter)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveSubscriber(IPorter porter)
        {
            throw new System.NotImplementedException();
        }
    }
}