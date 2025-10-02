using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Valve.Newtonsoft.Json;
using FistVR;

namespace H3TVR
{
    /// <summary>
    /// Custom Twitch IRC client for .NET Framework 3.5 compatibility
    /// Handles OAuth login, real-time chat integration, and sosig spawning commands
    /// </summary>
    public class TwitchChatManager : MonoBehaviour
    {
        #region Static Instance
        public static TwitchChatManager Instance { get; private set; }
        #endregion

        #region Core Components
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        private EnhancedChatSpawner chatSpawner;
        #endregion

        #region Twitch IRC Connection
        private TcpClient tcpClient;
        private NetworkStream stream;
        private Thread connectionThread;
        private bool isRunning;
        private readonly object connectionLock = new object();
        
        // Twitch IRC server details
        private const string TwitchIrcServer = "irc.chat.twitch.tv";
        private const int TwitchIrcPort = 6667;
        private const string TwitchIrcCapabilities = "CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands";
        #endregion

        #region Configuration
        private ConfigEntry<string> twitchUsername;
        private ConfigEntry<string> twitchOAuthToken;
        private ConfigEntry<string> twitchChannel;
        private ConfigEntry<bool> enableTwitchIntegration;
        private ConfigEntry<bool> requireModeratorForCommands;
        private ConfigEntry<bool> requireSubscriberForCommands;
        private ConfigEntry<float> commandCooldownSeconds;
        private ConfigEntry<int> maxSosigsPerUser;
        private ConfigEntry<bool> enableChatLogging;
        private ConfigEntry<string> allowedCommands;
        private ConfigEntry<bool> autoConnectOnStartup;
        private ConfigEntry<string> sosigSpawnCommand;
        private ConfigEntry<string> enemySosigSpawnCommand;
        private ConfigEntry<string> clearSosigsCommand;
        private ConfigEntry<bool> allowViewersToSpawn;
        private ConfigEntry<bool> enableCustomArmorCommands;
        #endregion

        #region State Management
        private bool isConnected;
        private bool isConnecting;
        private bool loginInProgress;
        private DateTime lastConnectionAttempt;
        private int connectionRetryCount;
        private const int MaxRetryAttempts = 5;
        private const float RetryDelaySeconds = 30f;
        
        private readonly Dictionary<string, DateTime> userCooldowns = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, int> userSpawnCounts = new Dictionary<string, int>();
        private readonly HashSet<string> moderators = new HashSet<string>();
        private readonly HashSet<string> subscribers = new HashSet<string>();
        private readonly List<string> messageQueue = new List<string>();
        #endregion

        #region Events
        public static event Action<string, string> OnChatMessage; // username, message
        public static event Action<string> OnUserJoined; // username
        public static event Action<string> OnUserLeft; // username
        public static event Action OnConnected;
        public static event Action OnDisconnected;
        public static event Action<string, bool> OnSosigSpawnRequest; // username, isFriendly
        #endregion

        #region Authentication Data
        [Serializable]
        public class TwitchAuthData
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public string Username { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool IsValid => !string.IsNullOrEmpty(AccessToken) && DateTime.Now < ExpiresAt;
        }

        private TwitchAuthData authData;
        private string authDataPath;
        #endregion

        #region GUI System
        private bool showTwitchGUI;
        private Rect twitchWindowRect = new Rect(100, 100, 600, 700);
        private Vector2 scrollPosition;
        private GUIStyle windowStyle, buttonStyle, labelStyle, textFieldStyle, toggleStyle, headerStyle, sectionStyle, infoStyle;
        private string usernameInput = "";
        private string tokenInput = "";
        private string channelInput = "";
        private List<string> recentMessages = new List<string>();
        private const int MaxRecentMessages = 20;
        #endregion

        #region Initialization
        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource, EnhancedChatSpawner spawner)
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            plugin = pluginInstance;
            logger = logSource;
            chatSpawner = spawner;

            InitializeConfiguration();
            SetupAuthDataPath();
            LoadAuthData();

            logger?.LogInfo("TwitchChatManager initialized (Custom IRC Client for .NET 3.5)");

