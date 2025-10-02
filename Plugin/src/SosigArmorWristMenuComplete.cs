using System;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using BepInEx.Configuration;
using System.Linq;

namespace H3TVR
{
    /// <summary>
    /// Complete Sosig Armor Wrist Menu - Full armor management system
    /// Provides comprehensive armor application with faction support, preset management, and customization
    /// </summary>
    public class SosigArmorWristMenuComplete : MonoBehaviour
    {
        #region Static Instance
        public static SosigArmorWristMenuComplete instance;
        #endregion

        #region Private Fields
        private H3TVRImproved plugin;
        private bool isInitialized = false;
        private bool isMenuVisible = false;
        private Rect menuRect = new Rect(50, 50, 400, 600);
        private Vector2 scrollPosition = Vector2.zero;
        
        // Armor configuration
        private ArmorPresetManager presetManager;
        private Dictionary<string, List<FVRObject>> availableArmor;
        private ArmorConfiguration currentAllyConfig;
        private ArmorConfiguration currentEnemyConfig;
        private bool factionArmorEnabled = true;
        private bool autoApplyArmor = true;
        
        // UI State
        private int selectedPresetIndex = 0;
        private bool showAdvancedOptions = false;
        private string[] presetNames = new string[0];
        #endregion

        #region Configuration Structures
        [System.Serializable]
        public class ArmorConfiguration
        {
            public string presetName = "Default";
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
            
            // Advanced settings
            public bool forceFullArmor = false;
            public bool randomizeColors = false;
            public float armorQuality = 1.0f; // 0.0 = basic, 1.0 = premium
        }

        public class ArmorPreset
        {
            public string name;
            public string description;
            public ArmorConfiguration allyConfig;
            public ArmorConfiguration enemyConfig;
            public bool isBuiltIn = false;
        }
        #endregion

        #region Initialization
        public void Initialize(H3TVRImproved pluginInstance, object wristMenuInstance)
        {
            if (isInitialized) return;
            
            instance = this;
            plugin = pluginInstance;
            
            InitializeArmorSystem();
            LoadArmorPresets();
            SetupDefaultConfigurations();
            
            isInitialized = true;
            
            Debug.Log("[SosigArmorWristMenuComplete] Complete armor wrist menu system initialized successfully");
            ShowMessage("Sosig Armor System Loaded - Press F6 to toggle menu");
        }

        private void InitializeArmorSystem()
        {
            presetManager = new ArmorPresetManager();
            availableArmor = new Dictionary<string, List<FVRObject>>();
            
            // Load available armor from H3VR
            LoadAvailableArmor();
            
            // Initialize configurations
            currentAllyConfig = new ArmorConfiguration
            {
                presetName = "Default Ally",
                headwearChance = 0.8f,
                torsowearChance = 0.9f,
                pantswearChance = 0.7f
            };
            
            currentEnemyConfig = new ArmorConfiguration
            {
                presetName = "Default Enemy",
                headwearChance = 0.7f,
                torsowearChance = 0.8f,
                pantswearChance = 0.6f,
                eyewearChance = 0.5f
            };
        }

        private void LoadAvailableArmor()
        {
            try
            {
                // Use H3VR Asset Loader if available
                if (H3VRAssetLoader.IsInitialized)
                {
                    availableArmor = H3VRAssetLoader.GetAllArmorCategories();
                    Debug.Log($"[SosigArmorWristMenuComplete] Loaded {availableArmor.Values.Sum(list => list.Count)} armor pieces from H3VR Asset Loader");
                }
                else
                {
                    // Fallback to manual ItemManager scanning
                    ScanItemManagerForArmor();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigArmorWristMenuComplete] Failed to load armor: {ex.Message}");
                // Create empty categories as fallback
                InitializeEmptyArmorCategories();
            }
        }

        private void ScanItemManagerForArmor()
        {
            InitializeEmptyArmorCategories();
            
            if (IM.OD == null) return;

            foreach (var kvp in IM.OD)
            {
                FVRObject obj = kvp.Value;
                if (obj == null) continue;

                string objectId = kvp.Key.ToLower();
                
                // Basic armor categorization
                if (IsArmorPiece(objectId))
                {
                    CategorizeArmorPiece(objectId, obj);
                }
            }
            
            Debug.Log($"[SosigArmorWristMenuComplete] Scanned ItemManager - found {availableArmor.Values.Sum(list => list.Count)} armor pieces");
        }

