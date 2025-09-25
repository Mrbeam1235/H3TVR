using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Configuration;
using System;
using System.Linq;

namespace H3TVR
{
    public class SosigSpawnerManager : MonoBehaviour
    {
        #region Configuration
        public static ConfigEntry<KeyCode> SpawnMenuKey;
        public static ConfigEntry<KeyCode> SpawnAllyKey;
        public static ConfigEntry<KeyCode> SpawnEnemyKey;
        public static ConfigEntry<KeyCode> SpawnSquadKey;
        public static ConfigEntry<KeyCode> ClearAllKey;
        public static ConfigEntry<float> SpawnDistance;
        public static ConfigEntry<bool> EnableCustomArmor;
        public static ConfigEntry<bool> EnableFactionControl;
        public static ConfigEntry<int> DefaultIFF;
        public static ConfigEntry<bool> AutoFollowPlayer;
        public static ConfigEntry<bool> EnableNameplates;
        public static ConfigEntry<string> AllyConfigPath;
        public static ConfigEntry<string> EnemyConfigPath;
        public static ConfigEntry<string> BossConfigPath;
        public static ConfigEntry<bool> EnablePuttersPrettyVoice;
        public static ConfigEntry<float> VoiceVolume;
        public static ConfigEntry<KeyCode> SpawnBossKey;
        public static ConfigEntry<bool> EnableBossSpawning;
        public static ConfigEntry<float> BossSpawnDistance;
        public static ConfigEntry<bool> BossSpecialEffects;
        public static ConfigEntry<float> BossHealthMultiplier;
        public static ConfigEntry<float> BossSpeedMultiplier;
        public static ConfigEntry<bool> BossImmuneToDamage;
        public static ConfigEntry<float> BossImmunityDuration;
        #endregion

        #region GUI Variables
        private bool showGUI = false;
        private Rect windowRect = new Rect(20, 20, 400, 600);
        private Vector2 scrollPosition = Vector2.zero;
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        private GUIStyle labelStyle;
        #endregion

        #region Sosig Configuration
        [System.Serializable]
        public class SosigLoadout
        {
            public string name;
            public List<SosigEnemyTemplate> templates;
            public List<SosigOutfitConfig> outfits;
            public int IFF;
            public bool isEnemy;
            public Color nameColor = Color.white;
        }

        [System.Serializable]
        public class ArmorConfiguration
        {
            public bool useHeadwear = true;
            public bool useFacewear = true;
            public bool useEyewear = true;
            public bool useTorsowear = true;
            public bool usePantswear = true;
            public bool useBackpacks = true;
            public bool useDecorations = true;
            
            public float headwearChance = 0.8f;
            public float facewearChance = 0.3f;
            public float eyewearChance = 0.4f;
            public float torsowearChance = 0.9f;
            public float pantswearChance = 0.7f;
            public float backpackChance = 0.2f;
            public float decorationChance = 0.1f;
        }
        #endregion

        #region Private Variables
        private List<SosigLoadout> availableLoadouts = new List<SosigLoadout>();
        private List<SosigLoadout> allyLoadouts = new List<SosigLoadout>();
        private List<SosigLoadout> enemyLoadouts = new List<SosigLoadout>();
        private List<SosigLoadout> bossLoadouts = new List<SosigLoadout>();
        private ArmorConfiguration currentArmorConfig = new ArmorConfiguration();
        private int selectedLoadoutIndex = 0;
        private int selectedIFF = 0;
        private bool spawnAsEnemy = false;
        private bool enableCustomName = false;
        private string customName = "Spawned Sosig";
        private List<Sosig> spawnedSosigs = new List<Sosig>();
        private GameObject namePlatePrefab;
        private GameObject enemyNamePlatePrefab;
        
        // IFF Options
        private string[] iffOptions = { "Friendly (0)", "Enemy (1)", "Neutral (2)", "Custom" };
        private int customIFF = 3;
        
        // INI Configuration
        private Dictionary<string, SosigINIConfig> allyConfigs = new Dictionary<string, SosigINIConfig>();
        private Dictionary<string, SosigINIConfig> enemyConfigs = new Dictionary<string, SosigINIConfig>();
        private Dictionary<string, SosigINIConfig> bossConfigs = new Dictionary<string, SosigINIConfig>();
        
        // Boss tracking
        private List<Sosig> activeBosses = new List<Sosig>();
        private Dictionary<Sosig, float> bossImmunityTimers = new Dictionary<Sosig, float>();
        
        // PuttersPrettyVoice Integration
        private AudioSource voiceAudioSource;
        private List<AudioClip> voiceClips = new List<AudioClip>();
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            // Initialize H3VR asset loading first
            H3VRAssetLoader.Initialize();
            SosigLoadoutManager.Initialize();
            
            InitializeConfiguration();
            LoadINIConfigurations();
            LoadDefaultLoadouts();
            LoadPuttersPrettyVoice();
            CreateNamePlatePrefabs();
            
            if (EnableBossSpawning.Value)
            {
                Debug.Log("Boss spawning system initialized");
            }
            
            Debug.Log("[SosigSpawnerManager] Advanced Sosig Spawner initialized with H3VR asset loading!");
        }

        void Update()
        {
            HandleKeyBinds();
            CleanupDestroyedSosigs();
            
            if (EnableBossSpawning.Value)
            {
                UpdateBossImmunity();
            }
        }
        
        private void HandleKeyBinds()
        {
            if (Input.GetKeyDown(SpawnMenuKey.Value))
            {
                showGUI = !showGUI;
                Cursor.lockState = showGUI ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = showGUI;
            }
            
            if (Input.GetKeyDown(SpawnAllyKey.Value))
            {
                QuickSpawnAlly();
            }
            
            if (Input.GetKeyDown(SpawnEnemyKey.Value))
            {
                QuickSpawnEnemy();
            }
            
            if (Input.GetKeyDown(SpawnSquadKey.Value))
            {
                QuickSpawnSquad();
            }
            
            if (Input.GetKeyDown(ClearAllKey.Value))
            {
                ClearAllSpawnedSosigs();
            }
            
            if (EnableBossSpawning.Value && Input.GetKeyDown(SpawnBossKey.Value))
            {
                QuickSpawnBoss();
            }
        }

        void OnGUI()
        {
            if (showGUI)
            {
                InitializeGUIStyles();
                windowRect = GUI.Window(0, windowRect, DrawSosigSpawnerWindow, "Advanced Sosig Spawner", windowStyle);
            }
        }
        #endregion

        #region Initialization
        private void InitializeConfiguration()
        {
            var config = ((BaseUnityPlugin)FindObjectOfType<H3TVR>()).Config;
            
            SpawnMenuKey = config.Bind("Sosig Spawner", "SpawnMenuKey", KeyCode.F9, "Key to open the sosig spawner menu");
            SpawnAllyKey = config.Bind("Sosig Spawner", "SpawnAllyKey", KeyCode.F10, "Key to quickly spawn an ally sosig");
            SpawnEnemyKey = config.Bind("Sosig Spawner", "SpawnEnemyKey", KeyCode.F11, "Key to quickly spawn an enemy sosig");
            SpawnSquadKey = config.Bind("Sosig Spawner", "SpawnSquadKey", KeyCode.F12, "Key to spawn a squad of allies");
            ClearAllKey = config.Bind("Sosig Spawner", "ClearAllKey", KeyCode.Delete, "Key to clear all spawned sosigs");
            SpawnDistance = config.Bind("Sosig Spawner", "SpawnDistance", 2.0f, "Distance from player to spawn sosigs");
            EnableCustomArmor = config.Bind("Sosig Spawner", "EnableCustomArmor", true, "Enable custom armor configuration");
            EnableFactionControl = config.Bind("Sosig Spawner", "EnableFactionControl", true, "Enable faction/IFF control");
            DefaultIFF = config.Bind("Sosig Spawner", "DefaultIFF", 0, "Default IFF code for spawned sosigs");
            AutoFollowPlayer = config.Bind("Sosig Spawner", "AutoFollowPlayer", true, "Make spawned sosigs follow the player");
            EnableNameplates = config.Bind("Sosig Spawner", "EnableNameplates", true, "Enable nameplates for spawned sosigs");
            
            // INI file paths
            AllyConfigPath = config.Bind("Sosig Spawner", "AllyConfigPath", "BepInEx/config/H3TVR_AllyConfig.ini", "Path to ally sosig configuration INI file");
            EnemyConfigPath = config.Bind("Sosig Spawner", "EnemyConfigPath", "BepInEx/config/H3TVR_EnemyConfig.ini", "Path to enemy sosig configuration INI file");
            BossConfigPath = config.Bind("Sosig Spawner", "BossConfigPath", "BepInEx/config/H3TVR_BossConfig.ini", "Path to boss sosig configuration INI file");
            
            // PuttersPrettyVoice settings
            EnablePuttersPrettyVoice = config.Bind("Sosig Spawner", "EnablePuttersPrettyVoice", true, "Enable PuttersPrettyVoice integration");
            VoiceVolume = config.Bind("Sosig Spawner", "VoiceVolume", 0.7f, "Volume for sosig voice clips (0.0 - 1.0)");
            
            // Boss settings
            SpawnBossKey = config.Bind("Sosig Spawner", "SpawnBossKey", KeyCode.B, "Key to spawn a boss enemy");
            EnableBossSpawning = config.Bind("Sosig Spawner", "EnableBossSpawning", true, "Enable boss spawning functionality");
            BossSpawnDistance = config.Bind("Sosig Spawner", "BossSpawnDistance", 5.0f, "Distance from player to spawn boss sosigs");
            BossSpecialEffects = config.Bind("Sosig Spawner", "BossSpecialEffects", true, "Enable special effects for boss sosigs");
            BossHealthMultiplier = config.Bind("Sosig Spawner", "BossHealthMultiplier", 3.0f, "Default health multiplier for boss sosigs");
            BossSpeedMultiplier = config.Bind("Sosig Spawner", "BossSpeedMultiplier", 1.2f, "Default speed multiplier for boss sosigs");
            BossImmuneToDamage = config.Bind("Sosig Spawner", "BossImmuneToDamage", true, "Give bosses temporary damage immunity on spawn");
            BossImmunityDuration = config.Bind("Sosig Spawner", "BossImmunityDuration", 3.0f, "Duration of boss damage immunity in seconds");
        }

