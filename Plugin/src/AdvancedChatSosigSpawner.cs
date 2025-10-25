using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using FistVR;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Spawn priority levels for queued spawns
    /// </summary>
    public enum SpawnPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Immediate = 3
    }
    
    /// <summary>
    /// Advanced Chat Sosig Spawner - Updated for Anton Update 120 TNH System
    /// Full-featured system with Twitch integration, armor customization, and modern TNH spawning
    /// COMPATIBLE WITH CHATWATCHER.CS for file-based Twitch chat integration
    /// </summary>
    public class AdvancedChatSosigSpawner : MonoBehaviour
    {
        #region Static Instance and Tracking
        public static AdvancedChatSosigSpawner Instance { get; private set; }
        public static List<Sosig> spawnedChatters = new List<Sosig>();
        public static List<Sosig> spawnedEnemyChatters = new List<Sosig>();
        #endregion

        #region Core Components
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        private SteamFriendsIntegration steamFriends; // Optional Steam Friends integration
        
        // ChatWatcher integration
        private ChatWatcher chatWatcher;
        private bool chatWatcherEnabled = false;
        #endregion

        #region Sosig Templates - Updated for U120
        [Header("Sosig Templates")]
        public SosigEnemyID defaultAllyID = SosigEnemyID.M_Swat_Scout;
        public SosigEnemyID defaultEnemyID = SosigEnemyID.M_Swat_Heavy;
        
        // Keep template lists for backwards compatibility
        public List<SosigEnemyTemplate> allyTemplates = new List<SosigEnemyTemplate>();
        public List<SosigEnemyTemplate> enemyTemplates = new List<SosigEnemyTemplate>();
        
        private SosigEnemyTemplate[] cachedSosigTemplates;
        
        // New U120 sosig pool system
        private List<SosigEnemyID> allyPoolIDs = new List<SosigEnemyID>();
        private List<SosigEnemyID> enemyPoolIDs = new List<SosigEnemyID>();
        #endregion

        #region Nameplate System
        public GameObject nameplateAlly;
        public GameObject nameplateEnemy;
        public string SpawnerName = "ChatUser";
        
        // Name lists from INI files
        private List<string> allyNames = new List<string>();
        private List<string> enemyNames = new List<string>();
        #endregion

        #region Configuration
        private ConfigEntry<int> maxAllySosigs;
        private ConfigEntry<int> maxEnemySosigs;
        private ConfigEntry<float> spawnCooldown;
        private ConfigEntry<bool> enableNameplates;
        private ConfigEntry<float> sosigLifetime;
        private ConfigEntry<bool> enableAutoCleanup;
        private ConfigEntry<float> enemyIFF;
        
        // REMOVED: Duplicate key bindings (ChatWatcher handles these)
        // private ConfigEntry<KeyCode> spawnAllyKey;
        // private ConfigEntry<KeyCode> spawnEnemyKey;
        // private ConfigEntry<KeyCode> clearSosigsKey;
        
        private ConfigEntry<float> followDistance;
        private ConfigEntry<float> enemyAggressionDistance;
        
        // New U120 configuration
        private ConfigEntry<bool> useModernSpawnSystem;
        private ConfigEntry<string> allySosigPool;
        private ConfigEntry<string> enemySosigPool;
        
        // Advanced features configuration
        private ConfigEntry<bool> enableArmorCustomization;
        private ConfigEntry<string> allyNamesFilePath;
        private ConfigEntry<string> enemyNamesFilePath;
        private ConfigEntry<bool> useRandomNames;
        private ConfigEntry<int> maxSosigsPerUser;
        private ConfigEntry<bool> enableCoverAI;
        private ConfigEntry<float> sosigUpdateInterval;
        
        // ChatWatcher integration config
        private ConfigEntry<bool> enableChatWatcherIntegration;
        #endregion

        #region Spawn Management
        private float lastSpawnTime;
        private static readonly LayerMask EnvironmentMask = LayerMask.GetMask("Environment");
        
        // User tracking for per-user limits
        private Dictionary<string, int> userSosigCounts = new Dictionary<string, int>();
        private Dictionary<string, float> userLastSpawnTime = new Dictionary<string, float>();
        #endregion

        #region Initialization
        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            plugin = pluginInstance;
            logger = logSource;

            InitializeConfiguration();
            InitializeSosigTemplates();

            logger?.LogInfo("Advanced Chat Sosig Spawner initialized (Update 120 TNH System, ChatWatcher compatible)");

            // Start coroutines
            StartCoroutine(DelayedInitialization());
            StartCoroutine(UpdateSosigsCoroutine());
            StartCoroutine(CleanupCoroutine());
            
            // Wait a frame then try to link with Steam Friends integration
            StartCoroutine(LinkSteamFriendsIntegration());
            
            // Initialize ChatWatcher if enabled
            if (enableChatWatcherIntegration.Value)
            {
                StartCoroutine(InitializeChatWatcher());
            }
        }
        
        /// <summary>
        /// Initialize ChatWatcher integration
        /// </summary>
        private IEnumerator InitializeChatWatcher()
        {
            yield return new WaitForSeconds(0.5f); // Wait for systems to be ready
            
            try
            {
                // Check if ChatWatcher is already created
                chatWatcher = FindObjectOfType<ChatWatcher>();
                
                if (chatWatcher == null)
                {
                    // Create ChatWatcher component
                    GameObject chatWatcherGO = new GameObject("H3TVR_ChatWatcher");
                    chatWatcher = chatWatcherGO.AddComponent<ChatWatcher>();
                    chatWatcher.Initialize(plugin, logger, this);
                    DontDestroyOnLoad(chatWatcherGO);
                    
                    chatWatcherEnabled = true;
                    logger?.LogInfo("ChatWatcher integration enabled - file-based chat spawning active");
                }
                else
                {
                    chatWatcherEnabled = true;
                    logger?.LogInfo("ChatWatcher already exists - using existing instance");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to initialize ChatWatcher: {ex.Message}");
                chatWatcherEnabled = false;
            }
        }
        
        /// <summary>
        /// Link with Steam Friends integration if available
        /// </summary>
        private IEnumerator LinkSteamFriendsIntegration()
        {
            yield return new WaitForSeconds(1f); // Wait for all systems to initialize
            
            try
            {
                var steamIntegration = plugin?.GetSteamFriendsIntegration();
                if (steamIntegration != null && steamIntegration.IsAvailable())
                {
                    steamFriends = steamIntegration;
                    logger?.LogInfo("Steam Friends integration linked successfully");
                }
                else
                {
                    logger?.LogInfo("Steam Friends integration not available");
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Failed to link Steam Friends integration: {ex.Message}");
            }
        }

        private IEnumerator DelayedInitialization()
        {
            float timeout = 10f;
            float elapsed = 0f;
            
            while (IM.Instance == null && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
            
            if (IM.Instance == null)
            {
                logger?.LogError("IM.Instance failed to initialize within timeout");
                yield break;
            }
            
            yield return null;
            BuildTemplateCache();
            logger?.LogInfo("Delayed initialization complete - Template cache ready");
        }

        private void InitializeConfiguration()
        {
            if (plugin?.Config == null)
            {
                logger?.LogError("Plugin config is null");
                return;
            }

            try
            {
                maxAllySosigs = plugin.Config.Bind("Chat Spawner", "MaxAllySosigs", 8, 
                    "Maximum ally sosigs");
                maxEnemySosigs = plugin.Config.Bind("Chat Spawner", "MaxEnemySosigs", 8, 
                    "Maximum enemy sosigs");
                spawnCooldown = plugin.Config.Bind("Chat Spawner", "SpawnCooldown", 2.0f, 
                    "Cooldown between spawns");
                
                enableNameplates = plugin.Config.Bind("Chat Spawner", "EnableNameplates", true, 
                    "Show nameplates above sosigs");
                sosigLifetime = plugin.Config.Bind("Chat Spawner", "SosigLifetime", 300.0f, 
                    "Sosig lifetime in seconds (0 = infinite)");
                enableAutoCleanup = plugin.Config.Bind("Chat Spawner", "EnableAutoCleanup", true, 
                    "Auto cleanup dead sosigs");
                enemyIFF = plugin.Config.Bind("Chat Spawner", "EnemyIFF", 1.0f, 
                    "Enemy IFF code");
                
                followDistance = plugin.Config.Bind("Chat Spawner", "FollowDistance", 6.0f, 
                    "Distance for allies to follow player");
                enemyAggressionDistance = plugin.Config.Bind("Chat Spawner", "EnemyAggressionDistance", 20.0f, 
                    "Distance at which enemies become aggressive");
                
                // REMOVED: Duplicate key bindings - ChatWatcher handles these now
                // spawnAllyKey = plugin.Config.Bind("Chat Spawner Keys", "SpawnAllyKey", KeyCode.P, 
                //     "Spawn ally key");
                // spawnEnemyKey = plugin.Config.Bind("Chat Spawner Keys", "SpawnEnemyKey", KeyCode.O, 
                //     "Spawn enemy key");
                // clearSosigsKey = plugin.Config.Bind("Chat Spawner Keys", "ClearSosigsKey", KeyCode.Delete, 
                //     "Clear sosigs key");
                
                // New U120 configuration
                useModernSpawnSystem = plugin.Config.Bind("Chat Spawner", "UseModernSpawnSystem", true,
                    "Use Update 120's modern TNH sosig spawn system (recommended)");
                allySosigPool = plugin.Config.Bind("Chat Spawner", "AllySosigPool", 
                    "M_Swat_Scout,M_Swat_Sniper,M_Swat_Breacher",
                    "Comma-separated list of SosigEnemyID names for allies\n" +
                    "Valid IDs include: M_Swat_Scout, M_Swat_Sniper, M_Swat_Breacher, M_Swat_Heavy, M_Swat_Riot, " +
                    "M_Merc_Scout, M_Merc_Sniper, M_Merc_Heavy, M_Zombies_Melee, M_Zombies_Ranged, " +
                    "M_Soldier_Scout, M_Soldier_Sniper, M_Soldier_Heavy, and many more. " +
                    "Check H3VR's SosigEnemyID enum for complete list.");
                enemySosigPool = plugin.Config.Bind("Chat Spawner", "EnemySosigPool",
                    "M_Swat_Heavy,M_Swat_Breacher,M_Swat_Sniper",
                    "Comma-separated list of SosigEnemyID names for enemies\n" +
                    "Valid IDs include: M_Swat_Heavy, M_Swat_Riot, M_Swat_Breacher, M_Merc_Heavy, " +
                    "M_Zombies_Ranged, M_Soldier_Heavy, M_PMC_Heavy, and many more. " +
                    "Check H3VR's SosigEnemyID enum for complete list.");
                
                // Advanced features
                enableArmorCustomization = plugin.Config.Bind("Chat Spawner Advanced", "EnableArmorCustomization", true,
                    "Enable armor customization system");
                allyNamesFilePath = plugin.Config.Bind("Chat Spawner Advanced", "AllyNamesFilePath", 
                    "Plugins/H3TwitchTools/AllyNames.ini",
                    "File path for ally names list (INI file)");

                enemyNamesFilePath = plugin.Config.Bind("Chat Spawner Advanced", "EnemyNamesFilePath", 
                    "Plugins/H3TwitchTools/EnemyNames.ini",
                    "File path for enemy names list (INI file)");
                
                useRandomNames = plugin.Config.Bind("Chat Spawner Advanced", "UseRandomNames", true,
                    "Use random names from name lists");
                maxSosigsPerUser = plugin.Config.Bind("Chat Spawner Advanced", "MaxSosigsPerUser", 2,
                    "Maximum sosigs per Twitch user");
                enableCoverAI = plugin.Config.Bind("Chat Spawner Advanced", "EnableCoverAI", true,
                    "Enable advanced cover-taking AI behavior");
                sosigUpdateInterval = plugin.Config.Bind("Chat Spawner Advanced", "UpdateInterval", 1.0f,
                    "Interval between sosig AI updates (seconds)");
                
                // ChatWatcher integration
                enableChatWatcherIntegration = plugin.Config.Bind("Chat Spawner Integration", "EnableChatWatcher", true,
                    "Enable ChatWatcher integration for file-based Twitch chat spawning\n" +
                    "When enabled, sosigs will spawn automatically from chat files (H3TwitchTools compatible)");

                logger?.LogInfo("Configuration initialized successfully (ChatWatcher integration ready)");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Config init failed: {ex.Message}");
            }
        }

        private void InitializeSosigTemplates()
        {
            try
            {
                // Initialize sosig pools from config
                InitializeSosigPools();
                
                // Load legacy templates for fallback
                StartCoroutine(LoadTemplatesDelayed());
                
                // Build template cache for U120
                BuildTemplateCache();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Template initialization failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initialize sosig ID pools for Update 120 spawn system
        /// </summary>
        private void InitializeSosigPools()
        {
            try
            {
                // Parse ally pool
                var allyIDs = allySosigPool.Value.Split(',');
                foreach (var idStr in allyIDs)
                {
                    try
                    {
                        var id = (SosigEnemyID)System.Enum.Parse(typeof(SosigEnemyID), idStr.Trim());
                        allyPoolIDs.Add(id);
                    }
                    catch
                    {
                        logger?.LogWarning($"Invalid ally sosig ID: {idStr}");
                    }
                }
                
                // Parse enemy pool
                var enemyIDs = enemySosigPool.Value.Split(',');
                foreach (var idStr in enemyIDs)
                {
                    try
                    {
                        var id = (SosigEnemyID)System.Enum.Parse(typeof(SosigEnemyID), idStr.Trim());
                        enemyPoolIDs.Add(id);
                    }
                    catch
                    {
                        logger?.LogWarning($"Invalid enemy sosig ID: {idStr}");
                    }
                }
                
                // Fallback to defaults if pools are empty
                if (allyPoolIDs.Count == 0)
                {
                    allyPoolIDs.Add(SosigEnemyID.M_Swat_Scout);
                    allyPoolIDs.Add(SosigEnemyID.M_Swat_Sniper);
                }
                
                if (enemyPoolIDs.Count == 0)
                {
                    enemyPoolIDs.Add(SosigEnemyID.M_Swat_Heavy);
                    enemyPoolIDs.Add(SosigEnemyID.M_Swat_Breacher);
                }
                
                logger?.LogInfo($"Loaded {allyPoolIDs.Count} ally sosig types, {enemyPoolIDs.Count} enemy sosig types");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to initialize sosig pools: {ex.Message}");
                // Use defaults
                allyPoolIDs.Add(SosigEnemyID.M_Swat_Scout);
                enemyPoolIDs.Add(SosigEnemyID.M_Swat_Heavy);
            }
        }

        private IEnumerator LoadTemplatesDelayed()
        {
            yield return null; // Wait one frame

            try
            {
                var sosigObjects = Resources.FindObjectsOfTypeAll<SosigEnemyTemplate>();
                if (sosigObjects != null && sosigObjects.Length > 0)
                {
                    cachedSosigTemplates = sosigObjects;
                    
                    foreach (var template in cachedSosigTemplates)
                    {
                        if (template != null)
                        {
                            allyTemplates.Add(template);
                            enemyTemplates.Add(template);
                        }
                    }
                    
                    logger?.LogInfo($"Loaded {allyTemplates.Count} legacy sosig templates (fallback)");
                }
                else
                {
                    logger?.LogWarning("No legacy sosig templates found - using modern spawn system only");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Template loading failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load name lists from INI files
        /// </summary>
        private void LoadNameLists()
        {
            try
            {
                // Load ally names
                if (File.Exists(allyNamesFilePath.Value))
                {
                    var lines = File.ReadAllLines(allyNamesFilePath.Value);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#") && !trimmed.StartsWith(";"))
                        {
                            allyNames.Add(trimmed);
                        }
                    }
                    logger?.LogInfo($"Loaded {allyNames.Count} ally names");
                }
                else
                {
                    logger?.LogWarning($"Ally names file not found: {allyNamesFilePath.Value}");
                    // Create default file
                    CreateDefaultNameFile(allyNamesFilePath.Value, true);
                }
                
                // Load enemy names
                if (File.Exists(enemyNamesFilePath.Value))
                {
                    var lines = File.ReadAllLines(enemyNamesFilePath.Value);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#") && !trimmed.StartsWith(";"))
                        {
                            enemyNames.Add(trimmed);
                        }
                    }
                    logger?.LogInfo($"Loaded {enemyNames.Count} enemy names");
                }
                else
                {
                    logger?.LogWarning($"Enemy names file not found: {enemyNamesFilePath.Value}");
                    // Create default file
                    CreateDefaultNameFile(enemyNamesFilePath.Value, false);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to load name lists: {ex.Message}");
            }
        }
        
        private void CreateDefaultNameFile(string path, bool isAlly)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var defaultNames = isAlly 
                    ? new[] { "# Ally Sosig Names", "Friendly Bot", "Guardian", "Protector", "Ally", "Helper" }
                    : new[] { "# Enemy Sosig Names", "Hostile Bot", "Attacker", "Enemy", "Threat", "Opponent" };
                
                File.WriteAllLines(path, defaultNames);
                logger?.LogInfo($"Created default name file: {path}");
                
                // Reload
                if (isAlly)
                {
                    allyNames.AddRange(defaultNames.Skip(1));
                }
                else
                {
                    enemyNames.AddRange(defaultNames.Skip(1));
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create default name file: {ex.Message}");
            }
        }
        #endregion

        #region Core Spawning Logic - Updated for U120
        /// <summary>
        /// Spawn friendly sosig - Updated for U120 TNH System
        /// CHATWATCHER COMPATIBLE - Can be called from file-based chat triggers
        /// </summary>
        public void SpawningSequence(string username)
        {
            try
            {
                if (spawnedChatters.Count >= maxAllySosigs.Value)
                {
                    logger?.LogWarning($"Max ally sosigs reached ({maxAllySosigs.Value}) - cannot spawn for {username}");
                    return;
                }

                if (Time.time - lastSpawnTime < spawnCooldown.Value)
                {
                    logger?.LogWarning($"Spawn cooldown active ({spawnCooldown.Value}s) - skipping {username}");
                    return;
                }

                // Check per-user limit
                if (userSosigCounts.ContainsKey(username))
                {
                    if (userSosigCounts[username] >= maxSosigsPerUser.Value)
                    {
                        logger?.LogWarning($"User {username} has reached max sosigs limit ({maxSosigsPerUser.Value})");
                        return;
                    }
                }

                Vector3 spawnPos = CalculateAllySpawnPoint();
                Quaternion spawnRot = Quaternion.identity;

                Sosig sosig = null;
                
                // Use modern spawn system if enabled and available
                if (useModernSpawnSystem.Value)
                {
                    var enemyID = GetRandomAllyID();
                    sosig = SpawnSosigModern(enemyID, spawnPos, spawnRot, 0);
                }
                
                // Fall back to legacy system if modern failed or disabled
                if (sosig == null)
                {
                    var template = GetRandomTemplate(true);
                    if (template != null)
                    {
                        sosig = SpawnSosigLegacy(template, spawnPos, spawnRot, 0);
                    }
                }
                
                if (sosig != null)
                {
                    // Set up ally behavior
                    SetupAllyBehavior(sosig);
                    
                    // Determine the name to use for the nameplate
                    string displayName = username;
                    if (useRandomNames.Value)
                    {
                        displayName = GetRandomName(true); // Get random ally name from INI
                        logger?.LogInfo($"Using random ally name from INI: {displayName} (spawned by {username})");
                    }
                    
                    // Add nameplate
                    if (enableNameplates.Value && nameplateAlly != null)
                    {
                        AttachNameplate(sosig, displayName, nameplateAlly, false);
                    }
                
                    // Track sosig
                    spawnedChatters.Add(sosig);
                    lastSpawnTime = Time.time;
                    
                    // Update user sosig count
                    if (userSosigCounts.ContainsKey(username))
                    {
                        userSosigCounts[username]++;
                    }
                    else
                    {
                        userSosigCounts.Add(username, 1);
                    }
                    
                    logger?.LogInfo($"✓ Spawned ally sosig '{displayName}' for {username} ({spawnedChatters.Count}/{maxAllySosigs.Value})");
                }
                else
                {
                    logger?.LogError($"Failed to spawn ally sosig for {username}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Ally spawn failed for {username}: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawn enemy sosig - Updated for U120 TNH System
        /// CHATWATCHER COMPATIBLE - Can be called from file-based chat triggers
        /// </summary>
        public void SpawningSequenceEnemy(int IFF, string username)
        {
            try
            {
                if (spawnedEnemyChatters.Count >= maxEnemySosigs.Value)
                {
                    logger?.LogWarning($"Max enemy sosigs reached ({maxEnemySosigs.Value}) - cannot spawn for {username}");
                    return;
                }

                if (Time.time - lastSpawnTime < spawnCooldown.Value)
                {
                    logger?.LogWarning($"Spawn cooldown active ({spawnCooldown.Value}s) - skipping {username}");
                    return;
                }

                Vector3 spawnPos = CalculateEnemySpawnPoint();
                Quaternion spawnRot = Quaternion.identity;

                // Use configured IFF or parameter
                int finalIFF = IFF > 0 ? IFF : Mathf.Max(1, (int)enemyIFF.Value);

                Sosig sosig = null;
                
                // Use modern spawn system if enabled and available
                if (useModernSpawnSystem.Value)
                {
                    var enemyID = GetRandomEnemyID();
                    sosig = SpawnSosigModern(enemyID, spawnPos, spawnRot, finalIFF);
                }
                
                // Fall back to legacy system if modern failed or disabled
                if (sosig == null)
                {
                    var template = GetRandomTemplate(false);
                    if (template != null)
                    {
                        sosig = SpawnSosigLegacy(template, spawnPos, spawnRot, finalIFF);
                    }
                }
                
                if (sosig != null)
                {
                    // Set up enemy behavior
                    SetupEnemyBehavior(sosig);
                    
                    // Determine the name to use for the nameplate
                    string displayName = username;
                    if (useRandomNames.Value)
                    {
                        displayName = GetRandomName(false); // Get random enemy name from INI
                        logger?.LogInfo($"Using random enemy name from INI: {displayName} (spawned by {username})");
                    }
                    
                    // Add nameplate
                    if (enableNameplates.Value && nameplateEnemy != null)
                    {
                        AttachNameplate(sosig, displayName, nameplateEnemy, true);
                    }
                    
                    // Track sosig
                    spawnedEnemyChatters.Add(sosig);
                    lastSpawnTime = Time.time;
                    
                    logger?.LogInfo($"✓ Spawned enemy sosig '{displayName}' for {username} ({spawnedEnemyChatters.Count}/{maxEnemySosigs.Value})");
                }
                else
                {
                    logger?.LogError($"Failed to spawn enemy sosig for {username}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Enemy spawn failed for {username}: {ex.Message}");
            }
        }

        /// <summary>
        /// Modern sosig spawning using Update 120 TNH system - FIXED for compatibility
        /// </summary>
        private Sosig SpawnSosigModern(SosigEnemyID enemyID, Vector3 pos, Quaternion rot, int IFF)
        {
            try
            {
                // Validate template cache is ready
                if (templateCache == null || templateCache.Count == 0)
                {
                    logger?.LogWarning("Template cache not ready, attempting to rebuild...");
                    BuildTemplateCache();
                    
                    if (templateCache.Count == 0)
                    {
                        logger?.LogError("Template cache rebuild failed - falling back to legacy spawn");
                        return null;
                    }
                }
                
                // Method 1: Try cached template first
                SosigEnemyTemplate template = null;
                if (templateCache.ContainsKey(enemyID))
                {
                    template = templateCache[enemyID];
                }
                
                // Method 2: Try IM.Instance direct access
                if (template == null && IM.Instance != null && IM.Instance.odicSosigObjsByID != null)
                {
                    if (IM.Instance.odicSosigObjsByID.ContainsKey(enemyID))
                    {
                        template = IM.Instance.odicSosigObjsByID[enemyID];
                        // Cache for future use
                        templateCache[enemyID] = template;
                        logger?.LogInfo($"Cached template for {enemyID} from IM.Instance");
                    }
                }
                
                // Method 3: Try Resources.FindObjectsOfTypeAll as fallback
                if (template == null)
                {
                    logger?.LogWarning($"Template not in cache for {enemyID}, searching Resources...");
                    var allTemplates = Resources.FindObjectsOfTypeAll<SosigEnemyTemplate>();
                    foreach (var t in allTemplates)
                    {
                        if (t != null && t.SosigEnemyID == enemyID)
                        {
                            template = t;
                            templateCache[enemyID] = template;
                            logger?.LogInfo($"Found and cached template for {enemyID}");
                            break;
                        }
                    }
                }
                
                if (template == null)
                {
                    logger?.LogError($"Could not find template for SosigEnemyID: {enemyID}");
                    return null;
                }
                
                // Spawn sosig using the template
                Sosig sosig = SpawnSosigLegacy(template, pos, rot, IFF);
                
                if (sosig == null)
                {
                    logger?.LogError("Modern sosig spawn returned null");
                    return null;
                }
                
                // Configure with modern config system if available
                try
                {
                    if (template.ConfigTemplates != null && template.ConfigTemplates.Count > 0)
                    {
                        var configTemplate = template.ConfigTemplates[UnityEngine.Random.Range(0, template.ConfigTemplates.Count)];
                        sosig.Configure(configTemplate);
                    }
                }
                catch (Exception configEx)
                {
                    logger?.LogWarning($"Failed to apply config template: {configEx.Message}");
                }
                
                // Set IFF properly
                sosig.E.IFFCode = IFF;
                sosig.SetIFF(IFF);
                
                // Equip weapons and fill ammo
                try
                {
                    sosig.Inventory.FillAllAmmo();
                }
                catch (Exception invEx)
                {
                    logger?.LogWarning($"Failed to fill ammo: {invEx.Message}");
                }
                
                // Apply outfit if available
                try
                {
                    if (template.OutfitConfig != null && template.OutfitConfig.Count > 0)
                    {
                        ApplyOutfit(sosig, template.OutfitConfig[UnityEngine.Random.Range(0, template.OutfitConfig.Count)]);
                    }
                }
                catch (Exception outfitEx)
                {
                    logger?.LogWarning($"Failed to apply outfit: {outfitEx.Message}");
                }
                
                return sosig;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Modern sosig spawn failed: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Legacy sosig spawning method (pre-Update 120)
        /// </summary>
        private Sosig SpawnSosigLegacy(SosigEnemyTemplate template, Vector3 pos, Quaternion rot, int IFF)
        {
            try
            {
                if (template == null || template.SosigPrefabs == null || template.SosigPrefabs.Count == 0)
                {
                    logger?.LogError("Invalid template");
                    return null;
                }

                // Get random prefab
                var prefab = template.SosigPrefabs[UnityEngine.Random.Range(0, template.SosigPrefabs.Count)];
                if (prefab?.GetGameObject() == null)
                {
                    logger?.LogError("Invalid prefab");
                    return null;
                }

                // Instantiate sosig
                GameObject sosigGO = Instantiate(prefab.GetGameObject(), pos, rot);
                Sosig sosig = sosigGO.GetComponentInChildren<Sosig>();
                
                if (sosig == null)
                {
                    Destroy(sosigGO);
                    return null;
                }

                // Configure sosig
                if (template.ConfigTemplates != null && template.ConfigTemplates.Count > 0)
                {
                    var config = template.ConfigTemplates[UnityEngine.Random.Range(0, template.ConfigTemplates.Count)];
                    if (config != null)
                    {
                        sosig.Configure(config);
                    }
                }

                // Set IFF
                sosig.E.IFFCode = IFF;
                if (IFF < sosig.Priority.IFFChart.Length)
                {
                    sosig.Priority.IFFChart[IFF] = true;
                }

                // Equip weapons
                EquipWeapons(sosig, template, pos, rot);

                // Apply outfit
                if (template.OutfitConfig != null && template.OutfitConfig.Count > 0)
                {
                    ApplyOutfit(sosig, template.OutfitConfig[UnityEngine.Random.Range(0, template.OutfitConfig.Count)]);
                }

                return sosig;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Legacy sosig spawn failed: {ex.Message}");
                return null;
            }
        }

        private void EquipWeapons(Sosig sosig, SosigEnemyTemplate template, Vector3 pos, Quaternion rot)
        {
            try
            {
                // Primary weapon
                if (template.WeaponOptions != null && template.WeaponOptions.Count > 0)
                {
                    EquipWeapon(sosig, template.WeaponOptions[UnityEngine.Random.Range(0, template.WeaponOptions.Count)], pos, rot);
                }

                // Secondary weapon
                if (template.WeaponOptions_Secondary != null && template.WeaponOptions_Secondary.Count > 0)
                {
                    EquipWeapon(sosig, template.WeaponOptions_Secondary[UnityEngine.Random.Range(0, template.WeaponOptions_Secondary.Count)], pos, rot);
                }

                // Tertiary weapon
                if (template.WeaponOptions_Tertiary != null && template.WeaponOptions_Tertiary.Count > 0)
                {
                    EquipWeapon(sosig, template.WeaponOptions_Tertiary[UnityEngine.Random.Range(0, template.WeaponOptions_Tertiary.Count)], pos, rot);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Weapon equip failed: {ex.Message}");
            }
        }

        private void EquipWeapon(Sosig sosig, FVRObject weaponObj, Vector3 pos, Quaternion rot)
        {
            try
            {
                if (weaponObj?.GetGameObject() == null) return;

                GameObject weaponGO = Instantiate(weaponObj.GetGameObject(), pos + Vector3.up * 0.1f, rot);
                SosigWeapon weapon = weaponGO.GetComponent<SosigWeapon>();
                
                if (weapon != null)
                {
                    weapon.SetAutoDestroy(true);
                    weapon.O.SpawnLockable = false;
                    weapon.SetAmmoClamping(true);
                    weapon.IsShakeReloadable = false;

                    if (weapon.Type == SosigWeapon.SosigWeaponType.Gun)
                    {
                        sosig.Inventory.FillAmmoWithType(weapon.AmmoType);
                    }

                    sosig.Inventory.Init();
                    sosig.Inventory.FillAllAmmo();
                    sosig.InitHands();
                    sosig.ForceEquip(weapon);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Weapon equip error: {ex.Message}");
            }
        }

        private void ApplyOutfit(Sosig sosig, SosigOutfitConfig outfit)
        {
            try
            {
                if (outfit == null || sosig.Links.Count < 4) return;

                if (UnityEngine.Random.value < outfit.Chance_Headwear)
                    SpawnAccessory(outfit.Headwear, sosig.Links[0]);
                if (UnityEngine.Random.value < outfit.Chance_Facewear)
                    SpawnAccessory(outfit.Facewear, sosig.Links[0]);
                if (UnityEngine.Random.value < outfit.Chance_Eyewear)
                    SpawnAccessory(outfit.Eyewear, sosig.Links[0]);
                if (UnityEngine.Random.value < outfit.Chance_Torsowear)
                    SpawnAccessory(outfit.Torsowear, sosig.Links[1]);
                if (UnityEngine.Random.value < outfit.Chance_Pantswear)
                    SpawnAccessory(outfit.Pantswear, sosig.Links[2]);
                if (sosig.Links.Count > 3 && UnityEngine.Random.value < outfit.Chance_Pantswear_Lower)
                    SpawnAccessory(outfit.Pantswear_Lower, sosig.Links[3]);
                if (UnityEngine.Random.value < outfit.Chance_Backpacks)
                    SpawnAccessory(outfit.Backpacks, sosig.Links[1]);
                if (UnityEngine.Random.value < outfit.Chance_TorosDecoration)
                    SpawnAccessory(outfit.TorosDecoration, sosig.Links[1]);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Outfit apply failed: {ex.Message}");
            }
        }

        private void SpawnAccessory(List<FVRObject> accessories, SosigLink link)
        {
            if (accessories == null || accessories.Count == 0 || link == null) return;

            try
            {
                var accessory = accessories[UnityEngine.Random.Range(0, accessories.Count)];
                if (accessory?.GetGameObject() == null) return;

                GameObject accessoryGO = Instantiate(accessory.GetGameObject(), link.transform.position, link.transform.rotation);
                accessoryGO.transform.SetParent(link.transform);
                
                var wearable = accessoryGO.GetComponent<SosigWearable>();
                if (wearable != null)
                {
                    wearable.RegisterWearable(link);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Accessory spawn failed: {ex.Message}");
            }
        }
        #endregion

        #region Behavior Setup
        private void SetupAllyBehavior(Sosig sosig)
        {
            try
            {
                if (GM.CurrentPlayerBody?.Head?.transform == null) return;

                var playerPos = GM.CurrentPlayerBody.Head.transform.position;
                float offsetX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                float offsetZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                Vector3 followPoint = new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);
                
                sosig.CommandAssaultPoint(followPoint);
                sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Ally behavior setup failed: {ex.Message}");
            }
        }

        private void SetupEnemyBehavior(Sosig sosig)
        {
            try
            {
                if (GM.CurrentPlayerBody?.transform == null) return;

                sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Enemy behavior setup failed: {ex.Message}");
            }
        }
        #endregion

        #region Nameplate System
        private void AttachNameplate(Sosig sosig, string name, GameObject nameplatePrefab, bool isEnemy)
        {
            try
            {
                if (sosig.Links.Count == 0 || nameplatePrefab == null) return;

                SpawnerName = name;
                
                // Attach to head (Links[0]) instead of torso (Links[1])
                GameObject nameplate = Instantiate(nameplatePrefab, sosig.Links[0].transform, false);
                
                // Position above the head
                nameplate.transform.localPosition = new Vector3(0f, 0.3f, 0f); // Raised 0.3 units above head
                nameplate.transform.localRotation = Quaternion.identity;
                
                var textComponents = nameplate.GetComponentsInChildren<Text>();
                foreach (Text text in textComponents)
                {
                    text.text = name;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Nameplate attach failed: {ex.Message}");
            }
        }
        #endregion

        #region Spawn Point Calculation
        private Vector3 CalculateAllySpawnPoint()
        {
            if (GM.CurrentPlayerBody?.Head?.transform == null)
                return Vector3.zero;

            var playerPos = GM.CurrentPlayerBody.Head.transform.position;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(2f, 4f);
            
            return new Vector3(
                playerPos.x + Mathf.Cos(angle) * distance,
                playerPos.y,
                playerPos.z + Mathf.Sin(angle) * distance
            );
        }

        private Vector3 CalculateEnemySpawnPoint()
        {
            if (GM.CurrentPlayerBody?.Head?.transform == null)
                return Vector3.zero;

            var playerPos = GM.CurrentPlayerBody.Head.transform.position;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(8f, 15f);
            
            return new Vector3(
                playerPos.x + Mathf.Cos(angle) * distance,
                playerPos.y,
                playerPos.z + Mathf.Sin(angle) * distance
            );
        }
        #endregion

        #region Update and Cleanup - H3TwitchTools Style
        private IEnumerator UpdateSosigsCoroutine()
        {
            var wait = new WaitForSeconds(1f);

            while (true)
            {
                yield return wait;
                UpdateAllySosigs();
                UpdateEnemySosigs();
            }
        }

        private void UpdateAllySosigs()
        {
            if (GM.CurrentPlayerBody?.Head == null) return;

            for (int i = spawnedChatters.Count - 1; i >= 0; i--)
            {
                if (spawnedChatters[i] == null || spawnedChatters[i].BodyState == Sosig.SosigBodyState.Dead)
                {
                    if (enableAutoCleanup.Value && spawnedChatters[i] != null)
                    {
                        spawnedChatters[i].TickDownToClear(3);
                    }
                    spawnedChatters.RemoveAt(i);
                    continue;
                }

                var sosig = spawnedChatters[i];
                
                // Follow player logic
                if (!sosig.m_isStunned)
                {
                    var playerPos = GM.CurrentPlayerBody.Head.position;
                    float distance = Vector3.Distance(playerPos, sosig.m_assaultPoint);
                    
                    if (distance > followDistance.Value)
                    {
                        float offsetX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                        float offsetZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                        Vector3 followPoint = new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);
                        
                        bool isBad = Physics.Linecast(playerPos, followPoint, EnvironmentMask);
                        if (!isBad)
                        {
                            sosig.CommandAssaultPoint(followPoint);
                        }
                    }
                }

                // Combat response
                if (sosig.Priority.HasFreshTarget() && sosig.CurrentOrder == Sosig.SosigOrder.Investigate && sosig.m_entityRecognition >= 0.65f)
                {
                    sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
                }
            }
        }

        private void UpdateEnemySosigs()
        {
            if (GM.CurrentPlayerBody?.Head == null) return;

            for (int i = spawnedEnemyChatters.Count - 1; i >= 0; i++)
            {
                if (spawnedEnemyChatters[i] == null || spawnedEnemyChatters[i].BodyState == Sosig.SosigBodyState.Dead)
                {
                    if (enableAutoCleanup.Value && spawnedEnemyChatters[i] != null)
                    {
                        spawnedEnemyChatters[i].TickDownToClear(3);
                    }
                    spawnedEnemyChatters.RemoveAt(i);
                    continue;
                }

                var sosig = spawnedEnemyChatters[i];
                
                // Aggression logic
                if (!sosig.m_isStunned)
                {
                    var playerPos = GM.CurrentPlayerBody.Head.position;
                    float distance = Vector3.Distance(playerPos, sosig.Links[1].transform.position);
                    
                    if (distance > enemyAggressionDistance.Value)
                    {
                        sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                    }
                }

                // Combat response
                if (sosig.Priority.HasFreshTarget() && sosig.CurrentOrder == Sosig.SosigOrder.Investigate && sosig.m_entityRecognition >= 0.55f)
                {
                    sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
                }
                
                // Force aggression if idle
                if (sosig.CurrentOrder == Sosig.SosigOrder.Disabled || sosig.CurrentOrder == Sosig.SosigOrder.Idle || sosig.CurrentOrder == Sosig.SosigOrder.GuardPoint)
                {
                    sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                }
            }
        }

        private IEnumerator CleanupCoroutine()
        {
            var wait = new WaitForSeconds(10f);

            while (true)
            {
                yield return wait;
                CleanupDeadSosigs();
            }
        }

        private void CleanupDeadSosigs()
        {
            if (!enableAutoCleanup.Value) return;

            foreach (var sosig in spawnedChatters.Concat(spawnedEnemyChatters))
            {
                if (sosig != null && sosig.BodyState == Sosig.SosigBodyState.Dead)
                {
                    sosig.TickDownToClear(3);
                }
            }
        }
        #endregion

        #region Public API
        public void ClearSosigs(bool clearAllies = true, bool clearEnemies = true)
        {
            try
            {
                int cleared = 0;

                if (clearAllies)
                {
                    for (int i = spawnedChatters.Count - 1; i >= 0; i--)
                    {
                        if (spawnedChatters[i] != null)
                        {
                            Destroy(spawnedChatters[i].gameObject);
                            cleared++;
                        }
                    }
                    spawnedChatters.Clear();
                }

                if (clearEnemies)
                {
                    for (int i = spawnedEnemyChatters.Count - 1; i >= 0; i--)
                    {
                        if (spawnedEnemyChatters[i] != null)
                        {
                            Destroy(spawnedEnemyChatters[i].gameObject);
                            cleared++;
                        }
                    }
                    spawnedEnemyChatters.Clear();
                }
                
                // Clear user tracking when clearing sosigs
                userSosigCounts.Clear();
                userLastSpawnTime.Clear();

                logger?.LogInfo($"Cleared {cleared} sosigs (ChatWatcher compatible)");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Clear sosigs failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all sosigs (both allies and enemies)
        /// CHATWATCHER COMPATIBLE - Can be called from ChatWatcher
        /// </summary>
        public void ClearAllSosigs()
        {
            ClearSosigs(true, true);
            
            // Also clear any active bosses
            BossSosigSystem.ClearAllBosses();
            
            // Notify ChatWatcher if available
            if (chatWatcherEnabled && chatWatcher != null)
            {
                try
                {
                    chatWatcher.ClearCache();
                }
                catch (Exception ex)
                {
                    logger?.LogWarning($"Failed to clear ChatWatcher cache: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get ChatWatcher instance (if available)
        /// </summary>
        public ChatWatcher GetChatWatcher()
        {
            return chatWatcher;
        }
        
        /// <summary>
        /// Check if ChatWatcher integration is active
        /// </summary>
        public bool IsChatWatcherEnabled()
        {
            return chatWatcherEnabled && chatWatcher != null;
        }

        /// <summary>
        /// Spawn a boss sosig - Enhanced enemy with special abilities
        /// </summary>
        public Sosig SpawningSequenceBoss(BossSosigSystem.BossType bossType, string username = null)
        {
            try
            {
                if (spawnedEnemyChatters.Count >= maxEnemySosigs.Value)
                {
                    logger?.LogWarning("Max enemy sosigs reached - cannot spawn boss");
                    return null;
                }

                if (Time.time - lastSpawnTime < spawnCooldown.Value)
                {
                    logger?.LogWarning("Spawn cooldown active");
                    return null;
                }

                // Calculate spawn position for boss - further from player
                Vector3 spawnPos = CalculateBossSpawnPoint();
                Quaternion spawnRot = Quaternion.identity;

                // Determine IFF
                int finalIFF = Mathf.Max(1, (int)enemyIFF.Value);

                Sosig sosig = null;
                
                // Spawn using modern or legacy system
                if (useModernSpawnSystem.Value)
                {
                    var enemyID = GetBossTemplate(bossType);
                    sosig = SpawnSosigModern(enemyID, spawnPos, spawnRot, finalIFF);
                }
                
                if (sosig == null)
                {
                    var template = GetRandomTemplate(false);
                    if (template != null)
                    {
                        sosig = SpawnSosigLegacy(template, spawnPos, spawnRot, finalIFF);
                    }
                }
                
                if (sosig != null)
                {
                    // Setup enemy behavior
                    SetupEnemyBehavior(sosig);
                    
                    // Add boss component
                    BossSosig boss = sosig.gameObject.AddComponent<BossSosig>();
                    boss.Initialize(bossType, logger);
                    
                    // Determine display name
                    string displayName = username ?? $"BOSS_{bossType}";
                    
                    // Add special boss nameplate
                    if (enableNameplates.Value && nameplateEnemy != null)
                    {
                        AttachNameplate(sosig, $"★ {displayName} ★", nameplateEnemy, true);
                    }
                
                    // Track sosig
                    spawnedEnemyChatters.Add(sosig);
                    lastSpawnTime = Time.time;
                    
                    logger?.LogInfo($"Spawned {bossType} BOSS '{displayName}' (IFF: {finalIFF})");
                    
                    return sosig;
                }
                else
                {
                    logger?.LogError("Failed to spawn boss sosig");
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Boss spawn failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculate boss spawn point - further from player than regular enemies
        /// </summary>
        private Vector3 CalculateBossSpawnPoint()
        {
            if (GM.CurrentPlayerBody?.Head?.transform == null)
                return Vector3.zero;

            var playerPos = GM.CurrentPlayerBody.Head.transform.position;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(20f, 30f); // Bosses spawn further away
            
            return new Vector3(
                playerPos.x + Mathf.Cos(angle) * distance,
                playerPos.y,
                playerPos.z + Mathf.Sin(angle) * distance
            );
        }

        /// <summary>
        /// Get appropriate sosig template for boss type
        /// </summary>
        private SosigEnemyID GetBossTemplate(BossSosigSystem.BossType bossType)
        {
            // Select appropriate enemy template based on boss type
            switch (bossType)
            {
                case BossSosigSystem.BossType.Tank:
                case BossSosigSystem.BossType.Juggernaut:
                    return SosigEnemyID.M_Swat_Heavy;
                
                case BossSosigSystem.BossType.Sniper:
                    return SosigEnemyID.M_Swat_Sniper;
                
                case BossSosigSystem.BossType.Berserker:
                case BossSosigSystem.BossType.Assassin:
                    return SosigEnemyID.M_Swat_Breacher;
                
                default:
                    return GetRandomEnemyID();
            }
        }

        /// <summary>
        /// Queue a spawn request - Advanced version with priority and armor
        /// CHATWATCHER COMPATIBLE - Can be called from file-based chat triggers
        /// </summary>
        public void QueueSpawn(string username, string displayName, bool isFriendly, string armorPreset = null, SpawnPriority priority = SpawnPriority.Normal, string behavior = null)
        {
            try
            {
                // Simple immediate spawn with queue-like features
                if (isFriendly)
                {
                    SpawningSequence(displayName ?? username);
                }
                else
                {
                    SpawningSequenceEnemy((int)enemyIFF.Value, displayName ?? username);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"QueueSpawn failed for {username}: {ex.Message}");
            }
        }

        public struct ChatSosigStats
        {
            public int ActiveAllies;
            public int ActiveEnemies;
            public int QueueLength;
            public int TotalSpawned;
        }

        /// <summary>
        /// Get detailed statistics about spawned sosigs
        /// CHATWATCHER COMPATIBLE - Used by ChatWatcher.GetStats()
        /// </summary>
        public struct SosigStats
        {
            public int Allies;
            public int Enemies;
            public int Queued;
            public int TotalActive;
            public bool ChatWatcherActive;
        }

        public SosigStats GetStats()
        {
            return new SosigStats
            {
                Allies = spawnedChatters.Count,
                Enemies = spawnedEnemyChatters.Count,
                Queued = 0, // No queue in immediate spawn system
                TotalActive = spawnedChatters.Count + spawnedEnemyChatters.Count,
                ChatWatcherActive = chatWatcherEnabled && chatWatcher != null
            };
        }

        /// <summary>
        /// Twitch-compatible spawn request method
        /// CHATWATCHER COMPATIBLE - Primary method for file-based chat spawning
        /// </summary>
        public bool QueueTwitchSpawnRequest(string username, string displayName, bool isFriendly, string armorPreset = null, SpawnPriority priority = SpawnPriority.Normal, string requestedBehavior = null)
        {
            try
            {
                // Log source of spawn request
                string source = chatWatcherEnabled ? "ChatWatcher" : "Direct";
                logger?.LogDebug($"Twitch spawn request from {source}: {username} (friendly: {isFriendly})");
                
                // Simple immediate spawn for compatibility
                if (isFriendly)
                {
                    SpawningSequence(displayName ?? username);
                }
                else
                {
                    SpawningSequenceEnemy((int)enemyIFF.Value, displayName ?? username);
                }
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Twitch spawn request failed for {username}: {ex.Message}");
                return false;
            }
        }
        #endregion
        
        /// <summary>
        /// Apply outfit using modern Update 120 system
        /// </summary>
        private void ApplyOutfitModern(Sosig sosig, SosigOutfitConfig outfit)
        {
            try
            {
                if (outfit == null || sosig.Links.Count < 4) return;

                // Use legacy outfit system - modern RegisterSpawnOnThis doesn't exist
                ApplyOutfit(sosig, outfit);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Modern outfit apply failed: {ex.Message}");
                // Fallback to legacy method
                ApplyOutfit(sosig, outfit);
            }
        }

        #region Helper Methods
        /// <summary>
        /// Get random name from the appropriate list
        /// </summary>
        private string GetRandomName(bool isAlly)
        {
            // Try Steam Friends first if available and configured
            if (steamFriends != null && steamFriends.IsAvailable() && plugin.UseSteamFriendsRandomNames())
            {
                try
                {
                    string friendName = steamFriends.GetRandomFriendName();
                    if (!string.IsNullOrEmpty(friendName) && friendName != "Steam Friend")
                    {
                        logger?.LogDebug($"Using Steam friend name: {friendName}");
                        return friendName;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning($"Failed to get Steam friend name: {ex.Message}");
                }
            }
            
            // Fall back to INI name lists
            var nameList = isAlly ? allyNames : enemyNames;
            
            // Load name lists if not already loaded
            if (nameList.Count == 0)
            {
                LoadNameLists();
            }
            
            if (nameList.Count == 0)
                return isAlly ? "Ally" : "Enemy";
            
            return nameList[UnityEngine.Random.Range(0, nameList.Count)];
        }

        /// <summary>
        /// Get random ally sosig ID from pool
        /// </summary>
        private SosigEnemyID GetRandomAllyID()
        {
            if (allyPoolIDs.Count == 0)
                return defaultAllyID;
            
            return allyPoolIDs[UnityEngine.Random.Range(0, allyPoolIDs.Count)];
        }
        
        /// <summary>
        /// Get random enemy sosig ID from pool
        /// </summary>
        private SosigEnemyID GetRandomEnemyID()
        {
            if (enemyPoolIDs.Count == 0)
                return defaultEnemyID;
            
            return enemyPoolIDs[UnityEngine.Random.Range(0, enemyPoolIDs.Count)];
        }

        /// <summary>
        /// Get random sosig template from legacy system
        /// </summary>
        private SosigEnemyTemplate GetRandomTemplate(bool isAlly)
        {
            var templates = isAlly ? allyTemplates : enemyTemplates;
            
            if (templates == null || templates.Count == 0)
            {
                logger?.LogWarning($"No {(isAlly ? "ally" : "enemy")} templates available");
                return null;
            }
            
            return templates[UnityEngine.Random.Range(0, templates.Count)];
        }
        #endregion
        
        // Template cache for U120
        private Dictionary<SosigEnemyID, SosigEnemyTemplate> templateCache = new Dictionary<SosigEnemyID, SosigEnemyTemplate>();

        /// <summary>
        /// Build template cache for U120 compatibility
        /// </summary>
        private void BuildTemplateCache()
        {
            try
            {
                // Add comprehensive null checks
                if (IM.Instance == null)
                {
                    logger?.LogWarning("Cannot build template cache - IM.Instance is null (H3VR not ready)");
                    return;
                }
                
                if (IM.Instance.odicSosigObjsByID == null)
                {
                    logger?.LogWarning("Cannot build template cache - odicSosigObjsByID is null (H3VR not ready)");
                    return;
                }
                
                int cacheCount = 0;
                
                logger?.LogInfo("Building template cache from IM.Instance...");
                
                foreach (var id in allyPoolIDs.Concat(enemyPoolIDs).Distinct())
                {
                    if (IM.Instance.odicSosigObjsByID.ContainsKey(id))
                    {
                        var template = IM.Instance.odicSosigObjsByID[id];
                        if (template != null)
                        {
                            templateCache[id] = template;
                            cacheCount++;
                            logger?.LogDebug($"  Cached: {id}");
                        }
                        else
                        {
                            logger?.LogWarning($"  Template null for {id}");
                        }
                    }
                    else
                    {
                        logger?.LogWarning($"  ID not found in IM: {id}");
                    }
                }
                logger?.LogInfo($"Template cache built: {cacheCount}/{allyPoolIDs.Count + enemyPoolIDs.Count} templates loaded");
                
                // Log cache status summary
                logger?.LogInfo($"Template cache status: {templateCache.Count} total templates");
                logger?.LogInfo($"  Ally pool: {allyPoolIDs.Count} IDs configured");
                logger?.LogInfo($"  Enemy pool: {enemyPoolIDs.Count} IDs configured");
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Failed to build template cache: {ex.Message}");
                logger?.LogDebug($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Receive and set name lists from ChatWatcher
        /// CHATWATCHER COMPATIBLE - Called by ChatWatcher when new name lists are available
        /// </summary>
        public void OnReceiveNameLists(List<string> allyList, List<string> enemyList)
        {
            try
            {
                allyNames = allyList;
                enemyNames = enemyList;
                
                logger?.LogInfo($"Received {allyNames.Count} ally names and {enemyNames.Count} enemy names from ChatWatcher");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to process received name lists: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get comprehensive system status for debugging and monitoring
        /// </summary>
        public string GetSystemStatus()
        {
            try
            {
                var status = new System.Text.StringBuilder();
                status.AppendLine("=== Advanced Chat Sosig Spawner Status ===");
                status.AppendLine($"Active Allies: {spawnedChatters.Count}/{maxAllySosigs.Value}");
                status.AppendLine($"Active Enemies: {spawnedEnemyChatters.Count}/{maxEnemySosigs.Value}");
                status.AppendLine($"ChatWatcher: {(chatWatcherEnabled ? "ACTIVE" : "INACTIVE")}");
                status.AppendLine($"Steam Friends: {(steamFriends != null && steamFriends.IsAvailable() ? "AVAILABLE" : "N/A")}");
                status.AppendLine($"Template Cache: {templateCache.Count} templates");
                status.AppendLine($"Ally Pool: {allyPoolIDs.Count} types");
                status.AppendLine($"Enemy Pool: {enemyPoolIDs.Count} types");
                status.AppendLine($"Ally Names: {allyNames.Count} loaded");
                status.AppendLine($"Enemy Names: {enemyNames.Count} loaded");
                status.AppendLine($"Modern Spawn System: {(useModernSpawnSystem.Value ? "ENABLED" : "DISABLED")}");
                status.AppendLine($"Nameplates: {(enableNameplates.Value ? "ENABLED" : "DISABLED")}");
                status.AppendLine($"User Tracking: {userSosigCounts.Count} users");
                status.AppendLine("==========================================");
                
                return status.ToString();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to get system status: {ex.Message}");
                return "ERROR: Could not get status";
            }
        }
        
        /// <summary>
        /// Force reload name lists from disk
        /// </summary>
        public void ReloadNameLists()
        {
            try
            {
                allyNames.Clear();
                enemyNames.Clear();
                LoadNameLists();
                logger?.LogInfo($"Reloaded name lists: {allyNames.Count} allies, {enemyNames.Count} enemies");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to reload name lists: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Force rebuild the template cache
        /// </summary>
        public void RebuildTemplateCache()
        {
            try
            {
                templateCache.Clear();
                BuildTemplateCache();
                logger?.LogInfo($"Rebuilt template cache: {templateCache.Count} templates");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to rebuild template cache: {ex.Message}");
            }
        }
    }
}

