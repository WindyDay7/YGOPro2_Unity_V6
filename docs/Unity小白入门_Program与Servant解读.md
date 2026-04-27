# Unity 小白入门:Program 与 Servant 解读

这份文档是给第一次接触这个项目、同时又刚开始学 Unity 的同学准备的. 

如果你只想先抓住主线, 请记住一句话:

> `Program` 是总控入口, `Servant` 是每个功能模块的统一基类. 

---

## 1. 先用一句话理解这个项目

这个项目不是常见的"切换很多 Unity 场景"的做法. 

它更像一个**单场景客户端程序**:

- Unity 负责渲染、输入、摄像机和对象生命周期;
- `Program` 负责总初始化和每帧驱动;
- `Servant` 负责菜单、设置、房间、卡组编辑、对局等具体功能模块;
- 各模块大多数时候不是直接挂在场景里的脚本, 而是普通 C# 对象, 需要显示时再创建自己的 UI / GameObject. 

所以你读代码时, 不要先按"Scene 切换"去理解, 而要按"桌面应用窗口切换"去理解. 

---

## 2. 你现在最该先看哪两个文件

推荐先看:

- [Assets/SibylSystem/Program.cs](../Assets/SibylSystem/Program.cs)
- [Assets/SibylSystem/Servant.cs](../Assets/SibylSystem/Servant.cs)

原因:

- `Program.cs` 决定了"游戏是怎么启动起来的";
- `Servant.cs` 决定了"每个功能界面是怎么被统一管理的". 

把这两个文件看懂后, 再去读 `Menu`、`Room`、`DeckManager`、`Ocgcore` 会轻松很多. 

---

## 3. `Program` 到底在干什么

你可以把 `Program` 理解成下面几个身份的组合:

### 3.1 Unity 场景入口

`Program` 继承自 `MonoBehaviour`, 所以 Unity 会自动调用它的生命周期函数:

- `Start()`:场景刚启动时调用一次
- `Update()`:每一帧调用一次
- `OnGUI()`:旧 GUI 事件回调
- `OnApplicationQuit()`:程序退出时调用

这说明 `Program` 不是一个普通类, 而是"挂在场景中的 Unity 组件". 

### 3.2 资源注册表

`Program` 前面有很多像这样的字段:

- `public GameObject new_ui_menu;`
- `public GameObject mod_ocgcore_card;`
- `public Camera main_camera;`

它们的含义通常是:

- 在 Unity Inspector 里, 把某个预制体或场景对象拖到这里;
- 运行时再由代码统一创建和管理. 

也就是说, `Program` 像一个"大仓库管理员". 

### 3.3 全局单例入口

`Program` 里有:

- `private static Program instance;`
- `public static Program I()`

它的目的很简单:

- 让别的普通类也能拿到当前 `Program`;
- 比如 `Servant` 里就会调用 `Program.I().create(...)`. 

这是一种很常见但耦合度也比较高的写法. 

### 3.4 模块调度器

`Program` 维护了很多模块实例:

- `menu`
- `setting`
- `selectDeck`
- `room`
- `deckManager`
- `ocgcore`
- `selectReplay`
- `puzzleMode`
- `aiRoom`

这些模块都统一放在 `List<Servant> servants` 里. 

然后通过 `shiftToServant()` 来切换当前功能界面. 

所以这里的核心思想是:

- 不是切场景;
- 而是在同一个运行环境里切换"当前显示的模块". 

### 3.5 主循环驱动器

在 `Update()` 中, `Program` 会做几件事:

1. 检查分辨率是否变化;
2. 更新摄像机位置和角度;
3. 读取鼠标、滚轮、键盘输入;
4. 计算当前鼠标指向了哪个对象;
5. 让每个 `Servant` 执行自己的 `Update()`;
6. 调用网络层 `TcpHelper.preFrameFunction()`;
7. 执行到点的延时任务. 

这就是这个客户端真正的"运行心跳". 

---

## 4. `Program.Start()` 的启动流程

你可以把启动过程理解成下面这条链:

### 第一步:Unity 调用 `Start()`

`Start()` 做了这些事:

- 修正分辨率;
- 设置帧率;
- 创建鼠标特效对象;
- 保存单例 `instance`;
- 调用 `initialize()`;
- 延迟调用 `gameStart()`. 

### 第二步:`initialize()` 分两批初始化

它不是一口气全部初始化, 而是用:

- `go(1, () => { ... })`
- `go(300, () => { ... })`

这种方式拆成延迟任务. 

大概意思是:

- 第 1 批先搭基础环境;
- 第 2 批再加载比较重的数据和模块. 

#### 第一批主要做什么

- 初始化头像等基础 UI 数据;
- 初始化多摄像机系统;
- 创建背景模块;
- 处理初始屏幕适配. 

#### 第二批主要做什么

- 读取翻译和配置;
- 初始化贴图管理器;
- 扫描 `expansions` / `cdb` / `diy` / `data` / `pack`;
- 加载 `.conf`、`.cdb`、`.zip`、`.ypk`;
- 初始化禁卡表;
- 创建所有 `Servant` 模块;
- 预加载高频资源;
- 读取命令行参数. 

