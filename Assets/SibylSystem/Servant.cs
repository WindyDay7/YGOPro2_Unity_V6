using UnityEngine;
using System;
using System.Collections.Generic;
using YGOSharp.OCGWrapper.Enums;

// Servant 是项目自定义的“窗口/模块基类”。
// 你可以把它理解成：
// - 不是场景切换，而是在单场景里切换不同功能模块
// - 每个模块有统一的 show / hide / Update / 输入回调 接口
// - 子类通常是普通 C# 对象，而不是直接挂在场景里的 MonoBehaviour
// - 真正的 Unity 对象（UI、按钮、贴图载体等）由 Servant 在需要时主动创建/销毁
//
// 从职责上看，Servant 主要做 5 件事：
// 1. 提供模块生命周期（initialize/show/hide）
// 2. 托管本模块创建的临时 GameObject
// 3. 提供统一的逐帧更新和输入事件分发
// 4. 管理模块工具栏与屏幕适配
// 5. 提供一套通用的“消息弹窗系统”（RMS）给各业务模块复用
public class Servant
{
    // 某些子类会把自己的根节点挂在这里，便于后续整体控制。
    // 注意：这个字段本身不是 Servant 机制必须依赖的，只是一个常用约定。
    public GameObject gameObject;

    // 表示这个模块当前是否处于“显示态”。
    // Program 每帧都会调用所有 Servant.Update()，但只有 isShowed 为 true 时才会真正处理逻辑。
    public bool isShowed = false;

    // 记录“由当前模块负责生命周期”的对象。
    // 常见用法：通过 create_s() 创建的对象会自动加入这里；hide() 时统一销毁，避免 UI 残留。
    List<GameObject> allGameObjects = new List<GameObject>();

    // 常驻逐帧动作列表：模块初始化后长期存在，只要模块显示就会每帧执行。
    // 构造函数里会默认把 preFrameFunction 注册进来。
    List<Action> updateActions = new List<Action>();

    // 临时逐帧动作列表：一般由模块在 show 后动态追加，hide() 时会自动清空。
    // 这很适合放一些只在本次显示期间有效的动画、检查器、引导逻辑。
    List<Action> updateActions_s = new List<Action>();


    public Servant()
    {
        // 构造阶段就完成基础初始化。
        // 这里的调用顺序是：
        // 1. 先让子类执行 initialize() 做一次性准备
        // 2. 再把 preFrameFunction 注册为默认的每帧逻辑
        //
        // 因此对继承者来说，initialize() 更像“模块构造期初始化”，
        // 而不是“每次 show 时重新初始化”。
        initialize();
        AddUpdateAction(preFrameFunction);
    }

    public virtual void initialize()
    {
        // 给子类覆盖：
        // - 缓存资源引用
        // - 注册常驻 updateActions
        // - 初始化数据结构
        // 一般不建议在这里直接创建只在显示时才需要的 UI，
        // 否则会让对象长期驻留、失去 hide() 统一清理的优势。
    }

    public virtual void show()
    {
        if (isShowed == false)
        {
            isShowed = true;
            // 模块显示/隐藏后，通常需要重新适配当前分辨率与布局。
            // 这里先取消旧的 fixScreenProblem 延时任务，再重新排一个新的，
            // 可以避免短时间内重复 show/hide 导致布局函数多次堆叠执行。
            Program.notGo(fixScreenProblem);
            Program.go(50, fixScreenProblem);
        }
    }

    public virtual void hide()
    {
        // hide 不只是“看不见了”，还是 Servant 最重要的“收尾点”。
        // 它会做这些事情：
        // 1. 关闭普通弹窗与强制确认弹窗
        // 2. 标记模块为隐藏，并重新刷新布局
        // 3. 销毁本模块登记过的临时对象
        // 4. 清空临时逐帧动作 updateActions_s
        // 5. 取消 safeGogo 注册过、但尚未执行的延时任务
        //
        // 也就是说，如果子类的显示态对象没有接入这些托管机制，
        // 就很容易在模块切换后留下脏 UI 或逻辑残留。
        RMSshow_clear();
        RMSshow_clearYNF();
        if (isShowed == true)
        {
            isShowed = false;
            Program.notGo(fixScreenProblem);
            Program.go(50, fixScreenProblem);
        }
        for (int i = 0; i < allGameObjects.Count; i++)
        {
            Program.I().destroy(allGameObjects[i], 0, false, true);
        }
        allGameObjects.Clear();
        updateActions_s.Clear();
        for (int i = 0; i < delayedTasks.Count; i++)
        {
            Program.notGo(delayedTasks[i].act);
        }
        delayedTasks.Clear();
    }

