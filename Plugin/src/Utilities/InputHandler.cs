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
            ProcessBossInputs();
            ProcessSteamFriendsInputs();
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
            
            // JerryAr mod spawns
            if (Input.GetKeyDown(keyBindings["SpawnAirStrike"].Value))
                spawnManager.SpawnAirStrikeGrenade();
            
            if (Input.GetKeyDown(keyBindings["SpawnTitanMachine"].Value))
                spawnManager.SpawnTitanMachine();
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
        
        private void ProcessSteamFriendsInputs()
        {
            try
            {
                var steamFriends = plugin?.GetSteamFriendsIntegration();
                if (steamFriends == null || !plugin.IsSteamFriendsEnabled())
                {
                    return; // Steam Friends integration not available
                }
                
                // Spawn random Steam friend as ally
                if (Input.GetKeyDown(keyBindings["SpawnSteamFriendAlly"].Value))
                {
                    steamFriends.SpawnSosigWithFriendName(true);
                    logger.LogInfo("Spawning Steam friend as ally");
                }
                
                // Spawn random Steam friend as enemy
                if (Input.GetKeyDown(keyBindings["SpawnSteamFriendEnemy"].Value))
                {
                    steamFriends.SpawnSosigWithFriendName(false);
                    logger.LogInfo("Spawning Steam friend as enemy");
                }
                
                // Spawn all Steam friends as allies
                if (Input.GetKeyDown(keyBindings["SpawnAllSteamFriendsAlly"].Value))
                {
                    steamFriends.SpawnAllFriendsAsSosigs(true);
                    logger.LogInfo("Spawning all Steam friends as allies");
                }
                
                // Spawn all Steam friends as enemies
                if (Input.GetKeyDown(keyBindings["SpawnAllSteamFriendsEnemy"].Value))
                {
                    steamFriends.SpawnAllFriendsAsSosigs(false);
                    logger.LogInfo("Spawning all Steam friends as enemies");
                }
                
                // Refresh Steam friends list
                if (Input.GetKeyDown(keyBindings["RefreshSteamFriends"].Value))
                {
                    steamFriends.RefreshFriendsList();
                    logger.LogInfo("Refreshing Steam friends list");
                }
                
                // Show Steam friends stats
                if (Input.GetKeyDown(keyBindings["SteamFriendsStats"].Value))
                {
                    logger.LogInfo(steamFriends.GetStatsInfo());
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Steam Friends input handling error: {ex.Message}");
            }
        }

        private void ProcessBossInputs()
        {
            try
            {
                // Random boss spawn
                if (Input.GetKeyDown(keyBindings["SpawnBossRandom"].Value))
                {
                    spawnManager?.SpawnBossSosig();
                }
                
                // Specific boss types
                if (Input.GetKeyDown(keyBindings["SpawnBossTank"].Value))
                {
                    spawnManager?.SpawnBossSosig(BossSosigSystem.BossType.Tank);
                }
                
                if (Input.GetKeyDown(keyBindings["SpawnBossBerserker"].Value))
                {
                    spawnManager?.SpawnBossSosig(BossSosigSystem.BossType.Berserker);
                }
                
                if (Input.GetKeyDown(keyBindings["SpawnBossSniper"].Value))
                {
                    spawnManager?.SpawnBossSosig(BossSosigSystem.BossType.Sniper);
                }
                
                if (Input.GetKeyDown(keyBindings["SpawnBossSummoner"].Value))
                {
                    spawnManager?.SpawnBossSosig(BossSosigSystem.BossType.Summoner);
                }
                
                if (Input.GetKeyDown(keyBindings["SpawnBossElite"].Value))
                {
                    spawnManager?.SpawnBossSosig(BossSosigSystem.BossType.Elite);
                }
                
                if (Input.GetKeyDown(keyBindings["SpawnBossJuggernaut"].Value))
                {
                    spawnManager?.SpawnBossSosig(BossSosigSystem.BossType.Juggernaut);
                }
                
                if (Input.GetKeyDown(keyBindings["SpawnBossAssassin"].Value))
                {
                    spawnManager?.SpawnBossSosig(BossSosigSystem.BossType.Assassin);
                }
                
                if (Input.GetKeyDown(keyBindings["SpawnBossCommander"].Value))
                {
                    spawnManager?.SpawnBossSosig(BossSosigSystem.BossType.Commander);
                }
                
                // Clear all bosses
                if (Input.GetKeyDown(keyBindings["ClearBosses"].Value))
                {
                    BossSosigSystem.ClearAllBosses();
                    logger.LogInfo("Cleared all boss sosigs");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Boss input handling error: {ex.Message}");
            }
        }
    }
}