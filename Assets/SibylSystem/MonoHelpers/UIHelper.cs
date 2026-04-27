using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;
using YGOSharp.OCGWrapper.Enums;

/// <summary>
/// UI 公共工具类.
/// 
/// 这个类在当前项目里承担了很多"横切能力": 
/// 1. 桌面窗口控制（最大化、恢复、闪烁提示）.
/// 2. NGUI 控件查找与事件注册.
/// 3. 贴图加载、切分与简单材质模式切换.
/// 4. 一些 UI 布局计算、文本辅助、卡牌位置说明拼装.
/// 5. 若干和界面强相关的杂项逻辑（播放音效、设置父节点、屏幕坐标换算等）.
/// 
/// 从"开发效率"角度, 它是高效的: 大量重复样板代码被集中到了一个地方, 业务代码可以直接通过名字取控件、绑事件.
/// 但从"运行效率 / 可维护性 / 单一职责"角度, 它并不理想: 
/// - 类职责过多, 已经接近 God Class（上帝类）.
/// - 许多方法内部会频繁调用 GetComponentsInChildren / 文件 IO / 像素级循环, 运行时成本不低.
/// - 多处通过控件名字查找节点, 属于 O(n) 遍历, 不适合在高频路径反复调用.
/// - 一些方法直接吞异常, 排障成本较高.
/// 
/// 因此可以把它理解为: 
/// "对旧项目和快速迭代很实用, 但不是长期最优架构".
/// </summary>
public static class UIHelper
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("user32")]
    static extern bool FlashWindow(IntPtr handle, bool invert);

    public delegate bool WNDENUMPROC(IntPtr hwnd, IntPtr lParam);
    [DllImport("user32", SetLastError = true)]
    static extern bool EnumWindows(WNDENUMPROC lpEnumFunc, IntPtr lParam);

    [DllImport("user32", SetLastError = true)]
    static extern IntPtr GetParent(IntPtr hWnd);
    [DllImport("user32")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref IntPtr lpdwProcessId);
    [DllImport("user32")]
    static extern int GetClassNameW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder lpString, int nMaxCount);
    [DllImport("user32")]
    static extern bool IsZoomed(IntPtr hWnd);
    [DllImport("user32")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("kernel32")]
    static extern void SetLastError(uint dwErrCode);
