# problem: `Channel`泛型不能被挂载，`Channel`与`Detector`和订阅强绑定不够明确
## `Channel`泛型声明不能被`Component`成功挂载
### 方案：
1.  ~~尝试能否代码挂载~~ *太麻烦了，对`Editor`内序列化操作不够友好*
2. ~~尝试对`Signal`使用`Atrribute`，编译时自动对`Signal`基类对应的泛型基类进行自动生成~~ *没有物理文件，无法在`editor`里面挂载*
3. 编译时让`Editor`扫描，自动对使用`Signal`子类对应的泛型基类进行封闭子类声明。
4. `Channel`只当`SO`，注入动作由`Manager`发起

### 执行：
1. [x] Channel改SO，把LateUpdate的调用改Manager里面去；
2. [x] 编译时让Editor扫描，自动对使用Signal子类对应的泛型基类进行封闭子类声明。

## `Detector`上下文注入过程对“订阅”体现过差
### 当前过程：
![订阅过程图](Detector-Context-Injection.svg)
1. `Channel`是把信号放在一个公共上下文中的，没有直接发给`Detector`；
2. 让`Detector`做事，只给了一个唤醒信号，其它什么也没有给;
3. 未处理好`Detector`中`handle`判定处理与唤起下一级连接的关系；
### 修复：
1. [x] 公共上下文移到`Manager`中，~~不再设为`static`~~；
2. [x] 在`Channel`中不使用`subscribers`数据，改为观察者模式：
   - [x] `Channel`对公共上下文的注入改为请求式，
   - [X] `Detector`在订阅时自己维护`Channel`，
   - [x] 并在订阅时/唤醒时加入`Call`事件订阅，
   - [x] 在`LateUpdate`之后通过自己的`Channel`去索要对应的公共上下文；
3. [x] 更改接口方法声明，对`IReciever`的加入`Inject`方法以注入上下文  

---------
# problem: `Channel`与`Detector`相连的`HeadDetector`在启动时与其它二三级的`Detector`没有区分清楚，导致全部的`Detector`都同时启动，与级联触发相冲突

## 修复：
1. [x] 继承一个`HeadDetector`，作为单独标明的层级，只有这层被`PlayerLoop`触发；
2. [x] `DetectorManager`内的`HashSet`改为`HeadDetector`类型；
3. [x] 加入一个`DetectionContext`与`SignalContext`以示区分，`DetectionContext`内容以`(IPort, SignalCaptured, AtomicDetection)`为格式，将通过判定的`Signal`传递下去；
4. [x] `IReciever`的`Inject`方法签名改为`DetectionContext`并以此传递；  

---

# mission: 为`Detection`补全取用信号及字段、返回上下文
## 取用信号设计
1. 需要取用信号的全部都是`Atomic Detection`
### 设计方案
1. `Field Path`已经标明了所取字段的完整路径，通过`Signal`的扩展方法`TryGetField`或`GetField`可以获取当前信号的值；
2. 这种方式隐去了具体`Signal`中间过程，对捕获`Signal`的设计不利；
3. `Caught Signal`由`Atomic Detection`使用，在根判定成功时递归获取相关上下文；
### 编码
1. [x] 为`Member Getter`加入无泛型返回的方法，更新``扩展方法；
2. [x] 不关注`Field Path`对具体的 Signal 的作用，`Field Path`只作为 Detection 对字段来源的标记,具体的 Signal 在Atomatic Detection 中向 Detection Context 索取；
3. [x] 在根判定成功时，立即开启 Context 重组,使用DFS对判定树进行子 Context 索取，并在本层时 Context 的 Detection Ptah 添加本层路径； 
4. [x] 在更改时发现 Head Detector 和 Body Detetctor 所用数据类型不一致，导致 Detection 要做两套寻址系统；  

---

# mission: 为 Runtime 部分做静态审查，检查项目中的缺陷、问题、改进方向

## 一、静态审查结论（总览）  
**TL;DR：架构分层（Signal/Channel/Detector/Detection/双 Context/PlayerLoop 调度）是清晰且正确的，但当前 Runtime 本体尚未端到端跑通。** 存在 **4 类编译打包阻断**、**4 类核心链必断** 的问题；且 `Tests/MultiChannelModel` 测试的是一个"平行实现"（ModelChannel/ModelDetector），**没有覆盖 Runtime 本体**，所以这些问题未被测试暴露。你接下来要写的 Editor 依赖注入，其前置的「序列化契约」目前尚未在 Runtime 中成立，需要先修。

