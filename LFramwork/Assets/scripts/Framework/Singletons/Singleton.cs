using Framework;
using QFramework;

public class Singleton<T> where T : class,ITSingleton 
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = SingletonCreator.CreateSingleton<T>();
            }
            return instance;
        }
    }

    protected Singleton() { }
}