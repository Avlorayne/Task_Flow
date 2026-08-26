using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskFlow
{
    public interface IPool
    {
        public void RecycleAll();
    }
    
    public class SignalPool<T> : IPool where T : Signal, new()
    {
        private static SignalPool<T> Instance { get; } = new ();
        
        private Stack<T> pool;
        private HashSet<T> recorder;

        public SignalPool()
        {
            recorder =  new HashSet<T>(10);
            pool = new Stack<T>(recorder);
            SignalPoolManager.Instance.SignalPools.Add(this);
        }

        public T GetItem()
        {
            var signal = pool.Count > 0 ? pool.Pop() : new T();
            recorder.Add(signal);
            return signal;
        }

        public void RecycleItem(T signal)
        {
            pool.Push(signal);
        }
        
        public void RecycleAll()
        {
            foreach (var signal in recorder.Where(signal => !signal.Trapped))
            {
                RecycleItem(signal);
            }
        }
    }
    
    public class SignalPoolManager : MonoSingleton<SignalPoolManager>
    {
        public List<IPool> SignalPools = new();
        
        void Start()
        {
            DetectorManager.OnDetectEnd += DetectorRecycle;
        }

        void DetectorRecycle()
        {
            foreach (var pool in SignalPools)
                pool.RecycleAll();
        }
    }
}