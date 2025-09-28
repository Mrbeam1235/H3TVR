using System.Collections.Generic;
using UnityEngine;
using FistVR;
using System;
using System.Linq;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Advanced weapon management system for sosigs that works with their existing weapons
    /// Manages weapon swapping, ammo, attachments, and weapon states without spawning new weapons
    /// </summary>
    public class SosigWeaponManager : MonoBehaviour
    {
        private static ManualLogSource logger;
        
        [System.Serializable]
        public class SosigWeaponProfile
        {
            public string name;
            public float accuracy = 1.0f;
            public float damage = 1.0f;
            public float fireRate = 1.0f;
            public float range = 1.0f;
            public bool unlimitedAmmo = false;
            public bool enableAttachments = true;
            public float weaponQuality = 1.0f;
        }

        // Cache for sosig weapons found in the scene
        private static List<SosigWeapon> sceneWeapons = new List<SosigWeapon>();
        private static Dictionary<string, SosigWeaponProfile> weaponProfiles = new Dictionary<string, SosigWeaponProfile>();
        private static DateTime lastWeaponScan = DateTime.MinValue;
        private const int WEAPON_SCAN_INTERVAL_SECONDS = 5;

        public static void Initialize(ManualLogSource logSource)
        {
            logger = logSource;
            RefreshSceneWeapons();
            LoadDefaultWeaponProfiles();
        }

        /// <summary>
        /// Scan the scene for all sosig weapons
        /// </summary>
        private static void RefreshSceneWeapons()
        {
            try
            {
                sceneWeapons.Clear();
                
                // Find all SosigWeapon objects in the scene
                SosigWeapon[] allSosigWeapons = UnityEngine.Object.FindObjectsOfType<SosigWeapon>();
                sceneWeapons.AddRange(allSosigWeapons.Where(w => w != null));
                
                lastWeaponScan = DateTime.Now;
                
                if (logger != null)
                    logger.LogInfo($"Found {sceneWeapons.Count} sosig weapons in scene");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to refresh scene weapons: {ex.Message}");
            }
        }

        /// <summary>
        /// Load default weapon profiles for different sosig types
        /// </summary>
        private static void LoadDefaultWeaponProfiles()
        {
            try
            {
                // Standard Profile
                weaponProfiles["Standard"] = new SosigWeaponProfile
                {
                    name = "Standard",
                    accuracy = 1.0f,
                    damage = 1.0f,
                    fireRate = 1.0f,
                    range = 1.0f,
                    unlimitedAmmo = false,
                    enableAttachments = false,
                    weaponQuality = 1.0f
                };

                // Elite Profile
                weaponProfiles["Elite"] = new SosigWeaponProfile
                {
                    name = "Elite",
                    accuracy = 1.5f,
                    damage = 1.2f,
                    fireRate = 1.1f,
                    range = 1.3f,
                    unlimitedAmmo = false,
                    enableAttachments = true,
                    weaponQuality = 1.0f
                };

                // Veteran Profile
                weaponProfiles["Veteran"] = new SosigWeaponProfile
                {
                    name = "Veteran",
                    accuracy = 1.8f,
                    damage = 1.5f,
                    fireRate = 1.2f,
                    range = 1.5f,
                    unlimitedAmmo = true,
                    enableAttachments = true,
                    weaponQuality = 1.0f
                };

                // Rookie Profile
                weaponProfiles["Rookie"] = new SosigWeaponProfile
                {
                    name = "Rookie",
                    accuracy = 0.7f,
                    damage = 0.8f,
                    fireRate = 0.9f,
                    range = 0.8f,
                    unlimitedAmmo = false,
                    enableAttachments = false,
                    weaponQuality = 0.8f
                };

                if (logger != null)
                    logger.LogInfo($"Loaded {weaponProfiles.Count} default sosig weapon profiles");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to load default weapon profiles: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply a weapon profile to a sosig's current weapons
        /// </summary>
        /// <param name="sosig">The sosig to modify</param>
        /// <param name="profileName">Name of the profile to apply</param>
        public static void ApplyWeaponProfile(Sosig sosig, string profileName)
        {
            try
            {
                if (sosig == null)
                {
                    if (logger != null)
                        logger.LogWarning("Cannot apply weapon profile: sosig is null");
                    return;
                }

                if (!weaponProfiles.ContainsKey(profileName))
                {
                    if (logger != null)
                        logger.LogWarning($"Weapon profile '{profileName}' not found, using Standard profile");
                    profileName = "Standard";
                }

                if (!weaponProfiles.ContainsKey(profileName))
                {
                    if (logger != null)
                        logger.LogError("No weapon profiles available");
                    return;
                }

                var profile = weaponProfiles[profileName];
                ApplyProfileToSosigWeapons(sosig, profile);

                if (logger != null)
                    logger.LogInfo($"Applied '{profileName}' weapon profile to sosig");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to apply weapon profile '{profileName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Swap weapons between two sosigs
        /// </summary>
        /// <param name="sosig1">First sosig</param>
        /// <param name="sosig2">Second sosig</param>
        /// <param name="swapPrimary">Whether to swap primary weapons</param>
        /// <param name="swapSecondary">Whether to swap secondary weapons</param>
        public static void SwapSosigWeapons(Sosig sosig1, Sosig sosig2, bool swapPrimary = true, bool swapSecondary = true)
        {
            try
            {
                if (sosig1 == null || sosig2 == null)
                {
                    if (logger != null)
                        logger.LogWarning("Cannot swap weapons: one or both sosigs are null");
                    return;
                }

                // Get current weapons from both sosigs
                var sosig1Weapons = GetSosigWeapons(sosig1);
                var sosig2Weapons = GetSosigWeapons(sosig2);

                if (sosig1Weapons.Count == 0 && sosig2Weapons.Count == 0)
                {
                    if (logger != null) 
                        logger.LogInfo("No weapons to swap between sosigs");
                    return;
                }

                // Simple weapon swap - move weapons from sosig1 to sosig2 and vice versa
                if (sosig1Weapons.Count > 0 && sosig2Weapons.Count > 0)
                {
                    var weapon1 = sosig1Weapons[0];
                    var weapon2 = sosig2Weapons[0];

                    TransferWeaponBetweenSosigs(sosig1, sosig2, weapon1);
                    TransferWeaponBetweenSosigs(sosig2, sosig1, weapon2);
                }
                else if (sosig1Weapons.Count > 0)
                {
                    // Transfer weapon from sosig1 to sosig2
                    TransferWeaponBetweenSosigs(sosig1, sosig2, sosig1Weapons[0]);
                }
                else if (sosig2Weapons.Count > 0)
                {
                    // Transfer weapon from sosig2 to sosig1
                    TransferWeaponBetweenSosigs(sosig2, sosig1, sosig2Weapons[0]);
                }

                if (logger != null)
                    logger.LogInfo($"Swapped weapons between sosigs");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to swap sosig weapons: {ex.Message}");
            }
        }

        /// <summary>
        /// Give a sosig unlimited ammo for their current weapons
        /// </summary>
        /// <param name="sosig">The sosig to modify</param>
        public static void GiveUnlimitedAmmo(Sosig sosig)
        {
            try
            {
                if (sosig == null) return;

                var weapons = GetSosigWeapons(sosig);
                foreach (var weapon in weapons)
                {
                    if (weapon != null)
                    {
                        SetWeaponUnlimitedAmmo(weapon, true);
                    }
                }

                if (logger != null)
                    logger.LogInfo($"Gave sosig unlimited ammo for all weapons");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to give unlimited ammo: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove all weapons from a sosig
        /// </summary>
        /// <param name="sosig">The sosig to disarm</param>
        public static void DisarmSosig(Sosig sosig)
        {
            try
            {
                if (sosig == null) return;

                var weapons = GetSosigWeapons(sosig);
                foreach (var weapon in weapons)
                {
                    if (weapon != null)
                    {
                        // Drop the weapon
                        DropSosigWeapon(sosig, weapon);
                    }
                }

                if (logger != null)
                    logger.LogInfo($"Disarmed sosig of all weapons");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to disarm sosig: {ex.Message}");
            }
        }

        /// <summary>
        /// Redistribute weapons among all sosigs in the scene
        /// </summary>
        public static void RandomlyRedistributeWeapons()
        {
            try
            {
                // Refresh weapon cache if needed
                if (ShouldRefreshWeaponCache())
                {
                    RefreshSceneWeapons();
                }

                // Get all sosigs in scene
                Sosig[] allSosigs = UnityEngine.Object.FindObjectsOfType<Sosig>();
                if (allSosigs.Length < 2)
                {
                    if (logger != null)
                        logger.LogInfo("Not enough sosigs in scene for weapon redistribution");
                    return;
                }

                // Collect all weapons from all sosigs
                List<SosigWeapon> allWeapons = new List<SosigWeapon>();
                foreach (var sosig in allSosigs)
                {
                    allWeapons.AddRange(GetSosigWeapons(sosig));
                    DisarmSosig(sosig);
                }

                // Randomly redistribute weapons
                foreach (var sosig in allSosigs)
                {
                    if (allWeapons.Count == 0) break;

                    // Give each sosig 1-2 random weapons
                    int weaponsToGive = UnityEngine.Random.Range(1, 3);
                    for (int i = 0; i < weaponsToGive && allWeapons.Count > 0; i++)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, allWeapons.Count);
                        var weapon = allWeapons[randomIndex];
                        allWeapons.RemoveAt(randomIndex);

                        // Equip weapon to sosig
                        if (weapon != null)
                        {
                            GiveWeaponToSosig(sosig, weapon);
                        }
                    }
                }

                if (logger != null)
                    logger.LogInfo($"Redistributed weapons among {allSosigs.Length} sosigs");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to redistribute weapons: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply weapon profile settings to all sosig weapons
        /// </summary>
        private static void ApplyProfileToSosigWeapons(Sosig sosig, SosigWeaponProfile profile)
        {
            try
            {
                var weapons = GetSosigWeapons(sosig);
                foreach (var weapon in weapons)
                {
                    if (weapon != null)
                    {
                        ApplyProfileToWeapon(weapon, profile);
                    }
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to apply profile to sosig weapons: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply profile settings to a specific weapon
        /// </summary>
        private static void ApplyProfileToWeapon(SosigWeapon weapon, SosigWeaponProfile profile)
        {
            try
            {
                // Apply unlimited ammo if specified
                if (profile.unlimitedAmmo)
                {
                    SetWeaponUnlimitedAmmo(weapon, true);
                }

                // Try to modify weapon properties through reflection
                try
                {
                    var weaponType = weapon.GetType();
                    
                    // Try to find and modify accuracy field
                    var accuracyField = weaponType.GetField("Accuracy") ?? weaponType.GetField("accuracy");
                    if (accuracyField != null && accuracyField.FieldType == typeof(float))
                    {
                        float currentAccuracy = (float)accuracyField.GetValue(weapon);
                        accuracyField.SetValue(weapon, currentAccuracy * profile.accuracy);
                    }

                    // Try to find and modify damage field
                    var damageField = weaponType.GetField("Damage") ?? weaponType.GetField("damage");
                    if (damageField != null && damageField.FieldType == typeof(float))
                    {
                        float currentDamage = (float)damageField.GetValue(weapon);
                        damageField.SetValue(weapon, currentDamage * profile.damage);
                    }
                }
                catch (Exception reflectionEx)
                {
                    if (logger != null)
                        logger.LogWarning($"Could not modify weapon properties via reflection: {reflectionEx.Message}");
                }

                if (logger != null)
                    logger.LogInfo($"Applied profile '{profile.name}' to weapon");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogWarning($"Failed to apply profile to weapon: {ex.Message}");
            }
        }

        /// <summary>
        /// Set unlimited ammo for a sosig weapon
        /// </summary>
        private static void SetWeaponUnlimitedAmmo(SosigWeapon weapon, bool unlimited)
        {
            try
            {
                var weaponType = weapon.GetType();
                
                // Try various field names for unlimited ammo
                string[] ammoFieldNames = { 
                    "hasInfiniteAmmo", "HasInfiniteAmmo", "m_hasInfiniteAmmo", 
                    "infiniteAmmo", "InfiniteAmmo", "m_infiniteAmmo",
                    "unlimitedAmmo", "UnlimitedAmmo", "m_unlimitedAmmo"
                };

                foreach (var fieldName in ammoFieldNames)
                {
                    var field = weaponType.GetField(fieldName);
                    if (field != null && field.FieldType == typeof(bool))
                    {
                        field.SetValue(weapon, unlimited);
                        if (logger != null)
                            logger.LogInfo($"Set unlimited ammo to {unlimited} for sosig weapon via field {fieldName}");
                        return;
                    }
                }

                if (logger != null)
                    logger.LogWarning("Could not find unlimited ammo field for sosig weapon");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogWarning($"Failed to set unlimited ammo: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all weapons from a sosig
        /// </summary>
        private static List<SosigWeapon> GetSosigWeapons(Sosig sosig)
        {
            var weapons = new List<SosigWeapon>();
            try
            {
                if (sosig == null) return weapons;

                // Try to find weapons through different methods
                
                // Method 1: Check if sosig has Inventory property
                try
                {
                    var inventoryProperty = sosig.GetType().GetProperty("Inventory");
                    if (inventoryProperty != null)
                    {
                        var inventory = inventoryProperty.GetValue(sosig, null);
                        if (inventory != null)
                        {
                            // Try to get slots from inventory
                            var slotsProperty = inventory.GetType().GetProperty("Slots");
                            if (slotsProperty != null)
                            {
                                var slots = slotsProperty.GetValue(inventory, null) as System.Collections.IList;
                                if (slots != null)
                                {
                                    foreach (var slot in slots)
                                    {
                                        if (slot != null)
                                        {
                                            var heldObjectProperty = slot.GetType().GetProperty("HeldObject");
                                            if (heldObjectProperty != null)
                                            {
                                                var heldObject = heldObjectProperty.GetValue(slot, null) as SosigWeapon;
                                                if (heldObject != null)
                                                {
                                                    weapons.Add(heldObject);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Inventory method failed, try other approaches
                }

                // Method 2: Search for SosigWeapons in sosig's children
                if (weapons.Count == 0)
                {
                    var childWeapons = sosig.GetComponentsInChildren<SosigWeapon>();
                    weapons.AddRange(childWeapons);
                }

                // Method 3: Check if sosig has hands or links with weapons
                if (weapons.Count == 0 && sosig.Links != null)
                {
                    foreach (var link in sosig.Links)
                    {
                        if (link != null)
                        {
                            var linkWeapons = link.GetComponentsInChildren<SosigWeapon>();
                            weapons.AddRange(linkWeapons);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogWarning($"Failed to get sosig weapons: {ex.Message}");
            }
            return weapons;
        }

        /// <summary>
        /// Transfer a weapon from one sosig to another
        /// </summary>
        private static void TransferWeaponBetweenSosigs(Sosig fromSosig, Sosig toSosig, SosigWeapon weapon)
        {
            try
            {
                // Remove weapon from source sosig
                DropSosigWeapon(fromSosig, weapon);

                // Give weapon to target sosig
                GiveWeaponToSosig(toSosig, weapon);
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to transfer weapon between sosigs: {ex.Message}");
            }
        }

        /// <summary>
        /// Drop a weapon from a sosig
        /// </summary>
        private static void DropSosigWeapon(Sosig sosig, SosigWeapon weapon)
        {
            try
            {
                if (sosig == null || weapon == null) return;

                // Detach weapon from sosig
                weapon.transform.SetParent(null);

                // Enable physics for the dropped weapon
                var rb = weapon.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // Position weapon near sosig's feet
                weapon.transform.position = sosig.transform.position + Vector3.down * 0.5f;
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to drop sosig weapon: {ex.Message}");
            }
        }

        /// <summary>
        /// Give a weapon to a sosig
        /// </summary>
        private static void GiveWeaponToSosig(Sosig sosig, SosigWeapon weapon)
        {
            try
            {
                if (sosig == null || weapon == null) return;

                // Try to use sosig's ForceEquip method if available
                try
                {
                    var forceEquipMethod = sosig.GetType().GetMethod("ForceEquip");
                    if (forceEquipMethod != null)
                    {
                        forceEquipMethod.Invoke(sosig, new object[] { weapon });
                        if (logger != null)
                            logger.LogInfo("Used ForceEquip method to give weapon to sosig");
                        return;
                    }
                }
                catch (Exception)
                {
                    // ForceEquip method not available or failed
                }

                // Fallback - attach weapon to sosig's hand or body
                if (sosig.Links != null && sosig.Links.Count > 1)
                {
                    weapon.transform.SetParent(sosig.Links[1].transform);
                    weapon.transform.localPosition = Vector3.zero;
                    weapon.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    weapon.transform.position = sosig.transform.position + Vector3.up * 1.0f;
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to give weapon to sosig: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all available weapon profile names
        /// </summary>
        public static List<string> GetAvailableProfiles()
        {
            return weaponProfiles.Keys.ToList();
        }

        /// <summary>
        /// Add a custom weapon profile
        /// </summary>
        public static void AddWeaponProfile(string name, SosigWeaponProfile profile)
        {
            weaponProfiles[name] = profile;
            if (logger != null)
                logger.LogInfo($"Added custom weapon profile: {name}");
        }

        /// <summary>
        /// Get count of weapons in scene
        /// </summary>
        public static int GetSceneWeaponCount()
        {
            if (ShouldRefreshWeaponCache())
            {
                RefreshSceneWeapons();
            }
            return sceneWeapons.Count;
        }

        /// <summary>
        /// Get all scene weapons
        /// </summary>
        public static List<SosigWeapon> GetAllSceneWeapons()
        {
            if (ShouldRefreshWeaponCache())
            {
                RefreshSceneWeapons();
            }
            return new List<SosigWeapon>(sceneWeapons);
        }

        #region Utility Methods
        private static bool ShouldRefreshWeaponCache()
        {
            return (DateTime.Now - lastWeaponScan).TotalSeconds > WEAPON_SCAN_INTERVAL_SECONDS;
        }

        /// <summary>
        /// Get statistics about sosig weapons in the scene
        /// </summary>
        public static string GetWeaponStatistics()
        {
            try
            {
                if (ShouldRefreshWeaponCache())
                {
                    RefreshSceneWeapons();
                }

                int totalWeapons = sceneWeapons.Count;
                int equippedWeapons = sceneWeapons.Count(w => w.transform.parent != null);
                int droppedWeapons = totalWeapons - equippedWeapons;

                return $"Scene Weapons - Total: {totalWeapons}, Equipped: {equippedWeapons}, Dropped: {droppedWeapons}";
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to get weapon statistics: {ex.Message}");
                return "Failed to get weapon statistics";
            }
        }

        /// <summary>
        /// Clean up orphaned weapons in the scene
        /// </summary>
        public static void CleanupOrphanedWeapons()
        {
            try
            {
                if (ShouldRefreshWeaponCache())
                {
                    RefreshSceneWeapons();
                }

                int cleanedCount = 0;
                foreach (var weapon in sceneWeapons.ToList())
                {
                    if (weapon == null)
                    {
                        sceneWeapons.Remove(weapon);
                        cleanedCount++;
                    }
                    else if (weapon.transform.parent == null && Vector3.Distance(weapon.transform.position, Vector3.zero) > 1000f)
                    {
                        // Remove weapons that are too far from the play area
                        UnityEngine.Object.Destroy(weapon.gameObject);
                        sceneWeapons.Remove(weapon);
                        cleanedCount++;
                    }
                }

                if (logger != null)
                    logger.LogInfo($"Cleaned up {cleanedCount} orphaned weapons");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to cleanup orphaned weapons: {ex.Message}");
            }
        }
        #endregion
    }

    /// <summary>
    /// Enum for sosig weapon slots
    /// </summary>
    public enum SosigWeaponSlot
    {
        Primary = 0,
        Secondary = 1,
        Tertiary = 2
    }
}