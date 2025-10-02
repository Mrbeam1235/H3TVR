using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using FistVR;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.Linq;
using System.Collections;
using HarmonyLib;

namespace H3TVR
{
    /// <summary>
    /// Advanced Meatyceiver 2 Integration Manager for H3TVR
    /// Handles all aspects of Meatyceiver 2 mod integration including detection,
    /// transformation logic, configuration, and contextual behavior
    /// </summary>
    public static class MeatyceiverIntegrationManager
    {
        #region Fields and Properties
        private static ManualLogSource logger;
        private static bool initialized = false;

        // Meatyceiver 2 Detection
        public static bool IsMeatyceiver2Available { get; private set; } = false;
        public static string DetectedVersion { get; private set; } = "Unknown";
        public static string DetectedApiVersion { get; private set; } = "Unknown";
        
        // Cached Meatyceiver 2 Components
        private static Type meatyceiverType;
        private static object meatyceiverInstance;
        private static MethodInfo transformMethod;
        private static MethodInfo checkCompatibilityMethod;
        private static MethodInfo isTransformedMethod;
        private static MethodInfo getQualityMethod;
        private static MethodInfo setQualityMethod;
        private static PropertyInfo transformChanceProperty;
        private static FieldInfo enabledField;

        // Configuration
        private static ConfigFile config;
        private static Dictionary<string, ConfigEntry<float>> chanceConfigs;
        private static Dictionary<string, ConfigEntry<bool>> featureConfigs;
        private static Dictionary<string, ConfigEntry<float>> multiplierConfigs;
        private static Dictionary<string, ConfigEntry<int>> intConfigs;

        // Caching and Performance
        private static readonly Dictionary<string, bool> transformationCache = new Dictionary<string, bool>();
        private static readonly Dictionary<string, DateTime> transformationTimes = new Dictionary<string, DateTime>();
        private static readonly Dictionary<string, DateTime> transformationCooldowns = new Dictionary<string, DateTime>();
        private static readonly Dictionary<string, WeaponQuality> weaponQualities = new Dictionary<string, WeaponQuality>();
        private static DateTime lastCacheClear = DateTime.Now;

        // Advanced Statistics
        public static int TotalTransformationAttempts { get; private set; } = 0;
        public static int SuccessfulTransformations { get; private set; } = 0;
        public static int CachedResults { get; private set; } = 0;
        public static int CooldownBlocked { get; private set; } = 0;
        public static int QualityPreserved { get; private set; } = 0;
        public static Dictionary<string, int> TransformationsByContext { get; private set; } = new Dictionary<string, int>();
        public static Dictionary<string, int> TransformationsByWeaponType { get; private set; } = new Dictionary<string, int>();

        // Plugin GUIDs for detection
        private const string MEATYCEIVER2_GUID = "Potatoes.Meatyceiver_2";
        private const string MEATYCEIVER_LEGACY_GUID = "potatoes1286.meatyceiver";
        private const string MEATYCEIVER_ALPHA_GUID = "potatoes.meatyceiver.alpha";

        // Weapon Quality Enum
        public enum WeaponQuality
        {
            Common = 0,
            Uncommon = 1,
            Rare = 2,
            Epic = 3,
            Legendary = 4,
            Artifact = 5
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the Meatyceiver 2 integration manager
        /// </summary>
        public static void Initialize(ManualLogSource logSource, ConfigFile configFile)
        {
            if (initialized) return;

            logger = logSource;
            config = configFile;
            
            logger.LogInfo("[MeatyceiverIntegration] Initializing Meatyceiver 2 integration...");

            InitializeConfiguration();
            DetectMeatyceiver2();
            
            if (IsMeatyceiver2Available)
            {
                CacheMeatyceiverMethods();
                InitializeCompatibilityLayer();
                logger.LogInfo($"[MeatyceiverIntegration] Successfully initialized with Meatyceiver 2 {DetectedVersion} (API: {DetectedApiVersion})");
            }
            else
            {
                logger.LogInfo("[MeatyceiverIntegration] Meatyceiver 2 not detected - integration disabled");
            }

            initialized = true;
        }

