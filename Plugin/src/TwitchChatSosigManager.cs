using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FistVR;
using BepInEx;
using BepInEx.Configuration;
using jediSpawner;
using System;

namespace H3TVR
{
    /// <summary>
    /// Standalone Twitch Chat Sosig Manager
    /// Assigns Twitch usernames to ally and enemy sosigs without requiring external tools.
    /// Self-contained system with direct keyboard controls and automatic username assignment.
    /// </summary>
    public class TwitchChatSosigManager : MonoBehaviour
    {
        #region Configuration
        public static ConfigEntry<KeyCode> SpawnAllyKey;
        public static ConfigEntry<KeyCode> SpawnEnemyKey;
        public static ConfigEntry<KeyCode> ToggleModeKey;
        public static ConfigEntry<KeyCode> ShowStatusKey;
        public static ConfigEntry<KeyCode> ClearQueuesKey;
        public static ConfigEntry<bool> EnableAutoMode;
        public static ConfigEntry<float> SpawnDistance;
        public static ConfigEntry<int> MaxQueueSize;
        public static ConfigEntry<bool> EnableDebugLogging;
        public static ConfigEntry<bool> FilterBots;
        public static ConfigEntry<string> BotFilterKeywords;
        #endregion

        #region Queue Management
        private Queue<string> allyQueue = new Queue<string>();
        private Queue<string> enemyQueue = new Queue<string>();
        private HashSet<string> usedUsernames = new HashSet<string>();
        private bool isAllyMode = true; // true = new chatters go to ally queue, false = enemy queue
        private bool autoMode = true; // true = automatic assignment, false = manual queue selection
        private List<string> botKeywords = new List<string>();
        private System.Random random = new System.Random();
        #endregion

        #region UI and Display
        private bool showStatusUI = false;
        private Rect statusWindowRect = new Rect(20, 20, 350, 400);
        private GUIStyle windowStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private Vector2 scrollPosition = Vector2.zero;
        #endregion

        #region Integration
        private ChatWatcher chatWatcher;
        private List<Sosig> spawnedAllies = new List<Sosig>();
        private List<Sosig> spawnedEnemies = new List<Sosig>();
        private Dictionary<Sosig, string> sosigUsernames = new Dictionary<Sosig, string>();
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            InitializeConfiguration();
            InitializeBotFilter();
            FindChatWatcher();
            StartUsernameMonitoring();
            
            Debug.Log("[TwitchChatSosigManager] Standalone Twitch Chat Sosig system initialized!");
            LogStatus("System ready - Use F1/F2 to spawn, F3 to toggle mode, F4 for status");
        }

        void Update()
        {
            HandleKeyboardInput();
            CleanupDestroyedSosigs();
        }

        void OnGUI()
        {
            if (showStatusUI)
            {
                InitializeGUIStyles();
                statusWindowRect = GUI.Window(1001, statusWindowRect, DrawStatusWindow, "Twitch Chat Sosig Manager", windowStyle);
            }
        }
        #endregion

