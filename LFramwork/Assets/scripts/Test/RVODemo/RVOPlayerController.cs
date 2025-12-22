using Lockstep.Math;
using RVO;
using UnityEngine;

/// <summary>
/// 简单玩家控制：
/// - 使用键盘 WASD 直接控制自身 GameObject 的移动；
/// - 使用 RVODemoManager 的边界（left/right/top/bottom）限制移动范围（XZ 平面内不出界）。
/// </summary>
public class RVOPlayerController : MonoBehaviour
{
    [Header("引用")]
    public RVODemoManager demoManager;

    [Header("输入配置")]
    public KeyCode keyUp = KeyCode.W;
    public KeyCode keyDown = KeyCode.S;
    public KeyCode keyLeft = KeyCode.A;
    public KeyCode keyRight = KeyCode.D;

    [Header("移动配置")]
    public float moveSpeed = 2f; // 玩家移动速度（世界坐标单位/秒）

    [Header("子弹配置")]
    public GameObject bulletPrefab;
    public float bulletFireInterval = 1f;
    public float bulletSpawnOffset = 0.5f;

    // 对应的服务器分配的玩家 Id（等于 userId）
    public int playerId;

    // 是否为本地玩家（只有本地玩家会采集输入并发送到服务器）
    public bool isLocalPlayer;

    // 起始位置
    public Vector3 start = Vector3.zero;

    // 定点数字段
    private Lockstep.Math.LVector2 _posFixed;
    private LFloat _moveSpeedFixed;

    private float _bulletTimer;

    private void Awake()
    {
        if (demoManager == null)
        {
            demoManager = FindObjectOfType<RVODemoManager>();
        }
    }

    private void Start()
    {
        _posFixed = new Lockstep.Math.LVector2((LFloat)start.x, (LFloat)start.z);
        _moveSpeedFixed = (LFloat)moveSpeed;
        Debug.Log($"[PlayerController.Start] playerId={playerId}, isLocalPlayer={isLocalPlayer}, LocalUserId={RVOClientNetwork.LocalUserId}");
    }

    private void Update()
    {
        if (demoManager == null)
        {
            return;
        }
        if (isLocalPlayer)
        {
            HandleInputAndSendToServer();
            HandleFireBullet();
        }
        else
        {
            // // 调试：查看为什么不是本地玩家
            // if (Time.frameCount % 60 == 0) // 每秒打印一次
            // {
            //     Debug.Log($"[PlayerController] Not local: playerId={playerId}, LocalUserId={RVOClientNetwork.LocalUserId}, isLocalPlayer={isLocalPlayer}");
            // }
        }
    }

    private void HandleInputAndSendToServer()
    {
        // 读取 WASD 输入，合成一个平面方向
        Vector2 dir = Vector2.zero;
        if (Input.GetKey(keyUp)) dir.y += 1f;
        if (Input.GetKey(keyDown)) dir.y -= 1f;
        if (Input.GetKey(keyRight)) dir.x += 1f;
        if (Input.GetKey(keyLeft)) dir.x -= 1f;

        // 只同步输入方向（定点），不在这里做本地移动，真正的逻辑位移放到服务器 tick 驱动的步骤里
        if (dir.sqrMagnitude > 1e-4f)
        {
            dir.Normalize();
        }

        // 把当前输入方向转换成定点整数发给服务器（包括0,0）
        int ix = Mathf.RoundToInt(dir.x * 1000f);
        int iz = Mathf.RoundToInt(dir.y * 1000f);

        // 直接使用 LocalUserId 发送（服务器会验证）
        RVOClientNetwork.SendPlayerInput(RVOClientNetwork.LocalUserId, ix, iz);
        
        // Debug输出以验证发送
        if (ix != 0 || iz != 0)
        {
            Debug.Log($"[PlayerInput] Sending: userId={RVOClientNetwork.LocalUserId}, dx={ix}, dz={iz}");
        }
    }

    private void HandleFireBullet()
    {
        _bulletTimer += Time.deltaTime;
        if (_bulletTimer < bulletFireInterval)
        {
            return;
        }

        _bulletTimer = 0f;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 1e-4f)
        {
            return;
        }
        forward.Normalize();

        Vector3 spawnPos = transform.position + forward * bulletSpawnOffset;
        int x = Mathf.RoundToInt(spawnPos.x);
        int z = Mathf.RoundToInt(spawnPos.z);
        int dirX = Mathf.RoundToInt(forward.x * 1000f);
        int dirZ = Mathf.RoundToInt(forward.z * 1000f);

        RVOClientNetwork.SendBulletRequest(x, z, dirX, dirZ);
    }

    public void ApplySnapshot(float x, float z, float vx, float vz)
    {
        Vector3 pos = transform.position;
        pos.x = x;
        pos.z = z;
        transform.position = pos;
        
        // 远端玩家根据服务器广播的朝向设置旋转，本地玩家的旋转已经在输入阶段处理
        if (!isLocalPlayer)
        {
            Vector3 vel = new Vector3(vx, 0f, vz);
            if (vel.sqrMagnitude > 1e-4f)
            {
                transform.rotation = Quaternion.LookRotation(vel, Vector3.up);
            }
        }
    }
}
