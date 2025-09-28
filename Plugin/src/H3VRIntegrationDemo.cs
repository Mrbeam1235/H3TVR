using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FistVR;

namespace H3TVR
{
    /// <summary>
    /// Demonstrates integration with H3VR systems for loading assets and configuring sosigs
    /// This class shows how to use the H3VRAssetLoader and related systems
    /// </summary>
    public class H3VRIntegrationDemo : MonoBehaviour
    {
        [Header("Demo Configuration")]
        public bool runDemoOnStart = false;
        public bool enableDebugOutput = true;
        
        [Header("Asset Loading")]
        public bool loadArmor = true;
        public bool loadWeapons = true;
        public bool loadTemplates = true;
        
        void Start()
        {
            if (runDemoOnStart)
            {
                StartCoroutine(RunIntegrationDemo());
            }
        }
        
        private System.Collections.IEnumerator RunIntegrationDemo()
        {
            LogDemo("Starting H3VR Integration Demo...");
            
            // Wait for H3VR systems to be ready
            while (IM.OD == null || IM.OD.Count == 0)
            {
                LogDemo("Waiting for H3VR ItemManager to be ready...");
                yield return new UnityEngine.WaitForSeconds(1f);
            }
            
            // Initialize H3VR Asset Loader
            LogDemo("Initializing H3VR Asset Loader...");
            H3VRAssetLoader.Initialize();
            
            if (!H3VRAssetLoader.IsInitialized)
            {
                LogDemo("Failed to initialize H3VR Asset Loader!");
                yield break;
            }
            
            // Demo asset loading capabilities
            if (loadArmor) yield return DemoArmorLoading();
            if (loadWeapons) yield return DemoWeaponLoading();
            if (loadTemplates) yield return DemoTemplateLoading();
            
            // Demo custom outfit creation
            yield return DemoCustomOutfitCreation();
            
            LogDemo("H3VR Integration Demo completed successfully!");
        }
        
        private System.Collections.IEnumerator DemoArmorLoading()
        {
            LogDemo("=== ARMOR LOADING DEMO ===");
            
            var armorCategories = H3VRAssetLoader.GetAllArmorCategories();
            LogDemo($"Found {armorCategories.Count} armor categories:");
            
            foreach (var category in armorCategories)
            {
                LogDemo($"  {category.Key}: {category.Value.Count} items");
                
                // Show first few items in each category
                for (int i = 0; i < Mathf.Min(3, category.Value.Count); i++)
                {
                    var armorPiece = category.Value[i];
                    LogDemo($"    - {armorPiece.ItemID} ({armorPiece.DisplayName})");
                }
                
                if (category.Value.Count > 3)
                {
                    LogDemo($"    ... and {category.Value.Count - 3} more items");
                }
            }
            
            // Demo getting random armor pieces
            LogDemo("\nTesting random armor selection:");
            var randomHelmet = H3VRAssetLoader.GetRandomArmor("Headwear");
            var randomVest = H3VRAssetLoader.GetRandomArmor("Torsowear");
            
            LogDemo($"Random helmet: {randomHelmet?.ItemID ?? "None available"}");
            LogDemo($"Random vest: {randomVest?.ItemID ?? "None available"}");
            
            yield return new UnityEngine.WaitForSeconds(0.5f);
        }
        
        private System.Collections.IEnumerator DemoWeaponLoading()
        {
            LogDemo("=== WEAPON LOADING DEMO ===");
            
            var allWeapons = H3VRAssetLoader.GetAllWeapons();
            var firearms = H3VRAssetLoader.GetWeaponsByCategory(FVRObject.ObjectCategory.Firearm);
            var melee = H3VRAssetLoader.GetWeaponsByCategory(FVRObject.ObjectCategory.MeleeWeapon);
            var thrown = H3VRAssetLoader.GetWeaponsByCategory(FVRObject.ObjectCategory.Thrown);
            
            LogDemo($"Total weapons loaded: {allWeapons.Count}");
            LogDemo($"  Firearms: {firearms.Count}");
            LogDemo($"  Melee weapons: {melee.Count}");
            LogDemo($"  Thrown weapons: {thrown.Count}");
            
            // Demo weapon pattern searching
            var rifles = H3VRAssetLoader.GetWeaponsByPattern("rifle");
            var pistols = H3VRAssetLoader.GetWeaponsByPattern("pistol");
            
            LogDemo($"\nWeapon pattern matching:");
            LogDemo($"  Rifles (contains 'rifle'): {rifles.Count}");
            LogDemo($"  Pistols (contains 'pistol'): {pistols.Count}");
            
            // Show random weapon selection
            var randomFirearm = H3VRAssetLoader.GetRandomWeapon(FVRObject.ObjectCategory.Firearm);
            LogDemo($"Random firearm: {randomFirearm?.ItemID ?? "None available"}");
            
            yield return new UnityEngine.WaitForSeconds(0.5f);
        }
        
