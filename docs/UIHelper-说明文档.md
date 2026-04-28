# UIHelper 说明文档

本文档面向刚接触 Unity 和这个项目的开发者, 专门解释 `Assets/SibylSystem/MonoHelpers/UIHelper.cs` 这个类在项目里的作用、常见用法、和其他模块的关系, 以及后续可以优化的方向.

如果你刚开始看这个项目, 可以先记住一句话:

> `UIHelper` 不是一个"单一职责"的类, 它更像一个把很多 UI 相关常用操作打包在一起的工具箱.

---

## 1. 它是什么

`UIHelper` 是一个静态工具类:

- 它不是挂在场景里的 `MonoBehaviour`
- 不需要 `new UIHelper()`
- 里面的方法一般直接通过 `UIHelper.方法名(...)` 调用

例如:

```csharp
UIHelper.registEvent(panel, "btn_ok", OnClickOk);
UIHelper.trySetLableText(panel, "title", "欢迎来到房间");
Texture2D avatar = UIHelper.getFace("player001");
```

从代码内容来看, 它主要负责这些事情:

1. Windows 桌面窗口控制
2. NGUI 控件查找
3. NGUI 事件注册
4. 文本设置与国际化辅助
5. 图片读取、头像缓存、场地贴图切分
6. 一些布局计算和坐标换算
7. 一些与 UI 强相关的杂项逻辑, 比如音效播放、设置父节点等

所以它并不是"只管 UI 显示", 而是一个广义上的"界面和交互辅助中心".

---

## 2. 它在项目中的定位

你可以把它理解成下面这种角色:

- `Program`:整个客户端总控器 / 全局入口
- `UIHelper`:给各个界面和交互逻辑提供通用工具
- 具体的 UI 模块:调用 `UIHelper` 来少写重复代码

换句话说, `UIHelper` 自己不直接决定业务规则, 但很多模块都会依赖它完成这些重复操作:

- 找某个按钮
- 找某个 `UILabel`
- 给按钮绑定点击事件
- 给输入框绑定提交事件
- 加载头像
- 给一棵 UI 树做国际化文本替换

它的优点是"方便", 缺点是"职责有点多".

---

## 3. 你需要先知道的基础概念

### 3.1 Helper 是什么

Helper 就是"工具类".

当很多地方都会重复写同一类代码时, 就会把它们集中到一个类里, 方便复用.例如:

- 查找子节点
- 读图片
- 绑事件
- 改材质透明模式

### 3.2 `static class` 是什么

`UIHelper` 是静态类, 意思是:

- 它本身不创建实例
- 方法是全局直接调用的
- 更像一个工具箱, 而不是一个"对象"

所以你会看到这种写法:

```csharp
UIHelper.Flash();
UIHelper.getByName<UILabel>(panel, "name");
```

而不会看到:

```csharp
var helper = new UIHelper();
```

### 3.3 这个项目主要在用 NGUI

`UIHelper` 里面很多类型都不是 Unity 新版常见的 uGUI, 而是 NGUI, 例如:

- `UIButton`
- `UILabel`
- `UIInput`
- `UIToggle`
- `UIPopupList`
- `UIScrollBar`
- `UIScrollView`
- `UIEventTrigger`

所以如果你拿 Unity 官方新教程对照着看, 会觉得名字有点陌生.这不是你看错了, 而是这个项目使用的是老一代常见的 NGUI 体系.

---

## 4. 它和其他模块之间的关系

这一部分是最重要的, 因为理解依赖关系以后, 你看代码会顺很多.

### 4.1 和 `Program` 的关系

`UIHelper` 很依赖 `Assets/SibylSystem/Program.cs`.

主要体现在以下几方面:

#### 1）访问全局单例

项目里通过 `Program.I()` 取得当前全局入口对象.

`UIHelper` 在很多地方通过它拿资源和模块, 例如:

- `Program.I().setting`
- `Program.I().ocgcore`
- `Program.I().face`
- `Program.I().destroy(...)`

#### 2）访问主相机

`UIHelper` 里有一些屏幕坐标和世界坐标换算, 需要用到主相机:

- `getCamGoodPosition`
- `getScreenDistance`

它们依赖 `Program.camera_game_main`.

#### 3）播放音效时创建和销毁对象

`playSound` 方法里会:

- 通过 `Program.I().ocgcore.create_s(...)` 创建音效对象
- 再通过 `Program.I().destroy(audio_helper, 5f)` 延迟销毁

这说明 `UIHelper` 不是完全自给自足, 它借助 `Program` 调用了全局资源和生命周期管理.

---

### 4.2 和 `InterString` 的关系

`UIHelper` 的国际化能力来自 `Assets/SibylSystem/InterString.cs`.

比如:

- `InterGameObject` 会扫描界面中的 `UILabel`
- 再调用 `InterString.Get(...)` 替换成当前语言文本

`InterString` 的工作方式大致是:

- 从语言文件读入翻译表
- 用原始文本作为 key
- 找到翻译后返回新文本
- 如果没有翻译, 还会把原始文本补写进翻译文件

所以从职责上讲:

