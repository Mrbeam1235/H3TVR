using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using UnityEngine;

namespace H3TVR
{
    /// <summary>
    /// Manages sosig name lists and random name selection
    /// </summary>
    public class SosigNameManager
    {
        private List<string> allyNames = new List<string>();
        private List<string> enemyNames = new List<string>();
        private ManualLogSource logger;
        
        public void Initialize(ManualLogSource logSource)
        {
            logger = logSource;
        }
        
        public void LoadNameLists(string allyPath, string enemyPath)
        {
            try
            {
                // Load ally names
                if (File.Exists(allyPath))
                {
                    var lines = File.ReadAllLines(allyPath);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#") && !trimmed.StartsWith(";"))
                        {
                            allyNames.Add(trimmed);
                        }
                    }
                    logger?.LogInfo($"Loaded {allyNames.Count} ally names");
                }
                else
                {
                    logger?.LogWarning($"Ally names file not found: {allyPath}");
                    CreateDefaultNameFile(allyPath, true);
                }
                
                // Load enemy names
                if (File.Exists(enemyPath))
                {
                    var lines = File.ReadAllLines(enemyPath);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#") && !trimmed.StartsWith(";"))
                        {
                            enemyNames.Add(trimmed);
                        }
                    }
                    logger?.LogInfo($"Loaded {enemyNames.Count} enemy names");
                }
                else
                {
                    logger?.LogWarning($"Enemy names file not found: {enemyPath}");
                    CreateDefaultNameFile(enemyPath, false);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to load name lists: {ex.Message}");
            }
        }
        
        private void CreateDefaultNameFile(string path, bool isAlly)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var defaultNames = isAlly 
                    ? new[] { "# Ally Sosig Names", "Friendly Bot", "Guardian", "Protector", "Ally", "Helper" }
                    : new[] { "# Enemy Sosig Names", "Hostile Bot", "Attacker", "Enemy", "Threat", "Opponent" };
                
                File.WriteAllLines(path, defaultNames);
                logger?.LogInfo($"Created default name file: {path}");
                
                // Reload
                if (isAlly)
                {
                    for (int i = 1; i < defaultNames.Length; i++)
                        allyNames.Add(defaultNames[i]);
                }
                else
                {
                    for (int i = 1; i < defaultNames.Length; i++)
                        enemyNames.Add(defaultNames[i]);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create default name file: {ex.Message}");
            }
        }
        
        public string GetRandomName(bool isAlly, SteamFriendsIntegration steamFriends = null, bool useSteamFriends = false)
        {
            // Try Steam Friends first if enabled
            if (steamFriends != null && steamFriends.IsAvailable() && useSteamFriends)
            {
                try
                {
                    string friendName = steamFriends.GetRandomFriendName();
                    if (!string.IsNullOrEmpty(friendName) && friendName != "Steam Friend")
                    {
                        return friendName;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning($"Failed to get Steam friend name: {ex.Message}");
                }
            }
            
            var nameList = isAlly ? allyNames : enemyNames;
            
            if (nameList.Count == 0)
                return isAlly ? "Ally" : "Enemy";
       
            return nameList[UnityEngine.Random.Range(0, nameList.Count)];
        }
    }
}
