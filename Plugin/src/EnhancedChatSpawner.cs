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
    /// Enhanced Chat Spawner - Advanced Sosig spawning system with Twitch integration
    /// Based on proven ChatSpawner.cs logic with enhanced features like armor presets, 
    /// queue management, performance optimization, and extensive customization options
    /// This version is fully self-contained and doesn't require TwitchChatSosigManager
    /// </summary>
    public class EnhancedChatSpawner : MonoBehaviour
    {
        #region Static Instance and Events
        public static EnhancedChatSpawner Instance { get; private set; }
        public static List<Sosig> spawnedChatters = new List<Sosig>();
        public static List<Sosig> spawnedEnemyChatters = new List<Sosig>();
        public static event Action<Sosig, string, bool> OnSosigSpawned;
        public static event Action<Sosig, string> OnSosigDestroyed;
        public static event Action<int, int> OnSosigCountChanged; // allies, enemies
        #endregion

        #region Core Components
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        #endregion

        #region Sosig Templates and Assets
        [Header("Sosig Templates")]
        public SosigEnemyTemplate defaultAllyTemplate;
        public List<SosigEnemyTemplate> allyTemplates = new List<SosigEnemyTemplate>();
        public List<SosigEnemyTemplate> enemyTemplates = new List<SosigEnemyTemplate>();
        
        [Header("Nameplate Prefabs")]
        public GameObject allyNameplatePrefab;
        public GameObject enemyNameplatePrefab;
        
        private SosigEnemyTemplate[] cachedSosigTemplates;
        #endregion

        #region Sosig Management
        public List<ChatSosig> ActiveAllies { get; private set; } = new List<ChatSosig>();
        public List<ChatSosig> ActiveEnemies { get; private set; } = new List<ChatSosig>();
        private readonly Dictionary<Sosig, ChatSosig> sosigLookup = new Dictionary<Sosig, ChatSosig>();
        #endregion

        #region Name Management (Self-contained)
        private List<string> cachedAllyNames = new List<string>();
        private List<string> cachedEnemyNames = new List<string>();
        private DateTime allyNamesLastWrite;
        private DateTime enemyNamesLastWrite;
        private string allyNamesFilePath;
        private string enemyNamesFilePath;
        #endregion

        #region Configuration
        private ConfigEntry<int> maxAllySosigs;
        private ConfigEntry<int> maxEnemySosigs;
        private ConfigEntry<float> spawnCooldown;
        private ConfigEntry<bool> enableAdvancedAI;
        private ConfigEntry<bool> enableNameplates;
        private ConfigEntry<bool> enableVoiceLines;
        private ConfigEntry<bool> enableSpawnEffects;
        private ConfigEntry<string> defaultAllyArmor;
        private ConfigEntry<string> defaultEnemyArmor;
        private ConfigEntry<float> sosigLifetime;
        private ConfigEntry<bool> enableAutoCleanup;
        private ConfigEntry<float> enemyIFF;
        private ConfigEntry<string> allyFilePath;
        private ConfigEntry<string> enemyFilePath;
        private ConfigEntry<KeyCode> spawnAllyKey;
        private ConfigEntry<KeyCode> spawnEnemyKey;
        private ConfigEntry<KeyCode> clearSosigsKey;
        #endregion

        #region Spawn Queue and Management
        private readonly Queue<SpawnRequest> spawnQueue = new Queue<SpawnRequest>();
        private readonly Dictionary<string, DateTime> userSpawnCooldowns = new Dictionary<string, DateTime>();
        private float lastSpawnTime;
        private int totalSpawnCount;
        public string SpawnerName { get; set; } = "ChatUser";
        #endregion

        #region Performance Monitoring
        private float lastPerformanceCheck;
        private const float PerformanceCheckInterval = 5f;
        private readonly List<float> recentFrameTimes = new List<float>();
        private bool performanceMode;
        #endregion

        #region Spawn Request Class
        public class SpawnRequest
        {
            public string UserName { get; set; }
            public string DisplayName { get; set; }
            public bool IsFriendly { get; set; }
            public string ArmorPreset { get; set; }
            public Vector3? CustomSpawnPoint { get; set; }
            public string VoiceLineSet { get; set; }
            public Dictionary<string, object> CustomData { get; set; }
            public DateTime RequestTime { get; set; }
            public SpawnPriority Priority { get; set; } = SpawnPriority.Normal;
        }

        public enum SpawnPriority
        {
            Low = 0,
            Normal = 1,
            High = 2,
            Immediate = 3
        }
        #endregion

        #region ChatSosig Wrapper Class
        public class ChatSosig
        {
            public Sosig Sosig { get; set; }
            public string UserName { get; set; }
            public string DisplayName { get; set; }
            public bool IsFriendly { get; set; }
            public string ArmorPreset { get; set; }
            public DateTime SpawnTime { get; set; }
            public float Lifetime { get; set; }
            public GameObject Nameplate { get; set; }
            public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
            
            public bool IsValid => Sosig != null && Sosig.gameObject != null;
            public bool IsDead => Sosig == null || Sosig.BodyState == Sosig.SosigBodyState.Dead;
            public float Age => Time.time - (float)SpawnTime.Subtract(DateTime.MinValue).TotalSeconds;
        }
        #endregion

        #region Public API Methods
        /// <summary>
        /// Queue a spawn request from Twitch chat
        /// </summary>
        public bool QueueSpawnRequest(string userName, bool isFriendly, string armorPreset = null, SpawnPriority priority = SpawnPriority.Normal)
        {
            // Check user cooldown
            if (IsUserOnCooldown(userName))
            {
                logger?.LogWarning($"User {userName} is on spawn cooldown");
                return false;
            }

            // Check sosig limits
            if (!CanSpawn(isFriendly))
            {
                logger?.LogWarning($"Cannot spawn {(isFriendly ? "ally" : "enemy")} - at limit");
                return false;
            }

            var request = new SpawnRequest
            {
                UserName = userName,
                DisplayName = userName,
                IsFriendly = isFriendly,
                ArmorPreset = armorPreset ?? (isFriendly ? defaultAllyArmor.Value : defaultEnemyArmor.Value),
                RequestTime = DateTime.Now,
                Priority = priority,
                CustomData = new Dictionary<string, object>()
            };

            // Add to queue based on priority
            if (priority == SpawnPriority.Immediate)
            {
                var tempQueue = new Queue<SpawnRequest>();
                tempQueue.Enqueue(request);
                while (spawnQueue.Count > 0)
                    tempQueue.Enqueue(spawnQueue.Dequeue());
                spawnQueue.Clear();
                while (tempQueue.Count > 0)
                    spawnQueue.Enqueue(tempQueue.Dequeue());
            }
            else
            {
                spawnQueue.Enqueue(request);
            }

            // Set user cooldown
            userSpawnCooldowns[userName] = DateTime.Now.AddSeconds(spawnCooldown.Value * 2);

            logger?.LogInfo($"Queued spawn request for {userName} ({(isFriendly ? "ally" : "enemy")})");
            return true;
        }

        /// <summary>
        /// Get sosig statistics
        /// </summary>
        public ChatSosigStats GetStats()
        {
            return new ChatSosigStats
            {
                ActiveAllies = ActiveAllies.Count,
                ActiveEnemies = ActiveEnemies.Count,
                QueueLength = spawnQueue.Count,
                TotalSpawned = totalSpawnCount,
                PerformanceMode = performanceMode
            };
        }

        /// <summary>
        /// Clear all sosigs of specified type
        /// </summary>
        public void ClearSosigs(bool allies = true, bool enemies = true)
        {
            if (allies)
            {
                foreach (var chatSosig in ActiveAllies.ToList())
                {
                    DestroyChatSosig(chatSosig);
                }
                spawnedChatters.Clear();
            }

            if (enemies)
            {
                foreach (var chatSosig in ActiveEnemies.ToList())
                {
                    DestroyChatSosig(chatSosig);
                }
                spawnedEnemyChatters.Clear();
            }

            logger?.LogInfo($"Cleared sosigs - Allies: {allies}, Enemies: {enemies}");
        }

        /// <summary>
        /// Spawn ally sosig - compatibility method for SpawnManager
        /// </summary>
        public void SpawningSequence(string userName = "Unknown")
        {
            if (allyTemplates.Count == 0)
            {
                logger?.LogError("No ally templates available for spawning");
                return;
            }

            try
            {
                var template = allyTemplates[UnityEngine.Random.Range(0, allyTemplates.Count)];
                if (template == null) return;

                // Use H3VR sosig spawning logic
                Sosig sosig = SpawnSosigFromTemplate(
                    template,
                    CalculateSpawnPoint(true),
                    Quaternion.identity,
                    0, // Ally IFF
                    userName
                );

                if (sosig != null)
                {
                    // Set up ally behavior
                    SetupAllyBehavior(sosig);
                    
                    // Create enhanced wrapper
                    var chatSosig = CreateChatSosig(sosig, userName, true);
                    ActiveAllies.Add(chatSosig);
                    spawnedChatters.Add(sosig);
                    sosigLookup[sosig] = chatSosig;

                    // Create nameplate
                    if (enableNameplates?.Value == true && allyNameplatePrefab != null)
                        CreateNameplate(allyNameplatePrefab, sosig, userName);

                    // Effects
                    if (enableSpawnEffects?.Value == true)
                        CreateSpawnEffects(chatSosig);

                    totalSpawnCount++;
                    OnSosigSpawned?.Invoke(sosig, userName, true);
                    OnSosigCountChanged?.Invoke(ActiveAllies.Count, ActiveEnemies.Count);

                    logger?.LogInfo($"Spawned ally sosig for {userName}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to spawn ally sosig for {userName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawn enemy sosig - compatibility method for SpawnManager  
        /// </summary>
        public void SpawningSequenceEnemy(int IFF, string userName = "Unknown")
        {
            if (enemyTemplates.Count == 0)
            {
                logger?.LogError("No enemy templates available for spawning");
                return;
            }

            try
            {
                var template = enemyTemplates[UnityEngine.Random.Range(0, enemyTemplates.Count)];
                if (template == null) return;

                // Use H3VR sosig spawning logic
                Sosig sosig = SpawnSosigFromTemplate(
                    template,
                    CalculateSpawnPoint(false),
                    Quaternion.identity,
                    IFF,
                    userName
                );

                if (sosig != null)
                {
                    // Set up enemy behavior
                    SetupEnemyBehavior(sosig);
                    
                    // Create enhanced wrapper
                    var chatSosig = CreateChatSosig(sosig, userName, false);
                    ActiveEnemies.Add(chatSosig);
                    spawnedEnemyChatters.Add(sosig);
                    sosigLookup[sosig] = chatSosig;

                    // Create nameplate
                    if (enableNameplates?.Value == true && enemyNameplatePrefab != null)
                        CreateNameplate(enemyNameplatePrefab, sosig, userName);

                    // Effects
                    if (enableSpawnEffects?.Value == true)
                        CreateSpawnEffects(chatSosig);

                    totalSpawnCount++;
                    OnSosigSpawned?.Invoke(sosig, userName, false);
                    OnSosigCountChanged?.Invoke(ActiveAllies.Count, ActiveEnemies.Count);

                    logger?.LogInfo($"Spawned enemy sosig for {userName}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to spawn enemy sosig for {userName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Find a chat sosig by user name
        /// </summary>
        public ChatSosig FindSosigByUser(string userName)
        {
            return ActiveAllies.Concat(ActiveEnemies)
                .FirstOrDefault(cs => cs.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
        }
        #endregion

        #region Helper Classes
        public class ChatSosigStats
        {
            public int ActiveAllies { get; set; }
            public int ActiveEnemies { get; set; }
            public int QueueLength { get; set; }
            public int TotalSpawned { get; set; }
            public bool PerformanceMode { get; set; }
        }
        #endregion

        #region Helper Methods (Minimal Implementation)
        private ChatSosig CreateChatSosig(Sosig sosig, string userName, bool isFriendly)
        {
            return new ChatSosig
            {
                Sosig = sosig,
                UserName = userName,
                DisplayName = userName,
                IsFriendly = isFriendly,
                ArmorPreset = isFriendly ? defaultAllyArmor?.Value ?? "Light" : defaultEnemyArmor?.Value ?? "Heavy",
                SpawnTime = DateTime.Now,
                Lifetime = sosigLifetime?.Value ?? 300f,
                CustomData = new Dictionary<string, object>()
            };
        }

        private bool CanSpawn(bool isFriendly)
        {
            if (Time.time - lastSpawnTime < (spawnCooldown?.Value ?? 2f))
                return false;

            int currentCount = isFriendly ? ActiveAllies.Count : ActiveEnemies.Count;
            int maxCount = isFriendly ? (maxAllySosigs?.Value ?? 8) : (maxEnemySosigs?.Value ?? 8);

            return currentCount < maxCount;
        }

        private bool IsUserOnCooldown(string userName)
        {
            if (userSpawnCooldowns.TryGetValue(userName, out DateTime cooldownEnd))
                return DateTime.Now < cooldownEnd;
            return false;
        }

        private void DestroyChatSosig(ChatSosig chatSosig)
        {
            if (chatSosig == null)
                return;

            try
            {
                // Remove from lookup
                if (chatSosig.Sosig != null)
                    sosigLookup.Remove(chatSosig.Sosig);

                // Destroy nameplate
                if (chatSosig.Nameplate != null)
                    Destroy(chatSosig.Nameplate);

                // Destroy sosig
                if (chatSosig.Sosig != null)
                {
                    OnSosigDestroyed?.Invoke(chatSosig.Sosig, chatSosig.UserName);
                    Destroy(chatSosig.Sosig.gameObject);
                }

                // Remove from lists
                ActiveAllies.Remove(chatSosig);
                ActiveEnemies.Remove(chatSosig);
                if (chatSosig.Sosig != null)
                {
                    spawnedChatters.Remove(chatSosig.Sosig);
                    spawnedEnemyChatters.Remove(chatSosig.Sosig);
                }

                OnSosigCountChanged?.Invoke(ActiveAllies.Count, ActiveEnemies.Count);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error destroying sosig {chatSosig.UserName}: {ex.Message}");
            }
        }
        #endregion

        #region Initialization (Minimal)
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

            // Initialize configuration
            InitializeConfiguration();

            logger?.LogInfo("Enhanced Chat Spawner initialized successfully (minimal mode)");

            // Start coroutines
            StartCoroutine(ProcessSpawnQueueCoroutine());
            StartCoroutine(UpdateSosigsCoroutine());
            StartCoroutine(PerformanceMonitorCoroutine());
            StartCoroutine(CleanupCoroutine());
        }

        /// <summary>
        /// Initialize all configuration entries
        /// </summary>
        private void InitializeConfiguration()
        {
            if (plugin?.Config == null)
            {
                logger?.LogError("Plugin config is null, using defaults");
                return;
            }

            try
            {
                // Core spawn settings
                maxAllySosigs = plugin.Config.Bind("Enhanced Chat Spawner", "MaxAllySosigs", 8, 
                    "Maximum number of ally sosigs");
                maxEnemySosigs = plugin.Config.Bind("Enhanced Chat Spawner", "MaxEnemySosigs", 8, 
                    "Maximum number of enemy sosigs");
                spawnCooldown = plugin.Config.Bind("Enhanced Chat Spawner", "SpawnCooldown", 2.0f, 
                    "Cooldown between spawns");
                
                // Features
                enableAdvancedAI = plugin.Config.Bind("Enhanced Chat Spawner", "EnableAdvancedAI", true, 
                    "Enable advanced AI");
                enableNameplates = plugin.Config.Bind("Enhanced Chat Spawner", "EnableNameplates", true, 
                    "Show nameplates");
                enableVoiceLines = plugin.Config.Bind("Enhanced Chat Spawner", "EnableVoiceLines", false, 
                    "Enable voice lines");
                enableSpawnEffects = plugin.Config.Bind("Enhanced Chat Spawner", "EnableSpawnEffects", true, 
                    "Enable spawn effects");
                
                // Defaults
                defaultAllyArmor = plugin.Config.Bind("Enhanced Chat Spawner", "DefaultAllyArmor", "Light", 
                    "Default ally armor");
                defaultEnemyArmor = plugin.Config.Bind("Enhanced Chat Spawner", "DefaultEnemyArmor", "Heavy", 
                    "Default enemy armor");
                sosigLifetime = plugin.Config.Bind("Enhanced Chat Spawner", "SosigLifetime", 300.0f, 
                    "Sosig lifetime seconds");
                enableAutoCleanup = plugin.Config.Bind("Enhanced Chat Spawner", "EnableAutoCleanup", true, 
                    "Auto cleanup expired");
                enemyIFF = plugin.Config.Bind("Enhanced Chat Spawner", "EnemyIFF", 1.0f, 
                    "Enemy IFF code");
                
                // File paths
                allyFilePath = plugin.Config.Bind("Enhanced Chat Spawner", "AllyFilePath", "ally_names.txt", 
                    "Ally names file");
                enemyFilePath = plugin.Config.Bind("Enhanced Chat Spawner", "EnemyFilePath", "enemy_names.txt", 
                    "Enemy names file");
                
                // Keys
                spawnAllyKey = plugin.Config.Bind("Enhanced Chat Spawner Keys", "SpawnAllyKey", KeyCode.P, 
                    "Spawn ally key");
                spawnEnemyKey = plugin.Config.Bind("Enhanced Chat Spawner Keys", "SpawnEnemyKey", KeyCode.O, 
                    "Spawn enemy key");
                clearSosigsKey = plugin.Config.Bind("Enhanced Chat Spawner Keys", "ClearSosigsKey", KeyCode.Delete, 
                    "Clear sosigs key");

                logger?.LogInfo("Configuration initialized successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Config init failed: {ex.Message}");
            }

            // Initialize sosig templates
            InitializeSosigTemplates();
        }

        /// <summary>
        /// Initialize sosig templates from H3VR systems
        /// </summary>
        private void InitializeSosigTemplates()
        {
            try
            {
                // Wait a frame for H3VR systems to be ready
                StartCoroutine(LoadTemplatesDelayed());
            }
            catch (Exception ex)
            {
                logger?.LogError($"Template initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Load templates with delay to ensure H3VR systems are ready
        /// </summary>
        private IEnumerator LoadTemplatesDelayed()
        {
            yield return null; // Wait one frame

            try
            {
                // Try to get templates from various H3VR manager sources
                LoadTemplatesFromManagers();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Template loading failed: {ex.Message}");
                CreateFallbackTemplates();
            }
        }

        /// <summary>
        /// Load templates from H3VR managers
        /// </summary>
        private void LoadTemplatesFromManagers()
        {
            // Method 1: Try to find sosig templates in scene objects
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
                
                if (cachedSosigTemplates.Length > 0)
                {
                    defaultAllyTemplate = cachedSosigTemplates[0];
                }
                
                logger?.LogInfo($"Loaded {allyTemplates.Count} templates from scene resources");
                return;
            }

            // Method 2: Try to find templates through ItemManager if available
            if (IM.Instance != null)
            {
                // Look for any sosig-related objects in ItemManager
                // This is a fallback approach since direct SosigEDB access isn't available
                logger?.LogInfo("ItemManager found but no direct sosig template access available");
            }

            // If no templates found, create fallbacks
            CreateFallbackTemplates();
        }

        /// <summary>
        /// Fallback template loading when primary method fails
        /// </summary>
        private void TryLoadFallbackTemplates()
        {
            CreateFallbackTemplates();
        }

        /// <summary>
        /// Create minimal fallback templates when H3VR templates aren't available
        /// </summary>
        private void CreateFallbackTemplates()
        {
            try
            {
                logger?.LogWarning("Creating fallback sosig templates - spawning may be limited");

                // Look for any existing SosigEnemyTemplate in the scene
                var existingTemplates = FindObjectsOfType<SosigEnemyTemplate>();
                if (existingTemplates != null && existingTemplates.Length > 0)
                {
                    foreach (var template in existingTemplates)
                    {
                        if (template != null)
                        {
                            allyTemplates.Add(template);
                            enemyTemplates.Add(template);
                        }
                    }
                    
                    if (existingTemplates.Length > 0)
                    {
                        defaultAllyTemplate = existingTemplates[0];
                        cachedSosigTemplates = existingTemplates;
                    }
                    
                    logger?.LogInfo($"Found {existingTemplates.Length} existing sosig templates in scene");
                }
                else
                {
                    logger?.LogWarning("No sosig templates found - sosig spawning will not be available");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Fallback template creation failed: {ex.Message}");
            }
        }
        #endregion

        #region Unity Lifecycle
        void Update()
        {
            // Handle keyboard input for manual spawning
            HandleKeyboardInput();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Handle keyboard input for manual sosig spawning
        /// </summary>
        private void HandleKeyboardInput()
        {
            try
            {
                if (spawnAllyKey?.Value != KeyCode.None && Input.GetKeyDown(spawnAllyKey.Value))
                {
                    SpawningSequence("ManualAlly");
                }

                if (spawnEnemyKey?.Value != KeyCode.None && Input.GetKeyDown(spawnEnemyKey.Value))
                {
                    SpawningSequenceEnemy((int)(enemyIFF?.Value ?? 1f), "ManualEnemy");
                }

                if (clearSosigsKey?.Value != KeyCode.None && Input.GetKeyDown(clearSosigsKey.Value))
                {
                    ClearSosigs(true, true);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Input handling error: {ex.Message}");
            }
        }
        #endregion

        #region Core Sosig Spawning Logic
        /// <summary>
        /// Core sosig spawning method using H3VR systems (Enhanced with TNH armor and optional dependencies)
        /// </summary>
        private Sosig SpawnSosigFromTemplate(SosigEnemyTemplate template, Vector3 position, Quaternion rotation, int IFF, string userName)
        {
            try
            {
                if (template == null || template.SosigPrefabs == null || template.SosigPrefabs.Count == 0)
                {
                    logger?.LogError("Invalid template for sosig spawning");
                    return null;
                }

                // Get random prefab from template
                var prefabObject = template.SosigPrefabs[UnityEngine.Random.Range(0, template.SosigPrefabs.Count)];
                if (prefabObject?.GetGameObject() == null)
                {
                    logger?.LogError("Template prefab has no GameObject");
                    return null;
                }

                // Instantiate the sosig
                GameObject sosigGO = Instantiate(prefabObject.GetGameObject(), position, rotation);
                if (sosigGO == null)
                {
                    logger?.LogError("Failed to instantiate sosig GameObject");
                    return null;
                }

                Sosig sosig = sosigGO.GetComponent<Sosig>();
                if (sosig == null)
                {
                    logger?.LogError("Spawned object does not have Sosig component");
                    Destroy(sosigGO);
                    return null;
                }

                // Configure sosig with template
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
                sosig.Priority.IFFChart[IFF] = true;

                // Equip weapons
                EquipSosigWeapons(sosig, template, position, rotation);

                // Apply TNH armor instead of basic outfit
                bool isFriendly = IFF == 0;
                ApplyTNHArmorToSosig(sosig, isFriendly, isFriendly ? defaultAllyArmor?.Value : defaultEnemyArmor?.Value);

                // ENHANCED: Apply optional dependency enhancements
                ApplyOptionalDependencyEnhancements(sosig, userName, !isFriendly);

                lastSpawnTime = Time.time;
                return sosig;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Exception in SpawnSosigFromTemplate: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Apply enhancements from optional dependencies
        /// </summary>
        private void ApplyOptionalDependencyEnhancements(Sosig sosig, string spawnerContext, bool isEnemy)
        {
            try
            {
                // Initialize sosig weapon enhancer if not done
                SosigWeaponEnhancer.Initialize(logger);

                // Apply contextual enhancements based on spawner and sosig type
                SosigWeaponEnhancer.ApplyContextualEnhancements(sosig, spawnerContext);

                logger?.LogDebug($"[EnhancedChatSpawner] Applied optional dependency enhancements to sosig for {spawnerContext}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"[EnhancedChatSpawner] Error applying optional dependency enhancements: {ex.Message}");
            }
        }

        /// <summary>
        /// Equip weapons on sosig from template
        /// </summary>
        private void EquipSosigWeapons(Sosig sosig, SosigEnemyTemplate template, Vector3 position, Quaternion rotation)
        {
            try
            {
                // Primary weapon
                if (template.WeaponOptions != null && template.WeaponOptions.Count > 0)
                {
                    var weaponObj = template.WeaponOptions[UnityEngine.Random.Range(0, template.WeaponOptions.Count)];
                    if (weaponObj?.GetGameObject() != null)
                    {
                        SpawnAndEquipWeapon(sosig, weaponObj.GetGameObject(), position + Vector3.up * 0.1f, rotation);
                    }
                }

                // Secondary weapon
                if (template.WeaponOptions_Secondary != null && template.WeaponOptions_Secondary.Count > 0)
                {
                    var weaponObj = template.WeaponOptions_Secondary[UnityEngine.Random.Range(0, template.WeaponOptions_Secondary.Count)];
                    if (weaponObj?.GetGameObject() != null)
                    {
                        SpawnAndEquipWeapon(sosig, weaponObj.GetGameObject(), position + Vector3.up * 0.1f, rotation);
                    }
                }

                // Tertiary weapon
                if (template.WeaponOptions_Tertiary != null && template.WeaponOptions_Tertiary.Count > 0)
                {
                    var weaponObj = template.WeaponOptions_Tertiary[UnityEngine.Random.Range(0, template.WeaponOptions_Tertiary.Count)];
                    if (weaponObj?.GetGameObject() != null)
                    {
                        SpawnAndEquipWeapon(sosig, weaponObj.GetGameObject(), position + Vector3.up * 0.1f, rotation);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to equip weapons on sosig: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawn and equip a single weapon on sosig
        /// </summary>
        private void SpawnAndEquipWeapon(Sosig sosig, GameObject weaponPrefab, Vector3 position, Quaternion rotation)
        {
            try
            {
                GameObject weaponGO = Instantiate(weaponPrefab, position, rotation);
                if (weaponGO == null) return;

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
                logger?.LogError($"Failed to spawn and equip weapon: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply outfit configuration to sosig
        /// </summary>
        private void ApplyOutfitToSosig(Sosig sosig, SosigEnemyTemplate template)
        {
            try
            {
                if (template.OutfitConfig == null || template.OutfitConfig.Count == 0 || sosig.Links.Count < 4)
                    return;

                var outfit = template.OutfitConfig[UnityEngine.Random.Range(0, template.OutfitConfig.Count)];
                if (outfit == null) return;

                // Apply outfit pieces to appropriate links
                if (UnityEngine.Random.Range(0.0f, 1f) < outfit.Chance_Headwear)
                    SpawnAccessoryToLink(outfit.Headwear, sosig.Links[0]);
                if (UnityEngine.Random.Range(0.0f, 1f) < outfit.Chance_Facewear)
                    SpawnAccessoryToLink(outfit.Facewear, sosig.Links[0]);
                if (UnityEngine.Random.Range(0.0f, 1f) < outfit.Chance_Eyewear)
                    SpawnAccessoryToLink(outfit.Eyewear, sosig.Links[0]);
                if (UnityEngine.Random.Range(0.0f, 1f) < outfit.Chance_Torsowear)
                    SpawnAccessoryToLink(outfit.Torsowear, sosig.Links[1]);
                if (UnityEngine.Random.Range(0.0f, 1f) < outfit.Chance_Pantswear)
                    SpawnAccessoryToLink(outfit.Pantswear, sosig.Links[2]);
                if (sosig.Links.Count > 3 && UnityEngine.Random.Range(0.0f, 1f) < outfit.Chance_Pantswear_Lower)
                    SpawnAccessoryToLink(outfit.Pantswear_Lower, sosig.Links[3]);
                if (UnityEngine.Random.Range(0.0f, 1f) < outfit.Chance_Backpacks)
                    SpawnAccessoryToLink(outfit.Backpacks, sosig.Links[1]);
                if (UnityEngine.Random.Range(0.0f, 1f) < outfit.Chance_TorosDecoration)
                    SpawnAccessoryToLink(outfit.TorosDecoration, sosig.Links[1]);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to apply outfit to sosig: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawn accessory to specific sosig link
        /// </summary>
        private void SpawnAccessoryToLink(List<FVRObject> accessories, SosigLink link)
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
                logger?.LogError($"Failed to spawn accessory to link: {ex.Message}");
            }
        }

        /// <summary>
        /// Set up ally behavior patterns
        /// </summary>
        private void SetupAllyBehavior(Sosig sosig)
        {
            try
            {
                if (GM.CurrentPlayerBody?.Head?.transform == null) return;

                // Follow player at a distance
                var playerPos = GM.CurrentPlayerBody.Head.transform.position;
                float offsetX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                float offsetZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                Vector3 followPoint = new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);
                
                sosig.CommandAssaultPoint(followPoint);
                sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to setup ally behavior: {ex.Message}");
            }
        }

        /// <summary>
        /// Set up enemy behavior patterns
        /// </summary>
        private void SetupEnemyBehavior(Sosig sosig)
        {
            try
            {
                if (GM.CurrentPlayerBody?.transform == null) return;

                // Attack the player
                sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to setup enemy behavior: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculate appropriate spawn point for sosigs
        /// </summary>
        private Vector3 CalculateSpawnPoint(bool isFriendly)
        {
            if (GM.CurrentPlayerBody?.Head?.transform == null)
                return Vector3.zero;

            var playerPos = GM.CurrentPlayerBody.Head.transform.position;
            
            if (isFriendly)
            {
                // Spawn allies near player
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = UnityEngine.Random.Range(2f, 4f);
                
                return new Vector3(
                    playerPos.x + Mathf.Cos(angle) * distance,
                    playerPos.y,
                    playerPos.z + Mathf.Sin(angle) * distance
                );
            }
            else
            {
                // Spawn enemies further away
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = UnityEngine.Random.Range(8f, 15f);
                
                return new Vector3(
                    playerPos.x + Mathf.Cos(angle) * distance,
                    playerPos.y,
                    playerPos.z + Mathf.Sin(angle) * distance
                );
            }
        }

        /// <summary>
        /// Create nameplate for sosig
        /// </summary>
        private void CreateNameplate(GameObject nameplatePrefab, Sosig sosig, string userName)
        {
            if (nameplatePrefab == null || sosig == null || sosig.Links.Count < 2) return;

            try
            {
                GameObject nameplate = Instantiate(nameplatePrefab, sosig.Links[1].transform, false);
                nameplate.transform.localPosition = Vector3.zero;
                nameplate.transform.localRotation = Quaternion.identity;
                
                var textComponents = nameplate.GetComponentsInChildren<Text>();
                foreach (Text text in textComponents)
                {
                    text.text = userName;
                }

                // Store reference for cleanup
                if (sosigLookup.TryGetValue(sosig, out var chatSosig))
                {
                    chatSosig.Nameplate = nameplate;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create nameplate for {userName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Create spawn effects for sosig
        /// </summary>
        private void CreateSpawnEffects(ChatSosig chatSosig)
        {
            if (!chatSosig.IsValid) return;

            try
            {
                var spawnPos = chatSosig.Sosig.transform.position;
                
                // Create particle effect
                var effectObj = new GameObject("SpawnEffect");
                effectObj.transform.position = spawnPos;
                
                var particles = effectObj.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.startColor = chatSosig.IsFriendly ? Color.green : Color.red;
                main.startLifetime = 2f;
                main.startSpeed = 5f;
                main.maxParticles = 30;
                
                var emission = particles.emission;
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0f, 30)
                });
                
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 1f;

                // Auto-destroy
                Destroy(effectObj, 3f);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create spawn effect for {chatSosig.UserName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply TNH armor to sosig using the integrated armor system
        /// </summary>
        private void ApplyTNHArmorToSosig(Sosig sosig, bool isFriendly, string armorPreset)
        {
            try
            {
                if (sosig?.Links == null || sosig.Links.Count == 0)
                {
                    logger?.LogWarning("Cannot apply TNH armor - sosig has no valid links");
                    return;
                }

                // Try to get the armor integration from the plugin
                var armorIntegration = plugin?.GetSosigArmorWristMenu();
                if (armorIntegration != null && armorIntegration.IsFactionArmorEnabled())
                {
                    // Use the advanced armor system
                    armorIntegration.ApplyArmorToSosig(sosig, isFriendly);
                    logger?.LogDebug($"Applied advanced armor to sosig via wrist menu integration");
                }
                else
                {
                    // Fallback to basic TNH armor application
                    logger?.LogDebug("Using fallback armor application");
                    ApplyBasicTNHArmor(sosig, isFriendly, armorPreset);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to apply TNH armor to sosig: {ex.Message}");
                // Fallback to basic armor
                ApplyBasicTNHArmor(sosig, isFriendly, armorPreset);
            }
        }

        /// <summary>
        /// Apply basic TNH armor when advanced integration is not available
        /// </summary>
        private void ApplyBasicTNHArmor(Sosig sosig, bool isFriendly, string armorPreset)
        {
            try
            {
                // Define basic armor chances based on faction
                var armorChances = isFriendly ? new Dictionary<string, float>
                {
                    {"Headwear", 0.9f}, {"Facewear", 0.3f}, {"Eyewear", 0.6f}, {"Torsowear", 1.0f},
                    {"Pantswear", 0.8f}, {"PantswearLower", 0.7f}, {"Backpacks", 0.6f}, {"Decorations", 0.4f}
                } : new Dictionary<string, float>
                {
                    {"Headwear", 0.7f}, {"Facewear", 0.8f}, {"Eyewear", 0.4f}, {"Torsowear", 0.8f},
                    {"Pantswear", 0.7f}, {"PantswearLower", 0.5f}, {"Backpacks", 0.3f}, {"Decorations", 0.2f}
                };

                // Try to get armor from H3VR asset loader
                if (H3VRAssetLoader.IsInitialized)
                {
                    var armorCategories = H3VRAssetLoader.GetAllArmorCategories();
                    ApplyArmorFromCategories(sosig, armorCategories, armorChances);
                }
                else
                {
                    logger?.LogWarning("H3VR Asset Loader not initialized - skipping TNH armor application");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to apply basic TNH armor: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply armor from available categories with specified chances
        /// </summary>
        private void ApplyArmorFromCategories(Sosig sosig, Dictionary<string, List<FVRObject>> armorCategories, Dictionary<string, float> armorChances)
        {
            foreach (var kvp in armorChances)
            {
                if (UnityEngine.Random.value < kvp.Value && armorCategories.ContainsKey(kvp.Key))
                {
                    var armorList = armorCategories[kvp.Key];
                    if (armorList.Count > 0)
                    {
                        var randomArmor = armorList[UnityEngine.Random.Range(0, armorList.Count)];
                        ApplyArmorPieceToSosig(sosig, kvp.Key, randomArmor);
                    }
                }
            }
        }

        /// <summary>
        /// Apply a specific armor piece to a sosig
        /// </summary>
        private void ApplyArmorPieceToSosig(Sosig sosig, string category, FVRObject armorObject)
        {
            var link = GetLinkForArmorCategory(sosig, category);
            if (link == null || armorObject?.GetGameObject() == null) return;

            try
            {
                var armorInstance = Instantiate(armorObject.GetGameObject(), link.transform);
                var wearable = armorInstance.GetComponent<SosigWearable>();
                if (wearable != null)
                {
                    wearable.RegisterWearable(link);
                }
                
                logger?.LogDebug($"Applied {category} armor piece to sosig");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to apply armor piece {category}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the appropriate sosig link for an armor category
        /// </summary>
        private SosigLink GetLinkForArmorCategory(Sosig sosig, string category)
        {
            if (sosig?.Links == null || sosig.Links.Count == 0) return null;

            switch (category)
            {
                case "Headwear":
                case "Facewear":
                case "Eyewear":
                    return sosig.Links[0]; // Head
                case "Torsowear":
                case "Backpacks":
                case "Decorations":
                    return sosig.Links.Count > 1 ? sosig.Links[1] : null; // Torso
                case "Pantswear":
                case "PantswearLower":
                    return sosig.Links.Count > 2 ? sosig.Links[2] : null; // Legs
                default:
                    return sosig.Links[0];
            }
        }
        #endregion

        #region Queue Processing and Coroutines
        /// <summary>
        /// Process spawn requests from the queue
        /// </summary>
        private IEnumerator ProcessSpawnQueueCoroutine()
        {
            var wait = new WaitForSeconds(0.1f);

            while (true)
            {
                yield return wait;

                if (spawnQueue.Count == 0 || performanceMode)
                    continue;

                if (Time.time - lastSpawnTime < (spawnCooldown?.Value ?? 2f))
                    continue;

                var request = spawnQueue.Dequeue();
                
                // Check if request is still valid
                if (CanSpawn(request.IsFriendly))
                {
                    ExecuteSpawn(request);
                }
                else
                {
                    // Re-queue if at limit but not too old
                    if ((DateTime.Now - request.RequestTime).TotalSeconds < 30)
                    {
                        spawnQueue.Enqueue(request);
                    }
                }
            }
        }

        /// <summary>
        /// Execute a spawn request
        /// </summary>
        private void ExecuteSpawn(SpawnRequest request)
        {
            try
            {
                SpawnerName = request.UserName;

                if (request.IsFriendly)
                {
                    SpawningSequence(request.UserName);
                }
                else
                {
                    SpawningSequenceEnemy((int)(enemyIFF?.Value ?? 1f), request.UserName);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error executing spawn for {request.UserName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Update all active sosigs
        /// </summary>
        private IEnumerator UpdateSosigsCoroutine()
        {
            var wait = new WaitForSeconds(1f);

            while (true)
            {
                yield return wait;
                UpdateAllSosigs();
            }
        }

        /// <summary>
        /// Monitor performance and adjust behavior
        /// </summary>
        private IEnumerator PerformanceMonitorCoroutine()
        {
            var wait = new WaitForSeconds(PerformanceCheckInterval);

            while (true)
            {
                yield return wait;
                MonitorPerformance();
            }
        }

        /// <summary>
        /// Clean up expired sosigs
        /// </summary>
        private IEnumerator CleanupCoroutine()
        {
            var wait = new WaitForSeconds(10f);

            while (true)
            {
                yield return wait;
                CleanupExpiredSosigs();
            }
        }

        /// <summary>
        /// Update all active sosigs
        /// </summary>
        private void UpdateAllSosigs()
        {
            UpdateSosigList(ActiveAllies, spawnedChatters);
            UpdateSosigList(ActiveEnemies, spawnedEnemyChatters);
        }

        /// <summary>
        /// Update specific sosig list
        /// </summary>
        private void UpdateSosigList(List<ChatSosig> chatSosigs, List<Sosig> legacyList)
        {
            for (int i = chatSosigs.Count - 1; i >= 0; i--)
            {
                var chatSosig = chatSosigs[i];

                if (!chatSosig.IsValid || chatSosig.IsDead)
                {
                    DestroyChatSosig(chatSosig);
                    continue;
                }

                // Update advanced AI if enabled
                if (enableAdvancedAI?.Value == true)
                {
                    UpdateAdvancedAI(chatSosig);
                }

                // Check lifetime
                if (chatSosig.Lifetime > 0 && chatSosig.Age > chatSosig.Lifetime)
                {
                    logger?.LogInfo($"Sosig {chatSosig.UserName} expired after {chatSosig.Lifetime} seconds");
                    DestroyChatSosig(chatSosig);
                }
            }

            // Also clean legacy lists
            for (int i = legacyList.Count - 1; i >= 0; i--)
            {
                if (legacyList[i] == null || legacyList[i].BodyState == Sosig.SosigBodyState.Dead)
                {
                    legacyList.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Update advanced AI behaviors
        /// </summary>
        private void UpdateAdvancedAI(ChatSosig chatSosig)
        {
            if (!chatSosig.IsValid || GM.CurrentPlayerBody?.Head == null)
                return;

            try
            {
                var sosig = chatSosig.Sosig;
                var playerPos = GM.CurrentPlayerBody.Head.position;
                var sosigPos = sosig.transform.position;
                var distance = Vector3.Distance(playerPos, sosigPos);

                if (chatSosig.IsFriendly)
                {
                    // Allies follow at reasonable distance
                    if (distance > 8f && sosig.CurrentOrder != Sosig.SosigOrder.Assault)
                    {
                        sosig.CommandAssaultPoint(playerPos + UnityEngine.Random.insideUnitSphere * 3f);
                    }
                }
                else
                {
                    // Enemies engage more aggressively
                    if (distance < 20f && sosig.CurrentOrder == Sosig.SosigOrder.Idle)
                    {
                        sosig.CommandAssaultPoint(playerPos);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error in advanced AI update: {ex.Message}");
            }
        }

        /// <summary>
        /// Monitor performance and enable performance mode if needed
        /// </summary>
        private void MonitorPerformance()
        {
            try
            {
                recentFrameTimes.Add(Time.deltaTime);
                if (recentFrameTimes.Count > 60) // Keep last 60 frames
                    recentFrameTimes.RemoveAt(0);

                float averageFrameTime = recentFrameTimes.Average();
                bool shouldEnterPerformanceMode = averageFrameTime > 0.033f; // 30 FPS threshold

                if (shouldEnterPerformanceMode != performanceMode)
                {
                    performanceMode = shouldEnterPerformanceMode;
                    logger?.LogWarning($"Performance mode {(performanceMode ? "enabled" : "disabled")}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error in performance monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up expired sosigs
        /// </summary>
        private void CleanupExpiredSosigs()
        {
            if (enableAutoCleanup?.Value != true)
                return;

            try
            {
                var expiredSosigs = ActiveAllies.Concat(ActiveEnemies)
                    .Where(cs => cs.Lifetime > 0 && cs.Age > cs.Lifetime)
                    .ToList();

                foreach (var sosig in expiredSosigs)
                {
                    DestroyChatSosig(sosig);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error in cleanup: {ex.Message}");
            }
        }
        #endregion

        #region Enhanced Stats Reporting
        /// <summary>
        /// Get comprehensive status report including dependencies
        /// </summary>
        public ChatSosigStats GetStatsWithDependencies()
        {
            var stats = GetStats();
            
            // Add dependency information
            var enhancedStats = new EnhancedChatSosigStats
            {
                ActiveAllies = stats.ActiveAllies,
                ActiveEnemies = stats.ActiveEnemies,
                QueueLength = stats.QueueLength,
                TotalSpawned = stats.TotalSpawned,
                PerformanceMode = stats.PerformanceMode,
                DependencyStatus = OptionalDependencyManager.GetDependencyStatusReport(),
                WeaponEnhancementStatus = SosigWeaponEnhancer.GetEnhancementStats(),
                EnhancementsActive = OptionalDependencyManager.GetAvailableDependencyCount()
            };

            return enhancedStats;
        }

        /// <summary>
        /// Enhanced stats class with dependency information
        /// </summary>
        public class EnhancedChatSosigStats : ChatSosigStats
        {
            public string DependencyStatus { get; set; }
            public string WeaponEnhancementStatus { get; set; }
            public int EnhancementsActive { get; set; }
        }
        #endregion
    }
}