using TaskFlow.Detection;

namespace TaskFlow
{
    public interface IReceiver
    {
        bool SelfActive { get; }
        void Inject<T>(T context) where T : IContextReader;
        void Handle();
#if UNITY_EDITOR
        void AddSubscriber(IPorter porter);
        void RemoveSubscriber(IPorter porter);
#endif
    }
}