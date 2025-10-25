using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using FistVR;
using BepInEx.Logging;
using System.Linq;

namespace H3TVR
{
    /// <summary>
    /// Manages optional dependencies and provides unified integration points
    /// Enhanced with Stovepipe integration for realistic weapon malfunctions
    /// </summary>
    public static class OptionalDependencyManager
    {
        private static ManualLogSource logger;
        private static bool initialized = false;

        // Dependency detection flags
        public static bool IsMagazinePatcherAvailable { get; private set; } = false;
        public static bool IsMeatyceiver2Available { get; private set; } = false;
        public static bool IsStovepipeAvailable { get; private set; } = false;
        public static bool IsJeditTippyToyAvailable { get; private set; } = false;
        public static bool IsOtherToolsAvailable { get; private set; } = false;

        // Integration instances
        private static Dictionary<string, bool> availableDependencies = new Dictionary<string, bool>();

        // Cached reflection objects
        private static Type magazinePatcherType;
        private static Type stovepipeManagerType;
        private static Type meatyceiverManagerType;
        private static MethodInfo magazinePatcherFindCompatibleMethod;
        private static MethodInfo stovepipeJamMethod;
        private static MethodInfo meatyceiverMeatMethod;

        // Plugin GUIDs
        private const string MAGAZINE_PATCHER_GUID = "h3vr.magazinepatcher";
        private const string STOVEPIPE_GUID = "dll.stovepipe";
        private const string MEATYCEIVER_GUID = "Potatoes.Meatyceiver_2";
        private const string JEDIT_TIPPY_TOY_GUID = "PutterMyBancakes.Jeditippytoy";

        public static void Initialize(ManualLogSource logSource)
        {
            if (initialized) return;
            
            logger = logSource;
            DetectOptionalDependencies();
            initialized = true;
        }

        private static void DetectOptionalDependencies()
        {
            try
            {
                logger.LogInfo("[OptionalDependencies] Scanning for optional dependencies...");

                // Detect Magazine Patcher
                IsMagazinePatcherAvailable = DetectMagazinePatcher();
                availableDependencies["MagazinePatcher"] = IsMagazinePatcherAvailable;

                // Detect Meatyceiver 2  
                CheckMeatyceiverAvailability();
                availableDependencies["Meatyceiver2"] = IsMeatyceiver2Available;

                // Detect Stovepipe
                CheckStovepipeAvailability();
                availableDependencies["Stovepipe"] = IsStovepipeAvailable;

                // Detect Jedit Tippy Toy
                IsJeditTippyToyAvailable = DetectJeditTippyToy();
                availableDependencies["JeditTippyToy"] = IsJeditTippyToyAvailable;

                // Detect other tools (placeholder for future integrations)
                IsOtherToolsAvailable = DetectOtherTools();
                availableDependencies["OtherTools"] = IsOtherToolsAvailable;

                LogDependencyStatus();
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Error during dependency detection: {ex.Message}");
            }
        }

        private static void LogDependencyStatus()
        {
            logger.LogInfo("[OptionalDependencies] Detection results:");
            logger.LogInfo($"  • Magazine Patcher: {(IsMagazinePatcherAvailable ? "? Available" : "? Not Found")}");
            logger.LogInfo($"  • Meatyceiver 2: {(IsMeatyceiver2Available ? "? Available" : "? Not Found")}");
            logger.LogInfo($"  • Stovepipe: {(IsStovepipeAvailable ? "? Available" : "? Not Found")}");
            logger.LogInfo($"  • Jedit Tippy Toy: {(IsJeditTippyToyAvailable ? "? Available" : "? Not Found")}");
            logger.LogInfo($"  • Other Tools: {(IsOtherToolsAvailable ? "? Available" : "? Not Found")}");
            
            int availableCount = GetAvailableDependencyCount();
            logger.LogInfo($"[OptionalDependencies] {availableCount}/5 optional dependencies detected");
        }

