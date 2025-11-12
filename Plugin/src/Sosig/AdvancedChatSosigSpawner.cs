using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FistVR;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Advanced Chat Sosig Spawner - Updated for Anton Update 120 TNH System
    /// Full-featured system with Twitch integration, armor customization, and modern TNH spawning
    /// COMPATIBLE WITH CHATWATCHER.CS for file-based Twitch chat integration
    /// REFACTORED: Now uses modular helper classes for better maintainability
    /// </summary>
    public class AdvancedChatSosigSpawner : MonoBehaviour
    {
     #region Static Instance and Tracking
        public static AdvancedChatSosigSpawner Instance { get; private set; }
        public static List<Sosig> spawnedChatters = new List<Sosig>();
      public static List<Sosig> spawnedEnemyChatters = new List<Sosig>();
      #endregion

      #region Core Components
   private H3TVRImproved plugin;
        private ManualLogSource logger;
        private SteamFriendsIntegration steamFriends;
        private ChatWatcher chatWatcher;
        private bool chatWatcherEnabled = false;
        
        // NEW: Modular helper classes
        private SosigSpawnConfig config;
      private SosigNameManager nameManager;
   private SosigTemplateCache templateCache;
   private SosigSpawner spawner;
        private SosigBehaviorController behaviorController;
        private SosigNameplateManager nameplateManager;
        private SosigSpawnPositionCalculator positionCalculator;
   #endregion

        #region Sosig Templates - Legacy Support
        [Header("Sosig Templates")]
        public List<SosigEnemyTemplate> allyTemplates = new List<SosigEnemyTemplate>();
     public List<SosigEnemyTemplate> enemyTemplates = new List<SosigEnemyTemplate>();
    #endregion

        #region Nameplate System
   public GameObject nameplateAlly;
     public GameObject nameplateEnemy;
  public string SpawnerName = "ChatUser";
        #endregion

        #region Spawn Management
   private float lastSpawnTime;
        private Dictionary<string, int> userSosigCounts = new Dictionary<string, int>();
        private Dictionary<string, float> userLastSpawnTime = new Dictionary<string, float>();
        #endregion

        #region Initialization
        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            if (Instance != null)
            {
                Destroy(this);
return;
            }

       Instance = this;
     plugin = pluginInstance;
            logger = logSource;

  // Initialize modular components
            config = new SosigSpawnConfig();
            nameManager = new SosigNameManager();
            templateCache = new SosigTemplateCache();
  spawner = new SosigSpawner();
      behaviorController = new SosigBehaviorController();
  nameplateManager = new SosigNameplateManager();
            positionCalculator = new SosigSpawnPositionCalculator();

            // Initialize each module
   config.Initialize(plugin.Config, logger);
          nameManager.Initialize(logger);
            templateCache.Initialize(logger);
  spawner.Initialize(logger, templateCache);
            behaviorController.Initialize(logger);

            // Initialize sosig pools
            config.InitializeSosigPools();

        logger?.LogInfo("Advanced Chat Sosig Spawner initialized (Modular Architecture)");

            StartCoroutine(DelayedInitialization());
          StartCoroutine(UpdateSosigsCoroutine());
       StartCoroutine(CleanupCoroutine());
            StartCoroutine(LinkSteamFriendsIntegration());

     if (config.enableChatWatcherIntegration.Value)
   {
     StartCoroutine(InitializeChatWatcher());
          }
        }

        private IEnumerator InitializeChatWatcher()
    {
         yield return new WaitForSeconds(0.5f);
   
        try
    {
 chatWatcher = FindObjectOfType<ChatWatcher>();
      
           if (chatWatcher == null)
      {
            GameObject chatWatcherGO = new GameObject("H3TVR_ChatWatcher");
             chatWatcher = chatWatcherGO.AddComponent<ChatWatcher>();
             chatWatcher.Initialize(plugin, logger, this);
              DontDestroyOnLoad(chatWatcherGO);
        
  chatWatcherEnabled = true;
           logger?.LogInfo("ChatWatcher integration enabled");
        }
    else
{
         chatWatcherEnabled = true;
    logger?.LogInfo("ChatWatcher already exists - using existing instance");
       }
       }
      catch (Exception ex)
   {
     logger?.LogError($"Failed to initialize ChatWatcher: {ex.Message}");
       chatWatcherEnabled = false;
}
        }

   private IEnumerator LinkSteamFriendsIntegration()
        {
       yield return new WaitForSeconds(1f);
  
            try
   {
                var steamIntegration = plugin?.GetSteamFriendsIntegration();
         if (steamIntegration != null && steamIntegration.IsAvailable())
        {
        steamFriends = steamIntegration;
     logger?.LogInfo("Steam Friends integration linked successfully");
     }
      }
       catch (Exception ex)
    {
 logger?.LogWarning($"Failed to link Steam Friends integration: {ex.Message}");
          }
        }

      private IEnumerator DelayedInitialization()
        {
            float timeout = 10f;
      float elapsed = 0f;
       
    while (IM.Instance == null && elapsed < timeout)
        {
    yield return new WaitForSeconds(0.5f);
  elapsed += 0.5f;
            }
            
      if (IM.Instance == null)
            {
           logger?.LogError("IM.Instance failed to initialize within timeout");
                yield break;
  }
        
            yield return null;
    
            // Build template cache
            templateCache.BuildCache(config.allyPoolIDs, config.enemyPoolIDs);
            
      // Load legacy templates for fallback
        StartCoroutine(LoadLegacyTemplates());
 
    // Load name lists
            nameManager.LoadNameLists(config.allyNamesFilePath.Value, config.enemyNamesFilePath.Value);
  
      logger?.LogInfo("Delayed initialization complete");
      }

        private IEnumerator LoadLegacyTemplates()
   {
            yield return null;

    try
      {
   var sosigObjects = Resources.FindObjectsOfTypeAll<SosigEnemyTemplate>();
        if (sosigObjects != null && sosigObjects.Length > 0)
                {
        foreach (var template in sosigObjects)
        {
 if (template != null)
   {
          allyTemplates.Add(template);
            enemyTemplates.Add(template);
     }
           }
   
          logger?.LogInfo($"Loaded {allyTemplates.Count} legacy templates (fallback)");
       }
            }
   catch (Exception ex)
   {
      logger?.LogError($"Legacy template loading failed: {ex.Message}");
            }
        }

    private void BuildTemplateCache()
        {
            templateCache.BuildCache(config.allyPoolIDs, config.enemyPoolIDs);
        }
        #endregion

        #region Core Spawning Logic
        public void SpawningSequence(string username)
 {
            try
        {
                if (spawnedChatters.Count >= config.maxAllySosigs.Value)
                {
  logger?.LogWarning($"Max ally sosigs reached ({config.maxAllySosigs.Value})");
            return;
           }

         if (Time.time - lastSpawnTime < config.spawnCooldown.Value)
         {
             logger?.LogWarning($"Spawn cooldown active");
     return;
       }

                if (userSosigCounts.ContainsKey(username))
                {
                    if (userSosigCounts[username] >= config.maxSosigsPerUser.Value)
         {
        logger?.LogWarning($"User {username} has reached max sosigs limit");
     return;
         }
      }

       Vector3 spawnPos = positionCalculator.CalculateAllySpawnPoint();
                Quaternion spawnRot = Quaternion.identity;

    Sosig sosig = null;
            
       if (config.useModernSpawnSystem.Value)
    {
        var enemyID = config.GetRandomAllyID();
        sosig = spawner.SpawnModern(enemyID, spawnPos, spawnRot, 0);
      }
       
          if (sosig == null)
     {
        var template = GetRandomTemplate(true);
           if (template != null)
   {
            sosig = spawner.SpawnLegacy(template, spawnPos, spawnRot, 0);
        }
       }
     
       if (sosig != null)
              {
    behaviorController.SetupAllyBehavior(sosig);

  if (config.enableArmorCustomization.Value)
 {
        try
       {
          var armorIntegration = plugin?.GetSosigArmorWristMenu();
  if (armorIntegration != null && armorIntegration.IsArmorIntegrationAvailable())
       {
           armorIntegration.ApplyArmorToSosig(sosig, true);
     }
        }
         catch (Exception armorEx)
            {
    logger?.LogWarning($"Failed to apply armor: {armorEx.Message}");
           }
  }
        
       string displayName = username;
      if (config.useRandomNames.Value)
    {
   displayName = nameManager.GetRandomName(true, steamFriends, plugin.UseSteamFriendsRandomNames());
             }
     
     if (config.enableNameplates.Value && nameplateAlly != null)
    {
                  nameplateManager.AttachNameplate(sosig, displayName, nameplateAlly, false);
            }
    
            spawnedChatters.Add(sosig);
          lastSpawnTime = Time.time;
        
          if (userSosigCounts.ContainsKey(username))
           {
      userSosigCounts[username]++;
      }
      else
         {
  userSosigCounts.Add(username, 1);
        }
          
       logger?.LogInfo($"✓ Spawned ally sosig '{displayName}' for {username}");
      }
  }
            catch (Exception ex)
          {
     logger?.LogError($"Ally spawn failed for {username}: {ex.Message}");
   }
        }

        public void SpawningSequenceEnemy(int IFF, string username)
 {
        try
            {
        if (spawnedEnemyChatters.Count >= config.maxEnemySosigs.Value)
                {
           logger?.LogWarning($"Max enemy sosigs reached ({config.maxEnemySosigs.Value})");
   return;
          }

       if (Time.time - lastSpawnTime < config.spawnCooldown.Value)
  {
      logger?.LogWarning($"Spawn cooldown active");
    return;
         }

      Vector3 spawnPos = positionCalculator.CalculateEnemySpawnPoint();
      Quaternion spawnRot = Quaternion.identity;
   int finalIFF = IFF > 0 ? IFF : Mathf.Max(1, (int)config.enemyIFF.Value);

            Sosig sosig = null;
   
      if (config.useModernSpawnSystem.Value)
     {
  var enemyID = config.GetRandomEnemyID();
  sosig = spawner.SpawnModern(enemyID, spawnPos, spawnRot, finalIFF);
       }
   
   if (sosig == null)
       {
               var template = GetRandomTemplate(false);
  if (template != null)
   {
             sosig = spawner.SpawnLegacy(template, spawnPos, spawnRot, finalIFF);
  }
      }
      
        if (sosig != null)
          {
          behaviorController.SetupEnemyBehavior(sosig);
      
         if (config.enableArmorCustomization.Value)
{
        try
         {
      var armorIntegration = plugin?.GetSosigArmorWristMenu();
     if (armorIntegration != null && armorIntegration.IsArmorIntegrationAvailable())
    {
        armorIntegration.ApplyArmorToSosig(sosig, false);
         }
 }
       catch (Exception armorEx)
              {
           logger?.LogWarning($"Failed to apply armor: {armorEx.Message}");
       }
           }
    
 string displayName = username;
          if (config.useRandomNames.Value)
      {
            displayName = nameManager.GetRandomName(false, steamFriends, plugin.UseSteamFriendsRandomNames());
  }
  
    if (config.enableNameplates.Value && nameplateEnemy != null)
    {
           nameplateManager.AttachNameplate(sosig, displayName, nameplateEnemy, true);
               }
     
      spawnedEnemyChatters.Add(sosig);
           lastSpawnTime = Time.time;
 
    logger?.LogInfo($"✓ Spawned enemy sosig '{displayName}' for {username}");
          }
            }
            catch (Exception ex)
            {
    logger?.LogError($"Enemy spawn failed for {username}: {ex.Message}");
            }
        }

     public void SpawningSequenceBoss(string bossType, string username = null)
        {
   try
 {
     if (spawnedEnemyChatters.Count >= config.maxEnemySosigs.Value)
      {
            logger?.LogWarning($"Max enemy sosigs reached - cannot spawn boss");
     return;
   }

         if (Time.time - lastSpawnTime < config.spawnCooldown.Value)
        {
     logger?.LogWarning("Spawn cooldown active");
  return;
   }

     Vector3 spawnPos = positionCalculator.CalculateBossSpawnPoint();
    Quaternion spawnRot = Quaternion.identity;
    int finalIFF = Mathf.Max(1, (int)config.enemyIFF.Value);

   Sosig sosig = null;
 
          if (config.useModernSpawnSystem.Value)
     {
var enemyID = GetBossTemplate(bossType);
         sosig = spawner.SpawnModern(enemyID, spawnPos, spawnRot, finalIFF);
     }
    
  if (sosig == null)
      {
   var template = GetRandomTemplate(false);
        if (template != null)
               {
   sosig = spawner.SpawnLegacy(template, spawnPos, spawnRot, finalIFF);
 }
     }
     
    if (sosig != null)
   {
behaviorController.SetupEnemyBehavior(sosig);
      
        if (config.enableArmorCustomization.Value)
    {
        try
    {
    var armorIntegration = plugin?.GetSosigArmorWristMenu();
if (armorIntegration != null && armorIntegration.IsArmorIntegrationAvailable())
      {
armorIntegration.ApplyArmorToSosig(sosig, false);
}
       }
      catch { }
       }
    
   string displayName = username ?? $"BOSS_{bossType}";
 
    if (config.enableNameplates.Value && nameplateEnemy != null)
   {
   nameplateManager.AttachNameplate(sosig, $"★ {displayName} ★", nameplateEnemy, true);
         }
 
   spawnedEnemyChatters.Add(sosig);
   lastSpawnTime = Time.time;
  
       logger?.LogInfo($"Spawned {bossType} BOSS '{displayName}'");
   }
    }
catch (Exception ex)
       {
logger?.LogError($"Boss spawn failed: {ex.Message}");
         }
     }

      /// <summary>
        /// Overload for BossType enum support
   /// </summary>
        public void SpawningSequenceBoss(BossSosigSystem.BossType bossType, string username = null)
 {
            SpawningSequenceBoss(bossType.ToString(), username);
        }

     private SosigEnemyID GetBossTemplate(string bossType)
  {
            switch (bossType.ToLower())
            {
       case "tank":
         case "juggernaut":
  return SosigEnemyID.M_Swat_Heavy;
    case "sniper":
          return SosigEnemyID.M_Swat_Sniper;
        case "berserker":
    case "assassin":
           return SosigEnemyID.M_Swat_Breacher;
    default:
       return config.GetRandomEnemyID();
       }
        }

  private SosigEnemyTemplate GetRandomTemplate(bool isAlly)
        {
    var templates = isAlly ? allyTemplates : enemyTemplates;
         
        if (templates == null || templates.Count == 0)
            {
    logger?.LogWarning($"No {(isAlly ? "ally" : "enemy")} templates available");
                return null;
  }

  return templates[UnityEngine.Random.Range(0, templates.Count)];
 }
    #endregion

        #region Update and Cleanup
  private IEnumerator UpdateSosigsCoroutine()
   {
  var wait = new WaitForSeconds(config.sosigUpdateInterval.Value);

            while (true)
  {
          yield return wait;
   UpdateAllySosigs();
       UpdateEnemySosigs();
            }
    }

      private void UpdateAllySosigs()
    {
       if (GM.CurrentPlayerBody?.Head == null) return;

      for (int i = spawnedChatters.Count - 1; i >= 0; i--)
{
           if (spawnedChatters[i] == null || spawnedChatters[i].BodyState == Sosig.SosigBodyState.Dead)
     {
     if (config.enableAutoCleanup.Value && spawnedChatters[i] != null)
        {
     spawnedChatters[i].TickDownToClear(3);
             }
     spawnedChatters.RemoveAt(i);
 continue;
          }

        behaviorController.UpdateAllyBehavior(spawnedChatters[i], config.followDistance.Value);
        }
        }

        private void UpdateEnemySosigs()
  {
  if (GM.CurrentPlayerBody?.Head == null) return;

            for (int i = spawnedEnemyChatters.Count - 1; i >= 0; i--)
        {
    if (spawnedEnemyChatters[i] == null || spawnedEnemyChatters[i].BodyState == Sosig.SosigBodyState.Dead)
                {
    if (config.enableAutoCleanup.Value && spawnedEnemyChatters[i] != null)
      {
      spawnedEnemyChatters[i].TickDownToClear(3);
    }
      spawnedEnemyChatters.RemoveAt(i);
      continue;
                }

          behaviorController.UpdateEnemyBehavior(spawnedEnemyChatters[i], config.enemyAggressionDistance.Value);
     }
   }

        private IEnumerator CleanupCoroutine()
        {
   var wait = new WaitForSeconds(10f);

  while (true)
     {
            yield return wait;
      CleanupDeadSosigs();
            }
  }

    private void CleanupDeadSosigs()
        {
            if (!config.enableAutoCleanup.Value) return;

       foreach (var sosig in spawnedChatters.Concat(spawnedEnemyChatters))
   {
       if (sosig != null && sosig.BodyState == Sosig.SosigBodyState.Dead)
        {
       sosig.TickDownToClear(3);
        }
        }
        }
        #endregion

    #region Public API
        public void ClearSosigs(bool clearAllies = true, bool clearEnemies = true)
 {
            try
      {
              int cleared = 0;

      if (clearAllies)
 {
   for (int i = spawnedChatters.Count - 1; i >= 0; i--)
              {
   if (spawnedChatters[i] != null)
          {
   Destroy(spawnedChatters[i].gameObject);
      cleared++;
       }
 }
         spawnedChatters.Clear();
         }

  if (clearEnemies)
              {
           for (int i = spawnedEnemyChatters.Count - 1; i >= 0; i--)
      {
    if (spawnedEnemyChatters[i] != null)
         {
                 Destroy(spawnedEnemyChatters[i].gameObject);
                 cleared++;
     }
           }
      spawnedEnemyChatters.Clear();
      }
     
            userSosigCounts.Clear();
 userLastSpawnTime.Clear();

    logger?.LogInfo($"Cleared {cleared} sosigs");
   }
            catch (Exception ex)
     {
     logger?.LogError($"Clear sosigs failed: {ex.Message}");
   }
   }

        public void ClearAllSosigs()
        {
        ClearSosigs(true, true);
     }

        public ChatWatcher GetChatWatcher()
        {
    return chatWatcher;
        }
        
        public bool IsChatWatcherEnabled()
        {
            return chatWatcherEnabled && chatWatcher != null;
        }

        public void QueueSpawn(string username, string displayName, bool isFriendly, string armorPreset = null, SpawnPriority priority = SpawnPriority.Normal, string behavior = null)
        {
            try
  {
        if (isFriendly)
  {
       SpawningSequence(displayName ?? username);
       }
        else
       {
       SpawningSequenceEnemy((int)config.enemyIFF.Value, displayName ?? username);
       }
       }
            catch (Exception ex)
      {
    logger?.LogError($"QueueSpawn failed for {username}: {ex.Message}");
   }
        }

        public struct SosigStats
        {
            public int Allies;
          public int Enemies;
        public int Queued;
      public int TotalActive;
  public bool ChatWatcherActive;
        }

      public SosigStats GetStats()
        {
            return new SosigStats
        {
                Allies = spawnedChatters.Count,
Enemies = spawnedEnemyChatters.Count,
         Queued = 0,
    TotalActive = spawnedChatters.Count + spawnedEnemyChatters.Count,
            ChatWatcherActive = chatWatcherEnabled && chatWatcher != null
   };
     }

        public bool QueueTwitchSpawnRequest(string username, string displayName, bool isFriendly, string armorPreset = null, SpawnPriority priority = SpawnPriority.Normal, string requestedBehavior = null)
        {
            try
      {
        if (isFriendly)
              {
    SpawningSequence(displayName ?? username);
          }
   else
                {
    SpawningSequenceEnemy((int)config.enemyIFF.Value, displayName ?? username);
          }
    return true;
    }
catch (Exception ex)
       {
       logger?.LogError($"Twitch spawn request failed for {username}: {ex.Message}");
      return false;
            }
    }
        #endregion
    }
}

