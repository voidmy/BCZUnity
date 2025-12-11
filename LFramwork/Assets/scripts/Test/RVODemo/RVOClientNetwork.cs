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
    public string serverUrl = "ws://172.16.10.30:8081";

    private WebSocket _ws;
    private static int _pendingTicks;

    // 服务器累计发送的 tick 总数量（从服务器 tickCount 字段同步过来），用于调试显示
    public static int TotalServerTickCount;

    public static RVOClientNetwork Instance;

    [Serializable]
    private class BaseMessage
    {
        public string type;
    }

    [Serializable]
    private class TickMessage
    {
        public string type;
        public int ticks;
        public int tickRate;
        public int tickCount;
    }

    [Serializable]
    public class SpawnMessage
    {
        public string type; // "spawn"
        public int side;    // 0: left side (Owner), 1: right side (Enemy)
        public int x;       // 出生点世界坐标 X（整数网格）
        public int z;       // 出生点世界坐标 Z（整数网格）
    }

    public static event Action<SpawnMessage> OnSpawnMessage;

    private void Awake()
    {
        Instance = this;
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
                    var baseMsg = JsonUtility.FromJson<BaseMessage>(e.Data);
                    if (baseMsg == null || string.IsNullOrEmpty(baseMsg.type))
                        return;

                    if (baseMsg.type == "tick")
                    {
                        var tickMsg = JsonUtility.FromJson<TickMessage>(e.Data);
                        if (tickMsg != null && tickMsg.ticks > 0)
                        {
                            Interlocked.Add(ref _pendingTicks, tickMsg.ticks);
                            TotalServerTickCount = tickMsg.tickCount;
                        }
                    }
                    else if (baseMsg.type == "spawn")
                    {
                        var spawnMsg = JsonUtility.FromJson<SpawnMessage>(e.Data);
                        if (spawnMsg != null && OnSpawnMessage != null)
                        {
                            OnSpawnMessage(spawnMsg);
                        }
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

    /// <summary>
    /// 本地请求在某一侧、某个整数网格坐标上出生一个 Agent。
    /// 实际生成由服务器广播 spawn 消息统一决定。
    /// </summary>
    public static void SendSpawnRequest(bool leftSide, int x, int z)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[RVOClientNetwork] Instance is null, cannot send spawn request");
            return;
        }

        if (Instance._ws == null)
        {
            Debug.LogWarning("[RVOClientNetwork] WebSocket is null, cannot send spawn request");
            return;
        }

        var msg = new SpawnMessage
        {
            type = "spawn",
            side = leftSide ? 0 : 1,
            x = x,
            z = z
        };

        string json = JsonUtility.ToJson(msg);
        Instance._ws.SendAsync(json);
    }
}
