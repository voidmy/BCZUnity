using Lockstep.Math;
using RVO;
using UnityEngine;
using LVector2 = RVO.LVector2;

public class RVOBulletController : MonoBehaviour
{
    public RVODemoManager demoManager;
    public LFloat speed = 6;
    public LFloat radius = (LFloat)0.2f;
    public int damage = 1;
    public LFloat maxLifeTime = 5;

    private int _agentId = -1;

    private void Awake()
    {
        if (demoManager == null)
        {
            demoManager = FindObjectOfType<RVODemoManager>();
        }

        var sim = Simulator.Instance;
        Vector3 pos = transform.position;
        LVector2 pos2 = new LVector2((LFloat)pos.x, (LFloat)pos.z);

        _agentId = sim.addAgent(pos2);
        if (_agentId >= 0)
        {
            // 子弹在避让中完全不可见：不参与邻居计算
            sim.setAgentAvoidType(_agentId, AgentAvoidType.Invisible);

            // 子弹自身不考虑任何邻居，避免被 RVO 偏转，保证直线飞行
            sim.setAgentMaxNeighbors(_agentId, 0);

            // 使用当前朝向设置一个固定的 RVO 速度，让 RVO 驱动子弹运动（全程在 LFloat 域内运算）
            Vector3 dir = transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-4f)
            {
                dir.Normalize();

                LFloat fx = (LFloat)dir.x;
                LFloat fz = (LFloat)dir.z;

                LFloat sx = fx * speed * Simulator.AgentSliceNum;
                LFloat sz = fz * speed * Simulator.AgentSliceNum;

                LVector2 vel = new LVector2(sx, sz);
                sim.setAgentPrefVelocity(_agentId, vel);
            }

            if (demoManager != null)
            {
                demoManager.RegisterBullet(_agentId, radius, damage, this);
            }
        }
    }

    private void Update()
    {
        var sim = Simulator.Instance;

        if (_agentId < 0 || !sim.getHasAgent(_agentId))
        {
            // 对应的 RVO Agent 已不存在，直接销毁子弹
            DestroyBullet();
            return;
        }

        // 从 RVO 获取当前逻辑位置，并直接用于表现
        LVector2 logicPos2 = sim.getAgentPosition(_agentId);
        Vector3 pos = new Vector3((float)logicPos2.x(), 0f, (float)logicPos2.y());
        transform.position = pos;

        if (demoManager != null)
        {
            float minX = float.NegativeInfinity;
            float maxX = float.PositiveInfinity;
            float minZ = float.NegativeInfinity;
            float maxZ = float.PositiveInfinity;

            if (demoManager.leftBoundary != null && demoManager.rightBoundary != null)
            {
                float lx = demoManager.leftBoundary.position.x;
                float rx = demoManager.rightBoundary.position.x;
                minX = Mathf.Min(lx, rx);
                maxX = Mathf.Max(lx, rx);
            }

            if (demoManager.topBoundary != null && demoManager.bottomBoundary != null)
            {
                float tz = demoManager.topBoundary.position.z;
                float bz = demoManager.bottomBoundary.position.z;
                minZ = Mathf.Min(tz, bz);
                maxZ = Mathf.Max(tz, bz);
            }

            if (pos.x < minX - 1f || pos.x > maxX + 1f || pos.z < minZ - 1f || pos.z > maxZ + 1f)
            {
                DestroyBullet();
                return;
            }
        }

        // 使用 RVO 逻辑位置和定点半径做伤害判定
        // 伤害结算改为在 RVODemoManager.StepSimulator 的 tick 循环中统一处理
    }

    private void OnDestroy()
    {
        var sim = Simulator.Instance;
        if (_agentId >= 0 && sim.getHasAgent(_agentId))
        {
            sim.delAgent(_agentId);
        }
        if (demoManager != null)
        {
            demoManager.UnregisterBullet(_agentId, this);
        }
        _agentId = -1;
    }

    private void DestroyBullet()
    {
        Destroy(gameObject);
    }
}
