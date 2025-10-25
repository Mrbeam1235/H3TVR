using System.Collections;
using UnityEngine;
using BepInEx;
using FistVR;

namespace H3TVR
{
    /// <summary>
    /// Handles delayed initialization when H3VR systems aren't ready immediately
    /// Notifies spawners when game systems are available
    /// </summary>
    public class H3VRDelayedInitializer : MonoBehaviour
    {
        private static H3VRDelayedInitializer instance;
        private bool initializationAttempted = false;
        private int maxRetries = 10;
        private float retryDelay = 2.0f;
        private bool systemsReady = false;
        
        public static bool AreSystemsReady => instance != null && instance.systemsReady;
        
        public static void EnsureInstance()
        {
            if (instance == null)
            {
                GameObject go = new GameObject("H3VRDelayedInitializer");
                instance = go.AddComponent<H3VRDelayedInitializer>();
                DontDestroyOnLoad(go);
            }
        }
        
        void Start()
        {
            if (!initializationAttempted)
            {
                StartCoroutine(DelayedInitialization());
            }
        }
        
        private IEnumerator DelayedInitialization()
        {
            initializationAttempted = true;
            int retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                // Wait a bit for H3VR systems to initialize
                yield return new WaitForSeconds(retryDelay);
                
                // Check if H3VR ItemManager is ready
                if (IM.OD != null && IM.OD.Count > 0)
                {
                    Debug.Log($"[H3VRDelayedInitializer] H3VR systems ready (attempt {retryCount + 1})");
                    
                    systemsReady = true;
                    
                    // Notify other systems that H3VR is ready
                    NotifySystemsReady();
                    yield break; // Success, exit
                }
                else
                {
                    Debug.Log($"[H3VRDelayedInitializer] H3VR systems not ready yet (attempt {retryCount + 1})");
                }
                
                retryCount++;
            }
            
            Debug.LogWarning($"[H3VRDelayedInitializer] H3VR systems not ready after {maxRetries} attempts");
        }
        
        private void NotifySystemsReady()
        {
            // Find and notify advanced chat spawners that systems are ready
            var advancedSpawners = FindObjectsOfType<AdvancedChatSosigSpawner>();
            foreach (var spawner in advancedSpawners)
            {
                Debug.Log("[H3VRDelayedInitializer] Notified AdvancedChatSosigSpawner that H3VR systems are ready");
            }
            
            Debug.Log("[H3VRDelayedInitializer] System ready notification complete");
        }
        
        /// <summary>
        /// Force immediate retry of initialization
        /// </summary>
        public static void ForceRetry()
        {
            EnsureInstance();
            if (instance != null && !instance.systemsReady)
            {
                instance.StopAllCoroutines();
                instance.initializationAttempted = false;
                instance.StartCoroutine(instance.DelayedInitialization());
            }
        }
    }
}