        private void InitializeEmptyArmorCategories()
        {
            availableArmor.Clear();
            availableArmor["Headwear"] = new List<FVRObject>();
            availableArmor["Facewear"] = new List<FVRObject>();
            availableArmor["Eyewear"] = new List<FVRObject>();
            availableArmor["Torsowear"] = new List<FVRObject>();
            availableArmor["Pantswear"] = new List<FVRObject>();
            availableArmor["Backpacks"] = new List<FVRObject>();
            availableArmor["Decorations"] = new List<FVRObject>();
        }

        private bool IsArmorPiece(string objectId)
        {
            string[] armorKeywords = {
                "helmet", "hat", "cap", "mask", "glasses", "goggles", "vest", "armor",
                "chest", "pants", "backpack", "bag", "gear", "uniform", "tactical"
            };
            
            return armorKeywords.Any(keyword => objectId.Contains(keyword));
        }

        private void CategorizeArmorPiece(string objectId, FVRObject obj)
        {
            if (objectId.Contains("helmet") || objectId.Contains("hat") || objectId.Contains("cap"))
                availableArmor["Headwear"].Add(obj);
            else if (objectId.Contains("mask") || objectId.Contains("face"))
                availableArmor["Facewear"].Add(obj);
            else if (objectId.Contains("glasses") || objectId.Contains("goggles"))
                availableArmor["Eyewear"].Add(obj);
            else if (objectId.Contains("vest") || objectId.Contains("armor") || objectId.Contains("chest"))
                availableArmor["Torsowear"].Add(obj);
            else if (objectId.Contains("pants") || objectId.Contains("leg"))
                availableArmor["Pantswear"].Add(obj);
            else if (objectId.Contains("backpack") || objectId.Contains("bag"))
                availableArmor["Backpacks"].Add(obj);
            else
                availableArmor["Decorations"].Add(obj);
        }

        private void LoadArmorPresets()
        {
            presetManager.LoadPresets();
            RefreshPresetNames();
        }

        private void RefreshPresetNames()
        {
            var presets = presetManager.GetAllPresets();
            presetNames = presets.Select(p => p.name).ToArray();
            if (presetNames.Length == 0)
            {
                presetNames = new[] { "Default" };
            }
        }

        private void SetupDefaultConfigurations()
        {
            // Create some default presets if none exist
            if (presetManager.GetAllPresets().Count == 0)
            {
                CreateDefaultPresets();
            }
        }

        private void CreateDefaultPresets()
        {
            // Military preset
            var militaryPreset = new ArmorPreset
            {
                name = "Military Standard",
                description = "Standard military armor configuration",
                isBuiltIn = true,
                allyConfig = new ArmorConfiguration
                {
                    presetName = "Military Ally",
                    headwearChance = 0.9f,
                    torsowearChance = 1.0f,
                    pantswearChance = 0.8f,
                    backpackChance = 0.6f
                },
                enemyConfig = new ArmorConfiguration
                {
                    presetName = "Military Enemy",
                    headwearChance = 0.8f,
                    torsowearChance = 0.9f,
                    pantswearChance = 0.7f,
                    eyewearChance = 0.4f
                }
            };
            
            // Civilian preset
            var civilianPreset = new ArmorPreset
            {
                name = "Civilian",
                description = "Light civilian protection",
                isBuiltIn = true,
                allyConfig = new ArmorConfiguration
                {
                    presetName = "Civilian Ally",
                    headwearChance = 0.3f,
                    torsowearChance = 0.4f,
                    pantswearChance = 0.2f,
                    backpackChance = 0.8f
                },
                enemyConfig = new ArmorConfiguration
                {
                    presetName = "Civilian Enemy",
                    headwearChance = 0.2f,
                    torsowearChance = 0.3f,
                    pantswearChance = 0.1f,
                    facewearChance = 0.6f
                }
            };
            
            // Elite preset
            var elitePreset = new ArmorPreset
            {
                name = "Elite Forces",
                description = "Maximum protection and equipment",
                isBuiltIn = true,
                allyConfig = new ArmorConfiguration
                {
                    presetName = "Elite Ally",
                    headwearChance = 1.0f,
                    facewearChance = 0.8f,
                    eyewearChance = 0.9f,
                    torsowearChance = 1.0f,
                    pantswearChance = 1.0f,
                    backpackChance = 0.8f,
                    decorationChance = 0.3f,
                    forceFullArmor = true
                },
                enemyConfig = new ArmorConfiguration
                {
                    presetName = "Elite Enemy",
                    headwearChance = 1.0f,
                    facewearChance = 0.7f,
                    eyewearChance = 0.8f,
                    torsowearChance = 1.0f,
                    pantswearChance = 1.0f,
                    backpackChance = 0.7f,
                    decorationChance = 0.2f,
                    forceFullArmor = true
                }
            };
            
            presetManager.AddPreset(militaryPreset);
            presetManager.AddPreset(civilianPreset);
            presetManager.AddPreset(elitePreset);
            
            RefreshPresetNames();
        }
        #endregion

