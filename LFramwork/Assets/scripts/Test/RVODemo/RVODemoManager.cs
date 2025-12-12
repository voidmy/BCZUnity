using System;
using System.Collections;
using System.Collections.Generic;
using Lockstep.Math;
using RVO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using LVector2 = RVO.LVector2;

public class RVODemoManager : MonoBehaviour
{
    [Header("边界（世界坐标，X-Z 平面）")] [Tooltip("左边界：决定从右往左移动时的回收位置，以及左侧出生点的参考位置")]
    public Transform leftBoundary;

    [Tooltip("右边界：决定从左往右移动时的回收位置，以及右侧出生点的参考位置")]
    public Transform rightBoundary;

    [Tooltip("上边界：与下边界一起决定纵向范围，用来平均分布一排排角色")]
    public Transform topBoundary;

    [Tooltip("下边界：与上边界一起决定纵向范围，用来平均分布一排排角色")]
    public Transform bottomBoundary;

    [Header("左右两侧角色预制体")] [Tooltip("从左往右移动的角色预制体")]
    public GameObject leftToRightPrefab;

    [Tooltip("从右往左移动的角色预制体")] public GameObject rightToLeftPrefab;

    [Header("生成配置")] [Tooltip("每一侧要生成多少个角色（左边 N 个，右边 N 个）")]
    public int agentsPerSide = 5;

    [Tooltip("纵向间距说明（目前主要由上/下边界平均分布，这个值可以作为备用或调试使用）")]
    public float verticalSpacing = 1.5f;

    [Tooltip("出生位置相对于左右边界在 X 方向的偏移量，>0 表示生成在边界内侧一点")]
    public float spawnOffsetFromBoundary = 0.5f;

    [Tooltip("生成最多的数量")] public float spawnMaxNum = 1200;

    [Header("RVO 配置（定点）")] [Tooltip("邻居感知距离（定点 LFloat）：RVO 计算时能看到多远的其他 Agent（越大越安全但更耗性能）")]
    public LFloat rvoNeighborDist = (LFloat)3f;

    [Tooltip("每个 Agent 参与计算的最大邻居数量（过小会不安全，过大会更耗性能）")]
    public int rvoMaxNeighbors = 10;

    [Tooltip("与其他 Agent 的时间视野（定点 LFloat）：越大越早避让，但自由度越低")]
    public LFloat rvoTimeHorizon = (LFloat)2f;

    [Tooltip("与障碍物的时间视野（定点 LFloat）：越大越早避让障碍，但自由度越低")]
    public LFloat rvoTimeHorizonObst = (LFloat)2f;

    [Tooltip("Agent 碰撞半径（定点 LFloat，决定彼此之间的最小间距）")]
    public LFloat rvoRadius = (LFloat)0.5f;

    [Tooltip("最大移动速度（定点 LFloat，RVO 会在不违反约束的前提下尽量接近这个速度")]
    public LFloat rvoMaxSpeed = (LFloat)2f;

    [Tooltip("RVO 仿真时间步长（定点 LFloat，单位秒）：越小越精确但计算更频繁")]
    public LFloat rvoTimeStep = (LFloat)0.1f;

    [Header("战斗相关")] public float damageRadius = 1.0f;
    public int defaultHP = 5;

    [Header("比分")]
    public int leftScore;
    public int rightScore;
    public Text ScoreTxt;
    private List<int> _nextRemoveIdList = new();

    // 等待在下一次消耗服务器 tick 时统一生成的出生消息队列，避免各客户端在不同帧/不同 Tick 上生成
    private readonly Queue<RVOClientNetwork.SpawnMessage> _pendingSpawnMessages = new();

    // 等待在下一次消耗服务器 tick 时统一执行的技能消息队列，确保技能也只在服务器 tick 中生效
    private readonly Queue<RVOClientNetwork.SkillMessage> _pendingSkillMessages = new();

    // 左右边界的定点 X，用于出界判断，避免不同客户端浮点差异
    private LFloat _leftBoundaryXFixed;
    private LFloat _rightBoundaryXFixed;

    public enum AgentType
    {
        Owner,
        Enemy,
    }

