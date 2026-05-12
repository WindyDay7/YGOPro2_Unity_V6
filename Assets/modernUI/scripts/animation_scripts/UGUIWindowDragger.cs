using UnityEngine;
using UnityEngine.EventSystems;

// 继承 UGUI 的拖拽接口
public class UGUIWindowDragger : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [Header("对应 NGUI 的 Target 参数")]
    [Tooltip("你想拖动的整个窗口根节点")]
    public RectTransform targetWindow; 

    private Canvas canvas;

    private void Awake()
    {
        // 自动往上寻找当前 UI 所在的 Canvas
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // （可选）当开始拖拽时，把窗口提到最上层，防止被其他窗口遮挡
        if (targetWindow != null)
        {
            targetWindow.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetWindow != null && canvas != null)
        {
            // eventData.delta 是鼠标的偏移量
            // 除以 canvas.scaleFactor 是为了兼容各种屏幕分辨率的 Canvas 缩放
            targetWindow.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }
}