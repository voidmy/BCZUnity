using System;
using System.Collections.Generic;
using Framework;
using UnityEngine;

/**
 * �¼�ϵͳ����
 * �ṩ���¼���ע�ᡢ������ע������
 * �����¼����洢Ϊ��̬����������ȫ�ַ���
 */
public class EventSystemBase:Singleton<EventSystemBase>, ITSingleton
{
    /**
     * ��̬�ֵ䣬�洢�����¼������Ӧ�Ļص�����
     */
    protected static Dictionary<string, Action<object>> events = new Dictionary<string, Action<object>>();

    /**
     * ע���¼�
     * @param eventName �¼�����
     * @param callback �¼�����ʱ���õĻص�����
     */
    public static void RegisterEvent(string eventName, Action<object> callback)
    {
        if (!events.ContainsKey(eventName))
        {
            events[eventName] = null;
        }
        events[eventName] += callback;
    }

    /**
     * �����¼�
     * @param eventName �¼�����
     * @param data ���ݸ��ص�����������
     */
    public static void TriggerEvent(string eventName, object data)
    {
        if (events.ContainsKey(eventName) && events[eventName] != null)
        {
            events[eventName](data);
        }
    }

    /**
     * ע���¼�
     * @param eventName �¼�����
     * @param callback Ҫע���Ļص�����
     */
    public static void UnregisterEvent(string eventName, Action<object> callback)
    {
        if (events.ContainsKey(eventName))
        {
            events[eventName] -= callback;
        }
    }

    public void OnSingletonInit()
    {
        
    }
}
