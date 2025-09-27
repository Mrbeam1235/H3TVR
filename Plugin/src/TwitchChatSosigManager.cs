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
    /// Simple Twitch chat-integrated sosig spawner using only two file paths for names
    /// </summary>
    public class TwitchChatSosigManager : MonoBehaviour
    {
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        
        // Cache for better performance
        private List<string> cachedAllyNames;
        private List<string> cachedEnemyNames;
        private SosigEnemyTemplate[] cachedSosigTemplates;

        [Header("File Paths")]
        public string allyNamesFilePath = "";
        public string enemyNamesFilePath = "";

        [Header("Active Sosigs")]
        public List<Sosig> activeSosigs = new List<Sosig>();

        // Configuration entries - simplified to just file paths
        private ConfigEntry<string> allyFilePath;
        private ConfigEntry<string> enemyFilePath;
        private ConfigEntry<KeyCode> spawnAllyKey;
        private ConfigEntry<KeyCode> spawnEnemyKey;
        private ConfigEntry<KeyCode> clearSosigsKey;
        private ConfigEntry<KeyCode> armorGUIKey;
        private ConfigEntry<int> maxActiveSosigs;
        private ConfigEntry<bool> enableNameplates;

        // GUI System
        private bool showArmorGUI = false;
        private Rect armorWindowRect = new Rect(50, 50, 450, 600);
        private Vector2 scrollPosition = Vector2.zero;
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        private GUIStyle labelStyle;
        private GUIStyle toggleStyle;
        private GUIStyle sliderStyle;

        // Armor Configuration
        private ArmorConfiguration currentArmorConfig;
        private int selectedSosigIndex = -1;
        private string[] armorSlots = { "Headwear", "Facewear", "Eyewear", "Torsowear", "Pantswear", "PantswearLower", "Backpacks", "Decorations" };
        private Dictionary<string, List<FVRObject>> availableArmor;
        private Dictionary<string, int> selectedArmorIndices;

        [System.Serializable]
        public class ArmorConfiguration
        {
            [Header("Armor Slots Enabled")]
            public bool enableHeadwear = true;
            public bool enableFacewear = true;
            public bool enableEyewear = true;
            public bool enableTorsowear = true;
            public bool enablePantswear = true;
            public bool enablePantswearLower = true;
            public bool enableBackpacks = true;
            public bool enableDecorations = true;

            [Header("Armor Spawn Chances")]
            [Range(0f, 1f)] public float headwearChance = 0.7f;
            [Range(0f, 1f)] public float facewearChance = 0.3f;
            [Range(0f, 1f)] public float eyewearChance = 0.4f;
            [Range(0f, 1f)] public float torsowearChance = 0.8f;
            [Range(0f, 1f)] public float pantswearChance = 0.6f;
            [Range(0f, 1f)] public float pantswearLowerChance = 0.4f;
            [Range(0f, 1f)] public float backpackChance = 0.2f;
            [Range(0f, 1f)] public float decorationChance = 0.1f;

            [Header("Armor Preferences")]
            public bool preferMilitaryArmor = true;
            public bool allowCivilianArmor = true;
            public bool allowFuturisticArmor = false;
            public bool randomizeColors = true;
        }

        void Start()
        {
            // Add safety check
            if (plugin == null)
            {
                Debug.LogError("TwitchChatSosigManager: Plugin not initialized before Start()");
                return;
            }
            
            InitializeArmorSystem();
            InitializeConfiguration();
            StartCoroutine(UpdateSosigsCoroutine());
        }

        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            plugin = pluginInstance;
            logger = logSource;
            
            logger.LogInfo("TwitchChatSosigManager initialized with armor GUI system!");
        }

        private void InitializeArmorSystem()
        {
            // Initialize armor configuration
            currentArmorConfig = new ArmorConfiguration();
            
            // Initialize armor selection indices
            selectedArmorIndices = new Dictionary<string, int>();
            foreach (string slot in armorSlots)
            {
                selectedArmorIndices[slot] = 0;
            }

            // Load available armor from H3VR asset loader
            StartCoroutine(LoadArmorAssetsCoroutine());
        }

        private IEnumerator LoadArmorAssetsCoroutine()
        {
            // Wait for H3VR asset loader to initialize
            yield return new WaitForSeconds(2f);
            
            // Initialize H3VR asset loader if not already done
            if (!H3VRAssetLoader.IsInitialized)
            {
                H3VRAssetLoader.Initialize();
                yield return new WaitForSeconds(1f);
            }

            // Load armor categories - using separate method to avoid yield in try-catch
            bool loadingSuccessful = LoadArmorCategoriesSafely();
            
            if (loadingSuccessful && logger != null)
            {
                int totalArmor = availableArmor.Values.Sum(list => list.Count);
                logger.LogInfo($"Loaded {totalArmor} armor pieces across {availableArmor.Count} categories for GUI");
            }
        }

        private bool LoadArmorCategoriesSafely()
        {
            try
            {
                // Load armor categories
                availableArmor = H3VRAssetLoader.GetAllArmorCategories();
                return true;
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to load armor assets: {ex.Message}");
                
                // Fallback to empty armor lists
                availableArmor = new Dictionary<string, List<FVRObject>>();
                foreach (string slot in armorSlots)
                {
                    availableArmor[slot] = new List<FVRObject>();
                }
                return false;
            }
        }

        private void InitializeConfiguration()
        {
            // Add null check
            if (plugin == null)
            {
                Debug.LogError("Plugin is null in InitializeConfiguration");
                return;
            }
            
            var config = plugin.Config;

            // File paths for sosig names
            allyFilePath = config.Bind("Chat Sosigs", "AllyNamesFilePath", 
                Path.Combine(BepInEx.Paths.GameRootPath, "ally_names.txt"),
                "Path to file containing ally sosig names (one per line)");
                
            enemyFilePath = config.Bind("Chat Sosigs", "EnemyNamesFilePath", 
                Path.Combine(BepInEx.Paths.GameRootPath, "enemy_names.txt"),
                "Path to file containing enemy sosig names (one per line)");

            // Basic controls
            spawnAllyKey = config.Bind("Chat Sosigs", "SpawnAllyKey", KeyCode.P,
                "Key to spawn an ally sosig");
            spawnEnemyKey = config.Bind("Chat Sosigs", "SpawnEnemyKey", KeyCode.O,
                "Key to spawn an enemy sosig");
            clearSosigsKey = config.Bind("Chat Sosigs", "ClearSosigsKey", KeyCode.Delete,
                "Key to clear all spawned sosigs");
            armorGUIKey = config.Bind("Chat Sosigs", "ArmorGUIKey", KeyCode.F6,
                "Key to open the armor configuration GUI");

            // Basic settings
            maxActiveSosigs = config.Bind("Chat Sosigs", "MaxActiveSosigs", 10,
                "Maximum number of active sosigs");
            enableNameplates = config.Bind("Chat Sosigs", "ShowNames", true,
                "Show names above sosigs");

            // Set the file paths
            allyNamesFilePath = allyFilePath.Value;
            enemyNamesFilePath = enemyFilePath.Value;

            // Create example files if they don't exist
            CreateExampleFilesIfNeeded();
        }

        void Update()
        {
            HandleInput();
            HandleGUIInput();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(spawnAllyKey.Value))
            {
                SpawnAllySosig();
            }

            if (Input.GetKeyDown(spawnEnemyKey.Value))
            {
                SpawnEnemySosig();
            }

            if (Input.GetKeyDown(clearSosigsKey.Value))
            {
                ClearAllSosigs();
            }
        }

        private void HandleGUIInput()
        {
            if (Input.GetKeyDown(armorGUIKey.Value))
            {
                showArmorGUI = !showArmorGUI;
                if (showArmorGUI)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        void OnGUI()
        {
            if (showArmorGUI)
            {
                InitializeGUIStyles();
                armorWindowRect = GUI.Window(12345, armorWindowRect, DrawArmorGUI, "Sosig Armor Configuration", windowStyle);
            }
        }

        private void InitializeGUIStyles()
        {
            if (windowStyle == null)
            {
                windowStyle = new GUIStyle(GUI.skin.window);
                windowStyle.fontSize = 12;
                windowStyle.padding = new RectOffset(10, 10, 25, 10);
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 11;
                buttonStyle.margin = new RectOffset(2, 2, 2, 2);
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 10;
                labelStyle.wordWrap = true;
            }

            if (toggleStyle == null)
            {
                toggleStyle = new GUIStyle(GUI.skin.toggle);
                toggleStyle.fontSize = 10;
            }

            if (sliderStyle == null)
            {
                sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            }
        }

        private void DrawArmorGUI(int windowID)
        {
            GUILayout.BeginVertical();

            // Header
            GUILayout.Label("=== Sosig Armor Configuration ===", labelStyle);
            GUILayout.Space(5);

            // Sosig Selection
            DrawSosigSelection();
            GUILayout.Space(10);

            // Scroll area for armor options
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));

            // Armor Slot Configuration
            DrawArmorSlotConfiguration();
            GUILayout.Space(10);

            // Individual Armor Selection
            DrawIndividualArmorSelection();
            GUILayout.Space(10);

            // Armor Preferences
            DrawArmorPreferences();

            GUILayout.EndScrollView();

            // Action buttons
            GUILayout.Space(10);
            DrawActionButtons();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawSosigSelection()
        {
            GUILayout.Label("Active Sosigs:", labelStyle);
            
            if (activeSosigs.Count == 0)
            {
                GUILayout.Label("No active sosigs. Spawn some sosigs first!", labelStyle);
                return;
            }

            // Create sosig selection buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", buttonStyle))
            {
                selectedSosigIndex = -1; // -1 means all sosigs
            }
            if (GUILayout.Button("Deselect", buttonStyle))
            {
                selectedSosigIndex = -2; // -2 means no selection
            }
            GUILayout.EndHorizontal();

            // Individual sosig buttons
            for (int i = 0; i < activeSosigs.Count; i++)
            {
                if (activeSosigs[i] == null) continue;

                GUILayout.BeginHorizontal();
                
                // Selection toggle
                bool isSelected = (selectedSosigIndex == i) || (selectedSosigIndex == -1);
                bool newSelection = GUILayout.Toggle(isSelected, "", toggleStyle, GUILayout.Width(20));
                
                if (newSelection && !isSelected)
                {
                    selectedSosigIndex = i;
                }
                else if (!newSelection && isSelected && selectedSosigIndex == i)
                {
                    selectedSosigIndex = -2;
                }

                // Sosig info
                string sosigInfo = $"Sosig {i + 1} ({(activeSosigs[i].E.IFFCode == 0 ? "Ally" : "Enemy")})";
                GUILayout.Label(sosigInfo, labelStyle);

                // Quick armor buttons
                if (GUILayout.Button("Strip Armor", buttonStyle, GUILayout.Width(80)))
                {
                    StripArmorFromSosig(activeSosigs[i]);
                }
                if (GUILayout.Button("Random Armor", buttonStyle, GUILayout.Width(100)))
                {
                    ApplyRandomArmorToSosig(activeSosigs[i]);
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawArmorSlotConfiguration()
        {
            GUILayout.Label("=== Armor Slot Configuration ===", labelStyle);

            foreach (string slot in armorSlots)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                
                // Slot enable/disable
                GUILayout.BeginHorizontal();
                bool enabled = GetArmorSlotEnabled(slot);
                bool newEnabled = GUILayout.Toggle(enabled, slot, toggleStyle, GUILayout.Width(120));
                SetArmorSlotEnabled(slot, newEnabled);

                if (enabled)
                {
                    // Chance slider
                    float chance = GetArmorSlotChance(slot);
                    GUILayout.Label($"Chance: {(chance * 100):F0}%", labelStyle, GUILayout.Width(80));
                    float newChance = GUILayout.HorizontalSlider(chance, 0f, 1f, sliderStyle, GUI.skin.horizontalSliderThumb, GUILayout.Width(100));
                    SetArmorSlotChance(slot, newChance);
                }

                GUILayout.EndHorizontal();

                if (enabled && availableArmor != null && availableArmor.ContainsKey(slot))
                {
                    int armorCount = availableArmor[slot].Count;
                    GUILayout.Label($"Available: {armorCount} items", labelStyle);
                }

                GUILayout.EndVertical();
                GUILayout.Space(2);
            }
        }

        private void DrawIndividualArmorSelection()
        {
            GUILayout.Label("=== Individual Armor Selection ===", labelStyle);

            if (availableArmor == null)
            {
                GUILayout.Label("Loading armor assets...", labelStyle);
                return;
            }

            foreach (string slot in armorSlots)
            {
                if (!GetArmorSlotEnabled(slot)) continue;
                if (!availableArmor.ContainsKey(slot) || availableArmor[slot].Count == 0) continue;

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"{slot}:", labelStyle);

                // Armor selection dropdown simulation
                GUILayout.BeginHorizontal();
                
                int currentIndex = selectedArmorIndices.ContainsKey(slot) ? selectedArmorIndices[slot] : 0;
                currentIndex = Mathf.Clamp(currentIndex, 0, availableArmor[slot].Count - 1);

                if (GUILayout.Button("?", buttonStyle, GUILayout.Width(25)))
                {
                    currentIndex = (currentIndex - 1 + availableArmor[slot].Count) % availableArmor[slot].Count;
                    selectedArmorIndices[slot] = currentIndex;
                }

                string armorName = "None";
                if (availableArmor[slot].Count > 0 && currentIndex < availableArmor[slot].Count)
                {
                    var armorObj = availableArmor[slot][currentIndex];
                    armorName = armorObj != null ? (armorObj.DisplayName ?? armorObj.ItemID) : "Unknown";
                }
                GUILayout.Label(armorName, labelStyle, GUILayout.MinWidth(150));

                if (GUILayout.Button("?", buttonStyle, GUILayout.Width(25)))
                {
                    currentIndex = (currentIndex + 1) % availableArmor[slot].Count;
                    selectedArmorIndices[slot] = currentIndex;
                }

                if (GUILayout.Button("Apply", buttonStyle, GUILayout.Width(50)))
                {
                    ApplySpecificArmorToSelectedSosigs(slot, currentIndex);
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(2);
            }
        }

        private void DrawArmorPreferences()
        {
            GUILayout.Label("=== Armor Preferences ===", labelStyle);
            GUILayout.BeginVertical(GUI.skin.box);

            currentArmorConfig.preferMilitaryArmor = GUILayout.Toggle(currentArmorConfig.preferMilitaryArmor, "Prefer Military Armor", toggleStyle);
            currentArmorConfig.allowCivilianArmor = GUILayout.Toggle(currentArmorConfig.allowCivilianArmor, "Allow Civilian Armor", toggleStyle);
            currentArmorConfig.allowFuturisticArmor = GUILayout.Toggle(currentArmorConfig.allowFuturisticArmor, "Allow Futuristic Armor", toggleStyle);
            currentArmorConfig.randomizeColors = GUILayout.Toggle(currentArmorConfig.randomizeColors, "Randomize Colors", toggleStyle);

            GUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply Current Config", buttonStyle))
            {
                ApplyCurrentArmorConfigToSelectedSosigs();
            }

            if (GUILayout.Button("Strip Armor", buttonStyle))
            {
                StripArmorFromSelectedSosigs();
            }

            if (GUILayout.Button("Random Armor", buttonStyle))
            {
                ApplyRandomArmorToSelectedSosigs();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save Config", buttonStyle))
            {
                SaveArmorConfiguration();
            }

            if (GUILayout.Button("Load Config", buttonStyle))
            {
                LoadArmorConfiguration();
            }

            if (GUILayout.Button("Close", buttonStyle))
            {
                showArmorGUI = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            GUILayout.EndHorizontal();
        }

        // Armor slot helper methods
        private bool GetArmorSlotEnabled(string slot)
        {
            switch (slot)
            {
                case "Headwear": return currentArmorConfig.enableHeadwear;
                case "Facewear": return currentArmorConfig.enableFacewear;
                case "Eyewear": return currentArmorConfig.enableEyewear;
                case "Torsowear": return currentArmorConfig.enableTorsowear;
                case "Pantswear": return currentArmorConfig.enablePantswear;
                case "PantswearLower": return currentArmorConfig.enablePantswearLower;
                case "Backpacks": return currentArmorConfig.enableBackpacks;
                case "Decorations": return currentArmorConfig.enableDecorations;
                default: return false;
            }
        }

        private void SetArmorSlotEnabled(string slot, bool enabled)
        {
            switch (slot)
            {
                case "Headwear": currentArmorConfig.enableHeadwear = enabled; break;
                case "Facewear": currentArmorConfig.enableFacewear = enabled; break;
                case "Eyewear": currentArmorConfig.enableEyewear = enabled; break;
                case "Torsowear": currentArmorConfig.enableTorsowear = enabled; break;
                case "Pantswear": currentArmorConfig.enablePantswear = enabled; break;
                case "PantswearLower": currentArmorConfig.enablePantswearLower = enabled; break;
                case "Backpacks": currentArmorConfig.enableBackpacks = enabled; break;
                case "Decorations": currentArmorConfig.enableDecorations = enabled; break;
            }
        }

        private float GetArmorSlotChance(string slot)
        {
            switch (slot)
            {
                case "Headwear": return currentArmorConfig.headwearChance;
                case "Facewear": return currentArmorConfig.facewearChance;
                case "Eyewear": return currentArmorConfig.eyewearChance;
                case "Torsowear": return currentArmorConfig.torsowearChance;
                case "Pantswear": return currentArmorConfig.pantswearChance;
                case "PantswearLower": return currentArmorConfig.pantswearLowerChance;
                case "Backpacks": return currentArmorConfig.backpackChance;
                case "Decorations": return currentArmorConfig.decorationChance;
                default: return 0f;
            }
        }

        private void SetArmorSlotChance(string slot, float chance)
        {
            switch (slot)
            {
                case "Headwear": currentArmorConfig.headwearChance = chance; break;
                case "Facewear": currentArmorConfig.facewearChance = chance; break;
                case "Eyewear": currentArmorConfig.eyewearChance = chance; break;
                case "Torsowear": currentArmorConfig.torsowearChance = chance; break;
                case "Pantswear": currentArmorConfig.pantswearChance = chance; break;
                case "PantswearLower": currentArmorConfig.pantswearLowerChance = chance; break;
                case "Backpacks": currentArmorConfig.backpackChance = chance; break;
                case "Decorations": currentArmorConfig.decorationChance = chance; break;
            }
        }

        // Armor application methods
        private void ApplyCurrentArmorConfigToSelectedSosigs()
        {
            var selectedSosigs = GetSelectedSosigs();
            foreach (var sosig in selectedSosigs)
            {
                ApplyArmorConfigurationToSosig(sosig, currentArmorConfig);
            }
            
            if (logger != null)
                logger.LogInfo($"Applied armor configuration to {selectedSosigs.Count} sosigs");
        }

        private void StripArmorFromSelectedSosigs()
        {
            var selectedSosigs = GetSelectedSosigs();
            foreach (var sosig in selectedSosigs)
            {
                StripArmorFromSosig(sosig);
            }
            
            if (logger != null)
                logger.LogInfo($"Stripped armor from {selectedSosigs.Count} sosigs");
        }

        private void ApplyRandomArmorToSelectedSosigs()
        {
            var selectedSosigs = GetSelectedSosigs();
            foreach (var sosig in selectedSosigs)
            {
                ApplyRandomArmorToSosig(sosig);
            }
            
            if (logger != null)
                logger.LogInfo($"Applied random armor to {selectedSosigs.Count} sosigs");
        }

        private void ApplySpecificArmorToSelectedSosigs(string slot, int armorIndex)
        {
            if (!availableArmor.ContainsKey(slot) || armorIndex >= availableArmor[slot].Count)
                return;

            var armorObj = availableArmor[slot][armorIndex];
            var selectedSosigs = GetSelectedSosigs();
            
            foreach (var sosig in selectedSosigs)
            {
                ApplySpecificArmorToSosig(sosig, slot, armorObj);
            }
            
            if (logger != null)
                logger.LogInfo($"Applied {slot} armor to {selectedSosigs.Count} sosigs");
        }

        private List<Sosig> GetSelectedSosigs()
        {
            var result = new List<Sosig>();
            
            if (selectedSosigIndex == -1) // All sosigs
            {
                result.AddRange(activeSosigs.Where(s => s != null));
            }
            else if (selectedSosigIndex >= 0 && selectedSosigIndex < activeSosigs.Count)
            {
                if (activeSosigs[selectedSosigIndex] != null)
                    result.Add(activeSosigs[selectedSosigIndex]);
            }
            
            return result;
        }

        private void StripArmorFromSosig(Sosig sosig)
        {
            if (sosig?.Links == null) return;

            foreach (var link in sosig.Links)
            {
                if (link?.transform == null) continue;
                
                // Find and remove armor components
                var wearables = link.GetComponentsInChildren<SosigWearable>();
                foreach (var wearable in wearables)
                {
                    if (wearable != null)
                        Destroy(wearable.gameObject);
                }
            }
        }

        private void ApplyRandomArmorToSosig(Sosig sosig)
        {
            if (availableArmor == null) return;
            
            // Create a random armor configuration
            var randomConfig = new ArmorConfiguration
            {
                enableHeadwear = UnityEngine.Random.value > 0.3f,
                enableFacewear = UnityEngine.Random.value > 0.7f,
                enableEyewear = UnityEngine.Random.value > 0.6f,
                enableTorsowear = UnityEngine.Random.value > 0.2f,
                enablePantswear = UnityEngine.Random.value > 0.4f,
                enablePantswearLower = UnityEngine.Random.value > 0.8f,
                enableBackpacks = UnityEngine.Random.value > 0.7f,
                enableDecorations = UnityEngine.Random.value > 0.9f,
                
                headwearChance = UnityEngine.Random.Range(0.5f, 1f),
                facewearChance = UnityEngine.Random.Range(0.2f, 0.8f),
                eyewearChance = UnityEngine.Random.Range(0.3f, 0.7f),
                torsowearChance = UnityEngine.Random.Range(0.6f, 1f),
                pantswearChance = UnityEngine.Random.Range(0.5f, 0.9f),
                pantswearLowerChance = UnityEngine.Random.Range(0.2f, 0.6f),
                backpackChance = UnityEngine.Random.Range(0.1f, 0.5f),
                decorationChance = UnityEngine.Random.Range(0.05f, 0.3f)
            };
            
            ApplyArmorConfigurationToSosig(sosig, randomConfig);
        }

        private void ApplySpecificArmorToSosig(Sosig sosig, string slot, FVRObject armorObj)
        {
            if (sosig?.Links == null || armorObj == null) return;
            
            // Find the appropriate link for this armor slot
            SosigLink targetLink = GetLinkForArmorSlot(sosig, slot);
            if (targetLink == null) return;
            
            // Remove existing armor in this slot first
            RemoveArmorFromLink(targetLink, slot);
            
            // Apply new armor
            try
            {
                GameObject armorInstance = Instantiate(armorObj.GetGameObject(), targetLink.transform);
                if (armorInstance != null)
                {
                    var wearable = armorInstance.GetComponent<SosigWearable>();
                    if (wearable != null)
                    {
                        wearable.RegisterWearable(targetLink);
                    }
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to apply {slot} armor: {ex.Message}");
            }
        }

        private void ApplyArmorConfigurationToSosig(Sosig sosig, ArmorConfiguration config)
        {
            if (sosig?.Links == null || availableArmor == null) return;
            
            // Strip existing armor first
            StripArmorFromSosig(sosig);
            
            // Apply armor based on configuration
            foreach (string slot in armorSlots)
            {
                bool slotEnabled = GetArmorSlotEnabledFromConfig(slot, config);
                if (!slotEnabled) continue;
                
                float slotChance = GetArmorSlotChanceFromConfig(slot, config);
                if (UnityEngine.Random.value > slotChance) continue;
                
                if (!availableArmor.ContainsKey(slot) || availableArmor[slot].Count == 0) continue;
                
                // Select random armor from this slot
                var armorObj = availableArmor[slot][UnityEngine.Random.Range(0, availableArmor[slot].Count)];
                ApplySpecificArmorToSosig(sosig, slot, armorObj);
            }
        }

        private bool GetArmorSlotEnabledFromConfig(string slot, ArmorConfiguration config)
        {
            switch (slot)
            {
                case "Headwear": return config.enableHeadwear;
                case "Facewear": return config.enableFacewear;
                case "Eyewear": return config.enableEyewear;
                case "Torsowear": return config.enableTorsowear;
                case "Pantswear": return config.enablePantswear;
                case "PantswearLower": return config.enablePantswearLower;
                case "Backpacks": return config.enableBackpacks;
                case "Decorations": return config.enableDecorations;
                default: return false;
            }
        }

        private float GetArmorSlotChanceFromConfig(string slot, ArmorConfiguration config)
        {
            switch (slot)
            {
                case "Headwear": return config.headwearChance;
                case "Facewear": return config.facewearChance;
                case "Eyewear": return config.eyewearChance;
                case "Torsowear": return config.torsowearChance;
                case "Pantswear": return config.pantswearChance;
                case "PantswearLower": return config.pantswearLowerChance;
                case "Backpacks": return config.backpackChance;
                case "Decorations": return config.decorationChance;
                default: return 0f;
            }
        }

        private SosigLink GetLinkForArmorSlot(Sosig sosig, string slot)
        {
            if (sosig?.Links == null || sosig.Links.Count == 0) return null;
            
            switch (slot)
            {
                case "Headwear":
                case "Facewear":
                case "Eyewear":
                    return sosig.Links.Count > 0 ? sosig.Links[0] : null; // Head link
                    
                case "Torsowear":
                case "Backpacks":
                case "Decorations":
                    return sosig.Links.Count > 1 ? sosig.Links[1] : null; // Torso link
                    
                case "Pantswear":
                case "PantswearLower":
                    return sosig.Links.Count > 2 ? sosig.Links[2] : null; // Leg link
                    
                default:
                    return sosig.Links.Count > 1 ? sosig.Links[1] : null; // Default to torso
            }
        }

        private void RemoveArmorFromLink(SosigLink link, string slotType)
        {
            if (link?.transform == null) return;
            
            var wearables = link.GetComponentsInChildren<SosigWearable>();
            foreach (var wearable in wearables)
            {
                // You could add more sophisticated filtering here based on armor type
                if (wearable != null)
                    Destroy(wearable.gameObject);
            }
        }

        // Configuration save/load
        private void SaveArmorConfiguration()
        {
            try
            {
                string configPath = Path.Combine(BepInEx.Paths.ConfigPath, "H3TVR_ArmorConfig.json");
                string jsonData = JsonUtility.ToJson(currentArmorConfig, true);
                File.WriteAllText(configPath, jsonData);
                
                if (logger != null)
                    logger.LogInfo($"Saved armor configuration to {configPath}");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to save armor configuration: {ex.Message}");
            }
        }

        private void LoadArmorConfiguration()
        {
            try
            {
                string configPath = Path.Combine(BepInEx.Paths.ConfigPath, "H3TVR_ArmorConfig.json");
                if (File.Exists(configPath))
                {
                    string jsonData = File.ReadAllText(configPath);
                    currentArmorConfig = JsonUtility.FromJson<ArmorConfiguration>(jsonData);
                    
                    if (logger != null)
                        logger.LogInfo($"Loaded armor configuration from {configPath}");
                }
                else
                {
                    if (logger != null)
                        logger.LogWarning("No saved armor configuration found");
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to load armor configuration: {ex.Message}");
            }
        }

        public void SpawnAllySosig()
        {
            if (activeSosigs.Count >= maxActiveSosigs.Value)
            {
                if (logger != null)
                    logger.LogWarning("Maximum sosigs reached. Cannot spawn more.");
                return;
            }

            string sosigName = GetRandomNameFromFile(allyNamesFilePath, true);
            if (string.IsNullOrEmpty(sosigName))
            {
                sosigName = "AllyBot";
                if (logger != null)
                    logger.LogWarning("No ally names found, using default name");
            }

            SpawnSosig(sosigName, true);
        }

        public void SpawnEnemySosig()
        {
            if (activeSosigs.Count >= maxActiveSosigs.Value)
            {
                if (logger != null)
                    logger.LogWarning("Maximum sosigs reached. Cannot spawn more.");
                return;
            }

            string sosigName = GetRandomNameFromFile(enemyNamesFilePath, false);
            if (string.IsNullOrEmpty(sosigName))
            {
                sosigName = "EnemyBot";
                if (logger != null)
                    logger.LogWarning("No enemy names found, using default name");
            }

            SpawnSosig(sosigName, false);
        }

        private string GetRandomNameFromFile(string filePath, bool isAlly)
        {
            // Use cached names for better performance
            List<string> cachedNames = isAlly ? cachedAllyNames : cachedEnemyNames;
            
            if (cachedNames == null || cachedNames.Count == 0)
            {
                // Load and cache names
                cachedNames = LoadNamesFromFile(filePath);
                if (isAlly)
                    cachedAllyNames = cachedNames;
                else
                    cachedEnemyNames = cachedNames;
            }
            
            return cachedNames.Count > 0 
                ? cachedNames[UnityEngine.Random.Range(0, cachedNames.Count)]
                : "";
        }

        private List<string> LoadNamesFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new List<string>();

                var lines = File.ReadAllLines(filePath);
                var result = new List<string>();
                
                foreach (string line in lines)
                {
                    if (!string.IsNullOrEmpty(line) && !line.Trim().StartsWith("#"))
                    {
                        string trimmedLine = line.Trim();
                        if (!string.IsNullOrEmpty(trimmedLine))
                        {
                            result.Add(trimmedLine);
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to read names from {filePath}: {ex.Message}");
                return new List<string>();
            }
        }

        private void SpawnSosig(string sosigName, bool isAlly)
        {
            try
            {
                Vector3 spawnPosition = CalculateSpawnPosition();
                
                // Use existing H3VR sosig spawning if available, otherwise basic spawn
                Sosig spawnedSosig = CreateBasicSosig(spawnPosition, sosigName, isAlly);
                
                if (spawnedSosig != null)
                {
                    // Apply current armor configuration to newly spawned sosig
                    ApplyArmorConfigurationToSosig(spawnedSosig, currentArmorConfig);
                    
                    activeSosigs.Add(spawnedSosig);
                    if (logger != null)
                        logger.LogInfo($"Spawned {(isAlly ? "ally" : "enemy")} sosig: {sosigName}");
                }
                else
                {
                    if (logger != null)
                        logger.LogError("Failed to spawn sosig");
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Error spawning sosig {sosigName}: {ex.Message}");
            }
        }

        private Sosig CreateBasicSosig(Vector3 position, string name, bool isAlly)
        {
            // Cache sosig templates for better performance
            if (cachedSosigTemplates == null)
            {
                cachedSosigTemplates = Resources.FindObjectsOfTypeAll<SosigEnemyTemplate>();
            }

            if (cachedSosigTemplates.Length == 0)
            {
                if (logger != null)
                    logger.LogError("No sosig templates found in game");
                return null;
            }

            // Use a random template
            var template = cachedSosigTemplates[UnityEngine.Random.Range(0, cachedSosigTemplates.Length)];
            
            // Spawn the sosig
            if (template.SosigPrefabs.Count == 0)
            {
                if (logger != null)
                    logger.LogError("Template has no sosig prefabs");
                return null;
            }

            var prefab = template.SosigPrefabs[UnityEngine.Random.Range(0, template.SosigPrefabs.Count)];
            GameObject sosigObject = Instantiate(prefab.GetGameObject(), position, Quaternion.identity);
            
            Sosig sosig = sosigObject.GetComponentInChildren<Sosig>();
            if (sosig == null)
            {
                if (logger != null)
                    logger.LogError("Spawned object has no Sosig component");
                Destroy(sosigObject);
                return null;
            }

            // Configure the sosig
            if (template.ConfigTemplates.Count > 0)
            {
                var config = template.ConfigTemplates[UnityEngine.Random.Range(0, template.ConfigTemplates.Count)];
                sosig.Configure(config);
            }

            // Set faction
            sosig.E.IFFCode = isAlly ? 0 : 1;

            // Set behavior
            if (isAlly)
            {
                // Ally follows player
                Vector3 followPoint = GM.CurrentPlayerBody.Head.position + UnityEngine.Random.insideUnitSphere * 2f;
                followPoint.y = GM.CurrentPlayerBody.Head.position.y;
                sosig.CommandAssaultPoint(followPoint);
                sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            }
            else
            {
                // Enemy attacks player
                sosig.CommandAssaultPoint(GM.CurrentPlayerBody.Head.position);
                sosig.SetCurrentOrder(Sosig.SosigOrder.Assault);
            }

            // Add nameplate if enabled
            if (enableNameplates.Value)
            {
                CreateSimpleNameplate(sosig, name, isAlly);
            }

            return sosig;
        }

        private void CreateSimpleNameplate(Sosig sosig, string name, bool isAlly)
        {
            if (sosig.Links.Count < 2) return;

            // Create a simple nameplate
            GameObject nameplate = new GameObject("Nameplate");
            nameplate.transform.SetParent(sosig.Links[1].transform);
            nameplate.transform.localPosition = Vector3.up * 0.3f;
            nameplate.transform.localRotation = Quaternion.identity;

            // Add canvas for UI text
            Canvas canvas = nameplate.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Fix potential null reference
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                canvas.worldCamera = mainCamera;
            }
            
            CanvasScaler scaler = nameplate.AddComponent<CanvasScaler>();
            scaler.scaleFactor = 0.01f;

            // Add text
            GameObject textObject = new GameObject("NameText");
            textObject.transform.SetParent(nameplate.transform);
            
            var text = textObject.AddComponent<UnityEngine.UI.Text>();
            text.text = name;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 20;
            text.color = isAlly ? Color.green : Color.red;
            text.alignment = TextAnchor.MiddleCenter;

            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(150, 30);
            rectTransform.localPosition = Vector3.zero;
        }

        private void CreateExampleFilesIfNeeded()
        {
            if (!File.Exists(allyNamesFilePath))
            {
                string[] exampleAllyNames = {
                    "ChatViewer1",
                    "FriendlyHelper",
                    "SupportGuy",
                    "BackupBuddy",
                    "AllyMcAllyFace"
                };
                File.WriteAllLines(allyNamesFilePath, exampleAllyNames);
                if (logger != null)
                    logger.LogInfo($"Created example ally names file at: {allyNamesFilePath}");
            }

            if (!File.Exists(enemyNamesFilePath))
            {
                string[] exampleEnemyNames = {
                    "TrollUser",
                    "EnemyBot",
                    "HostileViewer",
                    "BadGuy123",
                    "OpponentPlayer"
                };
                File.WriteAllLines(enemyNamesFilePath, exampleEnemyNames);
                if (logger != null)
                    logger.LogInfo($"Created example enemy names file at: {enemyNamesFilePath}");
            }
        }

        private Vector3 CalculateSpawnPosition()
        {
            Vector3 playerPos = GM.CurrentPlayerBody.Head.position;
            Vector3 forward = GM.CurrentPlayerBody.Head.forward;
            Vector3 spawnPos = playerPos + forward * 3f + UnityEngine.Random.insideUnitSphere * 1f;
            spawnPos.y = playerPos.y;
            return spawnPos;
        }

        private IEnumerator UpdateSosigsCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                CleanupDeadSosigs();
            }
        }

        private void CleanupDeadSosigs()
        {
            activeSosigs.RemoveAll(sosig => sosig == null || sosig.BodyState == Sosig.SosigBodyState.Dead);
        }

        public void ClearAllSosigs()
        {
            foreach (var sosig in activeSosigs)
            {
                if (sosig != null)
                {
                    Destroy(sosig.gameObject);
                }
            }
            activeSosigs.Clear();
            if (logger != null)
                logger.LogInfo("Cleared all spawned sosigs");
        }

        // Public API methods for external use
        public void SpawnSosigByName(string name, bool isAlly)
        {
            if (activeSosigs.Count >= maxActiveSosigs.Value)
            {
                if (logger != null)
                    logger.LogWarning("Maximum sosigs reached. Cannot spawn more.");
                return;
            }

            SpawnSosig(name, isAlly);
        }

        // Methods expected by other components
        public void SpawnFriendlyChatSosig()
        {
            SpawnAllySosig();
        }

        public void SpawnEnemyChatSosig()
        {
            SpawnEnemySosig();
        }

        public void QueueChatSpawn(string userName, bool isFriendly = true, string armorSetName = null)
        {
            // For the simplified version, just spawn directly
            SpawnSosig(userName, isFriendly);
        }

        public void ClearAllChatSosigs()
        {
            ClearAllSosigs();
        }

        public ChatSosigStats GetStats()
        {
            CleanupDeadSosigs();
            return new ChatSosigStats
            {
                activeSosigCount = activeSosigs.Count,
                friendlyCount = activeSosigs.Count(s => s != null && s.E.IFFCode == 0),
                enemyCount = activeSosigs.Count(s => s != null && s.E.IFFCode == 1),
                queuedSpawns = 0, // No queue in simplified version
                totalSpawned = activeSosigs.Count
            };
        }

        public List<string> GetAvailableArmorSets()
        {
            if (availableArmor != null)
            {
                return availableArmor.Keys.ToList();
            }
            return new List<string> { "Standard", "Light", "Heavy" };
        }

        public H3TVRImproved GetPlugin()
        {
            return plugin;
        }

        public int GetActiveSosigCount()
        {
            CleanupDeadSosigs();
            return activeSosigs.Count;
        }

        public List<string> GetAllyNames()
        {
            return GetNamesFromFile(allyNamesFilePath);
        }

        public List<string> GetEnemyNames()
        {
            return GetNamesFromFile(enemyNamesFilePath);
        }

        private List<string> GetNamesFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new List<string>();

                var lines = File.ReadAllLines(filePath);
                var result = new List<string>();
                
                foreach (string line in lines)
                {
                    if (!string.IsNullOrEmpty(line) && !line.Trim().StartsWith("#"))
                    {
                        string trimmedLine = line.Trim();
                        if (!string.IsNullOrEmpty(trimmedLine))
                        {
                            result.Add(trimmedLine);
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"Failed to read names from {filePath}: {ex.Message}");
                return new List<string>();
            }
        }

        void OnDestroy()
        {
            // Clean up resources
            StopAllCoroutines();
            ClearAllSosigs();
        }
    }

    // Simple stats class for the simplified chat sosig system
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