- `UIHelper` 负责"找出哪些文字要翻译"
- `InterString` 负责"真正做翻译"

---

### 4.3 和 `MonoDelegate` / `MonoListener` 的关系

相关文件:`Assets/SibylSystem/MonoHelpers/MonoDelegate.cs`

这几个类很关键:

- `MonoDelegate`
- `MonoListener`
- `MonoListenerRMS_ized`

它们的作用是把普通的 C# 回调包装成能被 NGUI `EventDelegate` 调用的方法.

简单理解如下:

#### `MonoDelegate`

保存一个 `Action`, 点击后执行.

#### `MonoListener`

保存一个 `Action<GameObject>`, 执行时会把当前对象本身传回去.

#### `MonoListenerRMS_ized`

保存一个 `Action<GameObject, Servant.messageSystemValue>`, 适合输入框提交、弹窗确认等需要额外上下文的场景.

所以 `UIHelper.registEvent(...)` 这一类方法, 并不是直接把 lambda 塞进 NGUI, 而是:

1. 先给按钮或输入框挂一个桥接组件
2. 把 Action 存到桥接组件里
3. 再把桥接组件的方法绑到 `EventDelegate`

这就是为什么你会看到很多地方都在:

- `GetComponent<MonoDelegate>()`
- 如果没有就 `AddComponent<MonoDelegate>()`

---

### 4.4 和 `Ocgcore` 的关系

`playSound` 里会调用:

- `Program.I().ocgcore.create_s(...)`

这说明 `Ocgcore` 不只是对局逻辑, 也承担了一部分运行时对象创建职责.

另外 `playSound` 还会判断:

- `Ocgcore.inSkiping`

也就是如果当前在跳过状态, 就不播放音效.

所以从依赖关系上说, `UIHelper` 里有一部分逻辑是和对局状态耦合的.

---

### 4.5 和配置系统的关系

`shouldMaximize()` 会调用:

- `Config.Get("maximize_", "0")`

说明 `UIHelper` 不只是处理界面表现, 还会读取配置项决定窗口行为.

---

### 4.6 和文件系统的关系

`UIHelper` 里有不少同步文件访问, 例如:

- 扫描 `texture/face`
- 读取 png 图片
- 查找音效文件 `sound/*.mp3/.wav/.ogg`

所以它还有一点"简易资源加载器"的味道.

---

## 5. 按功能分类详细说明

---

## 5.1 Windows 窗口控制

相关方法:

- `Flash()`
- `isMaximized()`
- `MaximizeWindow()`
- `RestoreWindow()`
- `shouldMaximize()`

### 它们是干什么的

这部分功能只在 Windows 桌面平台有意义, 用来:

- 让窗口闪烁提醒玩家
- 判断当前窗口是否最大化
- 把窗口最大化
- 把窗口恢复成普通大小
- 根据配置决定是否开局时最大化

### 新手怎么理解

可以把它想成是:

> 通过系统 API 去控制 Unity 程序所在的桌面窗口, 而不是控制 Unity 场景里的 UI 面板.

### 对你当前环境的提醒

你现在是在 macOS 上阅读代码.

所以这部分逻辑:

- 不一定会实际生效
- 看到 `#if UNITY_STANDALONE_WIN` 很正常
- 不是代码坏了, 而是平台不一样

---

## 5.2 材质透明模式切换

相关内容:

- `RenderingMode` 枚举
- `SetMaterialRenderingMode(Material material, RenderingMode renderingMode)`

### 功能

这个方法用来统一设置 Unity 材质的透明模式, 例如:

- `Opaque`:不透明
- `Cutout`:裁切透明
- `Fade`:普通半透明
- `Transparent`:预乘透明

### 为什么有用

Unity 标准材质在切换透明模式时, 通常要同时设置很多参数, 例如:

- `_SrcBlend`
- `_DstBlend`
- `_ZWrite`
- `Shader keyword`
- `renderQueue`

新手手动设置容易漏项, 所以这里封装成一个方法, 直接按"意图"切换.

### 常见用法

```csharp
Material mat = someRenderer.material;
UIHelper.SetMaterialRenderingMode(mat, UIHelper.RenderingMode.Fade);
```

这比你自己记一堆底层参数简单很多.

---

## 5.3 控件查找

相关方法:

- `getByName<T>(GameObject father, string name)`
- `getByName(GameObject father, string name)`
- `getByName<T>(GameObject father)`
- `getLabelName(GameObject father, string name)`
- `getGameObject(GameObject gameObject, string name)`

### 5.3.1 `getByName<T>(father, name)`

作用:

- 在 `father` 的所有子节点里找"名字等于 `name`"的指定类型组件

例如:

```csharp
UIButton btn = UIHelper.getByName<UIButton>(panel, "btn_ok");
UILabel label = UIHelper.getByName<UILabel>(panel, "title");
```

### 它的优点

- 不用在 Inspector 里拖很多引用
- 业务层写起来很快
- 对老项目和快速开发很方便

### 它的缺点

- 每次都要遍历整棵子树
- 强依赖节点名字不能乱改
- 如果有多个同名节点, 结果可能不稳定

