using System;
using System.IO;
using UnityEngine;

public class Module<T> : Singleton<T> where T : class, new()
{
    protected string dataPath; // ���ݴ洢·��

    // �������
    protected void ClearData()
    {
      //
    }
}