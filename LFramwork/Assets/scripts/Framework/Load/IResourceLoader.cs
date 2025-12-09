using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

    public interface IResourceLoader
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源的类型</typeparam>
        /// <param name="assetPath">资源的路径</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        /// <returns></returns>
        T LoadAssetSync<T>(string assetPath, string packageName) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源（回调）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetPath">资源的路径</param>
        /// <param name="callback">加载完成回调</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        void LoadAssetAsync<T>(string assetPath, Action<bool, T> callback, string packageName) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源（Task）
        /// </summary>
        /// <typeparam name="T">资源的类型</typeparam>
        /// <param name="assetPath">资源的路径</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        /// <returns></returns>
        UniTask<T> LoadAssetAsyncTask<T>(string assetPath, string packageName) where T : UnityEngine.Object;

        /// <summary>
        /// 同步克隆GameObject对象
        /// </summary>
        /// <param name="path">资源的路径</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        /// <returns></returns>
        GameObject Clone(string assetPath, string packageName);
        GameObject Clone(string assetPath, Transform parent, string packageName);
        GameObject Clone(string assetPath, Transform parent, Vector3 position, Quaternion rotation, string packageName);

        /// <summary>
        /// 异步克隆GameObject对象（回调）
        /// </summary>
        /// <param name="path">资源的路径</param>
        /// <param name="callback">克隆完成回调</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        void CloneAsync(string assetPath, Action<bool, GameObject> callback, string packageName);
        void CloneAsync(string assetPath, Transform parent, Action<bool, GameObject> callback, string packageName);
        void CloneAsync(string assetPath, Transform parent, Vector3 position, Quaternion rotation, Action<GameObject> callback, string packageName);

        /// <summary>
        /// 异步克隆GameObject对象（Task）
        /// </summary>
        /// <param name="path">资源的路径</param>
        /// <param name="packageName">指定资源包的名称。不传使用默认资源包</param>
        /// <returns></returns>
        UniTask<GameObject> CloneAsyncTask(string assetPath, string packageName);
        UniTask<GameObject> CloneAsyncTask(string assetPath, Transform parent, string packageName);
        UniTask<GameObject> CloneAsyncTask(string assetPath, Transform parent, Vector3 position, Quaternion rotation, string packageName);

        /// <summary>
        /// 同步加载场景
        /// </summary>
        /// <param name="scenePath">场景路径</param>
        /// <param name="packageName"></param>
        /// <param name="sceneMode"></param>
        /// <param name="physicsMode"></param>
        /// <returns></returns>
        object LoadSceneSync(string scenePath, string packageName, LoadSceneMode sceneMode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None);
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="scenePath">场景路径</param>
        /// <param name="packageName"></param>
        /// <param name="sceneMode"></param>
        /// <param name="physicsMode"></param>
        /// <param name="suspendLoad"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        object LoadSceneAsync(string scenePath, string packageName, LoadSceneMode sceneMode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, bool suspendLoad = false, uint priority = 100u);

        bool DestroyGameObject(GameObject go);
    }
