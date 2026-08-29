using System;
using System.Collections.Generic;
using System.Linq;
using TaskFlow.Detection;
using UnityEngine;
using UnityEngine.Events;

namespace TaskFlow
{
    public abstract class Detector : ScriptableObject, IPorter, IReceiver
    {
        [Header("订阅关系")]
        [SerializeField] protected HashSet<IPorter> _porters = new ();
        [SerializeField] protected HashSet<IReceiver> _receivers = new();
        
        public Detection.Detection RootDetection;

        public UnityAction OnSignal { get; set; }
        public bool Valid { get; protected set; } = true;
        public bool SelfActive { get; protected set; } = true;
        public bool Called { get; protected set; } = false;

        private void CallDetect() => Called = true;

        public abstract void Inject(DetectionContext context);
        public abstract void Handle();


        void OnEnable()
        {
            foreach (var porter in _porters)
                porter.OnSignal += CallDetect;
        }

        void OnDisable()
        {
            foreach (var porter in _porters)
                porter.OnSignal -= CallDetect;
        }

#if UNITY_EDITOR
        public void AddSubscriber(IPorter porter)
        {
            porter.OnSignal += CallDetect;
            _porters.Add(porter);
        }

        public void RemoveSubscriber(IPorter porter)
        {
            porter.OnSignal -= CallDetect;
            _porters.Remove(porter);
        }
#endif
    }
}