        #region Magazine Patcher Integration
        /// <summary>
        /// Check if Magazine Patcher mod is available
        /// </summary>
        private static bool DetectMagazinePatcher()
        {
            try
            {
                // Method 1: Check via BepInEx plugin manager
                var pluginInfos = BepInEx.Bootstrap.Chainloader.PluginInfos;
                if (pluginInfos.ContainsKey(MAGAZINE_PATCHER_GUID))
                {
                    logger.LogInfo("[OptionalDependencies] Magazine Patcher detected via BepInEx");
                    return true;
                }

                // Method 2: Try to find Magazine Patcher types via reflection
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            if (type.Name.Contains("MagazinePatcher") || 
                                type.Name.Contains("CompatibleMagazines") ||
                                type.Namespace?.Contains("MagazinePatcher") == true)
                            {
                                magazinePatcherType = type;
                                logger.LogInfo($"[OptionalDependencies] Magazine Patcher detected via reflection: {type.FullName}");
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip assemblies that can't be reflected
                        logger.LogDebug($"[OptionalDependencies] Could not reflect assembly {assembly.FullName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Error checking Magazine Patcher availability: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Find compatible magazine using Magazine Patcher if available
        /// </summary>
        public static FVRObject FindCompatibleMagazine(FVRObject firearmObject)
        {
            if (!IsMagazinePatcherAvailable || magazinePatcherFindCompatibleMethod == null || firearmObject == null)
                return null;

            try
            {
                // Try to use Magazine Patcher to find compatible magazine
                var result = magazinePatcherFindCompatibleMethod.Invoke(null, new object[] { firearmObject });
                if (result is FVRObject magazine)
                {
                    logger.LogDebug($"[OptionalDependencies] Found compatible magazine via Magazine Patcher: {magazine.ItemID}");
                    return magazine;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Failed to find compatible magazine via Magazine Patcher: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Get enhanced magazine compatibility list if Magazine Patcher is available
        /// </summary>
        public static List<FVRObject> GetEnhancedMagazineCompatibility(FVRObject firearmObject)
        {
            var compatibleMagazines = new List<FVRObject>();

            if (!IsMagazinePatcherAvailable || firearmObject == null)
                return compatibleMagazines;

            try
            {
                // Try to get all compatible magazines from Magazine Patcher
                if (magazinePatcherType != null)
                {
                    var getAllCompatibleMethod = magazinePatcherType.GetMethod("GetAllCompatibleMagazines", 
                        BindingFlags.Public | BindingFlags.Static);
                    
                    if (getAllCompatibleMethod != null)
                    {
                        var result = getAllCompatibleMethod.Invoke(null, new object[] { firearmObject });
                        if (result is List<FVRObject> magazines)
                        {
                            compatibleMagazines.AddRange(magazines);
                            logger.LogDebug($"[OptionalDependencies] Found {magazines.Count} compatible magazines via Magazine Patcher");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Failed to get enhanced magazine compatibility: {ex.Message}");
            }

            return compatibleMagazines;
        }
        #endregion

        #region Jedit Tippy Toy Integration
        /// <summary>
        /// Check if Jedit Tippy Toy mod is available
        /// </summary>
        private static bool DetectJeditTippyToy()
        {
            try
            {
                // Method 1: Check via BepInEx plugin manager
                var pluginInfos = BepInEx.Bootstrap.Chainloader.PluginInfos;
                if (pluginInfos.ContainsKey(JEDIT_TIPPY_TOY_GUID))
                {
                    logger.LogInfo("[OptionalDependencies] Jedit Tippy Toy detected via BepInEx");
                    return true;
                }

                // Method 2: Check if ftw.JediTippyToy exists in ItemManager (CORRECT ID)
                if (IM.OD != null && IM.OD.Count > 0 && IM.OD.ContainsKey("ftw.JediTippyToy"))
                {
                    logger.LogInfo("[OptionalDependencies] Jedit Tippy Toy detected via ItemManager");
                    return true;
                }

                // Method 3: Try to find Jedit Tippy Toy types via reflection
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            if (type.Name.Contains("JeditTippyToy") || 
                                type.Name.Contains("TippyToy") ||
                                type.Namespace?.Contains("JeditTippyToy") == true)
                            {
                                logger.LogInfo($"[OptionalDependencies] Jedit Tippy Toy detected via reflection: {type.FullName}");
                                return true;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Skip assemblies that can't be reflected
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Error checking Jedit Tippy Toy availability: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Check if Jedit Tippy Toy object is available for spawning
        /// </summary>
        public static bool IsJeditToySpawnable()
        {
            if (!IsJeditTippyToyAvailable)
                return false;

            try
            {
                return IM.OD != null && IM.OD.Count > 0 && IM.OD.ContainsKey("ftw.JediTippyToy");
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"[OptionalDependencies] Error checking Jedit Toy spawnability: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get Jedit Tippy Toy object ID for spawning
        /// </summary>
        public static string GetJeditToyObjectID()
        {
            return "ftw.JediTippyToy";
        }

        /// <summary>
        /// Validate Jedit Tippy Toy is properly installed and functional
        /// </summary>
        public static bool ValidateJeditTippyToy()
        {
            if (!IsJeditTippyToyAvailable)
            {
                logger.LogWarning("[OptionalDependencies] Jedit Tippy Toy not detected. Install from: https://thunderstore.io/c/h3vr/p/PutterMyBancakes/Jeditippytoy/");
                return false;
            }

            if (!IsJeditToySpawnable())
            {
                logger.LogWarning("[OptionalDependencies] Jedit Tippy Toy detected but ftw.JediTippyToy not found in ItemManager");
                return false;
            }

            logger.LogInfo("[OptionalDependencies] Jedit Tippy Toy validated and ready");
            return true;
        }
        #endregion

        #region Stovepipe Integration Methods
        /// <summary>
        /// Check if a firearm can experience jams via Stovepipe
        /// </summary>
        public static bool CanFirearmJam(FVRFireArm firearm)
        {
            if (!IsStovepipeAvailable || firearm == null)
                return false;

            try
            {
                return StovepipeIntegrationManager.CanWeaponJam(firearm);
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Error checking jam capability: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to trigger a Stovepipe jam on a firearm
        /// </summary>
        public static bool TryTriggerStovepipeJam(FVRFireArm firearm, string context = "Normal", float customChance = -1f)
        {
            if (!IsStovepipeAvailable || firearm == null)
                return false;

            try
            {
                return StovepipeIntegrationManager.TryJamWeapon(firearm, context, customChance);
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Error triggering Stovepipe jam: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Force a specific type of malfunction via Stovepipe
        /// </summary>
        public static bool ForceStovepipeMalfunction(FVRFireArm firearm, StovepipeIntegrationManager.MalfunctionType malfunctionType, string context = "Forced")
        {
            if (!IsStovepipeAvailable || firearm == null)
                return false;

            try
            {
                return StovepipeIntegrationManager.TryJamWeapon(firearm, context, 1.0f, true, malfunctionType);
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Error forcing Stovepipe malfunction: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a firearm is currently jammed
        /// </summary>
        public static bool IsFirearmJammed(FVRFireArm firearm)
        {
            if (!IsStovepipeAvailable || firearm == null)
                return false;

            try
            {
                return StovepipeIntegrationManager.IsWeaponJammed(firearm);
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Error checking jam status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear a jam from a firearm
        /// </summary>
        public static bool ClearFirearmJam(FVRFireArm firearm)
        {
            if (!IsStovepipeAvailable || firearm == null)
                return false;

            try
            {
                return StovepipeIntegrationManager.ClearWeaponJam(firearm);
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Error clearing jam: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply contextual jamming to multiple firearms (for sosig enhancement)
        /// </summary>
        public static Dictionary<FVRFireArm, bool> ApplyContextualJamming(List<FVRFireArm> firearms, string context, float baseChance = 0.02f)
        {
            var results = new Dictionary<FVRFireArm, bool>();
            
            if (!IsStovepipeAvailable || firearms == null)
            {
                foreach (var firearm in firearms ?? new List<FVRFireArm>())
                    results[firearm] = false;
                return results;
            }

            try
            {
                logger.LogInfo($"[OptionalDependencies] Applying contextual jamming to {firearms.Count} firearms (context: {context})");
                
                foreach (var firearm in firearms)
                {
                    if (firearm != null)
                    {
                        // Apply context-specific modifiers
                        float contextualChance = CalculateContextualJamChance(context, baseChance);
                        bool success = StovepipeIntegrationManager.TryJamWeapon(firearm, context, contextualChance);
                        results[firearm] = success;
                        
                        if (success)
                        {
                            logger.LogDebug($"[OptionalDependencies] Applied jam to {firearm.name} in context '{context}'");
                        }
                    }
                    else
                    {
                        results[firearm] = false;
                    }
                }

                int successCount = results.Count(r => r.Value);
                logger.LogInfo($"[OptionalDependencies] Contextual jamming completed: {successCount}/{firearms.Count} weapons jammed");
                
                return results;
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Error in contextual jamming: {ex.Message}");
                foreach (var firearm in firearms)
                    results[firearm] = false;
                return results;
            }
        }

        /// <summary>
        /// Calculate contextual jam chance based on sosig context
        /// </summary>
        private static float CalculateContextualJamChance(string context, float baseChance)
        {
            context = context.ToLower();
            
            // Context-specific multipliers for sosig enhancement
            if (context.Contains("elite") || context.Contains("boss"))
                return baseChance * 0.3f; // Elite weapons are well-maintained
            else if (context.Contains("enemy") || context.Contains("hostile"))
                return baseChance * 1.5f; // Enemy weapons may be less reliable
            else if (context.Contains("ally") || context.Contains("friendly"))
                return baseChance * 0.8f; // Allied weapons are better maintained
            else if (context.Contains("combat") || context.Contains("stress"))
                return baseChance * 2.0f; // Combat stress increases jam chance
            else if (context.Contains("dirty") || context.Contains("fouled"))
                return baseChance * 3.0f; // Dirty weapons jam more
            else if (context.Contains("worn") || context.Contains("damaged"))
                return baseChance * 2.5f; // Damaged weapons are unreliable
            
            return baseChance; // Default multiplier
        }

        /// <summary>
        /// Get Stovepipe statistics and status
        /// </summary>
        public static string GetStovepipeStatus()
        {
            if (!IsStovepipeAvailable)
                return "Stovepipe: Not Available";

            try
            {
                return StovepipeIntegrationManager.GetMalfunctionStats();
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Error getting Stovepipe status: {ex.Message}");
                return "Stovepipe: Error retrieving status";
            }
        }
        #endregion

        #region Enhanced Sosig Weapon Setup
        /// <summary>
        /// Enhanced sosig weapon setup with all available integrations
        /// </summary>
        public static void EnhanceSosigWeapon(FVRFireArm firearm, string sosigType, string context = "Normal")
        {
            if (firearm == null) return;

            try
            {
                logger.LogDebug($"[OptionalDependencies] Enhancing sosig weapon {firearm.name} for {sosigType} in context '{context}'");

                // MEATYCEIVER 2 INTEGRATION: Apply transformations
                if (IsMeatyceiver2Available)
                {
                    try
                    {
                        string meatyContext = DetermineMeatyceiverContext(sosigType, context);
                        if (MeatyceiverIntegrationManager.TryTransformWeapon(firearm, meatyContext))
                        {
                            logger.LogDebug($"[OptionalDependencies] Applied Meatyceiver transformation to {firearm.name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"[OptionalDependencies] Meatyceiver enhancement failed: {ex.Message}");
                    }
                }

                // STOVEPIPE INTEGRATION: Apply contextual jamming
                if (IsStovepipeAvailable && CanFirearmJam(firearm))
                {
                    try
                    {
                        string stovepipeContext = DetermineStovepipeContext(sosigType, context);
                        float jamChance = CalculateContextualJamChance(stovepipeContext, 0.02f);
                        
                        if (TryTriggerStovepipeJam(firearm, stovepipeContext, jamChance))
                        {
                            logger.LogDebug($"[OptionalDependencies] Applied Stovepipe jamming to {firearm.name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"[OptionalDependencies] Stovepipe enhancement failed: {ex.Message}");
                    }
                }

                logger.LogDebug($"[OptionalDependencies] Weapon enhancement completed for {firearm.name}");
            }
            catch (Exception ex)
            {
                logger.LogError($"[OptionalDependencies] Error in weapon enhancement: {ex.Message}");
            }
        }

        /// <summary>
        /// Determine appropriate Meatyceiver context from sosig type
        /// </summary>
        private static string DetermineMeatyceiverContext(string sosigType, string context)
        {
            sosigType = sosigType.ToLower();
            context = context.ToLower();

            if (sosigType.Contains("elite") || sosigType.Contains("boss"))
                return "Elite";
            else if (sosigType.Contains("enemy") || sosigType.Contains("hostile"))
                return "EnemyWeapon";
            else if (sosigType.Contains("ally") || sosigType.Contains("friendly"))
                return "AllyWeapon";
            else if (context.Contains("chaos"))
                return "Chaos";
            else
                return "Normal";
        }

        /// <summary>
        /// Determine appropriate Stovepipe context from sosig type
        /// </summary>
        private static string DetermineStovepipeContext(string sosigType, string context)
        {
            sosigType = sosigType.ToLower();
            context = context.ToLower();

            if (sosigType.Contains("elite") || sosigType.Contains("boss"))
                return "Elite";
            else if (sosigType.Contains("enemy") || sosigType.Contains("hostile"))
                return "Enemy";
            else if (sosigType.Contains("ally") || sosigType.Contains("friendly"))
                return "Ally";
            else if (context.Contains("combat") || context.Contains("stress"))
                return "Combat";
            else if (context.Contains("dirty") || context.Contains("fouled"))
                return "Dirty";
            else
                return "Normal";
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Detect other tools (placeholder for future integrations)
        /// </summary>
        private static bool DetectOtherTools()
        {
            // Placeholder for future tool integrations
            return false;
        }

        public static bool HasAnyDependencies()
        {
            return IsMagazinePatcherAvailable || IsMeatyceiver2Available || IsStovepipeAvailable || IsJeditTippyToyAvailable || IsOtherToolsAvailable;
        }

        public static int GetAvailableDependencyCount()
        {
            int count = 0;
            if (IsMagazinePatcherAvailable) count++;
            if (IsMeatyceiver2Available) count++;
            if (IsStovepipeAvailable) count++;
            if (IsJeditTippyToyAvailable) count++;
            if (IsOtherToolsAvailable) count++;
            return count;
        }

        public static Dictionary<string, bool> GetDependencyStatus()
        {
            return new Dictionary<string, bool>(availableDependencies);
        }

        /// <summary>
        /// Get comprehensive dependency information
        /// </summary>
        public static string GetDependencyInfo()
        {
            var info = "H3TVR Optional Dependencies Status:\n";
            info += $"• Magazine Patcher: {(IsMagazinePatcherAvailable ? "? Active" : "? Not Found")}\n";
            info += $"• Meatyceiver 2: {(IsMeatyceiver2Available ? "? Active" : "? Not Found")}\n";
            info += $"• Stovepipe: {(IsStovepipeAvailable ? "? Active" : "? Not Found")}\n";
            info += $"• Jedit Tippy Toy: {(IsJeditTippyToyAvailable ? "? Active" : "? Not Found")}\n";
            info += $"• Other Tools: {(IsOtherToolsAvailable ? "? Active" : "? Not Found")}\n";
            info += $"Total: {GetAvailableDependencyCount()}/5 dependencies available";
            
            return info;
        }

        /// <summary>
        /// Get status report for all dependencies (legacy method name for compatibility)
        /// </summary>
        public static string GetDependencyStatusReport()
        {
            var report = "H3TVR Optional Dependencies:\n";
            report += $"• Stovepipe: {(IsStovepipeAvailable ? "? Available" : "? Not Installed")}\n";
            report += $"• Meatyceiver 2: {(IsMeatyceiver2Available ? "? Available" : "? Not Installed")}\n";
            report += $"• Magazine Patcher: {(IsMagazinePatcherAvailable ? "? Available" : "? Not Installed")}\n";
            report += $"• Jedit Tippy Toy: {(IsJeditTippyToyAvailable ? "? Available" : "? Not Installed")}\n";
            
            if (!IsStovepipeAvailable || !IsMeatyceiver2Available || !IsMagazinePatcherAvailable || !IsJeditTippyToyAvailable)
            {
                report += "\nInstall missing dependencies for enhanced functionality:\n";
                if (!IsStovepipeAvailable)
                    report += "  Stovepipe: https://thunderstore.io/c/h3vr/p/Smidge204/Stovepipe/\n";
                if (!IsMeatyceiver2Available)
                    report += "  Meatyceiver 2: https://thunderstore.io/c/h3vr/p/Potatoes/Meatyceiver_2/\n";
                if (!IsMagazinePatcherAvailable)
                    report += "  Magazine Patcher: https://thunderstore.io/c/h3vr/p/O_Deka_K/MagazinePatcher/\n";
                if (!IsJeditTippyToyAvailable)
                    report += "  Jedit Tippy Toy: https://thunderstore.io/c/h3vr/p/PutterMyBancakes/Jeditippytoy/\n";
            }

            return report;
        }

        /// <summary>
        /// Check Meatyceiver availability safely
        /// </summary>
        private static void CheckMeatyceiverAvailability()
        {
            try
            {
                // Try to access the static property safely
                var meatyceiverManagerType = typeof(MeatyceiverIntegrationManager);
                var property = meatyceiverManagerType.GetProperty("IsMeatyceiver2Available", BindingFlags.Public | BindingFlags.Static);
                if (property != null)
                {
                    IsMeatyceiver2Available = (bool)property.GetValue(null, null);
                }
                else
                {
                    IsMeatyceiver2Available = false;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"[OptionalDependencies] Could not check Meatyceiver availability: {ex.Message}");
                IsMeatyceiver2Available = false;
            }
        }

        /// <summary>
        /// Check Stovepipe availability safely
        /// </summary>
        private static void CheckStovepipeAvailability()
        {
            try
            {
                // Try to access the static property safely
                var stovepipeManagerType = typeof(StovepipeIntegrationManager);
                var property = stovepipeManagerType.GetProperty("IsStovepipeAvailable", BindingFlags.Public | BindingFlags.Static);
                if (property != null)
                {
                    IsStovepipeAvailable = (bool)property.GetValue(null, null);
                }
                else
                {
                    IsStovepipeAvailable = false;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"[OptionalDependencies] Could not check Stovepipe availability: {ex.Message}");
                IsStovepipeAvailable = false;
            }
        }
        #endregion
    }
}