using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public abstract class WindowBase : MonoBehaviour
{
    // 窗口层级
    public UILayer Layer { get; protected set; } = UILayer.Window;
    // 窗口唯一标识（用于LRU缓存）
    public string WindowKey => GetType().Name;
    // 是否常驻内存（不参与LRU销毁）
    public bool IsPersistent { get; protected set; } = false;

    // 窗口数据（可保存）
    protected object _windowData;

    // 初始化（传入数据）
    public virtual void Init(object data = null)
    {
        _windowData = data;
        LoadData(); // 加载数据
    }

    // 显示窗口（带动画）
    public virtual void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(PlayShowAnimation());
        UpdateLRU(); // 更新LRU缓存
    }

    // 隐藏窗口（带动画）
    public virtual void Hide()
    {
        StartCoroutine(PlayHideAnimation());
        SaveData(); // 保存数据
    }

    // 默认动画：缩放 + 渐显
    protected virtual IEnumerator PlayShowAnimation()
    {
        transform.localScale = Vector3.zero;
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 0;

        float duration = 0.3f;
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            if (canvasGroup != null) canvasGroup.alpha = t;
            yield return null;
        }
    }

    protected virtual IEnumerator PlayHideAnimation()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        float duration = 0.2f;
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            if (canvasGroup != null) canvasGroup.alpha = 1 - t;
            yield return null;
        }
        gameObject.SetActive(false);
    }

    // 数据加载/保存（子类可重写）
    protected virtual void LoadData()
    {
        // 从本地加载数据（示例）
        string json = PlayerPrefs.GetString(WindowKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            _windowData = JsonUtility.FromJson(json, GetDataType());
        }
    }

    protected virtual void SaveData()
    {
        // 保存到本地（示例）
        string json = JsonUtility.ToJson(_windowData);
        PlayerPrefs.SetString(WindowKey, json);
    }

    protected virtual System.Type GetDataType() => typeof(object);

    // 触发事件
    protected void TriggerEvent(string eventName, object data = null)
    {
        //EventSystemBase.Instance.TriggerEvent(eventName, data);
    }

    // 更新LRU缓存
    private void UpdateLRU()
    {
       // UIManager.Instance.UpdateLRU(this);
    }
}