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
        // 鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            TryCastSkillByScreenPosition(Input.mousePosition);
        }

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
        // 触摸（简单处理第一个触点的按下）
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                TryCastSkillByScreenPosition(touch.position);
            }
        }
#endif

        // 可选：键盘按键也在屏幕中心释放一次技能
        if (Input.GetKeyDown(castKey))
        {
            Vector3 centerScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            TryCastSkillByScreenPosition(centerScreen);
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
        }
    }

    private void OnSkillFromServer(RVOClientNetwork.SkillMessage msg)
    {
        if (demoManager == null)
            return;

        // 不直接修改仿真数据，交给 RVODemoManager 在下一次 Tick 中统一处理
        demoManager.EnqueueSkill(msg);
    }
}