---

### P0 · 必修：编译 / 打包 / 运行阻断

| 当前的修复状态或态度                 | #    | 位置 | 问题 | 影响 | 建议 |
|:-------------------------------------|------|---|---|---|---|
| ✅                                   | P0-1 | `TaskFlow/Runtime/Atrributes/InspectorReadOnlyAttribute.cs` | Runtime 目录下 `using UnityEditor` + `CustomPropertyDrawer` | **打包编译失败**（UnityEditor 不进 Player） | Attribute 留在 Runtime（删 UnityEditor 引用），Drawer 移到 `TaskFlow/Editor/` |
| ✅                                   | P0-2 | `abstract/Detector.cs` · `AddSubscriber/RemoveSubscriber` | 包在 `#if UNITY_EDITOR` 里，但接口 `IReceiver` 声明了这两个成员 | **非 Editor 编译失败 CS0535**（接口未实现） | 移出 `#if`；或接口成员同样加条件 |
| ✅                                   | P0-3 | `extends/HeadDetector.cs` · `Inject(SignalContext)`；`Context/SignalContext.cs` · `GetQueue` | `(Queue<Signal>)pair.Value`：`Queue<具体信号>` → `Queue<Signal>` **不协变** | 核心链一进 `Inject` 即 **InvalidCastException** | Context 内部统一存 `Queue<Signal>`（入队时上转，读取用 `OfType<T>()`），或存 `IReadOnlyCollection<Signal>` |
| ✅                                   | P0-4 | `Manager/ChannelManager.cs` · `InitLookup` | 字典键是 `channel.GetType()`（即 `Channel<T>` 封闭类），查询用 `typeof(T)`（Signal 类型）——**两个永远不相等的类型** | `Sender.Send` 永远 LogError；`RefreshSignalQueue` 拿到 null porter，信号整链丢失 | `BaseChannel` 增加抽象 `public abstract Type SignalType { get; }`，以 `SignalType` 为键 |
| ✅<br/>已设置更新时清空              | P0-5 | `extends/Channel.cs` · `LateUpdate`；全局搜索 `Clear` | `_eventQueue` **从不清空**（`ClearQueue` 无人调用，`bool currentEvents` 是死代码），`SetQueue` 只是复制快照 | 信号**永久累积**，同一批信号**每帧重新进入判定**（配合"失败不重置 Called"会每帧重试）；内存无限增长 | `LateUpdate` 里 swap 后 Clear；`DetectorManager.DetectAll` 末尾清 `ChannelContext`；无信号短路 |
| ✅<br/>已删除字段，仅通过参数注入    | P0-6 | `Detection/Detection.cs` · `DetectionContext/SignalContext` 字段 | 两个 protected 字段**从未被赋值**；`Result(DetectionContext context)` 的**参数被无视**，内部用的是 null 字段 | 所有 `AtomicDetection.Result` 一进去就 **NullReferenceException** | 判定改纯函数用参数；或在构建/注入阶段给字段赋值 |
| 待定<br/>见改进方向                  | P0-7 | `abstract/Detector.cs` · `_porters/_receivers` | `HashSet<IPorter>` / `HashSet<IReceiver>`：Unity **既不序列化 HashSet，也不序列化接口字段** | **打包后订阅关系恒为空**，整个订阅网络消失；你在 Editor 里想配的依赖注入，物理上配不进去 | 配置层改 `List<BaseChannel>` / `List<Detector>`（具体 SO 类型），运行时解析回 HashSet；或见「改进方向」的清单资产方案 |
| 驳回<br/>你是傻逼，懂不懂unity       | P0-8 | `Manager/DetectorManager.cs` · `InitDetectorLoop`；`abstract/Sender.cs` | **系统无自举**：`[RuntimeInitializeOnLoadMethod]` 只插 PlayerLoop，从不创建 Manager；`Sender.Send` 与 `Channel<T>.Instance` 走的 `ChannelManager.GetChannelByTypeOfSignal<T>()` 是 **static 方法，不触发 MonoSingleton 实例化** | `Awake` 永不执行 → `channels` 空、`ChannelManager.LateUpdate` 循环不存在 → **整个系统无人启动**（你的 SampleSender 大概率只有一条 LogError） | 初始化时显式预热：`_ = ChannelManager.Instance; _ = DetectorManager.Instance;` |
| 待定<br/>判定的东西很复杂，后面再说  | P0-9 | `Detection/AtomicDetection.cs` · `Equal.Result` | `value0 == value1`，静态类型是 `object` → **引用比较**。装箱 int 是两个不同 box，运行时拼接的 string 也不同引用 | `Equal` 对数值/字符串字段**恒为 false**（误判率 100% 的语义炸弹） | 用 `Equals(value0, value1)` 静态方法；字符串可另设 IgnoreCase 选项 |
| 待定<br/>我不懂这个脚本，后面再改    | P0-10 | `Ultility/MemberGetter.cs` · `GetOrCreateGetter` | `TryCompileGetter` 失败返回 null，随后 `GetOrAdd(key, (Delegate)null)` | 走到错误路径时抛 **ArgumentNullException**（与注释声称的"null 表示已知不兼容"行为不符） | null 短路直接 `return null`（竞态幂等，无害） |
| 待定<br/>判定内容后面再更改          | P0-11 | `Detection/AtomicDetection.cs` · `Compare.Result` | `(float)RoutePropertyValue(...)`：object 里装箱的是 `int`/`double` 时，直接拆箱转 float 抛异常 | 配 int 字段即 **InvalidCastException**（任务系统里计数字段恰恰最常见是 int） | `Convert.ToSingle` + 字段类型白名单 |
| ✅<br/>已做空检查                    | P0-12 | `extends/Channel.cs` · `Instance` | `Resources.Load` 未命中返回 null，`AddChannel(null)` 照单全收 | `ChannelManager.LateUpdate` 遍历时 **NRE** | 判空 + LogError 指出"资产不在 `Resources/TaskFlow/Channel/` 下" |

