using System.Collections.Generic;
using Lockstep.Math;
using RVO;

/// <summary>
/// 技能范围结算工具：
/// - 由 RVODemoManager 在 StepSimulator 中调用；
/// - 基于 RVO 的定点坐标做标准圆形范围判定（距离平方 <= 半径平方）；
/// - 不关心阵营、血量与真正的删除逻辑，这些仍由 RVODemoManager 处理。
/// </summary>
public static class RVOSkillResolver
{
    /// <summary>
    /// 收集所有技能 Agent 命中的单位 id（基于圆形范围距离判定）。
    /// </summary>
    /// <param name="sim">RVO 模拟器实例。</param>
    /// <param name="skillAgentIds">当前场景中所有技能 Agent 的 id 列表。</param>
    /// <param name="skillRadii">与 skillAgentIds 对应的技能半径列表（定点）。</param>
    /// <param name="activeAgentIds">当前场景中所有可被命中的单位的 agentId 列表。</param>
    /// <param name="killedBySkill">输出：被任意技能命中的 agentId 集合。</param>
    public static void CollectSkillVictims(
        Simulator sim,
        IList<int> skillAgentIds,
        IList<LFloat> skillRadii,
        IList<int> activeAgentIds,
        HashSet<int> killedBySkill)
    {
        if (skillAgentIds == null || skillRadii == null || activeAgentIds == null || killedBySkill == null)
        {
            return;
        }

        for (int i = 0; i < skillAgentIds.Count; i++)
        {
            int skillId = skillAgentIds[i];
            if (!sim.getHasAgent(skillId))
            {
                continue;
            }

            if (i >= skillRadii.Count)
            {
                continue;
            }

            RVO.LVector2 center = sim.getAgentPosition(skillId);
            LFloat radius = skillRadii[i];
            LFloat radiusSqr = radius * radius;

            int hitCount = 0;

            for (int n = 0; n < activeAgentIds.Count; n++)
            {
                int targetId = activeAgentIds[n];
                if (!sim.getHasAgent(targetId))
                {
                    continue;
                }

                RVO.LVector2 pos = sim.getAgentPosition(targetId);
                RVO.LVector2 diff = pos - center;
                LFloat distSqr = diff.x() * diff.x() + diff.y() * diff.y();
                if (distSqr <= radiusSqr)
                {
                    killedBySkill.Add(targetId);
                    hitCount++;
                }
            }

            if (hitCount > 0)
            {
                UnityEngine.Debug.LogWarning($"[SkillDebug-RVO] Tick={RVOClientNetwork.TotalConsumedTicks + 1}, SkillAgentId={skillId}, HitCount={hitCount}");
            }
        }
    }
}
