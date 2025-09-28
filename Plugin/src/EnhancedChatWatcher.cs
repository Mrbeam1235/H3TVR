using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using FistVR;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Enhanced Chat Spawner integrated directly into H3TVR
    /// Provides advanced sosig spawning with armor presets, queue management, and Twitch integration
    /// </summary>
    public class EnhancedChatWatcher : MonoBehaviour
    {
        public static EnhancedChatWatcher instance;
        public static List<Sosig> spawnedChatters = new List<Sosig>();
        public static List<Sosig> spawnedEnemyChatters = new List<Sosig>();
        
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        
        // Enhanced features
        private readonly Dictionary<string, ArmorConfiguration> armorPresets = new Dictionary<string, ArmorConfiguration>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<QueuedChatSpawn> spawnQueue = new Queue<QueuedChatSpawn>();
        private Dictionary<string, List<FVRObject>> availableArmor;
        private List<string> cachedAllyNames;
        private List<string> cachedEnemyNames;
        private DateTime allyNamesLastWrite;
        private DateTime enemyNamesLastWrite;
        private SosigEnemyTemplate[] cachedSosigTemplates;
        
        private string armorPresetConfigPath;
        private LayerMask environmentMask;
        private TNH_Manager TNHManager;
        
        // Configuration entries
        private ConfigEntry<string> filePathToTextFolder;
        private ConfigEntry<string> filePathToTextFolderforEnemySosig;
        private ConfigEntry<KeyCode> keyToSpawnEnemySosig;
        private ConfigEntry<KeyCode> keyToSpawn;
        private ConfigEntry<KeyCode> armorGUIKey;
        private ConfigEntry<int> maxActiveSosigs;
        private ConfigEntry<bool> enableNameplates;
        private ConfigEntry<bool> enableArmorSystem;
        private ConfigEntry<float> followDistance;
        private ConfigEntry<float> spawnQueueInterval;
        
        // GUI
        private bool showArmorGUI;
        private Rect armorWindowRect = new Rect(50, 50, 500, 600);
        private Vector2 scrollPosition;
        private GUIStyle windowStyle, buttonStyle, labelStyle, toggleStyle, headerStyle, sectionStyle, infoStyle;
        
        // Enhanced spawn management
        private int totalSpawnedCount;
        private float lastSpawnTime;
        private float lastPerformanceCheck;
        
        // Events
        public static event Action<Sosig, bool, string> OnSosigSpawned;
        
        private class QueuedChatSpawn 
        { 
            public string Name; 
            public bool Friendly; 
            public string ArmorSetName; 
            public bool IsFromTwitchQueue;
        }

        [Serializable]
        public class ArmorConfiguration
        {
            public bool enableHeadwear = true, enableFacewear = true, enableEyewear = true, enableTorsowear = true;
            public bool enablePantswear = true, enablePantswearLower = true, enableBackpacks = true, enableDecorations = true;
            public float headwearChance = 0.7f, facewearChance = 0.3f, eyewearChance = 0.4f, torsowearChance = 0.8f;
            public float pantswearChance = 0.6f, pantswearLowerChance = 0.4f, backpackChance = 0.2f, decorationChance = 0.1f;
            public string presetName; 
            public string description; 
            public string armorLevel;
        }

        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            instance = this;
            plugin = pluginInstance;
            logger = logSource;
            InitializeConfiguration();
            SetupArmorSystem();
            environmentMask = LayerMask.GetMask("Environment");
        }

        public void Start()
        {
            if (enableArmorSystem.Value)
            {
                StartCoroutine(ProcessSpawnQueueCoroutine());
                StartCoroutine(UpdateSosigsCoroutine());
            }
        }

        private void InitializeConfiguration()
        {
            var config = plugin.Config;
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string lb = Path.Combine(docs, "LioranBoard 2.0");
            
            filePathToTextFolder = config.Bind("Enhanced Chat Spawner", "AllyFilePath", Path.Combine(lb, "ally.ini"),
                "The File Path to where the name of the ally chatter can be found");
            filePathToTextFolderforEnemySosig = config.Bind("Enhanced Chat Spawner", "EnemyFilePath", Path.Combine(lb, "enemy.ini"),
                "The File Path to where the name of the enemy chatter can be found");
            keyToSpawnEnemySosig = config.Bind("Enhanced Chat Spawner", "KeyBindForEnemySpawn", KeyCode.Keypad7,
                "The key used to spawn the enemy sosigs");
            keyToSpawn = config.Bind("Enhanced Chat Spawner", "KeyBind", KeyCode.P,
                "The key used to spawn the sosigs");
            
            // Enhanced configuration
            armorGUIKey = config.Bind("Enhanced Chat Spawner", "ArmorGUIKey", KeyCode.F6,
                "Key to open armor configuration GUI");
            maxActiveSosigs = config.Bind("Enhanced Chat Spawner", "MaxActiveSosigs", 15,
                "Maximum number of active sosigs");
            enableNameplates = config.Bind("Enhanced Chat Spawner", "EnableNameplates", true,
                "Show nameplates above sosigs");
            enableArmorSystem = config.Bind("Enhanced Chat Spawner", "EnableArmorSystem", true,
                "Enable advanced armor preset system");
            followDistance = config.Bind("Enhanced Chat Spawner", "FollowDistance", 6f,
                "Distance at which sosigs follow the player");
            spawnQueueInterval = config.Bind("Enhanced Chat Spawner", "SpawnQueueInterval", 0.75f,
                "Interval between queued spawns in seconds");
        }

        private void SetupArmorSystem()
        {
            if (!enableArmorSystem.Value) return;
            
            try
            {
                string basePath = null;
#if BepInEx
                try { basePath = BepInEx.Paths.ConfigPath; } catch { }
#endif
                if (string.IsNullOrEmpty(basePath)) basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(basePath)) basePath = Application.dataPath;
                armorPresetConfigPath = Path.Combine(basePath, "H3TVR_ChatSosigArmor.ini");
            }
            catch (Exception ex)
            {
                armorPresetConfigPath = "H3TVR_ChatSosigArmor.ini";
                logger?.LogWarning("Preset path resolve failed: " + ex.Message);
            }
            
            LoadArmorPresets();
            PrimeNameCaches();
            StartCoroutine(LoadArmorAssetsCoroutine());
        }

        public void Update()
        {
            HandleInput();
            UpdateSosigBehavior();
            
            if (enableArmorSystem.Value)
            {
                MonitorPerformance();
            }
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(keyToSpawn.Value))
            {
                SpawnFriendlySosig();
            }

            if (Input.GetKeyDown(keyToSpawnEnemySosig.Value))
            {
                SpawnEnemySosig();
            }

            if (enableArmorSystem.Value && Input.GetKeyDown(armorGUIKey.Value))
            {
                showArmorGUI = !showArmorGUI;
                Cursor.lockState = showArmorGUI ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = showArmorGUI;
            }
        }

        private void SpawnFriendlySosig()
        {
            if (spawnedChatters.Count >= maxActiveSosigs.Value) return;
            
            string name = ReadNameFromFile(filePathToTextFolder.Value);
            if (string.IsNullOrEmpty(name)) name = GetRandomAllyName();
            
            SpawnSosig(name, true, false);
        }

        private void SpawnEnemySosig()
        {
            if (spawnedEnemyChatters.Count >= maxActiveSosigs.Value) return;
            
            if (TNHManager == null && GM.TNH_Manager != null)
                TNHManager = GM.TNH_Manager;
            
            string name = ReadNameFromFile(filePathToTextFolderforEnemySosig.Value);
            if (string.IsNullOrEmpty(name)) name = GetRandomEnemyName();
            
            SpawnSosig(name, false, false);
        }

        private void SpawnSosig(string name, bool isFriendly, bool isFromQueue)
        {
            try
            {
                Vector3 spawnPoint;
                int iff = 0;
                
                if (isFriendly)
                {
                    spawnPoint = new Vector3(GM.CurrentPlayerBody.Head.transform.position.x, 
                                           GM.CurrentPlayerBody.transform.position.y, 
                                           GM.CurrentPlayerBody.Head.transform.position.z + 1);
                }
                else
                {
                    spawnPoint = CalculateEnemySpawnPoint(out iff);
                }
                
                // Use built-in spawning instead of external asset bundle
                Sosig sosig = SpawnBuiltInSosig(name, spawnPoint, isFriendly, iff);
                
                if (sosig != null)
                {
                    totalSpawnedCount++;
                    OnSosigSpawned?.Invoke(sosig, isFriendly, name);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to spawn sosig: {ex.Message}");
            }
        }

        private Sosig SpawnBuiltInSosig(string name, Vector3 spawnPoint, bool isFriendly, int iff)
        {
            try
            {
                if (GM.CurrentPlayerBody == null || GM.CurrentPlayerBody.Head == null) return null;
                
                // Cache sosig templates if not already done
                if (cachedSosigTemplates == null) 
                    cachedSosigTemplates = Resources.FindObjectsOfTypeAll<SosigEnemyTemplate>();
                
                if (cachedSosigTemplates.Length == 0) return null;
                
                // Select random template
                var template = cachedSosigTemplates[UnityEngine.Random.Range(0, cachedSosigTemplates.Length)];
                if (template == null || template.SosigPrefabs == null || template.SosigPrefabs.Count == 0) return null;
                
                // Spawn sosig
                var prefab = template.SosigPrefabs[UnityEngine.Random.Range(0, template.SosigPrefabs.Count)];
                if (prefab == null) return null;
                
                var sosigObj = Instantiate(prefab.GetGameObject(), spawnPoint, Quaternion.identity);
                var sosig = sosigObj.GetComponentInChildren<Sosig>();
                if (sosig == null) 
                {
                    Destroy(sosigObj);
                    return null;
                }
                
                // Configure sosig
                if (template.ConfigTemplates != null && template.ConfigTemplates.Count > 0)
                    sosig.Configure(template.ConfigTemplates[UnityEngine.Random.Range(0, template.ConfigTemplates.Count)]);
                
                sosig.E.IFFCode = iff;
                sosig.Priority.IFFChart[iff] = true;
                
                // Initialize equipment
                sosig.Inventory.Init();
                sosig.InitHands();
                
                // Equip weapons
                EquipWeaponsFromTemplate(sosig, template, spawnPoint);
                
                // Fill ammo
                sosig.Inventory.FillAllAmmo();
                
                // Apply outfit
                if (template.OutfitConfig != null && template.OutfitConfig.Count > 0)
                {
                    var outfit = template.OutfitConfig[UnityEngine.Random.Range(0, template.OutfitConfig.Count)];
                    ApplyOutfit(sosig, outfit);
                }
                
                // Apply armor preset if available
                if (enableArmorSystem.Value && armorPresets.Count > 0)
                {
                    string armorPreset = SelectArmorPreset(isFriendly);
                    if (!string.IsNullOrEmpty(armorPreset))
                        ApplyArmorToSosig(sosig, armorPreset);
                }
                
                // Setup behavior
                if (isFriendly)
                {
                    SetupFriendlyBehavior(sosig);
                    spawnedChatters.Add(sosig);
                }
                else
                {
                    SetupEnemyBehavior(sosig);
                    spawnedEnemyChatters.Add(sosig);
                }
                
                // Create nameplate
                if (enableNameplates.Value)
                    CreateNameplate(sosig, name, !isFriendly);
                
                return sosig;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to spawn built-in sosig: {ex.Message}");
                return null;
            }
        }

        private void EquipWeaponsFromTemplate(Sosig sosig, SosigEnemyTemplate template, Vector3 spawnPoint)
        {
            try
            {
                // Primary weapon
                if (template.WeaponOptions != null && template.WeaponOptions.Count > 0)
                {
                    var weaponPrefab = template.WeaponOptions[UnityEngine.Random.Range(0, template.WeaponOptions.Count)];
                    EquipWeapon(sosig, weaponPrefab.GetGameObject(), spawnPoint);
                }
                
                // Secondary weapon
                if (template.WeaponOptions_Secondary != null && template.WeaponOptions_Secondary.Count > 0 && UnityEngine.Random.value <= template.SecondaryChance)
                {
                    var weaponPrefab = template.WeaponOptions_Secondary[UnityEngine.Random.Range(0, template.WeaponOptions_Secondary.Count)];
                    EquipWeapon(sosig, weaponPrefab.GetGameObject(), spawnPoint);
                }
                
                // Tertiary weapon
                if (template.WeaponOptions_Tertiary != null && template.WeaponOptions_Tertiary.Count > 0 && UnityEngine.Random.value <= template.TertiaryChance)
                {
                    var weaponPrefab = template.WeaponOptions_Tertiary[UnityEngine.Random.Range(0, template.WeaponOptions_Tertiary.Count)];
                    EquipWeapon(sosig, weaponPrefab.GetGameObject(), spawnPoint);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to equip weapons: {ex.Message}");
            }
        }

        private void EquipWeapon(Sosig sosig, GameObject weaponPrefab, Vector3 spawnPoint)
        {
            if (weaponPrefab == null) return;
            
            try
            {
                var weapon = Instantiate(weaponPrefab, spawnPoint + Vector3.up * 0.1f, Quaternion.identity).GetComponent<SosigWeapon>();
                if (weapon == null) return;

                weapon.SetAutoDestroy(true);
                weapon.O.SpawnLockable = false;
                weapon.SetAmmoClamping(true);
                weapon.IsShakeReloadable = false;

                if (weapon.Type == SosigWeapon.SosigWeaponType.Gun)
                {
                    sosig.Inventory.FillAmmoWithType(weapon.AmmoType);
                }

                sosig.ForceEquip(weapon);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to equip specific weapon: {ex.Message}");
            }
        }

        private void ApplyOutfit(Sosig sosig, SosigOutfitConfig outfit)
        {
            if (sosig.Links.Count < 4 || outfit == null) return;
            
            try
            {
                if (UnityEngine.Random.Range(0f, 1f) < outfit.Chance_Headwear)
                    SpawnAccessoryToLink(outfit.Headwear, sosig.Links[0]);
                
                if (UnityEngine.Random.Range(0f, 1f) < outfit.Chance_Facewear)
                    SpawnAccessoryToLink(outfit.Facewear, sosig.Links[0]);
                
                if (UnityEngine.Random.Range(0f, 1f) < outfit.Chance_Eyewear)
                    SpawnAccessoryToLink(outfit.Eyewear, sosig.Links[0]);
                
                if (UnityEngine.Random.Range(0f, 1f) < outfit.Chance_Torsowear)
                    SpawnAccessoryToLink(outfit.Torsowear, sosig.Links[1]);
                
                if (UnityEngine.Random.Range(0f, 1f) < outfit.Chance_Pantswear)
                    SpawnAccessoryToLink(outfit.Pantswear, sosig.Links[2]);
                
                if (sosig.Links.Count > 3 && UnityEngine.Random.Range(0f, 1f) < outfit.Chance_Pantswear_Lower)
                    SpawnAccessoryToLink(outfit.Pantswear_Lower, sosig.Links[3]);
                
                if (UnityEngine.Random.Range(0f, 1f) < outfit.Chance_Backpacks)
                    SpawnAccessoryToLink(outfit.Backpacks, sosig.Links[1]);
                
                if (UnityEngine.Random.Range(0f, 1f) < outfit.Chance_TorosDecoration)
                    SpawnAccessoryToLink(outfit.TorosDecoration, sosig.Links[1]);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to apply outfit: {ex.Message}");
            }
        }

        private void SpawnAccessoryToLink(List<FVRObject> accessories, SosigLink link)
        {
            if (accessories == null || accessories.Count == 0 || link == null) return;
            
            try
            {
                var accessory = accessories[UnityEngine.Random.Range(0, accessories.Count)];
                if (accessory == null) return;

                var accessoryObj = Instantiate(accessory.GetGameObject(), link.transform.position, link.transform.rotation);
                accessoryObj.transform.SetParent(link.transform);
                
                var wearable = accessoryObj.GetComponent<SosigWearable>();
                if (wearable != null)
                {
                    wearable.RegisterWearable(link);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to spawn accessory: {ex.Message}");
            }
        }

        private string SelectArmorPreset(bool isFriendly)
        {
            if (armorPresets.Count == 0) return null;
            
            if (!isFriendly)
            {
                // Prefer heavy armor for enemies
                string[] preferredEnemyArmor = { "Heavy Assault", "Tactical Elite", "Juggernaut", "Riot Control", "Heavy" };
                foreach (string preferred in preferredEnemyArmor)
                {
                    if (armorPresets.ContainsKey(preferred))
                        return preferred;
                }
            }
            else
            {
                // Prefer lighter armor for allies
                string[] preferredAllyArmor = { "Standard", "Light", "Stealth Ops", "Civilian" };
                foreach (string preferred in preferredAllyArmor)
                {
                    if (armorPresets.ContainsKey(preferred))
                        return preferred;
                }
            }
            
            // Fallback to random preset
            var presetKeys = armorPresets.Keys.ToArray();
            return presetKeys[UnityEngine.Random.Range(0, presetKeys.Length)];
        }

        private void SetupFriendlyBehavior(Sosig sosig)
        {
            float offsetX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
            float offsetZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
            Vector3 followPoint = new Vector3(
                GM.CurrentPlayerBody.Head.position.x + offsetX,
                GM.CurrentPlayerBody.Head.position.y,
                GM.CurrentPlayerBody.Head.position.z + offsetZ
            );
            
            sosig.CommandAssaultPoint(followPoint);
            sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
        }

        private void SetupEnemyBehavior(Sosig sosig)
        {
            sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
            sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
        }

        private void CreateNameplate(Sosig sosig, string name, bool isEnemy)
        {
            if (sosig == null || sosig.Links == null || sosig.Links.Count < 2) return;
            
            try
            {
                // Create nameplate canvas
                var nameplateObj = new GameObject("Nameplate");
                nameplateObj.transform.SetParent(sosig.Links[0].transform); // Attach to head
                nameplateObj.transform.localPosition = Vector3.up * 0.6f;
                
                var canvas = nameplateObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 100;
                
                var rectTransform = canvas.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(2.5f, 0.6f);
                
                // Create text
                var textObj = new GameObject("Text");
                textObj.transform.SetParent(nameplateObj.transform);
                
                var text = textObj.AddComponent<Text>();
                text.text = name;
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 24;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = isEnemy ? Color.red : Color.green;
                
                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                
                // Add look-at-camera behavior
                nameplateObj.AddComponent<LookAtCamera>();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create nameplate: {ex.Message}");
            }
        }

        private Vector3 CalculateEnemySpawnPoint(out int iff)
        {
            iff = 1;
            
            if (TNHManager != null)
            {
                if (TNHManager.Phase == TNH_Phase.Hold)
                {
                    var attackVectors = TNHManager.m_curHoldPoint.AttackVectors;
                    if (attackVectors.Count > 0)
                    {
                        var vector = attackVectors[UnityEngine.Random.Range(0, attackVectors.Count)];
                        if (vector.SpawnPoints_Sosigs_Attack.Count > 0)
                        {
                            iff = TNHManager.m_curHoldPoint.m_curPhase.IFFUsed;
                            return vector.SpawnPoints_Sosigs_Attack[UnityEngine.Random.Range(0, vector.SpawnPoints_Sosigs_Attack.Count)].position;
                        }
                    }
                }
                else if (TNHManager.Phase == TNH_Phase.Take)
                {
                    var turretPoints = TNHManager.m_curHoldPoint.SpawnPoints_Turrets;
                    if (turretPoints.Count > 0)
                    {
                        if (TNHManager.m_curLevel.PatrolChallenge.Patrols.Count > 0)
                            iff = TNHManager.m_curLevel.PatrolChallenge.Patrols[0].IFFUsed;
                        else
                            iff = TNHManager.m_curHoldPoint.m_curPhase.IFFUsed;
                        
                        return turretPoints[0].transform.position;
                    }
                }
            }
            
            // Fallback spawn point
            return new Vector3(GM.CurrentPlayerBody.Head.transform.position.x, 
                             GM.CurrentPlayerBody.transform.position.y, 
                             GM.CurrentPlayerBody.Head.transform.position.z + 3);
        }

        #region Sosig Behavior Updates

        private void UpdateSosigBehavior()
        {
            UpdateFriendlySosigs();
            UpdateEnemySosigs();
        }

        private void UpdateFriendlySosigs()
        {
            if (spawnedChatters.Count == 0) return;
            
            // Clean up null references
            for (int i = spawnedChatters.Count - 1; i >= 0; i--)
            {
                if (spawnedChatters[i] == null)
                {
                    spawnedChatters.RemoveAt(i);
                }
            }
            
            foreach (Sosig sosig in spawnedChatters)
            {
                if (sosig == null) continue;
                
                UpdateSosigFollowBehavior(sosig, true);
                UpdateSosigCombatBehavior(sosig, 0.65f);
                
                if (sosig.BodyState == Sosig.SosigBodyState.Dead)
                {
                    sosig.TickDownToClear(3);
                }
            }
        }

        private void UpdateEnemySosigs()
        {
            if (spawnedEnemyChatters.Count == 0) return;
            
            // Clean up null references
            for (int i = spawnedEnemyChatters.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemyChatters[i] == null)
                {
                    spawnedEnemyChatters.RemoveAt(i);
                }
            }
            
            foreach (Sosig sosig in spawnedEnemyChatters)
            {
                if (sosig == null) continue;
                
                UpdateSosigAggressiveBehavior(sosig);
                UpdateSosigCombatBehavior(sosig, 0.55f);
                
                if (sosig.BodyState == Sosig.SosigBodyState.Dead)
                {
                    sosig.TickDownToClear(3);
                }
            }
        }

        private void UpdateSosigFollowBehavior(Sosig sosig, bool isFriendly)
        {
            if (sosig.m_isStunned) return;
            
            float distance = Vector3.Distance(GM.CurrentPlayerBody.Head.position, sosig.m_assaultPoint);
            
            if (distance > followDistance.Value)
            {
                float offsetX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                float offsetZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                Vector3 followPoint = new Vector3(
                    GM.CurrentPlayerBody.Head.position.x + offsetX,
                    GM.CurrentPlayerBody.Head.position.y,
                    GM.CurrentPlayerBody.Head.position.z + offsetZ
                );
                
                bool pathClear = !Physics.Linecast(GM.CurrentPlayerBody.Head.position, followPoint, environmentMask);
                if (pathClear)
                {
                    sosig.CommandAssaultPoint(followPoint);
                }
            }
        }

        private void UpdateSosigAggressiveBehavior(Sosig sosig)
        {
            if (sosig.m_isStunned) return;
            
            float distance = Vector3.Distance(GM.CurrentPlayerBody.Head.position, sosig.Links[1].transform.position);
            
            if (distance > 20f)
            {
                sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
            }
            
            if (sosig.CurrentOrder == Sosig.SosigOrder.Disabled || 
                sosig.CurrentOrder == Sosig.SosigOrder.Idle || 
                sosig.CurrentOrder == Sosig.SosigOrder.GuardPoint)
            {
                sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
            }
        }

        private void UpdateSosigCombatBehavior(Sosig sosig, float recognitionThreshold)
        {
            if (sosig.Priority.HasFreshTarget() && 
                sosig.CurrentOrder == Sosig.SosigOrder.Investigate && 
                sosig.m_entityRecognition >= recognitionThreshold)
            {
                sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
            }
        }

        #endregion

        #region Armor System

        private void LoadArmorPresets()
        {
            if (!enableArmorSystem.Value) return;
            
            armorPresets.Clear();
            
            // Add some default presets
            AddDefaultArmorPresets();
            
            try
            {
                if (!File.Exists(armorPresetConfigPath)) return;
                
                ArmorConfiguration current = null;
                foreach (var raw in File.ReadAllLines(armorPresetConfigPath))
                {
                    if (raw == null || raw.Trim().Length == 0) continue;
                    var line = raw.Trim();
                    if (line.StartsWith("#") || line.StartsWith(";")) continue;
                    
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        var name = line.Substring(1, line.Length - 2).Trim();
                        if (name.Length == 0) continue;
                        current = new ArmorConfiguration { presetName = name };
                        armorPresets[name] = current;
                        continue;
                    }
                    
                    if (current == null) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    
                    var key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    var val = line.Substring(eq + 1).Trim();
                    float f;
                    
                    switch (key)
                    {
                        case "description": current.description = val; break;
                        case "armor_level": current.armorLevel = val; break;
                        case "headwear_chance": 
                            if (float.TryParse(val, out f)) 
                            { 
                                current.enableHeadwear = f > 0; 
                                current.headwearChance = Mathf.Clamp01(f); 
                            } 
                            break;
                        case "facewear_chance": 
                            if (float.TryParse(val, out f)) 
                            { 
                                current.enableFacewear = f > 0; 
                                current.facewearChance = Mathf.Clamp01(f); 
                            } 
                            break;
                        case "eyewear_chance": 
                            if (float.TryParse(val, out f)) 
                            { 
                                current.enableEyewear = f > 0; 
                                current.eyewearChance = Mathf.Clamp01(f); 
                            } 
                            break;
                        case "torsowear_chance": 
                            if (float.TryParse(val, out f)) 
                            { 
                                current.enableTorsowear = f > 0; 
                                current.torsowearChance = Mathf.Clamp01(f); 
                            } 
                            break;
                        case "pantswear_chance": 
                            if (float.TryParse(val, out f)) 
                            { 
                                current.enablePantswear = f > 0; 
                                current.pantswearChance = Mathf.Clamp01(f); 
                            } 
                            break;
                        case "backpack_chance": 
                            if (float.TryParse(val, out f)) 
                            { 
                                current.enableBackpacks = f > 0; 
                                current.backpackChance = Mathf.Clamp01(f); 
                            } 
                            break;
                    }
                }
                
                logger?.LogInfo($"Loaded {armorPresets.Count} armor preset(s).");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to load armor presets: {ex.Message}");
            }
        }

        private void AddDefaultArmorPresets()
        {
            // Add some basic presets
            armorPresets["Light"] = new ArmorConfiguration
            {
                presetName = "Light",
                description = "Light armor configuration",
                armorLevel = "Light",
                headwearChance = 0.5f,
                torsowearChance = 0.7f,
                pantswearChance = 0.6f
            };
            
            armorPresets["Heavy"] = new ArmorConfiguration
            {
                presetName = "Heavy",
                description = "Heavy armor configuration", 
                armorLevel = "Heavy",
                headwearChance = 0.9f,
                torsowearChance = 1.0f,
                pantswearChance = 0.9f,
                backpackChance = 0.8f
            };
            
            armorPresets["Stealth"] = new ArmorConfiguration
            {
                presetName = "Stealth",
                description = "Stealth gear configuration",
                armorLevel = "Light",
                headwearChance = 0.6f,
                facewearChance = 0.8f,
                eyewearChance = 0.9f,
                torsowearChance = 0.8f
            };
        }

        private IEnumerator LoadArmorAssetsCoroutine()
        {
            yield return new WaitForSeconds(1.5f);
            
            if (!H3VRAssetLoader.IsInitialized) 
            {
                H3VRAssetLoader.Initialize();
            }
            
            try 
            { 
                availableArmor = H3VRAssetLoader.GetAllArmorCategories(); 
            }
            catch 
            { 
                availableArmor = new Dictionary<string, List<FVRObject>>();
                var armorSlots = new[] { "Headwear", "Facewear", "Eyewear", "Torsowear", "Pantswear", "PantswearLower", "Backpacks", "Decorations" };
                foreach (var slot in armorSlots) 
                    availableArmor[slot] = new List<FVRObject>(); 
            }
        }

        public void ApplyArmorToSosig(Sosig sosig, string presetName)
        {
            if (!enableArmorSystem.Value || sosig == null || availableArmor == null) return;
            
            ArmorConfiguration preset;
            if (!armorPresets.TryGetValue(presetName, out preset)) return;
            
            ApplyArmorConfigurationToSosig(sosig, preset);
        }

        private void ApplyArmorConfigurationToSosig(Sosig sosig, ArmorConfiguration config)
        {
            if (sosig == null || sosig.Links == null || availableArmor == null || config == null) return;
            
            TryApplyArmorSlot(sosig, config.enableHeadwear, config.headwearChance, "Headwear");
            TryApplyArmorSlot(sosig, config.enableFacewear, config.facewearChance, "Facewear");
            TryApplyArmorSlot(sosig, config.enableEyewear, config.eyewearChance, "Eyewear");
            TryApplyArmorSlot(sosig, config.enableTorsowear, config.torsowearChance, "Torsowear");
            TryApplyArmorSlot(sosig, config.enablePantswear, config.pantswearChance, "Pantswear");
            TryApplyArmorSlot(sosig, config.enablePantswearLower, config.pantswearLowerChance, "PantswearLower");
            TryApplyArmorSlot(sosig, config.enableBackpacks, config.backpackChance, "Backpacks");
            TryApplyArmorSlot(sosig, config.enableDecorations, config.decorationChance, "Decorations");
        }

        private void TryApplyArmorSlot(Sosig sosig, bool enabled, float chance, string slot)
        {
            if (!enabled || availableArmor == null) return;
            if (UnityEngine.Random.value > chance) return;
            if (!availableArmor.ContainsKey(slot) || availableArmor[slot].Count == 0) return;
            
            var armorObject = availableArmor[slot][UnityEngine.Random.Range(0, availableArmor[slot].Count)];
            ApplySpecificArmorToSosig(sosig, slot, armorObject);
        }

        private void ApplySpecificArmorToSosig(Sosig sosig, string slot, FVRObject armorObject)
        {
            if (sosig == null || sosig.Links == null || armorObject == null) return;
            
            var link = GetLinkForArmorSlot(sosig, slot);
            if (link == null) return;
            
            try
            {
                var instance = Instantiate(armorObject.GetGameObject(), link.transform);
                var wearable = instance.GetComponent<SosigWearable>();
                if (wearable != null)
                {
                    wearable.RegisterWearable(link);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to apply armor: {ex.Message}");
            }
        }

        private SosigLink GetLinkForArmorSlot(Sosig sosig, string slot)
        {
            if (sosig == null || sosig.Links == null || sosig.Links.Count == 0) return null;
            
            switch (slot)
            {
                case "Headwear":
                case "Facewear":
                case "Eyewear":
                    return sosig.Links[0];
                case "Torsowear":
                case "Backpacks":
                case "Decorations":
                    return sosig.Links.Count > 1 ? sosig.Links[1] : null;
                case "Pantswear":
                case "PantswearLower":
                    return sosig.Links.Count > 2 ? sosig.Links[2] : null;
                default:
                    return sosig.Links[0];
            }
        }

        #endregion

        #region Queue System and Name Management

        private IEnumerator ProcessSpawnQueueCoroutine()
        {
            var wait = new WaitForSeconds(0.1f);
            
            while (true)
            {
                yield return wait;
                
                if (spawnQueue.Count == 0) continue;
                if (Time.time - lastSpawnTime < spawnQueueInterval.Value) continue;
                if (spawnedChatters.Count + spawnedEnemyChatters.Count >= maxActiveSosigs.Value) continue;
                
                var queuedSpawn = spawnQueue.Dequeue();
                lastSpawnTime = Time.time;
                
                SpawnSosig(queuedSpawn.Name, queuedSpawn.Friendly, true);
            }
        }

        public void QueueChatSpawn(string userName, bool isFriendly, string armorSet = null)
        {
            if (string.IsNullOrEmpty(userName)) 
                userName = isFriendly ? "Ally" : "Enemy";
            
            if (spawnQueue.Count >= 50) // Prevent queue overflow
                spawnQueue.Dequeue();
            
            spawnQueue.Enqueue(new QueuedChatSpawn 
            { 
                Name = userName, 
                Friendly = isFriendly, 
                ArmorSetName = armorSet,
                IsFromTwitchQueue = true
            });
        }

        private void PrimeNameCaches()
        {
            cachedAllyNames = LoadNamesFromFile(filePathToTextFolder.Value, out allyNamesLastWrite);
            cachedEnemyNames = LoadNamesFromFile(filePathToTextFolderforEnemySosig.Value, out enemyNamesLastWrite);
        }

        private List<string> LoadNamesFromFile(string filePath, out DateTime lastWrite)
        {
            lastWrite = DateTime.MinValue;
            var names = new List<string>();
            
            // Create default files if they don't exist
            CreateLioranBoard20FilesIfNeeded();
            
            try
            {
                if (!File.Exists(filePath)) return names;
                
                var fileInfo = new FileInfo(filePath);
                lastWrite = fileInfo.LastWriteTimeUtc;
                
                foreach (var line in File.ReadAllLines(filePath))
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("#") || trimmed.StartsWith(";")) continue;
                    
                    if (trimmed.Contains("="))
                    {
                        var parts = trimmed.Split('=');
                        if (parts.Length >= 2)
                            names.Add(parts[1].Trim().Trim('"'));
                    }
                    else
                    {
                        names.Add(trimmed);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to load names from {filePath}: {ex.Message}");
            }
            
            return names;
        }

        private void CreateLioranBoard20FilesIfNeeded()
        {
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string dir = Path.Combine(docs, "LioranBoard 2.0");
                
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                if (!File.Exists(filePathToTextFolder.Value))
                    File.WriteAllLines(filePathToTextFolder.Value, new[] { "GoodViewer", "Helper", "StreamFriend" });
                
                if (!File.Exists(filePathToTextFolderforEnemySosig.Value))
                    File.WriteAllLines(filePathToTextFolderforEnemySosig.Value, new[] { "Troll", "Chaos", "StreamEnemy" });
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to create default files: {ex.Message}");
            }
        }

        private string ReadNameFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return string.Empty;
                
                string content = File.ReadAllText(filePath);
                int startIndex = content.IndexOf('"');
                
                if (startIndex >= 0)
                {
                    int endIndex = content.LastIndexOf('"');
                    if (endIndex > startIndex)
                    {
                        return content.Substring(startIndex + 1, endIndex - startIndex - 1);
                    }
                }
                
                return content.Trim();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to read name from {filePath}: {ex.Message}");
                return string.Empty;
            }
        }

        private string GetRandomAllyName()
        {
            RefreshNameCacheIfChanged();
            if (cachedAllyNames != null && cachedAllyNames.Count > 0)
                return cachedAllyNames[UnityEngine.Random.Range(0, cachedAllyNames.Count)];
            return "Ally";
        }

        private string GetRandomEnemyName()
        {
            RefreshNameCacheIfChanged();
            if (cachedEnemyNames != null && cachedEnemyNames.Count > 0)
                return cachedEnemyNames[UnityEngine.Random.Range(0, cachedEnemyNames.Count)];
            return "Enemy";
        }
        
        private void RefreshNameCacheIfChanged()
        {
            try
            {
                if (!string.IsNullOrEmpty(filePathToTextFolder.Value) && File.Exists(filePathToTextFolder.Value))
                {
                    var writeTime = File.GetLastWriteTimeUtc(filePathToTextFolder.Value);
                    if (writeTime != allyNamesLastWrite)
                    {
                        cachedAllyNames = LoadNamesFromFile(filePathToTextFolder.Value, out allyNamesLastWrite);
                    }
                }
                
                if (!string.IsNullOrEmpty(filePathToTextFolderforEnemySosig.Value) && File.Exists(filePathToTextFolderforEnemySosig.Value))
                {
                    var writeTime = File.GetLastWriteTimeUtc(filePathToTextFolderforEnemySosig.Value);
                    if (writeTime != enemyNamesLastWrite)
                    {
                        cachedEnemyNames = LoadNamesFromFile(filePathToTextFolderforEnemySosig.Value, out enemyNamesLastWrite);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to refresh name cache: {ex.Message}");
            }
        }

        #endregion

        #region Performance and Management

        private void MonitorPerformance()
        {
            if (Time.time - lastPerformanceCheck > 5f)
            {
                lastPerformanceCheck = Time.time;
                
                int totalSosigs = spawnedChatters.Count + spawnedEnemyChatters.Count;
                if (totalSosigs > maxActiveSosigs.Value * 0.8f)
                {
                    logger?.LogWarning($"High sosig count: {totalSosigs}. Consider reducing spawn rate.");
                }
            }
        }

        private IEnumerator UpdateSosigsCoroutine()
        {
            var wait = new WaitForSeconds(2f);
            
            while (true)
            {
                yield return wait;
                
                // Clean up dead sosigs
                spawnedChatters.RemoveAll(s => s == null || s.BodyState == Sosig.SosigBodyState.Dead);
                spawnedEnemyChatters.RemoveAll(s => s == null || s.BodyState == Sosig.SosigBodyState.Dead);
                
                // Enforce limits
                while (spawnedChatters.Count > maxActiveSosigs.Value / 2)
                {
                    var oldest = spawnedChatters.FirstOrDefault();
                    if (oldest != null)
                    {
                        Destroy(oldest.gameObject);
                        spawnedChatters.RemoveAt(0);
                    }
                }
                
                while (spawnedEnemyChatters.Count > maxActiveSosigs.Value / 2)
                {
                    var oldest = spawnedEnemyChatters.FirstOrDefault();
                    if (oldest != null)
                    {
                        Destroy(oldest.gameObject);
                        spawnedEnemyChatters.RemoveAt(0);
                    }
                }
            }
        }

        #endregion

        #region GUI

        void OnGUI()
        {
            if (!enableArmorSystem.Value || !showArmorGUI) return;
            
            InitStyles();
            armorWindowRect = GUILayout.Window(223355, armorWindowRect, DrawArmorGUI, "Enhanced Chat Spawner", windowStyle);
        }

        private void InitStyles()
        {
            if (windowStyle != null) return;
            
            windowStyle = new GUIStyle(GUI.skin.window) { fontSize = 14, padding = new RectOffset(8, 8, 22, 8) };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11 };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 11 };
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            sectionStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(6, 6, 6, 6) };
            infoStyle = new GUIStyle(labelStyle) { fontSize = 10, normal = { textColor = new Color(.7f, .7f, .7f) } };
        }

        private void DrawArmorGUI(int id)
        {
            GUILayout.BeginVertical();
            
            // Header
            GUILayout.BeginHorizontal();
            GUILayout.Label("Enhanced Chat Spawner", headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Allies:{spawnedChatters.Count} Enemies:{spawnedEnemyChatters.Count} Q:{spawnQueue.Count}", infoStyle);
            if (GUILayout.Button("X", GUILayout.Width(22)))
            {
                showArmorGUI = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(4);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(450));
            
            // Stats section
            DrawSection("Statistics", () =>
            {
                GUILayout.Label($"Total Spawned: {totalSpawnedCount}", labelStyle);
                GUILayout.Label($"Active Allies: {spawnedChatters.Count}", labelStyle);
                GUILayout.Label($"Active Enemies: {spawnedEnemyChatters.Count}", labelStyle);
                GUILayout.Label($"Queue Length: {spawnQueue.Count}", labelStyle);
            });
            
            // Controls section
            DrawSection("Controls", () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear All Allies", buttonStyle))
                {
                    ClearAllSosigs(true);
                }
                if (GUILayout.Button("Clear All Enemies", buttonStyle))
                {
                    ClearAllSosigs(false);
                }
                GUILayout.EndHorizontal();
                
                if (GUILayout.Button("Clear Queue", buttonStyle))
                {
                    spawnQueue.Clear();
                }
                
                if (GUILayout.Button("Reload Armor Presets", buttonStyle))
                {
                    LoadArmorPresets();
                }
            });
            
            // Armor presets section
            DrawSection("Armor Presets", () =>
            {
                if (armorPresets.Count == 0)
                {
                    GUILayout.Label("No presets loaded", infoStyle);
                }
                else
                {
                    foreach (var preset in armorPresets.Values)
                    {
                        GUILayout.BeginHorizontal(GUI.skin.box);
                        GUILayout.BeginVertical();
                        GUILayout.Label(preset.presetName + 
                            (string.IsNullOrEmpty(preset.armorLevel) ? "" : $" ({preset.armorLevel})"), headerStyle);
                        if (!string.IsNullOrEmpty(preset.description))
                            GUILayout.Label(preset.description, infoStyle);
                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Apply to All", buttonStyle, GUILayout.Width(80)))
                        {
                            ApplyArmorToAllSosigs(preset.presetName);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            });
            
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            
            GUI.DragWindow(new Rect(0, 0, 10000, 18));
        }

        private void DrawSection(string title, System.Action drawContent)
        {
            GUILayout.BeginVertical(sectionStyle);
            GUILayout.Label(title, headerStyle);
            drawContent?.Invoke();
            GUILayout.EndVertical();
        }

        #endregion

        #region Public API

        public void ClearAllSosigs(bool allies)
        {
            if (allies)
            {
                foreach (var sosig in spawnedChatters)
                {
                    if (sosig != null) Destroy(sosig.gameObject);
                }
                spawnedChatters.Clear();
            }
            else
            {
                foreach (var sosig in spawnedEnemyChatters)
                {
                    if (sosig != null) Destroy(sosig.gameObject);
                }
                spawnedEnemyChatters.Clear();
            }
        }

        public void ApplyArmorToAllSosigs(string presetName)
        {
            foreach (var sosig in spawnedChatters)
            {
                if (sosig != null) ApplyArmorToSosig(sosig, presetName);
            }
            
            foreach (var sosig in spawnedEnemyChatters)
            {
                if (sosig != null) ApplyArmorToSosig(sosig, presetName);
            }
        }

        public List<string> GetAvailableArmorSets()
        {
            return new List<string>(armorPresets.Keys);
        }

        public int GetActiveSosigCount()
        {
            return spawnedChatters.Count + spawnedEnemyChatters.Count;
        }

        #endregion

        public class LookAtCamera : MonoBehaviour
        {
            private Camera cam;
            private float updateInterval = 0.1f;
            private float lastUpdate;

            void Start()
            {
                cam = Camera.main ?? FindObjectOfType<Camera>();
            }

            void Update()
            {
                if (cam != null && Time.time - lastUpdate > updateInterval)
                {
                    transform.LookAt(cam.transform);
                    transform.Rotate(0, 180, 0);
                    lastUpdate = Time.time;
                }
            }
        }
    }
}