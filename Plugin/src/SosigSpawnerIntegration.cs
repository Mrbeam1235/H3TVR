using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using BepInEx;
using BepInEx.Configuration;

namespace H3TVR
{
    /// <summary>
    /// Integration class for the SosigSpawnerManager
    /// Handles initialization and integration with the main H3TVR plugin
    /// </summary>
    public class SosigSpawnerIntegration : MonoBehaviour
    {
        private SosigSpawnerManager spawnerManager;
        private SosigWeaponManager weaponManager;
        private SosigScenarioManager scenarioManager;
        private SosigStatsManager statsManager;
        private bool isInitialized = false;

        // Configuration entries
        public static ConfigEntry<bool> EnableSosigSpawner;
        public static ConfigEntry<bool> ShowSpawnerTips;
        public static ConfigEntry<float> SpawnerUpdateInterval;
        public static ConfigEntry<bool> EnableWeaponSystem;
        public static ConfigEntry<bool> EnableScenarioSystem;
        public static ConfigEntry<bool> EnableStatsTracking;
        public static ConfigEntry<KeyCode> ShowStatsKey;
        public static ConfigEntry<bool> AutoSaveStats;

        void Start()
        {
            StartCoroutine(InitializeWithDelay());
        }

        private IEnumerator InitializeWithDelay()
        {
            // Wait a frame to ensure other systems are initialized
            yield return null;
            
            InitializeConfiguration();
            
            if (EnableSosigSpawner.Value)
            {
                InitializeSpawner();
            }
        }

        private void InitializeConfiguration()
        {
            var h3tvrPlugin = FindObjectOfType<H3TVR>();
            if (h3tvrPlugin == null)
            {
                Debug.LogError("H3TVR plugin not found! Cannot initialize sosig spawner configuration.");
                return;
            }

            var config = ((BaseUnityPlugin)h3tvrPlugin).Config;

            EnableSosigSpawner = config.Bind("Sosig Spawner Integration", "EnableSosigSpawner", true, 
                "Enable the advanced sosig spawner system");
            
            ShowSpawnerTips = config.Bind("Sosig Spawner Integration", "ShowSpawnerTips", true, 
                "Show helpful tips for using the sosig spawner");
            
            SpawnerUpdateInterval = config.Bind("Sosig Spawner Integration", "SpawnerUpdateInterval", 1.0f, 
                "Update interval for spawner system checks (in seconds)");
                
            EnableWeaponSystem = config.Bind("Sosig Spawner Integration", "EnableWeaponSystem", true,
                "Enable advanced weapon management for sosigs");
                
            EnableScenarioSystem = config.Bind("Sosig Spawner Integration", "EnableScenarioSystem", true,
                "Enable scenario and wave-based spawning system");
                
            EnableStatsTracking = config.Bind("Sosig Spawner Integration", "EnableStatsTracking", true,
                "Enable statistics tracking for spawned sosigs");
                
            ShowStatsKey = config.Bind("Sosig Spawner Integration", "ShowStatsKey", KeyCode.Tab,
                "Key to display spawner statistics");
                
            AutoSaveStats = config.Bind("Sosig Spawner Integration", "AutoSaveStats", true,
                "Automatically save statistics periodically");
        }

        private void InitializeSpawner()
        {
            GameObject spawnerObject = new GameObject("SosigSpawnerManager");
            spawnerObject.transform.SetParent(transform);
            
            // Initialize H3VR asset loading with fallback handling
            Debug.Log("[SosigSpawnerIntegration] Initializing H3VR asset loading...");
            H3VRAssetLoader.TryInitializeWithDelay();
            
            // If immediate initialization failed, set up delayed initialization
            if (!H3VRAssetLoader.IsInitialized)
            {
                Debug.Log("[SosigSpawnerIntegration] H3VR not ready, setting up delayed initialization...");
                H3VRDelayedInitializer.EnsureInstance();
            }
            else
            {
                SosigLoadoutManager.Initialize();
            }
            
            spawnerManager = spawnerObject.AddComponent<SosigSpawnerManager>();
            
            // Initialize weapon system
            if (EnableWeaponSystem.Value)
            {
                weaponManager = spawnerObject.AddComponent<SosigWeaponManager>();
                Debug.Log("Sosig Weapon System initialized");
            }
            
            // Initialize scenario system
            if (EnableScenarioSystem.Value)
            {
                scenarioManager = spawnerObject.AddComponent<SosigScenarioManager>();
                scenarioManager.InitializeScenarios();
                Debug.Log("Sosig Scenario System initialized");
            }
            
            // Initialize stats system
            if (EnableStatsTracking.Value)
            {
                statsManager = spawnerObject.AddComponent<SosigStatsManager>();
                Debug.Log("Sosig Statistics System initialized");
            }
            
            if (spawnerManager != null)
            {
                isInitialized = true;
                Debug.Log("Advanced Sosig Spawner with H3VR asset loading and all systems initialized successfully!");
                LogAssetLoadingResults();
                
                if (ShowSpawnerTips.Value)
                {
                    StartCoroutine(ShowInitializationTip());
                }
            }
            else
            {
                Debug.LogError("Failed to initialize SosigSpawnerManager!");
            }
        }
        
