using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using FistVR;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.Linq;
using System.Collections;

namespace H3TVR
{
    /// <summary>
    /// Advanced Stovepipe Integration Manager for H3TVR
    /// Handles all aspects of Stovepipe mod integration including detection,
    /// malfunction logic, configuration, and contextual behavior
    /// </summary>
    public static class StovepipeIntegrationManager
    {
        #region Fields and Properties
        private static ManualLogSource logger;
        private static bool initialized = false;

        // Stovepipe Detection
        public static bool IsStovepipeAvailable { get; private set; } = false;
        public static string DetectedVersion { get; private set; } = "Unknown";
        public static string DetectedApiVersion { get; private set; } = "Unknown";
        
        // Cached Stovepipe Components
        private static Type stovepipeType;
        private static object stovepipeInstance;
        private static MethodInfo forceMalfunctionMethod;
        private static MethodInfo checkCompatibilityMethod;
        private static MethodInfo getJamStateMethod;
        private static MethodInfo clearJamMethod;
        private static MethodInfo setJamChanceMethod;
        private static MethodInfo getMalfunctionTypeMethod;
        private static PropertyInfo malfunctionChanceProperty;
        private static PropertyInfo jamStateProperty;
        private static FieldInfo enabledField;

        // Configuration
        private static ConfigFile config;
        private static Dictionary<string, ConfigEntry<float>> chanceConfigs;
        private static Dictionary<string, ConfigEntry<bool>> featureConfigs;
        private static Dictionary<string, ConfigEntry<float>> multiplierConfigs;
        private static Dictionary<string, ConfigEntry<int>> intConfigs;

        // Caching and Performance
        private static readonly Dictionary<string, bool> jamCapabilityCache = new Dictionary<string, bool>();
        private static readonly Dictionary<string, DateTime> lastJamTimes = new Dictionary<string, DateTime>();
        private static readonly Dictionary<string, MalfunctionType> lastMalfunctionTypes = new Dictionary<string, MalfunctionType>();
        private static readonly Dictionary<string, DateTime> jamCooldowns = new Dictionary<string, DateTime>();
        private static DateTime lastCacheClear = DateTime.Now;

        // Advanced Statistics
        public static int TotalMalfunctionAttempts { get; private set; } = 0;
        public static int SuccessfulMalfunctions { get; private set; } = 0;
        public static int CachedResults { get; private set; } = 0;
        public static int CooldownBlocked { get; private set; } = 0;
        public static int JamsCleared { get; private set; } = 0;
        public static Dictionary<string, int> MalfunctionsByContext { get; private set; } = new Dictionary<string, int>();
        public static Dictionary<string, int> MalfunctionsByWeaponType { get; private set; } = new Dictionary<string, int>();
        public static Dictionary<MalfunctionType, int> MalfunctionsByType { get; private set; } = new Dictionary<MalfunctionType, int>();

        // Plugin GUIDs for detection
        private const string STOVEPIPE_GUID = "dll.stovepipe";
        private const string STOVEPIPE_LEGACY_GUID = "stovepipe.weapon.jams";
        private const string STOVEPIPE_ALPHA_GUID = "stovepipe.alpha";
        private const string STOVEPIPE_BETA_GUID = "stovepipe.beta";

        // Malfunction Types Enum
        public enum MalfunctionType
        {
            None = 0,
            Stovepipe = 1,      // Spent casing stuck in ejection port
            DoubleFeed = 2,     // Two rounds trying to feed at once
            FailureToFeed = 3,  // Round fails to chamber properly
            FailureToEject = 4, // Spent casing fails to eject
            FailureToFire = 5,  // Round fails to ignite
            HangFire = 6,       // Delayed ignition
            SquibLoad = 7,      // Low power round, bullet stuck in barrel
            SlamFire = 8,       // Uncontrolled automatic firing
            OutOfBattery = 9,   // Bolt/slide not fully closed
            BrokenExtractor = 10, // Extractor damage/failure
            DirtyGun = 11,      // Fouling-related malfunctions
            AmmoIssue = 12      // Ammunition-related problems
        }

        // Weapon Category Enum for jam susceptibility
        public enum WeaponCategory
        {
            Unknown = 0,
            Pistol = 1,
            Rifle = 2,
            Shotgun = 3,
            SMG = 4,
            LMG = 5,
            AssaultRifle = 6,
            Sniper = 7,
            Revolver = 8,
            Bolt = 9,
            Pump = 10
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the Stovepipe integration manager
        /// </summary>
        public static void Initialize(ManualLogSource logSource, ConfigFile configFile)
        {
            if (initialized) return;

            logger = logSource;
            config = configFile;
            
            logger.LogInfo("[StovepipeIntegration] Initializing Stovepipe integration...");

            InitializeConfiguration();
            DetectStovepipe();
            
            if (IsStovepipeAvailable)
            {
                CacheStovepipeMethods();
                InitializeCompatibilityLayer();
                logger.LogInfo($"[StovepipeIntegration] Successfully initialized with Stovepipe {DetectedVersion} (API: {DetectedApiVersion})");
            }
            else
            {
                logger.LogInfo("[StovepipeIntegration] Stovepipe not detected - integration disabled");
            }

            initialized = true;
        }

