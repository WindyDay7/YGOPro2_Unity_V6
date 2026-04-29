# Program 资源注册表与 UI 绑定说明

本文档面向刚接触这个项目的开发者, 专门说明 `Assets/SibylSystem/Program.cs` 到底是什么、它绑定了哪些资源、这些资源是如何在场景中挂上的, 以及当前项目里的主要 UI 实际来自哪些目录、由哪些模块创建和驱动.

---

## 1. 先说结论: `Program` 不是"所有游戏对象本体"

如果你刚看到 `Program.cs` 里一大堆: 

- `public Camera main_camera;`
- `public GameObject new_ui_menu;`
- `public GameObject mod_ocgcore_card;`
- `public GameObject ES_1;`

很容易产生一种错觉: 

> `Program` 里是不是已经装着整个游戏里所有对象？

更准确的理解应该是: 

> `Program` 更像"全局总控器 + Inspector 资源注册表 + 模块切换中心".

也就是说, 它主要做三件事: 

1. **保存资源引用**  
   通过 Unity Inspector 把场景对象、音频、特效 prefab、UI prefab 拖进 `Program` 的 public 字段里.

2. **初始化业务模块**  
   在运行时创建 `Menu`、`Setting`、`SelectServer`、`DeckManager`、`Ocgcore` 等模块对象.

3. **给各个模块提供统一入口**  
   任何模块都可以通过 `Program.I().xxx` 取到: 
   - 一个 prefab
   - 一个公共相机
   - 一个全局模块
   - 一个全局配置对象

所以: 

- `Program` **不等于** 所有运行中的 `GameObject`
- `Program` 里更多存的是 **"引用"**, 不是"世界上所有实例"
- 真正的 UI/特效对象, 通常是在各模块 `initialize()` / `show()` 时再 `createWindow(...)` 或 `create(...)` 动态实例化出来的

相关代码: 

- `Program` 资源字段定义: `Assets/SibylSystem/Program.cs`
- 模块初始化: `Assets/SibylSystem/Program.cs`
- 模块切换: `Assets/SibylSystem/Program.cs`

---

## 2. `Program` 里的资源是怎么绑定上的

当前项目不是在 `Program.cs` 里手写: 

```csharp
new_ui_menu = Resources.Load(...)
```

而是走 **Unity 场景 Inspector 序列化绑定**.

### 2.1 场景里真正挂的是一个 `loader` prefab 实例

在 `Assets/main.unity` 里, 可以看到一个 `PrefabInstance`: 

- 它的名字被改成了 `Program`
- 它的来源 prefab 是 `Assets/old/loader.prefab`

也就是说, 当前主场景里实际放着一个 **旧的 loader prefab 实例**, 上面挂着 `Program` 组件.

### 2.2 资源绑定不是写死在代码里, 而是存进场景序列化数据里

在 `Assets/main.unity` 里可以看到大量这样的内容: 

- `propertyPath: new_ui_menu`
- `propertyPath: new_ui_setting`
- `propertyPath: new_ui_selectServer`
- `propertyPath: ES_1`
- `propertyPath: new_bar_duel`

后面跟着的是一个 `objectReference`, 也就是这个字段在 Inspector 中真正拖进去的资源.

这就是 Unity 典型的: 

> "脚本定义字段, 场景负责赋值"

### 2.3 运行时的真实流程

整体流程可以理解成: 

1. `Assets/main.unity` 载入
2. 场景里的 `Program` 组件拿到所有 Inspector 绑定的资源引用
3. `Program.initializeALLservants()` 创建各个业务模块对象
4. 这些模块在自己的 `initialize()` 里使用 `Program.I().xxx` 去创建对应 UI

例如: 

- `Menu.initialize()` -> `createWindow(Program.I().new_ui_menu)`
- `SelectServer.initialize()` -> `createWindow(Program.I().new_ui_selectServer)`
- `Setting.initialize()` -> `createWindow(this, Program.I().new_ui_setting)`
- `Book.initialize()` -> `createWindow(this, Program.I().new_ui_book)`
- `Ocgcore.initialize()` -> `create(Program.I().new_ui_gameInfo, ...)`

所以 `Program` 是整个"资源供应中心".

---

## 3. `Program` 里的资源大致可以分成哪几类