            // Auto-connect if enabled and we have valid auth
            if (autoConnectOnStartup.Value && authData?.IsValid == true)
            {
                StartCoroutine(DelayedAutoConnect());
            }
        }

        private void InitializeConfiguration()
        {
            var config = plugin.Config;

            // Core Twitch settings
            twitchUsername = config.Bind("Twitch Integration", "Username", "", 
                "Twitch username (will be auto-filled after OAuth login)");
            twitchOAuthToken = config.Bind("Twitch Integration", "OAuthToken", "", 
                "OAuth token (will be auto-generated during login process)");
            twitchChannel = config.Bind("Twitch Integration", "Channel", "", 
                "Twitch channel to connect to (usually your own channel)");
            
            // Feature toggles
            enableTwitchIntegration = config.Bind("Twitch Integration", "EnableTwitchIntegration", true, 
                "Enable Twitch chat integration");
            autoConnectOnStartup = config.Bind("Twitch Integration", "AutoConnectOnStartup", false, 
                "Automatically connect to Twitch on plugin startup");
            enableChatLogging = config.Bind("Twitch Integration", "EnableChatLogging", true, 
                "Log chat messages to console");
            
            // Permission settings
            requireModeratorForCommands = config.Bind("Twitch Integration", "RequireModeratorForCommands", false, 
                "Require moderator status for spawn commands");
            requireSubscriberForCommands = config.Bind("Twitch Integration", "RequireSubscriberForCommands", false, 
                "Require subscriber status for spawn commands");
            allowViewersToSpawn = config.Bind("Twitch Integration", "AllowViewersToSpawn", true, 
                "Allow regular viewers to spawn sosigs");
            
            // Rate limiting
            commandCooldownSeconds = config.Bind("Twitch Integration", "CommandCooldownSeconds", 30f, 
                "Cooldown between commands per user");
            maxSosigsPerUser = config.Bind("Twitch Integration", "MaxSosigsPerUser", 2, 
                "Maximum sosigs each user can have active");
            
            // Commands
            sosigSpawnCommand = config.Bind("Twitch Integration", "SosigSpawnCommand", "!ally", 
                "Command to spawn friendly sosig");
            enemySosigSpawnCommand = config.Bind("Twitch Integration", "EnemySosigSpawnCommand", "!enemy", 
                "Command to spawn enemy sosig");
            clearSosigsCommand = config.Bind("Twitch Integration", "ClearSosigsCommand", "!clear", 
                "Command to clear all sosigs (mods only)");
            enableCustomArmorCommands = config.Bind("Twitch Integration", "EnableCustomArmorCommands", true, 
                "Enable custom armor selection commands");
            
            allowedCommands = config.Bind("Twitch Integration", "AllowedCommands", 
                "!ally,!enemy,!clear,!help,!stats", 
                "Comma-separated list of allowed commands");

            // Initialize Channel Points configuration
            InitializeChannelPointsConfig();
        }

        private void SetupAuthDataPath()
        {
            try
            {
                string configDir = Path.Combine(Path.GetDirectoryName(plugin.Config.ConfigFilePath), "config");
                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);

                authDataPath = Path.Combine(configDir, "H3TVR_TwitchAuth.json");
                logger?.LogInfo($"Auth data will be stored at: {authDataPath}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to setup auth data path: {ex.Message}");
                authDataPath = "H3TVR_TwitchAuth.json";
            }
        }

        private IEnumerator DelayedAutoConnect()
        {
            yield return new WaitForSeconds(3f); // Wait for other systems to initialize
            
            if (authData?.IsValid == true)
            {
                ConnectToTwitch();
            }
        }
        #endregion

        #region Unity Lifecycle
        void Update()
        {
            // Handle input
            if (Input.GetKeyDown(KeyCode.F8))
            {
                ToggleTwitchGUI();
            }

            // Process message queue on main thread
            ProcessMessageQueue();

            // Handle connection retries
            if (!isConnected && !isConnecting && connectionRetryCount < MaxRetryAttempts)
            {
                if (Time.time - (float)lastConnectionAttempt.Subtract(DateTime.MinValue).TotalSeconds > RetryDelaySeconds)
                {
                    if (authData?.IsValid == true && enableTwitchIntegration.Value)
                    {
                        RetryConnection();
                    }
                }
            }

            // Clean up cooldowns
            CleanupExpiredCooldowns();
        }

        void OnDestroy()
        {
            DisconnectFromTwitch();
            
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Authentication System
        /// <summary>
        /// Start the OAuth login process
        /// </summary>
        public void StartTwitchLogin()
        {
            if (loginInProgress)
            {
                logger?.LogWarning("Login already in progress");
                return;
            }

            StartCoroutine(TwitchOAuthFlow());
        }

        /// <summary>
        /// OAuth flow for Twitch authentication
        /// </summary>
        private IEnumerator TwitchOAuthFlow()
        {
            loginInProgress = true;

            try
            {
                logger?.LogInfo("Starting Twitch OAuth flow...");
                
                // For .NET 3.5 compatibility, we'll require manual token input
                logger?.LogInfo("Please visit https://twitchapps.com/tmi/ to generate an OAuth token");
                logger?.LogInfo("Enter the token (including 'oauth:' prefix) in the GUI");
                
                // Wait for manual token input
                yield return new WaitUntil(() => !string.IsNullOrEmpty(tokenInput) && tokenInput.StartsWith("oauth:"));
                
                // Validate and store the token
                var newAuthData = new TwitchAuthData
                {
                    AccessToken = tokenInput.Trim(),
                    Username = usernameInput.Trim(),
                    ExpiresAt = DateTime.Now.AddDays(60), // Twitch tokens typically last ~60 days
                    RefreshToken = "" // Not available with simple token generation
                };

                // Test the connection
                if (TestConnection(newAuthData))
                {
                    authData = newAuthData;
                    SaveAuthData();
                    
                    // Update config
                    twitchUsername.Value = authData.Username;
                    twitchOAuthToken.Value = authData.AccessToken;
                    
                    logger?.LogInfo($"Successfully authenticated as {authData.Username}");
                    
                    // Auto-connect
                    ConnectToTwitch();
                }
                else
                {
                    logger?.LogError("Failed to authenticate with provided credentials");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"OAuth flow error: {ex.Message}");
            }
            finally
            {
                loginInProgress = false;
                tokenInput = "";
            }
        }

        private bool TestConnection(TwitchAuthData testAuth)
        {
            try
            {
                using (var testClient = new TcpClient())
                {
                    testClient.ReceiveTimeout = 5000;
                    testClient.SendTimeout = 5000;
                    
                    testClient.Connect(TwitchIrcServer, TwitchIrcPort);
                    var testStream = testClient.GetStream();
                    
                    // Send authentication
                    SendIrcMessage(testStream, $"PASS {testAuth.AccessToken}");
                    SendIrcMessage(testStream, $"NICK {testAuth.Username}");
                    
                    // Read response
                    byte[] buffer = new byte[4096];
                    int bytes = testStream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytes);
                    
                    return !response.Contains("Login authentication failed");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        private void LoadAuthData()
        {
            try
            {
                if (File.Exists(authDataPath))
                {
                    string json = File.ReadAllText(authDataPath);
                    authData = JsonConvert.DeserializeObject<TwitchAuthData>(json);
                    
                    if (authData?.IsValid == true)
                    {
                        logger?.LogInfo($"Loaded valid auth data for {authData.Username}");
                        
                        // Update GUI fields
                        usernameInput = authData.Username;
                        channelInput = authData.Username; // Default to own channel
                        
                        // Update config
                        twitchUsername.Value = authData.Username;
                        twitchChannel.Value = authData.Username;
                    }
                    else
                    {
                        logger?.LogWarning("Loaded auth data is expired or invalid");
                        authData = null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to load auth data: {ex.Message}");
                authData = null;
            }
        }

        private void SaveAuthData()
        {
            try
            {
                if (authData != null)
                {
                    string json = JsonConvert.SerializeObject(authData, Formatting.Indented);
                    File.WriteAllText(authDataPath, json);
                    logger?.LogInfo("Auth data saved successfully");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to save auth data: {ex.Message}");
            }
        }
        #endregion

        #region IRC Connection Management
        /// <summary>
        /// Connect to Twitch chat using IRC
        /// </summary>
        public void ConnectToTwitch()
        {
            if (isConnected || isConnecting)
            {
                logger?.LogWarning("Already connected or connecting to Twitch");
                return;
            }

            if (!enableTwitchIntegration.Value)
            {
                logger?.LogInfo("Twitch integration is disabled");
                return;
            }

            if (authData?.IsValid != true)
            {
                logger?.LogError("No valid authentication data available");
                return;
            }

            StartCoroutine(ConnectCoroutine());
        }

        private IEnumerator ConnectCoroutine()
        {
            isConnecting = true;
            lastConnectionAttempt = DateTime.Now;

            yield return StartCoroutine(ConnectIrcAsync());
        }

        private IEnumerator ConnectIrcAsync()
        {
            bool connected = false;
            Exception connectionError = null;

            // Perform connection on background thread
            var connectionThread = new Thread(() =>
            {
                try
                {
                    lock (connectionLock)
                    {
                        logger?.LogInfo($"Connecting to Twitch IRC as {authData.Username}...");

                        tcpClient = new TcpClient();
                        tcpClient.Connect(TwitchIrcServer, TwitchIrcPort);
                        stream = tcpClient.GetStream();

                        // Authenticate
                        SendIrcMessage(stream, $"PASS {authData.AccessToken}");
                        SendIrcMessage(stream, $"NICK {authData.Username}");
                        SendIrcMessage(stream, TwitchIrcCapabilities);

                        // Join channel
                        string channelToJoin = !string.IsNullOrEmpty(twitchChannel.Value) ? twitchChannel.Value : authData.Username;
                        if (!channelToJoin.StartsWith("#"))
                            channelToJoin = "#" + channelToJoin;
                        
                        SendIrcMessage(stream, $"JOIN {channelToJoin}");

                        isConnected = true;
                        isRunning = true;
                        connected = true;

                        // Start reading messages
                        this.connectionThread = new Thread(ReadIrcMessages) { IsBackground = true };
                        this.connectionThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    connectionError = ex;
                    isConnected = false;
                    isRunning = false;
                }
            });

            connectionThread.Start();

            // Wait for connection to complete
            yield return new WaitUntil(() => connected || connectionError != null);

            if (connected)
            {
                logger?.LogInfo($"Successfully connected to Twitch IRC");
                connectionRetryCount = 0;
                OnConnected?.Invoke();
            }
            else
            {
                logger?.LogError($"Failed to connect to Twitch IRC: {connectionError?.Message}");
                connectionRetryCount++;
            }

            isConnecting = false;
        }

        private void SendIrcMessage(NetworkStream networkStream, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\r\n");
                networkStream.Write(data, 0, data.Length);
                logger?.LogDebug($"IRC Sent: {message}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to send IRC message: {ex.Message}");
            }
        }

        private void ReadIrcMessages()
        {
            try
            {
                byte[] buffer = new byte[4096];
                
                while (isRunning && tcpClient?.Connected == true)
                {
                    try
                    {
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            break; // Connection closed
                        }

                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string[] lines = data.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string line in lines)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                // Queue message for main thread processing
                                lock (messageQueue)
                                {
                                    messageQueue.Add(line);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (isRunning)
                        {
                            logger?.LogError($"Error reading IRC messages: {ex.Message}");
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"IRC read thread error: {ex.Message}");
            }
            finally
            {
                isConnected = false;
                isRunning = false;
                OnDisconnected?.Invoke();
            }
        }

        private void ProcessMessageQueue()
        {
            List<string> messagesToProcess = new List<string>();
            
            lock (messageQueue)
            {
                if (messageQueue.Count > 0)
                {
                    messagesToProcess.AddRange(messageQueue);
                    messageQueue.Clear();
                }
            }

            foreach (string message in messagesToProcess)
            {
                ProcessIrcMessage(message);
            }
        }

        private void ProcessIrcMessage(string message)
        {
            try
            {
                logger?.LogDebug($"IRC Received: {message}");

                // Handle PING
                if (message.StartsWith("PING"))
                {
                    string pongResponse = message.Replace("PING", "PONG");
                    SendIrcMessage(stream, pongResponse);
                    return;
                }

                // Parse PRIVMSG (chat messages) with enhanced Channel Points support
                if (message.Contains("PRIVMSG"))
                {
                    ParseChatMessageEnhanced(message);
                }
                
                // Parse JOIN/PART messages
                if (message.Contains(" JOIN "))
                {
                    ParseJoinMessage(message);
                }
                else if (message.Contains(" PART "))
                {
                    ParsePartMessage(message);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing IRC message: {ex.Message}");
            }
        }

        private void ParseChatMessage(string message)
        {
            try
            {
                // Format: @badges=...;color=...;display-name=Username;... :username!username@username.tmi.twitch.tv PRIVMSG #channel :message text
                
                string username = "";
                string displayName = "";
                string messageText = "";
                bool isModerator = false;
                bool isSubscriber = false;
                bool isBroadcaster = false;

                // Extract tags
                if (message.StartsWith("@"))
                {
                    int tagEnd = message.IndexOf(" :");
                    if (tagEnd > 0)
                    {
                        string tags = message.Substring(1, tagEnd - 1);
                        var tagPairs = tags.Split(';');
                        
                        foreach (string tag in tagPairs)
                        {
                            var parts = tag.Split('=');
                            if (parts.Length == 2)
                            {
                                switch (parts[0])
                                {
                                    case "display-name":
                                        displayName = parts[1];
                                        break;
                                    case "badges":
                                        isModerator = parts[1].Contains("moderator") || parts[1].Contains("broadcaster");
                                        isBroadcaster = parts[1].Contains("broadcaster");
                                        isSubscriber = parts[1].Contains("subscriber");
                                        break;
                                }
                            }
                        }
                    }
                }

                // Extract username and message
                int userStart = message.IndexOf(" :");
                if (userStart > 0)
                {
                    int userEnd = message.IndexOf("!", userStart);
                    if (userEnd > 0)
                    {
                        username = message.Substring(userStart + 2, userEnd - userStart - 2);
                    }
                }

                int messageStart = message.LastIndexOf(" :");
                if (messageStart > 0)
                {
                    messageText = message.Substring(messageStart + 2);
                }

                if (string.IsNullOrEmpty(displayName))
                    displayName = username;

                // Update user tracking
                if (isModerator && !moderators.Contains(username))
                    moderators.Add(username);
                if (isSubscriber && !subscribers.Contains(username))
                    subscribers.Add(username);

                // Log chat message
                if (enableChatLogging.Value)
                {
                    logger?.LogInfo($"[{displayName}]: {messageText}");
                }

                // Add to recent messages for GUI
                AddRecentMessage($"{displayName}: {messageText}");

                // Trigger event
                OnChatMessage?.Invoke(username, messageText);

                // Process commands
                if (messageText.StartsWith("!"))
                {
                    ProcessChatCommand(username, displayName, messageText, isModerator, isSubscriber, isBroadcaster);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error parsing chat message: {ex.Message}");
            }
        }

        private void ParseJoinMessage(string message)
        {
            try
            {
                // Format: :username!username@username.tmi.twitch.tv JOIN #channel
                int userStart = message.IndexOf(":");
                int userEnd = message.IndexOf("!");
                
                if (userStart >= 0 && userEnd > userStart)
                {
                    string username = message.Substring(userStart + 1, userEnd - userStart - 1);
                    logger?.LogDebug($"{username} joined the chat");
                    OnUserJoined?.Invoke(username);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error parsing join message: {ex.Message}");
            }
        }

        private void ParsePartMessage(string message)
        {
            try
            {
                // Format: :username!username@username.tmi.twitch.tv PART #channel
                int userStart = message.IndexOf(":");
                int userEnd = message.IndexOf("!");
                
                if (userStart >= 0 && userEnd > userStart)
                {
                    string username = message.Substring(userStart + 1, userEnd - userStart - 1);
                    logger?.LogDebug($"{username} left the chat");
                    OnUserLeft?.Invoke(username);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error parsing part message: {ex.Message}");
            }
        }

        public void DisconnectFromTwitch()
        {
            try
            {
                isRunning = false;
                isConnected = false;

                if (connectionThread != null && connectionThread.IsAlive)
                {
                    connectionThread.Join(1000); // Wait up to 1 second
                }

                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }

                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }

                logger?.LogInfo("Disconnected from Twitch IRC");
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error disconnecting: {ex.Message}");
            }
        }

        private void RetryConnection()
        {
            if (connectionRetryCount < MaxRetryAttempts)
            {
                logger?.LogInfo($"Retrying IRC connection... Attempt {connectionRetryCount + 1}/{MaxRetryAttempts}");
                ConnectToTwitch();
            }
            else
            {
                logger?.LogError("Max connection retry attempts reached");
            }
        }
        #endregion

        #region Command Processing
        private void ProcessChatCommand(string username, string displayName, string command, bool isModerator, bool isSubscriber, bool isBroadcaster)
        {
            try
            {
                var commandParts = command.ToLower().Split(' ');
                var baseCommand = commandParts[0];

                // Check if command is allowed
                var allowedCmds = allowedCommands.Value.Split(',');
                bool isAllowed = false;
                foreach (var allowed in allowedCmds)
                {
                    if (baseCommand.Equals(allowed.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        isAllowed = true;
                        break;
                    }
                }

                if (!isAllowed)
                {
                    return; // Ignore unknown commands
                }

                // Check permissions
                if (!HasPermissionForCommand(username, isModerator, isSubscriber, isBroadcaster))
                {
                    SendChatMessage($"@{displayName} You don't have permission to use commands.");
                    return;
                }

                // Check cooldown
                if (IsUserOnCooldown(username))
                {
                    var remainingTime = GetRemainingCooldown(username);
                    SendChatMessage($"@{displayName} Command on cooldown. Wait {remainingTime:F0} more seconds.");
                    return;
                }

                // Process specific commands
                switch (baseCommand)
                {
                    case "!ally":
                        ProcessAllySpawnCommand(username, displayName, commandParts);
                        break;
                    case "!enemy":
                        ProcessEnemySpawnCommand(username, displayName, commandParts);
                        break;
                    case "!clear":
                        ProcessClearCommand(username, displayName, isModerator, isBroadcaster);
                        break;
                    case "!help":
                        ProcessHelpCommand(username, displayName);
                        break;
                    case "!stats":
                        ProcessStatsCommand(username, displayName);
                        break;
                    default:
                        // Unknown command - ignore silently
                        break;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing command '{command}' from {username}: {ex.Message}");
            }
        }

        private bool HasPermissionForCommand(string username, bool isModerator, bool isSubscriber, bool isBroadcaster)
        {
            // Broadcaster always has permission
            if (isBroadcaster)
                return true;

            // Check moderator requirement
            if (requireModeratorForCommands.Value && !isModerator && !isBroadcaster)
                return false;

            // Check subscriber requirement
            if (requireSubscriberForCommands.Value && !isSubscriber && !isModerator && !isBroadcaster)
                return false;

            // Check if viewers are allowed
            if (!allowViewersToSpawn.Value && !isModerator && !isBroadcaster && !isSubscriber)
                return false;

            return true;
        }

        private void ProcessAllySpawnCommand(string username, string displayName, string[] commandParts)
        {
            try
            {
                // Check sosig limits
                if (GetUserSosigCount(username) >= maxSosigsPerUser.Value)
                {
                    SendChatMessage($"@{displayName} You already have the maximum number of sosigs active.");
                    return;
                }

                // Get armor preset if specified
                string armorPreset = commandParts.Length > 1 ? commandParts[1] : null;

                // Queue spawn request
                bool queued = chatSpawner?.QueueTwitchSpawnRequest(username, displayName, true, armorPreset) ?? false;
                
                if (queued)
                {
                    SetUserCooldown(username);
                    IncrementUserSosigCount(username);
                    SendChatMessage($"@{displayName} Ally sosig queued for spawn!");
                    OnSosigSpawnRequest?.Invoke(username, true);
                }
                else
                {
                    SendChatMessage($"@{displayName} Unable to spawn ally sosig (server at capacity).");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing ally spawn for {username}: {ex.Message}");
            }
        }

        private void ProcessEnemySpawnCommand(string username, string displayName, string[] commandParts)
        {
            try
            {
                // Check sosig limits
                if (GetUserSosigCount(username) >= maxSosigsPerUser.Value)
                {
                    SendChatMessage($"@{displayName} You already have the maximum number of sosigs active.");
                    return;
                }

                // Get armor preset if specified
                string armorPreset = commandParts.Length > 1 ? commandParts[1] : null;

                // Queue spawn request
                bool queued = chatSpawner?.QueueTwitchSpawnRequest(username, displayName, false, armorPreset) ?? false;
                
                if (queued)
                {
                    SetUserCooldown(username);
                    IncrementUserSosigCount(username);
                    SendChatMessage($"@{displayName} Enemy sosig queued for spawn!");
                    OnSosigSpawnRequest?.Invoke(username, false);
                }
                else
                {
                    SendChatMessage($"@{displayName} Unable to spawn enemy sosig (server at capacity).");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing enemy spawn for {username}: {ex.Message}");
            }
        }

        private void ProcessClearCommand(string username, string displayName, bool isModerator, bool isBroadcaster)
        {
            try
            {
                // Only moderators and broadcaster can clear
                if (!isModerator && !isBroadcaster)
                {
                    SendChatMessage($"@{displayName} Only moderators can clear sosigs.");
                    return;
                }

                chatSpawner?.ClearSosigs(true, true);
                
                // Reset all user sosig counts
                userSpawnCounts.Clear();
                
                SendChatMessage($"@{displayName} All sosigs cleared!");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing clear command for {username}: {ex.Message}");
            }
        }

        private void ProcessHelpCommand(string username, string displayName)
        {
            try
            {
                var helpText = $"@{displayName} Commands: {sosigSpawnCommand.Value} (spawn ally), " +
                              $"{enemySosigSpawnCommand.Value} (spawn enemy), !stats (show stats)";
                
                if (enableCustomArmorCommands.Value)
                {
                    helpText += ", !ally <armor> or !enemy <armor> for custom armor";
                }

                SendChatMessage(helpText);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing help command for {username}: {ex.Message}");
            }
        }

        private void ProcessStatsCommand(string username, string displayName)
        {
            try
            {
                var stats = chatSpawner?.GetStats();
                if (stats != null)
                {
                    var message = $"@{displayName} Sosigs: {stats.ActiveAllies} allies, {stats.ActiveEnemies} enemies, " +
                                 $"{stats.QueueLength} queued. Your sosigs: {GetUserSosigCount(username)}/{maxSosigsPerUser.Value}";
                    SendChatMessage(message);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing stats command for {username}: {ex.Message}");
            }
        }
        #endregion

        #region Enhanced Channel Points Support
        /// <summary>
        /// Channel Points redemption data extracted from IRC
        /// </summary>
        public class ChannelPointsRedemption
        {
            public string Username { get; set; }
            public string DisplayName { get; set; }
            public string Message { get; set; }
            public string RewardId { get; set; }
            public string RewardTitle { get; set; }
            public int? RewardCost { get; set; }
            public bool IsChannelPointRedemption { get; set; }
            public DateTime RedemptionTime { get; set; }
            public Dictionary<string, string> RawTags { get; set; } = new Dictionary<string, string>();
        }

        /// <summary>
        /// Enhanced event for Channel Points
        /// </summary>
        public static event Action<ChannelPointsRedemption> OnChannelPointsRedemption;

        /// <summary>
        /// Configuration for Channel Points
        /// </summary>
        private ConfigEntry<bool> enableChannelPointsPriority;
        private ConfigEntry<bool> bypassCooldownForChannelPoints;
        private ConfigEntry<float> channelPointsCooldownMultiplier;
        private ConfigEntry<string> channelPointsAllyRewardIds;
        private ConfigEntry<string> channelPointsEnemyRewardIds;
        private ConfigEntry<string> channelPointsClearRewardIds;

        /// <summary>
        /// Initialize Channel Points configuration
        /// </summary>
        private void InitializeChannelPointsConfig()
        {
            var config = plugin.Config;

            enableChannelPointsPriority = config.Bind("Channel Points", "EnableChannelPointsPriority", true,
                "Give Channel Points redemptions priority in spawn queue");
            bypassCooldownForChannelPoints = config.Bind("Channel Points", "BypassCooldownForChannelPoints", true,
                "Bypass user cooldowns for Channel Points redemptions");
            channelPointsCooldownMultiplier = config.Bind("Channel Points", "ChannelPointsCooldownMultiplier", 0.5f,
                "Cooldown multiplier for Channel Points (0.5 = half cooldown)");
            
            // Reward ID mappings for automatic detection
            channelPointsAllyRewardIds = config.Bind("Channel Points", "AllyRewardIds", "",
                "Comma-separated list of reward IDs that should spawn allies");
            channelPointsEnemyRewardIds = config.Bind("Channel Points", "EnemyRewardIds", "",
                "Comma-separated list of reward IDs that should spawn enemies");
            channelPointsClearRewardIds = config.Bind("Channel Points", "ClearRewardIds", "",
                "Comma-separated list of reward IDs that should clear sosigs");
        }

        /// <summary>
        /// Enhanced chat message parsing with Channel Points support
        /// </summary>
        private void ParseChatMessageEnhanced(string message)
        {
            try
            {
                string username = "";
                string displayName = "";
                string messageText = "";
                bool isModerator = false;
                bool isSubscriber = false;
                bool isBroadcaster = false;
                
                // Channel Points specific data
                bool isChannelPointRedemption = false;
                string rewardId = "";
                string rewardTitle = "";
                int? rewardCost = null;
                var rawTags = new Dictionary<string, string>();

                // Extract tags
                if (message.StartsWith("@"))
                {
                    int tagEnd = message.IndexOf(" :");
                    if (tagEnd > 0)
                    {
                        string tags = message.Substring(1, tagEnd - 1);
                        var tagPairs = tags.Split(';');
                        
                        foreach (string tag in tagPairs)
                        {
                            var parts = tag.Split(new[] { '=' }, 2);
                            if (parts.Length == 2)
                            {
                                rawTags[parts[0]] = parts[1];
                                
                                switch (parts[0])
                                {
                                    case "display-name":
                                        displayName = parts[1];
                                        break;
                                    case "badges":
                                        isModerator = parts[1].Contains("moderator") || parts[1].Contains("broadcaster");
                                        isBroadcaster = parts[1].Contains("broadcaster");
                                        isSubscriber = parts[1].Contains("subscriber");
                                        break;
                                    case "msg-id":
                                        // Channel Points redemptions have specific msg-id values
                                        isChannelPointRedemption = parts[1].Contains("highlighted-message") ||
                                                                  parts[1].Contains("channel-points-redemption") ||
                                                                  parts[1].Contains("reward-redemption");
                                        break;
                                    case "custom-reward-id":
                                        // Direct Channel Points reward ID
                                        rewardId = parts[1];
                                        isChannelPointRedemption = true;
                                        break;
                                    case "custom-reward-name":
                                        rewardTitle = parts[1];
                                        break;
                                    case "custom-reward-cost":
                                        if (int.TryParse(parts[1], out int cost))
                                        {
                                            rewardCost = cost;
                                        }
                                        break;
                                    // Additional Channel Points tags
                                    case "msg-param-reward-name":
                                        rewardTitle = parts[1];
                                        break;
                                    case "msg-param-reward-cost":
                                        if (int.TryParse(parts[1], out int paramCost))
                                        {
                                            rewardCost = paramCost;
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }

                // Extract username and message (existing logic)
                int userStart = message.IndexOf(" :");
                if (userStart > 0)
                {
                    int userEnd = message.IndexOf("!", userStart);
                    if (userEnd > 0)
                    {
                        username = message.Substring(userStart + 2, userEnd - userStart - 2);
                    }
                }

                int messageStart = message.LastIndexOf(" :");
                if (messageStart > 0)
                {
                    messageText = message.Substring(messageStart + 2);
                }

                if (string.IsNullOrEmpty(displayName))
                    displayName = username;

                // Update user tracking
                if (isModerator && !moderators.Contains(username))
                    moderators.Add(username);
                if (isSubscriber && !subscribers.Contains(username))
                    subscribers.Add(username);

                // Log chat message
                if (enableChatLogging.Value)
                {
                    string logPrefix = isChannelPointRedemption ? "[CHANNEL POINTS]" : "";
                    logger?.LogInfo($"{logPrefix}[{displayName}]: {messageText}");
                }

                // Add to recent messages for GUI
                string displayMessage = isChannelPointRedemption ? 
                    $"?? {displayName}: {messageText}" : 
                    $"{displayName}: {messageText}";
                AddRecentMessage(displayMessage);

                // Handle Channel Points redemption
                if (isChannelPointRedemption)
                {
                    var redemption = new ChannelPointsRedemption
                    {
                        Username = username,
                        DisplayName = displayName,
                        Message = messageText,
                        RewardId = rewardId,
                        RewardTitle = rewardTitle,
                        RewardCost = rewardCost,
                        IsChannelPointRedemption = true,
                        RedemptionTime = DateTime.Now,
                        RawTags = rawTags
                    };

                    // Trigger Channel Points event
                    OnChannelPointsRedemption?.Invoke(redemption);

                    // Process Channel Points command with priority
                    if (messageText.StartsWith("!"))
                    {
                        ProcessChannelPointsCommand(redemption, isModerator, isSubscriber, isBroadcaster);
                    }
                    else
                    {
                        // Auto-detect command based on reward ID
                        ProcessChannelPointsRewardId(redemption, isModerator, isSubscriber, isBroadcaster);
                    }
                }
                else
                {
                    // Trigger regular chat event
                    OnChatMessage?.Invoke(username, messageText);

                    // Process regular commands
                    if (messageText.StartsWith("!"))
                    {
                        ProcessChatCommand(username, displayName, messageText, isModerator, isSubscriber, isBroadcaster);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error parsing enhanced chat message: {ex.Message}");
            }
        }

        /// <summary>
        /// Process Channel Points commands with enhanced handling
        /// </summary>
        private void ProcessChannelPointsCommand(ChannelPointsRedemption redemption, bool isModerator, bool isSubscriber, bool isBroadcaster)
        {
            try
            {
                var commandParts = redemption.Message.ToLower().Split(' ');
                var baseCommand = commandParts[0];

                // Check if command is allowed
                var allowedCmds = allowedCommands.Value.Split(',');
                bool isAllowed = false;
                foreach (var allowed in allowedCmds)
                {
                    if (baseCommand.Equals(allowed.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        isAllowed = true;
                        break;
                    }
                }

                if (!isAllowed)
                {
                    return; // Ignore unknown commands
                }

                // Check permissions (Channel Points users might have special privileges)
                if (!HasPermissionForCommand(redemption.Username, isModerator, isSubscriber, isBroadcaster))
                {
                    SendChatMessage($"@{redemption.DisplayName} You don't have permission to use commands.");
                    return;
                }

                // Channel Points users get reduced cooldown or bypass
                bool skipCooldown = bypassCooldownForChannelPoints.Value;
                if (!skipCooldown && IsUserOnCooldown(redemption.Username))
                {
                    var remainingTime = GetRemainingCooldown(redemption.Username);
                    SendChatMessage($"@{redemption.DisplayName} Command on cooldown. Wait {remainingTime:F0} more seconds.");
                    return;
                }

                // Determine priority
                var priority = enableChannelPointsPriority.Value ? 
                    EnhancedChatSpawner.SpawnPriority.High : 
                    EnhancedChatSpawner.SpawnPriority.Normal;

                // Process specific commands with Channel Points enhancements
                switch (baseCommand)
                {
                    case "!ally":
                        ProcessChannelPointsAllySpawn(redemption, commandParts, priority);
                        break;
                    case "!enemy":
                        ProcessChannelPointsEnemySpawn(redemption, commandParts, priority);
                        break;
                    case "!clear":
                        ProcessClearCommand(redemption.Username, redemption.DisplayName, isModerator, isBroadcaster);
                        break;
                    case "!help":
                        ProcessHelpCommand(redemption.Username, redemption.DisplayName);
                        break;
                    case "!stats":
                        ProcessStatsCommand(redemption.Username, redemption.DisplayName);
                        break;
                    default:
                        // Unknown command - ignore silently
                        break;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing Channel Points command from {redemption.Username}: {ex.Message}");
            }
        }

        /// <summary>
        /// Process Channel Points redemption based on reward ID
        /// </summary>
        private void ProcessChannelPointsRewardId(ChannelPointsRedemption redemption, bool isModerator, bool isSubscriber, bool isBroadcaster)
        {
            try
            {
                if (string.IsNullOrEmpty(redemption.RewardId))
                    return;

                // Check if this reward ID is mapped to specific actions
                var allyIds = channelPointsAllyRewardIds.Value.Split(',').Select(s => s.Trim()).ToArray();
                var enemyIds = channelPointsEnemyRewardIds.Value.Split(',').Select(s => s.Trim()).ToArray();
                var clearIds = channelPointsClearRewardIds.Value.Split(',').Select(s => s.Trim()).ToArray();

                if (allyIds.Contains(redemption.RewardId))
                {
                    // Spawn ally
                    ProcessChannelPointsAllySpawn(redemption, new[] { "!ally" }, EnhancedChatSpawner.SpawnPriority.High);
                }
                else if (enemyIds.Contains(redemption.RewardId))
                {
                    // Spawn enemy
                    ProcessChannelPointsEnemySpawn(redemption, new[] { "!enemy" }, EnhancedChatSpawner.SpawnPriority.High);
                }
                else if (clearIds.Contains(redemption.RewardId))
                {
                    // Clear sosigs (if user has permission)
                    ProcessClearCommand(redemption.Username, redemption.DisplayName, isModerator, isBroadcaster);
                }
                else
                {
                    // Log unrecognized reward ID
                    logger?.LogInfo($"Unrecognized Channel Points reward ID: {redemption.RewardId} from {redemption.Username}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing Channel Points reward ID: {ex.Message}");
            }
        }

        /// <summary>
        /// Process Channel Points ally spawn with enhanced feedback
        /// </summary>
        private void ProcessChannelPointsAllySpawn(ChannelPointsRedemption redemption, string[] commandParts, EnhancedChatSpawner.SpawnPriority priority)
        {
            try
            {
                // Check sosig limits
                if (GetUserSosigCount(redemption.Username) >= maxSosigsPerUser.Value)
                {
                    SendChatMessage($"@{redemption.DisplayName} You already have the maximum number of sosigs active. (Channel Points will be refunded)");
                    return;
                }

                // Get armor preset if specified
                string armorPreset = commandParts.Length > 1 ? commandParts[1] : null;

                // Queue spawn request with high priority
                bool queued = chatSpawner?.QueueTwitchSpawnRequest(redemption.Username, redemption.DisplayName, true, armorPreset, priority) ?? false;
                
                if (queued)
                {
                    // Set reduced cooldown for Channel Points
                    float cooldownTime = spawnCooldown.Value * channelPointsCooldownMultiplier.Value;
                    userCooldowns[redemption.Username] = DateTime.Now.AddSeconds(cooldownTime);
                    
                    IncrementUserSosigCount(redemption.Username);
                    
                    string costText = redemption.RewardCost.HasValue ? $" (Cost: {redemption.RewardCost} points)" : "";
                    SendChatMessage($"?? @{redemption.DisplayName} Channel Points ally sosig queued for spawn!{costText}");
                    OnSosigSpawnRequest?.Invoke(redemption.Username, true);
                }
                else
                {
                    SendChatMessage($"@{redemption.DisplayName} Unable to spawn ally sosig (server at capacity). Channel Points will be refunded.");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing Channel Points ally spawn for {redemption.Username}: {ex.Message}");
            }
        }

        /// <summary>
        /// Process Channel Points enemy spawn with enhanced feedback
        /// </summary>
        private void ProcessChannelPointsEnemySpawn(ChannelPointsRedemption redemption, string[] commandParts, EnhancedChatSpawner.SpawnPriority priority)
        {
            try
            {
                // Check sosig limits
                if (GetUserSosigCount(redemption.Username) >= maxSosigsPerUser.Value)
                {
                    SendChatMessage($"@{redemption.DisplayName} You already have the maximum number of sosigs active. (Channel Points will be refunded)");
                    return;
                }

                // Get armor preset if specified
                string armorPreset = commandParts.Length > 1 ? commandParts[1] : null;

                // Queue spawn request with high priority
                bool queued = chatSpawner?.QueueTwitchSpawnRequest(redemption.Username, redemption.DisplayName, false, armorPreset, priority) ?? false;
                
                if (queued)
                {
                    // Set reduced cooldown for Channel Points
                    float cooldownTime = spawnCooldown.Value * channelPointsCooldownMultiplier.Value;
                    userCooldowns[redemption.Username] = DateTime.Now.AddSeconds(cooldownTime);
                    
                    IncrementUserSosigCount(redemption.Username);
                    
                    string costText = redemption.RewardCost.HasValue ? $" (Cost: {redemption.RewardCost} points)" : "";
                    SendChatMessage($"?? @{redemption.DisplayName} Channel Points enemy sosig queued for spawn!{costText}");
                    OnSosigSpawnRequest?.Invoke(redemption.Username, false);
                }
                else
                {
                    SendChatMessage($"@{redemption.DisplayName} Unable to spawn enemy sosig (server at capacity). Channel Points will be refunded.");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing Channel Points enemy spawn for {redemption.Username}: {ex.Message}");
            }
        }
        #endregion

        #region Helper Methods for TwitchChatManager
        /// <summary>
        /// Check if TwitchChatManager is connected
        /// </summary>
        public bool IsConnected
        {
            get
            {
                try
                {
                    return isConnected;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Send a message to Twitch chat
        /// </summary>
        public void SendChatMessage(string message)
        {
            try
            {
                if (isConnected && stream != null)
                {
                    SendIrcMessage(stream, $"PRIVMSG #{twitchChannel.Value} :{message}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to send chat message: {ex.Message}");
            }
        }

        /// <summary>
        /// Decrement user sosig count
        /// </summary>
        public void DecrementUserSosigCount(string username)
        {
            if (userSpawnCounts.ContainsKey(username))
            {
                userSpawnCounts[username] = Math.Max(0, userSpawnCounts[username] - 1);
                if (userSpawnCounts[username] == 0)
                {
                    userSpawnCounts.Remove(username);
                }
            }
        }

        /// <summary>
        /// Reset all user sosig counts
        /// </summary>
        public void ResetAllUserCounts()
        {
            userSpawnCounts.Clear();
            userCooldowns.Clear();
        }

        /// <summary>
        /// Toggle Twitch GUI visibility
        /// </summary>
        private void ToggleTwitchGUI()
        {
            showTwitchGUI = !showTwitchGUI;
            logger?.LogInfo($"Twitch GUI {(showTwitchGUI ? "shown" : "hidden")}");
        }

        /// <summary>
        /// Add message to recent messages list
        /// </summary>
        private void AddRecentMessage(string message)
        {
            recentMessages.Add(message);
            if (recentMessages.Count > MaxRecentMessages)
            {
                recentMessages.RemoveAt(0);
            }
        }

        /// <summary>
        /// Clean up expired cooldowns
        /// </summary>
        private void CleanupExpiredCooldowns()
        {
            var now = DateTime.Now;
            var expiredKeys = userCooldowns.Where(kvp => kvp.Value < now).Select(kvp => kvp.Key).ToList();
            
            foreach (var key in expiredKeys)
            {
                userCooldowns.Remove(key);
            }
        }

        /// <summary>
        /// Check if user is on cooldown
        /// </summary>
        private bool IsUserOnCooldown(string username)
        {
            if (userCooldowns.TryGetValue(username, out DateTime cooldownEnd))
            {
                return DateTime.Now < cooldownEnd;
            }
            return false;
        }

        /// <summary>
        /// Get remaining cooldown time for user
        /// </summary>
        private float GetRemainingCooldown(string username)
        {
            if (userCooldowns.TryGetValue(username, out DateTime cooldownEnd))
            {
                return (float)(cooldownEnd - DateTime.Now).TotalSeconds;
            }
            return 0f;
        }

        /// <summary>
        /// Set user cooldown
        /// </summary>
        private void SetUserCooldown(string username)
        {
            userCooldowns[username] = DateTime.Now.AddSeconds(commandCooldownSeconds.Value);
        }

        /// <summary>
        /// Get user sosig count
        /// </summary>
        private int GetUserSosigCount(string username)
        {
            if (userSpawnCounts.TryGetValue(username, out int count))
            {
                return count;
            }
            return 0;
        }

        /// <summary>
        /// Increment user sosig count
        /// </summary>
        private void IncrementUserSosigCount(string username)
        {
            if (userSpawnCounts.ContainsKey(username))
            {
                userSpawnCounts[username]++;
            }
            else
            {
                userSpawnCounts[username] = 1;
            }
        }
        #endregion
    }
}