        /// <summary>
        /// Initialize configuration entries for Stovepipe
        /// </summary>
        private static void InitializeConfiguration()
        {
            chanceConfigs = new Dictionary<string, ConfigEntry<float>>();
            featureConfigs = new Dictionary<string, ConfigEntry<bool>>();
            multiplierConfigs = new Dictionary<string, ConfigEntry<float>>();
            intConfigs = new Dictionary<string, ConfigEntry<int>>();

            // Malfunction chances by context
            chanceConfigs["Normal"] = config.Bind("Stovepipe", "ChanceNormal", 0.01f, "Normal malfunction chance");
            chanceConfigs["Combat"] = config.Bind("Stovepipe", "ChanceCombat", 0.03f, "Combat stress malfunction chance");
            chanceConfigs["Dirty"] = config.Bind("Stovepipe", "ChanceDirty", 0.08f, "Dirty weapon malfunction chance");
            chanceConfigs["Player"] = config.Bind("Stovepipe", "ChancePlayer", 0.02f, "Player weapon malfunction chance");
            chanceConfigs["Enemy"] = config.Bind("Stovepipe", "ChanceEnemy", 0.04f, "Enemy weapon malfunction chance");
            chanceConfigs["Ally"] = config.Bind("Stovepipe", "ChanceAlly", 0.015f, "Ally weapon malfunction chance");
            chanceConfigs["Elite"] = config.Bind("Stovepipe", "ChanceElite", 0.005f, "Elite weapon malfunction chance");
            chanceConfigs["Boss"] = config.Bind("Stovepipe", "ChanceBoss", 0.001f, "Boss weapon malfunction chance");
            chanceConfigs["WornOut"] = config.Bind("Stovepipe", "ChanceWornOut", 0.12f, "Worn out weapon malfunction chance");
            chanceConfigs["Overheated"] = config.Bind("Stovepipe", "ChanceOverheated", 0.06f, "Overheated weapon malfunction chance");

            // Category multipliers
            multiplierConfigs["Pistol"] = config.Bind("Stovepipe", "PistolMultiplier", 1.2f, "Pistol malfunction multiplier");
            multiplierConfigs["Rifle"] = config.Bind("Stovepipe", "RifleMultiplier", 0.8f, "Rifle malfunction multiplier");
            multiplierConfigs["Shotgun"] = config.Bind("Stovepipe", "ShotgunMultiplier", 0.6f, "Shotgun malfunction multiplier");
            multiplierConfigs["SMG"] = config.Bind("Stovepipe", "SMGMultiplier", 1.4f, "SMG malfunction multiplier");
            multiplierConfigs["LMG"] = config.Bind("Stovepipe", "LMGMultiplier", 1.1f, "LMG malfunction multiplier");
            multiplierConfigs["AssaultRifle"] = config.Bind("Stovepipe", "AssaultRifleMultiplier", 0.9f, "Assault rifle malfunction multiplier");
            multiplierConfigs["Sniper"] = config.Bind("Stovepipe", "SniperMultiplier", 0.7f, "Sniper rifle malfunction multiplier");
            multiplierConfigs["Revolver"] = config.Bind("Stovepipe", "RevolverMultiplier", 0.3f, "Revolver malfunction multiplier");
            multiplierConfigs["Bolt"] = config.Bind("Stovepipe", "BoltMultiplier", 0.2f, "Bolt action malfunction multiplier");
            multiplierConfigs["Pump"] = config.Bind("Stovepipe", "PumpMultiplier", 0.4f, "Pump action malfunction multiplier");

            // Malfunction type multipliers
            multiplierConfigs["StovepipeChance"] = config.Bind("Stovepipe", "StovepipeChance", 1.5f, "Stovepipe jam chance multiplier");
            multiplierConfigs["DoubleFeedChance"] = config.Bind("Stovepipe", "DoubleFeedChance", 1.0f, "Double feed chance multiplier");
            multiplierConfigs["FailureToFeedChance"] = config.Bind("Stovepipe", "FailureToFeedChance", 1.2f, "Failure to feed chance multiplier");
            multiplierConfigs["FailureToEjectChance"] = config.Bind("Stovepipe", "FailureToEjectChance", 1.1f, "Failure to eject chance multiplier");
            multiplierConfigs["FailureToFireChance"] = config.Bind("Stovepipe", "FailureToFireChance", 0.8f, "Failure to fire chance multiplier");
            multiplierConfigs["HangFireChance"] = config.Bind("Stovepipe", "HangFireChance", 0.3f, "Hang fire chance multiplier");

            // Feature flags
            featureConfigs["Enabled"] = config.Bind("Stovepipe", "Enabled", true, "Enable Stovepipe integration");
            featureConfigs["AutoClearJams"] = config.Bind("Stovepipe", "AutoClearJams", false, "Automatically clear jams after delay");
            featureConfigs["ContextualMalfunctions"] = config.Bind("Stovepipe", "ContextualMalfunctions", true, "Use contextual malfunction logic");
            featureConfigs["RealisticJamTypes"] = config.Bind("Stovepipe", "RealisticJamTypes", true, "Use realistic jam types based on weapon");
            featureConfigs["EnableCooldowns"] = config.Bind("Stovepipe", "EnableCooldowns", true, "Enable jam cooldowns");
            featureConfigs["EnableCaching"] = config.Bind("Stovepipe", "EnableCaching", true, "Enable jam capability caching");
            featureConfigs["DirtAccumulation"] = config.Bind("Stovepipe", "DirtAccumulation", true, "Enable dirt accumulation system");
            featureConfigs["HeatBuildup"] = config.Bind("Stovepipe", "HeatBuildup", true, "Enable heat buildup system");
            featureConfigs["AmmoQualityAffectsJams"] = config.Bind("Stovepipe", "AmmoQualityAffectsJams", true, "Ammunition quality affects jam chance");
            featureConfigs["WeaponConditionTracking"] = config.Bind("Stovepipe", "WeaponConditionTracking", true, "Track weapon condition for jam chances");
            featureConfigs["PlayMalfunctionSounds"] = config.Bind("Stovepipe", "PlayMalfunctionSounds", true, "Play malfunction sound effects");
            featureConfigs["ShowMalfunctionParticles"] = config.Bind("Stovepipe", "ShowMalfunctionParticles", true, "Show malfunction particle effects");
            featureConfigs["EnableBatchJamming"] = config.Bind("Stovepipe", "EnableBatchJamming", true, "Enable batch jamming support");
            featureConfigs["DebugMode"] = config.Bind("Stovepipe", "DebugMode", false, "Enable debug mode");
            featureConfigs["VerboseLogging"] = config.Bind("Stovepipe", "VerboseLogging", false, "Enable verbose logging");

            // Integer configurations
            intConfigs["JamCooldownSeconds"] = config.Bind("Stovepipe", "JamCooldownSeconds", 5, "Cooldown between jams (seconds)");
            intConfigs["AutoClearDelaySeconds"] = config.Bind("Stovepipe", "AutoClearDelaySeconds", 15, "Auto clear jam delay (seconds)");
            intConfigs["CacheLifetimeMinutes"] = config.Bind("Stovepipe", "CacheLifetimeMinutes", 15, "Cache entry lifetime in minutes");
            intConfigs["MaxCacheSize"] = config.Bind("Stovepipe", "MaxCacheSize", 500, "Maximum number of cached entries");
            intConfigs["BatchSize"] = config.Bind("Stovepipe", "BatchSize", 5, "Maximum weapons to jam in a single batch");
            intConfigs["MaxDirtLevel"] = config.Bind("Stovepipe", "MaxDirtLevel", 100, "Maximum dirt accumulation level");
            intConfigs["MaxHeatLevel"] = config.Bind("Stovepipe", "MaxHeatLevel", 100, "Maximum heat buildup level");
        }