为了不被几百行 public 字段吓到, 建议你先按用途理解.

### 3.1 场景/核心对象引用

这一类不是 prefab 模板, 而是主场景里已经存在的对象引用, 例如: 

- `main_camera`
- `audio`
- `light`
- `face`

这些通常是: 

- 主相机
- 全局音频源
- 场景灯光
- 头像/表情管理器

它们不是"窗口 prefab", 而是游戏运行时的基础设施.

### 3.2 对局物体与特效 prefab

这一类大多是对局层的 3D 对象、粒子、装饰和动画资源, 例如: 

- `mod_ocgcore_card`
- `mod_ocgcore_card_cloude`
- `mod_ocgcore_decoration_*`
- `mod_ocgcore_bs_atk_*`
- `mod_ocgcore_cs_*`
- `mod_ocgcore_ss_*`
- `mod_ocgcore_ol_*`
- `New_arrow`
- `New_phase`
- `New_selectKuang`
- `New_chainKuang`

这一组更偏向: 

- 卡片本体
- 高亮/闪烁/召唤/连锁/攻击特效
- 决斗中用到的场上对象

### 3.3 UI prefab 资源

这是你当前最关心的一组, 主要包括: 

- 主界面窗口: `new_ui_*`
- 工具条: `new_bar_*`
- 弹窗: `ES_*`
- 若干 remaster 界面: `remaster_*`
- 动态小部件: `new_ui_superButton`、`new_ui_handShower`、`new_ui_cardOnSearchList` 等

---

## 4. 当前项目里的 UI 资源并不是只来自一个目录

这是这份文档里最重要的一个现实结论.

当前 `Program` 绑定的 UI 资源, 实际上是 **混用三套来源**: 

### 4.1 `Assets/transUI/prefab`

这是一批"过渡态/转换态"的 UI prefab.

当前依然有不少 `Program` 字段实际绑定到这里, 例如: 

- `new_ui_menu` -> `Assets/transUI/prefab/trans_menu.prefab`
- `new_ui_setting` -> `Assets/transUI/prefab/trans_setting.prefab`
- `new_ui_book` -> `Assets/transUI/prefab/trans_book.prefab`
- `new_ui_selectServer` -> `Assets/transUI/prefab/trans_selectServer.prefab`
- `new_ui_aiRoom` -> `Assets/transUI/prefab/trans_AIroom.prefab`
- 多个 `ES_*` 弹窗 -> `Assets/transUI/prefab/trans_ES*.prefab`

也就是说: 

> 变量名虽然叫 `new_ui_xxx`, 但场景里绑定的并不一定是 `ArtSystem` 下那个"new_xxx.prefab`, 而可能还是 `transUI` 版本.

### 4.2 `Assets/ArtSystem`

这是当前项目最核心的 UI 美术与 prefab 目录.

这里面包括: 

- `MainMenu/`
- `Setting/`
- `Book/`
- `Room/`
- `serverSelect/`
- `deckManager/`
- `cardDescription/`
- `gameInfo/`
- `picShower/`
- `superButton/`
- `MsgBox/`
- `remaster/`

很多当前实际运行中的对象来自这里, 尤其是: 

- 决斗信息面板
- 卡牌描述面板
- 搜索面板
- 工具条
- 房间/录像/卡组 remaster 界面
- 动态按钮/展示类 prefab

### 4.3 `Assets/old`

`old` 不是主 UI 入口目录, 但它仍然是: 

- 某些 loader/旧资源的来源
- 某些 atlas/shader 的共享依赖来源

所以不能简单把它看成完全废弃.

---

## 5. 当前最主要的 UI 字段与真实 prefab 对照

下面只列对你理解项目结构最重要的一批.

### 5.1 主窗口 / 主要界面