    private void SpawnOneAtSide(bool leftSide)
    {
        if (leftBoundary == null || rightBoundary == null || topBoundary == null || bottomBoundary == null)
        {
            Debug.LogError("RVODemoManager: 边界未全部指定");
            return;
        }

        float minZ = Mathf.Min(topBoundary.position.z, bottomBoundary.position.z);
        float maxZ = Mathf.Max(topBoundary.position.z, bottomBoundary.position.z);
        float height = maxZ - minZ;
        if (height <= 0.01f)
            return;

        int minZi = Mathf.RoundToInt(minZ);
        int maxZi = Mathf.RoundToInt(maxZ);
        if (minZi >= maxZi)
            maxZi = minZi + 1;

        int z = UnityEngine.Random.Range(minZi, maxZi);

        float xBoundary = leftSide ? leftBoundary.position.x : rightBoundary.position.x;
        int xBoundaryInt = Mathf.RoundToInt(xBoundary);
        int offsetInt = Mathf.RoundToInt(spawnOffsetFromBoundary) * (leftSide ? 1 : -1);
        int x = xBoundaryInt + offsetInt;

        // 本地只发送出生请求，由服务器广播统一生成
        RVOClientNetwork.SendSpawnRequest(leftSide, x, z);
    }

    private class AgentInfo
    {
        public int agentId;
        public GameObject go;
        public bool moveRight; // true: 从左到右，false: 从右到左
        public int moveStepIndex;
        public AgentType AgentType;
        public int hp;

        public float GetMoveStep()
        {
            moveStepIndex++;
            if (moveStepIndex > Simulator.AgentSliceNum)
            {
                moveStepIndex = 0;
            }

            var t = (float)moveStepIndex / Simulator.AgentSliceNum;
            return t;
        }
    }

    private readonly List<AgentInfo> _activeAgents = new List<AgentInfo>();
    private readonly Queue<GameObject> _poolLeftToRight = new Queue<GameObject>();
    private readonly Queue<GameObject> _poolRightToLeft = new Queue<GameObject>();

    private LFloat ToLFloat(float v) => (LFloat)v;
    private LVector2 ToLVector2(Vector3 v3) => new LVector2(ToLFloat(v3.x), ToLFloat(v3.z));
    private Vector3 ToVector3(LVector2 v2) => new Vector3((float)v2.x(), 0f, (float)v2.y());

    private void Start()
    {
        Application.runInBackground = true;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Application.targetFrameRate = 60;
        InitSimulator();

        // 监听服务器的 desync 事件，在检测到分叉时把当前完整状态详细打印出来，便于对比
        RVOClientNetwork.OnDesync += OnNetworkDesync;
    }

    private void OnEnable()
    {
        RVOClientNetwork.OnSpawnMessage += OnSpawnFromServer;
    }

    private void OnDestroy()
    {
        RVOClientNetwork.OnDesync -= OnNetworkDesync;
        RVOClientNetwork.OnSpawnMessage -= OnSpawnFromServer;
    }

    private void OnNetworkDesync(int tick, int expectedHash, int newHash)
    {
        Debug.LogWarning($"[DESYNC] Detected at Tick={tick}, expectedHash={expectedHash}, localHash={newHash}. Dumping full state...");

        var sim = Simulator.Instance;

        // 打印所有仍然存在于 RVO 模拟中的 Agent 的详细信息
        for (int i = 0; i < _activeAgents.Count; i++)
        {
            var info = _activeAgents[i];
            if (!sim.getHasAgent(info.agentId))
                continue;

            LVector2 pos2 = sim.getAgentPosition(info.agentId);
            var posX = pos2.x()._val;
            var posY = pos2.y()._val;

            Debug.LogWarning($"[DESYNC-DUMP] Tick={tick} AgentId={info.agentId} Type={info.AgentType} HP={info.hp} PosX={posX} PosY={posY}");
        }

        Debug.LogWarning($"[DESYNC-DUMP] Tick={tick} Scores: Left={leftScore}, Right={rightScore}, ActiveAgents={_activeAgents.Count}");
    }

