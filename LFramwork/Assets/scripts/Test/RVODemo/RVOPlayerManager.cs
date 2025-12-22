using System.Collections.Generic;
using Lockstep.Math;
using UnityEngine;

public class RVOPlayerManager : MonoBehaviour
{
    public GameObject playerPrefab;

    [Header("移动配置（定点仿真）")]
    public float moveSpeed = 2f; // 与 RVOPlayerController.moveSpeed 保持一致

    private readonly Dictionary<int, RVOPlayerController> _players = new Dictionary<int, RVOPlayerController>();

    private class PlayerSimState
    {
        public int playerId;
        public LVector2 pos;   // 定点位置（x,z）
        public int dirX;       // 输入方向（定点整数，缩放 1000）
        public int dirZ;
    }

    private readonly Dictionary<int, PlayerSimState> _simStates = new Dictionary<int, PlayerSimState>();

    private void OnEnable()
    {
        RVOClientNetwork.OnPlayerSpawn += OnPlayerSpawn;
        RVOClientNetwork.OnPlayerSnapshot += OnPlayerSnapshot;
        RVOClientNetwork.OnHello += OnHelloFromServer;
        RVOClientNetwork.OnPlayerInputSnapshot += OnPlayerInputSnapshot;
    }

    private void OnDisable()
    {
        RVOClientNetwork.OnPlayerSpawn -= OnPlayerSpawn;
        RVOClientNetwork.OnPlayerSnapshot -= OnPlayerSnapshot;
        RVOClientNetwork.OnHello -= OnHelloFromServer;
        RVOClientNetwork.OnPlayerInputSnapshot -= OnPlayerInputSnapshot;
    }