### 一个容易忽略的细节

它返回的是"最后一个匹配项", 不是"第一个匹配项".

这意味着如果你的 UI 树里有多个同名控件, 结果不一定符合直觉.

---

### 5.3.2 `getByName(father, name)`

作用:

- 按名字找 `GameObject`

例如:

```csharp
GameObject cardItem = UIHelper.getByName(panel, "card_item");
```

---

### 5.3.3 `getByName<T>(father)`

作用:

- 找子树中的第一个指定类型组件

适合场景结构比较稳定、而且只有一个该类型组件时使用.

---

### 5.3.4 `getLabelName(father, name)`

这是一个更"宽松"的 `UILabel` 查找函数.

它不只比较:

- `UILabel` 自己的名字

还会比较:

- 它父节点名字
- 祖父节点名字
- 曾祖父节点名字

### 为什么会这么写

说明这个项目里很多文本控件嵌套很深, 业务层更关心的是"逻辑块名字", 而不是最深层那个 `UILabel` 的真实名字.

### 新手理解方式

比如:

- `title`
  - `Container`
    - `Label`

业务代码想找的是"标题这块", 而不是最里面那个 `Label` 节点, 因此这里用一个更宽松的方法去兼容老结构.

---

## 5.4 文本与国际化

相关方法:

- `InterGameObject(GameObject father)`
- `trySetLableText(GameObject father, string name, string text)`
- `tryGetLableText(GameObject father, string name)`
- `trySetLableText(GameObject gameObject, string p)`
- `trySetLableTextList(GameObject father, string text)`

### 5.4.1 `InterGameObject`

作用:

- 扫描一棵界面树里的所有 `UILabel`
- 对符合规则的项执行国际化替换

被替换的规则是:

- 名字以 `!` 开头
- 名字是 `yes_`
- 名字是 `no_`

然后把它们当前的 `text` 传给 `InterString.Get(...)`.

### 你可以怎么理解

这是一种"命名约定驱动"的国际化方案:

- 某些标签只要符合命名规则
- 就自动被视为"需要翻译"的文字

### 适用场景

界面刚创建完, 可以来一句:

```csharp
UIHelper.InterGameObject(windowRoot);
```

这样这棵 UI 树里的可翻译标签就都会被处理.

---

### 5.4.2 `trySetLableText`

作用:

- 找到某个 `UILabel`
- 找到就设置文本
- 找不到就写调试日志

例如:

```csharp
UIHelper.trySetLableText(panel, "title", "设置界面");
```

### 关于拼写

这里方法名里的 `Lable` 是拼写错误, 标准写法是 `Label`.

不过这是命名历史问题, 不影响运行.

---

### 5.4.3 `tryGetLableText`

作用:

- 读取某个 `UILabel` 的文字
- 找不到就返回空字符串

---

### 5.4.4 `trySetLableTextList`

作用:

- 找到 `UITextList`
- 清空旧内容
- 再追加新文本

适合聊天、日志框、滚动文本等场景.

---

## 5.5 事件注册

这是 `UIHelper` 最重要的一组方法.

相关方法:

- `registEvent(UIButton btn, Action function)`
- `registEvent(GameObject father, string name, Action function)`
- `registEvent(GameObject father, string name, Action<GameObject, Servant.messageSystemValue> function, ...)`
- `registEventbtn(...)`
- `registClickListener(...)`
- `registEvent(UIScrollView, Action)`
- `registEvent(UIScrollBar, Action)`
- `registUIEventTriggerForClick(...)`
- `registUIEventTriggerForHoverOver(...)`
- `registUIEventTriggerForMouseDown(...)`
- `addButtonEvent_toolShift(...)`

### 5.5.1 `registEvent(UIButton btn, Action function)`

作用:

- 给一个按钮注册点击事件

内部过程大致是:

1. 找或添加 `MonoDelegate`
2. 把你的 `Action` 保存进去
3. 清掉原来的 `onClick`
4. 再挂一个新的 `EventDelegate`

### 重点提醒

它不是"追加监听", 更接近"覆盖监听", 因为里面用了:

- `btn.onClick.Clear()`

这意味着:

- 如果这个按钮之前已经绑定过别的事件
- 这里会把它们清掉

这是这个 Helper 很重要的行为特征.

---

### 5.5.2 `registEvent(father, name, Action function)`

这是最常用的版本.

它会根据控件类型自动判断该绑定哪个事件, 支持:

- `UISlider` -> `onChange`
- `UIPopupList` -> `onChange`
- `UIToggle` -> `onChange`
- `UIInput` -> `onSubmit`
- `UIScrollBar` -> `onChange`
- `UIButton` -> `onClick`

### 为什么它很方便

业务层只需要写:

```csharp
UIHelper.registEvent(panel, "btn_start", () =>
{
    Debug.Log("开始");
});
```

不用自己手动:

- 找组件
- 判断组件类型
- 添加桥接组件
- 绑定 `EventDelegate`

### 但它也有代价

1. 强依赖节点名字
2. 强依赖真实组件类型
3. 每次都要做一次遍历查找
4. 某些控件会清掉旧事件