| Program 字段 | 场景当前绑定的真实 prefab | 主要用途 | 主要消费者 |
| --- | --- | --- | --- |
| `new_ui_menu` | `Assets/transUI/prefab/trans_menu.prefab` | 主菜单 | `Assets/SibylSystem/Menu/Menu.cs` |
| `new_ui_setting` | `Assets/transUI/prefab/trans_setting.prefab` | 设置界面 | `Assets/SibylSystem/Setting/Setting.cs` |
| `new_ui_book` | `Assets/transUI/prefab/trans_book.prefab` | 对局日志/情报书 | `Assets/SibylSystem/Book/Book.cs` |
| `new_ui_selectServer` | `Assets/transUI/prefab/trans_selectServer.prefab` | 联机服务器/房间连接入口 | `Assets/SibylSystem/selectServer/SelectServer.cs` |
| `new_ui_aiRoom` | `Assets/transUI/prefab/trans_AIroom.prefab` | AI 对战界面 | `Assets/SibylSystem/Room/AIRoom.cs` |
| `new_ui_gameInfo` | `Assets/ArtSystem/gameInfo/new_gameInfoRemaster.prefab` | 决斗中的顶部/侧边信息 UI | `Assets/SibylSystem/Ocgcore/Ocgcore.cs` |
| `new_ui_cardDescription` | `Assets/ArtSystem/cardDescription/new_cardDescriptionRemaster.prefab` | 卡牌详情侧栏 | `Assets/SibylSystem/CardDescription/CardDescription.cs` |
| `new_ui_search` | `Assets/ArtSystem/deckManager/new_search_remaster.prefab` | 卡组编辑搜索主界面 | `Assets/SibylSystem/deckManager/DeckManager.cs` |
| `new_ui_searchDetailed` | `Assets/transUI/prefab/trans_detailSearch.prefab` | 详细检索面板 | `Assets/SibylSystem/deckManager/DeckManager.cs` |

### 5.2 动态列表项 / 小组件

| Program 字段 | 真实 prefab | 主要用途 | 主要消费者 |
| --- | --- | --- | --- |
| `new_ui_cardOnSearchList` | `Assets/ArtSystem/deckManager/new_cardOnListRemaster.prefab` | 搜索结果里的单张卡条目 | `DeckManager.itemOnListProducer()` |
| `new_ui_handShower` | `Assets/ArtSystem/picShower/new_ui_handShower.prefab` | 猜拳/抽签/手牌展示 | `Ocgcore`、`Room` |
| `new_ui_superButton` | `Assets/ArtSystem/superButton/new_superButton.prefab` | 卡片上方弹出的操作按钮 | `gameButton` |
| `new_ui_superButtonTransparent` | `Assets/ArtSystem/superButton/new_superButtonTransparent.prefab` | 透明样式操作按钮 | 动态按钮辅助 |
| `new_ui_textMesh` | `Assets/ArtSystem/Ocgcore/gameField/new_ui_textMesh.prefab` | 对局场上的文本/数字辅助对象 | 对局展示层 |

### 5.3 工具条

| Program 字段 | 真实 prefab | 主要消费者 |
| --- | --- | --- |
| `new_bar_duel` | `Assets/ArtSystem/superButton/toolBars/new_toolBar_duel.prefab` | `Ocgcore` |
| `new_bar_room` | `Assets/ArtSystem/superButton/toolBars/new_toolBar_Room.prefab` | `Room` |
| `new_bar_editDeck` | `Assets/ArtSystem/superButton/toolBars/new_toolBar_editDeck.prefab` | `DeckManager` |
| `new_bar_changeSide` | `Assets/ArtSystem/superButton/toolBars/new_toolBar_changeSide.prefab` | `DeckManager` |
| `new_bar_watchDuel` | `Assets/ArtSystem/superButton/toolBars/new_toolBar_watchDuel.prefab` | `Ocgcore` |
| `new_bar_watchRecord` | `Assets/ArtSystem/superButton/toolBars/new_toolBar_watchRecord.prefab` | `Ocgcore` |

### 5.4 选择/弹窗系统（`ES_*`）

这一组不是"某个业务界面", 而是 `Servant` 基类的通用消息系统模板.