        /// <summary>
        /// Detect Stovepipe using multiple methods
        /// </summary>
        private static void DetectStovepipe()
        {
            try
            {
                // Method 1: BepInEx Plugin Detection (Primary)
                var pluginInfos = BepInEx.Bootstrap.Chainloader.PluginInfos;
                if (pluginInfos.ContainsKey(STOVEPIPE_GUID))
                {
                    IsStovepipeAvailable = true;
                    DetectedVersion = pluginInfos[STOVEPIPE_GUID].Metadata.Version.ToString();
                    logger.LogInfo($"[StovepipeIntegration] Stovepipe detected via BepInEx: v{DetectedVersion}");
                    return;
                }

                // Method 2: Legacy GUID Fallback
                if (pluginInfos.ContainsKey(STOVEPIPE_LEGACY_GUID))
                {
                    IsStovepipeAvailable = true;
                    DetectedVersion = pluginInfos[STOVEPIPE_LEGACY_GUID].Metadata.Version.ToString();
                    logger.LogInfo($"[StovepipeIntegration] Stovepipe detected via legacy GUID: v{DetectedVersion}");
                    return;
                }

                // Method 3: Alpha GUID Fallback
                if (pluginInfos.ContainsKey(STOVEPIPE_ALPHA_GUID))
                {
                    IsStovepipeAvailable = true;
                    DetectedVersion = pluginInfos[STOVEPIPE_ALPHA_GUID].Metadata.Version.ToString();
                    logger.LogInfo($"[StovepipeIntegration] Stovepipe Alpha detected: v{DetectedVersion}");
                    return;
                }

                // Method 4: Beta GUID Fallback
                if (pluginInfos.ContainsKey(STOVEPIPE_BETA_GUID))
                {
                    IsStovepipeAvailable = true;
                    DetectedVersion = pluginInfos[STOVEPIPE_BETA_GUID].Metadata.Version.ToString();
                    logger.LogInfo($"[StovepipeIntegration] Stovepipe Beta detected: v{DetectedVersion}");
                    return;
                }

                // Method 5: Assembly Reflection
                DetectViaReflection();
            }
            catch (Exception ex)
            {
                logger.LogError($"[StovepipeIntegration] Error during Stovepipe detection: {ex.Message}");
                IsStovepipeAvailable = false;
            }
        }

