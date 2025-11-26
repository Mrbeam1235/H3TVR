using UnityEngine;

namespace H3TVR
{
    public static class MainThreadUtil
    {
        private static readonly System.Collections.Generic.Queue<System.Action> _actions = new System.Collections.Generic.Queue<System.Action>();
        private static MainThreadDispatcher _dispatcher;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_dispatcher == null)
            {
                GameObject obj = new GameObject("MainThreadDispatcher");
                _dispatcher = obj.AddComponent<MainThreadDispatcher>();
                Object.DontDestroyOnLoad(obj);
            }
        }

        public static void Run(System.Action action)
        {
            lock (_actions)
            {
                _actions.Enqueue(action);
            }
        }

        private class MainThreadDispatcher : MonoBehaviour
        {
            private void Update()
            {
                lock (_actions)
                {
                    while (_actions.Count > 0)
                    {
                        _actions.Dequeue()?.Invoke();
                    }
                }
            }
        }
    }
}
