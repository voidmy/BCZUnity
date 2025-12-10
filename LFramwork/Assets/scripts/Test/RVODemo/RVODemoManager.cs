using System.Collections;
using System.Collections.Generic;
using Lockstep.Math;
using RVO;
using UnityEngine;
using LVector2 = RVO.LVector2;

public class RVODemoManager : MonoBehaviour
{
    [Header("边界（世界坐标，X-Z 平面）")]
    [Tooltip("左边界：决定从右往左移动时的回收位置，以及左侧出生点的参考位置")]
    public Transform leftBoundary;
    [Tooltip("右边界：决定从左往右移动时的回收位置，以及右侧出生点的参考位置")]
    public Transform rightBoundary;
    [Tooltip("上边界：与下边界一起决定纵向范围，用来平均分布一排排角色")]
    public Transform topBoundary;
    [Tooltip("下边界：与上边界一起决定纵向范围，用来平均分布一排排角色")]
    public Transform bottomBoundary;

    [Header("左右两侧角色预制体")]
    [Tooltip("从左往右移动的角色预制体")]
    public GameObject leftToRightPrefab;
    [Tooltip("从右往左移动的角色预制体")]
    public GameObject rightToLeftPrefab;

    [Header("生成配置")]
    [Tooltip("每一侧要生成多少个角色（左边 N 个，右边 N 个）")]
    public int agentsPerSide = 5;
    [Tooltip("纵向间距说明（目前主要由上/下边界平均分布，这个值可以作为备用或调试使用）")]
    public float verticalSpacing = 1.5f;
    [Tooltip("出生位置相对于左右边界在 X 方向的偏移量，>0 表示生成在边界内侧一点")]
    public float spawnOffsetFromBoundary = 0.5f;

    [Header("RVO 配置（定点）")]
    [Tooltip("邻居感知距离：RVO 计算时能看到多远的其他 Agent（越大越安全但更耗性能）")]
    public float rvoNeighborDist = 3f;
    [Tooltip("每个 Agent 参与计算的最大邻居数量（过小会不安全，过大会更耗性能）")]
    public int rvoMaxNeighbors = 10;
    [Tooltip("与其他 Agent 的时间视野：越大越早避让，但自由度越低")]
    public float rvoTimeHorizon = 2f;
    [Tooltip("与障碍物的时间视野：越大越早避让障碍，但自由度越低")]
    public float rvoTimeHorizonObst = 2f;
    [Tooltip("Agent 碰撞半径（决定彼此之间的最小间距）")]
    public float rvoRadius = 0.5f;
    [Tooltip("最大移动速度（RVO 会在不违反约束的前提下尽量接近这个速度）")]
    public float rvoMaxSpeed = 2f;
    [Tooltip("RVO 仿真时间步长（单位秒）：越小越精确但计算更频繁")]
    public float rvoTimeStep = 0.1f;

    private class AgentInfo
    {
        public int agentId;
        public GameObject go;
        public bool moveRight; // true: 从左到右，false: 从右到左
    }

    private readonly List<AgentInfo> _activeAgents = new List<AgentInfo>();
    private readonly Queue<GameObject> _poolLeftToRight = new Queue<GameObject>();
    private readonly Queue<GameObject> _poolRightToLeft = new Queue<GameObject>();

    private LFloat ToLFloat(float v) => (LFloat)v;
    private LVector2 ToLVector2(Vector3 v3) => new LVector2(ToLFloat(v3.x), ToLFloat(v3.z));
    private Vector3 ToVector3(LVector2 v2) => new Vector3((float)v2.x(), 0f, (float)v2.y());

    private void Start()
    {
        InitSimulator();
        StartCoroutine(SpawnObj());
    }

    IEnumerator SpawnObj()
    {
        var tm = new WaitForSeconds(2f);
        SpawnLines();
        while (true)
        {
            yield return tm;
            if (_activeAgents.Count < 2000)
            {
                SpawnLines();
            }
            
        }
        
    }

