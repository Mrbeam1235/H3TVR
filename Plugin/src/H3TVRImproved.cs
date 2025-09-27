using BepInEx;
using BepInEx.Configuration;
using FistVR;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Valve.VR;
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
        
        // Chat Sosig Configuration - Simplified
        private ConfigEntry<bool> enableTwitchChatSosigs;
        private ConfigEntry<string> twitchChatFilePath;
        private ConfigEntry<string> twitchEnemyChatFilePath;
        private ConfigEntry<int> maxChatSosigs;
        #endregion

        #region Components
        private SlomoMovementController slomoMovementController;
        private readonly Hooks hooks = new Hooks();
        private InputHandler inputHandler;
        private SpawnManager spawnManager;
        private EffectsManager effectsManager;
        private WeaponManager weaponManager;
        #endregion

        #region Initialization
        public H3TVRImproved()
        {
            hooks.Hook();
            Logger.LogInfo("Loading H3TVR Enhanced Edition");
            InitializeConfiguration();
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

            // Chat Sosig Configuration - Simplified
            enableTwitchChatSosigs = Config.Bind("Chat Sosigs", "EnableTwitchChatSosigs", true, "Enable Twitch chat sosig spawning");
            twitchChatFilePath = Config.Bind("Chat Sosigs", "TwitchChatFilePath", "ally_names.txt", "File path for ally sosig names");
            twitchEnemyChatFilePath = Config.Bind("Chat Sosigs", "TwitchEnemyChatFilePath", "enemy_names.txt", "File path for enemy sosig names");
            maxChatSosigs = Config.Bind("Chat Sosigs", "MaxChatSosigs", 10, "Maximum number of active chat sosigs");
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
                { "ArmorGUI", new KeyValuePair<KeyCode, string>(KeyCode.F6, "Open Armor Configuration GUI") }
            };

            foreach (var kvp in keyBindingConfigs)
            {
                keyBindings[kvp.Key] = Config.Bind("KeyBindings", $"KeyBindFor{kvp.Key}", 
                    kvp.Value.Key, kvp.Value.Value);
            }
        }

        public void Awake()
        {
            Harmony.CreateAndPatchAll(this.GetType());
            
            // Initialize components
            slomoMovementController = new SlomoMovementController();
            slomoMovementController.Initialize(slomoMovementScale.Value, slomoAffectsMovement.Value, Logger);
            
            inputHandler = gameObject.AddComponent<InputHandler>();
            spawnManager = gameObject.AddComponent<SpawnManager>();
            effectsManager = gameObject.AddComponent<EffectsManager>();
            weaponManager = gameObject.AddComponent<WeaponManager>();
            
            // Initialize components with dependencies
            InitializeComponents();
            
            // Initialize sosig spawner integration
            InitializeSosigSpawner();
            
            Logger.LogInfo("Successfully loaded H3TVR Enhanced Edition!");
        }

        private void InitializeComponents()
        {
            inputHandler.Initialize(keyBindings, this);
            spawnManager.Initialize(this, Logger);
            effectsManager.Initialize(this, slomoMovementController, Logger);
            weaponManager.Initialize(this, Logger);
        }

        private void InitializeSosigSpawner()
        {
            // Initialize the simplified Twitch Chat Sosig Manager
            GameObject sosigSpawnerObject = new GameObject("TwitchChatSosigManager");
            sosigSpawnerObject.transform.SetParent(transform);
            
            var twitchChatManager = sosigSpawnerObject.AddComponent<TwitchChatSosigManager>();
            twitchChatManager.Initialize(this, Logger);
            
            Logger.LogInfo("Simplified Twitch Chat Sosig Manager initialized!");
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
            switch (slomoStatus)
            {
                case "Slowing":
                    Logger.LogInfo("Slowing!");
                    effectsManager.SlomoScaleDown();
                    break;
                case "Wait":
                    Logger.LogInfo("Waiting!");
                    slomoStatus = "Paused";
                    StartCoroutine(effectsManager.SlomoWait(() => slomoStatus = "Return"));
                    break;
                case "Return":
                    Logger.LogInfo("Returning!");
                    effectsManager.SlomoReturn();
                    break;
            }

            if (Time.timeScale == 1)
            {
                slomoStatus = "Off";
                slomoMovementController?.UpdateMovementScale(Time.timeScale);
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
        public void TriggerSlomo() => slomoStatus = "Slowing";
        public void TriggerZeroGravity() => effectsManager.ZeroGravityBumpDown();
        public void ActivateMalfunctionBoost() => weaponManager.ActivateMalfunctionBoost(ref malfunctionBoostActive, ref malfunctionBoostEndTime);
        
        // Component access methods
        public SpawnManager GetSpawnManager() => spawnManager;
        public WeaponManager GetWeaponManager() => weaponManager;
        public EffectsManager GetEffectsManager() => effectsManager;
        
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
        
        // State setters
        public void SetSlomoStatus(string status) => slomoStatus = status;

        // Chat Sosig Configuration Access - Simplified
        public bool IsTwitchChatSosigsEnabled()
        {
            return enableTwitchChatSosigs?.Value ?? false;
        }

        public string GetTwitchChatFilePath()
        {
            return twitchChatFilePath?.Value ?? "";
        }

        public string GetTwitchEnemyChatFilePath()
        {
            return twitchEnemyChatFilePath?.Value ?? "";
        }

        public int GetMaxChatSosigs()
        {
            return maxChatSosigs?.Value ?? 10;
        }
        #endregion

        #region Harmony Patches and Cleanup
        [HarmonyPatch(typeof(AudioSource), "pitch", MethodType.Setter)]
        [HarmonyPrefix]
        public static void FixPitch(ref float value)
        {
            if (Time.timeScale != 1f)
            {
                value *= Time.timeScale;
            }
        }

        private void OnDestroy()
        {
            hooks.Unhook();
            slomoMovementController?.Reset();
        }
        #endregion
    }
}