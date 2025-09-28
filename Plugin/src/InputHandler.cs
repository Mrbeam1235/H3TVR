using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using BepInEx.Logging;
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
        private ManualLogSource logger;

        public void Initialize(Dictionary<string, ConfigEntry<KeyCode>> bindings, H3TVRImproved pluginInstance)
        {
            keyBindings = bindings;
            plugin = pluginInstance;
            logger = BepInEx.Logging.Logger.CreateLogSource("H3TVR-InputHandler");
            
            // Get component references
            spawnManager = plugin.GetSpawnManager();
            effectsManager = plugin.GetEffectsManager();
            weaponManager = plugin.GetWeaponManager();
        }

        void Update()
        {
            ProcessSpawnInputs();
            ProcessEffectInputs();
            ProcessWeaponInputs();
            ProcessUtilityInputs();
            ProcessChatSosigInputs();
        }

        private void ProcessSpawnInputs()
        {
            if (Input.GetKeyDown(keyBindings["SpawnWonderfulToy"].Value))
                spawnManager.SpawnWonderfulToy();
                
            if (Input.GetKeyDown(keyBindings["SpawnPillow"].Value))
                spawnManager.SpawnPillow();
                
            if (Input.GetKeyDown(keyBindings["SpawnFlash"].Value))
                spawnManager.SpawnFlash();
                
            if (Input.GetKey(keyBindings["SpawnShuri"].Value))
                spawnManager.SpawnShuri();
                
            if (Input.GetKeyDown(keyBindings["SpawnNadeRain"].Value))
                spawnManager.SpawnNadeRain();
                
            if (Input.GetKeyDown(keyBindings["SpawnHydration"].Value))
                spawnManager.SpawnHydration();
                
            if (Input.GetKeyDown(keyBindings["SpawnJeditToy"].Value))
                spawnManager.SpawnJeditToy();
                
            if (Input.GetKeyDown(keyBindings["SpawnSkittySubGun"].Value))
                spawnManager.SpawnSkittySubGun();
                
            if (Input.GetKeyDown(keyBindings["SpawnFlash2"].Value))
                spawnManager.SpawnFlash2();
                
            if (Input.GetKeyDown(keyBindings["SpawnSkittyBigGun"].Value))
                spawnManager.SpawnSkittyBigGun();
        }

        private void ProcessEffectInputs()
        {
            // Slomo input handling
            bool slomoTriggered = Input.GetKeyDown(keyBindings["TriggerSlomo"].Value);
            
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
                
            if (Input.GetKeyDown(keyBindings["TriggerZeroG"].Value))
                plugin.TriggerZeroGravity();
                
            if (Input.GetKey(keyBindings["DangerCloseBarrage"].Value))
                spawnManager.DangerCloseBarrage();
        }

        private void ProcessWeaponInputs()
        {
            if (Input.GetKeyDown(keyBindings["ToggleFireMode"].Value))
                weaponManager.ToggleHeldGunFireMode();
                
            if (Input.GetKeyDown(keyBindings["BoostMalfunction"].Value))
                plugin.ActivateMalfunctionBoost();
        }

        private void ProcessUtilityInputs()
        {
            if (Input.GetKeyDown(keyBindings["DestroyHeld"].Value))
                spawnManager.DestroyHeld();
                
            if (Input.GetKeyDown(keyBindings["DestroyQuickbelt"].Value))
                spawnManager.DestroyQuickbelt();
                
            if (Input.GetKeyDown(keyBindings["ShowStats"].Value))
            {
                // Show general stats
                logger.LogInfo("H3TVR Stats - Plugin is running");
            }
        }

        private void ProcessChatSosigInputs()
        {
            try
            {
                // Chat Sosig Controls - using EnhancedChatSpawner directly
                if (Input.GetKeyDown(keyBindings["SpawnChatSosigFriendly"].Value))
                {
                    spawnManager?.SpawnChatSosigFriendly();
                }
                
                if (Input.GetKeyDown(keyBindings["SpawnChatSosigEnemy"].Value))
                {
                    spawnManager?.SpawnChatSosigEnemy();
                }
                
                if (Input.GetKeyDown(keyBindings["ClearChatSosigs"].Value))
                {
                    spawnManager?.ClearAllChatSosigs();
                }
                
                if (Input.GetKeyDown(keyBindings["ChatSosigStats"].Value))
                {
                    ShowChatSosigStats();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Input handling error: {ex.Message}");
            }
        }

        private void ShowChatSosigStats()
        {
            try
            {
                var stats = spawnManager?.GetChatSosigStats();
                if (stats != null)
                {
                    string statsMessage = $"Chat Sosigs - Active: {stats.activeSosigCount} | " +
                                        $"Friendly: {stats.friendlyCount} | " +
                                        $"Enemy: {stats.enemyCount} | " +
                                        $"Queued: {stats.queuedSpawns}";
                    logger.LogInfo(statsMessage);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to show chat sosig stats: {ex.Message}");
            }
        }
    }
}