using TaskFlow.Utility;
using UnityEngine;

namespace TaskFlow
{
    public abstract class Signal
    {
        public float TimeStamp = Time.time;
    }

    public static class SignalExtensions
    {
        private static readonly MemberGetter Getter = new();
        
        public static bool TryGetField<T>(this Signal signal, string fieldName, out T value)
            => Getter.TryGetValue(signal, fieldName, out value);
        
        public static T GetField<T>(this Signal signal, string fieldName)
            => Getter.TryGetValue<T>(signal, fieldName, out var v) ? v : default;
    }
}
