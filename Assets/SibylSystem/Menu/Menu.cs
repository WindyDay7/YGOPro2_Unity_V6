using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// 主菜单控制中心 (Menu)
/// 继承自 WindowServantSP(该框架底层实现的一种单例窗口服务类基类).
/// 
/// 【核心作用与职责】
/// 1. 负责管理游戏启动后的主界面逻辑, 它是各个子系统的枢纽入口(在线大厅、AI、残局、编辑卡组、设置等).
/// 2. 程序启动时的环境自动更新检查(访问服务器比对版本号).
/// 3. 与外部外置大厅(如萌卡对战平台 MyCard 等)进行被动式的 IPC(进程间通信), 核心方式为解析命令文件 command.shell.
/// </summary>
public class Menu : WindowServantSP 
{
    /// <summary>
    /// 【UI与逻辑协作连接点】: 初始化主菜单界面
    /// </summary>
    public override void initialize()
    {
        // 步骤1: 创建主菜单 UI 窗口
        // 读取并实例化 Program 全局管理器中记录的主菜单 Prefab (Program.I().new_ui_menu).
        createWindow(Program.I().new_ui_menu);
        
        // 步骤2: 绑定主菜单上的各个 NGUI 按钮事件.
        // 这就是核心协作方式！UIHelper.registEvent 是一个通过字符串在 Hierarchy 树形结构里去找同名 GameObject 的封装.
        // 找到 "setting_" 后, 强行获取其身上的 UIButton / UIEventTrigger, 并绑定对应的 onClick.
        // (这也就是为什么你不能乱改预制体内节点的名称, 一旦改名, 这里的依赖查找就会报错).
        UIHelper.registEvent(gameObject, "setting_", onClickSetting);
        UIHelper.registEvent(gameObject, "deck_", onClickSelectDeck);
        UIHelper.registEvent(gameObject, "online_", onClickOnline);
        UIHelper.registEvent(gameObject, "replay_", onClickReplay);
        UIHelper.registEvent(gameObject, "single_", onClickPizzle);
        UIHelper.registEvent(gameObject, "ai_", onClickAI);
        UIHelper.registEvent(gameObject, "exit_", onClickExit);
        
        // 步骤3: 启动协程: 在后台无感检查游戏版本更新
        Program.I().StartCoroutine(checkUpdate());
    }

    /// <summary>
    /// 显示主菜单时的回调
    /// </summary>
    public override void show()
    {
        // 调用基类让主界面的节点 setActive(true) 显示出来
        base.show();
        // 通知主程序进入充电或渲染待机状态(如果是移动端可以降低功耗等)
        Program.charge();
    }

    public override void hide()
    {
        base.hide();
    }

    // 存储从服务器获取到的更新链接和更新文本日志
    string upurl = "";
    string uptxt = "";
    
    /// <summary>
    /// 【模块协作】: 网络更新系统
    /// 通过读取本地 config/ver.txt 获取当前版本号与服务器地址.
    /// 向服务器请求文本文件比对第一行的版本号字符串.
    /// 如果不一致则会在帧函数触发更新提示框.
    /// </summary>
    IEnumerator checkUpdate()
    {
        yield return new WaitForSeconds(1); // 延迟1秒避免刚启动卡顿
        
        var verFile = File.ReadAllLines("config/ver.txt", Encoding.UTF8);
        if (verFile.Length != 2 || !Uri.IsWellFormedUriString(verFile[1], UriKind.Absolute))
        {
            Program.PrintToChat(InterString.Get("YGOPro2 自动更新: [ff5555]未设置更新服务器, 无法检查更新.[-]@n请从官网重新下载安装完整版以获得更新."));
            yield break;
        }
        string ver = verFile[0];
        string url = verFile[1];
        
        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("Cache-Control", "max-age=0, no-cache, no-store");
        www.SetRequestHeader("Pragma", "no-cache");
        yield return www.SendWebRequest();
        
        try
        {
            string result = www.downloadHandler.text;
            string[] lines = result.Replace("\r", "").Split('\n');
            string[] mats = lines[0].Split(new string[] { ":.:" }, StringSplitOptions.None);
            
            if (ver != mats[0])
            {
                // 版本号不匹配, 记录下载链接和后续的多行更新日志
                upurl = mats[1];
                for(int i = 1; i < lines.Length; i++)
                {
                    uptxt += lines[i] + "\n";
                }
            }
            else
            {
                Program.PrintToChat(InterString.Get("YGOPro2 自动更新: [55ff55]当前已是最新版本.[-]"));
            }
        }
        catch (System.Exception e)
        {
            Program.PrintToChat(InterString.Get("YGOPro2 自动更新: [ff5555]检查更新失败！[-]"));
        }
    }

