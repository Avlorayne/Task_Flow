using System;
using TaskFlow.Utility;
using UnityEngine;

namespace TaskFlow
{
    public abstract class Signal
    {
        public float TimeStamp = Time.time;
        
        public static Func<Signal,Signal,Signal> GetEarlier = (s0, s1) => s0.TimeStamp < s1.TimeStamp ? s0 : s1;
        public static Func<Signal,Signal,Signal> GetLater = (s0, s1) => s0.TimeStamp > s1.TimeStamp ? s1 : s0; 
    }

    public static class SignalExtensions
    {
        private static readonly MemberGetter Getter = new();
        
        public static bool TryGetField(this Signal signal, string fieldName, out object value)
            => Getter.TryGetValue(signal, fieldName, out value);
        
        public static object GetField(this Signal signal, string fieldName)
            => Getter.GetValue(signal, fieldName);
    }
}
