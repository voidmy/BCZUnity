using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor收藏夹窗口（支持所有资源+即时切换选中）
/// 支持：拖拽任意资源、显示原生图标、删除、即时定位、窗口停靠
/// </summary>
public class FavoriteWindow : EditorWindow
{
    // 收藏项数据结构（存储GUID和缓存名称/类型，优化显示）
    [System.Serializable]
    private class FavoriteItem
    {
        public string guid; // 资源唯一标识
        public string name; // 缓存资源名称
        public string typeName; // 缓存资源类型（显示用）
    }

    private List<FavoriteItem> _favoriteItems = new List<FavoriteItem>();
    private Vector2 _scrollPos; // 滚动视图位置
    private const string PREFS_KEY = "FavoriteWindow_Items"; // 持久化存储键名

    // 注册到Window菜单（路径：Window/收藏夹）
    [MenuItem("Window/收藏夹", false, 1000)]
    public static void ShowWindow()
    {
        // 显示窗口（支持停靠）
        GetWindow<FavoriteWindow>("收藏夹");
    }

    private void OnEnable()
    {
        // 窗口启用时加载持久化数据
        LoadFavorites();
        // 设置最小窗口大小
        minSize = new Vector2(200, 300);
    }

    private void OnGUI()
    {
        // 绘制窗口标题栏提示
        GUILayout.Label("拖拽任意资源到此处收藏", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 绘制拖拽区域（兼容所有Unity版本）
        DrawDropArea();

        GUILayout.Space(10);

        // 绘制收藏列表（带滚动视图）
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
        {
            // 遍历所有收藏项（倒序绘制，最新添加的在上面）
            for (int i = _favoriteItems.Count - 1; i >= 0; i--)
            {
                DrawFavoriteItem(i);
            }

            // 空列表提示
            if (_favoriteItems.Count == 0)
            {
                GUILayout.Label("暂无收藏项，拖拽资源到上方区域添加", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制拖拽区域（可视化提示，兼容所有版本）
    /// </summary>
    private void DrawDropArea()
    {
        // 创建拖拽区域（占满宽度，高度50，带边框）
        Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "", EditorStyles.helpBox); // 绘制边框
        GUI.Label(dropArea, "📁 拖拽资源到这里添加收藏", EditorStyles.boldLabel);

        // 处理拖拽事件
        EventType eventType = Event.current.type;
        if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
        {
            // 筛选有效资源（仅Project窗口的资源，排除场景对象）
            bool hasValidAsset = false;
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (IsValidAsset(obj))
                {
                    hasValidAsset = true;
                    break;
                }
            }

            if (!hasValidAsset)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; // 不可接收（禁止图标）
                return;
            }

            // 拖拽更新时显示可接收状态（复制图标）
            if (eventType == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }
            // 拖拽释放时添加收藏
            else if (eventType == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (Object obj in DragAndDrop.objectReferences)
                {
                    if (IsValidAsset(obj))
                    {
                        AddFavorite(obj);
                    }
                }
                Event.current.Use();
                Repaint();
            }
        }
    }

    /// <summary>
    /// 绘制单个收藏项（优化图标+名称+类型+删除按钮）
    /// </summary>
    private void DrawFavoriteItem(int index)
    {
        var item = _favoriteItems[index];
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        {
            // 1. 绘制资源图标（使用Unity原生图标，清晰统一）
            var asset = GetAssetByGuid(item.guid);
            if (asset != null)
            {
                // 获取资源原生图标（大小20x20，适配显示）
                GUIContent assetContent = EditorGUIUtility.ObjectContent(asset, asset.GetType());
                GUILayout.Label(assetContent.image, GUILayout.Width(20), GUILayout.Height(20));

                // 显示资源信息（名称+类型）
                EditorGUILayout.BeginVertical();
                {
                    // 名称（点击定位，优化即时性）
                    if (GUILayout.Button(item.name, EditorStyles.boldLabel, GUILayout.ExpandWidth(true)))
                    {
                        // 关键优化：强制立即定位并刷新
                        LocateAssetImmediately(item.guid);
                    }
                    // 名称（点击定位，优化即时性）
                    if (GUILayout.Button(item.typeName, EditorStyles.boldLabel, GUILayout.ExpandWidth(true)))
                    {
                        // 关键优化：强制立即定位并刷新
                        LocateAssetImmediately(item.guid);
                    }
                    // 类型（灰色小字，辅助识别）
                    //GUILayout.Label(item.typeName, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                // 资源已删除（无效项）
                GUILayout.Label("❌", GUILayout.Width(20), GUILayout.Height(20));
                EditorGUILayout.BeginVertical();
                {
                    GUILayout.Label($"[无效资源] {item.name}", EditorStyles.colorField);
                    GUILayout.Label(item.typeName, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
            }

            // 2. 删除按钮（ hover 时显示红色，更直观）
            using (new EditorGUI.DisabledScope(asset == null))
            {
                if (GUILayout.Button("×", GetDeleteButtonStyle(), GUILayout.Width(28), GUILayout.Height(28)))
                {
                    RemoveFavorite(index);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);
    }

    /// <summary>
    /// 获取删除按钮样式（hover红色）
    /// </summary>
    private GUIStyle GetDeleteButtonStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniButton);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 14;

        // hover 时背景变红
        if (Event.current.type == EventType.MouseMove && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        {
            style.normal.background = MakeColorTexture(Color.red);
            style.normal.textColor = Color.white;
        }
        else
        {
            style.normal.background = null;
            style.normal.textColor = Color.grey;
        }

        return style;
    }

    /// <summary>
    /// 创建纯色纹理（用于按钮背景）
    /// </summary>
    private Texture2D MakeColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// 判断是否为有效资源（仅Project窗口的资源，排除场景对象）
    /// </summary>
    private bool IsValidAsset(Object obj)
    {
        if (obj == null) return false;

        // 场景中的对象（如场景里的GameObject）没有AssetPath，排除
        string assetPath = AssetDatabase.GetAssetPath(obj);
        return !string.IsNullOrEmpty(assetPath);
    }

    /// <summary>
    /// 添加收藏项（自动去重，缓存名称和类型）
    /// </summary>
    private void AddFavorite(Object asset)
    {
        string assetPath = AssetDatabase.GetAssetPath(asset);
        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        // 去重：避免重复添加同一资源
        if (_favoriteItems.Exists(item => item.guid == guid)) return;

        // 添加到列表（最新添加的在前面）
        _favoriteItems.Insert(0, new FavoriteItem
        {
            guid = guid,
            name = asset.name,
            typeName = asset.GetType().Name // 缓存资源类型名称
        });

        // 保存到持久化存储
        SaveFavorites();
    }

    /// <summary>
    /// 移除收藏项
    /// </summary>
    private void RemoveFavorite(int index)
    {
        _favoriteItems.RemoveAt(index);
        SaveFavorites();
        Repaint(); // 刷新界面
    }

    /// <summary>
    /// 即时定位资源（优化快速切换的即时性）
    /// </summary>
    private void LocateAssetImmediately(string guid)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(assetPath)) return;

        // 加载资源（强制同步加载）
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (asset == null) return;

        // 关键步骤1：先清空选中状态，避免延迟
        Selection.activeObject = null;
        // 强制刷新Editor（消除之前的选中状态残留）
        EditorApplication.DirtyHierarchyWindowSorting();
        EditorApplication.RepaintProjectWindow();

        // 关键步骤2：设置新的选中状态
        Selection.activeObject = asset;
        // 关键步骤3：强制聚焦并刷新Project窗口
        EditorUtility.FocusProjectWindow();
        EditorApplication.RepaintProjectWindow(); // 强制刷新UI，即时显示选中状态
        EditorGUIUtility.PingObject(asset); // 闪烁提示（不影响即时性）

        // 额外保险：触发Editor刷新事件
        EditorApplication.QueuePlayerLoopUpdate();
    }

    /// <summary>
    /// 通过GUID获取资源
    /// </summary>
    private Object GetAssetByGuid(string guid)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<Object>(assetPath);
    }

    /// <summary>
    /// 保存收藏项到EditorPrefs（持久化）
    /// </summary>
    private void SaveFavorites()
    {
        string json = JsonUtility.ToJson(new FavoriteList { items = _favoriteItems });
        EditorPrefs.SetString(PREFS_KEY, json);
    }

    /// <summary>
    /// 从EditorPrefs加载收藏项
    /// </summary>
    private void LoadFavorites()
    {
        if (EditorPrefs.HasKey(PREFS_KEY))
        {
            string json = EditorPrefs.GetString(PREFS_KEY);
            var favoriteList = JsonUtility.FromJson<FavoriteList>(json);
            _favoriteItems = favoriteList?.items ?? new List<FavoriteItem>();

            // 校验无效资源（GUID对应的资源已删除）
            _favoriteItems.RemoveAll(item => string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(item.guid)));
        }
        else
        {
            _favoriteItems = new List<FavoriteItem>();
        }
    }

    // 辅助类：用于JSON序列化（JsonUtility不支持直接序列化List<T>）
    [System.Serializable]
    private class FavoriteList
    {
        public List<FavoriteItem> items;
    }

    // 清理持久化数据（可选：如需重置收藏夹，取消注释并执行一次）
    // [MenuItem("Tools/清理收藏夹数据")]
    // private static void ClearFavorites()
    // {
    //     EditorPrefs.DeleteKey(PREFS_KEY);
    //     Debug.Log("收藏夹数据已清理");
    // }
}