    private void FixedUpdate()
    {
        UnityEngine.Profiling.Profiler.BeginSample("RVODemoManager.StepSimulator");
        StepSimulator();
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("RVODemoManager.SyncTransforms");
        SyncTransforms();
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("RVODemoManager.RecycleOutOfBounds");
        RecycleOutOfBounds();
        UnityEngine.Profiling.Profiler.EndSample();

        // 同步当前激活角色数量到 FPS 显示脚本
        FPSDisplay.CurrentRoleCount = _activeAgents.Count;
    }

    private void InitSimulator()
    {
        var sim = Simulator.Instance;
        sim.Clear();
        sim.setTimeStep(ToLFloat(rvoTimeStep));
        sim.SetNumWorkers(0);

        sim.setAgentDefaults(
            ToLFloat(rvoNeighborDist),
            rvoMaxNeighbors,
            ToLFloat(rvoTimeHorizon),
            ToLFloat(rvoTimeHorizonObst),
            ToLFloat(rvoRadius),
            ToLFloat(rvoMaxSpeed),
            new  RVO.LVector2(LFloat.zero,LFloat.zero)
        );
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
            SpawnOne(true, z);  // 左到右
            SpawnOne(false, z); // 右到左
        }
    }

    private void SpawnOne(bool moveRight, float z)
    {
        var prefab = moveRight ? leftToRightPrefab : rightToLeftPrefab;
        if (prefab == null)
        {
            Debug.LogError("RVODemoManager: 预制体未指定");
            return;
        }

        float xBoundary = moveRight ? leftBoundary.position.x : rightBoundary.position.x;
        float dir = moveRight ? 1f : -1f;
        float x = xBoundary + dir * spawnOffsetFromBoundary;

        Vector3 worldPos = new Vector3(x, 0f, z);
        LVector2 pos2 = ToLVector2(worldPos);

        int agentId = Simulator.Instance.addAgent(pos2);
        if (agentId < 0)
        {
            Debug.LogError("RVODemoManager: addAgent 失败，可能是 defaultAgent 未设置");
            return;
        }

        // 设置首选速度
        LVector2 prefVel = new LVector2(ToLFloat(dir * rvoMaxSpeed), LFloat.zero);
        Simulator.Instance.setAgentPrefVelocity(agentId, prefVel);

        GameObject go = GetFromPool(moveRight, prefab);
        go.transform.position = worldPos;
        go.SetActive(true);

        _activeAgents.Add(new AgentInfo
        {
            agentId = agentId,
            go = go,
            moveRight = moveRight
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

    private void StepSimulator()
    {
        Simulator.Instance.doStep();
    }

    private void SyncTransforms()
    {
        var sim = Simulator.Instance;
        for (int i = 0; i < _activeAgents.Count; i++)
        {
            AgentInfo info = _activeAgents[i];
            if (!sim.getHasAgent(info.agentId))
                continue;

            LVector2 pos2 = sim.getAgentPosition(info.agentId);
            Vector3 pos3 = ToVector3(pos2);
            info.go.transform.position = pos3;
            // 面向 +X / -X
            Vector3 dir = info.moveRight ? Vector3.right : Vector3.left;
            info.go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    private void RecycleOutOfBounds()
    {
        if (leftBoundary == null || rightBoundary == null)
            return;

        float leftX = leftBoundary.position.x;
        float rightX = rightBoundary.position.x;
        float minX = Mathf.Min(leftX, rightX);
        float maxX = Mathf.Max(leftX, rightX);

        var sim = Simulator.Instance;

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
            float x = (float)pos2.x();

            bool outOfBounds = (x < minX && !info.moveRight) || (x > maxX && info.moveRight);
            if (outOfBounds)
            {
                sim.delAgent(info.agentId);
                ReturnToPool(info);
                _activeAgents.RemoveAt(i);
            }
        }
    }
}
