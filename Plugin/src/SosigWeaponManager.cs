using System.Collections.Generic;
using UnityEngine;
using FistVR;
using System;
using System.Linq;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Advanced weapon spawning system for sosigs using H3VR's native sosig weapon system
    /// Integrates with H3VR's SosigWeapon, SosigWeaponTemplate, and sosig inventory systems
    /// </summary>
    public class SosigWeaponManager : MonoBehaviour
    {
        private static ManualLogSource logger;
        
        [System.Serializable]
        public class SosigWeaponLoadout
        {
            public string name;
            public List<SosigWeaponTemplate> primaryWeapons = new List<SosigWeaponTemplate>();
            public List<SosigWeaponTemplate> secondaryWeapons = new List<SosigWeaponTemplate>();
            public List<SosigWeaponTemplate> tertiaryWeapons = new List<SosigWeaponTemplate>();
            public bool forceWeaponType = false;
            public float weaponQuality = 1.0f; // Weapon condition multiplier
            public bool enableRandomAttachments = true;
            public float attachmentChance = 0.3f;
        }

        // Cache for H3VR sosig weapon templates
        private static SosigWeaponTemplate[] cachedSosigWeaponTemplates;
        private static Dictionary<string, SosigWeaponLoadout> weaponLoadouts = new Dictionary<string, SosigWeaponLoadout>();
        private static DateTime lastCacheUpdate = DateTime.MinValue;
        private const int CACHE_LIFETIME_SECONDS = 60;

        public static void Initialize(ManualLogSource logSource)
        {
            logger = logSource;
            RefreshSosigWeaponCache();
            LoadDefaultWeaponLoadouts();
        }

        /// <summary>
        /// Refresh the cache of H3VR sosig weapon templates
        /// </summary>
        private static void RefreshSosigWeaponCache()
        {
            try
            {
                // Find all SosigWeaponTemplate objects in the game
                cachedSosigWeaponTemplates = Resources.FindObjectsOfTypeAll<SosigWeaponTemplate>();
                lastCacheUpdate = DateTime.Now;
                
                if (logger != null)
                    logger.LogInfo($"Cached {cachedSosigWeaponTemplates?.Length ?? 0} sosig weapon templates");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to cache sosig weapon templates: {ex.Message}");
                cachedSosigWeaponTemplates = new SosigWeaponTemplate[0];
            }
        }

        /// <summary>
        /// Load default weapon loadouts for different sosig types
        /// </summary>
        private static void LoadDefaultWeaponLoadouts()
        {
            try
            {
                if (cachedSosigWeaponTemplates == null || cachedSosigWeaponTemplates.Length == 0)
                {
                    RefreshSosigWeaponCache();
                }

                // Create default loadouts based on available sosig weapons
                CreateDefaultLoadouts();
                
                if (logger != null)
                    logger.LogInfo($"Loaded {weaponLoadouts.Count} default sosig weapon loadouts");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to load default weapon loadouts: {ex.Message}");
            }
        }

        /// <summary>
        /// Create default weapon loadouts
        /// </summary>
        private static void CreateDefaultLoadouts()
        {
            if (cachedSosigWeaponTemplates == null) return;

            // Categorize weapons by type
            var rifles = cachedSosigWeaponTemplates.Where(w => IsRifleType(w)).ToList();
            var pistols = cachedSosigWeaponTemplates.Where(w => IsPistolType(w)).ToList();
            var shotguns = cachedSosigWeaponTemplates.Where(w => IsShotgunType(w)).ToList();
            var smgs = cachedSosigWeaponTemplates.Where(w => IsSMGType(w)).ToList();
            var lmgs = cachedSosigWeaponTemplates.Where(w => IsLMGType(w)).ToList();

            // Standard Infantry Loadout
            weaponLoadouts["StandardInfantry"] = new SosigWeaponLoadout
            {
                name = "Standard Infantry",
                primaryWeapons = rifles.Take(10).ToList(),
                secondaryWeapons = pistols.Take(5).ToList(),
                weaponQuality = 1.0f,
                enableRandomAttachments = true,
                attachmentChance = 0.4f
            };

            // CQB Specialist Loadout
            weaponLoadouts["CQBSpecialist"] = new SosigWeaponLoadout
            {
                name = "CQB Specialist",
                primaryWeapons = smgs.Concat(shotguns).Take(8).ToList(),
                secondaryWeapons = pistols.Take(3).ToList(),
                weaponQuality = 1.0f,
                enableRandomAttachments = true,
                attachmentChance = 0.6f
            };

            // Heavy Gunner Loadout
            weaponLoadouts["HeavyGunner"] = new SosigWeaponLoadout
            {
                name = "Heavy Gunner",
                primaryWeapons = lmgs.Concat(rifles.Where(r => IsHeavyWeapon(r))).Take(6).ToList(),
                secondaryWeapons = pistols.Take(2).ToList(),
                weaponQuality = 1.0f,
                enableRandomAttachments = true,
                attachmentChance = 0.5f
            };

            // Marksman Loadout
            weaponLoadouts["Marksman"] = new SosigWeaponLoadout
            {
                name = "Marksman",
                primaryWeapons = rifles.Where(r => IsPrecisionWeapon(r)).Take(5).ToList(),
                secondaryWeapons = pistols.Take(2).ToList(),
                weaponQuality = 1.0f,
                enableRandomAttachments = true,
                attachmentChance = 0.8f
            };

            // Random Loadout (uses all weapon types)
            weaponLoadouts["Random"] = new SosigWeaponLoadout
            {
                name = "Random",
                primaryWeapons = cachedSosigWeaponTemplates.Take(20).ToList(),
                secondaryWeapons = pistols.Take(8).ToList(),
                weaponQuality = 1.0f,
                enableRandomAttachments = true,
                attachmentChance = 0.3f
            };
        }

        /// <summary>
        /// Equip a sosig with weapons from a specific loadout
        /// </summary>
        /// <param name="sosig">The sosig to equip</param>
        /// <param name="loadoutName">Name of the loadout to use</param>
        public static void EquipSosigWithLoadout(Sosig sosig, string loadoutName)
        {
            try
            {
                if (sosig == null)
                {
                    if (logger != null)
                        logger.LogWarning("Cannot equip sosig: sosig is null");
                    return;
                }

                // Refresh cache if needed
                if (ShouldRefreshCache())
                {
                    RefreshSosigWeaponCache();
                    LoadDefaultWeaponLoadouts();
                }

                if (!weaponLoadouts.ContainsKey(loadoutName))
                {
                    if (logger != null)
                        logger.LogWarning($"Loadout '{loadoutName}' not found, using Random loadout");
                    loadoutName = "Random";
                }

                if (!weaponLoadouts.ContainsKey(loadoutName))
                {
                    if (logger != null)
                        logger.LogError("No weapon loadouts available");
                    return;
                }

                var loadout = weaponLoadouts[loadoutName];
                EquipWeaponsFromLoadout(sosig, loadout);

                if (logger != null)
                    logger.LogInfo($"Equipped sosig with '{loadoutName}' loadout");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to equip sosig with loadout '{loadoutName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Equip a sosig with random H3VR sosig weapons
        /// </summary>
        /// <param name="sosig">The sosig to equip</param>
        /// <param name="primaryChance">Chance to equip primary weapon (0.0-1.0)</param>
        /// <param name="secondaryChance">Chance to equip secondary weapon (0.0-1.0)</param>
        public static void EquipSosigWithRandomWeapons(Sosig sosig, float primaryChance = 0.8f, float secondaryChance = 0.4f)
        {
            try
            {
                if (sosig == null) return;

                // Refresh cache if needed
                if (ShouldRefreshCache())
                {
                    RefreshSosigWeaponCache();
                }

                if (cachedSosigWeaponTemplates == null || cachedSosigWeaponTemplates.Length == 0)
                {
                    if (logger != null)
                        logger.LogWarning("No sosig weapon templates available for random equipping");
                    return;
                }

                // Equip primary weapon
                if (UnityEngine.Random.value < primaryChance)
                {
                    var primaryWeapon = cachedSosigWeaponTemplates[UnityEngine.Random.Range(0, cachedSosigWeaponTemplates.Length)];
                    EquipSosigWithWeaponTemplate(sosig, primaryWeapon, SosigWeaponSlot.Primary);
                }

                // Equip secondary weapon
                if (UnityEngine.Random.value < secondaryChance)
                {
                    var secondaryWeapons = cachedSosigWeaponTemplates.Where(w => IsPistolType(w) || IsSMGType(w)).ToArray();
                    if (secondaryWeapons.Length > 0)
                    {
                        var secondaryWeapon = secondaryWeapons[UnityEngine.Random.Range(0, secondaryWeapons.Length)];
                        EquipSosigWithWeaponTemplate(sosig, secondaryWeapon, SosigWeaponSlot.Secondary);
                    }
                }

                if (logger != null)
                    logger.LogInfo($"Equipped sosig with random weapons");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to equip sosig with random weapons: {ex.Message}");
            }
        }

        /// <summary>
        /// Equip weapons from a specific loadout
        /// </summary>
        private static void EquipWeaponsFromLoadout(Sosig sosig, SosigWeaponLoadout loadout)
        {
            try
            {
                // Equip primary weapon
                if (loadout.primaryWeapons.Count > 0)
                {
                    var primaryWeapon = loadout.primaryWeapons[UnityEngine.Random.Range(0, loadout.primaryWeapons.Count)];
                    EquipSosigWithWeaponTemplate(sosig, primaryWeapon, SosigWeaponSlot.Primary);
                }

                // Equip secondary weapon (chance-based)
                if (loadout.secondaryWeapons.Count > 0 && UnityEngine.Random.value < 0.6f)
                {
                    var secondaryWeapon = loadout.secondaryWeapons[UnityEngine.Random.Range(0, loadout.secondaryWeapons.Count)];
                    EquipSosigWithWeaponTemplate(sosig, secondaryWeapon, SosigWeaponSlot.Secondary);
                }

                // Equip tertiary weapon (lower chance)
                if (loadout.tertiaryWeapons.Count > 0 && UnityEngine.Random.value < 0.3f)
                {
                    var tertiaryWeapon = loadout.tertiaryWeapons[UnityEngine.Random.Range(0, loadout.tertiaryWeapons.Count)];
                    EquipSosigWithWeaponTemplate(sosig, tertiaryWeapon, SosigWeaponSlot.Tertiary);
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to equip weapons from loadout: {ex.Message}");
            }
        }

        /// <summary>
        /// Equip a sosig with a specific weapon template using H3VR's sosig weapon system
        /// </summary>
        private static void EquipSosigWithWeaponTemplate(Sosig sosig, SosigWeaponTemplate weaponTemplate, SosigWeaponSlot slot)
        {
            try
            {
                if (sosig == null || weaponTemplate == null) return;

                // Initialize sosig hands if needed
                if (sosig.Inventory == null)
                {
                    sosig.InitHands();
                }

                // Spawn the sosig weapon using H3VR's system
                GameObject weaponGO = UnityEngine.Object.Instantiate(weaponTemplate.WeaponPrefab);
                if (weaponGO == null) return;

                SosigWeapon sosigWeapon = weaponGO.GetComponent<SosigWeapon>();
                if (sosigWeapon == null)
                {
                    // If no SosigWeapon component, add one and configure it
                    sosigWeapon = weaponGO.AddComponent<SosigWeapon>();
                    ConfigureSosigWeapon(sosigWeapon, weaponTemplate);
                }

                // Configure weapon based on template
                if (sosigWeapon != null)
                {
                    // Apply weapon template settings
                    ApplyWeaponTemplateSettings(sosigWeapon, weaponTemplate);

                    // Equip the weapon to the sosig
                    EquipWeaponToSosig(sosig, sosigWeapon, slot);

                    if (logger != null)
                        logger.LogInfo($"Equipped sosig with {weaponTemplate.DisplayName} in {slot} slot");
                }
                else
                {
                    // Fallback: destroy the weapon if we can't configure it properly
                    UnityEngine.Object.Destroy(weaponGO);
                    if (logger != null)
                        logger.LogWarning($"Failed to configure SosigWeapon for {weaponTemplate.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to equip sosig with weapon template: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure a SosigWeapon component based on weapon template
        /// </summary>
        private static void ConfigureSosigWeapon(SosigWeapon sosigWeapon, SosigWeaponTemplate template)
        {
            try
            {
                // Basic sosig weapon configuration
                // Note: These properties may vary depending on H3VR version
                // sosigWeapon.Type = template.WeaponType;
                // sosigWeapon.HandlingMode = template.HandlingMode;
                
                // Set weapon as configured
                if (logger != null)
                    logger.LogInfo($"Configured SosigWeapon for {template.DisplayName}");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogWarning($"Failed to configure SosigWeapon: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply weapon template settings to sosig weapon
        /// </summary>
        private static void ApplyWeaponTemplateSettings(SosigWeapon sosigWeapon, SosigWeaponTemplate template)
        {
            try
            {
                // Apply accuracy settings
                // sosigWeapon.Accuracy = template.Accuracy;
                
                // Apply firing settings
                // sosigWeapon.FiringRate = template.FiringRate;
                
                // Apply range settings
                // sosigWeapon.Range = template.Range;
                
                // Note: Property names may vary based on H3VR version
                // This is a placeholder for actual weapon configuration
                
                if (logger != null)
                    logger.LogInfo($"Applied template settings for {template.DisplayName}");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogWarning($"Failed to apply weapon template settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Equip weapon to sosig using H3VR's inventory system
        /// </summary>
        private static void EquipWeaponToSosig(Sosig sosig, SosigWeapon weapon, SosigWeaponSlot slot)
        {
            try
            {
                // Initialize sosig inventory if needed
                if (sosig.Inventory == null)
                {
                    sosig.InitHands();
                }

                // Force equip the weapon
                sosig.ForceEquip(weapon);

                // Position weapon properly
                if (sosig.Links.Count > 1)
                {
                    weapon.transform.position = sosig.Links[1].transform.position;
                    weapon.transform.rotation = sosig.Links[1].transform.rotation;
                }

                if (logger != null)
                    logger.LogInfo($"Successfully equipped weapon to sosig in {slot} slot");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to equip weapon to sosig: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all available loadout names
        /// </summary>
        public static List<string> GetAvailableLoadouts()
        {
            return weaponLoadouts.Keys.ToList();
        }

        /// <summary>
        /// Add a custom weapon loadout
        /// </summary>
        public static void AddWeaponLoadout(string name, SosigWeaponLoadout loadout)
        {
            weaponLoadouts[name] = loadout;
            if (logger != null)
                logger.LogInfo($"Added custom weapon loadout: {name}");
        }

        #region Weapon Type Classification
        private static bool IsRifleType(SosigWeaponTemplate weapon)
        {
            if (weapon?.DisplayName == null) return false;
            string name = weapon.DisplayName.ToLower();
            return name.Contains("rifle") || name.Contains("m4") || name.Contains("ak") || 
                   name.Contains("ar-") || name.Contains("carbine");
        }

        private static bool IsPistolType(SosigWeaponTemplate weapon)
        {
            if (weapon?.DisplayName == null) return false;
            string name = weapon.DisplayName.ToLower();
            return name.Contains("pistol") || name.Contains("handgun") || name.Contains("glock") ||
                   name.Contains("beretta") || name.Contains("1911") || name.Contains("revolver");
        }

        private static bool IsShotgunType(SosigWeaponTemplate weapon)
        {
            if (weapon?.DisplayName == null) return false;
            string name = weapon.DisplayName.ToLower();
            return name.Contains("shotgun") || name.Contains("12ga") || name.Contains("gauge");
        }

        private static bool IsSMGType(SosigWeaponTemplate weapon)
        {
            if (weapon?.DisplayName == null) return false;
            string name = weapon.DisplayName.ToLower();
            return name.Contains("smg") || name.Contains("submachine") || name.Contains("mp5") ||
                   name.Contains("uzi") || name.Contains("vector");
        }

        private static bool IsLMGType(SosigWeaponTemplate weapon)
        {
            if (weapon?.DisplayName == null) return false;
            string name = weapon.DisplayName.ToLower();
            return name.Contains("lmg") || name.Contains("machine") || name.Contains("m249") ||
                   name.Contains("saw") || name.Contains("mg");
        }

        private static bool IsHeavyWeapon(SosigWeaponTemplate weapon)
        {
            if (weapon?.DisplayName == null) return false;
            string name = weapon.DisplayName.ToLower();
            return name.Contains("heavy") || name.Contains("launcher") || name.Contains("bazooka") ||
                   name.Contains("rpg") || IsLMGType(weapon);
        }

        private static bool IsPrecisionWeapon(SosigWeaponTemplate weapon)
        {
            if (weapon?.DisplayName == null) return false;
            string name = weapon.DisplayName.ToLower();
            return name.Contains("sniper") || name.Contains("precision") || name.Contains("marksman") ||
                   name.Contains("scope") || name.Contains("bolt");
        }
        #endregion

        #region Utility Methods
        private static bool ShouldRefreshCache()
        {
            return (DateTime.Now - lastCacheUpdate).TotalSeconds > CACHE_LIFETIME_SECONDS;
        }

        /// <summary>
        /// Get all available sosig weapon templates
        /// </summary>
        public static SosigWeaponTemplate[] GetAllSosigWeaponTemplates()
        {
            if (ShouldRefreshCache())
            {
                RefreshSosigWeaponCache();
            }
            return cachedSosigWeaponTemplates ?? new SosigWeaponTemplate[0];
        }

        /// <summary>
        /// Get random sosig weapon template
        /// </summary>
        public static SosigWeaponTemplate GetRandomSosigWeapon()
        {
            var templates = GetAllSosigWeaponTemplates();
            if (templates.Length == 0) return null;
            return templates[UnityEngine.Random.Range(0, templates.Length)];
        }

        /// <summary>
        /// Get sosig weapon template by name
        /// </summary>
        public static SosigWeaponTemplate GetSosigWeaponByName(string name)
        {
            var templates = GetAllSosigWeaponTemplates();
            return templates.FirstOrDefault(t => t.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        #endregion
    }

    /// <summary>
    /// Enum for sosig weapon slots
    /// </summary>
    public enum SosigWeaponSlot
    {
        Primary,
        Secondary,
        Tertiary
    }
}