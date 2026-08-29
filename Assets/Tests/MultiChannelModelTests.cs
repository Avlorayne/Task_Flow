using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TaskFlow.TestModel;
using UnityEngine;

namespace TaskFlow.Tests
{
    /// <summary>
    /// 多信号、多通道、多 Detector 订阅/接收的独立测试模型测试。
    /// 不依赖、不修改 Runtime 文件夹。
    /// 运行结束后会把所有场景的序列化 Context 写入：
    ///   Assets/Tests/MultiChannelModel/context-dump.json
    /// </summary>
    [TestFixture]
    public class MultiChannelModelTests
    {
        private static readonly List<string> _contextDumps = new();

        private readonly List<ScriptableObject> _createdAssets = new();
        private GameObject _senderGo;

        [OneTimeTearDown]
        public void SaveContextDump()
        {
            if (_contextDumps.Count == 0) return;

            var dir = Path.Combine(Application.dataPath, "Tests", "MultiChannelModel");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "context-dump.json");

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"contexts\": [");
            for (int i = 0; i < _contextDumps.Count; i++)
            {
                sb.Append("    ").Append(_contextDumps[i]);
                if (i < _contextDumps.Count - 1) sb.Append(',');
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[TestModel] 已保存序列化 Context 到: {path}");
            _contextDumps.Clear();
        }

        [SetUp]
        public void SetUp()
        {
            _senderGo = new GameObject("TestSender");
        }

        [TearDown]
        public void TearDown()
        {
            if (_senderGo != null)
                Object.DestroyImmediate(_senderGo);

            foreach (var asset in _createdAssets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }
            _createdAssets.Clear();
        }

        private ModelChannel NewChannel(string name)
        {
            var c = ScriptableObject.CreateInstance<ModelChannel>();
            c.name = name;
            _createdAssets.Add(c);
            return c;
        }

        private ModelDetector NewDetector(string name)
        {
            var d = ScriptableObject.CreateInstance<ModelDetector>();
            d.name = name;
            _createdAssets.Add(d);
            return d;
        }

        private MultiChannelSender AttachSender(params ModelChannel[] channels)
        {
            var sender = _senderGo.AddComponent<MultiChannelSender>();
            foreach (var c in channels)
                sender.AddChannel(c);
            return sender;
        }

        private static void RecordContext(string label, ModelSignalContext context)
        {
            _contextDumps.Add("{\"label\":\"" + label + "\",\"data\":" + (context?.Dump() ?? "null") + "}");
        }

        private static void RecordDetector(string name, ModelDetector detector)
        {
            RecordContext("Detector:" + name, detector?.LastContext);
        }

        [Test]
        public void OneChannel_MultipleDifferentSignals_DetectorReceivesAllInOrder()
        {
            var chA = NewChannel("chA");
            var sender = AttachSender(chA);
            var detector = NewDetector("detectorA");
            detector.Subscribe(chA);

            sender.Send(chA, new ModelSignalA { Text = "first" });
            sender.Send(chA, new ModelSignalB { Value = 7 });
            sender.Send(chA, new ModelSignalC { Enabled = true });

            var context = sender.FlushAll();
            Debug.Log($"[TestModel] Full context: {context.Dump()}");
            RecordContext("FullContext", context);
            detector.Receive(context);
            RecordDetector(detector.name, detector);

            Assert.That(detector.ReceiveCount, Is.EqualTo(1));

            var received = detector.ReceivedSignals(chA);
            Assert.That(received.Count, Is.EqualTo(3));
            Assert.That(((ModelSignalA)received[0]).Text, Is.EqualTo("first"));
            Assert.That(((ModelSignalB)received[1]).Value, Is.EqualTo(7));
            Assert.That(((ModelSignalC)received[2]).Enabled, Is.True);
        }

