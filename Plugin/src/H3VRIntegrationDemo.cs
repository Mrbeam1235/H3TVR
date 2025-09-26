using System.Collections;
using System.Linq;
using UnityEngine;
using FistVR;

namespace H3TVR
{
    /// <summary>
    /// Demonstration of H3VR DLL integration for loading armor and loadouts
    /// This class shows how to use the H3VR asset loading system
    /// </summary>
    public class H3VRIntegrationDemo : MonoBehaviour
    {
        [Header("Demo Configuration")]
        public bool runDemoOnStart = false;
        public float demoDelay = 2f;
        
        [Header("Status Display")]
        public bool showDebugInfo = true;
        
        private void Start()
        {
            if (runDemoOnStart)
            {
                StartCoroutine(RunDemoAfterDelay());
            }
        }
        
        private IEnumerator RunDemoAfterDelay()
        {
            yield return new WaitForSeconds(demoDelay);
            RunH3VRIntegrationDemo();
        }
        
        /// <summary>
        /// Demonstrates the complete H3VR integration workflow
        /// </summary>
        public void RunH3VRIntegrationDemo()
        {
            Debug.Log("=== H3VR Integration Demo Starting ===");
            
            // Step 1: Check system status
            LogSystemStatus();
            
            // Step 2: Test asset loading
            TestAssetLoading();
            
            // Step 3: Create a demo sosig with H3VR assets
            CreateDemoSosig();
            
            Debug.Log("=== H3VR Integration Demo Complete ===");
        }
        
        private void LogSystemStatus()
        {
            Debug.Log("--- System Status Check ---");
            
            // Check if H3VR systems are ready
            bool isReady = H3VRAssetLoader.IsH3VRSystemReady();
            Debug.Log($"H3VR System Ready: {isReady}");
            
            if (isReady)
            {
                var stats = H3VRAssetLoader.GetLoadingStats();
                Debug.Log($"Available Assets - {stats}");
            }
            else
            {
                Debug.Log("H3VR systems not ready - using delayed initialization");
                // The system will automatically retry via H3VRDelayedInitializer
            }
        }
        
        private void TestAssetLoading()
        {
            Debug.Log("--- Asset Loading Test ---");
            
            // Run the comprehensive asset loading test
            H3VRAssetLoadingTest.RunAssetLoadingTest();
            
            // Test specific asset categories
            TestArmorAssets();
            TestWeaponAssets();
        }
        
        private void TestArmorAssets()
        {
            Debug.Log("Testing Armor Assets:");
            
            var armorCategories = H3VRAssetLoader.GetAllArmorCategories();
            Debug.Log($"Found {armorCategories.Count} armor categories from H3VR DLL");
            
            // Show first few armor pieces as examples
            int count = 0;
            foreach (var category in armorCategories)
            {
                if (count >= 3) break; // Just show first 3 as examples
                Debug.Log($"  - {category.Key}: {category.Value.Count} items");
                count++;
            }
        }
        
        private void TestWeaponAssets()
        {
            Debug.Log("Testing Weapon Assets:");
            
            var weapons = H3VRAssetLoader.GetAllWeapons();
            Debug.Log($"Found {weapons.Count} weapons from H3VR DLL");
            
            // Show first few weapons as examples
            int count = 0;
            foreach (var weapon in weapons)
            {
                if (count >= 3) break; // Just show first 3 as examples
                Debug.Log($"  - {weapon.ItemID}: {weapon.DisplayName ?? "unknown"}");
                count++;
            }
        }
        
        private void CreateDemoSosig()
        {
            Debug.Log("--- Demo Sosig Creation ---");
            
            // Create a simple loadout configuration using H3VR assets
            var demoLoadout = new SosigLoadoutConfiguration
            {
                loadoutName = "H3VR Demo Loadout",
                description = "Demo loadout using H3VR assets",
                primaryWeapons = new System.Collections.Generic.List<string> { "AK74" },
                secondaryWeapons = new System.Collections.Generic.List<string> { "M1911" },
                tertiaryWeapons = new System.Collections.Generic.List<string>()
            };
            
            // Test dry run (doesn't actually spawn, just validates)
            bool canCreate = SosigLoadoutUtility.CanCreateSosigFromLoadout(demoLoadout);
            Debug.Log($"Can create demo sosig: {canCreate}");
            
            if (canCreate)
            {
                Debug.Log("Demo sosig loadout validated successfully!");
                Debug.Log($"Using H3VR assets: Primary={string.Join(", ", demoLoadout.primaryWeapons.ToArray())}, Secondary={string.Join(", ", demoLoadout.secondaryWeapons.ToArray())}");
            }
            else
            {
                Debug.LogWarning("Demo sosig could not be created - check H3VR asset availability");
            }
        }
        
        /// <summary>
        /// Public method to check if the H3VR integration is working
        /// </summary>
        /// <returns>True if integration is functional</returns>
        public bool IsH3VRIntegrationWorking()
        {
            // Basic functionality check
            if (!H3VRAssetLoader.IsH3VRSystemReady())
            {
                Debug.Log("H3VR systems not ready yet - may work after delayed initialization");
                return false;
            }
            
            var armorCategories = H3VRAssetLoader.GetAllArmorCategories();
            var weapons = H3VRAssetLoader.GetAllWeapons();
            var templates = H3VRAssetLoader.GetAllSosigTemplates();
            
            int armorCount = armorCategories.Values.Sum(list => list.Count);
            bool hasAssets = armorCount > 0 || weapons.Count > 0 || templates.Count > 0;
            
            if (!hasAssets)
            {
                Debug.LogWarning("H3VR system ready but no assets loaded");
                return false;
            }
            
            Debug.Log($"H3VR Integration Working! Loaded {armorCount} armor, {weapons.Count} weapons, {templates.Count} sosig templates");
            return true;
        }
        
        /// <summary>
        /// Force a complete system refresh and test
        /// </summary>
        public void RefreshAndTest()
        {
            Debug.Log("Forcing H3VR asset refresh and retest...");
            
            // Force reload all assets
            H3VRAssetLoader.ForceReload();
            
            // Wait a moment then test
            StartCoroutine(DelayedRetest());
        }
        
        private IEnumerator DelayedRetest()
        {
            yield return new WaitForSeconds(1f);
            RunH3VRIntegrationDemo();
        }
        
        // Unity Inspector buttons (if using custom inspector)
        [ContextMenu("Run H3VR Demo")]
        private void RunDemoFromMenu()
        {
            RunH3VRIntegrationDemo();
        }
        
        [ContextMenu("Check Integration Status")]
        private void CheckStatusFromMenu()
        {
            bool working = IsH3VRIntegrationWorking();
            Debug.Log($"H3VR Integration Status: {(working ? "WORKING" : "NOT READY")}");
        }
        
        [ContextMenu("Refresh and Test")]
        private void RefreshFromMenu()
        {
            RefreshAndTest();
        }
    }
}