        #region Configuration Setup
        private void InitializeConfiguration()
        {
            var config = ((BaseUnityPlugin)FindObjectOfType<H3TVR>()).Config;
            
            SpawnAllyKey = config.Bind("Twitch Chat Sosig", "SpawnAllyKey", KeyCode.F1, "Key to spawn ally sosig with next username from ally queue");
            SpawnEnemyKey = config.Bind("Twitch Chat Sosig", "SpawnEnemyKey", KeyCode.F2, "Key to spawn enemy sosig with next username from enemy queue");
            ToggleModeKey = config.Bind("Twitch Chat Sosig", "ToggleModeKey", KeyCode.F3, "Key to toggle between ally/enemy assignment for new chatters");
            ShowStatusKey = config.Bind("Twitch Chat Sosig", "ShowStatusKey", KeyCode.F4, "Key to show current queue status and help");
            ClearQueuesKey = config.Bind("Twitch Chat Sosig", "ClearQueuesKey", KeyCode.F5, "Key to clear all queues");
            
            EnableAutoMode = config.Bind("Twitch Chat Sosig", "EnableAutoMode", true, "Automatically assign new chatters to current queue (ally/enemy)");
            SpawnDistance = config.Bind("Twitch Chat Sosig", "SpawnDistance", 3.0f, "Distance from player to spawn sosigs");
            MaxQueueSize = config.Bind("Twitch Chat Sosig", "MaxQueueSize", 50, "Maximum number of usernames to keep in each queue");
            EnableDebugLogging = config.Bind("Twitch Chat Sosig", "EnableDebugLogging", true, "Enable detailed logging for debugging");
            FilterBots = config.Bind("Twitch Chat Sosig", "FilterBots", true, "Filter out bot usernames");
            BotFilterKeywords = config.Bind("Twitch Chat Sosig", "BotFilterKeywords", "bot,nightbot,streamlabs,moobot,streamelements,fossabot", "Comma-separated keywords to filter bot usernames");
            
            autoMode = EnableAutoMode.Value;
        }

        private void InitializeBotFilter()
        {
            if (FilterBots.Value)
            {
                string[] keywords = BotFilterKeywords.Value.Split(',');
                botKeywords = keywords.Select(k => k.Trim().ToLower()).Where(k => !string.IsNullOrEmpty(k)).ToList();
                LogDebug($"Bot filter initialized with {botKeywords.Count} keywords: {string.Join(", ", botKeywords)}");
            }
        }

        private void FindChatWatcher()
        {
            chatWatcher = FindObjectOfType<ChatWatcher>();
            if (chatWatcher != null)
            {
                LogDebug("Found existing ChatWatcher - will integrate with it");
            }
            else
            {
                LogDebug("No ChatWatcher found - will work independently");
            }
        }
        #endregion

