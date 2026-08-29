using UnityEngine;

namespace TaskFlow.Sample
{
    public class SampleSender : MonoBehaviour
    {
        void Start()
        {
            var signal = new SampleSignal();
            Sender.Send(signal);
        }
    }
}