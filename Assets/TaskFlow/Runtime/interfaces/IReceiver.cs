using TaskFlow.Detection;

namespace TaskFlow
{
    public interface IReceiver
    {
        bool SelfActive { get; }
        void Inject(DetectionContext context);
        void Handle();
        void AddSubscriber(IPorter porter);
        void RemoveSubscriber(IPorter porter);
    }
}