    public virtual void fixScreenProblem()
    {
        // Servant 不直接关心“具体该如何摆放”，
        // 它只负责在合适的时机触发两套布局策略：
        // - 显示态：applyShowArrangement()
        // - 隐藏态：applyHideArrangement()
        //
        // 子类只需要改写这两个方法，就能把“是否显示”和“如何布局”解耦开。
        if (isShowed)
        {
            applyShowArrangement();
        }
        else
        {
            applyHideArrangement();
        }
    }

    public void safeObject(GameObject o)
    {
        // 手动登记一个对象，方便 hide() 时统一销毁。
        // 适用于对象不是通过 create_s() 创建，但生命周期仍然属于当前模块的情况。
        allGameObjects.Add(o);
    }

    public virtual void preFrameFunction()
    {
        // 默认空实现。
        // 子类可把“核心逐帧逻辑”集中写在这里，
        // 它会在构造时自动加入 updateActions。
    }

    // 以下 ES_* 方法是 Servant 的统一输入回调接口。
    // Program 负责收集全局输入上下文，Servant.Update() 负责按当前 pointedGameObject 分发给子类。

    public virtual void ES_mouseDownEmpty()
    {

    }

    public virtual void ES_mouseDownGameObject(GameObject gameObject)
    {

    }

    public virtual void ES_mouseUp()
    {

    }

    public virtual void ES_mouseDownRight()    
    {

    }

    public virtual void ES_mouseUpRight()
    {

    }

    public virtual void ES_mouseUpEmpty()
    {

    }

    public virtual void ES_mouseUpGameObject(GameObject gameObject)
    {

    }

    public virtual void ES_HoverOverGameObject(GameObject gameObject)
    {

    }

