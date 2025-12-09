using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace QFramework
{
    public class ResourceLoader : IResourceLoader, IPoolType, IPoolable
    {
        private List<object> assetList = new List<object>();

        public static ResourceLoader Allocate()
        {
            var loader = SafeObjectPool<ResourceLoader>.Instance.Allocate();
            return loader;
        }


        #region 回收
        public void Recycle2Cache()
        {
            SafeObjectPool<ResourceLoader>.Instance.Recycle(this);
        }

        bool IPoolable.IsRecycled { get; set; }
        void IPoolable.OnRecycled()
        {
            
        }
        #endregion
        
        public T LoadAssetSync<T>(string assetPath) where T : Object
        {
            throw new NotImplementedException();
        }

        public T LoadAssetSync<T>(string assetPath, string packageName) where T : Object
        {
            throw new NotImplementedException();
        }

        public void LoadAssetAsync<T>(string assetPath, Action<bool, T> callback, string packageName) where T : Object
        {
            throw new NotImplementedException();
        }

        public UniTask<T> LoadAssetAsyncTask<T>(string assetPath, string packageName) where T : Object
        {
            throw new NotImplementedException();
        }

        public GameObject Clone(string assetPath, string packageName)
        {
            throw new NotImplementedException();
        }

        public GameObject Clone(string assetPath, Transform parent, string packageName)
        {
            throw new NotImplementedException();
        }

        public GameObject Clone(string assetPath, Transform parent, Vector3 position, Quaternion rotation, string packageName)
        {
            throw new NotImplementedException();
        }

        public void CloneAsync(string assetPath, Action<bool, GameObject> callback, string packageName)
        {
            throw new NotImplementedException();
        }

        public void CloneAsync(string assetPath, Transform parent, Action<bool, GameObject> callback, string packageName)
        {
            throw new NotImplementedException();
        }

        public void CloneAsync(string assetPath, Transform parent, Vector3 position, Quaternion rotation, Action<GameObject> callback,
            string packageName)
        {
            throw new NotImplementedException();
        }

        public UniTask<GameObject> CloneAsyncTask(string assetPath, string packageName)
        {
            throw new NotImplementedException();
        }

        public UniTask<GameObject> CloneAsyncTask(string assetPath, Transform parent, string packageName)
        {
            throw new NotImplementedException();
        }

        public UniTask<GameObject> CloneAsyncTask(string assetPath, Transform parent, Vector3 position, Quaternion rotation, string packageName)
        {
            throw new NotImplementedException();
        }

        public object LoadSceneSync(string scenePath, string packageName, LoadSceneMode sceneMode = LoadSceneMode.Single,
            LocalPhysicsMode physicsMode = LocalPhysicsMode.None)
        {
            throw new NotImplementedException();
        }

        public object LoadSceneAsync(string scenePath, string packageName, LoadSceneMode sceneMode = LoadSceneMode.Single,
            LocalPhysicsMode physicsMode = LocalPhysicsMode.None, bool suspendLoad = false, uint priority = 100)
        {
            throw new NotImplementedException();
        }

        public bool DestroyGameObject(GameObject go)
        {
            throw new NotImplementedException();
        }
    }
}