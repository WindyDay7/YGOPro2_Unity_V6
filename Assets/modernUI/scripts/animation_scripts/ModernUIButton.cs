using UnityEngine;
using UnityEngine.EventSystems; // 引入 UI 事件系统命名空间
using DG.Tweening; // 引入 DoTween 命名空间

// 这个特性确保挂载此脚本的物体必定有 RectTransform
[RequireComponent(typeof(RectTransform))] 
public class ModernUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("动画参数配置")]
    [Tooltip("鼠标悬停时的缩放倍数")]
    public float hoverScale = 1.1f; 
    
    [Tooltip("鼠标按下时的缩放倍数（增加点击反馈）")]
    public float pressScale = 0.95f; 
    
    [Tooltip("动画过渡时间")]
    public float tweenDuration = 0.15f;

    private Vector3 originalScale; // 记录按钮最初始的缩放值

    void Start()
    {
        // 初始化时记录默认大小
        originalScale = transform.localScale;
    }

    // 1. 鼠标/手指 悬浮进入时触发 (对应 NGUI 的 OnHover = true)
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 放大。SetUpdate(true) 表示即使 Time.timeScale = 0 (游戏暂停)，UI 动画依然播放
        transform.DOScale(originalScale * hoverScale, tweenDuration).SetUpdate(true);
    }

    // 2. 鼠标/手指 悬浮离开时触发 (对应 NGUI 的 OnHover = false)
    public void OnPointerExit(PointerEventData eventData)
    {
        // 恢复原状
        transform.DOScale(originalScale, tweenDuration).SetUpdate(true);
    }

    // 3. 鼠标/手指 按下时触发 (对应 NGUI 的 OnPress = true)
    public void OnPointerDown(PointerEventData eventData)
    {
        // 稍微缩小，模拟真实的物理按压感
        transform.DOScale(originalScale * pressScale, tweenDuration).SetUpdate(true);
    }

    // 4. 鼠标/手指 抬起时触发 (对应 NGUI 的 OnPress = false)
    public void OnPointerUp(PointerEventData eventData)
    {
        // 抬起时，鼠标通常还在按钮上，所以恢复到悬停状态的大小
        transform.DOScale(originalScale * hoverScale, tweenDuration).SetUpdate(true);
    }

    // [极其重要的避坑指南]
    private void OnDisable()
    {
        // 当这个按钮所在的菜单被隐藏 (SetActive(false)) 时
        // 必须立即停止它身上的动画，并强行复原状态。
        // 否则下次打开菜单时，按钮可能会保持在被放大或者缩小的错误状态。
        transform.DOKill(); 
        transform.localScale = originalScale;
    }
}