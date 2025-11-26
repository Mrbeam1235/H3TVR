using BepInEx;
using BepInEx.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace H3TVR
{
    public class LioranBoardIntegration
    {
        private ManualLogSource logger;
        private string commandFilePath;
        private CancellationTokenSource cancellationTokenSource;
        private H3TVRImproved plugin;

        public void Initialize(ManualLogSource logSource, H3TVRImproved pluginInstance)
        {
            logger = logSource;
            plugin = pluginInstance;
            commandFilePath = Path.Combine(Paths.BepInExRootPath, "LioranBoard_H3TVR.txt");
            cancellationTokenSource = new CancellationTokenSource();

            logger.LogInfo("Initializing LioranBoard 2 Integration...");
            logger.LogInfo($"Watching for commands in: {commandFilePath}");

            // Ensure the file exists
            if (!File.Exists(commandFilePath))
            {
                File.WriteAllText(commandFilePath, "// H3TVR LioranBoard Integration Command File\n");
            }

            // Use a background task to poll for file changes
            Task.Run(() => WatchFileForChanges(cancellationTokenSource.Token));
        }

        private async Task WatchFileForChanges(CancellationToken token)
        {
            long lastSize = new FileInfo(commandFilePath).Length;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var currentSize = new FileInfo(commandFilePath).Length;
                    if (currentSize > lastSize)
                    {
                        ReadAndProcessCommands();
                        lastSize = currentSize;
                    }
                    else if (currentSize < lastSize)
                    {
                        // File has been cleared or shrunk
                        lastSize = currentSize;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error watching file: {ex.Message}");
                }
                await Task.Delay(500, token); // Check every 500ms
            }
        }

        private void ReadAndProcessCommands()
        {
            try
            {
                string content = File.ReadAllText(commandFilePath);
                // Clear the file to prevent reprocessing commands
                File.WriteAllText(commandFilePath, "");

                var commands = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

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
                // Armor Commands
                case "set_ally_armor":
                    if (int.TryParse(param, out int allyArmor))
                    {
                        SosigCustomizationUI.SetAllyArmor(allyArmor);
                        logger.LogInfo($"Ally armor set to {allyArmor}");
                    }
                    break;
                case "set_enemy_armor":
                    if (int.TryParse(param, out int enemyArmor))
                    {
                        SosigCustomizationUI.SetEnemyArmor(enemyArmor);
                        logger.LogInfo($"Enemy armor set to {enemyArmor}");
                    }
                    break;

                // Sosig Spawning
                case "spawn_ally":
                    sosigSpawner?.SpawnSosig(0, param ?? "LioranBoard Viewer");
                    logger.LogInfo($"Spawning ally sosig with name: {param ?? "LioranBoard Viewer"}");
                    break;
                case "spawn_enemy":
                    sosigSpawner?.SpawnSosig(1, param ?? "LioranBoard Viewer");
                    logger.LogInfo($"Spawning enemy sosig with name: {param ?? "LioranBoard Viewer"}");
                    break;
                case "clear_sosigs":
                    sosigSpawner?.ClearAllSosigs();
                    logger.LogInfo("Clearing all chat sosigs.");
                    break;

                // Boss Spawning
                case "spawn_boss":
                    var bossSpawner = sosigSpawner?.GetComponent<BossSosigSystem>();
                    if (bossSpawner != null)
                    {
                        bossSpawner.SpawnBossByName(param ?? "random");
                        logger.LogInfo($"Spawning boss: {param ?? "random"}");
                    }
                    break;
                case "clear_bosses":
                    var bossSpawnerClear = sosigSpawner?.GetComponent<BossSosigSystem>();
                    if (bossSpawnerClear != null)
                    {
                        bossSpawnerClear.ClearAllBosses();
                        logger.LogInfo("Clearing all bosses.");
                    }
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

                default:
                    logger.LogWarning($"Unknown LioranBoard command: {action}");
                    break;
            }
        }

        public void Shutdown()
        {
            cancellationTokenSource?.Cancel();
        }
    }
}
