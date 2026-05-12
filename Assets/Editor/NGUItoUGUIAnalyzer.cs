using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public class NGUItoUGUIAnalyzer : EditorWindow
{
    [MenuItem("GameObject/🛠️ 分析 NGUI 到 UGUI 映射规则", false, 0)]
    public static void AnalyzeSelectedPrefab()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("提示", "请在 Hierarchy 或 Project 中选中一个带有 NGUI 组件的 GameObject！", "确定");
            return;
        }

        string savePath = EditorUtility.SaveFilePanel("保存分析报告", "Assets", $"{selected.name}_NGUI_转换报告", "md");
        if (string.IsNullOrEmpty(savePath)) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"# NGUI to UGUI 转换分析报告");
        sb.AppendLine($"**目标对象:** `{selected.name}`");
        sb.AppendLine($"**生成时间:** `{System.DateTime.Now}`\n");
        sb.AppendLine("---");

        AnalyzeNode(selected, sb, 0);

        File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>分析完成！</color> 报告已保存至: {savePath}");
    }

    static void AnalyzeNode(GameObject go, StringBuilder sb, int depth)
    {
        string indent = new string(' ', depth * 4);
        sb.AppendLine($"\n{indent}### 📦 [{go.name}]");

        Component[] components = go.GetComponents<Component>();
        bool hasNGUI = false;

        foreach (var comp in components)
        {
            if (comp == null) continue;
            string typeName = comp.GetType().Name;

            // 过滤 NGUI 核心组件
            if (typeName.StartsWith("UI") && typeName != "UIRoot" && typeName != "UICamera")
            {
                hasNGUI = true;
                AnalyzeComponent(comp, typeName, sb, indent);
            }
        }

        if (!hasNGUI && components.Length > 1)
        {
            sb.AppendLine($"{indent}- *纯节点或无 NGUI 核心UI组件，映射为普通 `RectTransform`*");
        }

        foreach (Transform child in go.transform)
        {
            AnalyzeNode(child.gameObject, sb, depth + 1);
        }
    }

    static void AnalyzeComponent(Component comp, string typeName, StringBuilder sb, string indent)
    {
        sb.AppendLine($"{indent}- **检测到 NGUI 组件**: `{typeName}`");
        SerializedObject so = new SerializedObject(comp);

        // 获取通用的 Widget 属性 (宽高)
        SerializedProperty widthProp = so.FindProperty("mWidth");
        SerializedProperty heightProp = so.FindProperty("mHeight");
        SerializedProperty depthProp = so.FindProperty("mDepth");
        
        string sizeStr = (widthProp != null && heightProp != null) ? $"{widthProp.intValue} x {heightProp.intValue}" : "未知";
        string depthStr = depthProp != null ? depthProp.intValue.ToString() : "未知";

        switch (typeName)
        {
            case "UITexture":
                sb.AppendLine($"{indent}  - **UGUI 替代方案**: `RawImage`");
                sb.AppendLine($"{indent}  - **参数映射**:");
                sb.AppendLine($"{indent}    - NGUI 尺寸 ({sizeStr}) -> UGUI `RectTransform` 的 Width/Height");
                sb.AppendLine($"{indent}    - NGUI Depth ({depthStr}) -> 转换为 **Hierarchy 面板中的节点上下顺序** (越靠下显示在越上层)");
                SerializedProperty texProp = so.FindProperty("mTexture");
                string texName = (texProp != null && texProp.objectReferenceValue != null) ? texProp.objectReferenceValue.name : "None";
                sb.AppendLine($"{indent}    - Texture: `{texName}` -> 赋给 RawImage 的 Texture 属性");
                
                SerializedProperty matProp = so.FindProperty("mMat");
                if (matProp != null && matProp.objectReferenceValue != null)
                {
                    sb.AppendLine($"{indent}    - **警告**: 检测到自定义材质 `{matProp.objectReferenceValue.name}`，需要将该材质挂载到 RawImage 的 Material 槽位中。");
                }
                break;

            case "UISprite":
                sb.AppendLine($"{indent}  - **UGUI 替代方案**: `Image`");
                sb.AppendLine($"{indent}  - **参数映射**:");
                sb.AppendLine($"{indent}    - NGUI 尺寸 ({sizeStr}) -> UGUI `RectTransform` 的 Width/Height");
                SerializedProperty spriteProp = so.FindProperty("mSpriteName");
                string spriteName = spriteProp != null ? spriteProp.stringValue : "None";
                SerializedProperty typeProp = so.FindProperty("mType");
                string imageType = typeProp != null ? ((NGUISpriteType)typeProp.enumValueIndex).ToString() : "Simple";
                sb.AppendLine($"{indent}    - Sprite Name: `{spriteName}` -> 需要从图集切分出 Sprite 赋给 Image 的 Source Image");
                sb.AppendLine($"{indent}    - Image Type: `{imageType}` -> 对应 UGUI Image 的 Image Type (Simple/Sliced/Tiled/Filled)");
                break;

            case "UILabel":
                sb.AppendLine($"{indent}  - **UGUI 替代方案**: `TextMeshProUGUI` (强烈推荐) 或 `Text`");
                SerializedProperty textProp = so.FindProperty("mText");
                string textContent = textProp != null ? textProp.stringValue.Replace("\n", "\\n") : "";
                if (textContent.Length > 20) textContent = textContent.Substring(0, 20) + "...";
                sb.AppendLine($"{indent}  - **参数映射**:");
                sb.AppendLine($"{indent}    - 文本内容: \"{textContent}\"");
                sb.AppendLine($"{indent}    - NGUI 尺寸 ({sizeStr}) -> UGUI `RectTransform` 框大小");
                SerializedProperty fontProp = so.FindProperty("mFont");
                sb.AppendLine($"{indent}    - **注意**: NGUI 字体需要通过 Window -> TextMeshPro -> Font Asset Creator 重新生成 SDF 字体文件。");
                break;

            case "UIButton":
                sb.AppendLine($"{indent}  - **UGUI 替代方案**: `Button` (配合 Image/RawImage 使用)");
                sb.AppendLine($"{indent}  - **注意事项**: ");
                sb.AppendLine($"{indent}    - UGUI 的 Button 组件只处理交互逻辑，必须和 Image/Text 挂在同一个（或父子）节点。");
                sb.AppendLine($"{indent}    - 需要手动将 NGUI 的 `OnClick` 脚本方法重新绑定到 UGUI Button 的 `OnClick()` UnityEvent 面板中。");
                break;

            case "UIGrid":
            case "UITable":
                sb.AppendLine($"{indent}  - **UGUI 替代方案**: `GridLayoutGroup` / `VerticalLayoutGroup` / `HorizontalLayoutGroup`");
                sb.AppendLine($"{indent}  - **注意事项**: 配合 `ContentSizeFitter` 使用以实现动态尺寸。");
                break;
                
            case "UIScrollView":
                sb.AppendLine($"{indent}  - **UGUI 替代方案**: `ScrollRect`");
                sb.AppendLine($"{indent}  - **注意事项**: UGUI 的 ScrollView 结构更复杂（包含 Viewport 遮罩层 和 Content 容器层），需要重新搭建节点结构。");
                break;

            default:
                sb.AppendLine($"{indent}  - **UGUI 替代方案**: 需手动分析对应逻辑。");
                break;
        }
    }

    // 模拟 NGUI 的 SpriteType 枚举以防止报错
    enum NGUISpriteType { Simple, Sliced, Tiled, Filled, Advanced }
}