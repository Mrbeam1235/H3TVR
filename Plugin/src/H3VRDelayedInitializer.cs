using System.Collections;
using UnityEngine;
using BepInEx;
using FistVR;

namespace H3TVR
{
    /// <summary>
    /// Handles delayed initialization of H3VR asset loading when systems aren't ready immediately
    /// </summary>
    public class H3VRDelayedInitializer : MonoBehaviour
    {
        private static H3VRDelayedInitializer instance;
        private bool initializationAttempted = false;
        private int maxRetries = 10;
        private float retryDelay = 2.0f;
        
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
                    Debug.Log($"[H3VRDelayedInitializer] H3VR systems ready, attempting asset loading (attempt {retryCount + 1})");
                    
                    try
                    {
                        H3VRAssetLoader.Initialize();
                        
                        if (H3VRAssetLoader.IsInitialized)
                        {
                            Debug.Log("[H3VRDelayedInitializer] H3VR asset loading successful!");
                            SosigLoadoutManager.Initialize();
                            
                            // Notify other systems that assets are ready
                            NotifySystemsReady();
                            yield break; // Success, exit
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[H3VRDelayedInitializer] Asset loading failed on attempt {retryCount + 1}: {ex.Message}");
                    }
                }
                else
                {
                    Debug.Log($"[H3VRDelayedInitializer] H3VR systems not ready yet (attempt {retryCount + 1})");
                }
                
                retryCount++;
            }
            
            Debug.LogWarning($"[H3VRDelayedInitializer] Failed to initialize H3VR assets after {maxRetries} attempts");
        }
        
        private void NotifySystemsReady()
        {
            // Find and notify spawner managers that assets are ready
            var spawnerManagers = FindObjectsOfType<SosigSpawnerManager>();
            foreach (var manager in spawnerManagers)
            {
                // You could add a method to notify the manager that assets are ready
                Debug.Log("[H3VRDelayedInitializer] Notified SosigSpawnerManager that H3VR assets are ready");
            }
            
            var integrations = FindObjectsOfType<SosigSpawnerIntegration>();
            foreach (var integration in integrations)
            {
                Debug.Log("[H3VRDelayedInitializer] Notified SosigSpawnerIntegration that H3VR assets are ready");
            }
        }
        
        /// <summary>
        /// Force immediate retry of initialization
        /// </summary>
        public static void ForceRetry()
        {
            EnsureInstance();
            if (instance != null && !H3VRAssetLoader.IsInitialized)
            {
                instance.StartCoroutine(instance.DelayedInitialization());
            }
        }
    }
}