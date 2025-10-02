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
    /// Enhanced Chat Spawner - Advanced Sosig spawning system with TwitchLib integration
    /// Features: TwitchLib real-time chat integration, name loading from INI files, armor GUI integration, nameplate display,
    /// advanced AI behaviors, dynamic difficulty scaling, audio integration, and performance monitoring
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
        public static event Action<ChatSosig, string> OnSosigBehaviorChanged; // sosig, new behavior
        public static event Action<float> OnDifficultyChanged; // new difficulty level
        #endregion

        #region Core Components
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        private TwitchChatManager twitchManager;
        private AudioManager audioManager;
        #endregion

        #region Sosig Templates and Assets
        [Header("Sosig Templates")]
        public SosigEnemyTemplate defaultAllyTemplate;
        public List<SosigEnemyTemplate> allyTemplates = new List<SosigEnemyTemplate>();
        public List<SosigEnemyTemplate> enemyTemplates = new List<SosigEnemyTemplate>();
        
        [Header("Nameplate System")]
        public GameObject nameplatePrefab;
        public Font nameplateFont;
        public Material nameplateMaterial;
        
        [Header("Audio Integration")]
        public AudioClip spawnSound;
        public AudioClip deathSound;
        public AudioClip commandSuccessSound;
        public AudioClip commandFailSound;
        
        private SosigEnemyTemplate[] cachedSosigTemplates;
        #endregion

        #region Sosig Management with Twitch Integration
        public List<ChatSosig> ActiveAllies { get; private set; } = new List<ChatSosig>();
        public List<ChatSosig> ActiveEnemies { get; private set; } = new List<ChatSosig>();
        private readonly Dictionary<Sosig, ChatSosig> sosigLookup = new Dictionary<Sosig, ChatSosig>();
        private readonly Dictionary<string, List<ChatSosig>> userSosigMap = new Dictionary<string, List<ChatSosig>>();
        #endregion

        #region Enhanced AI and Behavior System
        public enum SosigBehaviorState
        {
            Idle,
            Following,
            Guarding,
            Patrolling,
            Attacking,
            Searching,
            Retreating,
            Supporting,
            Custom
        }

        private readonly Dictionary<ChatSosig, SosigBehaviorState> sosigBehaviors = new Dictionary<ChatSosig, SosigBehaviorState>();
        private readonly Dictionary<ChatSosig, Vector3> sosigWaypoints = new Dictionary<ChatSosig, Vector3>();
        private readonly Dictionary<ChatSosig, float> sosigNextBehaviorUpdate = new Dictionary<ChatSosig, float>();
        #endregion

        #region Dynamic Difficulty System
        private float currentDifficulty = 1.0f;
        private readonly List<float> recentPlayerPerformance = new List<float>();
        private DateTime lastDifficultyAdjustment = DateTime.Now;
        private int playerKillCount;
        private int sosigKillCount;
        private const float DifficultyAdjustmentInterval = 120f; // 2 minutes
        #endregion

        #region Name Management System (Enhanced with Twitch)
        private List<string> allyNames = new List<string>();
        private List<string> enemyNames = new List<string>();
        private Dictionary<string, string> usedNames = new Dictionary<string, string>(); // sosig -> name mapping
        private Dictionary<string, string> twitchUserNames = new Dictionary<string, string>(); // twitch user -> display name
        private Dictionary<string, List<string>> customUserNames = new Dictionary<string, List<string>>(); // user -> custom names
        private string allyNamesPath;
        private string enemyNamesPath;
        private DateTime lastAllyNamesCheck;
        private DateTime lastEnemyNamesCheck;
        private readonly TimeSpan nameFileCheckInterval = TimeSpan.FromSeconds(30);
        #endregion

        #region Configuration (Enhanced)
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
        private ConfigEntry<string> allyNamesFile;
        private ConfigEntry<string> enemyNamesFile;
        private ConfigEntry<KeyCode> spawnAllyKey;
        private ConfigEntry<KeyCode> spawnEnemyKey;
        private ConfigEntry<KeyCode> clearSosigsKey;
        private ConfigEntry<float> nameplateHeight;
        private ConfigEntry<float> nameplateScale;
        private ConfigEntry<Color> allyNameplateColor;
        private ConfigEntry<Color> enemyNameplateColor;
        
        // Enhanced Twitch-specific config
        private ConfigEntry<bool> enableTwitchIntegration;
        private ConfigEntry<bool> useTwitchNamesOverIni;
        private ConfigEntry<bool> enableTwitchUserTracking;
        private ConfigEntry<int> maxSosigsPerTwitchUser;
        
        // New Advanced Features
        private ConfigEntry<bool> enableDynamicDifficulty;
        private ConfigEntry<bool> enableSosigPersonalities;
        private ConfigEntry<bool> enableAudioFeedback;
        private ConfigEntry<bool> enableSosigChat;
        private ConfigEntry<float> sosigChatFrequency;
        private ConfigEntry<bool> enableBehaviorCommands;
        private ConfigEntry<bool> enableSosigGroups;
        private ConfigEntry<int> maxSosigGroupSize;
        private ConfigEntry<bool> enablePerformanceScaling;
        private ConfigEntry<float> performanceThreshold;
        private ConfigEntry<bool> enableSosigExperience;
        private ConfigEntry<float> experienceGainRate;
        
        // Advanced Behavior Settings
        private ConfigEntry<float> allyFollowDistance;
        private ConfigEntry<float> allyReactionTime;
        private ConfigEntry<float> enemyAggressionLevel;
        private ConfigEntry<bool> enableAdvancedPathfinding;
        private ConfigEntry<bool> enableSosigCommunication;
        #endregion

        #region Spawn Queue and Management (Enhanced for Twitch)
        private readonly Queue<TwitchSpawnRequest> spawnQueue = new Queue<TwitchSpawnRequest>();
        private readonly Queue<TwitchSpawnRequest> prioritySpawnQueue = new Queue<TwitchSpawnRequest>();
        private readonly Dictionary<string, DateTime> userSpawnCooldowns = new Dictionary<string, DateTime>();
        private float lastSpawnTime;
        private int totalSpawnCount;
        public string SpawnerName { get; set; } = "ChatUser";
        #endregion

        #region Performance Monitoring (Enhanced)
        private float lastPerformanceCheck;
        private const float PerformanceCheckInterval = 5f;
        private readonly List<float> recentFrameTimes = new List<float>();
        private readonly List<int> recentSosigCounts = new List<int>();
        private bool performanceMode;
        private float currentFrameRate;
        private float averageFrameTime;
        private int recommendedSosigCount;
        #endregion

        #region Audio Integration System
        private readonly Dictionary<string, AudioClip> customSounds = new Dictionary<string, AudioClip>();
        private readonly Dictionary<ChatSosig, AudioSource> sosigAudioSources = new Dictionary<ChatSosig, AudioSource>();
        private AudioSource globalAudioSource;
        #endregion

        #region User Experience and Feedback System
        private readonly Dictionary<string, UserExperience> userExperienceData = new Dictionary<string, UserExperience>();
        private readonly Queue<UserNotification> notificationQueue = new Queue<UserNotification>();
        
        public class UserExperience
        {
            public string Username { get; set; }
            public int TotalSosigsSpawned { get; set; }
            public int SosigsKilled { get; set; }
            public int PlayerKills { get; set; }
            public float ExperiencePoints { get; set; }
            public int Level { get; set; }
            public DateTime FirstSpawn { get; set; }
            public DateTime LastActivity { get; set; }
            public List<string> UnlockedFeatures { get; set; } = new List<string>();
            public Dictionary<string, int> PreferredArmor { get; set; } = new Dictionary<string, int>();
        }

        public class UserNotification
        {
            public string Username { get; set; }
            public string Message { get; set; }
            public NotificationType Type { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error,
            Achievement
        }
        #endregion

        #region Twitch Spawn Request Class (Enhanced)
        public class TwitchSpawnRequest : SpawnRequest
        {
            public string TwitchUsername { get; set; }
            public bool IsFromTwitch { get; set; }
            public DateTime TwitchRequestTime { get; set; }
            public string TwitchDisplayName { get; set; }
            public string RequestedBehavior { get; set; }
            public Vector3? PreferredSpawnLocation { get; set; }
            public Dictionary<string, string> ChatTags { get; set; } = new Dictionary<string, string>();
        }

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

        #region ChatSosig Wrapper Class (Enhanced with Advanced Features)
        public class ChatSosig
        {
            public Sosig Sosig { get; set; }
            public string UserName { get; set; }
            public string DisplayName { get; set; }
            public string TwitchUsername { get; set; }
            public bool IsFriendly { get; set; }
            public string ArmorPreset { get; set; }
            public DateTime SpawnTime { get; set; }
            public float Lifetime { get; set; }
            public GameObject Nameplate { get; set; }
            public bool IsFromTwitch { get; set; }
            public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
            
            // Enhanced Properties
            public SosigBehaviorState CurrentBehavior { get; set; } = SosigBehaviorState.Idle;
            public float ExperiencePoints { get; set; }
            public int Level { get; set; } = 1;
            public List<string> PersonalityTraits { get; set; } = new List<string>();
            public Vector3 LastKnownPosition { get; set; }
            public DateTime LastBehaviorChange { get; set; }
            public int KillCount { get; set; }
            public int DamageDealt { get; set; }
            public int DamageTaken { get; set; }
            public bool IsGroupLeader { get; set; }
            public List<ChatSosig> GroupMembers { get; set; } = new List<ChatSosig>();
            public ChatSosig GroupLeader { get; set; }
            public AudioSource AudioSource { get; set; }
            public float NextChatTime { get; set; }
            public List<string> ChatPhrases { get; set; } = new List<string>();
            
            public bool IsValid => Sosig != null && Sosig.gameObject != null;
            public bool IsDead => Sosig == null || Sosig.BodyState == Sosig.SosigBodyState.Dead;
            public float Age => Time.time - (float)SpawnTime.Subtract(DateTime.MinValue).TotalSeconds;
            public bool IsExperienced => ExperiencePoints >= 100f;
            public bool IsVeteran => ExperiencePoints >= 500f;
        }
        #endregion

        #region Public API Methods (Enhanced for Advanced Features)
        /// <summary>
        /// Queue a spawn request from Twitch chat with enhanced tracking and behavior options
        /// </summary>
        public bool QueueTwitchSpawnRequest(string twitchUsername, string displayName, bool isFriendly, string armorPreset = null, SpawnPriority priority = SpawnPriority.Normal, string requestedBehavior = null)
        {
            // Check user cooldown
            if (IsUserOnCooldown(twitchUsername))
            {
                logger?.LogWarning($"Twitch user {twitchUsername} is on spawn cooldown");
                NotifyUser(twitchUsername, "You are on cooldown. Please wait before spawning another sosig.", NotificationType.Warning);
                return false;
            }

            // Check sosig limits
            if (!CanSpawn(isFriendly))
            {
                logger?.LogWarning($"Cannot spawn {(isFriendly ? "ally" : "enemy")} - at limit");
                NotifyUser(twitchUsername, $"Cannot spawn {(isFriendly ? "ally" : "enemy")} - server at capacity.", NotificationType.Error);
                return false;
            }

            // Check per-user limits
            if (enableTwitchUserTracking.Value && GetUserActiveSosigCount(twitchUsername) >= maxSosigsPerTwitchUser.Value)
            {
                logger?.LogWarning($"Twitch user {twitchUsername} already has maximum sosigs active");
                NotifyUser(twitchUsername, $"You already have the maximum number of sosigs active ({maxSosigsPerTwitchUser.Value}).", NotificationType.Warning);
                return false;
            }

            // Check if user qualifies for advanced features
            var userExp = GetUserExperience(twitchUsername);
            bool canUseAdvancedFeatures = userExp.Level >= 3 || userExp.ExperiencePoints >= 150f;

            var request = new TwitchSpawnRequest
            {
                UserName = twitchUsername,
                DisplayName = displayName ?? twitchUsername,
                TwitchUsername = twitchUsername,
                TwitchDisplayName = displayName,
                IsFriendly = isFriendly,
                ArmorPreset = armorPreset ?? (isFriendly ? defaultAllyArmor.Value : defaultEnemyArmor.Value),
                RequestTime = DateTime.Now,
                TwitchRequestTime = DateTime.Now,
                Priority = priority,
                IsFromTwitch = true,
                RequestedBehavior = canUseAdvancedFeatures ? requestedBehavior : null,
                CustomData = new Dictionary<string, object>
                {
                    { "TwitchUser", true },
                    { "OriginalUsername", twitchUsername },
                    { "UserLevel", userExp.Level },
                    { "UserExperience", userExp.ExperiencePoints }
                }
            };

            // Add to appropriate queue based on priority
            if (priority == SpawnPriority.Immediate || priority == SpawnPriority.High)
            {
                prioritySpawnQueue.Enqueue(request);
            }
            else
            {
                spawnQueue.Enqueue(request);
            }

            // Set user cooldown with experience-based reduction
            float cooldownMultiplier = Mathf.Max(0.3f, 1f - (userExp.Level * 0.1f));
            userSpawnCooldowns[twitchUsername] = DateTime.Now.AddSeconds(spawnCooldown.Value * cooldownMultiplier);

            logger?.LogInfo($"Queued Twitch spawn request for {twitchUsername} ({displayName}) ({(isFriendly ? "ally" : "enemy")})");
            NotifyUser(twitchUsername, $"{(isFriendly ? "Ally" : "Enemy")} sosig queued for spawn!", NotificationType.Success);
            
            return true;
        }

        /// <summary>
        /// Advanced sosig behavior control
        /// </summary>
        public bool SetSosigBehavior(string username, SosigBehaviorState behavior)
        {
            var sosigs = GetSosigsByTwitchUser(username);
            if (sosigs.Count == 0)
            {
                NotifyUser(username, "You don't have any active sosigs to control.", NotificationType.Warning);
                return false;
            }

            bool anyChanged = false;
            foreach (var sosig in sosigs)
            {
                if (SetSosigBehavior(sosig, behavior))
                {
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                NotifyUser(username, $"Changed behavior of your sosigs to {behavior}.", NotificationType.Success);
                PlayAudioFeedback(commandSuccessSound);
            }

            return anyChanged;
        }

        /// <summary>
        /// Set individual sosig behavior
        /// </summary>
        public bool SetSosigBehavior(ChatSosig chatSosig, SosigBehaviorState behavior)
        {
            if (!chatSosig.IsValid) return false;

            var oldBehavior = chatSosig.CurrentBehavior;
            chatSosig.CurrentBehavior = behavior;
            chatSosig.LastBehaviorChange = DateTime.Now;
            
            sosigBehaviors[chatSosig] = behavior;
            sosigNextBehaviorUpdate[chatSosig] = Time.time + UnityEngine.Random.Range(1f, 3f);

            // Apply behavior immediately
            ApplyBehaviorToSosig(chatSosig, behavior);

            OnSosigBehaviorChanged?.Invoke(chatSosig, behavior.ToString());
            logger?.LogDebug($"Changed {chatSosig.DisplayName} behavior from {oldBehavior} to {behavior}");

            return true;
        }

        /// <summary>
        /// Create sosig group for coordinated behavior
        /// </summary>
        public bool CreateSosigGroup(string username, List<ChatSosig> sosigs, ChatSosig leader = null)
        {
            if (!enableSosigGroups.Value || sosigs.Count > maxSosigGroupSize.Value)
                return false;

            if (leader == null)
                leader = sosigs.FirstOrDefault(s => s.IsValid);

            if (leader == null) return false;

            // Set up group
            leader.IsGroupLeader = true;
            leader.GroupMembers.Clear();
            leader.GroupMembers.AddRange(sosigs.Where(s => s != leader));

            foreach (var member in sosigs.Where(s => s != leader))
            {
                member.GroupLeader = leader;
                member.IsGroupLeader = false;
            }

            logger?.LogInfo($"Created sosig group for {username} with {sosigs.Count} members, leader: {leader.DisplayName}");
            NotifyUser(username, $"Created sosig group with {sosigs.Count} members!", NotificationType.Success);

            return true;
        }

        /// <summary>
        /// Get comprehensive stats including advanced metrics
        /// </summary>
        public ChatSosigStats GetStats()
        {
            return new ChatSosigStats
            {
                activeSosigCount = ActiveAllies.Count + ActiveEnemies.Count,
                friendlyCount = ActiveAllies.Count,
                enemyCount = ActiveEnemies.Count,
                queuedSpawns = spawnQueue.Count + prioritySpawnQueue.Count,
                totalSpawned = totalSpawnCount,
                ActiveAllies = ActiveAllies.Count,
                ActiveEnemies = ActiveEnemies.Count,
                QueueLength = spawnQueue.Count + prioritySpawnQueue.Count,
                TotalSpawned = totalSpawnCount
            };
        }
        
        /// <summary>
        /// Clear all sosigs (allies, enemies, or both)
        /// </summary>
        public void ClearSosigs(bool clearAllies = true, bool clearEnemies = true)
        {
            try
            {
                int cleared = 0;

                if (clearAllies)
                {
                    for (int i = ActiveAllies.Count - 1; i >= 0; i--)
                    {
                        var chatSosig = ActiveAllies[i];
                        if (chatSosig?.Sosig != null)
                        {
                            Destroy(chatSosig.Sosig.gameObject);
                            cleared++;
                        }
                    }
                    ActiveAllies.Clear();
                    spawnedChatters.Clear();
                }

                if (clearEnemies)
                {
                    for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
                    {
                        var chatSosig = ActiveEnemies[i];
                        if (chatSosig?.Sosig != null)
                        {
                            Destroy(chatSosig.Sosig.gameObject);
                            cleared++;
                        }
                    }
                    ActiveEnemies.Clear();
                    spawnedEnemyChatters.Clear();
                }

                // Clear all tracking
                sosigLookup.Clear();
                sosigBehaviors.Clear();
                sosigNextBehaviorUpdate.Clear();
                sosigWaypoints.Clear();
                sosigAudioSources.Clear();
                userSosigMap.Clear();

                logger?.LogInfo($"Cleared {cleared} sosigs");
                OnSosigCountChanged?.Invoke(ActiveAllies.Count, ActiveEnemies.Count);

                // Play audio feedback
                if (enableAudioFeedback != null && enableAudioFeedback.Value)
                {
                    PlayAudioFeedback(commandSuccessSound);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error clearing sosigs: {ex.Message}");
            }
        }

        /// <summary>
        /// Notify a user with a message
        /// </summary>
        public void NotifyUser(string username, string message, NotificationType type)
        {
            try
            {
                if (string.IsNullOrEmpty(username)) return;

                var notification = new UserNotification
                {
                    Username = username,
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.Now
                };

                notificationQueue.Enqueue(notification);

                // Also log for visibility
                switch (type)
                {
                    case NotificationType.Error:
                        logger?.LogError($"[{username}] {message}");
                        break;
                    case NotificationType.Warning:
                        logger?.LogWarning($"[{username}] {message}");
                        break;
                    case NotificationType.Achievement:
                    case NotificationType.Success:
                        logger?.LogInfo($"[{username}] ✓ {message}");
                        break;
                    default:
                        logger?.LogInfo($"[{username}] {message}");
                        break;
                }

                // Send to Twitch chat if available
                if (twitchManager != null && enableTwitchIntegration != null && enableTwitchIntegration.Value)
                {
                    try
                    {
                        twitchManager.SendChatMessage($"@{username} {message}");
                    }
                    catch
                    {
                        // Ignore errors sending to Twitch
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error notifying user {username}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get user experience data, creating if needed
        /// </summary>
        public UserExperience GetUserExperience(string username)
        {
            if (!userExperienceData.TryGetValue(username, out var experience))
            {
                experience = new UserExperience
                {
                    Username = username,
                    FirstSpawn = DateTime.Now,
                    LastActivity = DateTime.Now,
                    Level = 1
                };
                userExperienceData[username] = experience;
            }

            return experience;
        }

        /// <summary>
        /// Award experience to user
        /// </summary>
        public void AwardExperience(string username, float points, string reason = "")
        {
            if (!enableSosigExperience.Value) return;

            var userExp = GetUserExperience(username);
            userExp.ExperiencePoints += points * experienceGainRate.Value;
            userExp.LastActivity = DateTime.Now;

            // Check for level up
            int newLevel = Mathf.FloorToInt(userExp.ExperiencePoints / 100f) + 1;
            if (newLevel > userExp.Level)
            {
                userExp.Level = newLevel;
                NotifyUser(username, $"Level up! You are now level {newLevel}! {GetLevelUpReward(newLevel)}", NotificationType.Achievement);
                PlayAudioFeedback(commandSuccessSound);
            }

            if (!string.IsNullOrEmpty(reason))
            {
                logger?.LogDebug($"Awarded {points} XP to {username} for {reason}");
            }
        }

        /// <summary>
        /// Get level up rewards
        /// </summary>
        private string GetLevelUpReward(int level)
        {
            switch (level)
            {
                case 2:
                    return "Unlocked: Faster spawn cooldown!";
                case 3:
                    return "Unlocked: Custom behaviors!";
                case 5:
                    return "Unlocked: Advanced sosig commands!";
                case 10:
                    return "Unlocked: Elite sosig variants!";
                default:
                    return level % 5 == 0 ? "Unlocked: Special reward!" : "";
            }
        }
        
        /// <summary>
        /// Stats structure for sosig spawning
        /// </summary>
        public struct ChatSosigStats
        {
            public int activeSosigCount;
            public int friendlyCount;
            public int enemyCount;
            public int queuedSpawns;
            public int totalSpawned;
            
            // Additional properties for TwitchChatManager compatibility
            public int ActiveAllies;
            public int ActiveEnemies;
            public int QueueLength;
            public int TotalSpawned;
        }
        #endregion

        #region Initialization (Enhanced)
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

            // Set up name file paths
            SetupNameFilePaths();

            // Load initial names
            LoadNamesFromFiles();

            // Initialize Twitch integration if enabled
            if (enableTwitchIntegration.Value)
            {
                InitializeTwitchIntegration();
            }

            // Initialize audio system
            InitializeAudioSystem();

            logger?.LogInfo("Enhanced Chat Spawner initialized with TwitchLib integration");

            // Start coroutines
            StartCoroutine(ProcessSpawnQueueCoroutine());
            StartCoroutine(UpdateSosigsCoroutine());
            StartCoroutine(PerformanceMonitorCoroutine());
            StartCoroutine(CleanupCoroutine());
            StartCoroutine(NameFileMonitorCoroutine());
            StartCoroutine(BehaviorUpdateCoroutine());
            StartCoroutine(DifficultyUpdateCoroutine());
            StartCoroutine(NotificationProcessorCoroutine());
        }

        /// <summary>
        /// Initialize all configuration entries (Enhanced with Twitch settings)
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
                    "Show nameplates above sosigs");
                enableVoiceLines = plugin.Config.Bind("Enhanced Chat Spawner", "EnableVoiceLines", false, 
                    "Enable voice lines");
                enableSpawnEffects = plugin.Config.Bind("Enhanced Chat Spawner", "EnableSpawnEffects", true, 
                    "Enable spawn effects");
                
                // Defaults
                defaultAllyArmor = plugin.Config.Bind("Enhanced Chat Spawner", "DefaultAllyArmor", "Standard", 
                    "Default ally armor preset");
                defaultEnemyArmor = plugin.Config.Bind("Enhanced Chat Spawner", "DefaultEnemyArmor", "Heavy Assault", 
                    "Default enemy armor preset");
                sosigLifetime = plugin.Config.Bind("Enhanced Chat Spawner", "SosigLifetime", 300.0f, 
                    "Sosig lifetime seconds");
                enableAutoCleanup = plugin.Config.Bind("Enhanced Chat Spawner", "EnableAutoCleanup", true, 
                    "Auto cleanup expired");
                enemyIFF = plugin.Config.Bind("Enhanced Chat Spawner", "EnemyIFF", 1.0f, 
                    "Enemy IFF code");
                
                // Name file paths
                allyNamesFile = plugin.Config.Bind("Enhanced Chat Spawner", "AllyNamesFile", "H3TVR_AllyNames.ini", 
                    "Ally names INI file");
                enemyNamesFile = plugin.Config.Bind("Enhanced Chat Spawner", "EnemyNamesFile", "H3TVR_EnemyNames.ini", 
                    "Enemy names INI file");
                
                // Twitch Integration Settings
                enableTwitchIntegration = plugin.Config.Bind("Enhanced Chat Spawner", "EnableTwitchIntegration", true, 
                    "Enable TwitchLib integration for real-time chat");
                useTwitchNamesOverIni = plugin.Config.Bind("Enhanced Chat Spawner", "UseTwitchNamesOverIni", true, 
                    "Use Twitch usernames instead of INI file names");
                enableTwitchUserTracking = plugin.Config.Bind("Enhanced Chat Spawner", "EnableTwitchUserTracking", true, 
                    "Track sosigs per Twitch user");
                maxSosigsPerTwitchUser = plugin.Config.Bind("Enhanced Chat Spawner", "MaxSosigsPerTwitchUser", 2, 
                    "Maximum sosigs per Twitch user");
                
                // Advanced Features
                enableDynamicDifficulty = plugin.Config.Bind("Enhanced Chat Spawner", "EnableDynamicDifficulty", true, 
                    "Enable dynamic difficulty scaling based on player performance");
                enableSosigPersonalities = plugin.Config.Bind("Enhanced Chat Spawner", "EnableSosigPersonalities", true, 
                    "Enable distinct sosig personalities and traits");
                enableAudioFeedback = plugin.Config.Bind("Enhanced Chat Spawner", "EnableAudioFeedback", true, 
                    "Enable audio feedback for commands and events");
                enableSosigChat = plugin.Config.Bind("Enhanced Chat Spawner", "EnableSosigChat", true, 
                    "Enable chat interactions for sosigs");
                sosigChatFrequency = plugin.Config.Bind("Enhanced Chat Spawner", "SosigChatFrequency", 0.1f, 
                    "Frequency of sosig chat messages (lower is more frequent)");
                enableBehaviorCommands = plugin.Config.Bind("Enhanced Chat Spawner", "EnableBehaviorCommands", true, 
                    "Enable custom behavior commands for sosigs");
                enableSosigGroups = plugin.Config.Bind("Enhanced Chat Spawner", "EnableSosigGroups", true, 
                    "Enable grouping of sosigs for coordinated actions");
                maxSosigGroupSize = plugin.Config.Bind("Enhanced Chat Spawner", "MaxSosigGroupSize", 5, 
                    "Maximum size of sosig groups");
                enablePerformanceScaling = plugin.Config.Bind("Enhanced Chat Spawner", "EnablePerformanceScaling", true, 
                    "Enable scaling of sosig performance based on system capability");
                performanceThreshold = plugin.Config.Bind("Enhanced Chat Spawner", "PerformanceThreshold", 0.033f, 
                    "Frame time threshold for performance scaling (in seconds)");
                enableSosigExperience = plugin.Config.Bind("Enhanced Chat Spawner", "EnableSosigExperience", true, 
                    "Enable experience and leveling for sosigs");
                experienceGainRate = plugin.Config.Bind("Enhanced Chat Spawner", "ExperienceGainRate", 1.0f, 
                    "Rate of experience gain for sosigs");
                
                // Nameplate settings
                nameplateHeight = plugin.Config.Bind("Enhanced Chat Spawner Nameplates", "NameplateHeight", 2.5f, 
                    "Height above sosig head for nameplate");
                nameplateScale = plugin.Config.Bind("Enhanced Chat Spawner Nameplates", "NameplateScale", 0.02f, 
                    "Scale of nameplate text");
                allyNameplateColor = plugin.Config.Bind("Enhanced Chat Spawner Nameplates", "AllyNameplateColor", Color.green, 
                    "Color for ally nameplates");
                enemyNameplateColor = plugin.Config.Bind("Enhanced Chat Spawner Nameplates", "EnemyNameplateColor", Color.red, 
                    "Color for enemy nameplates");
                
                // Keys
                spawnAllyKey = plugin.Config.Bind("Enhanced Chat Spawner Keys", "SpawnAllyKey", KeyCode.P, 
                    "Spawn ally key");
                spawnEnemyKey = plugin.Config.Bind("Enhanced Chat Spawner Keys", "SpawnEnemyKey", KeyCode.O, 
                    "Spawn enemy key");
                clearSosigsKey = plugin.Config.Bind("Enhanced Chat Spawner Keys", "ClearSosigsKey", KeyCode.Delete, 
                    "Clear sosigs key");

                logger?.LogInfo("Configuration initialized successfully with Twitch integration");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Config init failed: {ex.Message}");
            }

            // Initialize sosig templates
            InitializeSosigTemplates();
        }

        /// <summary>
        /// Set up paths for name files
        /// </summary>
        private void SetupNameFilePaths()
        {
            try
            {
                string configDir = Path.Combine(Path.GetDirectoryName(plugin.Config.ConfigFilePath), "config");
                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);

                allyNamesPath = Path.Combine(configDir, allyNamesFile?.Value ?? "H3TVR_AllyNames.ini");
                enemyNamesPath = Path.Combine(configDir, enemyNamesFile?.Value ?? "H3TVR_EnemyNames.ini");

                logger?.LogInfo($"Name files: Allies={allyNamesPath}, Enemies={enemyNamesPath}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to setup name file paths: {ex.Message}");
                // Fallback paths
                allyNamesPath = "H3TVR_AllyNames.ini";
                enemyNamesPath = "H3TVR_EnemyNames.ini";
            }
        }
        #endregion

        #region Template Loading and Management
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

            Exception caughtException = null;
            bool done = false;
            try
            {
                // Try to get templates from various H3VR manager sources
                LoadTemplatesFromManagers();
                done = true;
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
            if (!done && caughtException != null)
            {
                logger?.LogError($"Template loading failed: {caughtException.Message}");
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

            // If no templates found, create fallbacks
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

        #region Core Sosig Spawning Logic
        /// <summary>
        /// Core sosig spawning method using H3VR systems
        /// </summary>
        private Sosig SpawnSosigFromTemplate(SosigEnemyTemplate template, Vector3 position, Quaternion rotation, int IFF, string displayName)
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
                if (IFF < sosig.Priority.IFFChart.Length)
                {
                    sosig.Priority.IFFChart[IFF] = true;
                }

                // Equip weapons
                EquipSosigWeapons(sosig, template, position, rotation);

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
        /// Apply armor to sosig using the armor GUI system
        /// </summary>
        private void ApplyArmorToSosig(Sosig sosig, bool isFriendly)
        {
            try
            {
                if (sosig?.Links == null || sosig.Links.Count == 0)
                {
                    logger?.LogWarning("Cannot apply armor - sosig has no valid links");
                    return;
                }

                // Try to get the armor integration from the plugin
                var armorIntegration = plugin?.GetComponent<SosigArmorWristMenuIntegration>();
                if (armorIntegration != null && armorIntegration.IsArmorIntegrationAvailable())
                {
                    // Use the armor GUI system
                    armorIntegration.ApplyArmorToSosig(sosig, isFriendly);
                    logger?.LogDebug($"Applied armor to sosig via armor GUI system (faction: {(isFriendly ? "ally" : "enemy")})");
                }
                else
                {
                    // Fallback to basic armor if GUI system not available
                    logger?.LogDebug("Armor GUI system not available, applying basic armor");
                    ApplyBasicArmorFallback(sosig, isFriendly);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to apply armor to sosig: {ex.Message}");
                // Try fallback armor
                ApplyBasicArmorFallback(sosig, isFriendly);
            }
        }

        /// <summary>
        /// Apply basic armor when GUI system is not available
        /// </summary>
        private void ApplyBasicArmorFallback(Sosig sosig, bool isFriendly)
        {
            try
            {
                if (sosig?.Links == null || sosig.Links.Count == 0) return;

                // Apply basic outfit from template if available
                if (allyTemplates.Count > 0 && isFriendly)
                {
                    var template = allyTemplates[0];
                    if (template.OutfitConfig != null && template.OutfitConfig.Count > 0)
                    {
                        ApplyOutfitToSosig(sosig, template);
                    }
                }
                else if (enemyTemplates.Count > 0 && !isFriendly)
                {
                    var template = enemyTemplates[0];
                    if (template.OutfitConfig != null && template.OutfitConfig.Count > 0)
                    {
                        ApplyOutfitToSosig(sosig, template);
                    }
                }

                logger?.LogDebug($"Applied basic armor fallback for {(isFriendly ? "ally" : "enemy")} sosig");
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Failed to apply basic armor fallback: {ex.Message}");
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

        #region Coroutines (Enhanced)
        /// <summary>
        /// Monitor name files for changes
        /// </summary>
        private IEnumerator NameFileMonitorCoroutine()
        {
            var wait = new WaitForSeconds(30f); // Check every 30 seconds

            while (true)
            {
                yield return wait;
                LoadNamesFromFiles();
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
                MonitorPerformanceEnhanced();
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
                    logger?.LogInfo($"Sosig {chatSosig.DisplayName} expired after {chatSosig.Lifetime} seconds");
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

        /// <summary>
        /// Coroutine for updating advanced behaviors
        /// </summary>
        private IEnumerator BehaviorUpdateCoroutine()
        {
            var wait = new WaitForSeconds(2f);

            while (true)
            {
                yield return wait;
                
                if (enableAdvancedAI?.Value == true)
                {
                    UpdateAdvancedBehaviors();
                }
            }
        }

        /// <summary>
        /// Coroutine for updating dynamic difficulty
        /// </summary>
        private IEnumerator DifficultyUpdateCoroutine()
        {
            var wait = new WaitForSeconds(30f);

            while (true)
            {
                yield return wait;
                UpdateDynamicDifficulty();
            }
        }

        /// <summary>
        /// Coroutine for processing user notifications
        /// </summary>
        private IEnumerator NotificationProcessorCoroutine()
        {
            var wait = new WaitForSeconds(1f);

            while (true)
            {
                yield return wait;
                ProcessNotifications();
            }
        }

        private void ProcessNotifications()
        {
            // Process queued notifications
            while (notificationQueue.Count > 0)
            {
                var notification = notificationQueue.Dequeue();
                // Handle notification display/logging
                logger?.LogInfo($"Notification for {notification.Username}: {notification.Message}");
            }
        }
        #endregion

        #region Missing Methods Implementation
        /// <summary>
        /// Check if user is on cooldown
        /// </summary>
        private bool IsUserOnCooldown(string username)
        {
            if (userSpawnCooldowns.TryGetValue(username, out DateTime cooldownEnd))
            {
                return DateTime.Now < cooldownEnd;
            }
            return false;
        }

        /// <summary>
        /// Check if spawning is possible
        /// </summary>
        private bool CanSpawn(bool isFriendly)
        {
            if (isFriendly)
            {
                return ActiveAllies.Count < maxAllySosigs.Value;
            }
            else
            {
                return ActiveEnemies.Count < maxEnemySosigs.Value;
            }
        }

        /// <summary>
        /// Create ChatSosig wrapper with Twitch integration
        /// </summary>
        private ChatSosig CreateChatSosig(Sosig sosig, string userName, string displayName, bool isFriendly)
        {
            var chatSosig = new ChatSosig
            {
                Sosig = sosig,
                UserName = userName,
                DisplayName = displayName,
                TwitchUsername = userName,
                IsFriendly = isFriendly,
                ArmorPreset = isFriendly ? defaultAllyArmor.Value : defaultEnemyArmor.Value,
                SpawnTime = DateTime.Now,
                Lifetime = sosigLifetime?.Value ?? 300f,
                IsFromTwitch = enableTwitchIntegration.Value,
                CustomData = new Dictionary<string, object>()
            };

            return chatSosig;
        }

        /// <summary>
        /// Destroy ChatSosig and cleanup
        /// </summary>
        private void DestroyChatSosig(ChatSosig chatSosig)
        {
            if (chatSosig?.Sosig != null)
            {
                // Cleanup nameplate
                if (chatSosig.Nameplate != null)
                {
                    Destroy(chatSosig.Nameplate);
                }

                // Cleanup audio source
                if (chatSosig.AudioSource != null)
                {
                    Destroy(chatSosig.AudioSource);
                    sosigAudioSources.Remove(chatSosig);
                }

                // Remove from tracking
                sosigLookup.Remove(chatSosig.Sosig);
                sosigBehaviors.Remove(chatSosig);
                sosigNextBehaviorUpdate.Remove(chatSosig);
                sosigWaypoints.Remove(chatSosig);
                UntrackSosigByUser(chatSosig);

                // Award experience for sosig death
                if (chatSosig.IsDead)
                {
                    AwardExperience(chatSosig.TwitchUsername, 5f, "sosig defeated");
                }

                // Play death sound
                PlayAudioFeedback(deathSound, chatSosig.Sosig.transform.position);

                // Destroy sosig
                Destroy(chatSosig.Sosig.gameObject);

                // Trigger event
                OnSosigDestroyed?.Invoke(chatSosig.Sosig, chatSosig.DisplayName);
            }

            // Remove from lists
            ActiveAllies.Remove(chatSosig);
            ActiveEnemies.Remove(chatSosig);
            spawnedChatters.Remove(chatSosig.Sosig);
            spawnedEnemyChatters.Remove(chatSosig.Sosig);

            // Update count
            OnSosigCountChanged?.Invoke(ActiveAllies.Count, ActiveEnemies.Count);
        }

        /// <summary>
        /// Get random ally name from INI file
        /// </summary>
        private string GetRandomAllyName()
        {
            if (allyNames.Count > 0)
            {
                return allyNames[UnityEngine.Random.Range(0, allyNames.Count)];
            }
            return null;
        }

        /// <summary>
        /// Get random enemy name from INI file
        /// </summary>
        private string GetRandomEnemyName()
        {
            if (enemyNames.Count > 0)
            {
                return enemyNames[UnityEngine.Random.Range(0, enemyNames.Count)];
            }
            return null;
        }

        /// <summary>
        /// Load names from INI files
        /// </summary>
        private void LoadNamesFromFiles()
        {
            try
            {
                LoadAllyNames();
                LoadEnemyNames();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to load names from files: {ex.Message}");
            }
        }

        private void LoadAllyNames()
        {
            try
            {
                if (File.Exists(allyNamesPath))
                {
                    var lines = File.ReadAllLines(allyNamesPath);
                    allyNames.Clear();
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                        {
                            allyNames.Add(trimmed);
                        }
                    }
                    logger?.LogDebug($"Loaded {allyNames.Count} ally names");
                }
                else
                {
                    CreateDefaultAllyNamesFile();
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to load ally names: {ex.Message}");
            }
        }

        private void LoadEnemyNames()
        {
            try
            {
                if (File.Exists(enemyNamesPath))
                {
                    var lines = File.ReadAllLines(enemyNamesPath);
                    enemyNames.Clear();
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                        {
                            enemyNames.Add(trimmed);
                        }
                    }
                    logger?.LogDebug($"Loaded {enemyNames.Count} enemy names");
                }
                else
                {
                    CreateDefaultEnemyNamesFile();
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to load enemy names: {ex.Message}");
            }
        }

        private void CreateDefaultAllyNamesFile()
        {
            try
            {
                var defaultNames = new[]
                {
                    "# Ally Names for Chat Sosigs",
                    "# Add one name per line",
                    "Alpha",
                    "Bravo", 
                    "Charlie",
                    "Delta",
                    "Echo",
                    "Foxtrot",
                    "Guardian",
                    "Protector",
                    "Defender",
                    "Support"
                };
                
                File.WriteAllLines(allyNamesPath, defaultNames);
                LoadAllyNames();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create default ally names file: {ex.Message}");
            }
        }

        private void CreateDefaultEnemyNamesFile()
        {
            try
            {
                var defaultNames = new[]
                {
                    "# Enemy Names for Chat Sosigs",
                    "# Add one name per line",
                    "Hostile",
                    "Raider",
                    "Bandit",
                    "Marauder",
                    "Enforcer",
                    "Threat",
                    "Aggressor",
                    "Adversary",
                    "Opponent",
                    "Nemesis"
                };
                
                File.WriteAllLines(enemyNamesPath, defaultNames);
                LoadEnemyNames();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create default enemy names file: {ex.Message}");
            }
        }

        /// <summary>
        /// Get remaining cooldown time for user
        /// </summary>
        private float GetRemainingCooldown(string username)
        {
            if (userSpawnCooldowns.TryGetValue(username, out DateTime cooldownEnd))
            {
                return (float)(cooldownEnd - DateTime.Now).TotalSeconds;
            }
            return 0f;
        }

        /// <summary>
        /// Set user cooldown
        /// </summary>
        private void SetUserCooldown(string username)
        {
            userSpawnCooldowns[username] = DateTime.Now.AddSeconds(spawnCooldown.Value);
        }

        /// <summary>
        /// Get user sosig count
        /// </summary>
        private int GetUserSosigCount(string username)
        {
            return GetUserActiveSosigCount(username);
        }

        /// <summary>
        /// Increment user sosig count
        /// </summary>
        private void IncrementUserSosigCount(string username)
        {
            // This is handled by the tracking system
        }

        /// <summary>
        /// Clean up expired cooldowns
        /// </summary>
        private void CleanupExpiredCooldowns()
        {
            var now = DateTime.Now;
            var expiredKeys = userSpawnCooldowns.Where(kvp => kvp.Value < now).Select(kvp => kvp.Key).ToList();
            
            foreach (var key in expiredKeys)
            {
                userSpawnCooldowns.Remove(key);
            }
        }
        #endregion

        #region Enhanced Effects and Features
        /// <summary>
        /// Create nameplate for sosig with proper positioning and styling
        /// </summary>
        private void CreateNameplateForSosig(Sosig sosig, string displayName, bool isFriendly)
        {
            if (sosig == null || sosig.Links.Count == 0) return;

            try
            {
                // Create nameplate GameObject
                GameObject nameplate = new GameObject($"Nameplate_{displayName}");
                nameplate.transform.SetParent(sosig.Links[0].transform, false); // Attach to head

                // Position above the head
                nameplate.transform.localPosition = Vector3.up * (nameplateHeight?.Value ?? 2.5f);
                nameplate.transform.localRotation = Quaternion.identity;

                // Add Canvas component for UI
                Canvas canvas = nameplate.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;

                // Add CanvasScaler
                CanvasScaler scaler = nameplate.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

                // Create text GameObject
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(nameplate.transform, false);

                // Add Text component
                Text text = textObj.AddComponent<Text>();
                text.text = displayName;
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 36;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = isFriendly ? (allyNameplateColor?.Value ?? Color.green) : (enemyNameplateColor?.Value ?? Color.red);

                // Set up RectTransform for proper sizing
                RectTransform rectTransform = text.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(200, 50);
                rectTransform.localPosition = Vector3.zero;

                // Scale the entire nameplate
                nameplate.transform.localScale = Vector3.one * (nameplateScale?.Value ?? 0.02f);

                // Make nameplate always face the camera
                StartCoroutine(FaceCamera(nameplate));

                // Store reference for cleanup
                if (sosigLookup.TryGetValue(sosig, out var chatSosig))
                {
                    chatSosig.Nameplate = nameplate;
                }

                logger?.LogDebug($"Created nameplate for {displayName}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create nameplate for {displayName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Make nameplate always face the camera
        /// </summary>
        private IEnumerator FaceCamera(GameObject nameplate)
        {
            while (nameplate != null)
            {
                if (Camera.main != null)
                {
                    nameplate.transform.LookAt(Camera.main.transform);
                    nameplate.transform.Rotate(0, 180, 0); // Flip to face correctly
                }
                yield return new WaitForSeconds(0.1f);
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
                logger?.LogError($"Failed to create spawn effect for {chatSosig.DisplayName}: {ex.Message}");
            }
        }

        #endregion

        // =========================================================
        // Added: Core spawn queue processing and manual spawn paths
        // =========================================================

        private IEnumerator ProcessSpawnQueueCoroutine()
        {
            var wait = new WaitForSeconds(0.1f);

            while (true)
            {
                bool shouldYield = false;
                Exception caughtEx = null;

                // throttle by cooldown
                if (Time.time - lastSpawnTime < (spawnCooldown != null ? spawnCooldown.Value : 2.0f))
                {
                    shouldYield = true;
                }
                // performance safeguard: if performance mode is on and we are over recommended count, pause spawns
                else if (performanceMode && recommendedSosigCount > 0 && (ActiveAllies.Count + ActiveEnemies.Count) >= recommendedSosigCount)
                {
                    shouldYield = true;
                }
                else
                {
                    try
                    {
                        TwitchSpawnRequest request = null;

                        // priority first
                        if (prioritySpawnQueue.Count > 0)
                            request = prioritySpawnQueue.Dequeue();
                        else if (spawnQueue.Count > 0)
                            request = spawnQueue.Dequeue();

                        if (request == null)
                        {
                            shouldYield = true;
                        }
                        else if (!CanSpawn(request.IsFriendly))
                        {
                            logger?.LogWarning("Spawn skipped: capacity reached");
                            NotifyUser(request.TwitchUsername ?? request.UserName, "Server is at capacity. Your spawn stayed in queue too long.", NotificationType.Warning);
                            shouldYield = true;
                        }
                        else
                        {
                            // Determine name and placement
                            string displayName = request.DisplayName;
                            if (string.IsNullOrEmpty(displayName))
                            {
                                if (!request.IsFromTwitch || (useTwitchNamesOverIni != null && !useTwitchNamesOverIni.Value))
                                    displayName = request.IsFriendly ? (GetRandomAllyName() ?? SpawnerName) : (GetRandomEnemyName() ?? SpawnerName);
                                else
                                    displayName = request.UserName ?? SpawnerName;
                            }

                            Vector3 spawnPos = request.CustomSpawnPoint ?? request.PreferredSpawnLocation ?? CalculateSpawnPoint(request.IsFriendly);
                            Quaternion rot = Quaternion.identity;

                            // Choose template
                            SosigEnemyTemplate template = GetTemplate(request.IsFriendly);
                            if (template == null)
                            {
                                logger?.LogError("No Sosig template available; cannot spawn.");
                                NotifyUser(request.TwitchUsername ?? request.UserName, "No Sosig template available. Spawn failed.", NotificationType.Error);
                                shouldYield = true;
                            }
                            else
                            {
                                // Spawn Sosig
                                int iff = request.IsFriendly ? 0 : Mathf.Max(1, (int)(enemyIFF != null ? enemyIFF.Value : 1f));
                                var sosig = SpawnSosigFromTemplate(template, spawnPos, rot, iff, displayName);
                                if (sosig == null)
                                {
                                    NotifyUser(request.TwitchUsername ?? request.UserName, "Spawn failed due to internal error.", NotificationType.Error);
                                    shouldYield = true;
                                }
                                else
                                {
                                    // Wrap ChatSosig
                                    var chatSosig = CreateChatSosig(sosig, request.UserName ?? request.TwitchUsername ?? "UnknownUser", displayName, request.IsFriendly);
                                    chatSosig.IsFromTwitch = request.IsFromTwitch;
                                    if (request.CustomData != null)
                                    {
                                        foreach (var kv in request.CustomData)
                                            chatSosig.CustomData[kv.Key] = kv.Value;
                                    }

                                    // Register and finalize
                                    RegisterSpawn(chatSosig);

                                    // Behavior request (if eligible)
                                    if (enableBehaviorCommands != null && enableBehaviorCommands.Value && !string.IsNullOrEmpty(request.RequestedBehavior))
                                    {
                                        try
                                        {
                                            var parsed = (SosigBehaviorState)Enum.Parse(typeof(SosigBehaviorState), request.RequestedBehavior, true);
                                            SetSosigBehavior(chatSosig, parsed);
                                        }
                                        catch
                                        {
                                            // ignore invalid behavior strings
                                        }
                                    }

                                    // Effects/audio
                                    if (enableSpawnEffects != null && enableSpawnEffects.Value)
                                        CreateSpawnEffects(chatSosig);

                                    PlayAudioFeedback(spawnSound, chatSosig.Sosig.transform.position);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        caughtEx = ex;
                    }
                }

                if (caughtEx != null)
                {
                    logger?.LogError("Error in ProcessSpawnQueueCoroutine: " + caughtEx.Message);
                }

                if (shouldYield)
 {
                    yield return wait;
                }
            }
        }

        private void RegisterSpawn(ChatSosig chatSosig)
        {
            if (chatSosig == null || !chatSosig.IsValid) return;

            // Track lists
            if (chatSosig.IsFriendly)
            {
                ActiveAllies.Add(chatSosig);
                spawnedChatters.Add(chatSosig.Sosig);
            }
            else
            {
                ActiveEnemies.Add(chatSosig);
                spawnedEnemyChatters.Add(chatSosig.Sosig);
            }

            // Track lookup
            sosigLookup[chatSosig.Sosig] = chatSosig;

            // Track per-user
            TrackSosigByUser(chatSosig);

            // Armor
            ApplyArmorToSosig(chatSosig.Sosig, chatSosig.IsFriendly);

            // Default behavior
            if (chatSosig.IsFriendly)
                SetupAllyBehavior(chatSosig.Sosig);
            else
                SetupEnemyBehavior(chatSosig.Sosig);

            // Nameplate (optional)
            if (enableNameplates != null && enableNameplates.Value)
                CreateNameplateForSosig(chatSosig.Sosig, chatSosig.DisplayName, chatSosig.IsFriendly);

            // AudioSource per sosig (for future voice lines)
            if (!sosigAudioSources.ContainsKey(chatSosig))
            {
                var src = chatSosig.Sosig.gameObject.AddComponent<AudioSource>();
                chatSosig.AudioSource = src;
                sosigAudioSources[chatSosig] = src;
            }

            totalSpawnCount++;
            OnSosigSpawned?.Invoke(chatSosig.Sosig, chatSosig.DisplayName, chatSosig.IsFriendly);
            OnSosigCountChanged?.Invoke(ActiveAllies.Count, ActiveEnemies.Count);

            logger?.LogInfo(string.Format("Spawned {0} Sosig: {1}", chatSosig.IsFriendly ? "Ally" : "Enemy", chatSosig.DisplayName));
        }

        private SosigEnemyTemplate GetTemplate(bool isFriendly)
        {
            try
            {
                var list = isFriendly ? allyTemplates : enemyTemplates;
                if (list != null && list.Count > 0)
                    return list[UnityEngine.Random.Range(0, list.Count)];

                if (defaultAllyTemplate != null && isFriendly)
                    return defaultAllyTemplate;

                if (cachedSosigTemplates != null && cachedSosigTemplates.Length > 0)
                    return cachedSosigTemplates[0];
            }
            catch { }
            return null;
        }

        // Manual spawn via keys (friendly) - made public for SpawnManager
        public void SpawningSequence(string username)
        {
            SpawnImmediate(true, username ?? "ManualAlly");
        }

        // Manual spawn via keys (enemy) - made public for SpawnManager
        public void SpawningSequenceEnemy(int IFF, string username)
        {
            // IFF param is accepted for backwards compatibility; actual IFF is taken from config enemyIFF
            SpawnImmediate(false, username ?? "ManualEnemy");
        }

        private void SpawnImmediate(bool isFriendly, string username, string displayName = null, int? customIFF = null, Vector3? customPos = null)
        {
            try
            {
                if (!CanSpawn(isFriendly))
                {
                    logger?.LogWarning("Immediate spawn denied: capacity reached");
                    return;
                }

                var template = GetTemplate(isFriendly);
                if (template == null)
                {
                    logger?.LogError("No Sosig template available for immediate spawn");
                    return;
                }

                Vector3 pos = customPos ?? CalculateSpawnPoint(isFriendly);
                Quaternion rot = Quaternion.identity;

                string finalName = displayName;
                if (string.IsNullOrEmpty(finalName))
                    finalName = isFriendly ? (GetRandomAllyName() ?? SpawnerName) : (GetRandomEnemyName() ?? SpawnerName);

                int iff = customIFF ?? (isFriendly ? 0 : Mathf.Max(1, (int)(enemyIFF != null ? enemyIFF.Value : 1f)));
                var sosig = SpawnSosigFromTemplate(template, pos, rot, iff, finalName);
                if (sosig == null) return;

                var chat = CreateChatSosig(sosig, username, finalName, isFriendly);
                RegisterSpawn(chat);

                if (enableSpawnEffects != null && enableSpawnEffects.Value)
                    CreateSpawnEffects(chat);

                PlayAudioFeedback(spawnSound, pos);
            }
            catch (Exception ex)
            {
                logger?.LogError("SpawnImmediate error: " + ex.Message);
            }
        }

        // ===============================================
        // Added: Twitch, audio, performance/difficulty
        // ===============================================

        private void InitializeTwitchIntegration()
        {
            try
            {
                if (enableTwitchIntegration != null && enableTwitchIntegration.Value)
                {
                    twitchManager = TwitchChatManager.Instance ?? FindObjectOfType<TwitchChatManager>();
                    if (twitchManager == null)
                    {
                        logger?.LogWarning("TwitchChatManager not found at this time. It may initialize later.");
                    }
                    else
                    {
                        // Not calling Initialize here to avoid duplicating plugin wiring;
                        // assume H3TVRImproved.InitializeTwitchIntegration handles it.
                        logger?.LogInfo("Twitch integration linked to EnhancedChatSpawner.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError("InitializeTwitchIntegration failed: " + ex.Message);
            }
        }

        private void InitializeAudioSystem()
        {
            try
            {
                if (audioManager == null)
                    audioManager = (plugin != null ? plugin.GetComponent<AudioManager>() : null) ?? FindObjectOfType<AudioManager>();

                if (globalAudioSource == null)
                {
                    globalAudioSource = gameObject.GetComponent<AudioSource>();
                    if (globalAudioSource == null)
                        globalAudioSource = gameObject.AddComponent<AudioSource>();
                    globalAudioSource.spatialBlend = 0f;
                    globalAudioSource.playOnAwake = false;
                }

                logger?.LogInfo("Audio system initialized for Enhanced Chat Spawner");
            }
            catch (Exception ex)
            {
                logger?.LogError("InitializeAudioSystem failed: " + ex.Message);
            }
        }

        private void PlayAudioFeedback(AudioClip clip, Vector3 position)
        {
            try
            {
                if (clip == null) return;

                if (audioManager != null)
                {
                    // Use AudioManager 3D path
                    var temp = new GameObject("ChatSpawner_SFX");
                    temp.transform.position = position;
                    var src = temp.AddComponent<AudioSource>();
                    src.clip = clip;
                    src.spatialBlend = 1f;
                    src.volume = 0.8f;
                    src.Play();
                    Destroy(temp, clip.length + 0.1f);
                }
                else
                {
                    // Fallback
                    AudioSource.PlayClipAtPoint(clip, position, 0.8f);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning("PlayAudioFeedback(3D) failed: " + ex.Message);
            }
        }

        private void PlayAudioFeedback(AudioClip clip)
        {
            try
            {
                if (clip == null || globalAudioSource == null) return;
                globalAudioSource.clip = clip;
                globalAudioSource.volume = 0.8f;
                globalAudioSource.spatialBlend = 0f;
                globalAudioSource.Play();
            }
            catch (Exception ex)
            {
                logger?.LogWarning("PlayAudioFeedback(2D) failed: " + ex.Message);
            }
        }

        private void MonitorPerformanceEnhanced()
        {
            try
            {
                // Collect frame samples
                recentFrameTimes.Add(Time.deltaTime);
                if (recentFrameTimes.Count > 120) // keep ~2 seconds at 60fps
                    recentFrameTimes.RemoveAt(0);

                if (recentFrameTimes.Count == 0) return;

                float sum = 0f;
                for (int i = 0; i < recentFrameTimes.Count; i++)
                    sum += recentFrameTimes[i];

                averageFrameTime = sum / recentFrameTimes.Count;
                currentFrameRate = (averageFrameTime > 0.0001f) ? (1f / averageFrameTime) : 999f;

                float threshold = performanceThreshold != null ? performanceThreshold.Value : 0.033f; // ~30fps default
                performanceMode = (enablePerformanceScaling != null && enablePerformanceScaling.Value && averageFrameTime > threshold);

                int active = ActiveAllies.Count + ActiveEnemies.Count;
                recentSosigCounts.Add(active);
                if (recentSosigCounts.Count > 60) recentSosigCounts.RemoveAt(0);

                // Simple recommendation: if in perf mode reduce target, else allow more
                if (performanceMode)
                    recommendedSosigCount = Math.Max(2, active - 1);
                else
                    recommendedSosigCount = Math.Max(active, active + 2);
            }
            catch (Exception ex)
            {
                logger?.LogWarning("MonitorPerformanceEnhanced failed: " + ex.Message);
            }
        }

        private void UpdateAdvancedBehaviors()
        {
            try
            {
                var now = Time.time;
                var keys = new List<ChatSosig>(sosigBehaviors.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    var cs = keys[i];
                    if (cs == null || !cs.IsValid) continue;

                    float nextAt;
                    if (!sosigNextBehaviorUpdate.TryGetValue(cs, out nextAt))
                        nextAt = 0f;

                    if (now >= nextAt)
                    {
                        ApplyBehaviorToSosig(cs, cs.CurrentBehavior);
                        sosigNextBehaviorUpdate[cs] = now + UnityEngine.Random.Range(1.5f, 3.5f);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning("UpdateAdvancedBehaviors failed: " + ex.Message);
            }
        }

        private void ApplyBehaviorToSosig(ChatSosig chatSosig, SosigBehaviorState behavior)
        {
            if (chatSosig == null || !chatSosig.IsValid) return;

            try
            {
                var sosig = chatSosig.Sosig;

                switch (behavior)
                {
                    case SosigBehaviorState.Idle:
                        sosig.CurrentOrder = Sosig.SosigOrder.Idle;
                        break;

                    case SosigBehaviorState.Following:
                        if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.Head != null)
                        {
                            var playerPos = GM.CurrentPlayerBody.Head.position;
                            sosig.CommandAssaultPoint(playerPos + UnityEngine.Random.insideUnitSphere * (allyFollowDistance != null ? allyFollowDistance.Value : 3f));
                        }
                        break;

                    case SosigBehaviorState.Guarding:
                        // Sosig doesn't have CommandGuardPosition, use CommandAssaultPoint instead
                        sosig.CommandAssaultPoint(sosig.transform.position);
                        break;

                    case SosigBehaviorState.Patrolling:
                        {
                            Vector3 wp;
                            if (!sosigWaypoints.TryGetValue(chatSosig, out wp))
                            {
                                wp = sosig.transform.position + UnityEngine.Random.insideUnitSphere * 6f;
                                wp.y = sosig.transform.position.y;
                                sosigWaypoints[chatSosig] = wp;
                            }
                            sosig.CommandAssaultPoint(wp);
                        }
                        break;

                    case SosigBehaviorState.Attacking:
                        if (GM.CurrentPlayerBody != null)
                        {
                            var target = GM.CurrentPlayerBody.transform.position;
                            sosig.CommandAssaultPoint(target);
                        }
                        break;

                    case SosigBehaviorState.Searching:
                        {
                            var origin = chatSosig.LastKnownPosition != Vector3.zero ? chatSosig.LastKnownPosition : sosig.transform.position;
                            var search = origin + UnityEngine.Random.insideUnitSphere * 8f;
                            search.y = origin.y;
                            sosig.CommandAssaultPoint(search);
                        }
                        break;

                    case SosigBehaviorState.Retreating:
                        if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.Head != null)
                        {
                            var from = GM.CurrentPlayerBody.Head.position;
                            var dir = (sosig.transform.position - from).normalized;
                            var back = sosig.transform.position + dir * 10f;
                            sosig.CommandAssaultPoint(back);
                        }
                        break;

                    case SosigBehaviorState.Supporting:
                        {
                            // Move near allies (simple: toward player with offset)
                            if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.Head != null)
                            {
                                var p = GM.CurrentPlayerBody.Head.position + UnityEngine.Random.insideUnitSphere * 4f;
                                p.y = GM.CurrentPlayerBody.Head.position.y;
                                sosig.CommandAssaultPoint(p);
                            }
                        }
                        break;

                    case SosigBehaviorState.Custom:
                        // Intentionally left flexible
                        break;
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning("ApplyBehaviorToSosig failed: " + ex.Message);
            }
        }

        private void UpdateDynamicDifficulty()
        {
            try
            {
                if (enableDynamicDifficulty != null && !enableDynamicDifficulty.Value)
                    return;

                // Basic heuristic using frame performance and active enemies
                float desired = 1.0f;

                if (currentFrameRate < 40f) desired -= 0.1f;
                if (currentFrameRate < 30f) desired -= 0.2f;
                if (ActiveEnemies.Count > (maxEnemySosigs != null ? maxEnemySosigs.Value : 8) * 0.75f) desired += 0.1f;
                if (ActiveAllies.Count > (maxAllySosigs != null ? maxAllySosigs.Value : 8) * 0.75f) desired -= 0.05f;

                desired = Mathf.Clamp(desired, 0.5f, 1.5f);

                // Smooth adjustment every call
                currentDifficulty = Mathf.Lerp(currentDifficulty, desired, 0.25f);
                lastDifficultyAdjustment = DateTime.Now;

                if (OnDifficultyChanged != null)
                    OnDifficultyChanged(currentDifficulty);
            }
            catch (Exception ex)
            {
                logger?.LogWarning("UpdateDynamicDifficulty failed: " + ex.Message);
            }
        }

        // ===============================================
        // Added: User tracking helpers and queries
        // ===============================================

        private void TrackSosigByUser(ChatSosig chatSosig)
        {
            if (chatSosig == null) return;
            var key = chatSosig.TwitchUsername ?? chatSosig.UserName ?? "UnknownUser";

            List<ChatSosig> list;
            if (!userSosigMap.TryGetValue(key, out list))
            {
                list = new List<ChatSosig>();
                userSosigMap[key] = list;
            }

            if (!list.Contains(chatSosig))
                list.Add(chatSosig);
        }

        private void UntrackSosigByUser(ChatSosig chatSosig)
        {
            if (chatSosig == null) return;
            var key = chatSosig.TwitchUsername ?? chatSosig.UserName ?? "UnknownUser";

            List<ChatSosig> list;
            if (userSosigMap.TryGetValue(key, out list))
            {
                list.Remove(chatSosig);
                if (list.Count == 0)
                    userSosigMap.Remove(key);
            }
        }

        private List<ChatSosig> GetSosigsByTwitchUser(string username)
        {
            if (string.IsNullOrEmpty(username)) return new List<ChatSosig>();
            List<ChatSosig> list;
            if (userSosigMap.TryGetValue(username, out list))
                return list.Where(s => s != null && s.IsValid && !s.IsDead).ToList();
            return new List<ChatSosig>();
        }

        private int GetTwitchSosigCount()
        {
            int count = 0;
            foreach (var kv in userSosigMap)
            {
                count += kv.Value.Count(s => s != null && s.IsValid && !s.IsDead);
            }
            return count;
        }

        /// <summary>
        /// Get the number of active sosigs for a given user (Twitch username)
        /// </summary>
        private int GetUserActiveSosigCount(string username)
        {
            if (string.IsNullOrEmpty(username)) return 0;
            if (userSosigMap.TryGetValue(username, out var list))
                return list.Count(s => s != null && s.IsValid && !s.IsDead);
            return 0;
        }
    }
}