        /// <summary>
        /// Detect Stovepipe via reflection scanning
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
                        // Look for Stovepipe-specific patterns
                        if (IsStovepipeType(type))
                        {
                            stovepipeType = type;
                            IsStovepipeAvailable = true;
                            DetectedVersion = assembly.GetName().Version?.ToString() ?? "Unknown";
                            logger.LogInfo($"[StovepipeIntegration] Stovepipe detected via reflection: {type.FullName}");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Skip problematic assemblies
                    logger.LogDebug($"[StovepipeIntegration] Could not scan assembly {assembly.FullName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Check if a type belongs to Stovepipe
        /// </summary>
        private static bool IsStovepipeType(Type type)
        {
            if (type == null) return false;

            string typeName = type.Name.ToLower();
            string namespaceName = type.Namespace?.ToLower() ?? "";

            return typeName.Contains("stovepipe") ||
                   typeName.Contains("jam") ||
                   typeName.Contains("malfunction") ||
                   namespaceName.Contains("stovepipe") ||
                   namespaceName.Contains("jamming") ||
                   (typeName.Contains("weapon") && (typeName.Contains("jam") || typeName.Contains("malfunction")));
        }

        /// <summary>
        /// Cache important Stovepipe methods and properties
        /// </summary>
        private static void CacheStovepipeMethods()
        {
            if (stovepipeType == null) return;

            try
            {
                // Get instance if it exists
                var instanceField = stovepipeType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceField == null)
                    instanceField = stovepipeType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
                
                if (instanceField != null)
                {
                    stovepipeInstance = instanceField.GetValue(null);
                    logger.LogDebug("[StovepipeIntegration] Found Stovepipe instance");
                }

                // Cache methods
                var methods = stovepipeType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    string methodName = method.Name.ToLower();
                    
                    if ((methodName.Contains("force") || methodName.Contains("trigger")) && 
                        (methodName.Contains("jam") || methodName.Contains("malfunction")))
                    {
                        forceMalfunctionMethod = method;
                        logger.LogDebug($"[StovepipeIntegration] Cached force malfunction method: {method.Name}");
                    }
                    else if (methodName.Contains("check") || methodName.Contains("compatible") || methodName.Contains("can"))
                    {
                        checkCompatibilityMethod = method;
                        logger.LogDebug($"[StovepipeIntegration] Cached compatibility method: {method.Name}");
                    }
                    else if (methodName.Contains("clear") && (methodName.Contains("jam") || methodName.Contains("malfunction")))
                    {
                        clearJamMethod = method;
                        logger.LogDebug($"[StovepipeIntegration] Cached clear jam method: {method.Name}");
                    }
                    else if (methodName.Contains("get") && (methodName.Contains("jam") || methodName.Contains("state")))
                    {
                        getJamStateMethod = method;
                        logger.LogDebug($"[StovepipeIntegration] Cached get jam state method: {method.Name}");
                    }
                    else if (methodName.Contains("set") && methodName.Contains("chance"))
                    {
                        setJamChanceMethod = method;
                        logger.LogDebug($"[StovepipeIntegration] Cached set jam chance method: {method.Name}");
                    }
                    else if (methodName.Contains("get") && methodName.Contains("type"))
                    {
                        getMalfunctionTypeMethod = method;
                        logger.LogDebug($"[StovepipeIntegration] Cached get malfunction type method: {method.Name}");
                    }
                }

                // Cache properties
                var properties = stovepipeType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                foreach (var property in properties)
                {
                    string propertyName = property.Name.ToLower();
                    
                    if (propertyName.Contains("chance") || propertyName.Contains("probability"))
                    {
                        malfunctionChanceProperty = property;
                        logger.LogDebug($"[StovepipeIntegration] Cached chance property: {property.Name}");
                    }
                    else if (propertyName.Contains("jam") && propertyName.Contains("state"))
                    {
                        jamStateProperty = property;
                        logger.LogDebug($"[StovepipeIntegration] Cached jam state property: {property.Name}");
                    }
                }

                // Cache fields
                var fields = stovepipeType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    string fieldName = field.Name.ToLower();
                    
                    if (fieldName.Contains("enabled") || fieldName.Contains("active"))
                    {
                        enabledField = field;
                        logger.LogDebug($"[StovepipeIntegration] Cached enabled field: {field.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[StovepipeIntegration] Error caching Stovepipe methods: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize compatibility layer for different Stovepipe versions
        /// </summary>
        private static void InitializeCompatibilityLayer()
        {
            try
            {
                // Detect API version based on available methods
                if (getMalfunctionTypeMethod != null && setJamChanceMethod != null)
                {
                    DetectedApiVersion = "2.0+";
                    logger.LogDebug("[StovepipeIntegration] Detected advanced API with malfunction types");
                }
                else if (forceMalfunctionMethod != null && clearJamMethod != null)
                {
                    DetectedApiVersion = "1.5+";
                    logger.LogDebug("[StovepipeIntegration] Detected standard jamming API");
                }
                else if (forceMalfunctionMethod != null)
                {
                    DetectedApiVersion = "1.0+";
                    logger.LogDebug("[StovepipeIntegration] Detected basic jamming API");
                }
                else
                {
                    DetectedApiVersion = "Limited";
                    logger.LogWarning("[StovepipeIntegration] Limited API detected - some features may not work");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[StovepipeIntegration] Error initializing compatibility layer: {ex.Message}");
                DetectedApiVersion = "Unknown";
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Get feature configuration value
        /// </summary>
        public static bool GetFeatureConfig(string key)
        {
            if (featureConfigs != null && featureConfigs.ContainsKey(key))
                return featureConfigs[key].Value;
            return false;
        }

        /// <summary>
        /// Try to cause a malfunction on a weapon using Stovepipe with contextual logic
        /// </summary>
        public static bool TryJamWeapon(FVRFireArm firearm, string context = "Normal", float customChance = -1f, bool forceJam = false, MalfunctionType specificType = MalfunctionType.None)
        {
            if (!IsIntegrationEnabled() || firearm == null)
                return false;

            TotalMalfunctionAttempts++;

            try
            {
                // Check cooldown first
                string weaponKey = GetWeaponKey(firearm);
                if (featureConfigs["EnableCooldowns"].Value && IsOnCooldown(weaponKey) && !forceJam)
                {
                    CooldownBlocked++;
                    if (featureConfigs["VerboseLogging"].Value)
                        logger.LogDebug($"[StovepipeIntegration] Jam blocked by cooldown for {weaponKey}");
                    return false;
                }

                // Check cache
                if (featureConfigs["EnableCaching"].Value && jamCapabilityCache.ContainsKey(weaponKey))
                {
                    CachedResults++;
                    bool cachedResult = jamCapabilityCache[weaponKey];
                    
                    if (!cachedResult)
                    {
                        if (featureConfigs["VerboseLogging"].Value)
                            logger.LogDebug($"[StovepipeIntegration] Using cached incompatible result for {weaponKey}");
                        return false;
                    }
                }

                // Check if weapon can jam
                if (!CanWeaponJam(firearm) && !forceJam)
                {
                    CacheJamCapability(weaponKey, false);
                    return false;
                }

                // Calculate jam chance
                float chance = customChance >= 0 ? customChance : CalculateJamChance(firearm, context);
                
                // Apply chance check unless forced
                if (!forceJam && UnityEngine.Random.value > chance)
                {
                    if (featureConfigs["VerboseLogging"].Value)
                        logger.LogDebug($"[StovepipeIntegration] Jam chance failed: {chance:P4}");
                    
                    return false;
                }

                // Determine malfunction type
                MalfunctionType jamType = specificType != MalfunctionType.None ? 
                    specificType : DetermineMalfunctionType(firearm, context);

                // Attempt jamming
                bool success = PerformJamming(firearm, jamType, context);
                
                if (success)
                {
                    SuccessfulMalfunctions++;
                    UpdateMalfunctionStatistics(context, firearm, jamType);
                    
                    // Set cooldown
                    if (featureConfigs["EnableCooldowns"].Value)
                        SetJamCooldown(weaponKey);
                    
                    logger.LogInfo($"[StovepipeIntegration] Successfully jammed {firearm.name} with {jamType} (context: {context})");
                    
                    // Play effects if enabled
                    if (featureConfigs["PlayMalfunctionSounds"].Value)
                        PlayMalfunctionSound(firearm.transform.position, jamType);
                    
                    if (featureConfigs["ShowMalfunctionParticles"].Value)
                        ShowMalfunctionParticles(firearm.transform.position, jamType);

                    // Auto-clear if enabled
                    if (featureConfigs["AutoClearJams"].Value)
                    {
                        float delay = intConfigs["AutoClearDelaySeconds"].Value;
                        StartAutoClearCoroutine(firearm, delay);
                    }
                }

                CacheJamCapability(weaponKey, true);
                return success;
            }
            catch (Exception ex)
            {
                logger.LogError($"[StovepipeIntegration] Error during weapon jamming: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to jam multiple weapons in a batch
        /// </summary>
        public static Dictionary<FVRFireArm, bool> TryJamWeaponsBatch(List<FVRFireArm> firearms, string context = "Normal", float customChance = -1f, bool forceJam = false)
        {
            var results = new Dictionary<FVRFireArm, bool>();
            
            if (!IsIntegrationEnabled() || !featureConfigs["EnableBatchJamming"].Value)
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
                    results[firearm] = TryJamWeapon(firearm, context, customChance, forceJam);
                }
            }

            logger.LogDebug($"[StovepipeIntegration] Batch jamming completed: {results.Count(r => r.Value)}/{results.Count} successful");
            return results;
        }

        /// <summary>
        /// Check if a weapon can jam
        /// </summary>
        public static bool CanWeaponJam(FVRFireArm firearm)
        {
            if (!IsIntegrationEnabled() || firearm == null)
                return false;

            try
            {
                // Use Stovepipe's check method if available
                if (checkCompatibilityMethod != null)
                {
                    return (bool)checkCompatibilityMethod.Invoke(stovepipeInstance, new object[] { firearm });
                }

                // Basic compatibility check - most automatic weapons can jam
                WeaponCategory category = GetWeaponCategory(firearm);
                
                // Weapons that typically can't jam or have very low jam rates
                switch (category)
                {
                    case WeaponCategory.Revolver:
                        return false; // Revolvers generally don't "jam" in the traditional sense
                    case WeaponCategory.Bolt:
                        return multiplierConfigs["BoltMultiplier"].Value > 0.1f; // Very rare for bolt actions
                    case WeaponCategory.Pump:
                        return multiplierConfigs["PumpMultiplier"].Value > 0.1f; // Rare for pump actions
                    default:
                        return true; // Most semi/auto weapons can jam
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[StovepipeIntegration] Error checking weapon jam capability: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a weapon is currently jammed
        /// </summary>
        public static bool IsWeaponJammed(FVRFireArm firearm)
        {
            if (firearm == null) return false;

            try
            {
                // Use Stovepipe's method if available
                if (getJamStateMethod != null)
                {
                    return (bool)getJamStateMethod.Invoke(stovepipeInstance, new object[] { firearm });
                }

                if (jamStateProperty != null)
                {
                    var target = stovepipeInstance ?? firearm;
                    return (bool)jamStateProperty.GetValue(target, null);
                }

                // Fallback: check if weapon has any obvious jam indicators
                return CheckWeaponJamStateFallback(firearm);
            }
            catch (Exception ex)
            {
                logger.LogDebug($"[StovepipeIntegration] Error checking jam status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear a jam from a weapon
        /// </summary>
        public static bool ClearWeaponJam(FVRFireArm firearm)
        {
            if (firearm == null) return false;

            try
            {
                // Use Stovepipe's clear method if available
                if (clearJamMethod != null)
                {
                    clearJamMethod.Invoke(stovepipeInstance, new object[] { firearm });
                    JamsCleared++;
                    logger.LogInfo($"[StovepipeIntegration] Cleared jam on {firearm.name}");
                    return true;
                }

                // Fallback method - try to reset weapon state
                return ClearJamFallback(firearm);
            }
            catch (Exception ex)
            {
                logger.LogError($"[StovepipeIntegration] Error clearing jam: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get malfunction statistics
        /// </summary>
        public static string GetMalfunctionStats()
        {
            float successRate = TotalMalfunctionAttempts > 0 ? 
                (float)SuccessfulMalfunctions / TotalMalfunctionAttempts * 100f : 0f;

            var contextStats = string.Join(", ", MalfunctionsByContext.Select(kvp => $"{kvp.Key}: {kvp.Value}").ToArray());
            var weaponStats = string.Join(", ", MalfunctionsByWeaponType.Select(kvp => $"{kvp.Key}: {kvp.Value}").ToArray());
            var typeStats = string.Join(", ", MalfunctionsByType.Select(kvp => $"{kvp.Key}: {kvp.Value}").ToArray());

            return $"Stovepipe Integration Stats:\n" +
                   $"• Status: {(IsStovepipeAvailable ? "? Active" : "? Not Available")}\n" +
                   $"• Version: {DetectedVersion} (API: {DetectedApiVersion})\n" +
                   $"• Attempts: {TotalMalfunctionAttempts}\n" +
                   $"• Successes: {SuccessfulMalfunctions}\n" +
                   $"• Success Rate: {successRate:F1}%\n" +
                   $"• Cached Results: {CachedResults}\n" +
                   $"• Cooldown Blocked: {CooldownBlocked}\n" +
                   $"• Jams Cleared: {JamsCleared}\n" +
                   $"• Cache Size: {jamCapabilityCache.Count}\n" +
                   $"• By Context: {contextStats}\n" +
                   $"• By Weapon Type: {weaponStats}\n" +
                   $"• By Malfunction Type: {typeStats}";
        }

        /// <summary>
        /// Clear jam cache
        /// </summary>
        public static void ClearCache()
        {
            jamCapabilityCache.Clear();
            lastJamTimes.Clear();
            lastMalfunctionTypes.Clear();
            jamCooldowns.Clear();
            CachedResults = 0;
            logger.LogDebug("[StovepipeIntegration] Cache cleared");
        }

        /// <summary>
        /// Check if integration is enabled and available
        /// </summary>
        public static bool IsIntegrationEnabled()
        {
            return IsStovepipeAvailable && featureConfigs["Enabled"].Value;
        }

        /// <summary>
        /// Get detailed compatibility information
        /// </summary>
        public static string GetCompatibilityInfo()
        {
            if (!IsStovepipeAvailable)
                return "Stovepipe not detected";

            var features = new List<string>();
            if (forceMalfunctionMethod != null) features.Add("Force Malfunction");
            if (checkCompatibilityMethod != null) features.Add("Compatibility Checking");
            if (getJamStateMethod != null) features.Add("Jam State Detection");
            if (clearJamMethod != null) features.Add("Jam Clearing");
            if (getMalfunctionTypeMethod != null) features.Add("Malfunction Types");
            if (setJamChanceMethod != null) features.Add("Jam Chance Control");

            return $"Stovepipe Compatibility:\n" +
                   $"• Version: {DetectedVersion}\n" +
                   $"• API Version: {DetectedApiVersion}\n" +
                   $"• Available Features: {string.Join(", ", features.ToArray())}\n" +
                   $"• Integration Status: {(IsIntegrationEnabled() ? "Active" : "Disabled")}";
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Calculate jam chance based on context and weapon
        /// </summary>
        private static float CalculateJamChance(FVRFireArm firearm, string context)
        {
            float baseChance = 0.01f; // Default 1%

            // Get base chance from context
            if (chanceConfigs.ContainsKey(context))
            {
                baseChance = chanceConfigs[context].Value;
            }
            else
            {
                // Context-based chance determination
                context = context.ToLower();
                if (context.Contains("combat") || context.Contains("stress"))
                    baseChance = chanceConfigs["Combat"].Value;
                else if (context.Contains("dirty") || context.Contains("fouled"))
                    baseChance = chanceConfigs["Dirty"].Value;
                else if (context.Contains("worn") || context.Contains("old"))
                    baseChance = chanceConfigs["WornOut"].Value;
                else if (context.Contains("hot") || context.Contains("overheated"))
                    baseChance = chanceConfigs["Overheated"].Value;
                else if (context.Contains("elite") || context.Contains("boss"))
                    baseChance = chanceConfigs["Elite"].Value;
                else if (context.Contains("player"))
                    baseChance = chanceConfigs["Player"].Value;
                else if (context.Contains("enemy"))
                    baseChance = chanceConfigs["Enemy"].Value;
                else if (context.Contains("ally"))
                    baseChance = chanceConfigs["Ally"].Value;
                else
                    baseChance = chanceConfigs["Normal"].Value;
            }

            // Apply weapon category multiplier
            float categoryMultiplier = GetWeaponCategoryMultiplier(firearm);
            float finalChance = baseChance * categoryMultiplier;

            // Apply contextual modifiers
            if (featureConfigs["ContextualMalfunctions"].Value)
            {
                finalChance = ApplyContextualModifiers(finalChance, firearm, context);
            }

            if (featureConfigs["DebugMode"].Value)
            {
                logger.LogDebug($"[StovepipeIntegration] Calculated jam chance for {firearm.name} ({context}): {finalChance:P4} (base: {baseChance:P4}, category: {categoryMultiplier:F2})");
            }

            return Mathf.Clamp01(finalChance);
        }

        /// <summary>
        /// Get weapon category multiplier
        /// </summary>
        private static float GetWeaponCategoryMultiplier(FVRFireArm firearm)
        {
            WeaponCategory category = GetWeaponCategory(firearm);
            
            switch (category)
            {
                case WeaponCategory.Pistol:
                    return multiplierConfigs["Pistol"].Value;
                case WeaponCategory.Rifle:
                    return multiplierConfigs["Rifle"].Value;
                case WeaponCategory.Shotgun:
                    return multiplierConfigs["Shotgun"].Value;
                case WeaponCategory.SMG:
                    return multiplierConfigs["SMG"].Value;
                case WeaponCategory.LMG:
                    return multiplierConfigs["LMG"].Value;
                case WeaponCategory.AssaultRifle:
                    return multiplierConfigs["AssaultRifle"].Value;
                case WeaponCategory.Sniper:
                    return multiplierConfigs["Sniper"].Value;
                case WeaponCategory.Revolver:
                    return multiplierConfigs["Revolver"].Value;
                case WeaponCategory.Bolt:
                    return multiplierConfigs["Bolt"].Value;
                case WeaponCategory.Pump:
                    return multiplierConfigs["Pump"].Value;
                default:
                    return 1.0f; // Default multiplier
            }
        }

        /// <summary>
        /// Determine weapon category from firearm
        /// </summary>
        private static WeaponCategory GetWeaponCategory(FVRFireArm firearm)
        {
            string weaponName = firearm.name.ToLower();
            string weaponType = firearm.GetType().Name.ToLower();

            // Check for specific weapon types
            if (weaponName.Contains("revolver") || weaponType.Contains("revolver"))
                return WeaponCategory.Revolver;
            
            if (weaponName.Contains("bolt") || weaponType.Contains("bolt"))
                return WeaponCategory.Bolt;
                
            if (weaponName.Contains("pump") || weaponType.Contains("pump"))
                return WeaponCategory.Pump;
                
            if (weaponName.Contains("shotgun") || weaponType.Contains("shotgun"))
                return WeaponCategory.Shotgun;
                
            if (weaponName.Contains("lmg") || weaponName.Contains("machinegun") || weaponType.Contains("lmg"))
                return WeaponCategory.LMG;
                
            if (weaponName.Contains("smg") || weaponName.Contains("submachine") || weaponType.Contains("smg"))
                return WeaponCategory.SMG;
                
            if (weaponName.Contains("sniper") || weaponName.Contains("precision") || weaponType.Contains("sniper"))
                return WeaponCategory.Sniper;
                
            if (weaponName.Contains("assault") || weaponName.Contains("carbine") || weaponType.Contains("assault"))
                return WeaponCategory.AssaultRifle;
                
            if (weaponName.Contains("pistol") || weaponName.Contains("handgun") || weaponType.Contains("pistol"))
                return WeaponCategory.Pistol;
                
            if (weaponName.Contains("rifle") || weaponType.Contains("rifle"))
                return WeaponCategory.Rifle;

            return WeaponCategory.Unknown;
        }

        /// <summary>
        /// Determine malfunction type based on weapon and context
        /// </summary>
        private static MalfunctionType DetermineMalfunctionType(FVRFireArm firearm, string context)
        {
            if (!featureConfigs["RealisticJamTypes"].Value)
                return MalfunctionType.Stovepipe; // Default

            WeaponCategory category = GetWeaponCategory(firearm);
            context = context.ToLower();

            // Context-based type selection
            if (context.Contains("dirty"))
            {
                return UnityEngine.Random.value < 0.6f ? MalfunctionType.FailureToFeed : MalfunctionType.FailureToEject;
            }
            
            if (context.Contains("overheated"))
            {
                return UnityEngine.Random.value < 0.7f ? MalfunctionType.FailureToEject : MalfunctionType.DoubleFeed;
            }

            // Weapon-specific malfunction types
            switch (category)
            {
                case WeaponCategory.Pistol:
                    return GetRandomMalfunctionType(new[] { 
                        MalfunctionType.Stovepipe, MalfunctionType.FailureToFeed, 
                        MalfunctionType.FailureToEject, MalfunctionType.FailureToFire 
                    });
                    
                case WeaponCategory.SMG:
                    return GetRandomMalfunctionType(new[] { 
                        MalfunctionType.DoubleFeed, MalfunctionType.Stovepipe, 
                        MalfunctionType.FailureToFeed, MalfunctionType.FailureToEject 
                    });
                    
                case WeaponCategory.LMG:
                    return GetRandomMalfunctionType(new[] { 
                        MalfunctionType.FailureToEject, MalfunctionType.DoubleFeed, 
                        MalfunctionType.DirtyGun, MalfunctionType.OutOfBattery 
                    });
                    
                case WeaponCategory.Revolver:
                    return GetRandomMalfunctionType(new[] { 
                        MalfunctionType.FailureToFire, MalfunctionType.HangFire, 
                        MalfunctionType.AmmoIssue 
                    });
                    
                default:
                    return GetRandomMalfunctionType(new[] { 
                        MalfunctionType.Stovepipe, MalfunctionType.FailureToFeed, 
                        MalfunctionType.FailureToEject, MalfunctionType.FailureToFire 
                    });
            }
        }

        private static MalfunctionType GetRandomMalfunctionType(MalfunctionType[] types)
        {
            return types[UnityEngine.Random.Range(0, types.Length)];
        }

        /// <summary>
        /// Apply contextual modifiers to jam chance
        /// </summary>
        private static float ApplyContextualModifiers(float baseChance, FVRFireArm firearm, string context)
        {
            float modifier = 1.0f;

            // Check for dirt accumulation
            if (featureConfigs["DirtAccumulation"].Value)
            {
                // Simulate dirt accumulation - could be enhanced with actual tracking
                int estimatedRoundsFired = GetEstimatedRoundsFired(firearm);
                if (estimatedRoundsFired > 100)
                {
                    modifier *= 1.0f + (estimatedRoundsFired / 1000.0f); // Gradual increase
                }
            }

            // Check for heat buildup
            if (featureConfigs["HeatBuildup"].Value)
            {
                // Simulate heat from rapid firing - could be enhanced with actual tracking
                float timeSinceLastShot = GetTimeSinceLastShot(firearm);
                if (timeSinceLastShot < 2.0f) // Rapid firing
                {
                    modifier *= 1.5f;
                }
            }

            // Weapon condition affects jams
            if (featureConfigs["WeaponConditionTracking"].Value)
            {
                float condition = GetWeaponCondition(firearm);
                modifier *= (2.0f - condition); // Lower condition = higher jam chance
            }

            return baseChance * modifier;
        }

        /// <summary>
        /// Perform the actual jamming
        /// </summary>
        private static bool PerformJamming(FVRFireArm firearm, MalfunctionType jamType, string context)
        {
            if (forceMalfunctionMethod == null)
            {
                logger.LogWarning("[StovepipeIntegration] No force malfunction method available");
                return false;
            }

            try
            {
                // Try different method signatures for forcing malfunctions
                var paramTypes = forceMalfunctionMethod.GetParameters();
                
                if (paramTypes.Length == 1)
                {
                    // Single parameter (firearm only)
                    forceMalfunctionMethod.Invoke(stovepipeInstance, new object[] { firearm });
                }
                else if (paramTypes.Length == 2)
                {
                    // Two parameters (firearm and malfunction type)
                    forceMalfunctionMethod.Invoke(stovepipeInstance, new object[] { firearm, (int)jamType });
                }
                else if (paramTypes.Length == 3)
                {
                    // Three parameters (firearm, type, and context)
                    forceMalfunctionMethod.Invoke(stovepipeInstance, new object[] { firearm, (int)jamType, context });
                }
                else
                {
                    // Try with just firearm
                    forceMalfunctionMethod.Invoke(stovepipeInstance, new object[] { firearm });
                }

                // Record the malfunction
                string weaponKey = GetWeaponKey(firearm);
                lastJamTimes[weaponKey] = DateTime.Now;
                lastMalfunctionTypes[weaponKey] = jamType;

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"[StovepipeIntegration] Jamming failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Play malfunction sound effect
        /// </summary>
        private static void PlayMalfunctionSound(Vector3 position, MalfunctionType jamType)
        {
            try
            {
                // Check if AudioManager has the static method
                var audioManagerType = typeof(AudioManager);
                var playStovepipeMethod = audioManagerType.GetMethod("PlayStovepipeEffect", BindingFlags.Static | BindingFlags.Public);
                
                if (playStovepipeMethod != null)
                {
                    string soundName = GetMalfunctionSoundName(jamType);
                    playStovepipeMethod.Invoke(null, new object[] { position, soundName });
                }
                else
                {
                    logger.LogDebug($"[StovepipeIntegration] Playing malfunction sound ({jamType}) at {position}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"[StovepipeIntegration] Could not play malfunction sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Show malfunction particle effects
        /// </summary>
        private static void ShowMalfunctionParticles(Vector3 position, MalfunctionType jamType)
        {
            try
            {
                // Check if EffectsManager has the static method
                var effectsManagerType = typeof(EffectsManager);
                var playStovepipeParticlesMethod = effectsManagerType.GetMethod("PlayStovepipeParticles", BindingFlags.Static | BindingFlags.Public);
                
                if (playStovepipeParticlesMethod != null)
                {
                    playStovepipeParticlesMethod.Invoke(null, new object[] { position, jamType });
                }
                else
                {
                    logger.LogDebug($"[StovepipeIntegration] Showing malfunction particles ({jamType}) at {position}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"[StovepipeIntegration] Could not show malfunction particles: {ex.Message}");
            }
        }

        /// <summary>
        /// Get malfunction sound name based on type
        /// </summary>
        private static string GetMalfunctionSoundName(MalfunctionType jamType)
        {
            switch (jamType)
            {
                case MalfunctionType.Stovepipe:
                    return "stovepipe/stovepipe_jam.wav";
                case MalfunctionType.DoubleFeed:
                    return "stovepipe/double_feed.wav";
                case MalfunctionType.FailureToFeed:
                    return "stovepipe/failure_to_feed.wav";
                case MalfunctionType.FailureToEject:
                    return "stovepipe/failure_to_eject.wav";
                case MalfunctionType.FailureToFire:
                    return "stovepipe/failure_to_fire.wav";
                case MalfunctionType.HangFire:
                    return "stovepipe/hang_fire.wav";
                default:
                    return "stovepipe/generic_jam.wav";
            }
        }

        /// <summary>
        /// Check if weapon is on jam cooldown
        /// </summary>
        private static bool IsOnCooldown(string weaponKey)
        {
            if (!jamCooldowns.ContainsKey(weaponKey))
                return false;

            var cooldownEnd = jamCooldowns[weaponKey].AddSeconds(intConfigs["JamCooldownSeconds"].Value);
            return DateTime.Now < cooldownEnd;
        }

        /// <summary>
        /// Set jam cooldown for weapon
        /// </summary>
        private static void SetJamCooldown(string weaponKey)
        {
            jamCooldowns[weaponKey] = DateTime.Now;
        }

        /// <summary>
        /// Start auto-clear coroutine
        /// </summary>
        private static void StartAutoClearCoroutine(FVRFireArm firearm, float delay)
        {
            // This would need to be implemented with a MonoBehaviour context
            // For now, just log the intention
            logger.LogDebug($"[StovepipeIntegration] Auto-clear scheduled for {firearm.name} in {delay} seconds");
        }

        /// <summary>
        /// Update malfunction statistics
        /// </summary>
        private static void UpdateMalfunctionStatistics(string context, FVRFireArm firearm, MalfunctionType jamType)
        {
            // Update context statistics
            if (!MalfunctionsByContext.ContainsKey(context))
                MalfunctionsByContext[context] = 0;
            MalfunctionsByContext[context]++;

            // Update weapon type statistics
            string weaponType = GetWeaponTypeName(firearm);
            if (!MalfunctionsByWeaponType.ContainsKey(weaponType))
                MalfunctionsByWeaponType[weaponType] = 0;
            MalfunctionsByWeaponType[weaponType]++;

            // Update malfunction type statistics
            if (!MalfunctionsByType.ContainsKey(jamType))
                MalfunctionsByType[jamType] = 0;
            MalfunctionsByType[jamType]++;
        }

        /// <summary>
        /// Get weapon type name for statistics
        /// </summary>
        private static string GetWeaponTypeName(FVRFireArm firearm)
        {
            WeaponCategory category = GetWeaponCategory(firearm);
            return category.ToString();
        }

        /// <summary>
        /// Check weapon jam state using fallback methods
        /// </summary>
        private static bool CheckWeaponJamStateFallback(FVRFireArm firearm)
        {
            // Try to detect jam state through various means
            try
            {
                // Check if firing pin/trigger mechanism seems stuck
                // This is a very basic fallback and may not be accurate
                return false; // Conservative approach
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Clear jam using fallback methods
        /// </summary>
        private static bool ClearJamFallback(FVRFireArm firearm)
        {
            try
            {
                // Basic fallback - try to reset some weapon states
                // This is very limited without direct Stovepipe integration
                logger.LogWarning("[StovepipeIntegration] Using limited fallback jam clearing");
                return false; // Conservative approach
            }
            catch
            {
                return false;
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
        /// Cache jam capability result
        /// </summary>
        private static void CacheJamCapability(string weaponKey, bool canJam)
        {
            if (!featureConfigs["EnableCaching"].Value) return;

            jamCapabilityCache[weaponKey] = canJam;

            // Enforce cache size limit
            if (jamCapabilityCache.Count > intConfigs["MaxCacheSize"].Value)
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

            foreach (var kvp in lastJamTimes)
            {
                if (kvp.Value < cutoffTime || forceClear)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            // If force clearing, remove oldest entries first
            if (forceClear && keysToRemove.Count < jamCapabilityCache.Count / 2)
            {
                var sortedEntries = lastJamTimes.OrderBy(kvp => kvp.Value).Take(jamCapabilityCache.Count / 2);
                keysToRemove.AddRange(sortedEntries.Select(kvp => kvp.Key));
            }

            foreach (var key in keysToRemove)
            {
                jamCapabilityCache.Remove(key);
                lastJamTimes.Remove(key);
                lastMalfunctionTypes.Remove(key);
                jamCooldowns.Remove(key);
            }

            if (keysToRemove.Count > 0)
            {
                logger.LogDebug($"[StovepipeIntegration] Cleaned {keysToRemove.Count} cache entries");
            }
        }

        /// <summary>
        /// Get estimated rounds fired (placeholder - would need actual tracking)
        /// </summary>
        private static int GetEstimatedRoundsFired(FVRFireArm firearm)
        {
            // Placeholder implementation
            return UnityEngine.Random.Range(0, 500);
        }

        /// <summary>
        /// Get time since last shot (placeholder - would need actual tracking)
        /// </summary>
        private static float GetTimeSinceLastShot(FVRFireArm firearm)
        {
            // Placeholder implementation
            return UnityEngine.Random.Range(0f, 10f);
        }

        /// <summary>
        /// Get weapon condition (placeholder - would need actual tracking)
        /// </summary>
        private static float GetWeaponCondition(FVRFireArm firearm)
        {
            // Placeholder implementation - returns value between 0.0 (broken) and 1.0 (perfect)
            return UnityEngine.Random.Range(0.7f, 1.0f);
        }
        #endregion
    }
}