| Program 字段 | 真实 prefab | 用途 |
| --- | --- | --- |
| `ES_1` | `Assets/transUI/prefab/trans_ES1.prefab` | 单按钮提示 |
| `ES_2` | `Assets/transUI/prefab/trans_ES2.prefab` | 是/否弹窗 |
| `ES_2Force` | `Assets/transUI/prefab/trans_ES2f.prefab` | 强制是/否弹窗 |
| `ES_3cancle` | `Assets/ArtSystem/MsgBox/ES_3cancle.prefab` | 三按钮弹窗 |
| `ES_Single_multiple_window` | `Assets/transUI/prefab/trans_ES_pan.prefab` | 单选/多选容器 |
| `ES_Single_option` | `Assets/transUI/prefab/trans_ES_singleSelection.prefab` | 单选按钮项 |
| `ES_multiple_option` | `Assets/transUI/prefab/trans_ES_multipleSelection.prefab` | 多选按钮项 |
| `ES_input` | `Assets/transUI/prefab/trans_ES_input.prefab` | 输入框弹窗 |
| `ES_position` | `Assets/transUI/prefab/trans_ES_pos.prefab` | 二选一表示形式 |
| `ES_position3` | `Assets/transUI/prefab/trans_ES_pos3.prefab` | 三选一表示形式 |
| `ES_Tp` | `Assets/transUI/prefab/trans_ES_tp.prefab` | 猜拳/先后手选择 |
| `ES_Face` | `Assets/transUI/prefab/trans_ES_face.prefab` | 头像/表情选择 |
| `ES_FS` | `Assets/transUI/prefab/trans_ES_fs.prefab` | 特殊双按钮弹窗 |

### 5.5 remaster 界面

| Program 字段 | 真实 prefab | 主要消费者 |
| --- | --- | --- |
| `remaster_deckManager` | `Assets/ArtSystem/remaster/remaster_deckManager.prefab` | `Assets/SibylSystem/SelectDeck/selectDeck.cs` |
| `remaster_replayManager` | `Assets/ArtSystem/remaster/remaster_replayManager.prefab` | `Assets/SibylSystem/selectReplay/selectReplay.cs` |
| `remaster_puzzleManager` | `Assets/ArtSystem/remaster/remaster_puzzleManager.prefab` | `Assets/SibylSystem/puzzleSystem/puzzleMode.cs` |
| `remaster_tagRoom` | `Assets/ArtSystem/remaster/remaster_Room_Tag.prefab` | `Assets/SibylSystem/Room/Room.cs` |
| `remaster_room` | `Assets/ArtSystem/Room/remaster_Room.prefab` | `Assets/SibylSystem/Room/Room.cs` |

---

## 6. 这些 UI 是如何被脚本"接上"的

这是理解整个项目最关键的"绑定链路".

### 6.1 第一步: 场景把 prefab 塞给 `Program`

这一层是: 

- `Assets/main.unity`
- 场景中的 `Program` 组件
- 通过 Inspector 序列化字段完成资源赋值

### 6.2 第二步: `Program` 创建模块对象

`Program.initializeALLservants()` 会创建: 

- `menu`
- `setting`
- `selectDeck`
- `selectReplay`
- `room`
- `cardDescription`
- `deckManager`
- `ocgcore`
- `selectServer`
- `book`
- `puzzleMode`
- `aiRoom`

这些大多是普通 C# 对象, 不是直接挂在场景里的 `MonoBehaviour`.

### 6.3 第三步: 模块在自己的 `initialize()` 里创建 UI

典型例子: 

#### 主菜单

- 逻辑模块: `Assets/SibylSystem/Menu/Menu.cs`
- UI 来源: `Program.I().new_ui_menu`
- 创建方式: `createWindow(Program.I().new_ui_menu)`
- 事件绑定: 
  - `setting_`
  - `deck_`
  - `online_`
  - `replay_`
  - `single_`
  - `ai_`
  - `exit_`

也就是说: 

- prefab 只是长相和节点树
- 真正的按钮逻辑由 `Menu.cs` 在运行时通过 `UIHelper.registEvent(...)` 绑定

#### 联机选择界面

- 逻辑模块: `Assets/SibylSystem/selectServer/SelectServer.cs`
- UI 来源: `Program.I().new_ui_selectServer`
- 创建方式: `createWindow(Program.I().new_ui_selectServer)`
- 节点绑定: 
  - `exit_`
  - `face_`
  - `join_`
  - `history_`
  - `ip_`、`port_`、`psw_`、`version_`

#### 设置界面

