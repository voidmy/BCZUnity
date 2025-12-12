using UnityEngine;

/// <summary>
/// 简单帧率/性能信息显示脚本。
/// 挂到任意场景物体上即可，在屏幕左上角显示 FPS 等信息。
/// 可以通过开关键（默认 F1）显示/隐藏。
/// </summary>
public class FPSDisplay : MonoBehaviour
{
    [Header("开关设置")]
    [Tooltip("初始是否显示性能信息")]
    public bool showStats = true;

    [Tooltip("用于开关显示的按键（例如 F1）")]
    public KeyCode toggleKey = KeyCode.F1;

    [Header("刷新设置")]
    [Tooltip("多少秒更新一次 FPS 统计，数值越大越平滑")]
    public float updateInterval = 0.5f;

    [Header("显示样式")]
    [Tooltip("字体大小（像素）")]
    public int fontSize = 18;

    [Tooltip("整体缩放（在高分辨率真机上可适当放大")]
    public float guiScale = 1.0f;

    // 静态角色数量：在其他脚本中直接赋值，例如 FPSDisplay.CurrentRoleCount = 当前角色数;
    public static int CurrentRoleCount;

    private int _frames;
    private float _accumTime;
    private float _timeLeft;
    private float _fps;

    private GUIStyle _style;

    private void Start()
    {
        
        _timeLeft = updateInterval;
    }

    private void Update()
    {
        // 按键开关
        if (Input.GetKeyDown(toggleKey))
        {
            showStats = !showStats;
        }

        if (!showStats)
            return;

        _timeLeft -= Time.unscaledDeltaTime;
        _accumTime += Time.unscaledDeltaTime;
        _frames++;

        if (_timeLeft <= 0f)
        {
            _fps = _frames / Mathf.Max(_accumTime, 1e-6f);
            _frames = 0;
            _accumTime = 0f;
            _timeLeft = updateInterval;
        }
    }

    private void OnGUI()
    {
        if (!showStats)
            return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = Color.white }
            };
        }

        // 左上角原点缩放
        Matrix4x4 oldMatrix = GUI.matrix;
        if (!Mathf.Approximately(guiScale, 1f))
        {
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(guiScale, guiScale, 1f));
        }

        string text = string.Format(
            "FPS: {0:F1}\nFrame Time: {1:F2} ms\nScreen: {2}x{3}\nRole Count: {4}\nServer TickCount: {5}\nLocal ConsumedTicks: {6}",
            _fps,
            _fps > 0.01f ? 1000f / _fps : 0f,
            Screen.width,
            Screen.height,
            CurrentRoleCount,
            RVOClientNetwork.TotalServerTickCount,
            RVOClientNetwork.TotalConsumedTicks
        );

        // 高度适当调大，容纳多出的几行信息
        GUI.Label(new Rect(10, 10, 400, 170), text, _style);

        GUI.matrix = oldMatrix;
    }
}
