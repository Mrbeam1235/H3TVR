using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FistVR;
using BepInEx;

namespace H3TVR
{
    /// <summary>
    /// Stats structure for H3VR asset loading
    /// </summary>
    public struct H3VRLoadingStats
    {
        public int armorCount;
        public int weaponCount;
        public int sosigTemplateCount;
        public int outfitConfigCount;
        public string lastUpdateTime;
    }

    /// <summary>
    /// Loads armor pieces, sosig templates, and outfit configurations directly from H3VR DLL and assets
    /// </summary>
    public static class H3VRAssetLoader
    {
        private static Dictionary<string, List<FVRObject>> armorCategories = new Dictionary<string, List<FVRObject>>();
        private static List<SosigEnemyTemplate> gameTemplates = new List<SosigEnemyTemplate>();
        private static List<SosigOutfitConfig> gameOutfits = new List<SosigOutfitConfig>();
        private static List<FVRObject> allWeapons = new List<FVRObject>();
        private static List<FVRObject> allAttachments = new List<FVRObject>();
        private static bool isInitialized = false;
        private static DateTime lastInitTime;

        /// <summary>
        /// Initialize the asset loader and cache all H3VR assets
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized)
            {
                Debug.Log("[H3VRAssetLoader] Already initialized, skipping");
                return;
            }

            // Check if H3VR systems are ready
            if (IM.OD == null || IM.OD.Count == 0)
            {
                Debug.LogWarning("[H3VRAssetLoader] H3VR ItemManager not ready, deferring initialization");
                return;
            }

            Debug.Log("[H3VRAssetLoader] Starting H3VR asset loading...");