        /// <summary>
        /// Log the results of H3VR asset loading
        /// </summary>
        private void LogAssetLoadingResults()
        {
            if (!H3VRAssetLoader.IsInitialized) return;
            
            var armorCategories = H3VRAssetLoader.GetAllArmorCategories();
            var weapons = H3VRAssetLoader.GetAllWeapons();
            var templates = H3VRAssetLoader.GetAllSosigTemplates();
            var outfits = H3VRAssetLoader.GetAllOutfitConfigs();
            var loadouts = SosigLoadoutManager.GetLoadouts();
            
            Debug.Log("=== H3VR ASSET LOADING RESULTS ===");
            Debug.Log($"Armor pieces loaded: {armorCategories.Values.Sum(list => list.Count)}");
            foreach (var category in armorCategories)
            {
                Debug.Log($"  {category.Key}: {category.Value.Count} items");
            }
            Debug.Log($"Weapons loaded: {weapons.Count}");
            Debug.Log($"Sosig templates: {templates.Count}");
            Debug.Log($"Outfit configs: {outfits.Count}");
            Debug.Log($"Advanced loadouts: {loadouts.Count}");
            Debug.Log("==================================");
        }

        private IEnumerator ShowInitializationTip()
        {
            yield return new WaitForSeconds(3f);
            
            // Show tip to player about the spawner
            if (GM.CurrentPlayerBody != null)
            {
                Debug.Log("Tip: Press F9 to open the Advanced Sosig Spawner menu!");
                // You could add a UI notification here if you have a notification system
            }
        }

        void Update()
        {
            if (!isInitialized || spawnerManager == null)
                return;

            // Handle statistics display
            if (EnableStatsTracking.Value && Input.GetKeyDown(ShowStatsKey.Value))
            {
                DisplayStatistics();
            }

            // Handle any integration-specific updates here
            HandleSpawnerIntegration();
        }
        
        private void DisplayStatistics()
        {
            if (statsManager != null)
            {
                string statsReport = statsManager.GetStatsReport();
                Debug.Log(statsReport);
                // Could also display in GUI or send to chat
            }
        }

        private void HandleSpawnerIntegration()
        {
            // This method can be used to handle integration between the spawner
            // and other H3TVR systems, such as:
            // - Synchronizing with slomo effects
            // - Coordinating with other spawned entities
            // - Managing performance when many sosigs are spawned
            
            if (Time.time % SpawnerUpdateInterval.Value < Time.deltaTime)
            {
                PerformPeriodicUpdates();
            }
        }

        private void PerformPeriodicUpdates()
        {
            if (spawnerManager == null) return;

            // Clean up any destroyed sosigs
            var spawnedSosigs = spawnerManager.GetSpawnedSosigs();
            
            // Optional: Implement performance management
            if (spawnedSosigs.Count > 20) // Arbitrary limit
            {
                Debug.LogWarning($"Many sosigs spawned ({spawnedSosigs.Count}). Consider clearing some for performance.");
            }

            // Optional: Integrate with slomo system
            var h3tvrPlugin = FindObjectOfType<H3TVR>();
            if (h3tvrPlugin != null && h3tvrPlugin.SlomoStatus == "On")
            {
                // You could implement special behavior during slomo here
                HandleSlomoIntegration(spawnedSosigs);
            }
        }

        private void HandleSlomoIntegration(List<Sosig> sosigs)
        {
            // Example: Make sosigs more dramatic during slomo
            foreach (var sosig in sosigs)
            {
                if (sosig == null) continue;
                
                // You could modify sosig behavior during slomo
                // For example, make them react more slowly or dramatically
            }
        }

        public SosigSpawnerManager GetSpawnerManager()
        {
            return spawnerManager;
        }
        
        public SosigWeaponManager GetWeaponManager()
        {
            return weaponManager;
        }
        
        public SosigScenarioManager GetScenarioManager()
        {
            return scenarioManager;
        }
        
        public SosigStatsManager GetStatsManager()
        {
            return statsManager;
        }

        public bool IsSpawnerInitialized()
        {
            return isInitialized && spawnerManager != null;
        }
        
        public bool AreAllSystemsInitialized()
        {
            return isInitialized && spawnerManager != null &&
                   (!EnableWeaponSystem.Value || weaponManager != null) &&
                   (!EnableScenarioSystem.Value || scenarioManager != null) &&
                   (!EnableStatsTracking.Value || statsManager != null);
        }

        void OnDestroy()
        {
            if (spawnerManager != null)
            {
                Debug.Log("Sosig Spawner Integration shutting down...");
            }
        }
    }

    /// <summary>
    /// Static utility class for easy access to sosig spawner functionality
    /// </summary>
    public static class SosigSpawnerAPI
    {
        private static SosigSpawnerIntegration integration;
        
        public static SosigSpawnerManager GetSpawner()
        {
            if (integration == null)
            {
                integration = Object.FindObjectOfType<SosigSpawnerIntegration>();
            }
            
            return integration?.GetSpawnerManager();
        }

        public static bool IsSpawnerAvailable()
        {
            var spawner = GetSpawner();
            return spawner != null;
        }

        public static void SpawnSosigAt(Vector3 position, int IFF = 0)
        {
            var spawner = GetSpawner();
            if (spawner != null)
            {
                // You could extend the spawner to support direct position spawning
                spawner.SpawnSosig();
            }
        }

        public static List<Sosig> GetAllSpawnedSosigs()
        {
            var spawner = GetSpawner();
            return spawner?.GetSpawnedSosigs() ?? new List<Sosig>();
        }

        public static int GetSpawnedSosigCount()
        {
            return GetAllSpawnedSosigs().Count;
        }
    }
}