using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameConfig : MonoBehaviour
{
    public static string GetEntryMetaUrl()
    {
#if UNITY_EDITOR
        var url = GameSetting.Instance.EntryMetaUrl.TrimEnd('/');
        //var url = DataManager.Instance.Local.Get(PlayerLocalData.Key.Dev_EntryMetaUrl, string.Format("{0}/{1}/EntryP{2}.json", GameSetting.Instance.EntryMetaUrl.TrimEnd('/'), platform, Version.Protocol));
#else
        var url = Build.EntryMetaUrl.TrimEnd('/');
        //var url = DataManager.Instance.Local.Get(PlayerLocalData.Key.Dev_EntryMetaUrl, string.Format("{0}/{1}/EntryP{2}.json", Build.EntryMetaUrl.TrimEnd('/'), platform, Version.Protocol));
#endif
        return url;
    }
    public static string GetLanguage()
    {
        return "";//Locale.Current.Lang;
    }
    public static bool IsPRD()
    {
        return GameSetting.Instance.IsPrd();
    }
}
