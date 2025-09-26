using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using FistVR;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Manages Twitch chat-integrated sosig spawning with configurable armor and weapons
    /// Integrates with the H3TVR SpawnManager system for enhanced functionality
    /// </summary>
    public class TwitchChatSosigManager : MonoBehaviour
    {
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        private SpawnManager spawnManager;

        [Header("Chat Integration")]
        public string currentChatUserName = "";
        public Queue<ChatSpawnRequest> chatSpawnQueue = new Queue<ChatSpawnRequest>();
        public List<ChatSosig> activeChatSosigs = new List<ChatSosig>();

        [Header("Spawn Configuration")]
        public List<SosigEnemyTemplate> friendlyTemplates = new List<SosigEnemyTemplate>();
        public List<SosigEnemyTemplate> enemyTemplates = new List<SosigEnemyTemplate>();
        public List<ArmorSet> availableArmorSets = new List<ArmorSet>();
        public GameObject nameplatePrefab;
        public GameObject enemyNameplatePrefab;

        // Configuration entries
        private ConfigEntry<string> chatFilePath;
        private ConfigEntry<string> enemyChatFilePath;
        private ConfigEntry<KeyCode> spawnFriendlyKey;
        private ConfigEntry<KeyCode> spawnEnemyKey;
        private ConfigEntry<KeyCode> toggleArmorKey;
        private ConfigEntry<KeyCode> clearAllSosigsKey;
        private ConfigEntry<bool> enableChatIntegration;
        private ConfigEntry<bool> enableArmorCustomization;
        private ConfigEntry<bool> enableWeaponRandomization;
        private ConfigEntry<int> maxActiveSosigs;
        private ConfigEntry<float> sosigFollowDistance;
        private ConfigEntry<float> sosigUpdateInterval;
        private ConfigEntry<bool> enableNameplates;
        private ConfigEntry<bool> autoCleanupDeadSosigs;
        private ConfigEntry<float> deadSosigCleanupTime;

        // Armor configuration
        private ConfigEntry<bool> enableRandomArmor;
        private ConfigEntry<float> armorSpawnChance;
        private ConfigEntry<bool> enableArmorUpgrades;
        private ConfigEntry<string> defaultArmorSet;

        void Start()
        {
            InitializeConfiguration();
            InitializeArmorSets();
            StartCoroutine(UpdateSosigsCoroutine());
        }

        public void Initialize(H3TVRImproved pluginInstance, SpawnManager spawnerInstance, ManualLogSource logSource)
        {
            plugin = pluginInstance;
            spawnManager = spawnerInstance;
            logger = logSource;
            
            logger.LogInfo("TwitchChatSosigManager initialized successfully!");
        }

        private void InitializeConfiguration()
        {
            var config = plugin.Config;

            // File paths
            chatFilePath = config.Bind("Twitch Chat Sosigs", "FriendlyChatFilePath", 
                Path.Combine(BepInEx.Paths.GameRootPath, "chat_spawner.txt"),
                "Path to the file containing chat user names for friendly sosigs");
                
            enemyChatFilePath = config.Bind("Twitch Chat Sosigs", "EnemyChatFilePath", 
                Path.Combine(BepInEx.Paths.GameRootPath, "enemy_chat_spawner.txt"),
                "Path to the file containing chat user names for enemy sosigs");

            // Key bindings
            spawnFriendlyKey = config.Bind("Twitch Chat Sosigs", "SpawnFriendlyKey", KeyCode.P,
                "Key to spawn a friendly chat sosig");
            spawnEnemyKey = config.Bind("Twitch Chat Sosigs", "SpawnEnemyKey", KeyCode.Keypad7,
                "Key to spawn an enemy chat sosig");
            toggleArmorKey = config.Bind("Twitch Chat Sosigs", "ToggleArmorKey", KeyCode.L,
                "Key to cycle through armor sets for the next spawned sosig");
            clearAllSosigsKey = config.Bind("Twitch Chat Sosigs", "ClearAllSosigsKey", KeyCode.Delete,
                "Key to clear all spawned chat sosigs");

            // General settings
            enableChatIntegration = config.Bind("Twitch Chat Sosigs", "EnableChatIntegration", true,
                "Enable Twitch chat integration for sosig spawning");
            enableArmorCustomization = config.Bind("Twitch Chat Sosigs", "EnableArmorCustomization", true,
                "Enable armor customization for spawned sosigs");
            enableWeaponRandomization = config.Bind("Twitch Chat Sosigs", "EnableWeaponRandomization", true,
                "Enable weapon randomization for spawned sosigs");
            maxActiveSosigs = config.Bind("Twitch Chat Sosigs", "MaxActiveSosigs", 10,
                "Maximum number of active chat sosigs at once");
            sosigFollowDistance = config.Bind("Twitch Chat Sosigs", "SosigFollowDistance", 6f,
                "Distance at which sosigs will follow the player");
            sosigUpdateInterval = config.Bind("Twitch Chat Sosigs", "SosigUpdateInterval", 0.5f,
                "Update interval for sosig AI and positioning (seconds)");
            enableNameplates = config.Bind("Twitch Chat Sosigs", "EnableNameplates", true,
                "Show nameplates above spawned chat sosigs");
            autoCleanupDeadSosigs = config.Bind("Twitch Chat Sosigs", "AutoCleanupDeadSosigs", true,
                "Automatically clean up dead sosigs after a delay");
            deadSosigCleanupTime = config.Bind("Twitch Chat Sosigs", "DeadSosigCleanupTime", 30f,
                "Time in seconds before dead sosigs are cleaned up");

            // Armor settings
            enableRandomArmor = config.Bind("Twitch Chat Sosigs - Armor", "EnableRandomArmor", true,
                "Randomly apply armor to spawned sosigs");
            armorSpawnChance = config.Bind("Twitch Chat Sosigs - Armor", "ArmorSpawnChance", 0.7f,
                "Chance (0-1) that a spawned sosig will have armor");
            enableArmorUpgrades = config.Bind("Twitch Chat Sosigs - Armor", "EnableArmorUpgrades", false,
                "Allow armor upgrades over time (experimental)");
            defaultArmorSet = config.Bind("Twitch Chat Sosigs - Armor", "DefaultArmorSet", "Standard",
                "Default armor set to use when no specific set is selected");
        }

        private void InitializeArmorSets()
        {
            // Define various armor configurations
            availableArmorSets.Clear();

            availableArmorSets.Add(new ArmorSet
            {
                name = "Standard",
                description = "Basic military gear",
                headwearChance = 0.8f,
                facewearChance = 0.3f,
                eyewearChance = 0.5f,
                torsowearChance = 0.9f,
                pantswearChance = 0.9f,
                backpackChance = 0.4f,
                armorLevel = ArmorLevel.Light
            });

            availableArmorSets.Add(new ArmorSet
            {
                name = "Heavy Assault",
                description = "Heavy combat armor",
                headwearChance = 1.0f,
                facewearChance = 0.8f,
                eyewearChance = 0.7f,
                torsowearChance = 1.0f,
                pantswearChance = 1.0f,
                backpackChance = 0.8f,
                armorLevel = ArmorLevel.Heavy
            });

            availableArmorSets.Add(new ArmorSet
            {
                name = "Stealth Ops",
                description = "Lightweight stealth gear",
                headwearChance = 0.6f,
                facewearChance = 0.9f,
                eyewearChance = 0.9f,
                torsowearChance = 0.8f,
                pantswearChance = 0.8f,
                backpackChance = 0.2f,
                armorLevel = ArmorLevel.Light
            });

            availableArmorSets.Add(new ArmorSet
            {
                name = "Riot Control",
                description = "Riot control equipment",
                headwearChance = 1.0f,
                facewearChance = 1.0f,
                eyewearChance = 0.3f,
                torsowearChance = 1.0f,
                pantswearChance = 1.0f,
                backpackChance = 0.1f,
                armorLevel = ArmorLevel.Heavy
            });

            availableArmorSets.Add(new ArmorSet
            {
                name = "Civilian",
                description = "Civilian clothing",
                headwearChance = 0.3f,
                facewearChance = 0.1f,
                eyewearChance = 0.4f,
                torsowearChance = 0.7f,
                pantswearChance = 0.8f,
                backpackChance = 0.2f,
                armorLevel = ArmorLevel.None
            });

            availableArmorSets.Add(new ArmorSet
            {
                name = "Tactical Elite",
                description = "Elite tactical equipment",
                headwearChance = 1.0f,
                facewearChance = 0.9f,
                eyewearChance = 0.8f,
                torsowearChance = 1.0f,
                pantswearChance = 1.0f,
                backpackChance = 0.9f,
                armorLevel = ArmorLevel.Elite
            });
        }

        void Update()
        {
            HandleInput();
            ProcessChatSpawnQueue();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(spawnFriendlyKey.Value))
            {
                SpawnFriendlyChatSosig();
            }

            if (Input.GetKeyDown(spawnEnemyKey.Value))
            {
                SpawnEnemyChatSosig();
            }

            if (Input.GetKeyDown(toggleArmorKey.Value))
            {
                CycleArmorSet();
            }

            if (Input.GetKeyDown(clearAllSosigsKey.Value))
            {
                ClearAllChatSosigs();
            }
        }

        private string selectedArmorSet = "Standard";
        private int currentArmorSetIndex = 0;

        private void CycleArmorSet()
        {
            currentArmorSetIndex = (currentArmorSetIndex + 1) % availableArmorSets.Count;
            selectedArmorSet = availableArmorSets[currentArmorSetIndex].name;
            logger.LogInfo($"Selected armor set: {selectedArmorSet}");
        }

        public void SpawnFriendlyChatSosig()
        {
            try
            {
                if (activeChatSosigs.Count >= maxActiveSosigs.Value)
                {
                    logger.LogWarning("Maximum active sosigs reached. Cannot spawn more.");
                    return;
                }

                string chatUserName = ReadChatUserName(chatFilePath.Value);
                if (string.IsNullOrEmpty(chatUserName))
                {
                    logger.LogWarning("No chat user name found for friendly sosig");
                    return;
                }

                Vector3 spawnPosition = CalculateSpawnPosition();
                ArmorSet armorSet = GetArmorSetByName(selectedArmorSet);

                var chatSosig = new ChatSosig
                {
                    userName = chatUserName,
                    isFriendly = true,
                    spawnTime = Time.time,
                    armorSet = armorSet
                };

                SpawnChatSosigInternal(chatSosig, spawnPosition, friendlyTemplates, 0);
                logger.LogInfo($"Spawned friendly chat sosig for user: {chatUserName} with {selectedArmorSet} armor");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to spawn friendly chat sosig: {ex.Message}");
            }
        }

        public void SpawnEnemyChatSosig()
        {
            try
            {
                if (activeChatSosigs.Count >= maxActiveSosigs.Value)
                {
                    logger.LogWarning("Maximum active sosigs reached. Cannot spawn more.");
                    return;
                }

                string chatUserName = ReadChatUserName(enemyChatFilePath.Value);
                if (string.IsNullOrEmpty(chatUserName))
                {
                    logger.LogWarning("No chat user name found for enemy sosig");
                    return;
                }

                Vector3 spawnPosition = CalculateEnemySpawnPosition();
                ArmorSet armorSet = GetArmorSetByName(selectedArmorSet);

                var chatSosig = new ChatSosig
                {
                    userName = chatUserName,
                    isFriendly = false,
                    spawnTime = Time.time,
                    armorSet = armorSet
                };

                SpawnChatSosigInternal(chatSosig, spawnPosition, enemyTemplates, GetEnemyIFF());
                logger.LogInfo($"Spawned enemy chat sosig for user: {chatUserName} with {selectedArmorSet} armor");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to spawn enemy chat sosig: {ex.Message}");
            }
        }

        private void SpawnChatSosigInternal(ChatSosig chatSosig, Vector3 position, List<SosigEnemyTemplate> templates, int iff)
        {
            if (templates.Count == 0)
            {
                logger.LogError("No sosig templates available for spawning");
                return;
            }

            SosigEnemyTemplate template = templates[UnityEngine.Random.Range(0, templates.Count)];
            
            // Spawn the sosig
            GameObject sosigPrefab = template.SosigPrefabs[UnityEngine.Random.Range(0, template.SosigPrefabs.Count)].GetGameObject();
            GameObject sosigGO = Instantiate(sosigPrefab, position, Quaternion.identity);
            Sosig sosig = sosigGO.GetComponentInChildren<Sosig>();

            if (sosig == null)
            {
                logger.LogError("Failed to get Sosig component from spawned object");
                Destroy(sosigGO);
                return;
            }

            // Configure sosig
            SosigConfigTemplate config = template.ConfigTemplates[UnityEngine.Random.Range(0, template.ConfigTemplates.Count)];
            sosig.Configure(config);
            sosig.E.IFFCode = iff;

            // Apply armor and outfit
            SosigOutfitConfig outfitConfig = template.OutfitConfig[UnityEngine.Random.Range(0, template.OutfitConfig.Count)];
            ApplyArmorToSosig(sosig, chatSosig.armorSet, outfitConfig);

            // Equip weapons
            EquipSosigWeapons(sosig, template, position);

            // Set up AI behavior
            ConfigureSosigBehavior(sosig, chatSosig.isFriendly);

            // Create nameplate
            if (enableNameplates.Value)
            {
                CreateNameplate(sosig, chatSosig);
            }

            // Store reference
            chatSosig.sosigInstance = sosig;
            activeChatSosigs.Add(chatSosig);
        }

        private void ApplyArmorToSosig(Sosig sosig, ArmorSet armorSet, SosigOutfitConfig outfitConfig)
        {
            if (!enableArmorCustomization.Value) return;

            bool shouldApplyArmor = enableRandomArmor.Value && UnityEngine.Random.value < armorSpawnChance.Value;
            if (!shouldApplyArmor) return;

            // Apply headwear
            if (UnityEngine.Random.value < armorSet.headwearChance && outfitConfig.Headwear.Count > 0)
            {
                SpawnArmorPiece(outfitConfig.Headwear, sosig.Links[0]);
            }

            // Apply facewear
            if (UnityEngine.Random.value < armorSet.facewearChance && outfitConfig.Facewear.Count > 0)
            {
                SpawnArmorPiece(outfitConfig.Facewear, sosig.Links[0]);
            }

            // Apply eyewear
            if (UnityEngine.Random.value < armorSet.eyewearChance && outfitConfig.Eyewear.Count > 0)
            {
                SpawnArmorPiece(outfitConfig.Eyewear, sosig.Links[0]);
            }

            // Apply torsowear
            if (UnityEngine.Random.value < armorSet.torsowearChance && outfitConfig.Torsowear.Count > 0)
            {
                SpawnArmorPiece(outfitConfig.Torsowear, sosig.Links[1]);
            }

            // Apply pantswear
            if (UnityEngine.Random.value < armorSet.pantswearChance && outfitConfig.Pantswear.Count > 0)
            {
                SpawnArmorPiece(outfitConfig.Pantswear, sosig.Links[2]);
            }

            // Apply backpack
            if (UnityEngine.Random.value < armorSet.backpackChance && outfitConfig.Backpacks.Count > 0)
            {
                SpawnArmorPiece(outfitConfig.Backpacks, sosig.Links[1]);
            }

            // Apply torso decoration
            if (outfitConfig.TorosDecoration.Count > 0)
            {
                SpawnArmorPiece(outfitConfig.TorosDecoration, sosig.Links[1]);
            }

            // Apply lower pants wear
            if (outfitConfig.Pantswear_Lower.Count > 0)
            {
                SpawnArmorPiece(outfitConfig.Pantswear_Lower, sosig.Links[3]);
            }
        }

        private void SpawnArmorPiece(List<FVRObject> armorPieces, SosigLink link)
        {
            if (armorPieces.Count == 0) return;

            FVRObject armorPiece = armorPieces[UnityEngine.Random.Range(0, armorPieces.Count)];
            GameObject armorGO = Instantiate(armorPiece.GetGameObject(), link.transform.position, link.transform.rotation);
            armorGO.transform.SetParent(link.transform);
            
            var wearable = armorGO.GetComponent<SosigWearable>();
            if (wearable != null)
            {
                wearable.RegisterWearable(link);
            }
        }

        private void EquipSosigWeapons(Sosig sosig, SosigEnemyTemplate template, Vector3 position)
        {
            // Primary weapon
            if (template.WeaponOptions.Count > 0)
            {
                GameObject weaponPrefab = template.WeaponOptions[UnityEngine.Random.Range(0, template.WeaponOptions.Count)].GetGameObject();
                EquipWeapon(sosig, weaponPrefab, position);
            }

            // Secondary weapon
            if (template.WeaponOptions_Secondary.Count > 0 && UnityEngine.Random.value < template.SecondaryChance)
            {
                GameObject weaponPrefab = template.WeaponOptions_Secondary[UnityEngine.Random.Range(0, template.WeaponOptions_Secondary.Count)].GetGameObject();
                EquipWeapon(sosig, weaponPrefab, position);
            }

            // Tertiary weapon
            if (template.WeaponOptions_Tertiary.Count > 0 && UnityEngine.Random.value < template.TertiaryChance)
            {
                GameObject weaponPrefab = template.WeaponOptions_Tertiary[UnityEngine.Random.Range(0, template.WeaponOptions_Tertiary.Count)].GetGameObject();
                EquipWeapon(sosig, weaponPrefab, position);
            }
        }

        private void EquipWeapon(Sosig sosig, GameObject weaponPrefab, Vector3 position)
        {
            GameObject weaponGO = Instantiate(weaponPrefab, position + Vector3.up * 0.1f, Quaternion.identity);
            SosigWeapon weapon = weaponGO.GetComponent<SosigWeapon>();
            
            if (weapon != null)
            {
                weapon.SetAutoDestroy(true);
                weapon.O.SpawnLockable = false;
                weapon.IsShakeReloadable = false;
                weapon.SetAmmoClamping(true);

                if (weapon.Type == SosigWeapon.SosigWeaponType.Gun)
                {
                    sosig.Inventory.FillAmmoWithType(weapon.AmmoType);
                }

                sosig.InitHands();
                sosig.ForceEquip(weapon);
            }
        }

        private void ConfigureSosigBehavior(Sosig sosig, bool isFriendly)
        {
            sosig.Inventory.Init();
            sosig.Inventory.FillAllAmmo();

            if (isFriendly)
            {
                // Friendly sosig follows player
                Vector3 followPoint = CalculateFollowPoint();
                sosig.CommandAssaultPoint(followPoint);
                sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            }
            else
            {
                // Enemy sosig attacks player
                sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                sosig.SetCurrentOrder(Sosig.SosigOrder.Assault);
            }
        }

        private void CreateNameplate(Sosig sosig, ChatSosig chatSosig)
        {
            GameObject nameplatePrefab = chatSosig.isFriendly ? this.nameplatePrefab : this.enemyNameplatePrefab;
            if (nameplatePrefab == null) return;

            GameObject nameplate = Instantiate(nameplatePrefab, sosig.Links[1].transform, false);
            nameplate.transform.localPosition = Vector3.zero;
            nameplate.transform.localRotation = Quaternion.identity;

            var textComponents = nameplate.GetComponentsInChildren<UnityEngine.UI.Text>();
            foreach (var text in textComponents)
            {
                text.text = chatSosig.userName;
            }

            chatSosig.nameplate = nameplate;
        }

        private IEnumerator UpdateSosigsCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(sosigUpdateInterval.Value);
                UpdateActiveSosigs();
            }
        }

        private void UpdateActiveSosigs()
        {
            // Clean up null references
            activeChatSosigs.RemoveAll(cs => cs.sosigInstance == null);

            foreach (var chatSosig in activeChatSosigs.ToList())
            {
                if (chatSosig.sosigInstance == null) continue;

                UpdateSosigAI(chatSosig);
                
                // Handle dead sosigs
                if (chatSosig.sosigInstance.BodyState == Sosig.SosigBodyState.Dead)
                {
                    HandleDeadSosig(chatSosig);
                }
            }
        }

        private void UpdateSosigAI(ChatSosig chatSosig)
        {
            Sosig sosig = chatSosig.sosigInstance;
            
            if (sosig.m_isStunned) return;

            if (chatSosig.isFriendly)
            {
                // Update friendly sosig follow behavior
                float distanceToAssaultPoint = Vector3.Distance(GM.CurrentPlayerBody.Head.position, sosig.m_assaultPoint);
                if (distanceToAssaultPoint > sosigFollowDistance.Value)
                {
                    Vector3 followPoint = CalculateFollowPoint();
                    if (!Physics.Linecast(GM.CurrentPlayerBody.Head.position, followPoint, LayerMask.GetMask("Environment")))
                    {
                        sosig.CommandAssaultPoint(followPoint);
                    }
                }

                // Handle combat behavior for friendly sosigs
                if (sosig.Priority.HasFreshTarget() && sosig.CurrentOrder == Sosig.SosigOrder.Investigate && sosig.m_entityRecognition >= 0.65f)
                {
                    sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
                }
            }
            else
            {
                // Update enemy sosig behavior
                float distanceToPlayer = Vector3.Distance(GM.CurrentPlayerBody.Head.position, sosig.Links[1].transform.position);
                if (distanceToPlayer > 20f)
                {
                    sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                }

                // Ensure enemy sosigs stay aggressive
                if (sosig.CurrentOrder == Sosig.SosigOrder.Disabled || 
                    sosig.CurrentOrder == Sosig.SosigOrder.Idle || 
                    sosig.CurrentOrder == Sosig.SosigOrder.GuardPoint)
                {
                    sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                }

                // Handle combat behavior for enemy sosigs
                if (sosig.Priority.HasFreshTarget() && sosig.CurrentOrder == Sosig.SosigOrder.Investigate && sosig.m_entityRecognition >= 0.55f)
                {
                    sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
                }
            }
        }

        private void HandleDeadSosig(ChatSosig chatSosig)
        {
            if (autoCleanupDeadSosigs.Value)
            {
                if (!chatSosig.deathProcessed)
                {
                    chatSosig.deathTime = Time.time;
                    chatSosig.deathProcessed = true;
                    chatSosig.sosigInstance.TickDownToClear(deadSosigCleanupTime.Value);
                }

                if (Time.time - chatSosig.deathTime > deadSosigCleanupTime.Value)
                {
                    RemoveChatSosig(chatSosig);
                }
            }
            else
            {
                chatSosig.sosigInstance.TickDownToClear(3f);
            }
        }

        private void RemoveChatSosig(ChatSosig chatSosig)
        {
            if (chatSosig.nameplate != null)
            {
                Destroy(chatSosig.nameplate);
            }

            if (chatSosig.sosigInstance != null)
            {
                Destroy(chatSosig.sosigInstance.gameObject);
            }

            activeChatSosigs.Remove(chatSosig);
        }

        public void ClearAllChatSosigs()
        {
            foreach (var chatSosig in activeChatSosigs.ToList())
            {
                RemoveChatSosig(chatSosig);
            }
            
            logger.LogInfo("Cleared all chat sosigs");
        }

        // Utility methods
        private string ReadChatUserName(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return "";

                string content = File.ReadAllText(filePath);
                int startIndex = content.IndexOf('"');
                if (startIndex == -1) return "";

                int endIndex = content.LastIndexOf('"');
                if (endIndex <= startIndex) return "";

                return content.Substring(startIndex + 1, endIndex - startIndex - 1);
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to read chat user name from {filePath}: {ex.Message}");
                return "";
            }
        }

        private Vector3 CalculateSpawnPosition()
        {
            Vector3 playerHead = GM.CurrentPlayerBody.Head.position;
            return new Vector3(playerHead.x, GM.CurrentPlayerBody.transform.position.y, playerHead.z + 1f);
        }

        private Vector3 CalculateEnemySpawnPosition()
        {
            // Try to use TNH spawn points if available
            if (GM.TNH_Manager != null)
            {
                var tnhManager = GM.TNH_Manager;
                if (tnhManager.Phase == TNH_Phase.Hold && tnhManager.m_curHoldPoint?.AttackVectors?.Count > 0)
                {
                    var attackVector = tnhManager.m_curHoldPoint.AttackVectors[UnityEngine.Random.Range(0, tnhManager.m_curHoldPoint.AttackVectors.Count)];
                    if (attackVector.SpawnPoints_Sosigs_Attack?.Count > 0)
                    {
                        return attackVector.SpawnPoints_Sosigs_Attack[UnityEngine.Random.Range(0, attackVector.SpawnPoints_Sosigs_Attack.Count)].position;
                    }
                }
                else if (tnhManager.Phase == TNH_Phase.Take && tnhManager.m_curHoldPoint?.SpawnPoints_Turrets?.Count > 0)
                {
                    return tnhManager.m_curHoldPoint.SpawnPoints_Turrets[0].transform.position;
                }
            }

            // Fallback to player-relative position
            return CalculateSpawnPosition();
        }

        private Vector3 CalculateFollowPoint()
        {
            float randomX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
            float randomZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
            
            Vector3 playerHead = GM.CurrentPlayerBody.Head.position;
            return new Vector3(playerHead.x + randomX, playerHead.y, playerHead.z + randomZ);
        }

        private int GetEnemyIFF()
        {
            if (GM.TNH_Manager != null)
            {
                var tnhManager = GM.TNH_Manager;
                if (tnhManager.Phase == TNH_Phase.Hold && tnhManager.m_curHoldPoint?.m_curPhase != null)
                {
                    return tnhManager.m_curHoldPoint.m_curPhase.IFFUsed;
                }
                else if (tnhManager.Phase == TNH_Phase.Take && tnhManager.m_curLevel?.PatrolChallenge?.Patrols?.Count > 0)
                {
                    return tnhManager.m_curLevel.PatrolChallenge.Patrols[0].IFFUsed;
                }
            }
            
            return 1; // Default enemy IFF
        }

        private ArmorSet GetArmorSetByName(string name)
        {
            return availableArmorSets.FirstOrDefault(a => a.name == name) ?? availableArmorSets.First();
        }

        private void ProcessChatSpawnQueue()
        {
            // Process queued chat spawn requests
            while (chatSpawnQueue.Count > 0 && activeChatSosigs.Count < maxActiveSosigs.Value)
            {
                var request = chatSpawnQueue.Dequeue();
                ProcessChatSpawnRequest(request);
            }
        }

        private void ProcessChatSpawnRequest(ChatSpawnRequest request)
        {
            currentChatUserName = request.userName;
            
            if (request.isFriendly)
            {
                SpawnFriendlyChatSosig();
            }
            else
            {
                SpawnEnemyChatSosig();
            }
        }

        // Public API methods
        public void QueueChatSpawn(string userName, bool isFriendly = true, string armorSetName = null)
        {
            if (!string.IsNullOrEmpty(armorSetName) && availableArmorSets.Any(a => a.name == armorSetName))
            {
                selectedArmorSet = armorSetName;
            }

            chatSpawnQueue.Enqueue(new ChatSpawnRequest
            {
                userName = userName,
                isFriendly = isFriendly,
                requestTime = Time.time
            });
        }

        public List<string> GetAvailableArmorSets()
        {
            return availableArmorSets.Select(a => a.name).ToList();
        }

        public ChatSosigStats GetStats()
        {
            return new ChatSosigStats
            {
                activeSosigCount = activeChatSosigs.Count,
                friendlyCount = activeChatSosigs.Count(cs => cs.isFriendly),
                enemyCount = activeChatSosigs.Count(cs => !cs.isFriendly),
                queuedSpawns = chatSpawnQueue.Count,
                totalSpawned = activeChatSosigs.Count // This would be tracked over time in a full implementation
            };
        }

        /// <summary>
        /// Gets the plugin instance - used by other components to access plugin configuration
        /// </summary>
        public H3TVRImproved GetPlugin()
        {
            return plugin;
        }
    }

    // Data structures
    [System.Serializable]
    public class ChatSosig
    {
        public string userName;
        public bool isFriendly;
        public float spawnTime;
        public float deathTime;
        public bool deathProcessed;
        public Sosig sosigInstance;
        public GameObject nameplate;
        public ArmorSet armorSet;
    }

    [System.Serializable]
    public class ArmorSet
    {
        public string name;
        public string description;
        public float headwearChance;
        public float facewearChance;
        public float eyewearChance;
        public float torsowearChance;
        public float pantswearChance;
        public float backpackChance;
        public ArmorLevel armorLevel;
    }

    public enum ArmorLevel
    {
        None,
        Light,
        Medium,
        Heavy,
        Elite
    }

    [System.Serializable]
    public class ChatSpawnRequest
    {
        public string userName;
        public bool isFriendly;
        public float requestTime;
    }

    [System.Serializable]
    public class ChatSosigStats
    {
        public int activeSosigCount;
        public int friendlyCount;
        public int enemyCount;
        public int queuedSpawns;
        public int totalSpawned;
    }
}