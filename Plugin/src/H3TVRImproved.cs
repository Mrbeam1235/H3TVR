using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FistVR;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using System;

namespace H3TVR
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    [BepInProcess("h3vr.exe")]
    public class H3TVRImproved : BaseUnityPlugin
    {
        #region Constants and Static Fields
        private const float SlowdownFactor = .001f;
        private const float SlowdownLength = 6f;
        private const float ZeroGWaitTime = 6f;
        private const float RealisticFallTime = 1f;
        private const float MalfunctionBoostDuration = 120f;
        private const float ForcedMalfunctionChance = 0.75f;
        #endregion

        #region State Management
        private string slomoStatus = "Off";
        private string zeroGStatus = "Off";
        private bool malfunctionBoostActive;
        private float malfunctionBoostEndTime;
        #endregion

        #region Configuration - Organized by Feature
        
        // Slomo Configuration
        private ConfigEntry<float> maxSlomo;
        private ConfigEntry<float> slomoWaitTime;
        private ConfigEntry<float> slomoScaleSpeed;
        private ConfigEntry<float> slomoReturnSpeed;
        private ConfigEntry<bool> slomoVRControllerEnabled;
        private ConfigEntry<string> slomoVRButton;
        private ConfigEntry<bool> slomoAffectsMovement;
        private ConfigEntry<float> slomoMovementScale;
        
        // Audio Configuration
        private ConfigEntry<bool> slomoAffectsAudio;
        private ConfigEntry<float> slomoAudioPitchScale;
        private ConfigEntry<bool> slomoAudioPreservePitch;
        private ConfigEntry<bool> slomoAffectsAudioSpeed;
        private ConfigEntry<float> slomoAudioSpeedScale;
        private ConfigEntry<string> slomoAudioMode; // "PitchOnly", "SpeedOnly", "Both", "Independent"
        
        // Gun Randomization Configuration
        private ConfigEntry<bool> useItemManagerForGunRandomization;
        private ConfigEntry<string> gunList;
        private ConfigEntry<string> magazineList;
        
        // Spawn Configuration - Shuriken
        private ConfigEntry<float> shurikenScale;
        private ConfigEntry<int> shurikenMinCount;
        private ConfigEntry<int> shurikenMaxCount;
        
        // Spawn Configuration - Pillow
        private ConfigEntry<int> pillowMinCount;
        private ConfigEntry<int> pillowMaxCount;
        private ConfigEntry<bool> pillowGrenadeEnabled;
        private ConfigEntry<float> pillowGrenadeChance;
        private ConfigEntry<float> pillowGrenadeArmedChance;
        private ConfigEntry<bool> pillowZeroGravityEnabled;
        private ConfigEntry<float> pillowZeroGravityChance;
        private ConfigEntry<float> pillowZeroGravityDuration;
        private ConfigEntry<bool> pillowSlomoEnabled;
        private ConfigEntry<float> pillowSlomoChance;
        private ConfigEntry<float> pillowSlomoDuration;
        
        // Danger Close Configuration
        private ConfigEntry<int> dangerCloseMinCount;
        private ConfigEntry<int> dangerCloseMaxCount;
        
        // Key Bindings - Organized
        private readonly Dictionary<string, ConfigEntry<KeyCode>> keyBindings = new Dictionary<string, ConfigEntry<KeyCode>>();
        
        // Chat Sosig Configuration
        private ConfigEntry<bool> enableTwitchChatSosigs;
        private ConfigEntry<bool> enableLegacyFileMode; // For backwards compatibility
        private ConfigEntry<string> twitchChatFilePath; // Legacy
        private ConfigEntry<string> twitchEnemyChatFilePath; // Legacy
        private ConfigEntry<int> maxChatSosigs;
        
        // Steam Friends Configuration
        private ConfigEntry<bool> enableSteamFriends;
        private ConfigEntry<bool> steamFriendsRandomNames;
        private ConfigEntry<float> steamFriendsRefreshInterval;
        #endregion

        #region Components
        private SlomoMovementController slomoMovementController;
        private readonly Hooks hooks = new Hooks();
        private InputHandler inputHandler;
        private SpawnManager spawnManager;
        private EffectsManager effectsManager;
        private WeaponManager weaponManager;
        private AudioManager audioManager;
        private AdvancedChatSosigSpawner advancedChatSpawner; // Advanced Chat Sosig Spawner with Update 120 TNH
        private SosigArmorWristMenuIntegration sosigArmorWristMenu;
        private SteamFriendsIntegration steamFriendsIntegration; // Steam Friends Integration
        #endregion

        #region Initialization
        public H3TVRImproved()
        {
            hooks.Hook();
            Logger.LogInfo("Loading H3TVR Enhanced Edition (Standalone Mode)");
        }

        private void Awake()
        {
            try
            {
                // Initialize optional dependency manager early
                OptionalDependencyManager.Initialize(base.Logger);
                
                base.Logger.LogInfo("H3TVR Enhanced Edition (Standalone Mode) is loading...");
                
                // Initialize configuration
                base.Logger.LogInfo("Step 1: Initializing configuration...");
                InitializeConfiguration();
                
                // Initialize optional dependencies
                base.Logger.LogInfo("Step 2: Initializing optional dependencies...");
                InitializeOptionalDependencies();
                
                // Initialize components
                base.Logger.LogInfo("Step 3: Initializing components...");
                InitializeComponents();
                
                // Initialize chat spawner first - it's the core component
                base.Logger.LogInfo("Step 4: Initializing Sosig Spawner...");
                InitializeSosigSpawner();
                
                // Now initialize SpawnManager with the chat spawner reference
                base.Logger.LogInfo("Step 5: Initializing SpawnManager...");
                if (spawnManager != null && advancedChatSpawner != null)
                {
                    spawnManager.Initialize(this, Logger, advancedChatSpawner, audioManager);
                    base.Logger.LogInfo("SpawnManager initialized successfully");
                }
                else
                {
                    Logger.LogWarning($"Cannot initialize SpawnManager - spawnManager: {spawnManager != null}, advancedChatSpawner: {advancedChatSpawner != null}");
                }
                
                // Initialize TwitchLib integration (if enabled)
                base.Logger.LogInfo("Step 6: Initializing Twitch integration...");
                InitializeTwitchIntegration();
                
                // Initialize Steam Friends integration (if enabled)
                base.Logger.LogInfo("Step 6.5: Initializing Steam Friends integration...");
                InitializeSteamFriendsIntegration();
                
                // Initialize wrist menu integration with error handling
                base.Logger.LogInfo("Step 7: Initializing wrist menu...");
                try
                {
                    InitializeSosigArmorWristMenuIntegration();
                }
                catch (Exception ex)
                {
                    base.Logger.LogWarning($"Non-critical error in wrist menu integration: {ex.Message}");
                }
                
                base.Logger.LogInfo("H3TVR Enhanced Edition loaded successfully!");
                
                // Log dependency status
                base.Logger.LogInfo(OptionalDependencyManager.GetDependencyStatusReport());
                
                // Log Meatyceiver 2 specific status
                if (MeatyceiverIntegrationManager.IsIntegrationEnabled())
                {
                    base.Logger.LogInfo("Meatyceiver 2 Integration: ACTIVE");
                    base.Logger.LogInfo(MeatyceiverIntegrationManager.GetTransformationStats());
                }

                // Log TwitchLib status
                if (enableTwitchChatSosigs != null && enableTwitchChatSosigs.Value)
                {
                    base.Logger.LogInfo("Chat Sosig System: ENABLED");
                    base.Logger.LogInfo("  - Standalone mode (no Twitch integration)");
                    base.Logger.LogInfo("  - Use keyboard: P (ally), O (enemy), Delete (clear)");
                }
                else
                {
                    base.Logger.LogInfo("Chat Sosig System: DISABLED");
                }
            }
            catch (Exception ex)
            {
                base.Logger.LogError($"Error during H3TVR initialization: {ex.Message}");
                base.Logger.LogError($"Stack trace: {ex.StackTrace}");
                
                // Try to continue with basic functionality
                try
                {
                    base.Logger.LogInfo("Attempting fallback initialization...");
                    InitializeConfiguration();
                    Logger.LogInfo("H3TVR running in fallback mode with basic functionality");
                }
                catch (Exception fallbackEx)
                {
                    Logger.LogError($"Critical error - H3TVR cannot initialize: {fallbackEx.Message}");
                }
            }
        }

        private void InitializeConfiguration()
        {
            // Slomo configuration
            maxSlomo = Config.Bind("Slomo", "MaxSlowmoScale", 0.1f, "Maximum slomo scale (0.01 = 1% speed, 0.1 = 10% speed)");
            slomoWaitTime = Config.Bind("Slomo", "WaitTime", 2f, "Time to wait at max slomo before returning to normal speed");
            slomoScaleSpeed = Config.Bind("Slomo", "ScaleDownSpeed", 1f, "Speed at which time slows down (higher = faster transition)");
            slomoReturnSpeed = Config.Bind("Slomo", "ReturnSpeed", 0.33f, "Speed at which time returns to normal (higher = faster return)");
            slomoVRControllerEnabled = Config.Bind("Slomo", "VRControllerEnabled", true, "Enable VR controller button to trigger slomo");
            slomoVRButton = Config.Bind("Slomo", "VRButton", "LeftX", "VR button to trigger slomo");
            slomoAffectsMovement = Config.Bind("Slomo", "AffectsMovement", true, "Whether slomo affects player movement speed");
            slomoMovementScale = Config.Bind("Slomo", "MovementScale", 0.3f, "Movement speed multiplier during slomo");
            
            // Audio configuration
            slomoAffectsAudio = Config.Bind("Audio", "SlomoAffectsAudio", true, "Whether slomo affects audio pitch");
            slomoAudioPitchScale = Config.Bind("Audio", "SlomoAudioPitchScale", 1f, "Audio pitch multiplier during slomo (1.0 = normal pitch, 0.5 = half pitch)");
            slomoAudioPreservePitch = Config.Bind("Audio", "SlomoPreservePitch", false, "If true, audio pitch is preserved (no pitch change). If false, uses pitch scaling.");
            slomoAffectsAudioSpeed = Config.Bind("Audio", "SlomoAffectsAudioSpeed", false, "Whether slomo affects audio speed (time stretching)");
            slomoAudioSpeedScale = Config.Bind("Audio", "SlomoAudioSpeedScale", 1f, "Audio speed multiplier during slomo (1.0 = normal speed, 0.5 = half speed)");
            slomoAudioMode = Config.Bind("Audio", "SlomoAudioMode", "Both", "Audio adjustment mode during slomo: 'PitchOnly', 'SpeedOnly', 'Both', 'Independent'");
            
            // Gun randomization
            useItemManagerForGunRandomization = Config.Bind("GunRandomization", "UseItemManager", true, 
                "Use ItemManager for gun randomization (includes all H3VR and modded guns). If false, uses GunList/MagazineList config files.");
            gunList = Config.Bind("General", "GunList", "DefaultGunList", "List of guns");
            magazineList = Config.Bind("General", "MagazineList", "DefaultMagazineList", "List of magazines");
            
            // Spawn configurations
            InitializeSpawnConfigurations();
            
            // Key bindings
            InitializeKeyBindings();
        }

        private void InitializeSpawnConfigurations()
        {
            // Shuriken Configuration
            shurikenScale = Config.Bind("Shuriken", "Scale", 1.0f, "Scale multiplier for spawned shuriken");
            shurikenMinCount = Config.Bind("Shuriken", "MinCount", 1, "Minimum number of shuriken to spawn");
            shurikenMaxCount = Config.Bind("Shuriken", "MaxCount", 3, "Maximum number of shuriken to spawn");

            // Pillow Configuration
            pillowMinCount = Config.Bind("Pillow", "MinCount", 1, "Minimum number of pillows to spawn");
            pillowMaxCount = Config.Bind("Pillow", "MaxCount", 3, "Maximum number of pillows to spawn");

            // Pillow Effects
            pillowGrenadeEnabled = Config.Bind("Pillow", "GrenadeEnabled", true, "Enable pillow grenade effect");
            pillowGrenadeChance = Config.Bind("Pillow", "GrenadeChance", 0.1f, "Chance for pillow to spawn grenade");
            pillowGrenadeArmedChance = Config.Bind("Pillow", "GrenadeArmedChance", 0.3f, "Chance for pillow grenade to be armed");

            pillowZeroGravityEnabled = Config.Bind("Pillow", "ZeroGEnabled", true, "Enable pillow zero gravity effect");
            pillowZeroGravityChance = Config.Bind("Pillow", "ZeroGChance", 0.15f, "Chance for pillow to trigger zero gravity");
            pillowZeroGravityDuration = Config.Bind("Pillow", "ZeroGDuration", 10f, "Duration of pillow zero gravity effect");

            pillowSlomoEnabled = Config.Bind("Pillow", "SlomoEnabled", true, "Enable pillow slow motion effect");
            pillowSlomoChance = Config.Bind("Pillow", "SlomoChance", 0.2f, "Chance for pillow to trigger slow motion");
            pillowSlomoDuration = Config.Bind("Pillow", "SlomoDuration", 8f, "Duration of pillow slow motion effect");

            // Danger Close Configuration
            dangerCloseMinCount = Config.Bind("DangerClose", "MinCount", 1, "Minimum danger close rounds");
            dangerCloseMaxCount = Config.Bind("DangerClose", "MaxCount", 5, "Maximum danger close rounds");
            
            // Chat Sosig Configuration
            enableTwitchChatSosigs = Config.Bind("ChatSosigs", "Enabled", true, "Enable Chat Sosig spawning system");
            enableLegacyFileMode = Config.Bind("ChatSosigs", "LegacyFileMode", false, "Enable legacy file-based chat watching (deprecated)");
            twitchChatFilePath = Config.Bind("ChatSosigs", "ChatFilePath", "chat.txt", "Path to Twitch chat file (legacy)");
            twitchEnemyChatFilePath = Config.Bind("ChatSosigs", "EnemyChatFilePath", "enemy_chat.txt", "Path to enemy chat file (legacy)");
            maxChatSosigs = Config.Bind("ChatSosigs", "MaxChatSosigs", 10, "Maximum number of active chat sosigs");

            // Steam Friends Configuration
            enableSteamFriends = Config.Bind("SteamFriends", "Enabled", true, "Enable Steam Friends integration for sosig spawning");
            steamFriendsRandomNames = Config.Bind("SteamFriends", "UseRandomNames", false, "Use random friend from list instead of specific name");
            steamFriendsRefreshInterval = Config.Bind("SteamFriends", "RefreshInterval", 300f, "Auto-refresh Steam friends list interval (seconds)");
        }

        private void InitializeKeyBindings()
        {
            var keyBindingConfigs = new Dictionary<string, KeyValuePair<KeyCode, string>>
            {
                { "SpawnWonderfulToy", new KeyValuePair<KeyCode, string>(KeyCode.Keypad1, "Spawn Wonderful Toy") },
                { "SpawnJeditToy", new KeyValuePair<KeyCode, string>(KeyCode.Keypad2, "Spawn Jedit Toy") },
                { "SpawnHydration", new KeyValuePair<KeyCode, string>(KeyCode.Keypad3, "Spawn Hydration") },
                { "SpawnPillow", new KeyValuePair<KeyCode, string>(KeyCode.Keypad4, "Spawn Pillow") },
                { "SpawnShuri", new KeyValuePair<KeyCode, string>(KeyCode.Keypad5, "Spawn Shuriken") },
                { "SpawnFlash", new KeyValuePair<KeyCode, string>(KeyCode.Keypad6, "Spawn Flash") },
                { "SpawnFlash2", new KeyValuePair<KeyCode, string>(KeyCode.Keypad7, "Spawn Flash2") },
                { "SpawnSkittySubGun", new KeyValuePair<KeyCode, string>(KeyCode.Keypad8, "Spawn Random Gun (Small)") },
                { "SpawnSkittyBigGun", new KeyValuePair<KeyCode, string>(KeyCode.Keypad9, "Spawn Random Gun (Large)") },
                { "SpawnNadeRain", new KeyValuePair<KeyCode, string>(KeyCode.KeypadDivide, "Spawn Grenade Rain") },
                { "DangerCloseBarrage", new KeyValuePair<KeyCode, string>(KeyCode.KeypadMultiply, "Danger Close Barrage") },
                { "DestroyHeld", new KeyValuePair<KeyCode, string>(KeyCode.KeypadMinus, "Destroy Held Item") },
                { "DestroyQuickbelt", new KeyValuePair<KeyCode, string>(KeyCode.KeypadPlus, "Drop Quickbelt Items") },
                { "TriggerSlomo", new KeyValuePair<KeyCode, string>(KeyCode.F, "Trigger Slow Motion") },
                { "TriggerZeroG", new KeyValuePair<KeyCode, string>(KeyCode.G, "Trigger Zero Gravity") },
                { "ToggleFireMode", new KeyValuePair<KeyCode, string>(KeyCode.T, "Toggle Fire Mode") },
                { "BoostMalfunction", new KeyValuePair<KeyCode, string>(KeyCode.Y, "Boost Malfunction") },
                { "ShowStats", new KeyValuePair<KeyCode, string>(KeyCode.Tab, "Show Stats") },
                
                // Chat Sosig Key Bindings
                { "SpawnChatSosigFriendly", new KeyValuePair<KeyCode, string>(KeyCode.P, "Spawn Friendly Chat Sosig") },
                { "SpawnChatSosigEnemy", new KeyValuePair<KeyCode, string>(KeyCode.O, "Spawn Enemy Chat Sosig") },
                { "CycleChatSosigArmor", new KeyValuePair<KeyCode, string>(KeyCode.L, "Cycle Chat Sosig Armor") },
                { "ClearChatSosigs", new KeyValuePair<KeyCode, string>(KeyCode.Delete, "Clear All Chat Sosigs") },
                { "ChatSosigStats", new KeyValuePair<KeyCode, string>(KeyCode.Insert, "Show Chat Sosig Stats") },
                { "ArmorGUI", new KeyValuePair<KeyCode, string>(KeyCode.F6, "Open Armor Configuration GUI") },
                
                // Steam Friends Key Bindings
                { "SpawnSteamFriendAlly", new KeyValuePair<KeyCode, string>(KeyCode.LeftBracket, "Spawn Steam Friend as Ally") },
                { "SpawnSteamFriendEnemy", new KeyValuePair<KeyCode, string>(KeyCode.RightBracket, "Spawn Steam Friend as Enemy") },
                { "SpawnAllSteamFriendsAlly", new KeyValuePair<KeyCode, string>(KeyCode.F7, "Spawn All Steam Friends as Allies") },
                { "SpawnAllSteamFriendsEnemy", new KeyValuePair<KeyCode, string>(KeyCode.F8, "Spawn All Steam Friends as Enemies") },
                { "RefreshSteamFriends", new KeyValuePair<KeyCode, string>(KeyCode.F9, "Refresh Steam Friends List") },
                { "SteamFriendsStats", new KeyValuePair<KeyCode, string>(KeyCode.Home, "Show Steam Friends Stats") },
                
                // New JerryAr mod keybindings
                { "SpawnAirStrike", new KeyValuePair<KeyCode, string>(KeyCode.F10, "Spawn Air Strike Smoke Grenade") },
                { "SpawnTitanMachine", new KeyValuePair<KeyCode, string>(KeyCode.F11, "Spawn Titan Machine (AI Enemy)") }
            };

            foreach (var kvp in keyBindingConfigs)
            {
                keyBindings[kvp.Key] = Config.Bind("KeyBindings", $"KeyBindFor{kvp.Key}", 
                    kvp.Value.Key, kvp.Value.Value);
            }
        }

        /// <summary>
        /// Initialize all optional dependency managers
        /// </summary>
        private void InitializeOptionalDependencies()
        {
            try
            {
                // Initialize OptionalDependencyManager first
                OptionalDependencyManager.Initialize(Logger);

                // Initialize Meatyceiver 2 Integration
                MeatyceiverIntegrationManager.Initialize(Logger, Config);

                // Initialize Stovepipe Integration
                StovepipeIntegrationManager.Initialize(Logger, Config);

                // Log final status
                if (OptionalDependencyManager.HasAnyDependencies())
                {
                    int availableCount = OptionalDependencyManager.GetAvailableDependencyCount();
                    Logger.LogInfo($"[H3TVRImproved] Enhanced functionality active with {availableCount} optional dependencies");
                    
                    if (OptionalDependencyManager.IsStovepipeAvailable)
                    {
                        Logger.LogInfo("[H3TVRImproved] Stovepipe integration active - realistic weapon malfunctions enabled");
                    }
                    
                    if (OptionalDependencyManager.IsMeatyceiver2Available)
                    {
                        Logger.LogInfo("[H3TVRImproved] Meatyceiver 2 integration active - weapon transformations enabled");
                    }
                }
                else
                {
                    Logger.LogInfo("[H3TVRImproved] Running in standard mode - no optional dependencies found");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[H3TVRImproved] Error initializing optional dependencies: {ex.Message}");
            }
        }

        private void InitializeComponents()
        {
            try
            {
                // Initialize components in proper order
                slomoMovementController = gameObject.AddComponent<SlomoMovementController>();
                slomoMovementController.Initialize(slomoMovementScale.Value, slomoAffectsMovement.Value, Logger);

                inputHandler = gameObject.AddComponent<InputHandler>();
                spawnManager = gameObject.AddComponent<SpawnManager>();  // Add SpawnManager component
                effectsManager = gameObject.AddComponent<EffectsManager>();
                weaponManager = gameObject.AddComponent<WeaponManager>();
                audioManager = gameObject.AddComponent<AudioManager>();

                // Initialize each component
                audioManager.Initialize(this, Logger);
                inputHandler.Initialize(keyBindings, this);
                // SpawnManager will be initialized after AdvancedChatSosigSpawner is created
                effectsManager.Initialize(this, slomoMovementController, Logger);
                weaponManager.Initialize(this, Logger, audioManager);

                Logger.LogInfo("All components initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error initializing components: {ex.Message}");
                Logger.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        private void InitializeSosigSpawner()
        {
            try
            {
                // Initialize the Advanced Chat Sosig Spawner (Update 120 TNH System)
                // Works standalone - no Twitch integration needed
                GameObject advancedSpawnerObject = new GameObject("AdvancedChatSosigSpawner");
                advancedSpawnerObject.transform.SetParent(transform);
                
                advancedChatSpawner = advancedSpawnerObject.AddComponent<AdvancedChatSosigSpawner>();
                advancedChatSpawner.Initialize(this, Logger);
                
                Logger.LogInfo("Advanced Chat Sosig Spawner initialized with Update 120 TNH system (standalone mode)!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error initializing Sosig Spawner: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize TwitchLib integration for real-time chat
        /// </summary>
        private void InitializeTwitchIntegration()
        {
            // TwitchChatManager removed - AdvancedChatSosigSpawner works standalone
            // Twitch integration no longer available
            Logger.LogInfo("Twitch integration disabled - AdvancedChatSosigSpawner runs in standalone mode");
            Logger.LogInfo("Use keyboard controls: P = spawn ally, O = spawn enemy, Delete = clear all");
        }
        
        /// <summary>
        /// Initialize Steam Friends integration
        /// </summary>
        private void InitializeSteamFriendsIntegration()
        {
            if (!enableSteamFriends.Value)
            {
                Logger.LogInfo("Steam Friends integration disabled in config");
                return;
            }
            
            try
            {
                // Create the integration component
                GameObject steamFriendsObject = new GameObject("SteamFriendsIntegration");
                steamFriendsObject.transform.SetParent(transform);
                
                steamFriendsIntegration = steamFriendsObject.AddComponent<SteamFriendsIntegration>();
                
                // Wait for sosig spawner to be ready
                if (advancedChatSpawner != null)
                {
                    steamFriendsIntegration.Initialize(this, advancedChatSpawner, Logger);
                    Logger.LogInfo("Steam Friends integration initialized successfully");
                    Logger.LogInfo("Steam Friends controls: [ = spawn ally, ] = spawn enemy, F7 = spawn all as allies, F8 = spawn all as enemies");
                }
                else
                {
                    Logger.LogWarning("Cannot initialize Steam Friends - Advanced Chat Spawner not ready");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize Steam Friends integration: {ex.Message}");
                steamFriendsIntegration = null;
            }
        }

        /// <summary>
        /// Initialize the Sosig Armor Wrist Menu Integration
        /// </summary>
        private void InitializeSosigArmorWristMenuIntegration()
        {
            try
            {
                // Create the integration component
                var integrationObject = new GameObject("SosigArmorWristMenuIntegration");
                integrationObject.transform.SetParent(transform);

                sosigArmorWristMenu = integrationObject.AddComponent<SosigArmorWristMenuIntegration>();

                // Initialize the integration with the plugin reference
                sosigArmorWristMenu.Initialize(this, null);
                
                Logger.LogInfo("Sosig Armor Wrist Menu Integration initialized successfully");
                
                // Start delayed armor system initialization to avoid H3VR timing issues
                StartCoroutine(DelayedArmorSystemInitialization());
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize Sosig Armor Wrist Menu Integration: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initialize armor system with delay to ensure H3VR systems are ready
        /// </summary>
        private IEnumerator DelayedArmorSystemInitialization()
        {
            // Wait a few seconds for H3VR systems to be fully loaded
            yield return new WaitForSeconds(3f);
            
            // Try to initialize H3VR Asset Loader (no try-catch with yield)
            H3VRAssetLoader.TryInitializeWithDelay();
            
            // Wait a bit more for asset loading
            yield return new WaitForSeconds(1f);
            
            // Force reload armor in the wrist menu
            if (sosigArmorWristMenu?.GetArmorMenu() != null)
            {
                sosigArmorWristMenu.GetArmorMenu().ShowMessage("Reloading armor assets after H3VR initialization...");
            }
            
            Logger.LogInfo("Delayed armor system initialization completed");
            
            // Retry template cache build for chat spawner after H3VR is ready
            yield return new WaitForSeconds(2f);
            
            // Retry template cache build
            if (advancedChatSpawner != null)
            {
                Logger.LogInfo("Retrying template cache build after H3VR initialization...");
                // Use reflection to call BuildTemplateCache
                var method = advancedChatSpawner.GetType().GetMethod("BuildTemplateCache", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    try
                    {
                        method.Invoke(advancedChatSpawner, null);
                        Logger.LogInfo("Template cache rebuild completed");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Template cache rebuild warning: {ex.Message}");
                    }
                }
            }
        }
        #endregion

        #region Update Loop - Delegated to InputHandler
        public void Update()
        {
            // Handle slomo state machine
            HandleSlomoStateMachine();
            
            // Handle zero gravity state machine
            HandleZeroGravityStateMachine();
            
            // Handle malfunction boost
            HandleMalfunctionBoost();
            
            // Input handling is delegated to InputHandler component
        }

        private void HandleSlomoStateMachine()
        {
            try
            {
                switch (slomoStatus)
                {
                    case "Slowing":
                        Logger.LogInfo("Slowing!");
                        effectsManager.SlomoScaleDown();
                        break;
                    case "Wait":
                        Logger.LogInfo("Waiting!");
                        slomoStatus = "Paused";
                        try
                        {
                            audioManager?.PlaySlomoSound("active"); // Play slomo active sound
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Audio error during slomo: {ex.Message}");
                        }
                        StartCoroutine(effectsManager.SlomoWait(() => slomoStatus = "Return"));
                        break;
                    case "Return":
                        Logger.LogInfo("Returning!");
                        effectsManager.SlomoReturn();
                        break;
                }

                if (Time.timeScale == 1)
                {
                    if (slomoStatus != "Off")
                    {
                        try
                        {
                            audioManager?.PlaySlomoSound("end"); // Play slomo end sound
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Audio error ending slomo: {ex.Message}");
                        }
                    }
                    slomoStatus = "Off";
                }
                
                // Update movement scaling based on current time scale
                slomoMovementController?.UpdateMovementScale(Time.timeScale);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in slomo state machine: {ex.Message}");
                slomoStatus = "Off"; // Reset to safe state
            }
        }

        private void HandleZeroGravityStateMachine()
        {
            if (zeroGStatus == "On")
            {
                StartCoroutine(effectsManager.ZeroGWait(() => {
                    zeroGStatus = "Falling";
                    effectsManager.RealisticFall();
                }));
            }

            if (zeroGStatus == "Falling")
            {
                StartCoroutine(effectsManager.RealisticFallWait(() => {
                    effectsManager.ZeroGravityBumpUp();
                    zeroGStatus = "Off";
                }));
            }
        }

        private void HandleMalfunctionBoost()
        {
            if (malfunctionBoostActive)
            {
                if (Time.time >= malfunctionBoostEndTime)
                {
                    malfunctionBoostActive = false;
                    Logger.LogInfo("Meatyceiver malfunction boost ended.");
                }
                else
                {
                    weaponManager.ApplyMalfunctionLogic();
                }
            }
        }
        #endregion

        #region Public API for Components
        public void TriggerSlomo() 
        { 
            slomoStatus = "Slowing";
            audioManager?.PlaySlomoSound("start"); // Play slomo start sound
        }
        
        public void TriggerZeroGravity() 
        { 
            effectsManager.ZeroGravityBumpDown();
            // Zero gravity doesn't have specific audio in the current setup
        }
        
        public void ActivateMalfunctionBoost() => weaponManager.ActivateMalfunctionBoost(ref malfunctionBoostActive, ref malfunctionBoostEndTime);
        
        // Component access methods
        public SpawnManager GetSpawnManager() => spawnManager;
        public WeaponManager GetWeaponManager() => weaponManager;
        public EffectsManager GetEffectsManager() => effectsManager;
        public AudioManager GetAudioManager() => audioManager;
        
        // Spawn configuration access methods
        public void GetShurikenConfig(out int min, out int max)
        {
            min = shurikenMinCount.Value;
            max = shurikenMaxCount.Value;
        }
        
        public float GetShurikenScale() => shurikenScale.Value;
        
        public void GetPillowConfig(out int min, out int max)
        {
            min = pillowMinCount.Value;
            max = pillowMaxCount.Value;
        }
        
        public void GetDangerCloseConfig(out int min, out int max)
        {
            min = dangerCloseMinCount.Value;
            max = dangerCloseMaxCount.Value;
        }
        
        // Pillow effect configurations
        public void GetPillowGrenadeConfig(out bool enabled, out float chance, out float armedChance)
        {
            enabled = pillowGrenadeEnabled.Value;
            chance = pillowGrenadeChance.Value;
            armedChance = pillowGrenadeArmedChance.Value;
        }
        
        public void GetPillowZeroGravityConfig(out bool enabled, out float chance, out float duration)
        {
            enabled = pillowZeroGravityEnabled.Value;
            chance = pillowZeroGravityChance.Value;
            duration = pillowZeroGravityDuration.Value;
        }
        
        public void GetPillowSlomoConfig(out bool enabled, out float chance, out float duration)
        {
            enabled = pillowSlomoEnabled.Value;
            chance = pillowSlomoChance.Value;
            duration = pillowSlomoDuration.Value;
        }
        
        // Gun randomization config
        public bool UseItemManagerForGuns() => useItemManagerForGunRandomization.Value;
        
        public void GetGunLists(out string gunListValue, out string magListValue)
        {
            gunListValue = gunList.Value;
            magListValue = magazineList.Value;
        }
        
        // Slomo config
        public void GetSlomoConfig(out float maxSlomoValue, out float waitTime, out float scaleSpeed, out float returnSpeed)
        {
            maxSlomoValue = maxSlomo.Value;
            waitTime = slomoWaitTime.Value;
            scaleSpeed = slomoScaleSpeed.Value;
            returnSpeed = slomoReturnSpeed.Value;
        }
        
        public void GetSlomoVRConfig(out bool vrEnabled, out string vrButton)
        {
            vrEnabled = slomoVRControllerEnabled.Value;
            vrButton = slomoVRButton.Value;
        }
        
        // Audio config
        public void GetSlomoAudioConfig(out bool affectsAudio, out float pitchScale, out bool preservePitch)
        {
            affectsAudio = slomoAffectsAudio.Value;
            pitchScale = slomoAudioPitchScale.Value;
            preservePitch = slomoAudioPreservePitch.Value;
        }
        
        // Complete slomo audio config for enhanced Harmony patch
        public void GetSlomoAudioConfigComplete(out bool affectsAudio, out float pitchScale, out bool preservePitch, 
            out bool affectsSpeed, out float speedScale, out string mode)
        {
            affectsAudio = slomoAffectsAudio.Value;
            pitchScale = slomoAudioPitchScale.Value;
            preservePitch = slomoAudioPreservePitch.Value;
            affectsSpeed = slomoAffectsAudioSpeed.Value;
            speedScale = slomoAudioSpeedScale.Value;
            mode = slomoAudioMode.Value;
        }
        
        // Movement config
        public void GetSlomoMovementConfig(out bool affectsMovement, out float movementScale)
        {
            affectsMovement = slomoAffectsMovement.Value;
            movementScale = slomoMovementScale.Value;
        }
        
        // Update movement settings at runtime
        public void UpdateSlomoMovementSettings()
        {
            slomoMovementController?.UpdateSettings(slomoMovementScale.Value, slomoAffectsMovement.Value);
        }

        // State setters
        public void SetSlomoStatus(string status) => slomoStatus = status;

        // Add missing methods
        /// <summary>
        /// Get the SosigArmorWristMenuIntegration instance
        /// </summary>
        public SosigArmorWristMenuIntegration GetSosigArmorWristMenu()
        {
            return sosigArmorWristMenu;
        }

        // Add access method for Advanced Chat Spawner
        public AdvancedChatSosigSpawner GetAdvancedChatSpawner() => advancedChatSpawner;
        
        // Add access method for Steam Friends Integration
        public SteamFriendsIntegration GetSteamFriendsIntegration() => steamFriendsIntegration;
        
        // Steam Friends configuration access
        public bool IsSteamFriendsEnabled() => enableSteamFriends != null && enableSteamFriends.Value;
        public bool UseSteamFriendsRandomNames() => steamFriendsRandomNames != null && steamFriendsRandomNames.Value;
        public float GetSteamFriendsRefreshInterval() => steamFriendsRefreshInterval != null ? steamFriendsRefreshInterval.Value : 300f;
        #endregion

        #region Harmony Patches and Cleanup
        // Dictionary to store original speeds for audio sources during slomo
        private static Dictionary<AudioSource, float> originalAudioSpeeds = new Dictionary<AudioSource, float>();
        
        [HarmonyPatch(typeof(AudioSource), "pitch", MethodType.Setter)]
        [HarmonyPrefix]
        public static void FixPitch(AudioSource __instance, ref float value)
        {
            // Get the current plugin instance to access configuration
            var instance = FindObjectOfType<H3TVRImproved>();
            if (instance == null) return;
            
            // Only process if not in normal time scale
            if (Time.timeScale >= 0.99f && Time.timeScale <= 1.01f)
            {
                // Normal time - clean up any stored state
                if (originalAudioSpeeds.ContainsKey(__instance))
                {
                    originalAudioSpeeds.Remove(__instance);
                }
                return;
            }
            
            // Get complete audio configuration
            bool affectsAudio, preservePitch, affectsSpeed;
            float pitchScale, speedScale;
            string mode;
            instance.GetSlomoAudioConfigComplete(out affectsAudio, out pitchScale, out preservePitch, 
                out affectsSpeed, out speedScale, out mode);
            
            if (!affectsAudio)
            {
                return;
            }
            
            // Store original speed if not already stored
            if (!originalAudioSpeeds.ContainsKey(__instance))
            {
                originalAudioSpeeds[__instance] = 1.0f; // Default unity speed
            }
            
            // Apply audio adjustments based on mode
            switch (mode.ToLower())
            {
                case "pitchonly":
                    // Only adjust pitch, preserve speed
                    ApplyPitchAdjustment(ref value, preservePitch, pitchScale);
                    break;
                    
                case "speedonly":
                    // Only adjust speed (via pitch manipulation), preserve pitch perception
                    // This is a simulation since Unity doesn't have direct speed control without pitch
                    ApplySpeedAdjustment(__instance, speedScale);
                    value = 1.0f; // Keep pitch normal
                    break;
                    
                case "both":
                    // Both pitch and speed scale with time (classic slomo effect)
                    ApplyPitchAdjustment(ref value, preservePitch, pitchScale);
                    if (affectsSpeed)
                    {
                        ApplySpeedAdjustment(__instance, speedScale);
                    }
                    break;
                    
                case "independent":
                    // Independent control of pitch and speed
                    ApplyPitchAdjustment(ref value, preservePitch, pitchScale);
                    if (affectsSpeed)
                    {
                        ApplySpeedAdjustment(__instance, speedScale);
                    }
                    break;
                    
                default:
                    // Default to "Both" mode
                    ApplyPitchAdjustment(ref value, preservePitch, pitchScale);
                    if (affectsSpeed)
                    {
                        ApplySpeedAdjustment(__instance, speedScale);
                    }
                    break;
            }
            
            // Always ensure pitch is within reasonable bounds
            value = Mathf.Clamp(value, 0.1f, 3.0f);
        }
        
        /// <summary>
        /// Apply pitch adjustment based on configuration
        /// </summary>
        private static void ApplyPitchAdjustment(ref float pitch, bool preservePitch, float pitchScale)
        {
            if (preservePitch)
            {
                // Preserve original pitch by compensating for time scale
                pitch *= (1f / Time.timeScale);
            }
            else
            {
                // Apply custom pitch scaling
                float newPitch = pitch * (Time.timeScale * pitchScale);
                pitch = Mathf.Clamp(newPitch, 0.1f, 3.0f);
            }
        }
        
        /// <summary>
        /// Apply speed adjustment to audio source
        /// Note: Unity doesn't support true time-stretching, so we simulate it
        /// </summary>
        private static void ApplySpeedAdjustment(AudioSource source, float speedScale)
        {
            if (source == null || source.clip == null) return;
            
            try
            {
                // Calculate target playback speed
                float targetSpeed = Time.timeScale * speedScale;
                
                // Clamp to reasonable values
                targetSpeed = Mathf.Clamp(targetSpeed, 0.1f, 3.0f);
                
                // Since Unity doesn't have direct speed control independent of pitch,
                // we adjust the playback position to simulate slower playback
                // This is a best-effort simulation
                if (source.isPlaying && targetSpeed < 0.95f)
                {
                    // Store current normalized time
                    float normalizedTime = source.time / source.clip.length;
                    
                    // Slow down by adjusting sample position
                    // This creates a time-stretching effect
                    int targetSample = Mathf.RoundToInt(source.timeSamples * targetSpeed);
                    
                    // Only adjust if significantly different
                    if (Mathf.Abs(targetSample - source.timeSamples) > 100)
                    {
                        source.timeSamples = Mathf.Clamp(targetSample, 0, source.clip.samples - 1);
                    }
                }
            }
            catch (Exception ex)
            {
                // Safely handle any errors in audio manipulation
                UnityEngine.Debug.LogWarning($"[H3TVR Audio] Speed adjustment failed: {ex.Message}");
            }
        }
        
        [HarmonyPatch(typeof(AudioSource), "Stop")]
        [HarmonyPrefix]
        public static void OnAudioSourceStop(AudioSource __instance)
        {
            // Clean up stored speed data when audio stops
            if (originalAudioSpeeds.ContainsKey(__instance))
            {
                originalAudioSpeeds.Remove(__instance);
            }
        }

        private void OnDestroy()
        {
            hooks.Unhook();
            slomoMovementController?.Reset();
            
            // Clean up audio speed tracking
            if (originalAudioSpeeds != null)
            {
                originalAudioSpeeds.Clear();
            }
        }
        #endregion
    }
}