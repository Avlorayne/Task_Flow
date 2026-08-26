using System;
using System.Collections.Generic;
using System.Linq;
using TaskFlow.TaskFlow.Manager;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace TaskFlow.Manager
{
    public class DetectorManager : MonoSingleton<DetectorManager>
    {
        private static HashSet<Detector> detectors = new();

        protected override void Awake()
        {
            base.Awake();
            detectors =  new HashSet<Detector>(Resources.FindObjectsOfTypeAll<Detector>().Where(d => d.Valid));
        }

        public static void AddDetector(Detector detector)
        {
            detectors.Add(detector);
        }

        private static void DetectAll()
        {
            Debug.Log($"Starting detector loop, count: {detectors.Count}");
            foreach (var detector in detectors)
            {
                if(detector.SelfActive && detector.Called)
                    detector.HeadDetect();
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void InitDetectorLoop()
        {
            var detectUpdate = new PlayerLoopSystem()
            {
                type = typeof(DetectUpdate),
                updateDelegate = DetectAll
            };
            
            var current = PlayerLoop.GetCurrentPlayerLoop();
            // Debug.Log($"=========================Original================================");
            // current.PrintEvents();
            // Debug.Log($"=========================Modified================================");
            current = current.InsertSystemAfter<PreLateUpdate.ScriptRunBehaviourLateUpdate>(detectUpdate);
            PlayerLoop.SetPlayerLoop(current);
            // current.PrintEvents();
        }
        
        private class DetectUpdate { }
    }
}