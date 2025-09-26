using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using BepInEx.Logging;
using System.Collections;
using System.Linq;

namespace H3TVR
{
    /// <summary>
    /// Simple Twitch chat integration example for automatic sosig spawning
    /// This is a basic implementation that can be extended for full Twitch integration
    /// </summary>
    public class SimpleTwitchChatIntegration : MonoBehaviour
    {
        private TwitchChatSosigManager chatSosigManager;
        private ManualLogSource logger;
        
        [Header("Twitch Configuration")]
        public string channel = "your_channel_name";
        public string username = "your_bot_username";
        public string oauth = "oauth:your_oauth_token";
        
        [Header("Chat Commands")]
        public string spawnFriendlyCommand = "!spawnsosig";
        public string spawnEnemyCommand = "!spawnenemy";
        public string armorCommand = "!armor";
        public string clearCommand = "!clear";
        
        private TcpClient tcpClient;
        private NetworkStream stream;
        private Thread chatThread;
        private bool isConnected = false;
        private bool shouldStop = false;

        public void Initialize(TwitchChatSosigManager sosigManager, ManualLogSource logSource)
        {
            chatSosigManager = sosigManager;
            logger = logSource;
            
            // Only connect if credentials are provided
            if (!string.IsNullOrEmpty(channel) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(oauth))
            {
                ConnectToTwitch();
            }
        }

        private void ConnectToTwitch()
        {
            try
            {
                tcpClient = new TcpClient("irc.chat.twitch.tv", 6667);
                stream = tcpClient.GetStream();
                
                // Send authentication
                SendMessage($"PASS {oauth}");
                SendMessage($"NICK {username}");
                SendMessage($"JOIN #{channel}");
                
                isConnected = true;
                
                // Start chat monitoring thread
                chatThread = new Thread(ChatThreadLoop);
                chatThread.Start();
                
                logger.LogInfo($"Connected to Twitch chat for channel: {channel}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to connect to Twitch: {ex.Message}");
            }
        }

        public new void SendMessage(string message)
        {
            if (stream == null || !isConnected) return;
            
            byte[] data = System.Text.Encoding.UTF8.GetBytes($"{message}\r\n");
            stream.Write(data, 0, data.Length);
        }

        private void ChatThreadLoop()
        {
            byte[] buffer = new byte[1024];
            
            while (isConnected && !shouldStop)
            {
                try
                {
                    if (stream.DataAvailable)
                    {
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ProcessChatMessage(message);
                    }
                    
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Chat thread error: {ex.Message}");
                    break;
                }
            }
        }

        private void ProcessChatMessage(string message)
        {
            string[] lines = message.Split('\n');
            
            foreach (string line in lines)
            {
                if (IsNullOrWhiteSpace(line)) continue;
                
                // Handle PING/PONG
                if (line.StartsWith("PING"))
                {
                    SendMessage(line.Replace("PING", "PONG"));
                    continue;
                }
                
                // Parse chat messages
                if (line.Contains("PRIVMSG"))
                {
                    ParseChatCommand(line);
                }
            }
        }

        private bool IsNullOrWhiteSpace(string value)
        {
            return string.IsNullOrEmpty(value) || value.Trim().Length == 0;
        }

        private void ParseChatCommand(string line)
        {
            try
            {
                // Extract username and message from IRC format
                // Format: :username!username@username.tmi.twitch.tv PRIVMSG #channel :message
                int usernameStart = line.IndexOf(':') + 1;
                int usernameEnd = line.IndexOf('!');
                if (usernameStart < 0 || usernameEnd < 0) return;
                
                string userName = line.Substring(usernameStart, usernameEnd - usernameStart);
                
                int messageStart = line.LastIndexOf(':') + 1;
                if (messageStart < 0) return;
                
                string chatMessage = line.Substring(messageStart).Trim();
                
                // Process commands
                ProcessCommand(userName, chatMessage);
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to parse chat command: {ex.Message}");
            }
        }

        private void ProcessCommand(string userName, string message)
        {
            string[] parts = message.Split(' ');
            string command = parts[0].ToLower();
            
            switch (command)
            {
                case var cmd when cmd == spawnFriendlyCommand.ToLower():
                    QueueFriendlySpawn(userName, parts);
                    break;
                    
                case var cmd when cmd == spawnEnemyCommand.ToLower():
                    QueueEnemySpawn(userName, parts);
                    break;
                    
                case var cmd when cmd == armorCommand.ToLower():
                    HandleArmorCommand(userName, parts);
                    break;
                    
                case var cmd when cmd == clearCommand.ToLower():
                    HandleClearCommand(userName);
                    break;
            }
        }

