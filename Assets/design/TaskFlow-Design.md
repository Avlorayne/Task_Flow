# TaskFlow 消息队列与上下文管理设计建议

> 适用对象：TaskFlow 事件通道 + 检测树框架  
> 核心议题：消息队列的上下文（Context）如何管理、如何与实际业务运行要求契合  
> 文档位置：`Assets/.design/`（以 `.` 开头的目录 Unity 不会作为资源导入，适合放设计文档）

---

## 1. 现状小结（基于当前代码）

当前组件职责：

| 组件 | 职责 | 现状问题 |
|---|---|---|
| `Porter` | 事件通道抽象（委托分发） | `OnRaised` 是裸委托字段，无 `event` 语义，无自动反注册 |
| `Channel` | 具体事件通道 | 空委托已修复为 `?.Invoke`；仍是同步分发，不是队列 |
| `Sender` | 业务侧发送入口 | `Channel` 未校验；只负责转发 |
| `Signal` | 消息体 | `PropertyHeader` 默认 null；`IDisposable` 是空实现 |
| `DetectionContext` | 上下文 | `Context` 默认 null；只有写入入口，没有 `Clear()`、容量、生命周期 |
| `AtomicDetection` | 相等/比较/包含检测 | 字符串弱类型；`float.Parse` 易抛异常；标志位命名与行为相反 |
| `CombinationDetection` | 与/或/非组合 | `ConcurrentBag` 无序；`Not` 取 `FirstOrDefault` 不确定；默认 null |
| `IHandler` | 处理接口 | 已定义但全项目未使用 |

已经完成的改进：

- `TaskFlow.Runtime` 运行时程序集 + `TaskFlow.Tests.EditMode` 测试程序集。
- 44 个 EditMode 单元测试，覆盖通道、信号、上下文、原子/组合检测、Sender。
- `Channel.Raise` / `Detector.Raise` 空引用修复。

当前上下文机制一句话概括：

> `DetectionContext.Context` 是一个 `Dictionary<Porter, List<Signal>>`，`EnqueueProperty` 按来源端口把消息追加进列表，`GetProperty(path, index)` 从第 `index` 条消息的 `PropertyHeader` 里按字符串键取值。

---

## 2. 先回答三个问题，再谈架构

“消息队列上下文”在不同业务里含义完全不同。先对号入座，否则架构会做偏。

### 问题 A：上下文的作用域是什么？

| 作用域 | 生命周期 | 适合的业务 |
|---|---|---|
| 单消息 | 一条 Signal 从产生到被检测/消费完成 | 即时事件，如“玩家按了攻击键” |
| 流程/事务 | 一次业务操作产生的多条关联消息，全部处理完才清空 | 购买、战斗结算、任务链 |
| 帧级批次 | 每帧从队列取一批，检测完清空 | 每帧轮询的规则系统、技能判定 |

建议：先实现“帧级批次 + 事务 ID”两种，单消息是它们的特例。

### 问题 B：队列是“即发即达”还是“缓冲队列”？

- 当前 `Channel.Raise` 是**同步委托分发**，不是队列。
- 如果业务只要求“发出后立刻有人处理”，保持同步即可，不要为了“队列”而队列。
- 如果业务要求“先攒一批再统一判定”“跨帧处理”“多生产者”，才需要真正的队列。

### 问题 C：容量和失败策略是什么？

- 队列满了：丢弃最旧 / 丢弃最新 / 阻塞 / 抛错？
- 消息处理失败：重试 / 跳过 / 走补偿流程？
- 上下文超时未结束：强制回收还是保留？

这三个答案决定接口设计，建议在实现前用一页纸写清楚。

---

## 3. 核心方案：给上下文一个明确的生命周期

把“谁写、谁读、何时清空”显式化。推荐状态机：

```
Begin(作用域) → Enqueue(消息) → Freeze(生成快照) → Evaluate(检测) → Clear(回收)
```

落地方式：

