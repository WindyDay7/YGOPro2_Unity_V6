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
            EditorGUILayout.HelpBox("选择场景中的 NGUI GameObject，点击转换将其变为基于 Canvas 的轻量级 UGUI RawImage 组件。", MessageType.Info);

            lightweightMaterial = (Material)EditorGUILayout.ObjectField("目标 Material (可选)", lightweightMaterial, typeof(Material), false);

            if (GUILayout.Button("一键转换当前选中对象", GUILayout.Height(40)))
            {
                ConvertSelected();
            }
        }

        private void ConvertSelected()
        {
            if (Selection.objects == null || Selection.objects.Length == 0)
            {
                Debug.LogWarning("[UI Converter] 请先在 Hierarchy 中选中至少一个包含 NGUI UISprite / UITexture 的 GameObject!");
                return;
            }

            // 确保场景中有 Canvas，这也是轻量化 UI 基于 UGUI 的大前提
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
                ConvertSingleObject(obj, canvas);
            }
        }

        private void ConvertSingleObject(GameObject obj, Canvas rootCanvas)
        {
            // 通过反射获取 NGUI 的组件 (因为当前在不同的工程环境下我们不确定它是否叫做 UISprite 还是 UITexture)
            Component nguiWidget = obj.GetComponent("UIWidget");
            if (nguiWidget == null)
            {
                Debug.LogWarning($"[UI Converter] {obj.name} 没有挂载 UIWidget，跳过。");
                return;
            }

            Undo.RecordObject(obj, "Convert to Lightweight UGUI");

            // 1. 读取 NGUI 参数 (颜色、尺寸、Alpha 等)
            Color widgetColor = Color.white;
            int width = 100, height = 100;
            Texture mainTexture = null;

            System.Type widgetType = nguiWidget.GetType();
            
            // 获取颜色
            PropertyInfo colorProp = widgetType.GetProperty("color");
            if (colorProp != null) widgetColor = (Color)colorProp.GetValue(nguiWidget, null);
            
            // 获取尺寸
            PropertyInfo wProp = widgetType.GetProperty("width");
            if (wProp != null) width = (int)wProp.GetValue(nguiWidget, null);
            
            PropertyInfo hProp = widgetType.GetProperty("height");
            if (hProp != null) height = (int)hProp.GetValue(nguiWidget, null);

            // 获取贴图 (如果是 UITexture)
            PropertyInfo texProp = widgetType.GetProperty("mainTexture");
            if (texProp != null) mainTexture = texProp.GetValue(nguiWidget, null) as Texture;

            // 2. 剥离沉重的 NGUI 处理逻辑
            DestroyImmediate(nguiWidget);

            // 3. 挂载轻量级 UGUI RawImage，以及 RectTransform
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null)
            {
                // 如果原本是纯 Transform，Unity 添加 RawImage 也会自动赋予 RectTransform
                rect = obj.AddComponent<RectTransform>();
            }

            RawImage rawImage = obj.GetComponent<RawImage>();
            if (rawImage == null)
                rawImage = obj.AddComponent<RawImage>();

            // 4. 将提取的数据还给轻量级组件
            rawImage.texture = mainTexture; // NGUI的Sprite情况可能需要专门从Atlas处理，后续根据具体业务细化
            rawImage.color = widgetColor;
            rect.sizeDelta = new Vector2(width, height);

            if (lightweightMaterial != null)
            {
                rawImage.material = lightweightMaterial;
            }

            // (可选) 剥离自带的 NGUI Collider 和事件响应脚本
            Collider col = obj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col); // UGUI 使用 GraphicRaycaster

            Debug.Log($"[UI Converter] 成功转换了 {obj.name} 向 UGUI RawImage!");
        }
    }
}