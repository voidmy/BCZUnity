using System;
using System.Threading;
using UnityEngine;
using UnityWebSocket; // 需要引入 WebSocketSharp 库

/// <summary>
/// 连接本地 Node.js RVO 控制服务器（ws://localhost:8080），
/// 接收 tick 消息并累积到 PendingTicks，供 RVODemoManager 消费。
/// </summary>
public class RVOClientNetwork : MonoBehaviour
{
    [Header("服务器地址")]
    public string serverUrl = "ws://localhost:8080";

    private WebSocket _ws;
    private static int _pendingTicks;

    [Serializable]
    private class TickMessage
    {
        public string type;
        public int ticks;
        public int tickRate;
        public int tickCount;
    }

    private void Start()
    {
        Connect();
    }

    private void OnDestroy()
    {
        try
        {
            if (_ws != null)
            {
                _ws.CloseAsync();
                _ws = null;
            }
        }
        catch { }
    }

    private void Connect()
    {
        try
        {
            _ws = new WebSocket(serverUrl);
            _ws.OnOpen += (sender, e) =>
            {
                Debug.Log("[RVOClientNetwork] Connected to server");
                // 默认连接后就请求开始模拟
                _ws.SendAsync("start");
            };

            _ws.OnMessage += (sender, e) =>
            {
                if (!e.IsText || string.IsNullOrEmpty(e.Data))
                    return;

                try
                {
                    var msg = JsonUtility.FromJson<TickMessage>(e.Data);
                    if (msg != null && msg.type == "tick" && msg.ticks > 0)
                    {
                        Interlocked.Add(ref _pendingTicks, msg.ticks);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RVOClientNetwork] Failed to parse message: " + ex.Message);
                }
            };

            _ws.OnError += (sender, e) =>
            {
                Debug.LogWarning("[RVOClientNetwork] WebSocket error: " + e.Message);
            };

            _ws.OnClose += (sender, e) =>
            {
                Debug.Log("[RVOClientNetwork] Disconnected from server");
            };

            _ws.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError("[RVOClientNetwork] Connect failed: " + ex.Message);
        }
    }

    /// <summary>
    /// 供 RVODemoManager 调用：取出当前待执行的 tick 数，并清零。
    /// </summary>
    public static int ConsumeTicks()
    {
        return Interlocked.Exchange(ref _pendingTicks, 0);
    }
}