        #region Keyboard Input Handling
        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(SpawnAllyKey.Value))
            {
                SpawnAllyFromQueue();
            }
            
            if (Input.GetKeyDown(SpawnEnemyKey.Value))
            {
                SpawnEnemyFromQueue();
            }
            
            if (Input.GetKeyDown(ToggleModeKey.Value))
            {
                ToggleAssignmentMode();
            }
            
            if (Input.GetKeyDown(ShowStatusKey.Value))
            {
                ToggleStatusUI();
            }
            
            if (Input.GetKeyDown(ClearQueuesKey.Value))
            {
                ClearAllQueues();
            }
        }
        #endregion

        #region Username Monitoring
        private void StartUsernameMonitoring()
        {
            // Start monitoring for new Twitch usernames
            // This will check the same file paths that ChatWatcher uses
            StartCoroutine(MonitorUsernameFiles());
        }

        private IEnumerator MonitorUsernameFiles()
        {
            string lastAllyUsername = "";
            string lastEnemyUsername = "";
            
            while (true)
            {
                yield return new WaitForSeconds(0.5f); // Check twice per second
                
                try
                {
                    // Check ally username file
                    if (chatWatcher != null)
                    {
                        string allyPath = GetChatWatcherFilePath(chatWatcher, true);
                        if (System.IO.File.Exists(allyPath))
                        {
                            string content = System.IO.File.ReadAllText(allyPath);
                            string username = ExtractUsernameFromContent(content);
                            
                            if (!string.IsNullOrEmpty(username) && username != lastAllyUsername)
                            {
                                ProcessNewUsername(username, true);
                                lastAllyUsername = username;
                            }
                        }
                    }
                    
                    // Check enemy username file
                    if (chatWatcher != null)
                    {
                        string enemyPath = GetChatWatcherFilePath(chatWatcher, false);
                        if (System.IO.File.Exists(enemyPath))
                        {
                            string content = System.IO.File.ReadAllText(enemyPath);
                            string username = ExtractUsernameFromContent(content);
                            
                            if (!string.IsNullOrEmpty(username) && username != lastEnemyUsername)
                            {
                                ProcessNewUsername(username, false);
                                lastEnemyUsername = username;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"Error monitoring username files: {ex.Message}");
                }
            }
        }

        private string ExtractUsernameFromContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";
            
            // Extract username from JSON-like content (same logic as ChatWatcher)
            int startIndex = content.IndexOf('"');
            if (startIndex == -1) return "";
            
            int endIndex = content.LastIndexOf('"');
            if (endIndex <= startIndex) return "";
            
            string username = content.Substring(startIndex + 1, endIndex - startIndex - 1);
            return username.Trim();
        }

        private void ProcessNewUsername(string username, bool fromAllyFile)
        {
            if (string.IsNullOrEmpty(username) || IsUsernameFiltered(username))
            {
                LogDebug($"Filtered username: {username}");
                return;
            }

            // Prevent duplicate processing
            if (usedUsernames.Contains(username))
            {
                LogDebug($"Username already processed: {username}");
                return;
            }

            AddUsernameToQueue(username, fromAllyFile);
            usedUsernames.Add(username);
            
            LogStatus($"New chatter: {username} -> {(GetTargetQueue(fromAllyFile) ? "Ally" : "Enemy")} queue");
        }

        private bool IsUsernameFiltered(string username)
        {
            if (!FilterBots.Value || botKeywords.Count == 0) return false;
            
            string lowerUsername = username.ToLower();
            return botKeywords.Any(keyword => lowerUsername.Contains(keyword));
        }

        private void AddUsernameToQueue(string username, bool fromAllyFile)
        {
            bool addToAllyQueue = GetTargetQueue(fromAllyFile);
            
            if (addToAllyQueue)
            {
                if (allyQueue.Count >= MaxQueueSize.Value)
                {
                    allyQueue.Dequeue(); // Remove oldest
                }
                allyQueue.Enqueue(username);
                LogDebug($"Added {username} to ally queue (size: {allyQueue.Count})");
            }
            else
            {
                if (enemyQueue.Count >= MaxQueueSize.Value)
                {
                    enemyQueue.Dequeue(); // Remove oldest
                }
                enemyQueue.Enqueue(username);
                LogDebug($"Added {username} to enemy queue (size: {enemyQueue.Count})");
            }
        }

        private bool GetTargetQueue(bool fromAllyFile)
        {
            if (autoMode)
            {
                // In auto mode, use current mode (ally/enemy)
                return isAllyMode;
            }
            else
            {
                // In manual mode, add to both queues randomly or based on file source
                if (fromAllyFile)
                {
                    return true; // Ally file -> ally queue
                }
                else
                {
                    return false; // Enemy file -> enemy queue
                }
            }
        }
        #endregion

        #region Sosig Spawning
        private void SpawnAllyFromQueue()
        {
            if (allyQueue.Count == 0)
            {
                LogStatus("No usernames in ally queue");
                return;
            }

            string username = allyQueue.Dequeue();
            Sosig spawnedSosig = SpawnSosigWithUsername(username, true);
            
            if (spawnedSosig != null)
            {
                spawnedAllies.Add(spawnedSosig);
                sosigUsernames[spawnedSosig] = username;
                LogStatus($"Spawned ally sosig: {username} (Queue: {allyQueue.Count})");
            }
        }

        private void SpawnEnemyFromQueue()
        {
            if (enemyQueue.Count == 0)
            {
                LogStatus("No usernames in enemy queue");
                return;
            }

            string username = enemyQueue.Dequeue();
            Sosig spawnedSosig = SpawnSosigWithUsername(username, false);
            
            if (spawnedSosig != null)
            {
                spawnedEnemies.Add(spawnedSosig);
                sosigUsernames[spawnedSosig] = username;
                LogStatus($"Spawned enemy sosig: {username} (Queue: {enemyQueue.Count})");
            }
        }

        private Sosig SpawnSosigWithUsername(string username, bool isAlly)
        {
            if (GM.CurrentPlayerBody == null)
            {
                LogStatus("Player not found - cannot spawn sosig");
                return null;
            }

            try
            {
                Vector3 spawnPosition = CalculateSpawnPosition();
                Quaternion spawnRotation = Quaternion.LookRotation(GM.CurrentPlayerBody.Head.forward);

                // Use existing ChatSpawner integration if available
                if (chatWatcher != null)
                {
                    GameObject prefab = GetChatWatcherPrefab();
                    if (prefab != null)
                    {
                        return SpawnUsingChatSpawner(username, spawnPosition, spawnRotation, isAlly, prefab);
                    }
                }
                else
                {
                    // Fallback to basic spawning
                    return SpawnBasicSosig(username, spawnPosition, spawnRotation, isAlly);
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error spawning sosig for {username}: {ex.Message}");
                return null;
            }
        }

        private Sosig SpawnUsingChatSpawner(string username, Vector3 position, Quaternion rotation, bool isAlly, GameObject prefab)
        {
            // Temporarily set the spawner name for ChatSpawner to use
            string originalName = chatWatcher.SpawnerName;
            chatWatcher.SpawnerName = username;

            try
            {
                GameObject spawnerObject = Instantiate(prefab, position, rotation);
                ChatSpawner spawner = spawnerObject.GetComponent<ChatSpawner>();
                
                if (spawner != null)
                {
                    if (isAlly)
                    {
                        spawner.SpawningSequence();
                        
                        // Get the spawned sosig from ChatWatcher's list
                        if (ChatWatcher.spawnedChatters.Count > 0)
                        {
                            return ChatWatcher.spawnedChatters[ChatWatcher.spawnedChatters.Count - 1];
                        }
                    }
                    else
                    {
                        int enemyIFF = GetEnemyIFF();
                        spawner.SpawningSequenceEnemy(enemyIFF);
                        
                        // Get the spawned sosig from ChatWatcher's enemy list
                        if (ChatWatcher.spawnedEnemyChatters.Count > 0)
                        {
                            return ChatWatcher.spawnedEnemyChatters[ChatWatcher.spawnedEnemyChatters.Count - 1];
                        }
                    }
                }
            }
            finally
            {
                // Restore original spawner name
                chatWatcher.SpawnerName = originalName;
            }

            return null;
        }

        private Sosig SpawnBasicSosig(string username, Vector3 position, Quaternion rotation, bool isAlly)
        {
            LogDebug("Spawning basic sosig - ChatSpawner not available");
            
            // This is a placeholder for basic sosig spawning without ChatSpawner
            // In a real implementation, you'd need access to sosig prefabs and templates
            LogStatus($"Basic spawn for {username} not implemented - need ChatSpawner integration");
            return null;
        }

        private Vector3 CalculateSpawnPosition()
        {
            Vector3 playerPos = GM.CurrentPlayerBody.Head.position;
            Vector3 forward = GM.CurrentPlayerBody.Head.forward;
            Vector3 right = GM.CurrentPlayerBody.Head.right;
            
            // Add some randomization to prevent overlapping
            float offsetX = UnityEngine.Random.Range(-1f, 1f);
            float offsetZ = UnityEngine.Random.Range(-0.5f, 0.5f);
            
            Vector3 spawnPos = playerPos + (forward * SpawnDistance.Value) + (right * offsetX) + (Vector3.forward * offsetZ);
            spawnPos.y = playerPos.y; // Keep at ground level
            
            return spawnPos;
        }

        private int GetEnemyIFF()
        {
            // Try to get IFF from TNH_Manager if available
            if (GM.TNH_Manager != null)
            {
                var tnhManager = GM.TNH_Manager;
                
                if (tnhManager.Phase == TNH_Phase.Hold && tnhManager.m_curHoldPoint != null)
                {
                    return tnhManager.m_curHoldPoint.m_curPhase.IFFUsed;
                }
                else if (tnhManager.Phase == TNH_Phase.Take && tnhManager.m_curLevel != null)
                {
                    if (tnhManager.m_curLevel.PatrolChallenge.Patrols.Count > 0)
                        return tnhManager.m_curLevel.PatrolChallenge.Patrols[0].IFFUsed;
                    else if (tnhManager.m_curHoldPoint != null)
                        return tnhManager.m_curHoldPoint.m_curPhase.IFFUsed;
                }
            }
            
            // Default enemy IFF
            return 1;
        }
        #endregion

        #region Mode Management
        private void ToggleAssignmentMode()
        {
            isAllyMode = !isAllyMode;
            string modeText = isAllyMode ? "ALLY" : "ENEMY";
            LogStatus($"Assignment mode: {modeText} (New chatters go to {modeText.ToLower()} queue)");
        }

        private void ToggleStatusUI()
        {
            showStatusUI = !showStatusUI;
            if (showStatusUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void ClearAllQueues()
        {
            int allyCount = allyQueue.Count;
            int enemyCount = enemyQueue.Count;
            
            allyQueue.Clear();
            enemyQueue.Clear();
            usedUsernames.Clear();
            
            LogStatus($"Cleared all queues (Allies: {allyCount}, Enemies: {enemyCount})");
        }
        #endregion

        #region UI Rendering
        private void InitializeGUIStyles()
        {
            if (windowStyle == null)
            {
                windowStyle = new GUIStyle(GUI.skin.window);
                windowStyle.fontSize = 12;
            }
            
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 10;
                labelStyle.wordWrap = true;
            }
            
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 11;
            }
        }

        private void DrawStatusWindow(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            // Header
            GUILayout.Label("=== Twitch Chat Sosig Manager ===", labelStyle);
            GUILayout.Space(10);
            
            // Mode Status
            string modeText = isAllyMode ? "ALLY" : "ENEMY";
            string modeColor = isAllyMode ? "green" : "red";
            GUILayout.Label($"Current Mode: <color={modeColor}>{modeText}</color>", labelStyle);
            GUILayout.Label($"Auto Mode: {(autoMode ? "ON" : "OFF")}", labelStyle);
            GUILayout.Space(5);
            
            // Queue Status
            GUILayout.Label("=== Queue Status ===", labelStyle);
            GUILayout.Label($"Ally Queue: {allyQueue.Count} usernames", labelStyle);
            GUILayout.Label($"Enemy Queue: {enemyQueue.Count} usernames", labelStyle);
            GUILayout.Space(5);
            
            // Spawned Sosigs
            GUILayout.Label("=== Spawned Sosigs ===", labelStyle);
            GUILayout.Label($"Active Allies: {spawnedAllies.Count}", labelStyle);
            GUILayout.Label($"Active Enemies: {spawnedEnemies.Count}", labelStyle);
            GUILayout.Space(5);
            
            // Controls
            GUILayout.Label("=== Controls ===", labelStyle);
            GUILayout.Label($"{SpawnAllyKey.Value}: Spawn Ally", labelStyle);
            GUILayout.Label($"{SpawnEnemyKey.Value}: Spawn Enemy", labelStyle);
            GUILayout.Label($"{ToggleModeKey.Value}: Toggle Mode", labelStyle);
            GUILayout.Label($"{ShowStatusKey.Value}: Toggle Status", labelStyle);
            GUILayout.Label($"{ClearQueuesKey.Value}: Clear Queues", labelStyle);
            GUILayout.Space(10);
            
            // Action Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Ally", buttonStyle) && allyQueue.Count > 0)
            {
                SpawnAllyFromQueue();
            }
            if (GUILayout.Button("Spawn Enemy", buttonStyle) && enemyQueue.Count > 0)
            {
                SpawnEnemyFromQueue();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Toggle Mode", buttonStyle))
            {
                ToggleAssignmentMode();
            }
            if (GUILayout.Button("Clear Queues", buttonStyle))
            {
                ClearAllQueues();
            }
            GUILayout.EndHorizontal();
            
            // Queue Preview
            if (allyQueue.Count > 0 || enemyQueue.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("=== Next in Queue ===", labelStyle);
                
                if (allyQueue.Count > 0)
                {
                    string[] allyArray = allyQueue.ToArray();
                    GUILayout.Label($"Ally: {allyArray[0]}", labelStyle);
                }
                
                if (enemyQueue.Count > 0)
                {
                    string[] enemyArray = enemyQueue.ToArray();
                    GUILayout.Label($"Enemy: {enemyArray[0]}", labelStyle);
                }
            }
            
            if (GUILayout.Button("Close", buttonStyle))
            {
                showStatusUI = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }
        #endregion

        #region Utility Methods
        private void CleanupDestroyedSosigs()
        {
            // Clean up ally list
            for (int i = spawnedAllies.Count - 1; i >= 0; i--)
            {
                if (spawnedAllies[i] == null)
                {
                    spawnedAllies.RemoveAt(i);
                }
            }
            
            // Clean up enemy list
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] == null)
                {
                    spawnedEnemies.RemoveAt(i);
                }
            }
            
            // Clean up username mapping
            var keysToRemove = sosigUsernames.Keys.Where(sosig => sosig == null).ToList();
            foreach (var key in keysToRemove)
            {
                sosigUsernames.Remove(key);
            }
        }

        private string GetChatWatcherFilePath(ChatWatcher watcher, bool isAlly)
        {
            try
            {
                var fieldName = isAlly ? "filePathToTextFolder" : "filePathToTextFolderforEnemySosig";
                var field = typeof(ChatWatcher).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    var configEntry = field.GetValue(watcher);
                    if (configEntry != null)
                    {
                        var valueProperty = configEntry.GetType().GetProperty("Value");
                        if (valueProperty != null)
                        {
                            return valueProperty.GetValue(configEntry) as string;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error accessing ChatWatcher file path: {ex.Message}");
            }
            
            return null;
        }

        private GameObject GetChatWatcherPrefab()
        {
            try
            {
                var field = typeof(ChatWatcher).GetField("PrefabToSpawn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(chatWatcher) as GameObject;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error accessing ChatWatcher prefab: {ex.Message}");
            }
            
            return null;
        }

        private void LogStatus(string message)
        {
            Debug.Log($"[TwitchChatSosigManager] {message}");
        }

        private void LogDebug(string message)
        {
            if (EnableDebugLogging.Value)
            {
                Debug.LogFormat($"[TwitchChatSosigManager] DEBUG: {message}");
            }
        }

        // Public API for integration
        public void AddUsernameToAllyQueue(string username)
        {
            if (!string.IsNullOrEmpty(username) && !IsUsernameFiltered(username))
            {
                AddUsernameToQueue(username, true);
            }
        }

        public void AddUsernameToEnemyQueue(string username)
        {
            if (!string.IsNullOrEmpty(username) && !IsUsernameFiltered(username))
            {
                AddUsernameToQueue(username, false);
            }
        }

        public int GetAllyQueueCount() => allyQueue.Count;
        public int GetEnemyQueueCount() => enemyQueue.Count;
        public bool IsInAllyMode() => isAllyMode;
        public int GetActiveAlliesCount() => spawnedAllies.Count;
        public int GetActiveEnemiesCount() => spawnedEnemies.Count;
        
        public string GetSystemStatus()
        {
            return $"Mode: {(isAllyMode ? "Ally" : "Enemy")}, Queues: A{allyQueue.Count}/E{enemyQueue.Count}, Active: A{spawnedAllies.Count}/E{spawnedEnemies.Count}";
        }
        #endregion
    }
}