---

### 5.5.3 `registEvent(..., Action<GameObject, Servant.messageSystemValue> function, ...)`

这是一个"带上下文值"的事件注册版本.

适合:

- 输入框提交
- 消息框确认
- 需要额外把 `messageSystemValue` 一起带出去的业务场景

如果目标是 `UIInput`, 它会挂到 `onSubmit`；如果传了第二个按钮名 `name2`, 还会把同一逻辑绑到按钮点击上.

这类写法属于项目定制逻辑, 新手不用一次全记住, 知道它是"带参数版的注册"就够了.

---

### 5.5.4 `registEventbtn`

作用:

- 明确告诉系统"我就是要给按钮绑点击事件"

这个方法比通用版更直白.

---

### 5.5.5 `registClickListener`

作用:

- 点击后, 把当前点击对象本身传给回调

例如:

```csharp
UIHelper.registClickListener(panel, "item_1", go =>
{
    Debug.Log(go.name);
});
```

这个版本特别适合:

- 动态列表项
- 多个相似按钮共用一套逻辑

---

### 5.5.6 `addButtonEvent_toolShift`

作用:

- 给按钮点击时先执行 `toolShift.shift`
- 再执行你的自定义回调

这一般用于:

- 先做一个视觉切换 / 动效 / 状态切换
- 再做业务逻辑

---

### 5.5.7 `registEvent(UIScrollView, Action)` 和 `registEvent(UIScrollBar, Action)`

这两个是滚动相关事件绑定.

---

### 5.5.8 `registUIEventTriggerForClick / HoverOver / MouseDown`

这些方法通过 `UIEventTrigger` 给带 `BoxCollider` 的对象注册事件.

### 一个重要细节

这里不是直接用你传入的根节点, 而是:

- 去找它子节点里的 `BoxCollider`
- 事件真正挂在碰撞体所在的节点上

这是 NGUI 项目里很常见的做法, 因为实际接收鼠标交互的通常是带碰撞体的对象.

---

### 5.5.9 这些事件绑定是不是基本都针对 NGUI

答案是:

> 是, `UIHelper` 里这一整组事件注册方法, 绝大多数都是围绕 NGUI 设计的.

原因很简单, 这些方法直接使用了大量 NGUI 类型和 NGUI 事件系统, 例如:

- `UIButton`
- `UIInput`
- `UIToggle`
- `UIPopupList`
- `UIScrollBar`
- `UIScrollView`
- `UIEventTrigger`
- `EventDelegate`

也就是说, 这套 Helper 的底层假设是:

1. 控件是 NGUI 控件
2. 事件系统是 NGUI 的 `EventDelegate`
3. 鼠标命中很多时候靠 `BoxCollider` + `UICamera`
4. 业务层通过名字去查 NGUI 组件, 再绑 NGUI 事件

所以如果你把界面改成 Unity 官方的 uGUI / UGUI, 这些方法不能直接照搬.

它们不是“稍微改个命名空间就能继续用”, 而是需要换一套绑定思路.

---

### 5.5.10 如果界面改成 UGUI, 应该怎么理解“对应关系”

你可以先把 NGUI 和 UGUI 对应起来:

- `UIButton` -> `UnityEngine.UI.Button`
- `UILabel` -> `UnityEngine.UI.Text` 或 `TMPro.TextMeshProUGUI`
- `UIInput` -> `UnityEngine.UI.InputField` 或 `TMPro.TMP_InputField`
- `UIToggle` -> `UnityEngine.UI.Toggle`
- `UISlider` -> `UnityEngine.UI.Slider`
- `UIScrollBar` -> `UnityEngine.UI.Scrollbar`
- `UIScrollView` -> `UnityEngine.UI.ScrollRect`
- `UITexture` / `UISprite` -> `RawImage` / `Image`
- `UIEventTrigger` -> `UnityEngine.EventSystems.EventTrigger` 或 `IPointerXXXHandler`
- `EventDelegate` -> `UnityEvent.AddListener(...)`

但要注意:

> “组件名能对上” 不等于 “Helper 可以直接复用”.

因为 `UIHelper.registEvent(...)` 里面的核心不是只有“找控件”, 还包括:

- 添加 `MonoDelegate`
- 绑定到 `EventDelegate`
- 使用 NGUI 的 `onClick / onSubmit / onChange`
- 使用 `UIEventTrigger.onHoverOver / onPress`

这些在 UGUI 里都不是同一套 API.

---

### 5.5.11 如果你想保留 `UIHelper` 的使用习惯, 最好的改法是什么

最现实的做法不是“继续让旧方法同时支持 NGUI 和 UGUI 的所有细节”, 而是:

> 保留 `UIHelper` 这个“统一入口”的思想, 但另外写一层 UGUI 版本的绑定方法.

也就是说, 不要硬改原来的 NGUI 方法, 而是新增一组更清晰的方法, 例如:

- `registerButtonClickUGUI(...)`
- `registerToggleChangedUGUI(...)`
- `registerInputSubmitUGUI(...)`
- `registerPointerClickUGUI(...)`
- `registerPointerEnterUGUI(...)`

