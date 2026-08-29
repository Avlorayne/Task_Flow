# TaskFlow MultiChannel Test Model

独立的、**不修改/不依赖 `Assets/TaskFlow/Runtime`** 的多信号、多通道、多 Detector 测试模型。

> 本工作区位于 `Assets/Tests/MultiChannelModel/`，模型使用独立程序集 `TaskFlow.MultiChannelModel`（runtime，非 Editor-only），因此 `MultiChannelSender` 可以作为正常 MonoBehaviour 挂到 Editor 场景的 GameObject 上。
> 测试文件在 `Assets/Tests/MultiChannelModelTests.cs`（Tests 测试程序集）。

## 组成

| 类型 | 说明 |
|---|---|
| `ModelSignal` / `ModelSignalA/B/C` | 可实例化的独立测试信号，带字段便于观察 |
| `ModelChannel` | ScriptableObject 通道，可承载多种信号 |
| `MultiChannelSender` | **MonoBehaviour** Sender，可挂到 GameObject |
| `ModelDetector` | ScriptableObject Detector，可订阅多个 Channel 并按订阅过滤上下文 |
| `ModelSignalContext` | 一次发送后的 `Channel -> Signal[]` 上下文，可 Dump 成日志 |

## 如何在编辑器中使用

1. **创建 Channel**：右键 `Create > TaskFlow/TestModel > Model Channel`
   （或代码：`ScriptableObject.CreateInstance<ModelChannel>()`）。
2. **挂载 Sender**：新建 GameObject，添加 `MultiChannelSender` 组件，
   在 Inspector 的 `Channels` 列表中加入第 1 步创建的 Channel。
3. **创建 Detector**：右键 `Create > TaskFlow/TestModel > Model Detector`，
   在 Inspector 的 `Subscribed Channels` 列表中加入要订阅的 Channel。
4. **发送**：
   - 按对象发送：`sender.Send(channel, new ModelSignalA { Text = "..." });`
   - 按名字发送：`sender.Send("channelName", new ModelSignalB { Value = 42 });`
5. **收集并分发**：
   ```csharp
   var context = sender.FlushAll();  // 汇总所有 Channel 的待发信号并清空
   detector.Receive(context);        // Detector 内部只保留订阅通道里的信号
   ```

每个 Detector 的 `Receive` 都会在 Console 输出它实际收到的信号，例如：
`[TestModel] Detector 'dAC' (第 1 次) 收到 2 条信号: Channel[chA] <- ModelSignalA(...); Channel[chC] <- ModelSignalC(...)`

## PlayMode 演示场景

已有可直接运行的场景：`Assets/Scenes/MultiChannelModelDemo.unity`

场景内容：
- GameObject `MultiChannelModelDemo`
  - `MultiChannelSender`
    - `Channels` 已配置：ChannelA / ChannelB / ChannelC / ChannelX
  - `MultiChannelDemoRunner`（每 2 秒自动发送一次演示信号，Detector 订阅关系直接读取 Detector 资产配置）

资源（Detector 资产里的 `Subscribed Channels` 已配置好）：
- `Assets/Tests/MultiChannelModel/Resources/ChannelA.asset`
- `Assets/Tests/MultiChannelModel/Resources/ChannelB.asset`
- `Assets/Tests/MultiChannelModel/Resources/ChannelC.asset`
- `Assets/Tests/MultiChannelModel/Resources/ChannelX.asset`
- `Assets/Tests/MultiChannelModel/Resources/DetectorAC.asset`
  - `Subscribed Channels`: ChannelA, ChannelC
- `Assets/Tests/MultiChannelModel/Resources/DetectorB.asset`
  - `Subscribed Channels`: ChannelB
- `Assets/Tests/MultiChannelModel/Resources/DetectorX.asset`
  - `Subscribed Channels`: ChannelX

在 Unity 中打开该场景，直接点 Play 即可看到 Console 输出：
- `[TestModel][PlayMode] Full context: {...}`
- `[TestModel] Detector 'DetectorAC' ...`
- `[TestModel] Detector 'DetectorB' ...`
- `[TestModel] Detector 'DetectorX' ...`

## 序列化输出

运行测试后，所有场景的序列化 Context 会汇总写入：

`Assets/Tests/MultiChannelModel/context-dump.json`

内容为 JSON 风格结构，每个条目包含 `label`（FullContext / Detector:<name>）和 `data`（Channel -> Signal[]）。

## 如何运行测试

Unity Test Runner → **EditMode** → 运行 `TaskFlow.Tests.MultiChannelModelTests`。

覆盖：
- 一个通道内放入多个不同信号，Detector 按顺序收到。
- 多个 Detector 订阅同一通道，都能收到相同数据。
- 多个 Detector 订阅不同通道，只收到自己订阅通道的信号。
- Sender 按通道名查找并发送。
- 未订阅通道的信号对 Detector 不可见。
