using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace H3TVR
{
    /// <summary>
    /// LioranBoard 2.0 Integration - Routes all spawns through ChatWatcher
    /// 
    /// FLOW:
    /// 1. LioranBoard writes username to C:\LioranBoard\ally or enemy file
    /// 2. This integration reads the INI file and writes to ChatWatcher files
    /// 3. ChatWatcher handles the actual spawning (unified for channel points + chat commands)
    /// 
    /// This ensures both LioranBoard AND InstructBot work through the same system.
    /// </summary>
    public class LioranBoardIntegration : MonoBehaviour
    {
        private ManualLogSource logger;
        private string configFilePath;
        private H3TVRImproved plugin;
        private bool isWatching = false;

        // Default LioranBoard folder path
        private const string DEFAULT_LIORANBOARD_PATH = @"C:\LioranBoard";
        
        // LioranBoard INI files (no extension)
        private const string ALLY_FILENAME = "ally";
        private const string ENEMY_FILENAME = "enemy";

        // LioranBoard file paths
        private string allyIniFilePath;
        private string enemyIniFilePath;
        private DateTime lastAllyIniWriteTime = DateTime.MinValue;
        private DateTime lastEnemyIniWriteTime = DateTime.MinValue;

        // ChatWatcher file paths (where we write for unified spawning)
        private string allyChatFilePath;
        private string enemyChatFilePath;

        public void Initialize(ManualLogSource logSource, H3TVRImproved pluginInstance)
        {
            logger = logSource;
            plugin = pluginInstance;

            // Set up config file path in BepInEx
            configFilePath = Path.Combine(Paths.BepInExRootPath, "H3TVR_LioranBoard_Config.ini");
            
            // Load or create config to get LioranBoard folder path
            string lioranBoardFolder = LoadOrCreateConfig();

            // Set up LioranBoard INI file paths
            allyIniFilePath = Path.Combine(lioranBoardFolder, ALLY_FILENAME);
            enemyIniFilePath = Path.Combine(lioranBoardFolder, ENEMY_FILENAME);

            // Set up ChatWatcher file paths (unified spawning destination)
            allyChatFilePath = Path.Combine(Path.Combine(Paths.BepInExRootPath, "config"), "H3TVR_AllyChat.txt");
            enemyChatFilePath = Path.Combine(Path.Combine(Paths.BepInExRootPath, "config"), "H3TVR_EnemyChat.txt");

            logger.LogInfo("=== LioranBoard 2.0 Integration (ChatWatcher Unified) ===");
            logger.LogInfo($"LioranBoard folder: {lioranBoardFolder}");
            logger.LogInfo($"Watching ally INI: {allyIniFilePath}");
            logger.LogInfo($"Watching enemy INI: {enemyIniFilePath}");
            logger.LogInfo($"Writing to ChatWatcher ally: {allyChatFilePath}");
            logger.LogInfo($"Writing to ChatWatcher enemy: {enemyChatFilePath}");
            logger.LogInfo("All spawns routed through ChatWatcher for unified handling.");

            // Ensure directories exist
            EnsureDirectoriesExist(lioranBoardFolder);

            // Start file watching coroutine
            isWatching = true;
            StartCoroutine(WatchFileCoroutine());
        }

        private void EnsureDirectoriesExist(string lioranBoardFolder)
        {
            try
            {
                // Ensure LioranBoard folder exists
                if (!Directory.Exists(lioranBoardFolder))
                {
                    Directory.CreateDirectory(lioranBoardFolder);
                    logger.LogInfo($"Created LioranBoard folder: {lioranBoardFolder}");
                }

                // Ensure ChatWatcher config folder exists
                string chatWatcherFolder = Path.GetDirectoryName(allyChatFilePath);
                if (!Directory.Exists(chatWatcherFolder))
                {
                    Directory.CreateDirectory(chatWatcherFolder);
                    logger.LogInfo($"Created ChatWatcher config folder: {chatWatcherFolder}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to create directories: {ex.Message}");
            }
        }

        private string LoadOrCreateConfig()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string[] lines = File.ReadAllLines(configFilePath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("LioranBoardFolder="))
                        {
                            string path = line.Substring("LioranBoardFolder=".Length).Trim();
                            if (!string.IsNullOrEmpty(path))
                            {
                                logger.LogInfo($"Using LioranBoard folder from config: {path}");
                                return path;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Error reading config: {ex.Message}");
            }

            // Create default config
            try
            {
                string configContent = 
                    "; H3TVR LioranBoard 2.0 Integration Config\r\n" +
                    "; \r\n" +
                    "; Set the path to your LioranBoard folder below.\r\n" +
                    "; This is where LioranBoard creates the 'ally' and 'enemy' files.\r\n" +
                    "; \r\n" +
                    "LioranBoardFolder=" + DEFAULT_LIORANBOARD_PATH + "\r\n" +
                    "; \r\n" +
                    "; === LIORANBOARD 2.0 SETUP ===\r\n" +
                    "; \r\n" +
                    "; For ALLY spawns, use File: Save Text with:\r\n" +
                    ";   file name: ally\r\n" +
                    ";   section: ally\r\n" +
                    ";   key: username (or any key name)\r\n" +
                    ";   text: /$user_name$/\r\n" +
                    "; \r\n" +
                    "; For ENEMY spawns, use File: Save Text with:\r\n" +
                    ";   file name: enemy\r\n" +
                    ";   section: enemy\r\n" +
                    ";   key: username (or any key name)\r\n" +
                    ";   text: /$user_name$/\r\n" +
                    "; \r\n" +
                    "; Works with both Channel Points AND Chat Commands!\r\n";

                File.WriteAllText(configFilePath, configContent);
                logger.LogInfo($"Created config file: {configFilePath}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to create config: {ex.Message}");
            }

            return DEFAULT_LIORANBOARD_PATH;
        }

        private IEnumerator WatchFileCoroutine()
        {
            // Initialize tracking for ally/enemy INI files
            if (File.Exists(allyIniFilePath))
            {
                lastAllyIniWriteTime = new FileInfo(allyIniFilePath).LastWriteTime;
            }
            if (File.Exists(enemyIniFilePath))
            {
                lastEnemyIniWriteTime = new FileInfo(enemyIniFilePath).LastWriteTime;
            }

            while (isWatching)
            {
                yield return new WaitForSeconds(0.25f); // Fast response time

                try
                {
                    // Watch ally file
                    if (File.Exists(allyIniFilePath))
                    {
                        var allyFileInfo = new FileInfo(allyIniFilePath);
                        if (allyFileInfo.LastWriteTime != lastAllyIniWriteTime)
                        {
                            ProcessLioranBoardFile(allyIniFilePath, true);
                            allyFileInfo.Refresh();
                            lastAllyIniWriteTime = allyFileInfo.LastWriteTime;
                        }
                    }

                    // Watch enemy file
                    if (File.Exists(enemyIniFilePath))
                    {
                        var enemyFileInfo = new FileInfo(enemyIniFilePath);
                        if (enemyFileInfo.LastWriteTime != lastEnemyIniWriteTime)
                        {
                            ProcessLioranBoardFile(enemyIniFilePath, false);
                            enemyFileInfo.Refresh();
                            lastEnemyIniWriteTime = enemyFileInfo.LastWriteTime;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error watching file: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Process LioranBoard INI file and write usernames to ChatWatcher files
        /// Accepts ANY key name - just needs a non-empty value
        /// </summary>
        private void ProcessLioranBoardFile(string filePath, bool isAlly)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                List<string> usernames = new List<string>();
                bool foundUsername = false;

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    
                    // Skip comments, empty lines, and section headers
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("["))
                        continue;

                    // Check for key=value format - accept ANY key name
                    if (trimmed.Contains("="))
                    {
                        int eqIndex = trimmed.IndexOf('=');
                        string value = trimmed.Substring(eqIndex + 1).Trim();

                        // Accept any key as long as there's a value
                        if (!string.IsNullOrEmpty(value))
                        {
                            usernames.Add(value);
                            foundUsername = true;
                        }
                    }
                }

                // Write each username to the appropriate ChatWatcher file
                foreach (string username in usernames)
                {
                    string chatFile = isAlly ? allyChatFilePath : enemyChatFilePath;
                    string type = isAlly ? "ally" : "enemy";
                    
                    // Append username to ChatWatcher file (ChatWatcher will handle spawning)
                    File.AppendAllText(chatFile, username + Environment.NewLine);
                    logger.LogInfo($"[LioranBoard -> ChatWatcher] {type}: '{username}'");
                }

                // Clear the LioranBoard file after processing
                if (foundUsername)
                {
                    string section = isAlly ? "ally" : "enemy";
                    File.WriteAllText(filePath, "[" + section + "]\r\nusername=\r\n");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing LioranBoard file: {ex.Message}");
            }
        }

        public void Shutdown()
        {
            isWatching = false;
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            Shutdown();
            plugin = null;
            logger = null;
        }
    }
}
