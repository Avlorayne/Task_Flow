using System;
using UnityEngine;

namespace TaskFlow
{
    public abstract class Porter : ScriptableObject
    {
        protected Action<Signal> OnRaised;

        public abstract void Raise(Signal signal);

        public void AddListener(Action<Signal> listener)
        {
            OnRaised += listener;
        }

        public void RemoveListener(Action<Signal> listener)
        {
            OnRaised -= listener;
        }
    }
}