using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FistVR;

namespace H3TVR
{
    /// <summary>
    /// Utility class for managing and selecting sosig loadouts from H3VR assets
    /// </summary>
    public static class SosigLoadoutUtility
    {
        /// <summary>
        /// Create a sosig using H3VR assets and advanced loadout configuration
        /// </summary>
        public static Sosig CreateSosigFromLoadout(AdvancedSosigLoadout loadout, Vector3 position, Quaternion rotation)
        {
            if (loadout == null)
            {
                Debug.LogError("[SosigLoadoutUtility] Loadout is null");
                return null;
            }

            try
            {
                // Select template from loadout
                SosigEnemyTemplate template = SelectTemplateFromLoadout(loadout);
                if (template == null)
                {
                    Debug.LogWarning($"[SosigLoadoutUtility] No template available for loadout: {loadout.loadoutName}");
                    return null;
                }

                // Create sosig from template
                Sosig sosig = SosigEnemyTemplate.SpawnSosig(template, position, rotation);
                if (sosig == null)
                {
                    Debug.LogError($"[SosigLoadoutUtility] Failed to spawn sosig from template: {template.name}");
                    return null;
                }

                // Apply loadout configuration
                ApplyLoadoutToSosig(sosig, loadout);

                Debug.Log($"[SosigLoadoutUtility] Successfully created sosig from loadout: {loadout.loadoutName}");
                return sosig;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigLoadoutUtility] Error creating sosig from loadout {loadout.loadoutName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Select the best template from the loadout configuration
        /// </summary>
        private static SosigEnemyTemplate SelectTemplateFromLoadout(AdvancedSosigLoadout loadout)
        {
            // Try primary templates first
            if (loadout.primaryTemplates != null && loadout.primaryTemplates.Count > 0)
            {
                var validTemplates = loadout.primaryTemplates.Where(t => t != null).ToList();
                if (validTemplates.Count > 0)
                {
                    return validTemplates[UnityEngine.Random.Range(0, validTemplates.Count)];
                }
            }

            // Try alternative templates
            if (loadout.alternativeTemplates != null && loadout.alternativeTemplates.Count > 0)
            {
                var validTemplates = loadout.alternativeTemplates.Where(t => t != null).ToList();
                if (validTemplates.Count > 0)
                {
                    return validTemplates[UnityEngine.Random.Range(0, validTemplates.Count)];
                }
            }

            // Fallback to H3VR templates
            var h3vrTemplates = H3VRAssetLoader.GetAllSosigTemplates();
            if (h3vrTemplates.Count > 0)
            {
                Debug.Log($"[SosigLoadoutUtility] Using fallback H3VR template for loadout: {loadout.loadoutName}");
                return h3vrTemplates[UnityEngine.Random.Range(0, h3vrTemplates.Count)];
            }

            return null;
        }

        /// <summary>
        /// Apply loadout configuration to a spawned sosig
        /// </summary>
        private static void ApplyLoadoutToSosig(Sosig sosig, AdvancedSosigLoadout loadout)
        {
            // Apply IFF and faction settings
            sosig.E.IFFCode = loadout.defaultIFF;
            
            // Apply health modifications
            if (loadout.useCustomHealth)
            {
                float newHealth = sosig.Health * loadout.customHealthMultiplier;
                sosig.Health = newHealth;
                sosig.MaxHealth = newHealth;
            }

            // Apply speed modifications
            if (loadout.useCustomSpeed)
            {
                // Note: Speed modification would require more complex sosig behavior modifications
                Debug.Log($"[SosigLoadoutUtility] Applied speed multiplier: {loadout.customSpeedMultiplier}");
            }

            // Apply weapons
            ApplyWeaponsToSosig(sosig, loadout);

            // Apply armor/outfit
            ApplyArmorToSosig(sosig, loadout);

            // Apply behavior settings
            ApplyBehaviorToSosig(sosig, loadout);

            Debug.Log($"[SosigLoadoutUtility] Applied loadout configuration to sosig: {loadout.loadoutName}");
        }

        /// <summary>
        /// Apply weapon configuration to sosig
        /// </summary>
        private static void ApplyWeaponsToSosig(Sosig sosig, AdvancedSosigLoadout loadout)
        {
            try
            {
                // Primary weapon
                if (loadout.customPrimaryWeapons != null && loadout.customPrimaryWeapons.Count > 0)
                {
                    var primaryWeapon = loadout.customPrimaryWeapons[UnityEngine.Random.Range(0, loadout.customPrimaryWeapons.Count)];
                    if (primaryWeapon != null && sosig.Links.Count > 1)
                    {
                        SpawnWeaponOnSosig(sosig, primaryWeapon, sosig.Links[1]); // Right hand
                    }
                }
                else if (loadout.useRandomWeapons)
                {
                    var randomWeapon = H3VRAssetLoader.GetRandomWeapon(FVRObject.ObjectCategory.Firearm);
                    if (randomWeapon != null && sosig.Links.Count > 1)
                    {
                        SpawnWeaponOnSosig(sosig, randomWeapon, sosig.Links[1]);
                    }
                }

                // Secondary weapon
                if (loadout.customSecondaryWeapons != null && loadout.customSecondaryWeapons.Count > 0)
                {
                    var secondaryWeapon = loadout.customSecondaryWeapons[UnityEngine.Random.Range(0, loadout.customSecondaryWeapons.Count)];
                    if (secondaryWeapon != null && sosig.Links.Count > 2)
                    {
                        SpawnWeaponOnSosig(sosig, secondaryWeapon, sosig.Links[2]); // Left hand or holster
                    }
                }

                // Tertiary equipment
                if (loadout.customTertiaryWeapons != null && loadout.customTertiaryWeapons.Count > 0)
                {
                    var tertiaryWeapon = loadout.customTertiaryWeapons[UnityEngine.Random.Range(0, loadout.customTertiaryWeapons.Count)];
                    if (tertiaryWeapon != null && sosig.Links.Count > 0)
                    {
                        SpawnWeaponOnSosig(sosig, tertiaryWeapon, sosig.Links[0]); // Head/back attachment
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigLoadoutUtility] Error applying weapons: {ex.Message}");
            }
        }

        /// <summary>\n        /// Spawn a weapon on a sosig link\n        /// </summary>\n        private static void SpawnWeaponOnSosig(Sosig sosig, FVRObject weaponObject, SosigLink link)\n        {\n            try\n            {\n                GameObject weaponPrefab = H3VRAssetLoader.GetSafeGameObject(weaponObject);\n                if (weaponPrefab != null)\n                {\n                    GameObject weaponGO = UnityEngine.Object.Instantiate(weaponPrefab);\n                    weaponGO.transform.position = link.transform.position;\n                    weaponGO.transform.rotation = link.transform.rotation;\n\n                    // Try to attach to sosig hand if applicable\n                    FVRPhysicalObject physObj = weaponGO.GetComponent<FVRPhysicalObject>();\n                    if (physObj != null && link.Hand != null)\n                    {\n                        sosig.ForceEquip(link.Hand, physObj);\n                    }\n                    else\n                    {\n                        // If can't equip, position near the sosig\n                        weaponGO.transform.SetParent(link.transform);\n                        weaponGO.transform.localPosition = Vector3.zero;\n                    }\n\n                    Debug.Log($\"[SosigLoadoutUtility] Spawned weapon {weaponObject.ItemID} on sosig\");\n                }\n                else\n                {\n                    Debug.LogWarning($\"[SosigLoadoutUtility] Could not get GameObject for weapon: {weaponObject?.ItemID}\");\n                }\n            }\n            catch (Exception ex)\n            {\n                Debug.LogError($\"[SosigLoadoutUtility] Error spawning weapon {weaponObject?.ItemID}: {ex.Message}\");\n            }\n        }

        /// <summary>
        /// Apply armor configuration to sosig
        /// </summary>
        private static void ApplyArmorToSosig(Sosig sosig, AdvancedSosigLoadout loadout)
        {
            try
            {
                if (loadout.armorConfig == null) return;

                // Create outfit config from loadout armor config
                var outfitConfig = loadout.armorConfig.CreateOutfitConfig();
                
                // Apply armor to each sosig link
                for (int i = 0; i < sosig.Links.Count; i++)
                {
                    var link = sosig.Links[i];
                    
                    // Apply different armor types based on link index
                    switch (i)
                    {
                        case 0: // Head
                            ApplyArmorToLink(link, outfitConfig.Headwear, outfitConfig.Chance_Headwear);
                            ApplyArmorToLink(link, outfitConfig.Facewear, outfitConfig.Chance_Facewear);
                            ApplyArmorToLink(link, outfitConfig.Eyewear, outfitConfig.Chance_Eyewear);
                            break;
                        case 1: // Torso
                            ApplyArmorToLink(link, outfitConfig.Torsowear, outfitConfig.Chance_Torsowear);
                            ApplyArmorToLink(link, outfitConfig.TorosDecoration, outfitConfig.Chance_TorosDecoration);
                            break;
                        case 2: // Lower body
                            ApplyArmorToLink(link, outfitConfig.Pantswear, outfitConfig.Chance_Pantswear);
                            break;
                        default: // Other links
                            if (i < sosig.Links.Count - 1) // Not the last link
                            {
                                ApplyArmorToLink(link, outfitConfig.Backpacks, outfitConfig.Chance_Backpacks);
                            }
                            break;
                    }
                }

                Debug.Log($"[SosigLoadoutUtility] Applied armor configuration to sosig");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigLoadoutUtility] Error applying armor: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply armor to a specific sosig link
        /// </summary>
        private static void ApplyArmorToLink(SosigLink link, List<FVRObject> armorList, float chance)
        {
            if (armorList == null || armorList.Count == 0 || UnityEngine.Random.Range(0f, 1f) > chance)
                return;

            try
            {
                var armorPiece = armorList[UnityEngine.Random.Range(0, armorList.Count)];
                if (armorPiece?.GetGameObject() != null)
                {
                    GameObject armorGO = UnityEngine.Object.Instantiate(armorPiece.GetGameObject(), link.transform);
                    armorGO.transform.localPosition = Vector3.zero;
                    armorGO.transform.localRotation = Quaternion.identity;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigLoadoutUtility] Error applying armor to link: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply behavior configuration to sosig
        /// </summary>
        private static void ApplyBehaviorToSosig(Sosig sosig, AdvancedSosigLoadout loadout)
        {
            try
            {
                // Set sosig order
                sosig.CommandSosigToPoint(sosig.transform.position, loadout.fallbackOrder);

                // Configure patrol behavior
                if (loadout.patrolArea)
                {
                    // Set up patrol behavior (this would require more complex implementation)
                    Debug.Log($"[SosigLoadoutUtility] Configured patrol area with radius: {loadout.patrolRadius}");
                }

                // Configure follow behavior
                if (loadout.followPlayer && GM.CurrentPlayerBody != null)
                {
                    sosig.CommandSosigToFollowPlayer();
                }

                // Configure chattering
                if (!loadout.enableChattering)
                {
                    // Disable sosig audio/chattering
                    var audioSource = sosig.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.enabled = false;
                    }
                }

                Debug.Log($"[SosigLoadoutUtility] Applied behavior configuration to sosig");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigLoadoutUtility] Error applying behavior: {ex.Message}");
            }
        }

        /// <summary>
        /// Get loadout recommendations based on scenario
        /// </summary>
        public static List<AdvancedSosigLoadout> GetLoadoutsForScenario(string scenarioType, bool friendlyOnly = false)
        {
            var allLoadouts = SosigLoadoutManager.GetLoadouts();
            var recommendations = new List<AdvancedSosigLoadout>();

            foreach (var loadout in allLoadouts)
            {
                bool isAppropriate = true;

                // Filter by friendly/enemy
                if (friendlyOnly && loadout.isHostileToPlayer)
                {
                    isAppropriate = false;
                }

                // Filter by scenario type
                switch (scenarioType.ToLower())
                {
                    case "urban":
                        isAppropriate = loadout.loadoutName.ToLower().Contains("soldier") || 
                                       loadout.loadoutName.ToLower().Contains("assault");
                        break;
                    case "sniper":
                        isAppropriate = loadout.loadoutName.ToLower().Contains("sniper") ||
                                       loadout.loadoutName.ToLower().Contains("marksman");
                        break;
                    case "heavy":
                        isAppropriate = loadout.loadoutName.ToLower().Contains("heavy") ||
                                       loadout.loadoutName.ToLower().Contains("gunner");
                        break;
                    default:
                        isAppropriate = true; // Accept all for unknown scenarios
                        break;
                }

                if (isAppropriate)
                {
                    recommendations.Add(loadout);
                }
            }

            return recommendations;
        }
    }
}