        /// <summary>
        /// Initialize configuration entries for Meatyceiver 2
        /// </summary>
        private static void InitializeConfiguration()
        {
            chanceConfigs = new Dictionary<string, ConfigEntry<float>>();
            featureConfigs = new Dictionary<string, ConfigEntry<bool>>();
            multiplierConfigs = new Dictionary<string, ConfigEntry<float>>();
            intConfigs = new Dictionary<string, ConfigEntry<int>>();

            // Transformation chances
            chanceConfigs["Normal"] = config.Bind("Meatyceiver 2", "ChanceNormal", 0.02f, "Normal transformation chance");
            chanceConfigs["Elite"] = config.Bind("Meatyceiver 2", "ChanceElite", 0.01f, "Elite sosig transformation chance");
            chanceConfigs["Chaos"] = config.Bind("Meatyceiver 2", "ChanceChaos", 0.15f, "Chaos mode transformation chance");
            chanceConfigs["Player"] = config.Bind("Meatyceiver 2", "ChancePlayer", 0.05f, "Player weapon transformation chance");
            chanceConfigs["SosigWeapon"] = config.Bind("Meatyceiver 2", "ChanceSosigWeapon", 0.03f, "Sosig weapon transformation chance");
            chanceConfigs["EnemyWeapon"] = config.Bind("Meatyceiver 2", "ChanceEnemyWeapon", 0.04f, "Enemy weapon transformation chance");
            chanceConfigs["AllyWeapon"] = config.Bind("Meatyceiver 2", "ChanceAllyWeapon", 0.02f, "Ally weapon transformation chance");
            chanceConfigs["BossWeapon"] = config.Bind("Meatyceiver 2", "ChanceBossWeapon", 0.001f, "Boss weapon transformation chance");
            chanceConfigs["RareWeapon"] = config.Bind("Meatyceiver 2", "ChanceRareWeapon", 0.005f, "Rare weapon transformation chance");
            chanceConfigs["LegendaryWeapon"] = config.Bind("Meatyceiver 2", "ChanceLegendaryWeapon", 0.001f, "Legendary weapon transformation chance");

            // Category multipliers
            multiplierConfigs["Pistol"] = config.Bind("Meatyceiver 2", "PistolMultiplier", 1.2f, "Pistol transformation multiplier");
            multiplierConfigs["Rifle"] = config.Bind("Meatyceiver 2", "RifleMultiplier", 1.0f, "Rifle transformation multiplier");
            multiplierConfigs["Shotgun"] = config.Bind("Meatyceiver 2", "ShotgunMultiplier", 0.8f, "Shotgun transformation multiplier");
            multiplierConfigs["SMG"] = config.Bind("Meatyceiver 2", "SMGMultiplier", 1.5f, "SMG transformation multiplier");
            multiplierConfigs["Sniper"] = config.Bind("Meatyceiver 2", "SniperMultiplier", 0.5f, "Sniper transformation multiplier");
            multiplierConfigs["LMG"] = config.Bind("Meatyceiver 2", "LMGMultiplier", 0.7f, "LMG transformation multiplier");
            multiplierConfigs["AssaultRifle"] = config.Bind("Meatyceiver 2", "AssaultRifleMultiplier", 0.9f, "Assault rifle transformation multiplier");

            // Quality multipliers
            multiplierConfigs["CommonQuality"] = config.Bind("Meatyceiver 2", "CommonQualityMultiplier", 1.0f, "Common quality transformation multiplier");
            multiplierConfigs["UncommonQuality"] = config.Bind("Meatyceiver 2", "UncommonQualityMultiplier", 0.8f, "Uncommon quality transformation multiplier");
            multiplierConfigs["RareQuality"] = config.Bind("Meatyceiver 2", "RareQualityMultiplier", 0.6f, "Rare quality transformation multiplier");
            multiplierConfigs["EpicQuality"] = config.Bind("Meatyceiver 2", "EpicQualityMultiplier", 0.4f, "Epic quality transformation multiplier");
            multiplierConfigs["LegendaryQuality"] = config.Bind("Meatyceiver 2", "LegendaryQualityMultiplier", 0.2f, "Legendary quality transformation multiplier");
            multiplierConfigs["ArtifactQuality"] = config.Bind("Meatyceiver 2", "ArtifactQualityMultiplier", 0.1f, "Artifact quality transformation multiplier");

            // Feature flags
            featureConfigs["Enabled"] = config.Bind("Meatyceiver 2", "Enabled", true, "Enable Meatyceiver 2 integration");
            featureConfigs["ForceTransformOnChaos"] = config.Bind("Meatyceiver 2", "ForceTransformOnChaos", false, "Force transformation in chaos mode");
            featureConfigs["AllowMultipleTransforms"] = config.Bind("Meatyceiver 2", "AllowMultipleTransforms", false, "Allow multiple transformations");
            featureConfigs["PreserveAmmo"] = config.Bind("Meatyceiver 2", "PreserveAmmo", true, "Preserve ammo during transformation");
            featureConfigs["PreserveAttachments"] = config.Bind("Meatyceiver 2", "PreserveAttachments", true, "Preserve attachments during transformation");
            featureConfigs["PreserveQuality"] = config.Bind("Meatyceiver 2", "PreserveQuality", true, "Preserve weapon quality during transformation");
            featureConfigs["PlayTransformSound"] = config.Bind("Meatyceiver 2", "PlayTransformSound", true, "Play transformation sound effects");
            featureConfigs["ShowTransformParticles"] = config.Bind("Meatyceiver 2", "ShowTransformParticles", true, "Show transformation particle effects");
            featureConfigs["EnableCaching"] = config.Bind("Meatyceiver 2", "EnableCaching", true, "Enable transformation result caching");
            featureConfigs["EnableCooldowns"] = config.Bind("Meatyceiver 2", "EnableCooldowns", true, "Enable transformation cooldowns");
            featureConfigs["RespectOriginalChances"] = config.Bind("Meatyceiver 2", "RespectOriginalChances", true, "Respect Meatyceiver 2's original chances");
            featureConfigs["UseContextualLogic"] = config.Bind("Meatyceiver 2", "UseContextualLogic", true, "Use H3TVR contextual logic");
            featureConfigs["UseQualityBasedChances"] = config.Bind("Meatyceiver 2", "UseQualityBasedChances", true, "Use weapon quality to modify transformation chances");
            featureConfigs["EnableBatchTransformation"] = config.Bind("Meatyceiver 2", "EnableBatchTransformation", true, "Enable batch transformation support");
            featureConfigs["DebugMode"] = config.Bind("Meatyceiver 2", "DebugMode", false, "Enable debug mode");
            featureConfigs["VerboseLogging"] = config.Bind("Meatyceiver 2", "VerboseLogging", false, "Enable verbose logging");

            // Integer configurations
            intConfigs["CooldownSeconds"] = config.Bind("Meatyceiver 2", "CooldownSeconds", 30, "Cooldown between transformations (seconds)");
            intConfigs["CacheLifetimeMinutes"] = config.Bind("Meatyceiver 2", "CacheLifetimeMinutes", 10, "Cache entry lifetime in minutes");
            intConfigs["MaxCacheSize"] = config.Bind("Meatyceiver 2", "MaxCacheSize", 1000, "Maximum number of cached entries");
            intConfigs["BatchSize"] = config.Bind("Meatyceiver 2", "BatchSize", 10, "Maximum weapons to transform in a single batch");
        }

