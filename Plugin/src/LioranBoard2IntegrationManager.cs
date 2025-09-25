using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using FistVR;

namespace H3TVR
{
    public class LioranBoard2IntegrationManager : MonoBehaviour
    {
        #region Configuration
        public static ConfigEntry<int> HttpPort;
        public static ConfigEntry<int> MaxQueueSize;
        public static ConfigEntry<KeyCode> SpawnAllyWithUsernameKey;
        public static ConfigEntry<KeyCode> SpawnEnemyWithUsernameKey;
        public static ConfigEntry<bool> EnableLioranBoardIntegration;
        public static ConfigEntry<bool> LogHttpRequests;
        #endregion

        #region Private Variables
        private HttpListener httpListener;
        private Thread httpListenerThread;
        private bool isListening = false;
        
        // Username queues for ally and enemy sosigs
        private Queue<string> allyUsernameQueue = new Queue<string>();
        private Queue<string> enemyUsernameQueue = new Queue<string>();
        
        // Recent chatters tracking
        private List<string> recentChatters = new List<string>();
        private const int MAX_RECENT_CHATTERS = 100;
        
        // Integration with existing systems
        private SosigSpawnerManager sosigManager;
        private ChatWatcher chatWatcher;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            InitializeConfiguration();
            
            if (EnableLioranBoardIntegration.Value)
            {
                StartHttpServer();
            }
            
            // Find existing components
            sosigManager = FindObjectOfType<SosigSpawnerManager>();
            chatWatcher = jediSpawner.ChatWatcher.instance;
        }

        void Update()
        {
            if (!EnableLioranBoardIntegration.Value)
                return;

            // Handle keyboard shortcuts
            if (Input.GetKeyDown(SpawnAllyWithUsernameKey.Value))
            {
                SpawnAllyWithUsername();
            }
            
            if (Input.GetKeyDown(SpawnEnemyWithUsernameKey.Value))
            {
                SpawnEnemyWithUsername();
            }
        }

        void OnDestroy()
        {
            StopHttpServer();
        }
        #endregion

        #region Configuration
        private void InitializeConfiguration()
        {
            var plugin = FindObjectOfType<H3TVR>();
            if (plugin == null) return;

            HttpPort = plugin.Config.Bind("LioranBoard2",
                "HttpPort",
                8080,
                "Port for HTTP server to receive LioranBoard 2 commands");

            MaxQueueSize = plugin.Config.Bind("LioranBoard2",
                "MaxQueueSize", 
                50,
                "Maximum number of usernames to keep in ally/enemy queues");

            SpawnAllyWithUsernameKey = plugin.Config.Bind("LioranBoard2",
                "SpawnAllyWithUsernameKey",
                KeyCode.F1,
                "Key to spawn ally sosig with username from queue");

            SpawnEnemyWithUsernameKey = plugin.Config.Bind("LioranBoard2",
                "SpawnEnemyWithUsernameKey", 
                KeyCode.F2,
                "Key to spawn enemy sosig with username from queue");

            EnableLioranBoardIntegration = plugin.Config.Bind("LioranBoard2",
                "EnableIntegration",
                true,
                "Enable LioranBoard 2 integration system");

            LogHttpRequests = plugin.Config.Bind("LioranBoard2",
                "LogHttpRequests",
                false,
                "Log all HTTP requests for debugging");
        }
        #endregion

