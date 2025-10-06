using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using FistVR;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Enhanced Chat Spawner - Sosig spawning system based on H3TwitchTools design
    /// Simplified for reliability with Twitch integration support
    /// </summary>
    public class EnhancedChatSpawner : MonoBehaviour
    {
        #region Static Instance and Tracking
        public static EnhancedChatSpawner Instance { get; private set; }
        public static List<Sosig> spawnedChatters = new List<Sosig>();
        public static List<Sosig> spawnedEnemyChatters = new List<Sosig>();
        #endregion

        #region Core Components
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        private TwitchChatManager twitchManager;
        #endregion

        #region Sosig Templates
        [Header("Sosig Templates")]
        public SosigEnemyTemplate defaultAllyTemplate;
        public List<SosigEnemyTemplate> allyTemplates = new List<SosigEnemyTemplate>();
        public List<SosigEnemyTemplate> enemyTemplates = new List<SosigEnemyTemplate>();
        
        private SosigEnemyTemplate[] cachedSosigTemplates;
        #endregion

        #region Nameplate System
        public GameObject nameplateAlly;
        public GameObject nameplateEnemy;
        public string SpawnerName = "ChatUser";
        #endregion

        #region Configuration
        private ConfigEntry<int> maxAllySosigs;
        private ConfigEntry<int> maxEnemySosigs;
        private ConfigEntry<float> spawnCooldown;
        private ConfigEntry<bool> enableNameplates;
        private ConfigEntry<float> sosigLifetime;
        private ConfigEntry<bool> enableAutoCleanup;
        private ConfigEntry<float> enemyIFF;
        private ConfigEntry<KeyCode> spawnAllyKey;
        private ConfigEntry<KeyCode> spawnEnemyKey;
        private ConfigEntry<KeyCode> clearSosigsKey;
        private ConfigEntry<float> followDistance;
        private ConfigEntry<float> enemyAggressionDistance;
        #endregion

        #region Spawn Management
        private float lastSpawnTime;
        private static readonly LayerMask EnvironmentMask = LayerMask.GetMask("Environment");
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

            InitializeConfiguration();
            InitializeSosigTemplates();

            logger?.LogInfo("Enhanced Chat Spawner initialized (H3TwitchTools style)");

            // Start coroutines
            StartCoroutine(UpdateSosigsCoroutine());
            StartCoroutine(CleanupCoroutine());
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
                maxAllySosigs = plugin.Config.Bind("Chat Spawner", "MaxAllySosigs", 8, 
                    "Maximum ally sosigs");
                maxEnemySosigs = plugin.Config.Bind("Chat Spawner", "MaxEnemySosigs", 8, 
                    "Maximum enemy sosigs");
                spawnCooldown = plugin.Config.Bind("Chat Spawner", "SpawnCooldown", 2.0f, 
                    "Cooldown between spawns");
                
                enableNameplates = plugin.Config.Bind("Chat Spawner", "EnableNameplates", true, 
                    "Show nameplates above sosigs");
                sosigLifetime = plugin.Config.Bind("Chat Spawner", "SosigLifetime", 300.0f, 
                    "Sosig lifetime in seconds (0 = infinite)");
                enableAutoCleanup = plugin.Config.Bind("Chat Spawner", "EnableAutoCleanup", true, 
                    "Auto cleanup dead sosigs");
                enemyIFF = plugin.Config.Bind("Chat Spawner", "EnemyIFF", 1.0f, 
                    "Enemy IFF code");
                
                followDistance = plugin.Config.Bind("Chat Spawner", "FollowDistance", 6.0f, 
                    "Distance for allies to follow player");
                enemyAggressionDistance = plugin.Config.Bind("Chat Spawner", "EnemyAggressionDistance", 20.0f, 
                    "Distance at which enemies become aggressive");
                
                spawnAllyKey = plugin.Config.Bind("Chat Spawner Keys", "SpawnAllyKey", KeyCode.P, 
                    "Spawn ally key");
                spawnEnemyKey = plugin.Config.Bind("Chat Spawner Keys", "SpawnEnemyKey", KeyCode.O, 
                    "Spawn enemy key");
                clearSosigsKey = plugin.Config.Bind("Chat Spawner Keys", "ClearSosigsKey", KeyCode.Delete, 
                    "Clear sosigs key");

                logger?.LogInfo("Configuration initialized successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Config init failed: {ex.Message}");
            }
        }

        private void InitializeSosigTemplates()
        {
            try
            {
                StartCoroutine(LoadTemplatesDelayed());
            }
            catch (Exception ex)
            {
                logger?.LogError($"Template initialization failed: {ex.Message}");
            }
        }

        private IEnumerator LoadTemplatesDelayed()
        {
            yield return null; // Wait one frame

            try
            {
                var sosigObjects = Resources.FindObjectsOfTypeAll<SosigEnemyTemplate>();
                if (sosigObjects != null && sosigObjects.Length > 0)
                {
                    cachedSosigTemplates = sosigObjects;
                    
                    foreach (var template in cachedSosigTemplates)
                    {
                        if (template != null)
                        {
                            allyTemplates.Add(template);
                            enemyTemplates.Add(template);
                        }
                    }
                    
                    if (cachedSosigTemplates.Length > 0)
                    {
                        defaultAllyTemplate = cachedSosigTemplates[0];
                    }
                    
                    logger?.LogInfo($"Loaded {allyTemplates.Count} sosig templates");
                }
                else
                {
                    logger?.LogWarning("No sosig templates found - spawning may be limited");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Template loading failed: {ex.Message}");
            }
        }
        #endregion

        #region Core Spawning Logic (H3TwitchTools Style)
        /// <summary>
        /// Spawn friendly sosig - H3TwitchTools style
        /// </summary>
        public void SpawningSequence(string username)
        {
            try
            {
                if (spawnedChatters.Count >= maxAllySosigs.Value)
                {
                    logger?.LogWarning("Max ally sosigs reached");
                    return;
                }

                if (Time.time - lastSpawnTime < spawnCooldown.Value)
                {
                    logger?.LogWarning("Spawn cooldown active");
                    return;
                }

                var template = GetRandomTemplate(true);
                if (template == null)
                {
                    logger?.LogError("No ally template available");
                    return;
                }

                Vector3 spawnPos = CalculateAllySpawnPoint();
                Quaternion spawnRot = Quaternion.identity;

                // Spawn the sosig
                Sosig sosig = SpawnSosig(template, spawnPos, spawnRot, 0);
                
                if (sosig != null)
                {
                    // Set up ally behavior
                    SetupAllyBehavior(sosig);
                    
                    // Add nameplate
                    if (enableNameplates.Value && nameplateAlly != null)
                    {
                        AttachNameplate(sosig, username ?? "Ally", nameplateAlly, false);
                    }
                    
                    // Track sosig
                    spawnedChatters.Add(sosig);
                    lastSpawnTime = Time.time;
                    
                    logger?.LogInfo($"Spawned ally sosig for {username}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Ally spawn failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawn enemy sosig - H3TwitchTools style
        /// </summary>
        public void SpawningSequenceEnemy(int IFF, string username)
        {
            try
            {
                if (spawnedEnemyChatters.Count >= maxEnemySosigs.Value)
                {
                    logger?.LogWarning("Max enemy sosigs reached");
                    return;
                }

                if (Time.time - lastSpawnTime < spawnCooldown.Value)
                {
                    logger?.LogWarning("Spawn cooldown active");
                    return;
                }

                var template = GetRandomTemplate(false);
                if (template == null)
                {
                    logger?.LogError("No enemy template available");
                    return;
                }

                Vector3 spawnPos = CalculateEnemySpawnPoint();
                Quaternion spawnRot = Quaternion.identity;

                // Use configured IFF or parameter
                int finalIFF = IFF > 0 ? IFF : Mathf.Max(1, (int)enemyIFF.Value);

                // Spawn the sosig
                Sosig sosig = SpawnSosig(template, spawnPos, spawnRot, finalIFF);
                
                if (sosig != null)
                {
                    // Set up enemy behavior
                    SetupEnemyBehavior(sosig);
                    
                    // Add nameplate
                    if (enableNameplates.Value && nameplateEnemy != null)
                    {
                        AttachNameplate(sosig, username ?? "Enemy", nameplateEnemy, true);
                    }
                    
                    // Track sosig
                    spawnedEnemyChatters.Add(sosig);
                    lastSpawnTime = Time.time;
                    
                    logger?.LogInfo($"Spawned enemy sosig for {username}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Enemy spawn failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Core sosig spawning method - H3TwitchTools style
        /// </summary>
        private Sosig SpawnSosig(SosigEnemyTemplate template, Vector3 pos, Quaternion rot, int IFF)
        {
            try
            {
                if (template == null || template.SosigPrefabs == null || template.SosigPrefabs.Count == 0)
                {
                    logger?.LogError("Invalid template");
                    return null;
                }

                // Get random prefab
                var prefab = template.SosigPrefabs[UnityEngine.Random.Range(0, template.SosigPrefabs.Count)];
                if (prefab?.GetGameObject() == null)
                {
                    logger?.LogError("Invalid prefab");
                    return null;
                }

                // Instantiate sosig
                GameObject sosigGO = Instantiate(prefab.GetGameObject(), pos, rot);
                Sosig sosig = sosigGO.GetComponentInChildren<Sosig>();
                
                if (sosig == null)
                {
                    Destroy(sosigGO);
                    return null;
                }

                // Configure sosig
                if (template.ConfigTemplates != null && template.ConfigTemplates.Count > 0)
                {
                    var config = template.ConfigTemplates[UnityEngine.Random.Range(0, template.ConfigTemplates.Count)];
                    if (config != null)
                    {
                        sosig.Configure(config);
                    }
                }

                // Set IFF
                sosig.E.IFFCode = IFF;
                if (IFF < sosig.Priority.IFFChart.Length)
                {
                    sosig.Priority.IFFChart[IFF] = true;
                }

                // Equip weapons
                EquipWeapons(sosig, template, pos, rot);

                // Apply outfit
                if (template.OutfitConfig != null && template.OutfitConfig.Count > 0)
                {
                    ApplyOutfit(sosig, template.OutfitConfig[UnityEngine.Random.Range(0, template.OutfitConfig.Count)]);
                }

                return sosig;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Sosig spawn failed: {ex.Message}");
                return null;
            }
        }

        private void EquipWeapons(Sosig sosig, SosigEnemyTemplate template, Vector3 pos, Quaternion rot)
        {
            try
            {
                // Primary weapon
                if (template.WeaponOptions != null && template.WeaponOptions.Count > 0)
                {
                    EquipWeapon(sosig, template.WeaponOptions[UnityEngine.Random.Range(0, template.WeaponOptions.Count)], pos, rot);
                }

                // Secondary weapon
                if (template.WeaponOptions_Secondary != null && template.WeaponOptions_Secondary.Count > 0)
                {
                    EquipWeapon(sosig, template.WeaponOptions_Secondary[UnityEngine.Random.Range(0, template.WeaponOptions_Secondary.Count)], pos, rot);
                }

                // Tertiary weapon
                if (template.WeaponOptions_Tertiary != null && template.WeaponOptions_Tertiary.Count > 0)
                {
                    EquipWeapon(sosig, template.WeaponOptions_Tertiary[UnityEngine.Random.Range(0, template.WeaponOptions_Tertiary.Count)], pos, rot);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Weapon equip failed: {ex.Message}");
            }
        }

        private void EquipWeapon(Sosig sosig, FVRObject weaponObj, Vector3 pos, Quaternion rot)
        {
            try
            {
                if (weaponObj?.GetGameObject() == null) return;

                GameObject weaponGO = Instantiate(weaponObj.GetGameObject(), pos + Vector3.up * 0.1f, rot);
                SosigWeapon weapon = weaponGO.GetComponent<SosigWeapon>();
                
                if (weapon != null)
                {
                    weapon.SetAutoDestroy(true);
                    weapon.O.SpawnLockable = false;
                    weapon.SetAmmoClamping(true);
                    weapon.IsShakeReloadable = false;

                    if (weapon.Type == SosigWeapon.SosigWeaponType.Gun)
                    {
                        sosig.Inventory.FillAmmoWithType(weapon.AmmoType);
                    }

                    sosig.Inventory.Init();
                    sosig.Inventory.FillAllAmmo();
                    sosig.InitHands();
                    sosig.ForceEquip(weapon);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Weapon equip error: {ex.Message}");
            }
        }

        private void ApplyOutfit(Sosig sosig, SosigOutfitConfig outfit)
        {
            try
            {
                if (outfit == null || sosig.Links.Count < 4) return;

                if (UnityEngine.Random.value < outfit.Chance_Headwear)
                    SpawnAccessory(outfit.Headwear, sosig.Links[0]);
                if (UnityEngine.Random.value < outfit.Chance_Facewear)
                    SpawnAccessory(outfit.Facewear, sosig.Links[0]);
                if (UnityEngine.Random.value < outfit.Chance_Eyewear)
                    SpawnAccessory(outfit.Eyewear, sosig.Links[0]);
                if (UnityEngine.Random.value < outfit.Chance_Torsowear)
                    SpawnAccessory(outfit.Torsowear, sosig.Links[1]);
                if (UnityEngine.Random.value < outfit.Chance_Pantswear)
                    SpawnAccessory(outfit.Pantswear, sosig.Links[2]);
                if (sosig.Links.Count > 3 && UnityEngine.Random.value < outfit.Chance_Pantswear_Lower)
                    SpawnAccessory(outfit.Pantswear_Lower, sosig.Links[3]);
                if (UnityEngine.Random.value < outfit.Chance_Backpacks)
                    SpawnAccessory(outfit.Backpacks, sosig.Links[1]);
                if (UnityEngine.Random.value < outfit.Chance_TorosDecoration)
                    SpawnAccessory(outfit.TorosDecoration, sosig.Links[1]);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Outfit apply failed: {ex.Message}");
            }
        }

        private void SpawnAccessory(List<FVRObject> accessories, SosigLink link)
        {
            if (accessories == null || accessories.Count == 0 || link == null) return;

            try
            {
                var accessory = accessories[UnityEngine.Random.Range(0, accessories.Count)];
                if (accessory?.GetGameObject() == null) return;

                GameObject accessoryGO = Instantiate(accessory.GetGameObject(), link.transform.position, link.transform.rotation);
                accessoryGO.transform.SetParent(link.transform);
                
                var wearable = accessoryGO.GetComponent<SosigWearable>();
                if (wearable != null)
                {
                    wearable.RegisterWearable(link);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Accessory spawn failed: {ex.Message}");
            }
        }
        #endregion

        #region Behavior Setup
        private void SetupAllyBehavior(Sosig sosig)
        {
            try
            {
                if (GM.CurrentPlayerBody?.Head?.transform == null) return;

                var playerPos = GM.CurrentPlayerBody.Head.transform.position;
                float offsetX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                float offsetZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                Vector3 followPoint = new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);
                
                sosig.CommandAssaultPoint(followPoint);
                sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Ally behavior setup failed: {ex.Message}");
            }
        }

        private void SetupEnemyBehavior(Sosig sosig)
        {
            try
            {
                if (GM.CurrentPlayerBody?.transform == null) return;

                sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Enemy behavior setup failed: {ex.Message}");
            }
        }
        #endregion

        #region Nameplate System
        private void AttachNameplate(Sosig sosig, string name, GameObject nameplatePrefab, bool isEnemy)
        {
            try
            {
                if (sosig.Links.Count == 0 || nameplatePrefab == null) return;

                SpawnerName = name;
                
                GameObject nameplate = Instantiate(nameplatePrefab, sosig.Links[1].transform, false);
                nameplate.transform.localPosition = Vector3.zero;
                nameplate.transform.localRotation = Quaternion.identity;
                
                var textComponents = nameplate.GetComponentsInChildren<Text>();
                foreach (Text text in textComponents)
                {
                    text.text = name;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Nameplate attach failed: {ex.Message}");
            }
        }
        #endregion

        #region Spawn Point Calculation
        private Vector3 CalculateAllySpawnPoint()
        {
            if (GM.CurrentPlayerBody?.Head?.transform == null)
                return Vector3.zero;

            var playerPos = GM.CurrentPlayerBody.Head.transform.position;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(2f, 4f);
            
            return new Vector3(
                playerPos.x + Mathf.Cos(angle) * distance,
                playerPos.y,
                playerPos.z + Mathf.Sin(angle) * distance
            );
        }

        private Vector3 CalculateEnemySpawnPoint()
        {
            if (GM.CurrentPlayerBody?.Head?.transform == null)
                return Vector3.zero;

            var playerPos = GM.CurrentPlayerBody.Head.transform.position;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(8f, 15f);
            
            return new Vector3(
                playerPos.x + Mathf.Cos(angle) * distance,
                playerPos.y,
                playerPos.z + Mathf.Sin(angle) * distance
            );
        }
        #endregion

        #region Update and Cleanup - H3TwitchTools Style
        private IEnumerator UpdateSosigsCoroutine()
        {
            var wait = new WaitForSeconds(1f);

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
                    if (enableAutoCleanup.Value && spawnedChatters[i] != null)
                    {
                        spawnedChatters[i].TickDownToClear(3);
                    }
                    spawnedChatters.RemoveAt(i);
                    continue;
                }

                var sosig = spawnedChatters[i];
                
                // Follow player logic
                if (!sosig.m_isStunned)
                {
                    var playerPos = GM.CurrentPlayerBody.Head.position;
                    float distance = Vector3.Distance(playerPos, sosig.m_assaultPoint);
                    
                    if (distance > followDistance.Value)
                    {
                        float offsetX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                        float offsetZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                        Vector3 followPoint = new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);
                        
                        bool isBad = Physics.Linecast(playerPos, followPoint, EnvironmentMask);
                        if (!isBad)
                        {
                            sosig.CommandAssaultPoint(followPoint);
                        }
                    }
                }

                // Combat response
                if (sosig.Priority.HasFreshTarget() && sosig.CurrentOrder == Sosig.SosigOrder.Investigate && sosig.m_entityRecognition >= 0.65f)
                {
                    sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
                }
            }
        }

        private void UpdateEnemySosigs()
        {
            if (GM.CurrentPlayerBody?.Head == null) return;

            for (int i = spawnedEnemyChatters.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemyChatters[i] == null || spawnedEnemyChatters[i].BodyState == Sosig.SosigBodyState.Dead)
                {
                    if (enableAutoCleanup.Value && spawnedEnemyChatters[i] != null)
                    {
                        spawnedEnemyChatters[i].TickDownToClear(3);
                    }
                    spawnedEnemyChatters.RemoveAt(i);
                    continue;
                }

                var sosig = spawnedEnemyChatters[i];
                
                // Aggression logic
                if (!sosig.m_isStunned)
                {
                    var playerPos = GM.CurrentPlayerBody.Head.position;
                    float distance = Vector3.Distance(playerPos, sosig.Links[1].transform.position);
                    
                    if (distance > enemyAggressionDistance.Value)
                    {
                        sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                    }
                }

                // Combat response
                if (sosig.Priority.HasFreshTarget() && sosig.CurrentOrder == Sosig.SosigOrder.Investigate && sosig.m_entityRecognition >= 0.55f)
                {
                    sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
                }
                
                // Force aggression if idle
                if (sosig.CurrentOrder == Sosig.SosigOrder.Disabled || sosig.CurrentOrder == Sosig.SosigOrder.Idle || sosig.CurrentOrder == Sosig.SosigOrder.GuardPoint)
                {
                    sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                }
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
            if (!enableAutoCleanup.Value) return;

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

                logger?.LogInfo($"Cleared {cleared} sosigs");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Clear sosigs failed: {ex.Message}");
            }
        }

        public struct ChatSosigStats
        {
            public int ActiveAllies;
            public int ActiveEnemies;
            public int QueueLength;
            public int TotalSpawned;
        }

        public ChatSosigStats GetStats()
        {
            return new ChatSosigStats
            {
                ActiveAllies = spawnedChatters.Count,
                ActiveEnemies = spawnedEnemyChatters.Count,
                QueueLength = 0,
                TotalSpawned = spawnedChatters.Count + spawnedEnemyChatters.Count
            };
        }
        #endregion

        #region Unity Lifecycle
        void Update()
        {
            if (spawnAllyKey?.Value != KeyCode.None && Input.GetKeyDown(spawnAllyKey.Value))
            {
                SpawningSequence("ManualAlly");
            }

            if (spawnEnemyKey?.Value != KeyCode.None && Input.GetKeyDown(spawnEnemyKey.Value))
            {
                SpawningSequenceEnemy((int)enemyIFF.Value, "ManualEnemy");
            }

            if (clearSosigsKey?.Value != KeyCode.None && Input.GetKeyDown(clearSosigsKey.Value))
            {
                ClearSosigs(true, true);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Helper Methods
        private SosigEnemyTemplate GetRandomTemplate(bool isFriendly)
        {
            var list = isFriendly ? allyTemplates : enemyTemplates;
            if (list != null && list.Count > 0)
                return list[UnityEngine.Random.Range(0, list.Count)];

            if (defaultAllyTemplate != null)
                return defaultAllyTemplate;

            if (cachedSosigTemplates != null && cachedSosigTemplates.Length > 0)
                return cachedSosigTemplates[0];

            return null;
        }

        // Twitch-compatible spawn request method
        public bool QueueTwitchSpawnRequest(string username, string displayName, bool isFriendly, string armorPreset = null, SpawnPriority priority = SpawnPriority.Normal, string requestedBehavior = null)
        {
            try
            {
                // Simple immediate spawn for compatibility
                if (isFriendly)
                {
                    SpawningSequence(displayName ?? username);
                }
                else
                {
                    SpawningSequenceEnemy((int)enemyIFF.Value, displayName ?? username);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public enum SpawnPriority
        {
            Low = 0,
            Normal = 1,
            High = 2,
            Immediate = 3
        }
        #endregion
    }
}

