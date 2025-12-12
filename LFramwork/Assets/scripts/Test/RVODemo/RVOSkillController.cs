using UnityEngine;

/// <summary>
/// 简单范围技能控制器：
/// - 本地检测鼠标点击 / 触摸，在点击位置请求释放技能；
/// - 技能请求通过 RVOClientNetwork 发送到服务器；
/// - 服务器广播 skill 消息后，本脚本在所有客户端统一执行技能效果。
/// </summary>
public class RVOSkillController : MonoBehaviour
{
    [Header("技能配置")]
    public float skillRadius = 3f;           // 技能半径（世界坐标，近似圆即可）
    public KeyCode castKey = KeyCode.Space;  // 键盘释放技能按键（可选）

    [Header("引用")]
    public RVODemoManager demoManager;       // 在 Inspector 里拖引用
    public Camera worldCamera;               // 用于从屏幕点击射线到场景，默认为 Camera.main

    [Header("技能特效与冷却")]
    public GameObject skillEffectPrefab;     // 释放技能时在世界中显示的特效
    public float cooldownSeconds = 1.5f;     // 冷却时间（秒），冷却结束前不能再次释放

    [Tooltip("技能特效在场景中保留的时间（秒），到时间后自动销毁")]
    public float effectLifetime = 1.0f;

    private float _nextCastTime = 0f;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        RVOClientNetwork.OnSkillMessage += OnSkillFromServer;
    }

    private void OnDisable()
    {
        RVOClientNetwork.OnSkillMessage -= OnSkillFromServer;
    }

    private void Update()
    {
        // 冷却中，直接忽略所有释放请求
        bool canCast = Time.time >= _nextCastTime;

        // 只允许按键释放技能
        if (canCast && Input.GetKeyDown(castKey))
        {
            // 使用当前鼠标位置来确定技能落点
            Vector3 mousePos = Input.mousePosition;
            TryCastSkillByScreenPosition(mousePos);
        }
    }

    private void TryCastSkillByScreenPosition(Vector3 screenPos)
    {
        if (worldCamera == null)
            return;

        Ray ray = worldCamera.ScreenPointToRay(screenPos);
        // 假设角色都在 y=0 的平面上，这里射到 y=0 平面
        if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter))
        {
            Vector3 worldPos = ray.GetPoint(enter);

            int x = Mathf.RoundToInt(worldPos.x);
            int z = Mathf.RoundToInt(worldPos.z);
            int r = Mathf.RoundToInt(skillRadius);

            // 本地只发送请求，真正生效在收到服务器广播的 skill 消息时
            RVOClientNetwork.SendSkillRequest(x, z, r);

            // 进入冷却
            _nextCastTime = Time.time + cooldownSeconds;
        }
    }

    private void OnSkillFromServer(RVOClientNetwork.SkillMessage msg)
    {
        if (demoManager == null)
            return;

        // 不直接修改仿真数据，交给 RVODemoManager 在下一次 Tick 中统一处理
        demoManager.EnqueueSkill(msg);

        // 在所有客户端统一显示技能特效（位置和大小一致）
        if (skillEffectPrefab != null)
        {
            Vector3 worldPos = new Vector3(msg.x, 0f, msg.z);
            var go = Instantiate(skillEffectPrefab, worldPos, Quaternion.identity);

            // 简单按半径缩放特效大小（假设 prefab 是单位大小）
            float scale = msg.radius * 2f;
            go.transform.localScale = new Vector3(scale, scale, scale);

            if (effectLifetime > 0f)
            {
                Destroy(go, effectLifetime);
            }
        }
    }
}