        private System.Collections.IEnumerator DemoTemplateLoading()
        {
            LogDemo("=== TEMPLATE LOADING DEMO ===");
            
            var templates = H3VRAssetLoader.GetAllSosigTemplates();
            var outfits = H3VRAssetLoader.GetAllOutfitConfigs();
            
            LogDemo($"Sosig templates loaded: {templates.Count}");
            LogDemo($"Outfit configurations loaded: {outfits.Count}");
            
            // Show first few templates
            LogDemo("\nAvailable sosig templates:");
            for (int i = 0; i < Mathf.Min(5, templates.Count); i++)
            {
                var template = templates[i];
                LogDemo($"  {i + 1}. {template.name ?? "Unknown Template"}");
            }
            
            if (templates.Count > 5)
            {
                LogDemo($"  ... and {templates.Count - 5} more templates");
            }
            
            yield return new UnityEngine.WaitForSeconds(0.5f);
        }
        
        private System.Collections.IEnumerator DemoCustomOutfitCreation()
        {
            LogDemo("=== CUSTOM OUTFIT CREATION DEMO ===");
            
            // Create a heavily armored outfit configuration
            var heavyOutfit = H3VRAssetLoader.CreateCustomOutfitFromAssets(
                headwearChance: 1.0f,      // Always wear helmet
                facewearChance: 0.8f,      // Usually wear face protection
                eyewearChance: 0.6f,       // Often wear eye protection
                torsowearChance: 1.0f,     // Always wear body armor
                pantswearChance: 0.9f,     // Usually wear leg protection
                pantswearLowerChance: 0.7f, // Often wear lower leg armor
                backpackChance: 0.8f,      // Usually wear backpack
                decorationChance: 0.3f     // Sometimes wear decorations
            );
            
            LogDemo("Created heavy combat outfit with high armor chances:");
            LogDemo($"  Headwear pieces: {heavyOutfit.Headwear?.Count ?? 0}");
            LogDemo($"  Facewear pieces: {heavyOutfit.Facewear?.Count ?? 0}");
            LogDemo($"  Torsowear pieces: {heavyOutfit.Torsowear?.Count ?? 0}");
            LogDemo($"  Backpack pieces: {heavyOutfit.Backpacks?.Count ?? 0}");
            
            // Create a light civilian outfit
            var civilianOutfit = H3VRAssetLoader.CreateCustomOutfitFromAssets(
                headwearChance: 0.2f,      // Rarely wear head gear
                facewearChance: 0.1f,      // Rarely wear face protection
                eyewearChance: 0.3f,       // Sometimes wear glasses
                torsowearChance: 0.8f,     // Usually wear clothing
                pantswearChance: 0.9f,     // Usually wear pants
                pantswearLowerChance: 0.1f, // Rarely wear lower leg gear
                backpackChance: 0.2f,      // Rarely wear backpack
                decorationChance: 0.1f     // Rarely wear decorations
            );
            
            LogDemo("\nCreated civilian outfit with low armor chances:");
            LogDemo($"  Outfit suitable for non-combat sosigs");
            LogDemo($"  Lower protection, higher civilian appearance");
            
            yield return new UnityEngine.WaitForSeconds(0.5f);
        }
        
        private void LogDemo(string message)
        {
            if (enableDebugOutput)
            {
                Debug.Log($"[H3VR Integration Demo] {message}");
            }
        }
        
        /// <summary>
        /// Public method to manually trigger the demo
        /// </summary>
        [ContextMenu("Run Integration Demo")]
        public void RunDemo()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(RunIntegrationDemo());
            }
            else
            {
                Debug.LogWarning("Demo can only be run during play mode");
            }
        }
        
        /// <summary>
        /// Get loading statistics for debugging
        /// </summary>
        [ContextMenu("Show Loading Stats")]
        public void ShowLoadingStats()
        {
            if (H3VRAssetLoader.IsInitialized)
            {
                LogDemo("=== H3VR ASSET LOADER STATISTICS ===");
                LogDemo(H3VRAssetLoader.GetLoadingStats());
                LogDemo("=====================================");
            }
            else
            {
                LogDemo("H3VR Asset Loader not initialized yet");
            }
        }
        
        /// <summary>
        /// Test specific object loading
        /// </summary>
        [ContextMenu("Test Object Loading")]
        public void TestObjectLoading()
        {
            // Test loading a known H3VR object
            var testObject = H3VRAssetLoader.GetObjectByID("AssaultRifle_M4");
            if (testObject != null)
            {
                LogDemo($"Successfully loaded test object: {testObject.ItemID} - {testObject.DisplayName}");
                
                var safeGameObject = H3VRAssetLoader.GetSafeGameObject(testObject);
                if (safeGameObject != null)
                {
                    LogDemo("GameObject retrieval successful");
                }
                else
                {
                    LogDemo("GameObject retrieval failed");
                }
            }
            else
            {
                LogDemo("Failed to load test object M4 - trying alternative...");
                
                // Try to find any firearm
                var allWeapons = H3VRAssetLoader.GetAllWeapons();
                if (allWeapons.Count > 0)
                {
                    var firstWeapon = allWeapons[0];
                    LogDemo($"Found alternative weapon: {firstWeapon.ItemID}");
                }
                else
                {
                    LogDemo("No weapons available for testing");
                }
            }
        }
    }
}