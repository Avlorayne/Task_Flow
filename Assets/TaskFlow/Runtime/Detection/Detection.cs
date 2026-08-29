using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow.Detection
{
    [Serializable]
    public abstract class Detection
    {
        [SerializeField] public int detectionId = 0;
        protected DetectionContext DetectionContext;
        protected SignalContext SignalContext;
        public abstract bool Result(DetectionContext context);
        public abstract bool Result(SignalContext context);

        public abstract KeyValuePair<DetectionContextPath, Signal>[] GetContext();
    }
}