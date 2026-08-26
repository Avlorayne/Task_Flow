using UnityEngine;

namespace TaskFlow
{
    public abstract class Signal
    {
        public float timeStamp;

        public Signal()
        {
            timeStamp = Time.time;
        }
    }
    
    public class SampleSignal : Signal
    {
        
    }
}
