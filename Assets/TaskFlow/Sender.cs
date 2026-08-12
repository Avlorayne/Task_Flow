using UnityEngine;

namespace TaskFlow
{
    public class Sender : MonoBehaviour
    {
        public Channel Channel;

        public void Send(Signal signal)
        {
            Channel.Raise(signal);
        }
    }    
}