        #region Public API
        public void ShowMessage(string message)
        {
            Debug.Log($"[SosigArmorWristMenuComplete] {message}");
        }

        public void ToggleMenu()
        {
            isMenuVisible = !isMenuVisible;
            if (isMenuVisible)
            {
                ShowMessage("Armor Menu Opened - Configure sosig armor settings");
            }
            else
            {
                ShowMessage("Armor Menu Closed");
            }
        }

        public void ApplyArmorToSosig(Sosig sosig, bool isAlly)
        {
            if (!isInitialized || sosig == null || !factionArmorEnabled) return;
            
            try
            {
                ArmorConfiguration config = isAlly ? currentAllyConfig : currentEnemyConfig;
                ApplyArmorConfiguration(sosig, config, isAlly);
                
                Debug.Log($"[SosigArmorWristMenuComplete] Applied {config.presetName} armor to {(isAlly ? "ally" : "enemy")} sosig");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigArmorWristMenuComplete] Failed to apply armor: {ex.Message}");
            }
        }

        public void ApplyFactionArmor(Sosig sosig, bool isAlly)
        {
            ApplyArmorToSosig(sosig, isAlly);
        }

        public void ApplyArmorToNewSosig(Sosig sosig, bool isAlly)
        {
            if (autoApplyArmor)
            {
                ApplyArmorToSosig(sosig, isAlly);
            }
        }

        public bool IsFactionArmorEnabled()
        {
            return factionArmorEnabled && isInitialized;
        }

        public string GetCurrentPresetInfo()
        {
            if (selectedPresetIndex < presetNames.Length)
            {
                return $"Current Preset: {presetNames[selectedPresetIndex]}";
            }
            return "Current Preset: Default";
        }

        public ArmorConfiguration GetAllyConfiguration()
        {
            return currentAllyConfig;
        }

        public ArmorConfiguration GetEnemyConfiguration()
        {
            return currentEnemyConfig;
        }

        public void SetArmorConfiguration(ArmorConfiguration allyConfig, ArmorConfiguration enemyConfig)
        {
            if (allyConfig != null) currentAllyConfig = allyConfig;
            if (enemyConfig != null) currentEnemyConfig = enemyConfig;
        }
        #endregion

        #region Armor Application
        private void ApplyArmorConfiguration(Sosig sosig, ArmorConfiguration config, bool isAlly)
        {
            if (sosig.Links == null || sosig.Links.Count == 0) return;

            // Apply armor pieces based on configuration
            if (config.useHeadwear && ShouldApplyArmor(config.headwearChance, config.forceFullArmor))
            {
                ApplyArmorPiece(sosig, "Headwear", 0); // Head link
            }

            if (config.useFacewear && ShouldApplyArmor(config.facewearChance, config.forceFullArmor))
            {
                ApplyArmorPiece(sosig, "Facewear", 0); // Head link
            }

            if (config.useEyewear && ShouldApplyArmor(config.eyewearChance, config.forceFullArmor))
            {
                ApplyArmorPiece(sosig, "Eyewear", 0); // Head link
            }

            if (config.useTorsowear && ShouldApplyArmor(config.torsowearChance, config.forceFullArmor))
            {
                ApplyArmorPiece(sosig, "Torsowear", 1); // Torso link
            }

            if (config.usePantswear && ShouldApplyArmor(config.pantswearChance, config.forceFullArmor))
            {
                ApplyArmorPiece(sosig, "Pantswear", 2); // Legs link
            }

            if (config.useBackpacks && ShouldApplyArmor(config.backpackChance, config.forceFullArmor))
            {
                ApplyArmorPiece(sosig, "Backpacks", 1); // Torso link
            }

            if (config.useDecorations && ShouldApplyArmor(config.decorationChance, config.forceFullArmor))
            {
                ApplyArmorPiece(sosig, "Decorations", 1); // Torso link
            }
        }

