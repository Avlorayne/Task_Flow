using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace TaskFlow
{
    public class Detector : Porter
    {
        public Porter[] prePorters;

        public DetectionContext Context = new ();
        
        public Detection RootDetection;

        public override void Raise(Signal signal)
        {
            if(RootDetection.Result)
                OnRaised(signal);
        }
    }
}