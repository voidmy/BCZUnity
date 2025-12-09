using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.U2D;
using System.Linq;
using QFramework;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.U2D;
#endif

[ExecuteInEditMode]
public class UIPageController : MonoBehaviour
{
    [System.Serializable]
    public class UIPage
    {
        public string pageName = "Page1";
        public List<UIControlledElement> controlledElements = new List<UIControlledElement>();
    }

    [System.Serializable]
    public class UIControlledElement
    {
        public GameObject target;
        // 记录激活状态
        public bool recordActiveState = false; 
        public bool isActive = false;        
        // 记录Transform属性
        public bool recordTransform = false;
        public bool recordPosition = false;
        public bool recordRotation = false;
        public bool recordScale = false;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one;
        
        // 记录RectTransform属性（修改）
        public bool recordRectTransform = false; // 记录RectTransform开关
        public bool recordAnchoredPosition = false; // 记录锚点位置
        public bool recordSizeDelta = false;        // 记录尺寸增量
        public bool recordPivot = false;            // 记录轴心点
        public bool recordAnchoredPosition3D = false; // 记录3D锚点位置
        public bool recordAnchorMinMax = false;     // 记录锚点范围
        public Vector2 anchoredPosition;         // 锚点位置
        public Vector2 sizeDelta;                // 尺寸增量
        public Vector2 pivot;                    // 轴心点
        public Vector4 anchoredPosition3D;       // 3D锚点位置
        public Vector2 anchorMin;                // 最小锚点
        public Vector2 anchorMax;                // 最大锚点
        
        public bool recordSpriteRenderer = false;
        public string spriteName;
        public string atlasName;
        public Color spriteColor = Color.white;

        public bool recordImage = false;
        public string imageSpriteName;
        public string imageAtlasName;
        public Color imageColor = Color.white;

        // TextMeshPro 属性
        public bool recordTextMeshPro = false;     // 主开关
        public bool recordTextMeshProText = true;  // 控制文本内容（默认开启）
        public string text;
        
        public bool recordTextMeshProStyle = true; // 控制文本样式（默认开启）
        public float fontSize;
        public Color textColor = Color.white;
        public FontStyles fontStyle;

        // Image的材质球属性
        public bool recordImageMaterial = false;
        public Material imageMaterial;

        // 编辑器专用字段
#if UNITY_EDITOR
        public Sprite editorSprite;
        public Sprite editorImageSprite;
#endif
    }
    
    public ResourceLoader resourceLoader;
    public List<UIPage> pages = new List<UIPage>();
    public int currentPageIndex = 0;

    private void OnEnable()
    {
        if (pages.Count == 0)
        {
            pages.Add(new UIPage());
        }
    }

    public void SetCurIndexAndChange(int index)
    {
        currentPageIndex = index;
        ApplyCurrentPage();
    }