        private bool ShouldApplyArmor(float chance, bool forceFullArmor)
        {
            return forceFullArmor || UnityEngine.Random.value < chance;
        }

        private void ApplyArmorPiece(Sosig sosig, string armorType, int linkIndex)
        {
            if (!availableArmor.ContainsKey(armorType) || availableArmor[armorType].Count == 0)
                return;

            if (linkIndex >= sosig.Links.Count) return;

            try
            {
                var armorList = availableArmor[armorType];
                FVRObject selectedArmor = armorList[UnityEngine.Random.Range(0, armorList.Count)];
                
                if (selectedArmor != null)
                {
                    AttachArmorToLink(sosig.Links[linkIndex], selectedArmor);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SosigArmorWristMenuComplete] Failed to apply {armorType}: {ex.Message}");
            }
        }

        private void AttachArmorToLink(SosigLink link, FVRObject armorObject)
        {
            try
            {
                GameObject armorGO = Instantiate(armorObject.GetGameObject(), link.transform.position, link.transform.rotation);
                armorGO.transform.SetParent(link.transform);
                
                // Try to register as sosig wearable
                SosigWearable wearable = armorGO.GetComponent<SosigWearable>();
                if (wearable != null)
                {
                    wearable.RegisterWearable(link);
                }
                else
                {
                    // Manual attachment if no SosigWearable component
                    armorGO.transform.localPosition = Vector3.zero;
                    armorGO.transform.localRotation = Quaternion.identity;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SosigArmorWristMenuComplete] Failed to attach armor {armorObject.ItemID}: {ex.Message}");
            }
        }
        #endregion

