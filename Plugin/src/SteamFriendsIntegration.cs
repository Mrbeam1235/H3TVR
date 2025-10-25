using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BepInEx.Logging;
using Steamworks;

namespace H3TVR
{
    /// <summary>
    /// Steam Friends Integration for Advanced Sosig Spawner
    /// Fetches Steam friend names and provides them to the sosig spawner
    /// </summary>
    public class SteamFriendsIntegration : MonoBehaviour
    {
        #region Static Instance
        public static SteamFriendsIntegration Instance { get; private set; }
        #endregion

        #region Core Fields
        private ManualLogSource logger;
        private AdvancedChatSosigSpawner sosigSpawner;
        private H3TVRImproved plugin;
        
        private List<string> friendNames = new List<string>();
        private List<CSteamID> friendSteamIDs = new List<CSteamID>();
        private bool isInitialized = false;
        private bool steamAvailable = false;
        
        private float lastRefreshTime = 0f;
        private const float REFRESH_INTERVAL = 300f; // Refresh friends list every 5 minutes
        #endregion

        #region Initialization
        public void Initialize(H3TVRImproved pluginInstance, AdvancedChatSosigSpawner spawner, ManualLogSource logSource)
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            plugin = pluginInstance;
            sosigSpawner = spawner;
            logger = logSource;

            try
            {
                // Check if Steam is available
                steamAvailable = SteamManager.Initialized;
                
                if (!steamAvailable)
                {
                    logger.LogWarning("[SteamFriends] Steam is not initialized - friends integration disabled");
                    return;
                }

                logger.LogInfo("[SteamFriends] Steam detected - loading friends list...");
                
                // Load friends list
                RefreshFriendsList();
                
                isInitialized = true;
                logger.LogInfo($"[SteamFriends] Integration initialized successfully with {friendNames.Count} friends");
            }
            catch (Exception ex)
            {
                logger.LogError($"[SteamFriends] Initialization failed: {ex.Message}");
                steamAvailable = false;
            }
        }
        #endregion

