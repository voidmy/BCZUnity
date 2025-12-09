
using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.U2D;

public static class AtlasMgr
{
    private static readonly ResourceLoader ResLoader=ResourceLoader.Allocate();
    private static readonly Dictionary<string, SpriteAtlas> LoadedAtlas = new Dictionary<string, SpriteAtlas>(16);
    private static void RequestAtlas(string atlasName, Action<SpriteAtlas> callback)
    {
        Debug.Log($"请求图集:{atlasName}");
        string path = GetAtlasPath(atlasName);
        SpriteAtlas atlas = LoadSpriteAtlas(path);
        callback?.Invoke(atlas);
    }
    
    public static SpriteAtlas LoadSpriteAtlas(string path)
    {
        if (LoadedAtlas.TryGetValue(path, out var value))
        {
            return value;
        }
        else
        {
            SpriteAtlas atlas = ResLoader.LoadAssetSync<SpriteAtlas>(path);
            LoadedAtlas.Add(path, atlas);
            Debug.Log($"加载图集,路径:{path}");
            return atlas;
        }
    }
    
    private static readonly Dictionary<string, string> AtlasMap = new Dictionary<string, string>()
    {
        {"common_atlas", "aaaaa"},
    };
    public static string GetAtlasPath(string tag)
    {
        if (AtlasMap.TryGetValue(tag, out var path))
            return path;
        Debug.LogError($"未找到图集配置: {tag}");
        return string.Empty;
    }
}