    /// <summary>
    /// 【模块协作】: 响应 RMS (Request Message System) 原有框架的弹窗和消息队列封装
    /// </summary>
    public override void ES_RMS(string hashCode, List<messageSystemValue> result)
    {
        base.ES_RMS(hashCode, result);
        // 此 hashCode 定义从下方 preFrameFunction() 中的弹窗传来.
        // result[0].value == "1" 对应着提示框点选了 "yes".
        if (hashCode == "update" && result[0].value == "1")
        {
            Application.OpenURL(upurl); // 通过操作系统浏览器打开更新网页
        }
    }

    bool msgUpdateShowed = false;
    bool msgPermissionShowed = false;

    /// <summary>
    /// 【系统生命周期模块】: 帧前函数(相当于全局每帧 Update 之前的插入点)
    /// 用于循环检测: 弹窗触发、权限检查、外部文件命令解析.
    /// </summary>
    public override void preFrameFunction()
    {
        base.preFrameFunction();
        
        // 核心协作模块: 每帧定时检查外部大厅发来的命令
        Menu.checkCommend();
        
        // C盘安装导致的读写权限错误警告弹窗
        if (Program.noAccess && !msgPermissionShowed)
        {
            msgPermissionShowed = true;
            Program.PrintToChat(InterString.Get("[b][FF0000]NO ACCESS!! NO ACCESS!! NO ACCESS!![-][/b]") + "\n" + InterString.Get("访问程序目录出错, 软件大部分功能将无法使用.@n请将 YGOPro2 安装到其他文件夹, 或以管理员身份运行."));
        }
        // 版本过期的更新提示弹窗
        else if (upurl != "" && !msgUpdateShowed)
        {
            msgUpdateShowed = true;
            RMSshow_yesOrNo("update", InterString.Get("[b]发现更新！[/b]") + "\n" + uptxt + "\n" + InterString.Get("是否打开下载页面？"), 
                            new messageSystemValue { value = "1", hint = "yes" }, 
                            new messageSystemValue { value = "0", hint = "no" });
        }
    }

    // ==========================================
    // UI状态机协作区(按钮点击事件处理)
    // 这个区域的所有方法代表当前UI接收到了用户的按键请求.
    // 处理方式是: 去调用全局单例 Program.I().shiftToServant() 让游戏状态转移给下一个场景(Servant类).
    // 例如从菜单切换到卡组编辑状态, 或者直接退出.
    // ==========================================
    
    public void onClickExit()
    {
        Program.I().quit();
        Program.Running = false;
        TcpHelper.SaveRecord(); // 断开并清空可能存在的网络录像连接
        Process.GetCurrentProcess().Kill();
    }

    void onClickOnline()
    {
        Program.I().shiftToServant(Program.I().selectServer); // 切换至网络联机界面
    }

    void onClickAI()
    {
        Program.I().shiftToServant(Program.I().aiRoom);       // 切换至AI本地对战界面
    }

    void onClickPizzle()
    {
        Program.I().shiftToServant(Program.I().puzzleMode);   // 切换至残局模式
    }

    void onClickReplay()
    {
        Program.I().shiftToServant(Program.I().selectReplay); // 切换至录像库界面
    }

    void onClickSetting()
    {
        // 设置比较特殊, 它不是一个替换当前屏幕的状态模式, 而是把它叠加(Push)在顶层显示.
        Program.I().setting.show(); 
    }

    void onClickSelectDeck()
    {
        Program.I().shiftToServant(Program.I().selectDeck);   // 切换至卡组编辑界面
    }


    // ==========================================
    // 【最核心的外部平台协作机制区】:  commamd.shell 文件共享内存通信
    // ==========================================

    public static void deleteShell()
    {
        try
        {
            if (File.Exists("commamd.shell") == true)
            {
                File.Delete("commamd.shell");
            }
        }
        catch (Exception)
        {
        }
    }

    static int lastTime = 0;
    