        #region HTTP Server
        private void StartHttpServer()
        {
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://localhost:{HttpPort.Value}/");
                httpListener.Start();
                isListening = true;

                httpListenerThread = new Thread(HandleHttpRequests);
                httpListenerThread.Start();

                Debug.Log($"LioranBoard 2 HTTP server started on port {HttpPort.Value}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start HTTP server: {ex.Message}");
            }
        }

        private void StopHttpServer()
        {
            try
            {
                isListening = false;
                
                if (httpListener != null && httpListener.IsListening)
                {
                    httpListener.Stop();
                    httpListener.Close();
                }

                if (httpListenerThread != null && httpListenerThread.IsAlive)
                {
                    httpListenerThread.Join(1000);
                    if (httpListenerThread.IsAlive)
                    {
                        httpListenerThread.Abort();
                    }
                }

                Debug.Log("LioranBoard 2 HTTP server stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error stopping HTTP server: {ex.Message}");
            }
        }

        private void HandleHttpRequests()
        {
            while (isListening && httpListener != null)
            {
                try
                {
                    var context = httpListener.GetContext();
                    ProcessRequest(context);
                }
                catch (HttpListenerException)
                {
                    // Expected when stopping the listener
                    break;
                }
                catch (Exception ex)
                {
                    if (isListening)
                    {
                        Debug.LogError($"HTTP request handling error: {ex.Message}");
                    }
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                if (LogHttpRequests.Value)
                {
                    Debug.Log($"HTTP {request.HttpMethod} {request.Url.AbsolutePath}");
                }

                // Only accept POST requests
                if (request.HttpMethod != "POST")
                {
                    SendErrorResponse(response, 405, "Method not allowed");
                    return;
                }

                // Read request body
                string requestBody;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    requestBody = reader.ReadToEnd();
                }

                // Parse and handle the request
                var jsonResponse = HandleLioranBoardCommand(requestBody);
                SendJsonResponse(response, jsonResponse);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing HTTP request: {ex.Message}");
                try
                {
                    SendErrorResponse(context.Response, 500, "Internal server error");
                }
                catch
                {
                    // Ignore response errors
                }
            }
        }

        private void SendJsonResponse(HttpListenerResponse response, object data)
        {
            try
            {
                response.ContentType = "application/json";
                response.StatusCode = 200;

                string json = SimpleJsonSerializer.Serialize(data);
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending JSON response: {ex.Message}");
            }
        }

        private void SendErrorResponse(HttpListenerResponse response, int statusCode, string message)
        {
            try
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";

                var errorResponse = new { error = message, status = statusCode };
                string json = SimpleJsonSerializer.Serialize(errorResponse);
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending error response: {ex.Message}");
            }
        }
        #endregion

        #region LioranBoard Command Handling
        private object HandleLioranBoardCommand(string requestBody)
        {
            try
            {
                var request = SimpleJsonParser.ParseLioranBoardRequest(requestBody);
                
                if (request == null || string.IsNullOrEmpty(request.command))
                {
                    return new { success = false, error = "Invalid request format" };
                }

                switch (request.command.ToLower())
                {
                    case "spawn_ally":
                        return HandleSpawnAlly(request);
                    case "spawn_enemy":
                        return HandleSpawnEnemy(request);
                    case "add_to_ally_queue":
                        return HandleAddToAllyQueue(request);
                    case "add_to_enemy_queue":
                        return HandleAddToEnemyQueue(request);
                    case "get_queue_status":
                        return HandleGetQueueStatus();
                    case "clear_queues":
                        return HandleClearQueues();
                    default:
                        return new { success = false, error = $"Unknown command: {request.command}" };
                }
            }
            catch (Exception ex)
            {
                return new { success = false, error = $"Command processing error: {ex.Message}" };
            }
        }

        private object HandleSpawnAlly(LioranBoardRequest request)
        {
            string username = GetUsernameForSpawn(request, true);
            if (string.IsNullOrEmpty(username))
            {
                return new { success = false, error = "No username available for ally spawn" };
            }

            // Queue the spawn on the main thread
            StartCoroutine(SpawnSosigWithUsername(username, false));
            
            return new { 
                success = true, 
                message = $"Spawning ally sosig with username: {username}",
                username = username,
                type = "ally"
            };
        }

        private object HandleSpawnEnemy(LioranBoardRequest request)
        {
            string username = GetUsernameForSpawn(request, false);
            if (string.IsNullOrEmpty(username))
            {
                return new { success = false, error = "No username available for enemy spawn" };
            }

            // Queue the spawn on the main thread
            StartCoroutine(SpawnSosigWithUsername(username, true));
            
            return new { 
                success = true, 
                message = $"Spawning enemy sosig with username: {username}",
                username = username,
                type = "enemy"
            };
        }

        private object HandleAddToAllyQueue(LioranBoardRequest request)
        {
            if (string.IsNullOrEmpty(request.username))
            {
                return new { success = false, error = "Username is required" };
            }

            AddToAllyQueue(request.username);
            return new { 
                success = true, 
                message = $"Added {request.username} to ally queue",
                queueSize = allyUsernameQueue.Count 
            };
        }

        private object HandleAddToEnemyQueue(LioranBoardRequest request)
        {
            if (string.IsNullOrEmpty(request.username))
            {
                return new { success = false, error = "Username is required" };
            }

            AddToEnemyQueue(request.username);
            return new { 
                success = true, 
                message = $"Added {request.username} to enemy queue",
                queueSize = enemyUsernameQueue.Count 
            };
        }

        private object HandleGetQueueStatus()
        {
            return new {
                success = true,
                allyQueueSize = allyUsernameQueue.Count,
                enemyQueueSize = enemyUsernameQueue.Count,
                recentChattersCount = recentChatters.Count,
                maxQueueSize = MaxQueueSize.Value
            };
        }

        private object HandleClearQueues()
        {
            int allyCount = allyUsernameQueue.Count;
            int enemyCount = enemyUsernameQueue.Count;
            
            allyUsernameQueue.Clear();
            enemyUsernameQueue.Clear();
            
            return new {
                success = true,
                message = $"Cleared {allyCount} ally and {enemyCount} enemy usernames from queues"
            };
        }
        #endregion

        #region Public API Methods
        /// <summary>
        /// Public method for external systems to add usernames to ally queue
        /// </summary>
        /// <param name="username">Twitch username to add</param>
        /// <returns>True if added successfully</returns>
        public static bool AddUsernameToAllyQueue(string username)
        {
            var instance = FindObjectOfType<LioranBoard2IntegrationManager>();
            if (instance != null)
            {
                instance.AddToAllyQueue(username);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Public method for external systems to add usernames to enemy queue
        /// </summary>
        /// <param name="username">Twitch username to add</param>
        /// <returns>True if added successfully</returns>
        public static bool AddUsernameToEnemyQueue(string username)
        {
            var instance = FindObjectOfType<LioranBoard2IntegrationManager>();
            if (instance != null)
            {
                instance.AddToEnemyQueue(username);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Public method for external systems to trigger ally spawning
        /// </summary>
        /// <param name="username">Optional specific username to use</param>
        /// <returns>True if spawn was triggered</returns>
        public static bool TriggerAllySpawn(string username = null)
        {
            var instance = FindObjectOfType<LioranBoard2IntegrationManager>();
            if (instance != null)
            {
                var request = new LioranBoard2IntegrationManager.LioranBoardRequest { username = username };
                string targetUsername = instance.GetUsernameForSpawn(request, true);
                if (!string.IsNullOrEmpty(targetUsername))
                {
                    instance.StartCoroutine(instance.SpawnSosigWithUsername(targetUsername, false));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Public method for external systems to trigger enemy spawning
        /// </summary>
        /// <param name="username">Optional specific username to use</param>
        /// <returns>True if spawn was triggered</returns>
        public static bool TriggerEnemySpawn(string username = null)
        {
            var instance = FindObjectOfType<LioranBoard2IntegrationManager>();
            if (instance != null)
            {
                var request = new LioranBoard2IntegrationManager.LioranBoardRequest { username = username };
                string targetUsername = instance.GetUsernameForSpawn(request, false);
                if (!string.IsNullOrEmpty(targetUsername))
                {
                    instance.StartCoroutine(instance.SpawnSosigWithUsername(targetUsername, true));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get current queue status for external systems
        /// </summary>
        /// <returns>Queue status information</returns>
        public static QueueStatus GetQueueStatus()
        {
            var instance = FindObjectOfType<LioranBoard2IntegrationManager>();
            if (instance != null)
            {
                return new QueueStatus
                {
                    allyQueueSize = instance.allyUsernameQueue.Count,
                    enemyQueueSize = instance.enemyUsernameQueue.Count,
                    recentChattersCount = instance.recentChatters.Count,
                    maxQueueSize = MaxQueueSize?.Value ?? 50
                };
            }
            return new QueueStatus();
        }

        [System.Serializable]
        public struct QueueStatus
        {
            public int allyQueueSize;
            public int enemyQueueSize;
            public int recentChattersCount;
            public int maxQueueSize;
        }
        #endregion
        private string GetUsernameForSpawn(LioranBoard2IntegrationManager.LioranBoardRequest request, bool isAlly)
        {
            // First, check if a specific username was provided
            if (!string.IsNullOrEmpty(request.username))
            {
                return request.username;
            }

            // Then check the appropriate queue
            Queue<string> targetQueue = isAlly ? allyUsernameQueue : enemyUsernameQueue;
            if (targetQueue.Count > 0)
            {
                return targetQueue.Dequeue();
            }

            // Fall back to recent chatters
            if (recentChatters.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, recentChatters.Count);
                return recentChatters[randomIndex];
            }

            return null;
        }

        public void AddToAllyQueue(string username)
        {
            if (string.IsNullOrEmpty(username)) return;

            // Ensure queue doesn't exceed max size
            while (allyUsernameQueue.Count >= MaxQueueSize.Value)
            {
                allyUsernameQueue.Dequeue();
            }

            allyUsernameQueue.Enqueue(username);
            AddToRecentChatters(username);
        }

        public void AddToEnemyQueue(string username)
        {
            if (string.IsNullOrEmpty(username)) return;

            // Ensure queue doesn't exceed max size
            while (enemyUsernameQueue.Count >= MaxQueueSize.Value)
            {
                enemyUsernameQueue.Dequeue();
            }

            enemyUsernameQueue.Enqueue(username);
            AddToRecentChatters(username);
        }

        public void AddToRecentChatters(string username)
        {
            if (string.IsNullOrEmpty(username)) return;

            // Remove if already exists to avoid duplicates
            recentChatters.Remove(username);
            
            // Add to front of list
            recentChatters.Insert(0, username);

            // Maintain max size
            while (recentChatters.Count > MAX_RECENT_CHATTERS)
            {
                recentChatters.RemoveAt(recentChatters.Count - 1);
            }
        }
        #endregion

        #region Public API Methods
        /// <summary>
        /// Public method for external systems to add usernames to ally queue
        /// </summary>
        /// <param name="username">Twitch username to add</param>
        /// <returns>True if added successfully</returns>
        public static bool AddUsernameToAllyQueue(string username)
        {
            var instance = FindObjectOfType<LioranBoard2IntegrationManager>();
            if (instance != null)
            {
                instance.AddToAllyQueue(username);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Public method for external systems to add usernames to enemy queue
        /// </summary>
        /// <param name="username">Twitch username to add</param>
        /// <returns>True if added successfully</returns>
        public static bool AddUsernameToEnemyQueue(string username)
        {
            var instance = FindObjectOfType<LioranBoard2IntegrationManager>();
            if (instance != null)
            {
                instance.AddToEnemyQueue(username);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Public method for external systems to trigger ally spawning
        /// </summary>
        /// <param name="username">Optional specific username to use</param>
        /// <returns>True if spawn was triggered</returns>
        public static bool TriggerAllySpawn(string username = null)
        {
            var instance = FindObjectOfType<LioranBoard2IntegrationManager>();
            if (instance != null)
            {
                var request = new LioranBoard2IntegrationManager.LioranBoardRequest { username = username };
                string targetUsername = instance.GetUsernameForSpawn(request, true);
                if (!string.IsNullOrEmpty(targetUsername))
                {
                    instance.StartCoroutine(instance.SpawnSosigWithUsername(targetUsername, false));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Public method for external systems to trigger enemy spawning
        /// </summary>
        /// <param name="username">Optional specific username to use</param>
        /// <returns>True if spawn was triggered</returns>
        public static bool TriggerEnemySpawn(string username = null)
        {
            var instance = FindObjectOfType<LioranBoard2IntegrationManager>();
            if (instance != null)
            {
                var request = new LioranBoard2IntegrationManager.LioranBoardRequest { username = username };
                string targetUsername = instance.GetUsernameForSpawn(request, false);
                if (!string.IsNullOrEmpty(targetUsername))
                {
                    instance.StartCoroutine(instance.SpawnSosigWithUsername(targetUsername, true));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get current queue status for external systems
        /// </summary>
        /// <returns>Queue status information</returns>
        public static QueueStatus GetQueueStatus()
        {
            var instance = FindObjectOfType<LioranBoard2IntegrationManager>();
            if (instance != null)
            {
                return new QueueStatus
                {
                    allyQueueSize = instance.allyUsernameQueue.Count,
                    enemyQueueSize = instance.enemyUsernameQueue.Count,
                    recentChattersCount = instance.recentChatters.Count,
                    maxQueueSize = MaxQueueSize?.Value ?? 50
                };
            }
            return new QueueStatus();
        }

        [System.Serializable]
        public struct QueueStatus
        {
            public int allyQueueSize;
            public int enemyQueueSize;
            public int recentChattersCount;
            public int maxQueueSize;
        }
        #endregion

        #region Sosig Spawning Integration
        private void SpawnAllyWithUsername()
        {
            string username = GetUsernameForSpawn(new LioranBoard2IntegrationManager.LioranBoardRequest(), true);
            if (!string.IsNullOrEmpty(username))
            {
                StartCoroutine(SpawnSosigWithUsername(username, false));
            }
            else
            {
                Debug.LogWarning("No username available for ally spawn");
            }
        }

        private void SpawnEnemyWithUsername()
        {
            string username = GetUsernameForSpawn(new LioranBoard2IntegrationManager.LioranBoardRequest(), false);
            if (!string.IsNullOrEmpty(username))
            {
                StartCoroutine(SpawnSosigWithUsername(username, true));
            }
            else
            {
                Debug.LogWarning("No username available for enemy spawn");
            }
        }

        private IEnumerator SpawnSosigWithUsername(string username, bool isEnemy)
        {
            // Wait for next frame to ensure we're on the main thread
            yield return null;

            try
            {
                // Set the username in ChatWatcher for the spawning system to use
                if (chatWatcher != null)
                {
                    chatWatcher.SpawnerName = username;
                    Debug.Log($"Set username for spawning: {username}");
                    
                    // Also write to file paths if they're configured (for compatibility)
                    WriteUsernameToFile(username, isEnemy);
                }

                if (sosigManager != null)
                {
                    // Use the existing SosigSpawnerManager to spawn sosigs
                    if (isEnemy)
                    {
                        sosigManager.GetType().GetMethod("QuickSpawnEnemy", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.Invoke(sosigManager, null);
                    }
                    else
                    {
                        sosigManager.GetType().GetMethod("QuickSpawnAlly", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.Invoke(sosigManager, null);
                    }

                    Debug.Log($"Spawned {(isEnemy ? "enemy" : "ally")} sosig with username: {username}");
                }
                else
                {
                    // Fallback to the original ChatWatcher system
                    if (chatWatcher != null)
                    {
                        SpawnUsingChatWatcher(username, isEnemy);
                    }
                    else
                    {
                        Debug.LogWarning("Neither SosigSpawnerManager nor ChatWatcher found - cannot spawn sosig");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error spawning sosig with username {username}: {ex.Message}");
            }
        }

        private void WriteUsernameToFile(string username, bool isEnemy)
        {
            try
            {
                if (chatWatcher == null) return;

                // Get the file paths from ChatWatcher configuration
                var filePathField = isEnemy ? 
                    chatWatcher.GetType().GetField("filePathToTextFolderforEnemySosig", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) :
                    chatWatcher.GetType().GetField("filePathToTextFolder", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (filePathField != null)
                {
                    var configEntry = filePathField.GetValue(chatWatcher);
                    if (configEntry != null)
                    {
                        // Get the Value property from ConfigEntry
                        var valueProperty = configEntry.GetType().GetProperty("Value");
                        if (valueProperty != null)
                        {
                            string filePath = valueProperty.GetValue(configEntry) as string;
                            if (!string.IsNullOrEmpty(filePath) && filePath != "null")
                            {
                                // Write username in the expected JSON format
                                string jsonContent = $"\"{{\\\"username\\\":\\\"{username}\\\"}}\"";
                                File.WriteAllText(filePath, jsonContent);
                                Debug.Log($"Written username {username} to file: {filePath}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not write username to file: {ex.Message}");
            }
        }

        private void SpawnUsingChatWatcher(string username, bool isEnemy)
        {
            try
            {
                if (chatWatcher == null) return;

                // Set the username
                chatWatcher.SpawnerName = username;

                // Get spawn position
                Vector3 spawnPoint = new Vector3(
                    GM.CurrentPlayerBody.Head.transform.position.x, 
                    GM.CurrentPlayerBody.transform.position.y, 
                    GM.CurrentPlayerBody.Head.transform.position.z + 1
                );

                // Find a ChatSpawner prefab to instantiate
                var prefabField = chatWatcher.GetType().GetField("PrefabToSpawn", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                
                if (prefabField != null)
                {
                    GameObject prefab = prefabField.GetValue(chatWatcher) as GameObject;
                    if (prefab != null)
                    {
                        var spawnerObject = Instantiate(prefab, spawnPoint, Quaternion.identity);
                        var chatSpawner = spawnerObject.GetComponent<jediSpawner.ChatSpawner>();
                        
                        if (chatSpawner != null)
                        {
                            if (isEnemy)
                            {
                                chatSpawner.SpawningSequenceEnemy(1); // Enemy IFF
                            }
                            else
                            {
                                chatSpawner.SpawningSequence(); // Ally spawn
                            }
                        }
                    }
                }
                
                Debug.Log($"Spawned {(isEnemy ? "enemy" : "ally")} sosig using ChatWatcher with username: {username}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error spawning with ChatWatcher: {ex.Message}");
            }
        }
        #endregion

        #region Data Classes
        [System.Serializable]
        public class LioranBoardRequest
        {
            public string command;
            public string username;
            public Dictionary<string, object> parameters;
        }
        #endregion
    }

    #region Simple JSON Utilities
    public static class SimpleJsonSerializer
    {
        public static string Serialize(object obj)
        {
            if (obj == null) return "null";

            var type = obj.GetType();
            
            if (type == typeof(string))
            {
                return $"\"{EscapeString(obj.ToString())}\"";
            }
            
            if (type == typeof(bool))
            {
                return obj.ToString().ToLower();
            }
            
            if (type == typeof(int) || type == typeof(float) || type == typeof(double))
            {
                return obj.ToString();
            }

            // Handle anonymous objects and dictionaries
            var properties = type.GetProperties();
            if (properties.Length > 0)
            {
                var parts = new List<string>();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(obj, null);
                    var key = prop.Name;
                    parts.Add($"\"{key}\":{Serialize(value)}");
                }
                return "{" + string.Join(",", parts.ToArray()) + "}";
            }

            return $"\"{EscapeString(obj.ToString())}\"";
        }

        private static string EscapeString(string str)
        {
            if (str == null) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }

    public static class SimpleJsonParser
    {
        public static LioranBoard2IntegrationManager.LioranBoardRequest ParseLioranBoardRequest(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                var request = new LioranBoard2IntegrationManager.LioranBoardRequest();
                
                // Simple regex-based parsing for basic JSON
                var commandMatch = System.Text.RegularExpressions.Regex.Match(json, @"""command""\s*:\s*""([^""]+)""");
                if (commandMatch.Success)
                {
                    request.command = commandMatch.Groups[1].Value;
                }

                var usernameMatch = System.Text.RegularExpressions.Regex.Match(json, @"""username""\s*:\s*""([^""]+)""");
                if (usernameMatch.Success)
                {
                    request.username = usernameMatch.Groups[1].Value;
                }

                return request;
            }
            catch (Exception ex)
            {
                Debug.LogError($"JSON parsing error: {ex.Message}");
                return null;
            }
        }
    }
    #endregion
}