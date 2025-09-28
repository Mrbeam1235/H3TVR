using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FistVR;
using BepInEx;

namespace H3TVR
{
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
                
                // Enhanced armor detection patterns for better coverage
                if (IsArmorPiece(objectId, obj))
                {
                    CategorizeArmorPiece(objectId, obj);
                }
            }

            int totalArmor = armorCategories.Values.Sum(list => list.Count);
            Debug.Log($"[H3VRAssetLoader] Loaded {totalArmor} armor pieces across {armorCategories.Count} categories");
        }

        /// <summary>
        /// Check if an object is an armor piece based on patterns and object properties
        /// </summary>
        private static bool IsArmorPiece(string objectId, FVRObject obj)
        {
            // Check for armor-related keywords in the object ID
            string[] armorKeywords = {
                "helmet", "hat", "cap", "beret", "headgear", "crown", "head", "skull",
                "mask", "face", "visor", "bandana", "scarf", "breath", "goggles", "glasses",
                "vest", "armor", "chest", "plate", "torso", "jacket", "shirt", "uniform", "body",
                "pants", "trousers", "leg", "kneepads", "thigh", "shorts", "shin", "calf",
                "backpack", "pack", "bag", "satchel", "rucksack", "pouch",
                "badge", "patch", "pin", "decoration", "medal", "emblem", "gear"
            };

            foreach (string keyword in armorKeywords)
            {
                if (objectId.Contains(keyword))
                    return true;
            }

            // Additional checks based on object category for miscellaneous items that could be armor
            if (objectId.Contains("wear") || objectId.Contains("equip") || objectId.Contains("cloth"))
                return true;

            return false;
        }

        /// <summary>
        /// Categorize armor piece into appropriate category
        /// </summary>
        private static void CategorizeArmorPiece(string objectId, FVRObject obj)
        {
            // Headwear detection (expanded patterns)
            if (objectId.Contains("helmet") || objectId.Contains("hat") || objectId.Contains("cap") || 
                objectId.Contains("beret") || objectId.Contains("headgear") || objectId.Contains("crown") ||
                objectId.Contains("head") || objectId.Contains("skull") || objectId.Contains("hood") ||
                objectId.Contains("beanie") || objectId.Contains("bandeau"))
            {
                armorCategories["Headwear"].Add(obj);
            }
            // Facewear detection (expanded patterns)
            else if (objectId.Contains("mask") || objectId.Contains("face") || objectId.Contains("visor") ||
                     objectId.Contains("bandana") || objectId.Contains("scarf") || objectId.Contains("breath") ||
                     objectId.Contains("balaclava") || objectId.Contains("gaiter") || objectId.Contains("mouthpiece"))
            {
                armorCategories["Facewear"].Add(obj);
            }
            // Eyewear detection (expanded patterns)
            else if (objectId.Contains("glasses") || objectId.Contains("goggles") || objectId.Contains("spectacles") ||
                     objectId.Contains("shades") || objectId.Contains("eyewear") || objectId.Contains("lens") ||
                     objectId.Contains("monocle") || objectId.Contains("sight") || objectId.Contains("scope"))
            {
                armorCategories["Eyewear"].Add(obj);
            }
            // Torsowear detection (expanded patterns)
            else if (objectId.Contains("vest") || objectId.Contains("armor") || objectId.Contains("chest") ||
                     objectId.Contains("plate") || objectId.Contains("torso") || objectId.Contains("jacket") ||
                     objectId.Contains("shirt") || objectId.Contains("uniform") || objectId.Contains("body") ||
                     objectId.Contains("coat") || objectId.Contains("sweater") || objectId.Contains("tunic") ||
                     objectId.Contains("blazer") || objectId.Contains("cardigan"))
            {
                armorCategories["Torsowear"].Add(obj);
            }
            // Pantswear detection (expanded patterns)
            else if (objectId.Contains("pants") || objectId.Contains("trousers" ) || objectId.Contains("leg") ||
                     objectId.Contains("kneepads") || objectId.Contains("thigh") || objectId.Contains("shorts") ||
                     objectId.Contains("jean") || objectId.Contains("chino") || objectId.Contains("cargo"))
            {
                if (objectId.Contains("lower") || objectId.Contains("shin") || objectId.Contains("calf") ||
                    objectId.Contains("ankle") || objectId.Contains("boot"))
                {
                    armorCategories["PantswearLower"].Add(obj);
                }
                else
                {
                    armorCategories["Pantswear"].Add(obj);
                }
            }
            // Backpack detection (expanded patterns)
            else if (objectId.Contains("backpack") || objectId.Contains("pack") || objectId.Contains("bag") ||
                     objectId.Contains("satchel") || objectId.Contains("rucksack") || objectId.Contains("pouch") ||
                     objectId.Contains("knapsack") || objectId.Contains("haversack") || objectId.Contains("carryall"))
            {
                armorCategories["Backpacks"].Add(obj);
            }
            // Decorations detection (expanded patterns)
            else if (objectId.Contains("badge") || objectId.Contains("patch") || objectId.Contains("pin") ||
                     objectId.Contains("decoration") || objectId.Contains("medal") || objectId.Contains("emblem") ||
                     objectId.Contains("insignia") || objectId.Contains("chevron") || objectId.Contains("stripe"))
            {
                armorCategories["Decorations"].Add(obj);
            }
            else
            {
                // If we can't categorize it specifically, add to the most general category based on context
                armorCategories["Decorations"].Add(obj);
            }
        }

        /// <summary>
        /// Load all weapons from the H3VR ItemManager with enhanced filtering
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

                // Primary weapon categories
                if (obj.Category == FVRObject.ObjectCategory.Firearm ||
                    obj.Category == FVRObject.ObjectCategory.MeleeWeapon ||
                    obj.Category == FVRObject.ObjectCategory.Thrown)
                {
                    allWeapons.Add(obj);
                }
                // Additional weapon-like items that might be useful
                else if (obj.Category == FVRObject.ObjectCategory.Explosive ||
                         obj.Category == FVRObject.ObjectCategory.Tool)
                {
                    string objectId = kvp.Key.ToLower();
                    if (IsWeaponLikeItem(objectId))
                    {
                        allWeapons.Add(obj);
                    }
                }
            }

            Debug.Log($"[H3VRAssetLoader] Loaded {allWeapons.Count} weapons");
        }

        /// <summary>
        /// Check if a non-weapon category item could be used as a weapon
        /// </summary>
        private static bool IsWeaponLikeItem(string objectId)
        {
            string[] weaponKeywords = {
                "knife", "blade", "sword", "axe", "hammer", "club", "bat", "stick", "rod",
                "grenade", "bomb", "explosive", "mine", "rocket", "launcher", "dart", "arrow",
                "throwing", "projectile", "sling", "whip", "chain", "flail"
            };

            foreach (string keyword in weaponKeywords)
            {
                if (objectId.Contains(keyword))
                    return true;
            }

            return false;
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

            // Create default runtime templates if none found
            if (gameTemplates.Count == 0)
            {
                Debug.Log("[H3VRAssetLoader] No existing templates found, creating default runtime templates...");
                CreateDefaultRuntimeTemplates();
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
        public static List<FVRObject> GetWeaponsByPattern(String pattern)
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
        /// Check if H3VR systems are ready
        /// </summary>
        public static bool IsH3VRSystemReady()
        {
            return IsInitialized && IM.OD != null && IM.OD.Count > 0;
        }

        /// <summary>
        /// Get loading statistics
        /// </summary>
        public static string GetLoadingStats()
        {
            if (!isInitialized)
                return "H3VR Asset Loader not initialized";
            
            return $"Assets Loaded - Armor: {armorCategories.Values.Sum(list => list.Count)}, " +
                   $"Weapons: {allWeapons.Count}, " +
                   $"Attachments: {allAttachments.Count}, " +
                   $"Templates: {gameTemplates.Count}, " +
                   $"Outfits: {gameOutfits.Count}";
        }

        /// <summary>
        /// Get available armor sets (simplified for compatibility)
        /// </summary>
        public static List<string> GetAvailableArmor()
        {
            if (!isInitialized) Initialize();
            return armorCategories.Keys.ToList();
        }

        /// <summary>
        /// Get available weapons (simplified for compatibility)
        /// </summary>
        public static List<string> GetAvailableWeapons()
        {
            if (!isInitialized) Initialize();
            return allWeapons.Select(w => w.ItemID).ToList();
        }
        
        /// <summary>
        /// Create default runtime templates when no files are available
        /// </summary>
        private static void CreateDefaultRuntimeTemplates()
        {
            try
            {
                var templateConfigs = new[]
                {
                    new { Name = "Runtime_Light_Infantry", Type = "Light" },
                    new { Name = "Runtime_Heavy_Infantry", Type = "Heavy" },
                    new { Name = "Runtime_Special_Ops", Type = "Operator" },
                    new { Name = "Runtime_Standard_Enemy", Type = "Default" }
                };
                
                foreach (var config in templateConfigs)
                {
                    var template = ScriptableObject.CreateInstance<SosigEnemyTemplate>();
                    template.name = config.Name;
                    
                    // Initialize all required lists
                    template.SosigPrefabs = new List<FVRObject>();
                    template.ConfigTemplates = new List<SosigConfigTemplate>();
                    template.WeaponOptions = new List<FVRObject>();
                    template.WeaponOptions_Secondary = new List<FVRObject>();
                    template.WeaponOptions_Tertiary = new List<FVRObject>();
                    template.OutfitConfig = new List<SosigOutfitConfig>();
                    
                    // Add weapons based on template type
                    switch (config.Type)
                    {
                        case "Light":
                            AddWeaponsToTemplate(template, new[] { "pistol", "smg" });
                            break;
                        case "Heavy":
                            AddWeaponsToTemplate(template, new[] { "rifle", "lmg", "shotgun" });
                            break;
                        case "Operator":
                            AddWeaponsToTemplate(template, new[] { "rifle", "sniper", "pistol" });
                            break;
                        default:
                            AddWeaponsToTemplate(template, new[] { "rifle", "pistol" });
                            break;
                    }
                    
                    gameTemplates.Add(template);
                    Debug.Log($"[H3VRAssetLoader] Created default runtime template: {template.name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[H3VRAssetLoader] Failed to create default runtime templates: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Add weapons to template based on weapon patterns
        /// </summary>
        private static void AddWeaponsToTemplate(SosigEnemyTemplate template, string[] weaponPatterns)
        {
            if (IM.OD == null) return;
            
            try
            {
                foreach (var pattern in weaponPatterns)
                {
                    var matchingWeapons = GetWeaponsByPattern(pattern);
                    
                    if (matchingWeapons.Count > 0)
                    {
                        // Add up to 3 weapons per pattern to avoid overcrowding
                        int weaponsToAdd = Math.Min(3, matchingWeapons.Count);
                        for (int i = 0; i < weaponsToAdd; i++)
                        {
                            var weapon = matchingWeapons[UnityEngine.Random.Range(0, matchingWeapons.Count)];
                            if (!template.WeaponOptions.Contains(weapon))
                            {
                                template.WeaponOptions.Add(weapon);
                            }
                        }
                    }
                }
                
                // Ensure template has at least one weapon option
                if (template.WeaponOptions.Count == 0 && allWeapons.Count > 0)
                {
                    var randomWeapon = allWeapons[UnityEngine.Random.Range(0, allWeapons.Count)];
                    template.WeaponOptions.Add(randomWeapon);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[H3VRAssetLoader] Error adding weapons to template {template.name}: {ex.Message}");
            }
        }
    }
}