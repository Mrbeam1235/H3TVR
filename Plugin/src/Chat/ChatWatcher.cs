using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Chat Watcher - H3TwitchTools compatible file-based chat monitor
    /// Watches text files for Twitch chat usernames and spawns sosigs accordingly
    /// Compatible with OBS, Streamlabs, Channel Points, and other streaming software integrations
    /// 
    /// SUPPORTED FILE FORMATS:
    /// 1. Plain username per line: "ViewerName"
    /// 2. Key=Value format: "username=ViewerName" or "user=ViewerName" or "redeemer=ViewerName"
    /// 3. Multiple formats on same line: "ViewerName redeemed!" extracts "ViewerName"
    /// </summary>
    public class ChatWatcher : MonoBehaviour
    {
        #region Static Instance
        public static ChatWatcher Instance { get; private set; }
        #endregion

        #region Core Components
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        private AdvancedChatSosigSpawner sosigSpawner;
        #endregion

        #region Configuration
        private ConfigEntry<bool> enableFileWatching;
        private ConfigEntry<string> allyChatFilePath;
        private ConfigEntry<string> enemyChatFilePath;
        private ConfigEntry<float> fileCheckInterval;
        private ConfigEntry<bool> clearFileAfterRead;
        private ConfigEntry<KeyCode> manualAllySpawnKey;
        private ConfigEntry<KeyCode> manualEnemySpawnKey;
        private ConfigEntry<KeyCode> clearAllSosigsKey;
        #endregion

        #region File Watching State
        private float lastFileCheckTime;
        private string lastAllyFileContent = "";
        private string lastEnemyFileContent = "";
        private HashSet<string> processedUsernames = new HashSet<string>();
        #endregion

        #region Initialization
        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource, AdvancedChatSosigSpawner spawner)
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            plugin = pluginInstance;
            logger = logSource;
            sosigSpawner = spawner;

            InitializeConfiguration();
            
            if (enableFileWatching.Value)
            {
                InitializeFileWatching();
            }

            logger?.LogInfo("Chat Watcher initialized (H3TwitchTools compatible file mode - Channel Points ready)");
        }

        private void InitializeConfiguration()
        {
            if (plugin?.Config == null)
            {
                logger?.LogError("Plugin config is null");
                return;
            }

            try
            {
                // File watching configuration
                enableFileWatching = plugin.Config.Bind("Chat Watcher - File Mode", "EnableFileWatching", true,
                    "Enable file watching mode for chat integration (H3TwitchTools style)");
                allyChatFilePath = plugin.Config.Bind("Chat Watcher - File Mode", "AllyChatFilePath",
                    "BepInEx/config/H3TVR_AllyChat.txt",
                    "Path to ally chat file\n" +
                    "SUPPORTED FORMATS:\n" +
                    "  - Plain username: ViewerName\n" +
                    "  - Key=Value: username=ViewerName\n" +
                    "  - Key=Value: user=ViewerName\n" +
                    "  - Key=Value: redeemer=ViewerName\n" +
                    "SUPPORTS ABSOLUTE PATHS: C:\\StreamFiles\\ally_chat.txt\n" +
                    "Or relative: BepInEx/config/H3TVR_AllyChat.txt");
                enemyChatFilePath = plugin.Config.Bind("Chat Watcher - File Mode", "EnemyChatFilePath",
                    "BepInEx/config/H3TVR_EnemyChat.txt",
                    "Path to enemy chat file\n" +
                    "SUPPORTED FORMATS:\n" +
                    "  - Plain username: ViewerName\n" +
                    "  - Key=Value: username=ViewerName\n" +
                    "  - Key=Value: user=ViewerName\n" +
                    "  - Key=Value: redeemer=ViewerName\n" +
                    "SUPPORTS ABSOLUTE PATHS: C:\\StreamFiles\\enemy_chat.txt\n" +
                    "Or relative: BepInEx/config/H3TVR_EnemyChat.txt");
                fileCheckInterval = plugin.Config.Bind("Chat Watcher - File Mode", "FileCheckInterval", 0.5f,
                    "How often to check files for changes (seconds)");
                clearFileAfterRead = plugin.Config.Bind("Chat Watcher - File Mode", "ClearFileAfterRead", true,
                    "Clear chat file after reading usernames (recommended for channel points)");

                // Manual spawn key bindings
                manualAllySpawnKey = plugin.Config.Bind("Chat Watcher - Keys", "ManualAllySpawnKey", KeyCode.P,
                    "Key to manually spawn ally sosig");
                manualEnemySpawnKey = plugin.Config.Bind("Chat Watcher - Keys", "ManualEnemySpawnKey", KeyCode.O,
                    "Key to manually spawn enemy sosig");
                clearAllSosigsKey = plugin.Config.Bind("Chat Watcher - Keys", "ClearAllSosigsKey", KeyCode.Delete,
                    "Key to clear all chat sosigs");

                logger?.LogInfo("Chat Watcher configuration initialized (Channel Points format supported)");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Chat Watcher config init failed: {ex.Message}");
            }
        }

        private void InitializeFileWatching()
        {
            try
            {
                // Set up ally file
                string allyPath = ResolveFilePath(allyChatFilePath.Value);
                CreateFileIfNotExists(allyPath, true);

                // Set up enemy file
                string enemyPath = ResolveFilePath(enemyChatFilePath.Value);
                CreateFileIfNotExists(enemyPath, false);

                logger?.LogInfo("File watching initialized (Channel Points ready)");
                logger?.LogInfo($"  Ally file: {allyPath}");
                logger?.LogInfo($"  Enemy file: {enemyPath}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"File watching init failed: {ex.Message}");
            }
        }

        private void CreateFileIfNotExists(string filePath, bool isAlly)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    string header = $"# H3TVR {(isAlly ? "Ally" : "Enemy")} Chat File (Channel Points Compatible)\n" +
                                  $"# SUPPORTED FORMATS:\n" +
                                  $"#   Plain username: ViewerName\n" +
                                  $"#   Key=Value: username=ViewerName\n" +
                                  $"#   Key=Value: user=ViewerName\n" +
                                  $"#   Key=Value: redeemer=ViewerName\n" +
                                  $"# File will be cleared after reading if ClearFileAfterRead is enabled\n" +
                                  $"# Perfect for Twitch Channel Points redemptions!\n";
                    
                    File.WriteAllText(filePath, header);
                    logger?.LogInfo($"Created chat file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create file {filePath}: {ex.Message}");
            }
        }
        #endregion

        #region Update Loop
        private void Update()
        {
        try
                {
                    // Handle manual keyboard spawning
                    HandleManualInput();

                    // Check files periodically
                    if (enableFileWatching.Value && Time.time - lastFileCheckTime >= fileCheckInterval.Value)
                    {
                        CheckChatFiles();
                        lastFileCheckTime = Time.time;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Update loop error: {ex.Message}");
                }
        }

        private void HandleManualInput()
        {
            if (Input.GetKeyDown(manualAllySpawnKey.Value))
            {
                SpawnManualAlly();
            }

            if (Input.GetKeyDown(manualEnemySpawnKey.Value))
            {
                SpawnManualEnemy();
            }

            if (Input.GetKeyDown(clearAllSosigsKey.Value))
            {
                ClearAllSosigs();
            }
        }

        private void SpawnManualAlly()
        {
            try
            {
                string username = "Player_" + UnityEngine.Random.Range(1000, 9999);
                sosigSpawner?.SpawningSequence(username);
                logger?.LogInfo($"Manually spawned ally: {username}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Manual ally spawn failed: {ex.Message}");
            }
        }

        private void SpawnManualEnemy()
        {
            try
            {
                string username = "Enemy_" + UnityEngine.Random.Range(1000, 9999);
                sosigSpawner?.SpawningSequenceEnemy(1, username);
                logger?.LogInfo($"Manually spawned enemy: {username}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Manual enemy spawn failed: {ex.Message}");
            }
        }

        private void ClearAllSosigs()
        {
            try
            {
                sosigSpawner?.ClearSosigs(true, true);
                processedUsernames.Clear();
                logger?.LogInfo("Cleared all chat sosigs");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Clear sosigs failed: {ex.Message}");
            }
        }
        #endregion

        #region File Watching
        private void CheckChatFiles()
        {
            // Check ally file
            string allyPath = ResolveFilePath(allyChatFilePath.Value);
            if (File.Exists(allyPath))
            {
                ProcessChatFile(allyPath, true);
            }

            // Check enemy file
            string enemyPath = ResolveFilePath(enemyChatFilePath.Value);
            if (File.Exists(enemyPath))
            {
                ProcessChatFile(enemyPath, false);
            }
        }

        private void ProcessChatFile(string filePath, bool isAlly)
        {
            try
            {
                // Read file content
                string content = File.ReadAllText(filePath);

                // Skip if content hasn't changed
                string lastContent = isAlly ? lastAllyFileContent : lastEnemyFileContent;
                if (content == lastContent)
                {
                    return;
                }

                // Update last content
                if (isAlly)
                {
                    lastAllyFileContent = content;
                }
                else
                {
                    lastEnemyFileContent = content;
                }

                // Skip if empty
                if (string.IsNullOrEmpty(content) || content.Trim().Length == 0)
                {
                    return;
                }

                // Parse usernames and commands
                var parsedResult = ParseContent(content);

                // Handle armor commands
                if (parsedResult.ArmorCommands.Count > 0)
                {
                    foreach (var command in parsedResult.ArmorCommands)
                    {
                        if (command.Key == "ally_armor")
                        {
                            SosigCustomizationUI.AllyArmor.Value = command.Value;
                            logger?.LogInfo($"Set ally armor to {command.Value} via file command.");
                        }
                        else if (command.Key == "enemy_armor")
                        {
                            SosigCustomizationUI.EnemyArmor.Value = command.Value;
                            logger?.LogInfo($"Set enemy armor to {command.Value} via file command.");
                        }
                    }
                }

                // Spawn sosigs for each username
                foreach (string username in parsedResult.Usernames)
                {
                    // Skip if already processed (prevent duplicates)
                    if (processedUsernames.Contains(username))
                    {
                        continue;
                    }

                    // Spawn sosig
                    if (isAlly)
                    {
                        sosigSpawner?.SpawningSequence(username);
                        logger?.LogInfo($"Channel Point Redemption: Spawned ally for {username}");
                    }
                    else
                    {
                        sosigSpawner?.SpawningSequenceEnemy(1, username);
                        logger?.LogInfo($"Channel Point Redemption: Spawned enemy for {username}");
                    }

                    // Mark as processed
                    processedUsernames.Add(username);
                }

                // Clear file if configured
                if (clearFileAfterRead.Value && (parsedResult.Usernames.Count > 0 || parsedResult.ArmorCommands.Count > 0))
                {
                    ClearChatFile(filePath, isAlly);
                }

                // Clean up processed usernames cache (keep last 1000)
                if (processedUsernames.Count > 1000)
                {
                    processedUsernames.Clear();
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to process chat file {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Holds the results of parsing the chat file.
        /// </summary>
        private struct ParsedContentResult
        {
            public List<string> Usernames;
            public List<KeyValuePair<string, int>> ArmorCommands;
        }

        /// <summary>
        /// Parse usernames and commands from file content.
        /// </summary>
        private ParsedContentResult ParseContent(string content)
        {
            var result = new ParsedContentResult
            {
                Usernames = new List<string>(),
                ArmorCommands = new List<KeyValuePair<string, int>>()
            };

            try
            {
                string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();

                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                    {
                        continue;
                    }

                    // Check for armor commands first
                    if (TryParseArmorCommand(trimmed, out var armorCommand))
                    {
                        result.ArmorCommands.Add(armorCommand);
                        logger?.LogDebug($"Parsed armor command: '{armorCommand.Key}={armorCommand.Value}'");
                        continue;
                    }

                    // Check for airdrop command
                    if (TryParseAirdropCommand(trimmed, out var airdropUser))
                    {
                        plugin.GetAirdropManager()?.CallAirdrop(airdropUser);
                        continue;
                    }

                    // If not an armor command, try to extract a username
                    string username = ExtractUsername(trimmed);
                    if (!string.IsNullOrEmpty(username))
                    {
                        result.Usernames.Add(username);
                        logger?.LogDebug($"Extracted username: '{username}' from line: '{trimmed}'");
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to parse content: {ex.Message}");
            }

            return result;
        }

        private bool TryParseArmorCommand(string line, out KeyValuePair<string, int> command)
        {
            command = default;
            if (!line.Contains("=")) return false;

            string[] parts = line.Split('=');
            if (parts.Length < 2) return false;

            string key = parts[0].Trim().ToLower();
            if (key == "ally_armor" || key == "enemy_armor")
            {
                if (int.TryParse(parts[1].Trim(), out int value))
                {
                    command = new KeyValuePair<string, int>(key, Mathf.Clamp(value, 0, 5));
                    return true;
                }
            }

            return false;
        }

        private bool TryParseAirdropCommand(string line, out string username)
        {
            username = null;
            if (!line.Contains("=")) return false;

            string[] parts = line.Split('=');
            if (parts.Length < 2) return false;

            string key = parts[0].Trim().ToLower();
            if (key == "airdrop")
            {
                username = parts[1].Trim();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parse usernames from file content - supports multiple formats for Channel Points compatibility
        /// Formats supported:
        /// 1. Plain username per line: "ViewerName"
        /// 2. Key=Value format: "username=ViewerName", "user=ViewerName", "redeemer=ViewerName"
        /// 3. INI format: "username=ViewerName"
        /// 4. Simple text with username first: "ViewerName redeemed your channel points!"
        /// </summary>
        private List<string> ParseUsernames(string content)
        {
            return ParseContent(content).Usernames;
        }

        /// <summary>
        /// Extract username from a single line - supports multiple formats
        /// Priority order:
        /// 1. Key=Value format (username=, user=, redeemer=, name=)
        /// 2. First word before space (for "Username did something" format)
        /// 3. Entire trimmed line (plain username)
        /// </summary>
        private string ExtractUsername(string line)
        {
            if (string.IsNullOrEmpty(line))
                return null;

            string trimmed = line.Trim();

            // Format 1: Key=Value format (INI style or channel points format)
            // Examples: "username=ViewerName", "user=ViewerName", "redeemer=ViewerName"
            if (trimmed.Contains("="))
            {
                string[] parts = trimmed.Split('=');
                if (parts.Length >= 2)
                {
                    string key = parts[0].Trim().ToLower();
                    string value = parts[1].Trim();

                    // Check if key matches known username keys
                    if (key == "username" || key == "user" || key == "redeemer" || key == "name")
                    {
                        // Remove any trailing text after the username (e.g., "ViewerName # comment")
                        int commentIndex = value.IndexOf('#');
                        if (commentIndex > 0)
                        {
                            value = value.Substring(0, commentIndex).Trim();
                        }

                        // Remove any trailing text after spaces (e.g., "ViewerName extra stuff")
                        int spaceIndex = value.IndexOf(' ');
                        if (spaceIndex > 0)
                        {
                            value = value.Substring(0, spaceIndex).Trim();
                        }

                        if (!string.IsNullOrEmpty(value))
                        {
                            return value;
                        }
                    }
                }
            }

            // Format 2: First word format (for "Username redeemed channel points!" style)
            // Example: "ViewerName redeemed your channel points!"
            if (trimmed.Contains(" "))
            {
                string[] words = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 0)
                {
                    string firstWord = words[0].Trim();
                    // Make sure first word doesn't look like a system message
                    if (!firstWord.StartsWith("[") && !firstWord.StartsWith("<") && 
                        !firstWord.EndsWith(":") && firstWord.Length > 0)
                    {
                        return firstWord;
                    }
                }
            }

            // Format 3: Plain username (entire line is the username)
            // Example: "ViewerName"
            if (trimmed.Length > 0 && !trimmed.StartsWith("[") && !trimmed.StartsWith("<"))
            {
                return trimmed;
            }

            return null;
        }

        private void ClearChatFile(string filePath, bool isAlly)
        {
            try
            {
                string header = $"# H3TVR {(isAlly ? "Ally" : "Enemy")} Chat File (Channel Points Compatible)\n" +
                              $"# SUPPORTED FORMATS:\n" +
                              $"#   Plain username: ViewerName\n" +
                              $"#   Key=Value: username=ViewerName\n" +
                              $"#   Key=Value: user=ViewerName\n" +
                              $"#   Key=Value: redeemer=ViewerName\n";

                File.WriteAllText(filePath, header);

                // Clear content cache
                if (isAlly)
                {
                    lastAllyFileContent = "";
                }
                else
                {
                    lastEnemyFileContent = "";
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to clear file {filePath}: {ex.Message}");
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Manually trigger a spawn from external source
        /// </summary>
        public void TriggerSpawn(string username, bool isAlly)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    logger?.LogWarning("Cannot spawn sosig - username is null or empty");
                    return;
                }

                // Spawn sosig
                if (isAlly)
                {
                    sosigSpawner?.SpawningSequence(username);
                    logger?.LogInfo($"API trigger: Spawned ally for {username}");
                }
                else
                {
                    sosigSpawner?.SpawningSequenceEnemy(1, username);
                    logger?.LogInfo($"API trigger: Spawned enemy for {username}");
                }

                // Mark as processed
                processedUsernames.Add(username);
            }
            catch (Exception ex)
            {
                logger?.LogError($"API spawn trigger failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get chat watcher statistics
        /// </summary>
        public struct ChatWatcherStats
        {
            public bool FileWatchingActive;
            public int ProcessedUsernames;
            public int ActiveAllies;
            public int ActiveEnemies;
            public int TotalActiveSosigs;
        }

        public ChatWatcherStats GetStats()
        {
            var sosigStats = sosigSpawner?.GetStats() ?? default;

            return new ChatWatcherStats
            {
                FileWatchingActive = enableFileWatching.Value,
                ProcessedUsernames = processedUsernames.Count,
                ActiveAllies = sosigStats.Allies,
                ActiveEnemies = sosigStats.Enemies,
                TotalActiveSosigs = sosigStats.TotalActive
            };
        }

        /// <summary>
        /// Clear processed usernames cache
        /// </summary>
        public void ClearCache()
        {
            processedUsernames.Clear();
            lastAllyFileContent = "";
            lastEnemyFileContent = "";
            logger?.LogInfo("Cleared chat watcher cache");
        }
        #endregion

        #region Helper Methods
        private string ResolveFilePath(string configuredPath)
        {
            if (string.IsNullOrEmpty(configuredPath))
            {
                var pluginDir = Path.GetDirectoryName(plugin.Info.Location);
                var bepInExDir = Path.Combine(pluginDir, "..");
                var configPath = Path.Combine(bepInExDir, "config");
                return Path.Combine(configPath, "H3TVR_Chat.txt");
            }

            // If it's already an absolute path, use it directly
            if (Path.IsPathRooted(configuredPath))
            {
                return configuredPath;
            }

            // Try as relative to plugin folder
            string pluginFolder = Path.GetDirectoryName(plugin.Info.Location);
            string relativePath = Path.Combine(pluginFolder, configuredPath);

            if (File.Exists(relativePath))
            {
                return relativePath;
            }

            // Try as relative to BepInEx root
            string bepInExRoot = Path.GetDirectoryName(pluginFolder);
            string bepInExRelative = Path.Combine(bepInExRoot, configuredPath);

            if (File.Exists(bepInExRelative))
            {
                return bepInExRelative;
            }

            // Return plugin folder relative path as default
            return relativePath;
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            try
            {
                logger?.LogInfo("Chat Watcher cleaned up");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Chat Watcher cleanup failed: {ex.Message}");
            }
        }
        #endregion
    }
}