- `ContextScope` 负责一个业务作用域的累积区，实现 `IDisposable`，用 `using` 包裹业务操作。
- `DetectionContext` 只做两件事：写入（`EnqueueProperty`）和读取（`GetProperty`）。
- `Detector` 只读快照，不直接持有可变的全局 Context。
- 作用域结束（`Dispose` / 帧末 / 事务提交）时统一 `Clear()`。

```csharp
public sealed class ContextScope : IDisposable
{
    private readonly DetectionContext _context = new DetectionContext();

    public DetectionContext Context => _context;

    public void Publish(Porter source, Signal signal)
        => _context.EnqueueProperty(source, signal);

    public void Dispose()
        => _context.Clear();
}
```

`DetectionContext` 需要补齐的防守：

```csharp
public class DetectionContext
{
    public Dictionary<Porter, List<Signal>> Context { get; } = new();

    public void EnqueueProperty(Porter source, Signal signal)
    {
        if (!Context.TryGetValue(source, out var list))
        {
            list = new List<Signal>();
            Context[source] = list;
        }
        list.Add(signal);
    }

    public string GetProperty(PropertyPath path, int index)
    {
        if (Context.TryGetValue(path.SourcePort, out var list)
            && index >= 0 && index < list.Count
            && list[index].PropertyHeader != null
            && list[index].PropertyHeader.TryGetValue(path.PropertyName, out var value))
        {
            return value;
        }
        return null;
    }

    public void Clear() => Context.Clear();
}
```

> 当前代码里 `Context` 字段默认是 null，`EnqueueProperty` 一调用就会 NRE；上面用属性初始化器解决。

---

## 4. 消息（Signal）需要补的元数据

上下文要“可追溯、可关联、可排序”，消息本身要有身份：

```csharp
public class Signal
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string CorrelationId { get; set; }   // 同一业务流程的消息共享此 ID
    public long Sequence { get; internal set; }  // 全局递增序号，保证顺序
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    public Dictionary<string, string> PropertyHeader { get; } = new();
    public object Payload { get; set; }          // 可选：强类型业务数据
}
```

建议：

- 去掉 `IDisposable`，除非消息真的持有非托管资源。纯数据消息实现空 `Dispose` 只会误导调用方。
- `PropertyHeader` 构造时初始化，避免到处判空。
- 高频业务消息建议对象池复用；低频消息直接 new。
- 如果字段固定，优先强类型消息 + 类型分支，避免 `string -> string` 的魔法键：

```csharp
public class PlayerDamagedSignal : Signal
{
    public int Damage;
    public int RemainingHp;
}
```

---

## 5. 如果业务真的要“消息队列”

当前 `Channel` 是同步事件，不是队列。引入队列层时保持最小接口：

```csharp
public interface IMessageQueue
{
    int Count { get; }
    int Capacity { get; }

    void Enqueue(Signal signal);
    bool TryDequeue(out Signal signal);
    IReadOnlyList<Signal> Drain(int maxCount);
}
```

推荐一个 `QueueProcessor`（纯 C#，便于测试）作为唯一消费方：

```csharp
public sealed class QueueProcessor
{
    private readonly IMessageQueue _queue;
    private readonly int _maxPerFrame;

    public QueueProcessor(IMessageQueue queue, int maxPerFrame)
    {
        _queue = queue;
        _maxPerFrame = maxPerFrame;
    }

    public void Tick(DetectionContext context)
    {
        var batch = _queue.Drain(_maxPerFrame);
        foreach (var signal in batch)
        {
            // 按来源写入 context，或按 CorrelationId 分组
        }

        // Freeze -> Evaluate -> Clear
        context.Clear();
    }
}
```

需要显式决策的队列语义：

| 语义 | 建议 |
|---|---|
| 顺序 | 单生产者 FIFO；多生产者用 `Sequence` 排序后再入上下文 |
| 帧预算 | `_maxPerFrame` 限制每帧处理量，超出留到下帧 |
| 背压 | 容量 + 溢出策略枚举（`DropOldest` / `DropNewest` / `Throw`） |
| 跨帧批次 | 一帧一批；批次边界用 `CorrelationId` 或帧号标识 |
| 线程 | 生产者可跨线程 `Enqueue`（用 `ConcurrentQueue`），但消费和检测必须在主线程 |