    private void Update()
    {
        int ticks;

        // 按键生成：L 生成左侧（Owner，从左往右），R 生成右侧（Enemy，从右往左）
        if (Input.GetKeyDown(KeyCode.L))
        {
            SpawnOneAtSide(true);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            SpawnOneAtSide(false);
        }

        UnityEngine.Profiling.Profiler.BeginSample("RVODemoManager.StepSimulator");
        ticks = StepSimulator();
        UnityEngine.Profiling.Profiler.EndSample();

        // 位置同步只读数据，不影响逻辑，可每帧执行
        UnityEngine.Profiling.Profiler.BeginSample("RVODemoManager.SyncTransforms");
        SyncTransforms();
        UnityEngine.Profiling.Profiler.EndSample();

        // 同步当前激活角色数量到 FPS 显示脚本
        FPSDisplay.CurrentRoleCount = _activeAgents.Count;
    }

    private void InitSimulator()
    {
        var sim = Simulator.Instance;
        sim.Clear();
        sim.setTimeStep(rvoTimeStep);
        sim.SetNumWorkers(0);

        sim.setAgentDefaults(
            rvoNeighborDist,
            rvoMaxNeighbors,
            rvoTimeHorizon,
            rvoTimeHorizonObst,
            rvoRadius,
            rvoMaxSpeed * Simulator.AgentSliceNum,
            new RVO.LVector2(LFloat.zero, LFloat.zero)
        );

        // 用当前场景里左右边界的位置初始化定点边界，供后续出界判断使用
        if (leftBoundary != null && rightBoundary != null)
        {
            _leftBoundaryXFixed = ToLFloat(leftBoundary.position.x);
            _rightBoundaryXFixed = ToLFloat(rightBoundary.position.x);
        }
    }

    private void SpawnLines()
    {
        if (leftBoundary == null || rightBoundary == null || topBoundary == null || bottomBoundary == null)
        {
            Debug.LogError("RVODemoManager: 边界未全部指定");
            return;
        }

        float minZ = Mathf.Min(topBoundary.position.z, bottomBoundary.position.z);
        float maxZ = Mathf.Max(topBoundary.position.z, bottomBoundary.position.z);
        float height = maxZ - minZ;
        if (agentsPerSide <= 0 || height <= 0.01f)
            return;

        float step = height / (agentsPerSide + 1);

        for (int i = 0; i < agentsPerSide; i++)
        {
            float z = minZ + step * (i + 1);
            SpawnOne(true, z, AgentType.Owner); // 左到右
            SpawnOne(false, z, AgentType.Enemy); // 右到左
        }
    }

    private void SpawnOne(bool moveRight, float z, AgentType agentType)
    {
        var prefab = moveRight ? leftToRightPrefab : rightToLeftPrefab;
        if (prefab == null)
        {
            Debug.LogError("RVODemoManager: 预制体未指定");
            return;
        }

        float xBoundary = moveRight ? leftBoundary.position.x : rightBoundary.position.x;
        float dirSign = moveRight ? 1f : -1f;
        float x = xBoundary + dirSign * spawnOffsetFromBoundary;

        Vector3 worldPos = new Vector3(x, 0f, z);
        LVector2 pos2 = ToLVector2(worldPos);

        int agentId = Simulator.Instance.addAgent(pos2);
        if (agentId < 0)
        {
            Debug.LogError("RVODemoManager: addAgent 失败，可能是 defaultAgent 未设置");
            return;
        }

        // 设置首选速度（完全使用定点数）
        LFloat dir = moveRight ? LFloat.one : -LFloat.one;
        LVector2 prefVel = new LVector2(dir * rvoMaxSpeed * Simulator.AgentSliceNum, LFloat.zero);
        Simulator.Instance.setAgentPrefVelocity(agentId, prefVel);

        GameObject go = GetFromPool(moveRight, prefab);
        go.transform.position = worldPos;
        go.SetActive(true);

        _activeAgents.Add(new AgentInfo
        {
            agentId = agentId,
            go = go,
            moveRight = moveRight,
            AgentType = agentType,
            hp = defaultHP
        });
    }