#endif

    static IntPtr myHWND = IntPtr.Zero;

    /// <summary>
    /// 获取当前 Unity 进程对应的主窗口句柄, 并做一次缓存.
    /// 
    /// 仅在 Windows 桌面平台下有意义: 
    /// - 用于后续最大化 / 恢复 / 闪烁任务栏窗口.
    /// - 通过枚举所有顶层窗口, 筛选类名为 UnityWndClass 且进程 ID 匹配的窗口.
    /// 
    /// 这是一个"平台桥接"方法, 本身不属于 UI 表现层, 但被放在 UIHelper 里是因为其用途服务于桌面 UI 窗口行为.
    /// </summary>
    static IntPtr GetProcessWnd()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        if (myHWND != IntPtr.Zero)
            return myHWND;

        IntPtr ptrWnd = IntPtr.Zero;
        IntPtr pid = (IntPtr)System.Diagnostics.Process.GetCurrentProcess().Id;  // 当前进程 ID

        bool bResult = EnumWindows(new WNDENUMPROC(delegate (IntPtr hwnd, IntPtr mypid)
        {
            IntPtr id = IntPtr.Zero;

            StringBuilder ClassName = new StringBuilder(256);
            GetClassNameW(hwnd, ClassName, ClassName.Capacity);
            
            if (string.Compare(ClassName.ToString(), "UnityWndClass", true, System.Globalization.CultureInfo.InvariantCulture) == 0)
            {
                GetWindowThreadProcessId(hwnd, ref id);
                if (id == mypid)    // 找到进程对应的主窗口句柄
                {
                    ptrWnd = hwnd;   // 把句柄缓存起来
                    SetLastError(0);    // 设置无错误
                    return false;   // 返回 false 以终止枚举窗口
                }
            }

            return true;

        }), pid);

        if (!bResult && Marshal.GetLastWin32Error() == 0)
        {
            myHWND = ptrWnd;
        }

        return myHWND;
    #else
        return IntPtr.Zero;
    #endif
    }

    /// <summary>
    /// 在 Windows 下让游戏窗口闪烁, 常用于提醒玩家程序需要关注.
    /// </summary>
    public static void Flash()
    {
    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        FlashWindow(GetProcessWnd(), true);
    #endif
    }

    /// <summary>
    /// 判断当前窗口是否已最大化.
    /// 非 Windows 平台下没有统一可靠实现, 因此直接返回 false.
    /// </summary>
    public static bool isMaximized()
    {
#if UNITY_STANDALONE_WIN
        return IsZoomed(GetProcessWnd());
#else
        // not a easy thing to check window status on non-windows desktop...
        return false;
#endif
    }

    /// <summary>
    /// 将窗口最大化.
    /// 只在 Windows 独立运行环境下生效.
    /// </summary>
    public static void MaximizeWindow()
    {
#if UNITY_STANDALONE_WIN
        ShowWindow(GetProcessWnd(), 3); // SW_MAXIMIZE
#endif
    }

    /// <summary>
    /// 将最大化窗口恢复为普通状态.
    /// </summary>
    public static void RestoreWindow()
    {
#if UNITY_STANDALONE_WIN
        ShowWindow(GetProcessWnd(), 9); // SW_RESTORE
#endif
    }

    /// <summary>
    /// 读取配置项, 判断启动后是否应该自动最大化窗口.
    /// 配置值约定: "1" 为真, "0" 为假.
    /// </summary>
    public static bool shouldMaximize()
    {
        return fromStringToBool(Config.Get("maximize_", "0"));
    }

    public enum RenderingMode
    {
        Opaque,
        Cutout,
        Fade,
        Transparent,
    }

    /// <summary>
    /// 统一设置材质透明模式.
    /// 
    /// 这是对 Unity Standard Shader 常见参数的直接封装, 方便业务层按"意图"切换: 
    /// Opaque / Cutout / Fade / Transparent.
    /// </summary>
    public static void SetMaterialRenderingMode(Material material, RenderingMode renderingMode)
    {
        switch (renderingMode)
        {
            case RenderingMode.Opaque:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                break;
            case RenderingMode.Cutout:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 2450;
                break;
            case RenderingMode.Fade:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
            case RenderingMode.Transparent:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
        }
    }

    /// <summary>
    /// 给 UIButton 注册点击事件.
    /// 
    /// 项目里通过 MonoDelegate 组件把普通 C# Action 转成 NGUI EventDelegate 可调用的方法.
    /// 这里会先清空旧 onClick, 再绑定新的 function, 因此它的语义更接近"覆盖绑定"而不是"追加绑定".
    /// </summary>
    internal static void registEvent(UIButton btn, Action function) 
    {
        if (btn != null)
        {
            MonoDelegate d = btn.gameObject.GetComponent<MonoDelegate>();
            if (d == null)
            {
                d = btn.gameObject.AddComponent<MonoDelegate>();
            }
            d.actionInMono = function;
            btn.onClick.Clear();
            btn.onClick.Add(new EventDelegate(d, "function"));
            return;
        }
    }

    /// <summary>
    /// 将决斗场地图切分为左 / 中 / 右三张贴图.
    /// 
    /// 用途: 项目中的场地图会按三个 UITexture 分开显示, 以适配不同布局或实现局部替换.
    /// 过程: 
    /// 1. 先缩放到固定尺寸 1024x819.
    /// 2. 按预设比例切分左右边界.
    /// 3. 对每个像素分别写入三个新纹理中.
    /// 
    /// 注意: 这是一个明显偏重的 CPU 像素级操作, 适合初始化阶段执行, 不适合频繁运行.
    /// </summary>
    internal static Texture2D[] sliceField(Texture2D textureField_) 
    {
        Texture2D textureField = ScaleTexture(textureField_,1024,819);
        Texture2D[] returnValue = new Texture2D[3];
        returnValue[0] = new Texture2D(textureField.width, textureField.height);
        returnValue[1] = new Texture2D(textureField.width, textureField.height);
        returnValue[2] = new Texture2D(textureField.width, textureField.height);
        float zuo = (float)textureField.width * 69f / 320f;
        float you = (float)textureField.width * 247f / 320f;
        for (int w = 0; w < textureField.width; w++)
        {
            for (int h = 0; h < textureField.height; h++)
            {
                Color c = textureField.GetPixel(w, h);
                if (c.a < 0.05f)
                {
                    c.a = 0;
                }
                if (w < zuo)
                {
                    returnValue[0].SetPixel(w, h, c);
                    returnValue[1].SetPixel(w, h, new Color(0, 0, 0, 0));
                    returnValue[2].SetPixel(w, h, new Color(0, 0, 0, 0));
                }
                else if (w > you)
                {
                    returnValue[2].SetPixel(w, h, c);
                    returnValue[0].SetPixel(w, h, new Color(0, 0, 0, 0));
                    returnValue[1].SetPixel(w, h, new Color(0, 0, 0, 0));
                }
                else
                {
                    returnValue[1].SetPixel(w, h, c);
                    returnValue[0].SetPixel(w, h, new Color(0, 0, 0, 0));
                    returnValue[2].SetPixel(w, h, new Color(0, 0, 0, 0));
                }
            }
        }
        returnValue[0].Apply();
        returnValue[1].Apply();
        returnValue[2].Apply();
        return returnValue;
    }

    /// <summary>
    /// 使用双线性采样将贴图缩放到指定大小.
    /// 
    /// 这里是纯 CPU 逐像素实现, 优点是简单直接、兼容性高；
    /// 缺点是会产生较多像素读写开销, 不适合运行时高频调用.
    /// </summary>
    static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);

        float incX = (1.0f / (float)targetWidth);
        float incY = (1.0f / (float)targetHeight);

        for (int i = 0; i < result.height; ++i)
        {
            for (int j = 0; j < result.width; ++j)
            {
                Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                result.SetPixel(j, i, newColor);
            }
        }

        result.Apply();
        return result;
    }

    /// <summary>
    /// 在 father 的所有子节点中, 查找"名字等于 name"的指定类型组件.
    /// 
    /// 这是本项目里使用最广的 UI 访问方式之一: 
    /// - 优点: 不需要在 Inspector 里逐个拖引用, 开发速度快.
    /// - 缺点: 每次都要遍历子树, 且强依赖节点命名, 重构名字时容易出问题.
    /// 
    /// 返回最后一个匹配项, 而不是第一个匹配项, 这一点在同名节点存在时需要特别注意.
    /// </summary>
    public static T getByName<T>(GameObject father,string name) where T:Component
    {
        T return_value = null;
        var all = father.transform.GetComponentsInChildren<T>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == name)
            {
                return_value = all[i];
            }
        }
        return return_value;
    }

    /// <summary>
    /// 对一个界面树做"文字国际化替换".
    /// 
    /// 规则: 
    /// - 名字以 ! 开头的 UILabel
    /// - 名字为 yes_ / no_ 的 UILabel
    /// 会把当前 text 视为语言键, 再通过 InterString.Get 取本地化文本.
    /// 
    /// 这种做法适合旧式 NGUI 界面快速补国际化, 但本质上仍然是"命名约定驱动".
    /// </summary>
    public static void InterGameObject(GameObject father)
    {
        var all = father.transform.GetComponentsInChildren<UILabel>();  
        for (int i = 0; i < all.Length; i++)
        {
            if ((all[i].name.Length > 1 && all[i].name[0] == '!') || all[i].name == "yes_" || all[i].name == "no_")
            {
                all[i].text = InterString.Get(all[i].text);
            }
        }
    } 

    /// <summary>
    /// 在子树中按名字查找 GameObject.
    /// 与泛型版本类似, 同样会遍历整个 Transform 树并返回最后一个匹配节点.
    /// </summary>
    public static GameObject getByName(GameObject father, string name)
    {
        GameObject return_value = null;
        var all = father.transform.GetComponentsInChildren<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == name)
            {
                return_value = all[i].gameObject;
            }
        }
        return return_value;
    }

    /// <summary>
    /// 获取子树中的第一个指定类型组件.
    /// 适合在场景结构稳定、且该类型只会出现一次时使用.
    /// </summary>
    public static T getByName<T>(GameObject father) where T : Component
    {
        T return_value = father.transform.GetComponentInChildren<T>();
        return return_value;
    }

    /// <summary>
    /// 查找与 name 相关的 UILabel.
    /// 
    /// 为兼容旧 UI 结构, 这里除了比较 label 自身名字, 还会比较它的父、祖父、曾祖父名字.
    /// 这说明项目中的 UILabel 常常嵌套较深, 且业务代码更关注"逻辑块名称"而不是文本组件本名.
    /// </summary>
    public static UILabel getLabelName(GameObject father, string name)
    {
        UILabel return_value = null;
        var all = father.transform.GetComponentsInChildren<UILabel>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == name 
                ||
                (all[i].transform.parent != null && all[i].transform.parent.name == name)
                ||
                (all[i].transform.parent.parent != null && all[i].transform.parent.parent.name == name)
                 ||
                (all[i].transform.parent.parent.parent != null && all[i].transform.parent.parent.parent.name == name)
                )
            {
                return_value = all[i];
            }
        }
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == name)
            {
                return_value = all[i];
            }
        }
        return return_value;
    }

    /// <summary>
    /// 根据主卡组数量, 计算四行布局时每一行应放多少张卡.
    /// 初始按 40 张平均分布, 41 张以后再依次补到四行中.
    /// </summary>
    internal static int[] get_decklieshuArray(int count)
    {
        int[] ret = new int[4];
        ret[0] = 10;
        ret[1] = 10;
        ret[2] = 10;
        ret[3] = 10;
        for (int i = 41; i <= count; i++)
        {
            int index = i % 4;
            index--;
            if (index == -1)
            {
                index = 3;
            }
            ret[index]++;
        }
        return ret;
    }



    /// <summary>
    /// 尝试按名字找到 UILabel 并设置文本；找不到时记录日志.
    /// 这里的 try 指"尽量设置", 不是异常保护.
    /// </summary>
    public static void trySetLableText(GameObject father, string name, string text)
    {
        var l = getLabelName(father, name);
        if (l != null)
        {
            l.text = text;
        }
        else
        {
            Program.DEBUGLOG("NO Lable"+ name);
        }
    }

    /// <summary>
    /// 尝试读取指定 UILabel 的文本；找不到时返回空字符串.
    /// </summary>
    public static string tryGetLableText(GameObject father, string name)
    {
        var l = getLabelName(father, name);
        if (l != null)
        {
            return l.text;
        }

        return "";
    }

    /// <summary>
    /// 字符串扩展: 按字符串分隔符切分, 并去掉空项.
    /// 这是对 string.Split(char[]) 的补充封装.
    /// </summary>
    public static string[] Split(this string str,string s)
    {
        return str.Split(new string[] { s }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// 为输入框或按钮注册"带 messageSystemValue 参数"的事件.
    /// 
    /// 典型用途: 
    /// - 某些弹窗或消息系统希望在点击/提交时回传上下文值.
    /// - 如果 name 对应的是 UIInput, 则会注册 onSubmit.
    /// - 如果 name2 对应一个按钮, 还会把同一逻辑挂到按钮点击上.
    /// - 否则回退为普通 UIButton 点击注册.
    /// 
    /// 这一套模式的核心目的是"少写桥接代码", 但代价是逻辑分发较隐式, 阅读时必须先理解控件名对应的真实类型.
    /// </summary>
    public static void registEvent(GameObject father, string name, Action<GameObject, Servant.messageSystemValue> function, Servant.messageSystemValue value,string name2="")
    {
        UIInput input = getByName<UIInput>(father, name);
        if (input != null)
        {
            MonoListenerRMS_ized d = input.gameObject.GetComponent<MonoListenerRMS_ized>();
            if (d == null)
            {
                d = input.gameObject.AddComponent<MonoListenerRMS_ized>();
            }
            d.actionInMono = function;
            d.value = value;
            input.onSubmit.Clear();
            input.onSubmit.Add(new EventDelegate(d, "function"));
            UIButton btns = getByName<UIButton>(father, name2);
            if (btns != null)
            {
                btns.onClick.Clear();
                btns.onClick.Add(new EventDelegate(d, "function"));
            }
            return;
        }
        UIButton btn = getByName<UIButton>(father, name);
        if (btn != null)
        {
            MonoListenerRMS_ized d = btn.gameObject.GetComponent<MonoListenerRMS_ized>();
            if (d == null)
            {
                d = btn.gameObject.AddComponent<MonoListenerRMS_ized>();
            }
            d.actionInMono = function;
            d.value = value;
            btn.onClick.Clear();
            btn.onClick.Add(new EventDelegate(d, "function"));
            return;
        }
    }



    /// <summary>
    /// 明确给按钮注册点击事件.
    /// 与通用 registEvent 相比, 这个版本更直接, 也更容易让调用者表达"我确定这里就是按钮".
    /// </summary>
    public static void registEventbtn(GameObject father, string name, Action function)
    {
        UIButton btn = getByName<UIButton>(father, name);
        if (btn != null)
        {
            MonoDelegate d = btn.gameObject.GetComponent<MonoDelegate>();
            if (d == null)
            {
                d = btn.gameObject.AddComponent<MonoDelegate>();
            }
            d.actionInMono = function;
            btn.onClick.Clear();
            btn.onClick.Add(new EventDelegate(d, "function"));
            return;
        }
    }

    /// <summary>
    /// 按控件名自动识别类型并注册事件.
    /// 
    /// 支持的控件类型: 
    /// - UISlider -> onChange
    /// - UIPopupList -> onChange
    /// - UIToggle -> onChange
    /// - UIInput -> onSubmit
    /// - UIScrollBar -> onChange
    /// - UIButton -> onClick
    /// 
    /// 这是 UIHelper 最常被调用的一组方法之一, 也是"开发效率高"的关键来源: 
    /// 业务层只写控件名和回调, 不用重复拿组件、加 MonoDelegate、绑 EventDelegate.
    /// 
    /// 但这里也有几个代价: 
    /// - 依赖节点命名和实际控件类型一致.
    /// - 每次注册前都要做一轮控件查找.
    /// - 多数情况下会 Clear 旧回调, 容易覆盖其他地方的监听.
    /// </summary>
    public static void registEvent(GameObject father, string name, Action function)
    {
        UISlider slider = getByName<UISlider>(father, name);    
        if (slider != null)
        {
            MonoDelegate d = slider.gameObject.GetComponent<MonoDelegate>();
            if (d == null)
            {
                d = slider.gameObject.AddComponent<MonoDelegate>();
            }
            d.actionInMono = function;
            slider.onChange.Add(new EventDelegate(d, "function"));
            return;
        }
        UIPopupList list = getByName<UIPopupList>(father, name);
        if (list != null)
        {
            MonoDelegate d = list.gameObject.GetComponent<MonoDelegate>();
            if (d == null)
            {
                d = list.gameObject.AddComponent<MonoDelegate>();
            }
            d.actionInMono = function;
            list.onChange.Add(new EventDelegate(d, "function"));
            return;
        }
        UIToggle tog = getByName<UIToggle>(father, name);
        if (tog != null)
        {
            MonoDelegate d = tog.gameObject.GetComponent<MonoDelegate>();
            if (d == null)
            {
                d = tog.gameObject.AddComponent<MonoDelegate>();
            }
            d.actionInMono = function;
            tog.onChange.Clear();
            tog.onChange.Add(new EventDelegate(d, "function"));
            return;
        }
        UIInput input = getByName<UIInput>(father, name);
        if (input != null)
        {
            MonoDelegate d = input.gameObject.GetComponent<MonoDelegate>();
            if (d == null)
            {
                d = input.gameObject.AddComponent<MonoDelegate>();
            }
            d.actionInMono = function;
            input.onSubmit.Clear();
            input.onSubmit.Add(new EventDelegate(d, "function"));
            return;
        }
        UIScrollBar bar = getByName<UIScrollBar>(father, name);
        if (bar != null)
        {
            MonoDelegate d = bar.gameObject.GetComponent<MonoDelegate>();
            if (d == null)
            {
                d = bar.gameObject.AddComponent<MonoDelegate>();
            }
            d.actionInMono = function;
            bar.onChange.Clear();
            bar.onChange.Add(new EventDelegate(d, "function"));
            return;
        }
        UIButton btn = getByName<UIButton>(father, name);
        if (btn != null)
        {
            MonoDelegate d = btn.gameObject.GetComponent<MonoDelegate>();
            if (d == null)
            {
                d = btn.gameObject.AddComponent<MonoDelegate>();
            }
            d.actionInMono = function;
            btn.onClick.Clear();
            btn.onClick.Add(new EventDelegate(d, "function"));
            return;
        }
    }

    /// <summary>
    /// 给按钮同时挂载 toolShift.shift 和自定义回调.
    /// 常用于按钮点击时先做一次视觉/位置切换, 再执行业务逻辑.
    /// </summary>
    public static void addButtonEvent_toolShift(GameObject father, string name, Action function)
    {
        UIButton btn = getByName<UIButton>(father, name);
        if (btn != null)
        {
            MonoDelegate d = btn.gameObject.GetComponent<MonoDelegate>();
            if (d == null)
            {
                d = btn.gameObject.AddComponent<MonoDelegate>();
            }
            d.actionInMono = function;
            btn.onClick.Clear();
            btn.onClick.Add(new EventDelegate(btn.gameObject.GetComponent<toolShift>(), "shift"));
            btn.onClick.Add(new EventDelegate(d, "function"));
        }
    }

    /// <summary>
    /// 给按钮注册"回传点击对象本身"的监听.
    /// 适合列表项、动态生成项等场景, 业务层可以直接从 GameObject.name 或其组件中读取上下文.
    /// </summary>
    public static void registClickListener(GameObject father, string name, Action<GameObject> ES_listenerForGameObject)
    {
        UIButton btn = getByName<UIButton>(father, name);
        if (btn != null)
        {
            MonoListener d = btn.gameObject.GetComponent<MonoListener>();
            if (d == null)
            {
                d = btn.gameObject.AddComponent<MonoListener>();
            }
            d.actionInMono = ES_listenerForGameObject;
            btn.onClick.Clear();
            btn.onClick.Add(new EventDelegate(d, "function"));
        }
    }

    /// <summary>
    /// 将一维索引转换为"第几行、第几列".
    /// x 表示行号, y 表示列号.
    /// </summary>
    public static Vector2 get_hang_lie(int index, int meihangdegeshu)
    {
        Vector2 return_value = Vector2.zero;
        return_value.x = (int)index / meihangdegeshu;
        return_value.y = index % meihangdegeshu;
        return return_value;
    }

    internal static Vector2 get_hang_lieArry(int v, int[] hangshu)
    {
        Vector2 return_value = Vector2.zero;
        for (int i = 0; i < 4; i++) 
        {
            if (v < hangshu[i])
            {
                return_value.x = i;
                return_value.y = v;
                return return_value;
            }
            else
            {
                v -= hangshu[i];
            }
        }
        return return_value;
    }

    public static int get_zuihouyihangdegeshu(int zongshu, int meihangdegeshu)
    {
        int re = 0;
        re = zongshu % meihangdegeshu;
        if (re == 0)
        {
            re = meihangdegeshu;
        }
        return re;
    }

    public static bool get_shifouzaizuihouyihang(int zongshu, int meihangdegeshu, int index)
    {
        return (int)((index) / meihangdegeshu) == (int)(zongshu / meihangdegeshu);
    }

    public static int get_zonghangshu(int zongshu, int meihangdegeshu)
    {
        return ((int)(zongshu - 1) / meihangdegeshu) + 1;
    }

    /// <summary>
    /// 给 UIScrollView 注册滚动通知.
    /// </summary>
    public static void registEvent(UIScrollView uIScrollView, Action function)
    {
        uIScrollView.onScrolled = new UIScrollView.OnDragNotification(function);
    }

    /// <summary>
    /// 给 UIScrollBar 注册变化事件.
    /// </summary>
    public static void registEvent(UIScrollBar scrollBar, Action function)
    {
        MonoDelegate d = scrollBar.gameObject.GetComponent<MonoDelegate>();
        if (d == null)
        {
            d = scrollBar.gameObject.AddComponent<MonoDelegate>();
        }
        d.actionInMono = function;
        scrollBar.onChange.Clear();
        scrollBar.onChange.Add(new EventDelegate(d, "function"));
    }

    /// <summary>
    /// 通过 UIEventTrigger 给对象的 BoxCollider 注册点击事件.
    /// 
    /// 这里注册的不是传入的根节点, 而是其子节点中的实际碰撞体对象.
    /// 这是 NGUI 项目里常见的一种做法: 真正接收鼠标事件的是 Collider 所在节点.
    /// </summary>
    public static void registUIEventTriggerForClick(GameObject gameObject, Action<GameObject> listenerForClicked)
    {
        BoxCollider boxCollider = gameObject.transform.GetComponentInChildren<BoxCollider>();
        boxCollider.gameObject.name = gameObject.name;
        if (boxCollider != null)
        {
            UIEventTrigger uIEventTrigger = boxCollider.gameObject.AddComponent<UIEventTrigger>();
            MonoListener d = boxCollider.gameObject.AddComponent<MonoListener>();
            d.actionInMono = listenerForClicked;
            uIEventTrigger.onClick.Add(new EventDelegate(d, "function"));
        }
    }

    /// <summary>
    /// 通过 UIEventTrigger 给对象注册鼠标悬停事件.
    /// </summary>
    public static void registUIEventTriggerForHoverOver(GameObject gameObject, Action<GameObject> listenerForHoverOver)
    {
        BoxCollider boxCollider = gameObject.transform.GetComponentInChildren<BoxCollider>();
        if (boxCollider != null)
        {
            UIEventTrigger uIEventTrigger = boxCollider.gameObject.AddComponent<UIEventTrigger>();
            MonoListener d = boxCollider.gameObject.AddComponent<MonoListener>();
            d.actionInMono = listenerForHoverOver;
            uIEventTrigger.onHoverOver.Add(new EventDelegate(d, "function"));
        }
    }

    /// <summary>
    /// 获取真正承载 UI 事件的 GameObject.
    /// 在当前项目里通常就是带 BoxCollider 的那一层节点.
    /// </summary>
    internal static GameObject getRealEventGameObject(GameObject gameObject)
    {
        GameObject re = null;
        BoxCollider boxCollider = gameObject.transform.GetComponentInChildren<BoxCollider>();
        if (boxCollider != null)
        {
            re = boxCollider.gameObject;
        }
        return re;
    }

    internal static GameObject getGameObject(GameObject gameObject, string name)
    {
        Transform t = getByName<Transform>(gameObject, name);
        if (t != null)
        {
            return t.gameObject;
        }
        else
        {
            return null;
        }
    }

    internal static void trySetLableText(GameObject gameObject, string p)
    {
        try
        {
            gameObject.GetComponentInChildren<UILabel>().text = p;
        }
        catch (Exception)   
        {
        }
    }

    internal static void registEvent(GameObject gameObject, Action act)
    {
        registEvent(gameObject, gameObject.name, act);
    }

    internal static void trySetLableTextList(GameObject father,string text)
    {
        try
        {
            var p = father.GetComponentInChildren<UITextList>();
            p.Clear();
            p.Add(text);
        }
        catch (Exception)
        {
            Program.DEBUGLOG("NO LableList");
        }
    }

    internal static int get_decklieshu(int zongshu)
    {
        int return_value = 10;
        for (int i = 0; i < 100; i++)
        {
            if ((zongshu + i) % 4 == 0)
            {
                return_value = (zongshu + i) / 4;
                break;
            }
        }
        return return_value;
    }

    internal static void clearITWeen(GameObject gameObject)
    {
        iTween[] iTweens = gameObject.GetComponents<iTween>();
        for (int i=0;i< iTweens.Length;i++)
        {
            MonoBehaviour.DestroyImmediate(iTweens[i]);
        }
    }

    internal static float get_left_right_index(float left, float right, int i, int count)
    {
        float return_value = 0;
        if (count == 1)
        {
            return_value = left + right;
            return_value /= 2;
        }
        else
        {
            return_value = left + (right - left) * (float)i / ((float)(count - 1));
        }
        return return_value; 
    }

    internal static float get_left_right_indexZuo(float v1, float v2, int v3, int count, int v4)
    {
        if (count >= v4)
        {
            return get_left_right_index(v1, v2, v3, count);
        }
        else
        {
            return get_left_right_index(v1, v2, v3, v4);
        }
    }

    internal static float get_left_right_indexEnhanced(float left, float right, int i, int count, int illusion)
    {
        float return_value = 0;
        if (count > illusion)
        {
            if (count == 1)
            {
                return_value = left + right;
                return_value /= 2;
            }
            else
            {
                return_value = left + (right - left) * (float)i / ((float)(count - 1));
            }
        }
        else
        {
            if (illusion == 1)
            {
                return_value = left + right;
                return_value /= 2;
            }
            else
            {
                float l = left;
                float r = right;
                float per = ((right - left) / (illusion - 1));
                float length = per * (count + 1);
                l = (left + right) / 2f - length / 2f;
                r = (left + right) / 2f + length / 2f;
                return_value = l + per * (float)(i + 1);
            }
        }
        return return_value;
    }

    internal static void registUIEventTriggerForMouseDown(GameObject gameObject, Action<GameObject> listenerForMouseDown)
    {
        BoxCollider boxCollider = gameObject.transform.GetComponentInChildren<BoxCollider>();
        if (boxCollider != null)
        {
            UIEventTrigger uIEventTrigger = boxCollider.gameObject.AddComponent<UIEventTrigger>();
            MonoListener d = boxCollider.gameObject.AddComponent<MonoListener>();
            d.actionInMono = listenerForMouseDown;
            uIEventTrigger.onPress.Add(new EventDelegate(d, "function"));
        }
    }

    /// <summary>
    /// 表情 / 头像贴图缓存.
    /// 键通常是文件名（不含扩展名）, 值为加载好的 Texture2D.
    /// </summary>
    public static Dictionary<string, Texture2D> faces = new Dictionary<string, Texture2D>();

    /// <summary>
    /// 扫描 texture/face 目录, 预加载所有 png 到 faces 缓存中.
    /// 
    /// 这是典型的"启动时多做一点, 运行时少查一点"的思路.
    /// 优点: 后续取表情时不必反复读磁盘.
    /// 缺点: 启动时会发生同步文件 IO, 如果目录很大, 启动成本会上升.
    /// </summary>
    internal static void iniFaces()
    {
        try
        {
            FileInfo[] fileInfos = (new DirectoryInfo("texture/face")).GetFiles();
            for (int i = 0; i < fileInfos.Length; i++)
            {
                if (fileInfos[i].Name.Length > 4)
                {
                    if (fileInfos[i].Name.Substring(fileInfos[i].Name.Length - 4, 4) == ".png")
                    {
                        string name = fileInfos[i].Name.Substring(0, fileInfos[i].Name.Length - 4);
                        if (!faces.ContainsKey(name))
                        {
                            try
                            {
                                faces.Add(name, UIHelper.getTexture2D("texture/face/" + fileInfos[i].Name));
                            }
                            catch (Exception e)
                            {
                                Debug.Log(e);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    /// <summary>
    /// 按名称获取表情贴图.
    /// 
    /// 如果缓存里没有对应名字, 则退化为根据名字字节和取模, 选择一个默认头像, 
    /// 这样可以保证"未知名称也能显示一个稳定结果", 避免界面出现空贴图.
    /// </summary>
    internal static Texture2D getFace(string name)
    {
        Texture2D re = null;
        if (faces.TryGetValue(name, out re))
        {
            if (re != null)
            {
                return re;
            }
        }
        byte[] buffer= System.Text.Encoding.UTF8.GetBytes(name);
        int sum = 0;
        for (int i=0;i< buffer.Length;i++)
        {
            sum += buffer[i];
        }
        sum = sum % 100;
        return Program.I().face.faces[sum];
    }

    /// <summary>
    /// 从磁盘读取图片文件并生成 Texture2D.
    /// 
    /// 特点: 
    /// - 直接走 FileStream + LoadImage, 简单直接.
    /// - 路径不存在时返回 null.
    /// - 属于同步磁盘读取, 不适合在频繁刷新 UI 的热点路径调用.
    /// 
    /// 这也是 UIHelper 的一个典型特征: 为了调用方便, 把资源读取能力直接塞进公共类中.
    /// </summary>
    public static Texture2D getTexture2D(string path) 
    {
        Texture2D pic = null;
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }
            FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
            file.Seek(0, SeekOrigin.Begin);
            byte[] data = new byte[file.Length];
            file.Read(data, 0, (int)file.Length);
            file.Close();
            file.Dispose();
            file = null;
            pic = new Texture2D(1024, 600);
            pic.LoadImage(data);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
        return pic;
    }


    /// <summary>
    /// 通过 localScale 在视觉上显示 / 隐藏按钮.
    /// 
    /// 这里不是 SetActive, 而是把缩放改为 0 或 1.
    /// 好处是简单, 且不一定会打断某些组件状态；
    /// 坏处是对象仍然存在于层级里, 调试时需要注意"它没消失, 只是缩成了 0".
    /// </summary>
    internal static void shiftButton(UIButton btn,bool enabled)
    {
        if (enabled)
        {
            btn.gameObject.transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            btn.gameObject.transform.localScale = new Vector3(0, 0, 0);
        }
        //try
        //{
        //    BoxCollider boxCollider = btn.gameObject.GetComponentInChildren<BoxCollider>();
        //    UILabel label = btn.gameObject.GetComponentInChildren<UILabel>();
        //    label.text = hint;
        //    boxCollider.enabled = enabled;
        //    if (enabled)
        //    {
        //        label.color = Color.white;
        //    }
        //    else
        //    {
        //        label.color = Color.gray;
        //    }
        //}
        //catch (Exception)   
        //{
        //}
    }

    /// <summary>
    /// 切换 UIToggle 的可点击 / 可改值状态, 并顺便更新提示文案和视觉颜色.
    /// 用于把一个 Toggle 临时变成"可见但不可操作"的提示控件.
    /// </summary>
    internal static void shiftUIToggle(UIToggle tog, bool canClick,bool canChange, string hint)      
    {
        try
        {
            tog.canChange = canChange;
            BoxCollider boxCollider = tog.gameObject.GetComponentInChildren<BoxCollider>();
            UILabel label = tog.gameObject.GetComponentInChildren<UILabel>();
            label.text = hint;
            boxCollider.enabled = canClick;
            if (canClick)
            {
                getByName<UISprite>(tog.gameObject, "Background").color= Color.white;
                //getByName<UISprite>(tog.gameObject, "Checkmark").color = Color.white;
            }
            else
            {
                getByName<UISprite>(tog.gameObject, "Background").color = Color.black;
                //getByName<UISprite>(tog.gameObject, "Checkmark").color = Color.gray;
            }
        }
        catch (Exception)
        {
        }
    }

    internal static string getBufferString(byte[] buffer)
    {
        string returnValue = "";
        foreach (var item in buffer)    
        {
            returnValue += ((int)item).ToString() + ".";
        }
        return returnValue;
    }

    internal static string getTimeString()
    {
        return (DateTime.Now.ToString("MM-dd「HH: mm: ss」"));
    }
    /// <summary>
    /// 项目内部布尔配置的字符串转布尔.
    /// 约定只有 "1" 表示 true, 其余一律 false.
    /// </summary>
    internal static bool fromStringToBool(string s)
    {
        return s == "1";
    }

    /// <summary>
    /// 布尔值转项目配置字符串: true -> "1", false -> "0".
    /// </summary>
    internal static string  fromBoolToString(bool s)
    {
        if (s)
        {
            return "1";
        }
        else
        {
            return "0";
        }
    }

    /// <summary>
    /// 将世界坐标点沿当前主相机视线方向前后平移一个距离.
    /// 
    /// 做法: 
    /// 1. 先把世界坐标投到屏幕坐标.
    /// 2. 只改 z 深度.
    /// 3. 再投回世界坐标.
    /// 
    /// 这类方法常用于: 
    /// - 让 3D 场景中的标记更贴近镜头
    /// - 调整数字、提示、特效的显示层次
    /// </summary>
    internal static Vector3 getCamGoodPosition(Vector3 v, float l)
    {
        Vector3 screenposition = Program.camera_game_main.WorldToScreenPoint(v);
        return Program.camera_game_main.ScreenToWorldPoint(new Vector3(screenposition.x, screenposition.y, screenposition.z + l));
    }


    public static int CompareTime(object x, object y)   
    {
        if (x == null && y == null)
        {
            return 0;
        }
        if (x == null)
        {
            return -1;
        }
        if (y == null)
        {
            return 1;
        }
        FileInfo xInfo = (FileInfo)x;
        FileInfo yInfo = (FileInfo)y;
        return yInfo.LastWriteTime.CompareTo(xInfo.LastWriteTime);
    }

    public static int CompareName(object x, object y)   
    {
        if (x == null && y == null)
        {
            return 0;
        }
        if (x == null)
        {
            return -1;
        }
        if (y == null)
        {
            return 1;
        }
        FileInfo xInfo = (FileInfo)x;
        FileInfo yInfo = (FileInfo)y;
        return xInfo.FullName.CompareTo(yInfo.FullName);
    }

    /// <summary>
    /// 播放一个音效文件.
    /// 
    /// 查找顺序: mp3 -> wav -> ogg.
    /// 其中 val 参数当前没有真正参与音量计算, 实际使用的是 setting.soundValue().
    /// 这意味着该参数更像历史遗留接口, 阅读代码时不要误以为传入 val 就会生效.
    /// </summary>
    internal static void playSound(string p, float val) 
    {
        if (Ocgcore.inSkiping) 
        {
            return;
        }
        string path = "sound/" + p + ".mp3";
        if (File.Exists(path) == false)
        {
            path = "sound/" + p + ".wav";
        }
        if (File.Exists(path) == false)
        {
            path = "sound/" + p + ".ogg";
        }
        if (File.Exists(path) == false)
        {
            return;
        }
        path = Environment.CurrentDirectory.Replace("\\", "/") + "/" + path;
        path = "file:///" + path;
        GameObject audio_helper = Program.I().ocgcore.create_s(Program.I().mod_audio_effect);
        audio_helper.GetComponent<audio_helper>().play(path, Program.I().setting.soundValue());
        Program.I().destroy(audio_helper,5f);
    }

    /// <summary>
    /// 把 GPS 位置信息转成玩家可读的中文区域描述.
    /// 例如: 对方 + 墓地 / 手牌 / 前场 / 后场 等.
    /// </summary>
    internal static string getGPSstringLocation(GPS p1)
    {
        string res = "";
        if (p1.controller == 0)
        {
            res += "";
        }
        else
        {
            res += InterString.Get("对方");
        }
        if ((p1.location & (UInt32)CardLocation.Deck) > 0)
        {
            res += InterString.Get("卡组");
        }
        if ((p1.location & (UInt32)CardLocation.Extra) > 0)
        {
            res += InterString.Get("额外");
        }
        if ((p1.location & (UInt32)CardLocation.Grave) > 0)
        {
            res += InterString.Get("墓地");
        }
        if ((p1.location & (UInt32)CardLocation.Hand) > 0)
        {
            res += InterString.Get("手牌");
        }
        if ((p1.location & (UInt32)CardLocation.MonsterZone) > 0)
        {
            res += InterString.Get("前场");
        }
        if ((p1.location & (UInt32)CardLocation.Removed) > 0)
        {
            res += InterString.Get("除外");
        }
        if ((p1.location & (UInt32)CardLocation.SpellZone) > 0)
        {
            res += InterString.Get("后场");
        }
        return res;
    }

    //internal static string getGPSstringPosition(GPS p1) 
    //{
    //    string res = "";
    //    if ((p1.location & (UInt32)CardLocation.Overlay) > 0)
    //    {
    //        res += InterString.Get("(被叠放)");
    //    }
    //    else
    //    {
    //        if ((p1.position & (UInt32)CardPosition.FaceUpAttack) > 0)
    //        {
    //            res += InterString.Get("(表侧攻击)");
    //        }
    //        else if ((p1.position & (UInt32)CardPosition.FaceUp_DEFENSE) > 0)
    //        {
    //            res += InterString.Get("(表侧守备)");
    //        }
    //        else if ((p1.position & (UInt32)CardPosition.FaceDownAttack) > 0)
    //        {
    //            res += InterString.Get("(里侧攻击)");
    //        }
    //        else if ((p1.position & (UInt32)CardPosition.FaceDown_DEFENSE) > 0)
    //        {
    //            res += InterString.Get("(里侧守备)");
    //        }
    //        else if ((p1.position & (UInt32)CardPosition.Attack) > 0)
    //        {
    //            res += InterString.Get("(攻击)");
    //        }
    //        else if ((p1.position & (UInt32)CardPosition.POS_DEFENSE) > 0)
    //        {
    //            res += InterString.Get("(守备)");
    //        }
    //        else if ((p1.position & (UInt32)CardPosition.FaceUp) > 0)
    //        {
    //            res += InterString.Get("(表侧)");
    //        }
    //        else if ((p1.position & (UInt32)CardPosition.POS_DEFENSE) > 0)
    //        {
    //            res += InterString.Get("(里侧)");
    //        }
    //    }

    //    return res;
    //}

    /// <summary>
    /// 生成用于日志 / 提示框显示的卡牌说明字符串.
    /// 内容包括: 所在区域 + 卡名超链接.
    /// green=true 时会套一层绿色富文本颜色标签.
    /// </summary>
    internal static string getGPSstringName(gameCard card, bool green = false)
    {
        string res = "";
        res += getGPSstringLocation(card.p) + "\n「" + getSuperName(card.get_data().Name, card.get_data().Id) + "」";
        if (green)
        {
            return "[00ff00]" + res + "[-]";
        }
        return res;
    }

    /// <summary>
    /// 生成带 url/u 标签的卡名富文本, 用于点击后根据 code 打开详情.
    /// </summary>
    internal static string getSuperName(string name,int code)
    {
        string res = "";
        res = "[url=" + code.ToString() + "][u]" + name + "[/u][/url]";
        return res;
    }

    /// <summary>
    /// 与 getSuperName 类似, 但会额外包一层书名号样式.
    /// </summary>
    internal static string getDName(string name, int code)  
    {
        string res = "";
        res = "「[url=" + code.ToString() + "][u]" + name + "[/u][/url]」";
        return res;
    }

    /// <summary>
    /// 计算两个对象在屏幕空间上的距离, 而不是世界空间距离.
    /// 更适合用于判断 UI 观感上的远近关系.
    /// </summary>
    internal static float getScreenDistance(GameObject a,GameObject b)
    {
        Vector3 sa = Program.camera_game_main.WorldToScreenPoint(a.transform.position);sa.z = 0;
        Vector3 sb = Program.camera_game_main.WorldToScreenPoint(b.transform.position);sb.z = 0;
        return Vector3.Distance(sa, sb);
    }

    /// <summary>
    /// 设置父节点, 并把整棵子树的 layer 统一为父节点 layer.
    /// 
    /// 这在 UI 项目里很重要: 
    /// - 仅仅 SetParent 后, 子节点 layer 可能仍停留在原值.
    /// - layer 不统一时, 可能会出现相机看不见、射线不命中、排序异常等问题.
    /// </summary>
    internal static void setParent(GameObject child, GameObject parent)
    {
        child.transform.SetParent(parent.transform, true);
        Transform[] Transforms = child.GetComponentsInChildren<Transform>();
        foreach (Transform achild in Transforms)
            achild.gameObject.layer = parent.layer;
    }

    /// <summary>
    /// 将一个世界坐标沿给定相机方向向镜头靠近指定距离.
    /// 与 getCamGoodPosition 类似, 只是这里显式传入相机.
    /// </summary>
    internal static Vector3 get_close(Vector3 input_vector, Camera cam, float l)
    {
        Vector3 o = Vector3.zero;
        Vector3 scr = cam.WorldToScreenPoint(input_vector);
        scr.z -= l;
        o = cam.ScreenToWorldPoint(scr);
        return o;
    }
}
