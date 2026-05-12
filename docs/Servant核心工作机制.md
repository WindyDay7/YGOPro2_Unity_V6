# Servant 核心工作机制

## 1. `Servant` 在这个项目里的定位

`Servant` 是 YGOPro2 这套客户端架构里的"模块控制器基类". 

它不是 Unity 常见教程里的"每个界面一个 MonoBehaviour 脚本挂场景对象", 而是另一种更接近桌面应用的做法: 

- 整个客户端长期运行在同一个 Unity 场景中;
- `Program` 负责全局初始化、每帧驱动、输入采集、模块切换;
- 各个业务模块(菜单、房间、卡组编辑、对局等)继承 `Servant`;
- 模块本身通常是普通 C# 对象, 需要显示时再创建自己的 UI 和 GameObject. 

所以 `Servant` 的本质不是"场景", 而是"窗口 / 页面 / 功能模块"的统一抽象. 

---

## 2. 它解决了什么问题

如果没有 `Servant`, 每个业务模块都要自己处理下面这些通用问题: 

- 什么时候初始化;
- 什么时候显示、隐藏;
- 分辨率变化后怎么重新布局;
- 如何统一接收鼠标点击、抬起、悬停、右键;
- 自己创建的临时对象如何回收;
- 延时任务在模块切走后如何取消;
- 各种确认框、输入框、单选框如何统一管理. 

`Servant` 就是把这套"窗口基础设施"抽出来, 让业务子类只关注自己的玩法逻辑. 

---

## 3. 它和 `Program` 的关系

### 3.1 创建关系

`Program.initializeALLservants()` 会统一 `new` 出所有业务模块, 并放进 `servants` 列表里. 

这说明: 

- `Servant` 子类大多数不是场景组件;
- 它们在程序启动时就会被构造出来;
- 但构造出来不等于正在显示. 

### 3.2 调度关系

`Program.Update()` 每帧都会遍历 `servants`, 调用每个模块的 `Update()`. 

但 `Servant.Update()` 内部会先判断 `isShowed`: 

- `true`: 执行逐帧逻辑和输入分发;
- `false`: 本帧直接跳过. 

因此项目虽然"每帧调用所有模块", 但真正活跃的通常只有当前显示的模块. 

### 3.3 切换关系

`Program.shiftToServant(Servant to)` 是模块切换入口. 

它的策略很直接: 

1. 先隐藏目标模块之外、当前已显示的模块;
2. 再显示目标模块. 

这套机制意味着这个项目更像"单场景里的多窗口切换", 而不是"多场景切换". 

---

## 4. `Servant` 的生命周期

### 4.1 构造期: `new SomeServant()`

`Servant` 构造函数会做两件事: 

1. 调用 `initialize()`;
2. 把 `preFrameFunction()` 注册进 `updateActions`. 

这说明 `initialize()` 更像"模块构造期初始化", 而不是"每次显示时初始化". 

适合放在 `initialize()` 里的东西: 

- 初始化数据结构;
- 缓存全局引用;
- 注册常驻更新逻辑;
- 做一次性准备. 

不太适合放进去的东西: 

- 只在显示态才需要的临时 UI;
- 需要在 `hide()` 时统一回收的对象. 

### 4.2 显示期: `show()`

`show()` 的核心工作很少, 但很关键: 

- 把 `isShowed` 设为 `true`;
- 重新安排一次 `fixScreenProblem()`. 

它不直接决定界面如何摆放, 而是把布局责任交给: 

- `applyShowArrangement()`
- `applyHideArrangement()`

这种设计把"是否显示"和"如何显示"拆开了. 

### 4.3 隐藏期: `hide()`

`hide()` 是 `Servant` 最重要的资源回收点. 

它会做以下事情: 

1. 清除当前 RMS 普通弹窗;
2. 清除强制确认框;
3. 把 `isShowed` 设为 `false`;
4. 再次触发一次屏幕布局刷新;
5. 销毁 `allGameObjects` 里登记的对象;
6. 清空临时逐帧动作 `updateActions_s`;
7. 取消通过 `safeGogo()` 注册、但还未触发的延时任务. 

