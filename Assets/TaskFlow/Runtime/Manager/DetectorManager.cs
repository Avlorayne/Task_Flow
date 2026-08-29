using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace TaskFlow
{
    public class DetectorManager : MonoSingleton<DetectorManager>
    {
        private static HashSet<Detector> detectors = new();
        public static UnityAction OnDetectEnd;

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
                    detector.Handle();
            }
            OnDetectEnd?.Invoke();
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
            // 把这个分片加在 MonoBehaviour 脚本运行完 LateUpdate 之后
            current = current.InsertSystemAfter<PreLateUpdate.ScriptRunBehaviourLateUpdate>(detectUpdate);
            PlayerLoop.SetPlayerLoop(current);
            // current.PrintEvents();
        }
        
        private class DetectUpdate { }
    }
}