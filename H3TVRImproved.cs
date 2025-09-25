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

        #region State Management - Performance Optimized
        private string slomoStatus = "Off";
        private string zeroGStatus = "Off";
        private bool malfunctionBoostActive;
        private float malfunctionBoostEndTime;
        
        // Performance optimization: Frame skipping for non-critical updates
        private int updateFrameCounter = 0;
        private const int STATE_CHECK_INTERVAL = 5; // Check state every 5 frames instead of every frame
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
            Logger.LogInfo("Loading H3TVR Performance Edition");
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
            // Shuriken configuration
            shurikenScale = Config.Bind("Shuriken", "Scale", 10f, "Scale multiplier for spawned shurikens");
            shurikenMinCount = Config.Bind("Shuriken", "MinCount", 15, "Minimum number of shurikens to spawn");
            shurikenMaxCount = Config.Bind("Shuriken", "MaxCount", 30, "Maximum number of shurikens to spawn");
            
            // Pillow configuration
            pillowMinCount = Config.Bind("Pillow", "MinCount", 1, "Minimum number of pillows to spawn");
            pillowMaxCount = Config.Bind("Pillow", "MaxCount", 3, "Maximum number of pillows to spawn");
            pillowGrenadeEnabled = Config.Bind("Pillow", "GrenadeEnabled", true, "Enable random grenade spawning with pillows");
            pillowGrenadeChance = Config.Bind("Pillow", "GrenadeChance", 0.1f, "Chance for grenade spawn with pillows");
            pillowGrenadeArmedChance = Config.Bind("Pillow", "GrenadeArmedChance", 0.1f, "Chance for armed grenades");
            pillowZeroGravityEnabled = Config.Bind("Pillow", "ZeroGravityEnabled", true, "Enable zero gravity with pillows");
            pillowZeroGravityChance = Config.Bind("Pillow", "ZeroGravityChance", 0.15f, "Chance for zero gravity activation");
            pillowZeroGravityDuration = Config.Bind("Pillow", "ZeroGravityDuration", 5f, "Duration for zero gravity effect");
            pillowSlomoEnabled = Config.Bind("Pillow", "SlomoEnabled", true, "Enable slow motion with pillows");
            pillowSlomoChance = Config.Bind("Pillow", "SlomoChance", 0.2f, "Chance for slow motion activation");
            pillowSlomoDuration = Config.Bind("Pillow", "SlomoDuration", 3f, "Duration for slow motion effect");
            
            // Danger Close configuration
            dangerCloseMinCount = Config.Bind("DangerClose", "MinCount", 1, "Minimum danger close rounds per barrage");
            dangerCloseMaxCount = Config.Bind("DangerClose", "MaxCount", 5, "Maximum danger close rounds per barrage");
        }

        private void InitializeKeyBindings()
        {
            var keyBindingConfigs = new Dictionary<string, KeyValuePair<KeyCode, string>>
            {
                { "WonderToy", new KeyValuePair<KeyCode, string>(KeyCode.Keypad0, "Spawn WonderToy") },
                { "Pillow", new KeyValuePair<KeyCode, string>(KeyCode.Keypad1, "Spawn Pillow") },
                { "Flash", new KeyValuePair<KeyCode, string>(KeyCode.Keypad2, "Spawn Flash") },
                { "Shuri", new KeyValuePair<KeyCode, string>(KeyCode.Keypad3, "Spawn Shuriken") },
                { "NadeRain", new KeyValuePair<KeyCode, string>(KeyCode.Keypad4, "Spawn Nade Rain") },
                { "Hydration", new KeyValuePair<KeyCode, string>(KeyCode.Keypad5, "Spawn Hydration") },
                { "JeditToy", new KeyValuePair<KeyCode, string>(KeyCode.Keypad6, "Spawn Jedit Toy") },
                { "Slomo", new KeyValuePair<KeyCode, string>(KeyCode.Keypad7, "Trigger Slomo") },
                { "DestroyHeld", new KeyValuePair<KeyCode, string>(KeyCode.Keypad8, "Destroy held object") },
                { "SkittySubGun", new KeyValuePair<KeyCode, string>(KeyCode.Keypad9, "Spawn Skitty Sub Gun") },
                { "ZeroGravity", new KeyValuePair<KeyCode, string>(KeyCode.KeypadMinus, "Toggle Zero Gravity") },
                { "MeatHands", new KeyValuePair<KeyCode, string>(KeyCode.KeypadPlus, "Enable Meat Hands") },
                { "DangerClose", new KeyValuePair<KeyCode, string>(KeyCode.F1, "Danger Close Barrage") },
                { "Flash2", new KeyValuePair<KeyCode, string>(KeyCode.F2, "Spawn Flash2") },
                { "DestroyQuickbelt", new KeyValuePair<KeyCode, string>(KeyCode.F3, "Destroy Quickbelt") },
                { "SkittyBigGun", new KeyValuePair<KeyCode, string>(KeyCode.F4, "Spawn Skitty Big Gun") },
                { "ToggleFireMode", new KeyValuePair<KeyCode, string>(KeyCode.F6, "Toggle held gun fire mode") },
                { "RandomizeHeldGun", new KeyValuePair<KeyCode, string>(KeyCode.F7, "Randomize held gun") },
                { "EmptyChamber", new KeyValuePair<KeyCode, string>(KeyCode.F8, "Empty held gun chamber") },
                { "BoostMalfunction", new KeyValuePair<KeyCode, string>(KeyCode.F9, "Boost Meatyceiver malfunction") }
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
            
            Logger.LogInfo("Successfully loaded H3TVR Performance Edition!");
        }

        private void InitializeComponents()
        {
            inputHandler.Initialize(keyBindings, this);
            spawnManager.Initialize(this, Logger);
            effectsManager.Initialize(this, slomoMovementController, Logger);
            weaponManager.Initialize(this, Logger);
        }
        #endregion

        #region Update Loop - Performance Optimized
        public void Update()
        {
            updateFrameCounter++;
            
            // Handle critical slomo state machine every frame for responsiveness
            if (slomoStatus != "Off" && slomoStatus != "Paused")
            {
                HandleSlomoStateMachine();
            }
            
            // Handle zero gravity state machine - less frequent checks
            if (updateFrameCounter % STATE_CHECK_INTERVAL == 0)
            {
                if (zeroGStatus == "On" || zeroGStatus == "Falling")
                {
                    HandleZeroGravityStateMachine();
                }
                
                // Handle malfunction boost - only when active
                if (malfunctionBoostActive)
                {
                    HandleMalfunctionBoost();
                }
            }
            
            // Reset counter to prevent overflow
            if (updateFrameCounter >= 1000)
                updateFrameCounter = 0;
            
            // Input handling is delegated to InputHandler component
        }

        private void HandleSlomoStateMachine()
        {
            switch (slomoStatus)
            {
                case "Slowing":
                    effectsManager.SlomoScaleDown();
                    break;
                case "Wait":
                    slomoStatus = "Paused";
                    StartCoroutine(effectsManager.SlomoWait(() => slomoStatus = "Return"));
                    break;
                case "Return":
                    effectsManager.SlomoReturn();
                    break;
            }

            // Only check timeScale once per frame and update status accordingly
            if (Time.timeScale >= 0.99f) // Use threshold instead of exact equality
            {
                if (slomoStatus != "Off")
                {
                    slomoStatus = "Off";
                    slomoMovementController?.UpdateMovementScale(1f);
                }
            }
        }

        private void HandleZeroGravityStateMachine()
        {
            switch (zeroGStatus)
            {
                case "On":
                    zeroGStatus = "Processing"; // Prevent multiple coroutine starts
                    StartCoroutine(effectsManager.ZeroGWait(() => {
                        zeroGStatus = "Falling";
                        effectsManager.RealisticFall();
                    }));
                    break;
                case "Falling":
                    zeroGStatus = "ProcessingFall"; // Prevent multiple coroutine starts
                    StartCoroutine(effectsManager.RealisticFallWait(() => {
                        effectsManager.ZeroGravityBumpUp();
                        zeroGStatus = "Off";
                    }));
                    break;
            }
        }

        private void HandleMalfunctionBoost()
        {
            if (Time.time >= malfunctionBoostEndTime)
            {
                malfunctionBoostActive = false;
                Logger.LogInfo("Meatyceiver malfunction boost ended.");
            }
            else
            {
                // Only apply malfunction logic every few frames to reduce overhead
                if (updateFrameCounter % 10 == 0) // Every 10 frames
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