这也是为什么这个类虽然看起来简单, 却是项目中非常关键的"收尾总线". 

---

## 5. 逐帧更新机制

`Servant.Update()` 只在 `isShowed == true` 时运行. 

执行顺序如下: 

1. 执行 `updateActions`(常驻逐帧动作);
2. 执行 `updateActions_s`(本次显示期间的临时动作);
3. 分发左键按下事件;
4. 分发左键抬起事件;
5. 分发右键按下/抬起事件;
6. 在悬停对象变化时触发 `ES_HoverOverGameObject()`. 

### 5.1 `updateActions` 与 `updateActions_s` 的区别

#### `updateActions`

特点: 

- 生命周期长;
- 构造后一直存在;
- 只要模块处于显示态就每帧执行. 

典型用途: 

- 模块主循环;
- 常驻动画;
- 主状态检查. 

#### `updateActions_s`

特点: 

- 生命周期短;
- 常用于当前这次 show 期间的临时逻辑;
- `hide()` 时会自动清空. 

典型用途: 

- 当前窗口阶段性的特效;
- 某段引导流程;
- 临时状态监听器. 

这两个列表的组合, 是 `Servant` 很实用的一点: 它把"常驻逻辑"和"显示态临时逻辑"分开了. 

---

## 6. 输入分发机制

`Servant` 不直接自己做 `Input.GetMouseButton()` 或射线检测. 

这些工作由 `Program.Update()` 提前完成: 

- 采集鼠标按键状态;
- 判断当前鼠标指向的对象 `Program.pointedGameObject`;
- 保存左右键、回车、滚轮等统一输入上下文. 

然后 `Servant.Update()` 只负责消费这些结果: 

- 点空白处 -> `ES_mouseDownEmpty()` / `ES_mouseUpEmpty()`
- 点到物体 -> `ES_mouseDownGameObject()` / `ES_mouseUpGameObject()`
- 左键抬起后 -> `ES_mouseUp()`
- 右键事件 -> `ES_mouseDownRight()` / `ES_mouseUpRight()`
- 悬停目标切换 -> `ES_HoverOverGameObject()`

这种设计的好处是: 

- 输入采集逻辑只写一份;
- 每个模块只重写自己关心的回调;
- 模块层不需要关心 Unity 的底层输入拼装细节. 

---

## 7. 对象托管机制

### 7.1 `allGameObjects`

这是当前模块负责回收的对象列表. 

只要对象的生命周期属于当前模块, 就应该想办法接入这套托管. 

### 7.2 `create()` 与 `create_s()`

#### `create()`

只是通过 `Program.I().create(...)` 统一实例化对象, 不会自动托管. 

适合: 

- 生命周期不由当前模块独占;
- 或者会被其他系统统一管理的对象. 

#### `create_s()`

比 `create()` 多一步: 

- 自动把返回对象加入 `allGameObjects`. 

适合: 

- 当前模块显示时创建、隐藏时就该销毁的 UI / 特效 / 临时节点. 

### 7.3 `safeObject()`

如果对象不是用 `create_s()` 创建, 但仍然应该由当前模块销毁, 可以手动调用 `safeObject()` 登记. 

### 7.4 `destroy()`

模块主动销毁对象时, 应优先走 `Servant.destroy()`, 因为它会先从托管表里移除, 避免后续 `hide()` 二次销毁. 

---

## 8. 工具栏机制

`Servant` 自带一个轻量工具栏系统: 

- `SetBar()`: 创建或替换工具栏;
- `showBarOnly()`: 把工具栏移动到可见位置;
- `hideBarOnly()`: 把工具栏移到屏幕外;
- `reShowBar()`: 更新偏移并重新展示. 

设计特点: 

- 工具栏位置基于屏幕坐标再转换到世界坐标;
- 缩放会跟随屏幕高度变化;
- 内部 `toolShift` 组件会在显示/隐藏时整体启停. 

这说明工具栏是 `Servant` 的内建能力, 而不是各模块各写一套. 

---

## 9. 延时任务机制: `safeGogo()`

项目原本有全局延时任务接口: 

