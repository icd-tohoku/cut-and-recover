using UnityEngine;
using System;

namespace Nankira
{
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance = null;

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;

                InitInstance();
                if (_instance == null)
                {
                    throw new UnityException("Singleton initialization failed.");
                }

                return _instance;
            }
        }

        private static void InitInstance()
        {
            if (_instance != null) return;

            Type typeOfThis = typeof(T);
            var go = new GameObject(typeOfThis.Name);
            _instance = go.AddComponent<T>();
            DontDestroyOnLoad(go);

            if (_instance is SingletonMonoBehaviour<T> result)
            {
                result.Init();
            }
        }

        protected abstract void Init();

        public static void Destroy()
        {
            Destroy(_instance?.gameObject);
            _instance = null;
        }
    }
}