- 逻辑模块: `Assets/SibylSystem/Setting/Setting.cs`
- UI 来源: `Program.I().new_ui_setting`
- 创建方式: `createWindow(this, Program.I().new_ui_setting)`
- 重要子脚本: `LAZYsetting`
- 绑定方式: 
  - 通过 `UIHelper.getByName<UIToggle / UISlider / UIInput>()` 找控件
  - 再通过 `UIHelper.registEvent(...)` 绑定逻辑

#### 书本/日志界面

- 逻辑模块: `Assets/SibylSystem/Book/Book.cs`
- UI 来源: `Program.I().new_ui_book`
- 重要子脚本: `lazyBookbtns`

#### 卡牌描述侧栏

- 逻辑模块: `Assets/SibylSystem/CardDescription/CardDescription.cs`
- UI 来源: `Program.I().new_ui_cardDescription`
- 绑定特征: 
  - 运行时动态 `AddComponent<cardPicLoader>()`
  - 使用 prefab 里已有的 `UIDragResize`、`UIDeckPanel`、`UITexture`、`UITextList`
  - 再通过 `UIHelper.registEvent(...)` 给按钮绑 `onPre`、`onNext`、`onb`、`ons`

#### 卡组搜索界面

- 逻辑模块: `Assets/SibylSystem/deckManager/DeckManager.cs`
- UI 来源: 
  - `Program.I().new_ui_search`
  - `Program.I().new_ui_searchDetailed`
  - `Program.I().new_ui_cardOnSearchList`
- 绑定方式: 
  - 整个搜索面板由 `DeckManager` 直接控制
  - 列表项 prefab 在 `itemOnListProducer()` 里动态创建
  - 列表项上再动态挂 `cardPicLoader`

#### 对局信息条

- 逻辑模块: `Assets/SibylSystem/Ocgcore/Ocgcore.cs`
- UI 来源: `Program.I().new_ui_gameInfo`
- 重要脚本: `gameInfo`
- 创建方式: `create(...).GetComponent<gameInfo>()`

#### 动态操作按钮

- 逻辑模块: `Assets/SibylSystem/Ocgcore/OCGobjects/gameButton.cs`
- UI 来源: `Program.I().new_ui_superButton`
- 重要脚本: `iconSetForButton`
- 用法: 卡片 hover 后, 把动作按钮动态生成到卡片上方

#### 手牌展示 / 猜拳展示

- UI 来源: `Program.I().new_ui_handShower`
- 重要脚本: `handShower`
- 使用方: `Ocgcore`、`Room`

---

## 7. 如果从"UI prefab 自身结构"看, 常见的绑定方式是什么

这个项目不是"把全部逻辑都写进 prefab 挂载脚本"那种风格.

更常见的是: 

1. prefab 里放好节点结构、贴图、UILabel、UIButton、UITexture 等 NGUI 控件
2. 业务模块在 `initialize()` 后: 
   - 通过名字找节点
   - 通过 `UIHelper.registEvent` 绑事件
   - 通过 `UIHelper.trySetLableText` 改文案
   - 通过 `UIHelper.InterGameObject` 做国际化
3. 少数 prefab 再配合一些辅助脚本: 
   - `LAZYsetting`
   - `lazyBookbtns`
   - `gameInfo`
   - `handShower`
   - `iconSetForButton`
   - `MonoCardInDeckManager`
   - `cardPicLoader`

所以你读 prefab 的时候要有一个预期: 

> prefab 本身经常只是"样子 + 局部组件", 真正的业务逻辑很多是在外部 `Servant` / `WindowServant` 里绑进去的.

---

## 8. 当前 UI 美术/图像资源一般在哪里找

这部分也建议按目录来找, 不要只盯 prefab.

### 8.1 `Assets/ArtSystem/MainMenu`

里面有: 

- `new_main_menu.prefab`
- `new_main_menuR.prefab`
- `MainMenuAtlas.png`
- `MainMenuAtlas.mat`
- `MainMenuAtlas.prefab`

所以主菜单相关图像资源, 优先看这里.

### 8.2 `Assets/ArtSystem/Setting`

里面有: 

- `new_setting.prefab`
- `new_setting_R.prefab`
- `LAZYsetting.cs`
- `lines.png`

### 8.3 `Assets/ArtSystem/cardDescription`

里面有: 

- `new_cardDescription.prefab`
- `new_cardDescriptionRemaster.prefab`
- `UImouseHint.cs`
- 多张局部图片