        /// <summary>
        /// Detect Meatyceiver 2 using multiple methods
        /// </summary>
        private static void DetectMeatyceiver2()
        {
            try
            {
                // Method 1: BepInEx Plugin Detection (Primary)
                var pluginInfos = BepInEx.Bootstrap.Chainloader.PluginInfos;
                if (pluginInfos.ContainsKey(MEATYCEIVER2_GUID))
                {
                    IsMeatyceiver2Available = true;
                    DetectedVersion = pluginInfos[MEATYCEIVER2_GUID].Metadata.Version.ToString();
                    logger.LogInfo($"[MeatyceiverIntegration] Meatyceiver 2 detected via BepInEx: v{DetectedVersion}");
                    return;
                }

                // Method 2: Legacy GUID Fallback
                if (pluginInfos.ContainsKey(MEATYCEIVER_LEGACY_GUID))
                {
                    IsMeatyceiver2Available = true;
                    DetectedVersion = pluginInfos[MEATYCEIVER_LEGACY_GUID].Metadata.Version.ToString();
                    logger.LogInfo($"[MeatyceiverIntegration] Meatyceiver detected via legacy GUID: v{DetectedVersion}");
                    return;
                }

                // Method 3: Alpha GUID Fallback
                if (pluginInfos.ContainsKey(MEATYCEIVER_ALPHA_GUID))
                {
                    IsMeatyceiver2Available = true;
                    DetectedVersion = pluginInfos[MEATYCEIVER_ALPHA_GUID].Metadata.Version.ToString();
                    logger.LogInfo($"[MeatyceiverIntegration] Meatyceiver Alpha detected via GUID: v{DetectedVersion}");
                    return;
                }

                // Method 4: Assembly Reflection
                DetectViaReflection();
            }
            catch (Exception ex)
            {
                logger.LogError($"[MeatyceiverIntegration] Error during Meatyceiver 2 detection: {ex.Message}");
                IsMeatyceiver2Available = false;
            }
        }