这样做的好处是:

1. 老代码不动, 风险小
2. 新 UI 可以逐步迁移
3. 不会让一个方法里堆满 `if (Button) else if (UIButton)` 这种混合判断
4. 阅读时一眼就知道当前界面到底是 NGUI 还是 UGUI

这是旧项目迁移里最稳的一种方式.

---

### 5.5.12 UGUI 版本大概会怎么写

如果换成 UGUI, 核心思路会从:

- `EventDelegate`
- `MonoDelegate`
- `UIEventTrigger`

改成:

- `Button.onClick.AddListener`
- `Toggle.onValueChanged.AddListener`
- `Slider.onValueChanged.AddListener`
- `InputField.onEndEdit.AddListener`
- `EventTrigger` 或 `IPointerEnterHandler / IPointerClickHandler / IPointerDownHandler`

例如, NGUI 里这类代码:

```csharp
UIHelper.registEvent(panel, "btn_start", OnClickStart);
```

在 UGUI 里更像是:

```csharp
Button btn = panel.transform.Find("btn_start").GetComponent<Button>();
btn.onClick.RemoveAllListeners();
btn.onClick.AddListener(OnClickStart);
```

如果你还想保留“按名字找 + 一句绑定”的风格, 可以做一个 UGUI 版 Helper:

```csharp
public static void RegisterButtonClickUGUI(GameObject father, string name, Action function)
{
   var button = father.transform.Find(name)?.GetComponent<UnityEngine.UI.Button>();
   if (button == null) return;
   button.onClick.RemoveAllListeners();
   button.onClick.AddListener(() => function());
}
```

这个写法在思想上就和原来的 `registEvent` 很接近了.

---

### 5.5.13 UGUI 下“悬停 / 按下 / 点击”该怎么替代

原来的 NGUI 写法:

- `registUIEventTriggerForClick(...)`
- `registUIEventTriggerForHoverOver(...)`
- `registUIEventTriggerForMouseDown(...)`

它依赖的是:

- `BoxCollider`
- `UIEventTrigger`
- `UICamera`

如果换成 UGUI, 常见有两种做法.

#### 做法一:使用 UGUI 自带 `EventTrigger`

适合:

- 快速迁移旧项目
- 先把功能跑通

思路是:

- 挂 `EventTrigger`
- 给 `PointerClick` / `PointerEnter` / `PointerDown` 添加回调

#### 做法二:自己写 `IPointer...Handler`

适合:

- 后续长期维护
- 希望性能更稳定
- 希望代码更清晰

