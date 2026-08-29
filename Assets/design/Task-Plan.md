## problem: `Channel`泛型不能被挂载，`Channel`与`Detector`和订阅强绑定不够明确
### `Channel`泛型声明不能被`Component`成功挂载
#### 方案：
1.  ~~尝试能否代码挂载~~ *太麻烦了，对`Editor`内序列化操作不够友好*
2. ~~尝试对`Signal`使用`Atrribute`，编译时自动对`Signal`基类对应的泛型基类进行自动生成~~ *没有物理文件，无法在`editor`里面挂载*
3. 编译时让`Editor`扫描，自动对使用`Signal`子类对应的泛型基类进行封闭子类声明。
4. `Channel`只当`SO`，注入动作由`Manager`发起

#### 执行：
1. [x] Channel改SO，把LateUpdate的调用改Manager里面去；
2. [x] 编译时让Editor扫描，自动对使用Signal子类对应的泛型基类进行封闭子类声明。

### `Detector`上下文注入过程对“订阅”体现过差
#### 当前过程：
![订阅过程图](Detector-Context-Injection.svg)
1. `Channel`是把信号放在一个公共上下文中的，没有直接发给`Detector`；
2. 让`Detector`做事，只给了一个唤醒信号，其它什么也没有给;
3. 未处理好`Detector`中`handle`判定处理与唤起下一级连接的关系；
#### 修复：
1. [x] 公共上下文移到`Manager`中，~~不再设为`static`~~；
2. [x] 在`Channel`中不使用`subscribers`数据，改为观察者模式：
   - [x] `Channel`对公共上下文的注入改为请求式，
   - [X] `Detector`在订阅时自己维护`Channel`，
   - [x] 并在订阅时/唤醒时加入`Call`事件订阅，
   - [x] 在`LateUpdate`之后通过自己的`Channel`去索要对应的公共上下文；
3. [x] 更改接口方法声明，对`IReciver`的加入`Inject`方法以注入上下文  

---------
## problem: `Channel`与`Detector`相连的`HeadDetector`在启动时与其它二三级的`Detector`没有区分清楚，导致全部的`Detector`都同时启动，与级联触发相冲突

#### 修复：
1. [x] 继承一个`HeadDetector`，作为单独标明的层级，只有这层被`PlayerLoop`触发；
2. [x] `DetectorManager`内的`HashSet`改为`HeadDetector`类型；
3. [x] 加入一个`DetectionContext`与`SignalContext`以示区分，`DetectionContext`内容以`(IPort, CaughtSiganl, AtomicDetection)`为格式，将通过判定的`Signal`传递下去；
4. [x] `IReciever`的`Inject`方法签名改为`DetectionContext`并以此传递；  

---

## mission: 为`Detection`补全取用信号及字段、返回上下文
### 取用信号设计
1. 需要取用信号的全部都是`Atomic Detection`
#### 设计方案
1. `Field Path`已经标明了所取字段的完整路径，通过`Signal`的扩展方法`TryGetField`或`GetField`可以获取当前信号的值；
2. 这种方式隐去了具体`Signal`中间过程，对捕获`Signal`的设计不利；
3. `Caught Signal`由`Atomic Detection`使用，在根判定成功时递归获取相关上下文；
#### 编码
1. [x] 为`Member Getter`加入无泛型返回的方法，更新`Siganl`扩展方法；
2. [x] 不关注`Field Path`对具体的 Signal 的作用，`Field Path`只作为 Detection 对字段来源的标记,具体的 Signal 在Atomatic Detection 中向 Detection Context 索取；
3. [x] 在根判定成功时，立即开启 Context 重组,使用DFS对判定树进行子 Context 索取，并在本层时 Context 的 Detection Ptah 添加本层路径； 
4. [x] 在更改时发现 Head Detector 和 Body Detetctor 所用数据类型不一致，导致 Detection 要做两套寻址系统；  

---