        /// <summary>
        /// Detect Meatyceiver 2 via reflection scanning
        /// </summary>
        private static void DetectViaReflection()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        // Look for Meatyceiver 2 specific patterns
                        if (IsMeatyceiverType(type))
                        {
                            meatyceiverType = type;
                            IsMeatyceiver2Available = true;
                            DetectedVersion = assembly.GetName().Version?.ToString() ?? "Unknown";
                            logger.LogInfo($"[MeatyceiverIntegration] Meatyceiver detected via reflection: {type.FullName}");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Skip problematic assemblies
                    logger.LogDebug($"[MeatyceiverIntegration] Could not scan assembly {assembly.FullName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Check if a type belongs to Meatyceiver 2
        /// </summary>
        private static bool IsMeatyceiverType(Type type)
        {
            if (type == null) return false;

            string typeName = type.Name.ToLower();
            string namespaceName = type.Namespace?.ToLower() ?? "";

            return typeName.Contains("meatyceiver") ||
                   typeName.Contains("meatyreceiver") ||
                   typeName.Contains("meattransform") ||
                   namespaceName.Contains("meatyceiver") ||
                   namespaceName.Contains("potatoes") ||
                   (typeName.Contains("meat") && (typeName.Contains("weapon") || typeName.Contains("transform")));
        }

        /// <summary>
        /// Cache important Meatyceiver 2 methods and properties
        /// </summary>
        private static void CacheMeatyceiverMethods()
        {
            if (meatyceiverType == null) return;

            try
            {
                // Get instance if it exists
                var instanceField = meatyceiverType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceField == null)
                    instanceField = meatyceiverType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
                
                if (instanceField != null)
                {
                    meatyceiverInstance = instanceField.GetValue(null);
                    logger.LogDebug("[MeatyceiverIntegration] Found Meatyceiver instance");
                }

                // Cache methods
                var methods = meatyceiverType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    string methodName = method.Name.ToLower();
                    
                    if ((methodName.Contains("transform") || methodName.Contains("meat")) && 
                        !methodName.Contains("check") && !methodName.Contains("is"))
                    {
                        transformMethod = method;
                        logger.LogDebug($"[MeatyceiverIntegration] Cached transform method: {method.Name}");
                    }
                    else if (methodName.Contains("check") || methodName.Contains("compatible") || methodName.Contains("can"))
                    {
                        checkCompatibilityMethod = method;
                        logger.LogDebug($"[MeatyceiverIntegration] Cached compatibility method: {method.Name}");
                    }
                    else if (methodName.Contains("is") && (methodName.Contains("transform") || methodName.Contains("meat")))
                    {
                        isTransformedMethod = method;
                        logger.LogDebug($"[MeatyceiverIntegration] Cached is-transformed method: {method.Name}");
                    }
                    else if (methodName.Contains("quality") && methodName.Contains("get"))
                    {
                        getQualityMethod = method;
                        logger.LogDebug($"[MeatyceiverIntegration] Cached get quality method: {method.Name}");
                    }
                    else if (methodName.Contains("quality") && methodName.Contains("set"))
                    {
                        setQualityMethod = method;
                        logger.LogDebug($"[MeatyceiverIntegration] Cached set quality method: {method.Name}");
                    }
                }

                // Cache properties
                var properties = meatyceiverType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                foreach (var property in properties)
                {
                    string propertyName = property.Name.ToLower();
                    
                    if (propertyName.Contains("chance") || propertyName.Contains("probability"))
                    {
                        transformChanceProperty = property;
                        logger.LogDebug($"[MeatyceiverIntegration] Cached chance property: {property.Name}");
                    }
                }

                // Cache fields
                var fields = meatyceiverType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    string fieldName = field.Name.ToLower();
                    
                    if (fieldName.Contains("enabled") || fieldName.Contains("active"))
                    {
                        enabledField = field;
                        logger.LogDebug($"[MeatyceiverIntegration] Cached enabled field: {field.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[MeatyceiverIntegration] Error caching Meatyceiver methods: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize compatibility layer for different Meatyceiver versions
        /// </summary>
        private static void InitializeCompatibilityLayer()
        {
            try
            {
                // Detect API version based on available methods
                if (getQualityMethod != null && setQualityMethod != null)
                {
                    DetectedApiVersion = "2.0+";
                    logger.LogDebug("[MeatyceiverIntegration] Detected advanced API with quality support");
                }
                else if (transformMethod != null)
                {
                    DetectedApiVersion = "1.5+";
                    logger.LogDebug("[MeatyceiverIntegration] Detected basic transformation API");
                }
                else
                {
                    DetectedApiVersion = "1.0";
                    logger.LogWarning("[MeatyceiverIntegration] Limited API detected - some features may not work");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[MeatyceiverIntegration] Error initializing compatibility layer: {ex.Message}");
                DetectedApiVersion = "Unknown";
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Try to transform a weapon using Meatyceiver 2 with contextual logic
        /// </summary>
        public static bool TryTransformWeapon(FVRFireArm firearm, string context = "Normal", float customChance = -1f, bool forceTransform = false)
        {
            if (!IsIntegrationEnabled() || firearm == null)
                return false;

            TotalTransformationAttempts++;

            try
            {
                // Check cooldown first
                string weaponKey = GetWeaponKey(firearm);
                if (featureConfigs["EnableCooldowns"].Value && IsOnCooldown(weaponKey) && !forceTransform)
                {
                    CooldownBlocked++;
                    if (featureConfigs["VerboseLogging"].Value)
                        logger.LogDebug($"[MeatyceiverIntegration] Transformation blocked by cooldown for {weaponKey}");
                    return false;
                }

                // Check cache
                if (featureConfigs["EnableCaching"].Value && transformationCache.ContainsKey(weaponKey))
                {
                    CachedResults++;
                    bool cachedResult = transformationCache[weaponKey];
                    
                    if (featureConfigs["VerboseLogging"].Value)
                        logger.LogDebug($"[MeatyceiverIntegration] Using cached result for {weaponKey}: {cachedResult}");
                    
                    return cachedResult;
                }

                // Check if weapon can be transformed
                if (!CanWeaponBeTransformed(firearm) && !forceTransform)
                {
                    CacheResult(weaponKey, false);
                    return false;
                }

                // Calculate transformation chance
                float chance = customChance >= 0 ? customChance : CalculateTransformationChance(firearm, context);
                
                // Apply chance check unless forced
                if (!forceTransform && UnityEngine.Random.value > chance)
                {
                    if (featureConfigs["VerboseLogging"].Value)
                        logger.LogDebug($"[MeatyceiverIntegration] Transformation chance failed: {chance:P2}");
                    
                    CacheResult(weaponKey, false);
                    return false;
                }

                // Attempt transformation
                bool success = PerformTransformation(firearm, context);
                
                if (success)
                {
                    SuccessfulTransformations++;
                    UpdateTransformationStatistics(context, firearm);
                    
                    // Set cooldown
                    if (featureConfigs["EnableCooldowns"].Value)
                        SetCooldown(weaponKey);
                    
                    logger.LogInfo($"[MeatyceiverIntegration] Successfully transformed {firearm.name} (context: {context})");
                    
                    // Play effects if enabled
                    if (featureConfigs["PlayTransformSound"].Value)
                        PlayTransformationSound(firearm.transform.position);
                    
                    if (featureConfigs["ShowTransformParticles"].Value)
                        ShowTransformationParticles(firearm.transform.position);
                }

                CacheResult(weaponKey, success);
                return success;
            }
            catch (Exception ex)
            {
                logger.LogError($"[MeatyceiverIntegration] Error during weapon transformation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to transform multiple weapons in a batch
        /// </summary>
        public static Dictionary<FVRFireArm, bool> TryTransformWeaponsBatch(List<FVRFireArm> firearms, string context = "Normal", float customChance = -1f, bool forceTransform = false)
        {
            var results = new Dictionary<FVRFireArm, bool>();
            
            if (!IsIntegrationEnabled() || !featureConfigs["EnableBatchTransformation"].Value)
            {
                foreach (var firearm in firearms)
                    results[firearm] = false;
                return results;
            }

            int batchSize = intConfigs["BatchSize"].Value;
            var batches = firearms.Select((x, i) => new { Index = i, Value = x })
                                 .GroupBy(x => x.Index / batchSize)
                                 .Select(x => x.Select(v => v.Value).ToList())
                                 .ToList();

            foreach (var batch in batches)
            {
                foreach (var firearm in batch)
                {
                    results[firearm] = TryTransformWeapon(firearm, context, customChance, forceTransform);
                }
            }

            logger.LogDebug($"[MeatyceiverIntegration] Batch transformation completed: {results.Count(r => r.Value)}/{results.Count} successful");
            return results;
        }

        /// <summary>
        /// Check if a weapon can be transformed
        /// </summary>
        public static bool CanWeaponBeTransformed(FVRFireArm firearm)
        {
            if (!IsIntegrationEnabled() || firearm == null)
                return false;

            try
            {
                // Use Meatyceiver's check method if available
                if (checkCompatibilityMethod != null)
                {
                    return (bool)checkCompatibilityMethod.Invoke(meatyceiverInstance, new object[] { firearm });
                }

                // Check if already transformed
                if (IsWeaponAlreadyTransformed(firearm))
                {
                    return featureConfigs["AllowMultipleTransforms"].Value;
                }

                // Basic compatibility check
                string weaponName = firearm.name.ToLower();
                return !weaponName.Contains("meat") && 
                       !weaponName.Contains("flesh") && 
                       !weaponName.Contains("organic") &&
                       !weaponName.Contains("bio");
            }
            catch (Exception ex)
            {
                logger.LogError($"[MeatyceiverIntegration] Error checking weapon compatibility: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a weapon is already transformed
        /// </summary>
        public static bool IsWeaponAlreadyTransformed(FVRFireArm firearm)
        {
            if (firearm == null) return false;

            try
            {
                // Use Meatyceiver's method if available
                if (isTransformedMethod != null)
                {
                    return (bool)isTransformedMethod.Invoke(meatyceiverInstance, new object[] { firearm });
                }

                // Fallback check
                string weaponName = firearm.name.ToLower();
                return weaponName.Contains("meat") || weaponName.Contains("flesh") || weaponName.Contains("organic");
            }
            catch (Exception ex)
            {
                logger.LogDebug($"[MeatyceiverIntegration] Error checking transformation status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get weapon quality if supported
        /// </summary>
        public static WeaponQuality GetWeaponQuality(FVRFireArm firearm)
        {
            if (firearm == null) return WeaponQuality.Common;

            try
            {
                string weaponKey = GetWeaponKey(firearm);
                
                // Check cache first
                if (weaponQualities.ContainsKey(weaponKey))
                    return weaponQualities[weaponKey];

                // Use Meatyceiver's method if available
                if (getQualityMethod != null)
                {
                    object result = getQualityMethod.Invoke(meatyceiverInstance, new object[] { firearm });
                    if (result is int qualityInt)
                    {
                        WeaponQuality quality = (WeaponQuality)Math.Min(qualityInt, (int)WeaponQuality.Artifact);
                        weaponQualities[weaponKey] = quality;
                        return quality;
                    }
                }

                // Fallback quality detection based on weapon name/properties
                WeaponQuality detectedQuality = DetectWeaponQualityFallback(firearm);
                weaponQualities[weaponKey] = detectedQuality;
                return detectedQuality;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"[MeatyceiverIntegration] Error getting weapon quality: {ex.Message}");
                return WeaponQuality.Common;
            }
        }

        /// <summary>
        /// Get transformation statistics
        /// </summary>
        public static string GetTransformationStats()
        {
            float successRate = TotalTransformationAttempts > 0 ? 
                (float)SuccessfulTransformations / TotalTransformationAttempts * 100f : 0f;

            var contextStats = string.Join(", ", TransformationsByContext.Select(kvp => $"{kvp.Key}: {kvp.Value}").ToArray());
            var weaponStats = string.Join(", ", TransformationsByWeaponType.Select(kvp => $"{kvp.Key}: {kvp.Value}").ToArray());

            return $"Meatyceiver 2 Integration Stats:\n" +
                   $"• Status: {(IsMeatyceiver2Available ? "✓ Active" : "✗ Not Available")}\n" +
                   $"• Version: {DetectedVersion} (API: {DetectedApiVersion})\n" +
                   $"• Attempts: {TotalTransformationAttempts}\n" +
                   $"• Successes: {SuccessfulTransformations}\n" +
                   $"• Success Rate: {successRate:F1}%\n" +
                   $"• Cached Results: {CachedResults}\n" +
                   $"• Cooldown Blocked: {CooldownBlocked}\n" +
                   $"• Quality Preserved: {QualityPreserved}\n" +
                   $"• Cache Size: {transformationCache.Count}\n" +
                   $"• By Context: {contextStats}\n" +
                   $"• By Weapon Type: {weaponStats}";
        }

        /// <summary>
        /// Clear transformation cache
        /// </summary>
        public static void ClearCache()
        {
            transformationCache.Clear();
            transformationTimes.Clear();
            transformationCooldowns.Clear();
            weaponQualities.Clear();
            CachedResults = 0;
            logger.LogDebug("[MeatyceiverIntegration] Cache cleared");
        }

        /// <summary>
        /// Check if integration is enabled and available
        /// </summary>
        public static bool IsIntegrationEnabled()
        {
            return IsMeatyceiver2Available && featureConfigs["Enabled"].Value;
        }

        /// <summary>
        /// Get detailed compatibility information
        /// </summary>
        public static string GetCompatibilityInfo()
        {
            if (!IsMeatyceiver2Available)
                return "Meatyceiver 2 not detected";

            var features = new List<string>();
            if (transformMethod != null) features.Add("Basic Transformation");
            if (checkCompatibilityMethod != null) features.Add("Compatibility Checking");
            if (isTransformedMethod != null) features.Add("Transform Status Detection");
            if (getQualityMethod != null) features.Add("Quality Reading");
            if (setQualityMethod != null) features.Add("Quality Setting");

            return $"Meatyceiver 2 Compatibility:\n" +
                   $"• Version: {DetectedVersion}\n" +
                   $"• API Version: {DetectedApiVersion}\n" +
                   $"• Available Features: {string.Join(", ", features.ToArray())}\n" +
                   $"• Integration Status: {(IsIntegrationEnabled() ? "Active" : "Disabled")}";
        }

        /// <summary>
        /// Get a feature configuration value
        /// </summary>
        public static bool GetFeatureConfig(string key)
        {
            if (!featureConfigs.ContainsKey(key))
                return false;
            return featureConfigs[key].Value;
        }

        /// <summary>
        /// Get a chance configuration value
        /// </summary>
        public static float GetChanceConfig(string key)
        {
            if (!chanceConfigs.ContainsKey(key))
                return 0.02f; // default 2%
            return chanceConfigs[key].Value;
        }

        /// <summary>
        /// Get a multiplier configuration value
        /// </summary>
        public static float GetMultiplierConfig(string key)
        {
            if (!multiplierConfigs.ContainsKey(key))
                return 1.0f; // default multiplier
            return multiplierConfigs[key].Value;
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Calculate transformation chance based on context and weapon
        /// </summary>
        private static float CalculateTransformationChance(FVRFireArm firearm, string context)
        {
            float baseChance = 0.02f; // Default 2%

            // Get base chance from context
            if (chanceConfigs.ContainsKey(context))
            {
                baseChance = chanceConfigs[context].Value;
            }
            else
            {
                // Context-based chance determination
                context = context.ToLower();
                if (context.Contains("chaos"))
                    baseChance = chanceConfigs["Chaos"].Value;
                else if (context.Contains("elite") || context.Contains("boss"))
                    baseChance = chanceConfigs["Elite"].Value;
                else if (context.Contains("player"))
                    baseChance = chanceConfigs["Player"].Value;
                else if (context.Contains("enemy"))
                    baseChance = chanceConfigs["EnemyWeapon"].Value;
                else if (context.Contains("ally"))
                    baseChance = chanceConfigs["AllyWeapon"].Value;
                else
                    baseChance = chanceConfigs["Normal"].Value;
            }

            // Apply weapon category multiplier
            float categoryMultiplier = GetWeaponCategoryMultiplier(firearm);
            float qualityMultiplier = 1.0f;
            
            // Apply quality-based multiplier if enabled
            if (featureConfigs["UseQualityBasedChances"].Value)
            {
                qualityMultiplier = GetQualityMultiplier(firearm);
            }

            float finalChance = baseChance * categoryMultiplier * qualityMultiplier;

            // Respect original Meatyceiver chances if configured
            if (featureConfigs["RespectOriginalChances"].Value && transformChanceProperty != null)
            {
                try
                {
                    float originalChance = (float)transformChanceProperty.GetValue(meatyceiverInstance, null);
                    finalChance = Math.Min(finalChance, originalChance);
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"[MeatyceiverIntegration] Could not get original chance: {ex.Message}");
                }
            }

            if (featureConfigs["DebugMode"].Value)
            {
                logger.LogDebug($"[MeatyceiverIntegration] Calculated chance for {firearm.name} ({context}): {finalChance:P2} (base: {baseChance:P2}, category: {categoryMultiplier:F2}, quality: {qualityMultiplier:F2})");
            }

            return Mathf.Clamp01(finalChance);
        }

        /// <summary>
        /// Get weapon category multiplier
        /// </summary>
        private static float GetWeaponCategoryMultiplier(FVRFireArm firearm)
        {
            string weaponName = firearm.name.ToLower();

            if (weaponName.Contains("pistol") || weaponName.Contains("handgun"))
                return multiplierConfigs["Pistol"].Value;
            else if (weaponName.Contains("shotgun"))
                return multiplierConfigs["Shotgun"].Value;
            else if (weaponName.Contains("smg") || weaponName.Contains("submachine"))
                return multiplierConfigs["SMG"].Value;
            else if (weaponName.Contains("sniper") || weaponName.Contains("precision"))
                return multiplierConfigs["Sniper"].Value;
            else if (weaponName.Contains("lmg") || weaponName.Contains("machinegun"))
                return multiplierConfigs["LMG"].Value;
            else if (weaponName.Contains("assault") || weaponName.Contains("carbine"))
                return multiplierConfigs["AssaultRifle"].Value;
            else if (weaponName.Contains("rifle"))
                return multiplierConfigs["Rifle"].Value;

            return 1.0f; // Default multiplier
        }

        /// <summary>
        /// Get quality-based multiplier
        /// </summary>
        private static float GetQualityMultiplier(FVRFireArm firearm)
        {
            WeaponQuality quality = GetWeaponQuality(firearm);
            
            switch (quality)
            {
                case WeaponQuality.Common:
                    return multiplierConfigs["CommonQuality"].Value;
                case WeaponQuality.Uncommon:
                    return multiplierConfigs["UncommonQuality"].Value;
                case WeaponQuality.Rare:
                    return multiplierConfigs["RareQuality"].Value;
                case WeaponQuality.Epic:
                    return multiplierConfigs["EpicQuality"].Value;
                case WeaponQuality.Legendary:
                    return multiplierConfigs["LegendaryQuality"].Value;
                case WeaponQuality.Artifact:
                    return multiplierConfigs["ArtifactQuality"].Value;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Detect weapon quality using fallback methods
        /// </summary>
        private static WeaponQuality DetectWeaponQualityFallback(FVRFireArm firearm)
        {
            string weaponName = firearm.name.ToLower();
            
            // Quality detection based on name patterns
            if (weaponName.Contains("legendary") || weaponName.Contains("mythic") || weaponName.Contains("unique"))
                return WeaponQuality.Legendary;
            else if (weaponName.Contains("epic") || weaponName.Contains("purple") || weaponName.Contains("elite"))
                return WeaponQuality.Epic;
            else if (weaponName.Contains("rare") || weaponName.Contains("blue") || weaponName.Contains("special"))
                return WeaponQuality.Rare;
            else if (weaponName.Contains("uncommon") || weaponName.Contains("green") || weaponName.Contains("enhanced"))
                return WeaponQuality.Uncommon;
            
            return WeaponQuality.Common;
        }

        /// <summary>
        /// Perform the actual transformation
        /// </summary>
        private static bool PerformTransformation(FVRFireArm firearm, string context)
        {
            if (transformMethod == null)
            {
                logger.LogWarning("[MeatyceiverIntegration] No transform method available");
                return false;
            }

            try
            {
                // Store original properties if preservation is enabled
                int originalAmmo = 0;
                WeaponQuality originalQuality = WeaponQuality.Common;
                List<FVRFireArmAttachment> originalAttachments = new List<FVRFireArmAttachment>();

                if (featureConfigs["PreserveAmmo"].Value && firearm.Magazine != null)
                {
                    originalAmmo = firearm.Magazine.m_numRounds;
                }

                if (featureConfigs["PreserveQuality"].Value)
                {
                    originalQuality = GetWeaponQuality(firearm);
                }

                if (featureConfigs["PreserveAttachments"].Value)
                {
                    originalAttachments.AddRange(firearm.GetComponentsInChildren<FVRFireArmAttachment>());
                }

                // Perform transformation
                object result = transformMethod.Invoke(meatyceiverInstance, new object[] { firearm });
                bool success = result is bool ? (bool)result : true;

                // Restore properties if transformation was successful and preservation is enabled
                if (success)
                {
                    if (featureConfigs["PreserveAmmo"].Value && firearm.Magazine != null && originalAmmo > 0)
                    {
                        firearm.Magazine.m_numRounds = originalAmmo;
                    }

                    if (featureConfigs["PreserveQuality"].Value && setQualityMethod != null && originalQuality > WeaponQuality.Common)
                    {
                        try
                        {
                            setQualityMethod.Invoke(meatyceiverInstance, new object[] { firearm, (int)originalQuality });
                            QualityPreserved++;
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug($"[MeatyceiverIntegration] Could not preserve quality: {ex.Message}");
                        }
                    }

                    // Note: Attachment preservation is complex and may require additional Meatyceiver 2 API support
                }

                return success;
            }
            catch (Exception ex)
            {
                logger.LogError($"[MeatyceiverIntegration] Transformation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if weapon is on cooldown
        /// </summary>
        private static bool IsOnCooldown(string weaponKey)
        {
            if (!transformationCooldowns.ContainsKey(weaponKey))
                return false;

            var cooldownEnd = transformationCooldowns[weaponKey].AddSeconds(intConfigs["CooldownSeconds"].Value);
            return DateTime.Now < cooldownEnd;
        }

        /// <summary>
        /// Set cooldown for weapon
        /// </summary>
        private static void SetCooldown(string weaponKey)
        {
            transformationCooldowns[weaponKey] = DateTime.Now;
        }

        /// <summary>
        /// Update transformation statistics
        /// </summary>
        private static void UpdateTransformationStatistics(string context, FVRFireArm firearm)
        {
            // Update context statistics
            if (!TransformationsByContext.ContainsKey(context))
                TransformationsByContext[context] = 0;
            TransformationsByContext[context]++;

            // Update weapon type statistics
            string weaponType = GetWeaponTypeName(firearm);
            if (!TransformationsByWeaponType.ContainsKey(weaponType))
                TransformationsByWeaponType[weaponType] = 0;
            TransformationsByWeaponType[weaponType]++;
        }

        /// <summary>
        /// Get weapon type name for statistics
        /// </summary>
        private static string GetWeaponTypeName(FVRFireArm firearm)
        {
            string weaponName = firearm.name.ToLower();
            
            if (weaponName.Contains("pistol") || weaponName.Contains("handgun"))
                return "Pistol";
            else if (weaponName.Contains("shotgun"))
                return "Shotgun";
            else if (weaponName.Contains("smg") || weaponName.Contains("submachine"))
                return "SMG";
            else if (weaponName.Contains("sniper"))
                return "Sniper";
            else if (weaponName.Contains("lmg"))
                return "LMG";
            else if (weaponName.Contains("rifle"))
                return "Rifle";
            
            return "Unknown";
        }

        /// <summary>
        /// Play transformation sound effect
        /// </summary>
        private static void PlayTransformationSound(Vector3 position)
        {
            try
            {
                // Try to use H3TVR's AudioManager if available
                var audioManager = UnityEngine.Object.FindObjectOfType<AudioManager>();
                if (audioManager != null)
                {
                    audioManager.PlayWeaponSpawnSound("transformation", position, true);
                }
                else
                {
                    logger.LogDebug($"[MeatyceiverIntegration] Playing transformation sound at {position}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"[MeatyceiverIntegration] Could not play transformation sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Show transformation particle effects
        /// </summary>
        private static void ShowTransformationParticles(Vector3 position)
        {
            try
            {
                // Try to use H3TVR's EffectsManager if available
                var effectsManager = UnityEngine.Object.FindObjectOfType<EffectsManager>();
                if (effectsManager != null)
                {
                    // Create a simple particle effect at the position
                    logger.LogDebug($"[MeatyceiverIntegration] Showing transformation particles at {position}");
                }
                else
                {
                    logger.LogDebug($"[MeatyceiverIntegration] Showing transformation particles at {position}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"[MeatyceiverIntegration] Could not show transformation particles: {ex.Message}");
            }
        }

        /// <summary>
        /// Get unique key for weapon caching
        /// </summary>
        private static string GetWeaponKey(FVRFireArm firearm)
        {
            try
            {
                if (firearm.ObjectWrapper?.ItemID != null)
                    return firearm.ObjectWrapper.ItemID;
                
                return firearm.gameObject.GetInstanceID().ToString();
            }
            catch
            {
                return firearm.GetHashCode().ToString();
            }
        }

        /// <summary>
        /// Cache transformation result
        /// </summary>
        private static void CacheResult(string weaponKey, bool result)
        {
            if (!featureConfigs["EnableCaching"].Value) return;

            transformationCache[weaponKey] = result;
            transformationTimes[weaponKey] = DateTime.Now;

            // Enforce cache size limit
            if (transformationCache.Count > intConfigs["MaxCacheSize"].Value)
            {
                CleanOldCacheEntries(true);
            }

            // Clean cache periodically
            if ((DateTime.Now - lastCacheClear).TotalMinutes > 5)
            {
                CleanOldCacheEntries();
                lastCacheClear = DateTime.Now;
            }
        }

        /// <summary>
        /// Clean old cache entries to prevent memory leaks
        /// </summary>
        private static void CleanOldCacheEntries(bool forceClear = false)
        {
            var cutoffTime = DateTime.Now.AddMinutes(-intConfigs["CacheLifetimeMinutes"].Value);
            var keysToRemove = new List<string>();

            foreach (var kvp in transformationTimes)
            {
                if (kvp.Value < cutoffTime || forceClear)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            // If force clearing, remove oldest entries first
            if (forceClear && keysToRemove.Count < transformationCache.Count / 2)
            {
                var sortedEntries = transformationTimes.OrderBy(kvp => kvp.Value).Take(transformationCache.Count / 2);
                keysToRemove.AddRange(sortedEntries.Select(kvp => kvp.Key));
            }

            foreach (var key in keysToRemove)
            {
                transformationCache.Remove(key);
                transformationTimes.Remove(key);
                weaponQualities.Remove(key);
            }

            if (keysToRemove.Count > 0)
            {
                logger.LogDebug($"[MeatyceiverIntegration] Cleaned {keysToRemove.Count} cache entries");
            }
        }
        #endregion
    }
}