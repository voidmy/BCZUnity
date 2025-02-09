using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * 事件系统基类
 * 提供了事件的注册、触发和注销功能
 * 所有事件都存储为静态变量，方便全局访问
 */
public class EventSystemBase:Singleton<EventSystemBase>
{
    /**
     * 静态字典，存储所有事件及其对应的回调函数
     */
    protected static Dictionary<string, Action<object>> events = new Dictionary<string, Action<object>>();

    /**
     * 注册事件
     * @param eventName 事件名称
     * @param callback 事件触发时调用的回调函数
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
     * 触发事件
     * @param eventName 事件名称
     * @param data 传递给回调函数的数据
     */
    public static void TriggerEvent(string eventName, object data)
    {
        if (events.ContainsKey(eventName) && events[eventName] != null)
        {
            events[eventName](data);
        }
    }

    /**
     * 注销事件
     * @param eventName 事件名称
     * @param callback 要注销的回调函数
     */
    public static void UnregisterEvent(string eventName, Action<object> callback)
    {
        if (events.ContainsKey(eventName))
        {
            events[eventName] -= callback;
        }
    }
}
