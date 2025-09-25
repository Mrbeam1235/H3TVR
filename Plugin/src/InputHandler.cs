using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using System;

namespace H3TVR
{
    /// <summary>
    /// Handles all input processing in a centralized, organized manner
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        private Dictionary<string, ConfigEntry<KeyCode>> keyBindings;
        private H3TVRImproved plugin;
        private SpawnManager spawnManager;
        private EffectsManager effectsManager;
        private WeaponManager weaponManager;

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
            ProcessSpawnInputs();
            ProcessEffectInputs();
            ProcessWeaponInputs();
            ProcessUtilityInputs();
        }

        private void ProcessSpawnInputs()
        {
            if (Input.GetKeyDown(keyBindings["WonderToy"].Value))
                spawnManager.SpawnWonderfulToy();
                
            if (Input.GetKeyDown(keyBindings["Pillow"].Value))
                spawnManager.SpawnPillow();
                
            if (Input.GetKeyDown(keyBindings["Flash"].Value))
                spawnManager.SpawnFlash();
                
            if (Input.GetKey(keyBindings["Shuri"].Value))
                spawnManager.SpawnShuri();
                
            if (Input.GetKeyDown(keyBindings["NadeRain"].Value))
                spawnManager.SpawnNadeRain();
                
            if (Input.GetKeyDown(keyBindings["Hydration"].Value))
                spawnManager.SpawnHydration();
                
            if (Input.GetKeyDown(keyBindings["JeditToy"].Value))
                spawnManager.SpawnJeditToy();
                
            if (Input.GetKeyDown(keyBindings["SkittySubGun"].Value))
                spawnManager.SpawnSkittySubGun();
                
            if (Input.GetKeyDown(keyBindings["Flash2"].Value))
                spawnManager.SpawnFlash2();
                
            if (Input.GetKeyDown(keyBindings["SkittyBigGun"].Value))
                spawnManager.SpawnSkittyBigGun();
        }

        private void ProcessEffectInputs()
        {
            // Slomo input handling
            bool slomoTriggered = Input.GetKeyDown(keyBindings["Slomo"].Value);
            
            // Check VR controller input for slomo
            bool vrEnabled;
            string vrButton;
            plugin.GetSlomoVRConfig(out vrEnabled, out vrButton);
            if (vrEnabled && effectsManager.CheckVRButtonPress(vrButton))
            {
                slomoTriggered = true;
            }
            
            if (slomoTriggered)
                plugin.TriggerSlomo();
                
            if (Input.GetKeyDown(keyBindings["ZeroGravity"].Value))
                plugin.TriggerZeroGravity();
                
            if (Input.GetKey(keyBindings["DangerClose"].Value))
                spawnManager.DangerCloseBarrage();
        }

        private void ProcessWeaponInputs()
        {
            if (Input.GetKeyDown(keyBindings["ToggleFireMode"].Value))
                weaponManager.ToggleHeldGunFireMode();
                
            if (Input.GetKeyDown(keyBindings["RandomizeHeldGun"].Value))
                weaponManager.RandomizeHeldGun();
                
            if (Input.GetKeyDown(keyBindings["EmptyChamber"].Value))
                weaponManager.EmptyHeldGunChamber();
                
            if (Input.GetKeyDown(keyBindings["BoostMalfunction"].Value))
                plugin.ActivateMalfunctionBoost();
        }

        private void ProcessUtilityInputs()
        {
            if (Input.GetKeyDown(keyBindings["DestroyHeld"].Value))
                spawnManager.DestroyHeld();
                
            if (Input.GetKeyDown(keyBindings["DestroyQuickbelt"].Value))
                spawnManager.DestroyQuickbelt();
                
            if (Input.GetKeyDown(keyBindings["MeatHands"].Value))
                effectsManager.EnableMeatHands();
        }

        private void ProcessChatSosigInputs()
        {
            try
            {
                // Chat Sosig Controls
                if (Input.GetKeyDown(keyBindings["SpawnChatSosigFriendly"].Value))
                {
                    plugin.GetSpawnManager()?.SpawnChatSosigFriendly();
                }
                
                if (Input.GetKeyDown(keyBindings["SpawnChatSosigEnemy"].Value))
                {
                    plugin.GetSpawnManager()?.SpawnChatSosigEnemy();
                }
                
                if (Input.GetKeyDown(keyBindings["CycleChatSosigArmor"].Value))
                {
                    // This will be handled by the TwitchChatSosigManager directly
                    // The key binding is processed there
                }
                
                if (Input.GetKeyDown(keyBindings["ClearChatSosigs"].Value))
                {
                    plugin.GetSpawnManager()?.ClearAllChatSosigs();
                }
                
                if (Input.GetKeyDown(keyBindings["ChatSosigStats"].Value))
                {
                    ShowChatSosigStats();
                }
                
                // ...existing input handling...
            }
            catch (Exception ex)
            {
                Logger.LogError($"Input handling error: {ex.Message}");
            }
        }

        private void ShowChatSosigStats()
        {
            try
            {
                var stats = plugin.GetSpawnManager()?.GetChatSosigStats();
                if (stats != null)
                {
                    string statsMessage = $"Chat Sosigs - Active: {stats.activeSosigCount} | " +
                                        $"Friendly: {stats.friendlyCount} | " +
                                        $"Enemy: {stats.enemyCount} | " +
                                        $"Queued: {stats.queuedSpawns}";
                    Logger.LogInfo(statsMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to show chat sosig stats: {ex.Message}");
            }
        }
    }
}