        #region Friends List Management
        /// <summary>
        /// Refresh the Steam friends list
        /// </summary>
        public void RefreshFriendsList()
        {
            if (!steamAvailable)
            {
                logger.LogWarning("[SteamFriends] Cannot refresh - Steam not available");
                return;
            }

            try
            {
                friendNames.Clear();
                friendSteamIDs.Clear();

                int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
                
                logger.LogInfo($"[SteamFriends] Found {friendCount} Steam friends");

                for (int i = 0; i < friendCount; i++)
                {
                    CSteamID friendID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                    string friendName = SteamFriends.GetFriendPersonaName(friendID);
                    
                    if (!string.IsNullOrEmpty(friendName))
                    {
                        friendNames.Add(friendName);
                        friendSteamIDs.Add(friendID);
                        logger.LogDebug($"[SteamFriends] Added friend: {friendName}");
                    }
                }

                lastRefreshTime = Time.time;
                logger.LogInfo($"[SteamFriends] Loaded {friendNames.Count} friend names");
            }
            catch (Exception ex)
            {
                logger.LogError($"[SteamFriends] Failed to refresh friends list: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a random friend name from the list
        /// </summary>
        public string GetRandomFriendName()
        {
            if (!isInitialized || friendNames.Count == 0)
            {
                logger.LogWarning("[SteamFriends] No friends available - using fallback");
                return "Steam Friend";
            }

            // Auto-refresh if it's been a while
            if (Time.time - lastRefreshTime > REFRESH_INTERVAL)
            {
                logger.LogInfo("[SteamFriends] Auto-refreshing friends list");
                RefreshFriendsList();
            }

            string friendName = friendNames[UnityEngine.Random.Range(0, friendNames.Count)];
            logger.LogDebug($"[SteamFriends] Selected random friend: {friendName}");
            return friendName;
        }

        /// <summary>
        /// Get a specific friend name by index
        /// </summary>
        public string GetFriendName(int index)
        {
            if (!isInitialized || friendNames.Count == 0)
            {
                return "Steam Friend";
            }

            if (index < 0 || index >= friendNames.Count)
            {
                logger.LogWarning($"[SteamFriends] Invalid index {index} - using random");
                return GetRandomFriendName();
            }

            return friendNames[index];
        }

        /// <summary>
        /// Get all friend names
        /// </summary>
        public List<string> GetAllFriendNames()
        {
            return new List<string>(friendNames);
        }

        /// <summary>
        /// Check if a friend is online
        /// </summary>
        public bool IsFriendOnline(int index)
        {
            if (!steamAvailable || index < 0 || index >= friendSteamIDs.Count)
                return false;

            try
            {
                var state = SteamFriends.GetFriendPersonaState(friendSteamIDs[index]);
                return state != EPersonaState.k_EPersonaStateOffline;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get count of friends
        /// </summary>
        public int GetFriendCount()
        {
            return friendNames.Count;
        }

        /// <summary>
        /// Check if Steam friends integration is available
        /// </summary>
        public bool IsAvailable()
        {
            return isInitialized && steamAvailable && friendNames.Count > 0;
        }
        #endregion

        #region Integration with Sosig Spawner
        /// <summary>
        /// Spawn a sosig with a random Steam friend's name
        /// </summary>
        public void SpawnSosigWithFriendName(bool isAlly)
        {
            if (!IsAvailable())
            {
                logger.LogWarning("[SteamFriends] Cannot spawn - integration not available");
                return;
            }

            string friendName = GetRandomFriendName();
            
            if (isAlly)
            {
                sosigSpawner.SpawningSequence(friendName);
                logger.LogInfo($"[SteamFriends] Spawned ally sosig with friend name: {friendName}");
            }
            else
            {
                sosigSpawner.SpawningSequenceEnemy(1, friendName);
                logger.LogInfo($"[SteamFriends] Spawned enemy sosig with friend name: {friendName}");
            }
        }

        /// <summary>
        /// Spawn multiple sosigs with friend names
        /// </summary>
        public void SpawnMultipleSosigsWithFriendNames(int count, bool isAlly)
        {
            if (!IsAvailable())
            {
                logger.LogWarning("[SteamFriends] Cannot spawn - integration not available");
                return;
            }

            count = Mathf.Min(count, friendNames.Count); // Don't spawn more than we have friends
            
            for (int i = 0; i < count; i++)
            {
                string friendName = friendNames[i % friendNames.Count]; // Cycle through friends
                
                if (isAlly)
                {
                    sosigSpawner.SpawningSequence(friendName);
                }
                else
                {
                    sosigSpawner.SpawningSequenceEnemy(1, friendName);
                }
                
                logger.LogInfo($"[SteamFriends] Spawned sosig {i+1}/{count} with friend name: {friendName}");
            }
        }

        /// <summary>
        /// Spawn all friends as sosigs (useful for fun scenarios)
        /// </summary>
        public void SpawnAllFriendsAsSosigs(bool isAlly)
        {
            if (!IsAvailable())
            {
                logger.LogWarning("[SteamFriends] Cannot spawn - integration not available");
                return;
            }

            int spawned = 0;
            foreach (string friendName in friendNames)
            {
                if (isAlly)
                {
                    sosigSpawner.SpawningSequence(friendName);
                }
                else
                {
                    sosigSpawner.SpawningSequenceEnemy(1, friendName);
                }
                spawned++;
            }
            
            logger.LogInfo($"[SteamFriends] Spawned all {spawned} friends as {(isAlly ? "allies" : "enemies")}");
        }
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            // Auto-refresh friends list periodically
            if (isInitialized && steamAvailable)
            {
                if (Time.time - lastRefreshTime > REFRESH_INTERVAL)
                {
                    logger.LogDebug("[SteamFriends] Auto-refreshing friends list");
                    RefreshFriendsList();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            friendNames?.Clear();
            friendSteamIDs?.Clear();
            
            logger?.LogInfo("[SteamFriends] Integration destroyed");
        }
        #endregion

        #region Statistics and Info
        /// <summary>
        /// Get statistics about Steam friends integration
        /// </summary>
        public string GetStatsInfo()
        {
            if (!isInitialized)
                return "[SteamFriends] Not initialized";

            int onlineCount = 0;
            for (int i = 0; i < friendSteamIDs.Count; i++)
            {
                if (IsFriendOnline(i))
                    onlineCount++;
            }

            return $"[SteamFriends] Stats:\n" +
                   $"  Total Friends: {friendNames.Count}\n" +
                   $"  Online Friends: {onlineCount}\n" +
                   $"  Last Refresh: {(Time.time - lastRefreshTime):F1}s ago\n" +
                   $"  Steam Available: {steamAvailable}\n" +
                   $"  Integration Ready: {IsAvailable()}";
        }

        /// <summary>
        /// Log all friends for debugging
        /// </summary>
        public void LogAllFriends()
        {
            if (!isInitialized)
            {
                logger.LogWarning("[SteamFriends] Not initialized");
                return;
            }

            logger.LogInfo($"[SteamFriends] Listing all {friendNames.Count} friends:");
            for (int i = 0; i < friendNames.Count; i++)
            {
                string status = IsFriendOnline(i) ? "Online" : "Offline";
                logger.LogInfo($"  [{i}] {friendNames[i]} - {status}");
            }
        }
        #endregion
    }
}
