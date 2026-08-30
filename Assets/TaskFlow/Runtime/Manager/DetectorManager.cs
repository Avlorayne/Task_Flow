using System;
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
        private HeadDetector[] headDetectors;
        public UnityAction OnDetectEnd;

        protected override void Awake()
        {
            base.Awake();
            headDetectors =  Resources.LoadAll<HeadDetector>(Detector.ResourcePath)
                .Where(d => d.Valid)
                .Distinct()                                    // 去重且保持加载顺序
                .OrderBy(d => d.name, StringComparer.Ordinal)  // 显式钉死顺序
                .ToArray();
        }

        private void DetectAll()
        {
            // Debug.Log($"Starting detector loop, count: {detectors.Count}");
            foreach (var detector in headDetectors)
            {
                if(detector.SelfActive && detector.Called)
                {
                    detector.Inject(ChannelManager.Instance.ChannelContext);
                    detector.Handle();
                }
            }
            OnDetectEnd?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod]
        private static void InitDetectorLoop()
        {
            var current = PlayerLoop.GetCurrentPlayerLoop();
    
            // 先移除已存在的相同类型的 System
            current = current.RemoveSystem<DetectUpdate>();
    
            var detectUpdate = new PlayerLoopSystem
            {
                type = typeof(DetectUpdate),
                updateDelegate = Instance.DetectAll
            };
    
            current = current.InsertSystemAfter<PreLateUpdate.ScriptRunBehaviourLateUpdate>(detectUpdate);
            PlayerLoop.SetPlayerLoop(current);
        }
        
        private class DetectUpdate { }
    }
}