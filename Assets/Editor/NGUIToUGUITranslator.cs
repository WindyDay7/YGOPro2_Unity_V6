using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

// 1. 定义承载 UI 树状结构的数据模型
[System.Serializable]
public class UIDataNode
{
    public string name;
    public bool isActive;
    public string type; 
    
    public float posX, posY, posZ;
    public float scaleX, scaleY, scaleZ;
    public int width, height;
    
    public string text;
    public string spriteName;
    public string colorHex;

    public List<UIDataNode> children = new List<UIDataNode>();
}

// 2. 主工具面板与核心逻辑
public class NGUIToUGUITool : EditorWindow
{
    private string apiKey = "sk-2af0ba2c32aa4bf498daada743d7d112"; 
    private string apiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions"; 
    private string modelName = "qwen3.5-122b-a10b";

    [MenuItem("Tools/AI 辅助 NGUI 转 UGUI")]
    public static void ShowWindow()
    {
        GetWindow<NGUIToUGUITool>("NGUI -> UGUI");
    }

    private void OnGUI()
    {
        GUILayout.Label("大模型 UI 转换器 (递归版)", EditorStyles.boldLabel);
        
        apiKey = EditorGUILayout.TextField("API Key", apiKey);
        apiUrl = EditorGUILayout.TextField("API URL", apiUrl);
        modelName = EditorGUILayout.TextField("Model Name", modelName);

        EditorGUILayout.Space(10);
        
        GameObject selected = Selection.activeGameObject;
        if (selected != null)
        {
            GUILayout.Label($"当前选中: {selected.name}", EditorStyles.helpBox);
        }
        else
        {
            GUILayout.Label("请在 Project 窗口中选中一个 NGUI Prefab", EditorStyles.helpBox);
        }

        GUI.enabled = selected != null;
        if (GUILayout.Button("一键转换选中 Prefab", GUILayout.Height(40)))
        {
            string assetPath = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab"))
            {
                EditorUtility.DisplayDialog("提示", "请在 Project 窗口中选中一个 .prefab 文件！", "确定");
                return;
            }
            _ = ProcessConversionAsync(selected, assetPath);
        }
        GUI.enabled = true;
    }