    public void showBarOnly()
    {
        if (toolBar != null)
        {
            // 工具栏本质上是一个跟随屏幕尺寸变化的浮动 UI。
            // showBarOnly 负责把它移到可见位置，并开启其内部 toolShift 组件。
            Vector3 vectorOfShowedBar_Screen = new Vector3(Screen.width - RightToScreen, buttomToScreen, 0);
            iTween.MoveTo(toolBar, Program.camera_back_ground_2d.ScreenToWorldPoint(vectorOfShowedBar_Screen), 0.6f);
            toolBar.transform.localScale = new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f);
            var items = toolBar.GetComponentsInChildren<toolShift>();
            for (int i = 0; i < items.Length; i++)  
            {
                items[i].enabled = true;
            }
        }
    }

    public void hideBarOnly()
    {
        if (toolBar != null)
        {
            // 隐藏时不是直接 Destroy，而是先移出屏幕。
            // 这样再次 show 时可以复用已有工具栏对象，减少创建/销毁频率。
            Vector3 vectorOfHidedBar_Screen = new Vector3(Screen.width - RightToScreen, -100, 0);
            iTween.MoveTo(toolBar, Program.camera_back_ground_2d.ScreenToWorldPoint(vectorOfHidedBar_Screen), 0.6f);
            toolBar.transform.localScale = new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f);
            var items = toolBar.GetComponentsInChildren<toolShift>();
            for (int i = 0; i < items.Length; i++)
            {
                items[i].enabled = false;
            }
        }
    }

    public virtual void applyShowArrangement()
    {
        // 默认行为只有“显示工具栏”。
        // 复杂子类通常会重写这里，把窗口、列表、3D 物件一起移动到展示位置。
        showBarOnly();
    }

    public virtual void applyHideArrangement()
    {
        // 默认行为只有“隐藏工具栏”。
        hideBarOnly();
    }

    public virtual void ES_quit()
    {

    }

    // 记录上一帧悬停对象，只在悬停目标发生变化时触发 ES_HoverOverGameObject，
    // 避免同一对象每帧重复通知。
    GameObject preHover = null;

    public void Update()
    {
        if (isShowed)
        {
            // Servant 的每帧执行顺序非常关键：
            // 1. 先执行常驻 updateActions
            // 2. 再执行本次显示期间的临时 updateActions_s
            // 3. 再根据 Program 已经收集好的输入结果分发事件
            //
            // 也就是说，Servant 本身并不直接读取 Input 或 Raycast，
            // 它消费的是 Program 提前整理好的“这一帧上下文”。
            for (int i = 0; i < updateActions.Count; i++)
            {
                updateActions[i]();
            }
            for (int i = 0; i < updateActions_s.Count; i++)
            {
                updateActions_s[i]();
            }
            if (Program.InputGetMouseButtonDown_0)
            {
                if (Program.pointedGameObject == null)
                {
                    ES_mouseDownEmpty();
                }
                else
                {
                    ES_mouseDownGameObject(Program.pointedGameObject);
                }
            }
            if (Program.InputGetMouseButtonUp_0)
            {
                if (Program.pointedGameObject == null)
                {
                    ES_mouseUpEmpty();
                }
                else
                {
                    ES_mouseUpGameObject(Program.pointedGameObject);
                }
                ES_mouseUp();
            }
            if (Program.InputGetMouseButtonDown_1)
            {
                ES_mouseDownRight();
            }
            if (Program.InputGetMouseButtonUp_1)
            {
                ES_mouseUpRight();
            }
            if (preHover != Program.pointedGameObject)
            {
                preHover = Program.pointedGameObject;
                if (preHover!=null)
                    ES_HoverOverGameObject(preHover);
            }
        }
    }

    public void OnQuit()
    {
        // Program 退出时会统一调用每个模块的 OnQuit。
        // 子类可以在 ES_quit 中做最终存档、断线通知或资源清理。
        ES_quit();
    }

    public GameObject create(
        GameObject mod,
        Vector3 position = default(Vector3),
        Vector3 rotation = default(Vector3),
        bool fade = false,
        GameObject father = null,
        bool allParamsInWorld = true,
        Vector3 wantScale = default(Vector3)
        )
    {
        // 统一走 Program.create，保证实例化参数、父节点、坐标空间规则一致。
        // 与 create_s 的区别是：这里不会自动接管返回对象的生命周期。
        var re = Program.I().create(mod, position, rotation, fade, father, allParamsInWorld, wantScale);
        return re;
    }

    public GameObject create_s(
        GameObject mod,
        Vector3 position = default(Vector3),
        Vector3 rotation = default(Vector3),
        bool fade = false,
        GameObject father = null,
        bool allParamsInWorld = true,
        Vector3 wantScale = default(Vector3)
        )
    {
        // create_s 比 create 多做了一件事：
        // 自动把新建对象登记到 allGameObjects，后续 hide() 时会统一清理。
        var re = Program.I().create(mod, position, rotation, fade, father, allParamsInWorld, wantScale);
        allGameObjects.Add(re);
        return re;
    }

    public void destroy(GameObject obj, float time = 0, bool fade = false, bool instantNull = false)
    {
        // 销毁前先从托管列表移除，防止 hide() 再次尝试销毁同一个对象。
        allGameObjects.Remove(obj);
        Program.I().destroy(obj, time, fade, instantNull);
    }

    public void AddUpdateAction(Action action)
    {
        // 添加常驻逐帧动作。
        updateActions.Add(action);
    }

    public void RemoveUpdateAction(Action action)
    {
        updateActions.Remove(action);
    }

    public void AddUpdateAction_s(Action action)
    {
        // 添加临时逐帧动作；hide() 时会自动清空。
        updateActions_s.Add(action);
    }

    public void RemoveUpdateAction_s(Action action)
    {
        updateActions_s.Remove(action);
    }

    // 当前模块挂接的工具栏对象。不是每个 Servant 都必须有。
    public GameObject toolBar;

    // 工具栏相对于屏幕底部的偏移。
    float buttomToScreen;

    // 工具栏相对于屏幕右侧的偏移。
    float RightToScreen;

    public void SetBar(GameObject mod,float buttomToScreen,float RightToScreen)
    {
        // 为模块创建/替换工具栏。
        // 注意这里即使已有旧工具栏，也会先直接销毁再重建，
        // 因为外观或按钮集合可能已经发生变化。
        this.buttomToScreen = buttomToScreen;
        this.RightToScreen = RightToScreen;
        if (toolBar!=null)
        {
            MonoBehaviour.DestroyImmediate(toolBar);
        }
        toolBar = create
            (
            mod,
            Program.camera_main_2d.ScreenToWorldPoint(new Vector3(Screen.width - RightToScreen, -100, 0)),
            new Vector3(0, 0, 0),
            false,
            Program.ui_main_2d
            );
        UIHelper.InterGameObject(toolBar);
        fixScreenProblem();
    }

    public void reShowBar(float buttomToScreen, float RightToScreen)
    {
        // 仅更新工具栏偏移，不重建对象。
        // 常用于分辨率变化或 UI 布局微调后的重新摆放。
        this.buttomToScreen = buttomToScreen;
        this.RightToScreen = RightToScreen; 
        if (isShowed)   
        {
            showBarOnly();
        }
    }

    // safeGogo 注册的延时任务表。
    // 它和 Program.delayedTasks 的区别在于：这里额外保存“属于本模块”的那一份引用，
    // 这样 hide() 时就能把还没触发的任务一并取消。
    List<Program.delayedTask> delayedTasks = new List<Program.delayedTask>();
    public void safeGogo(int delay_, Action act_)
    {
        // 对 Program.go 的“安全封装”。
        // 适用于那些只应该在当前模块可见期间执行的延迟行为。
        Program.go(delay_, act_);
        delayedTasks.Add(new Program.delayedTask
        {
            act = act_,
            timeToBeDone = delay_ + Program.TimePassed(),
        });
    }

    #region remasterMessageSystem

    public Vector3 centre(bool fix=false)
    {
        // 计算“当前业务窗口”的视觉中心点。
        // 当对局界面或卡组编辑器显示时，真正的视觉中心不一定等于屏幕中心，
        // 所以这里会优先参考 game_main 相机映射后的中心位置。
        if (Program.I().ocgcore.isShowed || Program.I().deckManager.isShowed)
        {
            Vector3 screenP = Program.camera_game_main.WorldToScreenPoint(Vector3.zero);
            screenP.z = 0;
            if (fix)
            {
                if (screenP.y > Screen.height - 350f)
                {
                    screenP.y = Screen.height - 350f;
                }
                if (screenP.y < 350f)
                {
                    screenP.y = 350f;
                }
            }
            return Program.camera_main_2d.ScreenToWorldPoint(screenP);
        }
        else
        {
            return Program.camera_main_2d.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        }
    }

    Vector3 MSentre()
    {
        // Message System 专用中心点。
        // 对局中弹窗需要略微跟随场地区域，否则在不同 fieldSize 下会显得偏位。
        if (Program.I().ocgcore.isShowed)
        {
            float real = (Program.fieldSize - 1) * 0.9f + 1f;
            Vector3 screenP = Program.camera_game_main.WorldToScreenPoint(new Vector3(0, 0, -5.65f * real));
            screenP.z = 0;
            return Program.camera_main_2d.ScreenToWorldPoint(screenP);
        }
        if (Program.I().deckManager.isShowed)
        {
            Vector3 screenP = Program.camera_game_main.WorldToScreenPoint(Vector3.zero);
            screenP.z = 0;
            return Program.camera_main_2d.ScreenToWorldPoint(screenP);
        }
        return Program.camera_main_2d.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0));
    }

    private enum messageSystemType
    {
        // none: 当前没有活动弹窗
        // onlyYes / yesOrNo / yesOrNoOrCancle: 标准确认框
        // singleChoice / multipleChoice: 单选、多选
        // input: 文本输入
        // position: 表示形式/站位选择
        // tp: 猜拳（石头剪刀布）
        none,
        onlyYes,
        yesOrNo,
        yesOrNoOrCancle,
        yesOrNoOrSee,
        singleChoice,
        multipleChoice,
        input,
        position,
        tp,
    }

    // 当前弹窗的交互类型，决定 ES_RMSpremono 如何解释一次点击。
    private messageSystemType currentMStype = messageSystemType.none;

    // 当前弹窗对应的业务标识。
    // 子类在 ES_RMS(hashCode, result) 中通常依靠它判断这次回调属于哪一类对话框。
    public string currentMShash;

    // 当前普通弹窗的根对象。一次只维护一个“标准 RMS 窗口”。
    private GameObject currentMSwindow = null;

    public class messageSystemValue
    {
        // value: 真正回传给业务逻辑的值
        // hint : 呈现在按钮/选项上的文案
        public string value = "";
        public string hint = "";
    }

    public virtual void ES_RMS(string hashCode, List<messageSystemValue> result)
    {
        // 子类覆盖此方法以接收 RMS 结果。
        // 默认实现只负责收掉当前弹窗，避免没有子类处理时窗口残留。
        RMSshow_clear();
    }

    void ES_RMSpremono(GameObject gameObjectClicked, messageSystemValue value)
    {
        // 所有 RMS 按钮最终都会汇总到这里，再按 currentMStype 分流。
        // 对于单选型窗口：一次点击就立刻回调 ES_RMS。
        // 对于多选窗口：需要先维护选中集，等数量满足要求再回调。
        List<messageSystemValue> re;
        switch (currentMStype)  
        {
            case messageSystemType.onlyYes:
            case messageSystemType.yesOrNo:
            case messageSystemType.yesOrNoOrCancle:
            case messageSystemType.yesOrNoOrSee:
            case messageSystemType.singleChoice:
            case messageSystemType.input:
            case messageSystemType.position:
            case messageSystemType.tp:
                re = new List<messageSystemValue>();
                re.Add(value);
                ES_RMS(currentMShash, re);
                break;
            case messageSystemType.multipleChoice:
                bool exist = false;
                for (int i = 0; i < RMSshow_multipleChoice_selected.Count; i++)
                {
                    if (RMSshow_multipleChoice_selected[i] == value)
                    {
                        exist = true;
                    }
                }
                UILabel lab = gameObjectClicked.GetComponentInChildren<UILabel>();
                if (exist)
                {
                    RMSshow_multipleChoice_selected.Remove(value);
                    if (lab != null)
                    {
                        Color c = lab.color;
                        c.a = 1f;
                        lab.color = c;
                    }
                }
                else
                {
                    RMSshow_multipleChoice_selected.Add(value);
                    if (lab != null)
                    {
                        Color c = lab.color;
                        c.a = 0.3f;
                        lab.color = c;
                    }
                }
                if (RMSshow_multipleChoice_selected.Count == RMSshow_multipleChoice_count)
                {
                    ES_RMS(currentMShash, RMSshow_multipleChoice_selected);
                }
                break;
        }
    }

    public void RMSshow_clear()
    {
        // 关闭普通 RMS 窗口，并把状态复位到“当前无消息框”。
        // 这里用字符串 "NULL" 作为无活动消息的哨兵值。
        currentMStype = messageSystemType.none;
        currentMShash = "NULL";
        if (currentMSwindow != null)
        {
            destroy(currentMSwindow, 0.1f, false, true);
            currentMSwindow = null;
        }
    }

    public void RMSshow_clearYNF()
    {
        // 关闭强制 yes/no 窗口。
        // 它单独管理，不与 currentMSwindow 共用，是因为项目里允许它作为特殊强提示存在。
        if (yesOrNoForce != null)
        {
            destroy(yesOrNoForce, 0.1f, false, true);
            yesOrNoForce = null;
        }
    }

    public bool IfNoMessage()
    {
        // 给业务层一个快速判断：当前是否没有标准 RMS 弹窗正在显示。
        return currentMShash == "NULL";
    }

    public void RMSshow_onlyYes(string hashCode, string hint, messageSystemValue yes)
    {
        // 标准“提示 + 确认”窗口。
        RMSshow_clear();
        currentMShash = hashCode;
        currentMStype = messageSystemType.onlyYes;
        currentMSwindow = create
            (
            Program.I().ES_1,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UIHelper.InterGameObject(currentMSwindow);
        UIHelper.trySetLableText(currentMSwindow, "hint_", hint);
        UIHelper.registEvent(currentMSwindow, "yes_", ES_RMSpremono, yes);
    }

    public void RMSshow_yesOrNo(string hashCode, string hint, messageSystemValue yes, messageSystemValue no)
    {
        // 标准“是 / 否”窗口。
        RMSshow_clear();
        currentMShash = hashCode;
        currentMStype = messageSystemType.yesOrNo;
        currentMSwindow = create
            (
            Program.I().ES_2,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UIHelper.InterGameObject(currentMSwindow);
        UIHelper.trySetLableText(currentMSwindow, "hint_", hint);
        UIHelper.registEvent(currentMSwindow, "yes_", ES_RMSpremono, yes);
        UIHelper.registEvent(currentMSwindow, "no_", ES_RMSpremono, no);
    }

    // 特殊的强制确认框根对象。
    // 它与普通 RMS 分开维护，通常用于必须立即回答、不可和其他窗口混淆的场景。
    private GameObject yesOrNoForce;

    public void RMSshow_yesOrNoForce(string hint, messageSystemValue yes, messageSystemValue no)
    {
        // 注意：这里没有 hashCode。
        // 强制确认框不走普通 ES_RMS，而是回调 ES_RMS_ForcedYesNo。
        RMSshow_clearYNF();
        yesOrNoForce = create
            (
            Program.I().ES_2Force,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UIHelper.InterGameObject(yesOrNoForce);
        UIHelper.trySetLableText(yesOrNoForce, "hint_", hint);
        UIHelper.registEvent(yesOrNoForce, "yes_", ES_RMSpremonoForceYesNo, yes);
        UIHelper.registEvent(yesOrNoForce, "no_", ES_RMSpremonoForceYesNo, no);
    }

    void ES_RMSpremonoForceYesNo(GameObject gameObjectClicked, messageSystemValue value)
    {
        ES_RMS_ForcedYesNo(value);
    }

    public virtual void ES_RMS_ForcedYesNo(messageSystemValue result)
    {
        // 默认只负责销毁强制确认框。
        // 子类若重写，通常会在处理完结果后再决定是否调用 destroy。
        destroy(yesOrNoForce, 0.6f, true, true);
    }

    public void RMSshow_FS(string hashCode, messageSystemValue first, messageSystemValue second)
    {
        // 一个双选窗口的特化版本，按钮文本通常不是 yes/no，而是 first/second 两个业务选项。
        RMSshow_clear();
        currentMShash = hashCode;
        currentMStype = messageSystemType.yesOrNo;
        currentMSwindow = create
            (
            Program.I().ES_FS,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UIHelper.InterGameObject(currentMSwindow);
        UIHelper.registEvent(currentMSwindow, "yes_", ES_RMSpremono, first);
        UIHelper.registEvent(currentMSwindow, "no_", ES_RMSpremono, second);
    }

    public void RMSshow_yesOrNoOrCancle(string hashCode, string hint, messageSystemValue yes, messageSystemValue no, messageSystemValue cancle)
    {
        // 三选确认框：yes / no / cancel。
        RMSshow_clear();
        currentMShash = hashCode;
        currentMStype = messageSystemType.yesOrNoOrCancle;
        currentMSwindow = create
            (
            Program.I().ES_3cancle,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UIHelper.InterGameObject(currentMSwindow);
        UIHelper.trySetLableText(currentMSwindow, "hint_", hint);
        UIHelper.registEvent(currentMSwindow, "yes_", ES_RMSpremono, yes);
        UIHelper.registEvent(currentMSwindow, "no_", ES_RMSpremono, no);
        UIHelper.registEvent(currentMSwindow, "cancle_", ES_RMSpremono, cancle);
    }

    public void RMSshow_singleChoice(string hashCode, List<messageSystemValue> options)
    {
        // 动态生成一个纵向单选列表。
        // 每个按钮点击后都会立即返回对应的 messageSystemValue。
        RMSshow_clear();
        currentMShash = hashCode;
        currentMStype = messageSystemType.singleChoice;
        currentMSwindow = create
            (
            Program.I().ES_Single_multiple_window,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UISprite sp = UIHelper.getByName<UISprite>(currentMSwindow, "under");
        sp.height = 70 + options.Count * 48;
        for (int i = 0; i < options.Count; i++)
        {
            GameObject btn = create
           (
           Program.I().ES_Single_option,
           new Vector3(-2, sp.height / 2 - 59 - 48 * i, 0),
           Vector3.zero,
           false,
           sp.gameObject,
           false
           );
            UIHelper.trySetLableText(btn, "[u]"+options[i].hint);
            UIHelper.registEvent(btn, btn.name, ES_RMSpremono, options[i]);
        }
        UIHelper.InterGameObject(currentMSwindow);
    }

    // 多选模式要求最终选中的数量。
    int RMSshow_multipleChoice_count = 0;

    // 多选模式当前已选项集合。
    List<messageSystemValue> RMSshow_multipleChoice_selected = new List<messageSystemValue>();

    public void RMSshow_multipleChoice(string hashCode, int selectCount, List<messageSystemValue> options)
    {
        // 动态生成一个网格状多选列表。
        // 只有当“已选数量 == selectCount”时，才会回调 ES_RMS。
        RMSshow_multipleChoice_count = selectCount;
        RMSshow_multipleChoice_selected.Clear();
        RMSshow_clear();
        currentMShash = hashCode;
        currentMStype = messageSystemType.multipleChoice;
        currentMSwindow = create
            (
            Program.I().ES_Single_multiple_window,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UISprite sp = UIHelper.getByName<UISprite>(currentMSwindow, "under");
        sp.height = 70 + UIHelper.get_zonghangshu(options.Count, 5) * 40;
        sp.width = 470;
        for (int i = 0; i < options.Count; i++)
        {
            Vector2 v = UIHelper.get_hang_lie(i, 5);
            float hang = v.x;
            float lie = v.y;
            GameObject btn = create
           (
           Program.I().ES_multiple_option,
           new Vector3(-162 + lie * 80, sp.height / 2 - 55 - 40 * hang, 0),
           Vector3.zero,
           false,
           sp.gameObject,
           false
           );
            UIHelper.trySetLableText(btn, "[u]" + options[i].hint);
            UIHelper.registEvent(btn, btn.name, ES_RMSpremono, options[i]);
        }
        UIHelper.InterGameObject(currentMSwindow);
    }

    public void RMSshow_position(string hashCode, int code, messageSystemValue atk, messageSystemValue def)
    {
        // 站位选择窗口：通常用于让玩家在攻击表示 / 守备表示之间选择。
        // 除了按钮外，还会根据位置值调整卡图朝向与第二按钮位置。
        RMSshow_clear();
        currentMShash = hashCode;
        currentMStype = messageSystemType.position;
        currentMSwindow = create
            (
            Program.I().ES_position,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UIHelper.InterGameObject(currentMSwindow);
        UIHelper.registEvent(currentMSwindow, "atk_", ES_RMSpremono, atk);
        UIHelper.registEvent(currentMSwindow, "def_", ES_RMSpremono, def);

        UITexture atkpic = UIHelper.getByName<UITexture>(currentMSwindow, "atkPic_");
        UIButton defbutton = UIHelper.getByName<UIButton>(currentMSwindow, "def_");
        if (Int32.Parse(atk.value) == (int)CardPosition.FaceUpDefence)
        {
            atkpic.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            defbutton.transform.localPosition = new Vector3(72.8f, 2f, 0f);
        }
        else
        {
            atkpic.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            defbutton.transform.localPosition = new Vector3(62.8f, 0f, 0f);
        }

        cardPicLoader cardPicLoader_ = currentMSwindow.AddComponent<cardPicLoader>();
        cardPicLoader_.code = code;
        cardPicLoader_.uiTexture = atkpic;
        cardPicLoader_ = currentMSwindow.AddComponent<cardPicLoader>();
        cardPicLoader_.code = (Int32.Parse(def.value) == (int)CardPosition.FaceDownDefence) ? 0 : code;
        cardPicLoader_.uiTexture = UIHelper.getByName<UITexture>(currentMSwindow, "defPic_");
    }
    public void RMSshow_position3(string hashCode, int code)
    {
        // 三态站位选择：表攻 / 表守 / 里守。
        RMSshow_clear();
        currentMShash = hashCode;
        currentMStype = messageSystemType.position;
        currentMSwindow = create
            (
            Program.I().ES_position3,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UIHelper.InterGameObject(currentMSwindow);
        UIHelper.registEvent(currentMSwindow, "upAtk_", ES_RMSpremono, new messageSystemValue { value = "1", hint = "Face-Up Attack" });
        UIHelper.registEvent(currentMSwindow, "upDef_", ES_RMSpremono, new messageSystemValue { value = "4", hint = "Face-Up Defense" });
        UIHelper.registEvent(currentMSwindow, "downDef_", ES_RMSpremono, new messageSystemValue { value = "8", hint = "Face-Down Defense" });

        UITexture upatkpic = UIHelper.getByName<UITexture>(currentMSwindow, "upAtkPic_");
        UITexture updefpic = UIHelper.getByName<UITexture>(currentMSwindow, "upDefPic_");
        UITexture downdefpic = UIHelper.getByName<UITexture>(currentMSwindow, "downDefPic_");

        cardPicLoader cardPicLoader_ = currentMSwindow.AddComponent<cardPicLoader>();
        cardPicLoader_.code = code;
        cardPicLoader_.uiTexture = upatkpic;
        cardPicLoader_ = currentMSwindow.AddComponent<cardPicLoader>();
        cardPicLoader_.code = code;
        cardPicLoader_.uiTexture = updefpic;
        cardPicLoader_ = currentMSwindow.AddComponent<cardPicLoader>();
        cardPicLoader_.code = 0;
        cardPicLoader_.uiTexture = downdefpic;
    }

    public void RMSshow_tp(string hashCode, messageSystemValue jiandao, messageSystemValue shitou, messageSystemValue bu)
    {
        // tp = 猜拳窗口（剪刀、石头、布）。
        RMSshow_clear();
        currentMShash = hashCode;
        currentMStype = messageSystemType.tp;
        currentMSwindow = create
            (
            Program.I().ES_Tp,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UIHelper.InterGameObject(currentMSwindow);
        UIHelper.registEvent(currentMSwindow, "jiandao_", ES_RMSpremono, jiandao);
        UIHelper.registEvent(currentMSwindow, "shitou_", ES_RMSpremono, shitou);
        UIHelper.registEvent(currentMSwindow, "bu_", ES_RMSpremono, bu);
    }

    public void RMSshow_input(string hashCode, string hint,string default_) 
    {
        // 文本输入窗口。
        // 创建后会延迟让输入框获得焦点，因为 NGUI 输入框在同帧创建时不一定能立即选中。
        RMSshow_clear();
        currentMShash = hashCode;
        currentMStype = messageSystemType.input;
        currentMSwindow = create
            (
            Program.I().ES_input,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UIHelper.InterGameObject(currentMSwindow);
        UIHelper.trySetLableText(currentMSwindow, "hint_", hint);
        UIHelper.registEvent(currentMSwindow, "input_", ES_RMSpremono, null, "yes_");
        UIHelper.getByName<UIInput>(currentMSwindow, "input_").value = default_;
        Program.go(100, () => { UIHelper.getByName<UIInput>(currentMSwindow, "input_").isSelected = true; });
    }

    public void RMSshow_none(string hint)
    {
        // 某些提示不需要真正弹窗，而是直接写入卡片描述/日志面板。
        Program.I().cardDescription.mLog(hint);
    }

    public void RMSshow_face(string hashCode, string name)  
    {
        // 展示头像/立绘并等待确认的窗口。
        RMSshow_clear();
        currentMShash = hashCode;
        currentMStype = messageSystemType.onlyYes;
        currentMSwindow = create
            (
            Program.I().ES_Face,
            MSentre(),
            Vector3.zero,
            true,
            Program.ui_main_2d,
            true,
            new Vector3(((float)Screen.height) / 700f, ((float)Screen.height) / 700f, ((float)Screen.height) / 700f)
            );
        UIHelper.InterGameObject(currentMSwindow);
        UIHelper.getByName<UITexture>(currentMSwindow, "face_").mainTexture = UIHelper.getFace(name);
        UIHelper.registEvent(currentMSwindow, "yes_", ES_RMSpremono, new messageSystemValue());
    }

    #endregion
}
