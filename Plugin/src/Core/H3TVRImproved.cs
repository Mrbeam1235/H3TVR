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
        
        // NEW: Slomo Ramp State
        private float slomoRampStartTime;
        private float slomoRampStartValue;
        private bool isRamping;
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
        
        // NEW: Slomo Ramp Configuration
        private ConfigEntry<bool> slomoUseRampSpeed;
        private ConfigEntry<string> slomoRampCurve; // "Linear", "EaseIn", "EaseOut", "EaseInOut", "Smooth"
        private ConfigEntry<float> slomoRampDuration; // How long the ramp takes
        private ConfigEntry<float> slomoReturnRampDuration; // Duration for return to normal

        // NEW: Kill Slomo Configuration
        private ConfigEntry<bool> enableKillSlomo;
        
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
        // NOTE: File paths are configured in ChatWatcher under [Chat Watcher - File Mode]
        // to avoid duplicate config entries. Only enable/max settings here.
        private ConfigEntry<bool> enableTwitchChatSosigs;
        private ConfigEntry<int> maxChatSosigs;
        
        // Steam Friends Configuration
        private ConfigEntry<bool> enableSteamFriends;
        private ConfigEntry<bool> steamFriendsRandomNames;
        private ConfigEntry<float> steamFriendsRefreshInterval;
        
        // Take and Hold Configuration
        private ConfigEntry<bool> enableInfiniteTokens;
        private ConfigEntry<bool> disableEncryptionNodes;
        
        // NEW: Specific encryption controls
        private ConfigEntry<bool> disableAllEncryptions;
        private ConfigEntry<bool> disableEncryptionType1;
        private ConfigEntry<bool> disableEncryptionType2;
        private ConfigEntry<bool> disableEncryptionType3;
        private ConfigEntry<bool> autoCompleteEncryption;
        private ConfigEntry<float> encryptionCompletionDelay;
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
        private SosigCustomizationUI sosigCustomizationUI;
        private AirdropManager airdropManager;
        private LioranBoardIntegration lioranBoardIntegration;
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

                // Initialize UI
                base.Logger.LogInfo("Step 3.5: Initializing UI...");
                InitializeUI();
                
                // Initialize Airdrop Manager
                base.Logger.LogInfo("Step 3.6: Initializing Airdrop Manager...");
                InitializeAirdropManager();

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

                // Initialize LioranBoard integration
                base.Logger.LogInfo("Step 6.6: Initializing LioranBoard 2 integration...");
                InitializeLioranBoardIntegration();
                
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
            
            // NEW: Slomo Ramp Configuration
            slomoUseRampSpeed = Config.Bind("Slomo.Ramp", "UseRampSpeed", true, "Enable smooth ramp speed transitions for slomo (more cinematic)");
            slomoRampCurve = Config.Bind("Slomo.Ramp", "RampCurve", "EaseInOut", 
       "Curve type for slomo ramp: Linear, EaseIn, EaseOut, EaseInOut, Smooth, Cinematic");
            slomoRampDuration = Config.Bind("Slomo.Ramp", "RampDuration", 0.5f, 
        "Duration in seconds for slomo to ramp down to max slow speed");
            slomoReturnRampDuration = Config.Bind("Slomo.Ramp", "ReturnRampDuration", 0.8f, 
       "Duration in seconds for slomo to ramp back to normal speed");

            // NEW: Kill Slomo Configuration
            enableKillSlomo = Config.Bind("Slomo", "EnableKillSlomo", true, "Enable slow motion effect on enemy kill.");
        
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
            // NOTE: File paths are in ChatWatcher [Chat Watcher - File Mode] section
            enableTwitchChatSosigs = Config.Bind("ChatSosigs", "Enabled", true, "Enable Chat Sosig spawning system");
            maxChatSosigs = Config.Bind("ChatSosigs", "MaxChatSosigs", 10, "Maximum number of active chat sosigs");

            // Steam Friends Configuration
            enableSteamFriends = Config.Bind("SteamFriends", "Enabled", true, "Enable Steam Friends integration for sosig spawning");
            steamFriendsRandomNames = Config.Bind("SteamFriends", "UseRandomNames", false, "Use random friend from list instead of specific name");
            steamFriendsRefreshInterval = Config.Bind("SteamFriends", "RefreshInterval", 300f, "Auto-refresh Steam friends list interval (seconds)");
            
            // Take and Hold Configuration
            enableInfiniteTokens = Config.Bind("TakeAndHold", "InfiniteTokens", false, "Enable infinite tokens in Take and Hold mode");
            disableEncryptionNodes = Config.Bind("TakeAndHold", "DisableEncryptionNodes", false, "Disable encryption nodes in Take and Hold mode for easier gameplay");
            
            // NEW: Specific encryption controls
            disableAllEncryptions = Config.Bind("TakeAndHold.Encryption", "DisableAllEncryptions", false, 
                "Master switch: Disable ALL encryption nodes (overrides specific settings)");
            disableEncryptionType1 = Config.Bind("TakeAndHold.Encryption", "DisableType1", false, 
                "Disable Type 1 encryption nodes (pattern matching)");
            disableEncryptionType2 = Config.Bind("TakeAndHold.Encryption", "DisableType2", false, 
                "Disable Type 2 encryption nodes (sequence)");
            disableEncryptionType3 = Config.Bind("TakeAndHold.Encryption", "DisableType3", false, 
                "Disable Type 3 encryption nodes (timed)");
            autoCompleteEncryption = Config.Bind("TakeAndHold.Encryption", "AutoComplete", false, 
                "Automatically complete enabled encryption nodes after delay");
            encryptionCompletionDelay = Config.Bind("TakeAndHold.Encryption", "CompletionDelay", 2.0f, 
                "Delay in seconds before auto-completing encryption (if AutoComplete enabled)");
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
                
                // Boss Sosig Key Bindings
                { "SpawnBossRandom", new KeyValuePair<KeyCode, string>(KeyCode.B, "Spawn Random Boss") },
                { "SpawnBossTank", new KeyValuePair<KeyCode, string>(KeyCode.Alpha1, "Spawn Tank Boss") },
                { "SpawnBossBerserker", new KeyValuePair<KeyCode, string>(KeyCode.Alpha2, "Spawn Berserker Boss") },
                { "SpawnBossSniper", new KeyValuePair<KeyCode, string>(KeyCode.Alpha3, "Spawn Sniper Boss") },
                { "SpawnBossSummoner", new KeyValuePair<KeyCode, string>(KeyCode.Alpha4, "Spawn Summoner Boss") },
                { "SpawnBossElite", new KeyValuePair<KeyCode, string>(KeyCode.Alpha5, "Spawn Elite Boss") },
                { "SpawnBossJuggernaut", new KeyValuePair<KeyCode, string>(KeyCode.Alpha6, "Spawn Juggernaut Boss") },
                { "SpawnBossAssassin", new KeyValuePair<KeyCode, string>(KeyCode.Alpha7, "Spawn Assassin Boss") },
                { "SpawnBossCommander", new KeyValuePair<KeyCode, string>(KeyCode.Alpha8, "Spawn Commander Boss") },
                { "ClearBosses", new KeyValuePair<KeyCode, string>(KeyCode.Backspace, "Clear All Bosses") },
                
                // Steam Friends Key Bindings
                { "SpawnSteamFriendAlly", new KeyValuePair<KeyCode, string>(KeyCode.LeftBracket, "Spawn Steam Friend as Ally") },
                { "SpawnSteamFriendEnemy", new KeyValuePair<KeyCode, string>(KeyCode.RightBracket, "Spawn Steam Friend as Enemy") },
                { "SpawnAllSteamFriendsAlly", new KeyValuePair<KeyCode, string>(KeyCode.F7, "Spawn All Steam Friends as Allies") },
                { "SpawnAllSteamFriendsEnemy", new KeyValuePair<KeyCode, string>(KeyCode.F8, "Spawn All Steam Friends as Enemies") },
                { "RefreshSteamFriends", new KeyValuePair<KeyCode, string>(KeyCode.F9, "Refresh Steam Friends List") },
                { "SteamFriendsStats", new KeyValuePair<KeyCode, string>(KeyCode.Home, "Show Steam Friends Stats") },
                
                // JerryAr mod keybindings
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

                // Initialize Advanced AI Config
                AdvancedAIConfig.ApplyConfig(Config);

                // Initialize Boss Sosig Config
                BossConfig.ApplyConfig(Config);

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

                // Log Advanced AI status
                if (AdvancedSosigAI.EnableAdvancedAI)
                {
                    Logger.LogInfo("[H3TVRImproved] Advanced AI system enabled");
                }

                // Log Boss System status
                if (BossSosigSystem.EnableBossSosigs)
                {
                    Logger.LogInfo($"[H3TVRImproved] Boss Sosig system enabled (Max: {BossSosigSystem.MaxBossesPerSession})");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[H3TVRImproved] Error initializing optional dependencies: {ex.Message}");
            }
        }

        private void InitializeAirdropManager()
        {
            airdropManager = gameObject.AddComponent<AirdropManager>();
            airdropManager.Initialize(this, Logger);
        }

        private void InitializeUI()
        {
            sosigCustomizationUI = gameObject.AddComponent<SosigCustomizationUI>();
            sosigCustomizationUI.Initialize(Config);
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
        /// Initialize LioranBoard 2 integration for external commands
        /// </summary>
        private void InitializeLioranBoardIntegration()
        {
            try
            {
                GameObject lioranBoardObject = new GameObject("LioranBoardIntegration");
                lioranBoardObject.transform.SetParent(transform);
                lioranBoardIntegration = lioranBoardObject.AddComponent<LioranBoardIntegration>();
                lioranBoardIntegration.Initialize(Logger, this);
                Logger.LogInfo("LioranBoard 2 integration initialized successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize LioranBoard 2 integration: {ex.Message}");
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
    
            // Handle infinite tokens for Take and Hold
            HandleInfiniteTokens();
            
            // Update weapon scale modifiers
            weaponManager?.UpdateScaleModifiers();
     
     // Input handling is delegated to InputHandler component
        }

        private void OnDestroy()
        {
            lioranBoardIntegration?.Shutdown();
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
                
                // Change to just update the controller - it handles its own scaling now
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
        
        private void HandleInfiniteTokens()
        {
            if (!enableInfiniteTokens.Value && !disableEncryptionNodes.Value && !disableAllEncryptions.Value) return;
            
            try
            {
                // Check if in TNH mode
                if (GM.TNH_Manager != null && GM.TNH_Manager.m_curHoldPoint != null)
                {
                    // Set tokens to a high number (999) if infinite tokens enabled
                    if (enableInfiniteTokens.Value)
                    {
                        GM.TNH_Manager.m_numTokens = 999;
                    }
                    
                    // Handle encryption disabling
                    if (disableAllEncryptions.Value || disableEncryptionNodes.Value)
                    {
                        DisableEncryptionNodes();
                    }
                    else if (disableEncryptionType1.Value || disableEncryptionType2.Value || disableEncryptionType3.Value)
                    {
                        DisableSpecificEncryptionNodes();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in HandleInfiniteTokens: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Disable ALL encryption nodes in TNH to make it easier
        /// </summary>
        private void DisableEncryptionNodes()
        {
            try
            {
                if (GM.TNH_Manager == null || GM.TNH_Manager.m_curHoldPoint == null) return;
                
                // Get current hold point
                var holdPoint = GM.TNH_Manager.m_curHoldPoint;
                
                // Check if there are encryption systems
                if (holdPoint.m_systemNode != null)
                {
                    // Mark encryption as complete/disabled
                    if (holdPoint.m_systemNode.m_hasActivated == false)
                    {
                        if (autoCompleteEncryption.Value)
                        {
                            // Auto-complete after delay
                            StartCoroutine(AutoCompleteEncryptionDelayed(holdPoint.m_systemNode, encryptionCompletionDelay.Value));
                            Logger.LogDebug($"[TNH] Auto-completing all encryptions after {encryptionCompletionDelay.Value}s delay");
                        }
                        else
                        {
                            // Instantly complete encryption
                            CompleteEncryptionNode(holdPoint.m_systemNode);
                            Logger.LogDebug("[TNH] Disabled all encryption nodes");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[TNH] Error disabling encryption nodes: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Disable specific encryption node types based on configuration
        /// </summary>
        private void DisableSpecificEncryptionNodes()
        {
            try
            {
                if (GM.TNH_Manager == null || GM.TNH_Manager.m_curHoldPoint == null) return;
                
                var holdPoint = GM.TNH_Manager.m_curHoldPoint;
                
                if (holdPoint.m_systemNode != null && !holdPoint.m_systemNode.m_hasActivated)
                {
                    var systemNode = holdPoint.m_systemNode;
                    
                    // Detect encryption type based on TNH system node properties
                    EncryptionType detectedType = DetectEncryptionType(systemNode);
                    
                    bool shouldDisable = false;
                    string typeDescription = "";
                    
                    switch (detectedType)
                    {
                        case EncryptionType.Pattern:
                            if (disableEncryptionType1.Value)
                            {
                                shouldDisable = true;
                                typeDescription = "Pattern";
                            }
                            break;
                            
                        case EncryptionType.Sequence:
                            if (disableEncryptionType2.Value)
                            {
                                shouldDisable = true;
                                typeDescription = "Sequence";
                            }
                            break;
                            
                        case EncryptionType.Timed:
                            if (disableEncryptionType3.Value)
                            {
                                shouldDisable = true;
                                typeDescription = "Timed";
                            }
                            break;
                            
                        case EncryptionType.Unknown:
                            // Unknown type - apply if any specific type is disabled
                            if (disableEncryptionType1.Value || disableEncryptionType2.Value || disableEncryptionType3.Value)
                            {
                                shouldDisable = true;
                                typeDescription = "Unknown";
                            }
                            break;
                    }
                    
                    if (shouldDisable)
                    {
                        if (autoCompleteEncryption.Value)
                        {
                            StartCoroutine(AutoCompleteEncryptionDelayed(systemNode, encryptionCompletionDelay.Value));
                            Logger.LogDebug($"[TNH] Auto-completing {typeDescription} encryption after {encryptionCompletionDelay.Value}s delay");
                        }
                        else
                        {
                            CompleteEncryptionNode(systemNode);
                            Logger.LogDebug($"[TNH] Disabled {typeDescription} encryption");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[TNH] Error disabling specific encryption: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Encryption type enum for classification
        /// </summary>
        private enum EncryptionType
        {
            Unknown,
            Pattern,    // Type 1: Pattern matching/specific targets
            Sequence,   // Type 2: Sequential/ordered
            Timed       // Type 3: Time pressure
        }
        
        /// <summary>
        /// Detect the type of encryption based on node properties
        /// Note: H3VR doesn't expose encryption types directly, so we use heuristics
        /// </summary>
        private EncryptionType DetectEncryptionType(TNH_HoldPointSystemNode encryptionNode)
        {
            try
            {
                if (encryptionNode == null) return EncryptionType.Unknown;
                
                // Type detection based on system node configuration
                // Since H3VR doesn't expose specific encryption types, we classify all as unknown
                // and let the user choose which ones to disable via config
                
                // Default to unknown - user can disable all with DisableAllEncryptions
                return EncryptionType.Unknown;
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[TNH] Error detecting encryption type: {ex.Message}");
                return EncryptionType.Unknown;
            }
        }
        
        /// <summary>
        /// Complete an encryption node (mark as finished)
        /// </summary>
        private void CompleteEncryptionNode(TNH_HoldPointSystemNode encryptionNode)
        {
            try
            {
                if (encryptionNode == null) return;
                
                // Deactivate the node by disabling it
                if (encryptionNode.gameObject != null)
                {
                    encryptionNode.gameObject.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[TNH] Error completing encryption node: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Auto-complete encryption after configured delay
        /// </summary>
        private IEnumerator AutoCompleteEncryptionDelayed(TNH_HoldPointSystemNode encryptionNode, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            try
            {
                if (encryptionNode != null && encryptionNode.gameObject != null && encryptionNode.gameObject.activeSelf)
                {
                    CompleteEncryptionNode(encryptionNode);
                    Logger.LogDebug($"[TNH] Auto-completed encryption after {delay}s delay");
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[TNH] Error auto-completing encryption: {ex.Message}");
            }
        }
        #endregion

        #region Public API for Components
        public void TriggerSlomo() 
        { 
            slomoStatus = "Slowing";
            
            // Initialize ramp state
            slomoRampStartTime = Time.unscaledTime;
            slomoRampStartValue = Time.timeScale;
            isRamping = true;
            
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
        public AirdropManager GetAirdropManager() => airdropManager;
        
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
        
        // Slomo config
        public bool IsKillSlomoEnabled() => enableKillSlomo.Value;

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
        
        // NEW: Slomo Ramp Config
        public void GetSlomoRampConfig(out bool useRamp, out string curve, out float rampDuration, out float returnDuration)
        {
         useRamp = slomoUseRampSpeed.Value;
  curve = slomoRampCurve.Value;
 rampDuration = slomoRampDuration.Value;
    returnDuration = slomoReturnRampDuration.Value;
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
        
        // Take and Hold methods
        public bool IsInfiniteTokensEnabled() => enableInfiniteTokens != null && enableInfiniteTokens.Value;
        public bool IsEncryptionDisabled() => disableEncryptionNodes != null && disableEncryptionNodes.Value;
        
        public void SetInfiniteTokens(bool enabled)
     {
      if (enableInfiniteTokens != null)
        {
       enableInfiniteTokens.Value = enabled;
      Logger.LogInfo($"Infinite tokens {(enabled ? "enabled" : "disabled")}");
            }
        }
        
        public void SetEncryptionNodes(bool disabled)
        {
      if (disableEncryptionNodes != null)
  {
       disableEncryptionNodes.Value = disabled;
       Logger.LogInfo($"Encryption nodes {(disabled ? "disabled" : "enabled")}");

   }
        }
        
        public void GetSlomoVRConfig(out bool vrEnabled, out string vrButton)
   {
            vrEnabled = slomoVRControllerEnabled.Value;
    vrButton = slomoVRButton.Value;
   }
      
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
       Debug.LogError($"Error applying speed adjustment: {ex.Message}");
      }
        }
        #endregion
    }
}