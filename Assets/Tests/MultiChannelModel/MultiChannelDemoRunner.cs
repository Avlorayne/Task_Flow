using System.Collections;
using System.Linq;
using UnityEngine;

namespace TaskFlow.TestModel
{
    /// <summary>
    /// PlayMode 演示驱动器：
    /// 从 Resources 加载 ModelChannel / ModelDetector，
    /// 周期性发送信号并分发 ModelSignalContext。
    /// Detector 的订阅关系已由 Detector 资产里的 Subscribed Channels 配置好。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MultiChannelDemoRunner : MonoBehaviour
    {
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private float _interval = 2f;

        private MultiChannelSender _sender;
        private ModelChannel[] _channels;
        private ModelDetector[] _detectors;

        private void Awake()
        {
            _sender = GetComponent<MultiChannelSender>();
            if (_sender == null)
                _sender = FindObjectOfType<MultiChannelSender>();

            _channels = Resources.LoadAll<ModelChannel>("");
            _detectors = Resources.LoadAll<ModelDetector>("");

            Debug.Log($"[TestModel][PlayMode] 加载完成: Channels={_channels.Length}, Detectors={_detectors.Length}", this);
        }

        private void Start()
        {
            if (_autoStart)
                StartCoroutine(RunLoop());
        }

        private IEnumerator RunLoop()
        {
            Debug.Log("[TestModel][PlayMode] Demo 开始运行...", this);
            while (true)
            {
                SendBatch();
                yield return new WaitForSeconds(_interval);
            }
        }

        /// <summary>手动/自动触发一次演示发送。</summary>
        public void SendBatch()
        {
            if (_sender == null)
            {
                Debug.LogError("[TestModel][PlayMode] 场景中缺少 MultiChannelSender。", this);
                return;
            }

            var chA = FindChannel("ChannelA");
            var chB = FindChannel("ChannelB");
            var chC = FindChannel("ChannelC");
            var chX = FindChannel("ChannelX");

            if (chA == null || chB == null || chC == null || chX == null)
            {
                Debug.LogError("[TestModel][PlayMode] 缺少 ChannelA / ChannelB / ChannelC / ChannelX 资源。", this);
                return;
            }

            _sender.Send(chA, new ModelSignalA { Text = "PlayMode-A" });
            _sender.Send(chB, new ModelSignalB { Value = 42 });
            _sender.Send(chC, new ModelSignalC { Enabled = true });
            _sender.Send(chX, new ModelSignalA { Text = "PlayMode-X" });

            var context = _sender.FlushAll();
            Debug.Log($"[TestModel][PlayMode] Full context: {context.Dump()}", this);

            foreach (var detector in _detectors)
            {
                if (detector != null)
                    detector.Receive(context);
            }
        }

        private ModelChannel FindChannel(string name)
            => _channels.FirstOrDefault(c => c != null && c.name == name);
    }
}
