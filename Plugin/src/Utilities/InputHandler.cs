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
        private AudioManager audioManager;

        public void Initialize(Dictionary<string, ConfigEntry<KeyCode>> bindings, H3TVRImproved pluginInstance)
        {
            keyBindings = bindings;
            plugin = pluginInstance;
            logger = BepInEx.Logging.Logger.CreateLogSource("H3TVR-InputHandler");
            
            // Get component references
            spawnManager = plugin.GetSpawnManager();
            effectsManager = plugin.GetEffectsManager();
            weaponManager = plugin.GetWeaponManager();
            audioManager = plugin.GetAudioManager();
        }

        void Update()
        {
            ProcessSpawnInputs();
            ProcessEffectInputs();
            ProcessWeaponInputs();
            ProcessUtilityInputs();
            ProcessSteamFriendsInputs();
        }

        private void ProcessSpawnInputs()
        {
            try
            {
                if (keyBindings.ContainsKey("SpawnWonderfulToy") && Input.GetKeyDown(keyBindings["SpawnWonderfulToy"].Value))
                    spawnManager?.SpawnWonderfulToy();
                    
                if (keyBindings.ContainsKey("SpawnPillow") && Input.GetKeyDown(keyBindings["SpawnPillow"].Value))
                    spawnManager?.SpawnPillow();
                    
                if (keyBindings.ContainsKey("SpawnFlash") && Input.GetKeyDown(keyBindings["SpawnFlash"].Value))
                    spawnManager?.SpawnFlash();
                    
                if (keyBindings.ContainsKey("SpawnShuri") && Input.GetKey(keyBindings["SpawnShuri"].Value))
                    spawnManager?.SpawnShuri();
                    
                if (keyBindings.ContainsKey("SpawnNadeRain") && Input.GetKeyDown(keyBindings["SpawnNadeRain"].Value))
                    spawnManager?.SpawnNadeRain();
                    
                if (keyBindings.ContainsKey("SpawnHydration") && Input.GetKeyDown(keyBindings["SpawnHydration"].Value))
                    spawnManager?.SpawnHydration();
                    
                if (keyBindings.ContainsKey("SpawnJeditToy") && Input.GetKeyDown(keyBindings["SpawnJeditToy"].Value))
                {
                    spawnManager?.SpawnJeditToy();
                    audioManager?.PlayJeditoySound();
                }
                    
                if (keyBindings.ContainsKey("SpawnSkittySubGun") && Input.GetKeyDown(keyBindings["SpawnSkittySubGun"].Value))
                    spawnManager?.SpawnSkittySubGun();
                    
                if (keyBindings.ContainsKey("SpawnFlash2") && Input.GetKeyDown(keyBindings["SpawnFlash2"].Value))
                    spawnManager?.SpawnFlash2();
                    
                if (keyBindings.ContainsKey("SpawnSkittyBigGun") && Input.GetKeyDown(keyBindings["SpawnSkittyBigGun"].Value))
                    spawnManager?.SpawnSkittyBigGun();
                
                if (keyBindings.ContainsKey("SpawnAirStrike") && Input.GetKeyDown(keyBindings["SpawnAirStrike"].Value))
                    spawnManager?.SpawnAirStrikeGrenade();
                
                if (keyBindings.ContainsKey("SpawnTitanMachine") && Input.GetKeyDown(keyBindings["SpawnTitanMachine"].Value))
                    spawnManager?.SpawnTitanMachine();
                
                if (keyBindings.ContainsKey("SpawnNuke") && Input.GetKeyDown(keyBindings["SpawnNuke"].Value))
                    spawnManager?.SpawnNuke();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Spawn input error: {ex.Message}");
            }
        }

        private void ProcessEffectInputs()
        {
            try
            {
                // Slomo input handling
                bool slomoTriggered = keyBindings.ContainsKey("TriggerSlomo") && Input.GetKeyDown(keyBindings["TriggerSlomo"].Value);
                
                // Check VR controller input for slomo
                bool vrEnabled;
                string vrButton;
                plugin.GetSlomoVRConfig(out vrEnabled, out vrButton);
                if (vrEnabled && effectsManager?.CheckVRButtonPress(vrButton) == true)
                {
                    slomoTriggered = true;
                }
                
                if (slomoTriggered)
                    plugin?.TriggerSlomo();
                    
                if (keyBindings.ContainsKey("TriggerZeroG") && Input.GetKeyDown(keyBindings["TriggerZeroG"].Value))
                    plugin?.TriggerZeroGravity();
                    
                if (keyBindings.ContainsKey("DangerCloseBarrage") && Input.GetKey(keyBindings["DangerCloseBarrage"].Value))
                    spawnManager?.DangerCloseBarrage();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Effect input error: {ex.Message}");
            }
        }

        private void ProcessWeaponInputs()
        {
            try
            {
                if (keyBindings.ContainsKey("ToggleFireMode") && Input.GetKeyDown(keyBindings["ToggleFireMode"].Value))
                    weaponManager?.ToggleHeldGunFireMode();
                    
                if (keyBindings.ContainsKey("BoostMalfunction") && Input.GetKeyDown(keyBindings["BoostMalfunction"].Value))
                    plugin?.ActivateMalfunctionBoost();
                    
                if (keyBindings.ContainsKey("EmptyHeldGunChamber") && Input.GetKeyDown(keyBindings["EmptyHeldGunChamber"].Value))
                    weaponManager?.EmptyHeldGunChamber();

                if (keyBindings.ContainsKey("SwapHeldGun") && Input.GetKeyDown(keyBindings["SwapHeldGun"].Value))
                    weaponManager?.SwapHeldGun();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Weapon input error: {ex.Message}");
            }
        }

        private void ProcessUtilityInputs()
        {
            try
            {
                if (keyBindings.ContainsKey("DestroyHeld") && Input.GetKeyDown(keyBindings["DestroyHeld"].Value))
                    spawnManager?.DestroyHeld();
                    
                if (keyBindings.ContainsKey("DestroyQuickbelt") && Input.GetKeyDown(keyBindings["DestroyQuickbelt"].Value))
                    spawnManager?.DestroyQuickbelt();
                    
                if (keyBindings.ContainsKey("ShowStats") && Input.GetKeyDown(keyBindings["ShowStats"].Value))
                {
                    logger?.LogInfo("H3TVR Stats - Plugin is running");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Utility input error: {ex.Message}");
            }
        }

        private void ProcessSteamFriendsInputs()
        {
            try
            {
                var steamFriends = plugin?.GetSteamFriendsIntegration();
                if (steamFriends == null || !plugin.IsSteamFriendsEnabled())
                {
                    return;
                }
                
                if (keyBindings.ContainsKey("RefreshSteamFriends") && Input.GetKeyDown(keyBindings["RefreshSteamFriends"].Value))
                {
                    steamFriends.RefreshFriendsList();
                    logger?.LogInfo("Refreshing Steam friends list");
                }
                
                if (keyBindings.ContainsKey("SteamFriendsStats") && Input.GetKeyDown(keyBindings["SteamFriendsStats"].Value))
                {
                    logger?.LogInfo(steamFriends.GetStatsInfo());
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Steam Friends input error: {ex.Message}");
            }
        }
    }
}