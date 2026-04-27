using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 项目的简易国际化 / 文本翻译工具。
/// 
/// 你可以把它理解成一个“运行时字典”：
/// - 输入原始中文（或默认文本）
/// - 返回当前语言下应该显示的文本
/// 
/// 它的特点是：
/// 1. 翻译数据直接来自一个普通文本文件，而不是复杂的数据库或 ScriptableObject。
/// 2. 文件格式很简单：每行一条“原文->译文”。
/// 3. 当程序请求一个还没被翻译过的文本时，它会自动把这条文本追加到翻译文件里，方便后续人工补翻译。
/// 
/// 因此，这个类既承担了“查翻译”的职责，也承担了“收集待翻译词条”的职责。
/// </summary>
public static class InterString
{
    /// <summary>
    /// 内存中的翻译表。
    /// key: 原始文本
    /// value: 翻译后的文本
    /// </summary>
    static Dictionary<string, string> translations = new Dictionary<string, string>();

    /// <summary>
    /// 当前翻译文件的路径。
    /// 在 initialize() 时赋值，后续 Get() 在发现缺失词条时，会把新词条追加到这个文件里。
    /// </summary>
    static string path;

    /// <summary>
    /// 是否已经完成初始化。
    /// 
    /// 注意：
    /// 这个字段只是一个简单标记，不会阻止重复 initialize()。
    /// 如果重复初始化，当前实现也不会主动清空 translations。
    /// 因此从使用约定上看，它更适合在程序启动时初始化一次。
    /// </summary>
    public static bool loaded = false;

    /// <summary>
    /// 读取翻译文件并建立内存词典，同时初始化一批 GameStringHelper 的常用静态文本。
    /// 
    /// 翻译文件格式：
    /// 原文->译文
    /// 
    /// 例如：
    /// 卡组->Deck
    /// 墓地->Graveyard
    /// 
    /// 处理流程：
    /// 1. 记录翻译文件路径。
    /// 2. 如果文件不存在，就先创建空文件。
    /// 3. 读出整个文本文件，按行切分。
    /// 4. 解析每一行中的“原文->译文”。
    /// 5. 把常用固定文案同步写入 GameStringHelper，方便别处直接使用。
    /// </summary>
    /// <param name="path">翻译文本文件的路径。</param>
    public static void initialize(string path)
    {
        InterString.path = path;

        // 如果翻译文件不存在，就先创建一个空文件。
        // 这样后面无论是读取还是追加缺失词条，都有明确的落点。
        if (File.Exists(path) == false)
        {
            File.Create(path).Close();
        }

        // 一次性把整个翻译文件读进来。
        // 文件中每一行约定为：原文->译文
        string txtString = File.ReadAllText(path);
        string[] lines = txtString.Replace("\r", "").Split("\n");
        for (int i = 0; i < lines.Length; i++)
        {
            // 这里使用固定分隔符“->”拆分原文和译文。
            // mats[0] 是原文，mats[1] 是译文。
            string[] mats = lines[i].Split("->");
            if (mats.Length == 2)
            {
                if (!translations.ContainsKey(mats[0]))
                {
                    translations.Add(mats[0], mats[1]);
                }
            }
        }

        // 下面这一段相当于给 GameStringHelper 做“静态文本预热”：
        // 这些文案在项目里是高频使用的，因此初始化后直接缓存一份，
        // 其他模块就不需要每次都手动写 InterString.Get("xxx")。
        GameStringHelper.xilie = Get("系列：");
        GameStringHelper.opHint = Get("*控制权经过转移");
        GameStringHelper.licechuwai= Get("*里侧表示的除外卡片");
        GameStringHelper.biaoceewai = Get("*表侧表示的额外卡片");
        GameStringHelper.teshuzhaohuan= Get("*被特殊召唤出场");
        GameStringHelper.yijingqueren = Get("卡片展示简表※  ");
        GameStringHelper._chaoliang = Get("超量：");
        GameStringHelper._ewaikazu = Get("额外卡组：");
        GameStringHelper._fukazu = Get("副卡组：");
        GameStringHelper._guaishou = Get("怪兽：");
        GameStringHelper._mofa = Get("魔法：");
        GameStringHelper._ronghe = Get("融合：");
        GameStringHelper._lianjie = Get("连接：");
        GameStringHelper._tongtiao = Get("同调：");
        GameStringHelper._xianjing = Get("陷阱：");
        GameStringHelper._zhukazu = Get("主卡组：");

        GameStringHelper.kazu = Get("卡组");
        GameStringHelper.mudi = Get("墓地");
        GameStringHelper.chuwai = Get("除外");
        GameStringHelper.ewai = Get("额外");
        GameStringHelper.SemiNomi = Get("未正规召唤");
        //GameStringHelper.diefang = Get("叠放");
        GameStringHelper._wofang = Get("我方");
        GameStringHelper._duifang = Get("对方");
        loaded = true;
    }

    /// <summary>
    /// 获取某个原始文本的翻译结果。
    /// 
    /// 规则如下：
    /// 1. 如果翻译表里已经有对应项，则直接返回译文。
    /// 2. 如果没有对应项，且 original 不为空：
    ///    - 自动把“原文->原文”追加到翻译文件中
    ///    - 同时把这条内容加入内存字典
    ///    - 返回原文本身
    /// 3. 会顺手处理两个项目约定的占位写法：
    ///    - @n  转成换行
    ///    - @ui 被去掉
    /// 
    /// 这意味着它不仅是“查询函数”，还是“翻译词条采集函数”。
    /// </summary>
    /// <param name="original">原始文本 / 默认文本。</param>
    /// <returns>翻译后的文本；如果没有翻译，则返回原文。</returns>
    public static string Get(string original)
    {

        string return_value = original;
        if (translations.TryGetValue(original, out return_value))
        {
            // 命中翻译后，还会做一层项目约定的占位符替换。
            // @n 代表换行；@ui 代表需要剔除的特殊标记。
            return return_value.Replace("@n", "\r\n").Replace("@ui", "");
        }
        else if (original != "")
        {
            try
            {
                // 缺失词条时，自动把“原文->原文”补写到翻译文件里。
                // 这样翻译人员后续就能直接在文件中把右侧原文改成目标语言。
                File.AppendAllText(path, original + "->" + original + "\r\n");
            }
            catch
            {
                // 常见原因是：没有文件写入权限、路径不可写、目录只读等。
                // 项目里通过这个标记告诉外部“翻译文件落盘失败了”。
                Program.noAccess = true;
            }

            // 即使写文件失败，也会先把这条内容放进内存字典，
            // 保证本次运行期间再次请求同一文本时，不会重复追加。
            translations.Add(original, original);
            return original.Replace("@n", "\r\n").Replace("@ui", "");
        }
        else
            return original;
    }

    /// <summary>
    /// 获取翻译结果，并把结果中的占位符“[?]”替换成指定文本。
    /// 
    /// 适合处理这类模板句：
    /// 你获得了[?]
    /// 
    /// 调用：
    /// Get("你获得了[?]", "青眼白龙")
    /// 结果：
    /// 你获得了青眼白龙
    /// </summary>
    /// <param name="original">带有模板占位符的原始文本。</param>
    /// <param name="replace">要替换进 [?] 的文本。</param>
    /// <returns>替换后的最终文本。</returns>
    public static string Get(string original, string replace)
    {
        return Get(original).Replace("[?]", replace);
    }

}
