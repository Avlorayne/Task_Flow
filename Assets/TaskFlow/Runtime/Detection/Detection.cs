using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow.Detection
{
    [Serializable]
    public abstract class Detection
    {   
        [SerializeField] public int detectionId = 0;
        
        public abstract bool Result(IContextReader contextReader);

        public abstract DetectionContext GetContext();
    }
}