    private GameObject GetFromPool(bool moveRight, GameObject prefab)
    {
        var pool = moveRight ? _poolLeftToRight : _poolRightToLeft;
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        return Instantiate(prefab);
    }

    private void ReturnToPool(AgentInfo info)
    {
        if (info.go == null)
            return;

        info.go.SetActive(false);
        var pool = info.moveRight ? _poolLeftToRight : _poolRightToLeft;
        pool.Enqueue(info.go);
    }

    private int StepSimulator()
    {
        // 一次性取出服务器这帧累计下发的 tick 数
        int ticks = RVOClientNetwork.ConsumeTicks();
        if (ticks <= 0)
        {
            return 0;
        }

        var sim = Simulator.Instance;

        // 按 tick 粒度前进：每一个 tick 都执行一次 doStep -> spawn/skill -> 扣血 -> 出界
        for (int t = 0; t < ticks; t++)
        {
            // 即将要模拟的逻辑 tick 序号（从 1 开始累加），用于按服务器指定的 tick 消费 spawn/skill 队列
            int logicTick = RVOClientNetwork.TotalConsumedTicks + 1;

            sim.doStep();

            // 先在当前 tick 上统一处理所有“应该在本 tick 及之前”生成的出生消息，确保多客户端在同一 tick 生成相同的 Agent 集合
            while (_pendingSpawnMessages.Count > 0 && _pendingSpawnMessages.Peek().tick <= logicTick)
            {
                var spawnMsg = _pendingSpawnMessages.Dequeue();
                SpawnFromServer(spawnMsg);
            }

            // 再在当前 tick 上统一处理所有“应该在本 tick 及之前”生效的技能消息
            while (_pendingSkillMessages.Count > 0 && _pendingSkillMessages.Peek().tick <= logicTick)
            {
                var skillMsg = _pendingSkillMessages.Dequeue();
                Vector3 center = new Vector3(skillMsg.x, 0f, skillMsg.z);
                float radius = skillMsg.radius;
                ExecuteSkillKillInCircle(center, radius);
            }

            UnityEngine.Profiling.Profiler.BeginSample("RVODemoManager.ApplyDamageByEnemy");
            ApplyDamageByEnemy();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("RVODemoManager.RecycleOutOfBounds");
            RecycleOutOfBounds();
            UnityEngine.Profiling.Profiler.EndSample();

            // 统计本地累计已消费的 tick，用于调试对比服务器 tickCount
            RVOClientNetwork.TotalConsumedTicks++;

            // 计算当前全局状态 hash，便于多客户端快速对比是否仍然一致
            int stateHash = ComputeStateHash();
            Debug.LogWarning($"[StateHash] Tick={RVOClientNetwork.TotalConsumedTicks}, Hash={stateHash}");

            // 将本 tick 的状态哈希发送给服务器，由服务器负责检测多客户端是否分叉
            RVOClientNetwork.SendStateHash(RVOClientNetwork.TotalConsumedTicks, stateHash);
        }

        return ticks;
    }