### 第三步:`gameStart()` 进入初始界面

`gameStart()` 会:

- 按需要最大化窗口;
- 显示背景;
- 切换到主菜单 `menu`. 

到这里, 整个客户端才算真正进入"可交互状态". 

---

## 5. 为什么这个项目用了这么多 Camera

如果你看到 `Program.initializeALLcameras()` 觉得很奇怪, 这很正常. 

这个项目用了"多摄像机 + 不同 Layer 分层渲染"的方式. 

大致是:

- `camera_back_ground_2d`:背景 UI
- `camera_container_3d`:中间 3D 容器
- `camera_game_main`:主相机
- `camera_main_2d`:主 UI
- `camera_windows_2d`:弹窗 UI
- `camera_main_3d`:主要 3D 表现

这样做的好处是:

- 不同层的对象互不干扰;
- 2D UI 和 3D 场景可以更灵活地叠加;
- 某些窗口或特效可以独立控制显示顺序. 

对 Unity 新手来说, 你只要先记住一条:

> Camera 不一定只有一个. 复杂项目里, 经常会用多个 Camera 叠加出最终画面. 

---

## 6. `Servant` 是什么

`Servant` 是这个项目自定义的一套"模块控制器基类". 

你可以把它理解成:

- 类似一个页面控制器;
- 类似一个窗口管理对象;
- 类似一个统一的功能模块接口. 

它的核心职责包括:

- `initialize()`:初始化模块
- `show()`:显示模块
- `hide()`:隐藏模块
- `fixScreenProblem()`:分辨率变化时重新布局
- `preFrameFunction()`:逐帧逻辑
- `ES_*()`:鼠标与交互回调
- `Update()`:统一驱动模块内部逻辑

### 6.1 为什么不直接每个模块都写成 MonoBehaviour

因为作者想让业务模块更像"纯 C# 对象", 而不是完全绑死在场景组件上. 

这样做的特点是:

- 模块结构更统一;
- 切换模块更方便;
- 可以在一个场景里长期运行;
- 业务逻辑和 GameObject 生命周期部分解耦. 

代价是:

- 初学者第一次看会不太像标准 Unity 教程;
- 你需要同时理解"普通 C# 对象"和"Unity GameObject 对象"两套东西如何协作. 

---

## 7. `Servant` 的运行方式

### 7.1 创建时机

在 `Program.initializeALLservants()` 里, 程序会这样做:

- `menu = new Menu();`
- `setting = new Setting();`
- `room = new Room();`
- `ocgcore = new Ocgcore();`

注意这里的 `new`:

- 创建的是普通 C# 对象;
- 不是直接往场景里挂一个组件. 

### 7.2 显示时机

真正显示模块时, 会调用:

- `show()`
- `applyShowArrangement()`

如果隐藏, 则调用:

- `hide()`
- `applyHideArrangement()`

### 7.3 每帧更新

`Program.Update()` 中会遍历所有 `servants`:

- `servants[i].Update();`

但 `Servant.Update()` 内部会先判断:

- `if (isShowed)`

也就是说:

- 模块对象可能一直存在;
- 但只有显示状态下才真正处理输入和逐帧逻辑. 

这也是它像"窗口系统"的地方. 

---

## 8. 这个项目里你最常见的 C# / Unity 特殊语法

下面这些是你读这两个文件时最常遇到、也最容易卡住的语法. 

### 8.1 `public static`

例如:

- `public static Program I()`
- `public static float wheelValue`

含义:

- `public`:外部可访问;
- `static`:属于类本身, 不属于某个具体对象. 

你可以简单理解为"全局共享成员". 

### 8.2 `List<Servant>`

这是泛型集合. 

- `List<T>` 表示"可动态增删的列表";
- `List<Servant>` 表示"里面装的都是 `Servant` 类型". 

类似于"一个只能装模块对象的数组". 

### 8.3 `Action`

例如:

- `public Action act;`
- `go(300, () => { ... });`

`Action` 表示"一个可以被调用的方法". 

它没有返回值, 也没有参数. 

常见用途:

- 把一段逻辑当作参数传进去;
- 实现回调、延时执行、事件处理. 

### 8.4 Lambda 表达式:`() => { ... }`

这在本项目里很多, 例如:

- `go(500, () => { gameStart(); });`

可以把它理解成"临时写一个匿名函数". 

拆开看:

- `()`:没有参数;
- `=>`:读作"变成"或"映射到";
- `{ ... }`:函数体. 

等价理解:

"500 毫秒后执行这段代码". 

### 8.5 `default(Vector3)`

例如:

- `Vector3 position = default(Vector3)`

意思是:

- 取 `Vector3` 这个类型的默认值;
- 对 `Vector3` 来说就是 `(0, 0, 0)`. 

这里通常用来做"可选参数默认值". 

### 8.6 `out hit`

例如:

- `Physics.Raycast(line, out hit, 1000, rayFilter)`

意思是:

- 如果射线检测成功, 函数会把结果写到 `hit` 变量里;
- `hit` 不是输入, 而是输出参数. 