    private async Task ProcessConversionAsync(GameObject nguiPrefab, string originalPath)
    {
        try
        {
            EditorUtility.DisplayProgressBar("AI 转换中", "1. 递归提取 NGUI 树状结构...", 0.2f);
            
            // 1. 递归提取数据
            UIDataNode rootNode = ExtractNodeRecursive(nguiPrefab.transform);
            string nguiJson = JsonConvert.SerializeObject(rootNode, Formatting.Indented);
            
            EditorUtility.DisplayProgressBar("AI 转换中", "2. 呼叫大模型进行逻辑与锚点转换...", 0.5f);
            
            // 2. 调用大模型
            string uguiJson = await CallLLMForConversion(nguiJson);
            
            EditorUtility.DisplayProgressBar("AI 转换中", "3. 递归重建 UGUI 并保存...", 0.8f);
            
            // 3. 根据 AI 返回的 JSON 重建节点
            string newName = nguiPrefab.name + "_UGUI";
            GameObject uguiRoot = BuildUGUIFromJSON(uguiJson, newName);

            if (uguiRoot == null) throw new System.Exception("UGUI 根节点生成失败，请检查大模型返回的 JSON 格式。");

            // 4. 保存为新 Prefab
            string newPath = originalPath.Replace(".prefab", "_UGUI.prefab");
            newPath = AssetDatabase.GenerateUniqueAssetPath(newPath); 
            
            PrefabUtility.SaveAsPrefabAsset(uguiRoot, newPath);
            DestroyImmediate(uguiRoot); // 清理场景临时对象

            EditorUtility.ClearProgressBar();
            
            // 5. 选中新生成的 Prefab
            Object newPrefabObj = AssetDatabase.LoadAssetAtPath<Object>(newPath);
            Selection.activeObject = newPrefabObj;
            EditorGUIUtility.PingObject(newPrefabObj);

            EditorUtility.DisplayDialog("成功", "转换完毕！新的 UGUI Prefab 已生成。", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"转换失败: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("错误", "转换失败，请查看 Console 日志。", "确定");
        }
    }

    // --- 提取逻辑 (Extractor) ---
    private UIDataNode ExtractNodeRecursive(Transform current)
    {
        UIDataNode node = new UIDataNode
        {
            name = current.gameObject.name,
            isActive = current.gameObject.activeSelf,
            posX = current.localPosition.x,
            posY = current.localPosition.y,
            posZ = current.localPosition.z,
            scaleX = current.localScale.x,
            scaleY = current.localScale.y,
            scaleZ = current.localScale.z
        };

        // 注意：这里使用了反射风格的组件获取，为了防止你的工程如果没有引 NGUI 命名空间时报错。
        // 如果你的环境里可以直接使用 UILabel，可以将这里改为标准的 GetComponent<UILabel>()
        Component label = current.GetComponent("UILabel");
        Component sprite = current.GetComponent("UISprite");
        Component panel = current.GetComponent("UIPanel");

        if (label != null)
        {
            node.type = "UILabel";
            node.text = GetPropertyValue(label, "text")?.ToString();
            node.colorHex = ColorUtility.ToHtmlStringRGBA((Color)GetPropertyValue(label, "color"));
            node.width = (int)GetPropertyValue(label, "width");
            node.height = (int)GetPropertyValue(label, "height");
        }
        else if (sprite != null)
        {
            node.type = "UISprite";
            node.spriteName = GetPropertyValue(sprite, "spriteName")?.ToString();
            node.colorHex = ColorUtility.ToHtmlStringRGBA((Color)GetPropertyValue(sprite, "color"));
            node.width = (int)GetPropertyValue(sprite, "width");
            node.height = (int)GetPropertyValue(sprite, "height");
        }
        else if (panel != null)
        {
            node.type = "UIPanel";
        }
        else
        {
            node.type = "GameObject"; 
        }

        foreach (Transform child in current)
        {
            node.children.Add(ExtractNodeRecursive(child));
        }

        return node;
    }

    // 用于安全获取 NGUI 属性的反射辅助方法
    private object GetPropertyValue(Component comp, string propertyName)
    {
        var prop = comp.GetType().GetProperty(propertyName);
        if (prop != null) return prop.GetValue(comp, null);
        
        var field = comp.GetType().GetField(propertyName);
        if (field != null) return field.GetValue(comp);
        
        return null;
    }

    // --- 重建逻辑 (Builder) ---
    private GameObject BuildUGUIFromJSON(string json, string rootName)
    {
        UIDataNode rootNode = JsonConvert.DeserializeObject<UIDataNode>(json);
        if (rootNode == null) return null;

        GameObject uguiRoot = BuildNodeRecursive(rootNode, null);
        uguiRoot.name = rootName; 

        if (uguiRoot.GetComponent<Canvas>() == null)
        {
            uguiRoot.AddComponent<Canvas>();
            uguiRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
            uguiRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        return uguiRoot;
    }

    private GameObject BuildNodeRecursive(UIDataNode nodeData, Transform parent)
    {
        GameObject go = new GameObject(nodeData.name);
        go.SetActive(nodeData.isActive);
        
        if (parent != null) go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.localPosition = new Vector3(nodeData.posX, nodeData.posY, nodeData.posZ);
        rect.localScale = new Vector3(nodeData.scaleX, nodeData.scaleY, nodeData.scaleZ);
        rect.sizeDelta = new Vector2(nodeData.width, nodeData.height);

        // 根据大模型修改后的 type 挂载组件
        if (nodeData.type == "TextMeshProUGUI")
        {
            var textComp = go.AddComponent<TMPro.TextMeshProUGUI>();
            textComp.text = nodeData.text;
            textComp.raycastTarget = false;
            
            if (!string.IsNullOrEmpty(nodeData.colorHex) && ColorUtility.TryParseHtmlString("#" + nodeData.colorHex, out Color color))
            {
                textComp.color = color;
            }
        }
        else if (nodeData.type == "Image")
        {
            var imgComp = go.AddComponent<UnityEngine.UI.Image>();
            imgComp.raycastTarget = false;
            
            if (!string.IsNullOrEmpty(nodeData.colorHex) && ColorUtility.TryParseHtmlString("#" + nodeData.colorHex, out Color color))
            {
                imgComp.color = color;
            }
            // 这里可以添加逻辑：通过 nodeData.spriteName 去图集加载对应的 Sprite 并赋值给 imgComp.sprite
        }

        if (nodeData.children != null)
        {
            foreach (UIDataNode childData in nodeData.children)
            {
                BuildNodeRecursive(childData, go.transform);
            }
        }
        return go;
    }

    // --- 大模型网络请求 ---
    // --- 大模型网络请求 ---
    private async Task<string> CallLLMForConversion(string sourceJson)
    {
        string systemPrompt = @"你是一个资深的 Unity UI 专家。
我将发送一个基于 NGUI 的深度嵌套的 JSON 树状结构。你的任务是将其转换为 UGUI 结构，并原样返回完整的 JSON。
规则：
1. 绝对不要删减、打乱 children 列表中的任何节点，保持整棵树的完整性。
2. 将 type 'UILabel' 替换为 'TextMeshProUGUI'。
3. 将 type 'UISprite' 替换为 'Image'。
4. 将 UIPanel 直接转换为普通 GameObject（不需要添加 CanvasRenderer），因为我们会在根节点统一添加 Canvas 组件。
5. 将 UIButton 转换为 Image + Button 组件的组合（如果你的输入里有的话）。
6. 其他节点按照 NGUI 的结构找到对应的 UGUI 组件, 原样转换为 UGUI，保持所有属性（如位置、缩放、宽高、颜色等）不变。
7. 调整坐标和宽高以适应 UGUI（如果必要）。
8. 只输出纯 JSON 字符串，不要任何 Markdown 标记 (不要输出 ```json)。";

        // 【核心修复点】: 废弃手动拼接，改用匿名对象进行安全的 JSON 序列化
        var requestData = new
        {
            model = modelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                // sourceJson 本身就是我们要发给大模型的文本内容
                new { role = "user", content = sourceJson } 
            },
            temperature = 0.1f
        };

        // 这样序列化出来的 JSON，回车符会被完美转义为 \n，引号也会被正确处理，绝对符合标准
        string requestPayload = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new System.Exception($"API 请求错误 ({request.responseCode}): {request.error}\n详情: {request.downloadHandler.text}");
            }

            var responseObj = JsonConvert.DeserializeObject<LLMResponse>(request.downloadHandler.text);
            if (responseObj != null && responseObj.choices != null && responseObj.choices.Length > 0)
            {
                string content = responseObj.choices[0].message.content;
                // 剔除大模型喜欢加的 Markdown 代码块包围符
                content = content.Replace("```json", "").Replace("```", "").Trim();
                return content;
            }
            throw new System.Exception("无法解析大模型的返回值。");
        }
    }
    [System.Serializable] private class LLMResponse { public Choice[] choices; }
    [System.Serializable] private class Choice { public Message message; }
    [System.Serializable] private class Message { public string content; }
}