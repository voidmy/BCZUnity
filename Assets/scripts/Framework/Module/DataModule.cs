using System;
using System.IO;
using UnityEngine;

public class Module<T> : Singleton<T> where T : class, new()
{
    protected string dataPath; // 数据存储路径

    // 清除数据
    protected void ClearData()
    {
      
    }
}