    /// <summary>
    /// 命令文件(共享文件)监视器.
    /// 【解决什么痛点】: 如何让一个外部大厅(如萌卡对战平台 MyCard) 唤起 YGOPro2 并直接进入指定的房间？
    /// 【实现方案】: 
    /// 外置的萌卡系统如果匹配到了对手, 会往 YGOPro2 根目录生成并写入一个名为 "commamd.shell" 的文本文件, 
    /// 里面可能写着:  online 127.0.0.1 7911 password testroom 等参数.
    /// 
    /// 本函数由 Unity 引擎层每隔一秒钟执行一次, 偷偷探测这个文件有没有内容.一旦发现有内容, 
    /// 立马解析命令, 强行调用对应的联机、测试、或者残局逻辑, 然后迅速把该文件清空.
    /// 这是一个极度老旧但是能够跨语言、跨进程强行合作的笨方案.
    /// </summary>
    public static void checkCommend()
    {
        // 限制每隔 1000ms(1秒) 轮询一次本地磁盘, 减少磁盘 IO 压力
        if (Program.TimePassed() - lastTime > 1000)
        {
            lastTime = Program.TimePassed();
            
            // 防御性拦截: 确保主要大模块已经被 Unity 加载过了
            if (Program.I().selectDeck == null || Program.I().selectReplay == null || 
                Program.I().puzzleMode == null || Program.I().selectServer == null)
            {
                return;
            }
            
            // 尝试创建监视文件
            try
            {
                if (File.Exists("commamd.shell") == false)
                {
                    File.Create("commamd.shell").Close();
                }
            }
            catch (System.Exception e)
            {
                Program.noAccess = true;
                UnityEngine.Debug.Log(e);
            }
            
            string all = "";
            try
            {
                all = File.ReadAllText("commamd.shell", Encoding.UTF8);
                
                // 【解析命令字符串】: 处理引号括起来的参数或空格
                char[] parmChars = all.ToCharArray();
                bool inQuote = false;
                for (int index = 0; index < parmChars.Length; index++)
                {
                    if (parmChars[index] == '"')
                    {
                        inQuote = !inQuote;
                        parmChars[index] = '\n'; // 巧妙地把引号替换成换行符方便后续分割
                    }
                    if (!inQuote && parmChars[index] == ' ')
                        parmChars[index] = '\n'; // 巧妙地把空格替换成换行符
                }
                
                // 去除空行得到最终的参数数组
                string[] mats = (new string(parmChars)).Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (mats.Length > 0)
                {
                    switch (mats[0])
                    {
                        case "online": // 从萌卡接受联机对战指令: online ip port 密码 玩家名
                            if (mats.Length == 5)
                            {
                                UIHelper.iniFaces(); // 加载玩家自定义的外部贴图头像
                                Program.I().selectServer.KF_onlineGame(mats[1], mats[2], mats[3], mats[4]);
                            }
                            if (mats.Length == 6)
                            {
                                UIHelper.iniFaces();
                                Program.I().selectServer.KF_onlineGame(mats[1], mats[2], mats[3], mats[4], mats[5]);
                            }
                            break;
                            
                        case "edit":   // 从外部大厅请求直接打开组卡器编辑某卡组
                            if (mats.Length == 2)
                            {
                                Program.I().selectDeck.KF_editDeck(mats[1]); 
                            }
                            break;
                            
                        case "replay": // 从外部拉起直接看某一场高玩比赛录像
                            if (mats.Length == 2)
                            {
                                UIHelper.iniFaces();
                                Program.I().selectReplay.KF_replay(mats[1]); 
                            }
                            break;
                            
                        case "puzzle": // 从萌卡天梯题库接指令, 直接开始解一道残局题
                            if (mats.Length == 2)
                            {
                                UIHelper.iniFaces();
                                Program.I().puzzleMode.KF_puzzle(mats[1]); 
                            }
                            break;
                            
                        default:
                            break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Program.noAccess = true;
                UnityEngine.Debug.Log(e);
            }
            
            // 解析指令完成后, 把文件内容写入为空白字符串, 抹掉痕迹, 这代表“指令我已经签收”, 继续监听下次命令
            try
            {
                if (all != "")
                {
                    if (File.Exists("commamd.shell") == true)
                    {
                        File.WriteAllText("commamd.shell", "");
                    }
                }
            }
            catch (System.Exception e)
            {
                Program.noAccess = true;
                UnityEngine.Debug.Log(e);
            }
        }
    }
}