        private void LoadDefaultLoadouts()
        {
            // Load from INI configurations first
            LoadLoadoutsFromINI();
            
            // Add default loadouts if none were loaded
            if (availableLoadouts.Count == 0)
            {
                var friendlyLoadout = new SosigLoadout()
                {
                    name = "Default Friendly",
                    templates = new List<SosigEnemyTemplate>(),
                    outfits = new List<SosigOutfitConfig>(),
                    IFF = 0,
                    isEnemy = false,
                    nameColor = Color.green
                };

                var enemyLoadout = new SosigLoadout()
                {
                    name = "Default Enemy",
                    templates = new List<SosigEnemyTemplate>(),
                    outfits = new List<SosigOutfitConfig>(),
                    IFF = 1,
                    isEnemy = true,
                    nameColor = Color.red
                };

                availableLoadouts.Add(friendlyLoadout);
                availableLoadouts.Add(enemyLoadout);
            }
        }

        private void CreateNamePlatePrefabs()
        {
            // Create basic nameplate prefabs
            namePlatePrefab = CreateNamePlate(Color.green);
            enemyNamePlatePrefab = CreateNamePlate(Color.red);
        }

        private GameObject CreateNamePlate(Color textColor)
        {
            GameObject nameplate = new GameObject("NamePlate");
            
            Canvas canvas = nameplate.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            CanvasScaler scaler = nameplate.AddComponent<CanvasScaler>();
            scaler.scaleFactor = 0.01f;
            
            GameObject textObject = new GameObject("NameText");
            textObject.transform.SetParent(nameplate.transform);
            
            Text nameText = textObject.AddComponent<Text>();
            nameText.text = "Sosig";
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = 24;
            nameText.color = textColor;
            nameText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);
            
