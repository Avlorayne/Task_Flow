using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskFlow.TestModel
{
    /// <summary>
    /// 独立测试通道：可承载多种 ModelSignal。
    /// 作为 ScriptableObject，可在编辑器里创建资产，也可用代码 CreateInstance。
    /// </summary>
    [CreateAssetMenu(fileName = "New ModelChannel", menuName = "TaskFlow/TestModel/Model Channel")]
    public sealed class ModelChannel : ScriptableObject
    {
        [SerializeField] private string _displayName;

        private readonly Queue<ModelSignal> _pending = new();

        /// <summary>通道显示名；未设置时使用资产名。</summary>
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        public int PendingCount => _pending.Count;

        public void Publish(ModelSignal signal)
        {
            if (signal == null) throw new ArgumentNullException(nameof(signal));
            _pending.Enqueue(signal);
        }

        /// <summary>取出并清空当前待处理信号。</summary>
        public List<ModelSignal> DrainPending()
        {
            var result = new List<ModelSignal>(_pending.Count);
            while (_pending.Count > 0)
                result.Add(_pending.Dequeue());
            return result;
        }

        public void Clear() => _pending.Clear();
    }
}