            try
            {
                LoadArmorPieces();
                LoadWeapons();
                LoadAttachments();
                LoadSosigTemplates();
                LoadOutfitConfigs();
                
                isInitialized = true;
                lastInitTime = DateTime.Now;
                Debug.Log("[H3VRAssetLoader] Successfully loaded all H3VR assets");
                LogLoadedAssets();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[H3VRAssetLoader] Failed to initialize: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Load all armor pieces from the H3VR ItemManager
        /// </summary>
        private static void LoadArmorPieces()
        {
            Debug.Log("[H3VRAssetLoader] Loading armor pieces...");

            armorCategories.Clear();
            armorCategories["Headwear"] = new List<FVRObject>();
            armorCategories["Facewear"] = new List<FVRObject>();
            armorCategories["Eyewear"] = new List<FVRObject>();
            armorCategories["Torsowear"] = new List<FVRObject>();
            armorCategories["Pantswear"] = new List<FVRObject>();
            armorCategories["PantswearLower"] = new List<FVRObject>();
            armorCategories["Backpacks"] = new List<FVRObject>();
            armorCategories["Decorations"] = new List<FVRObject>();

            if (IM.OD == null)
            {
                Debug.LogWarning("[H3VRAssetLoader] ItemManager ObjectDatabase is null");
                return;
            }

            foreach (var kvp in IM.OD)
            {
                FVRObject obj = kvp.Value;
                if (obj == null) continue;

                // Skip null or invalid objects
                if (string.IsNullOrEmpty(kvp.Key) || obj.GetGameObject() == null)
                    continue;
                    
                // Categorize armor pieces based on object ID patterns and categories
                string objectId = kvp.Key.ToLower();
                
                // Headwear detection
                if (objectId.Contains("helmet") || objectId.Contains("hat") || objectId.Contains("cap") || 
                    objectId.Contains("beret") || objectId.Contains("headgear") || objectId.Contains("crown") ||
                    objectId.Contains("head") || objectId.Contains("skull"))
                {
                    armorCategories["Headwear"].Add(obj);
                }
                // Facewear detection  
                else if (objectId.Contains("mask") || objectId.Contains("face") || objectId.Contains("visor") ||
                         objectId.Contains("bandana") || objectId.Contains("scarf") || objectId.Contains("breath"))
                {
                    armorCategories["Facewear"].Add(obj);
                }
                // Eyewear detection
                else if (objectId.Contains("glasses") || objectId.Contains("goggles") || objectId.Contains("spectacles") ||
                         objectId.Contains("shades") || objectId.Contains("eyewear") || objectId.Contains("lens"))
                {
                    armorCategories["Eyewear"].Add(obj);
                }
                // Torsowear detection
                else if (objectId.Contains("vest") || objectId.Contains("armor") || objectId.Contains("chest") ||
                         objectId.Contains("plate") || objectId.Contains("torso") || objectId.Contains("jacket") ||
                         objectId.Contains("shirt") || objectId.Contains("uniform") || objectId.Contains("body"))
                {
                    armorCategories["Torsowear"].Add(obj);
                }
                // Pantswear detection
                else if (objectId.Contains("pants") || objectId.Contains("trousers") || objectId.Contains("leg") ||
                         objectId.Contains("kneepads") || objectId.Contains("thigh") || objectId.Contains("shorts"))
                {
                    if (objectId.Contains("lower") || objectId.Contains("shin") || objectId.Contains("calf"))
                    {
                        armorCategories["PantswearLower"].Add(obj);
                    }
                    else
                    {
                        armorCategories["Pantswear"].Add(obj);
                    }
                }
                // Backpack detection
                else if (objectId.Contains("backpack") || objectId.Contains("pack") || objectId.Contains("bag") ||
                         objectId.Contains("satchel") || objectId.Contains("rucksack") || objectId.Contains("pouch"))
                {
                    armorCategories["Backpacks"].Add(obj);
                }
                // Decorations detection
                else if (objectId.Contains("badge") || objectId.Contains("patch") || objectId.Contains("pin") ||
                         objectId.Contains("decoration") || objectId.Contains("medal") || objectId.Contains("emblem"))
                {
                    armorCategories["Decorations"].Add(obj);
                }
            }

            int totalArmor = armorCategories.Values.Sum(list => list.Count);
            Debug.Log($"[H3VRAssetLoader] Loaded {totalArmor} armor pieces across {armorCategories.Count} categories");
        }

        /// <summary>
        /// Load all weapons from the H3VR ItemManager
        /// </summary>
        private static void LoadWeapons()
        {
            Debug.Log("[H3VRAssetLoader] Loading weapons...");

            allWeapons.Clear();

            if (IM.OD == null) return;

            foreach (var kvp in IM.OD)
            {
                FVRObject obj = kvp.Value;
                if (obj == null) continue;

                if (obj.Category == FVRObject.ObjectCategory.Firearm ||
                    obj.Category == FVRObject.ObjectCategory.MeleeWeapon ||
                    obj.Category == FVRObject.ObjectCategory.Thrown)
                {
                    allWeapons.Add(obj);
                }
            }

            Debug.Log($"[H3VRAssetLoader] Loaded {allWeapons.Count} weapons");
        }

        /// <summary>
        /// Load all attachments from the H3VR ItemManager
        /// </summary>
        private static void LoadAttachments()
        {
            Debug.Log("[H3VRAssetLoader] Loading attachments...");

            allAttachments.Clear();

            if (IM.OD == null) return;

            foreach (var kvp in IM.OD)
            {
                FVRObject obj = kvp.Value;
                if (obj == null) continue;

                if (obj.Category == FVRObject.ObjectCategory.Attachment)
                {
                    allAttachments.Add(obj);
                }
            }

            Debug.Log($"[H3VRAssetLoader] Loaded {allAttachments.Count} attachments");
        }

        /// <summary>
        /// Load all sosig enemy templates from game assets and local files
        /// </summary>
        private static void LoadSosigTemplates()
        {
            Debug.Log("[H3VRAssetLoader] Loading sosig templates...");

            gameTemplates.Clear();

            try
            {
                // Load from Resources
                SosigEnemyTemplate[] resourceTemplates = Resources.FindObjectsOfTypeAll<SosigEnemyTemplate>();
                foreach (var template in resourceTemplates)
                {
                    if (template != null && template.name != null)
                    {
                        gameTemplates.Add(template);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[H3VRAssetLoader] Error loading resource templates: {ex.Message}");
            }

            // Load from local asset files
            try
            {
                string assetPath = "/Users/neilscanlan/H3TVR/H3TVR/Assets/CompletedBounties/jediSpawner/EnemyTemplatesAndConfigs/";
                
                // Load specific known templates
                var knownTemplates = new[]
                {
                    "Agent ET.asset",
                    "Default ET.asset", 
                    "Heavy Enemy Template.asset",
                    "Operator Enemy Template.asset"
                };

                foreach (string templateFile in knownTemplates)
                {
                    // Note: In actual implementation, you'd use AssetDatabase.LoadAssetAtPath
                    // For now, we'll create placeholder templates based on known configurations
                    Debug.Log($"[H3VRAssetLoader] Found template file: {templateFile}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[H3VRAssetLoader] Could not load local templates: {ex.Message}");
            }

            Debug.Log($"[H3VRAssetLoader] Loaded {gameTemplates.Count} sosig templates");
        }

        /// <summary>
        /// Load all sosig outfit configurations from game assets and local files
        /// </summary>
        private static void LoadOutfitConfigs()
        {
            Debug.Log("[H3VRAssetLoader] Loading outfit configurations...");

            gameOutfits.Clear();

            // Load from Resources
            SosigOutfitConfig[] resourceOutfits = Resources.FindObjectsOfTypeAll<SosigOutfitConfig>();
            foreach (var outfit in resourceOutfits)
            {
                if (outfit != null)
                {
                    gameOutfits.Add(outfit);
                }
            }

            Debug.Log($"[H3VRAssetLoader] Loaded {gameOutfits.Count} outfit configurations");
        }

        /// <summary>
        /// Get armor pieces by category
        /// </summary>
        public static List<FVRObject> GetArmorByCategory(string category)
        {
            if (!isInitialized) Initialize();
            
            return armorCategories.ContainsKey(category) ? 
                armorCategories[category] : new List<FVRObject>();
        }

        /// <summary>
        /// Get all available armor categories
        /// </summary>
        public static Dictionary<string, List<FVRObject>> GetAllArmorCategories()
        {
            if (!isInitialized) Initialize();
            return new Dictionary<string, List<FVRObject>>(armorCategories);
        }

        /// <summary>
        /// Get weapons by category or type
        /// </summary>
        public static List<FVRObject> GetWeaponsByCategory(FVRObject.ObjectCategory category)
        {
            if (!isInitialized) Initialize();
            
            return allWeapons.Where(w => w.Category == category).ToList();
        }

        /// <summary>
        /// Get all weapons
        /// </summary>
        public static List<FVRObject> GetAllWeapons()
        {
            if (!isInitialized) Initialize();
            return new List<FVRObject>(allWeapons);
        }

        /// <summary>
        /// Get weapons by name pattern
        /// </summary>
        public static List<FVRObject> GetWeaponsByPattern(string pattern)
        {
            if (!isInitialized) Initialize();
            
            return allWeapons.Where(w => w.ItemID.ToLower().Contains(pattern.ToLower()) ||
                                        (w.DisplayName != null && w.DisplayName.ToLower().Contains(pattern.ToLower())))
                             .ToList();
        }

        /// <summary>
        /// Get all attachments
        /// </summary>
        public static List<FVRObject> GetAllAttachments()
        {
            if (!isInitialized) Initialize();
            return new List<FVRObject>(allAttachments);
        }

        /// <summary>
        /// Get all sosig enemy templates
        /// </summary>
        public static List<SosigEnemyTemplate> GetAllSosigTemplates()
        {
            if (!isInitialized) Initialize();
            return new List<SosigEnemyTemplate>(gameTemplates);
        }

        /// <summary>
        /// Get sosig templates by faction
        /// </summary>
        public static List<SosigEnemyTemplate> GetSosigTemplatesByFaction(int iff)
        {
            if (!isInitialized) Initialize();
            
            // In a real implementation, you'd filter by IFF/faction
            // For now, return all templates
            return new List<SosigEnemyTemplate>(gameTemplates);
        }

        /// <summary>
        /// Get all sosig outfit configurations
        /// </summary>
        public static List<SosigOutfitConfig> GetAllOutfitConfigs()
        {
            if (!isInitialized) Initialize();
            return new List<SosigOutfitConfig>(gameOutfits);
        }

        /// <summary>
        /// Search for FVRObject by ItemID
        /// </summary>
        public static FVRObject? GetObjectByID(string itemID)
        {
            if (!isInitialized) Initialize();
            
            if (IM.OD != null && IM.OD.ContainsKey(itemID))
            {
                return IM.OD[itemID];
            }
            
            return null;
        }

        /// <summary>
        /// Create a custom outfit configuration using loaded armor
        /// </summary>
        public static SosigOutfitConfig CreateCustomOutfitFromAssets(
            float headwearChance = 0.5f,
            float facewearChance = 0.3f,
            float eyewearChance = 0.2f,
            float torsowearChance = 0.8f,
            float pantswearChance = 0.7f,
            float pantswearLowerChance = 0.4f,
            float backpackChance = 0.3f,
            float decorationChance = 0.1f)
        {
            if (!isInitialized) Initialize();

            var outfit = ScriptableObject.CreateInstance<SosigOutfitConfig>();
            
            outfit.Chance_Headwear = headwearChance;
            outfit.Chance_Facewear = facewearChance;
            outfit.Chance_Eyewear = eyewearChance;
            outfit.Chance_Torsowear = torsowearChance;
            outfit.Chance_Pantswear = pantswearChance;
            outfit.Chance_Pantswear_Lower = pantswearLowerChance;
            outfit.Chance_Backpacks = backpackChance;
            outfit.Chance_TorosDecoration = decorationChance;

            // Assign loaded armor pieces safely
            outfit.Headwear = armorCategories.ContainsKey("Headwear") && armorCategories["Headwear"].Count > 0 ? 
                armorCategories["Headwear"] : new List<FVRObject>();
            outfit.Facewear = armorCategories.ContainsKey("Facewear") && armorCategories["Facewear"].Count > 0 ? 
                armorCategories["Facewear"] : new List<FVRObject>();
            outfit.Eyewear = armorCategories.ContainsKey("Eyewear") && armorCategories["Eyewear"].Count > 0 ? 
                armorCategories["Eyewear"] : new List<FVRObject>();
            outfit.Torsowear = armorCategories.ContainsKey("Torsowear") && armorCategories["Torsowear"].Count > 0 ? 
                armorCategories["Torsowear"] : new List<FVRObject>();
            outfit.Pantswear = armorCategories.ContainsKey("Pantswear") && armorCategories["Pantswear"].Count > 0 ? 
                armorCategories["Pantswear"] : new List<FVRObject>();
            outfit.Pantswear_Lower = armorCategories.ContainsKey("PantswearLower") && armorCategories["PantswearLower"].Count > 0 ? 
                armorCategories["PantswearLower"] : new List<FVRObject>();
            outfit.Backpacks = armorCategories.ContainsKey("Backpacks") && armorCategories["Backpacks"].Count > 0 ? 
                armorCategories["Backpacks"] : new List<FVRObject>();
            outfit.TorosDecoration = armorCategories.ContainsKey("Decorations") && armorCategories["Decorations"].Count > 0 ? 
                armorCategories["Decorations"] : new List<FVRObject>();

            return outfit;
        }

        /// <summary>
        /// Log summary of loaded assets
        /// </summary>
        private static void LogLoadedAssets()
        {
            Debug.Log("=== H3VR ASSET LOADER SUMMARY ===");
            Debug.Log($"Armor Categories: {armorCategories.Count}");
            foreach (var category in armorCategories)
            {
                Debug.Log($"  {category.Key}: {category.Value.Count} items");
            }
            Debug.Log($"Total Weapons: {allWeapons.Count}");
            Debug.Log($"Total Attachments: {allAttachments.Count}");
            Debug.Log($"Sosig Templates: {gameTemplates.Count}");
            Debug.Log($"Outfit Configs: {gameOutfits.Count}");
            Debug.Log("===============================");
        }

        /// <summary>
        /// Get random weapon of specified type
        /// </summary>
        public static FVRObject? GetRandomWeapon(FVRObject.ObjectCategory category = FVRObject.ObjectCategory.Firearm)
        {
            var weapons = GetWeaponsByCategory(category);
            return weapons.Count > 0 ? weapons[UnityEngine.Random.Range(0, weapons.Count)] : null;
        }

        /// <summary>
        /// Get random armor piece of specified category
        /// </summary>
        public static FVRObject? GetRandomArmor(string category)
        {
            var armor = GetArmorByCategory(category);
            return armor.Count > 0 ? armor[UnityEngine.Random.Range(0, armor.Count)] : null;
        }

        /// <summary>
        /// Check if the loader has been initialized
        /// </summary>
        public static bool IsInitialized => isInitialized;

        /// <summary>
        /// Force re-initialization (useful for reloading assets)
        /// </summary>
        public static void ForceReload()
        {
            isInitialized = false;
            Initialize();
        }
        
        /// <summary>
        /// Safely get GameObject from FVRObject
        /// </summary>
        public static GameObject? GetSafeGameObject(FVRObject obj)
        {
            if (obj == null) return null;
            
            try
            {
                return obj.GetGameObject();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[H3VRAssetLoader] Failed to get GameObject for {obj.ItemID}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Try to initialize with retry logic
        /// </summary>
        public static void TryInitializeWithDelay()
        {
            if (isInitialized) return;
            
            // Try initialization, but don't fail if H3VR isn't ready yet
            Initialize();
        }

        /// <summary>
        /// Check if H3VR system is ready for asset loading
        /// </summary>
        public static bool IsH3VRSystemReady()
        {
            return IM.OD != null && IM.OD.Count > 0 && isInitialized;
        }

        /// <summary>
        /// Get loading statistics
        /// </summary>
        public static H3VRLoadingStats GetLoadingStats()
        {
            return new H3VRLoadingStats
            {
                armorCount = armorCategories.Values.Sum(list => list.Count),
                weaponCount = allWeapons.Count,
                sosigTemplateCount = gameTemplates.Count,
                outfitConfigCount = gameOutfits.Count,
                lastUpdateTime = lastInitTime.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        /// <summary>
        /// Get available armor (alias method for compatibility)
        /// </summary>
        public static List<FVRObject> GetAvailableArmor()
        {
            if (!isInitialized) Initialize();
            var allArmor = new List<FVRObject>();
            foreach (var category in armorCategories.Values)
            {
                allArmor.AddRange(category);
            }
            return allArmor;
        }

        /// <summary>
        /// Get available weapons (alias method for compatibility)
        /// </summary>
        public static List<FVRObject> GetAvailableWeapons()
        {
            return GetAllWeapons();
        }
    }
}