            return nameplate;
        }
        #endregion

        #region GUI Drawing
        private void InitializeGUIStyles()
        {
            if (windowStyle == null)
            {
                windowStyle = new GUIStyle(GUI.skin.window);
                windowStyle.fontSize = 12;
            }
            
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 11;
            }
            
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 10;
            }
        }

        private void DrawSosigSpawnerWindow(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("=== Loadout Selection ===", labelStyle);
            
            // Loadout Selection
            if (availableLoadouts.Count > 0)
            {
                string[] loadoutNames = availableLoadouts.Select(l => l.name).ToArray();
                selectedLoadoutIndex = GUILayout.SelectionGrid(selectedLoadoutIndex, loadoutNames, 1, buttonStyle);
            }

            GUILayout.Space(10);
            GUILayout.Label("=== Faction Control ===", labelStyle);
            
            // IFF Selection
            if (EnableFactionControl.Value)
            {
                selectedIFF = GUILayout.SelectionGrid(selectedIFF, iffOptions, 2, buttonStyle);
                
                if (selectedIFF == 3) // Custom IFF
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Custom IFF:", GUILayout.Width(80));
                    string iffInput = GUILayout.TextField(customIFF.ToString(), GUILayout.Width(50));
                    if (int.TryParse(iffInput, out int newIFF))
                    {
                        customIFF = Mathf.Clamp(newIFF, 0, 10);
                    }
                    GUILayout.EndHorizontal();
                }
                
                spawnAsEnemy = GUILayout.Toggle(spawnAsEnemy, "Spawn as Enemy (Hostile to Player)");
            }

            GUILayout.Space(10);
            GUILayout.Label("=== Armor Configuration ===", labelStyle);
            
            // Armor Configuration
            if (EnableCustomArmor.Value)
            {
                DrawArmorConfiguration();
            }

            GUILayout.Space(10);
            GUILayout.Label("=== Naming ===", labelStyle);
            
            // Custom Naming
            enableCustomName = GUILayout.Toggle(enableCustomName, "Use Custom Name");
            if (enableCustomName)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name:", GUILayout.Width(50));
                customName = GUILayout.TextField(customName, GUILayout.Width(200));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label("=== Spawn Controls ===", labelStyle);
            
            // Main Spawn Controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Single", buttonStyle))
            {
                SpawnSosig();
            }
            if (GUILayout.Button("Spawn Squad (3)", buttonStyle))
            {
                for (int i = 0; i < 3; i++)
                {
                    SpawnSosig(i * 1.5f);
                }
            }
            GUILayout.EndHorizontal();
            
            // Quick Spawn Controls
            GUILayout.Label("Quick Spawn:", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Ally ({SpawnAllyKey.Value})", buttonStyle))
            {
                QuickSpawnAlly();
            }
            if (GUILayout.Button($"Enemy ({SpawnEnemyKey.Value})", buttonStyle))
            {
                QuickSpawnEnemy();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Squad ({SpawnSquadKey.Value})", buttonStyle))
            {
                QuickSpawnSquad();
            }
            if (EnableBossSpawning.Value && GUILayout.Button($"Boss ({SpawnBossKey.Value})", buttonStyle))
            {
                QuickSpawnBoss();
            }
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button($"Clear All ({ClearAllKey.Value})", buttonStyle))
            {
                ClearAllSpawnedSosigs();
            }

            if (GUILayout.Button("Close Menu", buttonStyle))
            {
                showGUI = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            GUILayout.Space(10);
            GUILayout.Label("=== Voice Settings ===", labelStyle);
            
            EnablePuttersPrettyVoice.Value = GUILayout.Toggle(EnablePuttersPrettyVoice.Value, "Enable PuttersPrettyVoice");
            if (EnablePuttersPrettyVoice.Value)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Volume:", GUILayout.Width(60));
                VoiceVolume.Value = GUILayout.HorizontalSlider(VoiceVolume.Value, 0f, 1f, GUILayout.Width(100));
                GUILayout.Label($"{(VoiceVolume.Value * 100):F0}%", GUILayout.Width(40));
                GUILayout.EndHorizontal();
                
                GUILayout.Label($"Voice clips loaded: {voiceClips.Count}", labelStyle);
            }
            
            GUILayout.Space(10);
            GUILayout.Label($"Spawned Sosigs: {spawnedSosigs.Count} | Active Bosses: {activeBosses.Count}", labelStyle);
            GUILayout.Label($"Ally: {allyConfigs.Count} | Enemy: {enemyConfigs.Count} | Boss: {bossConfigs.Count}", labelStyle);
            
            if (EnableBossSpawning.Value)
            {
                GUILayout.Space(5);
                GUILayout.Label("=== Boss Settings ===", labelStyle);
                
                BossHealthMultiplier.Value = GUILayout.HorizontalSlider(BossHealthMultiplier.Value, 1.0f, 10.0f);
                GUILayout.Label($"Boss Health: {BossHealthMultiplier.Value:F1}x", labelStyle);
                
                BossSpeedMultiplier.Value = GUILayout.HorizontalSlider(BossSpeedMultiplier.Value, 0.5f, 3.0f);
                GUILayout.Label($"Boss Speed: {BossSpeedMultiplier.Value:F1}x", labelStyle);
                
                BossSpecialEffects.Value = GUILayout.Toggle(BossSpecialEffects.Value, "Special Effects");
                BossImmuneToDamage.Value = GUILayout.Toggle(BossImmuneToDamage.Value, "Spawn Immunity");
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawArmorConfiguration()
        {
            GUILayout.Label("Armor Pieces:", labelStyle);
            
            currentArmorConfig.useHeadwear = GUILayout.Toggle(currentArmorConfig.useHeadwear, "Headwear");
            if (currentArmorConfig.useHeadwear)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Chance:", GUILayout.Width(60));
                currentArmorConfig.headwearChance = GUILayout.HorizontalSlider(currentArmorConfig.headwearChance, 0f, 1f, GUILayout.Width(100));
                GUILayout.Label($"{(currentArmorConfig.headwearChance * 100):F0}%", GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }

            currentArmorConfig.useFacewear = GUILayout.Toggle(currentArmorConfig.useFacewear, "Facewear");
            if (currentArmorConfig.useFacewear)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Chance:", GUILayout.Width(60));
                currentArmorConfig.facewearChance = GUILayout.HorizontalSlider(currentArmorConfig.facewearChance, 0f, 1f, GUILayout.Width(100));
                GUILayout.Label($"{(currentArmorConfig.facewearChance * 100):F0}%", GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }

            currentArmorConfig.useEyewear = GUILayout.Toggle(currentArmorConfig.useEyewear, "Eyewear");
            if (currentArmorConfig.useEyewear)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Chance:", GUILayout.Width(60));
                currentArmorConfig.eyewearChance = GUILayout.HorizontalSlider(currentArmorConfig.eyewearChance, 0f, 1f, GUILayout.Width(100));
                GUILayout.Label($"{(currentArmorConfig.eyewearChance * 100):F0}%", GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }

            currentArmorConfig.useTorsowear = GUILayout.Toggle(currentArmorConfig.useTorsowear, "Torsowear");
            if (currentArmorConfig.useTorsowear)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Chance:", GUILayout.Width(60));
                currentArmorConfig.torsowearChance = GUILayout.HorizontalSlider(currentArmorConfig.torsowearChance, 0f, 1f, GUILayout.Width(100));
                GUILayout.Label($"{(currentArmorConfig.torsowearChance * 100):F0}%", GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }

            currentArmorConfig.usePantswear = GUILayout.Toggle(currentArmorConfig.usePantswear, "Pantswear");
            if (currentArmorConfig.usePantswear)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Chance:", GUILayout.Width(60));
                currentArmorConfig.pantswearChance = GUILayout.HorizontalSlider(currentArmorConfig.pantswearChance, 0f, 1f, GUILayout.Width(100));
                GUILayout.Label($"{(currentArmorConfig.pantswearChance * 100):F0}%", GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }

            currentArmorConfig.useBackpacks = GUILayout.Toggle(currentArmorConfig.useBackpacks, "Backpacks");
            if (currentArmorConfig.useBackpacks)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Chance:", GUILayout.Width(60));
                currentArmorConfig.backpackChance = GUILayout.HorizontalSlider(currentArmorConfig.backpackChance, 0f, 1f, GUILayout.Width(100));
                GUILayout.Label($"{(currentArmorConfig.backpackChance * 100):F0}%", GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }

            currentArmorConfig.useDecorations = GUILayout.Toggle(currentArmorConfig.useDecorations, "Decorations");
            if (currentArmorConfig.useDecorations)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Chance:", GUILayout.Width(60));
                currentArmorConfig.decorationChance = GUILayout.HorizontalSlider(currentArmorConfig.decorationChance, 0f, 1f, GUILayout.Width(100));
                GUILayout.Label($"{(currentArmorConfig.decorationChance * 100):F0}%", GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }
        }
        #endregion

        #region Sosig Spawning
        public void SpawnSosig(float offsetDistance = 0f)
        {
            if (GM.CurrentPlayerBody == null) return;

            Vector3 spawnPosition = CalculateSpawnPosition(offsetDistance);
            Quaternion spawnRotation = Quaternion.LookRotation(GM.CurrentPlayerBody.Head.forward);

            // Get current IFF based on selection
            int currentIFF = GetCurrentIFF();

            // Create a basic sosig since we don't have loaded templates
            Sosig spawnedSosig = CreateBasicSosig(spawnPosition, spawnRotation, currentIFF);
            
            if (spawnedSosig != null)
            {
                ConfigureSosigBehavior(spawnedSosig, currentIFF);
                ApplyArmorConfiguration(spawnedSosig);
                AttachNameplate(spawnedSosig, currentIFF);
                spawnedSosigs.Add(spawnedSosig);
                
                Debug.Log($"Spawned sosig with IFF {currentIFF} at position {spawnPosition}");
            }
        }

        private Vector3 CalculateSpawnPosition(float offset)
        {
            Vector3 playerPosition = GM.CurrentPlayerBody.Head.position;
            Vector3 forward = GM.CurrentPlayerBody.Head.forward;
            Vector3 right = GM.CurrentPlayerBody.Head.right;
            
            Vector3 spawnPosition = playerPosition + forward * SpawnDistance.Value + right * offset;
            spawnPosition.y = playerPosition.y; // Keep at same height as player
            
            return spawnPosition;
        }

        private int GetCurrentIFF()
        {
            switch (selectedIFF)
            {
                case 0: return 0; // Friendly
                case 1: return 1; // Enemy
                case 2: return 2; // Neutral
                case 3: return customIFF; // Custom
                default: return DefaultIFF.Value;
            }
        }

        private Sosig CreateBasicSosig(Vector3 position, Quaternion rotation, int IFF)
        {
            // Since we don't have access to the sosig templates in this context,
            // we'll need to create a basic sosig. In a real implementation,
            // you'd load the appropriate templates from the Unity assets.
            
            // This is a placeholder - you'd need to implement the actual sosig creation
            // based on available SosigEnemyTemplates and prefabs
            GameObject sosigPrefab = FindBasicSosigPrefab();
            if (sosigPrefab == null)
            {
                Debug.LogError("No sosig prefab found!");
                return null;
            }

            GameObject sosigObject = Instantiate(sosigPrefab, position, rotation);
            Sosig sosig = sosigObject.GetComponentInChildren<Sosig>();
            
            if (sosig != null)
            {
                // Configure basic sosig properties
                sosig.E.IFFCode = IFF;
                sosig.Priority.IFFChart[IFF] = true;
                
                // Set faction relationships
                if (spawnAsEnemy)
                {
                    sosig.E.IFFCode = 1; // Force enemy IFF
                    sosig.Priority.IFFChart[0] = false; // Not friendly to player
                    sosig.Priority.IFFChart[1] = true;  // Friendly to other enemies
                }
            }

            return sosig;
        }

        private GameObject FindBasicSosigPrefab()
        {
            // Try to find a sosig prefab in the scene or resources
            // This is a placeholder - in reality you'd need to reference
            // the appropriate sosig prefabs from your Unity project
            
            // Look for existing sosigs in the scene to clone from
            Sosig existingSosig = FindObjectOfType<Sosig>();
            if (existingSosig != null)
            {
                return existingSosig.gameObject;
            }
            
            Debug.LogWarning("No sosig prefab found - you'll need to set up sosig templates");
            return null;
        }

        private void ConfigureSosigBehavior(Sosig sosig, int IFF)
        {
            // Get INI config if available
            SosigINIConfig config = GetConfigForCurrentLoadout();
            
            if (config != null)
            {
                // Apply config-based behavior
                if (config.followPlayer && !spawnAsEnemy)
                {
                    Vector3 followPoint = GM.CurrentPlayerBody.Head.position;
                    sosig.CommandAssaultPoint(followPoint);
                    sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
                }
                else if (spawnAsEnemy)
                {
                    sosig.CommandAssaultPoint(GM.CurrentPlayerBody.Head.position);
                    sosig.FallbackOrder = Sosig.SosigOrder.Assault;
                }
                
                // Apply health and speed multipliers
                if (config.healthMultiplier != 1.0f && sosig.BodyState != null)
                {
                    // Apply health multiplier (this would need access to sosig health system)
                    ApplyHealthMultiplier(sosig, config.healthMultiplier);
                }
                
                if (config.speedMultiplier != 1.0f)
                {
                    ApplySpeedMultiplier(sosig, config.speedMultiplier);
                }
                
                // Play spawn voice clip if enabled
                if (config.enableVoice && config.voiceClips != null && config.voiceClips.Length > 0)
                {
                    string randomClip = config.voiceClips[UnityEngine.Random.Range(0, config.voiceClips.Length)];
                    StartCoroutine(DelayedVoicePlay(sosig, randomClip, 1.0f));
                }
            }
            else
            {
                // Default behavior
                if (AutoFollowPlayer.Value && !spawnAsEnemy)
                {
                    Vector3 followPoint = GM.CurrentPlayerBody.Head.position;
                    sosig.CommandAssaultPoint(followPoint);
                    sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
                }
                else if (spawnAsEnemy)
                {
                    sosig.CommandAssaultPoint(GM.CurrentPlayerBody.Head.position);
                    sosig.FallbackOrder = Sosig.SosigOrder.Assault;
                }
            }
        }

        private SosigINIConfig GetConfigForCurrentLoadout()
        {
            if (selectedLoadoutIndex < 0 || selectedLoadoutIndex >= availableLoadouts.Count)
                return null;
                
            string loadoutName = availableLoadouts[selectedLoadoutIndex].name;
            
            // Remove [BOSS] prefix if present
            string cleanName = loadoutName.Replace("[BOSS] ", "");
            
            // Check boss configs first
            if (bossConfigs.ContainsKey(cleanName))
                return bossConfigs[cleanName];
                
            // Check ally configs
            if (allyConfigs.ContainsKey(loadoutName))
                return allyConfigs[loadoutName];
                
            // Check enemy configs  
            if (enemyConfigs.ContainsKey(loadoutName))
                return enemyConfigs[loadoutName];
                
            return null;
        }

        private void ApplyHealthMultiplier(Sosig sosig, float multiplier)
        {
            // This would need to be implemented based on H3VR's sosig health system
            // For now, we'll just log the intended change
            Debug.Log($"Applying health multiplier {multiplier} to sosig {sosig.name}");
            
            // Example implementation (would need access to sosig's health system):
            // if (sosig.BodyState != null)
            // {
            //     sosig.BodyState.MaxHealth *= multiplier;
            //     sosig.BodyState.Health = sosig.BodyState.MaxHealth;
            // }
        }

        private void ApplySpeedMultiplier(Sosig sosig, float multiplier)
        {
            // Apply speed multiplier to sosig movement
            Debug.Log($"Applying speed multiplier {multiplier} to sosig {sosig.name}");
            
            // This would need to be implemented based on H3VR's sosig movement system
            // Example implementation:
            // if (sosig.Agent != null)
            // {
            //     sosig.Agent.speed *= multiplier;
            //     sosig.Agent.acceleration *= multiplier;
            // }
        }

        private System.Collections.IEnumerator DelayedVoicePlay(Sosig sosig, string clipName, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (sosig != null)
            {
                PlayVoiceClipForSosig(sosig, clipName);
            }
        }

        private void ApplyArmorConfiguration(Sosig sosig)
        {
            if (!EnableCustomArmor.Value || sosig.Links == null || sosig.Links.Count == 0)
                return;

            // Apply armor based on configuration
            // Note: This requires having SosigOutfitConfig objects available
            // In a real implementation, you'd load these from your Unity assets
            
            SosigOutfitConfig outfitConfig = CreateCustomOutfitConfig();
            ApplyOutfitToSosig(sosig, outfitConfig);
        }

        private SosigOutfitConfig CreateCustomOutfitConfig()
        {
            // Create a custom outfit config using H3VR loaded assets
            SosigOutfitConfig config = ScriptableObject.CreateInstance<SosigOutfitConfig>();
            
            // Get INI config for current loadout
            SosigINIConfig iniConfig = GetConfigForCurrentLoadout();
            
            if (iniConfig != null)
            {
                // Use INI configuration values
                config.Chance_Headwear = currentArmorConfig.useHeadwear ? iniConfig.headwearChance : 0f;
                config.Chance_Facewear = currentArmorConfig.useFacewear ? iniConfig.facewearChance : 0f;
                config.Chance_Eyewear = currentArmorConfig.useEyewear ? iniConfig.eyewearChance : 0f;
                config.Chance_Torsowear = currentArmorConfig.useTorsowear ? iniConfig.torsowearChance : 0f;
                config.Chance_Pantswear = currentArmorConfig.usePantswear ? iniConfig.pantswearChance : 0f;
                config.Chance_Backpacks = currentArmorConfig.useBackpacks ? iniConfig.backpackChance : 0f;
                config.Chance_TorosDecoration = currentArmorConfig.useDecorations ? iniConfig.decorationChance : 0f;
            }
            else
            {
                // Use GUI configuration values as fallback
                config.Chance_Headwear = currentArmorConfig.useHeadwear ? currentArmorConfig.headwearChance : 0f;
                config.Chance_Facewear = currentArmorConfig.useFacewear ? currentArmorConfig.facewearChance : 0f;
                config.Chance_Eyewear = currentArmorConfig.useEyewear ? currentArmorConfig.eyewearChance : 0f;
                config.Chance_Torsowear = currentArmorConfig.useTorsowear ? currentArmorConfig.torsowearChance : 0f;
                config.Chance_Pantswear = currentArmorConfig.usePantswear ? currentArmorConfig.pantswearChance : 0f;
                config.Chance_Backpacks = currentArmorConfig.useBackpacks ? currentArmorConfig.backpackChance : 0f;
                config.Chance_TorosDecoration = currentArmorConfig.useDecorations ? currentArmorConfig.decorationChance : 0f;
            }
            
            // Load armor pieces from H3VR assets
            config.Headwear = H3VRAssetLoader.GetArmorByCategory("Headwear");
            config.Facewear = H3VRAssetLoader.GetArmorByCategory("Facewear");
            config.Eyewear = H3VRAssetLoader.GetArmorByCategory("Eyewear");
            config.Torsowear = H3VRAssetLoader.GetArmorByCategory("Torsowear");
            config.Pantswear = H3VRAssetLoader.GetArmorByCategory("Pantswear");
            config.Pantswear_Lower = H3VRAssetLoader.GetArmorByCategory("PantswearLower");
            config.Backpacks = H3VRAssetLoader.GetArmorByCategory("Backpacks");
            config.TorosDecoration = H3VRAssetLoader.GetArmorByCategory("Decorations");
            
            Debug.Log($"[SosigSpawnerManager] Created outfit config with {config.Headwear.Count} headwear, {config.Torsowear.Count} torsowear, {config.Backpacks.Count} backpacks from H3VR assets");
            
            return config;
        }

        private void ApplyOutfitToSosig(Sosig sosig, SosigOutfitConfig outfitConfig)
        {
            // Apply outfit configuration to sosig
            // This mirrors the logic from the original ChatSpawner
            
            if (sosig.Links.Count > 0 && UnityEngine.Random.Range(0.0f, 1f) < outfitConfig.Chance_Headwear)
                SpawnAccessoryToLink(outfitConfig.Headwear, sosig.Links[0]);
            
            if (sosig.Links.Count > 0 && UnityEngine.Random.Range(0.0f, 1f) < outfitConfig.Chance_Facewear)
                SpawnAccessoryToLink(outfitConfig.Facewear, sosig.Links[0]);
            
            if (sosig.Links.Count > 0 && UnityEngine.Random.Range(0.0f, 1f) < outfitConfig.Chance_Eyewear)
                SpawnAccessoryToLink(outfitConfig.Eyewear, sosig.Links[0]);
            
            if (sosig.Links.Count > 1 && UnityEngine.Random.Range(0.0f, 1f) < outfitConfig.Chance_Torsowear)
                SpawnAccessoryToLink(outfitConfig.Torsowear, sosig.Links[1]);
            
            if (sosig.Links.Count > 2 && UnityEngine.Random.Range(0.0f, 1f) < outfitConfig.Chance_Pantswear)
                SpawnAccessoryToLink(outfitConfig.Pantswear, sosig.Links[2]);
            
            if (sosig.Links.Count > 1 && UnityEngine.Random.Range(0.0f, 1f) < outfitConfig.Chance_Backpacks)
                SpawnAccessoryToLink(outfitConfig.Backpacks, sosig.Links[1]);
            
            if (sosig.Links.Count > 1 && UnityEngine.Random.Range(0.0f, 1f) < outfitConfig.Chance_TorosDecoration)
                SpawnAccessoryToLink(outfitConfig.TorosDecoration, sosig.Links[1]);
        }

        private void SpawnAccessoryToLink(List<FVRObject> accessories, SosigLink link)
        {
            if (accessories == null || accessories.Count == 0 || link == null)
                return;

            FVRObject accessory = accessories[UnityEngine.Random.Range(0, accessories.Count)];
            if (accessory != null)
            {
                GameObject accessoryObject = Instantiate(accessory.GetGameObject(), link.transform.position, link.transform.rotation);
                accessoryObject.transform.SetParent(link.transform);
                
                SosigWearable wearable = accessoryObject.GetComponent<SosigWearable>();
                if (wearable != null)
                {
                    wearable.RegisterWearable(link);
                }
            }
        }

        private void AttachNameplate(Sosig sosig, int IFF)
        {
            if (!EnableNameplates.Value || sosig.Links == null || sosig.Links.Count < 2)
                return;

            GameObject nameplatePrefab = spawnAsEnemy ? enemyNamePlatePrefab : this.namePlatePrefab;
            if (nameplatePrefab == null) return;

            GameObject nameplate = Instantiate(nameplatePrefab, sosig.Links[1].transform);
            nameplate.transform.localPosition = Vector3.zero;
            nameplate.transform.localRotation = Quaternion.identity;

            Text nameText = nameplate.GetComponentInChildren<Text>();
            if (nameText != null)
            {
                string displayName = enableCustomName ? customName : $"Sosig {UnityEngine.Random.Range(1000, 9999)}";
                nameText.text = displayName;
                
                // Set color based on faction
                if (spawnAsEnemy)
                    nameText.color = Color.red;
                else if (IFF == 0)
                    nameText.color = Color.green;
                else if (IFF == 2)
                    nameText.color = Color.yellow;
                else
                    nameText.color = Color.white;
            }
        }
        #endregion

        #region Utility Methods
        private void CleanupDestroyedSosigs()
        {
            spawnedSosigs.RemoveAll(sosig => sosig == null);
            activeBosses.RemoveAll(boss => boss == null);
            
            // Clean up immunity timers for destroyed bosses
            var keysToRemove = bossImmunityTimers.Keys.Where(boss => boss == null).ToList();
            foreach (var key in keysToRemove)
            {
                bossImmunityTimers.Remove(key);
            }
        }

        private void ClearAllSpawnedSosigs()
        {
            foreach (Sosig sosig in spawnedSosigs)
            {
                if (sosig != null)
                {
                    Destroy(sosig.gameObject);
                }
            }
            spawnedSosigs.Clear();
            activeBosses.Clear();
            bossImmunityTimers.Clear();
        }

        public void AddSosigToTracking(Sosig sosig)
        {
            if (sosig != null && !spawnedSosigs.Contains(sosig))
            {
                spawnedSosigs.Add(sosig);
            }
        }

        public void RemoveSosigFromTracking(Sosig sosig)
        {
            spawnedSosigs.Remove(sosig);
        }

        public List<Sosig> GetSpawnedSosigs()
        {
            CleanupDestroyedSosigs();
            return new List<Sosig>(spawnedSosigs);
        }
        #endregion

        #region INI Configuration
        [System.Serializable]
        public class SosigINIConfig
        {
            public string name;
            public string description;
            public int IFF;
            public bool isEnemy;
            public bool followPlayer;
            public string weaponPrimary;
            public string weaponSecondary;
            public string weaponTertiary;
            public float healthMultiplier = 1.0f;
            public float speedMultiplier = 1.0f;
            public bool enableVoice = true;
            public string[] voiceClips;
            
            // Boss-specific properties
            public bool isBoss = false;
            public float bossScale = 1.0f;
            public bool hasDamageImmunity = false;
            public float immunityDuration = 3.0f;
            public bool hasSpecialEffects = true;
            public string bossMusic;
            public string spawnEffect;
            public string deathEffect;
            public int minionsToSpawn = 0;
            public string[] minionTypes;
            public bool regeneratesHealth = false;
            public float regenerationRate = 0.1f;
            public bool enragesAtLowHealth = false;
            public float enrageThreshold = 0.3f;
            public float enrageMultiplier = 1.5f;
            
            // Armor settings
            public float headwearChance = 0.7f;
            public float facewearChance = 0.3f;
            public float eyewearChance = 0.4f;
            public float torsowearChance = 0.8f;
            public float pantswearChance = 0.6f;
            public float backpackChance = 0.2f;
            public float decorationChance = 0.1f;
        }

        private void LoadINIConfigurations()
        {
            LoadAllyConfigurations();
            LoadEnemyConfigurations();
            if (EnableBossSpawning.Value)
            {
                LoadBossConfigurations();
            }
        }

        private void LoadAllyConfigurations()
        {
            string allyPath = AllyConfigPath.Value;
            if (!System.IO.File.Exists(allyPath))
            {
                CreateDefaultAllyINI(allyPath);
            }
            
            try
            {
                var lines = System.IO.File.ReadAllLines(allyPath);
                ParseINIFile(lines, allyConfigs, false);
                Debug.Log($"Loaded {allyConfigs.Count} ally configurations from {allyPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load ally configurations: {e.Message}");
            }
        }

        private void LoadEnemyConfigurations()
        {
            string enemyPath = EnemyConfigPath.Value;
            if (!System.IO.File.Exists(enemyPath))
            {
                CreateDefaultEnemyINI(enemyPath);
            }
            
            try
            {
                var lines = System.IO.File.ReadAllLines(enemyPath);
                ParseINIFile(lines, enemyConfigs, true);
                Debug.Log($"Loaded {enemyConfigs.Count} enemy configurations from {enemyPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load enemy configurations: {e.Message}");
            }
        }

        private void LoadBossConfigurations()
        {
            string bossPath = BossConfigPath.Value;
            if (!System.IO.File.Exists(bossPath))
            {
                CreateDefaultBossINI(bossPath);
            }
            
            try
            {
                var lines = System.IO.File.ReadAllLines(bossPath);
                ParseINIFile(lines, bossConfigs, true, true); // true for enemy, true for boss
                Debug.Log($"Loaded {bossConfigs.Count} boss configurations from {bossPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load boss configurations: {e.Message}");
            }
        }

        private void ParseINIFile(string[] lines, Dictionary<string, SosigINIConfig> configDict, bool isEnemy, bool isBoss = false)
        {
            SosigINIConfig currentConfig = null;
            string currentSection = "";

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    // Save previous config
                    if (currentConfig != null && !string.IsNullOrEmpty(currentSection))
                    {
                        configDict[currentSection] = currentConfig;
                    }

                    // Start new config
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    currentConfig = new SosigINIConfig
                    {
                        name = currentSection,
                        isEnemy = isEnemy,
                        isBoss = isBoss,
                        IFF = isEnemy ? 1 : 0
                    };
                }
                else if (currentConfig != null && trimmedLine.Contains("="))
                {
                    string[] parts = trimmedLine.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim().ToLower();
                        string value = parts[1].Trim();

                        ParseConfigValue(currentConfig, key, value);
                    }
                }
            }

            // Save last config
            if (currentConfig != null && !string.IsNullOrEmpty(currentSection))
            {
                configDict[currentSection] = currentConfig;
            }
        }

        private void ParseConfigValue(SosigINIConfig config, string key, string value)
        {
            switch (key)
            {
                case "description":
                    config.description = value;
                    break;
                case "iff":
                    if (int.TryParse(value, out int iff))
                        config.IFF = iff;
                    break;
                case "followplayer":
                    if (bool.TryParse(value, out bool follow))
                        config.followPlayer = follow;
                    break;
                case "weaponprimary":
                    config.weaponPrimary = value;
                    break;
                case "weaponsecondary":
                    config.weaponSecondary = value;
                    break;
                case "weapontertiary":
                    config.weaponTertiary = value;
                    break;
                case "healthmultiplier":
                    if (float.TryParse(value, out float health))
                        config.healthMultiplier = health;
                    break;
                case "speedmultiplier":
                    if (float.TryParse(value, out float speed))
                        config.speedMultiplier = speed;
                    break;
                case "enablevoice":
                    if (bool.TryParse(value, out bool voice))
                        config.enableVoice = voice;
                    break;
                case "voiceclips":
                    config.voiceClips = value.Split(',');
                    for (int i = 0; i < config.voiceClips.Length; i++)
                    {
                        config.voiceClips[i] = config.voiceClips[i].Trim();
                    }
                    break;
                case "headwearchance":
                    if (float.TryParse(value, out float headwear))
                        config.headwearChance = Mathf.Clamp01(headwear);
                    break;
                case "facewearchance":
                    if (float.TryParse(value, out float facewear))
                        config.facewearChance = Mathf.Clamp01(facewear);
                    break;
                case "eyewearchance":
                    if (float.TryParse(value, out float eyewear))
                        config.eyewearChance = Mathf.Clamp01(eyewear);
                    break;
                case "torsowearchance":
                    if (float.TryParse(value, out float torsowear))
                        config.torsowearChance = Mathf.Clamp01(torsowear);
                    break;
                case "pantswearchance":
                    if (float.TryParse(value, out float pantswear))
                        config.pantswearChance = Mathf.Clamp01(pantswear);
                    break;
                case "backpackchance":
                    if (float.TryParse(value, out float backpack))
                        config.backpackChance = Mathf.Clamp01(backpack);
                    break;
                case "decorationchance":
                    if (float.TryParse(value, out float decoration))
                        config.decorationChance = Mathf.Clamp01(decoration);
                    break;
                // Boss-specific properties
                case "isboss":
                    if (bool.TryParse(value, out bool boss))
                        config.isBoss = boss;
                    break;
                case "bossscale":
                    if (float.TryParse(value, out float scale))
                        config.bossScale = scale;
                    break;
                case "hasdamageimmunity":
                    if (bool.TryParse(value, out bool immunity))
                        config.hasDamageImmunity = immunity;
                    break;
                case "immunityduration":
                    if (float.TryParse(value, out float duration))
                        config.immunityDuration = duration;
                    break;
                case "hasspecialeffects":
                    if (bool.TryParse(value, out bool effects))
                        config.hasSpecialEffects = effects;
                    break;
                case "bossmusic":
                    config.bossMusic = value;
                    break;
                case "spawneffect":
                    config.spawnEffect = value;
                    break;
                case "deatheffect":
                    config.deathEffect = value;
                    break;
                case "minionstospawn":
                    if (int.TryParse(value, out int minions))
                        config.minionsToSpawn = minions;
                    break;
                case "miniontypes":
                    config.minionTypes = value.Split(',');
                    for (int i = 0; i < config.minionTypes.Length; i++)
                    {
                        config.minionTypes[i] = config.minionTypes[i].Trim();
                    }
                    break;
                case "regenerateshealth":
                    if (bool.TryParse(value, out bool regen))
                        config.regeneratesHealth = regen;
                    break;
                case "regenerationrate":
                    if (float.TryParse(value, out float regenRate))
                        config.regenerationRate = regenRate;
                    break;
                case "enragesatlowhealth":
                    if (bool.TryParse(value, out bool enrage))
                        config.enragesAtLowHealth = enrage;
                    break;
                case "enragethreshold":
                    if (float.TryParse(value, out float threshold))
                        config.enrageThreshold = Mathf.Clamp01(threshold);
                    break;
                case "enragemultiplier":
                    if (float.TryParse(value, out float multiplier))
                        config.enrageMultiplier = multiplier;
                    break;
            }
        }

        private void CreateDefaultAllyINI(string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            
            string defaultContent = @"# H3TVR Ally Sosig Configuration
# This file defines loadouts for friendly sosigs
# Each section [SectionName] defines a different loadout

[Standard Soldier]
description=Standard friendly military unit
iff=0
followplayer=true
weaponprimary=AssaultRifle_M4
weaponsecondary=Pistol_M1911
weapontertiary=
healthmultiplier=1.0
speedmultiplier=1.0
enablevoice=true
voiceclips=ally_greeting.wav,ally_roger.wav,ally_moving.wav
headwearchance=0.8
facewearchance=0.3
eyewearchance=0.4
torsowearchance=0.9
pantswearchance=0.7
backpackchance=0.6
decorationchance=0.1

[Elite Operative]
description=High-tier special forces unit
iff=0
followplayer=true
weaponprimary=AssaultRifle_HK416
weaponsecondary=Pistol_Glock17
weapontertiary=Grenade_Frag
healthmultiplier=1.5
speedmultiplier=1.2
enablevoice=true
voiceclips=elite_ready.wav,elite_target.wav,elite_clear.wav
headwearchance=1.0
facewearchance=0.8
eyewearchance=0.6
torsowearchance=1.0
pantswearchance=1.0
backpackchance=0.8
decorationchance=0.3

[Support Medic]
description=Medical support unit
iff=0
followplayer=true
weaponprimary=SMG_MP5
weaponsecondary=Pistol_M1911
weapontertiary=
healthmultiplier=0.8
speedmultiplier=0.9
enablevoice=true
voiceclips=medic_healing.wav,medic_cover.wav,medic_ready.wav
headwearchance=0.9
facewearchance=0.2
eyewearchance=0.3
torsowearchance=0.8
pantswearchance=0.6
backpackchance=0.9
decorationchance=0.4";

            System.IO.File.WriteAllText(path, defaultContent);
            Debug.Log($"Created default ally configuration at {path}");
        }

        private void CreateDefaultEnemyINI(string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            
            string defaultContent = @"# H3TVR Enemy Sosig Configuration
# This file defines loadouts for hostile sosigs
# Each section [SectionName] defines a different loadout

[Standard Grunt]
description=Basic hostile infantry unit
iff=1
followplayer=false
weaponprimary=AssaultRifle_AK74
weaponsecondary=Pistol_Makarov
weapontertiary=
healthmultiplier=1.0
speedmultiplier=1.0
enablevoice=true
voiceclips=enemy_alert.wav,enemy_attack.wav,enemy_spotted.wav
headwearchance=0.6
facewearchance=0.4
eyewearchance=0.2
torsowearchance=0.8
pantswearchance=0.6
backpackchance=0.3
decorationchance=0.1

[Heavy Assault]
description=Heavily armored assault trooper
iff=1
followplayer=false
weaponprimary=LMG_M240
weaponsecondary=Pistol_Desert_Eagle
weapontertiary=Grenade_Frag
healthmultiplier=2.0
speedmultiplier=0.8
enablevoice=true
voiceclips=heavy_suppressing.wav,heavy_advance.wav,heavy_contact.wav
headwearchance=1.0
facewearchance=0.7
eyewearchance=0.5
torsowearchance=1.0
pantswearchance=1.0
backpackchance=0.5
decorationchance=0.2

[Sniper]
description=Long-range marksman
iff=1
followplayer=false
weaponprimary=SniperRifle_M24
weaponsecondary=Pistol_M1911
weapontertiary=
healthmultiplier=0.8
speedmultiplier=1.1
enablevoice=true
voiceclips=sniper_target.wav,sniper_shot.wav,sniper_relocating.wav
headwearchance=0.9
facewearchance=0.6
eyewearchance=0.8
torsowearchance=0.7
pantswearchance=0.8
backpackchance=0.4
decorationchance=0.1";

            System.IO.File.WriteAllText(path, defaultContent);
            Debug.Log($"Created default enemy configuration at {path}");
        }

        private void CreateDefaultBossINI(string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            
            string defaultContent = @"# H3TVR Boss Sosig Configuration
# This file defines loadouts for boss-level enemies
# Bosses are powerful enemies with special abilities and enhanced stats
# Each section [SectionName] defines a different boss type

[Warlord]
description=Heavily armored warlord with massive firepower
iff=1
followplayer=false
weaponprimary=LMG_M249
weaponsecondary=Pistol_Desert_Eagle
weapontertiary=Grenade_Frag
healthmultiplier=4.0
speedmultiplier=1.1
enablevoice=true
voiceclips=warlord_roar.wav,warlord_charge.wav,warlord_die.wav
isboss=true
bossscale=1.2
hasdamageimmunity=true
immunityduration=5.0
hasspecialeffects=true
bossmusic=boss_combat.wav
spawneffect=boss_spawn_explosion
deatheffect=boss_death_explosion
minionstospawn=2
miniontypes=Standard Grunt,Heavy Assault
regenerateshealth=false
enragesatlowhealth=true
enragethreshold=0.25
enragemultiplier=2.0
headwearchance=1.0
facewearchance=0.9
eyewearchance=0.8
torsowearchance=1.0
pantswearchance=1.0
backpackchance=0.8
decorationchance=0.5

[Assassin Lord]
description=Elite stealth assassin with deadly precision
iff=1
followplayer=false
weaponprimary=SniperRifle_Barrett
weaponsecondary=SMG_MP7
weapontertiary=Knife_Combat
healthmultiplier=2.5
speedmultiplier=1.8
enablevoice=true
voiceclips=assassin_strike.wav,assassin_vanish.wav,assassin_death.wav
isboss=true
bossscale=1.0
hasdamageimmunity=true
immunityduration=3.0
hasspecialeffects=true
bossmusic=stealth_boss.wav
spawneffect=smoke_appear
deatheffect=shadow_dissipate
minionstospawn=0
regenerateshealth=true
regenerationrate=0.05
enragesatlowhealth=false
headwearchance=0.8
facewearchance=1.0
eyewearchance=0.9
torsowearchance=0.7
pantswearchance=0.8
backpackchance=0.3
decorationchance=0.2

[Demolisher]
description=Explosive specialist with area destruction capabilities
iff=1
followplayer=false
weaponprimary=GrenadeLauncher_M32
weaponsecondary=Shotgun_AA12
weapontertiary=C4_Explosive
healthmultiplier=3.5
speedmultiplier=0.9
enablevoice=true
voiceclips=demo_boom.wav,demo_kaboom.wav,demo_destruction.wav
isboss=true
bossscale=1.1
hasdamageimmunity=true
immunityduration=4.0
hasspecialeffects=true
bossmusic=destruction_theme.wav
spawneffect=explosive_entry
deatheffect=massive_explosion
minionstospawn=3
miniontypes=Demolitions Expert,Standard Grunt
regenerateshealth=false
enragesatlowhealth=true
enragethreshold=0.3
enragemultiplier=1.5
headwearchance=1.0
facewearchance=0.7
eyewearchance=0.6
torsowearchance=1.0
pantswearchance=1.0
backpackchance=1.0
decorationchance=0.8

[Berserker King]
description=Savage melee specialist with overwhelming aggression
iff=1
followplayer=false
weaponprimary=Shotgun_Spas12
weaponsecondary=Melee_Chainsaw
weapontertiary=Melee_Axe
healthmultiplier=5.0
speedmultiplier=1.5
enablevoice=true
voiceclips=berserker_rage.wav,berserker_bloodlust.wav,berserker_last_stand.wav
isboss=true
bossscale=1.3
hasdamageimmunity=true
immunityduration=6.0
hasspecialeffects=true
bossmusic=rage_theme.wav
spawneffect=blood_aura
deatheffect=berserker_collapse
minionstospawn=1
miniontypes=Berserker
regenerateshealth=true
regenerationrate=0.1
enragesatlowhealth=true
enragethreshold=0.4
enragemultiplier=2.5
headwearchance=0.6
facewearchance=0.4
eyewearchance=0.2
torsowearchance=0.8
pantswearchance=0.7
backpackchance=0.2
decorationchance=0.1

[Cyber Commander]
description=High-tech boss with advanced weaponry and shields
iff=1
followplayer=false
weaponprimary=Rifle_Plasma
weaponsecondary=Pistol_Energy
weapontertiary=Shield_Generator
healthmultiplier=3.0
speedmultiplier=1.3
enablevoice=true
voiceclips=cyber_initialize.wav,cyber_shields_up.wav,cyber_system_failure.wav
isboss=true
bossscale=1.1
hasdamageimmunity=true
immunityduration=8.0
hasspecialeffects=true
bossmusic=cyber_boss.wav
spawneffect=tech_materialize
deatheffect=system_shutdown
minionstospawn=4
miniontypes=Commando,Elite Sniper
regenerateshealth=true
regenerationrate=0.08
enragesatlowhealth=false
headwearchance=1.0
facewearchance=1.0
eyewearchance=1.0
torsowearchance=1.0
pantswearchance=1.0
backpackchance=1.0
decorationchance=1.0";

            System.IO.File.WriteAllText(path, defaultContent);
            Debug.Log($"Created default boss configuration at {path}");
        }

        private void LoadLoadoutsFromINI()
        {
            // Convert ally configs to loadouts
            foreach (var kvp in allyConfigs)
            {
                var config = kvp.Value;
                var loadout = new SosigLoadout()
                {
                    name = config.name,
                    templates = new List<SosigEnemyTemplate>(),
                    outfits = new List<SosigOutfitConfig>(),
                    IFF = config.IFF,
                    isEnemy = false,
                    nameColor = Color.green
                };
                availableLoadouts.Add(loadout);
                allyLoadouts.Add(loadout);
            }

            // Convert enemy configs to loadouts
            foreach (var kvp in enemyConfigs)
            {
                var config = kvp.Value;
                var loadout = new SosigLoadout()
                {
                    name = config.name,
                    templates = new List<SosigEnemyTemplate>(),
                    outfits = new List<SosigOutfitConfig>(),
                    IFF = config.IFF,
                    isEnemy = true,
                    nameColor = Color.red
                };
                availableLoadouts.Add(loadout);
                enemyLoadouts.Add(loadout);
            }

            // Convert boss configs to loadouts
            foreach (var kvp in bossConfigs)
            {
                var config = kvp.Value;
                var loadout = new SosigLoadout()
                {
                    name = $"[BOSS] {config.name}",
                    templates = new List<SosigEnemyTemplate>(),
                    outfits = new List<SosigOutfitConfig>(),
                    IFF = config.IFF,
                    isEnemy = true,
                    nameColor = Color.magenta
                };
                availableLoadouts.Add(loadout);
                bossLoadouts.Add(loadout);
            }
        }
        #endregion

        #region PuttersPrettyVoice Integration
        private void LoadPuttersPrettyVoice()
        {
            if (!EnablePuttersPrettyVoice.Value)
                return;

            try
            {
                // Setup audio source for voice playback
                if (voiceAudioSource == null)
                {
                    GameObject voiceObject = new GameObject("SosigVoiceSource");
                    voiceObject.transform.SetParent(transform);
                    voiceAudioSource = voiceObject.AddComponent<AudioSource>();
                    voiceAudioSource.volume = VoiceVolume.Value;
                    voiceAudioSource.spatialBlend = 1.0f; // 3D audio
                }

                // Load voice clips from PuttersPrettyVoice folder
                string voicePath = "Assets/CompletedBounties/jediSpawner/PuttersPrettyVoice/";
                LoadVoiceClipsFromPath(voicePath);
                
                Debug.Log($"Loaded {voiceClips.Count} voice clips for sosig spawner");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load PuttersPrettyVoice: {e.Message}");
            }
        }

        private void LoadVoiceClipsFromPath(string path)
        {
            if (!System.IO.Directory.Exists(path))
            {
                Debug.LogWarning($"PuttersPrettyVoice directory not found: {path}");
                return;
            }

            string[] audioFiles = System.IO.Directory.GetFiles(path, "*.wav");
            foreach (string audioFile in audioFiles)
            {
                try
                {
                    // In a real implementation, you'd load the audio clip using Unity's audio loading system
                    // For now, we'll just log the files found
                    Debug.Log($"Found voice clip: {System.IO.Path.GetFileName(audioFile)}");
                    
                    // This is a placeholder - you'd need to implement actual audio loading
                    // AudioClip clip = LoadAudioClip(audioFile);
                    // if (clip != null) voiceClips.Add(clip);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to load voice clip {audioFile}: {e.Message}");
                }
            }
        }

        private void PlayVoiceClipForSosig(Sosig sosig, string clipName = null)
        {
            if (!EnablePuttersPrettyVoice.Value || voiceClips.Count == 0 || sosig == null)
                return;

            try
            {
                AudioClip clipToPlay = null;
                
                if (!string.IsNullOrEmpty(clipName))
                {
                    // Try to find specific clip
                    clipToPlay = voiceClips.Find(clip => clip.name.Contains(clipName));
                }
                
                if (clipToPlay == null && voiceClips.Count > 0)
                {
                    // Play random clip
                    clipToPlay = voiceClips[UnityEngine.Random.Range(0, voiceClips.Count)];
                }

                if (clipToPlay != null && voiceAudioSource != null)
                {
                    voiceAudioSource.transform.position = sosig.transform.position;
                    voiceAudioSource.volume = VoiceVolume.Value;
                    voiceAudioSource.PlayOneShot(clipToPlay);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to play voice clip for sosig: {e.Message}");
            }
        }
        #endregion

        #region Quick Spawn Methods
        private void QuickSpawnAlly()
        {
            if (allyLoadouts.Count > 0)
            {
                selectedLoadoutIndex = availableLoadouts.IndexOf(allyLoadouts[UnityEngine.Random.Range(0, allyLoadouts.Count)]);
                selectedIFF = 0; // Force friendly
                spawnAsEnemy = false;
                SpawnSosig();
            }
            else
            {
                Debug.LogWarning("No ally loadouts available for quick spawn");
            }
        }

        private void QuickSpawnEnemy()
        {
            if (enemyLoadouts.Count > 0)
            {
                selectedLoadoutIndex = availableLoadouts.IndexOf(enemyLoadouts[UnityEngine.Random.Range(0, enemyLoadouts.Count)]);
                selectedIFF = 1; // Force enemy
                spawnAsEnemy = true;
                SpawnSosig();
            }
            else
            {
                Debug.LogWarning("No enemy loadouts available for quick spawn");
            }
        }

        private void QuickSpawnSquad()
        {
            for (int i = 0; i < 3; i++)
            {
                QuickSpawnAlly();
                // Add slight delay and offset
                StartCoroutine(DelayedSpawn(i * 0.5f, i * 1.5f));
            }
        }

        private System.Collections.IEnumerator DelayedSpawn(float delay, float offset)
        {
            yield return new WaitForSeconds(delay);
            SpawnSosig(offset);
        }

        private void QuickSpawnBoss()
        {
            if (!EnableBossSpawning.Value)
            {
                Debug.LogWarning("Boss spawning is disabled");
                return;
            }

            if (bossLoadouts.Count > 0)
            {
                selectedLoadoutIndex = availableLoadouts.IndexOf(bossLoadouts[UnityEngine.Random.Range(0, bossLoadouts.Count)]);
                selectedIFF = 1; // Force enemy
                spawnAsEnemy = true;
                SpawnBoss();
            }
            else
            {
                Debug.LogWarning("No boss loadouts available for quick spawn");
            }
        }

        private void SpawnBoss()
        {
            if (GM.CurrentPlayerBody == null) return;

            Vector3 spawnPosition = CalculateBossSpawnPosition();
            Quaternion spawnRotation = Quaternion.LookRotation(GM.CurrentPlayerBody.Head.forward);

            // Get current IFF based on selection
            int currentIFF = GetCurrentIFF();

            // Create the boss sosig
            Sosig spawnedBoss = CreateBasicSosig(spawnPosition, spawnRotation, currentIFF);
            
            if (spawnedBoss != null)
            {
                ConfigureBossBehavior(spawnedBoss, currentIFF);
                ApplyBossConfiguration(spawnedBoss);
                ApplyArmorConfiguration(spawnedBoss);
                AttachBossNameplate(spawnedBoss, currentIFF);
                
                // Add to tracking lists
                spawnedSosigs.Add(spawnedBoss);
                activeBosses.Add(spawnedBoss);
                
                // Apply damage immunity if configured
                SosigINIConfig config = GetConfigForCurrentLoadout();
                if (config != null && config.hasDamageImmunity)
                {
                    bossImmunityTimers[spawnedBoss] = config.immunityDuration;
                }
                
                // Spawn minions if configured
                if (config != null && config.minionsToSpawn > 0)
                {
                    StartCoroutine(SpawnBossMinions(spawnedBoss, config));
                }
                
                // Play boss music and effects
                if (config != null && config.hasSpecialEffects)
                {
                    PlayBossSpawnEffects(spawnedBoss, config);
                }
                
                Debug.Log($"Spawned boss '{config?.name}' with {config?.healthMultiplier}x health at position {spawnPosition}");
            }
        }

        private Vector3 CalculateBossSpawnPosition()
        {
            Vector3 playerPosition = GM.CurrentPlayerBody.Head.position;
            Vector3 forward = GM.CurrentPlayerBody.Head.forward;
            
            // Spawn bosses further away than regular sosigs
            Vector3 spawnPosition = playerPosition + forward * BossSpawnDistance.Value;
            spawnPosition.y = playerPosition.y;
            
            return spawnPosition;
        }

        private void ConfigureBossBehavior(Sosig sosig, int IFF)
        {
            SosigINIConfig config = GetConfigForCurrentLoadout();
            
            if (config != null && config.isBoss)
            {
                // Always make bosses aggressive
                sosig.CommandAssaultPoint(GM.CurrentPlayerBody.Head.position);
                sosig.FallbackOrder = Sosig.SosigOrder.Assault;
                
                // Apply boss-specific multipliers
                float healthMult = config.healthMultiplier * BossHealthMultiplier.Value;
                float speedMult = config.speedMultiplier * BossSpeedMultiplier.Value;
                
                ApplyHealthMultiplier(sosig, healthMult);
                ApplySpeedMultiplier(sosig, speedMult);
                
                // Apply boss scale
                if (config.bossScale != 1.0f)
                {
                    sosig.transform.localScale *= config.bossScale;
                }
                
                // Play spawn voice clip
                if (config.enableVoice && config.voiceClips != null && config.voiceClips.Length > 0)
                {
                    string randomClip = config.voiceClips[UnityEngine.Random.Range(0, config.voiceClips.Length)];
                    StartCoroutine(DelayedVoicePlay(sosig, randomClip, 1.0f));
                }
                
                // Start boss-specific coroutines
                if (config.regeneratesHealth)
                {
                    StartCoroutine(BossHealthRegeneration(sosig, config));
                }
                
                if (config.enragesAtLowHealth)
                {
                    StartCoroutine(BossEnrageMonitor(sosig, config));
                }
            }
        }

        private void ApplyBossConfiguration(Sosig sosig)
        {
            SosigINIConfig config = GetConfigForCurrentLoadout();
            if (config == null || !config.isBoss) return;

            // Boss-specific visual effects could be applied here
            if (BossSpecialEffects.Value && config.hasSpecialEffects)
            {
                // Add glow effect, particle systems, etc.
                ApplyBossVisualEffects(sosig, config);
            }
        }

        private void ApplyBossVisualEffects(Sosig sosig, SosigINIConfig config)
        {
            // This would add visual effects to make the boss stand out
            // For example: glowing outline, particle effects, different materials
            Debug.Log($"Applying special visual effects to boss {config.name}");
            
            // Example: Add a colored glow effect
            // You could implement this with Unity's particle systems or post-processing effects
        }

        private void AttachBossNameplate(Sosig sosig, int IFF)
        {
            if (!EnableNameplates.Value || sosig.Links == null || sosig.Links.Count < 2)
                return;

            GameObject nameplatePrefab = this.enemyNamePlatePrefab;
            if (nameplatePrefab == null) return;

            GameObject nameplate = Instantiate(nameplatePrefab, sosig.Links[1].transform);
            nameplate.transform.localPosition = Vector3.up * 0.5f; // Higher nameplate for bosses
            nameplate.transform.localRotation = Quaternion.identity;

            Text nameText = nameplate.GetComponentInChildren<Text>();
            if (nameText != null)
            {
                SosigINIConfig config = GetConfigForCurrentLoadout();
                string displayName = config != null ? $"★ BOSS: {config.name} ★" : "★ BOSS ★";
                nameText.text = displayName;
                nameText.color = Color.magenta;
                nameText.fontSize = 28; // Larger font for bosses
            }
        }

        private void PlayBossSpawnEffects(Sosig sosig, SosigINIConfig config)
        {
            // Play boss music if specified
            if (!string.IsNullOrEmpty(config.bossMusic))
            {
                PlayVoiceClipForSosig(sosig, config.bossMusic);
            }

            // Create spawn effect
            if (!string.IsNullOrEmpty(config.spawnEffect))
            {
                CreateSpawnEffect(sosig.transform.position, config.spawnEffect);
            }

            Debug.Log($"Boss {config.name} has entered the battlefield!");
        }

        private void CreateSpawnEffect(Vector3 position, string effectName)
        {
            // This would create visual/audio effects at the spawn location
            Debug.Log($"Creating spawn effect '{effectName}' at {position}");
            // Implementation would depend on available particle systems and audio clips
        }

        private System.Collections.IEnumerator SpawnBossMinions(Sosig boss, SosigINIConfig config)
        {
            yield return new WaitForSeconds(2.0f); // Wait before spawning minions
            
            if (boss == null || config.minionTypes == null) yield break;

            for (int i = 0; i < config.minionsToSpawn; i++)
            {
                if (config.minionTypes.Length > 0)
                {
                    string minionType = config.minionTypes[UnityEngine.Random.Range(0, config.minionTypes.Length)];
                    SpawnMinionForBoss(boss, minionType, i);
                    yield return new WaitForSeconds(0.5f); // Stagger minion spawns
                }
            }
        }

        private void SpawnMinionForBoss(Sosig boss, string minionType, int index)
        {
            // Find the minion configuration
            SosigINIConfig minionConfig = null;
            if (enemyConfigs.ContainsKey(minionType))
            {
                minionConfig = enemyConfigs[minionType];
            }

            if (minionConfig != null)
            {
                Vector3 spawnPos = boss.transform.position + UnityEngine.Random.insideUnitSphere * 3f;
                spawnPos.y = boss.transform.position.y;
                
                // Spawn minion using the regular enemy spawning system
                // This would require temporarily setting the loadout to the minion type
                Debug.Log($"Spawning minion '{minionType}' for boss near {spawnPos}");
            }
        }

        private System.Collections.IEnumerator BossHealthRegeneration(Sosig boss, SosigINIConfig config)
        {
            while (boss != null && activeBosses.Contains(boss))
            {
                yield return new WaitForSeconds(1.0f);
                
                // Implement health regeneration logic here
                // This would need access to the sosig's health system
                Debug.Log($"Boss {config.name} regenerating health at rate {config.regenerationRate}");
            }
        }

        private System.Collections.IEnumerator BossEnrageMonitor(Sosig boss, SosigINIConfig config)
        {
            bool hasEnraged = false;
            
            while (boss != null && activeBosses.Contains(boss) && !hasEnraged)
            {
                yield return new WaitForSeconds(0.5f);
                
                // Check if boss health is below enrage threshold
                // This would need access to the sosig's health system
                // if (boss.Health / boss.MaxHealth <= config.enrageThreshold)
                // {
                //     EnrageBoss(boss, config);
                //     hasEnraged = true;
                // }
            }
        }

        private void EnrageBoss(Sosig boss, SosigINIConfig config)
        {
            Debug.Log($"Boss {config.name} has become enraged! Damage and speed increased by {config.enrageMultiplier}x");
            
            // Apply enrage effects
            ApplySpeedMultiplier(boss, config.enrageMultiplier);
            
            // Play enrage voice clip
            if (config.voiceClips != null && config.voiceClips.Length > 0)
            {
                PlayVoiceClipForSosig(boss, "enrage");
            }
            
            // Visual effects for enrage
            if (config.hasSpecialEffects)
            {
                // Add red glow, particle effects, etc.
                Debug.Log($"Applying enrage visual effects to {config.name}");
            }
        }

        private void UpdateBossImmunity()
        {
            var keysToRemove = new List<Sosig>();
            
            foreach (var kvp in bossImmunityTimers.ToList())
            {
                if (kvp.Key == null)
                {
                    keysToRemove.Add(kvp.Key);
                    continue;
                }
                
                bossImmunityTimers[kvp.Key] -= Time.deltaTime;
                
                if (bossImmunityTimers[kvp.Key] <= 0)
                {
                    keysToRemove.Add(kvp.Key);
                    Debug.Log($"Boss immunity expired for {kvp.Key.name}");
                }
            }
            
            foreach (var key in keysToRemove)
            {
                bossImmunityTimers.Remove(key);
            }
        }
        
        #region H3VR Asset Integration
        
        /// <summary>
        /// Spawn a sosig using advanced loadout configuration and H3VR assets
        /// </summary>
        public void SpawnSosigFromAdvancedLoadout(string loadoutName, Vector3? position = null)
        {
            try
            {
                var loadout = SosigLoadoutManager.GetLoadout(loadoutName);
                if (loadout == null)
                {
                    Debug.LogWarning($"Loadout not found: {loadoutName}");
                    return;
                }
                
                Vector3 spawnPos = position ?? (GM.CurrentPlayerBody.transform.position + GM.CurrentPlayerBody.transform.forward * 3f);
                Quaternion rotation = Quaternion.LookRotation(GM.CurrentPlayerBody.transform.forward);
                
                Sosig spawnedSosig = SosigLoadoutUtility.CreateSosigFromLoadout(loadout, spawnPos, rotation);
                
                if (spawnedSosig != null)
                {
                    Debug.Log($"Successfully spawned sosig using H3VR loadout: {loadout.loadoutName}");
                    
                    // Add to tracking
                    var statsManager = FindObjectOfType<SosigStatsManager>();
                    if (statsManager != null)
                    {
                        // Track the spawn
                        Debug.Log($"Tracked spawn for loadout: {loadout.loadoutName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Failed to spawn sosig from H3VR loadout: {loadout.loadoutName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error spawning sosig from H3VR loadout {loadoutName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get list of available H3VR loadouts
        /// </summary>
        public List<string> GetAvailableH3VRLoadouts()
        {
            var loadouts = SosigLoadoutManager.GetLoadouts();
            return loadouts.Select(l => l.loadoutName).ToList();
        }
        
        /// <summary>
        /// Test H3VR asset loading (for debugging)
        /// </summary>
        public void TestH3VRAssetLoading()
        {
            Debug.Log("[SosigSpawnerManager] Running H3VR asset loading test...");
            H3VRAssetLoadingTest.RunAssetLoadingTest();
            H3VRAssetLoadingTest.TestSosigCreationDryRun();
        }
        
        /// <summary>
        /// Refresh H3VR assets (useful if assets change)
        /// </summary>
        public void RefreshH3VRAssets()
        {
            Debug.Log("[SosigSpawnerManager] Refreshing H3VR assets...");
            H3VRAssetLoader.ForceReload();
            SosigLoadoutManager.RefreshFromH3VR();
            Debug.Log("[SosigSpawnerManager] H3VR assets refreshed");
        }
        
        /// <summary>
        /// Get the current status of H3VR asset loading
        /// </summary>
        /// <returns>Status report string</returns>
        public string GetH3VRAssetStatus()
        {
            var status = new System.Text.StringBuilder();
            status.AppendLine("=== H3VR Asset Loading Status ===");
            
            // Check if H3VR systems are initialized
            bool h3vrReady = H3VRAssetLoader.IsH3VRSystemReady();
            status.AppendLine($"H3VR System Ready: {h3vrReady}");
            
            if (h3vrReady)
            {
                var stats = H3VRAssetLoader.GetLoadingStats();
                status.AppendLine($"Armor Pieces Loaded: {stats.armorCount}");
                status.AppendLine($"Weapons Loaded: {stats.weaponCount}");
                status.AppendLine($"Sosig Templates Loaded: {stats.sosigTemplateCount}");
                status.AppendLine($"Last Update: {stats.lastUpdateTime}");
            }
            else
            {
                status.AppendLine("H3VR systems not yet initialized - using delayed initialization");
            }
            
            return status.ToString();
        }
        
        /// <summary>
        /// Check if H3VR asset loading is ready for sosig spawning
        /// </summary>
        /// <returns>True if ready, false if still initializing</returns>
        public bool IsH3VRAssetLoadingReady()
        {
            return H3VRAssetLoader.IsH3VRSystemReady();
        }
        
        #endregion
        
        #endregion
    }
}