---

### P1 · 高风险正确性问题

|                       当前的修复状态或态度                             | # | 位置 | 问题 |
|----------------------------------------------------|---|---|---|
| ✅                                                 | P1-1 | `abstract/Signal.cs` | `GetLater` 逻辑写反：`s0.TimeStamp > s1.TimeStamp ? s1 : s0` 返回的是**较早**的那个 |
| ✅                                                 | P1-2 | `Context/DetectionContext.cs` | `DetectionContextPath` 作字典键，却**未重写 `GetHashCode`**，`Equals` 里 `Stack.Equals` 是引用比较；且 `AddPath` / `CombinationDetection.GetContext` 对作为键的 Stack **原地 Push**——键内容漂移，查找随时失效 |
| ✅<br/>对`path`加入`slot:FieldPath`字段            | P1-3 | `Context/DetectionContext.cs` · 构造函数 | 逐个 `Items.Add`：`Equal.GetContext` 中 Field0/Field1 指向同一路径、或同一 Detection 被多处引用时返回**重复键** → `ArgumentException`；`Contain.GetContext` 中 key 用 `new DetectionContextPath(this)` 而 value 用 `path` 查——**key/value 不对应** |
| 待定。现已加入TryRead方法接口，判定协调之后再写    | P1-4 | `Detection/CombinationDetection.cs` · `GetContext` | `path.DetectionPath.Push(this)` 修改的是子 Detection 返回的**同一个共享 Stack**（多次调用叠加）；`context.Select(pair => pair)` 是 no-op；`Not` 在空列表时 `First()` 直接抛 |
| 待定<br/>同上                                      | P1-5 | `AtomicDetection` · `RoutePropertyValueFromSignalContext` | 判定中 `Dequeue()`——**判定函数有副作用**：同帧多个 Detection 互相吃数据、判定失败也把信号消费掉。判定必须纯函数化 |
| ✅<br/>已拆分并重置判定的继承结构                  | P1-6 | `AtomicDetection` · 三个 bool 字段 | `NotEqual0_Equal1` / `LessThan0_GreaterThan1` / `NotContain0_Contain1` 的**命名与代码语义完全相反**（如 `NotEqual0_Equal1=true` 时执行的是 `==`）。这是要暴露给策划的配置项，反向命名必然导致配置事故，建议拆成 `Equal`/`NotEqual` 两个类或改名 `IsEqualMode` |
| ✅<br/>已删除对`Channel`的方法重写，用其他逻辑判定 | P1-7 | `extends/Channel.cs` · `Equals/GetHashCode` | 重写成"同封闭泛型类型即相等" → `HashSet<BaseChannel>` 中**同类型两个不同资产实例被静默合并**，被丢弃实例的信号永远无人消费（静默丢事件） |
| 待定<br/>见改进方向                                | P1-8 | `Detection.cs` / `AtomicDetection.cs` | `Detection.RootDetection`（抽象类多态字段）无 `[SerializeReference]`；`IDetectionField` 接口字段不可序列化；`FieldPath` 内嵌 `IPorter Source` + `Stack<Detection>` 不可序列化——**你 Editor 阶段的配置链路当前不成立**（详见改进方向） |
| ✅<br/>在构造时已排序                              | P1-9 | `Manager/ChannelManager.cs` · `LateUpdate` | `foreach (var channel in channels)`（HashSet）→ 同帧多 Channel、`DetectAll` 中多 HeadDetector 的**执行顺序不确定**，任务触发顺序不可复现 |
| 待定<br/>成环检测在Editor中编写                    | P1-10 | `extends/HeadDetector.cs` / `BodyDetector.cs` · `Handle` | 递归派发**无环检测、无深度上限**（Detector 既可当 porter 又可当 receiver，配成环即栈溢出）；任一 receiver 抛异常会中断整帧且 `Called` 状态错乱 |
| ✅<br/>每次注入上下文时都拷贝副本                  | P1-11 | `HeadDetector.LocalContext` / `BodyDetector.LocalContext` | 跨帧残留（SetQueue 只覆盖存在的键），且与全局 `ChannelContext` **共享同一 Queue 引用**——一旦有消费动作就互相污染 |
| 驳回，乱放资源时程序和策划自己的锅                 | P1-12 | `Manager/DetectorManager.cs` · `Awake` | `Resources.FindObjectsOfTypeAll<HeadDetector>()` 只能找到**已加载**的资产：打包后未进 Resources、未被场景引用的 Detector 不会被找到；Editor 下行为又与打包不同 |
| ✅<br/>幂等保护：在`OnEnable`中先尝试注销事件订阅       | P1-13 | `abstract/Detector.cs` · `OnEnable` | 订阅时机依赖 SO 加载顺序，且 `OnEnable` 订阅 + `AddSubscriber` 再订阅，缺乏幂等保护，存在重复订阅/漏订阅窗口 |