        private void QueueFriendlySpawn(string userName, string[] parts)
        {
            string armorSet = parts.Length > 1 ? parts[1] : null;
            
            // Queue the spawn on the main thread
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                chatSosigManager?.QueueChatSpawn(userName, true, armorSet);
                logger.LogInfo($"Queued friendly sosig spawn for {userName}");
            });
        }

        private void QueueEnemySpawn(string userName, string[] parts)
        {
            string armorSet = parts.Length > 1 ? parts[1] : null;
            
            // Queue the spawn on the main thread
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                chatSosigManager?.QueueChatSpawn(userName, false, armorSet);
                logger.LogInfo($"Queued enemy sosig spawn for {userName}");
            });
        }

        private void HandleArmorCommand(string userName, string[] parts)
        {
            if (parts.Length < 2) return;
            
            string armorSet = parts[1];
            
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var availableArmor = chatSosigManager?.GetAvailableArmorSets();
                if (availableArmor != null && availableArmor.Contains(armorSet))
                {
                    logger.LogInfo($"{userName} selected armor set: {armorSet}");
                    // The armor set will be used for their next spawn
                }
                else
                {
                    logger.LogInfo($"Invalid armor set '{armorSet}' requested by {userName}");
                }
            });
        }

        private void HandleClearCommand(string userName)
        {
            // Only allow certain users to clear (moderators, broadcaster, etc.)
            // This is a simplified example - in a real implementation you'd check user privileges
            
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                chatSosigManager?.ClearAllChatSosigs();
                logger.LogInfo($"{userName} cleared all chat sosigs");
            });
        }

        void OnDestroy()
        {
            shouldStop = true;
            isConnected = false;
            
            if (chatThread != null && chatThread.IsAlive)
            {
                chatThread.Join(1000);
            }
            
            stream?.Close();
            tcpClient?.Close();
        }

        // File-based fallback for when Twitch integration is not available
        public void MonitorChatFiles()
        {
            // This method can be used to monitor file changes for chat integration
            // as a fallback when direct Twitch connection is not available
            StartCoroutine(MonitorChatFilesCoroutine());
        }

        private IEnumerator MonitorChatFilesCoroutine()
        {
            string friendlyFilePath = "";
            string enemyFilePath = "";
            
            // Get file paths safely
            if (chatSosigManager?.GetPlugin() != null)
            {
                friendlyFilePath = chatSosigManager.GetPlugin().GetTwitchChatFilePath();
                enemyFilePath = chatSosigManager.GetPlugin().GetTwitchEnemyChatFilePath();
            }
            
            DateTime lastFriendlyWrite = DateTime.MinValue;
            DateTime lastEnemyWrite = DateTime.MinValue;
            
            while (true)
            {
                // Check friendly file
                if (File.Exists(friendlyFilePath))
                {
                    DateTime currentFriendlyWrite = File.GetLastWriteTime(friendlyFilePath);
                    if (currentFriendlyWrite > lastFriendlyWrite)
                    {
                        lastFriendlyWrite = currentFriendlyWrite;
                        ProcessFileBasedSpawn(friendlyFilePath, true);
                    }
                }
                
                // Check enemy file
                if (File.Exists(enemyFilePath))
                {
                    DateTime currentEnemyWrite = File.GetLastWriteTime(enemyFilePath);
                    if (currentEnemyWrite > lastEnemyWrite)
                    {
                        lastEnemyWrite = currentEnemyWrite;
                        ProcessFileBasedSpawn(enemyFilePath, false);
                    }
                }
                
                yield return new WaitForSeconds(1f); // Check every second
            }
        }

        private void ProcessFileBasedSpawn(string filePath, bool isFriendly)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                
                // Extract username from JSON-like format: {"username":"UserName"}
                int startIndex = content.IndexOf('"', content.IndexOf("username") + 8) + 1;
                int endIndex = content.LastIndexOf('"') - 1;
                
                if (startIndex > 0 && endIndex > startIndex)
                {
                    string userName = content.Substring(startIndex, endIndex - startIndex + 1);
                    chatSosigManager?.QueueChatSpawn(userName, isFriendly);
                    
                    logger.LogInfo($"File-based spawn queued for {userName} ({(isFriendly ? "friendly" : "enemy")})");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to process file-based spawn from {filePath}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Simple dispatcher to run actions on Unity's main thread
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private readonly System.Collections.Generic.Queue<System.Action> _executionQueue = new System.Collections.Generic.Queue<System.Action>();

        public static UnityMainThreadDispatcher Instance()
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }

        public void Enqueue(System.Action action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        void Update()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }
    }
}