例如:

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public class UGUIHoverListener : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerClickHandler
{
   public System.Action<GameObject> onEnter;
   public System.Action<GameObject> onDown;
   public System.Action<GameObject> onClick;

   public void OnPointerEnter(PointerEventData eventData)
   {
      onEnter?.Invoke(gameObject);
   }

   public void OnPointerDown(PointerEventData eventData)
   {
      onDown?.Invoke(gameObject);
   }

   public void OnPointerClick(PointerEventData eventData)
   {
      onClick?.Invoke(gameObject);
   }
}
```

这其实就相当于把原来的 `MonoListener + UIEventTrigger` 思路, 改写成 UGUI 版桥接组件.

---

### 5.5.14 哪些 `UIHelper` 思想还能保留, 哪些要重写

可以保留的思想:

- 统一封装“按名字找控件并绑定事件”
- 统一封装“设置文本”
- 统一封装“界面树国际化替换”
- 统一封装“播放音效 / 图片读取 / 布局计算”

基本要重写的部分:

- 所有直接出现 `UIButton / UILabel / UIInput / UIToggle / UIScrollView / UIScrollBar / UIEventTrigger / EventDelegate` 的方法
- 所有依赖 `UICamera` 和 NGUI 命中系统的逻辑
- 所有依赖 `BoxCollider` 作为 UI 事件载体的逻辑

简单说:

> `UIHelper` 这个类名可以保留, 但里面“事件系统那一半”如果迁到 UGUI, 基本要重做.

---

### 5.5.15 对你现在这个项目, 更推荐的迁移策略

如果你准备把项目逐步转向 UGUI, 我更建议按下面顺序来:

#### 第一步:先分层

先把 `UIHelper` 的方法按三类理解:

1. 和 NGUI 强绑定的方法
2. 和资源 / 文件 / 音效有关的方法
3. 和布局 / 数学计算有关的方法

其中:

- 第 2 类和第 3 类大多还能继续用
- 第 1 类需要单独拆出来

#### 第二步:新增一个 UGUI 专用 Helper

例如新建一个类:

- `UGUIHelper.cs`

里面先只放最常见的几个方法:

- 绑定 `Button`
- 绑定 `Toggle`
- 绑定 `InputField`
- 绑定 `EventTrigger` 或 Pointer 事件
- 设置 `Text` / `TMP_Text`

#### 第三步:新界面只写 UGUI, 旧界面不强改

这是最稳的方式.

因为旧项目里“强行一次性全改”通常最容易出问题:

- 预制体引用会断
- 层级路径会变
- 输入事件会变
- 鼠标命中行为会变
- 某些看起来只是按钮的对象, 实际上还夹杂了 3D / Collider / 特效逻辑

#### 第四步:等新旧两套都稳定后, 再考虑抽象统一接口

到那时候你再决定要不要做一个更高层的统一接口, 比如:

- `BindClick(...)`
- `BindHover(...)`
- `SetText(...)`

然后底层再按 NGUI / UGUI 分开实现.

这样比一开始就强行“兼容双框架”更容易维护.

---

## 5.6 布局与位置计算

相关方法:

- `get_hang_lie`
- `get_hang_lieArry`
- `get_zuihouyihangdegeshu`
- `get_shifouzaizuihouyihang`
- `get_zonghangshu`
- `get_decklieshuArray`
- `get_decklieshu`
- `get_left_right_index`
- `get_left_right_indexZuo`
- `get_left_right_indexEnhanced`

这些名字看起来比较口语化, 但本质上是在做一件事:

> 帮界面或卡牌列表计算"排到第几行第几列"和"横向均匀分布的位置".

### 常见理解方式

#### `get_hang_lie(index, meihangdegeshu)`

把一个一维索引转成二维网格坐标.

例如:

- 第 7 个元素
- 每行 4 个
- 那它在哪一行哪一列

#### `get_left_right_index(left, right, i, count)`

在 `left` 到 `right` 区间内, 均匀摆放 `count` 个元素, 求第 `i` 个元素的坐标.

这类方法一般用于:

- 手牌排布
- 卡组列表排布
- 均匀分布按钮

### 对新手的建议

第一次读这些方法时, 不要纠结中文拼音命名, 先抓核心:

- 是不是在算网格
- 是不是在算平均分布
- 是不是在算最后一行数量

抓住用途以后再看实现, 会轻松很多.

---

## 5.7 贴图读取、头像缓存、场地切图

相关方法:

- `sliceField(Texture2D textureField_)`
- `ScaleTexture(Texture2D source, int targetWidth, int targetHeight)`
- `iniFaces()`
- `getFace(string name)`
- `getTexture2D(string path)`

### 5.7.1 `getTexture2D`

作用:

- 从磁盘读取图片文件
- 转成 `Texture2D`

流程很直接:

1. 判断文件是否存在
2. 打开 `FileStream`
3. 读二进制
4. `Texture2D.LoadImage(data)`

### 优点

- 简单直观
- 不依赖复杂资源系统

### 缺点

- 同步 IO
- 不适合在高频逻辑里反复调用

如果某个界面每次刷新都现场读文件, 就容易卡.

---

### 5.7.2 `iniFaces`

作用:

- 扫描 `texture/face` 目录
- 把所有 png 头像预读到 `faces` 字典里

这是典型的"预加载缓存"策略:

- 启动时成本高一点
- 运行时更快一些

---

### 5.7.3 `getFace`

作用:

- 优先从 `faces` 字典获取头像
- 如果没有对应名字, 就根据名字字节和取模, 给一个稳定的默认头像

这是一种挺实用的降级策略, 因为它避免了"头像不存在时 UI 直接空掉".

---

### 5.7.4 `sliceField`

作用:

- 把场地图切成三块:左 / 中 / 右

处理过程:

1. 先缩放成固定尺寸
2. 根据比例算出左右边界
3. 对每个像素分别写入三张新图中

### 新手理解

这其实是一个"比较重"的 CPU 像素操作, 适合初始化时做, 不适合频繁反复调用.

---

## 5.8 显示控制与 Toggle 交互状态

相关方法:

- `shiftButton(UIButton btn, bool enabled)`
- `shiftUIToggle(UIToggle tog, bool canClick, bool canChange, string hint)`

### 5.8.1 `shiftButton`

作用:

- 通过修改 `localScale` 为 `(1,1,1)` 或 `(0,0,0)`, 让按钮看起来显示或隐藏

### 一个很重要的点

这不是 `SetActive(false)`.

也就是说:

- 对象没有被销毁
- 对象没有真的失活
- 它只是视觉上缩成了 0

这对新手非常重要, 因为调试时你可能会觉得"它消失了", 但其实它还在层级里.

---

### 5.8.2 `shiftUIToggle`

作用:

- 控制 Toggle 能不能点
- 控制 Toggle 值能不能改
- 同时改提示文字和背景颜色

很适合这种场景:

- 这个选项现在不能操作
- 但你想告诉玩家"为什么不能点"

---

## 5.9 音效播放

相关方法:

- `playSound(string p, float val)`

### 它做了什么

1. 如果 `Ocgcore.inSkiping`, 直接不播
2. 依次尝试查找:
   - `sound/xxx.mp3`
   - `sound/xxx.wav`
   - `sound/xxx.ogg`
3. 找到文件后拼成本地 URL
4. 创建一个音效对象
5. 调用 `audio_helper.play(...)`
6. 5 秒后销毁音效对象

### 需要注意的一点

方法签名里有个 `val` 参数, 但当前实现里真正使用的音量来自:

- `Program.I().setting.soundValue()`

这意味着 `val` 现在更像一个遗留参数, 不要误以为你传不同的 `val` 会直接控制音量大小.

---

## 5.10 坐标换算与父子层级处理

相关方法:

- `getCamGoodPosition(Vector3 v, float l)`
- `getScreenDistance(GameObject a, GameObject b)`
- `setParent(GameObject child, GameObject parent)`
- `get_close(Vector3 input_vector, Camera cam, float l)`

### 5.10.1 `getCamGoodPosition`

作用:

- 把一个世界坐标先投到屏幕上
- 再沿 z 方向前后移动一点
- 最后再转回世界坐标

适合用来让一些对象"更靠近相机"或"更远离相机".

---

### 5.10.2 `getScreenDistance`

作用:

- 计算两个对象在屏幕空间上的距离

这和世界坐标距离不同, 更适合做"画面上看起来相距多远"的判断.

---

### 5.10.3 `setParent`

作用:

1. 设置父节点
2. 把整棵子树的 `layer` 都改成和父节点一致

### 为什么这很重要

在 Unity 里, 只 `SetParent` 并不代表子节点 layer 自动全对.

如果 layer 不统一, 可能出现:

- 相机看不到
- UI 点击不到
- 某些射线打不中
- 排序或遮挡异常

所以这个方法很实用.

---

### 5.10.4 `get_close`

作用和 `getCamGoodPosition` 类似, 只不过这里是显式传入相机.

---

## 5.11 卡牌描述字符串辅助

相关方法:

- `getGPSstringLocation(GPS p1)`
- `getGPSstringName(gameCard card, bool green = false)`
- `getSuperName(string name, int code)`
- `getDName(string name, int code)`

这组方法主要服务于:

- 日志显示
- 对局提示
- 富文本卡名超链接

### 例子

`getSuperName("青眼白龙", 89631139)` 可能生成类似:

```text
[url=89631139][u]青眼白龙[/u][/url]
```

意思就是:

- 用富文本包一层下划线
- 点击后能根据卡号定位卡牌信息

所以这组方法的本质是:

- 把对局数据对象转成"适合显示的说明字符串"

---

## 6. 最常见的使用场景

下面是最实用的一些入门写法.

### 6.1 给按钮绑定点击事件

```csharp
UIHelper.registEvent(panel, "btn_start", () =>
{
    Debug.Log("点击了开始按钮");
});
```

### 6.2 找 Label 并设置文本

```csharp
UIHelper.trySetLableText(panel, "title", "欢迎界面");
```

### 6.3 给整棵界面做国际化

```csharp
UIHelper.InterGameObject(windowRoot);
```

### 6.4 给按钮注册并拿到点击对象

```csharp
UIHelper.registClickListener(panel, "item_1", go =>
{
    Debug.Log("点击了:" + go.name);
});
```

### 6.5 播放音效

```csharp
UIHelper.playSound("summon", 1f);
```

### 6.6 设置父节点并同步 layer

```csharp
UIHelper.setParent(childObject, parentObject);
```

### 6.7 从磁盘读头像

```csharp
Texture2D tex = UIHelper.getTexture2D("texture/face/test.png");
```

---

## 7. 这个类的优点

### 7.1 开发效率高

很多重复样板代码都被包起来了.

例如, 业务层不需要反复写:

- `GetComponent`
- `AddComponent`
- `EventDelegate`
- 子节点遍历

### 7.2 对老项目很友好

在老的 NGUI 项目里, 这种集中式工具类很常见, 也很实用.

### 7.3 统一了写法

不同模块都用同一套方式:

- 按名字找控件
- 按统一方法绑事件
- 按统一方法读文本和图片

这在多人协作时是有价值的.

---

## 8. 它的问题和可优化点

这一部分非常值得你认真看, 因为它能帮助你从"会用"慢慢进阶到"会判断代码质量".

### 8.1 问题一:职责太多

`UIHelper` 现在几乎什么都做:

- 窗口控制
- 控件查找
- 事件绑定
- 文本处理
- 国际化
- 贴图读取
- 音效播放
- 布局计算
- 坐标换算
- 卡牌说明字符串拼装

这已经很接近"上帝类（God Class）"了.

### 优化建议

可以拆成几个更专注的类, 例如:

- `UIEventHelper`
- `UITextHelper`
- `UILayoutHelper`
- `UITextureHelper`
- `WindowHelper`
- `UIAudioHelper`

这样每个类的职责会更清晰.

---

### 8.2 问题二:大量按名字查找

`getByName` 这类方法的核心问题是:

- 每次都要遍历子树
- 很依赖节点命名
- 改名字容易出 bug
- 同名节点时结果可能不稳定

### 优化建议

#### 方案 A:初始化时缓存引用

界面创建时, 把常用控件引用缓存起来.

#### 方案 B:构建名字到组件的字典

如果一个界面会反复查找同一批控件, 可以在打开界面时先扫一遍, 后续 O(1) 读取.

#### 方案 C:更现代的绑定方式

如果未来迁移到 uGUI 或 UIToolkit, 可以更多依赖:

- Inspector 序列化引用
- 明确的组件字段
- 更结构化的视图绑定

---

### 8.3 问题三:很多事件注册会清空旧监听

很多地方都有:

- `onClick.Clear()`
- `onChange.Clear()`
- `onSubmit.Clear()`

### 风险

- 别的模块已经绑定好的监听可能被覆盖
- 调试时不容易发现是谁清掉了谁

### 优化建议

把"覆盖绑定"和"追加绑定"拆成不同方法, 例如:

- `SetClickListener()`
- `AddClickListener()`

这样语义会更清晰, 也更安全.

---

### 8.4 问题四:同步磁盘 IO 比较多

例如:

- `getTexture2D`
- `iniFaces`
- `playSound` 里查文件

### 问题

同步 IO 在 Unity 主线程上容易造成卡顿, 尤其是:

- 资源多
- 文件大
- 频繁读取

### 优化建议

- 提前缓存
- 统一资源索引
- 启动时预热
- 高频资源改为更合理的资源管理方案

---

### 8.5 问题五:异常处理有些地方过于宽泛

例如某些方法是这样写的:

```csharp
catch (Exception)
{
}
```

### 问题

- 出错了但没有日志
- 后续排查很痛苦

### 优化建议

至少记录关键上下文, 例如:

- 是哪个控件没找到
- 是哪个路径的图片读取失败
- 是哪个事件注册出错

---

### 8.6 问题六:命名历史包袱较重

例如:

- `registEvent` 应该是 `registerEvent`
- `Lable` 应该是 `Label`
- 一些中文拼音方法名对新人不太友好

### 说明

这不一定会影响运行, 但会影响理解成本和长期维护成本.

### 优化建议

后续可以逐步做别名方法或重构命名, 但需要谨慎, 因为老项目里引用可能很多.

---

### 8.7 问题七:有些方法平台耦合或项目耦合较强

例如:

- 窗口控制依赖 Windows API
- 音效依赖 `Program` / `Ocgcore`
- 国际化依赖 `InterString`
- 头像依赖 `Program.I().face.faces`

这说明它不是一个"纯 UI 通用库", 而是一个"本项目定制工具类".

这本身不一定错, 但要知道它的适用边界.

---

## 9. 作为 Unity 小白, 你该怎么读这个类

我建议你按下面顺序读, 而不是从头硬啃到底.

### 第一步:先看最常用的方法

先理解这些:

- `getByName`
- `registEvent`
- `trySetLableText`
- `InterGameObject`
- `setParent`
- `playSound`

因为它们最贴近日常开发.

### 第二步:再看它的依赖

接着看:

- `Program.cs`
- `MonoDelegate.cs`
- `InterString.cs`

因为这能帮助你理解:

- 为什么很多地方会 `Program.I()`
- 为什么事件不是直接写 lambda 到 NGUI 里
- 为什么文本翻译要走 `InterString`

### 第三步:最后再看那些辅助算法

例如:

- 布局计算
- 屏幕距离
- 贴图切分
- 卡牌说明字符串

这些不是最先必须掌握的, 但等你熟悉项目以后会越来越有感觉.

---

## 10. 适合你现在记住的核心结论

如果只保留最关键的理解, 可以记住下面几点:

1. `UIHelper` 是一个"项目级 UI 工具箱", 不是单一功能类.
2. 它最大的价值是:少写重复代码, 尤其是查控件和绑事件.
3. 它强依赖 `Program`、`InterString`、`MonoDelegate` 等模块.
4. 它服务的是 NGUI 体系, 不是新版 Unity 原生 UI 体系.
5. 它很好用, 但也有明显的维护成本和架构债务.
6. 对新手来说, 最先学会 `getByName`、`registEvent`、`trySetLableText` 就已经很够用了.

---

## 11. 推荐你接下来继续看的文件

如果你想真正把这套 UI 辅助体系看懂, 建议继续读这几个文件:

1. `Assets/SibylSystem/Program.cs`
   - 看全局入口和资源引用是怎么组织的

2. `Assets/SibylSystem/MonoHelpers/MonoDelegate.cs`
   - 看事件桥接是怎么工作的

3. `Assets/SibylSystem/InterString.cs`
   - 看国际化文本是怎么读和补写的

4. 任何一个实际调用 `UIHelper.registEvent(...)` 的 UI 模块
   - 这样你能看到它在真实业务里是怎么被使用的

---

## 12. 最后给新手的一点建议

第一次看这种老项目工具类, 很容易产生一种感觉:

> "怎么什么都塞在一起, 我完全看不懂."

这是正常的.

你不用一口气全部吃透.正确姿势是:

- 先抓最常用方法
- 先理解"它解决了什么重复问题"
- 再理解"为什么它会依赖其他模块"
- 最后再去看哪些地方设计得不够优雅

当你能回答下面这些问题时, 就说明你已经看懂一大半了:

- 它怎么按名字找控件？
- 它怎么给按钮和输入框绑事件？
- 它怎么做国际化？
- 它为什么要依赖 `Program`？
- 它哪些地方方便, 哪些地方容易出坑？

如果这些你已经能讲清楚, 那你就不是"完全看不懂"了, 而是在真正理解项目结构.
