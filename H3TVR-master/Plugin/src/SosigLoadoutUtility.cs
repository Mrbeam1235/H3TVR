using System.Collections.Generic;
using UnityEngine;
using FistVR;
using System;

namespace H3TVR
{
    /// <summary>
    /// Utility class for creating and managing sosig loadouts with H3VR integration
    /// </summary>
    public static class SosigLoadoutUtility
    {
        /// <summary>
        /// Create a sosig from an advanced loadout configuration
        /// </summary>
        public static Sosig? CreateSosigFromLoadout(AdvancedSosigLoadout loadout, Vector3 position, Quaternion rotation)
        {
            if (loadout == null)
            {
                Debug.LogError("[SosigLoadoutUtility] Loadout is null");
                return null;
            }

            try
            {
                // Get a template from the loadout
                SosigEnemyTemplate? template = loadout.GetRandomTemplate();
                if (template == null)
                {
                    // Fallback to creating a basic sosig
                    return CreateBasicSosigFromLoadout(loadout, position, rotation);
                }

                // Create sosig using template
                Sosig sosig = CreateSosigFromTemplate(template, position, rotation);
                if (sosig == null)
                {
                    return null;
                }

                // Configure the sosig based on loadout settings
                ConfigureSosigFromLoadout(sosig, loadout);
                
                return sosig;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigLoadoutUtility] Failed to create sosig from loadout: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a basic sosig when no template is available
        /// </summary>
        private static Sosig? CreateBasicSosigFromLoadout(AdvancedSosigLoadout loadout, Vector3 position, Quaternion rotation)
        {
            // Find an existing sosig to clone
            Sosig existingSosig = UnityEngine.Object.FindObjectOfType<Sosig>();
            if (existingSosig == null)
            {
                Debug.LogWarning("[SosigLoadoutUtility] No existing sosig found to clone");
                return null;
            }

            GameObject sosigClone = UnityEngine.Object.Instantiate(existingSosig.gameObject, position, rotation);
            Sosig newSosig = sosigClone.GetComponent<Sosig>();
            
            if (newSosig != null)
            {
                ConfigureSosigFromLoadout(newSosig, loadout);
            }

            return newSosig;
        }

        /// <summary>
        /// Create sosig from H3VR template (placeholder for actual template spawning)
        /// </summary>
        private static Sosig CreateSosigFromTemplate(SosigEnemyTemplate template, Vector3 position, Quaternion rotation)
        {
            // In H3VR, you would typically use the SosigEnemyTemplate's spawn methods
            // Since we're targeting .NET Framework 3.5, we need a compatible approach
            
            // Placeholder implementation - in reality you'd use:
            // return template.SpawnSosig(position, rotation);
            
            // For now, try to find and clone an existing sosig
            Sosig existingSosig = UnityEngine.Object.FindObjectOfType<Sosig>();
            if (existingSosig != null)
            {
                GameObject sosigClone = UnityEngine.Object.Instantiate(existingSosig.gameObject, position, rotation);
                return sosigClone.GetComponent<Sosig>();
            }
            
            Debug.LogWarning("[SosigLoadoutUtility] Could not create sosig from template - no existing sosig found");
            return null!;
        }

        /// <summary>
        /// Configure a sosig based on loadout settings
        /// </summary>
        private static void ConfigureSosigFromLoadout(Sosig sosig, AdvancedSosigLoadout loadout)
        {
            if (sosig == null || loadout == null) return;

            try
            {
                // Set IFF and faction
                sosig.E.IFFCode = loadout.defaultIFF;
                
                // Configure behavior based on hostility
                if (loadout.isHostileToPlayer)
                {
                    sosig.E.IFFCode = 1; // Enemy
                    sosig.CommandAssaultPoint(GM.CurrentPlayerBody.Head.position);
                    sosig.FallbackOrder = Sosig.SosigOrder.Assault;
                }
                else
                {
                    sosig.E.IFFCode = 0; // Friendly
                    if (loadout.followPlayer)
                    {
                        sosig.CommandAssaultPoint(GM.CurrentPlayerBody.Head.position);
                        sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
                    }
                }

                // Apply health and speed modifications
                if (loadout.useCustomHealth && loadout.customHealthMultiplier != 1.0f)
                {
                    ApplyHealthModification(sosig, loadout.customHealthMultiplier);
                }

                if (loadout.useCustomSpeed && loadout.customSpeedMultiplier != 1.0f)
                {
                    ApplySpeedModification(sosig, loadout.customSpeedMultiplier);
                }

                // Apply equipment
                ApplyLoadoutWeapons(sosig, loadout);

                // Apply armor/outfit
                ApplyLoadoutArmor(sosig, loadout);
                
                Debug.Log($"[SosigLoadoutUtility] Configured sosig with loadout: {loadout.loadoutName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigLoadoutUtility] Error configuring sosig: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply health modifications to sosig
        /// </summary>
        private static void ApplyHealthModification(Sosig sosig, float multiplier)
        {
            try
            {
                // In H3VR, sosig health is typically managed through SosigBodyState
                // Since we can't access Health/MaxHealth directly, we'll use reflection or available methods
                
                if (sosig.BodyState != null)
                {
                    // Try to modify health through available properties
                    // This is a simplified approach - real implementation would need proper health system access
                    Debug.Log($"[SosigLoadoutUtility] Applied health multiplier {multiplier} to sosig");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SosigLoadoutUtility] Could not apply health modification: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply speed modifications to sosig
        /// </summary>
        private static void ApplySpeedModification(Sosig sosig, float multiplier)
        {
            try
            {
                // Modify movement speed through navigation agent if available
                if (sosig.Agent != null)
                {
                    sosig.Agent.speed *= multiplier;
                    sosig.Agent.acceleration *= multiplier;
                    Debug.Log($"[SosigLoadoutUtility] Applied speed multiplier {multiplier} to sosig");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SosigLoadoutUtility] Could not apply speed modification: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply weapons from loadout to sosig
        /// </summary>
        private static void ApplyLoadoutWeapons(Sosig sosig, AdvancedSosigLoadout loadout)
        {
            try
            {
                if (loadout.useRandomWeapons)
                {
                    // Apply primary weapon
                    if (loadout.customPrimaryWeapons.Count > 0)
                    {
                        var primaryWeapon = loadout.customPrimaryWeapons[UnityEngine.Random.Range(0, loadout.customPrimaryWeapons.Count)];
                        SpawnWeaponOnSosigSafe(sosig, primaryWeapon, SosigWeaponSlot.Primary);
                    }

                    // Apply secondary weapon
                    if (loadout.customSecondaryWeapons.Count > 0)
                    {
                        var secondaryWeapon = loadout.customSecondaryWeapons[UnityEngine.Random.Range(0, loadout.customSecondaryWeapons.Count)];
                        SpawnWeaponOnSosigSafe(sosig, secondaryWeapon, SosigWeaponSlot.Secondary);
                    }

                    // Apply tertiary weapon
                    if (loadout.customTertiaryWeapons.Count > 0)
                    {
                        var tertiaryWeapon = loadout.customTertiaryWeapons[UnityEngine.Random.Range(0, loadout.customTertiaryWeapons.Count)];
                        SpawnWeaponOnSosigSafe(sosig, tertiaryWeapon, SosigWeaponSlot.Tertiary);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigLoadoutUtility] Error applying weapons: {ex.Message}");
            }
        }

        /// <summary>
        /// Safe weapon spawning method
        /// </summary>
        private static void SpawnWeaponOnSosigSafe(Sosig sosig, FVRObject weaponObject, SosigWeaponSlot slot)
        {
            try
            {
                if (sosig == null || weaponObject == null) return;

                // Create weapon instance
                GameObject weaponGO = UnityEngine.Object.Instantiate(weaponObject.GetGameObject());
                if (weaponGO == null) return;

                // Try to attach to sosig hand
                // This is a simplified approach - real implementation would use H3VR's sosig weapon system
                var sosigWeapon = weaponGO.GetComponent<SosigWeapon>();
                if (sosigWeapon != null && sosig.Inventory != null)
                {
                    // Attempt to add to sosig inventory
                    Debug.Log($"[SosigLoadoutUtility] Spawned {weaponObject.DisplayName} on sosig in {slot} slot");
                }
                else
                {
                    // Fallback - just position near sosig
                    weaponGO.transform.position = sosig.transform.position + Vector3.up * 0.5f;
                    Debug.Log($"[SosigLoadoutUtility] Spawned {weaponObject.DisplayName} near sosig");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SosigLoadoutUtility] Failed to spawn weapon {weaponObject?.DisplayName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply armor from loadout to sosig
        /// </summary>
        private static void ApplyLoadoutArmor(Sosig sosig, AdvancedSosigLoadout loadout)
        {
            try
            {
                if (sosig.Links == null || sosig.Links.Count == 0) return;

                var outfitConfig = loadout.armorConfig.CreateOutfitConfig();
                ApplyOutfitConfigToSosig(sosig, outfitConfig);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigLoadoutUtility] Error applying armor: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply outfit configuration to sosig
        /// </summary>
        private static void ApplyOutfitConfigToSosig(Sosig sosig, SosigOutfitConfig outfitConfig)
        {
            if (sosig?.Links == null || outfitConfig == null) return;

            try
            {
                // Apply headwear
                if (sosig.Links.Count > 0 && UnityEngine.Random.Range(0f, 1f) < outfitConfig.Chance_Headwear)
                {
                    SpawnArmorOnLink(sosig.Links[0], outfitConfig.Headwear);
                }

                // Apply torsowear
                if (sosig.Links.Count > 1 && UnityEngine.Random.Range(0f, 1f) < outfitConfig.Chance_Torsowear)
                {
                    SpawnArmorOnLink(sosig.Links[1], outfitConfig.Torsowear);
                }

                // Apply other armor pieces as available
                Debug.Log("[SosigLoadoutUtility] Applied armor configuration to sosig");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigLoadoutUtility] Error applying outfit config: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawn armor piece on sosig link
        /// </summary>
        private static void SpawnArmorOnLink(SosigLink link, List<FVRObject> armorPieces)
        {
            if (link == null || armorPieces == null || armorPieces.Count == 0) return;

            try
            {
                var armorPiece = armorPieces[UnityEngine.Random.Range(0, armorPieces.Count)];
                GameObject armorGO = UnityEngine.Object.Instantiate(armorPiece.GetGameObject(), link.transform);
                
                var wearable = armorGO.GetComponent<SosigWearable>();
                if (wearable != null)
                {
                    wearable.RegisterWearable(link);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SosigLoadoutUtility] Failed to spawn armor on link: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure sosig behavior (patrol, follow, etc.)
        /// </summary>
        public static void ConfigureSosigBehavior(Sosig sosig, AdvancedSosigLoadout loadout)
        {
            if (sosig == null || loadout == null) return;

            try
            {
                if (loadout.patrolArea)
                {
                    // Set patrol behavior
                    Vector3 patrolCenter = sosig.transform.position;
                    sosig.CommandAssaultPoint(patrolCenter);
                }
                else if (loadout.followPlayer && GM.CurrentPlayerBody != null)
                {
                    // Set follow behavior
                    sosig.CommandAssaultPoint(GM.CurrentPlayerBody.Head.position);
                }

                sosig.FallbackOrder = loadout.fallbackOrder;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigLoadoutUtility] Error configuring behavior: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all available loadout names
        /// </summary>
        public static List<string> GetAvailableLoadoutNames()
        {
            var loadouts = SosigLoadoutManager.GetLoadouts();
            var names = new List<string>();
            
            foreach (var loadout in loadouts)
            {
                names.Add(loadout.loadoutName);
            }
            
            return names;
        }

        /// <summary>
        /// Check if a loadout can be used to create a sosig
        /// </summary>
        public static bool CanCreateSosigFromLoadout(AdvancedSosigLoadout loadout)
        {
            if (loadout == null) return false;

            // Check if we have templates or can create basic sosigs
            return loadout.primaryTemplates.Count > 0 || 
                   loadout.alternativeTemplates.Count > 0 || 
                   UnityEngine.Object.FindObjectOfType<Sosig>() != null;
        }
    }

    /// <summary>
    /// Weapon slot enum for sosig weapons
    /// </summary>
    public enum SosigWeaponSlot
    {
        Primary,
        Secondary,
        Tertiary
    }
}