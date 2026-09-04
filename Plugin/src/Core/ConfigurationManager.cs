using BepInEx.Configuration;
using BepInEx.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace H3TVR
{
    /// <summary>
    /// Centralized configuration management for H3TVR.
    /// All BepInEx config entries live here, organized by feature.
    /// </summary>
    public class ConfigurationManager
    {
        private readonly ConfigFile config;
        private readonly ManualLogSource logger;

        #region Slomo Configuration
        public ConfigEntry<float> MaxSlomo { get; private set; }
        public ConfigEntry<float> SlomoWaitTime { get; private set; }
        public ConfigEntry<float> SlomoScaleSpeed { get; private set; }
        public ConfigEntry<float> SlomoReturnSpeed { get; private set; }
        public ConfigEntry<bool> SlomoVRControllerEnabled { get; private set; }
        public ConfigEntry<string> SlomoVRButton { get; private set; }
        public ConfigEntry<bool> SlomoAffectsMovement { get; private set; }
        public ConfigEntry<float> SlomoMovementScale { get; private set; }

        // Slomo Ramp
        public ConfigEntry<bool> SlomoUseRampSpeed { get; private set; }
        public ConfigEntry<string> SlomoRampCurve { get; private set; }
        public ConfigEntry<float> SlomoRampDuration { get; private set; }
        public ConfigEntry<float> SlomoReturnRampDuration { get; private set; }

        // Kill Slomo
        public ConfigEntry<bool> EnableKillSlomo { get; private set; }
        #endregion

        #region Audio Configuration
        public ConfigEntry<bool> SlomoAffectsAudio { get; private set; }
        #endregion

        #region Gun Randomization Configuration
        public ConfigEntry<bool> UseItemManagerForGunRandomization { get; private set; }
        public ConfigEntry<string> GunList { get; private set; }
        public ConfigEntry<string> MagazineList { get; private set; }
        #endregion

        #region Spawn Configuration - Shuriken
        public ConfigEntry<float> ShurikenScale { get; private set; }
        public ConfigEntry<int> ShurikenMinCount { get; private set; }
        public ConfigEntry<int> ShurikenMaxCount { get; private set; }
        #endregion

        #region Spawn Configuration - Pillow
        public ConfigEntry<int> PillowMinCount { get; private set; }
        public ConfigEntry<int> PillowMaxCount { get; private set; }
        public ConfigEntry<bool> PillowGrenadeEnabled { get; private set; }
        public ConfigEntry<float> PillowGrenadeChance { get; private set; }
        public ConfigEntry<float> PillowGrenadeArmedChance { get; private set; }
        public ConfigEntry<bool> PillowZeroGravityEnabled { get; private set; }
        public ConfigEntry<float> PillowZeroGravityChance { get; private set; }
        public ConfigEntry<float> PillowZeroGravityDuration { get; private set; }
        public ConfigEntry<bool> PillowSlomoEnabled { get; private set; }
        public ConfigEntry<float> PillowSlomoChance { get; private set; }
        public ConfigEntry<float> PillowSlomoDuration { get; private set; }
        #endregion

        #region Danger Close Configuration
        public ConfigEntry<int> DangerCloseMinCount { get; private set; }
        public ConfigEntry<int> DangerCloseMaxCount { get; private set; }
        #endregion

        #region Key Bindings
        public Dictionary<string, ConfigEntry<KeyCode>> KeyBindings { get; private set; }
            = new Dictionary<string, ConfigEntry<KeyCode>>();
        #endregion

        #region Steam Friends Configuration
        public ConfigEntry<bool> EnableSteamFriends { get; private set; }
        public ConfigEntry<bool> SteamFriendsRandomNames { get; private set; }
        public ConfigEntry<float> SteamFriendsRefreshInterval { get; private set; }
        #endregion

        #region Take and Hold Configuration
        public ConfigEntry<bool> EnableInfiniteTokens { get; private set; }
        public ConfigEntry<bool> DisableEncryptionNodes { get; private set; }
        public ConfigEntry<bool> DisableAllEncryptions { get; private set; }
        public ConfigEntry<bool> DisableEncryptionType1 { get; private set; }
        public ConfigEntry<bool> DisableEncryptionType2 { get; private set; }
        public ConfigEntry<bool> DisableEncryptionType3 { get; private set; }
        public ConfigEntry<bool> AutoCompleteEncryption { get; private set; }
        public ConfigEntry<float> EncryptionCompletionDelay { get; private set; }
        #endregion

        public ConfigurationManager(ConfigFile configFile, ManualLogSource logSource)
        {
            config = configFile;
            logger = logSource;
        }

        /// <summary>
        /// Initialize all configuration entries. Call once during plugin Awake().
        /// </summary>
        public void InitializeAll()
        {
            InitializeSlomoConfig();
            InitializeGunRandomizationConfig();
            InitializeSpawnConfigurations();
            InitializeKeyBindings();

            logger.LogInfo("ConfigurationManager: All configuration entries initialized");
        }

        private void InitializeSlomoConfig()
        {
            MaxSlomo = config.Bind("Slomo", "MaxSlowmoScale", 0.1f,
                "Maximum slomo scale (0.01 = 1% speed, 0.1 = 10% speed)");
            SlomoWaitTime = config.Bind("Slomo", "WaitTime", 2f,
                "Time to wait at max slomo before returning to normal speed");
            SlomoScaleSpeed = config.Bind("Slomo", "ScaleDownSpeed", 1f,
                "Speed at which time slows down (higher = faster transition)");
            SlomoReturnSpeed = config.Bind("Slomo", "ReturnSpeed", 0.33f,
                "Speed at which time returns to normal (higher = faster return)");
            SlomoVRControllerEnabled = config.Bind("Slomo", "VRControllerEnabled", true,
                "Enable VR controller button to trigger slomo");
            SlomoVRButton = config.Bind("Slomo", "VRButton", "LeftX",
                "VR button to trigger slomo");
            SlomoAffectsMovement = config.Bind("Slomo", "AffectsMovement", true,
                "Whether slomo affects player movement speed");
            SlomoMovementScale = config.Bind("Slomo", "MovementScale", 0.3f,
                "Movement speed multiplier during slomo");

            // Ramp
            SlomoUseRampSpeed = config.Bind("Slomo.Ramp", "UseRampSpeed", true,
                "Enable smooth ramp speed transitions for slomo (more cinematic)");
            SlomoRampCurve = config.Bind("Slomo.Ramp", "RampCurve", "EaseInOut",
                "Curve type for slomo ramp: Linear, EaseIn, EaseOut, EaseInOut, Smooth, Cinematic");
            SlomoRampDuration = config.Bind("Slomo.Ramp", "RampDuration", 0.5f,
                "Duration in seconds for slomo to ramp down to max slow speed");
            SlomoReturnRampDuration = config.Bind("Slomo.Ramp", "ReturnRampDuration", 0.8f,
                "Duration in seconds for slomo to ramp back to normal speed");

            // Kill Slomo
            EnableKillSlomo = config.Bind("Slomo", "EnableKillSlomo", true,
                "Enable slow motion effect on enemy kill.");
        }

        private void InitializeGunRandomizationConfig()
        {
            UseItemManagerForGunRandomization = config.Bind("GunRandomization", "UseItemManager", true,
                "Use ItemManager for gun randomization (includes all H3VR and modded guns). If false, uses GunList/MagazineList config files.");
            GunList = config.Bind("General", "GunList", "DefaultGunList", "List of guns");
            MagazineList = config.Bind("General", "MagazineList", "DefaultMagazineList", "List of magazines");
        }

        private void InitializeSpawnConfigurations()
        {
            // Shuriken
            ShurikenScale = config.Bind("Shuriken", "Scale", 1.0f,
                "Scale multiplier for spawned shuriken");
            ShurikenMinCount = config.Bind("Shuriken", "MinCount", 1,
                "Minimum number of shuriken to spawn");
            ShurikenMaxCount = config.Bind("Shuriken", "MaxCount", 3,
                "Maximum number of shuriken to spawn");

            // Pillow
            PillowMinCount = config.Bind("Pillow", "MinCount", 1,
                "Minimum number of pillows to spawn");
            PillowMaxCount = config.Bind("Pillow", "MaxCount", 3,
                "Maximum number of pillows to spawn");
            PillowGrenadeEnabled = config.Bind("Pillow", "GrenadeEnabled", true,
                "Enable pillow grenade effect");
            PillowGrenadeChance = config.Bind("Pillow", "GrenadeChance", 0.1f,
                "Chance for pillow to spawn grenade");
            PillowGrenadeArmedChance = config.Bind("Pillow", "GrenadeArmedChance", 0.3f,
                "Chance for pillow grenade to be armed");
            PillowZeroGravityEnabled = config.Bind("Pillow", "ZeroGEnabled", true,
                "Enable pillow zero gravity effect");
            PillowZeroGravityChance = config.Bind("Pillow", "ZeroGChance", 0.15f,
                "Chance for pillow to trigger zero gravity");
            PillowZeroGravityDuration = config.Bind("Pillow", "ZeroGDuration", 10f,
                "Duration of pillow zero gravity effect");
            PillowSlomoEnabled = config.Bind("Pillow", "SlomoEnabled", true,
                "Enable pillow slow motion effect");
            PillowSlomoChance = config.Bind("Pillow", "SlomoChance", 0.2f,
                "Chance for pillow to trigger slow motion");
            PillowSlomoDuration = config.Bind("Pillow", "SlomoDuration", 8f,
                "Duration of pillow slow motion effect");

            // Danger Close
            DangerCloseMinCount = config.Bind("DangerClose", "MinCount", 1,
                "Minimum danger close rounds");
            DangerCloseMaxCount = config.Bind("DangerClose", "MaxCount", 5,
                "Maximum danger close rounds");

            // Steam Friends
            EnableSteamFriends = config.Bind("SteamFriends", "Enabled", true,
                "Enable Steam Friends list integration");
            SteamFriendsRandomNames = config.Bind("SteamFriends", "UseRandomNames", false,
                "Use random friend from list instead of specific name");
            SteamFriendsRefreshInterval = config.Bind("SteamFriends", "RefreshInterval", 300f,
                "Auto-refresh Steam friends list interval (seconds)");

            // Take and Hold
            EnableInfiniteTokens = config.Bind("TakeAndHold", "InfiniteTokens", false,
                "Enable infinite tokens in Take and Hold mode");
            DisableEncryptionNodes = config.Bind("TakeAndHold", "DisableEncryptionNodes", false,
                "Disable encryption nodes in Take and Hold mode for easier gameplay");
            DisableAllEncryptions = config.Bind("TakeAndHold.Encryption", "DisableAllEncryptions", false,
                "Master switch: Disable ALL encryption nodes (overrides specific settings)");
            DisableEncryptionType1 = config.Bind("TakeAndHold.Encryption", "DisableType1", false,
                "Disable Type 1 encryption nodes (pattern matching)");
            DisableEncryptionType2 = config.Bind("TakeAndHold.Encryption", "DisableType2", false,
                "Disable Type 2 encryption nodes (sequence)");
            DisableEncryptionType3 = config.Bind("TakeAndHold.Encryption", "DisableType3", false,
                "Disable Type 3 encryption nodes (timed)");
            AutoCompleteEncryption = config.Bind("TakeAndHold.Encryption", "AutoComplete", false,
                "Automatically complete enabled encryption nodes after delay");
            EncryptionCompletionDelay = config.Bind("TakeAndHold.Encryption", "CompletionDelay", 2.0f,
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
                { "RefreshSteamFriends", new KeyValuePair<KeyCode, string>(KeyCode.F9, "Refresh Steam Friends List") },
                { "SteamFriendsStats", new KeyValuePair<KeyCode, string>(KeyCode.Home, "Show Steam Friends Stats") },
                { "SpawnAirStrike", new KeyValuePair<KeyCode, string>(KeyCode.F10, "Spawn Air Strike Smoke Grenade") },
                { "SpawnTitanMachine", new KeyValuePair<KeyCode, string>(KeyCode.F11, "Spawn Titan Machine (AI Enemy)") },
                { "SpawnNuke", new KeyValuePair<KeyCode, string>(KeyCode.N, "Spawn Nuke (Massive Explosion)") },
                { "EmptyHeldGunChamber", new KeyValuePair<KeyCode, string>(KeyCode.E, "Empty Held Gun Chamber") },
                { "SwapHeldGun", new KeyValuePair<KeyCode, string>(KeyCode.F12, "Swap Held Gun for Random Gun") }
            };

            foreach (var kvp in keyBindingConfigs)
            {
                KeyBindings[kvp.Key] = config.Bind("KeyBindings", $"KeyBindFor{kvp.Key}",
                    kvp.Value.Key, kvp.Value.Value);
            }
        }

        #region Cached Config Value Helpers (for hot paths)
        // These avoid accessing .Value on every frame in Update()

        private float cachedMaxSlomo;
        private float cachedSlomoWaitTime;
        private float cachedSlomoScaleSpeed;
        private float cachedSlomoReturnSpeed;
        private bool cachedSlomoUseRamp;
        private string cachedSlomoRampCurve;
        private float cachedSlomoRampDuration;
        private float cachedSlomoReturnRampDuration;
        private bool cachedEnableInfiniteTokens;
        private bool cachedDisableEncryptionNodes;
        private bool cachedDisableAllEncryptions;

        /// <summary>
        /// Refresh cached values from config entries. Call once per scene load or config change.
        /// </summary>
        public void RefreshCachedValues()
        {
            cachedMaxSlomo = MaxSlomo.Value;
            cachedSlomoWaitTime = SlomoWaitTime.Value;
            cachedSlomoScaleSpeed = SlomoScaleSpeed.Value;
            cachedSlomoReturnSpeed = SlomoReturnSpeed.Value;
            cachedSlomoUseRamp = SlomoUseRampSpeed.Value;
            cachedSlomoRampCurve = SlomoRampCurve.Value;
            cachedSlomoRampDuration = SlomoRampDuration.Value;
            cachedSlomoReturnRampDuration = SlomoReturnRampDuration.Value;
            cachedEnableInfiniteTokens = EnableInfiniteTokens.Value;
            cachedDisableEncryptionNodes = DisableEncryptionNodes.Value;
            cachedDisableAllEncryptions = DisableAllEncryptions.Value;
        }

        // Cached accessors for hot-path usage (no .Value boxing on every frame)
        public float CachedMaxSlomo => cachedMaxSlomo;
        public float CachedSlomoWaitTime => cachedSlomoWaitTime;
        public float CachedSlomoScaleSpeed => cachedSlomoScaleSpeed;
        public float CachedSlomoReturnSpeed => cachedSlomoReturnSpeed;
        public bool CachedSlomoUseRamp => cachedSlomoUseRamp;
        public string CachedSlomoRampCurve => cachedSlomoRampCurve;
        public float CachedSlomoRampDuration => cachedSlomoRampDuration;
        public float CachedSlomoReturnRampDuration => cachedSlomoReturnRampDuration;
        public bool CachedSlomoAffectsAudio => true;
        public float CachedSlomoAudioPitchScale => 1f;
        public bool CachedSlomoAudioPreservePitch => false;
        public bool CachedSlomoAffectsAudioSpeed => false;
        public float CachedSlomoAudioSpeedScale => 1f;
        public string CachedSlomoAudioMode => "Both";
        public bool CachedEnableInfiniteTokens => cachedEnableInfiniteTokens;
        public bool CachedDisableEncryptionNodes => cachedDisableEncryptionNodes;
        public bool CachedDisableAllEncryptions => cachedDisableAllEncryptions;
        #endregion
    }
}