- `Program.go(delay, action)`
- `Program.notGo(action)`

问题在于: 如果模块切换了, 之前安排的延时动作可能已经不该执行了. 

`safeGogo()` 就是为了解决这个问题: 

1. 它仍然调用 `Program.go()` 真正注册任务;
2. 但会额外把该任务保存到当前 `Servant.delayedTasks`;
3. `hide()` 时统一 `Program.notGo()` 取消这些任务. 

因此, 凡是"只在当前模块显示期间有效"的延时行为, 都应该优先用 `safeGogo()`. 

---

## 10. RMS: Servant 自带的通用消息系统

`RMS`(remasterMessageSystem)可以理解成 `Servant` 自带的一套标准弹窗框架. 

它负责统一处理: 

- 确认框;
- 是/否选择;
- 三选框;
- 单选列表;
- 多选列表;
- 输入框;
- 站位选择;
- 猜拳窗口;
- 头像确认窗口. 

### 10.1 RMS 的状态核心

关键字段有三个: 

- `currentMStype`: 当前弹窗属于哪种交互类型;
- `currentMShash`: 当前弹窗的业务标识;
- `currentMSwindow`: 当前普通弹窗的根对象. 

另外还有一个特殊窗口: 

- `yesOrNoForce`: 强制确认框, 不走普通 `currentMSwindow` 流程. 

### 10.2 `messageSystemValue`

这是 RMS 的统一选项结构: 

- `value`: 业务值;
- `hint`: 显示给玩家的文本. 

所以 RMS 的思路不是"每个窗口写一套专有返回类型", 而是统一返回 `List<messageSystemValue>`. 

### 10.3 回调链路

普通 RMS 的交互链路是: 

1. 调用 `RMSshow_xxx()` 创建窗口;
2. 设置 `currentMStype` 与 `currentMShash`;
3. 按钮事件统一注册到 `ES_RMSpremono()`;
4. `ES_RMSpremono()` 根据 `currentMStype` 组织结果;
5. 调用子类可重写的 `ES_RMS(hashCode, result)`;
6. 子类根据 `hashCode` 判断这是哪一类业务弹窗. 

### 10.4 多选的特殊点

多选窗口不会点一下就回调. 

它会: 

- 维护 `RMSshow_multipleChoice_selected`;
- 用标签透明度表现选中/取消选中;
- 当选中数量达到 `RMSshow_multipleChoice_count` 时, 才真正调用 `ES_RMS()`. 

### 10.5 强制确认框的特殊点

`RMSshow_yesOrNoForce()` 不走普通 `ES_RMS()`, 而是回调: 

- `ES_RMS_ForcedYesNo(messageSystemValue result)`

这是因为它的业务语义更强, 通常希望与普通弹窗完全分离. 

---

## 11. 继承 `Servant` 时的推荐做法

### 推荐

- 把一次性初始化放进 `initialize()`;
- 把主逻辑写进 `preFrameFunction()` 或 `updateActions`;
- 把只在当前显示阶段有效的逻辑写进 `updateActions_s`;
- 用 `create_s()` 或 `safeObject()` 接管临时对象;
- 用 `safeGogo()` 替代裸 `Program.go()`;
- 用 `ES_*` 回调处理输入, 而不是每个模块自己读 `Input`;
- 用 `applyShowArrangement()` / `applyHideArrangement()` 做布局和出入场动画. 

### 不推荐

- 在 `initialize()` 里堆大量只显示时才需要的 UI;
- 直接调用 `Program.go()` 但不做取消;
- 创建临时对象后不登记生命周期;
- 在多个子类里重复造确认框逻辑, 而不复用 RMS. 

---

## 12. 可以把 `Servant` 简化成一句话

如果只用一句话概括: 

> `Servant` 是 YGOPro2 单场景客户端架构中的"窗口控制器基类", 它把模块生命周期、输入分发、对象托管、延时任务取消和通用弹窗系统统一封装了起来. 

理解了这一点, 再去读 `Menu`、`Room`、`DeckManager`、`Ocgcore`, 就会更容易看出每个子类到底只是在"做业务", 还是也在"重复基础设施逻辑". 
