using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using FistVR;
using UnityEngine;

namespace H3TVR
{
    /// <summary>
    /// Configuration management for sosig spawning
    /// </summary>
    public class SosigSpawnConfig
    {
        #region Configuration Entries
   public ConfigEntry<int> maxAllySosigs;
        public ConfigEntry<int> maxEnemySosigs;
        public ConfigEntry<float> spawnCooldown;
   public ConfigEntry<bool> enableNameplates;
        public ConfigEntry<float> sosigLifetime;
        public ConfigEntry<bool> enableAutoCleanup;
 public ConfigEntry<float> enemyIFF;
      
 public ConfigEntry<float> followDistance;
    public ConfigEntry<float> enemyAggressionDistance;
        
        public ConfigEntry<bool> useModernSpawnSystem;
        public ConfigEntry<string> allySosigPool;
        public ConfigEntry<string> enemySosigPool;
   
        public ConfigEntry<bool> enableArmorCustomization;
        public ConfigEntry<string> allyNamesFilePath;
        public ConfigEntry<string> enemyNamesFilePath;
    public ConfigEntry<bool> useRandomNames;
        public ConfigEntry<int> maxSosigsPerUser;
        public ConfigEntry<bool> enableCoverAI;
  public ConfigEntry<float> sosigUpdateInterval;
        
 public ConfigEntry<bool> enableChatWatcherIntegration;
        #endregion
        
        #region Sosig Pools
  public List<SosigEnemyID> allyPoolIDs = new List<SosigEnemyID>();
        public List<SosigEnemyID> enemyPoolIDs = new List<SosigEnemyID>();
        
        public SosigEnemyID defaultAllyID = SosigEnemyID.M_Swat_Scout;
        public SosigEnemyID defaultEnemyID = SosigEnemyID.M_Swat_Heavy;
        #endregion
        
   private ManualLogSource logger;
        
        public void Initialize(ConfigFile config, ManualLogSource logSource)
        {
 logger = logSource;
            
            maxAllySosigs = config.Bind("Chat Spawner", "MaxAllySosigs", 8, "Maximum ally sosigs");
            maxEnemySosigs = config.Bind("Chat Spawner", "MaxEnemySosigs", 8, "Maximum enemy sosigs");
     spawnCooldown = config.Bind("Chat Spawner", "SpawnCooldown", 2.0f, "Cooldown between spawns");
 
      enableNameplates = config.Bind("Chat Spawner", "EnableNameplates", true, "Show nameplates above sosigs");
     sosigLifetime = config.Bind("Chat Spawner", "SosigLifetime", 300.0f, "Sosig lifetime in seconds (0 = infinite)");
      enableAutoCleanup = config.Bind("Chat Spawner", "EnableAutoCleanup", true, "Auto cleanup dead sosigs");
          enemyIFF = config.Bind("Chat Spawner", "EnemyIFF", 1.0f, "Enemy IFF code");
    
   followDistance = config.Bind("Chat Spawner", "FollowDistance", 6.0f, "Distance for allies to follow player");
       enemyAggressionDistance = config.Bind("Chat Spawner", "EnemyAggressionDistance", 20.0f, "Distance at which enemies become aggressive");
            
useModernSpawnSystem = config.Bind("Chat Spawner", "UseModernSpawnSystem", true,
             "Use Update 120's modern TNH sosig spawn system (recommended)");
      allySosigPool = config.Bind("Chat Spawner", "AllySosigPool", 
            "M_Swat_Scout,M_Swat_Sniper,M_Swat_Breacher",
    "Comma-separated list of SosigEnemyID names for allies");
            enemySosigPool = config.Bind("Chat Spawner", "EnemySosigPool",
   "M_Swat_Heavy,M_Swat_Breacher,M_Swat_Sniper",
  "Comma-separated list of SosigEnemyID names for enemies");
     
            enableArmorCustomization = config.Bind("Chat Spawner Advanced", "EnableArmorCustomization", true,
     "Enable armor customization system");
         allyNamesFilePath = config.Bind("Chat Spawner Advanced", "AllyNamesFilePath", 
      "Plugins/H3TwitchTools/AllyNames.ini", "File path for ally names list (INI file)");
        enemyNamesFilePath = config.Bind("Chat Spawner Advanced", "EnemyNamesFilePath", 
       "Plugins/H3TwitchTools/EnemyNames.ini", "File path for enemy names list (INI file)");
    
          useRandomNames = config.Bind("Chat Spawner Advanced", "UseRandomNames", true,
                "Use random names from name lists");
          maxSosigsPerUser = config.Bind("Chat Spawner Advanced", "MaxSosigsPerUser", 2,
            "Maximum sosigs per Twitch user");
  enableCoverAI = config.Bind("Chat Spawner Advanced", "EnableCoverAI", true,
      "Enable advanced cover-taking AI behavior");
          sosigUpdateInterval = config.Bind("Chat Spawner Advanced", "UpdateInterval", 1.0f,
  "Interval between sosig AI updates (seconds)");
   
        enableChatWatcherIntegration = config.Bind("Chat Spawner Integration", "EnableChatWatcher", true,
    "Enable ChatWatcher integration for file-based Twitch chat spawning");
          
 logger?.LogInfo("Sosig spawn configuration initialized");
     }
        
     public void InitializeSosigPools()
 {
         try
        {
      // Parse ally pool
      var allyIDs = allySosigPool.Value.Split(',');
     foreach (var idStr in allyIDs)
    {
     try
 {
         var id = (SosigEnemyID)System.Enum.Parse(typeof(SosigEnemyID), idStr.Trim());
        allyPoolIDs.Add(id);
     }
                  catch
                    {
         logger?.LogWarning($"Invalid ally sosig ID: {idStr}");
      }
                }
      
     // Parse enemy pool
      var enemyIDs = enemySosigPool.Value.Split(',');
       foreach (var idStr in enemyIDs)
           {
             try
   {
        var id = (SosigEnemyID)System.Enum.Parse(typeof(SosigEnemyID), idStr.Trim());
      enemyPoolIDs.Add(id);
 }
         catch
       {
            logger?.LogWarning($"Invalid enemy sosig ID: {idStr}");
        }
 }
        
     // Fallback to defaults if pools are empty
    if (allyPoolIDs.Count == 0)
   {
     allyPoolIDs.Add(SosigEnemyID.M_Swat_Scout);
           allyPoolIDs.Add(SosigEnemyID.M_Swat_Sniper);
}
 
      if (enemyPoolIDs.Count == 0)
        {
        enemyPoolIDs.Add(SosigEnemyID.M_Swat_Heavy);
        enemyPoolIDs.Add(SosigEnemyID.M_Swat_Breacher);
          }
     
logger?.LogInfo($"Loaded {allyPoolIDs.Count} ally sosig types, {enemyPoolIDs.Count} enemy sosig types");
         }
  catch (System.Exception ex)
       {
       logger?.LogError($"Failed to initialize sosig pools: {ex.Message}");
        allyPoolIDs.Add(SosigEnemyID.M_Swat_Scout);
         enemyPoolIDs.Add(SosigEnemyID.M_Swat_Heavy);
            }
    }
        
        public SosigEnemyID GetRandomAllyID()
        {
  if (allyPoolIDs.Count == 0) return defaultAllyID;
            return allyPoolIDs[Random.Range(0, allyPoolIDs.Count)];
        }
    
        public SosigEnemyID GetRandomEnemyID()
        {
   if (enemyPoolIDs.Count == 0) return defaultEnemyID;
            return enemyPoolIDs[Random.Range(0, enemyPoolIDs.Count)];
        }
    }
}