        #region Unity Lifecycle and Input
        private void Update()
        {
            // Toggle menu with F6 key
            if (Input.GetKeyDown(KeyCode.F6))
            {
                ToggleMenu();
            }
            
            // Quick preset switching with number keys while menu is open
            if (isMenuVisible)
            {
                for (int i = 1; i <= 9; i++)
                {
                    if (Input.GetKeyDown((KeyCode)(KeyCode.Alpha1 + i - 1)))
                    {
                        if (i - 1 < presetNames.Length)
                        {
                            selectedPresetIndex = i - 1;
                            ApplySelectedPreset();
                            ShowMessage($"Switched to preset: {presetNames[selectedPresetIndex]}");
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!isMenuVisible || !isInitialized) return;

            GUI.skin = null; // Use default Unity skin
            menuRect = GUI.Window(12345, menuRect, DrawArmorMenu, "H3TVR Sosig Armor System");
        }

        private void DrawArmorMenu(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            // Header
            GUILayout.Label("=== Sosig Armor Configuration ===", GUI.skin.box);
            
            // Enable/Disable toggle
            factionArmorEnabled = GUILayout.Toggle(factionArmorEnabled, "Enable Faction Armor System");
            autoApplyArmor = GUILayout.Toggle(autoApplyArmor, "Auto-Apply Armor to New Sosigs");
            
            GUILayout.Space(10);
            
            // Preset selection
            GUILayout.Label("Armor Presets:", GUI.skin.label);
            if (presetNames.Length > 0)
            {
                int newPresetIndex = GUILayout.SelectionGrid(selectedPresetIndex, presetNames, 2);
                if (newPresetIndex != selectedPresetIndex)
                {
                    selectedPresetIndex = newPresetIndex;
                    ApplySelectedPreset();
                }
            }
            
            GUILayout.Space(10);
            
            // Quick actions
            GUILayout.Label("Quick Actions:", GUI.skin.label);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Current as Preset"))
            {
                SaveCurrentAsPreset();
            }
            if (GUILayout.Button("Reset to Defaults"))
            {
                ResetToDefaults();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Advanced options toggle
            showAdvancedOptions = GUILayout.Toggle(showAdvancedOptions, "Show Advanced Options");
            
            if (showAdvancedOptions)
            {
                DrawAdvancedOptions();
            }
            
            // Faction configurations
            DrawFactionConfiguration("Ally Configuration", currentAllyConfig);
            GUILayout.Space(5);
            DrawFactionConfiguration("Enemy Configuration", currentEnemyConfig);
            
            // Status information
            GUILayout.Space(10);
            DrawStatusInformation();
            
            // Close button
            if (GUILayout.Button("Close Menu"))
            {
                isMenuVisible = false;
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawAdvancedOptions()
        {
            GUILayout.Label("=== Advanced Options ===", GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Armor Assets"))
            {
                LoadAvailableArmor();
                ShowMessage("Armor assets reloaded");
            }
            if (GUILayout.Button("Export Configuration"))
            {
                ExportConfiguration();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Import Configuration"))
            {
                ImportConfiguration();
            }
            if (GUILayout.Button("Reset All Presets"))
            {
                ResetAllPresets();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawFactionConfiguration(string title, ArmorConfiguration config)
        {
            GUILayout.Label($"=== {title} ===", GUI.skin.box);
            
            // Armor type toggles
            config.useHeadwear = GUILayout.Toggle(config.useHeadwear, "Headwear");
            if (config.useHeadwear)
            {
                config.headwearChance = GUILayout.HorizontalSlider(config.headwearChance, 0f, 1f);
                GUILayout.Label($"Chance: {(config.headwearChance * 100):F0}%");
            }
            
            config.useFacewear = GUILayout.Toggle(config.useFacewear, "Facewear");
            if (config.useFacewear)
            {
                config.facewearChance = GUILayout.HorizontalSlider(config.facewearChance, 0f, 1f);
                GUILayout.Label($"Chance: {(config.facewearChance * 100):F0}%");
            }
            
            config.useEyewear = GUILayout.Toggle(config.useEyewear, "Eyewear");
            if (config.useEyewear)
            {
                config.eyewearChance = GUILayout.HorizontalSlider(config.eyewearChance, 0f, 1f);
                GUILayout.Label($"Chance: {(config.eyewearChance * 100):F0}%");
            }
            
            config.useTorsowear = GUILayout.Toggle(config.useTorsowear, "Torsowear");
            if (config.useTorsowear)
            {
                config.torsowearChance = GUILayout.HorizontalSlider(config.torsowearChance, 0f, 1f);
                GUILayout.Label($"Chance: {(config.torsowearChance * 100):F0}%");
            }
            
            config.usePantswear = GUILayout.Toggle(config.usePantswear, "Pantswear");
            if (config.usePantswear)
            {
                config.pantswearChance = GUILayout.HorizontalSlider(config.pantswearChance, 0f, 1f);
                GUILayout.Label($"Chance: {(config.pantswearChance * 100):F0}%");
            }
            
            config.useBackpacks = GUILayout.Toggle(config.useBackpacks, "Backpacks");
            if (config.useBackpacks)
            {
                config.backpackChance = GUILayout.HorizontalSlider(config.backpackChance, 0f, 1f);
                GUILayout.Label($"Chance: {(config.backpackChance * 100):F0}%");
            }
            
            config.useDecorations = GUILayout.Toggle(config.useDecorations, "Decorations");
            if (config.useDecorations)
            {
                config.decorationChance = GUILayout.HorizontalSlider(config.decorationChance, 0f, 1f);
                GUILayout.Label($"Chance: {(config.decorationChance * 100):F0}%");
            }
            
            // Advanced settings
            if (showAdvancedOptions)
            {
                config.forceFullArmor = GUILayout.Toggle(config.forceFullArmor, "Force Full Armor");
                config.randomizeColors = GUILayout.Toggle(config.randomizeColors, "Randomize Colors");
                
                GUILayout.Label("Armor Quality:");
                config.armorQuality = GUILayout.HorizontalSlider(config.armorQuality, 0f, 1f);
                GUILayout.Label($"Quality: {(config.armorQuality * 100):F0}%");
            }
        }

        private void DrawStatusInformation()
        {
            GUILayout.Label("=== Status Information ===", GUI.skin.box);
            GUILayout.Label($"System Initialized: {isInitialized}");
            GUILayout.Label($"Faction Armor Enabled: {factionArmorEnabled}");
            GUILayout.Label($"Available Presets: {presetNames.Length}");
            GUILayout.Label($"Loaded Armor Categories: {availableArmor.Count}");
            GUILayout.Label($"Total Armor Pieces: {availableArmor.Values.Sum(list => list.Count)}");
            
            // Show breakdown by category
            foreach (var category in availableArmor)
            {
                if (category.Value.Count > 0)
                {
                    GUILayout.Label($"  {category.Key}: {category.Value.Count}");
                }
            }
        }
        #endregion

        #region Preset Management
        private void ApplySelectedPreset()
        {
            if (selectedPresetIndex < 0 || selectedPresetIndex >= presetNames.Length) return;
            
            var presets = presetManager.GetAllPresets();
            if (selectedPresetIndex < presets.Count)
            {
                var preset = presets[selectedPresetIndex];
                currentAllyConfig = preset.allyConfig ?? currentAllyConfig;
                currentEnemyConfig = preset.enemyConfig ?? currentEnemyConfig;
                
                Debug.Log($"[SosigArmorWristMenuComplete] Applied preset: {preset.name}");
            }
        }

        private void SaveCurrentAsPreset()
        {
            string presetName = $"Custom_{DateTime.Now:yyyyMMdd_HHmmss}";
            var newPreset = new ArmorPreset
            {
                name = presetName,
                description = "User-created preset",
                allyConfig = CloneConfiguration(currentAllyConfig),
                enemyConfig = CloneConfiguration(currentEnemyConfig),
                isBuiltIn = false
            };
            
            presetManager.AddPreset(newPreset);
            RefreshPresetNames();
            
            ShowMessage($"Saved current configuration as: {presetName}");
        }

        private ArmorConfiguration CloneConfiguration(ArmorConfiguration original)
        {
            return new ArmorConfiguration
            {
                presetName = original.presetName,
                useHeadwear = original.useHeadwear,
                useFacewear = original.useFacewear,
                useEyewear = original.useEyewear,
                useTorsowear = original.useTorsowear,
                usePantswear = original.usePantswear,
                useBackpacks = original.useBackpacks,
                useDecorations = original.useDecorations,
                headwearChance = original.headwearChance,
                facewearChance = original.facewearChance,
                eyewearChance = original.eyewearChance,
                torsowearChance = original.torsowearChance,
                pantswearChance = original.pantswearChance,
                backpackChance = original.backpackChance,
                decorationChance = original.decorationChance,
                forceFullArmor = original.forceFullArmor,
                randomizeColors = original.randomizeColors,
                armorQuality = original.armorQuality
            };
        }

        private void ResetToDefaults()
        {
            currentAllyConfig = new ArmorConfiguration
            {
                presetName = "Default Ally",
                headwearChance = 0.8f,
                torsowearChance = 0.9f,
                pantswearChance = 0.7f
            };
            
            currentEnemyConfig = new ArmorConfiguration
            {
                presetName = "Default Enemy",
                headwearChance = 0.7f,
                torsowearChance = 0.8f,
                pantswearChance = 0.6f,
                eyewearChance = 0.5f
            };
            
            ShowMessage("Reset to default configurations");
        }

        private void ExportConfiguration()
        {
            // TODO: Implement configuration export to JSON/INI file
            ShowMessage("Configuration export feature coming soon");
        }

        private void ImportConfiguration()
        {
            // TODO: Implement configuration import from JSON/INI file
            ShowMessage("Configuration import feature coming soon");
        }

        private void ResetAllPresets()
        {
            presetManager.ClearPresets();
            CreateDefaultPresets();
            selectedPresetIndex = 0;
            ShowMessage("All presets reset to defaults");
        }
        #endregion
    }

    #region Helper Classes
    public class ArmorPresetManager
    {
        private List<SosigArmorWristMenuComplete.ArmorPreset> presets = new List<SosigArmorWristMenuComplete.ArmorPreset>();

        public void LoadPresets()
        {
            // TODO: Load presets from file system
            // For now, presets are created in-memory
        }

        public void SavePresets()
        {
            // TODO: Save presets to file system
        }

        public void AddPreset(SosigArmorWristMenuComplete.ArmorPreset preset)
        {
            presets.Add(preset);
        }

        public List<SosigArmorWristMenuComplete.ArmorPreset> GetAllPresets()
        {
            return new List<SosigArmorWristMenuComplete.ArmorPreset>(presets);
        }

        public void ClearPresets()
        {
            presets.Clear();
        }

        public SosigArmorWristMenuComplete.ArmorPreset GetPreset(string name)
        {
            return presets.FirstOrDefault(p => p.name == name);
        }
    }
    #endregion
}