    public void SetCurPageByName(string name)
    {
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i].pageName == name)
            {
                currentPageIndex = i;
                ApplyCurrentPage();
                return;
            }
        }
    }

    public void ApplyCurrentPage()
    {
        if (currentPageIndex >= 0 && currentPageIndex < pages.Count)
        {
            var currentPage = pages[currentPageIndex];
            foreach (var element in currentPage.controlledElements)
            {
                if (element.target != null)
                {
                    // 应用激活状态
                    if (element.recordActiveState)
                        element.target.SetActive(element.isActive);
                    // 应用Transform属性
                    if (element.recordTransform)
                    {
                        if (element.recordPosition) element.target.transform.localPosition = element.position;
                        if (element.recordRotation) element.target.transform.localEulerAngles = element.rotation;
                        if (element.recordScale) element.target.transform.localScale = element.scale;
                    }
                    
                    // 应用RectTransform属性（修改）
                    if (element.recordRectTransform)
                    {
                        var rectTransform = element.target.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            if(element.recordAnchoredPosition)
                                rectTransform.anchoredPosition = element.anchoredPosition;
                            if(element.recordSizeDelta)
                                rectTransform.sizeDelta = element.sizeDelta;
                            if(element.recordPivot)
                                rectTransform.pivot = element.pivot;
                            if(element.recordAnchoredPosition3D)
                                rectTransform.anchoredPosition3D = element.anchoredPosition3D;
                            if(element.recordAnchorMinMax)
                            {
                                rectTransform.anchorMin = element.anchorMin;
                                rectTransform.anchorMax = element.anchorMax;
                            }
                        }
                    }

                    // 应用SpriteRenderer属性
                    if (element.recordSpriteRenderer)
                    {
                        var spriteRenderer = element.target.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null)
                        {
#if UNITY_EDITOR
                            if (Application.isPlaying)
                            {
                                SetElementSprite(element, spriteRenderer);
                            }
                            else
                            {
                                spriteRenderer.sprite = element.editorSprite;
                            }
#else
                            SetElementSprite(element, spriteRenderer);
#endif
                            spriteRenderer.color = element.spriteColor;
                        }
                    }

                    // 应用Image属性
                    if (element.recordImage)
                    {
                        var image = element.target.GetComponent<Image>();
                        if (image != null)
                        {
#if UNITY_EDITOR
                            if (Application.isPlaying)
                            {
                                SetElementSpriteImage(element, image);
                            }
                            else
                            {
                                image.sprite = element.editorImageSprite;
                            }
#else
                            SetElementSpriteImage(element, image);
#endif
                            image.color = element.imageColor;
                        }
                    }
                    
                    // 应用Image属性
                    if (element.recordImageMaterial)
                    {
                        var image = element.target.GetComponent<Image>();
                        if (image != null)
                        {
                            image.material = element.imageMaterial;
                        }
                    }

                    // 应用TextMeshPro属性
                    if (element.recordTextMeshPro && element.target.GetComponent<TMP_Text>() != null)
                    {
                        var textMeshPro = element.target.GetComponent<TMP_Text>();
                        if (textMeshPro != null)
                        {
                            // 应用文本内容
                            if (element.recordTextMeshProText)
                            {
                                textMeshPro.text = element.text;
                            }
                            
                            // 应用样式
                            if (element.recordTextMeshProStyle)
                            {
                                textMeshPro.fontSize = element.fontSize;
                                textMeshPro.color = element.textColor;
                                textMeshPro.fontStyle = element.fontStyle;
                            }
                        }
                    }
                }
            }
        }
    }

    private void SetElementSprite(UIControlledElement element, SpriteRenderer spriteRenderer)
    {
        if (!string.IsNullOrEmpty(element.spriteName) && !string.IsNullOrEmpty(element.atlasName))
        {
            var sprite = LoadSpriteFromAtlas(element.atlasName, element.spriteName);
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }
    }

    private void SetElementSpriteImage(UIControlledElement element, Image image)
    {
        if (!string.IsNullOrEmpty(element.imageSpriteName) && !string.IsNullOrEmpty(element.imageAtlasName))
        {
            var sprite = LoadSpriteFromAtlas(element.imageAtlasName, element.imageSpriteName);
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }
    }

    private Sprite LoadSpriteFromAtlas(string atlasName, string spriteName)
    {
        if (resourceLoader != null)
        {
            if (atlasName == "None")
            {
                return resourceLoader.LoadAssetSync<Sprite>(spriteName);
            }
            else
            {
                var path = AtlasMgr.GetAtlasPath(atlasName);
                var atlas = resourceLoader.LoadAssetSync<SpriteAtlas>(path);
                if (atlas != null)
                {
                    return atlas.GetSprite(spriteName);
                }
            }
        }
        return null;
    }