---

### P2 · 健壮性 / 时序 / 兼容性

| 当前的修复状态或态度                                                                                                 | # | 位置 | 问题 |
|----------------------------------------------------------------------------------------------------------------------|---|---|---|
| ✅<br/>已改为非并发队列                                                                                              | P2-1 | `Channel<T>` 用 `ConcurrentQueue`，但 `SignalContext`/`ChannelManager` 全是普通 `Dictionary`/`HashSet` | 线程模型含糊：要么明确"仅主线程"（ConcurrentQueue 换 Queue 即可），要么全程并发容器 |
| ✅<br/>确实用帧号更符合程序设计的逻辑                                                                                | P2-2 | `abstract/Signal.cs` · `TimeStamp = Time.time` | 受 `timescale` 影响（暂停时全帧信号同刻）；建议 `timeSinceLevelLoadAsDouble` 或帧号 |
| ✅<br/>已更新为先移除后更改                                                                                          | P2-3 | `Manager/DetectorManager.cs` · `InitDetectorLoop` | 关闭 Domain Reload（Enter Play Mode Options）时，PlayerLoop 保留上次修改 → **重复插入 `DetectUpdate`**；插入前需按 `type` 查重 |
| ✅<br/>改为字典查询，此字典在队列增改时自动更新维护                                                                  | P2-4 | `ChannelManager.LateUpdate` / `SignalContext.GetContextItemsByPorter` | 无信号也每帧复制队列 + 每个 HeadDetector 每帧 O(字典) 全扫——空转成本 |
| ✅<br/>不再使用`HashSet`                                                                                             | P2-5 | `ChannelManager.cs` | `channels.AddRange(so)` 依赖 `Unity.VisualScripting` 的 HashSet 扩展——**仅为一个 AddRange 引入 UVS 包依赖**，建议自写扩展方法；`Dictionary.TryAdd` 依赖 netstandard2.1 API 级别，若项目切 .NET Framework profile 会编译失败 |
| ✅<br/>插一嘴，过时归过时，你TM说Unity 2023+时几个意思？？你倒是把这个版本的拿给我用啊？？你以为我想窝在2022.3？？！ | P2-6 | `Manager/MonoSingleton.cs` | `FindObjectOfType` 已过时（2023+ 用 `FindFirstObjectByType`） |
| 待定<br/>示例工程后面再写                                                                                            | P2-7 | `Sample/SampleHandler.cs` | `SelfActive { get; }` 自动属性恒为 `false` + 全方法 throw——Sample 是占位品，无法演示闭环 |
| 待定<br/>不过我在生成文件的引用列表中限定了`Signal`为`TaskFlow`命名空间中                                            | P2-8 | `Editor/SignalChannelGenerator.cs` | 生成文件落在 `Assets/TaskFlow/Generated`，但它会引用任意程序集里的 Signal（如 Assembly-CSharp）——**生成代码归入哪个 asmdef 未定**；`_KeepAlive` 挂了 ContextMenu"Create Channel Asset in Resources"却是空方法，误导使用者 |

