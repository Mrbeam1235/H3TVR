using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FistVR;

namespace H3TVR
{
    /// <summary>
    /// Test class for verifying H3VR asset loading functionality
    /// </summary>
    public static class H3VRAssetLoadingTest
    {
        /// <summary>
        /// Run comprehensive test of H3VR asset loading
        /// </summary>
        public static void RunAssetLoadingTest()
        {
            Debug.Log("=== STARTING H3VR ASSET LOADING TEST ===");
            
            try
            {
                // Test 1: Initialize the asset loader
                Debug.Log("Test 1: Initializing H3VR Asset Loader...");
                H3VRAssetLoader.Initialize();
                
                if (!H3VRAssetLoader.IsInitialized)
                {
                    Debug.LogError("FAILED: Asset loader not initialized");
                    return;
                }
                Debug.Log("PASSED: Asset loader initialized successfully");
                
                // Test 2: Check armor loading
                Debug.Log("Test 2: Testing armor loading...");
                TestArmorLoading();
                
                // Test 3: Check weapon loading
                Debug.Log("Test 3: Testing weapon loading...");
                TestWeaponLoading();
                
                // Test 4: Check sosig template loading
                Debug.Log("Test 4: Testing sosig template loading...");
                TestSosigTemplateLoading();
                
                // Test 5: Check outfit config loading
                Debug.Log("Test 5: Testing outfit config loading...");
                TestOutfitConfigLoading();
                
                // Test 6: Test loadout creation
                Debug.Log("Test 6: Testing advanced loadout creation...");
                TestLoadoutCreation();
                
                // Test 7: Test custom outfit creation
                Debug.Log("Test 7: Testing custom outfit creation...");
                TestCustomOutfitCreation();
                
                Debug.Log("=== H3VR ASSET LOADING TEST COMPLETED ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"FAILED: Asset loading test crashed: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Test armor piece loading
        /// </summary>
        private static void TestArmorLoading()
        {
            var armorCategories = H3VRAssetLoader.GetAllArmorCategories();
            
            Debug.Log($"  Found {armorCategories.Count} armor categories");
            
            int totalArmor = 0;
            foreach (var category in armorCategories)
            {
                totalArmor += category.Value.Count;
                Debug.Log($"    {category.Key}: {category.Value.Count} items");
                
                // Test a few items from each category
                if (category.Value.Count > 0)
                {
                    var sampleItem = category.Value[0];
                    Debug.Log($"      Sample: {sampleItem?.ItemID ?? "null"}");
                }
            }
            
            if (totalArmor > 0)
            {
                Debug.Log($"  PASSED: Loaded {totalArmor} total armor pieces");
            }
            else
            {
                Debug.LogWarning("  WARNING: No armor pieces loaded");
            }
            
            // Test random armor selection
            var randomHeadwear = H3VRAssetLoader.GetRandomArmor("Headwear");
            Debug.Log($"  Random headwear test: {randomHeadwear?.ItemID ?? "none available"}");
        }
        
        /// <summary>
        /// Test weapon loading
        /// </summary>
        private static void TestWeaponLoading()
        {
            var allWeapons = H3VRAssetLoader.GetAllWeapons();
            Debug.Log($"  Found {allWeapons.Count} total weapons");
            
            var firearms = H3VRAssetLoader.GetWeaponsByCategory(FVRObject.ObjectCategory.Firearm);
            var meleeWeapons = H3VRAssetLoader.GetWeaponsByCategory(FVRObject.ObjectCategory.MeleeWeapon);
            var thrown = H3VRAssetLoader.GetWeaponsByCategory(FVRObject.ObjectCategory.Thrown);
            
            Debug.Log($"    Firearms: {firearms.Count}");
            Debug.Log($"    Melee: {meleeWeapons.Count}");
            Debug.Log($"    Thrown: {thrown.Count}");
            
            // Test pattern matching
            var rifles = H3VRAssetLoader.GetWeaponsByPattern("rifle");
            var pistols = H3VRAssetLoader.GetWeaponsByPattern("pistol");
            
            Debug.Log($"    Rifles (pattern match): {rifles.Count}");
            Debug.Log($"    Pistols (pattern match): {pistols.Count}");
            
            // Test random weapon selection
            var randomWeapon = H3VRAssetLoader.GetRandomWeapon();
            Debug.Log($"  Random weapon test: {randomWeapon?.ItemID ?? "none available"}");
            
            if (allWeapons.Count > 0)
            {
                Debug.Log("  PASSED: Weapons loaded successfully");
            }
            else
            {
                Debug.LogWarning("  WARNING: No weapons loaded");
            }
        }
        
        /// <summary>
        /// Test sosig template loading
        /// </summary>
        private static void TestSosigTemplateLoading()
        {
            var templates = H3VRAssetLoader.GetAllSosigTemplates();
            Debug.Log($"  Found {templates.Count} sosig templates");
            
            if (templates.Count > 0)
            {
                for (int i = 0; i < Math.Min(3, templates.Count); i++)
                {
                    var template = templates[i];
                    Debug.Log($"    Template {i + 1}: {template?.name ?? "unnamed"}");
                }
                Debug.Log("  PASSED: Sosig templates loaded");
            }
            else
            {
                Debug.LogWarning("  WARNING: No sosig templates loaded");
            }
        }
        
        /// <summary>
        /// Test outfit config loading
        /// </summary>
        private static void TestOutfitConfigLoading()
        {
            var outfits = H3VRAssetLoader.GetAllOutfitConfigs();
            Debug.Log($"  Found {outfits.Count} outfit configurations");
            
            if (outfits.Count > 0)
            {
                for (int i = 0; i < Math.Min(3, outfits.Count); i++)
                {
                    var outfit = outfits[i];
                    Debug.Log($"    Outfit {i + 1}: {outfit?.name ?? "unnamed"}");
                    if (outfit != null)
                    {
                        Debug.Log($"      Headwear chance: {outfit.Chance_Headwear}");
                        Debug.Log($"      Torsowear chance: {outfit.Chance_Torsowear}");
                    }
                }
                Debug.Log("  PASSED: Outfit configs loaded");
            }
            else
            {
                Debug.LogWarning("  WARNING: No outfit configs loaded");
            }
        }
        
        /// <summary>
        /// Test advanced loadout creation
        /// </summary>
        private static void TestLoadoutCreation()
        {
            Debug.Log("  Testing H3VR asset-based loadout creation...");
            
            try
            {
                // Test basic asset availability for loadout creation
                var allWeapons = H3VRAssetLoader.GetAllWeapons();
                var armorCategories = H3VRAssetLoader.GetAllArmorCategories();
                var templates = H3VRAssetLoader.GetAllSosigTemplates();
                
                Debug.Log($"  Available weapons for loadouts: {allWeapons.Count}");
                Debug.Log($"  Available armor categories: {armorCategories.Count}");
                Debug.Log($"  Available sosig templates: {templates.Count}");
                
                // Test creating a simple loadout configuration
                var testOutfit = H3VRAssetLoader.CreateCustomOutfitFromAssets(
                    headwearChance: 0.7f,
                    torsowearChance: 0.9f,
                    pantswearChance: 0.8f,
                    backpackChance: 0.5f
                );
                
                if (testOutfit != null)
                {
                    Debug.Log("  Successfully created test outfit configuration");
                    Debug.Log($"    Headwear pieces: {testOutfit.Headwear?.Count ?? 0}");
                    Debug.Log($"    Torsowear pieces: {testOutfit.Torsowear?.Count ?? 0}");
                    Debug.Log($"    Pantswear pieces: {testOutfit.Pantswear?.Count ?? 0}");
                    Debug.Log($"    Backpack pieces: {testOutfit.Backpacks?.Count ?? 0}");
                }
                
                // Create a few different outfit types for testing
                var lightOutfit = H3VRAssetLoader.CreateCustomOutfitFromAssets(
                    headwearChance: 0.2f,
                    torsowearChance: 0.6f,
                    pantswearChance: 0.7f,
                    backpackChance: 0.1f
                );
                
                var heavyOutfit = H3VRAssetLoader.CreateCustomOutfitFromAssets(
                    headwearChance: 1.0f,
                    torsowearChance: 1.0f,
                    pantswearChance: 1.0f,
                    backpackChance: 0.8f
                );
                
                Debug.Log($"  Created light outfit (low armor): {lightOutfit != null}");
                Debug.Log($"  Created heavy outfit (high armor): {heavyOutfit != null}");
                
                if (allWeapons.Count > 0 && templates.Count > 0)
                {
                    Debug.Log("  PASSED: Loadout creation assets available");
                }
                else
                {
                    Debug.LogWarning("  WARNING: Limited assets available for loadout creation");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"  FAILED: Loadout creation test error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test custom outfit creation from H3VR assets
        /// </summary>
        private static void TestCustomOutfitCreation()
        {
            var customOutfit = H3VRAssetLoader.CreateCustomOutfitFromAssets(
                headwearChance: 0.8f,
                torsowearChance: 0.9f,
                backpackChance: 0.5f
            );
            
            if (customOutfit != null)
            {
                Debug.Log($"  Custom outfit created:");
                Debug.Log($"    Headwear items: {customOutfit.Headwear?.Count ?? 0}");
                Debug.Log($"    Torsowear items: {customOutfit.Torsowear?.Count ?? 0}");
                Debug.Log($"    Backpack items: {customOutfit.Backpacks?.Count ?? 0}");
                Debug.Log($"    Headwear chance: {customOutfit.Chance_Headwear}");
                Debug.Log($"    Torsowear chance: {customOutfit.Chance_Torsowear}");
                Debug.Log("  PASSED: Custom outfit creation successful");
            }
            else
            {
                Debug.LogError("  FAILED: Could not create custom outfit");
            }
        }
        
        /// <summary>
        /// Test sosig creation with H3VR assets (dry run without actual spawning)
        /// </summary>
        public static void TestSosigCreationDryRun()
        {
            Debug.Log("=== TESTING SOSIG CREATION (DRY RUN) ===");
            
            try
            {
                // Test asset availability for sosig creation
                var allWeapons = H3VRAssetLoader.GetAllWeapons();
                var templates = H3VRAssetLoader.GetAllSosigTemplates();
                var armorCategories = H3VRAssetLoader.GetAllArmorCategories();
                
                Debug.Log($"Available for sosig creation:");
                Debug.Log($"  Weapons: {allWeapons.Count}");
                Debug.Log($"  Templates: {templates.Count}");
                Debug.Log($"  Armor categories: {armorCategories.Count}");
                
                // Test weapon selection by category
                var firearms = H3VRAssetLoader.GetWeaponsByCategory(FVRObject.ObjectCategory.Firearm);
                var meleeWeapons = H3VRAssetLoader.GetWeaponsByCategory(FVRObject.ObjectCategory.MeleeWeapon);
                
                Debug.Log($"  Firearms: {firearms.Count}");
                Debug.Log($"  Melee weapons: {meleeWeapons.Count}");
                
                // Test random selections
                var randomWeapon = H3VRAssetLoader.GetRandomWeapon();
                var randomArmor = H3VRAssetLoader.GetRandomArmor("Torsowear");
                
                Debug.Log($"Random weapon test: {randomWeapon?.ItemID ?? "none"}");
                Debug.Log($"Random armor test: {randomArmor?.ItemID ?? "none"}");
                
                // Test outfit creation for sosigs
                var sosigOutfit = H3VRAssetLoader.CreateCustomOutfitFromAssets();
                if (sosigOutfit != null)
                {
                    Debug.Log("Test sosig outfit created successfully");
                    Debug.Log($"  Total armor pieces: {GetTotalArmorPieces(sosigOutfit)}");
                }
                
                bool hasSufficientAssets = allWeapons.Count > 0 && 
                                         (templates.Count > 0 || armorCategories.Count > 0);
                
                if (hasSufficientAssets)
                {
                    Debug.Log("PASSED: Sosig creation prerequisites met");
                }
                else
                {
                    Debug.LogWarning("WARNING: Insufficient assets for sosig creation");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"FAILED: Sosig creation dry run error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Helper method to count total armor pieces in an outfit
        /// </summary>
        private static int GetTotalArmorPieces(SosigOutfitConfig outfit)
        {
            if (outfit == null) return 0;
            
            int total = 0;
            total += outfit.Headwear?.Count ?? 0;
            total += outfit.Facewear?.Count ?? 0;
            total += outfit.Eyewear?.Count ?? 0;
            total += outfit.Torsowear?.Count ?? 0;
            total += outfit.Pantswear?.Count ?? 0;
            total += outfit.Pantswear_Lower?.Count ?? 0;
            total += outfit.Backpacks?.Count ?? 0;
            total += outfit.TorosDecoration?.Count ?? 0;
            
            return total;
        }
    }
}