#if UNITY_EDITOR
    public void RecordCurrentPage()
    {
        if (currentPageIndex >= 0 && currentPageIndex < pages.Count)
        {
            var currentPage = pages[currentPageIndex];
            foreach (var element in currentPage.controlledElements)
            {
                if (element.target != null)
                {
                    // 记录激活状态
                    if (element.recordActiveState)
                        element.isActive = element.target.activeSelf;

                    // 记录Transform属性
                    if (element.recordTransform)
                    {
                        element.position = element.target.transform.localPosition;
                        element.rotation = element.target.transform.localEulerAngles;
                        element.scale = element.target.transform.localScale;
                    }
                    
                    // 记录RectTransform属性（修改）
                    if (element.recordRectTransform && element.target.GetComponent<RectTransform>() != null)
                    {
                        var rectTransform = element.target.GetComponent<RectTransform>();
                        if(element.recordAnchoredPosition)
                            element.anchoredPosition = rectTransform.anchoredPosition;
                        if(element.recordSizeDelta)
                            element.sizeDelta = rectTransform.sizeDelta;
                        if(element.recordPivot)
                            element.pivot = rectTransform.pivot;
                        if(element.recordAnchoredPosition3D)
                            element.anchoredPosition3D = rectTransform.anchoredPosition3D;
                        if(element.recordAnchorMinMax)
                        {
                            element.anchorMin = rectTransform.anchorMin;
                            element.anchorMax = rectTransform.anchorMax;
                        }
                    }

                    // 记录SpriteRenderer属性
                    if (element.recordSpriteRenderer && element.target.GetComponent<SpriteRenderer>() != null)
                    {
                        var spriteRenderer = element.target.GetComponent<SpriteRenderer>();
                        element.spriteColor = spriteRenderer.color;

#if UNITY_EDITOR
                        element.editorSprite = spriteRenderer.sprite;
                        element.atlasName = GetAtlasNameForSprite(spriteRenderer.sprite);
#endif
                        if (element.atlasName != "None")
                        {
                            element.spriteName = (spriteRenderer.sprite != null) ? spriteRenderer.sprite.name : "";
                        }
                        else
                        {
                            element.spriteName = GetSpritePath(spriteRenderer.sprite);
                        }
                    }

                    // 记录Image属性
                    if (element.recordImage && element.target.GetComponent<Image>() != null)
                    {
                        var image = element.target.GetComponent<Image>();
                        element.imageColor = image.color;

#if UNITY_EDITOR
                        element.editorImageSprite = image.sprite;
                        element.imageAtlasName = GetAtlasNameForSprite(image.sprite);
#endif
                        if (element.imageAtlasName == "None")
                        {
                            element.imageSpriteName = GetSpritePath(image.sprite);
                        }
                        else
                        {
                            element.imageSpriteName = (image.sprite != null) ? image.sprite.name : "";
                        }
                    }

                    if (element.recordImageMaterial && element.target.GetComponent<Image>() != null)
                    {
                        var image = element.target.GetComponent<Image>();
                        element.imageMaterial = image.material;
                    }

                    // 记录TextMeshPro属性
                    if (element.recordTextMeshPro && element.target.GetComponent<TMP_Text>() != null)
                    {
                        var textMeshPro = element.target.GetComponent<TMP_Text>();
                        
                        // 记录文本内容
                        if (element.recordTextMeshProText)
                        {
                            element.text = textMeshPro.text;
                        }
                        
                        // 记录样式
                        if (element.recordTextMeshProStyle)
                        {
                            element.fontSize = textMeshPro.fontSize;
                            element.textColor = textMeshPro.color;
                            element.fontStyle = textMeshPro.fontStyle;
                        }
                    }
                }
            }
        }
    }


    public string GetAtlasNameForSprite(Sprite sprite)
    {
        if (sprite == null) return "None";

        // 检查精灵是否属于某个图集
        var spriteAtlases = AssetDatabase.FindAssets("t:SpriteAtlas").Select(guid =>
            AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();

        foreach (SpriteAtlas atlas in spriteAtlases)
        {
            if (atlas != null && atlas.CanBindTo(sprite))
            {
                return atlas.name;
            }
        }
        return "None";
    }

    /// <summary>
    /// 获取精灵的路径
    /// </summary>
    /// <param name="sprite">要获取路径的精灵</param>
    /// <returns>精灵的路径，如果未找到则返回空字符串</returns>
    public static string GetSpritePath(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError("Sprite is null.");
            return string.Empty;
        }

        // 获取精灵的纹理
        Texture2D texture = sprite.texture;
        if (texture == null)
        {
            Debug.LogError("Sprite texture is null.");
            return string.Empty;
        }

        // 尝试从纹理获取路径
        string texturePath = AssetDatabase.GetAssetPath(texture);
        if (!string.IsNullOrEmpty(texturePath))
        {
            return texturePath;
        }
        string[] guids = AssetDatabase.FindAssets(sprite.name);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == sprite)
            {
                return path;
            }
        }
        return string.Empty;
    }
#endif
}