    private void OnPlayerSpawn(RVOClientNetwork.PlayerSpawnMessage msg)
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("[RVOPlayerManager] playerPrefab is null");
            return;
        }

        if (_players.ContainsKey(msg.playerId))
        {
            return;
        }

        Vector3 pos = new Vector3(msg.x, 0f, msg.z);
        Quaternion rot = Quaternion.identity;
        var go = Instantiate(playerPrefab, pos, rot);
        var controller = go.GetComponent<RVOPlayerController>();
        if (controller == null)
        {
            controller = go.AddComponent<RVOPlayerController>();
        }

        controller.playerId = msg.playerId;
        controller.isLocalPlayer = (msg.playerId == RVOClientNetwork.LocalUserId);
        _players[msg.playerId] = controller;
        
        Debug.Log($"[PlayerSpawn] playerId={msg.playerId}, LocalUserId={RVOClientNetwork.LocalUserId}, isLocalPlayer={controller.isLocalPlayer}");

        // 初始化该玩家的定点仿真状态
        var simState = new PlayerSimState
        {
            playerId = msg.playerId,
            pos = new LVector2((LFloat)msg.x, (LFloat)msg.z),
            dirX = 0,
            dirZ = 0,
        };
        _simStates[msg.playerId] = simState;
    }

    private void OnHelloFromServer(int userId)
    {
        Debug.Log($"[OnHelloFromServer] Got userId={userId}");
        
        // 在拿到本地的 userId 之后，重新标记本地玩家
        foreach (var kv in _players)
        {
            var ctrl = kv.Value;
            if (ctrl == null) continue;
            bool wasLocal = ctrl.isLocalPlayer;
            ctrl.isLocalPlayer = (kv.Key == userId);
            Debug.Log($"[OnHelloFromServer] Player {kv.Key}: wasLocal={wasLocal}, isLocal={ctrl.isLocalPlayer}");
        }
    }

    private void OnPlayerInputSnapshot(RVOClientNetwork.PlayerInputSnapshotMessage msg)
    {
        Debug.Log($"[PlayerInputSnapshot] tick={msg.tick}, players={msg.players?.Length ?? 0}");
        
        if (msg.players == null) return;

        for (int i = 0; i < msg.players.Length; i++)
        {
            var state = msg.players[i];
            Debug.Log($"[PlayerInputSnapshot] Player {state.playerId}: dx={state.dx}, dz={state.dz}");
            
            if (!_simStates.TryGetValue(state.playerId, out var simState) || simState == null)
            {
                simState = new PlayerSimState
                {
                    playerId = state.playerId,
                    pos = new LVector2(LFloat.zero, LFloat.zero),
                    dirX = 0,
                    dirZ = 0,
                };
                _simStates[state.playerId] = simState;
            }

            simState.dirX = state.dx;
            simState.dirZ = state.dz;
        }
    }

    /// <summary>
    /// 由 RVODemoManager.StepSimulator 在每个逻辑 tick 中调用一次，
    /// 根据当前输入方向用定点数推进玩家位置。
    /// </summary>
    public void StepPlayersOneTick()
    {
        if (_simStates.Count == 0)
            return;

        // dt 由服务器 tickRate 决定，保持所有客户端一致
        float dtFloat = 1f / Mathf.Max(1, RVOClientNetwork.ServerTickRate);
        LFloat dt = (LFloat)dtFloat;
        LFloat speed = (LFloat)moveSpeed;
        LFloat step = dt * speed;

        const int DIR_SCALE = 1000;

        foreach (var kv in _simStates)
        {
            var st = kv.Value;
            int dxInt = st.dirX;
            int dzInt = st.dirZ;
            if (dxInt == 0 && dzInt == 0)
                continue;

            // 将定点整数方向还原成 LFloat，并做一次归一化，保证不同客户端一致
            LFloat fx = (LFloat)(dxInt / (float)DIR_SCALE);
            LFloat fz = (LFloat)(dzInt / (float)DIR_SCALE);

            LFloat lenSqr = fx * fx + fz * fz;
            if (lenSqr > LFloat.zero)
            {
                LFloat invLen = LFloat.one / LMath.Sqrt(lenSqr);
                fx *= invLen;
                fz *= invLen;
            }

            LFloat moveX = fx * step;
            LFloat moveZ = fz * step;

            st.pos = new LVector2(st.pos.x + moveX, st.pos.y + moveZ);
        }
    }

    /// <summary>
    /// 在 Unity 帧更新中，把定点仿真出来的位置/朝向同步到各个玩家的 Transform。
    /// </summary>
    public void SyncPlayerTransforms()
    {
        foreach (var kv in _simStates)
        {
            int playerId = kv.Key;
            var st = kv.Value;
            if (!_players.TryGetValue(playerId, out var ctrl) || ctrl == null)
                continue;

            // 位置
            Vector3 pos = ctrl.transform.position;
            pos.x = (float)st.pos.x;
            pos.z = (float)st.pos.y;
            ctrl.transform.position = pos;

            // 朝向：根据输入方向
            if (st.dirX != 0 || st.dirZ != 0)
            {
                Vector3 dir = new Vector3(st.dirX, 0f, st.dirZ);
                if (dir.sqrMagnitude > 1e-4f)
                {
                    dir.Normalize();
                    ctrl.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                }
            }
        }
    }

    private void OnPlayerSnapshot(RVOClientNetwork.PlayerSnapshotMessage msg)
    {
        if (msg.players == null) return;

        for (int i = 0; i < msg.players.Length; i++)
        {
            var state = msg.players[i];
            if (!_players.TryGetValue(state.playerId, out var controller) || controller == null)
            {
                // 延迟生成：如果还没 Spawn，就触发一次 Spawn 逻辑
                var fakeSpawn = new RVOClientNetwork.PlayerSpawnMessage
                {
                    type = "playerSpawn",
                    playerId = state.playerId,
                    x = state.x,
                    z = state.z
                };
                OnPlayerSpawn(fakeSpawn);
                _players.TryGetValue(state.playerId, out controller);
                if (controller == null) continue;
            }

            controller.ApplySnapshot(state.x, state.z, state.vx, state.vz);
        }
    }
}