可以简单理解成"请把检测结果带回来, 放到这个变量里". 

### 8.7 `virtual` / `override`

在 `Servant` 体系里很多方法是:

- `public virtual void show()`
- `public virtual void initialize()`

`virtual` 表示:

- 子类可以重写这个方法. 

如果子类真的重写, 就会写成:

- `public override void show()`

这是面向对象继承的基本用法. 

### 8.8 `#region`

例如:

- `#region Resources`
- `#endregion`

这不是运行时语法, 而是给编辑器看的"代码折叠分区". 

作用只是:

- 把长文件按逻辑折叠整理;
- 不影响程序实际执行. 

### 8.9 `#if UNITY_EDITOR`

例如调试函数里:

- `#if UNITY_EDITOR`
- `Debug.Log(o);`
- `#endif`

意思是:

- 只有在 Unity 编辑器环境下才编译这段代码;
- 真正发布后, 这段代码可以不进入最终构建. 

这是条件编译. 

### 8.10 `Instantiate()` 和 `Destroy()`

这是 Unity 最常见的对象生命周期函数:

- `Instantiate()`:复制/创建一个对象
- `Destroy()`:销毁一个对象

在这个项目里, 作者又包了一层:

- `Program.create(...)`
- `Program.destroy(...)`

目的是统一处理:

- 父节点
- Layer
- 动画
- 销毁时机

### 8.11 `MonoBehaviour`

`Program : MonoBehaviour` 的意思是:

- `Program` 继承了 Unity 组件基类;
- 它可以挂在 GameObject 上;
- Unity 会自动调用它的生命周期方法. 

而 `Servant` 没有继承 `MonoBehaviour`, 说明它是普通 C# 类. 

这两者一定要分清. 

---

## 9. 你可以怎么读这个项目

推荐阅读顺序如下:

### 第一层:先看入口和基类

1. [Assets/SibylSystem/Program.cs](../Assets/SibylSystem/Program.cs)
2. [Assets/SibylSystem/Servant.cs](../Assets/SibylSystem/Servant.cs)

先搞懂:

- 项目怎么启动;
- 模块怎么创建;
- 输入怎么分发;
- 模块怎么切换. 

### 第二层:看主菜单和设置

3. `Menu`
4. `Setting`

因为这两块更接近"普通客户端 UI", 理解门槛最低. 

### 第三层:看卡组编辑与房间

5. `selectDeck`
6. `DeckManager`
7. `Room`

这几块能帮助你理解:

- 卡组数据流;
- 联机准备流程;
- UI 模块之间如何跳转. 

### 第四层:最后再看真正对局核心

8. `Ocgcore`
9. `TcpHelper`
10. `coreWrapper`
11. `precy`

这些文件会更偏规则、协议、桥接层, 难度明显更高. 

---

## 10. 如果你是 Unity 小白, 建议先建立的几个概念

### 10.1 场景不是唯一组织方式

新手教程常常是:

- 一个场景一个功能. 

但这个项目是:

- 一个主场景 + 多模块切换. 

这是完全可行的, 只是更像应用程序架构. 

### 10.2 普通 C# 类也能控制 Unity 对象

不要以为只有 `MonoBehaviour` 才能写游戏逻辑. 

这个项目大量逻辑都在普通类里, 只是通过 `Program` 间接创建和管理 GameObject. 

### 10.3 UI、输入、网络、规则不一定写在一起

这个项目把它们拆成了几层:

- `Program`:统一驱动
- `Servant`:功能模块
- `TcpHelper`:网络桥接
- `Ocgcore`:对局表现与状态
- `YGOSharp`:卡片数据

你读代码时, 最好先问自己:

> "这段代码属于哪一层？"

这样就不容易迷路. 

---

## 11. 你现在最值得记住的 5 个结论

- `Program` 是总入口, 不只是一个普通脚本. 
- `Servant` 是所有功能模块的共同基类. 
- 项目主要靠"模块切换", 不是靠"场景切换". 
- `Update()` 是整个客户端每帧驱动的中心. 
- 看到 `Action`、`() => {}`、`default(...)`、`out` 不要慌, 它们只是 C# 的常见写法. 

---

## 12. 下一步建议

如果你已经把 `Program` 和 `Servant` 看完, 建议下一步看下面其中一个方向:

### 路线 A:先看 UI 和交互

- `Menu`
- `Setting`
- `CardDescription`

适合想先理解"界面是怎么搭起来的". 

### 路线 B:先看卡组和数据

- `selectDeck`
- `DeckManager`
- `YGOSharp/CardsManager.cs`

适合想先理解"卡片数据和卡组编辑". 

### 路线 C:先看对局核心

- `Room`
- `Ocgcore`
- `TcpHelper`

适合想理解"联机消息怎么变成场上表现". 

---

如果你愿意, 我下一步可以继续帮你做两件事中的任意一种:

1. 继续给 `Menu` / `DeckManager` / `Ocgcore` 补这种新手向注释;
2. 给你画一份"从点击开始对局到进入 `Ocgcore`"的数据流说明. 
