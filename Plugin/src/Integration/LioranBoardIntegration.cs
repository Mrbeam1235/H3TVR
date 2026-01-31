using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using FistVR;

namespace H3TVR
{
    public class LioranBoardIntegration : MonoBehaviour
    {
        private ManualLogSource logger;
        private string commandFilePath;
        private H3TVRImproved plugin;
        private bool isWatching = false;
        private long lastFileSize = 0;

        public void Initialize(ManualLogSource logSource, H3TVRImproved pluginInstance)
        {
            logger = logSource;
            plugin = pluginInstance;
            commandFilePath = Path.Combine(Paths.BepInExRootPath, "LioranBoard_H3TVR.txt");

            logger.LogInfo("Initializing LioranBoard 2 Integration...");
            logger.LogInfo($"Watching for commands in: {commandFilePath}");

            // Ensure the file exists
            if (!File.Exists(commandFilePath))
            {
                File.WriteAllText(commandFilePath, "// H3TVR LioranBoard Integration Command File\n");
            }

            // Start file watching coroutine
            isWatching = true;
            StartCoroutine(WatchFileCoroutine());
        }

        private IEnumerator WatchFileCoroutine()
        {
            if (File.Exists(commandFilePath))
            {
                lastFileSize = new FileInfo(commandFilePath).Length;
            }

            while (isWatching)
            {
                yield return new WaitForSeconds(0.5f);

                try
                {
                    if (File.Exists(commandFilePath))
                    {
                        var currentSize = new FileInfo(commandFilePath).Length;
                        if (currentSize > lastFileSize)
                        {
                            ReadAndProcessCommands();
                            lastFileSize = currentSize;
                        }
                        else if (currentSize < lastFileSize)
                        {
                            // File has been cleared or shrunk
                            lastFileSize = currentSize;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error watching file: {ex.Message}");
                }
            }
        }

        private void ReadAndProcessCommands()
        {
            try
            {
                string content = File.ReadAllText(commandFilePath);
                // Clear the file to prevent reprocessing commands
                File.WriteAllText(commandFilePath, "");

                // Use semicolon as a command delimiter for easier LioranBoard setup
                var commands = content.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var command in commands)
                {
                    if (command.StartsWith("//")) continue; // Ignore comments

                    var parts = command.Trim().Split(new[] { ' ' }, 2);
                    if (parts.Length == 0) continue;

                    string action = parts[0].ToLower();
                    string param = parts.Length > 1 ? parts[1] : null;

                    logger.LogInfo($"Processing LioranBoard command: {action} {param}");

                    // Use a dispatcher to run game-related logic on the main thread
                    MainThreadUtil.Run(() => ProcessCommand(action, param));
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing LioranBoard commands: {ex.Message}");
            }
        }

        private void ProcessCommand(string action, string param)
        {
            if (plugin == null)
            {
                logger.LogError("LioranBoardIntegration: H3TVRImproved plugin instance is null!");
                return;
            }

            var sosigSpawner = plugin.GetAdvancedChatSpawner();
            var spawnManager = plugin.GetSpawnManager();

            switch (action)
            {
                // ============== UNIFIED ARMOR COMMAND ==============
                // Single command works both ways:
                //   armor heavy              ? Set global armor to heavy
                //   armor ViewerName heavy   ? Set ViewerName's armor to heavy
                //   armor ViewerName 3       ? Set ViewerName's armor to level 3
                case "armor":
                case "!armor":
                    HandleArmorCommand(param);
                    break;

                // Legacy armor commands (still supported for backwards compatibility)
                case "set_ally_armor":
                    if (int.TryParse(param, out int allyArmor))
                    {
                        SosigCustomizationUI.SetAllyArmor(allyArmor);
                        SosigArmorManager.SetGlobalDefaults(allyArmor, SosigCustomizationUI.EnemyArmor.Value);
                        logger.LogInfo($"Ally armor set to {SosigArmorManager.GetArmorName(allyArmor)}");
                    }
                    break;
                case "set_enemy_armor":
                    if (int.TryParse(param, out int enemyArmor))
                    {
                        SosigCustomizationUI.SetEnemyArmor(enemyArmor);
                        SosigArmorManager.SetGlobalDefaults(SosigCustomizationUI.AllyArmor.Value, enemyArmor);
                        logger.LogInfo($"Enemy armor set to {SosigArmorManager.GetArmorName(enemyArmor)}");
                    }
                    break;

                // Clear all armor preferences
                case "clear_armor":
                case "reset_armor":
                    SosigArmorManager.ClearAllPreferences();
                    SosigArmorManager.SetGlobalDefaults(0, 0);
                    SosigCustomizationUI.SetAllyArmor(0);
                    SosigCustomizationUI.SetEnemyArmor(0);
                    logger.LogInfo("[ARMOR] All armor preferences cleared");
                    break;

                // Sosig Spawning
                case "spawn_ally":
                    sosigSpawner?.SpawningSequence(param ?? "LioranBoard Viewer");
                    logger.LogInfo($"Spawning ally sosig with name: {param ?? "LioranBoard Viewer"}");
                    break;
                case "spawn_enemy":
                    sosigSpawner?.SpawningSequenceEnemy(1, param ?? "LioranBoard Viewer");
                    logger.LogInfo($"Spawning enemy sosig with name: {param ?? "LioranBoard Viewer"}");
                    break;
                case "clear_sosigs":
                    sosigSpawner?.ClearAllSosigs();
                    logger.LogInfo("Clearing all chat sosigs.");
                    break;

                // Boss Spawning
                case "spawn_boss":
                    if (spawnManager != null)
                    {
                        spawnManager.SpawnBossSosig();
                        logger.LogInfo($"Spawning boss: {param ?? "random"}");
                    }
                    break;
                case "clear_bosses":
                    BossSosigSystem.ClearAllBosses();
                    logger.LogInfo("Clearing all bosses.");
                    break;

                // Item/Effect Spawning
                case "spawn_item":
                    if (spawnManager != null && !string.IsNullOrEmpty(param))
                    {
                        switch (param.ToLower())
                        {
                            case "wonderful_toy": spawnManager.SpawnWonderfulToy(); break;
                            case "jedit_toy": spawnManager.SpawnJeditToy(); break;
                            case "hydration": spawnManager.SpawnHydration(); break;
                            case "pillow": spawnManager.SpawnPillow(); break;
                            case "shuriken": spawnManager.SpawnShuri(); break;
                            case "flash": spawnManager.SpawnFlash(); break;
                            case "flash2": spawnManager.SpawnFlash2(); break;
                            default: logger.LogWarning($"Unknown item to spawn: {param}"); break;
                        }
                        logger.LogInfo($"Spawning item: {param}");
                    }
                    break;
                case "grenade_rain":
                    spawnManager?.SpawnNadeRain();
                    logger.LogInfo("Triggering grenade rain.");
                    break;
                case "danger_close":
                    spawnManager?.DangerCloseBarrage();
                    logger.LogInfo("Triggering Danger Close barrage.");
                    break;

                // Global Effects
                case "slomo":
                    plugin.TriggerSlomo();
                    logger.LogInfo("Triggering slow motion.");
                    break;
                case "zero_g":
                    plugin.TriggerZeroGravity();
                    logger.LogInfo("Triggering zero gravity.");
                    break;

                // Ally Commands
                case "allies_follow":
                    var spawnerFollow = plugin.GetAdvancedChatSpawner();
                    if (spawnerFollow != null)
                    {
                        var behaviorController = spawnerFollow.GetComponent<SosigBehaviorController>();
                        if (behaviorController != null)
                        {
                            behaviorController.CommandAlliesFollowPlayer();
                        }
                    }
                    logger.LogInfo("Commanding allies to follow player.");
                    break;
                case "allies_defend":
                    var spawnerDefend = plugin.GetAdvancedChatSpawner();
                    if (spawnerDefend != null && GM.CurrentPlayerBody?.Head != null)
                    {
                        var behaviorController = spawnerDefend.GetComponent<SosigBehaviorController>();
                        if (behaviorController != null)
                        {
                            behaviorController.CommandAlliesDefendPoint(GM.CurrentPlayerBody.Head.position);
                        }
                    }
                    logger.LogInfo("Commanding allies to defend current position.");
                    break;
                case "allies_attack":
                    var spawnerAttack = plugin.GetAdvancedChatSpawner();
                    if (spawnerAttack != null && GM.CurrentPlayerBody?.Head != null)
                    {
                        var behaviorController = spawnerAttack.GetComponent<SosigBehaviorController>();
                        if (behaviorController != null)
                        {
                            // Attack towards player's look direction
                            var attackPoint = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 10f;
                            behaviorController.CommandAlliesAttackTarget(attackPoint);
                        }
                    }
                    logger.LogInfo("Commanding allies to attack forward.");
                    break;
                case "allies_hold_fire":
                    var spawnerHold = plugin.GetAdvancedChatSpawner();
                    if (spawnerHold != null)
                    {
                        var behaviorController = spawnerHold.GetComponent<SosigBehaviorController>();
                        if (behaviorController != null)
                        {
                            bool holdFire = param?.ToLower() == "true" || param == "1";
                            behaviorController.SetAlliesHoldFire(holdFire);
                        }
                    }
                    logger.LogInfo($"Allies hold fire: {param}");
                    break;

                default:
                    logger.LogWarning($"Unknown LioranBoard command: {action}");
                    break;
            }
        }

        /// <summary>
        /// Unified armor command handler
        /// Formats:
        ///   armor heavy              ? Set global armor
        ///   armor 3                  ? Set global armor by number
        ///   armor ViewerName heavy   ? Set specific user's armor
        ///   armor ViewerName 3       ? Set specific user's armor by number
        /// </summary>
        private void HandleArmorCommand(string param)
        {
            if (string.IsNullOrEmpty(param))
            {
                logger.LogInfo("[ARMOR] Usage: armor <level> OR armor <username> <level>");
                logger.LogInfo("[ARMOR] Levels: none/light/medium/heavy/tank/god or 0-5");
                return;
            }

            string[] parts = param.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 1)
            {
                // Single parameter: "armor heavy" or "armor 3" ? Global armor
                if (TryParseArmorLevel(parts[0], out int level, out string name))
                {
                    SosigCustomizationUI.SetAllyArmor(level);
                    SosigCustomizationUI.SetEnemyArmor(level);
                    SosigArmorManager.SetGlobalDefaults(level, level);
                    logger.LogInfo($"[ARMOR] Global armor set to {name} (level {level})");
                }
                else
                {
                    logger.LogWarning($"[ARMOR] Unknown armor level: {parts[0]}");
                }
            }
            else if (parts.Length >= 2)
            {
                // Two parameters: Could be "username level" or check if first is a level
                // First try: Is the first word an armor level? (e.g., "armor heavy" with extra text)
                if (TryParseArmorLevel(parts[0], out int globalLevel, out string globalName))
                {
                    // First word is armor level - treat as global
                    SosigCustomizationUI.SetAllyArmor(globalLevel);
                    SosigCustomizationUI.SetEnemyArmor(globalLevel);
                    SosigArmorManager.SetGlobalDefaults(globalLevel, globalLevel);
                    logger.LogInfo($"[ARMOR] Global armor set to {globalName} (level {globalLevel})");
                }
                else
                {
                    // First word is NOT an armor level - treat as username
                    string username = parts[0];
                    string armorValue = parts[1];
                    
                    if (TryParseArmorLevel(armorValue, out int userLevel, out string userName))
                    {
                        SosigArmorManager.SetUserArmorPreference(username, userLevel);
                        logger.LogInfo($"[ARMOR] {username}'s armor set to {userName} (level {userLevel})");
                    }
                    else
                    {
                        logger.LogWarning($"[ARMOR] Unknown armor level: {armorValue}");
                    }
                }
            }
        }

        /// <summary>
        /// Try to parse an armor level from string (name or number)
        /// </summary>
        private bool TryParseArmorLevel(string value, out int level, out string name)
        {
            level = 0;
            name = "None";

            if (string.IsNullOrEmpty(value)) return false;

            // Try parse as number first
            if (int.TryParse(value, out level))
            {
                level = Mathf.Clamp(level, 0, 5);
                name = SosigArmorManager.GetArmorName(level);
                return true;
            }

            // Parse by name
            switch (value.ToLower())
            {
                case "none": case "off": case "naked": case "n":
                    level = 0; name = "None"; return true;
                case "light": case "l":
                    level = 1; name = "Light"; return true;
                case "medium": case "med": case "m":
                    level = 2; name = "Medium"; return true;
                case "heavy": case "h":
                    level = 3; name = "Heavy"; return true;
                case "tank": case "juggernaut": case "jug": case "t":
                    level = 4; name = "Tank"; return true;
                case "god": case "godmode": case "immortal": case "g":
                    level = 5; name = "God"; return true;
                default:
                    return false;
            }
        }

        public void Shutdown()
        {
            isWatching = false;
            StopAllCoroutines();
        }
    }
}