### 8.4 `Assets/ArtSystem/superButton`

里面有: 

- `new_superButton.prefab`
- `new_superButtonTransparent.prefab`
- `iconSetForButton.cs`
- `icons/`

### 8.5 `Assets/ArtSystem/gameInfo`

里面有: 

- `new_gameInfo.prefab`
- `new_gameInfoRemaster.prefab`
- `gameInfo.cs`
- `new_btnOnInfo.prefab`
- `new_mod_healthBar.prefab`

### 8.6 `Assets/ArtSystem/deckManager`

里面有: 

- `new_search.prefab`
- `new_searchDetailed.prefab`
- `new_searchDetailed_r2.prefab`
- `new_cardOnList.prefab`
- `new_cardOnListRemaster.prefab`
- `MonoCardInDeckManager.cs`
- `cardPicLoader.cs`

### 8.7 `Assets/transUI/prefab`

这一层也非常重要, 因为当前场景里很多主界面和弹窗实际还绑在这里: 

- `trans_menu.prefab`
- `trans_setting.prefab`
- `trans_book.prefab`
- `trans_selectServer.prefab`
- `trans_AIroom.prefab`
- `trans_ES*.prefab`

所以当你发现: 

- 代码里叫 `new_ui_menu`
- 但实际打开的 UI 长得像旧风格

不要奇怪, 因为场景绑定现在就是这么配的.

---

## 9. 当前 `Program` 绑定里还能看到一些"历史遗留字段"

在 `Assets/main.unity` 的 loader prefab 覆盖里, 还能看到一些并不在当前 `Program.cs` 字段定义里的名字, 例如: 

- `nem_ui_menu`
- `new_ui_room`
- `new_ui_roomTag`
- `new_msg_hint`
- `new_msg_inputWindow`
- `new_msg_selectWindow`
- `new_msg_singleOption`
- `new_msg_selectWindowSmall`
- `new_msg_singleOptionSmall`
- `new_ui_faceShower`
- `new_ui_selectDeck`
- `new_ui_selectDeck_sort`
- `new_ui_selectReplayOnList`
- `new_ui_serverOnList`
- `remaster_selection`

这通常说明: 

1. `loader.prefab`/场景序列化历史比当前源码更老
2. 程序经过多轮 UI 改造
3. 一部分资源命名和字段重构过, 但场景里仍残留旧序列化痕迹

所以你在读这个项目时, **不要假设"字段名 = 当前最终资源名"**.

---

## 10. 你应该怎么阅读 `Program`

我建议按下面顺序看: 

### 第一步: 把它当"总控 + 资源表"来看

先不要把它理解成"所有对象逻辑都在这里".

### 第二步: 只记住三件事

- 资源引用在这里
- 模块初始化在这里
- 模块切换也在这里

### 第三步: 想追哪个 UI, 就追这条链

以主菜单为例: 

1. `Program.new_ui_menu`
2. `Menu.initialize()`
3. `createWindow(Program.I().new_ui_menu)`
4. `UIHelper.registEvent(...)`
5. prefab 里的节点名与按钮名对上

### 第四步: 区分"逻辑脚本"和"prefab 本体"

比如: 

- `Menu.cs` 是逻辑控制器
- `trans_menu.prefab` 是界面模板
- `UIHelper` 负责把按钮节点和逻辑函数连接起来

这三层不能混着看.

---

## 11. 最后给你的一个总判断

如果你现在问: 

> "我项目里当前主要 UI 到底在哪里？"

最准确的回答是: 

- **逻辑入口主要在** `Assets/SibylSystem/*`
- **资源注册入口主要在** `Assets/SibylSystem/Program.cs`
- **当前实际 UI prefab 来源是混合的**: 
  - 一部分在 `Assets/transUI/prefab`
  - 一部分在 `Assets/ArtSystem/*`
  - 少量历史依赖还会牵到 `Assets/old`

如果你接下来想继续梳理项目, 我建议下一步优先做这两件事: 

1. 以 `Menu -> SelectServer -> Room -> Ocgcore` 为主线, 继续把 UI 跳转链梳理出来
2. 把 `Program` 里的 UI 字段按"当前实际使用 / 历史残留 / 对局专用 / 通用弹窗"再细分一遍