---

## 6. 检测树与上下文的契合点

当前检测树和上下文基本是断开的：

- `Detector.prePorters` 声明了但没有把消息写入 `Context`。
- `EnqueueProperty` 新增了写入入口，但没人调用、没有清空时机。
- `And/Or/Not` 用 `ConcurrentBag`，顺序不确定；`Not` 在多子节点时结果随机。

建议改动：

1. `prePorters` 在业务入口处把消息写入当前 `ContextScope`，而不是闲置。
2. 组合检测改为有序 `List<Detection>`，提供 `Add/Remove`：

```csharp
public abstract class CombinationDetection : Detection
{
    public List<Detection> SubDetections { get; } = new();

    public void Add(Detection detection) => SubDetections.Add(detection);
    public void Remove(Detection detection) => SubDetections.Remove(detection);
}
```

3. `Not` 只接受一个子节点，构造时校验；空时报明确错误而不是 NRE。
4. 每次评估前对上下文做快照（例如 `Dictionary<Porter, Signal[]>`），检测过程中不允许再写入。

---

## 7. 与实际业务运行要求的契合清单

上线前逐条核对：

- [ ] 消息处理是否全在主线程？跨线程只允许入队。
- [ ] 每帧处理量是否有上限？会不会因为一帧消息太多而卡帧？
- [ ] 同一业务的多条消息是否可以通过 `CorrelationId` 关联？
- [ ] 上下文是否有明确的清空时机（帧末 / 事务结束 / 超时）？
- [ ] 队列满了、处理失败时行为是否可预期？
- [ ] MonoBehaviour 销毁时是否反注册了监听，避免幽灵回调？
- [ ] 日志里能否按 `Id` / `CorrelationId` 追踪一条消息的完整路径？
- [ ] 核心队列/上下文逻辑是否有单元测试（FIFO、容量、溢出、清理）？

---

## 8. 项目整体建议（非队列部分）

### 值得保留

- `Detection` 纯 C# 抽象 + 构造函数注入 Context，测试友好。
- `AtomicDetection` 的 `PropertyPath` / `CustomProperty` 路由清晰。
- asmdef 拆分已经为后续扩展打好基础。

### 需要修正

- `Equal.NotEqual0_Equal1`、`Contain.NotContain0_Contain1`、`Compare.LessThan0_GreaterThan1` 命名与行为相反，改为 `UseEqual` / `ContainExpected` 或枚举。
- `Compare` 用 `float.TryParse(..., NumberStyles.Float, CultureInfo.InvariantCulture, out ...)`，非法输入返回 false 而不是抛异常。
- `IHandler` 要么接入 `Porter` 的监听体系，要么删除。
- `Channel.ProtocolHashError` 永远返回 false，疑似未完成，补全或移除。
- 公开大写字段收敛为 `[SerializeField] private` + 只读属性。

### 不建议现在做

- 全局 EventBus / 服务定位器：项目规模还不需要。
- 为所有类加接口：只在真正有多个实现时再加。
- UniTask / 异步消息持久化：先跑通同步主线程模型。

---

## 9. 落地顺序

### 先做（本周）

1. `DetectionContext`：属性初始化 + `Clear()` + `GetProperty` 防御。
2. `Signal`：`PropertyHeader` 初始化，加 `Id` / `CorrelationId` / `Sequence`。
3. 组合检测：`ConcurrentBag` 改 `List` + `Add/Remove`，`Not` 单子节点。
4. `ContextScope` + `Detector` 接线：让 `prePorters` 真正写入上下文。

### 后做（功能需要时）

5. `IMessageQueue` + `QueueProcessor` + 每帧处理上限。
6. 强类型消息 + 对象池。
7. 多线程生产者（仅入队）与主线程消费。

---

## 10. 一句话总结

> 先把“上下文”从一个无主的公共字典，改成一个有明确生命周期（Begin → Enqueue → Freeze → Evaluate → Clear）的作用域对象；再把“事件通道”按业务需要升级成真正的队列。队列只是手段，上下文生命周期才是让检测树和实际业务对得上的关键。
