using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow.Manager
{
    public class DetectorManager : MonoSingleton<DetectorManager>
    {
        private HashSet<Detector> detectors = new();

        public void AddDetector(Detector detector)
        {
            detector.transform.SetParent(this.transform);
            detectors.Add(detector);
        }
    }
}