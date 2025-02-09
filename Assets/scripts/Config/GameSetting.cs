using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[CreateAssetMenu(menuName = "CreateGameSetting", fileName = "~GameSetting")]
public class GameSetting : ScriptableObject
{
    private Dictionary<string, string> servers = new Dictionary<string, string> {
        { "palette_stg","https://palette-stg.the-land.xyz:8990/Admin/version/query" },
        { "cd_stg","https://cd-stg.the-land.xyz:10900/Admin/version/query" },
        { "prd", "https://prod.theland.game:8900/Admin/version/query"},
        { "apl", "https://apl.the-land.xyz:18900/Admin/version/query"},
        { "cd_dev2","http://10.10.11.136/Admin/version/query" },
        { "cd_dev1","http://10.10.11.141/Admin/version/query" },
        { "pstg","https://pstg.the-land.xyz:7990/Admin/version/query" },
         { "stg_qa","https://pstg.the-land.xyz:7990/Admin/version/query" },
    };
    static GameSetting _instance;
    public bool Tutorial = true;
    public bool IsApl = true;
    public bool IsGM = false;
    public bool GuestLogin = false;
    [ContextMenuItem("palette_stg", "SetIPPaletteStg")]
    [ContextMenuItem("cd_stg", "SetIPCDStg")]
    [ContextMenuItem("cd_dev", "SetIPDev")]
    [ContextMenuItem("pstg", "SetIPPitayaStg")]
    [ContextMenuItem("prd", "SetIPPrd")]
    [ContextMenuItem("apl", "SetIPApl")]
    //[HideInInspector]
    public string EntryMetaUrl = "";
    public static GameSetting Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = LoadAsset();
            }
            if (_instance == null)
            {
                _instance = new GameSetting()
                {
                    EntryMetaUrl = "" //GameConfig.Build.EntryMetaUrl,
                };

            }
            return _instance;
        }
    }
    static GameSetting LoadAsset()
    {
        _instance = Resources.Load<GameSetting>("~GameSetting");
        if (_instance == null)
        {
            _instance =Resources.Load<GameSetting>("GameSetting");
        }
        return _instance;
    }
    void SetIPPaletteStg()
    {
        EntryMetaUrl = servers["palette_stg"];
        Save();
    }

    void SetIPCDStg()
    {
        EntryMetaUrl = servers["cd_stg"];
        Save();
    }

    void SetIPDev()
    {
        EntryMetaUrl = servers["cd_dev"];
        Save();
    }

    void SetIPPitayaStg()
    {
        EntryMetaUrl = servers["pstg"];
        Save();
    }

    void SetIPPrd()
    {
        EntryMetaUrl = servers["prd"];
        Save();
    }

    void SetIPApl()
    {
        EntryMetaUrl = servers["apl"];
        Save();
    }

    void SetIPStgQA()
    {
        EntryMetaUrl = servers["stg_qa"];
        Save();
    }

    public bool IsPrd()
    {
        if (GuestLogin)
        {
            var saveServer = PlayerPrefs.GetString("Cumstom_Server_Address", string.Empty);
            if (string.IsNullOrEmpty(saveServer))
            {
                saveServer ="";
            }
            return saveServer == servers["prd"];
        }
        return GameConfig.GetEntryMetaUrl() == servers["prd"];
    }
    public void Save()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
#endif
    }

}
