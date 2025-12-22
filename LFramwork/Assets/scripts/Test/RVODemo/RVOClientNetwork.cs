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
    public static int ServerTickRate = 20;

    // 本地累计已经消费的 tick 数量（通过 RVODemoManager.StepSimulator 统计），用于对比服务器 tickCount
    public static int TotalConsumedTicks;

    public static RVOClientNetwork Instance;

    // 服务器为当前连接分配的用户 Id（第一个连接的一般是 1）。
    public static int LocalUserId;

    [Serializable]
    private class BaseMessage
    {
        public string type;
    }

    [Serializable]
    private class HelloMessage
    {
        public string type;   // "hello"
        public int userId;    // 服务器分配给本连接的用户 Id
        public string message;
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
    private class DesyncMessage
    {
        public string type;        // "desync"
        public int tick;           // 发生分叉的 tick
        public int expectedHash;   // 服务器记录的第一个 hash
        public int newHash;        // 当前客户端上报的 hash
    }

    [Serializable]
    public class SpawnMessage
    {
        public string type; // "spawn"
        public int side;    // 0: left side (Owner), 1: right side (Enemy)
        public int x;       // 出生点世界坐标 X（整数网格）
        public int z;       // 出生点世界坐标 Z（整数网格）
        public int tick;    // 服务器指定的在第几个逻辑 tick 上生成
    }

    [Serializable]
    public class SkillMessage
    {
        public string type; // "skill"
        public int x;       // 技能中心世界坐标 X（整数网格）
        public int z;       // 技能中心世界坐标 Z（整数网格）
        public int radius;  // 技能半径（整数网格，近似范围即可）
        public int tick;    // 服务器指定的在第几个逻辑 tick 上生效
    }

    [Serializable]
    public class BulletMessage
    {
        public string type; // "bullet"
        public int x;       // 子弹出生点世界坐标 X（整数网格）
        public int z;       // 子弹出生点世界坐标 Z（整数网格）
        public int dirX;    // 子弹朝向在 X 轴上的分量（整数缩放）
        public int dirZ;    // 子弹朝向在 Z 轴上的分量（整数缩放）
        public int tick;    // 服务器指定的在第几个逻辑 tick 上生成
    }

    [Serializable]
    public class StateHashMessage
    {
        public string type; // "stateHash"
        public int tick;    // 本地累计已消费的 tick（应与服务器 tickCount 对齐）
        public int hash;    // 当前全局状态哈希
    }

    [Serializable]
    public class PlayerSpawnMessage
    {
        public string type;   // "playerSpawn"
        public int playerId;  // 与服务器 userId 一致
        public float x;
        public float z;
    }

    [Serializable]
    public class PlayerState
    {
        public int playerId;
        public float x;
        public float z;
        public float vx;
        public float vz;
    }

    [Serializable]
    public class PlayerSnapshotMessage
    {
        public string type;     // "playerSnapshot"
        public int tick;
        public PlayerState[] players;
    }

    [Serializable]
    public class PlayerInputState
    {
        public string type;
        public int playerId;
        public int dx;
        public int dz;
    }

    [Serializable]
    public class PlayerInputSnapshotMessage
    {
        public string type;     // "playerInputSnapshot"
        public int tick;
        public PlayerInputState[] players;
    }

    public static event Action<SpawnMessage> OnSpawnMessage;
    public static event Action<SkillMessage> OnSkillMessage;
    public static event Action<BulletMessage> OnBulletMessage;
    public static event Action<PlayerSpawnMessage> OnPlayerSpawn;
    public static event Action<PlayerSnapshotMessage> OnPlayerSnapshot;
    public static event Action<PlayerInputSnapshotMessage> OnPlayerInputSnapshot;
    public static event Action<int, int, int> OnDesync; // tick, expectedHash, newHash
    public static event Action<int> OnHello; // userId
    public static event Action OnReset;      // 服务器通知重置本地仿真

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
                            ServerTickRate = tickMsg.tickRate;
                        }
                    }
                    else if (baseMsg.type == "hello")
                    {
                        var helloMsg = JsonUtility.FromJson<HelloMessage>(e.Data);
                        if (helloMsg != null)
                        {
                            LocalUserId = helloMsg.userId;
                            Debug.Log($"[RVOClientNetwork] Hello from server, userId={LocalUserId}");
                            if (OnHello != null)
                            {
                                OnHello(LocalUserId);
                            }
                        }
                    }
                    else if (baseMsg.type == "reset")
                    {
                        // 服务器要求所有客户端重置本地仿真状态
                        if (OnReset != null)
                        {
                            OnReset();
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
                    else if (baseMsg.type == "skill")
                    {
                        var skillMsg = JsonUtility.FromJson<SkillMessage>(e.Data);
                        if (skillMsg != null && OnSkillMessage != null)
                        {
                            OnSkillMessage(skillMsg);
                        }
                    }
                    else if (baseMsg.type == "bullet")
                    {
                        var bulletMsg = JsonUtility.FromJson<BulletMessage>(e.Data);
                        if (bulletMsg != null && OnBulletMessage != null)
                        {
                            OnBulletMessage(bulletMsg);
                        }
                    }
                    else if (baseMsg.type == "playerSpawn")
                    {
                        var spawnPlayerMsg = JsonUtility.FromJson<PlayerSpawnMessage>(e.Data);
                        if (spawnPlayerMsg != null && OnPlayerSpawn != null)
                        {
                            OnPlayerSpawn(spawnPlayerMsg);
                        }
                    }
                    else if (baseMsg.type == "playerSnapshot")
                    {
                        var snapshotMsg = JsonUtility.FromJson<PlayerSnapshotMessage>(e.Data);
                        if (snapshotMsg != null && OnPlayerSnapshot != null)
                        {
                            OnPlayerSnapshot(snapshotMsg);
                        }
                    }
                    else if (baseMsg.type == "playerInputSnapshot")
                    {
                        var inputSnapshotMsg = JsonUtility.FromJson<PlayerInputSnapshotMessage>(e.Data);
                        if (inputSnapshotMsg != null && OnPlayerInputSnapshot != null)
                        {
                            OnPlayerInputSnapshot(inputSnapshotMsg);
                        }
                    }
                    else if (baseMsg.type == "desync")
                    {
                        var desyncMsg = JsonUtility.FromJson<DesyncMessage>(e.Data);
                        if (desyncMsg != null && OnDesync != null)
                        {
                            OnDesync(desyncMsg.tick, desyncMsg.expectedHash, desyncMsg.newHash);
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

    /// <summary>
    /// 本地发送玩家输入方向（定点整数 dx/dz），服务器只记录并在每个 tick 广播给所有客户端。
    /// </summary>
    public static void SendPlayerInput(int playerId, int dx, int dz)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[RVOClientNetwork] Instance is null, cannot send player input");
            return;
        }

        if (Instance._ws == null)
        {
            Debug.LogWarning("[RVOClientNetwork] WebSocket is null, cannot send player input");
            return;
        }

        var obj = new PlayerInputState
        {
            type = "playerInput",
            playerId = playerId,
            dx = dx,
            dz = dz
        };

        string json = JsonUtility.ToJson(obj);
        Instance._ws.SendAsync(json);
    }

    /// <summary>
    /// 本地上报玩家当前位置和朝向（均为世界坐标/方向的 float），
    /// 服务器只做记录和广播，不再积分移动。
    /// </summary>
    public static void SendPlayerState(int playerId, float x, float z, float vx, float vz)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[RVOClientNetwork] Instance is null, cannot send player state");
            return;
        }

        if (Instance._ws == null)
        {
            Debug.LogWarning("[RVOClientNetwork] WebSocket is null, cannot send player state");
            return;
        }

        var obj = new
        {
            type = "playerState",
            playerId = playerId,
            x = x,
            z = z,
            vx = vx,
            vz = vz
        };

        string json = JsonUtility.ToJson(obj);
        Instance._ws.SendAsync(json);
    }

    /// <summary>
    /// 本地请求在某个位置、某个方向发射一颗子弹，由服务器广播统一生成。
    /// </summary>
    public static void SendBulletRequest(int x, int z, int dirX, int dirZ)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[RVOClientNetwork] Instance is null, cannot send bullet request");
            return;
        }

        if (Instance._ws == null)
        {
            Debug.LogWarning("[RVOClientNetwork] WebSocket is null, cannot send bullet request");
            return;
        }

        var msg = new BulletMessage
        {
            type = "bullet",
            x = x,
            z = z,
            dirX = dirX,
            dirZ = dirZ
        };

        string json = JsonUtility.ToJson(msg);
        Instance._ws.SendAsync(json);
    }

    /// <summary>
    /// 本地请求在某个位置释放一个范围技能，由服务器广播统一生效。
    /// </summary>
    public static void SendSkillRequest(int x, int z, int radius)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[RVOClientNetwork] Instance is null, cannot send skill request");
            return;
        }

        if (Instance._ws == null)
        {
            Debug.LogWarning("[RVOClientNetwork] WebSocket is null, cannot send skill request");
            return;
        }

        var msg = new SkillMessage
        {
            type = "skill",
            x = x,
            z = z,
            radius = radius
        };

        string json = JsonUtility.ToJson(msg);
        Instance._ws.SendAsync(json);
    }

    /// <summary>
    /// 将当前 tick 的全局状态哈希发送给服务器，方便服务器检测多客户端是否分叉。
    /// 由 RVODemoManager.StepSimulator 在每个 tick 结束时调用。
    /// </summary>
    public static void SendStateHash(int tick, int hash)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[RVOClientNetwork] Instance is null, cannot send state hash");
            return;
        }

        if (Instance._ws == null)
        {
            Debug.LogWarning("[RVOClientNetwork] WebSocket is null, cannot send state hash");
            return;
        }

        var msg = new StateHashMessage
        {
            type = "stateHash",
            tick = tick,
            hash = hash
        };

        string json = JsonUtility.ToJson(msg);
        Instance._ws.SendAsync(json);
    }
}
