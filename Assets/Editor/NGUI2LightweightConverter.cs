using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Reflection;

namespace YGOPro.Editor
{
    public class NGUI2LightweightConverter : EditorWindow
    {
        [MenuItem("Tools/YGOPro UI Migration/1. Convert Selected NGUI Panel to Lightweight UGUI")]
        public static void ShowWindow()
        {
            GetWindow<NGUI2LightweightConverter>("UI Converter");
        }

        private Material lightweightMaterial;

        private void OnGUI()
        {
            GUILayout.Label("NGUI 向 轻量级 UGUI+Shader 转换工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("提示：\n1. 选中要转换的预制体或节点。\n2. 点击下面的「递归转换」即可一键替换所有子节点的 NGUI 属性。\n(已增加自动图集裁剪(UV)与原位置轴心对齐修复)", MessageType.Info);
            
            lightweightMaterial = (Material)EditorGUILayout.ObjectField("目标 Material (可选)", lightweightMaterial, typeof(Material), false);
            
            if (GUILayout.Button("仅转换选中对象本身", GUILayout.Height(30)))
            {
                ConvertSelected(false);
            }
            
            if (GUILayout.Button("一键递归转换 (包含选中对象及其所有子节点!)", GUILayout.Height(45)))
            {
                ConvertSelected(true);
            }
        }

        private void ConvertSelected(bool recursive)
        {
            if (Selection.objects == null || Selection.objects.Length == 0)
            {
                Debug.LogWarning("[UI Converter] 请先在 Hierarchy 中选中至少一个 GameObject!");
                return;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("UICanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Debug.Log("[UI Converter] 自动创建了全局 UICanvas。");
            }

            foreach (GameObject obj in Selection.gameObjects)
            {
                if (recursive)
                {
                    Transform[] allChildren = obj.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in allChildren)
                    {
                        ConvertSingleObject(child.gameObject, canvas);
                    }
                }
                else
                {
                    ConvertSingleObject(obj, canvas);
                }

                // 修复根节点的缩放和位置问题
                // NGUI 的 UIRoot 会将第一层缩放为 0.003 等极小的值，切换到 UGUI Canvas 后我们需要将其恢复为 1
                if (obj.transform.parent != null && obj.transform.parent.GetComponent<Canvas>() != null)
                {
                    obj.transform.localScale = Vector3.one;
                    obj.transform.localPosition = Vector3.zero;
                }
            }
        }

        private void ConvertSingleObject(GameObject obj, Canvas rootCanvas)
        {
            Undo.RecordObject(obj, "Convert to Lightweight UGUI");

            // 1. 强力剥离残留的 NGUI 容器组件
            Component panel = obj.GetComponent("UIPanel");
            if (panel != null) DestroyImmediate(panel);

            Component dragObj = obj.GetComponent("UIDragObject");
            if (dragObj != null) DestroyImmediate(dragObj);

            Collider col = obj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            // 查找 NGUI 的渲染组件
            Component nguiWidget = obj.GetComponent("UIWidget");
            if (nguiWidget == null)
            {
                return;
            }

            System.Type widgetType = nguiWidget.GetType();

            if (widgetType.Name == "UILabel")
            {
                return; // 暂不处理文字类型
            }

            // 读取 NGUI 参数
            Color widgetColor = Color.white;
            int width = 100, height = 100;
            Texture mainTexture = null;
            Vector2 pivotOffset = new Vector2(0.5f, 0.5f);
            Rect uvRect = new Rect(0, 0, 1, 1);

            PropertyInfo colorProp = widgetType.GetProperty("color");
            if (colorProp != null) widgetColor = (Color)colorProp.GetValue(nguiWidget, null);
            
            PropertyInfo wProp = widgetType.GetProperty("width");
            if (wProp != null) width = (int)wProp.GetValue(nguiWidget, null);
            
            PropertyInfo hProp = widgetType.GetProperty("height");
            if (hProp != null) height = (int)hProp.GetValue(nguiWidget, null);

            // 提取轴心 (Pivot)
            PropertyInfo pivotProp = widgetType.GetProperty("pivotOffset");
            if (pivotProp != null) pivotOffset = (Vector2)pivotProp.GetValue(nguiWidget, null);

            PropertyInfo texProp = widgetType.GetProperty("mainTexture");
            if (texProp != null) mainTexture = texProp.GetValue(nguiWidget, null) as Texture;

            // 特殊处理 UISprite：提取图集切片 UV
            if (widgetType.Name == "UISprite" && mainTexture != null)
            {
                MethodInfo getSpriteMethod = widgetType.GetMethod("GetSprite", BindingFlags.Public | BindingFlags.Instance);
                if (getSpriteMethod != null)
                {
                    object spriteData = getSpriteMethod.Invoke(nguiWidget, null);
                    if (spriteData != null)
                    {
                        System.Type spriteDataType = spriteData.GetType();
                        int sx = (int)spriteDataType.GetField("x").GetValue(spriteData);
                        int sy = (int)spriteDataType.GetField("y").GetValue(spriteData);
                        int sw = (int)spriteDataType.GetField("width").GetValue(spriteData);
                        int sh = (int)spriteDataType.GetField("height").GetValue(spriteData);

                        float texW = mainTexture.width;
                        float texH = mainTexture.height;
                        
                        // UGUI 的 V 坐标是从下往上，NGUI 是从上往下计算，需要反转一下 Y
                        uvRect = new Rect(sx / texW, (texH - sy - sh) / texH, sw / texW, sh / texH);
                    }
                }
            }

            // 保存当被转为 UGUI 后极易错乱的本地坐标
            Vector3 originalLocalPos = obj.transform.localPosition;

            // 4. 清除原始组件
            DestroyImmediate(nguiWidget);

            // 5. 挂载 UGUI 专用组件
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();

            RawImage rawImage = obj.GetComponent<RawImage>();
            if (rawImage == null) rawImage = obj.AddComponent<RawImage>();

            // 6. 还原设置
            rawImage.texture = mainTexture;
            rawImage.color = widgetColor;
            rawImage.uvRect = uvRect; // 解决全图集显示白块的问题！
            
            rect.pivot = pivotOffset; // 恢复对象的 NGUI 对齐中心
            rect.sizeDelta = new Vector2(width, height);
            rect.localPosition = originalLocalPos; // 恢复确切位置

            if (lightweightMaterial != null)
            {
                rawImage.material = lightweightMaterial;
            }
        }
    }
}