---

### P3 · 命名 / 风格
- 目录拼写：`Atrributes`→`Attributes`，`Ultility`→`Utility`（design 文档中还有 `IReciver`、`Siganl`、`CaughtSignal`）。
- `Contain`→`Contains`；`Compare` 建议叫 `CompareNumber` 之类以避开 IComparer 语义。
- `IHandler : IReceiver` 是无成员标记接口，当前无存在必要；`Detector.Valid` 无任何置 false 的路径（死配置）；`detectionId` 从未生成/使用；`Detector.Called` 对 BodyDetector 是无效状态（Body 由上游直接 `Handle`，不经 Called 驱动）——Head/Body 的驱动模型建议统一。
- `HeadDetector.Handle` 与 `BodyDetector.Handle` 的 receiver 派发逻辑完全重复，应上提到基类模板方法。
---
## 二、改进方向（重点：你即将写的 Editor 阶段）

你 Editor 阶段的第一件事不是写 UI，而是**先定序列化契约**，否则 UI 无处落笔。建议按此顺序：

1. **配置 / 运行时分离**。当前把"配置数据"（订阅关系、判定树、字段路径）和"运行时状态"（队列、Called、LocalContext）塞在同一批 SO 里，是 P0-5/P0-7/P1-8 的共同根源。建议拆成：
   - `XxxConfig`（纯数据 SO，可序列化，策划编辑）；
   - 运行时对象由 Config 在 `OnEnable`/首次访问时 Build 出来（订阅表、判定树实例、上下文）。
2. **订阅关系**：`List<BaseChannel> _channels` + `List<Detector> _receivers`（Unity 对 SO 引用列表序列化良好），Inspector 加下拉过滤 + 校验；运行时 OnEnable 遍历订阅到 HashSet。
3. **判定树**：`[SerializeReference] public Detection RootDetection`（Detection 是 plain class，正好适用）+ UIToolkit/IMGUI 树形 PropertyDrawer；配套 Asset 校验器（字段路径类型匹配、悬空引用、环检测）。
4. **FieldPath 重构**为可序列化形态：`(BaseChannel sourceSO, string detectionId 链, string fieldName)`，运行时解析成现在的 `DetectionContextPath`。当前内嵌 `IPorter`/`Stack` 的 struct 永远进不了资产。
5. **清单资产替代散装 Resources**：一个 `TaskFlowManifest`（List\<BaseChannel\> + List\<HeadDetector\>）统一加载，消灭 `Resources.LoadAll` / `FindObjectsOfTypeAll` 的时序与打包差异。
6. **判定纯函数化 + 帧末统一消费**：一帧 = "快照 → 全量判定（只读）→ 处理 → 清空"，这是修 P0-5/P1-5 的正解。
7. **稳定性设施**：HeadDetector 加 `SortOrder`；级联做 visited-set 防环；每个 Handle try-catch 隔离异常；调试窗口实时显示每帧 Context。
8. **补 Runtime 本体的 PlayMode 测试**：现有测试只测了平行模型。最小闭环：Send → 下一帧 HeadDetector 命中 → Handler 收到 Context，这一条测试就能拦住 P0 里的大半问题。

---
最后再强调一句优先级：**先修 P0-3/P0-4/P0-5/P0-6/P0-8 这五个（它们共同决定核心链能否跑通），然后立刻给 Runtime 本体补一条端到端 PlayMode 测试，再动 Editor。** 否则你会在 Editor 工具写完后、第一次真机联调时一次性踩完所有雷。需要的话，我可以下一步直接给出这几个 P0 的具体补丁代码。

---




---
# 补充改进：
1. 字段限定： 
   1. `Signal` 的字段做类型限定，只能是C#一级取用字段（`int`,`string`,`float` 等）或引擎内广泛基类，如 `Unity.Object`, `GameObject`, `Component`, `MonoBehabiour` 等；
   2. 在 `Atomic Detection` 中，在对字段进行拆箱时，进行上述类型的比对再进行拆箱与比较；