using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FistVR;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Enhanced sosig weapon management with advanced optional dependency integration
    /// Now includes Stovepipe jamming mechanics for realistic weapon malfunctions
    /// </summary>
    public static class SosigWeaponEnhancer
    {
        private static ManualLogSource logger;
        private static bool initialized = false;

        // Enhancement statistics
        public static int TotalWeaponsEnhanced { get; private set; } = 0;
        public static int MeatyceiverTransformations { get; private set; } = 0;
        public static int StovepipeJams { get; private set; } = 0;
        public static int MagazinePatcherMatches { get; private set; } = 0;

        // Context-based enhancement tracking
        private static readonly Dictionary<string, int> enhancementsByContext = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> jamsByContext = new Dictionary<string, int>();

        public static void Initialize(ManualLogSource logSource)
        {
            if (initialized) return;
            
            logger = logSource;
            initialized = true;
            
            logger.LogInfo("[SosigWeaponEnhancer] Enhanced sosig weapon system initialized");
            
            if (OptionalDependencyManager.HasAnyDependencies())
            {
                int depCount = OptionalDependencyManager.GetAvailableDependencyCount();
                logger.LogInfo($"[SosigWeaponEnhancer] Enhanced with {depCount} optional dependencies");
                
                if (OptionalDependencyManager.IsStovepipeAvailable)
                {
                    logger.LogInfo("[SosigWeaponEnhancer] Stovepipe integration active - realistic weapon malfunctions enabled");
                }
                
                if (OptionalDependencyManager.IsMeatyceiver2Available)
                {
                    logger.LogInfo("[SosigWeaponEnhancer] Meatyceiver 2 integration active - weapon transformations enabled");
                }
            }
        }

        /// <summary>
        /// Enhanced sosig weapon setup with contextual logic and optional dependency integration
        /// </summary>
        public static void EnhanceSosigWeapon(Sosig sosig, FVRFireArm weapon, string context = "Normal")
        {
            if (sosig == null || weapon == null) return;

            try
            {
                TotalWeaponsEnhanced++;
                
                // Determine sosig type for contextual enhancement
                string sosigType = DetermineSosigType(sosig);
                string enhancementContext = DetermineEnhancementContext(sosigType, context);
                
                // Track enhancements by context
                if (!enhancementsByContext.ContainsKey(enhancementContext))
                    enhancementsByContext[enhancementContext] = 0;
                enhancementsByContext[enhancementContext]++;

                logger.LogDebug($"[SosigWeaponEnhancer] Enhancing {sosigType} sosig weapon: {weapon.name} (context: {enhancementContext})");

                // Apply Meatyceiver transformations with sosig-specific logic
                if (OptionalDependencyManager.IsMeatyceiver2Available)
                {
                    ApplyMeatyceiverEnhancements(weapon, sosigType, enhancementContext);
                }

                // Apply Stovepipe jamming with sosig-specific logic
                if (OptionalDependencyManager.IsStovepipeAvailable)
                {
                    ApplyStovepipeEnhancements(weapon, sosigType, enhancementContext);
                }

                // Apply Magazine Patcher enhancements
                if (OptionalDependencyManager.IsMagazinePatcherAvailable)
                {
                    ApplyMagazinePatcherEnhancements(weapon, sosigType);
                }

                logger?.LogDebug($"[SosigWeaponEnhancer] Enhancement completed for {sosigType} sosig weapon");
            }
            catch (Exception ex)
            {
                logger?.LogError($"[SosigWeaponEnhancer] Error enhancing sosig weapon: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply Meatyceiver transformations with sosig-specific logic
        /// </summary>
        private static void ApplyMeatyceiverEnhancements(FVRFireArm weapon, string sosigType, string context)
        {
            try
            {
                // Determine transformation chance based on sosig type
                float transformChance = GetMeatyceiverChanceForSosigType(sosigType);
                string meatyContext = GetMeatyceiverContextForSosig(sosigType);

                if (MeatyceiverIntegrationManager.TryTransformWeapon(weapon, meatyContext, transformChance))
                {
                    MeatyceiverTransformations++;
                    logger?.LogInfo($"[SosigWeaponEnhancer] Applied Meatyceiver transformation to {sosigType} sosig weapon: {weapon.name}");
                }
                else if (MeatyceiverIntegrationManager.GetFeatureConfig("VerboseLogging"))
                {
                    logger?.LogDebug($"[SosigWeaponEnhancer] Meatyceiver transformation skipped for {sosigType} sosig (chance: {transformChance:P2})");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"[SosigWeaponEnhancer] Meatyceiver enhancement failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply Stovepipe jamming with sosig-specific logic
        /// </summary>
        private static void ApplyStovepipeEnhancements(FVRFireArm weapon, string sosigType, string context)
        {
            try
            {
                // Skip jamming for certain sosig types to maintain gameplay balance
                if (ShouldSkipJammingForSosigType(sosigType))
                {
                    logger?.LogDebug($"[SosigWeaponEnhancer] Skipping jamming for {sosigType} sosig (gameplay balance)");
                    return;
                }

                // Determine jam chance and type based on sosig characteristics
                float jamChance = GetStovepipeChanceForSosigType(sosigType);
                string stovepipeContext = GetStovepipeContextForSosig(sosigType, context);
                
                // Apply contextual jamming
                if (OptionalDependencyManager.TryTriggerStovepipeJam(weapon, stovepipeContext, jamChance))
                {
                    StovepipeJams++;
                    
                    // Track jams by context
                    if (!jamsByContext.ContainsKey(stovepipeContext))
                        jamsByContext[stovepipeContext] = 0;
                    jamsByContext[stovepipeContext]++;
                    
                    logger?.LogInfo($"[SosigWeaponEnhancer] Applied Stovepipe jam to {sosigType} sosig weapon: {weapon.name} (context: {stovepipeContext})");
                }
                else if (StovepipeIntegrationManager.GetFeatureConfig("VerboseLogging"))
                {
                    logger?.LogDebug($"[SosigWeaponEnhancer] Stovepipe jamming skipped for {sosigType} sosig (chance: {jamChance:P4})");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"[SosigWeaponEnhancer] Stovepipe enhancement failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply Magazine Patcher enhancements to ensure compatible magazines
        /// </summary>
        private static void ApplyMagazinePatcherEnhancements(FVRFireArm weapon, string sosigType)
        {
            try
            {
                // Check if weapon has compatible magazines through Magazine Patcher
                var compatibleMags = OptionalDependencyManager.GetEnhancedMagazineCompatibility(weapon.ObjectWrapper);
                
                if (compatibleMags != null && compatibleMags.Count > 0)
                {
                    MagazinePatcherMatches++;
                    logger.LogDebug($"[SosigWeaponEnhancer] Magazine Patcher found {compatibleMags.Count} compatible magazines for {weapon.name}");
                    
                    // Optionally spawn a compatible magazine near the sosig (for testing/gameplay)
                    if (UnityEngine.Random.value < 0.1f) // 10% chance
                    {
                        SpawnCompatibleMagazineForSosig(weapon, compatibleMags, sosigType);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[SosigWeaponEnhancer] Magazine Patcher enhancement failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Determine sosig type from sosig characteristics
        /// </summary>
        private static string DetermineSosigType(Sosig sosig)
        {
            try
            {
                // Check sosig faction/team
                if (sosig.E.IFFCode == GM.CurrentPlayerBody.GetPlayerIFF())
                {
                    return "Ally";
                }
                
                // Check if sosig appears to be elite/boss based on various factors
                bool isElite = false;
                
                // Health-based elite detection using Links
                if (sosig.Links != null)
                {
                    float totalHealth = sosig.Links.Sum(link => link.m_integrity);
                    if (totalHealth > 150f) // High health threshold
                    {
                        isElite = true;
                    }
                }
                
                // Equipment-based elite detection
                if (sosig.Inventory != null && sosig.Inventory.Slots != null)
                {
                    int weaponCount = sosig.Inventory.Slots.Count(slot => slot != null);
                    if (weaponCount > 2) // Multiple weapons suggest elite status
                    {
                        isElite = true;
                    }
                }

                // Speed/agility based detection
                if (sosig.Speed_Run > 4f || sosig.Speed_Sneak > 2f)
                {
                    isElite = true;
                }

                return isElite ? "Elite" : "Enemy";
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[SosigWeaponEnhancer] Error determining sosig type: {ex.Message}");
                return "Enemy"; // Default fallback
            }
        }

        /// <summary>
        /// Determine enhancement context from sosig type and current context
        /// </summary>
        private static string DetermineEnhancementContext(string sosigType, string context)
        {
            // Combine sosig type with context for more specific enhancement behavior
            if (context.Contains("chaos") || context.Contains("Chaos"))
                return $"{sosigType}_Chaos";
            else if (context.Contains("combat") || context.Contains("Combat"))
                return $"{sosigType}_Combat";
            else if (context.Contains("stealth") || context.Contains("Stealth"))
                return $"{sosigType}_Stealth";
            else
                return sosigType;
        }

        /// <summary>
        /// Get Meatyceiver transformation chance based on sosig type
        /// </summary>
        private static float GetMeatyceiverChanceForSosigType(string sosigType)
        {
            switch (sosigType.ToLower())
            {
                case "ally":
                    return 0.01f; // 1% - allies have good equipment
                case "enemy":
                    return 0.04f; // 4% - enemy equipment is less reliable
                case "elite":
                    return 0.005f; // 0.5% - elite sosigs have premium equipment
                case "ally_chaos":
                    return 0.08f; // 8% - chaos affects everyone
                case "enemy_chaos":
                    return 0.20f; // 20% - chaos heavily affects enemies
                case "elite_chaos":
                    return 0.03f; // 3% - elite equipment is more resistant to chaos
                default:
                    return 0.02f; // 2% - default
            }
        }

        /// <summary>
        /// Get Stovepipe jam chance based on sosig type
        /// </summary>
        private static float GetStovepipeChanceForSosigType(string sosigType)
        {
            switch (sosigType.ToLower())
            {
                case "ally":
                    return 0.008f; // 0.8% - allies maintain their weapons
                case "enemy":
                    return 0.025f; // 2.5% - enemy weapons are less maintained
                case "elite":
                    return 0.003f; // 0.3% - elite sosigs have excellent weapons
                case "ally_combat":
                    return 0.015f; // 1.5% - combat stress affects everyone
                case "enemy_combat":
                    return 0.040f; // 4% - enemies in combat have higher jam rates
                case "elite_combat":
                    return 0.008f; // 0.8% - elite weapons perform better under stress
                default:
                    return 0.015f; // 1.5% - default
            }
        }

        /// <summary>
        /// Check if jamming should be skipped for this sosig type (for gameplay balance)
        /// </summary>
        private static bool ShouldSkipJammingForSosigType(string sosigType)
        {
            // Skip jamming for certain sosig types to maintain gameplay balance
            sosigType = sosigType.ToLower();
            
            // Boss/Elite sosigs should rarely have weapon malfunctions for gameplay reasons
            if (sosigType.Contains("elite") && UnityEngine.Random.value > 0.3f) // 70% skip chance for elites
                return true;
                
            // Allies should have reliable weapons most of the time
            if (sosigType.Contains("ally") && UnityEngine.Random.value > 0.6f) // 40% skip chance for allies
                return true;
                
            return false; // Don't skip for regular enemies
        }

        /// <summary>
        /// Get Meatyceiver context string for sosig type
        /// </summary>
        private static string GetMeatyceiverContextForSosig(string sosigType)
        {
            switch (sosigType.ToLower())
            {
                case "ally":
                case "ally_combat":
                case "ally_chaos":
                    return "AllyWeapon";
                case "enemy":
                case "enemy_combat":
                case "enemy_chaos":
                    return "EnemyWeapon";
                case "elite":
                case "elite_combat":
                case "elite_chaos":
                    return "Elite";
                default:
                    return "SosigWeapon";
            }
        }

        /// <summary>
        /// Get Stovepipe context string for sosig type
        /// </summary>
        private static string GetStovepipeContextForSosig(string sosigType, string originalContext)
        {
            string baseContext = sosigType.ToLower().Contains("elite") ? "Elite" :
                                sosigType.ToLower().Contains("ally") ? "Ally" : "Enemy";
            
            if (originalContext.ToLower().Contains("combat"))
                return "Combat";
            else if (originalContext.ToLower().Contains("dirty"))
                return "Dirty";
            else if (originalContext.ToLower().Contains("worn"))
                return "WornOut";
            else
                return baseContext;
        }

        /// <summary>
        /// Spawn a compatible magazine for a sosig weapon (for testing/gameplay enhancement)
        /// </summary>
        private static void SpawnCompatibleMagazineForSosig(FVRFireArm weapon, List<FVRObject> compatibleMags, string sosigType)
        {
            try
            {
                if (compatibleMags.Count == 0) return;

                // Select random compatible magazine
                var selectedMag = compatibleMags[UnityEngine.Random.Range(0, compatibleMags.Count)];
                
                // Spawn magazine near the weapon
                Vector3 spawnPos = weapon.transform.position + Vector3.up * 0.1f + UnityEngine.Random.insideUnitSphere * 0.3f;
                GameObject magGO = UnityEngine.Object.Instantiate(selectedMag.GetGameObject(), spawnPos, weapon.transform.rotation);
                
                logger.LogDebug($"[SosigWeaponEnhancer] Spawned compatible magazine {selectedMag.ItemID} for {sosigType} sosig weapon");
            }
            catch (Exception ex)
            {
                logger.LogError($"[SosigWeaponEnhancer] Error spawning compatible magazine: {ex.Message}");
            }
        }

        /// <summary>
        /// Get enhancement statistics
        /// </summary>
        public static string GetEnhancementStats()
        {
            var contextStats = string.Join(", ", enhancementsByContext.Select(kvp => $"{kvp.Key}: {kvp.Value}").ToArray());
            var jamStats = string.Join(", ", jamsByContext.Select(kvp => $"{kvp.Key}: {kvp.Value}").ToArray());

            return $"Sosig Weapon Enhancement Stats:\n" +
                   $"• Total Weapons Enhanced: {TotalWeaponsEnhanced}\n" +
                   $"• Meatyceiver Transformations: {MeatyceiverTransformations}\n" +
                   $"• Stovepipe Jams Applied: {StovepipeJams}\n" +
                   $"• Magazine Patcher Matches: {MagazinePatcherMatches}\n" +
                   $"• Enhancements by Context: {contextStats}\n" +
                   $"• Jams by Context: {jamStats}\n" +
                   $"• Available Dependencies: {OptionalDependencyManager.GetAvailableDependencyCount()}/4";
        }

        /// <summary>
        /// Clear enhancement statistics
        /// </summary>
        public static void ClearStats()
        {
            TotalWeaponsEnhanced = 0;
            MeatyceiverTransformations = 0;
            StovepipeJams = 0;
            MagazinePatcherMatches = 0;
            enhancementsByContext.Clear();
            jamsByContext.Clear();
            
            logger.LogInfo("[SosigWeaponEnhancer] Enhancement statistics cleared");
        }

        /// <summary>
        /// Apply contextual enhancements to all weapons on a sosig (called from EnhancedChatSpawner)
        /// </summary>
        public static void ApplyContextualEnhancements(Sosig sosig, string spawnerContext)
        {
            if (sosig == null) return;

            try
            {
                // Get all weapons from sosig hands
                var weapons = new List<FVRFireArm>();
                
                if (sosig.Hands != null)
                {
                    foreach (var hand in sosig.Hands)
                    {
                        if (hand?.HeldObject != null && hand.HeldObject is SosigWeapon sosigWeapon)
                        {
                            var fireArm = sosigWeapon.GetComponent<FVRFireArm>();
                            if (fireArm != null)
                            {
                                weapons.Add(fireArm);
                            }
                        }
                    }
                }

                // Apply enhancements to each weapon
                foreach (var weapon in weapons)
                {
                    EnhanceSosigWeapon(sosig, weapon, spawnerContext);
                }

                if (weapons.Count > 0)
                {
                    logger?.LogDebug($"[SosigWeaponEnhancer] Applied contextual enhancements to {weapons.Count} weapons for sosig spawned by {spawnerContext}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"[SosigWeaponEnhancer] Error applying contextual enhancements: {ex.Message}");
            }
        }
    }
}