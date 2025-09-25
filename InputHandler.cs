using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using System;

namespace H3TVR
{
    /// <summary>
    /// Handles all input processing in a centralized, optimized manner with frame skipping
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        private Dictionary<string, ConfigEntry<KeyCode>> keyBindings;
        private H3TVRImproved plugin;
        private SpawnManager spawnManager;
        private EffectsManager effectsManager;
        private WeaponManager weaponManager;

        // Performance optimization: Frame skipping for non-critical inputs
        private int frameSkipCounter = 0;
        private const int FRAME_SKIP_INTERVAL = 3; // Process every 3rd frame for non-critical inputs
        
        // Cache for key states to avoid repeated Input.GetKey calls
        private readonly Dictionary<KeyCode, bool> keyStateCache = new Dictionary<KeyCode, bool>();
        private readonly Dictionary<KeyCode, bool> previousKeyStates = new Dictionary<KeyCode, bool>();

        public void Initialize(Dictionary<string, ConfigEntry<KeyCode>> bindings, H3TVRImproved pluginInstance)
        {
            keyBindings = bindings;
            plugin = pluginInstance;
            
            // Get component references
            spawnManager = GetComponent<SpawnManager>();
            effectsManager = GetComponent<EffectsManager>();
            weaponManager = GetComponent<WeaponManager>();
        }

        void Update()
        {
            // Always process critical inputs (effects) every frame
            ProcessCriticalInputs();
            
            // Use frame skipping for less critical actions
            frameSkipCounter++;
            if (frameSkipCounter >= FRAME_SKIP_INTERVAL)
            {
                frameSkipCounter = 0;
                ProcessNonCriticalInputs();
            }
        }

        private void ProcessCriticalInputs()
        {
            // These need immediate response - process every frame
            ProcessEffectInputs();
        }

        private void ProcessNonCriticalInputs()
        {
            // Cache key states to avoid multiple Input.GetKey calls
            UpdateKeyStateCache();
            
            ProcessSpawnInputs();
            ProcessWeaponInputs();
            ProcessUtilityInputs();
        }

        private void UpdateKeyStateCache()
        {
            // Store previous states and update current states
            foreach (var kvp in keyStateCache)
            {
                previousKeyStates[kvp.Key] = kvp.Value;
            }
            
            keyStateCache.Clear();
            
            // Only cache keys that we actually use
            foreach (var binding in keyBindings.Values)
            {
                KeyCode key = binding.Value;
                keyStateCache[key] = Input.GetKey(key);
            }
        }

        private bool GetKeyDown(KeyCode key)
        {
            bool currentState = keyStateCache.GetValueOrDefault(key, false);
            bool previousState = previousKeyStates.GetValueOrDefault(key, false);
            return currentState && !previousState;
        }

        private bool GetKey(KeyCode key)
        {
            return keyStateCache.GetValueOrDefault(key, false);
        }

        private void ProcessSpawnInputs()
        {
            if (GetKeyDown(keyBindings["WonderToy"].Value))
                spawnManager.SpawnWonderfulToy();
                
            if (GetKeyDown(keyBindings["Pillow"].Value))
                spawnManager.SpawnPillow();
                
            if (GetKeyDown(keyBindings["Flash"].Value))
                spawnManager.SpawnFlash();
                
            if (GetKey(keyBindings["Shuri"].Value))
                spawnManager.SpawnShuri();
                
            if (GetKeyDown(keyBindings["NadeRain"].Value))
                spawnManager.SpawnNadeRain();
                
            if (GetKeyDown(keyBindings["Hydration"].Value))
                spawnManager.SpawnHydration();
                
            if (GetKeyDown(keyBindings["JeditToy"].Value))
                spawnManager.SpawnJeditToy();
                
            if (GetKeyDown(keyBindings["SkittySubGun"].Value))
                spawnManager.SpawnSkittySubGun();
                
            if (GetKeyDown(keyBindings["Flash2"].Value))
                spawnManager.SpawnFlash2();
                
            if (GetKeyDown(keyBindings["SkittyBigGun"].Value))
                spawnManager.SpawnSkittyBigGun();
        }

        private void ProcessEffectInputs()
        {
            // Critical effects - process every frame for responsiveness
            bool slomoTriggered = Input.GetKeyDown(keyBindings["Slomo"].Value);
            
            // Check VR controller input for slomo
            if (!slomoTriggered) // Only check VR if keyboard wasn't pressed
            {
                bool vrEnabled;
                string vrButton;
                plugin.GetSlomoVRConfig(out vrEnabled, out vrButton);
                if (vrEnabled && effectsManager.CheckVRButtonPress(vrButton))
                {
                    slomoTriggered = true;
                }
            }
            
            if (slomoTriggered)
                plugin.TriggerSlomo();
                
            if (Input.GetKeyDown(keyBindings["ZeroGravity"].Value))
                plugin.TriggerZeroGravity();
                
            // DangerClose can be less responsive as it's a held action
            if (frameSkipCounter == 0 && GetKey(keyBindings["DangerClose"].Value))
                spawnManager.DangerCloseBarrage();
        }

        private void ProcessWeaponInputs()
        {
            if (GetKeyDown(keyBindings["ToggleFireMode"].Value))
                weaponManager.ToggleHeldGunFireMode();
                
            if (GetKeyDown(keyBindings["RandomizeHeldGun"].Value))
                weaponManager.RandomizeHeldGun();
                
            if (GetKeyDown(keyBindings["EmptyChamber"].Value))
                weaponManager.EmptyHeldGunChamber();
                
            if (GetKeyDown(keyBindings["BoostMalfunction"].Value))
                plugin.ActivateMalfunctionBoost();
        }

        private void ProcessUtilityInputs()
        {
            if (GetKeyDown(keyBindings["DestroyHeld"].Value))
                spawnManager.DestroyHeld();
                
            if (GetKeyDown(keyBindings["DestroyQuickbelt"].Value))
                spawnManager.DestroyQuickbelt();
                
            if (GetKeyDown(keyBindings["MeatHands"].Value))
                effectsManager.EnableMeatHands();
        }
    }

    // Extension method for Dictionary to provide GetValueOrDefault functionality
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
    }
}