    private void SyncTransforms()
    {
        var sim = Simulator.Instance;
        var len = _activeAgents.Count / Simulator.AgentSliceNum;
        var step = sim.IndexStep - 1; //因为后面加了
        _nextRemoveIdList.Clear();
        for (int i = 0; i < _activeAgents.Count; i++)
        {
            AgentInfo info = _activeAgents[i];
            if (!sim.getHasAgent(info.agentId))
                continue;
            if (i >= step * len && i < (step + 1) * len)
            {
                info.moveStepIndex = 0;
                //sim.getAgentNumAgentNeighbors(info.agentId);
            }

            LVector2 pos2 = sim.getAgentPosition(info.agentId);
            Vector3 pos3 = ToVector3(pos2);
            info.go.transform.position=Vector3.Lerp(info.go.transform.position, pos3, info.GetMoveStep());
            //info.go.transform.position = pos3;
            // 面向 +X / -X
            Vector3 dir = info.moveRight ? Vector3.right : Vector3.left;
            info.go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    private void ApplyDamageByEnemy()
    {
        var sim = Simulator.Instance;
        if (_activeAgents.Count == 0)
        {
            return;
        }

        var killedThisFrame = new List<int>();

        // agentId -> AgentInfo 映射，便于通过邻居 id 找到阵营/HP 信息
        var agentInfoMap = new Dictionary<int, AgentInfo>(_activeAgents.Count);
        for (int k = 0; k < _activeAgents.Count; k++)
        {
            var info = _activeAgents[k];
            agentInfoMap[info.agentId] = info;
        }

        for (int i = _activeAgents.Count - 1; i >= 0; i--)
        {
            var self = _activeAgents[i];
            if (!sim.getHasAgent(self.agentId))
            {
                continue;
            }

            int neighborCount = sim.getAgentNumAgentNeighbors(self.agentId);
            bool hasEnemyNearby = false;

            for (int n = 0; n < neighborCount; n++)
            {
                int neighborId = sim.getAgentAgentNeighbor(self.agentId, n);

                AgentInfo other;
                if (!agentInfoMap.TryGetValue(neighborId, out other))
                {
                    continue;
                }

                if (other.AgentType == self.AgentType)
                {
                    continue;
                }

                // 这里的邻居已经是基于 neighborDist 的 agent 邻居，且我们只区分阵营
                hasEnemyNearby = true;
                break;
            }

            if (!hasEnemyNearby)
            {
                continue;
            }

            self.hp -= 1;
            if (self.hp <= 0)
            {
                killedThisFrame.Add(self.agentId);
                sim.delAgent(self.agentId);
                ReturnToPool(self);
                _activeAgents.RemoveAt(i);
            }
        }

        if (killedThisFrame.Count > 0)
        {
            Debug.LogWarning($"[ApplyDamageByEnemy] Tick={RVOClientNetwork.TotalConsumedTicks}, Killed: {string.Join(",", killedThisFrame)}");
        }
    }

    private void RecycleOutOfBounds()
    {
        if (leftBoundary == null || rightBoundary == null)
            return;

        var sim = Simulator.Instance;

        var recycledThisFrame = new List<string>();

        for (int i = _activeAgents.Count - 1; i >= 0; i--)
        {
            var info = _activeAgents[i];
            if (!sim.getHasAgent(info.agentId))
            {
                ReturnToPool(info);
                _activeAgents.RemoveAt(i);
                continue;
            }

            LVector2 pos2 = sim.getAgentPosition(info.agentId);
            LFloat x = pos2.x();

            // 使用定点数与左右边界进行比较，避免浮点误差导致不同客户端出界时机不一致
            LFloat minX = LMath.Min(_leftBoundaryXFixed, _rightBoundaryXFixed);
            LFloat maxX = LMath.Max(_leftBoundaryXFixed, _rightBoundaryXFixed);

            bool outOfBounds = (x < minX && !info.moveRight) || (x > maxX && info.moveRight);
            if (outOfBounds)
            {
                // 到达对面墙的一侧得分：
                // 左到右移动并越过右边界 -> 右侧得分
                // 右到左移动并越过左边界 -> 左侧得分
                if (info.moveRight && x > maxX)
                {
                    rightScore++;
                    recycledThisFrame.Add($"{info.agentId}:RightScore");
                }
                else if (!info.moveRight && x < minX)
                {
                    leftScore++;
                    recycledThisFrame.Add($"{info.agentId}:LeftScore");
                }
                else
                {
                    recycledThisFrame.Add($"{info.agentId}:NoScore");
                }

                sim.delAgent(info.agentId);
                ReturnToPool(info);
                _activeAgents.RemoveAt(i);
            }
        }

        UpdateScore();

        if (recycledThisFrame.Count > 0)
        {
            Debug.LogWarning($"[RecycleOutOfBounds] Tick={RVOClientNetwork.TotalConsumedTicks}, Recycled: {string.Join(",", recycledThisFrame)}");
        }
    }

    private void OnSpawnFromServer(RVOClientNetwork.SpawnMessage msg)
    {
        // 不在网络回调里直接生成，先缓存，等待下一次消耗服务器 tick 时统一生成
        _pendingSpawnMessages.Enqueue(msg);
    }

    /// <summary>
    /// 供技能控制脚本在收到服务器广播的 skill 消息时调用，先缓存，等待下一次 Tick 统一执行。
    /// </summary>
    public void EnqueueSkill(RVOClientNetwork.SkillMessage msg)
    {
        _pendingSkillMessages.Enqueue(msg);
    }

    private void SpawnFromServer(RVOClientNetwork.SpawnMessage msg)
    {
        bool leftSide = msg.side == 0;
        bool moveRight = leftSide;
        AgentType agentType = leftSide ? AgentType.Owner : AgentType.Enemy;

        Vector3 worldPos = new Vector3(msg.x, 0f, msg.z);
        LVector2 pos2 = ToLVector2(worldPos);

        int agentId = Simulator.Instance.addAgent(pos2);
        if (agentId < 0)
        {
            Debug.LogError("RVODemoManager: addAgent 失败，可能是 defaultAgent 未设置");
            return;
        }

        LFloat dir = moveRight ? LFloat.one : -LFloat.one;
        LVector2 prefVel = new LVector2(dir * rvoMaxSpeed * Simulator.AgentSliceNum, LFloat.zero);
        Simulator.Instance.setAgentPrefVelocity(agentId, prefVel);

        var prefab = moveRight ? leftToRightPrefab : rightToLeftPrefab;
        GameObject go = GetFromPool(moveRight, prefab);
        go.transform.position = worldPos;
        go.SetActive(true);

        _activeAgents.Add(new AgentInfo
        {
            agentId = agentId,
            go = go,
            moveRight = moveRight,
            AgentType = agentType,
            hp = defaultHP
        });
    }

    private void UpdateScore()
    {
        ScoreTxt.text=$"分数 {leftScore} 比 {rightScore}";
    }

    /// <summary>
    /// 在一个圆形范围内（近似）秒杀所有单位（不分敌我），用于技能效果。
    /// 由 RVOSkillController 在收到服务器广播的 skill 消息时调用，确保多端一致。
    /// </summary>
    public void ExecuteSkillKillInCircle(Vector3 center, float radius)
    {
        var sim = Simulator.Instance;

        // 使用定点数做圆形范围判断，减少多客户端之间的边缘差异
        LVector2 centerFixed = new LVector2(ToLFloat(center.x), ToLFloat(center.z));
        LFloat radiusFixed = ToLFloat(radius);
        LFloat radiusSqrFixed = radiusFixed * radiusFixed;

        for (int i = _activeAgents.Count - 1; i >= 0; i--)
        {
            var info = _activeAgents[i];
            if (!sim.getHasAgent(info.agentId))
            {
                ReturnToPool(info);
                _activeAgents.RemoveAt(i);
                continue;
            }

            LVector2 pos2 = sim.getAgentPosition(info.agentId);
            LVector2 diff = pos2 - centerFixed;
            LFloat distSqr = diff.x() * diff.x() + diff.y() * diff.y();

            if (distSqr <= radiusSqrFixed)
            {
                // 命中技能范围，直接移除 agent 和对应 GameObject
                sim.delAgent(info.agentId);
                ReturnToPool(info);
                _activeAgents.RemoveAt(i);
            }
        }
    }

    // 基于当前所有存活 Agent 的 id、hp、阵营和定点坐标计算一个简单 hash，用于对比多端状态
    private int ComputeStateHash()
    {
        unchecked
        {
            int hash = 17;
            var sim = Simulator.Instance;

            for (int i = 0; i < _activeAgents.Count; i++)
            {
                var info = _activeAgents[i];
                if (!sim.getHasAgent(info.agentId))
                    continue;

                LVector2 pos2 = sim.getAgentPosition(info.agentId);

                hash = hash * 31 + info.agentId;
                hash = hash * 31 + (int)info.AgentType;
                hash = hash * 31 + info.hp;
                hash = hash * 31 + pos2.x()._val.GetHashCode();
                hash = hash * 31 + pos2.y()._val.GetHashCode();
            }

            hash = hash * 31 + leftScore;
            hash = hash * 31 + rightScore;

            return hash;
        }
    }
}