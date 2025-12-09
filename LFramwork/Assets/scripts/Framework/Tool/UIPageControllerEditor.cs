#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

[CustomEditor(typeof(UIPageController))]
public class UIPageControllerEditor : Editor
{
    private UIPageController controller;
    private string newPageName = "Page1";
    private string currentPageName = "Page1";
    private Vector2 scrollPos;
    private List<string> atlasNames = new List<string>();
    private Vector2 pagesScrollPos;
    private bool isNameFieldFocused = false;
    private string nameFieldControlName = "PageNameField";

    private void OnEnable()
    {
        controller = (UIPageController)target;
        RefreshAtlasList();
        UpdateCurrentPageName();
    }

    private void UpdateCurrentPageName()
    {
        if (controller.pages.Count > 0 && controller.currentPageIndex >= 0 && controller.currentPageIndex < controller.pages.Count)
        {
            currentPageName = controller.pages[controller.currentPageIndex].pageName;
        }
    }

    private void RefreshAtlasList()
    {
        atlasNames.Clear();
        atlasNames.Add("None");

        // 查找所有Sprite Atlas资源
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas != null)
            {
                atlasNames.Add(atlas.name);
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Page Controller", EditorStyles.boldLabel);

        // 刷新图集列表按钮
        if (GUILayout.Button("Refresh Atlas List"))
        {
            RefreshAtlasList();
        }

        // 添加新页面
        EditorGUILayout.BeginHorizontal();
        newPageName = EditorGUILayout.TextField("New Page Name:", newPageName);
        if (GUILayout.Button("Add Page", GUILayout.Width(100)))
        {
            string finalName = GenerateUniquePageName(newPageName);

            // 创建新页面并复制当前页面的GameObject设置
            var newPage = new UIPageController.UIPage() { pageName = finalName };

            if (controller.pages.Count > 0)
            {
                var currentPage = controller.pages[controller.currentPageIndex];

                // 复制当前页面的所有ControlledObject
                foreach (var element in currentPage.controlledElements)
                {
                    // 克隆ControlledObject
                    var newElement = new UIPageController.UIControlledElement
                    {
                        target = element.target,
                        recordActiveState = element.recordActiveState,
                        isActive = element.isActive,
                        recordTransform = element.recordTransform,
                        recordPosition = element.recordPosition,
                        recordRotation = element.recordRotation,
                        recordScale = element.recordScale,
                        recordRectTransform = element.recordRectTransform,      // 复制RectTransform记录状态
                        recordAnchoredPosition = element.recordAnchoredPosition, // 复制锚点位置记录状态
                        recordSizeDelta = element.recordSizeDelta,             // 复制尺寸增量记录状态
                        recordPivot = element.recordPivot,                     // 复制轴心点记录状态
                        recordAnchoredPosition3D = element.recordAnchoredPosition3D, // 复制3D锚点位置记录状态
                        recordAnchorMinMax = element.recordAnchorMinMax,       // 复制锚点范围记录状态
                        anchoredPosition = element.anchoredPosition,            // 复制锚点位置
                        sizeDelta = element.sizeDelta,                          // 复制尺寸增量
                        pivot = element.pivot,                                  // 复制轴心点
                        anchoredPosition3D = element.anchoredPosition3D,        // 复制3D锚点位置
                        anchorMin = element.anchorMin,                          // 复制最小锚点
                        anchorMax = element.anchorMax,                          // 复制最大锚点
                        recordSpriteRenderer = element.recordSpriteRenderer,
                        spriteName = element.spriteName,
                        atlasName = element.atlasName,
                        spriteColor = element.spriteColor,
                        recordImage = element.recordImage,
                        imageSpriteName = element.imageSpriteName,
                        imageAtlasName = element.imageAtlasName,
                        imageColor = element.imageColor,
                        recordTextMeshPro = element.recordTextMeshPro,
                        recordTextMeshProText = element.recordTextMeshProText,
                        recordTextMeshProStyle = element.recordTextMeshProStyle,
                        text = element.text,
                        fontSize = element.fontSize,
                        textColor = element.textColor,
                        fontStyle = element.fontStyle,
                        recordImageMaterial = element.recordImageMaterial,
                        imageMaterial = element.imageMaterial
                    };

#if UNITY_EDITOR
                    newElement.editorSprite = element.editorSprite;
                    newElement.editorImageSprite = element.editorImageSprite;
#endif

                    newPage.controlledElements.Add(newElement);
                }
            }

            controller.pages.Add(newPage);
            controller.currentPageIndex = controller.pages.Count - 1;
            newPageName = "Page1";
            UpdateCurrentPageName();
            EditorUtility.SetDirty(controller);
            controller.ApplyCurrentPage();

            // 失去焦点
            GUI.FocusControl(null);
            isNameFieldFocused = false;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 页面选择
        if (controller.pages.Count > 0)
        {
            EditorGUILayout.LabelField("Current Page:", EditorStyles.boldLabel);

            // 当前页面名称编辑框
            EditorGUILayout.BeginHorizontal();

            // 使文本框有唯一的控制ID以便于焦点管理
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName(nameFieldControlName);
            currentPageName = EditorGUILayout.TextField(currentPageName);

            // 跟踪焦点状态
            if (GUI.GetNameOfFocusedControl() == nameFieldControlName)
            {
                isNameFieldFocused = true;
            }
            else if (EditorGUI.EndChangeCheck())
            {
                // 处理名称变更
                if (isNameFieldFocused)
                {
                    isNameFieldFocused = false;

                    if (string.IsNullOrEmpty(currentPageName))
                    {
                        EditorUtility.DisplayDialog("错误", "页面名称不能为空", "确定");
                        UpdateCurrentPageName(); // 恢复原名称
                    }
                    else if (IsPageNameExists(currentPageName) && currentPageName != controller.pages[controller.currentPageIndex].pageName)
                    {
                        EditorUtility.DisplayDialog("错误", "页面名称已存在", "确定");
                        UpdateCurrentPageName(); // 恢复原名称
                    }
                    else
                    {
                        controller.pages[controller.currentPageIndex].pageName = currentPageName;
                        EditorUtility.SetDirty(controller);
                    }

                    // 失去焦点
                    GUI.FocusControl(null);
                }
            }

            // 失去焦点处理
            if (Event.current.type == EventType.MouseDown &&
                !GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) &&
                isNameFieldFocused)
            {
                isNameFieldFocused = false;
                GUI.FocusControl(null);

                // 验证名称
                if (string.IsNullOrEmpty(currentPageName))
                {
                    EditorUtility.DisplayDialog("错误", "页面名称不能为空", "确定");
                    UpdateCurrentPageName(); // 恢复原名称
                }
                else if (IsPageNameExists(currentPageName) && currentPageName != controller.pages[controller.currentPageIndex].pageName)
                {
                    EditorUtility.DisplayDialog("错误", "页面名称已存在", "确定");
                    UpdateCurrentPageName(); // 恢复原名称
                }
                else
                {
                    controller.pages[controller.currentPageIndex].pageName = currentPageName;
                    EditorUtility.SetDirty(controller);
                }
            }

            EditorGUILayout.EndHorizontal();

            // 创建按钮网格（每行显示5个按钮）
            float buttonHeight = 22f;
            float buttonWidth = (EditorGUIUtility.currentViewWidth - 40f) / 5f; // 每个5个按钮
            int buttonsPerRow = Mathf.FloorToInt(EditorGUIUtility.currentViewWidth / buttonWidth);
            int rows = Mathf.CeilToInt((float)controller.pages.Count / buttonsPerRow);
            float totalHeight = rows * buttonHeight + 10f;

            // 使用滚动视图
            pagesScrollPos = EditorGUILayout.BeginScrollView(pagesScrollPos, GUILayout.Height(totalHeight));

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < controller.pages.Count; i++)
            {
                // 计算每行按钮的开始位置
                if (i > 0 && i % buttonsPerRow == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                // 为当前选择的页面使用不同的样式
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                if (i == controller.currentPageIndex)
                {
                    buttonStyle.normal.textColor = Color.white;
                    buttonStyle.normal.background = CreateColoredTexture(60, 60, 60);
                }

                if (GUILayout.Button(controller.pages[i].pageName, buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                {
                    controller.currentPageIndex = i;
                    controller.ApplyCurrentPage();
                    UpdateCurrentPageName();
                    EditorUtility.SetDirty(controller);

                    // 失去焦点
                    GUI.FocusControl(null);
                    isNameFieldFocused = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Page"))
            {
                controller.ApplyCurrentPage();
            }
            if (GUILayout.Button("Record Page"))
            {
                controller.RecordCurrentPage();
                EditorUtility.SetDirty(controller);
            }
            if (GUILayout.Button("Remove Page") && controller.pages.Count > 1)
            {
                controller.pages.RemoveAt(controller.currentPageIndex);
                controller.currentPageIndex = Mathf.Clamp(controller.currentPageIndex, 0, controller.pages.Count - 1);
                UpdateCurrentPageName();
                EditorUtility.SetDirty(controller);
                controller.ApplyCurrentPage();

                // 失去焦点
                GUI.FocusControl(null);
                isNameFieldFocused = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        // 当前页面的控制元素
        if (controller.pages.Count > 0)
        {
            var currentPage = controller.pages[controller.currentPageIndex];

            EditorGUILayout.LabelField("Controlled Elements:", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));

            for (int i = 0; i < currentPage.controlledElements.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                currentPage.controlledElements[i].target = (GameObject)EditorGUILayout.ObjectField(
                    "Target",
                    currentPage.controlledElements[i].target,
                    typeof(GameObject),
                    true);

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    currentPage.controlledElements.RemoveAt(i);
                    EditorUtility.SetDirty(controller);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (currentPage.controlledElements[i].target != null)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Get Current"))
                    {
                        controller.RecordCurrentPage();
                        EditorUtility.SetDirty(controller);
                    }
                    if (GUILayout.Button("Apply"))
                    {
                        controller.ApplyCurrentPage();
                        EditorUtility.SetDirty(controller);
                    }
                    EditorGUILayout.EndHorizontal();

                    // 记录设置
                    EditorGUILayout.LabelField("Record Settings:", EditorStyles.boldLabel);
                    // 激活状态
                    currentPage.controlledElements[i].recordActiveState = EditorGUILayout.Toggle(
                        new GUIContent("Active State", "记录GameObject的激活状态"),
                        currentPage.controlledElements[i].recordActiveState);

                    if (currentPage.controlledElements[i].recordActiveState)
                    {
                        EditorGUI.indentLevel++;
                        currentPage.controlledElements[i].isActive = EditorGUILayout.Toggle(
                            new GUIContent("Is Active", "GameObject的激活状态"),
                            currentPage.controlledElements[i].isActive);
                        EditorGUI.indentLevel--;
                    }
                    // Transform设置
                    currentPage.controlledElements[i].recordTransform = EditorGUILayout.Toggle(
                        new GUIContent("Transform", "记录Transform组件的属性"),
                        currentPage.controlledElements[i].recordTransform);

                    if (currentPage.controlledElements[i].recordTransform)
                    {
                        EditorGUI.indentLevel++;

                        EditorGUILayout.BeginHorizontal();
                        currentPage.controlledElements[i].recordPosition = EditorGUILayout.Toggle(
                            new GUIContent("Position", "记录位置"),
                            currentPage.controlledElements[i].recordPosition);

                        currentPage.controlledElements[i].recordRotation = EditorGUILayout.Toggle(
                            new GUIContent("Rotation", "记录旋转"),
                            currentPage.controlledElements[i].recordRotation);

                        currentPage.controlledElements[i].recordScale = EditorGUILayout.Toggle(
                            new GUIContent("Scale", "记录缩放"),
                            currentPage.controlledElements[i].recordScale);
                        EditorGUILayout.EndHorizontal();

                        if (currentPage.controlledElements[i].recordPosition)
                            currentPage.controlledElements[i].position = EditorGUILayout.Vector3Field("Position", currentPage.controlledElements[i].position);

                        if (currentPage.controlledElements[i].recordRotation)
                            currentPage.controlledElements[i].rotation = EditorGUILayout.Vector3Field("Rotation", currentPage.controlledElements[i].rotation);

                        if (currentPage.controlledElements[i].recordScale)
                            currentPage.controlledElements[i].scale = EditorGUILayout.Vector3Field("Scale", currentPage.controlledElements[i].scale);

                        EditorGUI.indentLevel--;
                    }

                    // RectTransform设置（修改）
                    currentPage.controlledElements[i].recordRectTransform = EditorGUILayout.Toggle(
                        new GUIContent("RectTransform", "记录RectTransform组件的属性(UGUI专用)"),
                        currentPage.controlledElements[i].recordRectTransform);

                    if (currentPage.controlledElements[i].recordRectTransform)
                    {
                        EditorGUI.indentLevel++;

                        EditorGUILayout.LabelField("RectTransform Properties:");

                        EditorGUILayout.BeginHorizontal();
                        currentPage.controlledElements[i].recordAnchoredPosition = EditorGUILayout.Toggle(
                            new GUIContent("Anchored Pos", "记录锚点位置"),
                            currentPage.controlledElements[i].recordAnchoredPosition);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        currentPage.controlledElements[i].recordSizeDelta = EditorGUILayout.Toggle(
                            new GUIContent("Size Delta", "记录尺寸增量"),
                            currentPage.controlledElements[i].recordSizeDelta);
                        EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                        if (currentPage.controlledElements[i].recordSizeDelta)
                            currentPage.controlledElements[i].sizeDelta = EditorGUILayout.Vector2Field("Size Delta", currentPage.controlledElements[i].sizeDelta);


                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        currentPage.controlledElements[i].recordPivot = EditorGUILayout.Toggle(
                            new GUIContent("Pivot", "记录轴心点"),
                            currentPage.controlledElements[i].recordPivot);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        currentPage.controlledElements[i].recordAnchoredPosition3D = EditorGUILayout.Toggle(
                            new GUIContent("Anchored Pos 3D", "记录3D锚点位置"),
                            currentPage.controlledElements[i].recordAnchoredPosition3D);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        currentPage.controlledElements[i].recordAnchorMinMax = EditorGUILayout.Toggle(
                            new GUIContent("Anchor Min/Max", "记录锚点范围"),
                            currentPage.controlledElements[i].recordAnchorMinMax);
                        EditorGUILayout.EndHorizontal();

                        if (currentPage.controlledElements[i].recordAnchoredPosition)
                            currentPage.controlledElements[i].anchoredPosition = EditorGUILayout.Vector2Field("Anchored Position", currentPage.controlledElements[i].anchoredPosition);



                        if (currentPage.controlledElements[i].recordPivot)
                            currentPage.controlledElements[i].pivot = EditorGUILayout.Vector2Field("Pivot", currentPage.controlledElements[i].pivot);

                        if (currentPage.controlledElements[i].recordAnchoredPosition3D)
                            currentPage.controlledElements[i].anchoredPosition3D = EditorGUILayout.Vector4Field("Anchored Position 3D", currentPage.controlledElements[i].anchoredPosition3D);

                        if (currentPage.controlledElements[i].recordAnchorMinMax)
                        {
                            EditorGUILayout.BeginHorizontal();
                            currentPage.controlledElements[i].anchorMin = EditorGUILayout.Vector2Field("Anchor Min", currentPage.controlledElements[i].anchorMin);
                            currentPage.controlledElements[i].anchorMax = EditorGUILayout.Vector2Field("Anchor Max", currentPage.controlledElements[i].anchorMax);
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUI.indentLevel--;
                    }

                    // SpriteRenderer设置
                    var hasSpriteRenderer = currentPage.controlledElements[i].target.GetComponent<SpriteRenderer>() != null;
                    currentPage.controlledElements[i].recordSpriteRenderer = EditorGUILayout.Toggle(
                        new GUIContent("SpriteRenderer", "记录SpriteRenderer组件的属性"),
                        currentPage.controlledElements[i].recordSpriteRenderer && hasSpriteRenderer);

                    if (currentPage.controlledElements[i].recordSpriteRenderer && hasSpriteRenderer)
                    {
                        EditorGUI.indentLevel++;

                        // 显示Sprite名称
                        EditorGUILayout.LabelField("Sprite Name:", currentPage.controlledElements[i].spriteName);

                        // 图集选择
                        int selectedAtlasIndex = atlasNames.IndexOf(currentPage.controlledElements[i].atlasName);
                        if (selectedAtlasIndex < 0) selectedAtlasIndex = 0;

                        selectedAtlasIndex = EditorGUILayout.Popup("Atlas", selectedAtlasIndex, atlasNames.ToArray());
                        currentPage.controlledElements[i].atlasName = atlasNames[selectedAtlasIndex];

                        // Sprite选择
                        currentPage.controlledElements[i].editorSprite = (Sprite)EditorGUILayout.ObjectField(
                            "Sprite",
                            currentPage.controlledElements[i].editorSprite,
                            typeof(Sprite),
                            false);

                        currentPage.controlledElements[i].spriteColor = EditorGUILayout.ColorField("Color", currentPage.controlledElements[i].spriteColor);

                        EditorGUI.indentLevel--;
                    }

                    // Image设置
                    var hasImage = currentPage.controlledElements[i].target.GetComponent<Image>() != null;
                    currentPage.controlledElements[i].recordImage = EditorGUILayout.Toggle(
                        new GUIContent("Image", "记录Image组件的属性"),
                        currentPage.controlledElements[i].recordImage && hasImage);

                    if (currentPage.controlledElements[i].recordImage && hasImage)
                    {
                        EditorGUI.indentLevel++;

                        // 显示Image Sprite名称
                        EditorGUILayout.LabelField("Image Sprite Name:", currentPage.controlledElements[i].imageSpriteName);

                        // 图集选择
                        int selectedAtlasIndex = atlasNames.IndexOf(currentPage.controlledElements[i].imageAtlasName);
                        if (selectedAtlasIndex < 0) selectedAtlasIndex = 0;

                        selectedAtlasIndex = EditorGUILayout.Popup("Image Atlas", selectedAtlasIndex, atlasNames.ToArray());
                        currentPage.controlledElements[i].imageAtlasName = atlasNames[selectedAtlasIndex];

                        // Image Sprite选择
                        currentPage.controlledElements[i].editorImageSprite = (Sprite)EditorGUILayout.ObjectField(
                            "Image Sprite",
                            currentPage.controlledElements[i].editorImageSprite,
                            typeof(Sprite),
                            false);

                        currentPage.controlledElements[i].imageColor = EditorGUILayout.ColorField("Image Color", currentPage.controlledElements[i].imageColor);

                        EditorGUI.indentLevel--;
                    }
                    
                    currentPage.controlledElements[i].recordImageMaterial = EditorGUILayout.Toggle(
                        new GUIContent("ImageMaterial", "记录Image组件的材质球属性"),
                        currentPage.controlledElements[i].recordImageMaterial && hasImage);
                    if (currentPage.controlledElements[i].recordImageMaterial && hasImage)
                    {
                        EditorGUI.indentLevel++;
                        
                        // Image Material选择
                        currentPage.controlledElements[i].imageMaterial = (Material)EditorGUILayout.ObjectField(
                            "Image Material",
                            currentPage.controlledElements[i].imageMaterial,
                            typeof(Material),
                            false);

                        EditorGUI.indentLevel--;
                    }

                    // TextMeshPro设置
                    var hasTextMeshPro = currentPage.controlledElements[i].target.GetComponent<TMP_Text>() != null;
                    
                    // TextMeshPro 主开关
                    currentPage.controlledElements[i].recordTextMeshPro = EditorGUILayout.Toggle(
                        new GUIContent("TextMeshPro", "记录TextMeshPro组件的属性"),
                        currentPage.controlledElements[i].recordTextMeshPro && hasTextMeshPro);
                    
                    // 只有当主开关打开时才显示子选项
                    if (currentPage.controlledElements[i].recordTextMeshPro && hasTextMeshPro)
                    {
                        EditorGUI.indentLevel++;
                        
                        // 文本内容控制
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        currentPage.controlledElements[i].recordTextMeshProText = EditorGUILayout.Toggle(
                            new GUIContent("记录文本内容", "记录文本内容"),
                            currentPage.controlledElements[i].recordTextMeshProText);
                        
                        if (currentPage.controlledElements[i].recordTextMeshProText)
                        {
                            EditorGUI.indentLevel++;
                            currentPage.controlledElements[i].text = EditorGUILayout.TextField("Text", currentPage.controlledElements[i].text);
                            EditorGUI.indentLevel--;
                        }
                        EditorGUILayout.EndVertical();
                        
                        // 文本样式控制
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        currentPage.controlledElements[i].recordTextMeshProStyle = EditorGUILayout.Toggle(
                            new GUIContent("记录文本样式", "记录文本样式（字体大小、颜色、样式）"),
                            currentPage.controlledElements[i].recordTextMeshProStyle);
                        
                        if (currentPage.controlledElements[i].recordTextMeshProStyle)
                        {
                            EditorGUI.indentLevel++;
                            currentPage.controlledElements[i].fontSize = EditorGUILayout.FloatField("Font Size", currentPage.controlledElements[i].fontSize);
                            currentPage.controlledElements[i].textColor = EditorGUILayout.ColorField("Text Color", currentPage.controlledElements[i].textColor);
                            
                            // 字体样式选择
                            currentPage.controlledElements[i].fontStyle = (FontStyles)EditorGUILayout.EnumFlagsField(
                                "Font Style", currentPage.controlledElements[i].fontStyle);
                            EditorGUI.indentLevel--;
                        }
                        EditorGUILayout.EndVertical();
                        
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();

            // 添加新元素按钮
            if (GUILayout.Button("Add New Element"))
            {
                currentPage.controlledElements.Add(new UIPageController.UIControlledElement());
                EditorUtility.SetDirty(controller);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // 检查页面名称是否存在
    private bool IsPageNameExists(string name)
    {
        foreach (var page in controller.pages)
        {
            if (page.pageName == name)
                return true;
        }
        return false;
    }

    // 生成唯一页面名称
    private string GenerateUniquePageName(string proposedName)
    {
        // 如果名称为空或已存在，自动生成新名称
        if (string.IsNullOrEmpty(proposedName) || IsPageNameExists(proposedName))
        {
            int currentMax = 0;

            // 获取现有页面中的最大编号
            foreach (var page in controller.pages)
            {
                if (page.pageName.StartsWith("Page"))
                {
                    string numberPart = page.pageName.Substring(4);
                    if (int.TryParse(numberPart, out int num))
                    {
                        currentMax = Mathf.Max(currentMax, num);
                    }
                }
            }

            return "Page" + (currentMax + 1);
        }

        return proposedName;
    }

    // 创建有色纹理用于按钮背景
    private Texture2D CreateColoredTexture(int r, int g, int b)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, new Color(r / 255f, g / 255f, b / 255f));
        texture.Apply();
        return texture;
    }
}
#endif