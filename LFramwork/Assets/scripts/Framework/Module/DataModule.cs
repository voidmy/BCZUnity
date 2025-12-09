



using Framework;

public class Module<T> : Singleton<T> where T : class ,ITSingleton
{
    protected string dataPath; // ���ݴ洢·��

    // �������
    protected void ClearData()
    {
      //
    }
}