        [Test]
        public void MultipleDetectors_SameChannel_AllReceiveSameData()
        {
            var chShared = NewChannel("shared");
            var sender = AttachSender(chShared);
            var d1 = NewDetector("d1");
            d1.Subscribe(chShared);
            var d2 = NewDetector("d2");
            d2.Subscribe(chShared);

            sender.Send(chShared, new ModelSignalA { Text = "same" });
            sender.Send(chShared, new ModelSignalB { Value = 99 });

            var context = sender.FlushAll();
            RecordContext("FullContext", context);
            d1.Receive(context);
            d2.Receive(context);
            RecordDetector(d1.name, d1);
            RecordDetector(d2.name, d2);

            Assert.That(d1.ReceivedSignals(chShared).Count, Is.EqualTo(2));
            Assert.That(d2.ReceivedSignals(chShared).Count, Is.EqualTo(2));
            Assert.That(((ModelSignalA)d1.ReceivedSignals(chShared)[0]).Text, Is.EqualTo("same"));
            Assert.That(((ModelSignalA)d2.ReceivedSignals(chShared)[0]).Text, Is.EqualTo("same"));
            Assert.That(((ModelSignalB)d1.ReceivedSignals(chShared)[1]).Value, Is.EqualTo(99));
            Assert.That(((ModelSignalB)d2.ReceivedSignals(chShared)[1]).Value, Is.EqualTo(99));
        }

        [Test]
        public void MultipleDetectors_DifferentChannels_OnlyReceiveOwnSubscriptions()
        {
            var chA = NewChannel("chA");
            var chB = NewChannel("chB");
            var chC = NewChannel("chC");
            var sender = AttachSender(chA, chB, chC);

            var dAC = NewDetector("dAC");
            dAC.Subscribe(chA);
            dAC.Subscribe(chC);
            var dB = NewDetector("dB");
            dB.Subscribe(chB);

            sender.Send(chA, new ModelSignalA { Text = "A-data" });
            sender.Send(chB, new ModelSignalB { Value = 10 });
            sender.Send(chC, new ModelSignalC { Enabled = true });

            var full = sender.FlushAll();
            RecordContext("FullContext", full);
            dAC.Receive(full);
            dB.Receive(full);
            RecordDetector(dAC.name, dAC);
            RecordDetector(dB.name, dB);

            // dAC 的订阅关系
            Assert.That(dAC.Subscribes(chA), Is.True);
            Assert.That(dAC.Subscribes(chB), Is.False);
            Assert.That(dAC.Subscribes(chC), Is.True);

            // dAC 只应看到 A / C
            Assert.That(dAC.ReceivedSignals(chA).Count, Is.EqualTo(1));
            Assert.That(dAC.ReceivedSignals(chC).Count, Is.EqualTo(1));
            Assert.That(dAC.ReceivedSignals(chB).Count, Is.Zero);
            Assert.That(((ModelSignalA)dAC.ReceivedSignals(chA)[0]).Text, Is.EqualTo("A-data"));

            // dB 只应看到 B
            Assert.That(dB.ReceivedSignals(chB).Count, Is.EqualTo(1));
            Assert.That(dB.ReceivedSignals(chA).Count, Is.Zero);
            Assert.That(dB.ReceivedSignals(chC).Count, Is.Zero);
            Assert.That(((ModelSignalB)dB.ReceivedSignals(chB)[0]).Value, Is.EqualTo(10));
        }

        [Test]
        public void Sender_LooksUpChannelByDisplayName_AndSendsCorrectly()
        {
            var chX = NewChannel("chX");
            var sender = AttachSender(chX);
            var detector = NewDetector("detectorX");
            detector.Subscribe(chX);

            sender.Send("chX", new ModelSignalA { Text = "by-name" });

            var context = sender.FlushAll();
            RecordContext("FullContext", context);
            detector.Receive(context);
            RecordDetector(detector.name, detector);

            Assert.That(detector.ReceivedSignals(chX).Count, Is.EqualTo(1));
            Assert.That(((ModelSignalA)detector.ReceivedSignals(chX)[0]).Text, Is.EqualTo("by-name"));
        }

        [Test]
        public void UnsubscribedChannel_NotVisibleToDetector()
        {
            var chX = NewChannel("chX");
            var chY = NewChannel("chY");
            var sender = AttachSender(chX, chY);
            var detector = NewDetector("onlyX");
            detector.Subscribe(chX);

            sender.Send(chX, new ModelSignalA { Text = "x" });
            sender.Send(chY, new ModelSignalB { Value = 1 });

            var full = sender.FlushAll();
            RecordContext("FullContext", full);
            detector.Receive(full);
            RecordDetector(detector.name, detector);

            Assert.That(detector.ReceivedSignals(chX).Count, Is.EqualTo(1));
            Assert.That(detector.ReceivedSignals(chY).Count, Is.Zero);
        }
    }
}
