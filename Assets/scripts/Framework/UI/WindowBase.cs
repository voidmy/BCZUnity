using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public abstract class WindowBase : MonoBehaviour
{
    // ���ڲ㼶
    public UILayer Layer { get; protected set; } = UILayer.Window;
    // ����Ψһ��ʶ������LRU���棩
    public string WindowKey => GetType().Name;
    // �Ƿ�פ�ڴ棨������LRU���٣�
    public bool IsPersistent { get; protected set; } = false;

    // �������ݣ��ɱ��棩
    protected object _windowData;

    // ��ʼ�����������ݣ�
    public virtual void Init(object data = null)
    {
        _windowData = data;
        LoadData(); // ��������
    }

    // ��ʾ���ڣ���������
    public virtual void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(PlayShowAnimation());
        UpdateLRU(); // ����LRU����
    }

    // ���ش��ڣ���������
    public virtual void Hide()
    {
        StartCoroutine(PlayHideAnimation());
        SaveData(); // ��������
    }

    // Ĭ�϶��������� + ����
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

    // ���ݼ���/���棨�������д��
    protected virtual void LoadData()
    {
        // �ӱ��ؼ������ݣ�ʾ����
        string json = PlayerPrefs.GetString(WindowKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            _windowData = JsonUtility.FromJson(json, GetDataType());
        }
    }

    protected virtual void SaveData()
    {
        // ���浽���أ�ʾ����
        string json = JsonUtility.ToJson(_windowData);
        PlayerPrefs.SetString(WindowKey, json);
    }

    protected virtual System.Type GetDataType() => typeof(object);

    // �����¼�
    protected void TriggerEvent(string eventName, object data = null)
    {
        //EventSystemBase.Instance.TriggerEvent(eventName, data);
    }

    // ����LRU����
    private void UpdateLRU()
    {
       // UIManager.Instance.UpdateLRU(this);
    }
}